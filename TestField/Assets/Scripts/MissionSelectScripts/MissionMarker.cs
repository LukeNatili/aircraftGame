using UnityEngine;

public class MissionMarker : MonoBehaviour
{
    public string MissionId;
    public string MissionName;
    [TextArea] public string MissionDescription;

    public Renderer MarkerRenderer;
    public Color IdleColor = Color.white;
    public Color HighlightColor = Color.yellow;
    public string EmissionProperty = "_EmissionColor";

    private MaterialPropertyBlock mpb;

    void Awake()
    {
        mpb = new MaterialPropertyBlock();
    }

    public void SetHighlighted(bool highlighted)
    {
        if (!MarkerRenderer) return;
        MarkerRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(EmissionProperty, highlighted ? HighlightColor : IdleColor);
        MarkerRenderer.SetPropertyBlock(mpb);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
