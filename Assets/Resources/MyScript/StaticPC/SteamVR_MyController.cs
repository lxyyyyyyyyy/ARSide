using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Valve.VR;
using Valve.VR.Extras;
using Valve.VR.InteractionSystem;

public class SteamVR_MyController : MonoBehaviour
{
    public SteamVR_Action_Boolean segmentMode; // 按下按键进入线段选取模式
    public SteamVR_Action_Boolean selectPoint; // 在segmentMode下按下按键选点
    public SteamVR_Action_Boolean confirmSegment; // 确认生成线段
    public SteamVR_Action_Boolean deleteLastSegment; // 删除最后一个线段

    private SteamVR_LaserPointer laserPointer_right;
    private Mirror_MyController mirrorMyController;
    public List<Vector3> selectedPoints = new List<Vector3>(); // 用户选择点时，选择的点存放于此

    public bool isSegmentMode = false;

    public GameObject pointSpherePrefab;
    public GameObject pointSphere;

     

    // Start is called before the first frame update
    void Start()
    {
        if (GlobleInfo.ClientMode.Equals(CameraMode.AR)) { return; }
        laserPointer_right = GameObject.Find("[CameraRig]/Controller (right)").GetComponent<SteamVR_LaserPointer>();
        mirrorMyController = GetComponent<Mirror_MyController>();
        pointSphere = Instantiate(pointSpherePrefab);
        pointSphere.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (GlobleInfo.ClientMode.Equals(CameraMode.AR)) {  return; }
        if (!isSegmentMode && segmentMode.GetStateDown(SteamVR_Input_Sources.RightHand)){
            isSegmentMode = true;
            Debug.Log("启动智能线段选取模式:"+isSegmentMode);
            return;
        }
        if (isSegmentMode)
        {
            if (selectPoint.GetStateDown(SteamVR_Input_Sources.RightHand))
            {
                if (selectedPoints.Count < 2)
                {
                    Vector3 newPoint = laserPointer_right.endPoint + 0.02f*laserPointer_right.hitNormal;
                    if (selectedPoints.Count == 0)
                    {
                        pointSphere.SetActive(true);
                        pointSphere.transform.position = newPoint;
                    }
                    selectedPoints.Add(newPoint);
                    Debug.Log("端点添加成功");
                }
                else if (selectedPoints.Count == 2)
                {
                    Debug.Log("不可以添加更多的点，请按A键确认线段");
                }
                else if(selectedPoints.Count == 0)
                {
                    Debug.Log("已经没有点了，别删了");
                }
            }
            if (confirmSegment.GetStateDown(SteamVR_Input_Sources.RightHand))
            {
                if (selectedPoints.Count == 2)
                {
                    pointSphere.SetActive(false);
                    mirrorMyController.CmdUpdateSegmentInfo(new SegmentInfo(){
                        startPoint = selectedPoints[0],
                        endPoint = selectedPoints[1]
                    });
                    selectedPoints.Clear(); // 清空
                }
                else
                {
                    Debug.Log("线段端点数量不正确:"+selectedPoints.Count);
                }
            }
            if (segmentMode.GetStateDown(SteamVR_Input_Sources.RightHand))
            {
                
                isSegmentMode = false;
                selectedPoints.Clear();
                Debug.Log("退出线段选取模式:"+isSegmentMode);
            }
        }
        // 删除当前线段列表中的最后一个线段
        if (deleteLastSegment.GetStateDown(SteamVR_Input_Sources.RightHand))
        {
            Debug.Log("VR客户端发起删除线段请求");
            mirrorMyController.CmdDeleteSegmentInfo();
        }
        // 

    }


    

}
