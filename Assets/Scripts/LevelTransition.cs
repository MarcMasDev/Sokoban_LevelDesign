using TMPro;
using UnityEngine;

[RequireComponent(typeof(Animator))] 
public class LevelTransition : MonoBehaviour
{
    private Animator anim;
    [SerializeField] private string[] tips;
    [SerializeField] private TMP_Text tipText;
    
    private void Update()
    {
        if (anim == null) anim = GetComponent<Animator>();

        anim.SetBool("Load", Stats.isLoading);

        if (Stats.levelIndex < tips.Length && Stats.levelIndex > 0) tipText.text = tips[Stats.levelIndex-1];
        else tipText.text = tips[Random.Range(0, tips.Length)];
    }
    public void FinishLoad()
    {
        Stats.isLoading = false;
    }
}
