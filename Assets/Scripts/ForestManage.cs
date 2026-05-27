using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ForestManage : MonoBehaviour
{
    [Header("--- 石頭陣控制 (圍牆) ---")]
    public Transform stoneRingTransform;  
    public float targetYPositionSelf = -2f; // 自己選擇
    public float targetYPositionParent = 0f;// 父母選擇
    public float moveSpeed = 2f;            
    private float targetY;


    [Header("--- 聲音與震動設定 ---")]
    public AudioSource environmentAudioSource;
    public AudioClip stoneMoveClip;            
    public AudioSource playerAudioSource;      // 掛在玩家
    public AudioClip damageClip;               // 被撞擊
   
    // OVR 震動直接透過 OVRInput API 觸發，不需要手動綁定 Controller 物件


    [Header("--- 危險動物生成系統 ---")]
    public GameObject animalPrefab;        
    public Transform[] animalSpawnPoints;  
    private List<GameObject> activeAnimals = new List<GameObject>();
    private int currentSpawnPointIndex = 0;


    [Header("--- 玩家狀態與重啟 ---")]
    public Transform playerRig;            
    public Transform spawnPoint;            
    public int playerHealth = 3;          


    void Start()
    {
        if (stoneRingTransform != null)
        {
            targetY = stoneRingTransform.position.y;
        }
        playerHealth = 3;
    }


    void Update()
    {
        if (stoneRingTransform != null)
        {
            Vector3 currentPos = stoneRingTransform.position;
            float newY = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * moveSpeed);
            stoneRingTransform.position = new Vector3(currentPos.x, newY, currentPos.z);
        }
    }


    // 呼叫：當玩家做出自己的選擇
    public void OnMakeSelfChoice()
    {
        if (targetY != targetYPositionSelf)
        {
            PlayStoneMovementFeedback();
        }
       
        targetY = targetYPositionSelf;


        SpawnDangerousAnimal();
        Debug.Log("玩家選擇了自由：石頭陣下降，外面的危險生物甦醒了。");
    }


    // 呼叫：當玩家做出父母選擇
    public void OnMakeParentChoice()
    {
        // 確保只有在狀態改變時才播放震動與聲音
        if (targetY != targetYPositionParent)
        {
            PlayStoneMovementFeedback();
        }
       
        targetY = targetYPositionParent;
        Debug.Log("玩家選擇了服從：石頭陣保持封閉。");
    }


    // 內部機制：處理石頭移動時的聲光震動回饋
    private void PlayStoneMovementFeedback()
    {
        // 播放石頭摩擦/移動音效
        if (environmentAudioSource != null && stoneMoveClip != null)
        {
            environmentAudioSource.PlayOneShot(stoneMoveClip);
        }


        TriggerHapticFeedback(0.3f, 1.5f);
    }


    // 生成動物
    private void SpawnDangerousAnimal()
    {
        if (animalSpawnPoints.Length == 0 || animalPrefab == null) return;


        Transform spawnTP = animalSpawnPoints[currentSpawnPointIndex % animalSpawnPoints.Length];
        GameObject newAnimal = Instantiate(animalPrefab, spawnTP.position, Quaternion.identity);
       
        ForestPatrolAnimal2 patrolScript = newAnimal.GetComponent<ForestPatrolAnimal2>();
        if (patrolScript != null)
        {
            patrolScript.patrolPoints = animalSpawnPoints;
            patrolScript.forestManager = this;
        }


        activeAnimals.Add(newAnimal);
        currentSpawnPointIndex++;
    }


    // 懲罰機制
    public void TakeDamage()
    {
        playerHealth--;
        Debug.Log($"玩家被撞到了！剩餘血量: {playerHealth}");


        if (playerAudioSource != null && damageClip != null)
        {
            playerAudioSource.PlayOneShot(damageClip);
        }


        TriggerHapticFeedback(0.8f, 0.3f);


        if (playerHealth <= 0)
        {
            SendPlayerBackToSpawn();
        }
    }


    // 使用 OVR API 震動雙手，持續 duration 秒後停止
    private void TriggerHapticFeedback(float amplitude, float duration)
    {
        StartCoroutine(HapticCoroutine(amplitude, duration));
    }

    private IEnumerator HapticCoroutine(float amplitude, float duration)
    {
        OVRInput.SetControllerVibration(1f, amplitude, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(1f, amplitude, OVRInput.Controller.RTouch);
        yield return new WaitForSeconds(duration);
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
    }


    private void SendPlayerBackToSpawn()
    {
        Debug.Log("被撞擊 3 次，玩家被送回出生點！");
       
        playerRig.position = spawnPoint.position;
       
        playerHealth = 3;
    }
}


