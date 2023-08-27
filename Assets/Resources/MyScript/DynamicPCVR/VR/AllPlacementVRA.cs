using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.Extras;

public class AllPlacementVRA : MonoBehaviour
{

    private MirrorControllerA myController;
    private Exp myExp;
    private GlobalUtilsVR globalUtils; // VR工具类，用于深度碰撞

    private SymbolMode currentSymbolMode = SymbolMode.ARROW; // default mode is arrow

    public SteamVR_Action_Boolean switchSymbolMode;
    public SteamVR_Action_Boolean confirmSelection;
    public SteamVR_Action_Boolean deleteLastSymbol;

    private List<Vector3> currentPointList = new List<Vector3>();

    private GameObject rightHand;

    public GameObject rotateSymbolPrefab;
    public GameObject pressSymbolPrefab;
    private GameObject rotateSymbol;
    private GameObject pressSymbol;
    public GameObject assistPlaceSpherePrefab;
    private GameObject assistPlaceSphere;
    public GameObject AxesSymbolPrefab;

    // 辅助线段选点
    public GameObject drawpointprefab;
    private List<GameObject> drawpointList;
    // 辅助分割选点
    private List<GameObject> splitPointVisble;
    private List<Vector3> splitPoints;
    private List<GameObject> splitObjects;
    // 辅助轴
    private List<GameObject> initialAxesObjects;
    private List<GameObject> FinalAxesObjects;

    public bool autoGenerateLine;

    private enum SymbolPRState
    {
        Inactive = 0, SelectPosition, SelectRotation
    }
    private SymbolPRState nowPRState = SymbolPRState.Inactive;

    private enum SplitState
    {
        SelectSplitpoint = 0, SelectPosition, ManipulateSplitObj
    }
    private SplitState nowSplitState = SplitState.SelectSplitpoint;

    private enum AxesState
    {
        SelectAxesPosition = 0, ManipulateAxes, SelectAnotherAxesPosition, ManipulateAnotherAxes
    }
    private AxesState nowAxesState = AxesState.SelectAxesPosition;


