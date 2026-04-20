// little list of cell types, will grow later (water, forest, etc)
// пока три, дальше добавим ещё когда понадобится
public enum CellType
{
    Normal,
    Home,
    Forest // деревья, ходить нельзя
}

// три состояния тумана войны
// (никогда не видели / видели раньше / сейчас в поле зрения)
public enum CellVisibility
{
    Unseen,
    Explored,
    Visible
}

// tiny data bag for one cell, no Unity stuff here on purpose
// (так легче потом юзать для Dijkstra и всего такого)
public class CellData
{
    public int x;
    public int y;
    public CellType type;

    // шаги BFS от дома; -1 = недостижимо или лес
    public int distanceFromHome = -1;

    // туман войны: три состояния — никогда/раньше/сейчас
    public CellVisibility visibility = CellVisibility.Unseen;

    public CellData(int x, int y)
    {
        this.x = x;
        this.y = y;
        this.type = CellType.Normal; // по умолчанию обычная клетка
    }
}
