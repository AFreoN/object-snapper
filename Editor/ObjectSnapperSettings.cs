using UnityEngine;
using UnityEditor;

public class ObjectSnapperSettings : EditorWindow
{
    [MenuItem("Tools/Object Snapper Settings")]
    public static void ShowWindow()
    {
        ObjectSnapperSettings window = GetWindow<ObjectSnapperSettings>("Snapper Settings");
        window.minSize = new Vector2(350, 600);
        window.Show();
    }

    private Vector2 scrollPosition;
    private bool showAdvanced = false;
    private bool showKeyboardHelp = false;

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

        ObjectSnapper.alignmentMode = (ObjectSnapper.AlignmentMode)EditorGUILayout.EnumPopup(
            new GUIContent("Alignment Mode", "Surface: Align to hit surface\nCenter: Align to object center\nPivot: Align to object pivot"),
            ObjectSnapper.alignmentMode
        );

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

        // Visual Settings
        EditorGUILayout.LabelField("Visual Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        ObjectSnapper.showPreview = EditorGUILayout.Toggle(
            new GUIContent("Show Preview", "Display preview gizmos when hovering over direction buttons."),
            ObjectSnapper.showPreview
        );

        ObjectSnapper.showWarnings = EditorGUILayout.Toggle(
            new GUIContent("Show Warnings", "Display console warnings when snapping fails."),
            ObjectSnapper.showWarnings
        );

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Input Settings
        EditorGUILayout.LabelField("Input Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        ObjectSnapper.enableKeyboardShortcuts = EditorGUILayout.Toggle(
            new GUIContent("Enable Keyboard Shortcuts", "Use Shift+WASD/QE keys for quick snapping while the menu is active."),
            ObjectSnapper.enableKeyboardShortcuts
        );

        if (ObjectSnapper.enableKeyboardShortcuts)
        {
            showKeyboardHelp = EditorGUILayout.Foldout(showKeyboardHelp, "Keyboard Shortcuts");
            if (showKeyboardHelp)
            {
                EditorGUILayout.BeginVertical("helpbox");
                EditorGUILayout.LabelField("Shift+W / Shift+↑ : Snap Forward", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("Shift+S / Shift+↓ : Snap Backward", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("Shift+D / Shift+→ : Snap Right", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("Shift+A / Shift+← : Snap Left", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("Shift+E : Snap Up", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("Shift+Q : Snap Down", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Advanced Settings
        showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced Settings", true);
        if (showAdvanced)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("Layer Filtering", EditorStyles.boldLabel);
            ObjectSnapper.snapLayerMask = EditorGUILayout.MaskField(
                new GUIContent("Snap Layers", "Only snap to objects on these layers."),
                ObjectSnapper.snapLayerMask,
                UnityEditorInternal.InternalEditorUtility.layers
            );

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Layer mask allows you to control which objects can be snapped to. Useful for ignoring UI, effects, or other non-geometry layers.", MessageType.Info);

            EditorGUILayout.EndVertical();
        }

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
            ObjectSnapper.alignmentMode = ObjectSnapper.AlignmentMode.Surface;
            ObjectSnapper.showPreview = true;
            ObjectSnapper.enableKeyboardShortcuts = true;
        }

        if (GUILayout.Button("Precise Control"))
        {
            ObjectSnapper.snapDelay = 0.15f;
            ObjectSnapper.maxRaycastDistance = 1000f;
            ObjectSnapper.offsetDistance = 0f;
            ObjectSnapper.alignmentMode = ObjectSnapper.AlignmentMode.Surface;
            ObjectSnapper.showPreview = true;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("With Spacing (0.1)"))
        {
            ObjectSnapper.snapDelay = 0.05f;
            ObjectSnapper.offsetDistance = 0.1f;
            ObjectSnapper.alignmentMode = ObjectSnapper.AlignmentMode.Surface;
        }

        if (GUILayout.Button("Center Align"))
        {
            ObjectSnapper.alignmentMode = ObjectSnapper.AlignmentMode.Center;
            ObjectSnapper.offsetDistance = 0f;
            ObjectSnapper.showPreview = true;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Pivot Align"))
        {
            ObjectSnapper.alignmentMode = ObjectSnapper.AlignmentMode.Pivot;
            ObjectSnapper.offsetDistance = 0f;
            ObjectSnapper.showPreview = true;
        }

        if (GUILayout.Button("Reset to Default"))
        {
            ObjectSnapper.snapDelay = 0.05f;
            ObjectSnapper.maxRaycastDistance = 1000f;
            ObjectSnapper.offsetDistance = 0f;
            ObjectSnapper.useLocalSpace = false;
            ObjectSnapper.showWarnings = true;
            ObjectSnapper.showPreview = true;
            ObjectSnapper.snapLayerMask = ~0;
            ObjectSnapper.alignmentMode = ObjectSnapper.AlignmentMode.Surface;
            ObjectSnapper.enableKeyboardShortcuts = true;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Instructions
        EditorGUILayout.LabelField("How to Use", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("1. Select object(s) in Scene View", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("2. Press Shift+G to activate snapping mode", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("3a. Click a direction button to snap", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("3b. OR use Shift+WASD/QE keys for quick snap", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("4. Hover over buttons to preview snap position", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("5. Right-click or Shift+G again to cancel", EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Feature highlights
        EditorGUILayout.LabelField("Features", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("✓ Real-time preview with gizmos", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("✓ Multiple alignment modes (Surface/Center/Pivot)", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("✓ Layer mask filtering", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("✓ Keyboard shortcuts for rapid level design", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("✓ Configurable offset for modular spacing", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("✓ Local/World space support", EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        EditorGUILayout.EndScrollView();
    }
}
