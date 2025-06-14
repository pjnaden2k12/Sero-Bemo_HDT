using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MapData
{
    public int level;
    public GameObject mapPrefab;
    public int initialBodyCount;
    public List<string> moveSequence;
}