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

    [Header("--- 背景音樂 ---")]
    public AudioSource houseBgm;

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

    [Header("--- 轉場事件 (逃跑) ---")]
    public AudioSource catAudioSource;           
    public AudioClip catEscapeClip;              
    public AudioClip momFinalYellClip;           
    [TextArea]
    public string momFinalYellText = ".";

    [Header("--- 場景轉移設定 ---")]
    public Transform playerRig;
    public Transform forestSpawnPoint;

    [Header("--- 雙門顯示控制 ---")]
    public GameObject firstDoor;                  // 初始顯示的第一個門
    public GameObject secondDoor;                 // 重置客廳後（十萬火急回家後）才顯示的第二個門

    // === 動態參數與狀態記錄 ===
    private float ceilingSinkSpeed = 0f;
    private float lightFlickerSpeed = 0.5f;
    private float textShootForce = 5f;
    private bool isEscaping = false;

    // 故事是否已經完整播放過的鎖
    private bool hasPlayedMainStory = false; 

    // 用來記錄房間的初始狀態，方便玩家回家時還原
    private Vector3 initialCeilingPosition;
    private float initialLightIntensity;
    
    // 記錄發射出去的文字，以便重置時一次清空
    private List<GameObject> activeTexts = new List<GameObject>();

    IEnumerator Start()
    {
        Debug.Log("starting");
        currentPhase = GamePhase.Phase0_Normal;
        
        // 記錄客廳一開始的原始狀態
        if (ceilingTransform != null) initialCeilingPosition = ceilingTransform.position;
        if (roomMainLight != null) 
        {
            roomMainLight.enabled = true;
            initialLightIntensity = roomMainLight.intensity;
        }

        // === 初始狀態：顯示第一個門，隱藏第一個門 ===
        if (firstDoor != null) firstDoor.SetActive(true);
        if (secondDoor != null) secondDoor.SetActive(false);

        if (houseBgm != null) houseBgm.Play();

        if (!hasPlayedMainStory)
        {
            phoneRingingSource.Play();
            yield return new WaitForSeconds(3f);
            StartCoroutine(PlayScene1Script());
        }
    }

    public void StartMotherCalling()
    {
        if (hasPlayedMainStory) return; 

        if (phoneRingingSource != null) phoneRingingSource.Stop();
        StartCoroutine(PlayScene1Script());
    }

    IEnumerator PlayScene1Script()
    {
        hasPlayedMainStory = true;

        Debug.Log("first stage");

        currentPhase = GamePhase.Phase1_Oppressive;
        ceilingSinkSpeed = 0.02f;
        textShootForce = 4f;
        lightFlickerSpeed = 0.8f;
        StartCoroutine(LightFlickerLoop());

        foreach (var line in phase1Lines)
            yield return StartCoroutine(PlayLineAndSpawnText(line));


        Debug.Log("second stage");
        currentPhase = GamePhase.Phase2_Panic;
        ceilingSinkSpeed = 0.06f;
        textShootForce = 10f;
        lightFlickerSpeed = 0.15f;

        foreach (var line in phase2Lines)
            yield return StartCoroutine(PlayLineAndSpawnText(line));

        // 所有 clip 播完 → 進入逃跑序列
        yield return StartCoroutine(EscapeSequenceCoroutine());
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

        activeTexts.Add(spawnedText);
    }

    void Update()
    {
        if (currentPhase == GamePhase.Phase1_Oppressive || currentPhase == GamePhase.Phase2_Panic)
        {
            ceilingTransform.Translate(Vector3.down * ceilingSinkSpeed * Time.deltaTime, Space.World);
        }
    }

    IEnumerator LightFlickerLoop()
    {
        while (currentPhase == GamePhase.Phase1_Oppressive || currentPhase == GamePhase.Phase2_Panic)
        {
            if (roomMainLight != null) roomMainLight.enabled = !roomMainLight.enabled;
            yield return new WaitForSeconds(lightFlickerSpeed);
        }
    }

IEnumerator EscapeSequenceCoroutine()
    {
        isEscaping = true;
        currentPhase = GamePhase.Phase3_Escape;
        ceilingSinkSpeed = 0f;

        if (houseBgm != null) houseBgm.Stop();
        if (momAudioSource != null && momAudioSource.isPlaying)
            momAudioSource.Stop();

        ForestManage.Instance?.StartBGM();

        if (roomMainLight != null)
        {
            roomMainLight.enabled = true;
            roomMainLight.intensity = initialLightIntensity * 0.5f;
        }

        // 播 catEscapeClip，等它播完
        if (catAudioSource != null && catEscapeClip != null)
        {
            catAudioSource.clip = catEscapeClip;
            catAudioSource.Play();
            yield return new WaitUntil(() => !catAudioSource.isPlaying);
        }

        // 播 momFinalYellClip，等它播完
        if (momAudioSource != null && momFinalYellClip != null)
        {
            momAudioSource.clip = momFinalYellClip;
            momAudioSource.Play();

            float elapsed = 0f;
            while (elapsed < momFinalYellClip.length)
            {
                SpawnTextInPlayerView(momFinalYellText);
                yield return new WaitForSeconds(0.2f);
                elapsed += 0.2f;
            }
            yield return new WaitUntil(() => !momAudioSource.isPlaying);
        }

        if (roomMainLight != null) roomMainLight.enabled = false;
        yield return new WaitForSeconds(0.5f);

        TeleportToForest();
    }

    void TeleportToForest()
    {
        Debug.Log("準備傳送至森林！");

        if (playerRig != null && forestSpawnPoint != null)
        {
            CharacterController cc = playerRig.GetComponent<CharacterController>();
            
            if (cc != null) 
            {
                cc.enabled = false;
            }

            playerRig.position = forestSpawnPoint.position;
            playerRig.rotation = forestSpawnPoint.rotation; 

            if (cc != null)
            {
                cc.enabled = true;
            }

            Debug.Log("成功傳到森林座標: " + forestSpawnPoint.position);
        }

        ResetLivingRoom();
    }

    void ResetLivingRoom()
    {
        if (ceilingTransform != null)
        {
            ceilingTransform.position = initialCeilingPosition;
        }

        if (roomMainLight != null)
        {
            roomMainLight.enabled = true;
            roomMainLight.intensity = initialLightIntensity;
        }

        foreach (GameObject txt in activeTexts)
        {
            if (txt != null) Destroy(txt);
        }
        activeTexts.Clear(); 

        // === 核心修改：重置客廳後顯示第二個門 ===
        if (secondDoor != null) 
        {
            secondDoor.SetActive(true);
            Debug.Log("解鎖新路線：第二個門已顯示。");
        }

         if (firstDoor != null) firstDoor.SetActive(false);

        currentPhase = GamePhase.Phase0_Normal;
        isEscaping = false;

        Debug.Log("客廳已重置完畢，隨時準備迎接玩家回家。");
    }
}