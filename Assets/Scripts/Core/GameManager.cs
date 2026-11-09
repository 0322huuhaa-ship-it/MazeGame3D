using UnityEngine;

// ============================================================================
//  MAZE ADVENTURE 3D — Quản lý trò chơi (Singleton)
//  • Trạng thái: Menu / Playing / Paused / Win / Lose
//  • Điểm số, đồng xu, chìa khóa, mạng, đồng hồ đếm ngược
//  • Bắt đầu / tạm dừng / chơi lại / chuyển màn / quay về menu
//  • Lưu kỷ lục từng màn bằng PlayerPrefs
// ============================================================================

public enum GameState { Menu, Playing, Paused, Win, Lose }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // UIBuilder & AudioSource được GameBootstrap gán lúc khởi động
    public UIBuilder UI { get; set; }
    public AudioSource SfxSource { get; set; }

    // ---- Thông tin ván chơi hiện tại ----
    public GameState State { get; private set; } = GameState.Menu;
    public LevelDef CurrentDef { get; private set; }
    public int LevelIndex { get; private set; }
    public PlayerController Player { get; private set; }
    public ThirdPersonCamera Cam { get; private set; }

    public int CoinsCollected { get; private set; }
    public int TotalCoins { get; private set; }
    public int KeysCollected { get; private set; }
    public int KeysRequired { get; private set; }
    public int Lives { get; private set; }
    public int Score { get; private set; }
    public float TimeLeft { get; private set; }
    public bool IsLastLevel => LevelIndex >= MazeDatabase.Levels.Length - 1;

    GameObject levelRoot;
    float invulnTimer;   // thời gian bất tử sau khi trúng đòn
    float toastLock;     // chống lặp lại thông báo giống nhau
    string lastToast;
    bool hudDirty = true;
    int lastHudSecond = -1;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ================= LUỒNG MÀN CHƠI =================

    public void EnterMenu()
    {
        Time.timeScale = 1f;
        State = GameState.Menu;
        SetCursorLocked(false);
        UI.ShowMainMenu();
    }

    public void OpenLevelSelect()
    {
        UI.ShowLevelSelect();
    }

    public void OpenShop()
    {
        UI.ShowShop();
    }

    /// <summary>Ví xu của người chơi — lưu vĩnh viễn bằng PlayerPrefs.</summary>
    public static int Wallet
    {
        get => PlayerPrefs.GetInt("maze_wallet", 0);
        set { PlayerPrefs.SetInt("maze_wallet", Mathf.Max(0, value)); PlayerPrefs.Save(); }
    }

    /// <summary>Bắt đầu màn chơi theo số thứ tự (0, 1, 2, ...).
    /// Có màn bảo hiểm: nếu có lỗi xảy ra, hiện LỖI ngay trên màn hình game.</summary>
    public void StartLevel(int index)
    {
        try
        {
            StartLevelInternal(index);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[MAZE] LỖI KHI VÀO MÀN " + (index + 1) + ": " + e);
            State = GameState.Menu;
            Time.timeScale = 1f;
            SetCursorLocked(false);
            if (UI != null)
            {
                UI.ShowLevelSelect();
                UI.DisplayToast("LỖI VÀO MÀN " + (index + 1) + ": " + e.GetType().Name +
                                " — chụp màn hình Console gửi trợ lý!");
            }
        }
    }

    private void StartLevelInternal(int index)
    {
        Debug.Log("[MAZE] Bắt đầu màn chơi số " + (index + 1) + "...");
        LevelIndex = Mathf.Clamp(index, 0, MazeDatabase.Levels.Length - 1);
        CurrentDef = MazeDatabase.Levels[LevelIndex];

        Time.timeScale = 1f;
        if (levelRoot != null) Destroy(levelRoot);

        // Sinh mê cung + dựng cảnh 3D
        GeneratedMaze maze = MazeGenerator.Generate(CurrentDef);
        levelRoot = LevelBuilder.Build(CurrentDef, maze, CostumeDatabase.Equipped,
                                       out PlayerController player, out ThirdPersonCamera cam);
        Player = player;
        Cam = cam;

        // Reset thống kê
        CoinsCollected = 0;
        TotalCoins = maze.coins.Count;
        KeysCollected = 0;
        KeysRequired = CurrentDef.keys;
        Lives = CurrentDef.lives;
        Score = 0;
        TimeLeft = CurrentDef.timeLimit;
        invulnTimer = 0f;

        UI.ShowHUD(CurrentDef);
        hudDirty = true;
        lastHudSecond = -1;
        State = GameState.Playing;
        SetCursorLocked(false); // chuột LUÔN hiển thị để bấm được các nút trên màn hình

        // Reset trạng thái nút bấm màn hình (chống kẹt phím ảo)
        PlayerController.ScreenMove = Vector2.zero;
        PlayerController.ScreenRun = false;
        PlayerController.ScreenJump = false;

        ShowToast(CurrentDef.name + " — " + CurrentDef.subtitle +
                  ": Tìm " + KeysRequired + " chìa khóa rồi đến cổng thoát!");
    }

    void Update()
    {
        // Thông báo tự tắt
        if (toastLock > 0f) toastLock -= Time.unscaledDeltaTime;

        // Phím tắt tạm dừng: Esc hoặc P
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (State == GameState.Playing) { PauseGame(); return; }
            if (State == GameState.Paused) { ResumeGame(); return; }
        }

        if (State != GameState.Playing) return;

        // Đồng hồ đếm ngược
        TimeLeft -= Time.deltaTime;
        if (TimeLeft <= 0f)
        {
            TimeLeft = 0f;
            LoseLevel("Hết thời gian!");
            return;
        }

        if (invulnTimer > 0f) invulnTimer -= Time.deltaTime;

        // Cập nhật HUD (chỉ khi có thay đổi để tiết kiệm)
        int sec = (int)TimeLeft;
        if (hudDirty || sec != lastHudSecond)
        {
            lastHudSecond = sec;
            hudDirty = false;
            UI.UpdateHUD(CoinsCollected, TotalCoins, KeysCollected, KeysRequired,
                         Lives, TimeLeft, Score);
            if (sec <= 10 && sec > 0) PlaySfx(ProceduralAudio.Click, 0.7f); // tích tắc 10s cuối
        }
    }

    // ================= SỰ KIỆN GAMEPLAY =================

    /// <summary>Gọi bởi Collectible khi người chơi nhặt vật phẩm.</summary>
    public void Collect(Collectible c)
    {
        switch (c.kind)
        {
            case Collectible.Kind.Coin:
                CoinsCollected++;
                Score += 10;
                PlaySfx(ProceduralAudio.Coin);
                break;

            case Collectible.Kind.Key:
                KeysCollected++;
                Score += 50;
                PlaySfx(ProceduralAudio.Key);
                if (KeysCollected >= KeysRequired)
                    ShowToast("Đã đủ " + KeysRequired + " chìa khóa! Hãy tìm cổng thoát.");
                else
                    ShowToast("Nhặt chìa khóa " + KeysCollected + "/" + KeysRequired + "!");
                break;

            case Collectible.Kind.Gem:
                Score += 100;
                PlaySfx(ProceduralAudio.Gem);
                ShowToast("Kim cương bí mật: +100 điểm!");
                break;
        }
        hudDirty = true;
    }

    /// <summary>Gọi bởi Obstacle khi người chơi trúng lưỡi dao.</summary>
    public void OnPlayerHit(Vector3 hazardPos)
    {
        if (State != GameState.Playing || invulnTimer > 0f) return;

        invulnTimer = 1.4f;
        Lives--;
        hudDirty = true;

        PlaySfx(ProceduralAudio.Hurt);
        UI.FlashDamage();
        if (Cam != null) Cam.Shake(0.28f); // rung màn hình

        if (Player != null)
        {
            Player.Knockback(hazardPos);
            GameFX.Burst(Player.transform.position, new Color(1f, 0.3f, 0.2f), 18, 0.08f, 2.5f);
        }

        if (Lives <= 0) LoseLevel("Bạn đã hết mạng!");
        else ShowToast("Trúng chướng ngại! Còn " + Lives + " mạng.");
    }

    /// <summary>Bước qua cổng thoát đã mở -> thắng.</summary>
    public void WinLevel()
    {
        if (State != GameState.Playing) return;
        State = GameState.Win;
        SetCursorLocked(false);

        PlaySfx(ProceduralAudio.Win);

        int timeBonus = Mathf.RoundToInt(TimeLeft) * 2;
        int lifeBonus = Lives * 50;
        Score += timeBonus + lifeBonus;

        // Thu hoạch xu: toàn bộ xu nhặt được nạp vào ví (mua trang phục)
        int earnedCoins = CoinsCollected;
        Wallet += earnedCoins;

        // Lưu kỷ lục
        string bestKey = "maze_best_" + LevelIndex;
        int best = PlayerPrefs.GetInt(bestKey, 0);
        bool newRecord = Score > best;
        if (newRecord)
        {
            best = Score;
            PlayerPrefs.SetInt(bestKey, best);
            PlayerPrefs.Save();
        }

        if (Player != null)
            GameFX.Burst(Player.transform.position + Vector3.up * 1.5f,
                         new Color(1f, 0.85f, 0.2f), 80, 0.12f, 4f);

        string stats =
            "Xu: " + CoinsCollected + "/" + TotalCoins +
            "    Chìa khóa: " + KeysCollected + "/" + KeysRequired + "\n" +
            "Thời gian còn lại: " + FormatTime(TimeLeft) + "\n" +
            "Thưởng thời gian: +" + timeBonus + "    Thưởng mạng: +" + lifeBonus + "\n" +
            "Kiếm xu: +" + earnedCoins + " xu (Ví của bạn: " + Wallet + ")\n" +
            "TỔNG ĐIỂM: " + Score + (newRecord ? "   KỶ LỤC MỚI!" : "   (Kỷ lục: " + best + ")");

        UI.ShowWin(stats, !IsLastLevel, IsLastLevel);
    }

    public void LoseLevel(string reason)
    {
        if (State != GameState.Playing) return;
        State = GameState.Lose;
        SetCursorLocked(false);
        PlaySfx(ProceduralAudio.Lose);
        int earned = CoinsCollected / 2; // thua vẫn giữ nửa số xu đã nhặt
        Wallet += earned;
        UI.ShowLose(reason + "\nBạn được giữ lại " + earned + " xu vào ví.");
    }

    // ================= NÚT NHẤN TỪ GIAO DIỆN =================

    public void PauseGame()
    {
        if (State != GameState.Playing) return;
        State = GameState.Paused;
        Time.timeScale = 0f;
        SetCursorLocked(false);
        UI.ShowPause(true);
    }

    public void ResumeGame()
    {
        if (State != GameState.Paused) return;
        State = GameState.Playing;
        Time.timeScale = 1f;
        SetCursorLocked(false);
        UI.ShowPause(false);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        UI.ShowPause(false);
        StartLevel(LevelIndex);
    }

    public void NextLevel()
    {
        Time.timeScale = 1f;
        StartLevel(LevelIndex + 1);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        if (levelRoot != null) { Destroy(levelRoot); levelRoot = null; }
        Player = null;
        EnterMenu();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ================= TIỆN ÍCH =================

    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (SfxSource != null && clip != null) SfxSource.PlayOneShot(clip, volume);
    }

    public void ShowToast(string message)
    {
        if (message == lastToast && toastLock > 0f) return;
        lastToast = message;
        toastLock = 2.5f;
        if (UI != null) UI.DisplayToast(message);
    }

    /// <summary>Định dạng giây thành "mm:ss".</summary>
    public static string FormatTime(float t)
    {
        int s = Mathf.Max(0, Mathf.FloorToInt(t));
        return string.Format("{0:00}:{1:00}", s / 60, s % 60);
    }

    static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
