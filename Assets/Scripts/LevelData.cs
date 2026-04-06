using UnityEngine;
using System.Collections.Generic;

public enum CellType
{
    Empty,
    Floor,
    Wall,
    Player,
    Furniture,
    Goal
}

[System.Serializable]
public struct CellInfo
{
    public CellType cellType;
    public Color color;
    public Sprite icon;
}

[System.Serializable]
public struct FurnitureMetadata
{
    public int cellIndex;
    public string furnitureID;
}

[CreateAssetMenu(fileName = "NewLevel", menuName = "Sokoban/Level Data")]
public class LevelData : ScriptableObject
{
    public int width = 10;
    public int height = 10;

    //Usando 1D array para que Unity lo pueda guardar
    public CellType[] cells;
    public List<FurnitureMetadata> furnitureList = new List<FurnitureMetadata>();

    public void Resize(int w, int h, bool clear = false)
    {
        //Mantener la data ya existente
        CellType[] oldCells = cells;
        int oldW = width;
        int oldH = height;


        List<FurnitureMetadata> oldFurniture = new List<FurnitureMetadata>(furnitureList);

        //Cambiar el tamaño
        width = w;
        height = h;
        cells = new CellType[width * height];
        furnitureList.Clear();
        if (clear) return;


        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                //Crea un nuevo grid como si fuera un array bidimensional
                int newIdx = x + y * width;

                if (x < oldW && y < oldH)
                {
                    int oldIdx = x + y * oldW;
                    cells[newIdx] = oldCells[oldIdx];

                    //Mueve la furniture...
                    var meta = oldFurniture.Find(m => m.cellIndex == oldIdx);
                    if (!string.IsNullOrEmpty(meta.furnitureID))
                    {
                        furnitureList.Add(new FurnitureMetadata
                        {
                            cellIndex = newIdx,
                            furnitureID = meta.furnitureID
                        });
                    }
                }
                else
                {
                    cells[newIdx] = CellType.Empty;
                }
            }
        }
    }

    //FURNITURE
    public void SetFurnitureMetadata(int index, string id)
    {
        furnitureList.RemoveAll(m => m.cellIndex == index);

        if (!string.IsNullOrEmpty(id))
        {
            furnitureList.Add(new FurnitureMetadata { cellIndex = index, furnitureID = id });
        }
    }

    public string GetFurnitureID(int index)
    {
        var meta = furnitureList.Find(m => m.cellIndex == index);
        return meta.furnitureID;
    }
}