using UnityEngine;

// builds the whole grid when the game starts
public class GridManager : MonoBehaviour
{
    // size of the map, easy to change in inspector
    public int width = 20;
    public int height = 20;

    public CellView cellPrefab; // drag the CellView prefab here
    public Transform mapRoot;   // drag the MapRoot transform here

    // two 2D arrays, one for data and one for visuals (lookup by [x, y])
    // хм, не уверена что так лучше всего, но пока работает — проверю завтра
    public CellData[,] cells;
    public CellView[,] cellViews;

    void Start()
    {
        BuildGrid();
        MarkHomeCell();
    }

    // loop through every cell and spawn a little sprite
    void BuildGrid()
    {
        cells = new CellData[width, height];
        cellViews = new CellView[width, height];

        // shift so the grid sits nicely around (0,0)
        // (иначе всё уезжает в правый верх, некрасиво)
        float offsetX = (width - 1) * 0.5f;
        float offsetY = (height - 1) * 0.5f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CellData data = new CellData(x, y);
                cells[x, y] = data;

                Vector3 pos = new Vector3(x - offsetX, y - offsetY, 0f);
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
}
