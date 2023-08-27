using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

public class RayDisocclusion : MonoBehaviour
{
    // public GameObject[] sam;

    public List<SegmentInfo> segments;
    private int segmentIndex;
    private Vector3 p1, p2;     // current segment begin point/ end point

    public int VisibilitySampleCount = 30, BezierSampleCount = 50, CatmullRomSampleCount = 50;
    public bool StarightLineCompleteConvered;
    private const int MaxBezierCurveCount = 5, MaxstraightLinesCount = 5;
    private const float DirectChangeDepthVal = 0.0002f;
    private int[,] Cnk;

    public Material straightLineMaterial;
    public float straightLineThickness = 0.01f;

    public List<bool[]> linesVisibility;
    public List<GameObject[]> straightLines;
    public List<LineRenderer[]> straightLinesRender;
    public List<GameObject[]> straightLinesCurve;
    public List<LineRenderer[]> straightLinesCurveRender;
    private List<GameObject[]> straightLinesArrow;
    private List<LineRenderer[]> straightLinesArrowRender;

    private Camera depthCamera;
    private Depth GetDepthScript;

    // qinwen code
    private Mirror_MyController mirrorMyController;

    // vis 
    public bool[] visibleVisibility;
    public Button changeButton;
    public float threshold = 0.0003f;
    private int interval = 0;

    // Start is called before the first frame update
    void Start()
    {
        segments = new List<SegmentInfo>(0);
        linesVisibility = new List<bool[]>(0);
        straightLines = new List<GameObject[]>(0);
        straightLinesRender = new List<LineRenderer[]>(0);
        straightLinesCurve = new List<GameObject[]>(0);
        straightLinesCurveRender = new List<LineRenderer[]>(0);
        straightLinesArrow = new List<GameObject[]>(0);
        straightLinesArrowRender = new List<LineRenderer[]>(0);

        InitCnk(VisibilitySampleCount);

        
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

        // qinwen code
        mirrorMyController = GetComponent<Mirror_MyController>();

        // visibleVisibility = new bool[0];
        // changeButton = GameObject.Find("TestObj/Canvas/Change").GetComponent<Button>();
        // changeButton.onClick.AddListener(changeDepthMap);
    }

    // Update is called once per frame
    void Update()
    {
        /*if (interval < 10)
        {
            ++interval;
            return;
        }
        interval = 0;

        if (GlobleInfo.ClientMode.Equals(CameraMode.VR)) { return; }
        segments = mirrorMyController.clientSegmentList;

        int delta = segments.Count - linesVisibility.Count;
        // 删除了线
        for (int i = 0; i > delta; --i)
        {
            DeleteNewSegment();
        }

        // 增加了线
        for (int i = 0; i < delta; ++i)
        {
            AddNewSegment(linesVisibility.Count);
        }

        // 清理场景
        ClearScene();

        foreach (var segment in segments)
        {
            visibleVisibility = linesVisibility[segmentIndex];

            p1 = segment.startPoint;
            p2 = segment.endPoint;

            DrawArrow();
            AdjustPointOrder();
            RaisestraightLineLogo();
            DrawVisiblestraightLine();
            detourToEndpoint();
            detourToUnvisiblePoint();

            segmentIndex++;
        }*/

    }

    private void ClearScene()
    {
        for (int j = 0; j < straightLinesCurveRender.Count; ++j)
        {

            for (int i = 0; i < MaxBezierCurveCount + 2; ++i)
            {
                straightLinesCurveRender[j][i].positionCount = 0;
            }
            for (int i = 0; i < MaxstraightLinesCount; ++i)
            {
                straightLinesRender[j][i].positionCount = 0;
            }
            for (int i = 0; i < 2; ++i)
            {
                straightLinesArrowRender[j][i].positionCount = 0;
            }
        }
        segmentIndex = 0;
    }

    private void DeleteNewSegment()
    {
        linesVisibility.RemoveAt(linesVisibility.Count - 1);

        GameObject[] tList = straightLinesCurve[straightLinesCurve.Count - 1];
        straightLinesCurve.RemoveAt(straightLinesCurve.Count - 1);
        foreach(var t in tList)
        {
            Destroy(t);
        }
        straightLinesCurveRender.RemoveAt(straightLinesCurveRender.Count - 1);

        tList = straightLines[straightLines.Count - 1];
        straightLines.RemoveAt(straightLines.Count - 1);
        foreach (var t in tList)
        {
            Destroy(t);
        }
        straightLinesRender.RemoveAt(straightLinesRender.Count - 1);

        tList = straightLinesArrow[straightLinesArrow.Count - 1];
        straightLinesArrow.RemoveAt(straightLinesArrow.Count - 1);
        foreach (var t in tList)
        {
            Destroy(t);
        }
        straightLinesArrowRender.RemoveAt(straightLinesArrowRender.Count - 1);
    }

