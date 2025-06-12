using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WormLevelData", menuName = "Worm/Worm Level Data")]
public class WormLevelData : ScriptableObject
{
    public int levelId;
    public List<WormBodySegmentData> segments = new List<WormBodySegmentData>();
}
