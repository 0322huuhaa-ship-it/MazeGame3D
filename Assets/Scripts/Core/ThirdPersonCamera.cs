using UnityEngine;

// ============================================================================
//  MAZE ADVENTURE 3D — Camera 3 CHẾ ĐỘ quan sát (nhấn C để đổi)
//    1. Cận cảnh  — third person cổ điển, sát nhân vật
//    2. Toàn cảnh — cao và rộng hơn, nhìn thấy nhiều hành lang xung quanh
//    3. Trên cao  — gần như nhìn từ trên xuống, bao quát cả khu vực
//  • Di chuột: xoay (chế độ cận cảnh)   • Lăn chuột: zoom
//  • Chống xuyên tường ở chế độ cận cảnh
//  • Camera.Shake() — rung màn hình khi trúng chướng ngại
// ============================================================================

public class ThirdPersonCamera : MonoBehaviour
{
    public enum CameraMode { Close, Overview, TopDown }

    public Transform target; // nhân vật theo dõi

    [Header("Cài đặt chung")]
    public float mouseSensitivity = 2.0f; // xoay bằng chuột (kéo khi GIỮ CHUỘT PHẢI)
    public float focusHeight = 1.6f;      // điểm nhìn tập trung (đầu nhân vật)
    public float followSpeed = 1.6f;      // camera bám sau lưng CHẬM — nhấn A/D không bị xoay trớt trớt
    public float posSmooth = 16f;         // độ mượt vị trí camera

    [HideInInspector] public float yaw;   // góc ngang hiện tại

    CameraMode mode = CameraMode.Close;
    float targetPitch = 22f, targetDist = 6.5f;
    float curPitch = 22f, curDist = 6.5f;
    float zoomBias;      // phần zoom cộng thêm từ lăn chuột
    float shakeAmp;      // biên độ rung màn hình
    Vector3 currentPos;
    bool started;

    static readonly string[] ModeNames =
        { "Cận cảnh", "Toàn cảnh — nhìn rộng", "Trên cao — bao quát" };

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        // Phím C: chuyển vòng quanh 3 chế độ camera
        if (Input.GetKeyDown(KeyCode.C))
        {
            mode = (CameraMode)(((int)mode + 1) % 3);
            switch (mode)
            {
                case CameraMode.Close:    targetPitch = 22f; targetDist = 6.5f; break;
                case CameraMode.Overview: targetPitch = 50f; targetDist = 14f;  break;
                case CameraMode.TopDown:  targetPitch = 76f; targetDist = 19f;  break;
            }
            gm.PlaySfx(ProceduralAudio.Click, 0.4f);
            gm.ShowToast("Góc nhìn: " + ModeNames[(int)mode]);
        }
    }

    /// <summary>Gọi khi người chơi trúng chướng ngại để rung màn hình.</summary>
    public void Shake(float amplitude) => shakeAmp = Mathf.Max(shakeAmp, amplitude);

    void LateUpdate()
    {
        if (target == null) return;
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.State == GameState.Paused) return; // đứng yên khi tạm dừng

        float dt = Time.deltaTime;

        // 1) Lướt theo hướng nhân vật; GIỮ CHUỘT PHẢI + kéo để xoay tay
        //    (chuột luôn tự do nên bấm nút UI không bao giờ bị chặn)
        yaw = Mathf.LerpAngle(yaw, target.eulerAngles.y, followSpeed * dt);
        if (mode == CameraMode.Close && Input.GetMouseButton(1))
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;

        // 2) Lăn chuột zoom (trong phạm vi cho phép)
        zoomBias = Mathf.Clamp(zoomBias - Input.mouseScrollDelta.y * 0.8f, -2.5f, 8f);

        // 3) Chuyển động mượt tới góc/khoảng cách của chế độ hiện tại
        curPitch = Mathf.Lerp(curPitch, targetPitch, 1f - Mathf.Exp(-6f * dt));
        curDist = Mathf.Lerp(curDist, Mathf.Max(2.5f, targetDist + zoomBias),
                             1f - Mathf.Exp(-6f * dt));

        Vector3 focus = target.position + Vector3.up * focusHeight;
        Quaternion rot = Quaternion.Euler(curPitch, yaw, 0f);
        Vector3 desired = focus - rot * Vector3.forward * curDist;

        // 4) Chống xuyên tường: chỉ cần ở chế độ cận cảnh (camera thấp, gần tường).
        //    Hai chế độ cao bay trên nóc tường nên không cần.
        if (mode == CameraMode.Close)
        {
            Vector3 dir = desired - focus;
            float len = dir.magnitude;
            if (Physics.Raycast(focus, dir.normalized, out RaycastHit hit, len + 0.3f,
                                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                len = Mathf.Max(1.2f, hit.distance - 0.35f);
            }
            desired = focus + dir.normalized * len;
        }

        // 5) Làm mượt vị trí rồi luôn nhìn vào nhân vật
        if (!started) { currentPos = desired; started = true; }
        currentPos = Vector3.Lerp(currentPos, desired, 1f - Mathf.Exp(-posSmooth * dt));
        transform.position = currentPos;
        transform.LookAt(focus);

        // 6) Rung màn hình (giảm dần rồi tắt)
        if (shakeAmp > 0f)
        {
            transform.position += Random.insideUnitSphere * shakeAmp;
            shakeAmp = Mathf.MoveTowards(shakeAmp, 0f, dt * 1.5f);
        }
    }
}
