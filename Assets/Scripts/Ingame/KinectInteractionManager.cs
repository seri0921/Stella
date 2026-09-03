using System.Collections.Generic;
using UnityEngine;
using Microsoft.Azure.Kinect.BodyTracking;

public class KinectInteractionManager : MonoBehaviour
{
    private struct SwipeTracker
    {
        public float cumulativeDistance;
        public Vector3 startPosition;

        public void Reset(Vector3 currentPosition)
        {
            cumulativeDistance = 0f;
            startPosition = currentPosition;
        }
    }

    [Header("Kinect 接続設定")]
    [SerializeField] private TrackerHandler trackerHandler;

    [Header("タップ（突き出し）判定設定")]
    [SerializeField] private float forwardThreshold = 0.25f; // 肩から手までのZ軸方向の突き出し距離（m）
    [SerializeField] private float tapRadius = 0.3f; // タップ判定の球体半径

    [Header("スワイプ判定設定")]
    [SerializeField] private float swipeSpeedThreshold = 1.2f; // スワイプ平均速度（m/s）
    [SerializeField] private float swipeDistanceThreshold = 0.12f; // 最低移動距離（m）
    [SerializeField] private float swipeRadius = 0.25f; // スワイプ判定のカプセル半径

    [Header("デバッグ用（マウス）設定")]
    [SerializeField] private bool enableMouseDebug = true;
    [SerializeField] private Camera mainCamera;

    // 前フレームの手の状態
    private bool wasRightHandForward;
    private bool wasLeftHandForward;
    private Vector3 prevRightHandPos;
    private Vector3 prevLeftHandPos;

    // スワイプトラッカー
    private SwipeTracker rightHandSwipeTracker;
    private SwipeTracker leftHandSwipeTracker;

