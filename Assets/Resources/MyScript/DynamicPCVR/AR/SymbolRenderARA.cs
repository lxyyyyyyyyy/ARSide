using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SymbolRenderARA : MonoBehaviour
{
    private MirrorControllerA mirrorController;

    private List<GameObject> rotationObjectList;
    private List<GameObject> pressObjectList;

    public GameObject RotateSymbol;
    public GameObject PressSymbol;

    // Start is called before the first frame update
    void Start()
    {
        mirrorController = GetComponentInParent<MirrorControllerA>();
        rotationObjectList = new List<GameObject>();
        pressObjectList = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        RenderRotation();
        RenderPress();
    }

    private void RenderRotation()
    {
        int n_curRotationObj = rotationObjectList.Count; // 当前渲染的标识总数
        int n_clientRotation = mirrorController.syncRotationList.Count; // 此时控制中心的标识总数

        int delta = n_clientRotation - n_curRotationObj;

        // update rotation

        // delete rotation 
        for (int i = 0; i > delta; --i)
        {
            GameObject tempObj = rotationObjectList[n_clientRotation - 1 + i];
            rotationObjectList.Remove(tempObj);
            Destroy(tempObj);
        }
        // add new rotation
        for (int i = 0; i < delta; ++i)
        {
            DPCSymbol newRotation = mirrorController.syncRotationList[n_curRotationObj + i];

            GameObject tempObj = Instantiate(RotateSymbol);
            tempObj.transform.parent = transform;
            tempObj.transform.position = newRotation.position_new;
            tempObj.transform.forward = newRotation.up_new;
            rotationObjectList.Add(tempObj);
        }
        // update symbol
        for(int i = 0; i < rotationObjectList.Count; i++)
        {
            rotationObjectList[i].transform.position = mirrorController.syncRotationList[i].position_new;
            rotationObjectList[i].transform.forward = mirrorController.syncRotationList[i].up_new;
        }
    }


    private void RenderPress()
    {
        int n_curPressObj = pressObjectList.Count;
        int n_clientPress = mirrorController.syncPressList.Count;

        int delta = n_clientPress - n_curPressObj;
        // delete press 
        for (int i = 0; i > delta; --i)
        {
            GameObject tempObj = pressObjectList[n_clientPress - 1 + i];
            pressObjectList.Remove(tempObj);
            Destroy(tempObj);
        }
        // add new press
        for (int i = 0; i < delta; ++i)
        {
            DPCSymbol newPress = mirrorController.syncPressList[n_curPressObj + i];
            GameObject tempObj = Instantiate(PressSymbol);
            tempObj.transform.parent = transform;
            tempObj.transform.position = newPress.position_new;
            tempObj.transform.right = newPress.up_new;
            pressObjectList.Add(tempObj);
        }
        // update symbol
        for (int i = 0; i < pressObjectList.Count; i++)
        {
            pressObjectList[i].transform.position = mirrorController.syncPressList[i].position_new;
            pressObjectList[i].transform.right = mirrorController.syncPressList[i].up_new;
        }
    }

}
