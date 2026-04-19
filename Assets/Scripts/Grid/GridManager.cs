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

    // two 2D arrays, one for data and one for visuals lookup by [x, y]
    // хм не уверена что так лучше всего, но пока работает проверю завтра
    public CellData[,] cells;
    public CellView[,] cellViews;

    // чтоб не потерять ссылку на игрока потом
    public Player player;

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
        ClearOldViews();

        bool accepted = false;
        for (int attempt = 0; attempt < maxRegenerateAttempts; attempt++)
        {
            // свежий рандом каждый раз — и на старте, и по R (и на каждом ретрае)
            float noiseOffsetX = Random.Range(0f, 9999f);
            float noiseOffsetY = Random.Range(0f, 9999f);

            GenerateCellData(noiseOffsetX, noiseOffsetY);
            CarveSafeHomeArea(); // домик и 4 соседа всегда проходимы
            MarkHomeCell();

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
    }

    // снести старые спрайты клеток, иначе при R они будут накладываться
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

    // PCG: reachable area check
    // flood fill from home; forests block, 4-directional; tracks BFS step depth
    void BFSFromHome(out bool[,] visited, out int reachableCount, out int furthestDistance)
    {
        visited = new bool[width, height];
        reachableCount = 0;
        furthestDistance = 0;

        int cx = width / 2;
        int cy = height / 2;

        int[,] depth = new int[width, height];
        Queue<Vector2Int> frontier = new Queue<Vector2Int>();

        visited[cx, cy] = true;
        frontier.Enqueue(new Vector2Int(cx, cy));
        reachableCount = 1;

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        while (frontier.Count > 0)
        {
            Vector2Int c = frontier.Dequeue();
            int cd = depth[c.x, c.y];
            if (cd > furthestDistance) furthestDistance = cd;

            for (int i = 0; i < 4; i++)
            {
                int nx = c.x + dx[i];
                int ny = c.y + dy[i];
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                if (visited[nx, ny]) continue;
                if (cells[nx, ny].type == CellType.Forest) continue;

                visited[nx, ny] = true;
                depth[nx, ny] = cd + 1;
                frontier.Enqueue(new Vector2Int(nx, ny));
                reachableCount++;
            }
        }
    }

    // run BFS once, then check rules against its results
    bool ValidateMap()
    {
        bool[,] visited;
        int reachableCount, furthestDistance;
        BFSFromHome(out visited, out reachableCount, out furthestDistance);

        int cx = width / 2;
        int cy = height / 2;

        // PCG: validation check — home not trapped
        bool hasOpenNeighbor =
            (cx + 1 < width  && visited[cx + 1, cy]) ||
            (cx - 1 >= 0     && visited[cx - 1, cy]) ||
            (cy + 1 < height && visited[cx, cy + 1]) ||
            (cy - 1 >= 0     && visited[cx, cy - 1]);
        if (!hasOpenNeighbor) return false;

        // PCG: validation check — enough reachable area
        if (reachableCount < minReachableCells) return false;

        // PCG: validation check — far reachable cell exists (BFS steps, not manhattan)
        if (furthestDistance < minFarDistance) return false;

        // PCG: validation check — not too many isolated walkable islands
        int walkableUnreachable = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (cells[x, y].type != CellType.Forest && !visited[x, y])
                    walkableUnreachable++;
            }
        }
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
                cellViews[x, y].SetType(cells[x, y].type, alt);
            }
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
}
