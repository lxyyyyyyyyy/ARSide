using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Mirror_MyController : NetworkBehaviour
{
    /// <summary>
    /// ����ͬ�����ܱ�ʶ
    /// ���ֱ��ű����ø�Ϊ����
    /// AR��VR�ͻ��˶���ʹ��
    /// </summary>
    /// 
    public List<SegmentInfo> clientSegmentList;
    public List<GameObject> lineRendererObjList;
    public List<SymbolInfo> clientRotationList;
    public List<SymbolInfo> clientPressList;

    public Material segmentMaterial;
    public float segmentThickness = 0.1f;

    void Awake() {
        Debug.Log("Smart Sign Start");
        clientSegmentList = new List<SegmentInfo>();
        lineRendererObjList = new List<GameObject>();
        clientRotationList = new List<SymbolInfo>();
        clientPressList = new List<SymbolInfo>();
    }
    
    private void Start()
    {
        
    }



    [SyncVar(hook = nameof(AddNewSegmentToList))]
    public SegmentInfo segment;

    [SyncVar(hook =nameof(DeleteLastSegmentInList))]
    public int deleteSegment = 0;

    [SyncVar(hook =nameof(AddNewRotationToList))]
    public SymbolInfo rotationSymbol;

    [SyncVar(hook =nameof(AddNewPressToList))]
    public SymbolInfo pressSymbol;

    

    [Command]
    public void CmdUpdateSegmentInfo(SegmentInfo newSegment)
    {
        // Debug.Log("���������յ���������ִ��");
        segment = newSegment;
    }

    [Command]
    public void CmdUpdateRotationInfo(SymbolInfo newRotation)
    {
        // Debug.Log("���������յ���������ִ��");
        rotationSymbol = newRotation;
    }

    [Command]
    public void CmdUpdatePressInfo(SymbolInfo newPress)
    {
        // Debug.Log("���������յ���������ִ��");
        pressSymbol = newPress;
    }

    [Command]
    public void CmdDeleteSegmentInfo()
    {
        deleteSegment++; // Ŀ���Ǹı������ֵ���Ӷ�����hook����
    }

    public void AddNewSegmentToList(SegmentInfo oldSegment,SegmentInfo newSegment)
    {
        //if (lineRendererList.Count > 0) {
        //    lineRendererList[lineRendererList.Count - 1].enabled = false; 
        //}
        
        clientSegmentList.Add(newSegment);
        if (GlobleInfo.ClientMode.Equals(CameraMode.VR))
        {
            DrawSegment(newSegment);
            DrawArrow(newSegment);
        }
        if (GlobleInfo.ClientMode.Equals(CameraMode.AR))
        {
            // DrawSegment(newSegment);
        }
    }

    public void DeleteLastSegmentInList(int oldvar,int newvar)
    {
        clientSegmentList.RemoveAt(clientSegmentList.Count - 1);
        // straight line
        GameObject tempObj = lineRendererObjList[lineRendererObjList.Count - 1];
        lineRendererObjList.Remove(tempObj);
        Destroy(tempObj);
        // arrow
        tempObj = lineRendererObjList[lineRendererObjList.Count - 1];
        lineRendererObjList.Remove(tempObj);
        Destroy(tempObj);
        tempObj = lineRendererObjList[lineRendererObjList.Count - 1];
        lineRendererObjList.Remove(tempObj);
        Destroy(tempObj);
    }


    public void AddNewRotationToList(SymbolInfo oldRotation,SymbolInfo newRotation)
    {
        clientRotationList.Add(newRotation);
    }

    public void AddNewPressToList(SymbolInfo oldPress, SymbolInfo newPress)
    {
        clientPressList.Add(newPress);
    }

    private void DrawSegment(SegmentInfo segmentInfo)
    {
        GameObject segmentObj = new GameObject();
        segmentObj.transform.SetParent(this.transform);
        LineRenderer segmentRender = segmentObj.AddComponent<LineRenderer>();
        segmentRender.material = segmentMaterial;
        segmentRender.startWidth = segmentThickness;
        segmentRender.endWidth = segmentThickness;
        segmentRender.numCapVertices = 2;
        segmentRender.positionCount = 2;
        segmentRender.SetPosition(0,segmentInfo.startPoint);
        segmentRender.SetPosition(1, segmentInfo.endPoint);

        lineRendererObjList.Add(segmentObj);
    }

    private void DrawArrow(SegmentInfo segmentInfo)
    {
        Vector3 screenP1 = Camera.main.WorldToScreenPoint(segmentInfo.startPoint),
            screenP2 = Camera.main.WorldToScreenPoint(segmentInfo.endPoint);
        Vector2 dir = (screenP1 - screenP2).normalized;
        Vector2 verticalDir = new Vector2(-dir.y, dir.x);

        int length = 5;
        Vector3 screenArrowP1 = screenP2 + length * new Vector3(verticalDir.x, verticalDir.y) + length * new Vector3(dir.x, dir.y),
            screenArrowP2 = screenP2 - length * new Vector3(verticalDir.x, verticalDir.y) + length * new Vector3(dir.x, dir.y);

        Vector3 arrowP1 = Camera.main.ScreenToWorldPoint(screenArrowP1),
            arrowP2 = Camera.main.ScreenToWorldPoint(screenArrowP2);

        DrawSegment(new SegmentInfo() {
            startPoint = arrowP1,
            endPoint = segmentInfo.endPoint
        });

        DrawSegment(new SegmentInfo() {
            startPoint = arrowP2,
            endPoint = segmentInfo.endPoint
        });
    }

}
