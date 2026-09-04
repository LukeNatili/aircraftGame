using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;
using UnityEngine.Rendering;

[CustomEditor(typeof(MissionMarkerAuthoring))]
public class MissionMarkerAuthoringEditor : Editor
{
    private bool placementModeActive = false;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MissionMarkerAuthoring authoring = (MissionMarkerAuthoring)target;

        GUILayout.Space(10);
        string buttonLabel = placementModeActive ? "Placing... (Clicl Button or Esc to Stop)" : "Start Placing Markers";

        if (GUILayout.Button(buttonLabel))
        {
            placementModeActive = !placementModeActive;
            SceneView.RepaintAll();
        }

        if (placementModeActive)
        {
            EditorGUILayout.HelpBox("Click anywhere on the globe in Scene view to drop a marker there.", MessageType.Info);
        }
    }

    void OnSceneGUI()
    {
        if (!placementModeActive) return;

        MissionMarkerAuthoring authoring = (MissionMarkerAuthoring)target;
        Event e = Event.current;

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            placementModeActive = false;
            e.Use();
            return;
        }

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Collider globeCollider = authoring.GlobeTransform.GetComponent<Collider>();

            if (globeCollider != null && globeCollider.Raycast(ray, out RaycastHit hit, 1000f))
            {
                PlaceMarker(authoring, hit.point);
                e.Use();
            }
        }

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

    }

    void PlaceMarker(MissionMarkerAuthoring authoring, Vector3 worldHitPoint)
    {
        Vector3 localPoint = authoring.GlobeTransform.InverseTransformPoint(worldHitPoint);
        Vector3 direction = localPoint.normalized;
        float radius = localPoint.magnitude;

        MissionMarker instance = (MissionMarker)PrefabUtility.InstantiatePrefab(authoring.MarkerPrefab, authoring.GlobeTransform);
        instance.transform.localPosition = direction * radius;
        instance.transform.localRotation = Quaternion.FromToRotation(Vector3.forward, direction);

        // Counteract the parent's scale so the marker renders at its authored prefab size
        Vector3 parentScale = authoring.GlobeTransform.lossyScale;
        Vector3 prefabScale = authoring.MarkerPrefab.transform.localScale;
        instance.transform.localScale = new Vector3(
            prefabScale.x / parentScale.x,
            prefabScale.y / parentScale.y,
            prefabScale.z / parentScale.z
            );

        Undo.RegisterCreatedObjectUndo(instance.gameObject, "Place Mission Marker");
    }
}