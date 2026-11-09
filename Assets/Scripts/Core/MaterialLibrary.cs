using System.Collections.Generic;
using UnityEngine;

// ============================================================================
//  MAZE ADVENTURE 3D — Thư viện vật liệu (URP)
//  Tất cả vật liệu được tạo bằng code, dùng chung cho toàn game (có cache)
//  => không cần file .mat, không cần gán trong Inspector.
// ============================================================================

public static class MaterialLibrary
{
    static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
    static Shader litShader;

    /// <summary>Shader Lit của URP (có dự phòng Simple Lit / Standard).
    /// QUAN TRỌNG: trong bản build .exe, Shader.Find trả về null nếu shader
    /// không được tham chiếu — vì vậy nguồn chính là vật liệu "giữ chỗ" trong
    /// Assets/Resources/ (Unity luôn đóng gói shader mà Resources dùng).</summary>
    public static Shader LitShader
    {
        get
        {
            if (litShader == null)
            {
                // 1) Nguồn chắc chắn: vật liệu giữ chỗ trong Resources
                Material keeper = Resources.Load<Material>("ShaderKeeper_Lit");
                if (keeper != null) litShader = keeper.shader;
                // 2) Dự phòng: tìm theo tên (luôn có trong Editor)
                if (litShader == null) litShader = Shader.Find("Universal Render Pipeline/Lit");
                if (litShader == null) litShader = Shader.Find("Universal Render Pipeline/Simple Lit");
                if (litShader == null) litShader = Shader.Find("Standard");
                if (litShader == null)
                    Debug.LogError("[MAZE] Không tìm được shader Lit! Kiểm tra Assets/Resources/ShaderKeeper_Lit.mat");
            }
            return litShader;
        }
    }

    /// <summary>
    /// Lấy (hoặc tạo) vật liệu theo tên khóa. Cùng một tên => cùng một vật liệu.
    /// emission: màu phát sáng (tùy chọn), nhân với emissionIntensity.
    /// </summary>
    public static Material Get(string key, Color baseColor, float smoothness = 0.08f,
                               float metallic = 0f, Color? emission = null, float emissionIntensity = 1f)
    {
        Material m;
        if (cache.TryGetValue(key, out m) && m != null) return m;

        m = new Material(LitShader);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", baseColor); // URP
        else m.color = baseColor;                                            // Built-in
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);

        if (emission.HasValue && m.HasProperty("_EmissiveColor"))
        {
            m.EnableKeyword("_EMISSION");
            Color e = emission.Value;
            m.SetColor("_EmissiveColor", new Color(
                e.r * emissionIntensity, e.g * emissionIntensity, e.b * emissionIntensity, 1f));
        }

        cache[key] = m;
        return m;
    }

    // ---------------- Pa màu dùng chung ----------------
    public static readonly Color Gold = new Color(1f, 0.78f, 0.2f);
    public static readonly Color GemPurple = new Color(0.75f, 0.4f, 1f);
    public static readonly Color DangerRed = new Color(0.85f, 0.2f, 0.12f);
    public static readonly Color LampWarm = new Color(1f, 0.82f, 0.5f);
}
