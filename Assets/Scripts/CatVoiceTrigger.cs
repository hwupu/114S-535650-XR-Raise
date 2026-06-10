using UnityEngine;

// Attach to a GameObject with a Trigger Collider.
// When the player enters the zone AND CatManager.CatSaved == true, plays a voice clip once.
// Each CatVoiceTrigger instance is independent — drag a different AudioClip into each zone.
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class CatVoiceTrigger : MonoBehaviour
{
    [Header("語音")]
    [SerializeField] private AudioClip voiceClip;

    [Header("Debug")]
    [SerializeField] private bool enableDebugTrigger = true;

    private AudioSource _audioSource;
    private bool _triggered;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
    }

    private void Update()
    {
        if (!enableDebugTrigger) return;
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.V))
            TryPlay();
#endif
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only respond to the player (OVRCameraRig has a CharacterController)
        if (other.GetComponent<CharacterController>() == null) return;
        TryPlay();
    }

    private void TryPlay()
    {
        if (_triggered) return;
        if (CatManager.Instance == null || !CatManager.Instance.CatSaved) return;
        if (voiceClip == null) return;

        _triggered = true;
        _audioSource.PlayOneShot(voiceClip);
        Debug.Log($"[CatVoiceTrigger] '{gameObject.name}' triggered — playing '{voiceClip.name}'");
    }
}
