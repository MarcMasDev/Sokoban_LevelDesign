using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct FurnitureEntry
{
    public string id;
    public Sprite icon;
}

[CreateAssetMenu(fileName = "FurnitureDatabase", menuName = "Sokoban/Furniture Database")]
public class FurnitureDatabase : ScriptableObject
{
    public List<FurnitureEntry> entries = new List<FurnitureEntry>();
}