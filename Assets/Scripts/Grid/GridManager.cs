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

        // свежий рандом каждый раз — и на старте, и по R
        // берём большой offset чтоб perlin выдавал разные куски шума
        float noiseOffsetX = Random.Range(0f, 9999f);
        float noiseOffsetY = Random.Range(0f, 9999f);

        BuildCells(noiseOffsetX, noiseOffsetY);
        CarveSafeHomeArea(); // домик и 4 соседа всегда проходимы
        MarkHomeCell();

        // ВАЖНО: перекрашиваем ВСЕ клетки по финальным данным
        // (а то forest в safe-зоне останется зелёным визуально)
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

    // создать данные + визуалки, типы ставим по perlin
    void BuildCells(float offX, float offY)
    {
        cells = new CellData[width, height];
        cellViews = new CellView[width, height];

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

                Vector3 pos = GridToWorld(x, y);
                CellView view = Instantiate(cellPrefab, pos, Quaternion.identity, mapRoot);
                view.name = "Cell_" + x + "_" + y; // чтобы в иерархии было понятно что где
                cellViews[x, y] = view;
                // цвет поставим потом в RepaintAllCells, чтоб не рисовать дважды
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
