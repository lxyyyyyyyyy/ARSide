using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthCamera : MonoBehaviour
{
    private GameObject ARCamera;
    public GameObject cameraDepth;
    private Mirror_MyController mirror_MyController;
    // Start is called before the first frame update
    void Start()
    {
        mirror_MyController = GetComponent<Mirror_MyController>();

        if (GlobleInfo.ClientMode.Equals(CameraMode.AR))
        {
            ARCamera = GameObject.Find("MixedRealityPlayspace/Main Camera");
        }
        if (ARCamera == null)
        {
            Debug.LogError("AR Camera not find");
            ARCamera = null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (GlobleInfo.ClientMode.Equals(CameraMode.AR))
        {
            cameraDepth.transform.position = ARCamera.GetComponent<Transform>().position;
            cameraDepth.transform.rotation = ARCamera.GetComponent<Transform>().rotation;
        }
        if (GlobleInfo.ClientMode.Equals(CameraMode.VR))
        {

        }
    }
}
