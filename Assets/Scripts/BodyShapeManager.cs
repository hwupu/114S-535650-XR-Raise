using UnityEngine;
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
        Instance = this;
        if (swingLocomotion == null)
            swingLocomotion = GetComponent<SwingLocomotion>();
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
        float t = maxWeight > 0 ? (float)Weight / maxWeight : 0f;
        swingLocomotion?.SetWeightFactor(t);
    }
}
