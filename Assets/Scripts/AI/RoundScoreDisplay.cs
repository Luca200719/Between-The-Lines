using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SocialScenarios;

/// <summary>
/// End-of-session results panel.
/// Hidden during gameplay — call ShowFinal() once all rounds are done.
///
/// Works with your existing SocialProfile (running averages) for the
/// overall score, and RoundHistory for per-round breakdown rows.
///
/// Leave all Inspector refs null to auto-build the panel at runtime.
/// </summary>
public class RoundScoreDisplay : MonoBehaviour
{
    public static RoundScoreDisplay Instance { get; private set; }

    [Header("Assign or leave null to auto-build")]
    public RectTransform trackerPanel;
    public Transform roundRowContainer;
    public TMP_Text overallValueText;
    public TMP_Text sessionSummaryText;

    [Header("Prefab (optional)")]
    public GameObject roundRowPrefab;

    [Header("Style")]
    public Color barBackground = new Color(0.15f, 0.15f, 0.15f);
    public Color[] traitColors = {
        new Color(0.96f, 0.49f, 0.37f),  // assertiveness  – coral
        new Color(0.37f, 0.83f, 0.74f),  // empathy        – teal
        new Color(0.95f, 0.83f, 0.37f),  // emotional reg  – gold
        new Color(0.55f, 0.72f, 0.96f),  // social conf    – sky blue
        new Color(0.72f, 0.55f, 0.96f),  // prosocial      – lavender
    };
    public Color totalBarColor = Color.white;
    public float barHeight = 14f;
    public float rowHeight = 48f;

    private static readonly string[] ShortNames =
        { "ASSERT", "EMPATH", "EMREG", "SCONF", "PROS", "TOTAL" };

    // ── Lifecycle ─────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (trackerPanel == null)
            BuildUIHierarchy();

        trackerPanel.gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────

    /// <summary>
    /// Reveal and populate the results screen.
    /// Pass your existing SocialProfile for the overall 0-10 score,
    /// and RoundHistory for the per-round rows.
    /// </summary>
    public void ShowFinal(RoundHistory history, SocialProfile profile)
    {
        trackerPanel.gameObject.SetActive(true);
        StartCoroutine(AnimateResults(history, profile));
    }

    public void Reset()
    {
        if (roundRowContainer != null)
            foreach (Transform child in roundRowContainer)
                Destroy(child.gameObject);

        if (overallValueText) overallValueText.text = "—";
        if (sessionSummaryText) sessionSummaryText.text = "";
        trackerPanel.gameObject.SetActive(false);
    }

    // ── Animation ─────────────────────────────────────────────────────

    private IEnumerator AnimateResults(RoundHistory history, SocialProfile profile)
    {
        foreach (var rs in history.Rounds)
        {
            var row = SpawnRoundRow(rs);
            yield return StartCoroutine(
                FadeIn(row.GetComponent<CanvasGroup>() ?? row.AddComponent<CanvasGroup>())
            );
            yield return new WaitForSeconds(0.08f);
        }

        PopulateSessionSummary(profile);
        PopulateOverall(profile);

        if (overallValueText != null)
        {
            var cg = overallValueText.transform.parent.GetComponent<CanvasGroup>()
                  ?? overallValueText.transform.parent.gameObject.AddComponent<CanvasGroup>();
            yield return StartCoroutine(FadeIn(cg));
        }
    }

    // ── Row building ──────────────────────────────────────────────────

    private GameObject SpawnRoundRow(RoundScore rs)
    {
        return roundRowPrefab != null
            ? InstantiatePrefabRow(rs)
            : BuildCodeRow(rs, roundRowContainer);
    }

    private GameObject InstantiatePrefabRow(RoundScore rs)
    {
        var row = Instantiate(roundRowPrefab, roundRowContainer);
        var texts = row.GetComponentsInChildren<TMP_Text>();
        var images = row.GetComponentsInChildren<Image>();

        float[] v = Values10(rs);
        if (texts.Length > 0) texts[0].text = $"R{rs.Round}";
        for (int i = 0; i < 6 && i + 1 < texts.Length; i++) texts[i + 1].text = v[i].ToString("F1");
        for (int i = 0; i < 6 && i < images.Length; i++)
        {
            images[i].fillAmount = v[i] / 10f;
            images[i].color = i < 5 ? traitColors[i] : totalBarColor;
        }
        return row;
    }

