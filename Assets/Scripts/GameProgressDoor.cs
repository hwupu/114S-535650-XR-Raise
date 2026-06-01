using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // 用於切換場景
// GameProgressDoor.Instance.RecordEvent("EventID");

public class GameProgressDoor : MonoBehaviour
{
    public static GameProgressDoor Instance;

    [Header("--- 遊戲進度與結局設定 ---")]
    public int totalEventsRequired = 5;   
    public bool choseCS = false;             // true=資工, false=音樂

    [Header("--- 場景切換設定 (如果結局在不同 Scene) ---")]
    public string csEndingSceneName = "Scene3";     
    public string musicEndingSceneName = "Scene4";
    
    private List<string> completedEvents = new List<string>(); 

    private bool hasTriggeredDoor = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RecordEvent(string eventID)
    {
        if (!completedEvents.Contains(eventID))
        {
            completedEvents.Add(eventID);
            Debug.Log($"[進度更新] 事件 '{eventID}' 已完成！目前進度：{completedEvents.Count} / {totalEventsRequired}");
        }
        else
        {
            Debug.Log($"事件 '{eventID}' 已經解過了，不重複計算。");
        }
    }

    // 選擇資工系
    public void SelectComputerScience()
    {
        choseCS = true;
        RecordEvent("MajorDecision");
        Debug.Log("玩家選擇了：資工系");
    }

    // 選擇音樂系
    public void SelectMusic()
    {
        choseCS = false;
        RecordEvent("MajorDecision");
        Debug.Log("玩家選擇了：音樂系");
    }


    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Player") && !hasTriggeredDoor)
        {
            hasTriggeredDoor = true; // 立刻上鎖，避免重複執行
            Debug.Log("玩家跨過門檻，開始結算進度...");

            CheckProgressAndTransition();
        }
    }


    private void CheckProgressAndTransition()
    {
        // 如果完成的事件數量達標
        if (completedEvents.Count >= totalEventsRequired)
        {
            if (choseCS)
            {
                Debug.Log("➡️ 條件達成：進入【場景三：資工系結局】");
                SceneManager.LoadScene(csEndingSceneName); 
            }
            else
            {
                Debug.Log("➡️ 條件達成：進入【場景四：音樂系結局】");
                SceneManager.LoadScene(musicEndingSceneName);
            }
        }
        else
        {
            Debug.Log($"進度未滿 ({completedEvents.Count}/{totalEventsRequired})，只是普通的回家...");

            StartCoroutine(ResetDoorLock());
        }
    }

    private IEnumerator ResetDoorLock()
    {
        yield return new WaitForSeconds(3f);
        hasTriggeredDoor = false;
    }
}