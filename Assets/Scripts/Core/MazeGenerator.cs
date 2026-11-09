using System.Collections.Generic;
using UnityEngine;

// ============================================================================
//  MAZE ADVENTURE 3D — Bộ sinh mê cung
//  • Thuật toán "Recursive Backtracker" (DFS) đục hành lang -> mê cung LIÊN
//    THÔNG (mọi ô sàn đều đến được nhau) => LUÔN CÓ ĐƯỜNG RA.
//  • Braid: đục thêm vài lối vòng để bớt ngõ cụt, chơi vui hơn.
//  • BFS: đo khoảng cách từng ô, chọn cổng thoát ở ô XA NHẤT, đặt chìa khóa
//    ở các ngách xa, đặt lưỡi dao trên đường đi ngắn nhất.
//  • Hốc bí mật: biến một cụm tường kín thành phòng ẩn sau "tường giả".
//  Toàn bộ dùng System.Random(seed) => kết quả NHẤT QUÁN theo seed.
// ============================================================================

/// <summary>Kết quả sau khi sinh: lưới tường + vị trí mọi vật thể.</summary>
public class GeneratedMaze
{
    public int size;
    public bool[,] wall;       // true = tường thật (chắn người chơi)
    public bool[,] secretWall; // true = tường giả (cửa vào hốc bí mật)
    public Vector2Int start;   // ô xuất phát
    public Vector2Int exit;    // ô cổng thoát (xa nhất so với điểm bắt đầu)

    public readonly List<Vector2Int> keys = new List<Vector2Int>();
    public readonly List<Vector2Int> coins = new List<Vector2Int>();
    public readonly List<Vector2Int> blades = new List<Vector2Int>();
    public readonly List<Vector2Int> lamps = new List<Vector2Int>();
    public readonly List<Vector2Int> trees = new List<Vector2Int>();
    public readonly List<Vector2Int> rocks = new List<Vector2Int>();
    public readonly List<Vector2Int> grass = new List<Vector2Int>();
    public readonly List<Vector2Int> gems = new List<Vector2Int>();           // trong hốc bí mật
    public readonly List<Vector2Int> secretEntrances = new List<Vector2Int>(); // ô tường giả
}

public static class MazeGenerator
{
    static readonly int[] Dx = { 1, -1, 0, 0 };
    static readonly int[] Dz = { 0, 0, 1, -1 };

    public static GeneratedMaze Generate(LevelDef def)
    {
        int n = def.size % 2 == 0 ? def.size + 1 : def.size; // đảm bảo số lẻ
        var rnd = new System.Random(def.seed);
        var m = new GeneratedMaze { size = n, wall = new bool[n, n], secretWall = new bool[n, n] };

        // ---------- 0) Toàn bộ là tường ----------
        for (int x = 0; x < n; x++)
            for (int z = 0; z < n; z++)
                m.wall[x, z] = true;

        m.start = new Vector2Int(1, 1);
        m.wall[1, 1] = false;

        // ---------- 1) DFS đục hành lang ----------
        var stack = new Stack<Vector2Int>();
        stack.Push(m.start);
        while (stack.Count > 0)
        {
            Vector2Int c = stack.Peek();
            var order = new List<int> { 0, 1, 2, 3 };
            Shuffle(order, rnd);
            bool carved = false;
            foreach (int i in order)
            {
                int nx = c.x + Dx[i] * 2, nz = c.y + Dz[i] * 2;
                if (nx > 0 && nx < n - 1 && nz > 0 && nz < n - 1 && m.wall[nx, nz])
                {
                    m.wall[c.x + Dx[i], c.y + Dz[i]] = false; // đục ô giữa
                    m.wall[nx, nz] = false;                    // đục ô đích
                    stack.Push(new Vector2Int(nx, nz));
                    carved = true;
                    break;
                }
            }
            if (!carved) stack.Pop();
        }

        // ---------- 2) Braid: mở thêm đường vòng ở một số ngõ cụt ----------
        var deadEnds = new List<Vector2Int>();
        for (int x = 1; x < n - 1; x++)
            for (int z = 1; z < n - 1; z++)
                if (!m.wall[x, z] && FloorNeighbors(m, x, z) == 1)
                    deadEnds.Add(new Vector2Int(x, z));

        foreach (Vector2Int d in deadEnds)
        {
            if (rnd.NextDouble() >= def.braid) continue;
            var opts = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                int wx = d.x + Dx[i], wz = d.y + Dz[i];
                int fx = d.x + Dx[i] * 2, fz = d.y + Dz[i] * 2;
                if (wx > 0 && wx < n - 1 && wz > 0 && wz < n - 1 &&
                    m.wall[wx, wz] && !m.wall[fx, fz])
                    opts.Add(i);
            }
            if (opts.Count > 0)
            {
                int i = opts[rnd.Next(opts.Count)];
                m.wall[d.x + Dx[i], d.y + Dz[i]] = false;
            }
        }

