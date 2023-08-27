using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestGlobalUtils : MonoBehaviour
{
    public Camera depthCamera;
    public int unvisible_count;     // 用于多少个点被遮挡认为物体不可见
    private TestDepthDPC GetDepthScript;
    private TestColor GetColorScript;

    // 辅助碰撞
    public GameObject assistColliderSpherePrefab;
    private GameObject assistColliderSphere;
    // 根据顶点生成物体
    public GameObject splitPrefab;
    // public Material splitMaterial;

    public int[,] Cnk;

    void Awake()
    {
        depthCamera = GameObject.Find("DepthCamera").GetComponent<Camera>();
        GetDepthScript = GameObject.Find("DepthCamera").GetComponent<TestDepthDPC>();
        GetColorScript = GameObject.Find("ColorCamera").GetComponent<TestColor>();

        assistColliderSphere = Instantiate(assistColliderSpherePrefab);
        assistColliderSphere.layer = LayerMask.NameToLayer("DepthCameraUnivisible");
        assistColliderSphere.SetActive(false);
    }

    private void Start()
    {
        InitCnk();
    }

    private void Update()
    {
    }

    private void InitCnk(int maxn = 30)
    {
        Cnk = new int[maxn, maxn];
        for (int i = 0; i < maxn; ++i)
        {
            for (int j = 0; j < maxn; ++j)
            {
                if (j > i) Cnk[i, j] = 0;
                else if (j == i) Cnk[i, j] = 1;
                else if (j == 0) Cnk[i, j] = 1;
                else Cnk[i, j] = Cnk[i - 1, j] + Cnk[i - 1, j - 1];
            }
        }
    }

    public float GetDepth(int x, int y) => GetDepthScript.GetDepth(x, y);

    public float GetMinDepth(Vector3 p)
    {
        Vector3 screenP = MWorldToScreenPointDepth(p);
        if (screenP.x < 0 || screenP.x > Screen.width || screenP.y < 0 || screenP.y > Screen.height)
        {
            return float.MaxValue;
        }
            
        float minDepth = GetDepth((int)screenP.x, (int)screenP.y);
        return minDepth;
    }

    public float AverageSmooth(int x, int y)
    {
        float[] depth_around = { 1.0f, 1.0f, 1.0f, 1.0f, 1.0f };
        float[] weight_around = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };

        depth_around[4] = GetDepth(x, y);
        weight_around[4] = 0.125f;
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
    }   // new 

    public float MediaSmooth(int x, int y)
    {
        List<float> depth = new List<float>();
        depth.Add(GetDepth(x, y));
        if (depth[0] == 1.0f) return 1.0f;

        if (x > 0)  // left
        {
            depth.Add(GetDepth(x - 1, y));
        }

        if (y > 0)  // bottom
        {
            depth.Add(GetDepth(x, y - 1));
        }

        if (x < Screen.width)   // right
        {
            depth.Add(GetDepth(x + 1, y));
        }

        if (y < Screen.height)   // top
        {
            depth.Add(GetDepth(x, y + 1));
        }
        depth.Sort();
        return depth[depth.Count / 2];
    }   // new 

    public float GetSmoothDepth(int x, int y)
    {
        // return MediaSmooth(x, y);
        // return AverageSmooth(x, y);
        return GetDepth(x, y);
    }   // new 

    public Vector3 GetNormal(int x, int y, Vector3 center)
    {
        float d = 1.0f;
        Vector3 left = center;
        Vector3 right = center;
        Vector3 bottom = center;
        Vector3 top = center;
        if (x > 0)
        {
            d = GetSmoothDepth(x - 1, y);
            if (d != 1.0f)
            {
                left = MScreenToWorldPointDepth(new Vector3(x - 1, y, d));
            }    
        }
        if (y > 0)
        {
            d = GetSmoothDepth(x, y - 1);
            if (d != 1.0f)
            {
                bottom = MScreenToWorldPointDepth(new Vector3(x, y - 1, d));
            }
        }
        if (x < Screen.width)
        {
            d = GetSmoothDepth(x + 1, y);
            if (d != 1.0f)
            {
                right = MScreenToWorldPointDepth(new Vector3(x + 1, y, d));
            }
        }
        if (y < Screen.height)
        {
            d = GetSmoothDepth(x, y + 1);
            if (d != 1.0f)
            {
                top = MScreenToWorldPointDepth(new Vector3(x, y + 1, d));
            }
        }
        Vector3 left_to_right = Vector3.Normalize(right - left);
        Vector3 bottom_to_top = Vector3.Normalize(top - bottom);
        // Debug.LogFormat("right {0} left {1} bottom {2} top {3} left_to_right {4} bottom_to_top {5}", right.ToString("f4"), left.ToString("f4"),
        //     top.ToString("f4"), bottom.ToString("f4"), left_to_right.ToString("f4"), bottom_to_top.ToString("f4"));
        return Vector3.Normalize(Vector3.Cross(bottom_to_top, left_to_right));  // unity是左手
    }   // new 

    public Color GetColor(int x, int y) => GetColorScript.GetColor(x, y);

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
            return false;
        float minDepth = GetDepthScript.GetDepth((int)screenP.x, (int)screenP.y);

        return minDepth > screenP.z;
    }

    public bool OutOfRange(Vector3 p)
    {
        Vector3 screenP = MWorldToScreenPointDepth(p);
        // Debug.LogFormat("screenP: {0}", screenP.ToString("f4"));
        return (screenP.x < 0 || screenP.x > Screen.width || screenP.y < 0 || screenP.y > Screen.height);
    }   // new

    public bool Overlap(Vector3 p)      // new 
    {
        Vector3 screenP = MWorldToScreenPointDepth(p);
        if (screenP.x < 0 || screenP.x > Screen.width || screenP.y < 0 || screenP.y > Screen.height)
            return false;
        float minDepth = GetDepthScript.GetDepth((int)screenP.x, (int)screenP.y);
        return (minDepth != 1.0f);
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
        int unvisible = 0;
        /*foreach (var v in vAABB)
        {
            if (!GetPointVisibility(v))
            {
                return false;
            }
        }
        return true;*/
        /*foreach (var v in vAABB)
        {
            if (GetPointVisibility(v))
            {
                return true;
            }
        }
        return false;*/
        foreach (var v in vAABB)
        {
            if (!GetPointVisibility(v))
            {
                ++unvisible;
            }
        }
        return (unvisible <= unvisible_count);
    }

    public Vector3 GetCollisionPoint()
    {
        Ray ray = depthCamera.ScreenPointToRay(Input.mousePosition);
        return GetCollisionPoint(ray);
    }

    public Vector3 GetCollisionPoint(Ray ray)
    {
        assistColliderSphere.SetActive(true);

        //TODO
        int MAXSTEP = 1000, stepCount = 0;
        float step = 0.01f;
        assistColliderSphere.transform.position = ray.origin;
        while (GameObjectVisible(assistColliderSphere))
        {
            assistColliderSphere.transform.position += step * ray.direction;
            stepCount++;
            if (stepCount > MAXSTEP) break;
        }

        assistColliderSphere.SetActive(false);

        return (assistColliderSphere.transform.position - 2 * step * ray.direction);
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
        /*GameObject split_target = Instantiate(splitPrefab);
        split_target.name = "SplitTarget";

        Vector3[] vertices2 = { new Vector3(0, 0, 0), new Vector3(0, 2, 0), new Vector3(2, 2, 0), new Vector3(2, 0, 0) };
        Color[] colors2 = { new Color(0.21763f, 0, 0, 1), new Color(0.21763f, 0, 0, 1), new Color(0.21763f, 0, 0, 1), new Color(0.21763f, 0, 0, 1) };
        Vector2[] uv2 = { new Vector3(0, 0), new Vector3(0, 1), new Vector3(1, 1), new Vector3(1, 0) };
        int[] indices = { 0, 1, 2, 0, 2, 3 };


        Mesh m = new Mesh();
        m.SetVertices(vertices2);
        m.SetColors(colors2);
        m.SetIndices(indices, MeshTopology.Triangles, 0);
        split_target.GetComponent<MeshFilter>().mesh = m;
        return split_target;*/

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

        /* submesh解决不掉
         * GameObject split_target = Instantiate(splitPrefab);
        split_target.name = "SplitTarget";

        int submesh_count = vertices.Count / 50000 + 1, last_submesh_point_count = vertices.Count % 50000;
        // material
        Material[] materials = new Material[submesh_count];
        for (int i = 0; i < submesh_count; ++i) materials[i] = splitMaterial;
        split_target.GetComponent<MeshRenderer>().materials = materials;
        // submesh ind
        var indices = new List<List<int>>(0);
        int current_index = 0;
        for (int i = 0; i < submesh_count; ++i)
        {
            indices.Add(new List<int>());

            int icount = 50000;
            if (i == submesh_count - 1) icount = last_submesh_point_count;

            for (int j = 0; j < icount; ++j) indices[i].Add(current_index++);
        }

        Mesh m = new Mesh();
        m.SetVertices(vertices);
        m.SetColors(colors);
        m.subMeshCount = submesh_count;
        for (int i = 0; i < indices.Count; ++i)
        {
            m.SetIndices(indices[i], MeshTopology.Points, i, false);
        }
        split_target.GetComponent<MeshFilter>().mesh = m;
        */
    }
}
