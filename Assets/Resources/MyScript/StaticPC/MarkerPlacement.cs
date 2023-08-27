using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Valve.VR;
using Valve.VR.Extras;
using Valve.VR.InteractionSystem;

public class MarkerPlacement : MonoBehaviour
{
    public Material lineMaterial;
    public float lineThickness = 0.01f; 

    private List<GameObject> symbolList;
    
    public GameObject RotateSymbol;
    public GameObject PressSymbol;
    private GameObject symbol;
    private GameObject copySymbol;
    public GameObject AssitRotateSphere;
    public GameObject leftHand;

    public SteamVR_Action_Boolean PlaceRotButton, PlacePressButton, Confirm;
    public bool rotState = false;

    // qinwen code
    private GameObject assitRotation;
    private Mirror_MyController mirror_MyController;


    private enum State
    {
        Inactive = 0, SelectPosition, SelectRotation
    };
    private State nowState = 0;

    // public Button PlaceRotButton, PlacePressButton;

    // Start is called before the first frame update
    void Start()
    {
        symbolList = new List<GameObject>(); 
        leftHand = GameObject.Find("[CameraRig]/Controller (left)");
        // qinwen code
        assitRotation = GameObject.Instantiate(AssitRotateSphere);
        assitRotation.layer = 10;
        assitRotation.SetActive(false);
        mirror_MyController = GetComponent<Mirror_MyController>();
        


        // PlaceRotButton = GameObject.Find("PlaceRotMarker").GetComponent<Button>();
        // PlaceRotButton.onClick.AddListener(ActivateRotPlacement);

        // PlacePressButton = GameObject.Find("PlacePressMarker").GetComponent<Button>();
        // PlacePressButton.onClick.AddListener(ActivatePressPlacement);
    }

    // Update is called once per frame
    void Update()
    {
        if (GlobleInfo.ClientMode.Equals(CameraMode.AR)) { return; }
        if (PlaceRotButton.GetStateDown(SteamVR_Input_Sources.LeftHand)) {
            Debug.Log("按下了旋转按钮");
            ActivateRotPlacement();
        }

        if (PlacePressButton.GetStateDown(SteamVR_Input_Sources.LeftHand)) {
            Debug.Log("按下了按压按钮");
            ActivatePressPlacement();
        }

        if (nowState == State.Inactive) {
            return;
        }

        // Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Ray ray = new Ray(leftHand.transform.position, leftHand.transform.forward);
        RaycastHit hitInfo;
        
        int assitSphereLayer = LayerMask.NameToLayer("AssitRotateSphere");
        int onlyCastAssitSphere = 1 << (assitSphereLayer);
        int ignoreAssotSphere = ~onlyCastAssitSphere;

        if (nowState == State.SelectPosition) {
            
            if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, ignoreAssotSphere)) {
                assitRotation.transform.position = hitInfo.point;
                assitRotation.GetComponent<MeshRenderer>().enabled = true;
                if(Confirm.GetStateDown(SteamVR_Input_Sources.LeftHand)) 
                {
                    Debug.Log("按下了确认按钮");
                    nowState = State.SelectRotation;
                    copySymbol = GameObject.Instantiate(symbol);
                }
            }
        }
        
        else if (nowState == State.SelectRotation) {

            if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, onlyCastAssitSphere)) {

                
                if (rotState)
                {
                    copySymbol.transform.position = hitInfo.point;
                    copySymbol.transform.forward = hitInfo.normal;
                } else
                {
                    copySymbol.transform.position = hitInfo.point + 0.05f * hitInfo.normal;
                    copySymbol.transform.right = hitInfo.normal;
                }

                if(Confirm.GetStateDown(SteamVR_Input_Sources.Any)) 
                {
                    symbolList.Add(copySymbol);
                    nowState = State.Inactive;
                    assitRotation.SetActive(false);
                    if (rotState)
                    {
                        mirror_MyController.CmdUpdateRotationInfo(new SymbolInfo()
                        {
                            up = hitInfo.normal,
                            position = copySymbol.transform.position
                        }); 
                    }
                    else
                    {
                        mirror_MyController.CmdUpdatePressInfo(new SymbolInfo()
                        {
                            up = hitInfo.normal,
                            position = copySymbol.transform.position
                        });
                    }

                }
            }
        }        
    }

    private void ActivateRotPlacement() {
        nowState = State.SelectPosition;
        assitRotation.SetActive(true);
        symbol = RotateSymbol;
        rotState = true;
    }

    private void ActivatePressPlacement() {
        nowState = State.SelectPosition;
        assitRotation.SetActive(true);
        symbol = PressSymbol;
        rotState = false;
    }

    private GameObject CreateNewLine(string objName)
    {
        GameObject lineObj = new GameObject(objName);
        lineObj.transform.SetParent(this.transform);
        LineRenderer lineRender = lineObj.AddComponent<LineRenderer>();
        lineRender.material = lineMaterial;
  
        lineRender.startWidth = lineThickness;
        lineRender.endWidth = lineThickness;
        return lineObj;
    }
}
