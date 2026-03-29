using System;

namespace SocialScenarios {
    [Serializable]
    public class SocialProfile {
        public float Assertiveness;      // Do they stand their ground or defer?
        public float Empathy;            // Do they consider others' perspectives?
        public float EmotionalRegulation;// Are they reactive or composed?
        public float SocialConfidence;   // How comfortable in the situation?
        public float ProsocialIntent;    // Cooperative or self-serving?

        public int RoundsCompleted;

        public SocialProfile() {
            Assertiveness = 0.5f;
            Empathy = 0.5f;
            EmotionalRegulation = 0.5f;
            SocialConfidence = 0.5f;
            ProsocialIntent = 0.5f;
            RoundsCompleted = 0;
        }

        public void Accumulate(float[] scores) {
            if (scores.Length != 5)
                throw new ArgumentException("Score vector must have exactly 5 values.");

            int n = RoundsCompleted + 1;
            Assertiveness = Lerp(Assertiveness, scores[0], 1f / n);
            Empathy = Lerp(Empathy, scores[1], 1f / n);
            EmotionalRegulation = Lerp(EmotionalRegulation, scores[2], 1f / n);
            SocialConfidence = Lerp(SocialConfidence, scores[3], 1f / n);
            ProsocialIntent = Lerp(ProsocialIntent, scores[4], 1f / n);

            RoundsCompleted = n;
        }

        public float[] ToVector() => new float[]
        {
            Assertiveness, Empathy, EmotionalRegulation, SocialConfidence, ProsocialIntent
        };

        public override string ToString() =>
            $"Assertiveness={Assertiveness:F2}  Empathy={Empathy:F2}  " +
            $"Regulation={EmotionalRegulation:F2}  Confidence={SocialConfidence:F2}  " +
            $"Prosocial={ProsocialIntent:F2}  (rounds={RoundsCompleted})";

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}