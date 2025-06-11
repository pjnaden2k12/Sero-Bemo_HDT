using UnityEngine;
using DG.Tweening;

public class PushableItem : MonoBehaviour
{
    public LayerMask obstacleLayer;
    public LayerMask noMoveLayer;

    public float moveDuration = 0.1f;

    public Transform visualTransform;   // Visual lơ lửng
    public Transform shadowTransform;   // Shadow co giãn

    private float floatHeight = 0.2f;
    private float floatDuration = 1f;

    private Tween floatTweenVisual;
    private Tween floatTweenShadow;

    protected virtual void Start()
    {
        StartFloating();
    }
       
    void StartFloating()
    {
        if (visualTransform != null)
        {
            floatTweenVisual = visualTransform.DOLocalMoveY(floatHeight, floatDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
        if (shadowTransform != null)
        {
            floatTweenShadow = shadowTransform.DOScale(new Vector3(0.8f, 0.8f, 1f), floatDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
    }

    public virtual bool TryPush(Vector3 direction)
    {
        Vector3 targetPos = transform.position + direction;
        Collider2D hit = Physics2D.OverlapCircle(targetPos, 0.1f, obstacleLayer | noMoveLayer);

        if (hit != null)
        {
            if (((1 << hit.gameObject.layer) & noMoveLayer) != 0)
                return false;

            PushableItem nextItem = hit.GetComponent<PushableItem>();
            if (nextItem != null)
            {
                bool nextPushed = nextItem.TryPush(direction);
                if (!nextPushed)
                    return false;
            }
            else
            {
                return false;
            }
        }

        transform.DOMove(targetPos, moveDuration).SetEase(Ease.Linear);
        return true;
    }

    protected void KillTweens()
    {
        floatTweenVisual?.Kill();
        floatTweenShadow?.Kill();
    }
    public virtual void OnEaten()
    {
        KillTweens();
        Destroy(gameObject);
    }


}