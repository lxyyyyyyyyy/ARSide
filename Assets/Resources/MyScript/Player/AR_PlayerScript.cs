using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;


public class AR_PlayerScript : NetworkBehaviour
{
    public GameObject ARCamera;

    private Vector3 holoPosition;
    private Vector3 holoRotation;
    [Header("Initial Y Position")]
    [Tooltip("��ɫ��ʼ�߶�")]
    public float avatarY = -0.8f;

    public GameObject RightSHRPointer = null;
    public GameObject RightSHRPointerCursor = null;
    public GameObject LeftSHRPointer = null;
    public GameObject LeftSHRPointerCursor = null;

    public GameObject rightHandController;
    public GameObject leftHandController;
    private LineRenderer rightHandLineRenderer;
    private LineRenderer leftHandLineRenderer;

    // public readonly SyncList<RayInfo> ArHands = new SyncList<RayInfo>();

    [SyncVar]
    public RayInfo rightHand;

    [SyncVar]
    public RayInfo leftHand;

    [Header("Hit Circle")]
    [Tooltip("������ײ��Բ������Ԥ��������˴�")]
    public GameObject focusCircle_right;
    public GameObject focusCircle_left;
    public float circleDistanceRate = 0.01f;

    public override void OnStartClient()
    {
        base.OnStartClient();
        rightHandLineRenderer = rightHandController.GetComponent<LineRenderer>();
        leftHandLineRenderer = leftHandController.GetComponent<LineRenderer>();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        // ��LocalAvator����Ϊ����������ɼ� 3:CameraNotVisible
        UtilSetThisObjectLayer(this.transform, 3);

        // ��ȡAR���
        ARCamera = GameObject.Find("MixedRealityPlayspace/Main Camera");
        if (ARCamera == null)
        {
            Debug.LogError("AR Camera not find");
            ARCamera = null;
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (isLocalPlayer)
        {
            // ��ȡAR�����λ�ú���̬�Ը���Player
            holoPosition = ARCamera.GetComponent<Transform>().position;
            holoRotation = ARCamera.GetComponent<Transform>().eulerAngles;
            holoPosition.y = avatarY;
            holoRotation.x = 0;
            holoRotation.z = 0;
            this.GetComponent<Transform>().position = holoPosition;
            this.GetComponent<Transform>().eulerAngles = holoRotation;

            ARGetRayInfo();
            // Debug.Log("Client:"+rightHand.startPoint+","+rightHand.endPoint);

        }
        // ����Ҫ�Ǳ����û�ֱ�ӻ���
        if (!isLocalPlayer && isClient)
        {
            PaintRayBy2Position(rightHand.startPoint, rightHand.endPoint, rightHand.isActive, rightHandLineRenderer, HandMode.right);
            PaintRayBy2Position(leftHand.startPoint, leftHand.endPoint, leftHand.isActive, leftHandLineRenderer, HandMode.left);
        }


    }

    /// <summary>
    /// ��������Ĳ������õ�ǰ���弰���������Layer
    /// </summary>
    /// <param name="layer"></param>
    public void UtilSetThisObjectLayer(Transform transform, int layer)
    {
        if (transform.childCount > 0)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                UtilSetThisObjectLayer(transform.GetChild(i), layer);
            }
            transform.gameObject.layer = layer;
        }
        else
        {
            transform.gameObject.layer = layer;
        }
    }

    /// <summary>
    /// ��ȡAR�ֲ�������Դ˻�ȡ�ֲ����ߵ������յ㣬ֻ��localPlayerִ��
    /// </summary>
    private void ARGetRayInfo()
    {
        RayInfo rayInfoUpdateR = new RayInfo();
        RayInfo rayInfoUpdateL = new RayInfo();
        rayInfoUpdateR.handMode = HandMode.right;
        rayInfoUpdateL.handMode = HandMode.left;

        // RightSHRPointer ���ֲ��ĳ����㣬�������ֲ�ģ��
        // cursor ���ֲ����ߺͱ߽����ײ�㣬ֻ�з�����ײʱ�ų�����hierachy��
        if (RightSHRPointer == null)
        {
            RightSHRPointer = GameObject.Find("MixedRealityPlayspace/Right_ShellHandRayPointer(Clone)");
        }
        if (RightSHRPointer != null && RightSHRPointerCursor == null)
        {
            RightSHRPointerCursor = GameObject.Find("MixedRealityPlayspace/Right_ShellHandRayPointer(Clone)_Cursor");
        }
        if (RightSHRPointer != null && RightSHRPointerCursor != null)
        {
            if (RightSHRPointer.activeInHierarchy == true && RightSHRPointerCursor.activeInHierarchy == true)
            {
                rayInfoUpdateR.startPoint = RightSHRPointer.transform.position;
                rayInfoUpdateR.endPoint = RightSHRPointerCursor.transform.position;
                rayInfoUpdateR.isActive = true;
                // Debug.Log("����������Ϣ"+rayInfoUpadate.startPoint+","+rayInfoUpadate.endPoint);
            }
            else
            {
                rayInfoUpdateR.startPoint = new Vector3();
                rayInfoUpdateR.endPoint = new Vector3();
                rayInfoUpdateR.isActive = false;
            }
            CmdUpdateHandsRay(rayInfoUpdateR);
        }
        else if (rightHand.isActive)
        {
            rayInfoUpdateR.startPoint = new Vector3();
            rayInfoUpdateR.endPoint = new Vector3();
            rayInfoUpdateR.isActive = false;
            CmdUpdateHandsRay(rayInfoUpdateR);
        }
        // ����
        if (LeftSHRPointer == null)
        {
            LeftSHRPointer = GameObject.Find("MixedRealityPlayspace/Left_ShellHandRayPointer(Clone)");
        }
        if (LeftSHRPointer != null && LeftSHRPointerCursor == null)
        {
            LeftSHRPointerCursor = GameObject.Find("MixedRealityPlayspace/Left_ShellHandRayPointer(Clone)_Cursor");
        }
        if (LeftSHRPointer != null && LeftSHRPointerCursor != null)
        {
            if (LeftSHRPointer.activeInHierarchy == true && LeftSHRPointerCursor.activeInHierarchy == true)
            {
                rayInfoUpdateL.startPoint = LeftSHRPointer.transform.position;
                rayInfoUpdateL.endPoint = LeftSHRPointerCursor.transform.position;
                rayInfoUpdateL.isActive = true;
            }
            else
            {
                rayInfoUpdateL.startPoint = new Vector3();
                rayInfoUpdateL.endPoint = new Vector3();
                rayInfoUpdateL.isActive = false;
            }
            CmdUpdateHandsRay(rayInfoUpdateL);
        }
        else if (leftHand.isActive)
        {
            rayInfoUpdateL.startPoint = new Vector3();
            rayInfoUpdateL.endPoint = new Vector3();
            rayInfoUpdateL.isActive = false;
            CmdUpdateHandsRay(rayInfoUpdateL);
        }

    }

    [Command]
    private void CmdUpdateHandsRay(RayInfo rayInfo)
    {
        if (rayInfo.handMode == HandMode.right)
        {
            rightHand = rayInfo;
        }
        if (rayInfo.handMode == HandMode.left)
        {
            leftHand = rayInfo;
        }
    }


    private void PaintRayBy2Position(Vector3 startPoint, Vector3 endPoint, bool isActive, LineRenderer lineRenderer, HandMode handMode)
    {
        if (isActive == false)
        {
            lineRenderer.enabled = false;
            return;
        }

        GameObject tempCircle = null;
        if (handMode.Equals(HandMode.right)) { tempCircle = focusCircle_right; }
        else { tempCircle = focusCircle_left; }

        lineRenderer.enabled = true;

        Vector3 direction = endPoint - startPoint;
        Ray ray = new Ray(startPoint, direction);
        RaycastHit hit;
        bool isHit = Physics.Raycast(ray, out hit);
        tempCircle.SetActive(isHit);
        if (isHit)
        {
            lineRenderer.SetPosition(0, startPoint);
            lineRenderer.SetPosition(1, hit.point);

            Quaternion newRotation = Quaternion.LookRotation(hit.normal);
            tempCircle.transform.rotation = newRotation;
            tempCircle.transform.position = hit.point + hit.normal * circleDistanceRate;
        }
    }
}
