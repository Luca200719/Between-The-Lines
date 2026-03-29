using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;

namespace SocialScenarios {
    public class ScenarioManager : MonoBehaviour {
        [Header("Dialogue Cubes")]
        public List<DialogueEntry> dialogues = new();

        [Header("Canvas Overlay")]
        public CanvasGroup overlayCanvasGroup;
        public TMP_Text promptText;
        public TMP_InputField answerInput;
        public Button submitButton;
        public TMP_Text statusText;

        [Header("Camera")]
        public Transform cameraTransform;
        public float cameraMoveDuration = 1.5f;
        public float fadeDuration = 0.4f;

        private SocialProfile profile = new();
        private AIScorer scorer;
        private HashSet<int> usedIds = new();
        private DialogueEntry currentDialogue;
        private int roundCount = 0;
        private const int MaxRounds = 5;

        void Awake() {
            scorer = GetComponent<AIScorer>();
            overlayCanvasGroup.alpha = 0;
            overlayCanvasGroup.interactable = false;
            overlayCanvasGroup.blocksRaycasts = false;
            statusText.text = "";

            submitButton.onClick.AddListener(OnSubmit);
        }

        public void BeginRound(DialogueEntry dialogue, string question) {
            if (roundCount >= MaxRounds) return;

            currentDialogue = dialogue;
            promptText.text = question;
            answerInput.text = "";
            statusText.text = "";

            StartCoroutine(FadeOverlay(true, () => answerInput.ActivateInputField()));
        }

        private void OnSubmit() {
            if (string.IsNullOrWhiteSpace(answerInput.text)) return;
            StartCoroutine(ProcessRound(answerInput.text));
        }

        private IEnumerator ProcessRound(string userAnswer) {
            submitButton.interactable = false;
            statusText.text = "Thinking...";

            float[] scores = null;
            string error = null;

            yield return scorer.ScoreRound(
                conversationContext: currentDialogue.conversationText,
                question: promptText.text,
                userAnswer: userAnswer,
                onComplete: s => scores = s,
                onError: e => error = e
            );

            if (error != null) {
                statusText.text = "Something went wrong. Please try again.";
                submitButton.interactable = true;
                Debug.LogError(error);
                yield break;
            }

            profile.Accumulate(scores);
            roundCount++;
            Debug.Log($"[Round {roundCount}/{MaxRounds}] {profile}");

            yield return FadeOverlay(false);

            if (roundCount >= MaxRounds) {
                Debug.Log("Session complete. Final profile: " + profile);
                yield break;
            }

            DialogueEntry next = GetBestMatch();
            if (next == null) {
                Debug.LogWarning("No unused dialogues left.");
                yield break;
            }

            usedIds.Add(next.id);
            yield return MoveCamera(next.cameraTarget);

            Debug.Log($"Camera arrived at: {next.title}");
        }

        private DialogueEntry GetBestMatch() {
            float[] userVec = profile.ToVector();
            DialogueEntry best = null;
            float bestScore = float.MinValue;

            foreach (var d in dialogues) {
                if (usedIds.Contains(d.id)) continue;

                float score = CosineSimilarity(userVec, d.TraitVector);
                if (score > bestScore) { bestScore = score; best = d; }
            }

            return best;
        }

        private static float CosineSimilarity(float[] a, float[] b) {
            float dot = 0, magA = 0, magB = 0;
            for (int i = 0; i < a.Length; i++) {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }
            float denom = Mathf.Sqrt(magA) * Mathf.Sqrt(magB);
            return denom < 1e-6f ? 0f : dot / denom;
        }

        private IEnumerator MoveCamera(Transform target) {
            Vector3 startPos = cameraTransform.position;
            Quaternion startRot = cameraTransform.rotation;
            float elapsed = 0f;

            while (elapsed < cameraMoveDuration) {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / cameraMoveDuration);
                cameraTransform.position = Vector3.Lerp(startPos, target.position, t);
                cameraTransform.rotation = Quaternion.Slerp(startRot, target.rotation, t);
                yield return null;
            }

            cameraTransform.position = target.position;
            cameraTransform.rotation = target.rotation;
        }

        private IEnumerator FadeOverlay(bool fadeIn, System.Action onComplete = null) {
            float start = fadeIn ? 0f : 1f;
            float end = fadeIn ? 1f : 0f;
            float elapsed = 0f;

            overlayCanvasGroup.interactable = fadeIn;
            overlayCanvasGroup.blocksRaycasts = fadeIn;

            while (elapsed < fadeDuration) {
                elapsed += Time.deltaTime;
                overlayCanvasGroup.alpha = Mathf.Lerp(start, end, elapsed / fadeDuration);
                yield return null;
            }

            overlayCanvasGroup.alpha = end;
            onComplete?.Invoke();
        }
    }
}