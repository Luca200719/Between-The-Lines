using UnityEngine;
using TMPro;

public class ScoreTracker : MonoBehaviour
{
    [Header("References")]
    public RadarChart radarChart;
    public TextMeshProUGUI totalScoreText;
    public TextMeshProUGUI feedbackText;

    private float[] runningTotals = new float[5];
    private int roundsPlayed = 0;

    private readonly string[] names = {
        "Assertiveness",
        "Empathy",
        "Emotional Regulation",
        "Social Confidence",
        "Prosocial Intent"
    };

    public void SubmitScores(float[] scores)
    {
        if (scores.Length != 5) return;

        for (int i = 0; i < 5; i++)
            runningTotals[i] += scores[i];

        roundsPlayed++;

        radarChart.UpdateScoresFromArray(scores);
        UpdateUI(scores);
    }

    void UpdateUI(float[] scores)
    {
        float grandTotal = 0;
        foreach (float t in runningTotals) grandTotal += t;
        totalScoreText.text = "Total: " + grandTotal.ToString("F1");

        int bestIdx = 0, worstIdx = 0;
        for (int i = 1; i < scores.Length; i++)
        {
            if (scores[i] > scores[bestIdx]) bestIdx = i;
            if (scores[i] < scores[worstIdx]) worstIdx = i;
        }

        feedbackText.text = "Strongest: " + names[bestIdx] + "\nWeakest: " + names[worstIdx];

    }

    public ScoreData GetRunningTotals() => new ScoreData(runningTotals);

    public ScoreData GetAverages()
    {
        if (roundsPlayed == 0) return new ScoreData(new float[5]);
        float[] avgs = new float[5];
        for (int i = 0; i < 5; i++)
            avgs[i] = runningTotals[i] / roundsPlayed;
        return new ScoreData(avgs);
    }

    public void Reset()
    {
        runningTotals = new float[5];
        roundsPlayed = 0;
        totalScoreText.text = "Total: 0";
        feedbackText.text = "";
    }
}