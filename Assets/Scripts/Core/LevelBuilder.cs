using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// ============================================================================
//  MAZE ADVENTURE 3D — Bộ dựng màn chơi 3D
//  Đọc kết quả từ MazeGenerator và dựng toàn bộ cảnh bằng các khối nguyên thủy
//  (Cube / Sphere / Cylinder / Capsule):
//    sàn, tường (gộp từng hàng thành khối dài cho nhẹ), tường giả, cổng thoát,
//    đệm xuất phát, xu / chìa khóa / kim cương, lưỡi dao quay, đèn, cây, đá,
//    cỏ, trang trí vòng ngoài mê cung, ánh sáng & sương mù theo chủ đề,
//    nhân vật người chơi và camera góc nhìn thứ ba.
// ============================================================================

public static class LevelBuilder
{
    // ---------------- Cấu hình chủ đề ----------------
    struct ThemeCfg
    {
        public Color floor, wallA, wallB, secretTint;
        public Color sunColor, ambient, fogColor;
        public float sunIntensity, fogStart, fogEnd;
        public Vector3 sunRotation;
        public bool solidSky;
        public Color skyColor;
        public Color foliage1, foliage2;
    }

    static ThemeCfg GetCfg(LevelTheme t)
    {
        switch (t)
        {
            case LevelTheme.Stone:
                return new ThemeCfg
                {
                    floor = new Color(0.52f, 0.56f, 0.60f),
                    wallA = new Color(0.50f, 0.54f, 0.58f),
                    wallB = new Color(0.42f, 0.46f, 0.50f),
                    secretTint = new Color(0.46f, 0.50f, 0.55f),
                    sunColor = new Color(1f, 0.97f, 0.9f),
                    sunIntensity = 1.0f,
                    sunRotation = new Vector3(55f, -125f, 0f),
                    ambient = new Color(0.55f, 0.60f, 0.66f),
                    fogColor = new Color(0.62f, 0.70f, 0.76f),
                    fogStart = 8f, fogEnd = 50f,
                    solidSky = false, skyColor = Color.white,
                    foliage1 = new Color(0.30f, 0.44f, 0.28f),
                    foliage2 = new Color(0.38f, 0.54f, 0.33f),
                };
            case LevelTheme.Dark:
                return new ThemeCfg
                {
                    floor = new Color(0.20f, 0.22f, 0.34f),
                    wallA = new Color(0.28f, 0.26f, 0.42f),
                    wallB = new Color(0.23f, 0.22f, 0.36f),
                    secretTint = new Color(0.30f, 0.34f, 0.36f),
                    sunColor = new Color(0.55f, 0.62f, 1f),
                    sunIntensity = 0.32f,
                    sunRotation = new Vector3(60f, -80f, 0f),
                    ambient = new Color(0.14f, 0.15f, 0.24f),
                    fogColor = new Color(0.03f, 0.04f, 0.09f),
                    fogStart = 2f, fogEnd = 26f,
                    solidSky = true, skyColor = new Color(0.03f, 0.04f, 0.09f),
                    foliage1 = new Color(0.12f, 0.22f, 0.20f),
                    foliage2 = new Color(0.15f, 0.28f, 0.24f),
                };
            default: // Garden
                return new ThemeCfg
                {
                    floor = new Color(0.42f, 0.68f, 0.35f),
                    wallA = new Color(0.91f, 0.85f, 0.69f),
                    wallB = new Color(0.84f, 0.77f, 0.57f),
                    secretTint = new Color(0.82f, 0.80f, 0.62f),
                    sunColor = new Color(1f, 0.96f, 0.88f),
                    sunIntensity = 1.15f,
                    sunRotation = new Vector3(55f, -35f, 0f),
                    ambient = new Color(0.70f, 0.76f, 0.82f),
                    fogColor = new Color(0.78f, 0.87f, 0.93f),
                    fogStart = 10f, fogEnd = 55f,
                    solidSky = false, skyColor = Color.white,
                    foliage1 = new Color(0.22f, 0.55f, 0.25f),
                    foliage2 = new Color(0.32f, 0.65f, 0.30f),
                };
        }
    }

    /// <summary>Đổi ô lưới (cx, cz) sang tọa độ world X/Z.</summary>
    static float World(int c, int n) => c * 2f - (n - 1);

