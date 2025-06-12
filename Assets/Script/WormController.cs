using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class WormMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveStepX = 1.0f;
    public float moveStepY = 1.0f;

    [Header("Body Prefabs")]
    public GameObject bodyPrefab;
    public GameObject tailPrefab;
    public int initialBodyCount = 3;

    [Header("Effects")]
    public GameObject smokeEffectPrefab;

    [Header("Sprites - Body Shapes")]
    public Sprite cornerTopRight;
    public Sprite cornerTopLeft;
    public Sprite cornerBottomLeft;
    public Sprite cornerBottomRight;
    public Sprite bodyHorizontal;
    public Sprite bodyVertical;

    [Header("Face Components")]
    public SpriteRenderer headFaceRenderer;
    public SpriteRenderer mouthRenderer;
    public SpriteRenderer rainbowRenderer;

    [Header("Face Sprites")]
    public Sprite faceNormal;
    public Sprite faceHappy;
    public Sprite faceEatMedicine;
    public Sprite faceEatMedicinev2;
    public Sprite faceFallEat;
    public Sprite faceFallEatv2;

    [Header("Tags")]
    public string tagBanana = "Banana";
    public string tagMedicine = "Medicine";

    [Header("Layer Masks")]
    public LayerMask NoMoveLayer;
    public LayerMask WormBodyLayer;
    //public LayerMask fallMapLayer;

    // Internal states
    private bool canMove = true;
    private bool isReversed = false;

    private List<Transform> bodyParts = new List<Transform>();
    private List<Vector3> positionHistory = new List<Vector3>();
    private List<Direction> directionHistory = new List<Direction>();
    private Direction currentDirection;
    private Vector3 movementDirection;

    private Tween flyTween;
    private List<Tween> flyingItemTweens = new List<Tween>();

    private enum Direction { Up, Down, Left, Right }

    void Start()
    {
        currentDirection = Direction.Down;
        movementDirection = Vector3.down;

        Vector3 lastPos = transform.position;

        positionHistory.Add(lastPos);
        directionHistory.Add(currentDirection);
        Transform wormRoot = transform.parent;
        for (int i = 0; i < initialBodyCount; i++)
        {
            lastPos += Vector3.up * moveStepY;
            GameObject part = Instantiate(bodyPrefab, lastPos, Quaternion.identity);
            part.transform.SetParent(wormRoot);
            part.layer = LayerMask.NameToLayer("WormBodyLayer");
            bodyParts.Add(part.transform);
            positionHistory.Add(lastPos);
            directionHistory.Add(currentDirection);
        }

        lastPos += Vector3.up * moveStepY;
        GameObject tail = Instantiate(tailPrefab, lastPos, Quaternion.identity);
        tail.transform.SetParent(wormRoot);
        tail.layer = LayerMask.NameToLayer("WormBodyLayer");
        bodyParts.Add(tail.transform);
        UpdateTailRotation();
        positionHistory.Add(lastPos);
        directionHistory.Add(currentDirection);

        mouthRenderer.enabled = false;
        rainbowRenderer.enabled = false;
        headFaceRenderer.sprite = faceNormal;
    }

    void Update()
    {
        if (isReversed) { return; }

       if (!canMove) { return; }
        Vector3 moveDir = Vector3.zero;
        float rotationZ = 0f;
        Direction newDirection = currentDirection;

        if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            if (currentDirection != Direction.Down)
            {
                moveDir = Vector3.up;
                rotationZ = 180f;
                newDirection = Direction.Up;
                movementDirection = Vector3.up;
            }
        }
        else if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            if (currentDirection != Direction.Up)
            {
                moveDir = Vector3.down;
                rotationZ = 0f;
                newDirection = Direction.Down;
                movementDirection = Vector3.down;
            }
        }
        else if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            if (currentDirection != Direction.Right)
            {
                moveDir = Vector3.left;
                rotationZ = 270f;
                newDirection = Direction.Left;
                movementDirection = Vector3.left;
            }
        }
        else if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            if (currentDirection != Direction.Left)
            {
                moveDir = Vector3.right;
                rotationZ = 90f;
                newDirection = Direction.Right;
                movementDirection = Vector3.right;
            }
        }

        // Check item in front
        Vector3 checkPos = transform.position + GetMovementStep(newDirection);
        Collider2D preview = Physics2D.OverlapCircle(checkPos, 0.1f);
        if (preview != null)
        {
            if (preview.CompareTag(tagBanana) || preview.CompareTag(tagMedicine))
                mouthRenderer.enabled = true;
            else
                mouthRenderer.enabled = false;
        }
        else
        {
            mouthRenderer.enabled = false;
        }

        if (moveDir != Vector3.zero)
        {
            Vector3 nextPos = transform.position + GetMovementStep(newDirection);

            Collider2D obstacle = Physics2D.OverlapCircle(nextPos, 0.1f, NoMoveLayer);
            if (obstacle != null && obstacle.gameObject.layer != LayerMask.NameToLayer("WormBodyLayer"))
            {
                //headFaceRenderer.sprite = faceFallEat;
                return;
            }

            Collider2D hitCollider = Physics2D.OverlapCircle(nextPos, 0.1f);
            if (hitCollider != null)
            {
                PushableItem pushable = hitCollider.GetComponent<PushableItem>();
                if (pushable != null)
                {
                    bool pushed = pushable.TryPush(GetMovementStep(newDirection));
                    if (!pushed)
                    {
                        Banana banana = pushable.GetComponent<Banana>();
                        if (banana != null)
                        {
                            banana.Eat();
                            GrowBody();
                            StartCoroutine(SetFaceTemporary(faceHappy, 1.5f));
                        }
                        else
                        {
                            Medicine medicine = pushable.GetComponent<Medicine>();
                            if (medicine != null)
                            {
                                medicine.Eat();
                                StartCoroutine(HandleEatMedicine(movementDirection));

                            }
                            else
                            {
                                //headFaceRenderer.sprite = faceFallEat;
                                return;
                            }
                        }
                    }
                }
                else
                {
                    Transform tail = bodyParts[bodyParts.Count - 1];
                    Vector3 tailNextPos = positionHistory[positionHistory.Count - 2];
                    if (hitCollider.transform != tail || nextPos == tailNextPos)
                    {
                        //headFaceRenderer.sprite = faceFallEat;
                        return;
                    }
                }
            }

            Vector3 newPosition = transform.position + GetMovementStep(newDirection);

            positionHistory.Insert(0, newPosition);
            if (positionHistory.Count > bodyParts.Count + 1)
                positionHistory.RemoveAt(positionHistory.Count - 1);

            directionHistory.Insert(0, newDirection);
            if (directionHistory.Count > bodyParts.Count + 1)
                directionHistory.RemoveAt(directionHistory.Count - 1);

            transform.position = newPosition;
            transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
            currentDirection = newDirection;
            SpawnSmokeAtTail();

            for (int i = 0; i < bodyParts.Count; i++)
            {
                int historyIndex = i + 1;
                if (historyIndex < positionHistory.Count)
                {
                    bodyParts[i].position = positionHistory[historyIndex];

                    if (i < bodyParts.Count - 1 && i + 1 < directionHistory.Count)
                    {
                        Direction from = directionHistory[i + 1];
                        Direction to = directionHistory[i];
                        SpriteRenderer sr = bodyParts[i].GetComponent<SpriteRenderer>();
                        if (sr) sr.sprite = GetBodySprite(from, to);
                    }
                }
            }
            UpdateTailRotation();
            StartCoroutine(MoveDelay());
        }
    }

    void SpawnSmokeAtTail()
    {
        if (smokeEffectPrefab == null) return;
        Transform tail = bodyParts[bodyParts.Count - 1];
        Transform beforeTail = bodyParts[bodyParts.Count - 2];
        Vector3 dir = (tail.position - beforeTail.position).normalized;
        GameObject smoke = Instantiate(smokeEffectPrefab, tail.position, Quaternion.LookRotation(Vector3.forward, dir));
        smoke.transform.SetParent(null);
        Destroy(smoke, 2f);
    }

    public void TriggerReverseMovement(Vector3 currentMoveDir)
    {
        Transform wormRoot = transform.parent;
        if (flyTween != null && flyTween.IsActive()) flyTween.Kill();
        foreach (Tween t in flyingItemTweens)
            if (t.IsActive()) t.Kill();
        flyingItemTweens.Clear();

        canMove = false;
        isReversed = true;
        Vector3 moveDir = currentMoveDir.normalized * -1f;

        flyTween = DOTween.To(() => wormRoot.position,
                      x => wormRoot.position = x,
                      wormRoot.position + moveDir * 100f,
                      20f)
        .SetEase(Ease.Linear)
        .SetLoops(-1, LoopType.Incremental)
        .OnUpdate(() =>
        {
            foreach (Transform part in wormRoot)
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(part.position, 0.45f);
                foreach (Collider2D hit in hits)
                {
                    if (hit == null || hit.gameObject == gameObject) continue;
                    if (((1 << hit.gameObject.layer) & NoMoveLayer) != 0)
                    {
                        StopReverseMovement();
                        return;
                    }

                    PushableItem pushable = hit.GetComponent<PushableItem>();
                    if (pushable != null && !IsItemAlreadyFlying(pushable))
                        StartFlyingItem(pushable.transform, moveDir);
                }
            }
        });
    }

    void StartFlyingItem(Transform item, Vector3 direction)
    {
        Tween itemTween = DOTween.To(() => item.position,
              x => item.position = x,
              item.position + direction * 100f,
              20f)
              .SetEase(Ease.Linear)
              .SetLoops(-1, LoopType.Incremental)
              .OnUpdate(() =>
              {
                  Collider2D[] hits = Physics2D.OverlapCircleAll(item.position, 0.425f);
                  foreach (var hit in hits)
                  {
                      if (hit == null || hit.gameObject == item.gameObject) continue;
                      if (((1 << hit.gameObject.layer) & NoMoveLayer) != 0)
                      {
                          StopReverseMovement();
                          return;
                      }
                  }
              });

        flyingItemTweens.Add(itemTween);
    }

    void StopReverseMovement()
    {
        if (flyTween != null && flyTween.IsActive()) flyTween.Kill(true);
        foreach (Tween t in flyingItemTweens)
            if (t.IsActive()) t.Kill(true);
        flyingItemTweens.Clear();

        UpdateHistoryAfterReverse();
        ResetFace();

        canMove = true;
        isReversed = false;
    }

    void SetFaceEatMedicine()
    {
        headFaceRenderer.sprite = faceEatMedicine;
        mouthRenderer.enabled = true;
        rainbowRenderer.enabled = true;
    }

    IEnumerator SetFaceTemporary(Sprite face, float duration)
    {
        headFaceRenderer.sprite = face;
        yield return new WaitForSeconds(duration);
        headFaceRenderer.sprite = faceNormal;
    }

    void ResetFace()
    {
        headFaceRenderer.sprite = faceNormal;
        mouthRenderer.enabled = false;
        rainbowRenderer.enabled = false;
    }

    bool IsItemAlreadyFlying(PushableItem item)
    {
        foreach (Tween t in flyingItemTweens)
        {
            if ((Object)t.target == (Object)item.transform)
                return true;
        }
        return false;
    }

    public void GrowBody()
    {
        Vector3 tailPos = bodyParts[bodyParts.Count - 1].position;
        Vector3 beforeTailPos = bodyParts[bodyParts.Count - 2].position;
        Vector3 dir = (tailPos - beforeTailPos).normalized;
        Vector3 spawnPos = tailPos + dir * GetMovementStep(currentDirection).magnitude;

        GameObject newBody = Instantiate(bodyPrefab, spawnPos, Quaternion.identity);
        newBody.transform.SetParent(transform.parent);
        newBody.layer = LayerMask.NameToLayer("WormBodyLayer");

        bodyParts.Insert(bodyParts.Count - 1, newBody.transform);
        positionHistory.Add(spawnPos);
        directionHistory.Add(currentDirection);
    }

    Vector3 GetMovementStep(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up: return Vector3.up * moveStepY;
            case Direction.Down: return Vector3.down * moveStepY;
            case Direction.Left: return Vector3.left * moveStepX;
            case Direction.Right: return Vector3.right * moveStepX;
            default: return Vector3.zero;
        }
    }

    void UpdateTailRotation()
    {
        if (bodyParts.Count < 2) return;
        Transform tail = bodyParts[bodyParts.Count - 1];
        Transform beforeTail = bodyParts[bodyParts.Count - 2];
        Vector3 directionToHead = beforeTail.position - tail.position;

        float angle = -90f;
        if (Mathf.Abs(directionToHead.x) > Mathf.Abs(directionToHead.y))
        {
            angle = directionToHead.x > 0 ? 180f : 0f;
        }
        else
        {
            angle = directionToHead.y > 0 ? -90f : 90f;
        }

        tail.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    IEnumerator MoveDelay()
    {
        canMove = false;
        yield return new WaitForSeconds(0.1f);
        canMove = true;
    }

    Sprite GetBodySprite(Direction prevDir, Direction currDir)
    {
        if (prevDir == currDir)
        {
            return (currDir == Direction.Left || currDir == Direction.Right) ? bodyHorizontal : bodyVertical;
        }
        else
        {
            if ((prevDir == Direction.Up && currDir == Direction.Left) || (prevDir == Direction.Right && currDir == Direction.Down)) return cornerTopRight;
            if ((prevDir == Direction.Up && currDir == Direction.Right) || (prevDir == Direction.Left && currDir == Direction.Down)) return cornerTopLeft;
            if ((prevDir == Direction.Down && currDir == Direction.Left) || (prevDir == Direction.Right && currDir == Direction.Up)) return cornerBottomRight;
            if ((prevDir == Direction.Down && currDir == Direction.Right) || (prevDir == Direction.Left && currDir == Direction.Up)) return cornerBottomLeft;
        }

        return bodyVertical;
    }

    void UpdateHistoryAfterReverse()
    {
        positionHistory.Clear();
        directionHistory.Clear();
        positionHistory.Add(transform.position);

        foreach (Transform part in bodyParts)
        {
            positionHistory.Add(part.position);
        }

        for (int i = 0; i < positionHistory.Count - 1; i++)
        {
            Vector3 dir = positionHistory[i] - positionHistory[i + 1];
            directionHistory.Add(DirectionFromVector(dir));
        }

        directionHistory.Add(directionHistory[directionHistory.Count - 1]);
    }

    Direction DirectionFromVector(Vector3 dir)
    {
        dir = dir.normalized;
        if (dir == Vector3.up) return Direction.Up;
        if (dir == Vector3.down) return Direction.Down;
        if (dir == Vector3.left) return Direction.Left;
        if (dir == Vector3.right) return Direction.Right;
        return currentDirection;
    }
    IEnumerator HandleEatMedicine(Vector3 currentMoveDir)
    {
        headFaceRenderer.sprite = faceEatMedicine;
        yield return new WaitForSeconds(0.5f);

        headFaceRenderer.sprite = faceEatMedicinev2;
        mouthRenderer.enabled = true;
        rainbowRenderer.enabled = true;

        TriggerReverseMovement(currentMoveDir);
    }

}
