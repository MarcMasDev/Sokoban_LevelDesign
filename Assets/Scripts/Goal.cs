using UnityEngine;

public class Goal : MonoBehaviour
{
    [Header("Color Settings")]
    public SpriteRenderer colorRenderer;
    public Color incompleteColor = Color.red;
    public Color completedColor = Color.green;

    [HideInInspector] public bool isCompleted = false;

    private void OnEnable()
    {
        SetState(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Furniture"))
        {
            SetState(true);
            Stats.goalsCompleted++;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Furniture"))
        {
            SetState(false); 
            Stats.goalsCompleted--;
        }
    }

    private void SetState(bool completed)
    {
        isCompleted = completed;

        if (isCompleted) colorRenderer.color = completedColor;
        else colorRenderer.color = incompleteColor;
    }
}