    private bool isFirstFrame = true;

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (trackerHandler == null)
        {
            trackerHandler = FindObjectOfType<TrackerHandler>();
        }
    }

    private void Update()
    {
        if (IngameGameManager.Instance == null) return;

        IngameGameManager.GamePhase phase = IngameGameManager.Instance.CurrentPhase;

        // プレイ中（前半お菓子、後半ゴミ掃除）以外のフェーズではインタラクション処理をしない
        if (phase != IngameGameManager.GamePhase.EatingSnucks && phase != IngameGameManager.GamePhase.CleaningTrash)
        {
            isFirstFrame = true;
            return;
        }

        // --- 1. Kinectによる操作処理 ---
        bool hasKinectData = ProcessKinectInteractions(phase);

        // --- 2. Kinectデータが無い場合のデバッグ用マウス操作（オプション） ---
        if (!hasKinectData && enableMouseDebug)
        {
            ProcessMouseDebugInteractions(phase);
        }
    }

    /// <summary>
    /// Kinectのデータに基づいてインタラクションを処理します。
    /// トラッキングデータが存在して処理が行われた場合は true を返します。
    /// </summary>
    private bool ProcessKinectInteractions(IngameGameManager.GamePhase phase)
    {
        // TrackerHandlerがアタッチされていない、または有効な関節情報が得られない場合はスキップ
        if (trackerHandler == null) return false;

        // 右手・左手・両肩の位置を取得
        // 関節位置が正常に取得できない場合は (0,0,0) 付近になるため、値の検証を行います
        Vector3 rightHand = trackerHandler.GetJointWorldPosition(JointId.HandRight);
        Vector3 leftHand = trackerHandler.GetJointWorldPosition(JointId.HandLeft);
        Vector3 rightShoulder = trackerHandler.GetJointWorldPosition(JointId.ShoulderRight);
        Vector3 leftShoulder = trackerHandler.GetJointWorldPosition(JointId.ShoulderLeft);

        // 簡易的なデータ有効性チェック（両手の距離が極端に原点に近すぎる場合はトラッキング未開始とみなす）
        if (rightHand.sqrMagnitude < 0.001f && leftHand.sqrMagnitude < 0.001f)
        {
            return false;
        }

        if (isFirstFrame)
        {
            rightHandSwipeTracker.Reset(rightHand);
            leftHandSwipeTracker.Reset(leftHand);
            prevRightHandPos = rightHand;
            prevLeftHandPos = leftHand;
            isFirstFrame = false;
            return true;
        }

        float deltaTime = Time.deltaTime;
        if (deltaTime > 0f)
        {
            // --- 前半（お菓子を食べる）の処理 ---
            if (phase == IngameGameManager.GamePhase.EatingSnucks)
            {
                // 右手の突き出し（タップ）判定
                bool isRightForward = IsHandForward(rightHand, rightShoulder);
                if (isRightForward && !wasRightHandForward)
                {
                    PerformTapInteraction(rightHand);
                }
                wasRightHandForward = isRightForward;

                // 左手の突き出し（タップ）判定
                bool isLeftForward = IsHandForward(leftHand, leftShoulder);
                if (isLeftForward && !wasLeftHandForward)
                {
                    PerformTapInteraction(leftHand);
                }
                wasLeftHandForward = isLeftForward;
            }

            // --- 後半（ゴミ掃除）の処理 ---
            else if (phase == IngameGameManager.GamePhase.CleaningTrash)
            {
                // 右手のスワイプ更新と判定
                UpdateSwipeTracking(rightHand, prevRightHandPos, ref rightHandSwipeTracker);
                ProcessSwipeInteraction(rightHand, ref rightHandSwipeTracker, deltaTime);

                // 左手のスワイプ更新と判定
                UpdateSwipeTracking(leftHand, prevLeftHandPos, ref leftHandSwipeTracker);
                ProcessSwipeInteraction(leftHand, ref leftHandSwipeTracker, deltaTime);
            }
        }

        prevRightHandPos = rightHand;
        prevLeftHandPos = leftHand;
        return true;
    }

    /// <summary>
    /// 手が肩より一定距離前に出ているかを判定します（Z方向）。
    /// Kinect座標系では前に出すとZ座標が小さくなる（カメラに近づく）特性を考慮します。
    /// </summary>
    private bool IsHandForward(Vector3 handPos, Vector3 shoulderPos)
    {
        return handPos.z < shoulderPos.z - forwardThreshold;
    }

    /// <summary>
    /// 手の突き出し位置（3D座標）の周囲にあるお菓子をタップ（捕食）します。
    /// </summary>
    private void PerformTapInteraction(Vector3 handPosition)
    {
        // 手の位置を中心に球体でコライダーを検出
        Collider[] hitColliders = Physics.OverlapSphere(handPosition, tapRadius);
        foreach (var col in hitColliders)
        {
            SnuckObject snuck = col.GetComponent<SnuckObject>();
            if (snuck != null)
            {
                snuck.Eat();
                Debug.Log("Kinect Tap: お菓子を食べました。");
            }
        }
    }

    /// <summary>
    /// 手のスワイプ移動量を累積します。
    /// </summary>
    private void UpdateSwipeTracking(Vector3 currHandPos, Vector3 prevHandPos, ref SwipeTracker tracker)
    {
        float distance = Vector3.Distance(currHandPos, prevHandPos);
        
        // 動きが検知されている場合は累積
        if (distance > 0.01f)
        {
            tracker.cumulativeDistance += distance;
        }
        else
        {
            // 動いていない場合は開始地点をリセット
            tracker.Reset(currHandPos);
        }
    }

    /// <summary>
    /// 手の移動がスワイプ速度と距離を満たしている場合、その軌道上のゴミを消去します。
    /// </summary>
    private void ProcessSwipeInteraction(Vector3 currHandPos, ref SwipeTracker tracker, float deltaTime)
    {
        float velocity = Vector3.Distance(currHandPos, tracker.startPosition) / deltaTime;

        // 一定速度以上かつ一定距離以上の移動をスワイプと判定
        if (velocity > swipeSpeedThreshold && tracker.cumulativeDistance >= swipeDistanceThreshold)
        {
            // スワイプの始点から終点（現在位置）までのカプセル範囲内のコライダーを検出
            Collider[] hitColliders = Physics.OverlapCapsule(tracker.startPosition, currHandPos, swipeRadius);
            foreach (var col in hitColliders)
            {
                TrashObject trash = col.GetComponent<TrashObject>();
                if (trash != null)
                {
                    trash.Clean();
                    Debug.Log("Kinect Swipe: ゴミを掃除しました。");
                }
            }

            // 判定後にトラッカーをリセット
            tracker.Reset(currHandPos);
        }
    }

    /// <summary>
    /// マウスによるデバッグ用のインタラクション処理。
    /// </summary>
    private void ProcessMouseDebugInteractions(IngameGameManager.GamePhase phase)
    {
        if (mainCamera == null) return;

        // --- 前半：お菓子を食べる ---
        if (phase == IngameGameManager.GamePhase.EatingSnucks)
        {
            // 左クリックした瞬間にRayを投射
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    SnuckObject snuck = hit.collider.GetComponent<SnuckObject>();
                    if (snuck != null)
                    {
                        snuck.Eat();
                        if (AudioManager.Instance != null) AudioManager.Instance.PlaySE_eating();
                    }
                }
            }
        }
        
        // --- 後半：ゴミ掃除 ---
        else if (phase == IngameGameManager.GamePhase.CleaningTrash)
        {
            // ドラッグ中（ホバー）にRayを投射
            if (Input.GetMouseButton(0))
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    TrashObject trash = hit.collider.GetComponent<TrashObject>();
                    if (trash != null)
                    {
                        trash.Clean();
                        Debug.Log("Mouse Swipe: ゴミを掃除しました。");
                    }
                }
            }
        }
    }

    // デバッグ用に手の位置での判定範囲をSceneビューに表示
    private void OnDrawGizmos()
    {
        if (trackerHandler == null) return;

        Gizmos.color = Color.red;
        Vector3 rHand = trackerHandler.GetJointWorldPosition(JointId.HandRight);
        Vector3 lHand = trackerHandler.GetJointWorldPosition(JointId.HandLeft);

        if (rHand.sqrMagnitude > 0.001f)
        {
            Gizmos.DrawWireSphere(rHand, tapRadius);
        }
        if (lHand.sqrMagnitude > 0.001f)
        {
            Gizmos.DrawWireSphere(lHand, tapRadius);
        }
    }
}
