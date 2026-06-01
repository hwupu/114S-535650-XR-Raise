using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitBook : MonoBehaviour
{
    public int hitCountToDestroy = 5;
   
    private int currentHitCount = 0;
    private float hitCooldown = 0.4f;  
    private float lastHitTime = 0f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // 初始時把 Rigidbody 設為 Kinematic，讓它穩穩站著不會自己倒塌
        if (rb != null) rb.isKinematic = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[HitBook] OnTriggerEnter: {other.gameObject.name} tag={other.gameObject.tag}");
        if (other.CompareTag("Hand") || other.CompareTag("Player"))
        {
            if (Time.time - lastHitTime > hitCooldown)
            {
                currentHitCount++;
                lastHitTime = Time.time;

                Debug.Log($"書本被敲擊！目前次數: {currentHitCount}/{hitCountToDestroy}");

                // === [新增] 觸發手把震動 ===
                TriggerHaptics(other.gameObject);

                if (currentHitCount >= hitCountToDestroy)
                {
                    CollapseAndDestroy();
                }
            }
        }
    }

    // === [新增] 震動判斷與執行函式 ===
    private void TriggerHaptics(GameObject handObject)
    {
        // 將碰到的物件名稱轉小寫，檢查是否包含 "left" 來判斷是左手還是右手
        bool isLeftHand = handObject.name.ToLower().Contains("left");
        
        // 指定對應的控制器
        OVRInput.Controller controller = isLeftHand ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;

        // 啟動震動協程 (頻率: 0.5, 強度: 0.8, 持續時間: 0.15秒)
        StartCoroutine(VibrateCoroutine(controller, 0.5f, 0.8f, 0.15f));
    }

    // === [新增] 控制震動時間的協程 ===
    private IEnumerator VibrateCoroutine(OVRInput.Controller controller, float frequency, float amplitude, float duration)
    {
        // 開始震動
        OVRInput.SetControllerVibration(frequency, amplitude, controller);
        
        // 等待指定的時間
        yield return new WaitForSeconds(duration);
        
        // 停止震動 (非常重要，否則手把會一直震動到沒電)
        OVRInput.SetControllerVibration(0, 0, controller);
    }

    private void CollapseAndDestroy()
    {
        Debug.Log("書本牆被打破了！");

        // if (destroyEffectPrefab != null)
        // {
        //     Instantiate(destroyEffectPrefab, transform.position, Quaternion.identity);
        // }

        // 做法 B：解開物理限制，讓書本啪一聲散落倒塌，3秒後再消失
        if (rb != null)
        {
            rb.isKinematic = false; // 開啟物理模擬
            
            // [優化] 改為 transform.forward，這樣書本才會往「它自己面對的方向」倒塌，而不是世界座標的絕對北方
            rb.AddForce(transform.forward * 3f, ForceMode.Impulse); 
        }
       
        // 停用 Collider 防止重複觸發，並在 3 秒後把物件清掉
        GetComponent<Collider>().enabled = false;
        Destroy(gameObject, 3f);
    }
}