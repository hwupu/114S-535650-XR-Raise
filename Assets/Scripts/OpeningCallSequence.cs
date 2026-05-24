using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class OpeningCallSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private OVRPassthroughLayer passthroughLayer;
    [SerializeField] private GameObject callUI;
    [SerializeField] private Transform leftHandAnchor;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip momVoiceClip;
    [SerializeField] private TextMeshProUGUI callLabel;

    [Header("Timing")]
    [SerializeField] private float callStartDelay = 2f;
    [SerializeField] private float hapticPulseInterval = 0.5f;
    [SerializeField] private float voiceExtraDelay = 1f;
    [SerializeField] private float fadeDuration = 2.5f;
    [SerializeField] private string homeSceneName = "scene 1_house";

    private enum State { Idle, Ringing, Answered, Fading }
    private State _state;
    private float _hapticTimer;
    private bool _hapticHigh;
    private int _dotCount;
    private float _dotTimer;

    private void Start()
    {
        callUI.SetActive(false);
        StartCoroutine(BeginSequence());
    }

    private IEnumerator BeginSequence()
    {
        yield return new WaitForSeconds(callStartDelay);
        _state = State.Ringing;
        callUI.SetActive(true);
    }

    private void Update()
    {
        if (_state != State.Ringing) return;

        // Keep UI above left controller, facing the player
        callUI.transform.position = leftHandAnchor.position + Vector3.up * 0.25f;
        callUI.transform.rotation = Quaternion.LookRotation(
            callUI.transform.position - Camera.main.transform.position);

        // Pulse haptics: on/off every hapticPulseInterval seconds
        _hapticTimer += Time.deltaTime;
        if (_hapticTimer >= hapticPulseInterval)
        {
            _hapticTimer = 0f;
            _hapticHigh = !_hapticHigh;
            OVRInput.SetControllerVibration(1f, _hapticHigh ? 0.8f : 0f, OVRInput.Controller.LTouch);
        }

        // Animate trailing dots on "來電中"
        _dotTimer += Time.deltaTime;
        if (_dotTimer >= 0.4f)
        {
            _dotTimer = 0f;
            _dotCount = (_dotCount + 1) % 4;
            callLabel.text = "媽媽\n來電中" + new string('.', _dotCount);
        }

        if (OVRInput.GetDown(OVRInput.RawButton.X))
            AnswerCall();
    }

    private void AnswerCall()
    {
        _state = State.Answered;
        callUI.SetActive(false);
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
        StartCoroutine(PostAnswerSequence());
    }

    private IEnumerator PostAnswerSequence()
    {
        if (momVoiceClip != null)
        {
            audioSource.PlayOneShot(momVoiceClip);
            yield return new WaitForSeconds(momVoiceClip.length + voiceExtraDelay);
        }
        else
        {
            yield return new WaitForSeconds(voiceExtraDelay);
        }
        yield return StartCoroutine(FadeAndLoadHome());
    }

    private IEnumerator FadeAndLoadHome()
    {
        _state = State.Fading;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            passthroughLayer.textureOpacity = 1f - (elapsed / fadeDuration);
            yield return null;
        }
        passthroughLayer.textureOpacity = 0f;
        SceneManager.LoadScene(homeSceneName);
    }
}
