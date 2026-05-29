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

    [Header("--- 轉場事件 (逃跑) ---")]
    public AudioSource catAudioSource;           
    public AudioClip catEscapeClip;              
    public AudioClip momFinalYellClip;           
    [TextArea]
    public string momFinalYellText = "整天只知道出去玩！你最好給我10分鐘內回家！";

    [Header("--- 場景轉移設定 ---")]
    public Transform playerRig;                  
    public Transform forestSpawnPoint;     

    [Header("--- 打開門 ---")]
    public GameObject firstDoor;        
    public GameObject secondDoor;      

    // === 動態參數與狀態記錄 ===
    private float ceilingSinkSpeed = 0f;
    private float lightFlickerSpeed = 0.5f;
    private float textShootForce = 5f;
    private bool isEscaping = false;

    // [新增] 故事是否已經完整播放過的鎖
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
        if (firstDoor != null) firstDoor.SetActive(true);
        if (secondDoor != null) secondDoor.SetActive(false);

        
        if (!hasPlayedMainStory)
        {
            phoneRingingSource.Play();
            yield return new WaitForSeconds(3f);
            StartCoroutine(PlayScene1Script());
        }
    }

    public void StartMotherCalling()
    {
        // [新增] 防呆鎖：如果已經播過故事，直接中斷，不再執行
        if (hasPlayedMainStory) return; 

        if (phoneRingingSource != null) phoneRingingSource.Stop();
        StartCoroutine(PlayScene1Script());
    }

    IEnumerator PlayScene1Script()
    {
        // [新增] 一進入劇本就立刻上鎖，確保不會被重複觸發
        hasPlayedMainStory = true; 

        Debug.Log("first stage");

        //第一階段
        currentPhase = GamePhase.Phase1_Oppressive;
        ceilingSinkSpeed = 0.05f;
        textShootForce = 4f;  
        lightFlickerSpeed = 0.8f;
        StartCoroutine(LightFlickerLoop());

        foreach (var line in phase1Lines)
        {
            if (isEscaping) yield break; 
            yield return StartCoroutine(PlayLineAndSpawnText(line));
        }
        yield return new WaitForSeconds(3f);

        //第二階段
        Debug.Log("second stage");
        currentPhase = GamePhase.Phase2_Panic;
        ceilingSinkSpeed = 0.25f;
        textShootForce = 10f;
        lightFlickerSpeed = 0.15f;

        foreach (var line in phase2Lines)
        {
            if (isEscaping) yield break;
            if (ceilingTransform.position.y <= 1.3f) break;
            yield return StartCoroutine(PlayLineAndSpawnText(line));
        }

        // 第三階段 (觸發轉場事件)
        if (!isEscaping)
        {
            TriggerFinalEscapeSequence();
        }
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
            if (isEscaping) yield break; 
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

            if (ceilingTransform.position.y <= 1.2f && !isEscaping)
            {
                TriggerFinalEscapeSequence();
            }
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

    void TriggerFinalEscapeSequence()
    {
        if (isEscaping) return;
        isEscaping = true;
        currentPhase = GamePhase.Phase3_Escape;

        Debug.Log("第三階段：貓咪介入，準備逃跑！");
        
        StopAllCoroutines(); 
        if (momAudioSource != null) momAudioSource.Stop();
        
        StartCoroutine(EscapeSequenceCoroutine());
    }

    IEnumerator EscapeSequenceCoroutine()
    {
        if (roomMainLight != null)
        {
            roomMainLight.enabled = true;
            roomMainLight.intensity = initialLightIntensity * 0.5f; 
        }

        if (catAudioSource != null && catEscapeClip != null)
        {
            catAudioSource.clip = catEscapeClip;
            catAudioSource.Play();
            yield return new WaitForSeconds(catEscapeClip.length);
        }

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
            yield return new WaitWhile(() => momAudioSource.isPlaying);
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
            // 1. 抓取玩家身上的角色控制器 (如果有的話)
            CharacterController cc = playerRig.GetComponent<CharacterController>();
            
            // 2. 傳送前：先關閉物理控制器，避免座標被強制拉回
            if (cc != null) 
            {
                cc.enabled = false;
            }

            // 3. 執行傳送 (同時傳送位置與面向角度)
            playerRig.position = forestSpawnPoint.position;
            playerRig.rotation = forestSpawnPoint.rotation; // 建議把 Rotation 也一起同步，這樣玩家一傳過去才會面向正前方

            // 4. 傳送後：立刻重新開啟控制器
            if (cc != null) 
            {
                cc.enabled = true;
            }

            Debug.Log("成功傳到森林座標: " + forestSpawnPoint.position);
        }

        // 傳送完畢後重置客廳
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

        if (firstDoor != null) firstDoor.SetActive(true);
        if (secondDoor != null) secondDoor.SetActive(false);

        currentPhase = GamePhase.Phase0_Normal;
        isEscaping = false;

        Debug.Log("客廳已重置完畢，隨時準備迎接玩家回家。");
    }
}