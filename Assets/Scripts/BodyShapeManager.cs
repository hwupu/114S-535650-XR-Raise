using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class BodyShapeManager : MonoBehaviour
{
    public static BodyShapeManager Instance { get; private set; }

    [Header("Weight")]
    [SerializeField] private int startWeight = 10;
    [SerializeField] private int maxWeight   = 30;

    [Header("References")]
    [SerializeField] private SwingLocomotion swingLocomotion;

    public int Weight     { get; private set; }
    public int SnackCount { get; private set; }
    public event Action<int> OnWeightChanged;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        // 優先 GetComponent（同 GO），若不同 GO 則場景搜尋
        if (swingLocomotion == null)
            swingLocomotion = GetComponent<SwingLocomotion>();
        if (swingLocomotion == null)
            swingLocomotion = FindObjectOfType<SwingLocomotion>();
    }

    private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 新場景載入後重新尋找 SwingLocomotion（舊場景的已被銷毀）
        swingLocomotion = FindObjectOfType<SwingLocomotion>();
        ApplyWeightToLocomotion();
    }

    private void Start()
    {
        Weight = startWeight;
        ApplyWeightToLocomotion();
    }

    public void AddWeight(int delta)
    {
        Weight = Mathf.Clamp(Weight + delta, 0, maxWeight);
        if (delta > 0) SnackCount++;
        ApplyWeightToLocomotion();
        OnWeightChanged?.Invoke(Weight);
    }

    private void ApplyWeightToLocomotion()
    {
        float range = maxWeight - startWeight;
        float t = range > 0 ? Mathf.Clamp01((float)(Weight - startWeight) / range) : 0f;
        // 不用 ?. 運算子：Unity 被銷毀的物件對 C# 來說不是 null，用 == null 才能觸發 Unity 的覆寫
        if (swingLocomotion != null) swingLocomotion.SetWeightFactor(t);
    }
}
