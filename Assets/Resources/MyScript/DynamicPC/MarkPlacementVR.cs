using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.Extras;

public class MarkPlacementVR : MonoBehaviour
{

    private MirrorController myController;
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

    public GlobalUtils globalUtils;

    public GameObject assistPlaceSpherePrefab;
    private GameObject assistPlaceSphere;
    public GameObject assistColliderSpherePrefab;
    private GameObject assistColliderSphere;


    public GameObject drawpointprefab;
    public List<GameObject> drawpointList;

    private enum SymbolPRState
    {
        Inactive = 0, SelectPosition, SelectRotation
    }
    private SymbolPRState nowPRState = SymbolPRState.Inactive;

    private void Awake()
    {
        globalUtils = GetComponentInParent<GlobalUtils>();
    }

    // Start is called before the first frame update
    void Start()
    {
        myController = GetComponentInParent<MirrorController>();

        rightHand = GameObject.Find("[CameraRig]/Controller (right)");
        
        assistPlaceSphere = Instantiate(assistPlaceSpherePrefab);
        assistPlaceSphere.layer = LayerMask.NameToLayer("AssitRotateSphere"); ;
        assistPlaceSphere.SetActive(false);

        assistColliderSphere = Instantiate(assistColliderSpherePrefab);
        assistColliderSphere.layer = LayerMask.NameToLayer("VRCameraUnvisible"); ;
        assistColliderSphere.SetActive(false);

        rotateSymbol = Instantiate(rotateSymbolPrefab);
        rotateSymbol.SetActive(false);

        pressSymbol = Instantiate(pressSymbolPrefab);
        pressSymbol.SetActive(false);

        drawpointList = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        if (switchSymbolMode.GetStateDown(SteamVR_Input_Sources.RightHand))
        {
            SwitchSymbolMode();
            Debug.Log("switch symbol mode: " + currentSymbolMode);
        }
        if (currentSymbolMode.Equals(SymbolMode.ARROW))
        {
            if (confirmSelection.GetStateDown(SteamVR_Input_Sources.RightHand))
            {
                Debug.Log("press the select button");
                AddArrowPoint();
            }
            if (deleteLastSymbol.GetStateDown(SteamVR_Input_Sources.RightHand))
            {
                Debug.Log("press the delete button");
                DeleteLastArrow();
            }
        }
        // else if (currentSymbolMode.Equals(SymbolMode.PRESS))
        {
            AddPress();
        }
        // else if (currentSymbolMode.Equals(SymbolMode.ROTATE))
        {
            AddRotation();
        }
    }

    /// <summary>
    /// clear environment, and then switch mode
    /// </summary>
    private void SwitchSymbolMode() {
        // clear environment
        if (currentSymbolMode.Equals(SymbolMode.ARROW))
        {
            currentPointList.Clear();
        }
        nowPRState = SymbolPRState.Inactive;

        // switch mode
        int n_symbol = System.Enum.GetNames(typeof(SymbolMode)).Length; // get symbol numbers
        currentSymbolMode = (SymbolMode)(((int)currentSymbolMode+1)% n_symbol);

        
    }

    private void AddArrowPoint() {
        Vector3 newPoint = GetCollisionPoint();
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
            myController.CmdUpdateSegmentInfo(new SegmentInfo()
            {
                startPoint = currentPointList[0],
                endPoint = currentPointList[1]
            });
            currentPointList.Clear();
            for(int i = 0; i < drawpointList.Count; i++)
            {
                Destroy(drawpointList[i]);
            }
            drawpointList.Clear();
        }
    }

    private void DeleteLastArrow() {

        Debug.Log("VR客户端发起删除线段请求");
        myController.CmdDeleteSegmentInfo();
    }

    private Vector3 GetCollisionPoint()
    {
        //TODO
        int MAXSTEP = 1000, stepCount = 0;
        float step = 0.01f;
        assistColliderSphere.transform.position = rightHand.transform.position;
        while (globalUtils.GameObjectVisible(assistColliderSphere))
        {
            assistColliderSphere.transform.position += step * rightHand.transform.forward;
            stepCount++;
            if (stepCount > MAXSTEP) break;
        }

        return (assistColliderSphere.transform.position - step * rightHand.transform.forward);
    }

    private void AddRotation()
    {
        if(nowPRState == SymbolPRState.Inactive)
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
                assistPlaceSphere.transform.position = GetCollisionPoint();
                assistPlaceSphere.GetComponent<MeshRenderer>().enabled = true;

                nowPRState = SymbolPRState.SelectRotation;
                rotateSymbol.SetActive(true);
            }
        }

        // second select symbol rotation on surface, and confirm
        else if (nowPRState.Equals(SymbolPRState.SelectRotation))
        {
            Debug.Log("state is select rotation");
            if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity,onlyCastAssitSphere))
            {
                Debug.Log("ray hit");
                rotateSymbol.transform.position = hitInfo.point;
                rotateSymbol.transform.forward = hitInfo.normal;
                if (confirmSelection.GetStateDown(SteamVR_Input_Sources.RightHand))
                {
                    Debug.Log("Rotation symbol, select symbol position");
                    assistPlaceSphere.SetActive(false);
                    nowPRState = SymbolPRState.Inactive;
                    myController.CmdUpdateRotationInfo(new SymbolInfo()
                    {
                        up = hitInfo.normal,
                        position = rotateSymbol.transform.position
                    });
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
                assistPlaceSphere.transform.position = GetCollisionPoint();
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
                    myController.CmdUpdatePressInfo(new SymbolInfo()
                    {
                        up = hitInfo.normal,
                        position = pressSymbol.transform.position
                    });
                    pressSymbol.SetActive(false);
                }
            }
        }

    }



}
