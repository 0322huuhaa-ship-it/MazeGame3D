using UnityEngine;

// ============================================================================
//  MAZE ADVENTURE 3D — Âm thanh tổng hợp bằng code
//  Toàn bộ nhạc nền + hiệu ứng âm thanh được TẠO TRỰC TIẾP bằng sóng sin
//  (không cần file .mp3/.wav bên ngoài). Mỗi clip chỉ tạo đúng 1 lần.
// ============================================================================

public static class ProceduralAudio
{
    const int SR = 44100; // tần số lấy mẫu

    static AudioClip _music, _coin, _key, _gem, _door, _win, _lose, _click, _hurt, _secret;

    public static AudioClip Music => _music != null ? _music : (_music = BuildMusic());
    public static AudioClip Coin  => _coin  != null ? _coin  : (_coin  = BuildCoin());
    public static AudioClip Key   => _key   != null ? _key   : (_key   = BuildKey());
    public static AudioClip Gem   => _gem   != null ? _gem   : (_gem   = BuildGem());
    public static AudioClip Door  => _door  != null ? _door  : (_door  = BuildDoor());
    public static AudioClip Win   => _win   != null ? _win   : (_win   = BuildWin());
    public static AudioClip Lose  => _lose  != null ? _lose  : (_lose  = BuildLose());
    public static AudioClip Click => _click != null ? _click : (_click = BuildClick());
    public static AudioClip Hurt  => _hurt  != null ? _hurt  : (_hurt  = BuildHurt());
    public static AudioClip Secret=> _secret!= null ? _secret: (_secret= BuildSecret());

    delegate float Synth(float t);

    /// <summary>Tạo AudioClip từ hàm sóng f(t) với t tính bằng giây.</summary>
    static AudioClip Make(string name, float seconds, Synth f)
    {
        int n = Mathf.CeilToInt(seconds * SR);
        var data = new float[n];
        for (int i = 0; i < n; i++)
            data[i] = Mathf.Clamp(f(i / (float)SR), -1f, 1f);
        AudioClip clip = AudioClip.Create(name, n, 1, SR, false);
        clip.SetData(data, 0);
        return clip;
    }

    static float Note(float t, float freq) => Mathf.Sin(2f * Mathf.PI * freq * t);

    // ---------------- HIỆU ỨNG ÂM ----------------

    // Nhặt xu: hai nốt vui vẻ "xình-xình"
    static AudioClip BuildCoin() => Make("sfx_coin", 0.22f, t =>
    {
        bool first = t < 0.09f;
        float f = first ? 988f : 1319f;
        float env = first ? Mathf.Exp(-t * 16f) : Mathf.Exp(-(t - 0.09f) * 12f);
        return Note(t, f) * env * 0.7f;
    });

    // Nhặt chìa khóa: hợp âm rải 3 nốt
    static AudioClip BuildKey() => Make("sfx_key", 0.38f, t =>
    {
        float[] notes = { 659f, 880f, 1175f };
        int idx = Mathf.Min(2, (int)(t / 0.1f));
        float lt = t - idx * 0.1f;
        return Note(t, notes[idx]) * Mathf.Exp(-lt * 9f) * 0.65f;
    });

    // Kim cương bí mật: quét tần số lấp lánh
    static AudioClip BuildGem() => Make("sfx_gem", 0.5f, t =>
    {
        // pha tích phân của f(t) = 900 + 2400t  =>  pha = 2π(900t + 1200t²)
        float v = Mathf.Sin(2f * Mathf.PI * (900f * t + 1200f * t * t));
        return v * Mathf.Exp(-t * 5f) * 0.6f;
    });

    // Cổng mở: tiếng rền trầm + rung
    static AudioClip BuildDoor() => Make("sfx_door", 0.9f, t =>
    {
        float v = Mathf.Sin(2f * Mathf.PI * (60f * t + 55f * t * t)); // quét 60 -> ~160 Hz
        v += (Mathf.PerlinNoise(t * 90f, 0.3f) * 2f - 1f) * 0.25f;    // nhiễu nhẹ
        float env = Mathf.Min(t / 0.08f, 1f) * Mathf.Exp(-t * 2.2f);
        return v * env * 0.8f;
    });

