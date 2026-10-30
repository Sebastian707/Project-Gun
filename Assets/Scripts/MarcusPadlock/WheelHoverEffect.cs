using UnityEngine;

public class WheelHoverEffect : MonoBehaviour
{
    private Renderer wheelRenderer;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow; // The color when the ray points at it

    void Start()
    {
        wheelRenderer = GetComponent<Renderer>();
        SetColor(normalColor);
    }

    // Call this when the ray enters the wheel
    public void OnHoverEnter()
    {
        SetColor(hoverColor);
    }

    // Call this when the ray leaves the wheel
    public void OnHoverExit()
    {
        SetColor(normalColor);
    }

    void SetColor(Color col)
    {
        if (wheelRenderer != null)
        {
            wheelRenderer.material.color = col;
        }
    }
}