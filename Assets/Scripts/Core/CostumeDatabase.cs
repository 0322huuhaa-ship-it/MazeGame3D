using UnityEngine;

// ============================================================================
//  MAZE ADVENTURE 3D — Cửa hàng trang phục
//  • CostumeDef: một trang phục (màu tóc/áo/quần/giày/khăn + kiểu tóc + phụ kiện)
//  • CostumeDatabase: danh sách trang phục + LƯU trạng thái (đã mua / đang mặc)
//    bằng PlayerPrefs — tắt game mở lại vẫn còn nguyên.
//  • Xu kiếm được khi thắng màn (thua được giữ nửa) — xem GameManager.Wallet.
//  Thêm trang phục mới: chỉ cần thêm 1 phần tử vào mảng All bên dưới.
// ============================================================================

/// <summary>Kiểu tóc của nhân vật.</summary>
public enum HairStyle { TwinTails, ShortHair, Bun }

/// <summary>Định nghĩa một trang phục.</summary>
[System.Serializable]
public class CostumeDef
{
    [Header("Thông tin")]
    public string id = "new";        // khóa lưu trữ (không trùng)
    public string name = "Trang phục mới";
    public int price = 100;          // giá xu (0 = mặc định, sở hữu sẵn)

    [Header("Màu sắc")]
    public Color hair = new Color(0.38f, 0.22f, 0.12f);  // tóc
    public Color shirt = new Color(0.98f, 0.5f, 0.55f);  // áo
    public Color pants = new Color(0.38f, 0.52f, 0.82f); // quần
    public Color shoes = new Color(0.92f, 0.92f, 0.96f); // giày
    public Color scarf = new Color(1f, 0.78f, 0.3f);     // khăn quàng
    public Color tie = new Color(0.95f, 0.32f, 0.38f);   // nơ tóc

    [Header("Kiểu dáng")]
    public HairStyle hairStyle = HairStyle.TwinTails;
    public bool scarfOn = true;      // có khăn quàng không
    public bool backpackOn = true;   // có ba lô không
}

public static class CostumeDatabase
{
    const string KeyEquipped = "maze_costume";

    public static readonly CostumeDef[] All =
    {
        // ---------------- Mặc định (sở hữu sẵn) ----------------
        new CostumeDef
        {
            id = "classic", name = "Thám Hiểm Hồng Sanh", price = 0,
            hair = new Color(0.38f, 0.22f, 0.12f),
            shirt = new Color(0.98f, 0.5f, 0.55f),
            pants = new Color(0.38f, 0.52f, 0.82f),
            shoes = new Color(0.92f, 0.92f, 0.96f),
            scarf = new Color(1f, 0.78f, 0.3f),
            tie = new Color(0.95f, 0.32f, 0.38f),
            hairStyle = HairStyle.TwinTails, scarfOn = true, backpackOn = true
        },
        // ---------------- 100 xu ----------------
        new CostumeDef
        {
            id = "student", name = "Học Sinh Thanh Lịch", price = 100,
            hair = new Color(0.12f, 0.12f, 0.14f),
            shirt = new Color(0.95f, 0.96f, 0.98f),
            pants = new Color(0.16f, 0.2f, 0.38f),
            shoes = new Color(0.15f, 0.15f, 0.17f),
            scarf = new Color(0.85f, 0.15f, 0.2f),
            tie = new Color(0.85f, 0.15f, 0.2f),
            hairStyle = HairStyle.ShortHair, scarfOn = true, backpackOn = true
        },
        // ---------------- 150 xu ----------------
        new CostumeDef
        {
            id = "marine", name = "Thủy Thủ Biển Xanh", price = 150,
            hair = new Color(0.95f, 0.8f, 0.35f),
            shirt = new Color(0.2f, 0.35f, 0.7f),
            pants = new Color(0.93f, 0.94f, 0.97f),
            shoes = new Color(0.25f, 0.3f, 0.4f),
            scarf = new Color(0.95f, 0.96f, 0.98f),
            tie = new Color(1f, 0.85f, 0.3f),
            hairStyle = HairStyle.TwinTails, scarfOn = true, backpackOn = false
        },
        // ---------------- 250 xu ----------------
        new CostumeDef
        {
            id = "knight", name = "Hiệp Sĩ Bạch Kim", price = 250,
            hair = new Color(0.9f, 0.92f, 0.95f),
            shirt = new Color(0.55f, 0.58f, 0.65f),
            pants = new Color(0.25f, 0.27f, 0.32f),
            shoes = new Color(0.2f, 0.2f, 0.24f),
            scarf = new Color(0.45f, 0.75f, 0.95f),
            tie = new Color(0.45f, 0.75f, 0.95f),
            hairStyle = HairStyle.Bun, scarfOn = true, backpackOn = false
        },
        // ---------------- 400 xu ----------------
        new CostumeDef
        {
            id = "witch", name = "Phù Thủy Tím Bí Ẩn", price = 400,
            hair = new Color(0.55f, 0.3f, 0.75f),
            shirt = new Color(0.35f, 0.15f, 0.5f),
            pants = new Color(0.2f, 0.12f, 0.3f),
            shoes = new Color(0.18f, 0.12f, 0.25f),
            scarf = new Color(0.7f, 0.4f, 0.9f),
            tie = new Color(0.95f, 0.7f, 0.2f),
            hairStyle = HairStyle.TwinTails, scarfOn = true, backpackOn = false
        },
        // ---------------- 600 xu ----------------
        new CostumeDef
        {
            id = "sakura", name = "Nàng Thơ Sakura", price = 600,
            hair = new Color(0.98f, 0.7f, 0.8f),
            shirt = new Color(0.98f, 0.95f, 0.88f),
            pants = new Color(0.95f, 0.75f, 0.85f),
            shoes = new Color(0.98f, 0.85f, 0.9f),
            scarf = new Color(0.98f, 0.6f, 0.72f),
            tie = new Color(0.95f, 0.4f, 0.55f),
            hairStyle = HairStyle.Bun, scarfOn = true, backpackOn = false
        },
    };

    /// <summary>Lấy trang phục theo id (không thấy → trả mặc định đầu tiên).</summary>
    public static CostumeDef GetById(string id)
    {
        foreach (CostumeDef c in All)
            if (c.id == id) return c;
        return All[0];
    }

    /// <summary>Trang phục đang mặc — dùng khi dựng nhân vật.</summary>
    public static CostumeDef Equipped => GetById(EquippedId);

    public static string EquippedId
    {
        get => PlayerPrefs.GetString(KeyEquipped, "classic");
        set { PlayerPrefs.SetString(KeyEquipped, value); PlayerPrefs.Save(); }
    }

    /// <summary>Trang phục giá 0 luôn được sở hữu sẵn.</summary>
    public static bool IsOwned(CostumeDef c)
        => c.price <= 0 || PlayerPrefs.GetInt("maze_owned_" + c.id, 0) == 1;

    public static void SetOwned(CostumeDef c)
    {
        PlayerPrefs.SetInt("maze_owned_" + c.id, 1);
        PlayerPrefs.Save();
    }
}
