using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveSpeed = 5f;
    public LayerMask wallLayer;
    public LayerMask furnitureLayer;

    [Header("Directional Sprites")]
    public Sprite upSprite;
    public Sprite downSprite;
    public Sprite sideSprite;

    private SpriteRenderer spriteRenderer;
    private bool isMoving;
    private Vector2 input;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    private void OnEnable()
    {
        isMoving = false;
    }

    private void Update()
    {
        if (!isMoving)
        {
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");

            if (input.x != 0) input.y = 0;

            if (input != Vector2.zero)
            {
                UpdateSprite(input);
                StartCoroutine(MovePlayer(input));
            }
        }
    }

    private void UpdateSprite(Vector2 direction)
    {
        //UP
        if (direction.y > 0)
        {
            spriteRenderer.sprite = upSprite;
            spriteRenderer.flipX = false;
        }
        //DOWN
        else if (direction.y < 0)
        {
            spriteRenderer.sprite = downSprite;
            spriteRenderer.flipX = false;
        }
        //RIGHT
        else if (direction.x > 0)
        {
            spriteRenderer.sprite = sideSprite;
            spriteRenderer.flipX = false;
        }
        //LEFT
        else if (direction.x < 0)
        {
            spriteRenderer.sprite = sideSprite;
            spriteRenderer.flipX = true;
        }
    }

    private IEnumerator MovePlayer(Vector2 direction)
    {
        isMoving = true;
        Vector3 targetPos = transform.position + (Vector3)direction;

        Furniture pushedBox = null;
        Vector3 boxStartPos = Vector3.zero;

        //Comprobamos si podemos movernos y si hay un mueble involucrado
        if (CanMove(direction, out pushedBox) && !Stats.isLoading)
        {
            if (pushedBox != null)
            {
                //Guardamos la posición del mueble ANTES de que se empuje
                boxStartPos = pushedBox.transform.position;
                pushedBox.Push(direction);
            }

            //GUARDAMOS EL HISTORIAL
            UndoManager.RecordMove(transform.position, pushedBox, boxStartPos);

            while (Vector3.Distance(transform.position, targetPos) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = targetPos;
            Stats.movesCount++;
        }

        isMoving = false;
    }

    private bool CanMove(Vector2 dir, out Furniture pushedFurniture)
    {

        pushedFurniture = null;
        Vector3 targetPos = transform.position + (Vector3)dir;

        if (Physics2D.OverlapPoint(targetPos, wallLayer)) return false;

        Collider2D furnitureCollider = Physics2D.OverlapPoint(targetPos, furnitureLayer);

        if (furnitureCollider != null)
        {
            Furniture furniture = furnitureCollider.GetComponent<Furniture>();

            Vector3 boxTargetPos = furniture.transform.position + (Vector3)dir;

            if (!Physics2D.OverlapPoint(boxTargetPos, wallLayer) && !Physics2D.OverlapPoint(boxTargetPos, furnitureLayer))
            {
                pushedFurniture = furniture;
                return true;
            }
            return false;
        }

        return true;
    }

    public void ResetMovementState()
    {
        isMoving = false;
    }
}