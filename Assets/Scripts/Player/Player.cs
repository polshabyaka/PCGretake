using System.Collections.Generic;
using UnityEngine;

// grid-step movement with a little slide between cells
// зажал клавишу — шагаем дальше; ЛКМ по клетке — автопуть через A*
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

    // очередь клеток от A* — по одной за слайд
    List<Vector2Int> path;
    int pathIndex;

    // PCG: level completion — ставится в true когда все монетки собраны
    // пока true — не слушаем никакой ввод (клавиши, мышь, шаги A*)
    public bool inputLocked;

    // кэшируем камеру один раз, чтоб не дёргать Camera.main каждый клик
    Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

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
            return; // пока едем — ничего не слушаем, а то застрянем посерединке
        }

        // after Completed! — dno of input, тихо стоим пока LevelGoal не разблокирует
        if (inputLocked) return;

        // ЛКМ по клетке — прокладываем маршрут A*
        if (Input.GetMouseButtonDown(0) && cam != null && grid != null)
            TryClickToMove();

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

        // если клавиш нет и есть очередь из A* — делаем следующий шаг
        if (!isMoving && path != null && pathIndex < path.Count)
        {
            Vector2Int next = path[pathIndex];
            pathIndex++;
            StartStepTo(next.x, next.y);
            if (pathIndex >= path.Count) CancelPath(); // дошли, чистим
        }
    }

    // мышь -> клетка -> A* -> запомнить маршрут
    void TryClickToMove()
    {
        Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
        world.z = 0f;

        if (!grid.TryWorldToGrid(world, out int tx, out int ty)) return;

        List<Vector2Int> found = Pathfinder.FindPath(grid, gridX, gridY, tx, ty);
        if (found == null || found.Count <= 1) return; // нет пути или та же клетка

        path = found;
        pathIndex = 1; // нулевой элемент — это мы сами, пропускаем
    }

    // манул-шаг с клавиатуры — он всегда перебивает автопуть
    void StartStep(int dx, int dy)
    {
        CancelPath();
        StartStepTo(gridX + dx, gridY + dy);
    }

    // общий старт слайда на конкретную соседнюю клетку
    // используется и клавишами, и очередью A*
    void StartStepTo(int nx, int ny)
    {
        // стенки карты, дальше нельзя
        if (nx < 0 || nx >= grid.width) return;
        if (ny < 0 || ny >= grid.height) return;

        // в лес не ходим, там волки ( ˘･_･˘ )
        if (grid.cells[nx, ny].type == CellType.Forest) return;

        gridX = nx;
        gridY = ny;

        // туман разгоняем сразу при шаге, чтоб чувствовалось отзывчиво
        grid.RevealArea(gridX, gridY, grid.revealRadius);

        moveFrom = transform.position;
        moveTarget = grid.GridToWorld(gridX, gridY);
        moveT = 0f;
        isMoving = true;
    }

    // очистить очередь автопути — зовётся и самим игроком, и GridManager при R
    public void CancelPath()
    {
        path = null;
        pathIndex = 0;
    }

    // hard stop: cancel A* queue AND kill current slide
    // нужно чтобы после Completed! игрок не доезжал и не догонял старый путь
    public void ForceStop()
    {
        CancelPath();
        if (isMoving)
        {
            transform.position = moveTarget; // доезжаем ровно на клетку, чтоб не висеть между
            isMoving = false;
        }
        moveT = 0f;
    }
}
