using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class ClipDiffPrinter
{
    [MenuItem("Tools/Diff Moving Bones Between Selected Clips")]
    static void DiffMovingBones()
    {
        var clips = Selection.objects.OfType<AnimationClip>().ToArray();
        if (clips.Length != 2)
        {
            Debug.LogError("Select exactly two AnimationClips in the Project window (ctrl/cmd-click both), then run this again.");
            return;
        }

        var setA = GetMovingBonePaths(clips[0]);
        var setB = GetMovingBonePaths(clips[1]);

        Debug.Log($"--- Only in {clips[0].name} ({setA.Except(setB).Count()} bones) ---");
        foreach (var path in setA.Except(setB))
            Debug.Log(path == "" ? "(root)" : path);

        Debug.Log($"--- Only in {clips[1].name} ({setB.Except(setA).Count()} bones) ---");
        foreach (var path in setB.Except(setA))
            Debug.Log(path == "" ? "(root)" : path);

        Debug.Log($"--- Moving in BOTH ({setA.Intersect(setB).Count()} bones) ---");
        foreach (var path in setA.Intersect(setB))
            Debug.Log(path == "" ? "(root)" : path);
    }

    static HashSet<string> GetMovingBonePaths(AnimationClip clip)
    {
        var result = new HashSet<string>();
        foreach (var b in AnimationUtility.GetCurveBindings(clip))
        {
            var curve = AnimationUtility.GetEditorCurve(clip, b);
            if (curve == null || curve.length < 2) continue;

            float min = float.MaxValue, max = float.MinValue;
            foreach (var key in curve.keys)
            {
                min = Mathf.Min(min, key.value);
                max = Mathf.Max(max, key.value);
            }

            if (max - min > 0.001f)
                result.Add(b.path);
        }
        return result;
    }
}