    // Start is called before the first frame update
    void Start()
    {
        myController = GetComponentInParent<MirrorControllerA>();
        myExp = GetComponent<Exp>();
        globalUtils = GetComponent<GlobalUtilsVR>();

        rightHand = GameObject.Find("[CameraRig]/Controller (right)");

        assistPlaceSphere = Instantiate(assistPlaceSpherePrefab);
        assistPlaceSphere.layer = LayerMask.NameToLayer("AssitRotateSphere"); ;
        assistPlaceSphere.SetActive(false);

        rotateSymbol = Instantiate(rotateSymbolPrefab);
        rotateSymbol.SetActive(false);

        pressSymbol = Instantiate(pressSymbolPrefab);
        pressSymbol.SetActive(false);

        drawpointList = new List<GameObject>();
        splitPointVisble = new List<GameObject>();
        splitPoints = new List<Vector3>();
        splitObjects = new List<GameObject>();
        initialAxesObjects = new List<GameObject>();
        FinalAxesObjects = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        if (switchSymbolMode.GetStateDown(SteamVR_Input_Sources.LeftHand))      // VR端开始画标识, AR端结束, 左手扳机键
        {
            myExp.VRBeginAREnd();
        }

        if (!myExp.GetVRExpState()) return;

        if (switchSymbolMode.GetStateDown(SteamVR_Input_Sources.RightHand))     // 切换 画线<->分割物体 右手扳机键
        {
            // SwitchSymbolMode();
            Debug.Log("switch symbol mode: " + currentSymbolMode);
        }

        if (myExp.exp_type == Exp.ExpType.CG)
        {
            currentSymbolMode = SymbolMode.Axes;
        }
        if (myExp.exp_type == Exp.ExpType.EG1)
        {
            currentSymbolMode = SymbolMode.SPLIT;
        }


        if (currentSymbolMode.Equals(SymbolMode.ARROW))
        {
            if (confirmSelection.GetStateDown(SteamVR_Input_Sources.RightHand))     // 右手A键
            {
                Debug.Log("press the select button");
                AddArrowPoint();
            }
            if (deleteLastSymbol.GetStateDown(SteamVR_Input_Sources.RightHand))     // 右手B键
            {
                Debug.Log("press the delete button");
                DeleteLastArrow();
            }
        }
        else if (currentSymbolMode.Equals(SymbolMode.SPLIT))
        {
            if (confirmSelection.GetStateDown(SteamVR_Input_Sources.RightHand))     // 右手A键
            {
                if (nowSplitState == SplitState.SelectSplitpoint)   // 选点
                {
                    AddSplitPoint();   
                }
                else if (nowSplitState == SplitState.SelectPosition)
                {
                    SelectSplitPosition();
                }
                else   // 停止操作并发送给AR端
                {
                    ConfirmSyncSplit();
                }
            }
            if (confirmSelection.GetStateDown(SteamVR_Input_Sources.LeftHand))  // 选点结束 左手A键
            {
                if (splitPoints.Count >= 3) ConfirmSplit();
            }
            if (deleteLastSymbol.GetStateDown(SteamVR_Input_Sources.RightHand)) // 删除上一个split的物体  右手B键
            {
                DeleteLastSplit();
            }
        }
        else if (currentSymbolMode.Equals(SymbolMode.Axes))
        {
            if (confirmSelection.GetStateDown(SteamVR_Input_Sources.RightHand))     // 右手A键
            {
                if (nowAxesState == AxesState.SelectAxesPosition)   // 选点
                {
                    AddAxes();   
                }
                else if (nowAxesState == AxesState.ManipulateAxes)
                {
                    globalUtils.RestManipulateObj();
                    nowAxesState = AxesState.SelectAnotherAxesPosition;
                } 
                else if (nowAxesState == AxesState.SelectAnotherAxesPosition)
                {
                    AddAnotherAxes();
                }
                else
                {
                    ConfirmSyncAxes();   
                }
            }
            if (deleteLastSymbol.GetStateDown(SteamVR_Input_Sources.RightHand)) // 删除上一个Axes  右手B键
            {
                DeleteAxes();
            }
        }

    }

    /// <summary>
    /// clear environment, and then switch mode
    /// </summary>
    private void SwitchSymbolMode()
    {
        // 操作过程中切换模式 clear environment
        if (currentSymbolMode.Equals(SymbolMode.ARROW))
        {
            currentPointList.Clear();
            for (int i = 0; i < drawpointList.Count; i++)
            {
                Destroy(drawpointList[i]);
            }
            drawpointList.Clear();
        }

        if (currentSymbolMode.Equals(SymbolMode.SPLIT))
        {
            splitPoints.Clear();
            for (int i = 0; i < splitPointVisble.Count; i++)
            {
                DestroyGameObject(splitPointVisble[i]);
            }
            splitPointVisble.Clear();
        }

        nowPRState = SymbolPRState.Inactive;
        nowSplitState = SplitState.SelectSplitpoint;
        nowAxesState = AxesState.SelectAxesPosition;

        // switch mode
        int n_symbol = System.Enum.GetNames(typeof(SymbolMode)).Length; // get symbol numbers
        currentSymbolMode = (SymbolMode)(((int)currentSymbolMode + 1) % n_symbol);
        
        if (myExp.exp_type == Exp.ExpType.CG && currentSymbolMode.Equals(SymbolMode.SPLIT))
        {
            currentSymbolMode = SymbolMode.Axes;
        }
        if (myExp.exp_type == Exp.ExpType.EG1 && currentSymbolMode.Equals(SymbolMode.Axes))
        {
            currentSymbolMode = SymbolMode.SPLIT;
        }
    }

