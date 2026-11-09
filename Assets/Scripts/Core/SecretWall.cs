using UnityEngine;

// ============================================================================
//  MAZE ADVENTURE 3D — Tường giả che lối mật
//  Trông giống tường thường (hơi ánh xanh nhẹ) nhưng khi người chơi lại gần,
//  bức tường từ từ chìm xuống đất mở đường vào hốc bí mật (kim cương + xu).
// ============================================================================

public class SecretWall : MonoBehaviour
{
    public float detectRadius = 1.9f; // khoảng cách kích hoạt

    Transform playerT;
    bool opening;
    float targetSinkY;
    float sinkSpeed = 2.2f;

    void Start()
    {
        // Chìm sâu xuống dưới sàn (qua hết chiều cao tường + dư 0.5)
        targetSinkY = transform.position.y - transform.localScale.y - 0.5f;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        if (!opening)
        {
            if (gm.State != GameState.Playing) return;
            if (playerT == null)
            {
                if (gm.Player == null) return;
                playerT = gm.Player.transform;
            }

            Vector3 p = transform.position;
            Vector2 d = new Vector2(p.x - playerT.position.x, p.z - playerT.position.z);
            if (d.sqrMagnitude <= detectRadius * detectRadius)
            {
                opening = true;
                gm.PlaySfx(ProceduralAudio.Secret);
                gm.ShowToast("Bạn phát hiện một lối mật!");
                GameFX.Burst(p + Vector3.up * 1.5f, new Color(0.7f, 1f, 0.8f), 24, 0.09f, 2.5f);
            }
        }
        else if (transform.position.y > targetSinkY)
        {
            // Chìm xuống đất
            Vector3 p = transform.position;
            p.y -= sinkSpeed * Time.deltaTime;
            if (p.y <= targetSinkY)
            {
                p.y = targetSinkY;
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false; // hết chắn đường
            }
            transform.position = p;
        }
    }
}
