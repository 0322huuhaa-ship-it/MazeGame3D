using UnityEngine;

// ============================================================================
//  MAZE ADVENTURE 3D — Vật phẩm nhặt được (Xu / Chìa khóa / Kim cương)
//  Tự quay + nhấp nhô; khi người chơi lại gần sẽ tự nhặt (theo khoảng cách,
//  không phụ thuộc vật lý trigger nên không bao giờ bị lỡ mất vật phẩm).
// ============================================================================

public class Collectible : MonoBehaviour
{
    public enum Kind { Coin, Key, Gem }

    public Kind kind = Kind.Coin;
    public float pickRadius = 1.05f; // bán kính tự nhặt

    float baseY;
    float phase;
    Transform playerT;

    void Start()
    {
        baseY = transform.position.y;
        phase = Random.Range(0f, 6.28f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;
        if (playerT == null)
        {
            if (gm.Player == null) return;
            playerT = gm.Player.transform;
        }

        float dt = Time.deltaTime;

        // Xoay quanh trục đứng + nhấp nhô lên xuống
        transform.Rotate(0f, 120f * dt, 0f, Space.World);
        Vector3 p = transform.position;
        phase += 2.6f * dt;
        p.y = baseY + Mathf.Sin(phase) * 0.14f;
        transform.position = p;

        // Người chơi lại gần -> nhặt
        Vector2 d = new Vector2(p.x - playerT.position.x, p.z - playerT.position.z);
        if (d.sqrMagnitude <= pickRadius * pickRadius &&
            Mathf.Abs(p.y - playerT.position.y) < 2.2f)
        {
            switch (kind)
            {
                case Kind.Coin:
                    GameFX.Burst(p, new Color(1f, 0.8f, 0.15f), 16, 0.07f, 2.5f);
                    break;
                case Kind.Key:
                    GameFX.Burst(p, new Color(1f, 0.9f, 0.4f), 28, 0.09f, 3f);
                    break;
                case Kind.Gem:
                    GameFX.Burst(p, new Color(0.8f, 0.5f, 1f), 34, 0.1f, 3.2f);
                    break;
            }
            gm.Collect(this);
            Destroy(gameObject);
        }
    }
}