    // Chiến thắng: fanfare 4 nốt
    static AudioClip BuildWin() => Make("sfx_win", 1.25f, t =>
    {
        float[] notes = { 523f, 659f, 784f, 1047f };
        int idx = Mathf.Min(3, (int)(t / 0.16f));
        float lt = t - idx * 0.16f;
        float env = idx == 3 ? Mathf.Exp(-lt * 2.5f) : Mathf.Exp(-lt * 5f);
        float v = Note(t, notes[idx]) * env;
        if (idx == 3) v += Note(t, notes[idx] * 1.5f) * env * 0.4f; // hài âm cho nốt kết
        return v * 0.65f;
    });

    // Thất bại: rơi xuống trầm + rung nhẹ
    static AudioClip BuildLose() => Make("sfx_lose", 1.0f, t =>
    {
        float wobble = Mathf.Sin(2f * Mathf.PI * 5f * t) * 30f;
        float v = Mathf.Sin(2f * Mathf.PI * (380f * t - 260f * t * t) + wobble * t);
        return v * Mathf.Min(t / 0.05f, 1f) * Mathf.Exp(-t * 2.2f) * 0.7f;
    });

    // Nhấn nút UI
    static AudioClip BuildClick() => Make("sfx_click", 0.06f, t =>
        Note(t, 1100f) * Mathf.Exp(-t * 70f) * 0.5f);

    // Trúng chướng ngại: tiếng đục + trầm
    static AudioClip BuildHurt() => Make("sfx_hurt", 0.28f, t =>
    {
        float noise = Mathf.PerlinNoise(t * 400f, 0.7f) * 2f - 1f;
        float thud = Note(t, 95f);
        float env = Mathf.Exp(-t * 12f);
        return (noise * 0.5f + thud * 0.8f) * env * 0.8f;
    });

    // Phát hiện lối mật: rung linh tinh huyền bí
    static AudioClip BuildSecret() => Make("sfx_secret", 0.65f, t =>
    {
        float v = Mathf.Sin(2f * Mathf.PI * 600f * t + 55f * Mathf.Sin(2f * Mathf.PI * 5f * t));
        float v2 = Mathf.Sin(2f * Mathf.PI * (300f * t + 500f * t * t));
        return (v * 0.5f + v2 * 0.5f) * Mathf.Exp(-t * 3f) * 0.55f;
    });

    // ---------------- NHẠC NỀN ----------------
    // Vòng hợp âm Am - F - C - G (mỗi hợp âm 4 giây, tổng 16s lặp lại),
    // gồm lớp pad mềm + arpeggio nhẹ + bass. Phong cách phiêu lưu êm dịu.
    static AudioClip BuildMusic()
    {
        float[][] chords =
        {
            new[] { 220.00f, 261.63f, 329.63f }, // Am
            new[] { 174.61f, 220.00f, 261.63f }, // F
            new[] { 130.81f, 164.81f, 196.00f }, // C
            new[] { 196.00f, 246.94f, 293.66f }, // G
        };
        return Make("music_maze", 16f, t =>
        {
            int ci = (int)(t / 4f) % 4;
            float ct = t - (int)(t / 4f) * 4f;

            // Envelope mềm ở đầu/cuối mỗi hợp âm để lặp không bị "click"
            float padEnv = Mathf.Min(ct / 1.4f, 1f) * Mathf.Min((4f - ct) / 1.4f, 1f);

            float v = 0f;
            // Lớp pad
            for (int i = 0; i < 3; i++)
                v += Mathf.Sin(2f * Mathf.PI * chords[ci][i] * t) * 0.11f * padEnv;

            // Arpeggio nhẹ mỗi 0.5s
            float at = t % 0.5f;
            int noteIdx = (int)(t / 0.5f) % 4;
            float arpFreq = chords[ci][noteIdx % 3] * 2f;
            v += Mathf.Sin(2f * Mathf.PI * arpFreq * t) * 0.05f * Mathf.Exp(-at * 6f);

            // Bass
            v += Mathf.Sin(2f * Mathf.PI * chords[ci][0] * 0.5f * t) * 0.08f * padEnv;

            // LFO cho "sóng"
            v *= 0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 0.15f * t);

            // Soft-clip bằng công thức tanh thủ công (Mathf.Tanh chỉ có từ Unity 6)
            float x = v * 1.2f;
            float th = 2f / (1f + Mathf.Exp(-2f * x)) - 1f;
            return th * 0.9f;
        });
    }
}