        // ---------- 3) BFS từ điểm bắt đầu (đo khoảng cách + lưu cha) ----------
        var dist = new int[n, n];
        var parent = new Vector2Int[n, n];
        for (int x = 0; x < n; x++)
            for (int z = 0; z < n; z++)
                dist[x, z] = -1;
        dist[m.start.x, m.start.y] = 0;

        var queue = new Queue<Vector2Int>();
        queue.Enqueue(m.start);
        while (queue.Count > 0)
        {
            Vector2Int c = queue.Dequeue();
            for (int i = 0; i < 4; i++)
            {
                int nx = c.x + Dx[i], nz = c.y + Dz[i];
                if (nx >= 0 && nx < n && nz >= 0 && nz < n && !m.wall[nx, nz] && dist[nx, nz] < 0)
                {
                    dist[nx, nz] = dist[c.x, c.y] + 1;
                    parent[nx, nz] = c;
                    queue.Enqueue(new Vector2Int(nx, nz));
                }
            }
        }

        // Cổng thoát = ô sàn xa nhất so với điểm bắt đầu
        int best = -1;
        for (int x = 1; x < n - 1; x++)
            for (int z = 1; z < n - 1; z++)
                if (dist[x, z] > best) { best = dist[x, z]; m.exit = new Vector2Int(x, z); }

        // ---------- 4) Đường đi ngắn nhất start -> exit ----------
        var path = new List<Vector2Int>();
        {
            Vector2Int cur = m.exit;
            while (cur != m.start) { path.Add(cur); cur = parent[cur.x, cur.y]; }
            path.Add(m.start);
            path.Reverse();
        }

        var reserved = new HashSet<Vector2Int> { m.start, m.exit };

        // ---------- 5) Hốc bí mật: ngõ cụt bị che bởi TƯỜNG GIẢ ----------
        // Chọn ngõ cụt d sao cho ô hành lang p duy nhất dẫn vào d là "đường cụt"
        // (p có đúng 2 ô sàn kề) => đặt tường giả tại p, khoá kim cương trong d.
        // Vì p là đường cụt nên chặn p không làm hỏng đường đi chính của mê cung.
        var secretCandidates = new List<Vector2Int>();
        for (int x = 1; x < n - 1; x++)
            for (int z = 1; z < n - 1; z++)
            {
                Vector2Int cell = new Vector2Int(x, z);
                if (!m.wall[x, z] && dist[x, z] > 2 &&
                    cell != m.start && cell != m.exit &&
                    FloorNeighbors(m, x, z) == 1)
                    secretCandidates.Add(cell);
            }
        Shuffle(secretCandidates, rnd);

