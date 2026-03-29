using UnityEngine;
using System.Collections;
using SocialScenarios;

public class Tester : MonoBehaviour
{
    public ScoreTracker scoreTracker;
    public EndScreen endScreen;
    public ScoreBars scoreBars;

    void Start()
    {
        score = ScenarioManager.Instance.FinalOverall;
        StartCoroutine(RunTest());
    }

    IEnumerator RunTest()
    {
        yield return null;
        yield return null;
        float[] testScores = RoundHistory.Current.FinalScores;
        scoreTracker.SubmitScores(testScores);
        endScreen.Show();
        scoreBars.Show(testScores);
    }
}