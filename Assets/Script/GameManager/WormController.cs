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

    [Header("UI & Level Manager")]
    public UIManager uiManager;
    public LevelManager levelManager;

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

    [Header("Hole Settings")]
    public SpriteRenderer holeRenderer;
    public Sprite holeOpen;
    public Sprite holeClose;


    [Header("Tags")]
    public string tagBanana = "Banana";
    public string tagMedicine = "Medicine";

    [Header("Layer Masks")]
    public LayerMask NoMoveLayer;
    public LayerMask interactableLayer;

    private bool hasWon=false;
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
    public void SetupLevelData(int count, List<string> sequence)
    {
        initialBodyCount = count;
        moveSequence = sequence;
        Debug.Log($"Setup Worm: BodyCount={count}, Moves={string.Join(",", sequence)}");
    }
    void Start()
    {
        if (holeRenderer != null && holeClose != null)
        {
            holeRenderer.sprite = holeClose;
        }

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
        if (uiManager == null)
            uiManager = FindFirstObjectByType<UIManager>();

        if (uiManager != null)
        {
            uiManager.OnDirectionButtonPressed += HandleDirectionInput;
        }
        if (levelManager != null) 
            levelManager = FindFirstObjectByType<LevelManager>();
        

        StartCoroutine(SetUpWorm());
    }
    void HandleDirectionInput(string direction)
    {
        Direction newDir = direction switch
        {
            "Up" => Direction.Up,
            "Down" => Direction.Down,
            "Left" => Direction.Left,
            "Right" => Direction.Right,
            _ => currentDirection
        };

        TrySetDirection(newDir);
    }
    void OnDestroy()
    {
        if (uiManager != null)
        {
            uiManager.OnDirectionButtonPressed -= HandleDirectionInput;
        }
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!hasWon) return;

        if (other.CompareTag("Hole"))
        {
            SpriteRenderer sr = other.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite == holeOpen)
            {
                Debug.Log("Worm touching open hole!");
                StartCoroutine(WormEnterHole(other.transform.position));
            }
        }
    }
    IEnumerator WormEnterHole(Vector3 holePos)
    {
        canMove = false;

        float duration = 0.6f;

        transform.DOMove(holePos, duration).SetEase(Ease.InQuad);
        transform.DOScale(Vector3.zero, duration).SetEase(Ease.InQuad);

        foreach (Transform part in bodyParts)
            part.DOScale(Vector3.zero, duration).SetEase(Ease.InQuad);

        SpawnSmokeAtTail();

        yield return new WaitForSeconds(duration);

        WinGame();
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
                        StartCoroutine(DelayedCheckWin());

                    }

                    Medicine med = item.GetComponent<Medicine>();
                    if (med != null)
                    {
                        med.Eat();
                        StartCoroutine(HandleEatMedicine(movementDirection));
                        StartCoroutine(DelayedCheckWin());
                       

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
        Collider2D hit = Physics2D.OverlapCircle(aheadPos, 0.9f, interactableLayer);
        if (hit != null)
        {
            if (hit.CompareTag(tagBanana) || hit.CompareTag(tagMedicine))
            {
                Vector3 toTarget = (hit.transform.position - transform.position).normalized;
                float dot = Vector3.Dot(toTarget, movementDirection.normalized);
                if (dot > 0.9f)
                {
                    mouthRenderer.enabled = true;
                    return;
                }
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
        if (safeZoneChecker != null)
        {
            StopCoroutine(safeZoneChecker);
            safeZoneChecker = null;
        }
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
                bool allOutOfView = true;

                foreach (Transform part in wormRoot)
                {
                    Vector3 viewPos = Camera.main.WorldToViewportPoint(part.position);
                    bool isVisible = viewPos.x >= 0 && viewPos.x <= 1 &&
                                     viewPos.y >= 0 && viewPos.y <= 1 &&
                                     viewPos.z > 0;

                    if (isVisible)
                    {
                        allOutOfView = false;
                        break;
                    }
                }

                if (allOutOfView)
                {
                    Debug.Log("Worm flew out of camera view");
                    StopReverseMovement();
                    StartCoroutine( LoseGame());
                    return;
                }

               
                foreach (Transform part in wormRoot)
                {
                    Collider2D[] hits = Physics2D.OverlapCircleAll(part.position, 0.5f);
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
                Collider2D[] hits = Physics2D.OverlapCircleAll(item.position, 0.54f);
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
        if (safeZoneChecker == null)
            safeZoneChecker = StartCoroutine(CheckWormOutsideSafeZoneRoutine());
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
    IEnumerator SetFallingFace()
    {
        mouthRenderer.enabled=false;
        headFaceRenderer.sprite = facefalleat;
        yield return new WaitForSeconds(0.7f);
        headFaceRenderer.sprite = facefalleatv2;
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

        tail.rotation = Quaternion.Euler(0f, 0f, angle - 180f); 
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
            // Kiểm tra giun
            if (IsAllOutsideZone("SafeZoneA", GetAllWormParts()) || IsAllInsideZone("SafeZoneB", GetAllWormParts()))
            {
                headFaceRenderer.sprite = faceDrop;
                Debug.Log("Thua vì GIUN ra khỏi A hoặc toàn bộ giun vào B");
                StartCoroutine( LoseGame());
                yield break;
            }

            // Kiểm tra Banana
            GameObject[] bananas = GameObject.FindGameObjectsWithTag(tagBanana);
            if (bananas.Length > 0)
            {
                if (IsAllOutsideZone("SafeZoneA", bananas) || IsAllInsideZone("SafeZoneB", bananas))
                {
                    StartCoroutine(SetFallingFace());

                    Debug.Log("Thua vì BANANA ra khỏi A hoặc vào hết B");
                    StartCoroutine(LoseGame());
                    yield break;
                }
            }

            // Kiểm tra Medicine
            GameObject[] medicines = GameObject.FindGameObjectsWithTag(tagMedicine);
            if (medicines.Length > 0)
            {
                if (IsAllOutsideZone("SafeZoneA", medicines) || IsAllInsideZone("SafeZoneB", medicines))
                {
                    StartCoroutine(SetFallingFace());

                    Debug.Log("Thua vì MEDICINE ra khỏi A hoặc vào hết B");
                    StartCoroutine(LoseGame());
                    yield break;
                }
            }

            yield return wait;
        }
    }

    void CheckWinCondition()
    {
        if (hasWon) return;
        GameObject[] bananas = GameObject.FindGameObjectsWithTag(tagBanana);
        GameObject[] medicines = GameObject.FindGameObjectsWithTag(tagMedicine);

        if (bananas.Length == 0 && medicines.Length == 0)
        {
            hasWon = true;
            Debug.Log("WIN! Open the hole!");
            if (holeRenderer != null && holeOpen != null)
            {
                holeRenderer.sprite = holeOpen;
            }

        }
    }

    IEnumerator LoseGame()
    {
        VanishWorm();
        yield return new WaitForSeconds(1.5f);
        if (uiManager != null)
        {
            uiManager.ShowLoseUI();
        }

        Debug.Log("Game Over!");
        
    }
    void WinGame()
    {
        levelManager.OnLevelCompleted();
        StartCoroutine(HandleWinSequence());
    }

    IEnumerator HandleWinSequence()
    {
        CloudScreenEffect cloud = FindFirstObjectByType<CloudScreenEffect>();

        // Bắt đầu hiệu ứng mây vào
        Coroutine enterCloud = null;
        if (cloud != null)
        {
            enterCloud = StartCoroutine(cloud.EnterScreenEffect());
        }

        // Đợi 0.7s rồi chuyển level
        yield return new WaitForSeconds(0.4f);
        LevelManager levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager != null)
        {
            levelManager.NextLevel();
        }

       

        yield return new WaitForSeconds(1.5f);

        if (cloud != null)
        {
            yield return cloud.ExitScreenEffect();
        }
    }


    IEnumerator DelayedCheckWin()
    {
        yield return null; 
        CheckWinCondition();
    }
    public void VanishWorm()
    {
        StartCoroutine(VanishWormRoutine());        
    }

    IEnumerator VanishWormRoutine()
    {
        yield return new WaitForSeconds(1f);
        GameObject worm = GameObject.Find("Worm");
        if (worm != null)
        {
            float fadeDuration = 0f;
            Instantiate(smokeEffectPrefab, worm.transform.position, Quaternion.identity);

            SpriteRenderer[] renderers = worm.GetComponentsInChildren<SpriteRenderer>();

            foreach (var sr in renderers)
            {
                sr.DOFade(0f, fadeDuration);
            }

        }

        SpawnSmokeAt(transform.position);
        foreach (Transform part in bodyParts)
        {
            SpawnSmokeAt(part.position);
        }
    }

    void SpawnSmokeAt(Vector3 position)
    {
        if (smokeEffectPrefab != null)
        {
            GameObject smoke = Instantiate(smokeEffectPrefab, position, Quaternion.identity);
            Destroy(smoke, 2f);
        }
    }


    List<Transform> GetAllWormParts()
    {
        List<Transform> allParts = new List<Transform>(bodyParts);
        allParts.Insert(0, transform);
        return allParts;
    }

    bool IsAllOutsideZone(string zoneTag, IEnumerable<Transform> parts)
    {
        foreach (var part in parts)
        {
            if (IsInsideZone(part.position, zoneTag)) return false;
        }
        return true;
    }

    bool IsAllOutsideZone(string zoneTag, GameObject[] objects)
    {
        foreach (var obj in objects)
        {
            if (IsInsideZone(obj.transform.position, zoneTag)) return false;
        }
        return true;
    }

    bool IsAllInsideZone(string zoneTag, IEnumerable<Transform> parts)
    {
        foreach (var part in parts)
        {
            if (!IsInsideZone(part.position, zoneTag)) return false;
        }
        return true;
    }

    bool IsAllInsideZone(string zoneTag, GameObject[] objects)
    {
        foreach (var obj in objects)
        {
            if (!IsInsideZone(obj.transform.position, zoneTag)) return false;
        }
        return true;
    }

    bool IsInsideZone(Vector3 position, string tag)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, 0.1f);
        foreach (var hit in hits)
        {
            if (hit != null && hit.CompareTag(tag))
                return true;
        }
        return false;
    }

}
