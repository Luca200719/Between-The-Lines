using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SocialScenarios {
    public class AIScorer : MonoBehaviour {
        private const string API_URL = "https://api.anthropic.com/v1/messages";
        private const string MODEL = "claude-haiku-4-5-20251001";

        [Header("Anthropic API Key")]
        public string apiKey;

        /// <summary>
        /// Scores a round given the dialogue conversation that was playing,
        /// the question asked, and the user's typed answer.
        /// Returns float[5]: [assertiveness, empathy, emotional_regulation,
        ///                     social_confidence, prosocial_intent]
        /// Invokes onError with "INVALID_ANSWER" if the answer was not meaningful.
        /// </summary>
        public IEnumerator ScoreRound(
            string conversationContext,
            string question,
            string userAnswer,
            Action<float[]> onComplete,
            Action<string> onError) {

            string systemPrompt =
                "You are a social psychology scoring engine. " +
                "You will receive a conversation that a user just watched, " +
                "a question about it, and their answer. " +
                "Score their answer on these 5 traits from 0.0 to 1.0:\n" +
                "- assertiveness: how much they stand their ground vs defer\n" +
                "- empathy: how much they consider others' feelings\n" +
                "- emotional_regulation: how composed vs reactive they are\n" +
                "- social_confidence: how comfortable they seem in the situation\n" +
                "- prosocial_intent: how cooperative vs self-serving they are\n\n" +
                "You MUST always reply with ONLY a raw JSON object, even if the answer is poor or irrelevant. " +
                "Never refuse. Never explain. Never use markdown or backticks. " +
                "Only set invalid to true if the answer is completely blank, pure gibberish (e.g. 'asdfgh'), " +
                "or entirely unrelated to human interaction (e.g. 'pizza'). " +
                "Short, blunt, or socially poor answers are still valid — score them low but do not mark invalid. " +
                "When in doubt, set invalid to false and score accordingly.\n" +
                "and set invalid to true. Otherwise set invalid to false.\n" +
                "Reply ONLY with this exact JSON format, no other text:\n" +
                "{\"assertiveness\":0.0,\"empathy\":0.0,\"emotional_regulation\":0.0," +
                "\"social_confidence\":0.0,\"prosocial_intent\":0.0,\"invalid\":false}";

            string userMessage =
                $"Conversation:\n{conversationContext}\n\n" +
                $"Question: {question}\n\n" +
                $"User's answer: {userAnswer}";

            string bodyJson = BuildRequestJson(systemPrompt, userMessage);

            using var request = new UnityWebRequest(API_URL, "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(bodyJson));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("x-api-key", apiKey);
            request.SetRequestHeader("anthropic-version", "2023-06-01");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success) {
                onError?.Invoke(request.error);
                yield break;
            }

            try {
                var raw = request.downloadHandler.text;
                var resp = JsonUtility.FromJson<APIResponse>(raw);

                // Strip markdown code fences in case the model ignores instructions
                var jsonText = resp.content[0].text.Trim()
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                // If it still doesn't look like JSON, the model refused to score
                if (!jsonText.StartsWith("{")) {
                    onError?.Invoke("INVALID_ANSWER");
                    yield break;
                }

                var scores = JsonUtility.FromJson<ScoreResult>(jsonText);

                // Model flagged the answer as invalid
                if (scores.invalid) {
                    onError?.Invoke("INVALID_ANSWER");
                    yield break;
                }

                onComplete?.Invoke(new float[] {
                    scores.assertiveness,
                    scores.empathy,
                    scores.emotional_regulation,
                    scores.social_confidence,
                    scores.prosocial_intent
                });
            }
            catch (Exception e) {
                onError?.Invoke($"Parse error: {e.Message}\nRaw: {request.downloadHandler.text}");
            }
        }

        // ── Serialization ─────────────────────────────────────────────

        private static string BuildRequestJson(string system, string userMsg) {
            // Manual build avoids nested object serialization issues with JsonUtility
            return "{" +
                $"\"model\":\"{MODEL}\"," +
                "\"max_tokens\":150," +
                $"\"system\":{JsonString(system)}," +
                "\"messages\":[{\"role\":\"user\",\"content\":" + JsonString(userMsg) + "}]" +
            "}";
        }

        private static string JsonString(string s) =>
            "\"" + s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "") + "\"";

        [Serializable] private class APIResponse { public ContentBlock[] content; }
        [Serializable] private class ContentBlock { public string type; public string text; }

        [Serializable]
        private class ScoreResult {
            public float assertiveness;
            public float empathy;
            public float emotional_regulation;
            public float social_confidence;
            public float prosocial_intent;
            public bool invalid;
        }
    }
}