    private GameObject BuildCodeRow(RoundScore rs, Transform parent)
    {
        float[] v = Values10(rs);

        var row = NewRect("Row_R" + rs.Round, parent);
        SetLayout(row, rowHeight);
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.spacing = 6f;
        hlg.padding = new RectOffset(4, 4, 4, 4);
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // Round label
        var lbl = NewRect("Lbl", row.transform);
        SetSize(lbl, 36f, rowHeight);
        AddTMP(lbl, $"R{rs.Round}", 11f, Color.gray, TextAlignmentOptions.MidlineLeft);

        for (int i = 0; i < 6; i++)
        {
            bool isTotal = i == 5;
            Color barColor = isTotal ? totalBarColor : traitColors[i];
            float val = v[i];

            var col = NewRect($"Col{i}", row.transform);
            SetSize(col, 60f, rowHeight);
            var vlg = col.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.spacing = 2f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Value label
            var valGO = NewRect("Val", col.transform);
            SetSize(valGO, 60f, 16f);
            var tmp = AddTMP(valGO, val.ToString("F1"), isTotal ? 13f : 11f,
                             isTotal ? Color.white : barColor, TextAlignmentOptions.Center);
            if (isTotal) tmp.fontStyle = FontStyles.Bold;

            // Bar background
            var bgGO = NewRect("BG", col.transform);
            SetSize(bgGO, 60f, barHeight);
            bgGO.AddComponent<Image>().color = barBackground;

            // Bar fill
            var fill = NewRect("Fill", bgGO.transform);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(val / 10f, 1f);
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            fill.AddComponent<Image>().color = barColor;
        }

        return row;
    }

    // ── Session summary — driven by existing SocialProfile ────────────

    private void PopulateSessionSummary(SocialProfile p)
    {
        if (sessionSummaryText == null) return;
        // SocialProfile.ToVector() returns the running averages (0-1)
        float[] vec = p.ToVector();
        sessionSummaryText.text =
            $"Session avg  " +
            $"Assert <color=#F57D5E>{vec[0] * 10f:F1}</color>  " +
            $"Empath <color=#5ED4BD>{vec[1] * 10f:F1}</color>  " +
            $"EmReg <color=#F2D45E>{vec[2] * 10f:F1}</color>  " +
            $"SConf <color=#8DB7F5>{vec[3] * 10f:F1}</color>  " +
            $"Pros <color=#B08CF5>{vec[4] * 10f:F1}</color>";
    }

    private void PopulateOverall(SocialProfile p)
    {
        if (overallValueText == null) return;
        float[] vec = p.ToVector();
        float avg01 = (vec[0] + vec[1] + vec[2] + vec[3] + vec[4]) / 5f;
        float score10 = Mathf.Clamp(avg01 * 10f, 0f, 10f);
        overallValueText.text = score10.ToString("F1");
    }

    // ── Auto-build UI ─────────────────────────────────────────────────

