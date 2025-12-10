using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

[InitializeOnLoad]
public class ObjectSnapper
{
    static int buttonWidth = 100, buttonHeight = 50;

    static float ForwardBtnDistance = 50, BackwardBtnDistance = 50;
    static float UpBtnDistance = 60, DownBtnDistance = 60;
    static float RightBtnDistance = 120, LeftBtnDistance = 120;

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

        //style = new GUIStyle(GUI.skin.button);
    }

    static void OnSceneGUI(SceneView sceneView)
    {
        if (noSkin)
            InitSkin();

        if(!snapping)
            checkForInput();

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
        Rect forwardRect = new Rect(startMousePosition.x - buttonWidth * .5f, startMousePosition.y - ForwardBtnDistance - buttonHeight * .5f, buttonWidth, buttonHeight);
        bool forwardHover = IsMouseOnRect(forwardRect);
        if(forwardHover && !hasPreview) { UpdatePreview(Directions.FORWARD); }
        if(GUI.Button(getRectScale(forwardRect), "<color=#8D9AD9>Forward</color>", style))
        {
            SnapToObject(Directions.FORWARD);
            haveInput = false;
        }

        Rect backwardRect = new Rect(startMousePosition.x - buttonWidth * .5f, startMousePosition.y + BackwardBtnDistance - buttonHeight * .5f, buttonWidth, buttonHeight);
        bool backwardHover = IsMouseOnRect(backwardRect);
        if(backwardHover && !hasPreview) { UpdatePreview(Directions.BACKWARD); }
        if(GUI.Button(getRectScale(backwardRect), "<color=#8D9AD9>Backward</color>", style))
        {
            SnapToObject(Directions.BACKWARD);
            haveInput = false;
        }

        Rect upRect = new Rect(startMousePosition.x - buttonWidth * .5f, startMousePosition.y - ForwardBtnDistance - UpBtnDistance - buttonHeight * .5f, buttonWidth, buttonHeight);
        bool upHover = IsMouseOnRect(upRect);
        if(upHover && !hasPreview) { UpdatePreview(Directions.UP); }
        if(GUI.Button(getRectScale(upRect), "<color=#FFFF00>Top</color>", style))
        {
            SnapToObject(Directions.UP);
            haveInput = false;
        }

        Rect downRect = new Rect(startMousePosition.x - buttonWidth * .5f, startMousePosition.y + BackwardBtnDistance + DownBtnDistance - buttonHeight * .5f, buttonWidth, buttonHeight);
        bool downHover = IsMouseOnRect(downRect);
        if(downHover && !hasPreview) { UpdatePreview(Directions.DOWN); }
        if(GUI.Button(getRectScale(downRect), "<color=#FFFF00>Down</color>", style))
        {
            SnapToObject(Directions.DOWN);
            haveInput = false;
        }

        Rect rightRect = new Rect(startMousePosition.x + RightBtnDistance - buttonWidth * .5f, startMousePosition.y - buttonHeight * .5f, buttonWidth, buttonHeight);
        bool rightHover = IsMouseOnRect(rightRect);
        if(rightHover && !hasPreview) { UpdatePreview(Directions.RIGHT); }
        if(GUI.Button(getRectScale(rightRect), "<color=#FF0000>Right</color>", style))
        {
            SnapToObject(Directions.RIGHT);
            haveInput = false;
        }

        Rect leftRect = new Rect(startMousePosition.x - LeftBtnDistance - buttonWidth * .5f, startMousePosition.y - buttonHeight * .5f, buttonWidth, buttonHeight);
        bool leftHover = IsMouseOnRect(leftRect);
        if(leftHover && !hasPreview) { UpdatePreview(Directions.LEFT); }
        if(GUI.Button(getRectScale(leftRect), "<color=#FF0000>Left</color>", style))
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

        Vector3 rayDirection = useLocalSpace ? transform.TransformDirection(enumToVector3(direction)) : enumToVector3(direction);
        Vector3 rayOrigin = transform.position;

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, maxRaycastDistance, snapLayerMask))
        {
            // Check if hit object has a collider (redundant but good for clarity)
            if (hit.collider == null && showWarnings)
            {
                Debug.LogWarning($"ObjectSnapper: No collider found on target object for {transform.name}");
                lastTime = Time.realtimeSinceStartup;
                return;
            }

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

    static Vector3 CalculateSnapPosition(Transform transform, RaycastHit hit, Vector3 rayDirection, Directions direction)
    {
        Vector3 targetPosition = transform.position;

        switch (alignmentMode)
        {
            case AlignmentMode.Surface:
                Vector3 directionalized = MultiplyVector3Segments(-1 * rayDirection.normalized, transform.position);
                directionalized.x = rayDirection.x == 0 ? transform.position.x : hit.point.x + GetExtremeDistance(transform, direction) + (offsetDistance * Mathf.Sign(rayDirection.x));
                directionalized.y = rayDirection.y == 0 ? transform.position.y : hit.point.y + GetExtremeDistance(transform, direction) + (offsetDistance * Mathf.Sign(rayDirection.y));
                directionalized.z = rayDirection.z == 0 ? transform.position.z : hit.point.z + GetExtremeDistance(transform, direction) + (offsetDistance * Mathf.Sign(rayDirection.z));
                targetPosition = directionalized;
                break;

            case AlignmentMode.Center:
                Renderer hitRenderer = hit.collider.GetComponent<Renderer>();
                if (hitRenderer != null)
                {
                    Vector3 hitCenter = hitRenderer.bounds.center;
                    targetPosition = hitCenter + (rayDirection.normalized * offsetDistance);
                }
                else
                {
                    targetPosition = hit.point + (rayDirection.normalized * offsetDistance);
                }
                break;

            case AlignmentMode.Pivot:
                targetPosition = hit.transform.position + (rayDirection.normalized * offsetDistance);
                break;
        }

        return targetPosition;
    }

    static float GetExtremeDistance(Transform transform, Directions direction)
    {
        Renderer renderer = transform.GetComponent<Renderer>();

        if (renderer == null)
            return 0;

        Bounds bounds = renderer.bounds;
        float result = 0;

        switch(direction)
        {
            case Directions.UP:
                result = transform.position.y - bounds.max.y;
                break;
            case Directions.DOWN:
                result = transform.position.y - bounds.min.y;
                break;
            case Directions.RIGHT:
                result = transform.position.x - bounds.max.x;
                break;
            case Directions.LEFT:
                result = transform.position.x - bounds.min.x;
                break;
            case Directions.FORWARD:
                result = transform.position.z - bounds.max.z;
                break;
            case Directions.BACKWARD:
                result = transform.position.z - bounds.min.z;
                break;
        }

        return result;
    }

    static int YPosComp(Transform t1, Transform t2)
    {
        if (t1 == null) return (t2 == null) ? 0 : -1;
        if (t2 == null) return 1;

        //var y1 = t1.position.y;
        //var y2 = t2.position.y;
        var y1 = getPositionFromCorrectDirection(t1.position);
        var y2 = getPositionFromCorrectDirection(t2.position);

        return y1.CompareTo(y2);
    }

    static Vector3 MultiplyVector3Segments(Vector3 v1, Vector3 v2)
    {
        Vector3 result = new Vector3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
        return result;
    }

    static float getPositionFromCorrectDirection(Vector3 pos)
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

    static void checkForInput()
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

    static Rect getRectScale(Rect r)
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

    static Vector3 enumToVector3(Directions direction)
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

            Vector3 rayDirection = useLocalSpace ? t.TransformDirection(enumToVector3(direction)) : enumToVector3(direction);
            Vector3 rayOrigin = t.position;

            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, maxRaycastDistance, snapLayerMask))
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

            Renderer renderer = t.GetComponent<Renderer>();
            if (renderer != null)
            {
                Bounds bounds = renderer.bounds;
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
