using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarkRenderVR : MonoBehaviour
{
    private MirrorController MirrorController;

    private List<GameObject> segmentObjectList;
    private List<GameObject> rotationObjectList;
    private List<GameObject> pressObjectList;

    public Material segmentMaterial;
    public float segmentThickness = 0.01f;

    public GameObject RotateSymbol;
    public GameObject PressSymbol;

    // Start is called before the first frame update
    void Start()
    {
        MirrorController = GetComponentInParent<MirrorController>();
        segmentObjectList = new List<GameObject>();
        rotationObjectList = new List<GameObject>();
        pressObjectList = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        RenderSegment();
        RenderRotation();
        RenderPress();
    }

    /// <summary>
    /// render segmetn on VR client per frame
    /// </summary>
    private void RenderSegment()
    {
        // 1 arrow contained with 3 segment
        int n_curSegmentObj = segmentObjectList.Count; // number of segment object
        int n_curSegment = n_curSegmentObj / 3; // number of segment
        int n_clientSegment = MirrorController.clientSegmentList.Count; // number of latest segment list

        int delta = n_clientSegment - n_curSegment;
        // delete segment
        for(int i = 0; i > delta; --i)
        {
            for(int j = 0; i < 3; ++j)
            {
                GameObject tempObj = segmentObjectList[n_curSegmentObj - 1];
                segmentObjectList.Remove(tempObj);
                Destroy(tempObj);
            }
        }
        // add new segment
        for(int i = 0; i < delta; ++i)
        {
            SegmentInfo segment = MirrorController.clientSegmentList[n_curSegment + i];
            Debug.Log("point1:" + segment.startPoint.ToString() + ",point2:" + segment.endPoint.ToString());
            DrawSegment(segment);
            DrawArrow(segment);
        }
    }

    private void RenderRotation()
    {
        int n_curRotationObj = rotationObjectList.Count;
        int n_clientRotation = MirrorController.clientRotationList.Count;

        int delta = n_clientRotation - n_curRotationObj;
        // delete rotation 
        for (int i = 0; i > delta; --i)
        {
            GameObject tempObj = segmentObjectList[n_clientRotation - 1];
            rotationObjectList.Remove(tempObj);
            Destroy(tempObj);
        }
        // add new rotation
        for (int i = 0; i < delta; ++i)
        {
            SymbolInfo symbol_rotation = MirrorController.clientRotationList[n_curRotationObj + i];
            GameObject tempObj = Instantiate(RotateSymbol);
            tempObj.transform.parent = transform;
            tempObj.transform.position = symbol_rotation.position;
            tempObj.transform.forward = symbol_rotation.up;
            rotationObjectList.Add(tempObj);
        }
    }

    private void RenderPress()
    {
        int n_curPressObj = pressObjectList.Count;
        int n_clientPress = MirrorController.clientPressList.Count;

        int delta = n_clientPress - n_curPressObj;
        // delete press 
        for (int i = 0; i > delta; --i)
        {
            GameObject tempObj = pressObjectList[n_clientPress - 1];
            pressObjectList.Remove(tempObj);
            Destroy(tempObj);
        }
        // add new press
        for (int i = 0; i < delta; ++i)
        {
            SymbolInfo symbol_press = MirrorController.clientPressList[n_curPressObj + i];
            GameObject tempObj = Instantiate(PressSymbol);
            tempObj.transform.parent = transform;
            tempObj.transform.position = symbol_press.position;
            tempObj.transform.right = symbol_press.up;
            pressObjectList.Add(tempObj);
        }
    }

    /// <summary>
    /// draw an segment
    /// </summary>
    /// <param name="segmentInfo"></param>
    private void DrawSegment(SegmentInfo segmentInfo)
    {
        GameObject segmentObj = new GameObject();
        segmentObj.transform.SetParent(this.transform);
        segmentObj.layer = LayerMask.NameToLayer("DepthCameraUnivisible"); 
        LineRenderer segmentRender = segmentObj.AddComponent<LineRenderer>();
        segmentRender.material = segmentMaterial;
        segmentRender.startWidth = segmentThickness;
        segmentRender.endWidth = segmentThickness;
        segmentRender.numCapVertices = 2;
        segmentRender.positionCount = 2;
        segmentRender.SetPosition(0, segmentInfo.startPoint);
        segmentRender.SetPosition(1, segmentInfo.endPoint);
        

        segmentObjectList.Add(segmentObj);
    }

    /// <summary>
    /// draw two segment as an arrow
    /// </summary>
    /// <param name="segmentInfo"></param>
    private void DrawArrow(SegmentInfo segmentInfo)
    {
        Vector3 screenP1 = Camera.main.WorldToScreenPoint(segmentInfo.startPoint),
            screenP2 = Camera.main.WorldToScreenPoint(segmentInfo.endPoint);
        Vector2 dir = (screenP1 - screenP2).normalized;
        Vector2 verticalDir = new Vector2(-dir.y, dir.x);

        int length = 20;
        Vector3 screenArrowP1 = screenP2 + length * new Vector3(verticalDir.x, verticalDir.y) + length * new Vector3(dir.x, dir.y),
            screenArrowP2 = screenP2 - length * new Vector3(verticalDir.x, verticalDir.y) + length * new Vector3(dir.x, dir.y);

        Vector3 arrowP1 = Camera.main.ScreenToWorldPoint(screenArrowP1),
            arrowP2 = Camera.main.ScreenToWorldPoint(screenArrowP2);

        DrawSegment(new SegmentInfo()
        {
            startPoint = arrowP1,
            endPoint = segmentInfo.endPoint
        });

        DrawSegment(new SegmentInfo()
        {
            startPoint = arrowP2,
            endPoint = segmentInfo.endPoint
        });
    }

   
}