    private void AddArrowPoint()
    {
        Vector3 newPoint = globalUtils.GetCollisionPoint();
        Debug.Log("select point is:" + newPoint.ToString());

        int currentPointNumber = currentPointList.Count;
        if (currentPointNumber < 2)
        {
            GameObject pointobj = Instantiate(drawpointprefab);
            pointobj.transform.position = newPoint;
            pointobj.layer = LayerMask.NameToLayer("DepthCameraUnivisible");
            drawpointList.Add(pointobj);
            Debug.Log("current point number is:" + currentPointNumber + ", add new point");
            currentPointList.Add(newPoint);

        }
        if (currentPointNumber == 2)
        {
            Debug.Log("current point number is:" + currentPointNumber + ", update segment");
            myController.CmdAddDPCArrow(new DPCArrow()
            {
                index = myController.syncArrowList.Count,
                startPoint = currentPointList[0],
                endPoint = currentPointList[1],
                curvePointList = new List<Vector3[]>(),
                originPointList = new List<Vector3[]>(),
            });
            // 清空临时变量
            currentPointList.Clear();
            for (int i = 0; i < drawpointList.Count; i++)
            {
                Destroy(drawpointList[i]);
            }
            drawpointList.Clear();
        }
    }

    private void DeleteLastArrow()
    {
        myController.CmdDeleteDPCArrow();
    }

    // ======================================= Split ========================================
    private void AddSplitPoint()
    {
        
        Vector3 p = globalUtils.GetCollisionPoint();
        splitPoints.Add(p);

        GameObject t = Instantiate(drawpointprefab);
        t.transform.position = p;
        splitPointVisble.Add(t);
    }

    private void SelectSplitPosition()
    {
        Vector3 p = globalUtils.GetCollisionPoint();

        GameObject t = splitObjects[splitObjects.Count - 1];
        t.transform.position = p;
        globalUtils.SetManipulateObj(t);

        nowSplitState = SplitState.ManipulateSplitObj;
    }

    private void ConfirmSplit()
    {
        splitPoints.Add(splitPoints[0]);

        Vector3 center = new Vector3();
        List<List<Vector3>> vertices = new List<List<Vector3>>();
        List<List<Color>> color = new List<List<Color>>();

        GameObject fa = GetComponent<SplitVRA>().SplitCPU(splitPoints, ref center, ref vertices, ref color);
        splitObjects.Add(fa);
        myExp.RecordObjInitRot(fa.transform.eulerAngles);

        myController.CmdAddDPCSplitMesh(new DPCSplitMesh()
        {
            index = myController.syncSplitMeshList.Count,
            center = center,
            color = color,
            vertices = vertices,
        });

        myController.CmdAddDPCSplitPos(new DPCSplitPosture()
        {
            index = myController.syncSplitPosList.Count,
            valid = false,
            position = center,
            rotation = new Quaternion(),
        });
        Debug.Assert(myController.syncSplitMeshList.Count == myController.syncSplitPosList.Count);
        
        splitPoints.Clear();
        foreach (GameObject g in splitPointVisble) Destroy(g);
        splitPointVisble.Clear();

        nowSplitState = SplitState.SelectPosition;
    }

    private void DeleteLastSplit()
    {
        GameObject father = splitObjects[splitObjects.Count-1];
        DestroyGameObject(father);
        splitObjects.RemoveAt(splitObjects.Count - 1);

        int lineIndex = myController.syncSplitPosList[myController.syncSplitPosList.Count - 1].correspondingLineIndex;
        if (lineIndex != -1) myController.CmdDeleteDPCArrow(lineIndex);

        myController.CmdDeleteDPCSplitMesh();
        myController.CmdDeleteDPCSplitPos();
        Debug.Assert(myController.syncSplitMeshList.Count == myController.syncSplitPosList.Count);

        if (nowSplitState == SplitState.ManipulateSplitObj)
        {
            globalUtils.RestManipulateObj();
            nowSplitState = SplitState.SelectSplitpoint;
        }
    }

