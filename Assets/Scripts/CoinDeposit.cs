using UnityEngine;

// Place on an invisible trigger zone above the safe OR piggy bank model.
// Add a BoxCollider/SphereCollider with IsTrigger = true.
// Coin prefabs must have the "Coin" tag, a Rigidbody, and OVRGrabbable.
public class CoinDeposit : MonoBehaviour
{
    public enum DepositType { Safe, Piggy }

    [Header("Setup")]
    [SerializeField] private DepositType depositType;
    [SerializeField] private ParticleSystem depositFX;   // optional coin burst effect

    [Header("音效")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip depositSoundSafe;   // 投入 Vault 音效
    [SerializeField] private AudioClip depositSoundPiggy;  // 投入 PiggyBank 音效

    [Header("Debug")]
    [SerializeField] private bool enableDebugTrigger = true;

    private void Update()
    {
        if (!enableDebugTrigger) return;

        if (depositType == DepositType.Safe  && Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("[CoinDeposit] Debug: deposit to Safe");
            GameManager.Instance?.RecordCoinDeposit(true);
        }
        if (depositType == DepositType.Piggy && Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("[CoinDeposit] Debug: deposit to Piggy");
            GameManager.Instance?.RecordCoinDeposit(false);
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("[CoinDeposit] Debug: finalize money event");
            GameManager.Instance?.FinalizeAndCompleteMoneyEvent();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Coin")) return;

        bool isSafe = depositType == DepositType.Safe;
        GameManager.Instance?.RecordCoinDeposit(isSafe);

        if (depositFX != null)
        {
            depositFX.transform.position = other.transform.position;
            depositFX.Play();
        }

        if (audioSource != null)
        {
            AudioClip clip = isSafe ? depositSoundSafe : depositSoundPiggy;
            if (clip != null) audioSource.PlayOneShot(clip);
        }

        Debug.Log($"[CoinDeposit] Coin deposited into {depositType}.");
        Destroy(other.gameObject);
    }
}
