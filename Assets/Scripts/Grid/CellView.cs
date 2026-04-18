using UnityEngine;

// the visual half of a cell, just paints itself a color
public class CellView : MonoBehaviour
{
    public SpriteRenderer sprite; // drag the prefab's SpriteRenderer here

    // colors per type, tweak in inspector if you want
    // хм, может потом заменю на SO чтобы цвета в одном месте были
    public Color normalColor = Color.white;                       // обычная клетка, вариант A
    public Color normalColorB = new Color(0.92f, 0.92f, 0.92f);   // вариант B для шашечек, чуть темнее
    public Color homeColor = Color.yellow;

    // pick a color based on what kind of cell this is
    // altTile is only used for Normal cells — это та самая шашечка
    public void SetType(CellType type, bool altTile = false)
    {
        // пока простой if, потом наверно switch будет когда типов больше
        if (type == CellType.Home)
            sprite.color = homeColor; // home sweet home
        else
            sprite.color = altTile ? normalColorB : normalColor; // чередуем по шахматке
    }
}
