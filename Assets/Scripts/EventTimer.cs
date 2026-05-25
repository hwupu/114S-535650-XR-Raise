using UnityEngine;
using TMPro;
using System;

// Place on each event zone's root GameObject.
// Add a trigger collider (Box/Sphere, IsTrigger=true) to the same GameObject.
// Timer starts when the player's OVRCameraRig enters the trigger.
[RequireComponent(typeof(AudioSource))]
public class EventTimer : MonoBehaviour
{
    [Header("Event")]
    [SerializeField] private int eventNumber = 1;           // 1, 2, or 3
    [SerializeField] private float totalTime  = 180f;

    [Header("References")]
    [SerializeField] private AudioClip warningClip;         // 30-second voice reminder
    [SerializeField] private TextMeshProUGUI timerDisplay;  // world-space canvas label

    [Header("Debug")]
    [SerializeField] private bool enableDebugTrigger = true;

    public bool IsRunning { get; private set; }
    public bool IsExpired { get; private set; }

    // Subscribers can react to expiry (e.g., lock interactions)
    public event Action OnExpired;

    private float       _remaining;
    private bool        _warningPlayed;
    private AudioSource _audio;

    private void Awake() => _audio = GetComponent<AudioSource>();

    private void Update()
    {
        if (enableDebugTrigger)
        {
            if (Input.GetKeyDown(KeyCode.T) && IsRunning)
            {
                _remaining = 31f;
                Debug.Log($"[EventTimer {eventNumber}] Debug: skipped to 31 s");
            }
            if (Input.GetKeyDown(KeyCode.Y) && IsRunning)
            {
                Debug.Log($"[EventTimer {eventNumber}] Debug: force expire");
                _remaining = 0f;
            }
        }

        if (!IsRunning || IsExpired) return;

        _remaining -= Time.deltaTime;
        UpdateDisplay();

        if (!_warningPlayed && _remaining <= 30f)
        {
            _warningPlayed = true;
            if (warningClip != null) _audio.PlayOneShot(warningClip);
            Debug.Log($"[EventTimer {eventNumber}] 30-second warning played.");
        }

        if (_remaining <= 0f)
        {
            _remaining = 0f;
            IsRunning  = false;
            IsExpired  = true;
            UpdateDisplay();
            Debug.Log($"[EventTimer {eventNumber}] Time expired.");
            OnExpired?.Invoke();
            GameManager.Instance?.CompleteEvent(eventNumber);
        }
    }

    // Called when OVRCameraRig enters the zone trigger collider
    private void OnTriggerEnter(Collider other)
    {
        if (IsRunning || IsExpired) return;
        if (other.GetComponentInParent<OVRCameraRig>() != null)
        {
            Debug.Log($"[EventTimer {eventNumber}] Player entered zone — starting timer.");
            StartTimer();
        }
    }

    public void StartTimer()
    {
        _remaining     = totalTime;
        _warningPlayed = false;
        IsRunning      = true;
        IsExpired      = false;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (timerDisplay == null) return;
        int mins = Mathf.FloorToInt(_remaining / 60f);
        int secs = Mathf.FloorToInt(_remaining % 60f);
        timerDisplay.text = $"{mins:D2}:{secs:D2}";
    }
}
