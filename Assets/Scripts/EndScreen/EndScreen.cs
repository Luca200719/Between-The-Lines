using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EndScreen : MonoBehaviour
{
    [Header("References")]
    public ScoreTracker scoreTracker;
    public RadarChart endRadarChart;
    public ScoreBars scoreBars;

    [Header("Score Labels")]
    public TextMeshProUGUI assertivenessText;
    public TextMeshProUGUI empathyText;
    public TextMeshProUGUI emotionalRegulationText;
    public TextMeshProUGUI socialConfidenceText;
    public TextMeshProUGUI prosocialIntentText;
    public TextMeshProUGUI grandTotalText;
    public TextMeshProUGUI bestCategoryText;
    public TextMeshProUGUI worstCategoryText;

    private readonly string[] names = {
        "Assertiveness",
        "Empathy",
        "Emotional Regulation",
        "Social Confidence",
        "Prosocial Intent"
    };

    public void Show()
    {
        gameObject.SetActive(true);

        ScoreData totals = scoreTracker.GetRunningTotals();
        float[] arr = totals.ToArray();

        assertivenessText.text =        "Assertiveness: "        + arr[0].ToString("F1");
        empathyText.text =              "Empathy: "              + arr[1].ToString("F1");
        emotionalRegulationText.text =  "Emotional Regulation: " + arr[2].ToString("F1");
        socialConfidenceText.text =     "Social Confidence: "    + arr[3].ToString("F1");
        prosocialIntentText.text =      "Prosocial Intent: "     + arr[4].ToString("F1");
        grandTotalText.text =           "Total: "                + totals.Total.ToString("F1");

        int bestIdx = 0, worstIdx = 0;
        for (int i = 1; i < arr.Length; i++)
        {
            if (arr[i] > arr[bestIdx]) bestIdx = i;
            if (arr[i] < arr[worstIdx]) worstIdx = i;
        }

        bestCategoryText.text =  "Your strongest skill: " + names[bestIdx];
        worstCategoryText.text = "Your growth area: "     + names[worstIdx];

        endRadarChart.UpdateScoresFromArray(arr);

        // Feed the same scores into the bars
        scoreBars.Show(arr);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}