using System.Collections.Generic;
using UnityEngine;

// маленький A* для click-to-move
// сетка 20x20, так что без кучи/heap — линейным поиском по open-списку норм
public static class Pathfinder
{
    // диагональ длиннее чем кардинальный шаг, дальше её используем и в цене, и в эвристике
    const float DIAG = 1.41421f;

    // PCG: A* pathfinding
    // возвращает список клеток от старта до цели включительно, или null если пути нет
    public static List<Vector2Int> FindPath(GridManager grid, int sx, int sy, int gx, int gy)
    {
        // если цель та же клетка или не ходибельная — молча ничего не делаем
        if (!IsWalkable(grid, sx, sy)) return null;
        if (!IsWalkable(grid, gx, gy)) return null;
        if (sx == gx && sy == gy) return null;

        int w = grid.width;
        int h = grid.height;

        // параллельные массивы по [x,y] — проще чем словари для маленькой сетки
        float[,] gScore = new float[w, h];
        float[,] fScore = new float[w, h];
        Vector2Int[,] cameFrom = new Vector2Int[w, h];
        bool[,] closed = new bool[w, h];
        bool[,] inOpen = new bool[w, h];

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                gScore[x, y] = float.PositiveInfinity;
                fScore[x, y] = float.PositiveInfinity;
                cameFrom[x, y] = new Vector2Int(-1, -1); // -1 = "родителя нет"
            }
        }

        List<Vector2Int> open = new List<Vector2Int>();
        gScore[sx, sy] = 0f;
        fScore[sx, sy] = Heuristic(sx, sy, gx, gy);
        open.Add(new Vector2Int(sx, sy));
        inOpen[sx, sy] = true;

        // 8 соседей: 4 кардинальных + 4 диагональных
        int[] dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        int[] dy = { 0, 0, 1, -1, 1, -1, 1, -1 };

        while (open.Count > 0)
        {
            // берём клетку с минимальным f — линейный скан, норм для 20x20
            int bestIdx = 0;
            float bestF = fScore[open[0].x, open[0].y];
            for (int i = 1; i < open.Count; i++)
            {
                float f = fScore[open[i].x, open[i].y];
                if (f < bestF) { bestF = f; bestIdx = i; }
            }

            Vector2Int cur = open[bestIdx];

            // дошли до цели — собираем путь назад по cameFrom
            if (cur.x == gx && cur.y == gy)
                return Reconstruct(cameFrom, cur);

            open.RemoveAt(bestIdx);
            inOpen[cur.x, cur.y] = false;
            closed[cur.x, cur.y] = true;

            for (int i = 0; i < 8; i++)
            {
                int nx = cur.x + dx[i];
                int ny = cur.y + dy[i];
                if (!IsWalkable(grid, nx, ny)) continue;
                if (closed[nx, ny]) continue;

                bool diag = dx[i] != 0 && dy[i] != 0;
                if (diag)
                {
                    // PCG: prevent diagonal corner cutting
                    // чтобы нельзя было пройти "сквозь угол" между двумя лесами
                    if (!IsWalkable(grid, cur.x + dx[i], cur.y)) continue;
                    if (!IsWalkable(grid, cur.x, cur.y + dy[i])) continue;
                }

                float stepCost = diag ? DIAG : 1f;
                float tentative = gScore[cur.x, cur.y] + stepCost;
                if (tentative < gScore[nx, ny])
                {
                    cameFrom[nx, ny] = cur;
                    gScore[nx, ny] = tentative;
                    fScore[nx, ny] = tentative + Heuristic(nx, ny, gx, gy);
                    if (!inOpen[nx, ny])
                    {
                        open.Add(new Vector2Int(nx, ny));
                        inOpen[nx, ny] = true;
                    }
                }
            }
        }

        // open опустел, цель недостижима
        return null;
    }

    // walkable для пасфайндера: в границах, не лес, и хотя бы когда-то видели
    // (на Unseen клетки кликать нельзя — мы их как бы и не знаем ещё)
    static bool IsWalkable(GridManager grid, int x, int y)
    {
        if (x < 0 || x >= grid.width || y < 0 || y >= grid.height) return false;
        CellData c = grid.cells[x, y];
        if (c.type == CellType.Forest) return false;
        if (c.visibility == CellVisibility.Unseen) return false;
        return true;
    }

    // PCG: heuristic — octile distance
    // согласована с ценой шагов (1 и ~1.414), так что A* оптимален
    static float Heuristic(int x, int y, int gx, int gy)
    {
        int adx = Mathf.Abs(x - gx);
        int ady = Mathf.Abs(y - gy);
        int min = Mathf.Min(adx, ady);
        int max = Mathf.Max(adx, ady);
        return (max - min) + DIAG * min;
    }

    // восстанавливаем путь от цели к старту и разворачиваем
    static List<Vector2Int> Reconstruct(Vector2Int[,] cameFrom, Vector2Int end)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int cur = end;
        // -1 в x значит "родителя нет" — значит дошли до старта
        while (cur.x >= 0)
        {
            path.Add(cur);
            cur = cameFrom[cur.x, cur.y];
        }
        path.Reverse();
        return path;
    }
}
