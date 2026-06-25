using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace ObjectSnapperTool
{
[InitializeOnLoad]
public class ObjectSnapper
{
    // Button dimensions (pixels)
    static int buttonWidth = 100;
    static int buttonHeight = 50;

    // Button distances from center of radial menu (pixels)
    static float forwardBtnDistance = 50;
    static float backwardBtnDistance = 50;
    static float upBtnDistance = 60;
    static float downBtnDistance = 60;
    static float rightBtnDistance = 120;
    static float leftBtnDistance = 120;

    // Scale multiplier when hovering over a button
    static float hoverScale = 1.2f;

    static Vector2 startMousePosition;
    static bool haveInput = false;
    static bool noSkin = true;

    static GUIStyle style;

    static bool snapping = false;
    static int currentIndex = 0;
    static List<Transform> currentSelection = new List<Transform>();

    static int undoGroupID;
    static Directions currentDirection;

    static float lastTime;

    // Settings
    public static float maxRaycastDistance = 1000f;
    public static float snapDelay = 0.05f;
    public static float offsetDistance = 0f;
    public static bool useLocalSpace = false;
    public static bool showWarnings = true;
    public static bool showPreview = true;
    public static LayerMask snapLayerMask = ~0; // All layers by default
    public static AlignmentMode alignmentMode = AlignmentMode.Surface;
    public static bool enableKeyboardShortcuts = true;

    // Preview data
    static Dictionary<Transform, Vector3> previewPositions = new Dictionary<Transform, Vector3>();
    static bool hasPreview = false;
    static Directions previewDirection;

    static ObjectSnapper()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    static void OnSceneGUI(SceneView sceneView)
    {
        if (noSkin)
            InitSkin();

        if(!snapping)
            CheckForInput();

        if (snapping == false && haveInput)
        {
            Handles.BeginGUI();
            DrawGUI();
            Handles.EndGUI();

            // Draw preview gizmos
            if (showPreview && hasPreview)
            {
                DrawPreviewGizmos();
            }
        }
        else if(snapping)
        {
            //if(currentSelection[currentIndex].position != currentPosition[currentIndex])
            if(Time.realtimeSinceStartup >= lastTime + snapDelay)
            {
                currentIndex++;
                if(currentIndex >= currentSelection.Count)
                {
                    snapping = false;
                    //Debug.Log("Snapped " + currentIndex + " objects");
                    currentSelection.Clear();
                    ClearPreview();

                    Undo.CollapseUndoOperations(undoGroupID);
                }
                else
                {
                    SnapTransform(currentSelection[currentIndex], currentDirection);
                }
            }

            sceneView.Repaint();
        }

        if (haveInput)
            sceneView.Repaint();
    }

    static void DrawGUI()
    {
        Rect forwardRect = new Rect(startMousePosition.x - buttonWidth * .5f, startMousePosition.y - forwardBtnDistance - buttonHeight * .5f, buttonWidth, buttonHeight);
        bool forwardHover = IsMouseOnRect(forwardRect);
        if(forwardHover && !hasPreview) { UpdatePreview(Directions.FORWARD); }
        if(GUI.Button(GetRectScale(forwardRect), "<color=#8D9AD9>Forward</color>", style))
        {
            SnapToObject(Directions.FORWARD);
            haveInput = false;
        }

        Rect backwardRect = new Rect(startMousePosition.x - buttonWidth * .5f, startMousePosition.y + backwardBtnDistance - buttonHeight * .5f, buttonWidth, buttonHeight);
        bool backwardHover = IsMouseOnRect(backwardRect);
        if(backwardHover && !hasPreview) { UpdatePreview(Directions.BACKWARD); }
        if(GUI.Button(GetRectScale(backwardRect), "<color=#8D9AD9>Backward</color>", style))
        {
            SnapToObject(Directions.BACKWARD);
            haveInput = false;
        }

        Rect upRect = new Rect(startMousePosition.x - buttonWidth * .5f, startMousePosition.y - forwardBtnDistance - upBtnDistance - buttonHeight * .5f, buttonWidth, buttonHeight);
        bool upHover = IsMouseOnRect(upRect);
        if(upHover && !hasPreview) { UpdatePreview(Directions.UP); }
        if(GUI.Button(GetRectScale(upRect), "<color=#FFFF00>Top</color>", style))
        {
            SnapToObject(Directions.UP);
            haveInput = false;
        }

        Rect downRect = new Rect(startMousePosition.x - buttonWidth * .5f, startMousePosition.y + backwardBtnDistance + downBtnDistance - buttonHeight * .5f, buttonWidth, buttonHeight);
        bool downHover = IsMouseOnRect(downRect);
        if(downHover && !hasPreview) { UpdatePreview(Directions.DOWN); }
        if(GUI.Button(GetRectScale(downRect), "<color=#FFFF00>Down</color>", style))
        {
            SnapToObject(Directions.DOWN);
            haveInput = false;
        }

        Rect rightRect = new Rect(startMousePosition.x + rightBtnDistance - buttonWidth * .5f, startMousePosition.y - buttonHeight * .5f, buttonWidth, buttonHeight);
        bool rightHover = IsMouseOnRect(rightRect);
        if(rightHover && !hasPreview) { UpdatePreview(Directions.RIGHT); }
        if(GUI.Button(GetRectScale(rightRect), "<color=#FF0000>Right</color>", style))
        {
            SnapToObject(Directions.RIGHT);
            haveInput = false;
        }

        Rect leftRect = new Rect(startMousePosition.x - leftBtnDistance - buttonWidth * .5f, startMousePosition.y - buttonHeight * .5f, buttonWidth, buttonHeight);
        bool leftHover = IsMouseOnRect(leftRect);
        if(leftHover && !hasPreview) { UpdatePreview(Directions.LEFT); }
        if(GUI.Button(GetRectScale(leftRect), "<color=#FF0000>Left</color>", style))
        {
            SnapToObject(Directions.LEFT);
            haveInput = false;
        }

        // Clear preview if not hovering any button
        if(!forwardHover && !backwardHover && !upHover && !downHover && !rightHover && !leftHover)
        {
            ClearPreview();
        }
    }

    static void SnapToObject(Directions direction)
    {
        if (Selection.gameObjects.Length == 0)
            return;

        Object[] all = Selection.GetFiltered(typeof(Transform), SelectionMode.TopLevel);
        Transform[] selected = new Transform[all.Length];


        for(int i = 0; i < all.Length; i++)
        {
            selected[i] = all[i] as Transform;
        }

        currentIndex = 0;
        currentDirection = direction;
        Transform[] orderedSelection = OrderSelection(selected);


        currentSelection.Clear();
        foreach(Transform t in orderedSelection)
        {
            currentSelection.Add(t);
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Snap into other objects");
        undoGroupID = Undo.GetCurrentGroup();

        SnapTransform(currentSelection[0], currentDirection);

        snapping = true;
    }

    static Transform[] OrderSelection(Transform[] selected)
    {
        System.Array.Sort(selected, YPosComp);

        return selected;
    }

    static void SnapTransform(Transform transform, Directions direction)
    {
        if (AssetDatabase.Contains(transform.gameObject))
            return;

        Vector3 rayDirection = useLocalSpace ? transform.TransformDirection(EnumToVector3(direction)) : EnumToVector3(direction);

        if (TryGetSnapHit(transform, rayDirection, out RaycastHit hit))
        {
            Undo.RecordObject(transform, "Snap Object");

            Vector3 targetPosition = CalculateSnapPosition(transform, hit, rayDirection, direction);
            transform.position = targetPosition;
        }
        else if (showWarnings)
        {
            Debug.LogWarning($"ObjectSnapper: No object found in {direction} direction within {maxRaycastDistance} units for {transform.name}");
        }

        lastTime = Time.realtimeSinceStartup;
    }

    // Raycasts from the object's pivot, ignoring any colliders belonging to the object
    // itself (or its children) so it never snaps to its own geometry.
    static bool TryGetSnapHit(Transform transform, Vector3 rayDirection, out RaycastHit closestHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(transform.position, rayDirection, maxRaycastDistance, snapLayerMask);

        closestHit = default;
        float closestDistance = float.MaxValue;
        bool found = false;

        foreach (RaycastHit hit in hits)
        {
            // Skip colliders that are part of the object being snapped.
            if (hit.collider.transform.IsChildOf(transform) || transform.IsChildOf(hit.collider.transform))
                continue;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestHit = hit;
                found = true;
            }
        }

        return found;
    }

    static Vector3 CalculateSnapPosition(Transform transform, RaycastHit hit, Vector3 rayDirection, Directions direction)
    {
        Vector3 dir = rayDirection.normalized;
        Vector3 targetPosition = transform.position;

        switch (alignmentMode)
        {
            case AlignmentMode.Surface:
                // Distance from the object's pivot to its surface, measured along the ray
                // direction. Projecting the world-space bounds onto the ray handles both
                // axis-aligned and rotated objects correctly.
                float surfaceExtent = GetExtentAlongDirection(transform, dir);

                // Place the object so its leading surface rests on the hit point, then push
                // back along the ray by the requested offset (positive = gap, negative = overlap).
                targetPosition = hit.point - (dir * surfaceExtent) - (dir * offsetDistance);
                break;

            case AlignmentMode.Center:
                Bounds hitBounds;
                if (TryGetWorldBounds(hit.collider.transform, out hitBounds))
                    targetPosition = hitBounds.center + (dir * offsetDistance);
                else
                    targetPosition = hit.point + (dir * offsetDistance);
                break;

            case AlignmentMode.Pivot:
                // hit.collider may belong to a child; align to the root object's pivot.
                targetPosition = hit.collider.transform.root.position + (dir * offsetDistance);
                break;
        }

        return targetPosition;
    }

    // Returns how far the object's surface extends from its pivot along the given world
    // direction. Falls back from Renderer bounds to Collider bounds, then to zero.
    static float GetExtentAlongDirection(Transform transform, Vector3 worldDir)
    {
        if (!TryGetWorldBounds(transform, out Bounds bounds))
            return 0f;

        Vector3 dir = worldDir.normalized;
        Vector3 extents = bounds.extents;

        // Projection of an axis-aligned box's half-extents onto an arbitrary direction.
        float projected = Mathf.Abs(dir.x) * extents.x
                        + Mathf.Abs(dir.y) * extents.y
                        + Mathf.Abs(dir.z) * extents.z;

        // Account for the pivot not being at the bounds center (offset pivots).
        float pivotOffset = Vector3.Dot(bounds.center - transform.position, dir);

        return projected + pivotOffset;
    }

    // Resolves world-space bounds for an object, preferring Renderer, then Collider.
    static bool TryGetWorldBounds(Transform transform, out Bounds bounds)
    {
        Renderer renderer = transform.GetComponent<Renderer>();
        if (renderer != null)
        {
            bounds = renderer.bounds;
            return true;
        }

        Collider collider = transform.GetComponent<Collider>();
        if (collider != null)
        {
            bounds = collider.bounds;
            return true;
        }

        bounds = default;
        return false;
    }

    static int YPosComp(Transform t1, Transform t2)
    {
        if (t1 == null) return (t2 == null) ? 0 : -1;
        if (t2 == null) return 1;

        //var y1 = t1.position.y;
        //var y2 = t2.position.y;
        var y1 = GetPositionFromCorrectDirection(t1.position);
        var y2 = GetPositionFromCorrectDirection(t2.position);

        return y1.CompareTo(y2);
    }

    static float GetPositionFromCorrectDirection(Vector3 pos)
    {
        float result = 0;
        switch(currentDirection)
        {
            case Directions.UP:
                result = pos.y * -1;
                break;
            case Directions.DOWN:
                result = pos.y;
                break;
            case Directions.RIGHT:
                result = pos.x * -1;
                break;
            case Directions.LEFT:
                result = pos.x;
                break;
            case Directions.FORWARD:
                result = pos.z * -1;
                break;
            case Directions.BACKWARD:
                result = pos.z;
                break;
        }

        return result;
    }

    static void CheckForInput()
    {
        if(Event.current.isKey && Event.current.shift && Event.current.type == EventType.KeyDown)
        {
            if(Event.current.keyCode == KeyCode.G)
            {
                haveInput = !haveInput;
                if(haveInput)
                {
                    startMousePosition = Event.current.mousePosition;
                }
                else
                {
                    ClearPreview();
                }
                Event.current.Use();
            }

            // Direct keyboard shortcuts
            if(enableKeyboardShortcuts && haveInput)
            {
                if(Event.current.keyCode == KeyCode.W || Event.current.keyCode == KeyCode.UpArrow)
                {
                    SnapToObject(Directions.FORWARD);
                    haveInput = false;
                    Event.current.Use();
                }
                else if(Event.current.keyCode == KeyCode.S || Event.current.keyCode == KeyCode.DownArrow)
                {
                    SnapToObject(Directions.BACKWARD);
                    haveInput = false;
                    Event.current.Use();
                }
                else if(Event.current.keyCode == KeyCode.D || Event.current.keyCode == KeyCode.RightArrow)
                {
                    SnapToObject(Directions.RIGHT);
                    haveInput = false;
                    Event.current.Use();
                }
                else if(Event.current.keyCode == KeyCode.A || Event.current.keyCode == KeyCode.LeftArrow)
                {
                    SnapToObject(Directions.LEFT);
                    haveInput = false;
                    Event.current.Use();
                }
                else if(Event.current.keyCode == KeyCode.E)
                {
                    SnapToObject(Directions.UP);
                    haveInput = false;
                    Event.current.Use();
                }
                else if(Event.current.keyCode == KeyCode.Q)
                {
                    SnapToObject(Directions.DOWN);
                    haveInput = false;
                    Event.current.Use();
                }
            }
        }

        if(Event.current.isMouse && Event.current.type == EventType.MouseDown && Event.current.button == 1)
        {
            haveInput = false;
            ClearPreview();
        }
    }

    static void InitSkin()
    {
        style = new GUIStyle(GUI.skin.button);
        style.richText = true;
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;

        //GUI.backgroundColor = new Color32(255,248,230,150);
        GUI.backgroundColor = new Color(.3f, .3f, .3f, .7f);

        noSkin = false;
    }

    static Rect GetRectScale(Rect r)
    {
        if(IsMouseOnRect(r))
            r = new Rect(r.x - (r.width * hoverScale - r.width) * .5f, r.y - (r.height * hoverScale - r.height) * .5f, r.width * hoverScale, r.height * hoverScale);

        return r;
    }

    static bool IsMouseOnRect(Rect r)
    {
        Vector2 mousePos = Event.current.mousePosition;

        if(mousePos.x > r.x && mousePos.x < r.x + r.width)
        {
            if(mousePos.y > r.y && mousePos.y < r.y + r.height)
            {
                return true;
            }
        }

        return false;
    }

    static Vector3 EnumToVector3(Directions direction)
    {
        Vector3 result = Vector3.zero;
        switch (direction)
        {
            case Directions.UP:
                result = Vector3.up;
                break;
            case Directions.DOWN:
                result = Vector3.down;
                break;
            case Directions.RIGHT:
                result = Vector3.right;
                break;
            case Directions.LEFT:
                result = Vector3.left;
                break;
            case Directions.FORWARD:
                result = Vector3.forward;
                break;
            case Directions.BACKWARD:
                result = Vector3.back;
                break;
        }
        return result;
    }

    static void UpdatePreview(Directions direction)
    {
        if (Selection.gameObjects.Length == 0 || !showPreview)
        {
            ClearPreview();
            return;
        }

        previewPositions.Clear();
        previewDirection = direction;

        Object[] all = Selection.GetFiltered(typeof(Transform), SelectionMode.TopLevel);

        foreach (Transform t in all)
        {
            if (AssetDatabase.Contains(t.gameObject))
                continue;

            Vector3 rayDirection = useLocalSpace ? t.TransformDirection(EnumToVector3(direction)) : EnumToVector3(direction);

            if (TryGetSnapHit(t, rayDirection, out RaycastHit hit))
            {
                Vector3 previewPos = CalculateSnapPosition(t, hit, rayDirection, direction);
                previewPositions[t] = previewPos;
            }
        }

        hasPreview = previewPositions.Count > 0;
    }

    static void ClearPreview()
    {
        previewPositions.Clear();
        hasPreview = false;
    }

    static void DrawPreviewGizmos()
    {
        foreach (var kvp in previewPositions)
        {
            Transform t = kvp.Key;
            Vector3 previewPos = kvp.Value;

            if (t == null)
                continue;

            if (TryGetWorldBounds(t, out Bounds bounds))
            {
                Vector3 size = bounds.size;
                Vector3 offset = previewPos - t.position;
                Vector3 previewCenter = bounds.center + offset;

                // Draw semi-transparent preview box (wireframe)
                Handles.color = new Color(0.2f, 1f, 0.2f, 0.6f);
                Handles.DrawWireCube(previewCenter, size);

                // Draw a second slightly larger wireframe for better visibility
                Handles.color = new Color(0.2f, 1f, 0.2f, 0.3f);
                Handles.DrawWireCube(previewCenter, size * 1.02f);

                // Draw direction arrow from current to preview position
                Handles.color = new Color(0.2f, 1f, 0.2f, 0.8f);
                Handles.DrawDottedLine(t.position, previewPos, 4f);

                // Draw arrow at preview position
                Vector3 arrowDirection = (previewPos - t.position).normalized;
                if (arrowDirection != Vector3.zero)
                {
                    Handles.color = new Color(0.2f, 1f, 0.2f, 1f);
                    Handles.ArrowHandleCap(0, previewPos - arrowDirection * 0.5f, Quaternion.LookRotation(arrowDirection), 0.5f, EventType.Repaint);
                }

                // Draw position label
                GUIStyle labelStyle = new GUIStyle(EditorStyles.helpBox);
                labelStyle.fontSize = 10;
                labelStyle.alignment = TextAnchor.MiddleCenter;
                labelStyle.normal.textColor = Color.white;

                Handles.Label(previewPos + Vector3.up * size.y * 0.5f,
                    $"Preview: {t.name}\n{previewDirection}",
                    labelStyle);
            }
        }
    }

    enum Directions
    {
        UP,
        DOWN,
        RIGHT,
        LEFT,
        FORWARD,
        BACKWARD
    }

    public enum AlignmentMode
    {
        Surface,
        Center,
        Pivot
    }
}
}
