using UnityEngine;
using UnityEditor;

public class ObjectSnapperSettings : EditorWindow
{
    [MenuItem("Tools/Object Snapper Settings")]
    public static void ShowWindow()
    {
        ObjectSnapperSettings window = GetWindow<ObjectSnapperSettings>("Snapper Settings");
        window.minSize = new Vector2(300, 400);
        window.Show();
    }

    private Vector2 scrollPosition;

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Object Snapper Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox("Configure snapping behavior and performance settings.\nUse Shift+G in Scene View to activate snapping.", MessageType.Info);

        EditorGUILayout.Space(10);

        // Performance Settings
        EditorGUILayout.LabelField("Performance", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        ObjectSnapper.snapDelay = EditorGUILayout.Slider(
            new GUIContent("Snap Delay", "Delay between snapping multiple objects (seconds). Set to 0 for instant snapping."),
            ObjectSnapper.snapDelay,
            0f,
            0.5f
        );

        ObjectSnapper.maxRaycastDistance = EditorGUILayout.FloatField(
            new GUIContent("Max Raycast Distance", "Maximum distance to search for objects to snap to."),
            ObjectSnapper.maxRaycastDistance
        );

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Snapping Behavior
        EditorGUILayout.LabelField("Snapping Behavior", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        ObjectSnapper.offsetDistance = EditorGUILayout.FloatField(
            new GUIContent("Offset Distance", "Additional spacing between snapped objects. Positive values add space, negative overlaps."),
            ObjectSnapper.offsetDistance
        );

        ObjectSnapper.useLocalSpace = EditorGUILayout.Toggle(
            new GUIContent("Use Local Space", "Snap in object's local space instead of world space. Useful for rotated objects."),
            ObjectSnapper.useLocalSpace
        );

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // UI Settings
        EditorGUILayout.LabelField("UI Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        ObjectSnapper.showWarnings = EditorGUILayout.Toggle(
            new GUIContent("Show Warnings", "Display console warnings when snapping fails."),
            ObjectSnapper.showWarnings
        );

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Quick Presets
        EditorGUILayout.LabelField("Quick Presets", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Fast Snapping"))
        {
            ObjectSnapper.snapDelay = 0f;
            ObjectSnapper.maxRaycastDistance = 500f;
            ObjectSnapper.offsetDistance = 0f;
        }

        if (GUILayout.Button("Precise Control"))
        {
            ObjectSnapper.snapDelay = 0.15f;
            ObjectSnapper.maxRaycastDistance = 1000f;
            ObjectSnapper.offsetDistance = 0f;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("With Spacing (0.1)"))
        {
            ObjectSnapper.snapDelay = 0.05f;
            ObjectSnapper.offsetDistance = 0.1f;
        }

        if (GUILayout.Button("Reset to Default"))
        {
            ObjectSnapper.snapDelay = 0.05f;
            ObjectSnapper.maxRaycastDistance = 1000f;
            ObjectSnapper.offsetDistance = 0f;
            ObjectSnapper.useLocalSpace = false;
            ObjectSnapper.showWarnings = true;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Instructions
        EditorGUILayout.LabelField("How to Use", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("1. Select object(s) in Scene View", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("2. Press Shift+G to activate", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("3. Click a direction button to snap", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("4. Right-click to cancel", EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        EditorGUILayout.EndScrollView();
    }
}
