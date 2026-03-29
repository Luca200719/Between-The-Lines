using System;
using System.Collections.Generic;
using UnityEngine;

namespace SocialScenarios
{

    /// <summary>
    /// Stores the raw 0-1 scores for a single round and exposes
    /// them mapped to 0-10 for display.
    /// </summary>
    [Serializable]
    public class RoundScore
    {
        public int Round;
        public float Assertiveness;
        public float Empathy;
        public float EmotionalRegulation;
        public float SocialConfidence;
        public float ProsocialIntent;

        // 0-10 mapped versions
        public float AssertivenessOut => To10(Assertiveness);
        public float EmpathyOut => To10(Empathy);
        public float EmotionalRegulationOut => To10(EmotionalRegulation);
        public float SocialConfidenceOut => To10(SocialConfidence);
        public float ProsocialIntentOut => To10(ProsocialIntent);
        public float TotalOut => To10(
            (Assertiveness + Empathy + EmotionalRegulation +
             SocialConfidence + ProsocialIntent) / 5f);

        private static float To10(float v) => Mathf.Clamp(v * 10f, 0f, 10f);
    }

    /// <summary>
    /// Sits alongside the existing SocialProfile and records each round's
    /// raw scores so they can be displayed at session end.
    ///
    /// Usage:
    ///   // In ScenarioManager, declare:
    ///   private RoundHistory roundHistory = new();
    ///
    ///   // After profile.Accumulate(scores) each round:
    ///   roundHistory.Record(scores, profile.RoundsCompleted);
    ///
    ///   // At session end, pass both to the display:
    ///   RoundScoreDisplay.Instance?.ShowFinal(roundHistory, profile);
    /// </summary>
    public class RoundHistory
    {
        public List<RoundScore> Rounds { get; } = new();

        public void Record(float[] scores, int roundNumber)
        {
            if (scores == null || scores.Length < 5)
                throw new ArgumentException("Expected 5 scores.");

            Rounds.Add(new RoundScore
            {
                Round = roundNumber,
                Assertiveness = scores[0],
                Empathy = scores[1],
                EmotionalRegulation = scores[2],
                SocialConfidence = scores[3],
                ProsocialIntent = scores[4]
            });
        }
    }
}