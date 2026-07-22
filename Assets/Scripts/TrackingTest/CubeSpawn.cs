using System.Collections.Generic;
using UnityEngine;
using Microsoft.Azure.Kinect.BodyTracking;

/// <summary>
/// Azure Kinect の骨格追跡を利用して、手の動き（パンチ・スワイプ）に基づき
/// キューブを生成・消去するテスト用コンポーネントです。
/// </summary>
public class CubeSpawn : MonoBehaviour
{
    /// <summary>
    /// 生成したキューブとその生成時間を保持する構造体
    /// </summary>
    private struct SpawnedCubeInfo
    {
        public GameObject cubeObject;
        public float spawnTime;

        public SpawnedCubeInfo(GameObject cubeObject, float spawnTime)
        {
            this.cubeObject = cubeObject;
            this.spawnTime = spawnTime;
        }
    }

    /// <summary>
    /// 手のスワイプ動作（上に払う）の進捗を追跡する構造体
    /// </summary>
    private struct SwipeTracker
    {
        public float cumulativeDistance; // 上方向への累積移動距離（メートル）
        public Vector3 startPosition;    // スワイプ（払い）動作を開始した位置

        /// <summary>
        /// トラッカーの状態をリセットします
        /// </summary>
        /// <param name="currentPosition">リセット時の基準となる現在位置</param>
        public void Reset(Vector3 currentPosition)
        {
            cumulativeDistance = 0f;
            startPosition = currentPosition;
        }
    }

    [Header("Kinect Settings")]
    [SerializeField]
    [Tooltip("Azure Kinectのトラッキングデータを管理するハンドラー")]
    private TrackerHandler trackerHandler;

    [Header("Cube Settings")]
    [SerializeField]
    [Tooltip("手の位置に生成するキューブのプレハブ")]
    private GameObject cubePrefab;

    [Header("Gesture - Forward Spawn")]
    [SerializeField]
    [Tooltip("手が肩よりどれだけ前に出たらキューブを生成するか（メートル）")]
    private float forwardThreshold = 0.25f;

    [Header("Gesture - Swipe Erase")]
    [SerializeField]
    [Tooltip("手を上に払う動作と判定するY軸方向の最低速度（m/s）")]
    private float swipeUpSpeedThreshold = 1.5f;

    [SerializeField]
    [Tooltip("手を上に払う動作と判定するために必要な最低移動距離（メートル）")]
    private float swipeDistanceThreshold = 0.15f;

    [SerializeField]
    [Tooltip("手の軌道からキューブを消去する判定半径（メートル）")]
    private float eraseRadius = 0.3f;

    [SerializeField]
    [Tooltip("生成されたキューブが即座に消去されないように保護する時間（秒）")]
    private float spawnProtectionDuration = 0.3f;

    // 前フレームで手が前に出ていたかどうかの状態フラグ
    private bool wasRightHandForward;
    private bool wasLeftHandForward;

    // 前フレームの手の位置（速度計算用）
    private Vector3 prevRightHandPosition;
    private Vector3 prevLeftHandPosition;

    // 左右の手のスワイプ進捗トラッカー
    private SwipeTracker rightHandSwipeTracker;
    private SwipeTracker leftHandSwipeTracker;

    // アプリケーション起動直後の1フレーム目であるかを管理するフラグ
    // （初回フレームでの予期せぬ大きな速度計算や状態の不一致を避けるため）
    private bool isFirstFrame = true;

    // 生成したキューブの情報を管理するリスト
    private List<SpawnedCubeInfo> spawnedCubes = new List<SpawnedCubeInfo>();

