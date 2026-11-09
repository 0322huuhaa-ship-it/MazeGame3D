using UnityEngine;

// ============================================================================
//  MAZE ADVENTURE 3D — Điều khiển nhân vật (góc nhìn thứ ba)
//  • Di chuyển: WASD hoặc phím mũi tên (theo hướng camera)
//  • Chạy nhanh: giữ Shift trái/phải
//  • Va chạm vật lý qua CharacterController (không xuyên tường)
//  • Nhân vật tự xoay mặt theo hướng di chuyển + vung tay chân khi đi
// ============================================================================

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Tốc độ")]
    public float walkSpeed = 3.4f;  // tốc độ đi bộ (hành lang hẹp — vừa phải dễ lái)
    public float runSpeed = 6.2f;   // tốc độ chạy nhanh (Shift)
    public float accel = 14f;       // độ mượt tăng/giảm tốc (càng cao càng gắt)
    public float turnSpeed = 11f;   // độ mượt khi xoay người
    public float gravity = -22f;    // trọng lực

    [Header("Chi tiết mô hình (LevelBuilder tự gán)")]
    public Transform ArmL;
    public Transform ArmR;
    public Transform LegL;
    public Transform LegR;

    /// <summary>Camera theo dõi — dùng để tính hướng di chuyển.</summary>
    public Transform CameraTransform { get; set; }

    public bool IsRunning { get; private set; }

    // ---- Input từ NÚT BẤM TRÊN MÀN HÌNH (UIBuilder ghi vào đây) ----
    // x: -1..1 (trái/phải), y: -1..1 (lùi/tiến) — cộng dồn với bàn phím
    public static Vector2 ScreenMove = Vector2.zero;
    public static bool ScreenRun = false;  // giữ nút CHẠY
    public static bool ScreenJump = false; // giữ nút NHẢY

    CharacterController cc;
    float yVel;          // vận tốc đứng (trọng lực)
    float animPhase;     // pha bước chân
    float swingAmount;   // biên độ vung tay chân (0..1)
    Vector3 currentVel;  // vận tốc ngang hiện tại (làm mượt tăng/giảm tốc)
    Vector3 pushVel;     // vận tốc đẩy lùi khi trúng chướng ngại

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Chỉ di chuyển khi đang chơi (không di chuyển khi Pause/Menu/Win/Lose)
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
            return;

        float dt = Time.deltaTime;
        // Gộp input bàn phím + nút bấm màn hình
        float h = Mathf.Clamp(Input.GetAxisRaw("Horizontal") + ScreenMove.x, -1f, 1f);
        float v = Mathf.Clamp(Input.GetAxisRaw("Vertical") + ScreenMove.y, -1f, 1f);
        bool wantRun = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || ScreenRun;

        // Hướng di chuyển tính theo hướng nhìn của camera (đi "lên màn hình" = W)
        Vector3 moveDir = Vector3.zero;
        if (CameraTransform != null && (h != 0f || v != 0f))
        {
            Vector3 fwd = CameraTransform.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = CameraTransform.right; right.y = 0f; right.Normalize();
            moveDir = fwd * v + right * h;
            if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();
        }
        IsRunning = wantRun && moveDir != Vector3.zero;

        // Tăng/giảm tốc mượt — dễ điều khiển hơn hẳn so với bật/tắt tức thời
        Vector3 targetVel = moveDir * (IsRunning ? runSpeed : walkSpeed);
        currentVel = Vector3.Lerp(currentVel, targetVel, 1f - Mathf.Exp(-accel * dt));

        // Tổng vận tốc = di chuyển + đẩy lùi (giảm dần)
        Vector3 vel = currentVel + pushVel;
        pushVel = Vector3.Lerp(pushVel, Vector3.zero, 1f - Mathf.Exp(-6f * dt));

        // Nhảy: Space hoặc giữ nút NHẢY trên màn hình (khi đang chạm đất)
        if (cc.isGrounded && (Input.GetKeyDown(KeyCode.Space) || ScreenJump))
            yVel = Mathf.Sqrt(2f * -gravity * 1.1f);

        // Trọng lực
        yVel += gravity * dt;
        if (cc.isGrounded && yVel < 0f) yVel = -2f;

        // CharacterController tự xử lý va chạm với tường (không xuyên được)
        cc.Move((vel + Vector3.up * yVel) * dt);

        // Xoay mặt theo hướng di chuyển
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRot, 1f - Mathf.Exp(-turnSpeed * dt));

            animPhase += dt * (IsRunning ? 13f : 8.5f);
            swingAmount = Mathf.Lerp(swingAmount, 1f, 1f - Mathf.Exp(-8f * dt));
        }
        else
        {
            swingAmount = Mathf.Lerp(swingAmount, 0f, 1f - Mathf.Exp(-8f * dt));
        }

        // Vung tay / bước chân
        float armSwing = Mathf.Sin(animPhase) * 35f * swingAmount;
        float legSwing = Mathf.Sin(animPhase) * 48f * swingAmount;
        if (ArmL != null) ArmL.localRotation = Quaternion.Euler(armSwing, 0f, 0f);
        if (ArmR != null) ArmR.localRotation = Quaternion.Euler(-armSwing, 0f, 0f);
        if (LegL != null) LegL.localRotation = Quaternion.Euler(-legSwing, 0f, 0f);
        if (LegR != null) LegR.localRotation = Quaternion.Euler(legSwing, 0f, 0f);
    }

    /// <summary>Bị đẩy lùi away từ vị trí nguy hiểm (khi trúng lưỡi dao).</summary>
    public void Knockback(Vector3 hazardPos)
    {
        Vector3 dir = transform.position - hazardPos;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = -transform.forward;
        dir.Normalize();
        pushVel = dir * 9f;
        yVel = Mathf.Max(yVel, 2.5f);
    }
}
