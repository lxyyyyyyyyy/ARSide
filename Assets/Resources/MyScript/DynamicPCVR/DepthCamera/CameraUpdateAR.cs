using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraUpdateAR : MonoBehaviour
{
    private GameObject arCamera;

    // Start is called before the first frame update
    void Start()
    {
        if (GlobleInfo.ClientMode.Equals(CameraMode.VR)) { return; }
        arCamera = GameObject.Find("MixedRealityPlayspace/Main Camera");
    }

    // Update is called once per frame
    void Update()
    {
        if (GlobleInfo.ClientMode.Equals(CameraMode.VR)) { return; }
        if (arCamera!=null)
        {
            Debug.Log("Update depth camera now...");
            transform.position = arCamera.transform.position;
            transform.rotation = arCamera.transform.rotation;
        }
        else
        {
            Debug.LogError("AR Camera not found");
        }
    }
}
