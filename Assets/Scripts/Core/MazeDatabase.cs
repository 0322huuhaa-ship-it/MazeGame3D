using UnityEngine;

// ============================================================================
//  MAZE ADVENTURE 3D — Cơ sở dữ liệu màn chơi
//  Muốn thêm/sửa màn chơi: chỉ cần thêm 1 phần tử "new LevelDef {...}" vào
//  danh sách Levels bên dưới. Mê cung sẽ được sinh tự động theo seed —
//  cùng một seed luôn cho ra cùng một mê cung (có thể tìm được lối ra).
// ============================================================================

/// <summary>Chủ đề thẩm mỹ của từng màn chơi.</summary>
public enum LevelTheme
{
    Garden, // Màn 1 — khu vườn tươi sáng
    Stone,  // Màn 2 — đền đá cổ
    Dark    // Màn 3 — mê cung bóng tối
}

/// <summary>Cấu hình một màn chơi (dễ chỉnh sửa trong Inspector hoặc code).</summary>
[System.Serializable]
public class LevelDef
{
    [Header("Thông tin màn chơi")]
    public string name = "MÀN MỚI";
    public string subtitle = "Tên phụ đề";

    [Header("Kích thước (số lẻ) & seed ngẫu nhiên")]
    public int size = 15;   // số ô mỗi cạnh — càng lớn càng khó
    public int seed = 12345;// đổi seed = đổi bố cục mê cung
    [Range(0f, 1f)] public float braid = 0.3f; // tỉ lệ mở thêm đường vòng (giảm ngõ cụt)

    [Header("Vật phẩm & chướng ngại")]
    public int keys = 1;    // số chìa khóa cần thu thập để mở cổng
    public int coins = 18;  // số đồng xu
    public int blades = 0;  // số lưỡi dao quay (chướng ngại gây sát thương)
    public int secrets = 1; // số hốc bí mật (tường giả + kim cương)

    [Header("Luật chơi")]
    public int timeLimit = 240; // giới hạn thời gian (giây)
    public int lives = 3;       // số mạng

    [Header("Thẩm mỹ")]
    public LevelTheme theme = LevelTheme.Garden;
    public float wallHeight = 3.0f; // chiều cao tường
}

/// <summary>Danh sách toàn bộ màn chơi của game.</summary>
public static class MazeDatabase
{
    public static readonly LevelDef[] Levels =
    {
        // ---------------- MÀN 1: dễ — mê cung nhỏ, 1 chìa khóa ----------------
        new LevelDef
        {
            name = "MÀN 1", subtitle = "Khu Vườn Xanh",
            size = 15, seed = 20260101, braid = 0.30f,
            keys = 1, coins = 30, blades = 0, secrets = 1,
            timeLimit = 240, lives = 3,
            theme = LevelTheme.Garden, wallHeight = 3.0f
        },

        // ---------------- MÀN 2: trung bình — to hơn, 2 chìa khóa, có dao quay -
        new LevelDef
        {
            name = "MÀN 2", subtitle = "Đền Đá Cổ",
            size = 21, seed = 20260202, braid = 0.42f,
            keys = 2, coins = 50, blades = 3, secrets = 1,
            timeLimit = 300, lives = 3,
            theme = LevelTheme.Stone, wallHeight = 3.6f
        },

        // ---------------- MÀN 3: khó — mê cung rộng, 3 chìa khóa, tối, nhiều dao
        new LevelDef
        {
            name = "MÀN 3", subtitle = "Mê Cung Bóng Tối",
            size = 27, seed = 20260303, braid = 0.35f,
            keys = 3, coins = 70, blades = 6, secrets = 2,
            timeLimit = 360, lives = 3,
            theme = LevelTheme.Dark, wallHeight = 4.2f
        },
    };
}
