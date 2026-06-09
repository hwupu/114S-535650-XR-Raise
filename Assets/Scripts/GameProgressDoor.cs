using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameProgressDoor : MonoBehaviour
{
    public static GameProgressDoor Instance;

    [Header("場景切換設定")]
    public string csEndingSceneName    = "Scene3";
    public string musicEndingSceneName = "Scene4";

    [Header("傳送目標 (在 Scene3/Scene4 各放一個空物件，命名同此欄位)")]
    public string spawnPointName = "PlayerSpawnPoint";

    [Header("Debug — Play Mode 勾選即觸發，自動取消")]
    [SerializeField] private bool debugCompleteCS;
    [SerializeField] private bool debugCompleteMusic;

    private bool _triggered;
    private bool _pendingTeleport;
    private static readonly WaitForSeconds _resetWait = new(3f);

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GPD-診斷] OnSceneLoaded 觸發 scene={scene.name}  _pendingTeleport={_pendingTeleport}");
        if (!_pendingTeleport) return;
        if (scene.name != csEndingSceneName && scene.name != musicEndingSceneName) return;
        _pendingTeleport = false;
        Debug.Log($"[GPD-診斷] 開始 TeleportAfterLoad coroutine，目標場景={scene.name}");
        StartCoroutine(TeleportAfterLoad());
    }

    private IEnumerator TeleportAfterLoad()
    {
        // 等兩幀：第一幀讓新場景 Start() 跑完，第二幀讓物理引擎初始化
        yield return null;
        yield return null;
        Debug.Log("[GPD-診斷] 兩幀等待結束，開始尋找物件...");

        // ── 1. 尋找 PlayerSpawnPoint ──
        GameObject spawnGO = GameObject.Find(spawnPointName);
        if (spawnGO == null)
        {
            Debug.LogError($"[GPD-診斷] ❌ 找不到 '{spawnPointName}'！請在 Scene3/Scene4 新增同名空物件。傳送中止。");
            yield break;
        }
        Debug.Log($"[GPD-診斷] ✅ SpawnPoint 找到：{spawnGO.name}  世界座標={spawnGO.transform.position}");

        // ── 2. 尋找 OVRCameraRig ──
        OVRCameraRig cameraRig = FindObjectOfType<OVRCameraRig>();
        if (cameraRig == null)
        {
            Debug.LogError("[GPD-診斷] ❌ 找不到 OVRCameraRig！Scene3 可能沒有 Player prefab。傳送中止。");
            yield break;
        }

        // ── 3. 列印完整 Hierarchy（找出父物件層級）──
        string hierarchy = cameraRig.name;
        Transform t = cameraRig.transform.parent;
        while (t != null)
        {
            hierarchy = t.name + " → " + hierarchy;
            t = t.parent;
        }
        Debug.Log($"[GPD-診斷] OVRCameraRig hierarchy：{hierarchy}");
        Debug.Log($"[GPD-診斷] OVRCameraRig 傳送前位置：{cameraRig.transform.position}  (父物件位置：{(cameraRig.transform.parent != null ? cameraRig.transform.parent.position.ToString() : "無父物件")})");

        // ── 4. 確認 CharacterController 位置 ──
        var cc = cameraRig.GetComponent<CharacterController>();
        var ccOnParent = cameraRig.transform.parent != null ? cameraRig.transform.parent.GetComponent<CharacterController>() : null;
        Debug.Log($"[GPD-診斷] CC 在 OVRCameraRig 上：{cc != null}  CC 在父物件上：{ccOnParent != null}");

        // ── 5. 確認 SwingLocomotion ──
        var locomotion = cameraRig.GetComponent<SwingLocomotion>();
        Debug.Log($"[GPD-診斷] SwingLocomotion 存在：{locomotion != null}");

        // ── 6. Raycast 向下確認地板 ──
        Vector3 rayOrigin = spawnGO.transform.position + Vector3.up * 0.5f;
        bool hasFloor = Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit floorHit, 20f);
        if (hasFloor)
            Debug.Log($"[GPD-診斷] ✅ 地板存在：{floorHit.collider.name}  距離={floorHit.distance:F2}m  碰撞點={floorHit.point}");
        else
            Debug.LogError("[GPD-診斷] ❌ SpawnPoint 下方 20m 內沒有地板 Collider！玩家一定會掉落。");

        // ── 7. 決定要移動的根節點（誰有 CC 誰就是移動目標）──
        // 若父物件有 CC 則移動父物件，否則移動 OVRCameraRig 本身
        Transform moveTarget;
        CharacterController targetCC;
        if (ccOnParent != null)
        {
            moveTarget = cameraRig.transform.parent;
            targetCC   = ccOnParent;
            Debug.Log($"[GPD-診斷] 移動目標 = 父物件「{moveTarget.name}」（CC 在父物件上）");
        }
        else
        {
            moveTarget = cameraRig.transform;
            targetCC   = cc;
            Debug.Log($"[GPD-診斷] 移動目標 = OVRCameraRig「{moveTarget.name}」（CC 在自身或無 CC）");
        }

        // ── 8. 執行傳送 ──
        if (targetCC != null) targetCC.enabled = false;

        moveTarget.position = spawnGO.transform.position;
        moveTarget.rotation = spawnGO.transform.rotation;

        if (targetCC != null) targetCC.enabled = true;
        if (locomotion != null) locomotion.ResetVelocity();

        Debug.Log($"[GPD-診斷] ✅ 傳送完成  新位置={moveTarget.position}  (預期={spawnGO.transform.position})");

        // ── 9. 一幀後再次確認位置是否被覆蓋 ──
        yield return null;
        Debug.Log($"[GPD-診斷] 傳送後第 1 幀位置={moveTarget.position}  isGrounded={targetCC?.isGrounded}");
        yield return null;
        Debug.Log($"[GPD-診斷] 傳送後第 2 幀位置={moveTarget.position}  isGrounded={targetCC?.isGrounded}");
    }

    private void Update()
    {
        if (debugCompleteCS)
        {
            debugCompleteCS = false;
            Debug.Log("[GameProgressDoor] Debug: 強制完成全部事件（資工路線）");
            ForceCompleteAllEvents(isCS: true);
        }
        if (debugCompleteMusic)
        {
            debugCompleteMusic = false;
            Debug.Log("[GameProgressDoor] Debug: 強制完成全部事件（音樂路線）");
            ForceCompleteAllEvents(isCS: false);
        }
    }

    private void ForceCompleteAllEvents(bool isCS)
    {
        var gm = GameManager.Instance;
        if (gm == null) { Debug.LogWarning("[GameProgressDoor] GameManager.Instance = null"); return; }

        gm.RecordDepartmentChoice(isCS);
        gm.CompleteEvent(2);
        gm.FinalizeAndCompleteMoneyEvent();

        Debug.Log($"[GameProgressDoor] 進度: {gm.CompletedEvents}/3，科系: {(isCS ? "資工" : "音樂")}。直接觸發轉場...");
        _triggered = false;
        CheckProgressAndTransition();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || _triggered) return;
        _triggered = true;
        Debug.Log("[GameProgressDoor] 玩家跨過門檻，開始結算進度...");
        CheckProgressAndTransition();
    }

    private void CheckProgressAndTransition()
    {
        var gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("[GameProgressDoor] GameManager.Instance = null，無法結算。");
            StartCoroutine(ResetLock());
            return;
        }

        if (gm.CompletedEvents >= 3)
        {
            bool isCS = gm.ChoseCSDepartment == true;
            string sceneName = isCS ? csEndingSceneName : musicEndingSceneName;
            Debug.Log($"[GameProgressDoor] 全部完成 → 載入【{sceneName}】");
            _pendingTeleport = true;
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.Log($"[GameProgressDoor] 進度未滿 ({gm.CompletedEvents}/3)，普通的回家...");
            StartCoroutine(ResetLock());
        }
    }

    private IEnumerator ResetLock()
    {
        yield return _resetWait;
        _triggered = false;
    }
}
