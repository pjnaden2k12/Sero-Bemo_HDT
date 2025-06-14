using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WormController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveStepX = 1.0f;
    public float moveStepY = 1.0f;
    public List<string> moveSequence;

    [Header("UI Buttons")]
    public Button btnUp;
    public Button btnDown;
    public Button btnLeft;
    public Button btnRight;

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
    public Sprite faceDrop;
    public Sprite facefalleat;
    public Sprite facefalleatv2;

    [Header("Tags")]
    public string tagBanana = "Banana";
    public string tagMedicine = "Medicine";

    [Header("Layer Masks")]
    public LayerMask NoMoveLayer;
    public LayerMask interactableLayer;

    private bool canMove = true;
    private bool isReversed = false;
    private Direction currentDirection;
    private Vector3 movementDirection;

    private List<Transform> bodyParts = new List<Transform>();
    private List<Vector3> positionHistory = new List<Vector3>();
    private List<Direction> directionHistory = new List<Direction>();
    private Tween flyTween;
    private List<Tween> flyingItemTweens = new List<Tween>();

    private Coroutine safeZoneChecker;

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
            GameObject part = Instantiate(bodyPrefab, lastPos, Quaternion.identity, wormRoot);
            part.layer = LayerMask.NameToLayer("WormBodyLayer");
            bodyParts.Add(part.transform);
            positionHistory.Add(lastPos);
            directionHistory.Add(currentDirection);
        }

        lastPos += Vector3.up * moveStepY;
        GameObject tail = Instantiate(tailPrefab, lastPos, Quaternion.identity, wormRoot);
        tail.layer = LayerMask.NameToLayer("WormBodyLayer");
        bodyParts.Add(tail.transform);
        positionHistory.Add(lastPos);
        directionHistory.Add(currentDirection);
        UpdateTailRotation();

        mouthRenderer.enabled = false;
        rainbowRenderer.enabled = false;
        headFaceRenderer.sprite = faceNormal;
        // Button setup
        if (btnUp == null) btnUp = GameObject.Find("UpBt")?.GetComponent<Button>();
        if (btnDown == null) btnDown = GameObject.Find("DownBt")?.GetComponent<Button>();
        if (btnLeft == null) btnLeft = GameObject.Find("LeftBt")?.GetComponent<Button>();
        if (btnRight == null) btnRight = GameObject.Find("RightBt")?.GetComponent<Button>();

        if (btnUp != null) btnUp.onClick.AddListener(() => TrySetDirection(Direction.Up));
        if (btnDown != null) btnDown.onClick.AddListener(() => TrySetDirection(Direction.Down));
        if (btnLeft != null) btnLeft.onClick.AddListener(() => TrySetDirection(Direction.Left));
        if (btnRight != null) btnRight.onClick.AddListener(() => TrySetDirection(Direction.Right));


        
        StartCoroutine(SetUpWorm());
    }

    IEnumerator SetUpWorm()
    {
        transform.localScale = Vector3.zero;
        foreach (Transform part in bodyParts)
            part.localScale = Vector3.zero;

        foreach (var dir in moveSequence)
        {
            yield return ForceMoveOneStep(dir);
            yield return new WaitForSeconds(0.001f);
        }

        float duration = 1f;
        transform.DOScale(Vector3.one, duration).SetEase(Ease.OutBack);
        foreach (Transform part in bodyParts)
            part.DOScale(Vector3.one, duration).SetEase(Ease.OutBack);

        yield return new WaitForSeconds(duration); 

        safeZoneChecker = StartCoroutine(CheckWormOutsideSafeZoneRoutine());
    }

    void TrySetDirection(Direction newDir)
    {
        if (!canMove || isReversed) return;
        if ((currentDirection == Direction.Up && newDir == Direction.Down) ||
            (currentDirection == Direction.Down && newDir == Direction.Up) ||
            (currentDirection == Direction.Left && newDir == Direction.Right) ||
            (currentDirection == Direction.Right && newDir == Direction.Left))
            return;

        movementDirection = GetMovementStep(newDir).normalized;
        float rotationZ = newDir switch
        {
            Direction.Up => 180f,
            Direction.Down => 0f,
            Direction.Left => 270f,
            Direction.Right => 90f,
            _ => 0f
        };

        StartCoroutine(MoveByInput(GetMovementStep(newDir), newDir, rotationZ));
    }

    IEnumerator MoveByInput(Vector3 step, Direction newDir, float rotationZ)
    {
        Vector3 nextPos = transform.position + step;

        CheckMouthTargetAhead();

        Collider2D obstacle = Physics2D.OverlapCircle(nextPos, 0.1f, NoMoveLayer);
        if (obstacle != null) yield break;

        Collider2D hit = Physics2D.OverlapCircle(nextPos, 0.1f, interactableLayer);
        if (hit != null)
        {
            PushableItem item = hit.GetComponent<PushableItem>();
            if (item != null)
            {
                if (!item.TryPush(step))
                {
                    Banana banana = item.GetComponent<Banana>();
                    if (banana != null)
                    {
                        banana.Eat();
                        GrowBody();
                        StartCoroutine(SetFaceTemporary(faceHappy, 1.5f));
                        mouthRenderer.enabled = false;
                    }

                    Medicine med = item.GetComponent<Medicine>();
                    if (med != null)
                    {
                        med.Eat();
                        StartCoroutine(HandleEatMedicine(movementDirection));
                    }
                }
            }
        }

        MoveStep(nextPos, newDir, rotationZ);
        yield return StartCoroutine(MoveDelay());
    }

    void CheckMouthTargetAhead()
    {
        Vector3 aheadPos = transform.position + movementDirection;
        Collider2D hit = Physics2D.OverlapCircle(aheadPos, 1.5f, interactableLayer);
        if (hit != null)
        {
            if (hit.CompareTag(tagBanana) || hit.CompareTag(tagMedicine))
            {
                mouthRenderer.enabled = true;
                return;
            }
        }

        mouthRenderer.enabled = false;
    }

    void MoveStep(Vector3 newPosition, Direction newDir, float rotationZ)
    {
        positionHistory.Insert(0, newPosition);
        if (positionHistory.Count > bodyParts.Count + 1)
            positionHistory.RemoveAt(positionHistory.Count - 1);

        directionHistory.Insert(0, newDir);
        if (directionHistory.Count > bodyParts.Count + 1)
            directionHistory.RemoveAt(directionHistory.Count - 1);

        transform.position = newPosition;
        transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
        currentDirection = newDir;
        SpawnSmokeAtTail();

        for (int i = 0; i < bodyParts.Count; i++)
        {
            int historyIndex = i + 1;
            if (historyIndex < positionHistory.Count)
            {
                bodyParts[i].position = positionHistory[historyIndex];
                if (i < bodyParts.Count - 1 && i + 1 < directionHistory.Count)
                {
                    SpriteRenderer sr = bodyParts[i].GetComponent<SpriteRenderer>();
                    sr.sprite = GetBodySprite(directionHistory[i + 1], directionHistory[i]);
                }
            }
        }

        UpdateTailRotation();
    }

    IEnumerator MoveDelay()
    {
        canMove = false;
        yield return new WaitForSeconds(0.1f);
        canMove = true;
    }

    void SpawnSmokeAtTail()
    {
        if (smokeEffectPrefab == null) return;
        Transform tail = bodyParts[bodyParts.Count - 1];
        Transform beforeTail = bodyParts[bodyParts.Count - 2];
        Vector3 dir = (tail.position - beforeTail.position).normalized;
        GameObject smoke = Instantiate(smokeEffectPrefab, tail.position, Quaternion.LookRotation(Vector3.forward, dir));
        Destroy(smoke, 2f);
    }

    public void GrowBody()
    {
        Transform tail = bodyParts[bodyParts.Count - 1];
        Transform beforeTail = bodyParts[bodyParts.Count - 2];
        Vector3 dir = (tail.position - beforeTail.position).normalized;
        Vector3 spawnPos = tail.position + dir * GetMovementStep(currentDirection).magnitude;

        GameObject newBody = Instantiate(bodyPrefab, spawnPos, Quaternion.identity, transform.parent);
        newBody.layer = LayerMask.NameToLayer("WormBodyLayer");
        bodyParts.Insert(bodyParts.Count - 1, newBody.transform);
        positionHistory.Add(spawnPos);
        directionHistory.Add(currentDirection);
    }

    IEnumerator HandleEatMedicine(Vector3 currentMoveDir)
    {
        headFaceRenderer.sprite = faceEatMedicine;
        mouthRenderer.enabled = false;
        yield return new WaitForSeconds(0.5f);

        headFaceRenderer.sprite = faceEatMedicinev2;
        mouthRenderer.enabled = true;
        rainbowRenderer.enabled = true;

        TriggerReverseMovement(currentMoveDir); 
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
                        {
                            StartFlyingItem(pushable.transform, moveDir);
                        }
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
                Collider2D[] hits = Physics2D.OverlapCircleAll(item.position, 0.52f);
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
        SpawnSmokeAtTail();
        UpdateHistoryAfterReverse();
        ResetFace();

        canMove = true;
        isReversed = false;
    }
    void UpdateHistoryAfterReverse()
    {
        positionHistory.Clear();
        directionHistory.Clear();
        positionHistory.Add(transform.position);

        foreach (Transform part in bodyParts)
            positionHistory.Add(part.position);

        for (int i = 0; i < positionHistory.Count - 1; i++)
        {
            Vector3 dir = positionHistory[i] - positionHistory[i + 1];
            directionHistory.Add(DirectionFromVector(dir));
        }

        // Lặp lại direction cuối để đủ chiều dài
        if (directionHistory.Count > 0)
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

    IEnumerator SetFaceTemporary(Sprite face, float duration)
    {
        headFaceRenderer.sprite = face;
        yield return new WaitForSeconds(duration);
        headFaceRenderer.sprite = faceNormal;
    }

    Vector3 GetMovementStep(Direction dir)
    {
        return dir switch
        {
            Direction.Up => Vector3.up * moveStepY,
            Direction.Down => Vector3.down * moveStepY,
            Direction.Left => Vector3.left * moveStepX,
            Direction.Right => Vector3.right * moveStepX,
            _ => Vector3.zero
        };
    }

    void UpdateTailRotation()
    {
        if (bodyParts.Count < 2) return;

        Transform tail = bodyParts[bodyParts.Count - 1];
        Transform beforeTail = bodyParts[bodyParts.Count - 2];

        Vector3 direction = (beforeTail.position - tail.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        tail.rotation = Quaternion.Euler(0f, 0f, angle - 180f); // -90 để xoay đúng hướng sprite
    }


    Sprite GetBodySprite(Direction prev, Direction next)
    {
        if ((prev == Direction.Up && next == Direction.Left) || (prev == Direction.Right && next == Direction.Down))
            return cornerTopRight;
        if ((prev == Direction.Up && next == Direction.Right) || (prev == Direction.Left && next == Direction.Down))
            return cornerTopLeft;
        if ((prev == Direction.Down && next == Direction.Left) || (prev == Direction.Right && next == Direction.Up))
            return cornerBottomRight;
        if ((prev == Direction.Down && next == Direction.Right) || (prev == Direction.Left && next == Direction.Up))
            return cornerBottomLeft;
        if ((prev == Direction.Up && next == Direction.Up) || (prev == Direction.Down && next == Direction.Down))
            return bodyVertical;
        if ((prev == Direction.Left && next == Direction.Left) || (prev == Direction.Right && next == Direction.Right))
            return bodyHorizontal;

        return bodyHorizontal;
    }

    IEnumerator ForceMoveOneStep(string direction)
    {
        Direction dirEnum = direction switch
        {
            "Up" => Direction.Up,
            "Down" => Direction.Down,
            "Left" => Direction.Left,
            "Right" => Direction.Right,
            _ => Direction.Down
        };

        Vector3 step = GetMovementStep(dirEnum);
        float rotationZ = dirEnum switch
        {
            Direction.Up => 180f,
            Direction.Down => 0f,
            Direction.Left => 270f,
            Direction.Right => 90f,
            _ => 0f
        };

        Vector3 newPos = transform.position + step;
        MoveStep(newPos, dirEnum, rotationZ);
        yield return new WaitForSeconds(0.1f);
    }
    IEnumerator CheckWormOutsideSafeZoneRoutine()
    {
      
        WaitForSeconds wait = new WaitForSeconds(0.1f);

        while (true)
        {
            bool allOutsideA = true;  // giả sử tất cả ngoài A
            bool allInsideB = true;   // giả sử tất cả trong B

            List<Transform> allParts = new List<Transform>(bodyParts);
            allParts.Insert(0, transform); // thêm đầu giun

            foreach (Transform part in allParts)
            {
                bool inA = false;
                bool inB = false;
                CheckTagsAtPosition(part.position, ref inA, ref inB);

                if (inA) allOutsideA = false;  // phần này trong A → không phải toàn bộ ngoài A
                if (!inB) allInsideB = false;  // phần này không trong B → không phải toàn bộ trong B

                if (!allOutsideA && !allInsideB)
                {
                    // Đã đủ điều kiện để tiếp tục, thoát vòng check sớm
                    break;
                }
            }

            if (allOutsideA || allInsideB)
            {
                headFaceRenderer.sprite = faceDrop;
                Debug.Log("Thua vì toàn bộ giun ra khỏi A hoặc toàn bộ giun vào B");
                LoseGame();
                yield break;
            }

            yield return wait;
        }
    }




    void CheckTagsAtPosition(Vector3 pos, ref bool inA, ref bool inB)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 0.1f);
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (hit.CompareTag("SafeZoneA")) inA = true;
            if (hit.CompareTag("SafeZoneB")) inB = true;

            if (inA && inB) break;
        }
    }


    void LoseGame()
    {
        Debug.Log("Game Over!");
       
    }

}
