using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraUpdateVR : MonoBehaviour
{
    private GameObject vrCamera;

    // Start is called before the first frame update
    void Start()
    {
        if (GlobleInfo.ClientMode.Equals(CameraMode.AR)) { return; }
        vrCamera = GameObject.Find("[CameraRig]/Camera");
    }

    // Update is called once per frame
    void Update()
    {
        if (GlobleInfo.ClientMode.Equals(CameraMode.AR)) { return; }
        if (vrCamera != null)
        {
            // Debug.Log("Update depth camera now...");
            transform.position = vrCamera.transform.position;
            transform.rotation = vrCamera.transform.rotation;
        }
        else
        {
            Debug.LogError("VR Camera not found");
        }
    }
}
