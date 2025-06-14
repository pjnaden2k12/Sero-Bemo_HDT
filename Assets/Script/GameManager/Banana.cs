using UnityEngine;
using DG.Tweening;

public class Banana : PushableItem
{
    public void Eat()
    {       
        KillTweens();
        gameObject.SetActive(false);
    }
}
