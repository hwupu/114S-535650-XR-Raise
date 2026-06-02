using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool enableDebugTrigger = true;

    // Event 1: Department choice
    public bool? ChoseCSDepartment { get; private set; }   // null = not chosen

    // Event 2: Cat — read directly from CatManager
    public bool CatWasSaved => CatManager.Instance != null && CatManager.Instance.CatSaved;

    // Event 3: Money tree
    public int SafeCoins  { get; private set; }
    public int PiggyCoins { get; private set; }
    public bool ChoseSafe { get; private set; }

    // Fragment unlock counter
    public int CompletedEvents { get; private set; }

    // Fired with fragment index 1, 2, or 3 when an event completes
    public event Action<int> OnEventCompleted;

    private bool _event1Done, _event2Done, _event3Done;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!enableDebugTrigger) return;
        if (Input.GetKeyDown(KeyCode.Alpha1)) { Debug.Log("[GameManager] Debug: CompleteEvent(1)"); CompleteEvent(1); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { Debug.Log("[GameManager] Debug: CompleteEvent(2)"); CompleteEvent(2); }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { Debug.Log("[GameManager] Debug: FinalizeAndCompleteMoneyEvent"); FinalizeAndCompleteMoneyEvent(); }
    }

    // --- Event 1: Department ---

    public void RecordDepartmentChoice(bool isCS)
    {
        if (_event1Done) return;
        ChoseCSDepartment = isCS;
        Debug.Log($"[GameManager] Department chosen: {(isCS ? "CS" : "Music")}");
        CompleteEvent(1);
    }

    // --- Event 2: Cat (InjuredCat calls this) ---
    // CatWasSaved is read from CatManager — no setter needed here.
    // This method is called both on heal AND on timer expiry (guard prevents double).

    // --- Event 3: Money ---

    public void RecordCoinDeposit(bool isSafe)
    {
        if (isSafe) SafeCoins++;
        else PiggyCoins++;
        Debug.Log($"[GameManager] Coin deposited — Safe:{SafeCoins} Piggy:{PiggyCoins}");
    }

    public void FinalizeAndCompleteMoneyEvent()
    {
        if (_event3Done) return;
        ChoseSafe = SafeCoins >= PiggyCoins;
        Debug.Log($"[GameManager] Money finalized — ChoseSafe:{ChoseSafe} (Safe:{SafeCoins} Piggy:{PiggyCoins})");

        if (ChoseSafe)
        {
            if (ForestManage.Instance != null) ForestManage.Instance.OnMakeParentChoice();
            Debug.Log("[GameManager] Vault 獲勝（安全選擇）→ ForestManage.OnMakeParentChoice()");
        }
        else
        {
            if (ForestManage.Instance != null) ForestManage.Instance.OnMakeSelfChoice();
            Debug.Log("[GameManager] PiggyBank 獲勝（危險選擇）→ ForestManage.OnMakeSelfChoice()");
        }

        CompleteEvent(3);
    }

    // --- Core ---

    public void CompleteEvent(int eventNumber)
    {
        switch (eventNumber)
        {
            case 1:
                if (_event1Done) return;
                _event1Done = true;
                break;
            case 2:
                if (_event2Done) return;
                _event2Done = true;
                break;
            case 3:
                if (_event3Done) return;
                _event3Done = true;
                break;
            default:
                return;
        }

        CompletedEvents++;
        Debug.Log($"[GameManager] Event {eventNumber} complete — total fragments: {CompletedEvents}");
        OnEventCompleted?.Invoke(eventNumber);
    }
}