    private void Update()
    {
        // 必要なコンポーネントが設定されていない場合は処理を行わない
        if (trackerHandler == null || cubePrefab == null)
        {
            return;
        }

        // 1. 各関節の現在位置を取得
        Vector3 currentRightHandPos = trackerHandler.GetJointWorldPosition(JointId.HandRight);
        Vector3 currentLeftHandPos = trackerHandler.GetJointWorldPosition(JointId.HandLeft);
        Vector3 rightShoulderPos = trackerHandler.GetJointWorldPosition(JointId.ShoulderRight);
        Vector3 leftShoulderPos = trackerHandler.GetJointWorldPosition(JointId.ShoulderLeft);

        // 2. 既に破棄されたキューブをリストからクリーンアップ
        CleanupDestroyedCubes();

        // 3. 初回フレームでなければジェスチャー判定とキューブ生成・消去を行う
        if (!isFirstFrame)
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime > 0f)
            {
                // 右手の処理（スワイプ追跡、生成・消去判定）
                UpdateSwipeTracking(currentRightHandPos, prevRightHandPosition, ref rightHandSwipeTracker);
                ProcessHandInteractions(
                    currentRightHandPos,
                    rightShoulderPos,
                    ref wasRightHandForward,
                    ref rightHandSwipeTracker,
                    deltaTime
                );

                // 左手の処理（スワイプ追跡、生成・消去判定）
                UpdateSwipeTracking(currentLeftHandPos, prevLeftHandPosition, ref leftHandSwipeTracker);
                ProcessHandInteractions(
                    currentLeftHandPos,
                    leftShoulderPos,
                    ref wasLeftHandForward,
                    ref leftHandSwipeTracker,
                    deltaTime
                );
            }
        }
        else
        {
            // 初回フレーム時はスワイプの開始位置を初期化
            rightHandSwipeTracker.Reset(currentRightHandPos);
            leftHandSwipeTracker.Reset(currentLeftHandPos);
        }

        // 4. 次フレームのために現在の手の位置を保存
        prevRightHandPosition = currentRightHandPos;
        prevLeftHandPosition = currentLeftHandPos;
        isFirstFrame = false;
    }

    /// <summary>
    /// 手の上方向への移動を監視し、スワイプ動作の累積距離を更新します
    /// </summary>
    /// <param name="currHandPos">現在の手の位置</param>
    /// <param name="prevHandPos">前フレームの手の位置</param>
    /// <param name="tracker">更新対象のスワイプトラッカー</param>
    private void UpdateSwipeTracking(Vector3 currHandPos, Vector3 prevHandPos, ref SwipeTracker tracker)
    {
        // Y軸（高さ方向）のフレーム間移動量を計算
        float deltaY = currHandPos.y - prevHandPos.y;

        // 手が上方向に動いている場合のみ移動量を累積
        if (deltaY > 0f)
        {
            tracker.cumulativeDistance += deltaY;
        }
        else
        {
            // 手が下方向に動いた、あるいは静止した場合は累積をリセットし、
            // 新たなスワイプ開始候補位置として現在位置を設定
            tracker.Reset(currHandPos);
        }
    }

    /// <summary>
    /// 手の動きに基づくインタラクション（キューブ生成および消去）を一括処理します
    /// </summary>
    /// <param name="currHandPos">現在の手の位置</param>
    /// <param name="shoulderPos">肩の位置</param>
    /// <param name="wasHandForward">前フレームで手が前に出ていたかどうかの状態参照</param>
    /// <param name="tracker">該当する手のリファレンススワイプトラッカー</param>
    /// <param name="deltaTime">フレーム間の経過時間</param>
    private void ProcessHandInteractions(Vector3 currHandPos, Vector3 shoulderPos, ref bool wasHandForward, ref SwipeTracker tracker, float deltaTime)
    {
        // --- 1. キューブ生成判定 (パンチジェスチャー) ---
        bool isHandForward = IsHandForward(currHandPos, shoulderPos);
        
        // 手が新しく前に出た瞬間にキューブを生成
        if (isHandForward && !wasHandForward)
        {
            SpawnCube(currHandPos);
        }
        wasHandForward = isHandForward;

        // --- 2. キューブ消去判定 (スワイプアップジェスチャー) ---
        // 「上方向の移動速度が閾値を超えている」かつ「上方向への累積移動距離が設定値を超えている」場合
        float velocityY = (currHandPos.y - tracker.startPosition.y) / deltaTime; // スワイプ開始からの平均速度
        
        if (velocityY > swipeUpSpeedThreshold && tracker.cumulativeDistance >= swipeDistanceThreshold)
        {
            // スワイプ開始位置から現在位置までの軌道上のキューブを消去
            EraseCubesOnTrajectory(tracker.startPosition, currHandPos);

            // 消去を実行したため、同じ一連の動作で何度も判定されないようにトラッカーをリセット
            tracker.Reset(currHandPos);
        }
    }

    /// <summary>
    /// 手が肩より一定距離以上、前に出ているかどうかを判定します
    /// </summary>
    /// <param name="handPos">手の位置</param>
    /// <param name="shoulderPos">肩の位置</param>
    /// <returns>前に出ている場合はtrue</returns>
    private bool IsHandForward(Vector3 handPos, Vector3 shoulderPos)
    {
        // Z軸方向（Azure Kinectの奥行き方向）で、手が肩より手前に出ているかを判定
        return handPos.z < shoulderPos.z - forwardThreshold;
    }

    /// <summary>
    /// 手の位置にキューブを生成し、生成時間とともにリストに追加します
    /// </summary>
    /// <param name="position">生成するワールド座標</param>
    private void SpawnCube(Vector3 position)
    {
        GameObject newCube = Instantiate(cubePrefab, position, Quaternion.identity);
        if (newCube != null)
        {
            spawnedCubes.Add(new SpawnedCubeInfo(newCube, Time.time));
        }
    }

    /// <summary>
    /// 手の前フレームから現フレームへの移動軌道（線分）の近くにあるキューブを消去します
    /// </summary>
    /// <param name="startPoint">軌道の始点（スワイプ開始時の手の位置）</param>
    /// <param name="endPoint">軌道の終点（現在の手の位置）</param>
    private void EraseCubesOnTrajectory(Vector3 startPoint, Vector3 endPoint)
    {
        // 削除対象のキューブを一時保存するリスト
        List<SpawnedCubeInfo> cubesToDestroy = new List<SpawnedCubeInfo>();

        float currentTime = Time.time;

        // リスト内の各キューブに対して、手の移動軌道（線分）との距離および生成時間を確認
        foreach (SpawnedCubeInfo cubeInfo in spawnedCubes)
        {
            if (cubeInfo.cubeObject == null) continue;

            // 生成直後の保護期間内にあるキューブは消去判定から除外する
            if (currentTime - cubeInfo.spawnTime < spawnProtectionDuration)
            {
                continue;
            }

            // 線分 (startPoint -> endPoint) とキューブ座標の最短距離を算出
            float distance = DistanceToSegment(cubeInfo.cubeObject.transform.position, startPoint, endPoint);

            // 最短距離が消去半径以内であれば削除対象に追加
            if (distance <= eraseRadius)
            {
                cubesToDestroy.Add(cubeInfo);
            }
        }

        // 対象のキューブをゲームから削除し、管理リストからも除外する
        foreach (SpawnedCubeInfo cubeInfo in cubesToDestroy)
        {
            spawnedCubes.Remove(cubeInfo);
            Destroy(cubeInfo.cubeObject);
        }
    }

    /// <summary>
    /// リスト内から既に破棄された（Nullの）ゲームオブジェクト情報をごっそり除外します
    /// </summary>
    private void CleanupDestroyedCubes()
    {
        spawnedCubes.RemoveAll(item => item.cubeObject == null);
    }

    /// <summary>
    /// 点 P と線分 AB の最短距離を計算します
    /// </summary>
    /// <param name="p">対象 of 点（キューブの位置）</param>
    /// <param name="a">線分の始点</param>
    /// <param name="b">線分の終点</param>
    /// <returns>最短距離（メートル）</returns>
    private float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        Vector3 ap = p - a;
        float abLenSq = ab.sqrMagnitude;

        // 始点と終点がほぼ同じ位置にある場合は、点と点の距離を返す
        if (abLenSq < 0.0001f)
        {
            return Vector3.Distance(p, a);
        }

        // 線分上への射影位置の比率 t (0.0〜1.0) を算出
        float t = Vector3.Dot(ap, ab) / abLenSq;
        t = Mathf.Clamp01(t); // 線分の外に外れないようにクランプ

        // 最短となる線分上の座標を特定し、そこから点 P への距離を返す
        Vector3 closestPoint = a + t * ab;
        return Vector3.Distance(p, closestPoint);
    }
}
