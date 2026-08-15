using UnityEngine;
using UnityEngine.UI;

public class HazardBarScroller : MonoBehaviour
{
    [Tooltip("How fast the bar scrolls. Positive = to the right, negative = to the left")]
    public float ScrollSpeed = 0.15f;

    private RawImage rawImage;

    void Awake()
    {
        rawImage = GetComponent<RawImage>();
    }

    void Update()
    {
        Rect uv = rawImage.uvRect;
        uv.x += ScrollSpeed * Time.deltaTime;
        rawImage.uvRect = uv;
    }
}
