using UnityEngine;

public enum Direction { Up, Down, Left, Right }

[System.Serializable]
public class WormBodySegmentData
{
    public int id;
    public Direction directionFromPrevious;

    public WormBodySegmentData(int id, Direction direction)
    {
        this.id = id;
        this.directionFromPrevious = direction;
    }
}
