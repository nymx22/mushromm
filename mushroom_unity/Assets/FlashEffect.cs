using UnityEngine;

public class FlashEffect : MonoBehaviour
{
    public Color flashColor = Color.white;
    public float flashIntensity = 0.8f;

    private Renderer flashRenderer;  // Changed from 'renderer' to 'flashRenderer'
    private Material flashMaterial;

    void Start()
    {
        flashRenderer = GetComponent<Renderer>();  // Changed from 'renderer'
        flashMaterial = new Material(Shader.Find("Unlit/Color"));
        flashMaterial.color = new Color(flashColor.r, flashColor.g, flashColor.b, flashIntensity);
        flashRenderer.material = flashMaterial;  // Changed from 'renderer'
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (gameObject.activeInHierarchy)
        {
            // Make flash cover entire view
            transform.localScale = Vector3.one * 200f;
        }
    }
}