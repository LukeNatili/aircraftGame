using UnityEngine;
using UnityEditor;

public class ClipMovingBonesPrinter
{
    [MenuItem("Tools/Print Moving Bones In Clip")]
    static void PrintMovingBones()
    {
        AnimationClip clip = Selection.activeObject as AnimationClip;
        if (clip == null)
        {
            Debug.LogError("Select an AnimationClip in the Project window first, then run this again.");
            return;
        }

        var bindings = AnimationUtility.GetCurveBindings(clip);
        Debug.Log($"--- {clip.name}: checking {bindings.Length} curves for actual movement ---");

        int movingCount = 0;
        foreach (var b in bindings)
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, b);
            if (curve == null || curve.length < 2) continue;

            float min = float.MaxValue, max = float.MinValue;
            foreach (var key in curve.keys)
            {
                min = Mathf.Min(min, key.value);
                max = Mathf.Max(max, key.value);
            }

            float range = max - min;
            if (range > 0.001f) // flat curves will be ~0; anything above this is genuinely animated
            {
                movingCount++;
                Debug.Log($"MOVES  {b.path}  ({b.propertyName})  range={range:F4}");
            }
        }
        Debug.Log($"--- {movingCount} of {bindings.Length} curves actually move ---");
    }
}