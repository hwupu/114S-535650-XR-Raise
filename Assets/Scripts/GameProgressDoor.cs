using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameProgressDoor : MonoBehaviour
{
    public static GameProgressDoor Instance;

    [Header("場景切換設定")]
    public string csEndingSceneName    = "Scene3";
    public string musicEndingSceneName = "Scene4";

    [Header("Debug — Play Mode 勾選即觸發，自動取消")]
    [SerializeField] private bool debugCompleteCS;    // 勾選 → 完成全部事件（資工）
    [SerializeField] private bool debugCompleteMusic; // 勾選 → 完成全部事件（音樂）

    private bool _triggered;
    private static readonly WaitForSeconds _resetWait = new(3f);

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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
