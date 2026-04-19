using UnityEngine;

// grid-step movement with a little slide between cells
// зажал клавишу — шагаем дальше, никаких диагоналей
public class Player : MonoBehaviour
{
    // где мы сейчас на сетке
    public int gridX;
    public int gridY;

    // ссылка на менеджер, даёт нам мир-координаты и размеры
    public GridManager grid;

    // сколько клеток в секунду проезжаем, крутить в инспекторе
    public float moveSpeed = 6f;

    // состояние скольжения между клетками
    bool isMoving;
    Vector3 moveFrom;
    Vector3 moveTarget;
    float moveT;

    void Update()
    {
        // если извне телепортнули (R regenerate) — слайд отменяем
        // иначе он дотянет нас до старой цели которой уже нет ( °-°)
        if (isMoving && grid != null)
        {
            Vector3 expected = grid.GridToWorld(gridX, gridY);
            if ((expected - moveTarget).sqrMagnitude > 0.0001f)
            {
                isMoving = false;
                transform.position = expected;
            }
        }

        if (isMoving)
        {
            // плавно едем от одной клетки к другой
            moveT += Time.deltaTime * moveSpeed;
            if (moveT >= 1f)
            {
                transform.position = moveTarget; // ровно на клеточке стоим
                isMoving = false;
            }
            else
            {
                transform.position = Vector3.Lerp(moveFrom, moveTarget, moveT);
            }
            return; // пока едем — клавиши не слушаем, а то застрянем посерединке
        }

        // GetKey, чтобы зажатие продолжало шагать без долбёжки по клавише
        // вертикаль первее, чтоб случайно не поехать по диагонали
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            StartStep(0, 1);
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            StartStep(0, -1);
        else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            StartStep(-1, 0);
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            StartStep(1, 0);
    }

    // начинаем шаг на одну клеточку, если можно
    void StartStep(int dx, int dy)
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

        moveFrom = transform.position;
        moveTarget = grid.GridToWorld(gridX, gridY);
        moveT = 0f;
        isMoving = true;
    }
}
