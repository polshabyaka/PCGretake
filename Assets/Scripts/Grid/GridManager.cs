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

    // two 2D arrays, one for data and one for visuals lookup by [x, y]
    // хм не уверена что так лучше всего, но пока работает проверю завтра
    public CellData[,] cells;
    public CellView[,] cellViews;

    // чтоб не потерять ссылку на игрока потом
    public Player player;

    void Start()
    {
        BuildGrid();
        MarkHomeCell();
        SpawnPlayer();
    }

    // loop through every cell and spawn a little sprite
    void BuildGrid()
    {
        cells = new CellData[width, height];
        cellViews = new CellView[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CellData data = new CellData(x, y);
                cells[x, y] = data;

                Vector3 pos = GridToWorld(x, y);
                CellView view = Instantiate(cellPrefab, pos, Quaternion.identity, mapRoot);
                view.name = "Cell_" + x + "_" + y; // чтобы в иерархии было понятно что где

                // шашечка по координатам, чтоб глазу было легче читать сетку
                bool alt = (x + y) % 2 == 1;
                view.SetType(data.type, alt);

                cellViews[x, y] = view;
            }
        }
    }

    // middle-ish cell is home
    // домик в серединке, от него потом игрок будет стартовать
    void MarkHomeCell()
    {
        int cx = width / 2;
        int cy = height / 2;

        cells[cx, cy].type = CellType.Home;
        cellViews[cx, cy].SetType(CellType.Home); // repaint it so we see it
    }

    // spawn the player right on the home cell
    // домик в серединке, там и появляемся
    void SpawnPlayer()
    {
        int cx = width / 2;
        int cy = height / 2;

        Vector3 pos = GridToWorld(cx, cy);
        player = Instantiate(playerPrefab, pos, Quaternion.identity);

        // отдаём игроку его стартовые координаты и ссылку на меня
        player.grid = this;
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
