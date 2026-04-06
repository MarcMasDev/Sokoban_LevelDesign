using System.Collections;
using UnityEngine;

public class Furniture : MonoBehaviour
{
    public LayerMask wallLayer;
    public LayerMask furnitureLayer;
    public float pushSpeed = 5f;

    //CAN IT BE PUSHED?
    public bool Push(Vector2 direction)
    {
        Vector3 targetPos = transform.position + (Vector3)direction;

        if (Physics2D.OverlapPoint(targetPos, wallLayer) || Physics2D.OverlapPoint(targetPos, furnitureLayer)) return false;

        StartCoroutine(SmoothMove(targetPos));
        return true;
    }

    private IEnumerator SmoothMove(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, pushSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
        Stats.pushCount++;
    }
}
