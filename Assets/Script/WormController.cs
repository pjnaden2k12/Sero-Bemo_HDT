using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class WormMovement : MonoBehaviour
{
    public float moveStepX = 1.0f;
    public float moveStepY = 1.0f;
    private bool canMove = true;

    public GameObject bodyPrefab;
    public GameObject tailPrefab;
    public int initialBodyCount = 3;

    public LayerMask obstacleLayer;

    private List<Transform> bodyParts = new List<Transform>();
    private List<Vector3> positionHistory = new List<Vector3>();
    private List<Direction> directionHistory = new List<Direction>();
    private Direction currentDirection;

    public Sprite bodyStraight;
    public Sprite cornerTopRight;
    public Sprite cornerTopLeft;
    public Sprite cornerBottomLeft;
    public Sprite cornerBottomRight;
    public Sprite bodyHorizontal;
    public Sprite bodyVertical;

    private enum Direction { Up, Down, Left, Right }

    private Vector3 movementDirection;

    void Start()
    {
        currentDirection = Direction.Down;
        movementDirection = Vector3.down;

        Vector3 spawnDir = Vector3.up;
        Vector3 lastPos = transform.position;

        positionHistory.Add(lastPos);
        directionHistory.Add(currentDirection);

        for (int i = 0; i < initialBodyCount; i++)
        {
            lastPos += Vector3.up * moveStepY;
            GameObject part = Instantiate(bodyPrefab, lastPos, Quaternion.identity);
            bodyParts.Add(part.transform);
            positionHistory.Add(lastPos);
            directionHistory.Add(currentDirection);
        }

        lastPos += Vector3.up * moveStepY;
        GameObject tail = Instantiate(tailPrefab, lastPos, Quaternion.identity);
        bodyParts.Add(tail.transform);
        UpdateTailRotation();
        positionHistory.Add(lastPos);
        directionHistory.Add(currentDirection);
    }

    void Update()
    {
        if (!canMove) return;

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

        if (moveDir != Vector3.zero)
        {
            if (IsPathBlocked(newDirection))
                return;

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

    bool IsPathBlocked(Direction dir)
    {
        Vector3 checkOffset = GetMovementStep(dir);
        Vector3 checkPosition = transform.position + checkOffset;
        float checkRadius = 0.1f;

        Collider2D hitCollider = Physics2D.OverlapCircle(checkPosition, checkRadius, obstacleLayer);
        return hitCollider != null;
    }

    Vector3 GetMovementStep(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up:
                return Vector3.up * moveStepY;
            case Direction.Down:
                return Vector3.down * moveStepY;
            case Direction.Left:
                return Vector3.left * moveStepX;
            case Direction.Right:
                return Vector3.right * moveStepX;
            default:
                return Vector3.zero;
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
            if (directionToHead.x > 0)
                angle = 180f;
            else
                angle = 0f;
        }
        else
        {
            if (directionToHead.y > 0)
                angle = -90f;
            else
                angle = 90f;
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
            if (currDir == Direction.Left || currDir == Direction.Right)
                return bodyHorizontal;
            else
                return bodyVertical;
        }
        else
        {
            if ((prevDir == Direction.Up && currDir == Direction.Left) || (prevDir == Direction.Right && currDir == Direction.Down))
                return cornerTopRight;
            if ((prevDir == Direction.Up && currDir == Direction.Right) || (prevDir == Direction.Left && currDir == Direction.Down))
                return cornerTopLeft;
            if ((prevDir == Direction.Down && currDir == Direction.Left) || (prevDir == Direction.Right && currDir == Direction.Up))
                return cornerBottomRight;
            if ((prevDir == Direction.Down && currDir == Direction.Right) || (prevDir == Direction.Left && currDir == Direction.Up))
                return cornerBottomLeft;
        }

        return bodyVertical;
    }
}
