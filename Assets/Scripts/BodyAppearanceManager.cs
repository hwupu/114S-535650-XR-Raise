using UnityEngine;

// Attach to a manager GameObject in Scene3 and Scene4.
// Drag the fat and thin body model GameObjects into the Inspector slots.
// On Start, reads BodyShapeManager.Weight and activates the correct model.
public class BodyAppearanceManager : MonoBehaviour
{
    [Header("體型模型")]
    [SerializeField] private GameObject fatModel;
    [SerializeField] private GameObject thinModel;

    [Header("判斷門檻（Weight >= 此值 → 胖體型）")]
    [SerializeField] private int weightThreshold = 20;

    [Header("Debug — Play Mode 勾選即重新套用")]
    [SerializeField] private bool debugReapply;

    private void Start()
    {
        ApplyAppearance();
    }

    private void Update()
    {
        if (debugReapply)
        {
            debugReapply = false;
            ApplyAppearance();
        }
    }

    private void ApplyAppearance()
    {
        var bsm = BodyShapeManager.Instance;
        if (bsm == null)
        {
            Debug.LogWarning("[BodyAppearanceManager] BodyShapeManager.Instance = null，無法判斷體型。");
            return;
        }

        bool isFat = bsm.Weight >= weightThreshold;
        Debug.Log($"[BodyAppearanceManager] Weight={bsm.Weight}，門檻={weightThreshold} → {(isFat ? "胖體型" : "瘦體型")}");

        if (fatModel  != null) fatModel.SetActive(isFat);
        if (thinModel != null) thinModel.SetActive(!isFat);
    }
}
