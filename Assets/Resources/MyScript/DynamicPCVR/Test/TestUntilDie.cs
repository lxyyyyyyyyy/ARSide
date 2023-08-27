using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

public class TestUntilDie : MonoBehaviour
{
    public GameObject RotateSymbolPrefab;
    public GameObject PressSymbolPrefab;
    public GameObject assitRotateSpherePrefab;
    public GameObject axesPrefab;
    private GameObject assitRotateSphere;

    private TestExp GetExpStateScript;
    private TestGlobalUtils globalUtils;
    private TestMirror mirrorController;

    private int currentLineIndex = 0, 
        currentPressIndex = 0, 
        currentRotateIndex = 0;

    private enum State
    {
        Inactive = 0, SelectPosition, SelectRotation, SelectP1, SelectP2, SelectSplitPoint, SelectAxesPoint, SelectAnotherAxesPoint,
    };
    private State nowState = 0;

    // Button
    private Button PlaceRotButton, PlacePressButton, LineButton, SplitButton, AxesButton, DeleteButton;
    // press and rotate
    private bool press;
    private GameObject currentOperateSymbol;
    // line
    private Vector3 p1, p2;
    // segment
    public GameObject splitPointVisiblePrefab;
    private List<GameObject> splitPointVisble;
    private List<Vector3> splitPoints;

    // Start is called before the first frame update
    void Start()
    {
        assitRotateSphere = Instantiate(assitRotateSpherePrefab);
        assitRotateSphere.layer = LayerMask.NameToLayer("AssitRotateSphere");
        assitRotateSphere.SetActive(false);

        PlaceRotButton = GameObject.Find("TestObj/Canvas/Rotate").GetComponent<Button>();
        PlaceRotButton.onClick.AddListener(ActivateRotPlacement);

        PlacePressButton = GameObject.Find("TestObj/Canvas/Press").GetComponent<Button>();
        PlacePressButton.onClick.AddListener(ActivatePressPlacement);

        LineButton = GameObject.Find("TestObj/Canvas/Line").GetComponent<Button>();
        LineButton.onClick.AddListener(ActivateLine);

        SplitButton = GameObject.Find("TestObj/Canvas/Split").GetComponent<Button>();
        SplitButton.onClick.AddListener(ActivateSplit);

        AxesButton = GameObject.Find("TestObj/Canvas/Axes").GetComponent<Button>();
        AxesButton.onClick.AddListener(ActivateAxes);

        globalUtils = GameObject.Find("Script").GetComponent<TestGlobalUtils>();
        mirrorController = GameObject.Find("Script").GetComponent<TestMirror>();
        GetExpStateScript = GameObject.Find("ExpObj").GetComponent<TestExp>();

        DeleteButton = GameObject.Find("TestObj/Canvas/Delete").GetComponent<Button>();
        DeleteButton.onClick.AddListener(ActivateDelete);

        splitPoints = new List<Vector3>();
        splitPointVisble = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {

        if (nowState == State.Inactive) return;

        if (nowState == State.SelectPosition) DealSelectPositionState();
        else if (nowState == State.SelectRotation) DealSelectRotationState();
        else if (nowState == State.SelectP1) DealSelectLineP1State();
        else if (nowState == State.SelectP2) DealSelectLineP2State();
        else if (nowState == State.SelectSplitPoint) DealSelectSplitPointState();
        else if (nowState == State.SelectAxesPoint) DealPlaceAxesState();
        else if (nowState == State.SelectAnotherAxesPoint) DealPlaceAnotherAxesState();
    }

    private void ActivateRotPlacement()
    {
        Debug.Log("rotate");
        if (GetExpStateScript.manualDraw)
        {
            GetExpStateScript.ManualBegin();
        }
        nowState = State.SelectPosition;
        press = false;
    }

    private void ActivatePressPlacement()
    {
        Debug.Log("press");
        if (GetExpStateScript.manualDraw)
        {
            GetExpStateScript.ManualBegin();
        }
        nowState = State.SelectPosition;
        press = true;
    }

    private void ActivateLine()
    {
        Debug.Log("line");
        if (GetExpStateScript.manualDraw)
        {
            GetExpStateScript.ManualBegin();
        }
        nowState = State.SelectP1;
    }

    private void ActivateSplit()
    {
        Debug.Log("split");
        if (GetExpStateScript.manualDraw)
        {
            GetExpStateScript.ManualBegin();
        }
        nowState = State.SelectSplitPoint;
    }

    private void ActivateAxes()
    {
        Debug.Log("axes");
        if (GetExpStateScript.manualDraw)
        {
            GetExpStateScript.ManualBegin();
        }
        nowState = State.SelectAxesPoint;
    }

    private void DealSelectPositionState()
    {
        assitRotateSphere.transform.position = globalUtils.GetCollisionPoint();
        assitRotateSphere.SetActive(true);

        if (Input.GetMouseButtonDown(0))
        {
            if (press) currentOperateSymbol = Instantiate(PressSymbolPrefab);
            else currentOperateSymbol = Instantiate(RotateSymbolPrefab);

            nowState = State.SelectRotation;
        }
    }

    private void DealSelectRotationState()
    {
        // Ïò AssitRotateSphere send Ò»Ìõ ray
        int assitSphereLayer = LayerMask.NameToLayer("AssitRotateSphere");
        int onlyCastAssitSphere = 1 << (assitSphereLayer);
        Ray t = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hitInfo;
        if (!Physics.Raycast(t, out hitInfo, Mathf.Infinity, onlyCastAssitSphere)) return;

        if (press) DealPressSymbolRot(hitInfo);
        else DealRotateSymbolRot(hitInfo); 
    }

    private void DealPressSymbolRot(RaycastHit hitInfo)
    {
        // currentOperateSymbol.transform.position = hitInfo.point + 0.05f * hitInfo.normal;
        currentOperateSymbol.transform.position = hitInfo.point;
        currentOperateSymbol.transform.right = hitInfo.normal;

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Press symbol position: " + hitInfo.point.ToString("f4"));
            Debug.Log("Press symbol normal: " + hitInfo.normal.ToString("f4"));

            if (GetExpStateScript.manualDraw)
            {
                currentPressIndex = 0;
                GetExpStateScript.ManualEnd();
            }

            mirrorController.syncPressList.Add(new DPCSymbol()
            {
                index = currentPressIndex++,
                position = hitInfo.point,
                up = hitInfo.normal
            });

            DealSelectRotStateEnd();
        }
    }

