using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SocialScenarios;

/// <summary>
/// Attach to an empty GameObject alongside AIScorer.
///
/// Canvas hierarchy:
///   Canvas (has CanvasGroup, Alpha=0, Interactable=false, BlocksRaycasts=false)
///    └── Panel
///         ├── PromptText     (TMP_Text)
///         ├── AnswerInput    (TMP_InputField)
///         └── SubmitButton   (Button)
/// </summary>
public class ScenarioManager : MonoBehaviour {
    public static ScenarioManager Instance { get; private set; }

    [Header("Dialogue Cubes")]
    public List<DialogueEntry> dialogues = new();

    [Header("Canvas Overlay")]
    public CanvasGroup overlayCanvasGroup;
    public TMP_Text promptText;
    public TMP_InputField answerInput;
    public Button submitButton;

    [Header("Camera")]
    public Transform cameraTransform;
    public float cameraMoveDuration = 1.5f;
    public float fadeDuration = 0.4f;

    // ── State ─────────────────────────────────────────────────────────
    private SocialProfile profile = new();
    private AIScorer scorer;
    private HashSet<int> usedIds = new();
    private int roundCount = 0;
    private const int MaxRounds = 5;

    private string pendingConversation;
    private string pendingQuestion;
    private DialogueEntry pendingEntry;

    // ── Lifecycle ─────────────────────────────────────────────────────

    void Awake() {
        Instance = this;
        scorer = GetComponent<AIScorer>();

        overlayCanvasGroup.alpha = 0f;
        overlayCanvasGroup.interactable = false;
        overlayCanvasGroup.blocksRaycasts = false;

        submitButton.onClick.AddListener(OnSubmit);
    }

    // ── Called by Dialogue.cs when a conversation finishes ────────────

    /// <summary>
    /// Dialogue.cs calls this when RunDialogue finishes.
    /// conversationText is the full A/B transcript built from the lines array.
    /// </summary>
    public void BeginRound(DialogueEntry entry, string conversationText, string question) {
        if (roundCount >= MaxRounds) return;

        pendingEntry = entry;
        pendingConversation = conversationText;
        pendingQuestion = question;

        usedIds.Add(entry.id);

        promptText.text = question;
        answerInput.text = "";

        StartCoroutine(FadeOverlay(true, () => answerInput.ActivateInputField()));
    }

    // ── Submit ────────────────────────────────────────────────────────

    private void OnSubmit() {
        if (string.IsNullOrWhiteSpace(answerInput.text)) return;
        StartCoroutine(ProcessRound(answerInput.text));
    }

    private IEnumerator ProcessRound(string userAnswer) {
        submitButton.interactable = false;

        float[] scores = null;
        string error = null;

        yield return scorer.ScoreRound(
            conversationContext: pendingConversation,
            question: pendingQuestion,
            userAnswer: userAnswer,
            onComplete: s => scores = s,
            onError: e => error = e
        );

        submitButton.interactable = true;

        if (error != null) {
            Debug.LogError($"Scoring error: {error}");
            yield break;
        }

        profile.Accumulate(scores);
        roundCount++;

        Debug.Log($"[Round {roundCount}/{MaxRounds}] {profile}");

        // Fade out overlay then move camera
        yield return FadeOverlay(false);

        if (roundCount >= MaxRounds) {
            Debug.Log("Session complete. Final profile: " + profile);
            // Add your end-of-session logic here
            yield break;
        }

        DialogueEntry next = GetBestMatch();
        if (next == null) {
            Debug.LogWarning("No unused dialogues remaining.");
            yield break;
        }

        yield return MoveCamera(next.cameraTarget);

        // Camera has arrived — your DialogueManager will pick up from here
        // via its existing queue, or call next.GetComponent<Dialogue>().Play()
        Debug.Log($"Arrived at: {next.title}");
    }

    // ── Matching ──────────────────────────────────────────────────────

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

    // ── Camera ────────────────────────────────────────────────────────

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

    // ── Fade ──────────────────────────────────────────────────────────

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