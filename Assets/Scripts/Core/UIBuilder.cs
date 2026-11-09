using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ============================================================================
//  MAZE ADVENTURE 3D — Bộ dựng giao diện người dùng (uGUI)
//  Toàn bộ màn hình UI được tạo bằng code (không cần Canvas prefab):
//    • Màn hình chính: START GAME / QUIT
//    • Màn chọn màn chơi (hiện kỷ lục từng màn)
//    • HUD khi chơi: xu, chìa khóa, mạng (tim), đồng hồ, điểm, nút PAUSE
//    • Màn tạm dừng: TIẾP TỤC / CHƠI LẠI / MENU CHÍNH
//    • Màn chiến thắng (YOU WIN) & thất bại (GAME OVER)
//    • Thông báo nhanh (toast) + hiệu ứng nháy đỏ khi bị thương
// ============================================================================

public class UIBuilder : MonoBehaviour
{
    GameManager gm;

    // Các màn hình
    RectTransform menuRoot, selectRoot, shopRoot, hudRoot, pauseRoot, winRoot, loseRoot;
    readonly List<RectTransform> allScreens = new List<RectTransform>();

    // Cửa hàng trang phục
    Text shopWalletText;
    Text[] shopStatusTexts;
    RawImage[] shopPreviewImages;
    static readonly Dictionary<string, Texture2D> costumePreviews = new Dictionary<string, Texture2D>();

    // Các thành phần HUD
    Text coinsText, keysText, timerText, scoreText, levelNameText;
    readonly Image[] hearts = new Image[3];

    // Màn thắng / thua
    Text winTitleText, winStatsText, loseReasonText;
    Button nextLevelButton;
    Text[] levelBestTexts;

    // Thông báo & hiệu ứng
    Text toastText;
    Image damageOverlay;
    float toastTimer;
    float damageAlpha;

    // ---------------- Tiện ích tĩnh dùng chung ----------------
    static Font uiFont;
    public static Font DefaultFont
    {
        get
        {
            if (uiFont == null)
            {
                try { uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
                if (uiFont == null)
                    try { uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
            }
            return uiFont;
        }
    }

    static Sprite circle;
    public static Sprite Circle
    {
        get
        {
            if (circle == null)
            {
                var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                for (int y = 0; y < 32; y++)
                    for (int x = 0; x < 32; x++)
                    {
                        float d = Mathf.Sqrt((x - 15.5f) * (x - 15.5f) + (y - 15.5f) * (y - 15.5f));
                        tex.SetPixel(x, y, d <= 14.5f ? Color.white : Color.clear);
                    }
                tex.Apply();
                circle = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
                circle.name = "UICircle";
            }
            return circle;
        }
    }

    // =====================================================================
    //  KHỞI TẠO
    // =====================================================================
    public void Build(GameManager manager)
    {
        gm = manager;

        CreateCanvas();
        CreateEventSystem();

        BuildMenuScreen();
        BuildSelectScreen();
        BuildShopScreen();
        BuildHUD();
        BuildPauseScreen();
        BuildWinScreen();
        BuildLoseScreen();

        ShowMainMenu(); // mở màn hình chính đầu tiên
    }

    void CreateCanvas()
    {
        var go = new GameObject("UICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // LUÔN trên cùng — không canvas nào khác che được

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        DontDestroyOnLoad(go); // UI sống sót qua mọi cảnh
    }

    void CreateEventSystem()
    {
        EnsureEventSystem();
    }

    /// <summary>Bảo đảm luôn có EventSystem sống (nếu bị mất → tự tạo lại ngay).</summary>
    static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(go);
        Debug.Log("[MAZE] Đã tự tạo lại EventSystem (tự phục hồi).");
    }

    // =====================================================================
    //  HÀM DỰNG PHẦN TỬ GIAO DIỆN
    // =====================================================================

    /// <summary>Tạo một màn hình (panel toàn trang, ẩn mặc định).</summary>
    RectTransform MakeScreen(string name, Color bg)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rt = Stretch(go.GetComponent<RectTransform>());
        go.GetComponent<Image>().color = bg;
        rt.SetParent(FindObjectOfType<Canvas>().transform, false);
        allScreens.Add(rt);
        return rt;
    }

    static RectTransform Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    /// <summary>Tạo ô chữ. anchor = điểm neo (0.5,0.5 là giữa màn hình).</summary>
    Text MakeText(Transform parent, string content, int fontSize, Color color,
                  Vector2 anchor, Vector2 anchoredPos, Vector2 size,
                  FontStyle style = FontStyle.Normal, bool outline = false)
    {
        var go = new GameObject("Text_" + content, typeof(RectTransform), typeof(Text));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var txt = go.GetComponent<Text>();
        txt.font = DefaultFont;
        txt.text = content;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.color = color;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.raycastTarget = false; // chữ trang trí — KHÔNG BAO GIỜ chặn chuột

        if (outline)
        {
            var o = go.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.9f);
            o.effectDistance = new Vector2(2.5f, -2.5f);
        }
        return txt;
    }