    // =====================================================================
    //  HÀM CHÍNH — dựng toàn bộ màn chơi
    // =====================================================================
    public static GameObject Build(LevelDef def, GeneratedMaze m, CostumeDef costume,
                                   out PlayerController player, out ThirdPersonCamera cam)
    {
        int n = m.size;
        float wallH = def.wallHeight;
        var rnd = new System.Random(def.seed ^ 0x51EE);
        ThemeCfg cfg = GetCfg(def.theme);

        var root = new GameObject("Level_" + def.name.Replace(" ", "_"));
        ApplyTheme(cfg, root.transform);

        // ---------------- SÀN ----------------
        float floorSpan = n * 2f + 22f; // rộng hơn mê cung để trồng cây quanh ngoài
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(root.transform, false);
        floor.transform.localPosition = new Vector3(0f, -0.2f, 0f);
        floor.transform.localScale = new Vector3(floorSpan, 0.4f, floorSpan);
        floor.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get(
            "floor_" + def.theme, cfg.floor, 0.06f);

        Material wallMatA = MaterialLibrary.Get("wallA_" + def.theme, cfg.wallA, 0.12f);
        Material wallMatB = MaterialLibrary.Get("wallB_" + def.theme, cfg.wallB, 0.1f);
        Material secretMat = MaterialLibrary.Get("secret_" + def.theme, cfg.secretTint, 0.1f,
            0f, new Color(0.1f, 0.35f, 0.2f), 0.45f); // ánh xanh nhẹ — manh mối

        // ---------------- TƯỜNG (gộp hàng ngang thành khối dài) ----------------
        for (int z = 0; z < n; z++)
        {
            int x = 0;
            while (x < n)
            {
                if (!(m.wall[x, z] && !m.secretWall[x, z])) { x++; continue; }
                int x0 = x;
                while (x < n && m.wall[x, z] && !m.secretWall[x, z]) x++;
                int len = x - x0;

                var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
                w.name = "Wall";
                w.transform.SetParent(root.transform, false);
                float jitter = (float)(rnd.NextDouble() * 0.3 - 0.15); // cao thấp lệch nhẹ
                w.transform.localPosition = new Vector3(
                    (World(x0, n) + World(x - 1, n)) * 0.5f,
                    (wallH + jitter) * 0.5f - 0.02f,
                    World(z, n));
                w.transform.localScale = new Vector3(len * 2f, wallH + jitter, 2f);
                w.GetComponent<Renderer>().sharedMaterial =
                    rnd.NextDouble() < 0.5 ? wallMatA : wallMatB;
            }
        }

        // ---------------- TƯỜNG GIẢ (cửa vào hốc bí mật) ----------------
        foreach (Vector2Int c in m.secretEntrances)
        {
            var sw = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sw.name = "SecretWall";
            sw.transform.SetParent(root.transform, false);
            sw.transform.localPosition = new Vector3(World(c.x, n), wallH * 0.5f - 0.02f, World(c.y, n));
            sw.transform.localScale = new Vector3(2f, wallH, 2f);
            sw.GetComponent<Renderer>().sharedMaterial = secretMat;
            sw.AddComponent<SecretWall>();
        }

        // ---------------- ĐỆM XUẤT PHÁT ----------------
        {
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "StartPad";
            pad.transform.SetParent(root.transform, false);
            pad.transform.localPosition = new Vector3(World(m.start.x, n), 0.03f, World(m.start.y, n));
            pad.transform.localScale = new Vector3(1.7f, 0.04f, 1.7f);
            pad.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get(
                "startPad", new Color(0.3f, 0.55f, 0.95f), 0.3f,
                0f, new Color(0.2f, 0.6f, 1.4f), 0.9f);
            Object.Destroy(pad.GetComponent<Collider>());
        }

        // ---------------- CỔNG THOÁT ----------------
        BuildExitGate(root.transform, def, m, n);

        // ---------------- VẬT PHẨM ----------------
        List<Transform> keyTransforms = new List<Transform>(); // cho mũi tên chỉ đường
        Material coinMat = MaterialLibrary.Get("coin", MaterialLibrary.Gold, 0.65f, 0.7f,
            new Color(1.3f, 0.9f, 0.15f), 1.1f);
        foreach (Vector2Int c in m.coins)
        {
            var coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = "Coin";
            coin.transform.SetParent(root.transform, false);
            coin.transform.localPosition = new Vector3(World(c.x, n), 0.85f, World(c.y, n));
            coin.transform.localScale = new Vector3(0.6f, 0.035f, 0.6f);
            coin.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // dựng đĩa xu đứng
            coin.GetComponent<Renderer>().sharedMaterial = coinMat;
            Object.Destroy(coin.GetComponent<Collider>());
            coin.AddComponent<Collectible>().kind = Collectible.Kind.Coin;
        }

        foreach (Vector2Int c in m.keys)
        {
            var keyGo = new GameObject("Key");
            keyGo.transform.SetParent(root.transform, false);
            keyGo.transform.localPosition = new Vector3(World(c.x, n), 1.0f, World(c.y, n));
            keyGo.AddComponent<Collectible>().kind = Collectible.Kind.Key;
            BuildKeyVisual(keyGo.transform, coinMat);
            keyTransforms.Add(keyGo.transform);
        }

        foreach (Vector2Int c in m.gems)
        {
            var gem = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gem.name = "Gem";
            gem.transform.SetParent(root.transform, false);
            gem.transform.localPosition = new Vector3(World(c.x, n), 0.9f, World(c.y, n));
            gem.transform.localScale = new Vector3(0.42f, 0.58f, 0.42f);
            gem.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get(
                "gem", MaterialLibrary.GemPurple, 0.5f, 0.2f, new Color(0.9f, 0.4f, 1.7f), 1.2f);
            Object.Destroy(gem.GetComponent<Collider>());
            gem.AddComponent<Collectible>().kind = Collectible.Kind.Gem;

            var gl = new GameObject("GemLight").AddComponent<Light>();
            gl.transform.SetParent(gem.transform, false);
            gl.type = LightType.Point;
            gl.color = new Color(0.75f, 0.45f, 1f);
            gl.range = 4.5f;
            gl.intensity = 0.9f;
            gl.shadows = LightShadows.None;
        }

        // ---------------- LƯỠI DAO QUAY ----------------
        foreach (Vector2Int c in m.blades)
        {
            var bladeGo = new GameObject("Blade");
            bladeGo.transform.SetParent(root.transform, false);
            bladeGo.transform.localPosition = new Vector3(World(c.x, n), 0f, World(c.y, n));

            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(bladeGo.transform, false);
            pole.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            pole.transform.localScale = new Vector3(0.12f, 1.25f, 0.12f);
            pole.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get(
                "metal", new Color(0.25f, 0.26f, 0.3f), 0.6f, 0.8f);
            Object.Destroy(pole.GetComponent<Collider>());

            var spinner = new GameObject("Spinner");
            spinner.transform.SetParent(bladeGo.transform, false);
            spinner.transform.localPosition = new Vector3(0f, 1.0f, 0f);

            var hub = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hub.name = "Hub";
            hub.transform.SetParent(spinner.transform, false);
            hub.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            hub.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get(
                "bladeHub", MaterialLibrary.DangerRed, 0.4f, 0.4f, new Color(1.2f, 0.15f, 0.05f), 1f);
            Object.Destroy(hub.GetComponent<Collider>());

            Material armMat = MaterialLibrary.Get("bladeArm", new Color(0.8f, 0.22f, 0.12f),
                0.45f, 0.5f, new Color(1.1f, 0.16f, 0.05f), 0.8f);
            for (int i = 0; i < 2; i++)
            {
                var arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arm.name = "Arm";
                arm.transform.SetParent(spinner.transform, false);
                arm.transform.localRotation = Quaternion.Euler(0f, i * 90f, 0f);
                arm.transform.localScale = new Vector3(1.9f, 0.1f, 0.18f);
                arm.GetComponent<Renderer>().sharedMaterial = armMat;
                Object.Destroy(arm.GetComponent<Collider>());
            }

            var obs = bladeGo.AddComponent<Obstacle>();
            obs.spinner = spinner.transform;
            obs.rpm = (float)(rnd.NextDouble() * 50.0 + 90.0) * (rnd.Next(2) == 0 ? 1f : -1f);
            obs.damageRadius = 1.2f;
        }

        // ---------------- ĐÈN CHIẾU SÁNG ----------------
        foreach (Vector2Int c in m.lamps)
        {
            var lampGo = new GameObject("Lamp");
            lampGo.transform.SetParent(root.transform, false);
            lampGo.transform.localPosition = new Vector3(World(c.x, n), 0f, World(c.y, n));

            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(lampGo.transform, false);
            pole.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            pole.transform.localScale = new Vector3(0.13f, 1.15f, 0.13f);
            pole.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get(
                "lampPole", new Color(0.2f, 0.22f, 0.26f), 0.55f, 0.6f);
            Object.Destroy(pole.GetComponent<Collider>());

            var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = "Bulb";
            bulb.transform.SetParent(lampGo.transform, false);
            bulb.transform.localPosition = new Vector3(0f, 2.45f, 0f);
            bulb.transform.localScale = new Vector3(0.36f, 0.36f, 0.36f);
            bulb.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get(
                "lampBulb", MaterialLibrary.LampWarm, 0.3f, 0f, MaterialLibrary.LampWarm * 2.2f, 1f);
            Object.Destroy(bulb.GetComponent<Collider>());

            var light = lampGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.85f, 0.6f);
            light.range = 9f;
            light.intensity = def.theme == LevelTheme.Dark ? 1.35f : 1.05f;
            light.shadows = LightShadows.None;
        }

