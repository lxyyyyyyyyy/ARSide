using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalUtilsVR : MonoBehaviour
{
    private GameObject rightHand;
    public GameObject DepthCameraObject;
    public Camera depthCamera;
    private DepthDPC getDepthScript;
    private ManipulateVRA manipulateScript;

    // 辅助碰撞
    public GameObject assistColliderSpherePrefab;
    private GameObject assistColliderSphere;
    // 根据顶点生成物体
    public GameObject splitPrefab;

    void Awake()
    {
        depthCamera = DepthCameraObject.GetComponent<Camera>();
        getDepthScript = DepthCameraObject.GetComponent<DepthDPC>();
        manipulateScript = GetComponent<ManipulateVRA>();

        assistColliderSphere = Instantiate(assistColliderSpherePrefab);
        assistColliderSphere.layer = LayerMask.NameToLayer("DepthCameraUnivisible");
        assistColliderSphere.SetActive(false);

        rightHand = GameObject.Find("[CameraRig]/Controller (right)");
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        if (depthCamera) { return; }
    }

    public float GetDepth(int x, int y) => getDepthScript.GetDepth(x, y);

    public float GetSmoothDepth(int x, int y)
    {
        float[] depth_around = { 1.0f, 1.0f, 1.0f, 1.0f, 1.0f };
        float[] weight_around = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };

        depth_around[4] = GetDepth(x, y);
        weight_around[4] = 0.5f;
        if (depth_around[4] == 1.0f) return 1.0f;

        if (x > 0)  // left
        {
            depth_around[0] = GetDepth(x - 1, y);
            weight_around[0] = 0.125f;
        }

        if (y > 0)  // bottom
        {
            depth_around[1] = GetDepth(x, y - 1);
            weight_around[1] = 0.125f;
        }

        if (x < Screen.width)   // right
        {
            depth_around[2] = GetDepth(x + 1, y);
            weight_around[2] = 0.125f;
        }

        if (y < Screen.height)   // top
        {
            depth_around[3] = GetDepth(x, y + 1);
            weight_around[3] = 0.125f;
        }

        // 权重归一化
        float total = 0.0f;
        for (int i = 0; i < 5; ++i)
        {
            total += weight_around[i];
        }
        for (int i = 0; i < 5; ++i)
        {
            weight_around[i] /= total;
        }

        float smooth_depth = 0.0f;
        for (int i = 0; i < 5; ++i)
        {
            smooth_depth += weight_around[i] * depth_around[i];
        }

        return smooth_depth;
    }


    public Color GetColor(int x, int y) => getDepthScript.GetColor(x, y);

    public Vector3 MScreenToWorldPointDepth(Vector3 p)
    {
        p.z *= depthCamera.farClipPlane;
        return depthCamera.ScreenToWorldPoint(p);
    }

    public Vector3 MWorldToScreenPointDepth(Vector3 p)
    {
        Vector3 screenP = depthCamera.WorldToScreenPoint(p);
        screenP.z = screenP.z / depthCamera.farClipPlane;
        return screenP;
    }

    public bool GetPointVisibility(Vector3 p)
    {
        Vector3 screenP = MWorldToScreenPointDepth(p);
        if (screenP.x < 0 || screenP.x > Screen.width || screenP.y < 0 || screenP.y > Screen.height)
            return true;
        float minDepth = getDepthScript.GetDepth((int)screenP.x, (int)screenP.y);

        return minDepth > screenP.z;
    }

    public bool GameObjectVisible(GameObject t)
    {
        Bounds tAABB;
        if (t.GetComponentsInChildren<Transform>(true).Length > 1)
        {
            tAABB = t.transform.GetChild(0).GetComponent<MeshRenderer>().bounds;
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

    public Vector3 GetCollisionPoint()
    {
        int MAXSTEP = 1000, stepCount = 0;
        float step = 0.01f;
        assistColliderSphere.transform.position = rightHand.transform.position;
        while (GameObjectVisible(assistColliderSphere))
        {
            assistColliderSphere.transform.position += step * rightHand.transform.forward;
            stepCount++;
            if (stepCount > MAXSTEP) break;
        }

        return (assistColliderSphere.transform.position - 3 * step * rightHand.transform.forward);
    }

    public GameObject CreateNewLine(string objName)
    {
        GameObject lineObj = new GameObject(objName);
        lineObj.transform.SetParent(this.transform);
        LineRenderer curveRender = lineObj.AddComponent<LineRenderer>();

        lineObj.layer = LayerMask.NameToLayer("DepthCameraUnivisible"); ;

        curveRender.startWidth = 0.002f;
        curveRender.endWidth = 0.002f;

        return lineObj;
    }

    public GameObject CreateNewObjUsingVertices(ref List<Vector3> vertices, ref List<Color> colors, string name = "", Transform father = null)
    {

        GameObject split_target = Instantiate(splitPrefab, father);
        split_target.name = name;

        var indices = new int[vertices.Count];
        for (int i = 0; i < vertices.Count; i++)
            indices[i] = i;

        Mesh m = new Mesh();
        m.SetVertices(vertices);
        m.SetColors(colors);
        m.SetIndices(indices, MeshTopology.Points, 0, false);
        split_target.GetComponent<MeshFilter>().mesh = m;

        return split_target;
    }

    public void SetManipulateObj(GameObject tar) => manipulateScript.RegisterObj(tar);

    public void RestManipulateObj() => manipulateScript.UnRegisterObj();
}
