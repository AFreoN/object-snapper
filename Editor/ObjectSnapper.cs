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
        }
        else if(snapping)
        {
            //if(currentSelection[currentIndex].position != currentPosition[currentIndex])
            if(Time.realtimeSinceStartup >= lastTime + .05f)
            {
                currentIndex++;
                if(currentIndex >= currentSelection.Count)
                {
                    snapping = false;
                    //Debug.Log("Snapped " + currentIndex + " objects");
                    currentSelection.Clear();

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
        if(GUI.Button(getRectScale(forwardRect), "<color=#8D9AD9>Forward</color>", style))
        {
            SnapToObject(Directions.FORWARD);
            haveInput = false;
        }

        Rect backwardRect = new Rect(startMousePosition.x - buttonWidth * .5f, startMousePosition.y + BackwardBtnDistance - buttonHeight * .5f, buttonWidth, buttonHeight);
        if(GUI.Button(getRectScale(backwardRect), "<color=#8D9AD9>Backward</color>", style))
        {
            SnapToObject(Directions.BACKWARD);
            haveInput = false;
        }

        Rect upRect = new Rect(startMousePosition.x - buttonWidth * .5f, startMousePosition.y - ForwardBtnDistance - UpBtnDistance - buttonHeight * .5f, buttonWidth, buttonHeight);
        if(GUI.Button(getRectScale(upRect), "<color=#FFFF00>Top</color>", style))
        {
            SnapToObject(Directions.UP);
            haveInput = false;
        }

        Rect downRect = new Rect(startMousePosition.x - buttonWidth * .5f, startMousePosition.y + BackwardBtnDistance + DownBtnDistance - buttonHeight * .5f, buttonWidth, buttonHeight);
        if(GUI.Button(getRectScale(downRect), "<color=#FFFF00>Down</color>", style))
        {
            SnapToObject(Directions.DOWN);
            haveInput = false;
        }

        Rect rightRect = new Rect(startMousePosition.x + RightBtnDistance - buttonWidth * .5f, startMousePosition.y - buttonHeight * .5f, buttonWidth, buttonHeight);
        if(GUI.Button(getRectScale(rightRect), "<color=#FF0000>Right</color>", style))
        {
            SnapToObject(Directions.RIGHT);
            haveInput = false;
        }

        Rect leftRect = new Rect(startMousePosition.x - LeftBtnDistance - buttonWidth * .5f, startMousePosition.y - buttonHeight * .5f, buttonWidth, buttonHeight);
        if(GUI.Button(getRectScale(leftRect), "<color=#FF0000>Left</color>", style))
        {
            SnapToObject(Directions.LEFT);
            haveInput = false;
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

        Vector3 rayDirection = enumToVector3(direction);

        if (Physics.Raycast(transform.position, rayDirection, out RaycastHit hit))
        {
            float boundY = 0;

            float final = 0;

            if (transform.GetComponent<MeshRenderer>() != null)
            {
                boundY = transform.GetComponent<MeshRenderer>().bounds.size.y;

                final = boundY * .5f;
            }

            Undo.RecordObject(transform, "Clamped Transform");
            float yPos = (hit.point + final * Vector3.up).y;

            Vector3 directionalized = MultiplyVector3Segments(-1 * rayDirection, transform.position);
            directionalized.x = rayDirection.x == 0 ? transform.position.x : hit.point.x + GetExtremeDistance(transform, direction);
            directionalized.y = rayDirection.y == 0 ? transform.position.y : hit.point.y + GetExtremeDistance(transform, direction);
            directionalized.z = rayDirection.z == 0 ? transform.position.z : hit.point.z + GetExtremeDistance(transform, direction);

            transform.position = directionalized;
        }

        lastTime = Time.realtimeSinceStartup;
    }

    static float GetExtremeDistance(Transform transform, Directions direction)
    {
        float iterator = float.MaxValue;
        float result = 0;

        MeshFilter meshFilter = transform.GetComponent<MeshFilter>();
        bool haveMF = transform.GetComponent<MeshFilter>() != null ? true : false;
        bool haveSMR = transform.GetComponent<SkinnedMeshRenderer>() != null ? true : false;

        if (haveMF == false && haveSMR == false)
            return 0;

        Mesh m = haveMF ? transform.GetComponent<MeshFilter>().sharedMesh : transform.GetComponent<SkinnedMeshRenderer>().sharedMesh;

        switch(direction)
        {
            case Directions.UP:
                iterator = float.MinValue;
                foreach(Vector3 v in m.vertices)
                {
                    if (transform.TransformPoint(v).y > iterator)
                        iterator = transform.TransformPoint(v).y;
                }
                result = transform.position.y - iterator;
                break;
            case Directions.DOWN:
                iterator = float.MaxValue;
                foreach(Vector3 v in m.vertices)
                {
                    if(transform.TransformPoint(v).y < iterator)
                    {
                        iterator = transform.TransformPoint(v).y;
                    }
                }
                result = transform.position.y - iterator;
                break;
            case Directions.RIGHT:
                iterator = float.MinValue;
                foreach(Vector3 v in m.vertices)
                {
                    if (transform.TransformPoint(v).x > iterator)
                        iterator = transform.TransformPoint(v).x;
                }
                result = transform.position.x - iterator;
                break;
            case Directions.LEFT:
                iterator = float.MaxValue;
                foreach(Vector3 v in m.vertices)
                {
                    if (transform.TransformPoint(v).x < iterator)
                        iterator = transform.TransformPoint(v).x;
                }
                result = transform.position.x - iterator;
                break;
            case Directions.FORWARD:
                iterator = float.MinValue;
                foreach(Vector3 v in m.vertices)
                {
                    if (transform.TransformPoint(v).z > iterator)
                        iterator = transform.TransformPoint(v).z;
                }
                result = transform.position.z - iterator;
                break;
            case Directions.BACKWARD:
                iterator = float.MaxValue;
                foreach(Vector3 v in m.vertices)
                {
                    if (transform.TransformPoint(v).z < iterator)
                        iterator = transform.TransformPoint(v).z;
                }
                result = transform.position.z - iterator;
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
            }
        }

        if(Event.current.isMouse && Event.current.type == EventType.MouseDown && Event.current.button == 1)
        {
            haveInput = false;
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

    enum Directions
    {
        UP,
        DOWN,
        RIGHT,
        LEFT,
        FORWARD,
        BACKWARD
    }
}
