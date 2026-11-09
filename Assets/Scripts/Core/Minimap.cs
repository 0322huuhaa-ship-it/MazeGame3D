using UnityEngine;
using UnityEngine.UI;

// ============================================================================
//  MAZE ADVENTURE 3D — Bản đồ mini toàn mê cung (góc phải trên màn hình)
//  • Hiện sơ đồ: tường, sàn, cổng thoát (đỏ), chìa khóa (vàng), điểm xuất
//    phát (xanh dương)
//  • Mũi tên trắng chạy theo nhân vật, xoay theo hướng nhìn
//  • Nhấn M để ẩn / hiện bản đồ
//  • Hốc bí mật được vẽ NHƯ TƯỜNG THƯỜNG — phải tự khám phá!
//  Bản đồ sống cùng Level (bị dọn khi đổi màn), tự tạo canvas riêng.
// ============================================================================

public class Minimap : MonoBehaviour
{
    const float MapSize = 170f; // kích thước bản đồ trên màn hình (px) — đổi số này để chỉnh to/nhỏ

    GeneratedMaze maze;
    RectTransform mapArea;
    RectTransform playerDot;
    Texture2D mapTex;
    float cellPx;
    Vector2 worldMin; // tọa độ world của ô lưới (0,0)

    public void Init(GeneratedMaze m, LevelDef def, Transform player)
    {
        maze = m;
        int n = m.size;
        cellPx = MapSize / n;
        worldMin = new Vector2(-(n - 1f), -(n - 1f)); // World(c) = c*2 - (n-1)

        // Canvas riêng, vẽ DƯỚI canvas chính để HUD/pause/win đè lên bản đồ
        var canvasGo = new GameObject("MinimapCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -1;
        canvasGo.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        // Khung bản đồ
        var frame = new GameObject("MapFrame", typeof(RectTransform), typeof(Image));
        mapArea = (RectTransform)frame.transform;
        mapArea.SetParent(canvasGo.transform, false);
        mapArea.anchorMin = new Vector2(1f, 1f);
        mapArea.anchorMax = new Vector2(1f, 1f);
        mapArea.pivot = new Vector2(1f, 1f);
        mapArea.anchoredPosition = new Vector2(-16f, -16f); // sát góc phải trên
        mapArea.sizeDelta = new Vector2(MapSize + 12f, MapSize + 12f);
        Image frameImg = frame.GetComponent<Image>();
        frameImg.color = new Color(0f, 0f, 0f, 0.55f);
        frameImg.raycastTarget = false;

        // Ảnh sơ đồ mê cung (vẽ 1 lần lúc mở màn)
        mapTex = BuildMapTexture(m);
        var rawGo = new GameObject("MapImage", typeof(RectTransform), typeof(RawImage));
        RectTransform rawRt = (RectTransform)rawGo.transform;
        rawRt.SetParent(mapArea, false);
        rawRt.anchorMin = new Vector2(0.5f, 0.5f);
        rawRt.anchorMax = new Vector2(0.5f, 0.5f);
        rawRt.sizeDelta = new Vector2(MapSize, MapSize);
        RawImage raw = rawGo.GetComponent<RawImage>();
        raw.texture = mapTex;
        raw.raycastTarget = false;

        // Mũi tên trắng theo nhân vật
        var dotGo = new GameObject("PlayerDot", typeof(RectTransform), typeof(Image));
        playerDot = (RectTransform)dotGo.transform;
        playerDot.SetParent(mapArea, false);
        playerDot.sizeDelta = new Vector2(Mathf.Max(10f, cellPx * 1.6f), Mathf.Max(10f, cellPx * 1.6f));
        Image dotImg = dotGo.GetComponent<Image>();
        dotImg.sprite = UIBuilder.Circle;
        dotImg.color = Color.white;
        dotImg.raycastTarget = false;
    }

    /// <summary>Vẽ sơ đồ mê cung: 1 pixel = 1 ô lưới.</summary>
    Texture2D BuildMapTexture(GeneratedMaze m)
    {
        int n = m.size;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color wall = new Color(0.12f, 0.12f, 0.16f, 0.95f);
        Color floor = new Color(0.85f, 0.83f, 0.75f, 0.95f);
        Color exitC = new Color(1f, 0.25f, 0.15f);   // cổng thoát
        Color keyC = new Color(1f, 0.85f, 0.1f);     // chìa khóa
        Color startC = new Color(0.2f, 0.55f, 1f);   // xuất phát

        for (int x = 0; x < n; x++)
            for (int z = 0; z < n; z++)
            {
                Color c = !m.wall[x, z] ? floor : wall;
                Vector2Int cell = new Vector2Int(x, z);
                if (cell == m.exit) c = exitC;
                else if (m.keys.Contains(cell)) c = keyC;
                else if (cell == m.start) c = startC;
                // đảo trục z để hướng LÊN trên bản đồ trùng hướng +Z trong game
                tex.SetPixel(x, n - 1 - z, c);
            }
        tex.Apply();
        return tex;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        // M: ẩn / hiện bản đồ
        if (Input.GetKeyDown(KeyCode.M) && mapArea != null)
            mapArea.gameObject.SetActive(!mapArea.gameObject.activeSelf);

        if (gm.State != GameState.Playing || gm.Player == null || playerDot == null) return;

        // Chuyển tọa độ world -> tọa độ trên bản đồ
        Vector3 p = gm.Player.transform.position;
        float fx = (p.x - worldMin.x) / 2f + 0.5f; // tâm ô theo trục X
        float fz = (p.z - worldMin.y) / 2f + 0.5f; // tâm ô theo trục Z
        float px = (fx / maze.size - 0.5f) * MapSize;
        float pz = (0.5f - fz / maze.size) * MapSize; // đảo trục z như texture
        playerDot.anchoredPosition = new Vector2(px, pz);

        // Xoay mũi tên theo hướng nhìn của nhân vật
        float yaw = gm.Player.transform.eulerAngles.y;
        playerDot.localRotation = Quaternion.Euler(0f, 0f, -yaw);
    }

    void OnDestroy()
    {
        if (mapTex != null) Destroy(mapTex);
    }
}
