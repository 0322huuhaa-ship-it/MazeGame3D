#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

// ============================================================================
//  MAZE ADVENTURE 3D — Công cụ hỗ trợ trong Unity Editor
//  • "Tạo / mở Scene chính": tạo (hoặc sửa + mở) scene Main.unity có sẵn
//    GameBootstrap, đồng thời thêm vào Build Settings. Dùng nếu scene bị lỗi
//    hoặc muốn tạo lại từ đầu.
//  • "Kiểm tra mê cung (BFS)": chạy thử bộ sinh mê cung và chứng minh mọi
//    màn đều có đường ra, chìa khóa với kim cương bí mật đều với được.
// ============================================================================

public static class MazeGameSetup
{
    const string ScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Tools/MAZE ADVENTURE 3D/Tạo hoặc mở Scene chính")]
    public static void SetupScene()
    {
        Directory.CreateDirectory("Assets/Scenes");

        if (File.Exists(ScenePath))
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (Object.FindObjectOfType<GameBootstrap>() == null)
            {
                var go = new GameObject("GameBootstrap");
                go.AddComponent<GameBootstrap>();
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log("[MAZE] Đã sửa Main.unity (bổ sung GameBootstrap).");
            }
        }
        else
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("GameBootstrap");
            go.AddComponent<GameBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[MAZE] Đã tạo mới Assets/Scenes/Main.unity.");
        }

        AddToBuildSettings();
        Debug.Log("[MAZE] Scene chính đã mở và sẵn sàng. Nhấn nút Play để chơi!");
    }

    static void AddToBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == ScenePath)) return;
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    [MenuItem("Tools/MAZE ADVENTURE 3D/Kiểm tra mê cung (BFS)")]
    public static void ValidateMazes()
    {
        int fail = 0;
        for (int i = 0; i < MazeDatabase.Levels.Length; i++)
        {
            LevelDef def = MazeDatabase.Levels[i];
            GeneratedMaze m = MazeGenerator.Generate(def);

            bool exitOk = Reachable(m, m.start, m.exit, false);
            bool keysOk = m.keys.TrueForAll(k => Reachable(m, m.start, k, false));
            bool gemsOk = m.gems.TrueForAll(g => Reachable(m, m.start, g, true));

            if (exitOk && keysOk && gemsOk)
            {
                Debug.Log($"[MAZE] {def.name} ({def.subtitle}) — HỢP LỆ ✓  " +
                          $"sàn: {CountFloor(m)} ô, chìa khóa: {m.keys.Count}, " +
                          $"xu: {m.coins.Count}, dao quay: {m.blades.Count}, " +
                          $"hốc bí mật: {m.secretEntrances.Count}, đèn: {m.lamps.Count}");
            }
            else
            {
                fail++;
                Debug.LogError($"[MAZE] {def.name} — LỖI! exitOk={exitOk} keysOk={keysOk} gemsOk={gemsOk}");
            }
        }
        Debug.Log(fail == 0
            ? "[MAZE] TẤT CẢ CÁC MÀN ĐỀU CÓ ĐƯỜNG ĐI HỢP LỆ. Yên tâm chơi!"
            : $"[MAZE] Có {fail} màn lỗi — hãy đổi seed trong MazeDatabase.cs!");
    }

    [MenuItem("Tools/MAZE ADVENTURE 3D/Chẩn đoán: tự vào cả 3 màn")]
    public static void DiagnoseStartLevel()
    {
        Debug.Log("[DIAG] === BẮT ĐẦU CHẨN ĐOÁN: thử khởi động cả 3 màn ===");
        var bsGo = new GameObject("DiagBootstrap");
        GameManager gm = null;
        try
        {
            bsGo.AddComponent<GameBootstrap>(); // dựng GameManager + UI như lúc Play
            gm = GameManager.Instance;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[DIAG] Khởi động game CRASH → " + e);
        }

        int okCount = 0;
        if (gm != null && gm.UI != null)
        {
            for (int i = 0; i < MazeDatabase.Levels.Length; i++)
            {
                try
                {
                    gm.StartLevel(i);
                    if (gm.Player != null && gm.Cam != null)
                    {
                        okCount++;
                        Debug.Log("[DIAG] MÀN " + (i + 1) + ": VÀO ĐƯỢC ✓ (nhân vật + camera đã dựng)");
                    }
                    else
                        Debug.LogError("[DIAG] MÀN " + (i + 1) + ": StartLevel chạy xong nhưng THIẾU Player/Cam!");
                }
                catch (System.Exception e)
                {
                    Debug.LogError("[DIAG] MÀN " + (i + 1) + ": CRASH → " + e);
                }
            }
        }
        else
            Debug.LogError("[DIAG] Không tạo được GameManager/UI — xem lỗi đỏ phía trên.");

        if (okCount == MazeDatabase.Levels.Length)
            Debug.Log("[DIAG] KẾT LUẬN: cả 3 màn đều vào được ✓ → lỗi click nằm ở phía Editor " +
                      "(khung Game Scale ≠ 1, hoặc Game view chưa có focus).");
        else
            Debug.Log("[DIAG] KẾT LUẬN: có màn bị lỗi — gửi lại các dòng [DIAG] đỏ cho trợ lý.");

        // ---------- Dọn dẹp sạch sẽ, để lại scene như cũ ----------
        foreach (Canvas c in Object.FindObjectsOfType<Canvas>(true))
            Object.DestroyImmediate(c.gameObject);
        foreach (EventSystem es in Object.FindObjectsOfType<EventSystem>(true))
            Object.DestroyImmediate(es.gameObject);
        UIBuilder ui = Object.FindObjectOfType<UIBuilder>();
        if (ui != null) Object.DestroyImmediate(ui.gameObject);
        foreach (Transform t in Object.FindObjectsOfType<Transform>(true))
            if (t.parent == null && (t.name.StartsWith("Level_") || t.name == "BackgroundMusic"))
                Object.DestroyImmediate(t.gameObject);
        if (gm != null) Object.DestroyImmediate(gm.gameObject);
        Object.DestroyImmediate(bsGo);
        Debug.Log("[DIAG] Đã dọn dẹp scene — mọi thứ trở lại như cũ.");
    }

    [MenuItem("Tools/MAZE ADVENTURE 3D/Đóng gói Windows (Build .exe)")]
    public static void BuildWindowsExe()
    {
        var scenes = new[] { "Assets/Scenes/Main.unity" };
        var report = BuildPipeline.BuildPlayer(scenes, "Build/MazeAdventure3D.exe",
            BuildTarget.StandaloneWindows64, BuildOptions.None);
        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log("[BUILD] THÀNH CÔNG! File game: " +
                      System.IO.Path.GetFullPath("Build/MazeAdventure3D.exe"));
        else
            Debug.LogError("[BUILD] THẤT BẠI: " + report.summary.result);
    }

    [MenuItem("Tools/MAZE ADVENTURE 3D/Xuất ảnh bản đồ mê cung (báo cáo)")]
    public static void ExportMazeImages()
    {
        string dir = @"C:\Users\Admin\Desktop\MazeGame3D\Report_Images";
        Directory.CreateDirectory(dir);
        Color wallC = new Color(0.13f, 0.13f, 0.18f);
        Color floorC = new Color(0.93f, 0.91f, 0.84f);
        Color secretC = new Color(0.55f, 0.78f, 0.62f);

        for (int li = 0; li < MazeDatabase.Levels.Length; li++)
        {
            LevelDef def = MazeDatabase.Levels[li];
            GeneratedMaze m = MazeGenerator.Generate(def);
            int cell = 14;
            int size = (m.size + 2) * cell;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, wallC);

            // Vẽ 1 ô lưới màu c
            void Paint(int gx, int gz, Color c)
            {
                for (int y = 0; y < cell - 1; y++)
                    for (int x = 0; x < cell - 1; x++)
                        tex.SetPixel((gx + 1) * cell + x, size - ((gz + 1) * cell + y) - 1, c);
            }

            for (int z = 0; z < m.size; z++)
                for (int x = 0; x < m.size; x++)
                {
                    if (m.wall[x, z]) continue;
                    Paint(x, z, m.secretWall[x, z] ? secretC : floorC);
                }
            foreach (Vector2Int k in m.keys) Paint(k.x, k.y, new Color(1f, 0.78f, 0.1f));
            foreach (Vector2Int c in m.gems) Paint(c.x, c.y, new Color(0.75f, 0.45f, 1f));
            Paint(m.start.x, m.start.y, new Color(0.2f, 0.55f, 1f));
            Paint(m.exit.x, m.exit.y, new Color(0.9f, 0.2f, 0.15f));
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            File.WriteAllBytes(dir + "\\map_man" + (li + 1) + ".png", tex.EncodeToPNG());
        }
        Debug.Log("[MAZE] Đã xuất 3 ảnh bản đồ mê cung vào " + dir);
    }

    // ================= CHẨN ĐOÁN TRONG PLAY MODE (thật) =================
    // Tự vào Play mode, chạy StartLevel(0) rồi chơi thử 10 frame gameplay
    // để bắt CHÍNH XÁC exception xảy ra lúc runtime (nếu có).

    static bool diagStartedLevel;
    static int diagStartFrame;

    [MenuItem("Tools/MAZE ADVENTURE 3D/Chẩn đoán Play mode (batch)")]
    public static void EnterPlayAndTest()
    {
        SessionState.SetBool("diag_done", false);
        diagStartedLevel = false;
        EditorApplication.update += PlayDiagTick;
        EditorApplication.isPlaying = true;
    }

    static void PlayDiagTick()
    {
        if (SessionState.GetBool("diag_done", false)) return;
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;

        int frame = Time.frameCount;
        GameManager gm = GameManager.Instance;

        if (!diagStartedLevel)
        {
            if (frame < 5) return; // chờ vài frame cho menu ổn định
            if (gm == null || gm.UI == null)
            {
                Debug.LogError("[DIAG] PLAY MODE: GameManager/UI KHÔNG tồn tại!");
                FinishDiag(false);
                return;
            }
            Debug.Log("[DIAG] PLAY MODE: menu đã sống ✓ — gọi StartLevel(0)...");
            try
            {
                gm.StartLevel(0);
                diagStartedLevel = true;
                diagStartFrame = frame;
                Debug.Log("[DIAG] StartLevel(0) trả về bình thường. Player=" +
                          (gm.Player != null) + " Cam=" + (gm.Cam != null) +
                          " State=" + gm.State);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[DIAG] StartLevel(0) CRASH → " + e);
                FinishDiag(false);
            }
            return;
        }

        // Đã vào màn — chạy thêm 10 frame gameplay để mọi Update() chạy thật
        if (frame < diagStartFrame + 10) return;

        bool alive = gm != null && gm.State == GameState.Playing && gm.Player != null;
        Debug.Log(alive
            ? "[DIAG] Sau 10 frame gameplay: State=" + gm.State + " ✓ KHÔNG crash"
            : "[DIAG] Sau 10 frame gameplay: State=" +
              (gm != null ? gm.State.ToString() : "null") + " — BẤT THƯỜNG (xem lỗi đỏ phía trên)");
        FinishDiag(alive);
    }

    static void FinishDiag(bool ok)
    {
        SessionState.SetBool("diag_done", true);
        EditorApplication.update -= PlayDiagTick;
        EditorApplication.isPlaying = false;
        Debug.Log("[DIAG] KẾT THÚC — kết quả: " + (ok ? "TỐT ✓" : "CÓ LỖI (xem log)"));
        EditorApplication.Exit(ok ? 0 : 2);
    }

    /// <summary>BFS trên mê cung: ô đi được = sàn (cho phép xuyên tường giả khi allowSecret).</summary>
    static bool Reachable(GeneratedMaze m, Vector2Int from, Vector2Int to, bool allowSecret)
    {
        int n = m.size;
        var visited = new bool[n, n];
        var queue = new Queue<Vector2Int>();
        bool Passable(int x, int z)
            => !m.wall[x, z] && (allowSecret || !m.secretWall[x, z]);

        visited[from.x, from.y] = true;
        queue.Enqueue(from);
        int[] dx = { 1, -1, 0, 0 };
        int[] dz = { 0, 0, 1, -1 };

        while (queue.Count > 0)
        {
            Vector2Int c = queue.Dequeue();
            if (c == to) return true;
            for (int i = 0; i < 4; i++)
            {
                int nx = c.x + dx[i], nz = c.y + dz[i];
                if (nx >= 0 && nx < n && nz >= 0 && nz < n &&
                    !visited[nx, nz] && Passable(nx, nz))
                {
                    visited[nx, nz] = true;
                    queue.Enqueue(new Vector2Int(nx, nz));
                }
            }
        }
        return false;
    }

    static int CountFloor(GeneratedMaze m)
    {
        int count = 0;
        for (int x = 0; x < m.size; x++)
            for (int z = 0; z < m.size; z++)
                if (!m.wall[x, z]) count++;
        return count;
    }
}
#endif