    private void ConfirmSyncSplit()
    {
        int i = splitObjects.Count - 1;

        myExp.RecordObjEndRot(splitObjects[i].transform.eulerAngles);
        myExp.VREndARBegin();

        myController.CmdUpdateDPCSplitPos(new DPCSplitPosture()     // 释放时一定更新
        {
            index = myController.syncSplitPosList.Count - 1,
            valid = true,
            position = splitObjects[i].transform.position,
            rotation = splitObjects[i].transform.rotation,
            correspondingLineIndex = autoGenerateLine ? myController.syncArrowList.Count : -1,
        });

        if (autoGenerateLine)
        {
            myController.CmdAddDPCArrow(new DPCArrow()
            {
                index = myController.syncArrowList.Count,
                startPoint = myController.syncSplitMeshList[i].center,
                endPoint = splitObjects[i].transform.position,
                curvePointList = new List<Vector3[]>(),
                originPointList = new List<Vector3[]>(),
            });
        }

        globalUtils.RestManipulateObj();
        nowSplitState = SplitState.SelectSplitpoint;
    }

    // ======================================= Axes ========================================
    private void AddAxes()
    {
        Vector3 p = globalUtils.GetCollisionPoint();
        GameObject Axes = Instantiate(AxesSymbolPrefab);
        Axes.transform.position = p;
        initialAxesObjects.Add(Axes);
        FinalAxesObjects.Add(null);

        globalUtils.SetManipulateObj(Axes);
        nowAxesState = AxesState.ManipulateAxes;
    }

    private void AddAnotherAxes()
    {
        Vector3 p = globalUtils.GetCollisionPoint();
        GameObject Axes = Instantiate(AxesSymbolPrefab);
        Axes.transform.position = p;
        FinalAxesObjects[FinalAxesObjects.Count - 1] = Axes;

        globalUtils.SetManipulateObj(Axes);
        nowAxesState = AxesState.ManipulateAnotherAxes;
    }

    private void ConfirmSyncAxes()
    {
        myExp.RecordObjEndRot(FinalAxesObjects[FinalAxesObjects.Count - 1].transform.eulerAngles);
        myExp.VREndARBegin();

        int i = initialAxesObjects.Count - 1;
        myController.CmdAddDPCAxes(new DPCAxes()
        {
            index = myController.syncAxesList.Count,
            init_position = initialAxesObjects[i].transform.position,
            init_rotation = initialAxesObjects[i].transform.rotation,
            end_position = FinalAxesObjects[i].transform.position,
            end_rotation = FinalAxesObjects[i].transform.rotation,
            correspondingLineIndex = autoGenerateLine ? myController.syncArrowList.Count : -1,
        });

        if (autoGenerateLine)
        {
            myController.CmdAddDPCArrow(new DPCArrow()
            {
                index = myController.syncArrowList.Count,
                startPoint = initialAxesObjects[i].transform.position,
                endPoint = FinalAxesObjects[i].transform.position,
                curvePointList = new List<Vector3[]>(),
                originPointList = new List<Vector3[]>(),
            });
        }

        globalUtils.RestManipulateObj();
        nowAxesState = AxesState.SelectAxesPosition;
    }

    private void DeleteAxes()
    {
        GameObject axes1 = initialAxesObjects[initialAxesObjects.Count - 1];
        GameObject axes2 = FinalAxesObjects[FinalAxesObjects.Count - 1];

        DestroyGameObject(axes1);
        initialAxesObjects.RemoveAt(initialAxesObjects.Count - 1);

        if (axes2) DestroyGameObject(axes2);
        FinalAxesObjects.RemoveAt(FinalAxesObjects.Count - 1);

        int lineIndex = myController.syncAxesList[myController.syncAxesList.Count - 1].correspondingLineIndex;
        if (lineIndex != -1) myController.CmdDeleteDPCArrow(lineIndex);

        myController.CmdDeleteDPCAxes();
        if (nowAxesState == AxesState.ManipulateAxes || nowAxesState == AxesState.ManipulateAnotherAxes)
        {
            globalUtils.RestManipulateObj();
            nowAxesState = AxesState.SelectAxesPosition;
        }
    }
    
