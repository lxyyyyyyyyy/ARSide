using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientModeController : MonoBehaviour
{
    public GameObject MixedRealityToolkit;
    public GameObject MixedRealityPlayspace;
    public GameObject MixedRealitySceneContent;
    public GameObject CameraRig;

    void OnValidate()
    {
        if(MixedRealityToolkit == null || MixedRealityPlayspace==null || MixedRealitySceneContent==null || CameraRig == null)
        {
            Debug.Log("some object is null");
            return;
        }
        if (GlobleInfo.ClientMode.Equals(CameraMode.VR))
        {
            CameraRig.SetActive(true);
            MixedRealityToolkit.SetActive(false);
            MixedRealityPlayspace.SetActive(false);
            MixedRealitySceneContent.SetActive(false);
        }
        if (GlobleInfo.ClientMode.Equals(CameraMode.AR))
        {
            CameraRig.SetActive(false);
            MixedRealityToolkit.SetActive(true);
            MixedRealityPlayspace.SetActive(true);
            MixedRealitySceneContent.SetActive(true);
        }
    }
}
