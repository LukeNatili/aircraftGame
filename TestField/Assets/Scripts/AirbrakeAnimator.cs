using UnityEngine;

public class AirbrakeAnimator : MonoBehaviour
{
    [Tooltip("Reference to the plane's controller — this is the single source of truth for whether the airbrake is deployed.")]
    public PlaneController planeController;

    [Tooltip("Name of the state in the Animator Controller holding the airbrake clip.")]
    public string stateName = "Armature|Move Dive Brakes";

    [Tooltip("The clip itself, used only to read its length so time can be normalized correctly.")]
    public AnimationClip clip;

    [Tooltip("Seconds to play fully open or fully closed. Defaults to the clip's own length if left at 0.")]
    public float PlaybackDuration = 0f;

    private Animator animator;
    private float NormalizedTime = 0f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        animator.speed = 0f; // we drive time manually below

        if (PlaybackDuration <= 0f && clip != null)
        {
            PlaybackDuration = clip.length;
        }
    }

    private void Update()
    {
        bool deployed = planeController != null && planeController.AirbrakeDeployed;

        float step = Time.deltaTime / Mathf.Max(PlaybackDuration, 0.01f);
        NormalizedTime += deployed ? step : -step;
        NormalizedTime = Mathf.Clamp01(NormalizedTime);

        animator.Play(stateName, 0, NormalizedTime);
    }
}
