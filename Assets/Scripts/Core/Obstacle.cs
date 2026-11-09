using UnityEngine;

// ============================================================================
//  MAZE ADVENTURE 3D — Chướng ngại vật: lưỡi dao quay
//  Quay quanh trục đứng; nếu người chơi chạm vào trong bán kính sẽ mất mạng
//  (GameManager xử lý thời gian bất tử + đẩy lùi).
// ============================================================================

public class Obstacle : MonoBehaviour
{
    public Transform spinner;          // phần quay (được LevelBuilder gán)
    public float rpm = 100f;           // vòng / phút
    public float damageRadius = 1.25f; // tầm với của lưỡi dao
    const float HitArcDeg = 28f;       // chỉ trúng khi lưỡi quét VÀO người (±28°)

    Transform playerT;
    float hitCooldown;
    static int lastHintLevel = -1;     // gợi ý cách vượt 1 lần duy nhất mỗi màn

    void Update()
    {
        // Luôn quay (khi Pause thì deltaTime = 0 nên tự đứng yên)
        if (spinner != null)
            spinner.Rotate(0f, rpm * Time.deltaTime, 0f, Space.Self);

        GameManager gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;
        if (playerT == null)
        {
            if (gm.Player == null) return;
            playerT = gm.Player.transform;
        }
        if (hitCooldown > 0f) { hitCooldown -= Time.deltaTime; return; }

        Vector3 p = transform.position;
        Vector2 d = new Vector2(p.x - playerT.position.x, p.z - playerT.position.z);
        float dist = d.magnitude;
        if (dist > damageRadius) return;

        // Đến gần lần đầu trong màn -> hướng dẫn cách vượt
        if (lastHintLevel != gm.LevelIndex && dist < 4.5f)
        {
            lastHintLevel = gm.LevelIndex;
            gm.ShowToast("Chờ lưỡi dao quét qua rồi băng qua — hoặc NHẢY (Space) qua nó!");
        }

        // Đã nhảy lên trên cao -> lưỡi dao thấp sơt qua người, an toàn!
        if (playerT.position.y > 1.0f) return;

        // Chỉ mất mạng khi CÓ LƯỠI chỉ về phía người chơi => chờ đúng nhịp là đi qua được
        Vector3 toPlayer = new Vector3(-d.x, 0f, -d.y).normalized;
        float target = Mathf.Atan2(toPlayer.x, toPlayer.z) * Mathf.Rad2Deg;
        float diff = Mathf.Abs(Mathf.DeltaAngle(spinner != null ? spinner.eulerAngles.y : 0f, target));
        if (diff > 45f) diff = 90f - diff; // cánh dao đối xứng mỗi 90°
        if (diff <= HitArcDeg)
        {
            hitCooldown = 0.5f;
            gm.OnPlayerHit(p);
        }
    }
}