        // ---------------- CÂY / ĐÁ / CỎ ----------------
        Material trunkMat = MaterialLibrary.Get("trunk", new Color(0.42f, 0.29f, 0.17f), 0.1f);
        Material leaf1 = MaterialLibrary.Get("leaf1_" + def.theme, cfg.foliage1, 0.05f);
        Material leaf2 = MaterialLibrary.Get("leaf2_" + def.theme, cfg.foliage2, 0.05f);

        foreach (Vector2Int c in m.trees)
        {
            Vector2 openDir = TreeOpenDir(m, c);
            BuildTree(root.transform, rnd, trunkMat, leaf1, leaf2, World(c.x, n), World(c.y, n), openDir);
        }

        Material rockMat = MaterialLibrary.Get("rock", new Color(0.52f, 0.52f, 0.55f), 0.25f);
        foreach (Vector2Int c in m.rocks) BuildRock(root.transform, rnd, rockMat, World(c.x, n), World(c.y, n));

        Material grassMat = MaterialLibrary.Get("grass_" + def.theme,
            def.theme == LevelTheme.Dark ? new Color(0.16f, 0.3f, 0.24f) : new Color(0.35f, 0.62f, 0.3f), 0.05f);
        foreach (Vector2Int c in m.grass) BuildGrass(root.transform, rnd, grassMat, World(c.x, n), World(c.y, n));

        // ---------------- TRANG TRÍ VÒNG NGOÀI MÊ CUNG ----------------
        int outerTrees = n / 2;
        for (int i = 0; i < outerTrees; i++)
        {
            float ang = (float)(rnd.NextDouble() * Mathf.PI * 2.0);
            float r = (n - 1) + (float)(rnd.NextDouble() * 5.0 + 3.5);
            float wx = Mathf.Cos(ang) * r;
            float wz = Mathf.Sin(ang) * r;
            if (rnd.NextDouble() < 0.6) BuildTree(root.transform, rnd, trunkMat, leaf1, leaf2, wx, wz, Vector2.zero);
            else BuildRock(root.transform, rnd, rockMat, wx, wz);
        }

