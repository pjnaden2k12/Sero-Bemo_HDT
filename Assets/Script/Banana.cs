using UnityEngine;
using DG.Tweening;

public class Banana : PushableItem
{
    // Thời gian delay trước khi destroy chuối, cho tween chạy xong
    

    public void Eat()
    {
        // Kill tween đang chạy để tránh lỗi
        KillTweens();

        // Delay destroy sau một khoảng thời gian
        Destroy(gameObject);
    }
}
