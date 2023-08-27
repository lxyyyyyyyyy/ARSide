using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SymbolDisocclusionVRA : MonoBehaviour
{
    private MirrorControllerA mirrorController; // 网络中控
    private GlobalUtils globalUtils; // 通用工具类

    public GameObject rotateSymbolPrefab;
    public GameObject pressSymbolPrefab;

    private GameObject rotateSymbolObject;
    private GameObject pressSymbolObject;

    // De occlusion parameters
    public float raiseStep = 0.1f;


    private void Start()
    {
        mirrorController = GetComponentInParent<MirrorControllerA>();
        globalUtils = GetComponent<GlobalUtils>();

        rotateSymbolObject = Instantiate(rotateSymbolPrefab);
        UtilSetThisObjectLayer(rotateSymbolObject.transform, LayerMask.NameToLayer("CameraNotVisible"));
        pressSymbolObject = Instantiate(pressSymbolPrefab);
        UtilSetThisObjectLayer(pressSymbolObject.transform, LayerMask.NameToLayer("CameraNotVisible"));

    }

    // 1.for symbol in server symbol list 
    // 
    private void Update()
    {
        // update rotation symbol
        for (int i = 0; i < mirrorController.syncRotationList.Count; ++i)
        {
            DPCSymbol curRotation = mirrorController.syncRotationList[i];
            // initialize symbol's transform          
            rotateSymbolObject.transform.position = curRotation.position;
            rotateSymbolObject.transform.forward = curRotation.up;
            //Debug.Log("position before: " + rotateSymbolObject.transform.position.ToString("f4"));
            // de occlusion
            symbolDisocclusion(rotateSymbolObject);
            // update curRotation's transform
            curRotation.position_new = rotateSymbolObject.transform.position;
            curRotation.up_new = rotateSymbolObject.transform.forward;
            //Debug.Log("position after: " + rotateSymbolObject.transform.position.ToString("f4"));
            mirrorController.CmdUpdateDPCRotation(curRotation);
        }
        // update press symbol
        for (int i = 0; i < mirrorController.syncPressList.Count; ++i)
        {
            DPCSymbol curPress = mirrorController.syncPressList[i];
            pressSymbolObject.transform.position = curPress.position;
            pressSymbolObject.transform.right = curPress.up;

            symbolDisocclusion(pressSymbolObject);

            curPress.position_new = pressSymbolObject.transform.position;
            curPress.up_new = pressSymbolObject.transform.right;
            mirrorController.CmdUpdateDPCPress(curPress);
        }
    }

    private void symbolDisocclusion(GameObject t)
    {
        // De occlusion calculation is performed here
        while (!globalUtils.GameObjectVisible(t))
        {
            
            t.transform.position += raiseStep * Vector3.up;
        }
    }


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
}