    /// <summary>Tạo nút bấm, tự phát âm click và gọi hành động.</summary>
    Button MakeButton(Transform parent, string label, Vector2 anchoredPos, Vector2 size,
                      Color bg, UnityAction action, int fontSize = 42,
                      Vector2? anchor = null)
    {
        Vector2 a = anchor ?? new Vector2(0.5f, 0.5f);

        var go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = a; rt.anchorMax = a; rt.pivot = a;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var img = go.GetComponent<Image>();
        img.color = bg;

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.fadeDuration = 0.08f;
        btn.colors = colors;
        btn.onClick.AddListener(() =>
        {
            Debug.Log("[MAZE] Đã bấm nút: " + label);
            if (gm != null) gm.PlaySfx(ProceduralAudio.Click, 0.6f);
            action?.Invoke();
        });

        // Nhãn nút
        var labelObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        var lrt = (RectTransform)labelObj.transform;
        lrt.SetParent(go.transform, false);
        Stretch(lrt);
        var txt = labelObj.GetComponent<Text>();
        txt.font = DefaultFont;
        txt.text = label;
        txt.fontSize = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.raycastTarget = false; // nhãn không chặn tia chuột — click đi thẳng vào nút

        return btn;
    }

    /// <summary>Biểu tượng tròn nhỏ (đồng xu / tim).</summary>
    Image MakeIcon(Transform parent, Color color, Vector2 anchor, Vector2 pos, float size)
    {
        var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(size, size);
        var img = go.GetComponent<Image>();
        img.sprite = Circle;
        img.color = color;
        return img;
    }

    // =====================================================================
    //  CÁC MÀN HÌNH
    // =====================================================================

