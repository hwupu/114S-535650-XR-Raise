using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MirrorMovment: MonoBehaviour
{
    public Transform playerTarget;
    public Transform mirror;

    void Start()
    {
        // Inspector 沒指定時自動找場景內的 OVRCameraRig 眼睛位置
        if (playerTarget == null)
        {
            var rig = FindObjectOfType<OVRCameraRig>();
            if (rig != null)
            {
                playerTarget = rig.centerEyeAnchor;
                Debug.Log($"[MirrorMovment] 自動綁定 playerTarget = {playerTarget.name}");
            }
            else
                Debug.LogWarning("[MirrorMovment] 找不到 OVRCameraRig，鏡子不會跟蹤玩家。");
        }
    }

    void Update()
    {
        // 防止 Transform 被銷毀後仍繼續存取
        if (playerTarget == null || mirror == null) return;

        Vector3 localPlayer = mirror.InverseTransformPoint(playerTarget.position);
        transform.position = mirror.TransformPoint(new Vector3(localPlayer.x, localPlayer.y, -localPlayer.z / 2));

        Vector3 lookAtMirror = mirror.TransformPoint(new Vector3(-localPlayer.x, localPlayer.y, localPlayer.z));
        transform.LookAt(lookAtMirror);
    }
}
