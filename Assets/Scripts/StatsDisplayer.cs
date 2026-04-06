using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public static class Stats
{
    public static int movesCount = 0;
    public static int pushCount = 0;
    public static int goalsCompleted = 0;
    public static int goalsToComplete = 0;
    public static int levelIndex = 0;
    public static float timer = 0;
    public static bool isLoading = false;
    public static void ResetStats()
    {
        goalsToComplete = 0;
        movesCount = 0;
        pushCount = 0;
        goalsCompleted = 0;
        timer = 0;
    }
}


public class StatsDisplayer : MonoBehaviour
{
    [Header("UI Reference")]
    public TMP_Text statsText;
    public TMP_Text statsTextWinScreen;

    [Header("Settings")]
    public string levelPrefix = "Robot #";
    public string movesPrefix = "Cost: ";
    public string pushesPrefix = "Pushes: ";
    public string completionPrefix = "Completion Rate: ";
    public string timePrefix = "Efficiency: ";

    private Player player;
    void Update()
    {
        if (!Stats.isLoading) Stats.timer += Time.deltaTime;

        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        if (statsText == null) return;

        //Level Index
        string levelStr = $"{levelPrefix}{Stats.levelIndex+1}";

        //Moves Done
        string movesStr = $"{movesPrefix}{Stats.movesCount}";

        //Pushes Done
        string pushesStr = $"{pushesPrefix} {Stats.pushCount}";

        //Completion Rate
        float rate = ((float)Stats.goalsCompleted / Stats.goalsToComplete) * 100;
        string completionStr = $"{completionPrefix} {(int)rate}%";

        //Time Spent
        int minutes = Mathf.FloorToInt(Stats.timer / 60);
        int seconds = Mathf.FloorToInt(Stats.timer % 60);
        string timeStr = $"{timePrefix} {minutes:00}:{seconds:00}";

        //Combine everything with new lines
        statsText.text = levelStr + "\n" + movesStr + "\n" + pushesStr + "\n" + completionStr + "\n" + timeStr;
        statsTextWinScreen.text = levelStr + " | " + movesStr + " | " + pushesStr + " | " + timeStr;
    }
}