    private void AddNewSegment(int index)
    {
        // linesVisibility
        linesVisibility.Add(new bool[VisibilitySampleCount]);

        // curve
        GameObject[] t_straightLineCurve = new GameObject[MaxBezierCurveCount + 2];
        LineRenderer[] t_straightLineCurveRender = new LineRenderer[MaxBezierCurveCount + 2];
        for (int i = 0; i < MaxBezierCurveCount + 2; i++)
        {
            t_straightLineCurve[i] = CreateNewLine("Segment_" + index.ToString() +
                "_StraightCurve_" + i.ToString());
            t_straightLineCurveRender[i] = t_straightLineCurve[i].GetComponent<LineRenderer>();
        }
        straightLinesCurve.Add(t_straightLineCurve);
        straightLinesCurveRender.Add(t_straightLineCurveRender);

        // straight
        GameObject[] t_straightLine = new GameObject[MaxstraightLinesCount];
        LineRenderer[] t_straightLineRender = new LineRenderer[MaxstraightLinesCount];
        for (int i = 0; i < MaxstraightLinesCount; i++)
        {
            t_straightLine[i] = CreateNewLine("Segment_" + index.ToString() +
                "_straightLines_" + i.ToString());
            t_straightLineRender[i] = t_straightLine[i].GetComponent<LineRenderer>();
        }
        straightLines.Add(t_straightLine);
        straightLinesRender.Add(t_straightLineRender);

        // arrow
        GameObject[] t_arrow = new GameObject[2];
        LineRenderer[] t_arrowRender = new LineRenderer[2];
        for (int i = 0; i < 2; i++)
        {
            t_arrow[i] = CreateNewLine("Segment_" + index.ToString() +
                "_arrow_" + i.ToString());
            t_arrowRender[i] = t_arrow[i].GetComponent<LineRenderer>();
        }
        straightLinesArrow.Add(t_arrow);
        straightLinesArrowRender.Add(t_arrowRender);
    }

    private void DrawArrow()
    {
        if (!GetPointVisibility(p2))
        {
            return;
        }
        Vector3 screenP1 = MWorldToScreenPointDepth(p1),
            screenP2 = MWorldToScreenPointDepth(p2);
        Vector2 dir = (screenP1 - screenP2).normalized;
        Vector2 verticalDir = new Vector2(-dir.y, dir.x);

        int length = 10;
        Vector3 screenArrowP1 = screenP2 + length * new Vector3(verticalDir.x, verticalDir.y) + length * new Vector3(dir.x, dir.y),
            screenArrowP2 = screenP2 - length * new Vector3(verticalDir.x, verticalDir.y) + length * new Vector3(dir.x, dir.y);

        Vector3 arrowP1 = MScreenToWorldPointDepth(screenArrowP1),
            arrowP2 = MScreenToWorldPointDepth(screenArrowP2);

        straightLinesArrowRender[segmentIndex][0].positionCount = 2;
        straightLinesArrowRender[segmentIndex][0].SetPosition(0, arrowP1);
        straightLinesArrowRender[segmentIndex][0].SetPosition(1, p2);

        straightLinesArrowRender[segmentIndex][1].positionCount = 2;
        straightLinesArrowRender[segmentIndex][1].SetPosition(0, arrowP2);
        straightLinesArrowRender[segmentIndex][1].SetPosition(1, p2);
    }

