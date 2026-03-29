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
        StartCoroutine(RunTest());
    }

    IEnumerator RunTest()
    {
        yield return null;
        yield return null;
        float[] testScores = ScenarioManager.Instance.FinalScores;
        scoreTracker.SubmitScores(testScores);
        endScreen.Show();
        scoreBars.Show(testScores);
    }
}