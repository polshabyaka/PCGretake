using System.Collections.Generic;
using UnityEngine;

// builds the whole grid when the game starts
public class GridManager : MonoBehaviour
{
    // size of the map, easy to change in inspector
    public int width = 20;
    public int height = 20;

    public CellView cellPrefab; // CellView prefab here
    public Transform mapRoot;   //1MapRoot transform here
    public Player playerPrefab; // PlayerView prefab here (нужен Player скрипт на нём)

    // perlin settings, крутить в инспекторе пока не станет красиво
    public float noiseScale = 0.15f;      // чем меньше — тем крупнее пятна леса
    public float forestThreshold = 0.55f; // выше = меньше леса

    // PCG validation thresholds, tweak in inspector
    public int minReachableCells = 50;       // минимум проходимых клеток из дома
    public int minFarDistance = 8;           // в шагах BFS от дома, не манхэттен
    public int maxUnreachableWalkable = 20;  // сколько walkable-островков терпим
    public int maxRegenerateAttempts = 20;   // чтоб не крутиться бесконечно

    // PCG loot settings
    public LootItem lootPrefab;          // простой кружочек с LootItem скриптом
    public int commonMaxDistance = 4;    // d <= это -> common
    public int rareMinDistance = 10;     // d >= это -> rare (между ними uncommon)
    public int commonLootCount = 6;
    public int uncommonLootCount = 4;
    public int rareLootCount = 2;

    // fog of war: радиус открытия вокруг игрока (и вокруг дома на старте)
    public int revealRadius = 3;

    // two 2D arrays, one for data and one for visuals lookup by [x, y]
    // хм не уверена что так лучше всего, но пока работает проверю завтра
    public CellData[,] cells;
    public CellView[,] cellViews;

    // чтоб не потерять ссылку на игрока потом
    public Player player;

    // список лута, чтоб можно было чистить список при регенерации
    // (сами объекты уже снесутся вместе с детьми mapRoot)
    List<GameObject> activeLoot = new List<GameObject>();
    // параллельный список клеточек для лута — нужен чтобы прятать/показывать по туману
    List<Vector2Int> activeLootCells = new List<Vector2Int>();

    void Start()
    {
        // один и тот же путь для старта и для R, чтоб не дублировать логику
        Regenerate();
    }

    void Update()
    {
        // R — пересобрать карту заново (dev-кнопка)
        if (Input.GetKeyDown(KeyCode.R))
            Regenerate();
    }

    // главный пайплайн. всё происходит тут и только тут
    // и на Start, и по R — один путь
    void Regenerate()
    {
        ClearOldViews(); // сносит и клетки, и лут (они все дети mapRoot)

        bool accepted = false;
        for (int attempt = 0; attempt < maxRegenerateAttempts; attempt++)
        {
            // свежий рандом каждый раз — и на старте, и по R (и на каждом ретрае)
            float noiseOffsetX = Random.Range(0f, 9999f);
            float noiseOffsetY = Random.Range(0f, 9999f);

            GenerateCellData(noiseOffsetX, noiseOffsetY);
            CarveSafeHomeArea(); // домик и 4 соседа всегда проходимы
            MarkHomeCell();
            FillDistanceMap();   // нужно для валидации и для лута

            if (ValidateMap())
            {
                accepted = true;
                break;
            }
            // PCG: regenerate bad map — loop rolls a new seed on next iteration
        }

        if (!accepted)
            Debug.LogWarning("PCG: map validation never passed, using last map — loosen thresholds?");

        // визуалки создаём один раз, уже по принятой карте
        InstantiateCellViews();
        RepaintAllCells();
        PlacePlayerAtHome();
        PlaceLoot();

        // стартовая зона видимости вокруг дома — и сразу refresh визуалов/лута
        RevealArea(width / 2, height / 2, revealRadius);
    }

    // снести старые спрайты клеток и лут одним махом
    void ClearOldViews()
    {
        if (mapRoot == null) return;
        for (int i = mapRoot.childCount - 1; i >= 0; i--)
            Destroy(mapRoot.GetChild(i).gameObject);
    }

