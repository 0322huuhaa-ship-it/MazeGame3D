using UnityEngine;

// ============================================================================
//  MAZE ADVENTURE 3D — Cổng thoát
//  • Bị khóa (đèn đỏ): cần đủ chìa khóa, lại gần sẽ hiện thông báo nhắc.
//  • Đủ chìa khóa + lại gần: phát âm thanh mở cửa, thanh cổng trượt xuống,
//    đệm phát sáng xanh lá.
//  • Cửa đã mở + người chơi bước vào: chiến thắng màn chơi.
// ============================================================================

public class ExitGate : MonoBehaviour
{
    [Header("Cấu hình")]
    public int keysRequired = 1;
    public Transform bars;          // nhóm thanh cổng (trượt xuống khi mở)
    public Renderer padRenderer;    // đệm dưới cổng (đổi màu đỏ -> xanh)
    public Light gateLight;

    public float detectRadius = 2.4f; // khoảng cách kích hoạt mở/gợi ý
    public float winRadius = 1.35f;   // khoảng cách tính là bước qua cổng

    Material padMat;
    float closedY;
    bool open;
    bool animating;
    Transform playerT;
    float hintCooldown;

    void Start()
    {
        if (bars != null) closedY = bars.localPosition.y;
        if (padRenderer != null) padMat = padRenderer.material; // bản sao riêng để đổi màu
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        AnimateBars();

        if (gm.State != GameState.Playing) return;
        if (playerT == null)
        {
            if (gm.Player == null) return;
            playerT = gm.Player.transform;
        }
        if (hintCooldown > 0f) hintCooldown -= Time.deltaTime;

        Vector3 p = transform.position;
        Vector2 d = new Vector2(p.x - playerT.position.x, p.z - playerT.position.z);
        float dist2 = d.sqrMagnitude;

        if (!open)
        {
            if (dist2 <= detectRadius * detectRadius)
            {
                if (gm.KeysCollected >= keysRequired)
                {
                    OpenGate();
                }
                else if (hintCooldown <= 0f)
                {
                    hintCooldown = 2.5f;
                    gm.ShowToast("Cổng bị khóa! Cần thêm " +
                                 (keysRequired - gm.KeysCollected) + " chìa khóa.");
                }
            }
        }
        else if (dist2 <= winRadius * winRadius)
        {
            gm.WinLevel(); // bước qua cổng => thắng
        }
    }

    void OpenGate()
    {
        open = true;
        animating = true;

        GameManager gm = GameManager.Instance;
        gm.PlaySfx(ProceduralAudio.Door);
        gm.ShowToast("Cổng đã mở! Hãy bước qua để chiến thắng!");

        if (padMat != null)
        {
            if (padMat.HasProperty("_BaseColor"))
                padMat.SetColor("_BaseColor", new Color(0.2f, 0.85f, 0.35f));
            if (padMat.HasProperty("_EmissiveColor"))
            {
                padMat.EnableKeyword("_EMISSION");
                padMat.SetColor("_EmissiveColor", new Color(0.1f, 1.6f, 0.3f));
            }
        }
        if (gateLight != null)
        {
            gateLight.color = new Color(0.3f, 1f, 0.45f);
            gateLight.intensity = 1.6f;
        }
        if (bars != null)
        {
            Collider col = bars.GetComponent<Collider>();
            if (col != null) col.enabled = false; // cho đi qua
        }
        GameFX.Burst(transform.position + Vector3.up * 2f, new Color(0.4f, 1f, 0.5f), 50, 0.1f, 3.5f);
    }

    void AnimateBars()
    {
        if (bars == null || !animating) return;
        Vector3 lp = bars.localPosition;
        lp.y = Mathf.MoveTowards(lp.y, closedY - 3.6f, 2.8f * Time.deltaTime);
        bars.localPosition = lp;
        if (lp.y <= closedY - 3.59f) animating = false;
    }
}
