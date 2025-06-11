using UnityEngine;
using DG.Tweening;

public class Medicine : PushableItem
{
    public void Eat()
    {
        KillTweens();
        Destroy(gameObject);
    }
}
