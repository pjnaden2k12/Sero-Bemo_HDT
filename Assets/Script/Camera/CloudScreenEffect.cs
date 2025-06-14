using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class CloudScreenEffect : MonoBehaviour
{
    [Header("Clouds")]
    public RectTransform leftCloud;
    public RectTransform rightCloud;

    [Header("Target Positions (Near Center)")]
    public float offsetFromCenter = 150f; // khoảng cách từ tâm ra ngoài

    [Header("Move Settings")]
    public float moveDuration = 0.6f;
    public float pauseTime = 0.4f;

    private Vector2 leftStartPos;
    private Vector2 rightStartPos;
    private Vector2 leftTargetPos;
    private Vector2 rightTargetPos;

    void Awake()
    {
        // Lưu lại vị trí gốc
        leftStartPos = leftCloud.anchoredPosition;
        rightStartPos = rightCloud.anchoredPosition;

        // Tính vị trí target gần giữa
        float centerX = 0f;
        leftTargetPos = new Vector2(centerX - offsetFromCenter, leftStartPos.y);
        rightTargetPos = new Vector2(centerX + offsetFromCenter, rightStartPos.y);
    }

    public IEnumerator EnterScreenEffect()
    {
        leftCloud.DOAnchorPos(leftTargetPos, moveDuration).SetEase(Ease.InOutSine);
        rightCloud.DOAnchorPos(rightTargetPos, moveDuration).SetEase(Ease.InOutSine);

        yield return new WaitForSeconds(moveDuration + pauseTime);
    }

    public IEnumerator ExitScreenEffect()
    {
        leftCloud.DOAnchorPos(leftStartPos, moveDuration).SetEase(Ease.InOutSine);
        rightCloud.DOAnchorPos(rightStartPos, moveDuration).SetEase(Ease.InOutSine);

        yield return new WaitForSeconds(moveDuration);
    }
}
