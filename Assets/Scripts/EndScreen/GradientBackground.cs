using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class GradientBackground : MonoBehaviour
{
    public Color topColor = new Color(0.95f, 0.95f, 1f);
    public Color bottomColor = new Color(0.85f, 0.88f, 0.98f);

    void Start()
    {
        Texture2D tex = new Texture2D(1, 2);
        tex.SetPixel(0, 0, bottomColor);
        tex.SetPixel(0, 1, topColor);
        tex.Apply();
        GetComponent<RawImage>().texture = tex;
    }
}