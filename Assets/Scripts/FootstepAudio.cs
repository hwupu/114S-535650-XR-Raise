using UnityEngine;

// Attach to the OVRCameraRig (or any scene GameObject).
// Plays a looping footstep clip while the player is walking and stops it when they stop.
[RequireComponent(typeof(AudioSource))]
public class FootstepAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SwingLocomotion swingLocomotion;

    [Header("Audio")]
    [SerializeField] private AudioClip footstepClip;

    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.clip = footstepClip;
        _audioSource.loop = true;
        _audioSource.playOnAwake = false;
    }

    private void Start()
    {
        if (swingLocomotion == null)
            swingLocomotion = FindObjectOfType<SwingLocomotion>();

        if (swingLocomotion == null)
            Debug.LogWarning("[FootstepAudio] SwingLocomotion not found.");
    }

    private void Update()
    {
        if (swingLocomotion == null || footstepClip == null) return;

        if (swingLocomotion.IsWalking && !_audioSource.isPlaying)
            _audioSource.Play();
        else if (!swingLocomotion.IsWalking && _audioSource.isPlaying)
            _audioSource.Stop();
    }
}
