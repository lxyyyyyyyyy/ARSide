using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Valve.VR.Extras;

public class VR_PlayerScript : NetworkBehaviour
{
    public GameObject VRCamera;
    private Vector3 Position;
    private Vector3 Rotation;
    [Header("Initial Y Position")]
    [Tooltip("角色初始高度")]
    public float avatarY = -0.8f;

    public SteamVR_LaserPointer controllerRight;
    public SteamVR_LaserPointer controllerLeft;

    public GameObject rightHandController;
    public GameObject leftHandController;
    private LineRenderer rightHandLineRenderer;
    private LineRenderer leftHandLineRenderer;

    [SyncVar]
    public RayInfo rightHand;
    [SyncVar]
    public RayInfo leftHand;

    [Header("Hit Circle")]
    [Tooltip("射线碰撞点圆环，将预制体拖入此处")]
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
        
        VRCamera = GameObject.Find("[CameraRig]/Camera");
        if (VRCamera == null)
        {
            Debug.LogError("VR Camera not find");
            VRCamera = null;
        }
        controllerRight = GameObject.Find("[CameraRig]/Controller (right)").GetComponent<SteamVR_LaserPointer>();
        controllerLeft = GameObject.Find("[CameraRig]/Controller (left)").GetComponent<SteamVR_LaserPointer>();


        // 将Local Avator设置为本地相机不可见
        // 3:CameraNotVisible
        UtilSetThisObjectLayer(this.transform, 3);

    }

    // Update is called once per frame
    void Update()
    {
        if (isLocalPlayer)
        {
            Position = VRCamera.GetComponent<Transform>().position;
            Rotation = VRCamera.GetComponent<Transform>().eulerAngles;
            Position.y = avatarY;
            Rotation.x = 0;
            Rotation.z = 0;
            this.GetComponent<Transform>().position = Position;
            this.GetComponent<Transform>().eulerAngles = Rotation;

            VRGetRayInfo();
        }
        if (!isLocalPlayer && isClient)
        {
            PaintRayBy2Position(rightHand.startPoint, rightHand.endPoint, rightHand.isActive, rightHandLineRenderer,HandMode.right);
            PaintRayBy2Position(leftHand.startPoint, leftHand.endPoint, leftHand.isActive, leftHandLineRenderer,HandMode.left);
        }

        

    }

    /// <summary>
    /// 根据输入的参数设置当前物体及其子物体的Layer
    /// </summary>
    /// <param name="layer"></param>
    private void UtilSetThisObjectLayer(Transform transform, int layer)
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

    private void VRGetRayInfo()
    {
        RayInfo rayInfoUpdateR = new RayInfo();
        RayInfo rayInfoUpdateL = new RayInfo();
        rayInfoUpdateR.handMode = HandMode.right;
        rayInfoUpdateL.handMode = HandMode.left;
        
        if (controllerRight.isHit)
        {
            rayInfoUpdateR.startPoint = controllerRight.startPoint;
            rayInfoUpdateR.endPoint = controllerRight.endPoint;
            rayInfoUpdateR.isActive = true;
        }
        else
        {
            rayInfoUpdateR.startPoint = new Vector3();
            rayInfoUpdateR.endPoint = new Vector3();
            rayInfoUpdateR.isActive = false;
        }

        if (controllerLeft.isHit)
        {
            rayInfoUpdateL.startPoint = controllerLeft.startPoint;
            rayInfoUpdateL.endPoint = controllerLeft.endPoint;
            rayInfoUpdateL.isActive = true;
        }
        else
        {
            rayInfoUpdateL.startPoint = new Vector3();
            rayInfoUpdateL.endPoint = new Vector3();
            rayInfoUpdateL.isActive = false;
        }

        CmdUpdateHandsRay(rayInfoUpdateR);
        CmdUpdateHandsRay(rayInfoUpdateL);

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
        if (handMode.Equals(HandMode.right)){ tempCircle = focusCircle_right;}
        else{ tempCircle = focusCircle_left;}

        lineRenderer.enabled = true;

        Vector3 direction = endPoint - startPoint;
        Ray ray = new Ray(startPoint, direction);
        RaycastHit hit;
        bool isHit = Physics.Raycast(ray, out hit);
        tempCircle.SetActive(isHit); // 只在碰撞时显示圆环
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