    void BuildMenuScreen()
    {
        menuRoot = MakeScreen("MainMenu", new Color(0.09f, 0.06f, 0.20f, 0.98f));

        // NỀN DỰ PHÒNG: bấm vào BẤT KỲ vùng trống nào cũng mở màn chọn màn chơi
        // (đặt đầu tiên = nằm dưới cùng; nút chính vẫn đè lên và thắng click)
        MakeButton(menuRoot, "", new Vector2(0f, 0f), new Vector2(4000f, 4000f),
                   new Color(0f, 0f, 0f, 0f), () => gm.OpenLevelSelect(), 1);

        MakeText(menuRoot, "MAZE ADVENTURE 3D", 120, new Color(1f, 0.84f, 0.25f),
                 new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(1700f, 200f),
                 FontStyle.Bold, true);
        MakeText(menuRoot, "Phiêu lưu vượt mê cung 3D", 46, Color.white,
                 new Vector2(0.5f, 0.5f), new Vector2(0f, 130f), new Vector2(1200f, 80f));

        MakeButton(menuRoot, "START GAME", new Vector2(0f, -10f), new Vector2(500f, 110f),
                   new Color(0.18f, 0.65f, 0.28f), () => gm.OpenLevelSelect(), 48);
        MakeButton(menuRoot, "CỬA HÀNG TRANG PHỤC", new Vector2(0f, -140f), new Vector2(500f, 100f),
                   new Color(0.72f, 0.38f, 0.72f), () => gm.OpenShop(), 40);
        MakeButton(menuRoot, "QUIT — THOÁT", new Vector2(0f, -260f), new Vector2(500f, 100f),
                   new Color(0.45f, 0.45f, 0.5f), () => gm.QuitGame(), 40);

        MakeText(menuRoot,
            "Di chuyển: WASD / Phím mũi tên / NÚT MÀN HÌNH (góc trái dưới)    •    Chạy: SHIFT    •    Nhảy: SPACE\n" +
            "Xoay camera: GIỮ CHUỘT PHẢI + di chuột    •    Đổi góc nhìn: C    •    Bản đồ mini: M    •    Tạm dừng: ESC\n" +
            "Mũi tên vàng trên đầu: chỉ đường tới chìa khóa / cổng thoát — mọi nút trên màn hình đều bấm được bằng chuột",
            30, new Color(1f, 1f, 1f, 0.75f),
            new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1800f, 160f));
    }

    void BuildSelectScreen()
    {
        selectRoot = MakeScreen("LevelSelect", new Color(0.07f, 0.09f, 0.16f, 0.98f));

        // NỀN DỰ PHÒNG: bấm vào BẤT KỲ vùng trống nào cũng vào MÀN 1
        // (đặt đầu tiên = nằm dưới cùng; 3 nút màn + QUAY LẠI đè lên nên vẫn nhận click đúng)
        MakeButton(selectRoot, "", new Vector2(0f, 0f), new Vector2(4000f, 4000f),
                   new Color(0f, 0f, 0f, 0f), () => gm.StartLevel(0), 1);

        MakeText(selectRoot, "CHỌN MÀN CHƠI", 80, Color.white,
                 new Vector2(0.5f, 0.5f), new Vector2(0f, 350f), new Vector2(1200f, 120f),
                 FontStyle.Bold, true);

        levelBestTexts = new Text[MazeDatabase.Levels.Length];
        for (int i = 0; i < MazeDatabase.Levels.Length; i++)
        {
            LevelDef def = MazeDatabase.Levels[i];
            int idx = i; // bắt buộc biến cục bộ cho lambda
            string diff = i == 0 ? "Dễ" : (i == 1 ? "Trung bình" : "Khó");
            Button b = MakeButton(selectRoot,
                def.name + " — " + def.subtitle + "   (" + diff + ")",
                new Vector2(0f, 180f - i * 170f), new Vector2(1000f, 140f),
                new Color(0.16f, 0.45f, 0.75f), () => gm.StartLevel(idx), 44);

            // Dòng phụ: kỷ lục
            var sub = new GameObject("Best", typeof(RectTransform), typeof(Text));
            var srt = (RectTransform)sub.transform;
            srt.SetParent(b.transform, false);
            srt.anchorMin = new Vector2(0.5f, 0f); srt.anchorMax = new Vector2(0.5f, 0f);
            srt.pivot = new Vector2(0.5f, 0f);
            srt.anchoredPosition = new Vector2(0f, 6f);
            srt.sizeDelta = new Vector2(880f, 30f);
            var st = sub.GetComponent<Text>();
            st.font = DefaultFont;
            st.fontSize = 26;
            st.color = new Color(1f, 1f, 1f, 0.8f);
            st.alignment = TextAnchor.LowerCenter;
            levelBestTexts[i] = st;
        }

        MakeButton(selectRoot, "QUAY LẠI", new Vector2(0f, -400f), new Vector2(400f, 95f),
                   new Color(0.45f, 0.45f, 0.5f), () => gm.EnterMenu(), 38);

        MakeText(selectRoot, "Bấm nút màn chơi • hoặc nhấn 1/2/3 • hoặc bấm vào VÙNG TRỐNG bất kỳ để vào MÀN 1",
                 32, new Color(1f, 0.9f, 0.5f),
                 new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(1500f, 60f),
                 FontStyle.Bold, true);
    }

    // =====================================================================
    //  CỬA HÀNG TRANG PHỤC
    // =====================================================================

    void BuildShopScreen()
    {
        shopRoot = MakeScreen("Shop", new Color(0.1f, 0.05f, 0.18f, 0.98f));

        MakeText(shopRoot, "CỬA HÀNG TRANG PHỤC", 78, new Color(1f, 0.75f, 1f),
                 new Vector2(0.5f, 0.5f), new Vector2(0f, 350f), new Vector2(1300f, 110f),
                 FontStyle.Bold, true);

        shopWalletText = MakeText(shopRoot, "Ví của bạn: 0 xu", 44, new Color(1f, 0.85f, 0.3f),
                 new Vector2(0.5f, 0.5f), new Vector2(0f, 262f), new Vector2(900f, 70f),
                 FontStyle.Bold, true);

        shopStatusTexts = new Text[CostumeDatabase.All.Length];
        shopPreviewImages = new RawImage[CostumeDatabase.All.Length];
        for (int i = 0; i < CostumeDatabase.All.Length; i++)
        {
            CostumeDef c = CostumeDatabase.All[i];
            int idx = i;

            Button row = MakeButton(shopRoot, c.name,
                new Vector2(0f, 165f - i * 95f), new Vector2(1150f, 80f),
                new Color(0.18f, 0.3f, 0.5f), () => OnShopRow(idx), 38);

            // KHUNG ẢNH MINH HỌA: nhân vật 3D thật mặc trang phục (chụp khi mở cửa hàng)
            var pv = new GameObject("Preview", typeof(RectTransform), typeof(RawImage));
            var prt = (RectTransform)pv.transform;
            prt.SetParent(row.transform, false);
            prt.anchorMin = new Vector2(0f, 0.5f); prt.anchorMax = new Vector2(0f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = new Vector2(80f, 0f);
            prt.sizeDelta = new Vector2(92f, 92f);
            shopPreviewImages[i] = pv.GetComponent<RawImage>();

            // Dòng trạng thái bên phải: đang mặc / đã sở hữu / giá
            var st = new GameObject("Status", typeof(RectTransform), typeof(Text));
            var srt = (RectTransform)st.transform;
            srt.SetParent(row.transform, false);
            srt.anchorMin = new Vector2(1f, 0.5f); srt.anchorMax = new Vector2(1f, 0.5f);
            srt.pivot = new Vector2(1f, 0.5f);
            srt.anchoredPosition = new Vector2(-28f, 0f);
            srt.sizeDelta = new Vector2(460f, 70f);
            var t = st.GetComponent<Text>();
            t.font = DefaultFont;
            t.fontSize = 30;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleRight;
            t.raycastTarget = false;
            shopStatusTexts[i] = t;
        }

        MakeButton(shopRoot, "QUAY LẠI", new Vector2(0f, -420f), new Vector2(400f, 90f),
                   new Color(0.45f, 0.45f, 0.5f), () => gm.EnterMenu(), 36);

        MakeText(shopRoot, "Kiếm xu bằng cách thắng các màn chơi — xu nhặt được sẽ nạp vào ví để mua trang phục!",
                 30, new Color(1f, 1f, 1f, 0.7f),
                 new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1700f, 60f));
    }

    /// <summary>Bấm một dòng trang phục: mặc nếu đã sở hữu, mua nếu đủ xu.</summary>
    void OnShopRow(int i)
    {
        CostumeDef c = CostumeDatabase.All[i];
        if (CostumeDatabase.IsOwned(c))
        {
            CostumeDatabase.EquippedId = c.id;
            gm.PlaySfx(ProceduralAudio.Key, 0.8f);
            gm.ShowToast("Đã mặc: " + c.name);
        }
        else if (GameManager.Wallet >= c.price)
        {
            GameManager.Wallet -= c.price;
            CostumeDatabase.SetOwned(c);
            CostumeDatabase.EquippedId = c.id;
            gm.PlaySfx(ProceduralAudio.Win, 0.8f);
            GameFX.Burst(Camera.main != null ? Camera.main.transform.position + Camera.main.transform.forward * 2f
                         : Vector3.zero, new Color(1f, 0.8f, 0.3f), 30, 0.1f, 3f);
            gm.ShowToast("Đã mua và mặc: " + c.name + "!");
        }
        else
        {
            gm.PlaySfx(ProceduralAudio.Click, 0.5f);
            gm.ShowToast("Thiếu " + (c.price - GameManager.Wallet) + " xu — hãy chơi kiếm xu nhé!");
        }
        RefreshShop();
    }

    /// <summary>Làm mới dòng tiền + trạng thái từng trang phục.</summary>
    void RefreshShop()
    {
        if (shopWalletText != null)
            shopWalletText.text = "Ví của bạn: " + GameManager.Wallet + " xu";

        for (int i = 0; i < CostumeDatabase.All.Length; i++)
        {
            if (shopStatusTexts[i] == null) continue;
            CostumeDef c = CostumeDatabase.All[i];
            if (CostumeDatabase.EquippedId == c.id)
            {
                shopStatusTexts[i].text = "✓ ĐANG MẶC";
                shopStatusTexts[i].color = new Color(0.4f, 1f, 0.5f);
            }
            else if (CostumeDatabase.IsOwned(c))
            {
                shopStatusTexts[i].text = "Đã sở hữu — bấm để mặc";
                shopStatusTexts[i].color = new Color(0.7f, 0.9f, 1f);
            }
            else if (GameManager.Wallet >= c.price)
            {
                shopStatusTexts[i].text = "MUA: " + c.price + " xu";
                shopStatusTexts[i].color = new Color(1f, 0.85f, 0.4f);
            }
            else
            {
                shopStatusTexts[i].text = "Khóa — cần " + c.price + " xu (thiếu " +
                                          (c.price - GameManager.Wallet) + ")";
                shopStatusTexts[i].color = new Color(1f, 1f, 1f, 0.55f);
            }
        }
    }

    public void ShowShop()
    {
        // Chụp ảnh minh họa từng trang phục (làm 1 lần rồi dùng lại)
        if (shopPreviewImages != null)
            for (int i = 0; i < CostumeDatabase.All.Length; i++)
                if (shopPreviewImages[i] != null)
                    shopPreviewImages[i].texture = GetCostumePreview(CostumeDatabase.All[i]);

        RefreshShop();
        ShowOnly(shopRoot);
    }

    /// <summary>
    /// Vẽ ảnh minh họa nhân vật chibi mặc trang phục — vẽ 2D bằng pixel nên
    /// LUÔN đúng màu (không phụ thuộc ánh sáng hay camera như render 3D).
    /// </summary>
    Texture2D GetCostumePreview(CostumeDef c)
    {
        if (costumePreviews.TryGetValue(c.id, out Texture2D cached) && cached != null)
            return cached;

        const int S = 128;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
                tex.SetPixel(x, y, clear);

        Color skin  = new Color(1f, 0.88f, 0.78f);
        Color eye   = new Color(0.15f, 0.11f, 0.09f);
        Color cheek = new Color(1f, 0.55f, 0.6f);
        Color white = Color.white;

        // Chân + giày
        FillEllipse(tex, 56f, 18f, 5f, 10f, c.pants);
        FillEllipse(tex, 72f, 18f, 5f, 10f, c.pants);
        FillEllipse(tex, 56f, 7f, 7f, 4f, c.shoes);
        FillEllipse(tex, 72f, 7f, 7f, 4f, c.shoes);

        // Thân + tay + bàn tay
        FillEllipse(tex, 64f, 42f, 16f, 16f, c.shirt);
        FillEllipse(tex, 47f, 44f, 5f, 12f, c.shirt);
        FillEllipse(tex, 81f, 44f, 5f, 12f, c.shirt);
        FillEllipse(tex, 47f, 31f, 4.5f, 4.5f, skin);
        FillEllipse(tex, 81f, 31f, 4.5f, 4.5f, skin);

        // Khăn quàng
        if (c.scarfOn)
        {
            FillEllipse(tex, 64f, 59f, 14f, 5f, c.scarf);
            FillEllipse(tex, 64f, 51f, 4f, 7f, c.scarf);
        }

        // Tóc sau + đuôi sam / búi tóc
        if (c.hairStyle == HairStyle.TwinTails)
        {
            FillEllipse(tex, 33f, 64f, 8f, 15f, c.hair);
            FillEllipse(tex, 95f, 64f, 8f, 15f, c.hair);
            FillEllipse(tex, 32f, 47f, 10f, 11f, c.hair);
            FillEllipse(tex, 96f, 47f, 10f, 11f, c.hair);
        }
        else if (c.hairStyle == HairStyle.Bun)
        {
            FillEllipse(tex, 64f, 106f, 11f, 11f, c.hair);
        }
        FillEllipse(tex, 64f, 82f, 29f, 27f, c.hair);

        // Đầu
        FillEllipse(tex, 64f, 78f, 26f, 24f, skin);

        // Tóc mái phủ trán
        FillEllipse(tex, 64f, 93f, 27f, 13f, c.hair);
        FillEllipse(tex, 64f, 87f, 26f, 8f, c.hair);

        // Nơ tóc
        if (c.hairStyle == HairStyle.TwinTails)
        {
            FillEllipse(tex, 39f, 79f, 4f, 4f, c.tie);
            FillEllipse(tex, 89f, 79f, 4f, 4f, c.tie);
        }

        // Mắt to + điểm sáng + má hồng
        FillEllipse(tex, 55f, 74f, 3.5f, 4.5f, eye);
        FillEllipse(tex, 73f, 74f, 3.5f, 4.5f, eye);
        FillEllipse(tex, 54f, 76f, 1.5f, 2f, white);
        FillEllipse(tex, 72f, 76f, 1.5f, 2f, white);
        FillEllipse(tex, 48f, 69f, 4f, 2.5f, cheek);
        FillEllipse(tex, 80f, 69f, 4f, 2.5f, cheek);

        tex.Apply();
        costumePreviews[c.id] = tex;
        return tex;
    }

    /// <summary>Tô một hình elip đặc lên texture (tiện vẽ nhân vật).</summary>
    static void FillEllipse(Texture2D tex, float cx, float cy, float rx, float ry, Color col)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx));
        int x1 = Mathf.Min(tex.width - 1, Mathf.CeilToInt(cx + rx));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry));
        int y1 = Mathf.Min(tex.height - 1, Mathf.CeilToInt(cy + ry));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = (x - cx) / rx, dy = (y - cy) / ry;
                if (dx * dx + dy * dy <= 1f)
                    tex.SetPixel(x, y, col);
            }
    }

    void BuildHUD()
    {
        // HUD không phải panel khóa toàn màn hình — vẫn thêm vào allScreens để bật/tắt cùng lúc
        var go = new GameObject("HUD", typeof(RectTransform));
        hudRoot = Stretch((RectTransform)go.transform);
        hudRoot.SetParent(FindObjectOfType<Canvas>().transform, false);
        allScreens.Add(hudRoot);

        // ---- Bảng thông tin góc trên trái ----
        var panel = new GameObject("InfoPanel", typeof(RectTransform), typeof(Image));
        var prt = (RectTransform)panel.transform;
        prt.SetParent(hudRoot, false);
        prt.anchorMin = new Vector2(0f, 1f); prt.anchorMax = new Vector2(0f, 1f);
        prt.pivot = new Vector2(0f, 1f);
        prt.anchoredPosition = new Vector2(16f, -16f);
        prt.sizeDelta = new Vector2(680f, 118f);
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);

        MakeIcon(panel.transform, new Color(1f, 0.8f, 0.15f), new Vector2(0f, 0.5f), new Vector2(30f, 0f), 44f);
        coinsText = MakeText(panel.transform, "0/0", 40, Color.white,
                             new Vector2(0f, 0.5f), new Vector2(105f, 0f), new Vector2(130f, 60f), FontStyle.Bold, true);

        MakeIcon(panel.transform, new Color(1f, 0.9f, 0.45f), new Vector2(0f, 0.5f), new Vector2(250f, 0f), 44f);
        keysText = MakeText(panel.transform, "0/0", 40, Color.white,
                            new Vector2(0f, 0.5f), new Vector2(325f, 0f), new Vector2(170f, 60f), FontStyle.Bold, true);

        // 3 tim (mạng)
        for (int i = 0; i < 3; i++)
            hearts[i] = MakeIcon(panel.transform, new Color(0.95f, 0.2f, 0.25f),
                                 new Vector2(0f, 0.5f), new Vector2(470f + i * 62f, 0f), 48f);

        // ---- Đồng hồ giữa trên ----
        timerText = MakeText(hudRoot, "00:00", 64, Color.white,
                             new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(400f, 100f),
                             FontStyle.Bold, true);

        // ---- Điểm (bên trái bản đồ) + nút PAUSE góc dưới phải ----
        scoreText = MakeText(hudRoot, "ĐIỂM: 0", 42, Color.white,
                             new Vector2(1f, 1f), new Vector2(-215f, -45f), new Vector2(350f, 60f),
                             FontStyle.Bold, true);
        scoreText.alignment = TextAnchor.MiddleRight;
        MakeButton(hudRoot, "PAUSE", new Vector2(-16f, 16f), new Vector2(190f, 74f),
                   new Color(0.85f, 0.55f, 0.1f), () => gm.PauseGame(), 34,
                   new Vector2(1f, 0f));

        // ---- Tên màn góc trên trái (dưới bảng thông tin) ----
        levelNameText = MakeText(hudRoot, "", 36, new Color(1f, 1f, 1f, 0.85f),
                                 new Vector2(0f, 1f), new Vector2(24f, -140f), new Vector2(900f, 60f),
                                 FontStyle.Bold, true);
        levelNameText.alignment = TextAnchor.MiddleLeft;

        // ---- Thông báo nhanh (toast) ----
        var toastGo = new GameObject("Toast", typeof(RectTransform), typeof(Text));
        var trt = (RectTransform)toastGo.transform;
        trt.SetParent(hudRoot, false);
        trt.anchorMin = new Vector2(0.5f, 0f); trt.anchorMax = new Vector2(0.5f, 0f);
        trt.pivot = new Vector2(0.5f, 0f);
        trt.anchoredPosition = new Vector2(0f, 130f);
        trt.sizeDelta = new Vector2(1500f, 90f);
        toastText = toastGo.GetComponent<Text>();
        toastText.font = DefaultFont;
        toastText.fontSize = 42;
        toastText.fontStyle = FontStyle.Bold;
        toastText.alignment = TextAnchor.MiddleCenter;
        toastText.color = new Color(1f, 0.92f, 0.55f, 0f);
        toastText.raycastTarget = false;

        // ---- Lớp nháy đỏ khi bị thương ----
        var dmgGo = new GameObject("DamageFlash", typeof(RectTransform), typeof(Image));
        var drt = (RectTransform)dmgGo.transform;
        Stretch(drt);
        drt.SetParent(hudRoot, false);
        damageOverlay = dmgGo.GetComponent<Image>();
        damageOverlay.color = new Color(1f, 0.15f, 0.1f, 0f);
        damageOverlay.raycastTarget = false;

        // ---- BỘ NÚT BẤM TRÊN MÀN HÌNH (chuột/cảm ứng) ----
        BuildTouchControls();
    }

    /// <summary>Nút giữ: nhấn giữ để kích hoạt, thả/kéo ra để tắt.</summary>
    void BindHold(Button btn, System.Action onDown, System.Action onUp)
    {
        var trig = btn.gameObject.AddComponent<EventTrigger>();
        void Add(EventTriggerType type, System.Action action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trig.triggers.Add(entry);
        }
        Add(EventTriggerType.PointerDown, onDown);
        Add(EventTriggerType.PointerUp, onUp);
        Add(EventTriggerType.PointerExit, onUp); // kéo chuột ra khỏi nút = thả
    }

    static void SetMoveX(float x)
        => PlayerController.ScreenMove = new Vector2(x, PlayerController.ScreenMove.y);
    static void SetMoveY(float y)
        => PlayerController.ScreenMove = new Vector2(PlayerController.ScreenMove.x, y);

    void BuildTouchControls()
    {
        Color btnColor = new Color(0.16f, 0.28f, 0.5f, 0.62f);
        Vector2 bl = new Vector2(0f, 0f); // neo góc trái dưới
        Vector2 br = new Vector2(1f, 0f); // neo góc phải dưới

        // ---- D-pad hình chữ thập (trái dưới): giữ nút = đi liên tục ----
        MakeHoldButton("▲", new Vector2(70f, 190f), new Vector2(78f, 78f), btnColor, bl,
            () => SetMoveY(1f), () => SetMoveY(0f));
        MakeHoldButton("◀", new Vector2(22f, 108f), new Vector2(78f, 78f), btnColor, bl,
            () => SetMoveX(-1f), () => SetMoveX(0f));
        MakeHoldButton("▶", new Vector2(118f, 108f), new Vector2(78f, 78f), btnColor, bl,
            () => SetMoveX(1f), () => SetMoveX(0f));
        MakeHoldButton("▼", new Vector2(70f, 26f), new Vector2(78f, 78f), btnColor, bl,
            () => SetMoveY(-1f), () => SetMoveY(0f));

        // ---- Nút NHẢY + CHẠY (phải trên nút PAUSE) ----
        MakeHoldButton("NHẢY", new Vector2(-16f, 236f), new Vector2(122f, 88f), btnColor, br,
            () => PlayerController.ScreenJump = true, () => PlayerController.ScreenJump = false);
        MakeHoldButton("CHẠY", new Vector2(-16f, 132f), new Vector2(122f, 88f), btnColor, br,
            () => PlayerController.ScreenRun = true, () => PlayerController.ScreenRun = false);
    }

    /// <summary>Tạo nút giữ (không có hành động nhấn-thông-thường).</summary>
    Button MakeHoldButton(string label, Vector2 anchoredPos, Vector2 size, Color bg,
                          Vector2 anchor, System.Action onDown, System.Action onUp)
    {
        Button btn = MakeButton(hudRoot, label, anchoredPos, size, bg, () => { }, 34, anchor);
        BindHold(btn, onDown, onUp);
        return btn;
    }

    void BuildPauseScreen()
    {
        pauseRoot = MakeScreen("Pause", new Color(0f, 0f, 0f, 0.7f));

        // Hộp giữa màn hình
        var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        var brt = (RectTransform)box.transform;
        brt.SetParent(pauseRoot, false);
        brt.anchorMin = new Vector2(0.5f, 0.5f); brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.anchoredPosition = Vector2.zero;
        brt.sizeDelta = new Vector2(760f, 700f);
        box.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.18f, 0.97f);

        MakeText(box.transform, "TẠM DỪNG", 84, Color.white,
                 new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(700f, 110f),
                 FontStyle.Bold, true);

        MakeButton(box.transform, "TIẾP TỤC", new Vector2(0f, 90f), new Vector2(560f, 105f),
                   new Color(0.18f, 0.65f, 0.28f), () => gm.ResumeGame());
        MakeButton(box.transform, "CHƠI LẠI MÀN NÀY", new Vector2(0f, -40f), new Vector2(560f, 105f),
                   new Color(0.85f, 0.55f, 0.1f), () => gm.RestartLevel());
        MakeButton(box.transform, "VỀ MENU CHÍNH", new Vector2(0f, -170f), new Vector2(560f, 105f),
                   new Color(0.45f, 0.45f, 0.5f), () => gm.GoToMenu());
    }

    void BuildWinScreen()
    {
        winRoot = MakeScreen("Win", new Color(0.05f, 0.2f, 0.1f, 0.93f));

        winTitleText = MakeText(winRoot, "YOU WIN!", 120, new Color(1f, 0.84f, 0.25f),
                                new Vector2(0.5f, 0.5f), new Vector2(0f, 320f), new Vector2(1600f, 170f),
                                FontStyle.Bold, true);
        winStatsText = MakeText(winRoot, "", 44, Color.white,
                                new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(1500f, 300f));

        nextLevelButton = MakeButton(winRoot, "MÀN TIẾP THEO ►", new Vector2(0f, -110f), new Vector2(540f, 110f),
                                     new Color(0.18f, 0.65f, 0.28f), () => gm.NextLevel(), 46);
        MakeButton(winRoot, "CHƠI LẠI", new Vector2(0f, -240f), new Vector2(540f, 100f),
                   new Color(0.85f, 0.55f, 0.1f), () => gm.RestartLevel());
        MakeButton(winRoot, "VỀ MENU CHÍNH", new Vector2(0f, -360f), new Vector2(540f, 100f),
                   new Color(0.45f, 0.45f, 0.5f), () => gm.GoToMenu());
    }

    void BuildLoseScreen()
    {
        loseRoot = MakeScreen("Lose", new Color(0.25f, 0.04f, 0.05f, 0.93f));

        MakeText(loseRoot, "GAME OVER", 120, new Color(1f, 0.35f, 0.3f),
                 new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(1600f, 170f),
                 FontStyle.Bold, true);
        loseReasonText = MakeText(loseRoot, "", 48, Color.white,
                                  new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(1400f, 90f));

        MakeButton(loseRoot, "THỬ LẠI", new Vector2(0f, 0f), new Vector2(540f, 110f),
                   new Color(0.85f, 0.55f, 0.1f), () => gm.RestartLevel(), 46);
        MakeButton(loseRoot, "VỀ MENU CHÍNH", new Vector2(0f, -140f), new Vector2(540f, 100f),
                   new Color(0.45f, 0.45f, 0.5f), () => gm.GoToMenu());
    }

    // =====================================================================
    //  ĐIỀU KHIỂN HIỂN THỊ
    // =====================================================================

    void ShowOnly(RectTransform visible)
    {
        foreach (RectTransform rt in allScreens)
            rt.gameObject.SetActive(rt == visible);
    }

    public void ShowMainMenu() => ShowOnly(menuRoot);

    public void ShowLevelSelect()
    {
        // Làm mới dòng kỷ lục từng màn
        for (int i = 0; i < levelBestTexts.Length; i++)
        {
            int best = PlayerPrefs.GetInt("maze_best_" + i, 0);
            levelBestTexts[i].text = best > 0
                ? "Kỷ lục của bạn: " + best + " điểm"
                : "Chưa có kỷ lục — hãy là người đầu tiên!";
        }
        ShowOnly(selectRoot);
    }

    public void ShowHUD(LevelDef def)
    {
        levelNameText.text = def.name + " — " + def.subtitle;
        ShowOnly(hudRoot);
    }

    public void ShowPause(bool show)
    {
        pauseRoot.gameObject.SetActive(show);
        if (!show && hudRoot != null) hudRoot.gameObject.SetActive(true);
    }

    public void ShowWin(string stats, bool hasNext, bool allClear)
    {
        winTitleText.text = allClear ? "CHINH PHỤC TẤT CẢ!" : "YOU WIN!";
        if (allClear) winTitleText.fontSize = 96;
        winStatsText.text = stats;
        nextLevelButton.gameObject.SetActive(hasNext);
        ShowOnly(winRoot);
    }

    public void ShowLose(string reason)
    {
        loseReasonText.text = reason;
        ShowOnly(loseRoot);
    }

    // =====================================================================
    //  CẬP NHẬT HUD & HIỆU ỨNG
    // =====================================================================

    public void UpdateHUD(int coins, int totalCoins, int keys, int keysRequired,
                          int lives, float timeLeft, int score)
    {
        if (coinsText != null)
        {
            coinsText.text = coins + "/" + totalCoins;
            keysText.text = keys + "/" + keysRequired;
            timerText.text = GameManager.FormatTime(timeLeft);
            timerText.color = timeLeft < 30f
                ? new Color(1f, 0.3f, 0.25f)
                : (timeLeft < 60f ? new Color(1f, 0.8f, 0.3f) : Color.white);
            scoreText.text = "ĐIỂM: " + score;

            for (int i = 0; i < hearts.Length; i++)
                hearts[i].color = i < lives
                    ? new Color(0.95f, 0.2f, 0.25f)
                    : new Color(0.3f, 0.3f, 0.32f, 0.6f);
        }
    }

    public void DisplayToast(string message)
    {
        toastText.text = message;
        toastTimer = 2.8f;
    }

    public void FlashDamage() => damageAlpha = 0.55f;

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // Tự phục hồi EventSystem nếu bị mất — chuột bấm nút không bao giờ chết
        EnsureEventSystem();

        // Phím tắt dự phòng (khi chuột không bấm được vẫn chơi được bình thường)
        if (gm != null && GameManager.Instance != null &&
            GameManager.Instance.State == GameState.Menu)
        {
            // Màn chọn màn: 1/2/3 = chọn màn, Esc = quay lại
            if (selectRoot != null && selectRoot.gameObject.activeSelf)
            {
                for (int i = 0; i < MazeDatabase.Levels.Length; i++)
                {
                    KeyCode digit = (KeyCode)((int)KeyCode.Alpha1 + i);      // phím số hàng trên
                    KeyCode keypad = (KeyCode)((int)KeyCode.Keypad1 + i);    // phím số bên phải
                    if (Input.GetKeyDown(digit) || Input.GetKeyDown(keypad))
                    {
                        gm.PlaySfx(ProceduralAudio.Click, 0.6f);
                        gm.StartLevel(i);
                        return;
                    }
                }
                if (Input.GetKeyDown(KeyCode.Escape)) gm.EnterMenu();
            }
            // Menu chính: Enter = START GAME
            else if (menuRoot != null && menuRoot.gameObject.activeSelf &&
                     Input.GetKeyDown(KeyCode.Return))
            {
                gm.PlaySfx(ProceduralAudio.Click, 0.6f);
                gm.OpenLevelSelect();
            }
            // Cửa hàng: Esc = về menu chính
            else if (shopRoot != null && shopRoot.gameObject.activeSelf &&
                     Input.GetKeyDown(KeyCode.Escape))
            {
                ShowMainMenu();
            }
        }

        // Toast mờ dần
        if (toastTimer > 0f)
        {
            toastTimer -= dt;
            float alpha = Mathf.Clamp01(toastTimer / 0.6f);
            Color c = toastText.color;
            c.a = alpha;
            toastText.color = c;
        }

        // Nháy đỏ mờ dần
        if (damageAlpha > 0f)
        {
            damageAlpha = Mathf.Max(0f, damageAlpha - dt * 1.1f);
            Color c = damageOverlay.color;
            c.a = damageAlpha * 0.6f;
            damageOverlay.color = c;
        }
    }
}
