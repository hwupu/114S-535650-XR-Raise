using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Scene1manage : MonoBehaviour
{
    public enum GamePhase { Phase0_Normal, Phase1_Oppressive, Phase2_Panic, Phase3_Escape }
    [Header("--- 遊戲目前階段 ---")]
    public GamePhase currentPhase = GamePhase.Phase0_Normal;


    [Header("--- Phase 0: 電話互動 ---")]
    public GameObject phoneObject;
    public AudioSource phoneRingingSource;
    public AudioSource momAudioSource;  


    [Header("--- 環境物件連結 ---")]
    public Transform ceilingTransform;
    public Light roomMainLight;
    public Transform playerCameraTransform;


    [Header("--- 文字發射系統 ---")]
    public GameObject textPrefab;  
    public float spawnRadius = 3f;


    [System.Serializable]
    public struct VoiceSubtitlePair
    {
        public AudioClip voiceClip;
        [TextArea] public string subtitleText;
    }


    [Header("--- 第一階段語音與字串 (關心、碎念) ---")]
    public List<VoiceSubtitlePair> phase1Lines;


    [Header("--- 第二階段語音與字串 (控制、飆速) ---")]
    public List<VoiceSubtitlePair> phase2Lines;


    private float ceilingSinkSpeed = 0f;
    private float lightFlickerSpeed = 0.5f;
    private float textShootForce = 5f;
    private bool isFlickering = false;


    void Start()
    {
        currentPhase = GamePhase.Phase0_Normal;
        roomMainLight.enabled = true;
       
        phoneRingingSource.Play();


        // if (phoneInteractable != null)
        // {
        //     phoneInteractable.selectEntered.AddListener(OnPhonePickedUp);
        //     phoneRingingSource.Play();
        // }
    }
    public void StartMotherCalling()
    {
        if (phoneRingingSource != null) phoneRingingSource.Stop();
       
        StartCoroutine(PlayScene1Script());
    }


    // private void OnPhonePickedUp(SelectEnterEventArgs args)
    // {
    //     phoneRingingSource.Stop();
    //     phoneInteractable.enabled = false;
       
    //     StartCoroutine(PlayScene1Script());
    // }


    IEnumerator PlayScene1Script()
    {
        //第一階段
        currentPhase = GamePhase.Phase1_Oppressive;
        ceilingSinkSpeed = 0.05f;
        textShootForce = 4f;  
        lightFlickerSpeed = 0.8f;
        StartCoroutine(LightFlickerLoop());


        // 依序播放
        foreach (var line in phase1Lines)
        {
            yield return StartCoroutine(PlayLineAndSpawnText(line));
        }


        //第二階段
        currentPhase = GamePhase.Phase2_Panic;
        ceilingSinkSpeed = 0.25f;
        textShootForce = 10f;
        lightFlickerSpeed = 0.15f;


        foreach (var line in phase2Lines)
        {
            if (ceilingTransform.position.y <= 1.3f) break;
            yield return StartCoroutine(PlayLineAndSpawnText(line));
        }


        //第三階段
        TriggerEscapeHole();
    }


 
    IEnumerator PlayLineAndSpawnText(VoiceSubtitlePair line)
    {
        if (line.voiceClip == null) yield break;


        momAudioSource.clip = line.voiceClip;
        momAudioSource.Play();




        float clipDuration = line.voiceClip.length;
        float elapsed = 0f;
        float spawnInterval = (currentPhase == GamePhase.Phase2_Panic) ? 0.3f : 0.8f;


        while (elapsed < clipDuration)
        {
            SpawnTextInPlayerView(line.subtitleText);
            yield return new WaitForSeconds(spawnInterval);
            elapsed += spawnInterval;
        }


        // 播完才換下一句
        if (momAudioSource.isPlaying)
        {
            yield return new WaitWhile(() => momAudioSource.isPlaying);
        }
    }




    private void SpawnTextInPlayerView(string textContent)
    {
        float randomAngle = Random.Range(-60f, 60f);
        float randomHeight = Random.Range(-0.5f, 1.5f);


        Vector3 playerPos = playerCameraTransform.position;
        Vector3 forwardDirection = playerCameraTransform.forward;
        forwardDirection.y = 0;
        forwardDirection.Normalize();


        Vector3 spawnDirection = Quaternion.Euler(0, randomAngle, 0) * forwardDirection;
        Vector3 spawnPosition = playerPos + (spawnDirection * spawnRadius);
        spawnPosition.y += randomHeight;


        GameObject spawnedText = Instantiate(textPrefab, spawnPosition, Quaternion.identity);
       
        spawnedText.transform.LookAt(playerPos);


        TMPro.TextMeshPro tmp = spawnedText.GetComponentInChildren<TMPro.TextMeshPro>();
        if (tmp != null) tmp.text = textContent;


        Rigidbody rb = spawnedText.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 shootDirection = (playerPos - spawnPosition).normalized;
            rb.AddForce(shootDirection * textShootForce, ForceMode.Impulse);
        }
    }


    void Update()
    {
        if (currentPhase == GamePhase.Phase1_Oppressive || currentPhase == GamePhase.Phase2_Panic)
        {
            ceilingTransform.Translate(Vector3.down * ceilingSinkSpeed * Time.deltaTime, Space.World);


            if (ceilingTransform.position.y <= 1.2f && currentPhase != GamePhase.Phase3_Escape)
            {
                TriggerEscapeHole();
            }
        }
    }


    IEnumerator LightFlickerLoop()
    {
        isFlickering = true;
        while (currentPhase == GamePhase.Phase1_Oppressive || currentPhase == GamePhase.Phase2_Panic)
        {
            roomMainLight.enabled = !roomMainLight.enabled;
            yield return new WaitForSeconds(lightFlickerSpeed);
        }
        roomMainLight.enabled = false; // 進入下一關時全黑
    }


    // 地板破洞逃脫
    void TriggerEscapeHole()
    {
        if (currentPhase == GamePhase.Phase3_Escape) return;
        currentPhase = GamePhase.Phase3_Escape;
       
        StopAllCoroutines();
        momAudioSource.Stop();
        roomMainLight.enabled = false;
       
        Debug.Log("【第一場景結束】天花板壓迫至極限，觸發轉場動畫進入第二場景森林。");
        // 這裡轉到第二場景
    }
}

