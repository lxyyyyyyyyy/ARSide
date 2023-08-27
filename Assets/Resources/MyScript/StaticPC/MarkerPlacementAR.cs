using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarkerPlacementAR : MonoBehaviour
{
    public List<SymbolInfo> RotationSymbolInfoList;
    public List<SymbolInfo> PressSymbolInfoList;

    public GameObject RotateSymbol;
    public GameObject PressSymbol;

    private List<GameObject> RotateSymbolList;
    private List<GameObject> PressSymbolList;

    private Mirror_MyController mirror_MyController;
    private Camera depthCamera;
    private Depth GetDepthScript;

    public int RotationSize, PressSize;

    void Awake() 
    {
        RotateSymbolList = new List<GameObject>();
        PressSymbolList = new List<GameObject>();
        RotationSymbolInfoList = new List<SymbolInfo>();
        PressSymbolInfoList = new List<SymbolInfo>();
        mirror_MyController = GetComponent<Mirror_MyController>();
        if (GameObject.Find("DepthCamera"))
        {
            depthCamera = GameObject.Find("DepthCamera").GetComponent<Camera>();
            GetDepthScript = GameObject.Find("DepthCamera").GetComponent<Depth>();
        }
        else
        {
            depthCamera = Camera.main;
            GetDepthScript = Camera.main.GetComponent<Depth>();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        RotationSize = RotationSymbolInfoList.Count;
        PressSize = PressSymbolInfoList.Count;


        if (GlobleInfo.ClientMode.Equals(CameraMode.VR)) { return; }
        RotationSymbolInfoList = mirror_MyController.clientRotationList;
        PressSymbolInfoList = mirror_MyController.clientPressList;

        int deltaRotateCount = RotationSymbolInfoList.Count - RotateSymbolList.Count;
        for (int rotateI = 0; rotateI < deltaRotateCount; ++rotateI) {
            GameObject t = GameObject.Instantiate(RotateSymbol);
            t.transform.position = RotationSymbolInfoList[RotateSymbolList.Count].position;
            t.transform.forward = RotationSymbolInfoList[RotateSymbolList.Count].up;
            RotateSymbolList.Add(t);
        }

        int deltaPressCount = PressSymbolInfoList.Count - PressSymbolList.Count;
        for (int rotateI = 0; rotateI < deltaPressCount; ++rotateI) {
            GameObject t = GameObject.Instantiate(PressSymbol);
            t.transform.position = PressSymbolInfoList[PressSymbolList.Count].position;
            t.transform.right = PressSymbolInfoList[PressSymbolList.Count].up;  // press is right
            PressSymbolList.Add(t);
        }

        for (int i = 0; i < RotateSymbolList.Count; i++)
        {
            RotateSymbolList[i].transform.position = RotationSymbolInfoList[i].position;
            RaiseSymbol(RotateSymbolList[i]);
        }

        for (int i = 0; i < PressSymbolList.Count; i++)
        {
            PressSymbolList[i].transform.position = PressSymbolInfoList[i].position;
            RaiseSymbol(PressSymbolList[i]);
        }
    }

    private void RaiseSymbol(GameObject t)
    {
        float step = 0.1f;

        while (!GameObjectVisible(t))
        {
            t.transform.position += step * depthCamera.transform.up;
            // step *= 2;
        }
    }

    private Vector3 MWorldToScreenPointDepth(Vector3 p)
    {
        Vector3 screenP = depthCamera.WorldToScreenPoint(p);
        screenP.z = screenP.z / depthCamera.farClipPlane;
        return screenP;
    }

    private bool GetPointVisibility(Vector3 p)
    {
        Vector3 screenP = MWorldToScreenPointDepth(p);
        if (screenP.x < 0 || screenP.x > Screen.width || screenP.y < 0 || screenP.y > Screen.height)
            return true;
        float minDepth = GetDepthScript.depthTextureRead.GetPixel((int)screenP.x, (int)screenP.y).r;

        return minDepth > screenP.z;
    }

    private bool GameObjectVisible(GameObject t)
    {
        Bounds tAABB;
        var child = t.transform.GetChild(0).gameObject;
        if (child != null)
        {
            tAABB = child.GetComponent<MeshRenderer>().bounds;
        }
        else
        {
            tAABB = t.GetComponent<MeshRenderer>().bounds;
        }
        // var tAABB = t.GetComponent<MeshRenderer>().bounds;
        float x = tAABB.extents.x, y = tAABB.extents.y, z = tAABB.extents.z;
        float scale = 0.9f;
        Vector3[] vAABB = new Vector3[]{
            tAABB.center + scale * new Vector3( x,  y,  z),
            tAABB.center + scale * new Vector3( x,  y, -z),
            tAABB.center + scale * new Vector3( x, -y,  z),
            tAABB.center + scale * new Vector3( x, -y, -z),
            tAABB.center + scale * new Vector3(-x,  y,  z),
            tAABB.center + scale * new Vector3(-x,  y, -z),
            tAABB.center + scale * new Vector3(-x, -y,  z),
            tAABB.center + scale * new Vector3(-x, -y, -z)
        };
        foreach (var v in vAABB)
        {
            if (!GetPointVisibility(v))
            {
                return false;
            }
        }
        return true;
    }
}
