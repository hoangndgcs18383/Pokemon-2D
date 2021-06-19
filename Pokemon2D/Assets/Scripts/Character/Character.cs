using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public float movingSpeed = 5f;

    public bool isMoving { get; private set; }
    public float OffsetY { get; private set; } = 0.3f;

    CharacterAnimator animator;
    private void Awake()
    {
        animator = GetComponent<CharacterAnimator>();
        SetPositionAndSnapToTile(transform.position);
    }
    public void SetPositionAndSnapToTile(Vector2 pos)
    {
        // 2.3 => float => 2 => 2.5
        pos.x = Mathf.Floor(pos.x) + 0.5f;
        pos.y = Mathf.Floor(pos.y) + 0.5f + OffsetY;

        transform.position = pos;
    }
    public IEnumerator Move(Vector2 moveVec, Action OnMoveOver=null)
    {
        animator.MoveX = Mathf.Clamp(moveVec.x, -1f, 1f);
        animator.MoveY = Mathf.Clamp(moveVec.y, -1f, 1f);

        var targetPos = transform.position;
        targetPos.x += moveVec.x;
        targetPos.y += moveVec.y;

        if (!IsPathClear(targetPos))
            yield break;

        isMoving = true;

        while ((targetPos - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, movingSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos;

        isMoving = false;

        OnMoveOver?.Invoke();
    }
    private bool IsPathClear(Vector3 targetPos)
    {
        var diff = targetPos - transform.position;
        var dir = diff.normalized;

        if(Physics2D.BoxCast(transform.position + dir, new Vector2(0.2f, 0.2f), 0f, dir, diff.magnitude - 1, GameLayers.i.SolidObjectLayer | GameLayers.i.InteractableLayer | GameLayers.i.PlayerLayer))
            return false;

        return true;
    }
    public void HandleUpdate()
    {
        animator.isMoving = isMoving;
    }
    private bool isWalkable(Vector3 targetPos)
    {
        if (Physics2D.OverlapCircle(targetPos, 0.2f, GameLayers.i.SolidObjectLayer | GameLayers.i.InteractableLayer | GameLayers.i.PlayerLayer) != null)
        {
            return false;
        }
        else
        {
            return true;
        }
    }
    public void LookTowards(Vector3 targetPos)
    {
        var xdiff = Mathf.Floor(targetPos.x) - Mathf.Floor(transform.position.x);
        var ydiff = Mathf.Floor(targetPos.y) - Mathf.Floor(transform.position.y);

        if(xdiff == 0 || ydiff == 0)
        {
            animator.MoveX = Mathf.Clamp(xdiff, -1f, 1f);
            animator.MoveY = Mathf.Clamp(ydiff, -1f, 1f);
        }
        else
        {
            Debug.LogError("Error");
        }
    }
    public CharacterAnimator Animator
    {
        get => animator;
    }
}
