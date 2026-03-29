using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class RadarChart : Graphic
{
    [Range(0, 10)] public float assertiveness = 0f;
    [Range(0, 10)] public float empathy = 0f;
    [Range(0, 10)] public float emotionalRegulation = 0f;
    [Range(0, 10)] public float socialConfidence = 0f;
    [Range(0, 10)] public float prosocialIntent = 0f;

    public Color fillColor = new Color(0.32f, 0.29f, 0.71f, 0.25f);
    public Color outlineColor = new Color(0.32f, 0.29f, 0.71f, 1f);
    public Color gridColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
    public float outlineThickness = 3f;
    public float gridThickness = 1.5f;
    public float animationSpeed = 5f;

    private float[] targetScores = new float[5];
    private float[] animatedScores = new float[] { 0, 0, 0, 0, 0 };
    private bool animating = false;

    private float[] Scores => new float[]
    {
        assertiveness, empathy, emotionalRegulation, socialConfidence, prosocialIntent
    };

    void Update()
    {
        if (!animating) return;

        bool done = true;
        for (int i = 0; i < 5; i++)
        {
            animatedScores[i] = Mathf.MoveTowards(
                animatedScores[i],
                targetScores[i],
                Time.deltaTime * animationSpeed * 1f
            );
            if (animatedScores[i] < targetScores[i]) done = false;
        }

        assertiveness =         animatedScores[0];
        empathy =               animatedScores[1];
        emotionalRegulation =   animatedScores[2];
        socialConfidence =      animatedScores[3];
        prosocialIntent =       animatedScores[4];

        SetVerticesDirty();
        if (done) animating = false;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        int n = 5;
        float cx = rectTransform.rect.width / 2f;
        float cy = rectTransform.rect.height / 2f;
        float radius = Mathf.Min(cx, cy) * 0.75f;

        for (int level = 1; level <= 5; level++)
        {
            float r = radius * level / 5f;
            DrawPolygonOutline(vh, cx, cy, r, n, gridColor, gridThickness);
        }

        for (int i = 0; i < n; i++)
        {
            Vector2 pt = GetPoint(cx, cy, radius, i, n);
            DrawLine(vh, new Vector2(cx, cy), pt, gridColor, gridThickness);
        }

        DrawFilledPolygon(vh, cx, cy, radius, n, Scores, fillColor);
        DrawDataOutline(vh, cx, cy, radius, n, Scores, outlineColor, outlineThickness);

        for (int i = 0; i < n; i++)
        {
            Vector2 pt = GetPoint(cx, cy, radius * Scores[i] / 10f, i, n);
            DrawDot(vh, pt, outlineColor, 5f);
        }
    }

    Vector2 GetPoint(float cx, float cy, float r, int i, int n)
    {
        float angle = Mathf.PI * 2f * i / n - Mathf.PI / 2f;
        return new Vector2(cx + r * Mathf.Cos(angle), cy + r * Mathf.Sin(angle));
    }

    void DrawFilledPolygon(VertexHelper vh, float cx, float cy, float r, int n, float[] scores, Color col)
    {
        int startIdx = vh.currentVertCount;
        UIVertex center = UIVertex.simpleVert;
        center.color = col;
        center.position = new Vector3(cx, cy);
        vh.AddVert(center);

        for (int i = 0; i < n; i++)
        {
            Vector2 pt = GetPoint(cx, cy, r * scores[i] / 10f, i, n);
            UIVertex v = UIVertex.simpleVert;
            v.color = col;
            v.position = new Vector3(pt.x, pt.y);
            vh.AddVert(v);
        }

        for (int i = 0; i < n; i++)
            vh.AddTriangle(startIdx, startIdx + i + 1, startIdx + (i + 1) % n + 1);
    }

    void DrawPolygonOutline(VertexHelper vh, float cx, float cy, float r, int n, Color col, float thickness)
    {
        for (int i = 0; i < n; i++)
        {
            Vector2 a = GetPoint(cx, cy, r, i, n);
            Vector2 b = GetPoint(cx, cy, r, (i + 1) % n, n);
            DrawLine(vh, a, b, col, thickness);
        }
    }

    void DrawDataOutline(VertexHelper vh, float cx, float cy, float r, int n, float[] scores, Color col, float thickness)
    {
        for (int i = 0; i < n; i++)
        {
            Vector2 a = GetPoint(cx, cy, r * scores[i] / 10f, i, n);
            Vector2 b = GetPoint(cx, cy, r * scores[(i + 1) % n] / 10f, (i + 1) % n, n);
            DrawLine(vh, a, b, col, thickness);
        }
    }

    void DrawLine(VertexHelper vh, Vector2 a, Vector2 b, Color col, float thickness)
    {
        Vector2 dir = (b - a).normalized;
        Vector2 perp = new Vector2(-dir.y, dir.x) * (thickness / 2f);
        int idx = vh.currentVertCount;
        UIVertex v = UIVertex.simpleVert;
        v.color = col;
        v.position = new Vector3(a.x + perp.x, a.y + perp.y); vh.AddVert(v);
        v.position = new Vector3(a.x - perp.x, a.y - perp.y); vh.AddVert(v);
        v.position = new Vector3(b.x - perp.x, b.y - perp.y); vh.AddVert(v);
        v.position = new Vector3(b.x + perp.x, b.y + perp.y); vh.AddVert(v);
        vh.AddTriangle(idx, idx + 1, idx + 2);
        vh.AddTriangle(idx, idx + 2, idx + 3);
    }

    void DrawDot(VertexHelper vh, Vector2 center, Color col, float radius)
    {
        int segments = 8;
        int startIdx = vh.currentVertCount;
        UIVertex v = UIVertex.simpleVert;
        v.color = col;
        v.position = new Vector3(center.x, center.y);
        vh.AddVert(v);

        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            v.position = new Vector3(
                center.x + radius * Mathf.Cos(angle),
                center.y + radius * Mathf.Sin(angle)
            );
            vh.AddVert(v);
        }

        for (int i = 0; i < segments; i++)
            vh.AddTriangle(startIdx, startIdx + i + 1, startIdx + (i + 1) % segments + 1);
    }

    public void UpdateScores(float a, float e, float er, float sc, float pi)
    {
        targetScores = new float[] { a, e, er, sc, pi };
        animatedScores = new float[] { 0, 0, 0, 0, 0 };
        animating = true;
    }

    public void UpdateScoresFromArray(float[] scores)
    {
        if (scores.Length != 5) return;
        UpdateScores(scores[0], scores[1], scores[2], scores[3], scores[4]);
    }
}