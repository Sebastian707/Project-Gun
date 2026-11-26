using UnityEngine;

public class WheelHoverEffect : MonoBehaviour
{
    private Renderer wheelRenderer;

    [Header("Colors")]
    public Color normalColor = Color.white;

    // The color when the ray points at it
    public Color hoverColor = Color.yellow; 

    void Start()
    {
        wheelRenderer = GetComponent<Renderer>();
        SetColor(normalColor);
    }

    // When the ray enters the wheel
    public void OnHoverEnter()
    {
        SetColor(hoverColor);
    }

    // When the ray leaves the wheel
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