        int secretsPlaced = 0;
        foreach (Vector2Int d in secretCandidates)
        {
            if (secretsPlaced >= def.secrets) break;

            // Ô hành lang p duy nhất nối với ngõ cụt d
            Vector2Int p = Vector2Int.zero; bool found = false;
            for (int i = 0; i < 4; i++)
            {
                int nx = d.x + Dx[i], nz = d.y + Dz[i];
                if (nx >= 0 && nx < n && nz >= 0 && nz < n && !m.wall[nx, nz])
                { p = new Vector2Int(nx, nz); found = true; break; }
            }
            if (!found || p == m.start || p == m.exit) continue;
            if (FloorNeighbors(m, p.x, p.y) != 2) continue; // p phải là đường cụt

            m.secretWall[p.x, p.y] = true; // tường giả chặn lối vào hốc
            m.secretEntrances.Add(p);
            m.gems.Add(d);                 // kim cương để trong hốc
            reserved.Add(p); reserved.Add(d);
            secretsPlaced++;
        }

        // ---------- 6) Chìa khóa: các ô xa, rải rác ----------
        var far = new List<Vector2Int>();
        for (int x = 1; x < n - 1; x++)
            for (int z = 1; z < n - 1; z++)
                if (dist[x, z] > 3 && dist[x, z] >= best * 0.4f && !reserved.Contains(new Vector2Int(x, z)))
                    far.Add(new Vector2Int(x, z));
        far.Sort((a, b) => dist[b.x, b.y].CompareTo(dist[a.x, a.y])); // xa trước

        foreach (Vector2Int cell in far)
        {
            if (m.keys.Count >= def.keys) break;
            bool spaced = true;
            foreach (Vector2Int k in m.keys)
                if (Mathf.Abs(k.x - cell.x) + Mathf.Abs(k.y - cell.y) < n / 3) { spaced = false; break; }
            if (!spaced) continue;
            m.keys.Add(cell);
            reserved.Add(cell);
        }
        // Dự phòng: nếu chưa đủ (hiếm), nới lỏng điều kiện khoảng cách
        if (m.keys.Count < def.keys)
        {
            foreach (Vector2Int cell in far)
            {
                if (m.keys.Count >= def.keys) break;
                if (reserved.Contains(cell) || m.keys.Contains(cell)) continue;
                m.keys.Add(cell);
                reserved.Add(cell);
            }
        }

        // ---------- 7) Lưỡi dao quay: cài trên đường đi ngắn nhất ----------
        if (def.blades > 0 && path.Count > 8)
        {
            int spacing = Mathf.Max(2, path.Count / (def.blades + 1));
            for (int k = 1; k <= def.blades; k++)
            {
                int idx = Mathf.Min(path.Count - 2, k * spacing);
                if (idx < 6) idx = 6; // không cài sát điểm xuất phát
                for (int tries = 0; tries < 3 && idx < path.Count - 1; tries++, idx++)
                {
                    Vector2Int cell = path[idx];
                    if (!reserved.Contains(cell))
                    {
                        m.blades.Add(cell);
                        reserved.Add(cell);
                        break;
                    }
                }
            }
        }

        // ---------- 8) Đồng xu: nửa trên đường đi ngắn nhất, nửa ngẫu nhiên ----------
        int target = def.coins;
        int onPath = target / 2;

        if (path.Count > 10 && onPath > 0)
        {
            int step = Mathf.Max(1, path.Count / onPath);
            for (int idx = step; idx < path.Count - 1 && m.coins.Count < onPath; idx += step)
            {
                Vector2Int cell = path[idx];
                if (!reserved.Contains(cell)) { m.coins.Add(cell); reserved.Add(cell); }
            }
        }
        var floorCells = new List<Vector2Int>();
        for (int x = 1; x < n - 1; x++)
            for (int z = 1; z < n - 1; z++)
                if (!m.wall[x, z] && !m.secretWall[x, z] && !reserved.Contains(new Vector2Int(x, z)))
                    floorCells.Add(new Vector2Int(x, z));
        int guard = 500;
        while (m.coins.Count < target && floorCells.Count > 0 && guard-- > 0)
        {
            Vector2Int cell = floorCells[rnd.Next(floorCells.Count)];
            floorCells.Remove(cell);
            m.coins.Add(cell);
            reserved.Add(cell);
        }

