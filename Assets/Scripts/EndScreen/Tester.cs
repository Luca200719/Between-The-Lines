using UnityEngine;

public class TestScores : MonoBehaviour
{
    public ScoreTracker scoreTracker;
    public EndScreen endScreen;

    void Start()
    {
        float[] testScores = new float[] { 7.5f, 6.0f, 8.0f, 4.5f, 9.0f };
        scoreTracker.SubmitScores(testScores);
        endScreen.Show();
    }
}