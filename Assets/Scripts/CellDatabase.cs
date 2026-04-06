using UnityEngine;

[CreateAssetMenu(menuName = "Sokoban/Cell Database")]
public class CellDatabase : ScriptableObject
{
    public CellInfo[] cells;
    public Color GetColor(CellType type)
    {
        foreach (var c in cells)
        {
            if (c.cellType == type)
                return c.color;
        }

        return Color.black;
    }
}