    // ======================================= Abandon ========================================
    private void AddRotation()
    {
        if (nowPRState == SymbolPRState.Inactive)
        {
            nowPRState = SymbolPRState.SelectPosition;
        }

        Ray ray = new Ray(rightHand.transform.position, rightHand.transform.forward);
        RaycastHit hitInfo;
        int assitSphereLayer = LayerMask.NameToLayer("AssitRotateSphere");
        int onlyCastAssitSphere = 1 << (assitSphereLayer);

        // fisrt select assist sphere position
        if (nowPRState == SymbolPRState.SelectPosition)
        {
            if (confirmSelection.GetStateDown(SteamVR_Input_Sources.RightHand))
            {
                Debug.Log("Rotation symbol, select assist sphere position");
                assistPlaceSphere.SetActive(true);
                assistPlaceSphere.transform.position = globalUtils.GetCollisionPoint();
                assistPlaceSphere.GetComponent<MeshRenderer>().enabled = true;

                nowPRState = SymbolPRState.SelectRotation;
                rotateSymbol.SetActive(true);
            }
        }

        // second select symbol rotation on surface, and confirm
        else if (nowPRState.Equals(SymbolPRState.SelectRotation))
        {
            Debug.Log("state is select rotation");
            if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, onlyCastAssitSphere))
            {
                Debug.Log("ray hit");
                rotateSymbol.transform.position = hitInfo.point;
                rotateSymbol.transform.forward = hitInfo.normal;
                if (confirmSelection.GetStateDown(SteamVR_Input_Sources.RightHand))
                {
                    Debug.Log("Rotation symbol, select symbol position");
                    assistPlaceSphere.SetActive(false);
                    nowPRState = SymbolPRState.Inactive;
                    myController.CmdAddDPCRotation(new DPCSymbol()
                    {
                        index = myController.syncRotationList.Count,
                        up = hitInfo.normal,
                        position = rotateSymbol.transform.position,
                        up_new = new Vector3(),
                        position_new = new Vector3()
                    }) ;
                    rotateSymbol.SetActive(false);
                }

            }
        }

    }

    private void AddPress()
    {
        if (nowPRState == SymbolPRState.Inactive)
        {
            nowPRState = SymbolPRState.SelectPosition;
        }

        Ray ray = new Ray(rightHand.transform.position, rightHand.transform.forward);
        RaycastHit hitInfo;
        int assitSphereLayer = LayerMask.NameToLayer("AssitRotateSphere");
        int onlyCastAssitSphere = 1 << (assitSphereLayer);

        if (nowPRState.Equals(SymbolPRState.SelectPosition))
        {
            if (confirmSelection.GetStateDown(SteamVR_Input_Sources.RightHand))
            {
                Debug.Log("Press symbol, select assist sphere position");
                assistPlaceSphere.SetActive(true);
                assistPlaceSphere.transform.position = globalUtils.GetCollisionPoint();
                assistPlaceSphere.GetComponent<MeshRenderer>().enabled = true;
                nowPRState = SymbolPRState.SelectRotation;
                pressSymbol.SetActive(true);
            }
        }

        else if (nowPRState.Equals(SymbolPRState.SelectRotation))
        {
            if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, onlyCastAssitSphere))
            {

                pressSymbol.transform.position = hitInfo.point + 0.05f * hitInfo.normal;
                pressSymbol.transform.right = hitInfo.normal;
                if (confirmSelection.GetStateDown(SteamVR_Input_Sources.RightHand))
                {
                    Debug.Log("Press symbol, select symbol position");
                    assistPlaceSphere.SetActive(false);
                    nowPRState = SymbolPRState.Inactive;
                    myController.CmdAddDPCPress(new DPCSymbol()
                    {
                        index = myController.syncPressList.Count,
                        up = hitInfo.normal,
                        position = pressSymbol.transform.position,
                        up_new = new Vector3(),
                        position_new = new Vector3()
                    });
                    pressSymbol.SetActive(false);
                }
            }
        }

    }

    private void DestroyGameObject(GameObject t)
    {
        if (!t) return;

        int j = 0;
        while (j < t.transform.childCount)
        {
            Destroy(t.transform.GetChild(j++).gameObject);
        }
        Destroy(t);
    }

}
