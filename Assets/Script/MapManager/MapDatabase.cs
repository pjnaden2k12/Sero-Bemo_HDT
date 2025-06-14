using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MapDatabase", menuName = "Game/MapDatabase")]
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
        return null;
    }

    public MapData GetMapDataByLevel(int level)
    {
        foreach (var map in maps)
        {
            if (map.level == level)
                return map;
        }
        return null;
    }
}