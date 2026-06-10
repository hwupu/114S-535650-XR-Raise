using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SceneBGM : MonoBehaviour
{
    [SerializeField] private AudioClip bgmClip;

    private void Start()
    {
        var src = GetComponent<AudioSource>();
        src.clip = bgmClip;
        src.loop = true;
        src.playOnAwake = false;
        src.Play();
    }
}