    private void BuildUIHierarchy()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var cgo = new GameObject("TrackerCanvas");
            canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cgo.AddComponent<CanvasScaler>();
            cgo.AddComponent<GraphicRaycaster>();
        }

        var panel = NewRect("ScoreTrackerPanel", canvas.transform);
        trackerPanel = panel.GetComponent<RectTransform>();
        trackerPanel.anchorMin = new Vector2(0.1f, 0.05f);
        trackerPanel.anchorMax = new Vector2(0.9f, 0.95f);
        trackerPanel.offsetMin = trackerPanel.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.95f);

        var vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 4f;
        vlg.padding = new RectOffset(16, 16, 16, 16);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Title
        var title = NewRect("Title", panel.transform);
        SetSize(title, 0f, 36f);
        AddTMP(title, "SESSION RESULTS", 18f, Color.white,
               TextAlignmentOptions.Center).fontStyle = FontStyles.Bold;

        // Column headers
        var header = NewRect("Header", panel.transform);
        SetSize(header, 0f, 22f);
        var hhlg = header.AddComponent<HorizontalLayoutGroup>();
        hhlg.spacing = 6f;
        hhlg.padding = new RectOffset(40, 0, 0, 0);
        hhlg.childForceExpandWidth = false;
        hhlg.childForceExpandHeight = false;
        for (int i = 0; i < ShortNames.Length; i++)
        {
            var hc = NewRect($"H{i}", header.transform);
            SetSize(hc, 60f, 22f);
            AddTMP(hc, ShortNames[i], 8f, i < 5 ? traitColors[i] : Color.gray,
                   TextAlignmentOptions.Center);
        }

        // Scrollable round rows
        var scrollGO = NewRect("Scroll", panel.transform);
        SetSize(scrollGO, 0f, 280f);
        var scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = false;

        var vp = NewRect("Viewport", scrollGO.transform);
        var vpRect = vp.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero; vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = vpRect.offsetMax = Vector2.zero;
        vp.AddComponent<Image>().color = Color.clear;
        vp.AddComponent<Mask>().showMaskGraphic = false;
        scroll.viewport = vpRect;

        var content = NewRect("Content", vp.transform);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = Vector2.zero;
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var cvlg = content.AddComponent<VerticalLayoutGroup>();
        cvlg.spacing = 2f;
        cvlg.childForceExpandWidth = true;
        cvlg.childForceExpandHeight = false;
        scroll.content = contentRect;
        roundRowContainer = content.transform;

        // Divider
        var div = NewRect("Div", panel.transform);
        SetSize(div, 0f, 1f);
        div.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);

        // Session summary text
        var sessGO = NewRect("SessionSummary", panel.transform);
        SetSize(sessGO, 0f, 36f);
        sessionSummaryText = AddTMP(sessGO, "", 10f, Color.gray,
                                    TextAlignmentOptions.Center);

        // Overall badge
        var badge = NewRect("Badge", panel.transform);
        SetSize(badge, 0f, 72f);
        var bhlg = badge.AddComponent<HorizontalLayoutGroup>();
        bhlg.childAlignment = TextAnchor.MiddleCenter;
        bhlg.spacing = 8f;
        bhlg.childForceExpandWidth = false;

        var ovLbl = NewRect("OvLbl", badge.transform);
        SetSize(ovLbl, 90f, 40f);
        AddTMP(ovLbl, "OVERALL", 11f, Color.gray, TextAlignmentOptions.MidlineRight);

        var ovVal = NewRect("OvVal", badge.transform);
        SetSize(ovVal, 80f, 60f);
        overallValueText = AddTMP(ovVal, "—", 46f, Color.white,
                                   TextAlignmentOptions.MidlineLeft);
        overallValueText.fontStyle = FontStyles.Bold;

        var ovSub = NewRect("OvSub", badge.transform);
        SetSize(ovSub, 30f, 40f);
        AddTMP(ovSub, "/10", 14f, Color.gray, TextAlignmentOptions.MidlineLeft);
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private static float[] Values10(RoundScore rs) => new[] {
        rs.AssertivenessOut, rs.EmpathyOut, rs.EmotionalRegulationOut,
        rs.SocialConfidenceOut, rs.ProsocialIntentOut, rs.TotalOut
    };

    private static GameObject NewRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static void SetSize(GameObject go, float w, float h)
    {
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = h; le.minHeight = h;
        if (w > 0) { le.preferredWidth = w; le.minWidth = w; }
        else { le.flexibleWidth = 1f; }
    }

    private static void SetLayout(GameObject go, float h)
    {
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = h; le.minHeight = h; le.flexibleWidth = 1f;
    }

    private static TMP_Text AddTMP(GameObject go, string text, float size,
                                    Color color, TextAlignmentOptions align)
    {
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
        return t;
    }

    private static IEnumerator FadeIn(CanvasGroup cg)
    {
        cg.alpha = 0f;
        float e = 0f;
        while (e < 0.25f) { e += Time.deltaTime; cg.alpha = Mathf.Clamp01(e / 0.25f); yield return null; }
        cg.alpha = 1f;
    }
}