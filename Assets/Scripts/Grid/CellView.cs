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

    //цвет по типам клеточек
    public void SetType(CellType type, bool altTile = false)
    {
        // пока простой if, потом наверно switch будет когда типов больше
        if (type == CellType.Home)
            sprite.color = homeColor; // home sweet home
        else if (type == CellType.Forest)
            sprite.color = forestColor; // дремучий лес (・_・;)
        else
            sprite.color = altTile ? normalColorB : normalColor; // чередуем по шахматке
    }
}
