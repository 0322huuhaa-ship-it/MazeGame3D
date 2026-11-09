using System.Collections.Generic;
using UnityEngine;

// ============================================================================
//  MAZE ADVENTURE 3D — Mũi tên chỉ đường bay trên đầu nhân vật
//  • Còn chìa khóa: chỉ về chìa khóa GẦN NHẤT (màu vàng)
//  • Đủ chìa khóa (hoặc đã nhặt hết): chỉ về CỔNG THOÁT (màu xanh lá)
//  • Nhấp nhô nhẹ, tự xoay theo mục tiêu — giúp không bao giờ "lạc đường chết"
// ============================================================================

public class CompassArrow : MonoBehaviour
{
    public List<Transform> keyTransforms = new List<Transform>(); // LevelBuilder gán
    public Vector3 exitWorld;                                     // vị trí cổng thoát
    public Material goldMat;
    public Material greenMat;

    Transform playerT;
    float bobPhase;

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;
        if (playerT == null) playerT = transform.parent;

        // Chìa khóa đã nhặt bị hủy => transform == null => loại khỏi danh sách
        keyTransforms.RemoveAll(k => k == null);

        bool toExit = gm.KeysCollected >= gm.KeysRequired || keyTransforms.Count == 0;
        Vector3 target = toExit ? exitWorld : NearestKey();

        // Xoay mũi tên về phía mục tiêu (chỉ quanh trục đứng)
        Vector3 dir = target - playerT.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion rot = Quaternion.LookRotation(dir);
            Vector3 e = rot.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, e.y, 0f);
        }

        // Nhấp nhô nhẹ
        bobPhase += Time.deltaTime * 3f;
        Vector3 lp = transform.localPosition;
        lp.y = 2.5f + Mathf.Sin(bobPhase) * 0.12f;
        transform.localPosition = lp;

        // Đổi màu theo mục tiêu: vàng = chìa khóa, xanh lá = cổng
        Material want = toExit ? greenMat : goldMat;
        foreach (Transform child in transform)
        {
            Renderer r = child.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != want) r.sharedMaterial = want;
        }
    }

    Vector3 NearestKey()
    {
        Vector3 best = exitWorld;
        float bestD = float.MaxValue;
        foreach (Transform k in keyTransforms)
        {
            float d = (k.position - playerT.position).sqrMagnitude;
            if (d < bestD) { bestD = d; best = k.position; }
        }
        return best;
    }
}