        // ---------- 9) Đèn chiếu sáng (rải đều, hạn chế số lượng) ----------
        int lampCap = def.theme == LevelTheme.Dark ? 15 : Mathf.Max(5, n / 2);
        floorCells.Clear();
        for (int x = 1; x < n - 1; x++)
            for (int z = 1; z < n - 1; z++)
                if (!m.wall[x, z] && !m.secretWall[x, z] && dist[x, z] > 0 &&
                    !reserved.Contains(new Vector2Int(x, z)))
                    floorCells.Add(new Vector2Int(x, z));
        guard = 600;
        while (m.lamps.Count < lampCap && floorCells.Count > 0 && guard-- > 0)
        {
            Vector2Int cell = floorCells[rnd.Next(floorCells.Count)];
            floorCells.Remove(cell);
            bool spaced = true;
            foreach (Vector2Int l in m.lamps)
                if (Mathf.Abs(l.x - cell.x) + Mathf.Abs(l.y - cell.y) < 5) { spaced = false; break; }
            if (!spaced) continue;
            m.lamps.Add(cell);
        }

        // ---------- 10) Cây (ngõ cụt trống), đá, cỏ ----------
        // Cây đứng ở ngõ cụt KHÔNG chứa vật phẩm để không chắn đường lấy đồ
        deadEnds.Clear();
        for (int x = 1; x < n - 1; x++)
            for (int z = 1; z < n - 1; z++)
            {
                Vector2Int cell = new Vector2Int(x, z);
                if (!m.wall[x, z] && dist[x, z] > 1 && FloorNeighbors(m, x, z) == 1 &&
                    !reserved.Contains(cell) && cell != m.exit)
                    deadEnds.Add(cell);
            }
        Shuffle(deadEnds, rnd);
        int treeCount = Mathf.Min(deadEnds.Count, n / 3);
        for (int i = 0; i < treeCount; i++) { m.trees.Add(deadEnds[i]); reserved.Add(deadEnds[i]); }

        // Đá trang trí
        floorCells.Clear();
        for (int x = 1; x < n - 1; x++)
            for (int z = 1; z < n - 1; z++)
                if (!m.wall[x, z] && !m.secretWall[x, z] && !reserved.Contains(new Vector2Int(x, z)))
                    floorCells.Add(new Vector2Int(x, z));
        int rockCount = Mathf.Min(floorCells.Count, n / 3);
        guard = 300;
        while (m.rocks.Count < rockCount && floorCells.Count > 0 && guard-- > 0)
        {
            Vector2Int cell = floorCells[rnd.Next(floorCells.Count)];
            floorCells.Remove(cell);
            m.rocks.Add(cell);
        }

        // Cỏ (không chắn đường, chỉ trang trí)
        guard = 800;
        while (m.grass.Count < n && floorCells.Count > 0 && guard-- > 0)
        {
            Vector2Int cell = floorCells[rnd.Next(floorCells.Count)];
            floorCells.Remove(cell);
            m.grass.Add(cell);
        }

        return m;
    }

    // ================= các hàm tiện ích =================

    /// <summary>Số ô sàn kề trực tiếp (4 hướng) của một ô.</summary>
    static int FloorNeighbors(GeneratedMaze m, int x, int z)
    {
        int count = 0;
        for (int i = 0; i < 4; i++)
        {
            int nx = x + Dx[i], nz = z + Dz[i];
            if (nx >= 0 && nx < m.size && nz >= 0 && nz < m.size && !m.wall[nx, nz]) count++;
        }
        return count;
    }

    static void Shuffle<T>(List<T> list, System.Random rnd)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
