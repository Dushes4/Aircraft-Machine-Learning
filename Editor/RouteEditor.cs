using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Route))]
public class RouteEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var route = (Route)target;

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(route.checkpointPrefab == null))
        {
            if (GUILayout.Button("Generate Radial Fourier Route"))
                Generate(route);
        }

        if (GUILayout.Button("Random Seed"))
        {
            Undo.RecordObject(route, "Randomize Seed");
            route.seed = Random.Range(int.MinValue, int.MaxValue);
            EditorUtility.SetDirty(route);
        }
        EditorGUILayout.EndHorizontal();

        if (route.checkpointPrefab == null)
            EditorGUILayout.HelpBox("Assign Checkpoint Prefab to generate route.", MessageType.Warning);

        if (GUILayout.Button("Clear Checkpoints"))
            Clear(route);
    }


    private static void Generate(Route route)
    {
        Undo.RegisterFullObjectHierarchyUndo(route.gameObject, "Generate Route");
        Clear(route);

        var localPoints = route.GenerateRadialFourierRouteLocal();
        if (localPoints == null || localPoints.Count < 2) return;

        // ⬅️ ВАЖНО: меняем порядок, если нужно по часовой стрелке
        if (route.clockwise)
            localPoints.Reverse();

        var prefab = route.checkpointPrefab.gameObject;

        for (int i = 0; i < localPoints.Count; i++)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, route.transform);
            go.name = $"Checkpoint_{i:00}";
            Undo.RegisterCreatedObjectUndo(go, "Create Checkpoint");

            Vector3 p = localPoints[i];
            Vector3 pNext = localPoints[(i + 1) % localPoints.Count];

            Vector3 dir = pNext - p;
            if (dir.sqrMagnitude < 1e-8f)
                dir = Vector3.forward;

            go.transform.localPosition = p;

            go.transform.localRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

            var cp = go.GetComponent<Checkpoint>();
            if (cp == null) cp = go.AddComponent<Checkpoint>();
        }

        EditorUtility.SetDirty(route);
    }




    private static void Clear(Route route)
    {
        // delete all children (checkpoints)
        for (int i = route.transform.childCount - 1; i >= 0; i--)
        {
            var child = route.transform.GetChild(i).gameObject;
            Undo.DestroyObjectImmediate(child);
        }

        EditorUtility.SetDirty(route);
    }
}