        // ---------------- NHÂN VẬT & CAMERA ----------------
        Vector3 spawn = new Vector3(World(m.start.x, n), 0.05f, World(m.start.y, n));
        Vector3 startDir = StartDirection(m);
        player = BuildPlayer(root.transform, def, spawn, startDir, costume);
        cam = BuildCamera(root.transform, def, spawn, startDir);

        // Kết nối camera <-> nhân vật
        cam.target = player.transform;
        cam.yaw = Mathf.Atan2(startDir.x, startDir.z) * Mathf.Rad2Deg;
        player.CameraTransform = cam.transform;

        // Bản đồ mini (góc phải trên) + mũi tên chỉ đường trên đầu nhân vật
        var minimapGo = new GameObject("Minimap");
        minimapGo.transform.SetParent(root.transform, false);
        minimapGo.AddComponent<Minimap>().Init(m, def, player.transform);

        BuildCompass(player.transform, m, n, keyTransforms);

        return root;
    }

    // =====================================================================
    //  CÁC PHẦN TỬ
    // =====================================================================

    static void ApplyTheme(ThemeCfg cfg, Transform root)
    {
        // Ánh sáng mặt trời
        var sunGo = new GameObject("Sun");
        sunGo.transform.SetParent(root, false);
        sunGo.transform.rotation = Quaternion.Euler(cfg.sunRotation);
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = cfg.sunColor;
        sun.intensity = cfg.sunIntensity;
        sun.shadows = LightShadows.Soft;

        // Môi trường
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = cfg.ambient;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = cfg.fogColor;
        RenderSettings.fogStartDistance = cfg.fogStart;
        RenderSettings.fogEndDistance = cfg.fogEnd;
    }

    static void BuildExitGate(Transform root, LevelDef def, GeneratedMaze m, int n)
    {
        Vector2Int e = m.exit;
        bool corridorAlongX =
            (e.x + 1 < n && !m.wall[e.x + 1, e.y]) || (e.x - 1 >= 0 && !m.wall[e.x - 1, e.y]);

        var gateGo = new GameObject("ExitGate");
        gateGo.transform.SetParent(root, false);
        gateGo.transform.localPosition = new Vector3(World(e.x, n), 0f, World(e.y, n));
        // Cổng phủ ngang hành lang: hành lang chạy theo X => cổng xoay 90°
        gateGo.transform.localRotation = Quaternion.Euler(0f, corridorAlongX ? 90f : 0f, 0f);

        Material pillarMat = MaterialLibrary.Get("gatePillar", new Color(0.35f, 0.33f, 0.38f), 0.25f);
        Material barMat = MaterialLibrary.Get("gateBar", new Color(0.72f, 0.7f, 0.62f), 0.6f, 0.7f);

        // Đệm dưới chân cổng (đỏ khi khóa -> xanh khi mở)
        var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.name = "GatePad";
        pad.transform.SetParent(gateGo.transform, false);
        pad.transform.localPosition = new Vector3(0f, 0.04f, 0f);
        pad.transform.localScale = new Vector3(1.9f, 0.05f, 1.9f);
        pad.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get(
            "gatePad", new Color(0.8f, 0.2f, 0.15f), 0.3f, 0f, new Color(1.3f, 0.15f, 0.1f), 1f);
        Object.Destroy(pad.GetComponent<Collider>());

        // Trụ 2 bên
        for (int s = -1; s <= 1; s += 2)
        {
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = "Pillar";
            pillar.transform.SetParent(gateGo.transform, false);
            pillar.transform.localPosition = new Vector3(0.95f * s, 1.65f, 0f);
            pillar.transform.localScale = new Vector3(0.45f, 3.3f, 0.45f);
            pillar.GetComponent<Renderer>().sharedMaterial = pillarMat;
        }

        // Thanh ngang trên + biển phát sáng
        var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
        top.name = "TopBar";
        top.transform.SetParent(gateGo.transform, false);
        top.transform.localPosition = new Vector3(0f, 3.3f, 0f);
        top.transform.localScale = new Vector3(2.35f, 0.42f, 0.45f);
        top.GetComponent<Renderer>().sharedMaterial = pillarMat;

        var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sign.name = "Sign";
        sign.transform.SetParent(gateGo.transform, false);
        sign.transform.localPosition = new Vector3(0f, 3.02f, 0f);
        sign.transform.localScale = new Vector3(2.0f, 0.2f, 0.52f);
        sign.GetComponent<Renderer>().sharedMaterial = MaterialLibrary.Get(
            "gateSign", new Color(0.9f, 0.2f, 0.12f), 0.4f, 0f, new Color(1.4f, 0.2f, 0.1f), 1f);
        Object.Destroy(sign.GetComponent<Collider>());
        Object.Destroy(top.GetComponent<Collider>());

        // Nhóm thanh cổng (trượt xuống khi mở)
        var barsGo = new GameObject("Bars");
        barsGo.transform.SetParent(gateGo.transform, false);
        barsGo.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        var barsCol = barsGo.AddComponent<BoxCollider>();
        barsCol.size = new Vector3(1.7f, 3f, 0.6f); // chắn người chơi khi chưa mở

        for (int i = -1; i <= 1; i++)
        {
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "Bar";
            bar.transform.SetParent(barsGo.transform, false);
            bar.transform.localPosition = new Vector3(0.55f * i, 0f, 0f);
            bar.transform.localScale = new Vector3(0.15f, 3f, 0.15f);
            bar.GetComponent<Renderer>().sharedMaterial = barMat;
            Object.Destroy(bar.GetComponent<Collider>());
        }

        // Đèn cổng
        var gateLight = gateGo.AddComponent<Light>();
        gateLight.type = LightType.Point;
        gateLight.color = new Color(1f, 0.35f, 0.25f);
        gateLight.range = 8f;
        gateLight.intensity = 1.2f;
        gateLight.shadows = LightShadows.None;

        // Script điều khiển cổng
        var gate = gateGo.AddComponent<ExitGate>();
        gate.keysRequired = def.keys;
        gate.bars = barsGo.transform;
        gate.padRenderer = pad.GetComponent<Renderer>();
        gate.gateLight = gateLight;
    }

    static void BuildKeyVisual(Transform parent, Material goldMat)
    {
        var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "Ring";
        ring.transform.SetParent(parent, false);
        ring.transform.localPosition = new Vector3(0f, 0.18f, 0f);
        ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        ring.transform.localScale = new Vector3(0.26f, 0.03f, 0.26f);
        ring.GetComponent<Renderer>().sharedMaterial = goldMat;
        Object.Destroy(ring.GetComponent<Collider>());

        var shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shaft.name = "Shaft";
        shaft.transform.SetParent(parent, false);
        shaft.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        shaft.transform.localScale = new Vector3(0.08f, 0.4f, 0.08f);
        shaft.GetComponent<Renderer>().sharedMaterial = goldMat;
        Object.Destroy(shaft.GetComponent<Collider>());

        for (int i = 0; i < 2; i++)
        {
            var tooth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tooth.name = "Tooth";
            tooth.transform.SetParent(parent, false);
            tooth.transform.localPosition = new Vector3(0.09f, -0.14f - i * 0.11f, 0f);
            tooth.transform.localScale = new Vector3(0.14f, 0.06f, 0.08f);
            tooth.GetComponent<Renderer>().sharedMaterial = goldMat;
            Object.Destroy(tooth.GetComponent<Collider>());
        }
    }

    static void BuildTree(Transform root, System.Random rnd, Material trunk, Material leaf1, Material leaf2,
                          float wx, float wz, Vector2 openDir)
    {
        var tree = new GameObject("Tree");
        tree.transform.SetParent(root, false);

        // Cây trong ngõ cụt: lệch vào góc tường đối diện lối vào để chừa lối đi vòng qua
        Vector2 offset = Vector2.zero;
        if (openDir.sqrMagnitude > 0.01f)
        {
            Vector2 perp = new Vector2(-openDir.y, openDir.x);
            if (rnd.Next(2) == 0) perp = -perp;
            offset = (perp - openDir) * 0.55f;
        }
        tree.transform.localPosition = new Vector3(wx + offset.x, 0f, wz + offset.y);
        tree.transform.localRotation = Quaternion.Euler(0f, (float)(rnd.NextDouble() * 360.0), 0f);

        var t = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        t.name = "Trunk";
        t.transform.SetParent(tree.transform, false);
        t.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        t.transform.localScale = new Vector3(0.3f, 0.55f, 0.3f);
        t.GetComponent<Renderer>().sharedMaterial = trunk;
        Object.Destroy(t.GetComponent<Collider>());

        var f1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        f1.name = "Foliage1";
        f1.transform.SetParent(tree.transform, false);
        f1.transform.localPosition = new Vector3(0f, 1.55f, 0f);
        f1.transform.localScale = new Vector3(1.7f, 1.6f, 1.7f);
        f1.GetComponent<Renderer>().sharedMaterial = leaf1;
        Object.Destroy(f1.GetComponent<Collider>());

        var f2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        f2.name = "Foliage2";
        f2.transform.SetParent(tree.transform, false);
        f2.transform.localPosition = new Vector3(0.18f, 2.3f, 0.1f);
        f2.transform.localScale = new Vector3(1.1f, 1.0f, 1.1f);
        f2.GetComponent<Renderer>().sharedMaterial = leaf2;
        Object.Destroy(f2.GetComponent<Collider>());

        // Thân cây nhỏ, né vào góc nên người chơi vẫn đi vòng qua được
        var col = tree.AddComponent<CapsuleCollider>();
        col.radius = 0.3f;
        col.height = 2.6f;
        col.center = new Vector3(0f, 1.3f, 0f);
    }

    static void BuildRock(Transform root, System.Random rnd, Material rockMat, float wx, float wz)
    {
        var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rock.name = "Rock";
        rock.transform.SetParent(root, false);
        rock.transform.localPosition = new Vector3(wx, 0.16f, wz);
        rock.transform.localRotation = Quaternion.Euler(
            0f, (float)(rnd.NextDouble() * 360.0), 0f);
        rock.transform.localScale = new Vector3(
            0.8f + (float)rnd.NextDouble() * 0.3f,
            0.5f + (float)rnd.NextDouble() * 0.2f,
            0.9f);
        rock.GetComponent<Renderer>().sharedMaterial = rockMat;
        Object.Destroy(rock.GetComponent<Collider>());
    }

    static void BuildGrass(Transform root, System.Random rnd, Material grassMat, float wx, float wz)
    {
        var grass = new GameObject("Grass");
        grass.transform.SetParent(root, false);
        grass.transform.localPosition = new Vector3(wx, 0f, wz);

        for (int i = 0; i < 3; i++)
        {
            var blade = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            blade.name = "Blade";
            blade.transform.SetParent(grass.transform, false);
            blade.transform.localPosition = new Vector3(
                (float)(rnd.NextDouble() * 1.1 - 0.55), 0.14f, (float)(rnd.NextDouble() * 1.1 - 0.55));
            blade.transform.localRotation = Quaternion.Euler(
                (float)(rnd.NextDouble() * 24 - 12), (float)(rnd.NextDouble() * 360.0), (float)(rnd.NextDouble() * 24 - 12));
            blade.transform.localScale = new Vector3(0.06f, 0.14f, 0.06f);
            blade.GetComponent<Renderer>().sharedMaterial = grassMat;
            Object.Destroy(blade.GetComponent<Collider>());
        }
    }

    // =====================================================================
    //  NHÂN VẬT & CAMERA
    // =====================================================================

    static Vector3 StartDirection(GeneratedMaze m)
    {
        // Ô xuất phát luôn ở góc (1,1) nên chỉ có thể mở về +X hoặc +Z
        if (!m.wall[m.start.x, m.start.y + 1]) return Vector3.forward;
        if (!m.wall[m.start.x + 1, m.start.y]) return Vector3.right;
        return Vector3.forward;
    }

    /// <summary>Hướng (mặt XZ) từ ô ngõ cụt tới ô hành lang duy nhất của nó — dùng để né cây vào góc.</summary>
    static Vector2 TreeOpenDir(GeneratedMaze m, Vector2Int c)
    {
        int[] dx = { 1, -1, 0, 0 };
        int[] dz = { 0, 0, 1, -1 };
        for (int i = 0; i < 4; i++)
        {
            int nx = c.x + dx[i], nz = c.y + dz[i];
            if (nx >= 0 && nx < m.size && nz >= 0 && nz < m.size && !m.wall[nx, nz])
                return new Vector2(dx[i], dz[i]);
        }
        return Vector2.zero;
    }

    /// <summary>
    /// Dựng THÂN nhân vật chibi theo trang phục — dùng chung cho người chơi
    /// trong game và ảnh minh họa trong cửa hàng.
    /// Bộ phận đặt tên: Head, ArmL/ArmR, LegL/LegR (trục xoay tay chân).
    /// </summary>
    public static GameObject BuildCharacterBody(Transform parent, CostumeDef costume)
    {
        var body = new GameObject("CharacterBody");
        body.transform.SetParent(parent, false);

        // ----- Mô hình chibi theo TRANG PHỤC (màu lấy từ CostumeDef) -----
        string k = costume.id;
        Material skinMat  = MaterialLibrary.Get("pSkin",  new Color(1f, 0.88f, 0.78f), 0.15f);
        Material hairMat  = MaterialLibrary.Get("pHair_" + k,  costume.hair,  0.35f);
        Material shirtMat = MaterialLibrary.Get("pShirt_" + k, costume.shirt, 0.25f);
        Material pantsMat = MaterialLibrary.Get("pPants_" + k, costume.pants, 0.2f);
        Material shoeMat  = MaterialLibrary.Get("pShoe_" + k,  costume.shoes, 0.3f);
        Material eyeMat   = MaterialLibrary.Get("pEye",   new Color(0.15f, 0.11f, 0.09f), 0.5f);
        Material whiteMat = MaterialLibrary.Get("pWhite", Color.white, 0.5f);
        Material cheekMat = MaterialLibrary.Get("pCheek", new Color(1f, 0.58f, 0.58f), 0.2f);
        Material scarfMat = MaterialLibrary.Get("pScarf_" + k, costume.scarf, 0.3f);
        Material packMat  = MaterialLibrary.Get("pPack",  new Color(0.62f, 0.42f, 0.25f), 0.2f);
        Material tieMat   = MaterialLibrary.Get("pTie_" + k,   costume.tie,   0.3f);

        Transform MakePart(PrimitiveType type, Vector3 localPos, Vector3 scale, Material mat, string partName)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = partName;
            part.transform.SetParent(body.transform, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = mat;
            Object.Destroy(part.GetComponent<Collider>());
            return part.transform;
        }

        // Thân + khăn quàng + ba lô (theo trang phục)
        MakePart(PrimitiveType.Capsule, new Vector3(0f, 0.98f, 0f), new Vector3(0.6f, 0.42f, 0.52f), shirtMat, "Torso");
        if (costume.scarfOn)
        {
            MakePart(PrimitiveType.Cylinder, new Vector3(0f, 1.32f, 0f), new Vector3(0.4f, 0.05f, 0.4f), scarfMat, "Scarf");
            MakePart(PrimitiveType.Sphere, new Vector3(0f, 1.22f, 0.24f), new Vector3(0.14f, 0.22f, 0.08f), scarfMat, "ScarfTail");
        }
        if (costume.backpackOn)
            MakePart(PrimitiveType.Cube, new Vector3(0f, 0.98f, -0.3f), new Vector3(0.36f, 0.42f, 0.18f), packMat, "Backpack");

        // Đầu to tròn + lớp tóc phủ
        MakePart(PrimitiveType.Sphere, new Vector3(0f, 1.62f, 0f), new Vector3(0.62f, 0.6f, 0.6f), skinMat, "Head");
        MakePart(PrimitiveType.Sphere, new Vector3(0f, 1.74f, -0.06f), new Vector3(0.66f, 0.56f, 0.64f), hairMat, "HairBack");
        MakePart(PrimitiveType.Sphere, new Vector3(0f, 1.86f, 0.02f), new Vector3(0.64f, 0.34f, 0.62f), hairMat, "HairTop");
        MakePart(PrimitiveType.Sphere, new Vector3(0f, 1.78f, 0.22f), new Vector3(0.5f, 0.2f, 0.22f), hairMat, "Bangs");

        // Kiểu tóc theo trang phục: đuôi sam / tóc ngắn / búi tóc
        if (costume.hairStyle == HairStyle.TwinTails)
        {
            MakePart(PrimitiveType.Capsule, new Vector3(-0.36f, 1.6f, -0.08f), new Vector3(0.16f, 0.22f, 0.16f), hairMat, "TailL");
            MakePart(PrimitiveType.Sphere, new Vector3(-0.38f, 1.34f, -0.08f), new Vector3(0.2f, 0.24f, 0.2f), hairMat, "TailTipL");
            MakePart(PrimitiveType.Capsule, new Vector3(0.36f, 1.6f, -0.08f), new Vector3(0.16f, 0.22f, 0.16f), hairMat, "TailR");
            MakePart(PrimitiveType.Sphere, new Vector3(0.38f, 1.34f, -0.08f), new Vector3(0.2f, 0.24f, 0.2f), hairMat, "TailTipR");
            MakePart(PrimitiveType.Sphere, new Vector3(-0.32f, 1.76f, -0.05f), new Vector3(0.13f, 0.1f, 0.1f), tieMat, "TieL");
            MakePart(PrimitiveType.Sphere, new Vector3(0.32f, 1.76f, -0.05f), new Vector3(0.13f, 0.1f, 0.1f), tieMat, "TieR");
        }
        else if (costume.hairStyle == HairStyle.Bun)
        {
            MakePart(PrimitiveType.Sphere, new Vector3(0f, 2.04f, -0.16f), new Vector3(0.28f, 0.26f, 0.28f), hairMat, "Bun");
        }
        // ShortHair: chỉ giữ lớp tóc gốc phía trên — không thêm gì nữa

        // Mặt: mắt to có điểm sáng + má hồng phấn
        MakePart(PrimitiveType.Sphere, new Vector3(-0.13f, 1.64f, 0.27f), new Vector3(0.1f, 0.13f, 0.06f), eyeMat, "EyeL");
        MakePart(PrimitiveType.Sphere, new Vector3(0.13f, 1.64f, 0.27f), new Vector3(0.1f, 0.13f, 0.06f), eyeMat, "EyeR");
        MakePart(PrimitiveType.Sphere, new Vector3(-0.1f, 1.68f, 0.31f), new Vector3(0.04f, 0.05f, 0.03f), whiteMat, "EyeGlowL");
        MakePart(PrimitiveType.Sphere, new Vector3(0.16f, 1.68f, 0.31f), new Vector3(0.04f, 0.05f, 0.03f), whiteMat, "EyeGlowR");
        MakePart(PrimitiveType.Sphere, new Vector3(-0.22f, 1.55f, 0.2f), new Vector3(0.09f, 0.055f, 0.04f), cheekMat, "CheekL");
        MakePart(PrimitiveType.Sphere, new Vector3(0.22f, 1.55f, 0.2f), new Vector3(0.09f, 0.055f, 0.04f), cheekMat, "CheekR");

        // Tay chân tròn có bàn tay / đôi giày — trục xoay ở vai/hông để vung khi đi
        Transform MakeLimb(string name, Vector3 pivotPos, Vector3 scale, Material mat,
                           float tipY, Vector3 tipScale, Material tipMat)
        {
            var pivot = new GameObject(name);
            pivot.transform.SetParent(body.transform, false);
            pivot.transform.localPosition = pivotPos;

            var limb = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            limb.name = name + "_Limb";
            limb.transform.SetParent(pivot.transform, false);
            limb.transform.localPosition = new Vector3(0f, -scale.y * 0.45f, 0f);
            limb.transform.localScale = scale;
            limb.GetComponent<Renderer>().sharedMaterial = mat;
            Object.Destroy(limb.GetComponent<Collider>());

            var tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.name = name + "_Tip";
            tip.transform.SetParent(pivot.transform, false);
            tip.transform.localPosition = new Vector3(0f, tipY, 0f);
            tip.transform.localScale = tipScale;
            tip.GetComponent<Renderer>().sharedMaterial = tipMat;
            Object.Destroy(tip.GetComponent<Collider>());
            return pivot.transform;
        }

        MakeLimb("ArmL", new Vector3(-0.4f, 1.2f, 0f),
            new Vector3(0.15f, 0.3f, 0.15f), shirtMat, -0.45f, new Vector3(0.14f, 0.14f, 0.14f), skinMat);
        MakeLimb("ArmR", new Vector3(0.4f, 1.2f, 0f),
            new Vector3(0.15f, 0.3f, 0.15f), shirtMat, -0.45f, new Vector3(0.14f, 0.14f, 0.14f), skinMat);
        MakeLimb("LegL", new Vector3(-0.15f, 0.6f, 0f),
            new Vector3(0.16f, 0.42f, 0.16f), pantsMat, -0.58f, new Vector3(0.17f, 0.12f, 0.22f), shoeMat);
        MakeLimb("LegR", new Vector3(0.15f, 0.6f, 0f),
            new Vector3(0.16f, 0.42f, 0.16f), pantsMat, -0.58f, new Vector3(0.17f, 0.12f, 0.22f), shoeMat);

        return body;
    }

    static PlayerController BuildPlayer(Transform root, LevelDef def, Vector3 spawn, Vector3 startDir,
                                        CostumeDef costume)
    {
        var pgo = new GameObject("Player");
        pgo.tag = "Player";
        pgo.transform.SetParent(root, false);
        pgo.transform.position = spawn;
        pgo.transform.rotation = Quaternion.LookRotation(startDir);

        var cc = pgo.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.95f, 0f);
        cc.slopeLimit = 50f;
        cc.stepOffset = 0.45f;

        // Thân nhân vật dựng từ hàm dùng chung (cửa hàng cũng dùng để chụp ảnh)
        GameObject body = BuildCharacterBody(pgo.transform, costume);
        Transform armL = body.transform.Find("ArmL");
        Transform armR = body.transform.Find("ArmR");
        Transform legL = body.transform.Find("LegL");
        Transform legR = body.transform.Find("LegR");
        Transform headGo = body.transform.Find("Head");

        // Đèn đội đầu gắn lên tóc ở màn tối cho dễ nhìn đường
        if (def.theme == LevelTheme.Dark)
        {
            Light headlamp = headGo.gameObject.AddComponent<Light>();
            headlamp.type = LightType.Point;
            headlamp.color = new Color(0.7f, 0.8f, 1f);
            headlamp.range = 7f;
            headlamp.intensity = 0.9f;
            headlamp.shadows = LightShadows.None;
        }

        var pc = pgo.AddComponent<PlayerController>();
        pc.ArmL = armL; pc.ArmR = armR; pc.LegL = legL; pc.LegR = legR;
        return pc;
    }

    static ThirdPersonCamera BuildCamera(Transform root, LevelDef def, Vector3 spawn, Vector3 startDir)
    {
        var camGo = new GameObject("MainCamera");
        camGo.transform.SetParent(root, false);
        camGo.transform.position = spawn - startDir * 6f + Vector3.up * 3.5f;

        var camComp = camGo.AddComponent<Camera>();
        camComp.fieldOfView = 62f;
        camComp.nearClipPlane = 0.1f;
        camComp.farClipPlane = 300f;
        if (def.theme == LevelTheme.Dark)
        {
            camComp.clearFlags = CameraClearFlags.SolidColor;
            camComp.backgroundColor = new Color(0.03f, 0.04f, 0.09f);
        }

        camGo.AddComponent<AudioListener>(); // máy nghe duy nhất của cảnh

        var tpc = camGo.AddComponent<ThirdPersonCamera>();
        return tpc;
    }

    /// <summary>
    /// Mũi tên chỉ đường bay trên đầu nhân vật: vàng = chìa khóa gần nhất,
    /// xanh lá = cổng thoát. Giúp người chơi không bao giờ mất phương hướng.
    /// </summary>
    static void BuildCompass(Transform playerT, GeneratedMaze m, int n, List<Transform> keyTransforms)
    {
        var compass = new GameObject("Compass");
        compass.transform.SetParent(playerT, false);
        compass.transform.localPosition = new Vector3(0f, 2.5f, 0f);

        Material gold = MaterialLibrary.Get("compassGold", MaterialLibrary.Gold, 0.4f, 0.2f,
            new Color(1.2f, 0.9f, 0.2f), 1f);
        Material green = MaterialLibrary.Get("compassGreen", new Color(0.3f, 0.9f, 0.4f), 0.4f, 0.2f,
            new Color(0.2f, 1.2f, 0.4f), 1f);

        // Mũi tên hình chữ V (2 thanh nghiêng tạo thành mũi chỉ về +Z)
        for (int i = -1; i <= 1; i += 2)
        {
            var wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wing.name = "Wing";
            wing.transform.SetParent(compass.transform, false);
            wing.transform.localPosition = new Vector3(0.1f * i, 0f, 0.15f);
            wing.transform.localRotation = Quaternion.Euler(0f, i == 1 ? 45f : -135f, 0f);
            wing.transform.localScale = new Vector3(0.26f, 0.05f, 0.05f);
            wing.GetComponent<Renderer>().sharedMaterial = gold;
            Object.Destroy(wing.GetComponent<Collider>());
        }

        CompassArrow arrow = compass.AddComponent<CompassArrow>();
        arrow.keyTransforms = keyTransforms;
        arrow.goldMat = gold;
        arrow.greenMat = green;
        arrow.exitWorld = new Vector3(World(m.exit.x, n), 0f, World(m.exit.y, n));
    }
}
