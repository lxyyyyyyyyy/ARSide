using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestNewScript : MonoBehaviour
{
    public GameObject sphere;
    public Vector3 delta_p;
    public float proportion;

    private bool begin = false;
    private Vector3 p0, p2;
    private GameObject line;
    private GameObject t, t2;

    public TestGlobalUtils globalUtils;

    // Start is called before the first frame update
    void Start()
    {
        globalUtils = GameObject.Find("Script").GetComponent<TestGlobalUtils>();
    }

    // Update is called once per frame
    void Update()
    {
        if (begin) TestBezier(p0, p2);
    }

    /*
     * p0 是一个已知控制点
     * p2 是另一个已知控制点
     * p 是经过Bezier曲线上的一点
     * 返回值是另一个控制点，位于p0,p2确定的线段的中垂面
     */
    Vector3 Bezier(Vector3 p0, Vector3 p2, Vector3 p)
    {
        float t = 1 - ((p.x - p0.x) / (p2.x - p0.x));
        if (Vector3.Distance(p0 * t + p2 * (1 - t), p) < 0.0001f) return (p0 + p2) / 2;
        
        Vector4 coef = new Vector4(Mathf.Pow(t, 2), 2 * t * (1 - t), Mathf.Pow(1 - t, 2), 1.0f);

        Vector3 p0p = p - p0;
        Vector3 p2p = p - p2;
        Vector3 p0p2 = p2 - p0;
        Vector3 vertical_to_plane = Vector3.Cross(p0p, p2p);
        Vector3 vertical_to_line = Vector3.Normalize(Vector3.Cross(vertical_to_plane, p0p2));
        Debug.Log(vertical_to_line.magnitude);
        Vector3 p_mid = (p0 + p2) / 2;
        Vector3 delta = p - coef.x * p0 - coef.y * p_mid - coef.z * p2;
        float k = Mathf.Max(delta.x / (coef.y * vertical_to_line.x),
            delta.y / (coef.y * vertical_to_line.y),
            delta.z / (coef.y * vertical_to_line.z));

        return p_mid + k * vertical_to_line;
    }

    float Bezierxxx(Vector3 p0, Vector3 p2, Vector3 p)
    {
        Vector3 p1 = (p0 + p2) / 2;
        Vector4 col1 = new Vector4(p0.x - 2 * p1.x + p2.x, 2 * p1.x - 2 * p2.x, p2.x, 0);
        Vector4 col2 = new Vector4(p0.y - 2 * p1.y + p2.y, 2 * p1.y - 2 * p2.y, p2.y, 0);
        Vector4 col3 = new Vector4(p0.z - 2 * p1.z + p2.z, 2 * p1.z - 2 * p2.z, p2.z, 0);
        Vector4 col4 = new Vector4(0, 0, 0, 1);
        Matrix4x4 A = new Matrix4x4(col1, col2, col3, col4);

        float y = (p2.y - p0.y) * ((p.x - p0.x) / (p2.x - p0.x)) + p0.y;    // p在直线上的y
        if (Mathf.Abs(y - p.y) < 0.0001f) return p1.y;

        Vector4 res = new Vector4(p.x, y, p.z, 1);
        float t = (Matrix4x4.Transpose(Matrix4x4.Inverse(A)) * res).y;
        Vector4 coef = new Vector4(Mathf.Pow(t, 2), 2 * t * (1 - t), Mathf.Pow(1 - t, 2), 1.0f);

        return (p.y - coef.x * p0.y - coef.z * p2.y) / coef.y;
    }

    public void TestBezier(Vector3 p0, Vector3 p2)
    {
        Debug.Log("start");

        if (!begin)
        {
            begin = true;
            this.p0 = p0;
            this.p2 = p2;
            line = globalUtils.CreateNewLine("test");
            t = Instantiate(sphere);
            t2 = Instantiate(sphere);
        }

        Vector3 p = proportion * p0 + (1 - proportion) * p2;
        p += delta_p;
        Vector3 p1 = Bezier(p0, p2, p);

        t.transform.position = p;
        t2.transform.position = p1;

        DrawBezierCurve(new List<Vector3>() { p0, p1, p2 });
    }

    private void DrawBezierCurve(List<Vector3> controlPoints)
    {
        int n = controlPoints.Count - 1;
        int BezierSampleCount = 30;

        List<Vector3> p_list = new List<Vector3>();
        float t = 0, dt = 1.0f / BezierSampleCount;
        for (int j = 0; j < BezierSampleCount; ++j)
        {
            Vector3 np = new Vector3(0.0f, 0.0f, 0.0f);
            for (int i = 0; i <= n; ++i)
            {
                float coef = (float)Mathf.Pow(t, i) * (float)Mathf.Pow(1 - t, n - i) * globalUtils.Cnk[n, i];
                np += coef * controlPoints[i];
            }

            p_list.Add(np);
            t += dt;
        }

        line.GetComponent<LineRenderer>().positionCount = p_list.Count;
        line.GetComponent<LineRenderer>().SetPositions(p_list.ToArray());
    }

}
