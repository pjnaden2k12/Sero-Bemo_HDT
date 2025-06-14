using UnityEngine;

[CreateAssetMenu(fileName = "MapData", menuName = "Game/MapData", order = 1)]
public class MapData : ScriptableObject
{
    public int level;
    public GameObject mapPrefab;
}