    private void DealRotateSymbolRot(RaycastHit hitInfo)
    {
        currentOperateSymbol.transform.position = hitInfo.point;
        currentOperateSymbol.transform.forward = hitInfo.normal;

        if (GetExpStateScript.manualDraw)
        {
            currentRotateIndex = 0;
            GetExpStateScript.ManualEnd();
        }

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Rotate symbol position: " + hitInfo.point.ToString("f4"));
            Debug.Log("Rotate symbol normal: " + hitInfo.normal.ToString("f4"));

            mirrorController.syncRotationList.Add(new DPCSymbol()
            {
                index = currentRotateIndex++,
                position = hitInfo.point,
                up = hitInfo.normal
            });

            DealSelectRotStateEnd();
        }
    }

    private void DealSelectRotStateEnd()
    {
        Destroy(currentOperateSymbol);
        nowState = State.Inactive;
        assitRotateSphere.SetActive(false);
    }

    private void DealSelectLineP1State()
    {
        if (Input.GetMouseButtonDown(0))
        {
            p1 = globalUtils.GetCollisionPoint();
            nowState = State.SelectP2;
            // Debug.Log("p1 " + p1.ToString("f4"));
        }
    }

    private void DealSelectLineP2State()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (GetExpStateScript.manualDraw)
            {
                // currentLineIndex = 0;
                //GetExpStateScript.ManualEnd();
            }

            mirrorController.syncArrowList.Add(new DPCArrow()
            {
                index = currentLineIndex++,
                startPoint = p1,
                endPoint = globalUtils.GetCollisionPoint(),
                curvePointList = new List<Vector3[]>(),
                originPointList = new List<Vector3[]>(),
            });
            // GameObject.Find("Script").GetComponent<TestNewScript>().TestBezier(p1, globalUtils.GetCollisionPoint());

            nowState = State.Inactive;
            // Debug.Log("p2 " + p2.ToString("f4"));
        }
    }

    private void ActivateDelete()
    {

        if (mirrorController.syncArrowList.Count == 0) return;
        currentLineIndex--;
        mirrorController.syncArrowList.RemoveAt(mirrorController.syncArrowList.Count - 1);
    }

    private void DealSelectSplitPointState()
    {
        if (Input.GetMouseButtonDown(0)) {
            Vector3 p = globalUtils.GetCollisionPoint();
            splitPoints.Add(p);

            GameObject t = Instantiate(splitPointVisiblePrefab);
            t.transform.position = p;
            splitPointVisble.Add(t);

            // Debug.Log(globalUtils.MWorldToScreenPointDepth(p).ToString("f4"));
        }       

        float dis = float.MaxValue;
        if (splitPoints.Count > 3) {
            dis = Vector3.Distance(splitPoints[0], splitPoints[splitPoints.Count - 1]);
        }

        if (dis < 0.04)
        {
            splitPoints.RemoveAt(splitPoints.Count - 1);
            splitPoints.Add(splitPoints[0]);
            DealSplitRegion();
            nowState = State.Inactive;
        }
    }

    private void DealSplitRegion()
    {
        /*Debug.Log(splitPoints.Count);

        GameObject line = globalUtils.CreateNewLine("SplitBoundary");
        line.GetComponent<LineRenderer>().positionCount = splitPoints.Count;
        line.GetComponent<LineRenderer>().SetPositions(splitPoints.ToArray());*/

        GameObject.Find("Script").GetComponent<TestSplit>().SplitCPU(splitPoints);

        splitPoints.Clear();
        foreach(GameObject g in splitPointVisble) Destroy(g);
        splitPointVisble.Clear();
    }

    private void DealPlaceAxesState()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 p = globalUtils.GetCollisionPoint();
            splitPoints.Add(p);

            GameObject t = Instantiate(axesPrefab);
            t.transform.position = p;

            nowState = State.SelectAnotherAxesPoint;
        }
    }

    private void DealPlaceAnotherAxesState()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 p = globalUtils.GetCollisionPoint();
            splitPoints.Add(p);

            GameObject t = Instantiate(axesPrefab);
            t.transform.position = p;

            nowState = State.Inactive;
        }
    }
}
