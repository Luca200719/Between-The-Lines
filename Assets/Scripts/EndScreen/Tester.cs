using UnityEngine;
using System.Collections;

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

        float[] testScores = new float[] { 7.5f, 6.0f, 8.0f, 4.5f, 9.0f };
        scoreTracker.SubmitScores(testScores);
        endScreen.Show();
        scoreBars.Show(testScores);
    }
}