using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MapDatabase", menuName = "Game/MapDatabase", order = 2)]
public class MapDatabase : ScriptableObject
{
    public List<MapData> maps;

    public GameObject GetMapPrefabByLevel(int level)
    {
        foreach (var map in maps)
        {
            if (map.level == level)
                return map.mapPrefab;
        }

        Debug.LogWarning("Không tìm thấy map cho level " + level);
        return null;
    }
}