    // только данные, без визуалок — так дёшево ретраить при валидации
    void GenerateCellData(float offX, float offY)
    {
        cells = new CellData[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CellData data = new CellData(x, y);

                // шум в диапазоне ~[0..1], если больше порога — это лес
                float n = Mathf.PerlinNoise((x + offX) * noiseScale, (y + offY) * noiseScale);
                if (n > forestThreshold)
                    data.type = CellType.Forest;

                cells[x, y] = data;
            }
        }
    }

    // спавним CellView-ы по готовым данным
    void InstantiateCellViews()
    {
        cellViews = new CellView[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = GridToWorld(x, y);
                CellView view = Instantiate(cellPrefab, pos, Quaternion.identity, mapRoot);
                view.name = "Cell_" + x + "_" + y; // чтобы в иерархии было понятно что где
                cellViews[x, y] = view;
            }
        }
    }

    // home и 4 соседа должны быть walkable, чтобы игрок не застрял
    // хоум пока оставим Normal, сам Home тип поставит MarkHomeCell ниже
    void CarveSafeHomeArea()
    {
        int cx = width / 2;
        int cy = height / 2;

        ForceNormal(cx, cy);
        ForceNormal(cx + 1, cy);
        ForceNormal(cx - 1, cy);
        ForceNormal(cx, cy + 1);
        ForceNormal(cx, cy - 1);
    }

    // маленький помошник, чтоб не писать границы 5 раз
    void ForceNormal(int x, int y)
    {
        if (x < 0 || x >= width) return;
        if (y < 0 || y >= height) return;
        cells[x, y].type = CellType.Normal;
    }

    // middle-ish cell is home
    // домик в серединке, от него потом игрок будет стартовать
    void MarkHomeCell()
    {
        int cx = width / 2;
        int cy = height / 2;
        cells[cx, cy].type = CellType.Home;
    }

    // PCG: Dijkstra distance map — BFS from home (step cost = 1, so BFS == Dijkstra)
    void FillDistanceMap()
    {
        int cx = width / 2;
        int cy = height / 2;

        cells[cx, cy].distanceFromHome = 0;

        Queue<Vector2Int> frontier = new Queue<Vector2Int>();
        frontier.Enqueue(new Vector2Int(cx, cy));

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        while (frontier.Count > 0)
        {
            Vector2Int c = frontier.Dequeue();
            int cd = cells[c.x, c.y].distanceFromHome;

            for (int i = 0; i < 4; i++)
            {
                int nx = c.x + dx[i];
                int ny = c.y + dy[i];
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                if (cells[nx, ny].distanceFromHome >= 0) continue; // уже посетили
                if (cells[nx, ny].type == CellType.Forest) continue;

                cells[nx, ny].distanceFromHome = cd + 1;
                frontier.Enqueue(new Vector2Int(nx, ny));
            }
        }
    }

    // validation reads distances straight from cells
    bool ValidateMap()
    {
        int cx = width / 2;
        int cy = height / 2;

        // PCG: validation check — home not trapped
        bool hasOpenNeighbor =
            (cx + 1 < width  && cells[cx + 1, cy].distanceFromHome >= 0) ||
            (cx - 1 >= 0     && cells[cx - 1, cy].distanceFromHome >= 0) ||
            (cy + 1 < height && cells[cx, cy + 1].distanceFromHome >= 0) ||
            (cy - 1 >= 0     && cells[cx, cy - 1].distanceFromHome >= 0);
        if (!hasOpenNeighbor) return false;

        int reachableCount = 0;
        int furthestDistance = 0;
        int walkableUnreachable = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int d = cells[x, y].distanceFromHome;
                if (d >= 0)
                {
                    reachableCount++;
                    if (d > furthestDistance) furthestDistance = d;
                }
                else if (cells[x, y].type != CellType.Forest)
                {
                    walkableUnreachable++;
                }
            }
        }

        // PCG: validation check — enough reachable area
        if (reachableCount < minReachableCells) return false;

        // PCG: validation check — far reachable cell exists (BFS steps, not manhattan)
        if (furthestDistance < minFarDistance) return false;

        // PCG: validation check — not too many isolated walkable islands
        if (walkableUnreachable > maxUnreachableWalkable) return false;

        return true;
    }

    // один проход — перекрашиваем всё по финальным типам
    // так визуал точно совпадёт с данными (и после R тоже)
    void RepaintAllCells()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // шашечка по координатам, чтоб глазу было легче читать сетку
                bool alt = (x + y) % 2 == 1;
                cellViews[x, y].SetType(cells[x, y].type, alt, cells[x, y].visibility);
            }
        }
    }

    // fog of war: reveal pass — demote current Visible to Explored, promote new diamond to Visible
    public void RevealArea(int centerX, int centerY, int radius)
    {
        // pass A: всё что было Visible — теперь Explored (помним, но не видим)
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (cells[x, y].visibility == CellVisibility.Visible)
                    cells[x, y].visibility = CellVisibility.Explored;
            }
        }

        // pass B: круглик вокруг центра, плюс линия обзора — теперь Visible
        // PCG: circular visibility radius
        int r2 = radius * radius;
        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (dx * dx + dy * dy > r2) continue; // круглик, не квадрат и не ромб
                int nx = centerX + dx;
                int ny = centerY + dy;
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;

                // центр всегда видно, линию самим в себя тянуть нечего
                // лес по дороге — прячем всё что за ним (сам лес остаётся виден)
                if ((dx == 0 && dy == 0) || HasLineOfSight(centerX, centerY, nx, ny))
                    cells[nx, ny].visibility = CellVisibility.Visible;
            }
        }

        // перекрашиваем всю сетку — для 20x20 это копейки
        RepaintAllCells();

        // лут показываем только на клетках, которые СЕЙЧАС видны
        // explored и unseen — прячем
        for (int i = 0; i < activeLoot.Count; i++)
        {
            Vector2Int cellPos = activeLootCells[i];
            bool visible = cells[cellPos.x, cellPos.y].visibility == CellVisibility.Visible;
            if (activeLoot[i] != null)
                activeLoot[i].SetActive(visible);
        }
    }

    // PCG: line of sight (Bresenham)
    // идём по клеткам от центра к цели; любой лес "посередине" перекрывает обзор
    // саму стартовую и финальную клетку на лес не проверяем,
    // так что дерево-блокер остаётся видимым, а то что ЗА ним — уже нет
    bool HasLineOfSight(int cx, int cy, int tx, int ty)
    {
        if (cx == tx && cy == ty) return true; // подстраховка, чтоб не зациклиться

        int dx = Mathf.Abs(tx - cx);
        int dy = Mathf.Abs(ty - cy);
        int sx = cx < tx ? 1 : -1;
        int sy = cy < ty ? 1 : -1;
        int err = dx - dy;

        int x = cx;
        int y = cy;

        while (true)
        {
            // шаг по Брезенхэму
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x += sx; }
            if (e2 <  dx) { err += dx; y += sy; }

            if (x == tx && y == ty) return true; // дошли, цель сама себя не блочит
            if (cells[x, y].type == CellType.Forest) return false; // промежуточный лес — стоп
        }
    }

    // PCG: loot placement by distance — split reachable cells into rarity bands
    void PlaceLoot()
    {
        // сами объекты уже уничтожены вместе с детьми mapRoot в ClearOldViews,
        // просто очищаем списки, чтоб не держать мёртвые ссылки
        activeLoot.Clear();
        activeLootCells.Clear();

        if (lootPrefab == null) return; // забыли кинуть префаб — просто ничего не ставим

        List<Vector2Int> commonCells = new List<Vector2Int>();
        List<Vector2Int> uncommonCells = new List<Vector2Int>();
        List<Vector2Int> rareCells = new List<Vector2Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int d = cells[x, y].distanceFromHome;
                if (d <= 0) continue; // пропускаем Home (d==0) и недостижимые (d==-1)

                Vector2Int p = new Vector2Int(x, y);
                if (d <= commonMaxDistance)
                    commonCells.Add(p);
                else if (d >= rareMinDistance)
                    rareCells.Add(p);
                else
                    uncommonCells.Add(p);
            }
        }

        SpawnLootBand(commonCells, commonLootCount, LootRarity.Common);
        SpawnLootBand(uncommonCells, uncommonLootCount, LootRarity.Uncommon);
        SpawnLootBand(rareCells, rareLootCount, LootRarity.Rare);
    }

    // берём count случайных клеток без повторов и ставим на них лут
    void SpawnLootBand(List<Vector2Int> band, int count, LootRarity rarity)
    {
        int actual = Mathf.Min(count, band.Count);
        for (int i = 0; i < actual; i++)
        {
            // swap-with-random — достаём уникальные клетки, никто не дублируется
            int j = Random.Range(i, band.Count);
            Vector2Int picked = band[j];
            band[j] = band[i];

            // чуть вытаскиваем вперёд по z, чтоб кружок рисовался поверх клетки
            Vector3 cellPos = GridToWorld(picked.x, picked.y);
            Vector3 pos = new Vector3(cellPos.x, cellPos.y, -0.1f);

            LootItem loot = Instantiate(lootPrefab, pos, Quaternion.identity, mapRoot);
            loot.SetRarity(rarity);
            loot.gameObject.SetActive(false); // пока туман — прячем сразу, RevealArea откроет что надо
            activeLoot.Add(loot.gameObject);
            activeLootCells.Add(picked);
        }
    }

    // spawn the player right on the home cell, or teleport him back there on R
    // домик в серединке, там и появляемся
    void PlacePlayerAtHome()
    {
        int cx = width / 2;
        int cy = height / 2;
        Vector3 pos = GridToWorld(cx, cy);

        if (player == null)
        {
            player = Instantiate(playerPrefab, pos, Quaternion.identity);
            player.grid = this;
        }
        else
        {
            // на R просто телепортируем обратно домой
            player.transform.position = pos;
        }

        player.gridX = cx;
        player.gridY = cy;
        player.CancelPath(); // на R не хотим чтоб он дошагивал старый маршрут
    }

    // grid coords -> world position
    // shift so the grid sits nicely around (0,0)
    // (иначе всё уезжает в правый верх, некрасиво)
    public Vector3 GridToWorld(int x, int y)
    {
        float offsetX = (width - 1) * 0.5f;
        float offsetY = (height - 1) * 0.5f;
        return new Vector3(x - offsetX, y - offsetY, 0f);
    }

    // обратная операция для GridToWorld — мир -> клетка
    // true если попало внутрь сетки, false если мимо (клик за пределами)
    public bool TryWorldToGrid(Vector3 world, out int gx, out int gy)
    {
        float offsetX = (width - 1) * 0.5f;
        float offsetY = (height - 1) * 0.5f;
        gx = Mathf.RoundToInt(world.x + offsetX);
        gy = Mathf.RoundToInt(world.y + offsetY);
        return gx >= 0 && gx < width && gy >= 0 && gy < height;
    }
}
