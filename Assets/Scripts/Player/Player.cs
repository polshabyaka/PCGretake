using UnityEngine;

// simple grid-step movement, one cell per key press
// никаких диагоналей пока, только 4 стороны
public class Player : MonoBehaviour
{
    // где мы сейчас на сетке
    public int gridX;
    public int gridY;

    // ссылка на менеджер, даёт нам мир-координаты и размеры
    public GridManager grid;

    void Update()
    {
        // GetKeyDown — чтобы один тап = одна клетка (а то улетит)
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            TryMove(0, 1);
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            TryMove(0, -1);
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            TryMove(-1, 0);
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            TryMove(1, 0);
    }

    // двигаемся на одну клетку, если не вылезаем за край
    void TryMove(int dx, int dy)
    {
        int nx = gridX + dx;
        int ny = gridY + dy;

        // стенки карты, дальше нельзя
        if (nx < 0 || nx >= grid.width) return;
        if (ny < 0 || ny >= grid.height) return;

        // в лес не ходим, там волки ( ˘･_･˘ )
        if (grid.cells[nx, ny].type == CellType.Forest) return;

        gridX = nx;
        gridY = ny;
        transform.position = grid.GridToWorld(gridX, gridY); // прыг на новую клетку
    }
}
