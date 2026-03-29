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
///         ├── PromptText       (TMP_Text)
///         ├── AnswerInput      (TMP_InputField)
///         ├── SubmitButton     (Button)
///         └── ValidationText   (TMP_Text) ← add this, style red, alpha 0
/// </summary>
public class ScenarioManager : MonoBehaviour {
    public static ScenarioManager Instance { get; private set; }

    [Header("Dialogue Cubes")]
    public List<DialogueEntry> dialogues = new();

    [Header("Starting Dialogue")]
    public int startId = 0;

    [Header("Canvas Overlay")]
    public CanvasGroup overlayCanvasGroup;
    public TMP_Text promptText;
    public TMP_InputField answerInput;
    public Button submitButton;
    public TMP_Text validationTextBubble;
    public TMP_Text validationText;

    [Header("Camera")]
    public float fadeDuration = 0.4f;

    // ── State ─────────────────────────────────────────────────────────
    private SocialProfile profile = new();
    private AIScorer scorer;
    public HashSet<int> usedIds = new();
    private int roundCount = 0;
    private const int MaxRounds = 5;

    private string pendingConversation;
    private string pendingQuestion;
    private DialogueEntry pendingEntry;

    // ── Lifecycle ─────────────────────────────────────────────────────

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        scorer = GetComponent<AIScorer>();

        overlayCanvasGroup.alpha = 0f;
        overlayCanvasGroup.interactable = false;
        overlayCanvasGroup.blocksRaycasts = false;

        validationText.alpha = 0f;
        validationTextBubble.alpha = 0f;

        submitButton.onClick.AddListener(OnSubmit);
    }

    void Start() {
        DialogueEntry start = dialogues.Find(d => d != null && d.id == startId);

        if (start == null) {
            Debug.LogWarning($"ScenarioManager: no DialogueEntry found with startId={startId}.");
            return;
        }

        if (CameraController.Instance == null) {
            Debug.LogError("ScenarioManager: CameraController.Instance is null.");
            return;
        }

        if (start.cameraTarget == null) {
            Debug.LogError($"ScenarioManager: cameraTarget not assigned on DialogueEntry id={startId}.");
            return;
        }

        CameraController.Instance.SnapTo(start.cameraTarget);
    }

    // ── Called by Dialogue.cs when a conversation finishes ────────────

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
        string answer = answerInput.text.Trim();
        if (string.IsNullOrWhiteSpace(answer)) return;
        StartCoroutine(ProcessRound(answer));
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

        if (error == "INVALID_ANSWER") {
            StartCoroutine(FlashValidation("Please enter a relevant response."));
            yield break;
        }

        if (error != null) {
            Debug.LogError($"Scoring error: {error}");
            yield break;
        }

        profile.Accumulate(scores);
        roundCount++;

        Debug.Log($"[Round {roundCount}/{MaxRounds}] {profile}");

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

        bool arrived = false;
        CameraController.Instance.MoveTo(next.cameraTarget, () => arrived = true);
        yield return new WaitUntil(() => arrived);

        Dialogue nextDialogue = next.GetComponent<Dialogue>();
        if (nextDialogue != null)
            DialogueManager.dialogueManager.PlayDialogue(nextDialogue);
        else
            Debug.LogWarning($"No Dialogue component on cube: {next.title}");
    }

    // ── Validation Flash ──────────────────────────────────────────────

    private IEnumerator FlashValidation(string message) {
        validationText.text = message;
        validationTextBubble.text = message;

        CanvasGroup textGroup = validationText.GetComponent<CanvasGroup>(); 
        CanvasGroup bubbleGroup = validationTextBubble.GetComponent<CanvasGroup>();

        // Fade in
        float elapsed = 0f;
        while (elapsed < 0.2f) {
            elapsed += Time.deltaTime;
            textGroup.alpha = Mathf.Clamp01(elapsed / 0.2f);
            bubbleGroup.alpha = Mathf.Clamp01(elapsed / 0.2f);
            yield return null;
        }
        validationText.alpha = 1f;
        validationTextBubble.alpha = 1f;

        yield return new WaitForSeconds(1.5f);

        // Fade out
        elapsed = 0f;
        while (elapsed < 0.2f) {
            elapsed += Time.deltaTime;
            textGroup.alpha = Mathf.Clamp01(1f - elapsed / 0.2f);
            bubbleGroup.alpha = Mathf.Clamp01(1f - elapsed / 0.2f);
            yield return null;
        }
        validationText.alpha = 0f;
        validationTextBubble.alpha = 0f;
        validationText.text = "";
        validationTextBubble.text = "";
    }

    // ── Matching ──────────────────────────────────────────────────────

    private DialogueEntry GetBestMatch() {
        float[] userVec = profile.ToVector();
        DialogueEntry best = null;
        float bestScore = float.MinValue;

        foreach (var d in dialogues) {
            if (d == null || usedIds.Contains(d.id)) continue;

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

    // ── Fade Overlay ──────────────────────────────────────────────────

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