using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.Extras;

public class ManipulateVRA : MonoBehaviour
{
    public GameObject targetObj;    // manipulate target object
    public GameObject grabObj;      // for rotate

    private GameObject VRHandLeft;
    private GameObject VRHandRight;  
    private Vector3 VRhandtPosPre;  // for translate

    public SteamVR_Action_Boolean manipulate;

    private LaserVRA laser;
    private MirrorControllerA myController;

    // Start is called before the first frame update
    void Start()
    {
        myController = GetComponentInParent<MirrorControllerA>();

        VRHandLeft = GameObject.Find("[CameraRig]/Controller (left)");
        VRHandRight = GameObject.Find("[CameraRig]/Controller (right)");
        laser = VRHandRight.GetComponent<LaserVRA>();
    }

    // Update is called once per frame
    void Update()
    {
        if (targetObj == null) return;

        if (manipulate.GetState(SteamVR_Input_Sources.LeftHand))    // Rot
        {
            GrabObject();
            targetObj.transform.rotation = grabObj.transform.rotation;
        }

        if (manipulate.GetState(SteamVR_Input_Sources.RightHand))   // Translate
        {
            Vector3 offsetPos = VRHandRight.transform.position - VRhandtPosPre;
            targetObj.transform.position += offsetPos;
        }

        if (!manipulate.GetState(SteamVR_Input_Sources.LeftHand))   //Rot Release
        {
            ReleaseObject();
        }

        VRhandtPosPre = VRHandRight.transform.position;
    }

    public void RegisterObj(GameObject o) {
        targetObj = o;
        laser.SetTarget(o);
    }

    public void UnRegisterObj() {
        grabObj.transform.rotation = new Quaternion();  // 为下一个物体的旋转做准备，暂时不知道是否需要
        targetObj = null;
        laser.UnsetTarget();
    }

    void GrabObject()
    {
        if (!VRHandLeft.GetComponent<FixedJoint>().connectedBody)
            VRHandLeft.GetComponent<FixedJoint>().connectedBody = grabObj.GetComponent<Rigidbody>();
    }

    void ReleaseObject()
    {
        VRHandLeft.GetComponent<FixedJoint>().connectedBody = null;
    }
}
