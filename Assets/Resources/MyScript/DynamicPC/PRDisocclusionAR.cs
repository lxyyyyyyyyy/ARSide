using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PRDisocclusionAR : MonoBehaviour
{
    public List<SymbolInfo> RotationSymbolInfoList;
    public List<SymbolInfo> PressSymbolInfoList;

    public GameObject RotateSymbol;
    public GameObject PressSymbol;

    private List<GameObject> RotateSymbolList;
    private List<GameObject> PressSymbolList;

    private MirrorController mirrorController;

    public int RotationSize, PressSize;

    private GlobalUtils globalUtils;

    void Awake()
    {
        RotateSymbolList = new List<GameObject>();
        PressSymbolList = new List<GameObject>();
        RotationSymbolInfoList = new List<SymbolInfo>();
        PressSymbolInfoList = new List<SymbolInfo>();
        mirrorController = GetComponentInParent<MirrorController>();

        globalUtils = GetComponentInParent<GlobalUtils>();
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
        RotationSymbolInfoList = mirrorController.clientRotationList;
        PressSymbolInfoList = mirrorController.clientPressList;

        int deltaRotateCount = RotationSymbolInfoList.Count - RotateSymbolList.Count;
        for (int rotateI = 0; rotateI < deltaRotateCount; ++rotateI)
        {
            GameObject t = GameObject.Instantiate(RotateSymbol);
            t.transform.position = RotationSymbolInfoList[RotateSymbolList.Count].position;
            t.transform.forward = RotationSymbolInfoList[RotateSymbolList.Count].up;
            RotateSymbolList.Add(t);
        }

        int deltaPressCount = PressSymbolInfoList.Count - PressSymbolList.Count;
        for (int rotateI = 0; rotateI < deltaPressCount; ++rotateI)
        {
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

        while (!globalUtils.GameObjectVisible(t))
        {
            t.transform.position += step * Vector3.up;
            // step *= 2;
        }
    }

}
