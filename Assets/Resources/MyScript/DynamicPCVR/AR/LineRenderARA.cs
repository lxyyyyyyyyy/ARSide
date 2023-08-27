using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineRenderARA : MonoBehaviour
{
    private MirrorControllerA mirrorController; // ÍøÂçÖÐ¿Ø
    private List<GameObject> lines;
    private int line_index;

    public Material straightLineMaterial;
    public float straightLineThickness;

    public bool origin_line = false;

    public GameObject SpherePrefab;
    private List<GameObject> startpointSphere;

    // Start is called before the first frame update
    void Start()
    {
        mirrorController = GetComponentInParent<MirrorControllerA>();
        lines = new List<GameObject>();
        startpointSphere = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var t in startpointSphere)
        {
            t.SetActive(false);
        }

        line_index = 0;
        for (int i = 0; i < mirrorController.syncArrowList.Count; ++i)
        {
            DPCArrow current_line = mirrorController.syncArrowList[i];
            DrawLine(ref current_line);
            DrawSphere(ref current_line, i);
        }
        ClearLine();
    }

    void DrawLine(ref DPCArrow current_line)
    {
        if (origin_line)
        {
            foreach (var t in current_line.originPointList)
            {
                if (line_index >= lines.Count)
                {
                    lines.Add(CreateNewLine("line" + line_index.ToString()));
                }

                lines[line_index].GetComponent<LineRenderer>().positionCount = t.Length;
                lines[line_index].GetComponent<LineRenderer>().SetPositions(t);
                ++line_index;
            }
        }
        else
        {
            foreach (var t in current_line.curvePointList)
            {
                if (line_index >= lines.Count)
                {
                    lines.Add(CreateNewLine("line" + line_index.ToString()));
                }

                lines[line_index].GetComponent<LineRenderer>().positionCount = t.Length;
                lines[line_index].GetComponent<LineRenderer>().SetPositions(t);
                ++line_index;

            }
        }

    }

    void DrawSphere(ref DPCArrow current_line, int sphere_index)
    {
        if (sphere_index >= startpointSphere.Count)
        {
            startpointSphere.Add(Instantiate(SpherePrefab, this.transform));
        }
        startpointSphere[sphere_index].transform.position = current_line.startPoint;
        startpointSphere[sphere_index].SetActive(current_line.startPointVisibility);
    }

    void ClearLine()
    {
        while (line_index < lines.Count)
        {
            lines[line_index++].GetComponent<LineRenderer>().positionCount = 0;
        }
    }

    private GameObject CreateNewLine(string objName)
    {
        GameObject lineObj = new GameObject(objName);
        lineObj.transform.SetParent(this.transform);
        LineRenderer curveRender = lineObj.AddComponent<LineRenderer>();
        // curveRender.material = new Material(Shader.Find("Sprites/Default"));
        lineObj.layer = LayerMask.NameToLayer("DepthCameraUnivisible"); ;

        curveRender.startWidth = straightLineThickness;
        curveRender.endWidth = straightLineThickness;

        return lineObj;
    }
}
