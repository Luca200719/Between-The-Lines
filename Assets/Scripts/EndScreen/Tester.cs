using UnityEngine;
using System.Collections;
using SocialScenarios;
using Unity.VisualScripting;

public class Tester : MonoBehaviour
{
    public ScoreTracker scoreTracker;
    public EndScreen endScreen;
    public ScoreBars scoreBars;

    RoundHistory score;

    void Start()
    {
        score = GameObject.FindWithTag("Dialogue Manager").GetComponent<RoundHistory>();
        StartCoroutine(RunTest());
    }

    IEnumerator RunTest()
    {
        yield return null;
        yield return null;

        float[] testScores = score.FinalScores;
        scoreTracker.SubmitScores(testScores);
        endScreen.Show();
        scoreBars.Show(testScores);
    }
}