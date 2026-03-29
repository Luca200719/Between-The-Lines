using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreBars : MonoBehaviour
{
    public float animationSpeed = 2f;
    public TMP_FontAsset customFont;

    private float[] targetValues = new float[5];
    private float[] currentValues = new float[5];
    private Image[] fillBars = new Image[5];
    private TextMeshProUGUI[] scoreLabels = new TextMeshProUGUI[5];

    private string[] names = {
        "Assertiveness", "Empathy", "Emotional Regulation",
        "Social Confidence", "Prosocial Intent"
    };

    private Color[] colors = {
        new Color(0.49f, 0.45f, 0.87f),
        new Color(0.11f, 0.62f, 0.46f),
        new Color(0.85f, 0.35f, 0.19f),
        new Color(0.09f, 0.37f, 0.65f),
        new Color(0.73f, 0.46f, 0.09f)
    };

    void Awake()
    {
        BuildBars();
    }

    void BuildBars()
    {
        VerticalLayoutGroup vlg = gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 12;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(10, 10, 10, 10);

        Sprite whiteSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");

        for (int i = 0; i < 5; i++)
        {
            GameObject row = new GameObject("Bar_" + names[i]);
            row.transform.SetParent(transform, false);
            row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 30);

            HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(row.transform, false);
            labelObj.AddComponent<RectTransform>().sizeDelta = new Vector2(160, 30);
            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = names[i];
            label.fontSize = 14;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            if (customFont != null) label.font = customFont;

            // Back
            GameObject backObj = new GameObject("Back");
            backObj.transform.SetParent(row.transform, false);
            backObj.AddComponent<RectTransform>().sizeDelta = new Vector2(200, 20);
            Image backImage = backObj.AddComponent<Image>();
            backImage.sprite = whiteSprite;
            backImage.color = new Color(0.88f, 0.88f, 0.88f, 0.3f);
            backImage.type = Image.Type.Sliced;
            LayoutElement le = backObj.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 20;

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(backObj.transform, false);
            RectTransform fillRT = fillObj.AddComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.sizeDelta = Vector2.zero;
            fillRT.anchoredPosition = Vector2.zero;
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.sprite = whiteSprite;
            fillImage.color = colors[i];
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 0f;
            fillBars[i] = fillImage;

            // Score label
            GameObject scoreObj = new GameObject("ScoreLabel");
            scoreObj.transform.SetParent(row.transform, false);
            scoreObj.AddComponent<RectTransform>().sizeDelta = new Vector2(45, 30);
            TextMeshProUGUI scoreLabel = scoreObj.AddComponent<TextMeshProUGUI>();
            scoreLabel.text = "0";
            scoreLabel.fontSize = 14;
            scoreLabel.color = Color.white;
            scoreLabel.alignment = TextAlignmentOptions.MidlineRight;
            if (customFont != null) scoreLabel.font = customFont;
            scoreLabels[i] = scoreLabel;
        }
    }

    public void Show(float[] scores)
    {
        for (int i = 0; i < 5; i++)
        {
            targetValues[i] = Mathf.Clamp(scores[i], 0f, 10f) / 10f;
            currentValues[i] = 0f;
            fillBars[i].fillAmount = 0f;
            scoreLabels[i].text = "0";
        }
    }

    void Update()
    {
        for (int i = 0; i < 5; i++)
        {
            if (currentValues[i] < targetValues[i])
            {
                currentValues[i] = Mathf.MoveTowards(
                    currentValues[i], targetValues[i], Time.deltaTime * animationSpeed
                );
                fillBars[i].fillAmount = currentValues[i];
                scoreLabels[i].text = (currentValues[i] * 10f).ToString("F1");
            }
        }
    }
}