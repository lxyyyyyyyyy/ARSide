using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Valve.VR;
using Valve.VR.Extras;
using Valve.VR.InteractionSystem;

public class SteamVR_MyController : MonoBehaviour
{
    public SteamVR_Action_Boolean segmentMode; // ���°��������߶�ѡȡģʽ
    public SteamVR_Action_Boolean selectPoint; // ��segmentMode�°��°���ѡ��
    public SteamVR_Action_Boolean confirmSegment; // ȷ�������߶�
    public SteamVR_Action_Boolean deleteLastSegment; // ɾ�����һ���߶�

    private SteamVR_LaserPointer laserPointer_right;
    private Mirror_MyController mirrorMyController;
    public List<Vector3> selectedPoints = new List<Vector3>(); // �û�ѡ���ʱ��ѡ��ĵ����ڴ�

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
            Debug.Log("���������߶�ѡȡģʽ:"+isSegmentMode);
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
                    Debug.Log("�˵���ӳɹ�");
                }
                else if (selectedPoints.Count == 2)
                {
                    Debug.Log("��������Ӹ���ĵ㣬�밴A��ȷ���߶�");
                }
                else if(selectedPoints.Count == 0)
                {
                    Debug.Log("�Ѿ�û�е��ˣ���ɾ��");
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
                    selectedPoints.Clear(); // ���
                }
                else
                {
                    Debug.Log("�߶ζ˵���������ȷ:"+selectedPoints.Count);
                }
            }
            if (segmentMode.GetStateDown(SteamVR_Input_Sources.RightHand))
            {
                
                isSegmentMode = false;
                selectedPoints.Clear();
                Debug.Log("�˳��߶�ѡȡģʽ:"+isSegmentMode);
            }
        }
        // ɾ����ǰ�߶��б��е����һ���߶�
        if (deleteLastSegment.GetStateDown(SteamVR_Input_Sources.RightHand))
        {
            Debug.Log("VR�ͻ��˷���ɾ���߶�����");
            mirrorMyController.CmdDeleteSegmentInfo();
        }
        // 

    }


    

}
