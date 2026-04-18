// little list of cell types, will grow later (water, forest, etc)
// пока только два, потом добавлю больше когда будет генерация
public enum CellType
{
    Normal,
    Home
}

// tiny data bag for one cell, no Unity stuff here on purpose
// (так легче потом юзать для Dijkstra и всего такого)
public class CellData
{
    public int x;
    public int y;
    public CellType type;

    public CellData(int x, int y)
    {
        this.x = x;
        this.y = y;
        this.type = CellType.Normal; // по умолчанию обычная клетка
    }
}
