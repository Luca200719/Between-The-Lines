[System.Serializable]
public class ScoreData
{
    public float assertiveness;
    public float empathy;
    public float emotionalRegulation;
    public float socialConfidence;
    public float prosocialIntent;

    public ScoreData(float[] arr)
    {
        assertiveness = arr[0];
        empathy = arr[1];
        emotionalRegulation = arr[2];
        socialConfidence = arr[3];
        prosocialIntent = arr[4];
    }

    public float Total => assertiveness + empathy + emotionalRegulation + socialConfidence + prosocialIntent;

    public float[] ToArray() => new float[]
    {
        assertiveness, empathy, emotionalRegulation, socialConfidence, prosocialIntent
    };
}