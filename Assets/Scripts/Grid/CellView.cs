using UnityEngine;

// the visual half of a cell, just paints itself a color
public class CellView : MonoBehaviour
{
    public SpriteRenderer sprite; // drag SpriteRenderer

    // colors per type, tweak in inspector if you want
    // хм, может потом заменю на SO чтобы цвета в одном месте были
    public Color normalColor = new Color(0.80f, 0.92f, 0.87f);
    public Color normalColorB = new Color(0.72f, 0.86f, 0.80f);   // вариант B чтоб сетку видно было, чуть темнее
    public Color homeColor = Color.yellow;
    public Color forestColor = new Color(0.20f, 0.45f, 0.25f); // тёмно-зелёный, как лесок
    public Color hiddenColor = new Color(0.10f, 0.10f, 0.12f); // почти чёрный — никогда не видели

    // explored tweakables: затемняем базовый цвет типа и делаем полупрозрачным
    // (типо "помним что тут лес, но точно сейчас не видим")
    [Range(0f, 1f)] public float exploredDarken = 0.45f; // 0 = чёрный, 1 = как visible
    [Range(0f, 1f)] public float exploredAlpha = 0.7f;

    //цвет по типам клеточек с учётом тумана войны
    public void SetType(CellType type, bool altTile = false, CellVisibility visibility = CellVisibility.Visible)
    {
        // ни разу не видели — просто чёрненько
        if (visibility == CellVisibility.Unseen)
        {
            sprite.color = hiddenColor;
            return;
        }

        // берём базовый цвет типа, чтобы лес/дом/обычные всё ещё отличались
        Color baseColor = GetBaseColor(type, altTile);

        if (visibility == CellVisibility.Explored)
        {
            // тусклая версия того же цвета + немножко прозрачности
            Color dim = new Color(
                baseColor.r * exploredDarken,
                baseColor.g * exploredDarken,
                baseColor.b * exploredDarken,
                exploredAlpha
            );
            sprite.color = dim;
            return;
        }

        // currently visible — цвет как есть, полностью непрозрачный
        sprite.color = baseColor;
    }

    // маленький помошник, чтоб не дублировать логику типов
    Color GetBaseColor(CellType type, bool altTile)
    {
        if (type == CellType.Home) return homeColor;         // home sweet home
        if (type == CellType.Forest) return forestColor;     // дремучий лес (・_・;)
        return altTile ? normalColorB : normalColor;         // шашечка по координатам
    }
}
