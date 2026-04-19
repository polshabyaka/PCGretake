using UnityEngine;

// camera follows the player but never shows empty space outside the grid
// бегает за игроком, но не выходит за край карты
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    public GridManager grid; // drag GridManager here

    Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    // LateUpdate чтобы сначала игрок подвинулся, а потом камера
    void LateUpdate()
    {
        if (grid == null || grid.player == null) return; // ещё не успели заспавниться

        Vector3 target = grid.player.transform.position;

        // размер половины того что видно на экране
        float halfY = cam.orthographicSize;
        float halfX = halfY * cam.aspect;

        // реальные внешние края карты в мировых координатах
        float left = grid.GridToWorld(0, 0).x - 0.5f;
        float right = grid.GridToWorld(grid.width - 1, 0).x + 0.5f;
        float bottom = grid.GridToWorld(0, 0).y - 0.5f;
        float top = grid.GridToWorld(0, grid.height - 1).y + 0.5f;

        float x = target.x;
        float y = target.y;

        // если карта шире экрана — прижимаем к краю, иначе просто центр
        if ((right - left) > halfX * 2f)
            x = Mathf.Clamp(x, left + halfX, right - halfX);
        else
            x = (left + right) * 0.5f;

        if ((top - bottom) > halfY * 2f)
            y = Mathf.Clamp(y, bottom + halfY, top - halfY);
        else
            y = (top + bottom) * 0.5f;

        // z не трогаем, а то камера в 2D уедет и всё станет чёрным
        transform.position = new Vector3(x, y, transform.position.z);
    }
}