using UnityEngine;

// ============================================================================
//  MAZE ADVENTURE 3D — Điểm khởi động của game
//  Đây là script DUY NHẤT có sẵn trong scene Main.unity.
//  Khi game chạy, nó tự tạo mọi thứ còn lại:
//    GameManager (điều phối) + AudioSource (hiệu ứng âm thanh)
//    UIBuilder   (toàn bộ giao diện)
//    Nhạc nền    (tổng hợp bằng code, không cần file ngoài)
//  => Người chơi không phải gán bất kỳ thứ gì trong Inspector.
// ============================================================================

[DefaultExecutionOrder(-100)]
public class GameBootstrap : MonoBehaviour
{
    void Awake()
    {
        // Nếu scene vô tình chứa nhiều bootstrap, chỉ giữ một
        if (FindObjectOfType<GameManager>() != null)
        {
            Destroy(gameObject);
            return;
        }

        // ---- Quản lý trò chơi + nguồn âm hiệu ứng ----
        var gmGo = new GameObject("GameManager");
        GameManager gm = gmGo.AddComponent<GameManager>();
        AudioSource sfx = gmGo.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.spatialBlend = 0f; // âm 2D nghe rõ như nhau mọi vị trí
        gm.SfxSource = sfx;

        // ---- Giao diện người dùng ----
        var uiGo = new GameObject("UIBuilder");
        UIBuilder ui = uiGo.AddComponent<UIBuilder>();
        ui.Build(gm);
        gm.UI = ui;

        // ---- Nhạc nền nhẹ nhàng (lặp vô hạn) ----
        var musicGo = new GameObject("BackgroundMusic");
        AudioSource music = musicGo.AddComponent<AudioSource>();
        music.clip = ProceduralAudio.Music;
        music.loop = true;
        music.volume = 0.42f;
        music.playOnAwake = false;
        music.Play();

        // ---- Vào menu chính ----
        gm.EnterMenu();
        Debug.Log("[MAZE ADVENTURE 3D] Khởi động hoàn tất! Nhấn START GAME để chơi.");

        // ---- Chế độ tự kiểm tra: chạy exe với tham số --autotest ----
        if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--autotest") >= 0)
            StartCoroutine(AutoTest());

        // ---- Chế độ chụp ảnh minh họa cho báo cáo: tham số --shots ----
        if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--shots") >= 0)
            StartCoroutine(ShotsTour());
    }

    System.Collections.IEnumerator WaitForFrames(int n)
    {
        for (int i = 0; i < n; i++) yield return null;
    }

    System.Collections.IEnumerator ShotsTour()
    {
        Application.runInBackground = true;
        string dir = @"C:\Users\Admin\Desktop\MazeGame3D\Report_Images";
        System.IO.Directory.CreateDirectory(dir);

        // 1) Menu chính
        yield return WaitForFrames(45);
        ScreenCapture.CaptureScreenshot(dir + "\\01_menu.png");
        yield return WaitForFrames(8);

        // 2) Màn chọn màn chơi
        GameManager.Instance.OpenLevelSelect();
        yield return WaitForFrames(12);
        ScreenCapture.CaptureScreenshot(dir + "\\02_chon_man.png");
        yield return WaitForFrames(8);

        // 3) Cửa hàng trang phục (chờ vẽ avatar)
        GameManager.Instance.OpenShop();
        yield return WaitForFrames(25);
        ScreenCapture.CaptureScreenshot(dir + "\\03_cua_hang.png");
        yield return WaitForFrames(8);

        // 4) Gameplay Màn 1 (bản đồ mini + mũi tên + HUD)
        GameManager.Instance.StartLevel(0);
        yield return WaitForFrames(55);
        ScreenCapture.CaptureScreenshot(dir + "\\04_man1_gameplay.png");
        yield return WaitForFrames(8);

        // 5) Màn 2 — đứng cạnh lưỡi dao quay để minh họa chướng ngại
        GameManager.Instance.StartLevel(1);
        yield return WaitForFrames(50);
        Obstacle blade = FindObjectOfType<Obstacle>();
        if (blade != null && GameManager.Instance.Player != null)
        {
            Vector3 b = blade.transform.position;
            GameManager.Instance.Player.transform.position = b + new Vector3(2.1f, 0f, 0.4f);
        }
        yield return WaitForFrames(45);
        ScreenCapture.CaptureScreenshot(dir + "\\09_luoi_dao.png");
        yield return WaitForFrames(8);

        // 6) Dịch chuyển tới gần cổng thoát
        ExitGate gate = FindObjectOfType<ExitGate>();
        if (gate != null && GameManager.Instance.Player != null)
        {
            Vector3 g = gate.transform.position;
            GameManager.Instance.Player.transform.position = g + new Vector3(2.4f, 0f, 0f);
        }
        yield return WaitForFrames(45);
        ScreenCapture.CaptureScreenshot(dir + "\\05_cong_thoat.png");
        yield return WaitForFrames(8);

        // 6) Màn 3 bóng tối
        GameManager.Instance.StartLevel(2);
        yield return WaitForFrames(60);
        ScreenCapture.CaptureScreenshot(dir + "\\06_man3_bong_toi.png");
        yield return WaitForFrames(8);

        // 7) Menu tạm dừng
        GameManager.Instance.PauseGame();
        yield return WaitForFrames(12);
        ScreenCapture.CaptureScreenshot(dir + "\\07_tam_dung.png");
        yield return WaitForFrames(8);

        // 8) Màn hình chiến thắng
        GameManager.Instance.ResumeGame();
        yield return WaitForFrames(5);
        GameManager.Instance.WinLevel();
        yield return WaitForFrames(25);
        ScreenCapture.CaptureScreenshot(dir + "\\08_chien_thang.png");
        yield return WaitForFrames(8);

        Application.Quit();
    }

    System.Collections.IEnumerator AutoTest()
    {
        yield return null;
        Debug.Log("[AUTOTEST] Bắt đầu — gọi StartLevel(0)...");
        GameManager.Instance.StartLevel(0);
        yield return new WaitForSeconds(2.5f);
        GameManager g = GameManager.Instance;
        Debug.Log("[AUTOTEST] KẾT QUẢ: State=" + (g != null ? g.State.ToString() : "null") +
                  " | Player=" + (g != null && g.Player != null) +
                  " | Điểm=" + (g != null ? g.Score : -1));
        Application.Quit();
    }
}
