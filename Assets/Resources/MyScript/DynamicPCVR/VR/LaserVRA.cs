using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.Extras;

public class LaserVRA : MonoBehaviour
{
    public GameObject Hand;
    public GameObject Target;
    public GameObject Laser;

    public SteamVR_Action_Boolean switchSymbolMode;
    public SteamVR_Action_Boolean confirmSelection;
    public SteamVR_Action_Boolean deleteLastSymbol;
    public SteamVR_Action_Boolean manipulate;

    public Material m;

    private Vector3[] laser_vertices = new Vector3[2];

    // Start is called before the first frame update
    void Start()
    {
        Hand = GetComponent<Transform>().gameObject;
        Laser = CreateLaser();
        Laser.GetComponent<LineRenderer>().positionCount = 2;
    }

    // Update is called once per frame
    void Update()
    {
        laser_vertices[0] = Hand.transform.position;
        if (Target) laser_vertices[1] = Target.transform.position;
        else laser_vertices[1] = laser_vertices[0] + 100 * Hand.transform.forward;
        Laser.GetComponent<LineRenderer>().SetPositions(laser_vertices);


        bool press = switchSymbolMode.GetState(SteamVR_Input_Sources.Any) ||
            confirmSelection.GetState(SteamVR_Input_Sources.Any) ||
            deleteLastSymbol.GetState(SteamVR_Input_Sources.Any) ||
            manipulate.GetState(SteamVR_Input_Sources.Any);

        if (press) {
            Laser.GetComponent<LineRenderer>().startColor = Color.green;
            Laser.GetComponent<LineRenderer>().endColor = Color.green;
        } 
        else
        {
            Laser.GetComponent<LineRenderer>().startColor = Color.black;
            Laser.GetComponent<LineRenderer>().endColor = Color.black;
        }
        
    }

    public void SetTarget(GameObject t) => Target = t;

    public void UnsetTarget() => Target = null;

    private GameObject CreateLaser()
    {
        GameObject lineObj = new GameObject("Laser");
        lineObj.transform.SetParent(this.transform);
        LineRenderer curveRender = lineObj.AddComponent<LineRenderer>();
        lineObj.layer = LayerMask.NameToLayer("DepthCameraUnivisible"); ;
        curveRender.material = m;

        curveRender.startWidth = 0.005f;
        curveRender.endWidth = 0.005f;

        return lineObj;
    }
}