    private GameObject CreateNewLine(string objName)
    {
        GameObject lineObj = new GameObject(objName);
        lineObj.transform.SetParent(this.transform);
        LineRenderer curveRender = lineObj.AddComponent<LineRenderer>();
        curveRender.material = straightLineMaterial;

        curveRender.startWidth = straightLineThickness;
        curveRender.endWidth = straightLineThickness;
        return lineObj;
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

    /*
     * =========================
     * Translate -- straightLines
     */
    private Vector3 worldToScreenWithDepth(Vector3 p)
    {
        Vector4 wp = p;
        wp.w = 1.0f;

        Vector4 t = depthCamera.projectionMatrix * depthCamera.worldToCameraMatrix * wp;

        t /= t.w;
        t += new Vector4(1.0f, 1.0f, 1.0f, 0.0f);
        t /= 2.0f;      // ���ʱ��ZֵԽС����Խ������Ϊunity�������z������
        t.x *= Screen.width;
        t.y *= Screen.height;
        t.z = 1 - t.z;  // ����������ͺ���� _CameraDepthTexture ͼ�����ֵһ���� 

        return t;
    }

    private Vector3 MWorldToScreenPointDepth(Vector3 p)
    {
        Vector3 screenP = depthCamera.WorldToScreenPoint(p);
        screenP.z = screenP.z / depthCamera.farClipPlane;
        return screenP;
    }

    private Vector3 MScreenToWorldPointDepth(Vector3 p)
    {
        p.z *= depthCamera.farClipPlane;
        return depthCamera.ScreenToWorldPoint(p);
    }

    private bool inScreenRange(Vector3 v) =>
        0 <= v.x && v.x < Screen.width && 0 <= v.y && v.y < Screen.height && 0 <= v.z && v.z <= 1;

    private Vector3 scaleToVec(int i) =>
        p1 + (float)i / (VisibilitySampleCount - 1) * (p2 - p1);

    private void ChangePointDepth(ref Vector3 p, float changeDepth = DirectChangeDepthVal)
    {
        Vector3 screenP = MWorldToScreenPointDepth(p);
        float minDepth = GetDepthScript.depthTextureRead.GetPixel((int)screenP.x, (int)screenP.y).r;

        if (minDepth < screenP.z)
        {
            screenP.z = minDepth - changeDepth;
            p = MScreenToWorldPointDepth(screenP);
        }
    }

    private bool GetPointVisibility(Vector3 p)
    {
        Vector3 screenP = MWorldToScreenPointDepth(p);
        if (screenP.x < 0 || screenP.x > Screen.width || screenP.y < 0 || screenP.y > Screen.height)
            return true;
        float minDepth = GetDepthScript.depthTextureRead.GetPixel((int)screenP.x, (int)screenP.y).r;
        return minDepth > screenP.z;
    }

    private bool GetSamplePointsVisibility()
    {
        bool completeConvered = true, completeOutofView = true;
        for (int i = 0; i < VisibilitySampleCount; ++i)
        {
            Vector3 p = scaleToVec(i);
            Vector3 screenP = MWorldToScreenPointDepth(p);
            
            if (!inScreenRange(screenP))
            {
                linesVisibility[segmentIndex][i] = true;      // ������Ļ��Χֱ��true
                continue;
            }

            completeOutofView = false;
            float minDepth = GetDepthScript.depthTextureRead.GetPixel((int)screenP.x, (int)screenP.y).r;
            float testVisibleThreshold = 0.00001f; ;
            linesVisibility[segmentIndex][i] = (minDepth >= screenP.z);        // true�ܿ���
            if (completeConvered)  
            {
                completeConvered = !linesVisibility[segmentIndex][i];
            }
            // Debug.Log(interval + " " + screenP.x + " " + screenP.y + " " + screenP.z + ";;;;" + minDepth);    
        }
        if (completeOutofView)
        {
            completeConvered = false;
        }

        // for (int i = 1; i < VisibilitySampleCount - 1; ++i)
        // {
        //     if (!linesVisibility[i - 1] && linesVisibility[i] && !linesVisibility[i + 1])
        //     { 
        //         linesVisibility[i] = false;
        //     }
        // }

        return completeConvered;
    }

    private void DrawVisiblestraightLine()
    {
        int i = 0, lineIndex = 0;
        while (i < VisibilitySampleCount)
        {
            if (linesVisibility[segmentIndex][i])
            {
                straightLinesRender[segmentIndex][lineIndex].positionCount = 2;
                straightLinesRender[segmentIndex][lineIndex].SetPosition(0, scaleToVec(i));
                while (i < VisibilitySampleCount && linesVisibility[segmentIndex][i]) { i++; }
                straightLinesRender[segmentIndex][lineIndex].SetPosition(1, scaleToVec(i - 1));
                lineIndex++;
            }
            i++;
        }
    }

    private void DrawBezierCurve(ref List<Vector3> controlPoints, ref LineRenderer lineRenderer)
    {
        int n = controlPoints.Count - 1;
        // for (int i = 0; i <= n; ++i) {
        //     sam[i].transform.position = controlPoints[i];
        // }

        double t = 0, dt = 1.0 / BezierSampleCount;
        Vector3[] curvePoints = new Vector3[BezierSampleCount + 1];
        for (int j = 0; j <= BezierSampleCount; ++j)
        {
            Vector3 np = new Vector3(0.0f, 0.0f, 0.0f);
            for (int i = 0; i <= n; ++i)
            {
                float coef = (float)Math.Pow(t, i) * (float)Math.Pow(1 - t, n - i) * Cnk[n, i];
                np += coef * controlPoints[i];
            }
            ChangePointDepth(ref np, 0.0001f);
            curvePoints[j] = np;
            t += dt;
        }
        lineRenderer.positionCount = BezierSampleCount + 1;
        lineRenderer.SetPositions(curvePoints);
    }

    void disturbanceControlPoints(ref List<Vector3> controlPoints)
    {
        int countP = controlPoints.Count;
        Vector3 lookAt = depthCamera.transform.forward;
        Vector3 line = controlPoints[countP - 1] - controlPoints[0];
        Vector3 offset = Vector3.Distance(controlPoints[0], controlPoints[countP - 1]) * Vector3.Cross(line, lookAt).normalized;    // unity������ϵ
        // std::cout << offset.x << " " << offset.y << " " << offset.z << ";" << std::endl;

        for (int i = 1, j = countP - 2; i <= j; i++, j--)
        {
            float coef = (float)i / countP * 0.3f;
            controlPoints[i] += coef * offset;
            if (i != j)
            {
                controlPoints[j] += coef * offset;
            }
        }
    }

    private void detourToUnvisiblePoint()
    {
        // new
        int l = 0, r = VisibilitySampleCount - 1;
        while (!linesVisibility[segmentIndex][l]) { l++; }
        while (!linesVisibility[segmentIndex][r]) { r--; }

        int bezierCurveCount = 0;
        List<Vector3> controlPoints = new List<Vector3>();
        bool meetl = false;
        for (int i = l; i <= r; ++i)
        {
            Vector3 p = scaleToVec(i);

            // new 
            // right visible 放前面 因为这个点可能同时是起点和终点
            if (meetl && i >= 1 && !linesVisibility[segmentIndex][i - 1] && linesVisibility[segmentIndex][i])
            {
                controlPoints.Add(p);

                disturbanceControlPoints(ref controlPoints);  // 绕一下
                if (bezierCurveCount < MaxBezierCurveCount)     // 最多五条
                {
                    DrawBezierCurve(ref controlPoints, ref straightLinesCurveRender[segmentIndex][bezierCurveCount++]);
                }
                controlPoints.Clear();
                meetl = false;
            }
            // left visible
            if (i + 1 < VisibilitySampleCount && linesVisibility[segmentIndex][i] && !linesVisibility[segmentIndex][i + 1])
            {
                controlPoints.Add(p);
                meetl = true;
            }
            // invisible
            if (meetl && !linesVisibility[segmentIndex][i])
            {
                ChangePointDepth(ref p);
                controlPoints.Add(p);
            }
        }

    }

    private Vector3 disturbanceEndpoint(Vector3 p, Vector2 direction)
    {
        Vector3 faceP = MWorldToScreenPointDepth(p);    // ģ���ϰ������ĵ�
        float lastMinDepth = GetDepthScript.depthTextureRead.GetPixel((int)faceP.x, (int)faceP.y).r;
        // faceP.z = lastMinDepth;

        int stepp = 0;
        while (inScreenRange(faceP + new Vector3(direction.x, direction.y)))
        {
            faceP += new Vector3(direction.x, direction.y);     // sets z = 0
            float minDepth = GetDepthScript.depthTextureRead.GetPixel((int)faceP.x, (int)faceP.y).r;
            if (Math.Abs(minDepth - lastMinDepth) > threshold)
            {
                break;
            }
            faceP.z = Math.Min(faceP.z, minDepth);
            lastMinDepth = minDepth;

            ++stepp;
            if (stepp > 50)
            {
                break;
            }
        }
        // Debug.Log(stepp);
        

        float dis = Vector3.Distance(MScreenToWorldPointDepth(faceP), p);

        faceP -= new Vector3(0.0f, 0.0f, 0.0002f);
        faceP += dis * 30.0f * new Vector3(direction.x, direction.y);
        for (int i = 0; i < 10; i++)
        {
            faceP -= dis * 10.0f * new Vector3(0.0f, 1.0f, 0.0f);
            if (!inScreenRange(faceP))
            {
                break;
            }
            float minDepth = GetDepthScript.depthTextureRead.GetPixel((int)faceP.x, (int)faceP.y).r;
            if (minDepth < faceP.z)
            {
                break;
            }
        }
        faceP += dis * 10.0f * new Vector3(0.0f, 1.0f, 0.0f);

        faceP.x = Math.Max(faceP.x, 0.0f);
        faceP.x = Math.Min(faceP.x, (float)Screen.width);
        faceP.y = Math.Max(faceP.y, 0.0f);
        faceP.y = Math.Min(faceP.y, (float)Screen.height);

        p = MScreenToWorldPointDepth(faceP);
        return p;
    }

    private void detourToEndpoint()
    {
        Vector3 screenP1 = MWorldToScreenPointDepth(p1);
        Vector3 screenP2 = MWorldToScreenPointDepth(p2);
        Vector2 screenP12 = screenP2 - screenP1;
        screenP12.Normalize();

        if (!linesVisibility[segmentIndex][0])
        {
            int i = 0;
            while (i < VisibilitySampleCount - 1 && !linesVisibility[segmentIndex][++i]) { }
            Vector3 visibleP = scaleToVec(i);

            // Vector3 edgeP1 = disturbanceEndpoint(p1, screenP12 * -1.0f);
            Vector3 edgeP1 = disturbanceEndpoint(p1, new Vector2(-1.0f, 0.0f));
            Vector3 extraP = p1 + (visibleP - edgeP1);
            var catmullRomP = new List<Vector3> { extraP, p1, edgeP1, visibleP, extraP };
            DrawCatmullRomCurve(catmullRomP, ref straightLinesCurveRender[segmentIndex][MaxBezierCurveCount]);
        }

        if (!linesVisibility[segmentIndex][VisibilitySampleCount - 1])
        {
            int i = VisibilitySampleCount - 1;
            while (i > 0 && !linesVisibility[segmentIndex][--i]) { }
            Vector3 visibleP = scaleToVec(i);

            // Vector3 edgeP2 = disturbanceEndpoint(p2, screenP12);
            Vector3 edgeP2 = disturbanceEndpoint(p2, new Vector2(1.0f, 0.0f));
            Vector3 extraP = p2 + (visibleP - edgeP2);
            var catmullRomP = new List<Vector3> { extraP, p2, edgeP2, visibleP, extraP };

            DrawCatmullRomCurve(catmullRomP, ref straightLinesCurveRender[segmentIndex][MaxBezierCurveCount + 1]);
        }
    }

    private void DrawCatmullRomCurve(List<Vector3> controlPoints, ref LineRenderer lineRenderer)
    {
        float factor = 0.8f;
        int index = 0;
        int pointCount = (controlPoints.Count - 3) * (CatmullRomSampleCount + 1);
        Vector3[] curvePoints = new Vector3[pointCount];

        for (int i = 0; i + 3 < controlPoints.Count; ++i)
        {
            Vector3 p0 = controlPoints[i], p1 = controlPoints[i + 1],
                p2 = controlPoints[i + 2], p3 = controlPoints[i + 3];

            float t = 0, step = 1.0f / CatmullRomSampleCount;
            while (t <= 1 + 0.0001f)
            {
                Vector3 c0 = p1;
                Vector3 c1 = (p2 - p0) * factor;
                Vector3 c2 = (p2 - p1) * 3.0f - (p3 - p1) * factor - (p2 - p0) * 2.0f * factor;
                Vector3 c3 = (p2 - p1) * -2.0f + (p3 - p1) * factor + (p2 - p0) * factor;

                Vector3 curvePoint = c3 * t * t * t + c2 * t * t + c1 * t + c0;

                if (i == 1)
                {
                    ChangePointDepth(ref curvePoint, 0.0001f);
                }
                curvePoints[index++] = curvePoint;
                t += step;
            }
        }

        lineRenderer.positionCount = pointCount;
        lineRenderer.SetPositions(curvePoints);
    }

    private void RaisestraightLineLogo()
    {
        float step = 0.1f;

        while (GetSamplePointsVisibility())
        {
            p1 += step * depthCamera.transform.up;
            p2 += step * depthCamera.transform.up;
            step *= 2;
        }
    }

    private void AdjustPointOrder()
    {
        Vector3 line = p2 - p1;
        float dir = Vector3.Dot(line, depthCamera.transform.right);
        if (dir < 0)
        {
            Vector3 t = p1;
            p1 = p2;
            p2 = t;
        }
    }

    private void changeDepthMap()
    {
        GetDepthScript = Camera.main.GetComponent<Depth>();
        // depthCamera = GameObject.Find("DepthCamera").GetComponent<Camera>();
    }
}

