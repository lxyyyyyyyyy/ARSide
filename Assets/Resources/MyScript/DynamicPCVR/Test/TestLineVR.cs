using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

public struct visibiltyFilter
{
    public int true_count, false_count;
    public bool last_frame_val;
    public Queue<bool> past_val;
}

public class TestLineVR : MonoBehaviour
{
    private TestMirror mirrorController;
    private Vector3 p1, p2;
    private DPCArrow current_line;

    public int VisibleInRangeSampleCount;
    public int SampleCount, BezierSampleCount = 30, CatmullRomSampleCount = 30, QuadraticSampleCount = 30;
    private int samplePointIndex;
    private const float DirectChangeDepthVal = 0.0002f;
    public bool[] lineVisibility;
    public float threshold_change_obj = 0.00002f; 
    public float threshold_unvisible = 0.00001f;
    public float threshold_distance = 10.0f;
    // disturb to endpoint
    private float test_x = 4.0f, test_y = 15.0f;
    private int last_frame_p1 = filter_dir_init, last_frame_p2 = filter_dir_init;  // 0 - init 1 - horizontal 2 - vertical 
    private const int filter_dir_init = 0, last_h = 1, last_v = 2;
    private Vector3 last_edge_p1 = new Vector3(), last_edge_p2 = new Vector3();
    // 
    public bool filter = true;
    public int filter_frame_count = 20;
    private int current_line_index;
    private List<visibiltyFilter[]> pastLineVisibility;

    public int test_max_step;
    public bool global_curve;

    private TestGlobalUtils globalUtils;
    // 测试相关
    public bool testChangeDirectly = true, testDrawToEnd = true;
    public GameObject visibleSphere;
    
    private int line_index;
    public bool four_order;
    public bool next;
    public bool previous;
    private GameObject control1, control2;
    private List<GameObject> visible;

    public float arrow_length;

    List<Vector3> fuxk = new List<Vector3>();

    private void Start()
    {
        next = false;
        
        control1 = Instantiate(visibleSphere);
        control2 = Instantiate(visibleSphere);

        mirrorController = GetComponent<TestMirror>();
        globalUtils = GetComponent<TestGlobalUtils>();

        lineVisibility = new bool[SampleCount];
        pastLineVisibility = new List<visibiltyFilter[]>();
    }

    private void Update()
    {
        while (pastLineVisibility.Count > mirrorController.syncArrowList.Count)
        {
            pastLineVisibility.RemoveAt(pastLineVisibility.Count - 1);
        }
        while (pastLineVisibility.Count < mirrorController.syncArrowList.Count)
        {
            visibiltyFilter[] tmp = new visibiltyFilter[SampleCount];
            for (int i = 0; i < SampleCount; ++i)
            {
                tmp[i] = new visibiltyFilter()
                {
                    true_count = 0,
                    false_count = 0,
                    past_val = new Queue<bool>()
                };
            }
            pastLineVisibility.Add(tmp);
        }

        for (int i = 0; i < mirrorController.syncArrowList.Count; ++i)
        {
            fuxk.Clear();
            current_line_index = i;
            current_line = mirrorController.syncArrowList[i];

            /*Debug.LogWarning("=======================");
            Debug.LogWarning("origin count: " + mirrorController.syncArrowList[i].curvePointList.Count);
            Debug.LogWarning("origin current_line count: " + current_line.curvePointList.Count);*/

            current_line.curvePointList.Clear();
            current_line.originPointList.Clear();

            /*Debug.LogWarning("after count: " + mirrorController.syncArrowList[i].curvePointList.Count);
            Debug.LogWarning("after current_line count: " + current_line.curvePointList.Count);
            Debug.LogWarning("=======================");*/

            arrowDisocclusion();
            mirrorController.CmdUpdateDPCArrow(current_line);
        }
    }

    private void arrowDisocclusion()
    {
        // De occlusion calculation is performed here
        p1 = current_line.startPoint;
        p2 = current_line.endPoint;

        DrawArrow();    
        RaisestraightLineLogo();
        DrawVisiblestraightLine();  // origin 不加去遮挡，被遮挡的直线不画的原直线

        // fuxk.Add(p1);
        // if (global_curve) BezierCurveScore();
        if (global_curve) NewGlobalCurve();
        // else LocalCurve();
        // fuxk.Add(p2);

        Vector3[] t_array = fuxk.ToArray();
        current_line.curvePointList.Add(t_array);
        // current_line.originPointList.Add(new Vector3[] { p1, p2 });

    }

    private void LocalCurve()
    {
        for (samplePointIndex = 0; samplePointIndex < SampleCount; samplePointIndex++)
        {
            if (!lineVisibility[samplePointIndex])
            {
                DealUnvisiblePoint();
            }
        }
    }

    private void GlobalCurve()
    {
        Vector3 mid_p = (p1 + p2) / 2;
        Vector4 plane = GetPlaneEquation(p1, p2);
        Vector4 np1 = scaleToVec(0), np2 = scaleToVec(SampleCount - 1);
        Vector4 quadratic_func = new Vector4();
        float max_height = float.MinValue;
        for (samplePointIndex = 1; samplePointIndex < SampleCount - 1; samplePointIndex++)
        {
            if (!lineVisibility[samplePointIndex])
            {
                Vector3 cur_p = scaleToVec(samplePointIndex);
                
                float dis = (float)Mathf.Abs(samplePointIndex - SampleCount / 2.0f) / SampleCount;  // 0 - 1/2
                float proportion = (float)Math.Pow((1.0 - dis), 4.0);   //后面一个2.0用于调整比例
                // int max_step = (int)(50 * proportion);
                int max_step = (int)(proportion * test_max_step * Vector3.Distance(p1, p2));
                while (!GetPointVisibility(cur_p, 0) && max_step > 0)
                {
                    cur_p += new Vector3(0, 0.001f, 0);
                    max_step--;
                }
                Debug.LogFormat("up up up {0}", (int)(proportion * test_max_step * Vector3.Distance(p1, p2)) - max_step);
                
                Vector4 W = GetQuadraticFunction(np1, np2, cur_p);
                if (QuadraticFunctionVal(W, mid_p, plane) > max_height)
                {
                    max_height = QuadraticFunctionVal(W, mid_p, plane);
                    quadratic_func = W;
                }
            }
        }
        if (max_height != float.MinValue) DrawQuadraticCurve(quadratic_func, plane);
    }

    private void DrawVisiblestraightLine()
    {
        int i = 0;
        while (i < SampleCount)
        {
            if (lineVisibility[i])
            {
                Vector3 tp = scaleToVec(i);
                while (i < SampleCount && lineVisibility[i]) { i++; }
                current_line.originPointList.Add(new Vector3[] { tp, scaleToVec(i - 1) });
            }
            i++;
        }
        current_line.startPointVisibility = lineVisibility[0];
    }

    private void DealUnvisiblePoint()
    {
        if (samplePointIndex == 0)
        {
            DetourToP1();
        }
        else if (samplePointIndex == SampleCount)
        {
            DetourToP2();
        }
        else
        {
            DetourToMidP();
        }
    }

    private void DetourToP1()
    {
        Vector3 screenP1 = globalUtils.MWorldToScreenPointDepth(p1);
        Vector3 screenP2 = globalUtils.MWorldToScreenPointDepth(p2);
        Vector2 screenP21 = screenP1 - screenP2;
        screenP21.Normalize();

        while (samplePointIndex < SampleCount - 1 && !lineVisibility[++samplePointIndex]) { }
        Vector3 visibleP = scaleToVec(samplePointIndex);

        float horizontal_dir = (screenP21.x > 0 ? 1.0f : -1.0f);    // 恒小于0应该是
        float vertical_dir = (screenP21.y > 0 ? 1.0f : -1.0f);
        int step_vertical = 0, step_horizontal = 0, step_line = 0;

        // Vector3 t_line = disturbanceEndpoint(p1, screenP12 * -1.0f, ref step_line);
        Vector3 t_vertival = disturbanceEndpoint(p1, new Vector2(0.0f, vertical_dir),
            new Vector2(horizontal_dir, vertical_dir), ref step_vertical);
        Vector3 t_horizontal = disturbanceEndpoint(p1, new Vector2(horizontal_dir, 0.0f),
            new Vector2(horizontal_dir, vertical_dir), ref step_horizontal);

        Vector3 edgeP1 = new Vector3();
        if (last_frame_p1 == filter_dir_init)
        {
            edgeP1 = (step_horizontal < step_vertical) ? t_horizontal : t_vertival;
            last_frame_p1 = (step_horizontal < step_vertical) ? last_h : last_v;
        }
        else if (last_frame_p1 == last_h)
        {
            edgeP1 = (step_horizontal > step_vertical * 2) ? t_vertival : t_horizontal;
            last_frame_p1 = (step_horizontal > step_vertical * 2) ? last_v : last_h;
        }
        else if (last_frame_p1 == last_v)
        {
            edgeP1 = (step_vertical > step_horizontal * 2) ? t_horizontal : t_vertival;
            last_frame_p1 = (step_vertical > step_horizontal * 2) ? last_h : last_v;
        }

        // 强行固定一个方向先
        edgeP1 = t_vertival;
        Debug.LogFormat("distance: {0}", Vector3.Distance(edgeP1, last_edge_p1));
        if (Vector3.Distance(edgeP1, last_edge_p1) < threshold_distance)
        {
            edgeP1 = last_edge_p1;
        }
        last_edge_p1 = edgeP1;

        Vector3 extraP = p1 + (visibleP - edgeP1);

        var catmullRomP = new List<Vector3> { extraP, p1, edgeP1, visibleP, extraP };
        DrawCatmullRomCurve(catmullRomP, 0);
    }

    private void DetourToP2()
    {
        Vector3 screenP1 = globalUtils.MWorldToScreenPointDepth(p1);
        Vector3 screenP2 = globalUtils.MWorldToScreenPointDepth(p2);
        Vector2 screenP12 = screenP2 - screenP1;
        screenP12.Normalize();

        int i = SampleCount - 1;
        while (i > 0 && !lineVisibility[--i]) { }
        Vector3 visibleP = scaleToVec(i);

        float horizontal_dir = (screenP12.x > 0 ? 1.0f : -1.0f);    // 恒小于0应该是
        float vertical_dir = (screenP12.y > 0 ? 1.0f : -1.0f);
        int step_vertical = 0, step_horizontal = 0, step_line = 0;

        // Vector3 t_line = disturbanceEndpoint(p2, screenP12 * 1.0f, ref step_line);
        Vector3 t_vertival = disturbanceEndpoint(p2, new Vector2(0.0f, vertical_dir),
            new Vector2(horizontal_dir, vertical_dir), ref step_vertical);
        Vector3 t_horizontal = disturbanceEndpoint(p2, new Vector2(horizontal_dir, 0.0f),
            new Vector2(horizontal_dir, vertical_dir), ref step_horizontal);

        Vector3 edgeP2 = new Vector3();
        if (last_frame_p2 == filter_dir_init)
        {
            edgeP2 = (step_horizontal < step_vertical) ? t_horizontal : t_vertival;
            last_frame_p2 = (step_horizontal < step_vertical) ? last_h : last_v;
        }
        else if (last_frame_p2 == last_h)
        {
            edgeP2 = (step_horizontal > step_vertical * 2) ? t_vertival : t_horizontal;
            last_frame_p2 = (step_horizontal > step_vertical * 2) ? last_v : last_h;
        }
        else if (last_frame_p2 == last_v)
        {
            edgeP2 = (step_vertical > step_horizontal * 2) ? t_horizontal : t_vertival;
            last_frame_p2 = (step_vertical > step_horizontal * 2) ? last_h : last_v;
        }

        edgeP2 = t_horizontal;
        if (Vector3.Distance(edgeP2, last_edge_p2) < threshold_distance)
        {
            edgeP2 = last_edge_p2;
        }
        last_edge_p2 = edgeP2;

        Vector3 extraP = p2 + (visibleP - edgeP2);
        var catmullRomP = new List<Vector3> { extraP, visibleP, edgeP2, p2, extraP };

        DrawCatmullRomCurve(catmullRomP, 1);
    }

    private void DetourToMidP()
    {
        List<Vector3> controlPoints = new List<Vector3>();
        Vector3 p = scaleToVec(samplePointIndex - 1);
        controlPoints.Add(p);

        for (; samplePointIndex < SampleCount; ++samplePointIndex)
        {
            p = scaleToVec(samplePointIndex);

            // new 
            // right visible 放前面 因为这个点可能同时是起点和终点
            if (lineVisibility[samplePointIndex])
            {
                controlPoints.Add(p);
                disturbanceControlPoints(ref controlPoints);  // 绕一下
                DrawBezierCurve(ref controlPoints);
                return;
            }
            // invisible
            if (!lineVisibility[samplePointIndex])
            {
                ChangePointDepth(ref p);
                controlPoints.Add(p);
            }
        }

        if (samplePointIndex == SampleCount)
        {
            DetourToP2();
        }
    }

    private bool inScreenRange(Vector3 v) =>
        0 <= v.x && v.x < Screen.width && 0 <= v.y && v.y < Screen.height && 0 <= v.z && v.z <= 1;

    private Vector3 scaleToVec(int i) =>
        p1 + (float)i / (SampleCount - 1) * (p2 - p1);

    private Vector3 scaleToVec(float t) =>
        p1 + t * (p2 - p1);

    private void DrawArrow()
    {
        Vector3 screenP1 = globalUtils.MWorldToScreenPointDepth(p1);
        Vector3 screenP2 = globalUtils.MWorldToScreenPointDepth(p2);
        Vector2 dir = (screenP1 - screenP2).normalized;
        Vector2 verticalDir = new Vector2(-dir.y, dir.x);

        if (!GetPointVisibility(p2, threshold_unvisible) && !testDrawToEnd)
        {
            current_line.curvePointList.Add(new Vector3[] { });
            current_line.curvePointList.Add(new Vector3[] { });
            return;
        }

        float length = arrow_length / (float)Vector3.Distance(p2, Camera.main.transform.position);
        Vector3 screenArrowP1 = screenP2 + length * new Vector3(verticalDir.x, verticalDir.y)
            + length * new Vector3(dir.x, dir.y);
        Vector3 screenArrowP2 = screenP2 - length * new Vector3(verticalDir.x, verticalDir.y)
            + length * new Vector3(dir.x, dir.y);

        Vector3 arrowP1 = globalUtils.MScreenToWorldPointDepth(screenArrowP1);
        Vector3 arrowP2 = globalUtils.MScreenToWorldPointDepth(screenArrowP2);

        current_line.curvePointList.Add(new Vector3[] { arrowP1, p2 });
        current_line.curvePointList.Add(new Vector3[] { arrowP2, p2 });
        current_line.originPointList.Add(new Vector3[] { arrowP1, p2 });
        current_line.originPointList.Add(new Vector3[] { arrowP2, p2 });
    }

    private void AdjustPointOrder()
    {
        if (Vector3.Dot((p2 - p1), globalUtils.depthCamera.transform.right) < 0)
        {
            Vector3 t = p1;
            p1 = p2;
            p2 = t;
        }
    }

    private bool GetPointVisibility(Vector3 p, float threshold)
    {
        Vector3 screenP = globalUtils.MWorldToScreenPointDepth(p);
        if (!inScreenRange(screenP))
        {
            return true;    //
        }
        float minDepth = globalUtils.GetDepth((int)screenP.x, (int)screenP.y);
        return (minDepth >= screenP.z - threshold);
    }

    private bool GetSamplePointsVisibility()
    {
        bool completeConvered = true, completeOutofView = true;
        for (int i = 0; i < SampleCount; ++i)
        {
            Vector3 p = scaleToVec(i);
            Vector3 screenP = globalUtils.MWorldToScreenPointDepth(p);

            if (!inScreenRange(screenP))
            { 
                lineVisibility[i] = true;
                continue;
            }

            completeOutofView = false;
            float minDepth = globalUtils.GetDepth((int)screenP.x, (int)screenP.y);
            lineVisibility[i] = (minDepth >= screenP.z - threshold_unvisible);

            //filter
            pastLineVisibility[current_line_index][i].past_val.Enqueue(lineVisibility[i]);
            pastLineVisibility[current_line_index][i].true_count += lineVisibility[i] ? 1 : 0;
            pastLineVisibility[current_line_index][i].false_count += lineVisibility[i] ? 0 : 1;
            if (pastLineVisibility[current_line_index][i].past_val.Count > filter_frame_count)
            {
                bool val = pastLineVisibility[current_line_index][i].past_val.Dequeue();
                pastLineVisibility[current_line_index][i].true_count -= val ? 1 : 0;
                pastLineVisibility[current_line_index][i].false_count -= val ? 0 : 1;
                
                if (filter && lineVisibility[i] && pastLineVisibility[current_line_index][i].true_count != filter_frame_count)
                {
                    lineVisibility[i] = pastLineVisibility[current_line_index][i].last_frame_val;
                }

                if (filter && !lineVisibility[i] && pastLineVisibility[current_line_index][i].false_count != filter_frame_count)
                {
                    lineVisibility[i] = pastLineVisibility[current_line_index][i].last_frame_val;
                }
                pastLineVisibility[current_line_index][i].last_frame_val = lineVisibility[i];
            }

            if (completeConvered)
            {
                completeConvered = !lineVisibility[i];
            }
        }
        if (completeOutofView)
        {
            completeConvered = false;
        }

        return completeConvered;
    }

    private void RaisestraightLineLogo()
    {
        float step = 0.1f;

        while (GetSamplePointsVisibility())
        {
            p1 += step * globalUtils.depthCamera.transform.up;
            p2 += step * globalUtils.depthCamera.transform.up;
            step *= 2;
        }
    }

   private void ChangePointDepth(ref Vector3 p, float changeDepth = DirectChangeDepthVal)
    {
        Vector3 screenP = globalUtils.MWorldToScreenPointDepth(p);
        float minDepth = globalUtils.GetDepth((int)screenP.x, (int)screenP.y);

        if (minDepth < screenP.z)
        {
            screenP.z = minDepth - changeDepth;
            p = globalUtils.MScreenToWorldPointDepth(screenP);
        }
    }

    // =========================================================== detour to endpoint ============================================================
    private void detourToEndpoint()
    {
        Vector3 screenP1 = globalUtils.MWorldToScreenPointDepth(p1);
        Vector3 screenP2 = globalUtils.MWorldToScreenPointDepth(p2);
        Vector2 screenP12 = screenP2 - screenP1;
        Vector2 screenP21 = -screenP12;
        screenP12.Normalize();

        if (!lineVisibility[0])
        {
            int i = 0;
            while (i < SampleCount - 1 && !lineVisibility[++i]) { }
            Vector3 visibleP = scaleToVec(i);

            float horizontal_dir = (screenP21.x > 0 ? 1.0f : -1.0f);    // 恒小于0应该是
            float vertical_dir = (screenP21.y > 0 ? 1.0f : -1.0f);
            int step_vertical = 0, step_horizontal = 0, step_line = 0;

            // Vector3 t_line = disturbanceEndpoint(p1, screenP12 * -1.0f, ref step_line);
            Vector3 t_vertival = disturbanceEndpoint(p1, new Vector2(0.0f, vertical_dir), 
                new Vector2(horizontal_dir, vertical_dir), ref step_vertical);
            Vector3 t_horizontal = disturbanceEndpoint(p1, new Vector2(horizontal_dir, 0.0f), 
                new Vector2(horizontal_dir, vertical_dir), ref step_horizontal);

            Vector3 edgeP1 = new Vector3();
            if (last_frame_p1 == filter_dir_init)
            {
                edgeP1 = (step_horizontal < step_vertical) ? t_horizontal : t_vertival;
                last_frame_p1 = (step_horizontal < step_vertical) ? last_h : last_v;
            } 
            else if (last_frame_p1 == last_h)
            {
                edgeP1 = (step_horizontal > step_vertical * 2) ? t_vertival : t_horizontal;
                last_frame_p1 = (step_horizontal > step_vertical * 2) ? last_v : last_h;
            }
            else if (last_frame_p1 == last_v)
            {
                edgeP1 = (step_vertical > step_horizontal * 2) ? t_horizontal : t_vertival;
                last_frame_p1 = (step_vertical > step_horizontal * 2) ? last_h : last_v;
            }
            
            Vector3 extraP = p1 + (visibleP - edgeP1);

            /*Vector3 visP1 = p1;
            ChangePointDepth(ref visP1);*/

            var catmullRomP = new List<Vector3> { extraP, visibleP, edgeP1, p1, extraP };
            DrawCatmullRomCurve(catmullRomP, 1);
        }

        if (!lineVisibility[SampleCount - 1])
        {
            int i = SampleCount - 1;
            while (i > 0 && !lineVisibility[--i]) { }
            Vector3 visibleP = scaleToVec(i);
            
            float horizontal_dir = (screenP12.x > 0 ? 1.0f : -1.0f);    // 恒小于0应该是
            float vertical_dir = (screenP12.y > 0 ? 1.0f : -1.0f);
            int step_vertical = 0, step_horizontal = 0, step_line = 0;

            // Vector3 t_line = disturbanceEndpoint(p2, screenP12 * 1.0f, ref step_line);
            Vector3 t_vertival = disturbanceEndpoint(p2, new Vector2(0.0f, vertical_dir),
                new Vector2(horizontal_dir, vertical_dir), ref step_vertical);
            Vector3 t_horizontal = disturbanceEndpoint(p2, new Vector2(horizontal_dir, 0.0f),
                new Vector2(horizontal_dir, vertical_dir), ref step_horizontal);

            Vector3 edgeP2 = new Vector3();
            if (last_frame_p2 == filter_dir_init)
            {
                edgeP2 = (step_horizontal < step_vertical) ? t_horizontal : t_vertival;
                last_frame_p2 = (step_horizontal < step_vertical) ? last_h : last_v;
            }
            else if (last_frame_p2 == last_h)
            {
                edgeP2 = (step_horizontal > step_vertical * 2) ? t_vertival : t_horizontal;
                last_frame_p2 = (step_horizontal > step_vertical * 2) ? last_v : last_h;
            }
            else if (last_frame_p2 == last_v)
            {
                edgeP2 = (step_vertical > step_horizontal * 2) ? t_horizontal : t_vertival;
                last_frame_p2 = (step_vertical > step_horizontal * 2) ? last_h : last_v;
            }

            Vector3 extraP = p2 + (visibleP - edgeP2);
            var catmullRomP = new List<Vector3> { extraP, visibleP, edgeP2, p2, extraP };

            DrawCatmullRomCurve(catmullRomP, 1);
        }
    }

    private Vector3 disturbanceEndpoint(Vector3 p, Vector2 direction, Vector2 disturbance_dir, ref int step)
    {
        Vector3 faceP = globalUtils.MWorldToScreenPointDepth(p);
        float lastMinDepth = globalUtils.GetDepth((int)faceP.x, (int)faceP.y);
        // faceP.z = lastMinDepth;

        // sphere1.transform.position = globalUtils.MScreenToWorldPointDepth(new Vector3(faceP.x, faceP.y, lastMinDepth));

        int MAXSTEP = 0;
        float depth_diff = 0.0f;
        while (inScreenRange(faceP + new Vector3(direction.x, direction.y)))
        {
            faceP += new Vector3(direction.x, direction.y);     // sets z = 0
            float minDepth = globalUtils.GetDepth((int)faceP.x, (int)faceP.y);
            if (minDepth - lastMinDepth > threshold_change_obj)
            {
                depth_diff = Math.Abs(minDepth - lastMinDepth);
                break;
            }
            Debug.Log(Math.Abs(minDepth - lastMinDepth).ToString("f6"));
            faceP.z = Math.Min(faceP.z, minDepth);
            lastMinDepth = minDepth;

            ++MAXSTEP;
            if (MAXSTEP > 200)
            {
                break;
            }
        }
        step = MAXSTEP;

        if (direction.x == 0)
        {
            // sphere2.transform.position = globalUtils.MScreenToWorldPointDepth(faceP);
        }
        if (direction.y == 0)
        {
            // sphere3.transform.position = globalUtils.MScreenToWorldPointDepth(faceP);
        }

        float dis = Vector2.Distance(faceP, globalUtils.MWorldToScreenPointDepth(p));

        Vector3 base_disturbance_dir = new Vector3(test_x, test_y, depth_diff / 2);
        if (direction.x != 0)
        {
            base_disturbance_dir.x *= direction.x;
            base_disturbance_dir.y *= -disturbance_dir.y;
        }
        if (direction.y != 0)
        {
            base_disturbance_dir.y *= direction.y;
            base_disturbance_dir.x *= -disturbance_dir.x;
        }
        faceP += base_disturbance_dir;

        /*Vector2 disturbance_dir = new Vector2(0.5f, 1.0f);
        if (direction.x == -1.0f || direction.y == -1.0f)
        {
            disturbance_dir = new Vector2(-0.5f, -1.0f);
        }
        for (int i = 0; i < (int)dis/2; ++i)
        {
            faceP += new Vector3(disturbance_dir.x, disturbance_dir.y);
        }*/
        /*faceP += dis * new Vector3(direction.x, direction.y);
        for (int i = 0; i < 10; i++)
        {
            faceP -= dis * 10.0f * new Vector3(0.0f, 1.0f, 0.0f);
            if (!inScreenRange(faceP))
            {
                break;
            }
            float minDepth = globalUtils.GetDepth((int)faceP.x, (int)faceP.y);
            if (minDepth < faceP.z)
            {
                break;
            }
        }
        */

        faceP.x = Math.Max(faceP.x, 0.0f);
        faceP.x = Math.Min(faceP.x, (float)Screen.width);
        faceP.y = Math.Max(faceP.y, 0.0f);
        faceP.y = Math.Min(faceP.y, (float)Screen.height);

        p = globalUtils.MScreenToWorldPointDepth(faceP);
        return p;
    }

    private void DrawCatmullRomCurve(List<Vector3> controlPoints, int coverId)
    {
        float factor = 0.8f;
        // int index = 0;
        // int pointCount = (controlPoints.Count - 3) * (CatmullRomSampleCount + 1);
        // Vector3[] curvePoints = new Vector3[pointCount];
        // List<Vector3> curvePoints = new List<Vector3>();

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

                if (i != coverId && testChangeDirectly)
                {
                    ChangePointDepth(ref curvePoint, 0.0001f);
                }

                if (i == coverId && !GetPointVisibility(curvePoint, threshold_unvisible) && !testDrawToEnd)   // 被遮挡部分不渲染
                {
                    break;
                }

                // curvePoints[index++] = curvePoint;
                t += step;
                fuxk.Add(curvePoint);
            }
        }
        // sphere1.transform.position = p2;
        if (!GetPointVisibility(current_line.endPoint, threshold_unvisible) && testDrawToEnd && controlPoints[3] == (current_line.endPoint))
        {
            UpdateArrow(fuxk[fuxk.Count - 5], current_line.endPoint);
        }

        // current_line.curvePointList.Add(curvePoints);
        // Vector3[] t_array = curvePoints.ToArray();
        // current_line.curvePointList.Add(t_array);
    }

    private void UpdateArrow(Vector3 p, Vector3 endpoint)
    {
        Vector3 screenP = globalUtils.MWorldToScreenPointDepth(p);
        Vector3 screenP2 = globalUtils.MWorldToScreenPointDepth(endpoint);
        Vector2 dir = (screenP - screenP2).normalized;
        Vector2 verticalDir = new Vector2(-dir.y, dir.x);

        float length = arrow_length / (float)Vector3.Distance(p2, Camera.main.transform.position);
        Vector3 screenArrowP1 = screenP2 + length * new Vector3(verticalDir.x, verticalDir.y)
            + length * new Vector3(dir.x, dir.y);
        Vector3 screenArrowP2 = screenP2 - length * new Vector3(verticalDir.x, verticalDir.y)
            + length * new Vector3(dir.x, dir.y);

        
        Vector3 arrowP1 = globalUtils.MScreenToWorldPointDepth(screenArrowP1);
        Vector3 arrowP2 = globalUtils.MScreenToWorldPointDepth(screenArrowP2);
        // sphere2.transform.position = arrowP1;
        // sphere3.transform.position = arrowP2;

        if (current_line.curvePointList[0].Length <= 0) return;
        current_line.curvePointList[0][0] = arrowP1;
        current_line.curvePointList[1][0] = arrowP2;
        current_line.curvePointList[0][1] = endpoint;
        current_line.curvePointList[1][1] = endpoint;
    }

    // ====================================================== detour to UnvisiblePoint ============================================================
    private void detourToUnvisiblePoint()
    {
        int l = 0, r = SampleCount - 1;
        while (!lineVisibility[l]) { l++; }
        while (!lineVisibility[r]) { r--; }

        List<Vector3> controlPoints = new List<Vector3>();
        bool meetl = false;
        for (int i = l; i <= r; ++i)
        {
            Vector3 p = scaleToVec(i);

            // new 
            // right visible 放前面 因为这个点可能同时是起点和终点
            if (meetl && i >= 1 && !lineVisibility[i - 1] && lineVisibility[i])
            {
                controlPoints.Add(p);
                disturbanceControlPoints(ref controlPoints);  // 绕一下
                DrawBezierCurve(ref controlPoints);
                controlPoints.Clear();
                meetl = false;
            }
            // left visible
            if (i + 1 < SampleCount && lineVisibility[i] && !lineVisibility[i + 1])
            {
                controlPoints.Add(p);
                meetl = true;
            }
            // invisible
            if (meetl && !lineVisibility[i])
            {
                ChangePointDepth(ref p);
                controlPoints.Add(p);
            }
        }

    }

    void disturbanceControlPoints(ref List<Vector3> controlPoints)
    {
        int countP = controlPoints.Count;
        Vector3 lookAt = globalUtils.depthCamera.transform.forward;
        Vector3 line = controlPoints[countP - 1] - controlPoints[0];
        Vector3 offset = Vector3.Distance(controlPoints[0], controlPoints[countP - 1]) * Vector3.Cross(line, lookAt).normalized;
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

    private void DrawBezierCurve(ref List<Vector3> controlPoints)
    {
        int n = controlPoints.Count - 1;

        double t = 0, dt = 1.0 / BezierSampleCount;
        // Vector3[] curvePoints = new Vector3[BezierSampleCount + 1];
        for (int j = 0; j <= BezierSampleCount; ++j)
        {
            Vector3 np = new Vector3(0.0f, 0.0f, 0.0f);
            for (int i = 0; i <= n; ++i)
            {
                float coef = (float)Math.Pow(t, i) * (float)Math.Pow(1 - t, n - i) * globalUtils.Cnk[n, i];
                np += coef * controlPoints[i];
            }
            t += dt;

            if (!GetPointVisibility(np, threshold_unvisible))   // 被遮挡部分不渲染
            {
                continue;
            }

            // curvePoints[j] = np;
            fuxk.Add(np); 
        }
        // current_line.curvePointList.Add(curvePoints);
        UpdateArrow(fuxk[fuxk.Count - 5], fuxk[fuxk.Count - 1]);
    }

    // ====================================================== Quadratic method ======================================================
    float QuadraticFunctionVal(Vector4 W, float x) => W.x * x * x + W.y * x + W.z;

    /*
     * W是位于plane上的二次函数
     * W.x = a W.y = b W.z = c 
     * a*x^2 + b*x + c = y
     * xy是位于plane上的点
     * return xy对应的二次函数的函数值
     */
    float QuadraticFunctionVal(Vector4 W, Vector3 coordinate, Vector4 plane)
    {
        float dz = 0;
        if (plane.z != 0)
        {
            dz = plane.w / plane.z;
        }
        coordinate.z -= dz;
        float d = Mathf.Sign(coordinate.x) * Mathf.Sqrt(Mathf.Pow(coordinate.x, 2) + Mathf.Pow(coordinate.z, 2));

        return QuadraticFunctionVal(W, d);
    }

    /*
     * 简化版，垂直于xoz平面
     * p1, p2平面上两点
     * return N
     * n.x*X + n.y*Y + n.z*Z = n.w
     */
    Vector4 GetPlaneEquation(Vector3 p1, Vector3 p2)
    {
        Vector3 dir1 = (p1 - p2);
        Vector3 dir2 = new Vector3(0, 1.0f, 0);
        Vector3 normal = Vector3.Cross(dir1, dir2);
        //Debug.Assert(normal.y == 0);

        float k = normal.x * p1.x + normal.y * p1.y + normal.z * p1.z;
        //Debug.Assert(Mathf.Abs(k - (normal.x * p2.x + normal.y * p2.y + normal.z * p2.z)) < 0.00001);

        return new Vector4(normal.x, normal.y, normal.z, k);
    }

    /* 
     * 简化版，获得p1, p2, p确定的平面上的经过p1, p2, p的二次函数
     * 其中p1, p2, p确定的平面垂直于xoz平面
     * return W
     * W.x*x^2 + W.y*x + W.z = y
     * W.w = 1
     */
    Vector4 GetQuadraticFunction(Vector3 p1, Vector3 p2, Vector3 p)
    {
        Vector4 normal = GetPlaneEquation(p1, p2);

        // move to zero
        float dz = 0;
        if (normal.z != 0)
        {
            dz = normal.w / normal.z;
        }
        p1.z -= dz;
        p2.z -= dz;
        p.z -= dz;
        //Debug.Assert(Mathf.Abs(normal.x * p.x + normal.y * p.y + normal.z * p.z) < 0.00001);
        //Debug.Assert(Mathf.Abs(normal.x * p1.x + normal.y * p1.y + normal.z * p1.z) < 0.00001);
        //Debug.Assert(Mathf.Abs(normal.x * p2.x + normal.y * p2.y + normal.z * p2.z) < 0.00001);

        // 3-dimenson -> 2-dimenson
        Vector2 np1 = new Vector2(Mathf.Sign(p1.x) * Mathf.Sqrt(Mathf.Pow(p1.x, 2) + Mathf.Pow(p1.z, 2)), p1.y);
        Vector2 np2 = new Vector2(Mathf.Sign(p2.x) * Mathf.Sqrt(Mathf.Pow(p2.x, 2) + Mathf.Pow(p2.z, 2)), p2.y);
        Vector2 np = new Vector2(Mathf.Sign(p.x) * Mathf.Sqrt(Mathf.Pow(p.x, 2) + Mathf.Pow(p.z, 2)), p.y);

        // 2-dimenson curve
        // AW = B => W = (A^TA)^-1A^TB
        Vector4 row1 = new Vector4(np1.x * np1.x, np1.x, 1, 0);
        Vector4 row2 = new Vector4(np2.x * np2.x, np2.x, 1, 0);
        Vector4 row3 = new Vector4(np.x * np.x, np.x, 1, 0);
        Vector4 row4 = new Vector4(0, 0, 0, 1);
        Vector4 B = new Vector4(np1.y, np2.y, np.y, 1);
        Matrix4x4 A = new Matrix4x4(row1, row2, row3, row4);    // 构造函数是按列...
        A = Matrix4x4.Transpose(A);
        Matrix4x4 t = Matrix4x4.Inverse(Matrix4x4.Transpose(A) * A);
        Vector4 W = t * Matrix4x4.Transpose(A) * B;
        //Debug.Assert(W.w == 1);
        // Debug.Log(Mathf.Abs(QuadraticFunctionVal(W, np1.x) - np1.y));
        //Debug.Assert(Mathf.Abs(QuadraticFunctionVal(W, np1.x) - np1.y) < 0.00001);
        //Debug.Assert(Mathf.Abs(QuadraticFunctionVal(W, np2.x) - np2.y) < 0.00001);
        //Debug.Assert(Mathf.Abs(QuadraticFunctionVal(W, np.x) - np.y) < 0.00001);

        return W;
    }

    void DrawQuadraticCurve(Vector4 W, Vector4 plane)
    {
        float t = 0, step = 1.0f / QuadraticSampleCount;
        for (int i = 0; i < QuadraticSampleCount; ++i)
        {
            Vector3 corrdinate = scaleToVec(t);
            float y_val = QuadraticFunctionVal(W, corrdinate, plane);
            fuxk.Add(new Vector3(corrdinate.x, y_val, corrdinate.z));

            t += step;
            if (i == 0)
            {
                Debug.LogWarningFormat("p1: {0}, calculte p1: {1}", corrdinate.ToString("f4"), y_val.ToString("f4"));
            }
        }

        UpdateArrow(fuxk[fuxk.Count - 5], fuxk[fuxk.Count - 1]);
        // UpdateArrow(fuxk[fuxk.Count - 10], fuxk[fuxk.Count - 1]);
        
    }

    // ==================================================== new =====================================================

    float BezierCurvature(List<Vector3> control_points)
    {
        float min_R = float.MaxValue;
        if (control_points.Count == 3)
        {
            // Path = (1-t)^2A + 2t(1-t)B + t^2C
            // Path' = 2(t-1)A + (2-4t)B + 2tC
            // Path'' = 2A - 4B + 2C
            Vector3 second_derivative = 2 * control_points[0] - 4 * control_points[1] + 2 * control_points[2];
            float t = 0, step = 1.0f / SampleCount;
            while (t <= 1)
            {
                Vector3 first_derivative =
                    2 * (t - 1) * control_points[0] +
                    (2 - 4 * t) * control_points[1] +
                    2 * t * control_points[2];

                float R_part1 = Mathf.Pow(first_derivative.magnitude, 3);
                float R_part2 =
                    Mathf.Pow(second_derivative.z * first_derivative.y - second_derivative.y * first_derivative.z, 2) +
                    Mathf.Pow(second_derivative.x * first_derivative.z - second_derivative.z * first_derivative.x, 2) +
                    Mathf.Pow(second_derivative.y * first_derivative.x - second_derivative.x * first_derivative.y, 2);
                float R_part3 = Mathf.Pow(R_part2, 0.5f);
                float R = R_part1 / R_part3;
                if (R < min_R) min_R = R;

                t += step;
            }
        }
        return min_R;
    }

    void SampleDirection(ref List<Vector3> direction)
    {
        Vector3 p1p2 = Vector3.Normalize(p2 - p1);
        Vector3 view = Camera.main.transform.forward;
        Vector3 up = new Vector3(0, 1.0f, 0);
        Vector3 vertical_to_vertical_plane = Vector3.Normalize(Vector3.Cross(up, p1p2));
        if (Vector3.Dot(view, vertical_to_vertical_plane) > 0)
        {
            vertical_to_vertical_plane = -vertical_to_vertical_plane;
        }

        /*Vector3 front = vertical_to_vertical_plane.z > 0 ? new Vector3(0, 0, 1) : new Vector3(0, 0, -1); ;
        if (Mathf.Abs(vertical_to_vertical_plane.x) > Mathf.Abs(vertical_to_vertical_plane.z))
        {
            front = vertical_to_vertical_plane.x > 0 ? new Vector3(1, 0, 0) : new Vector3(-1, 0, 0);
        }*/

        const int piece = 4;
        for (int i = 0; i < piece; i++)
        {
            float degree = i * 90.0f / (piece - 1);
            direction.Add(Mathf.Cos(Mathf.Deg2Rad * degree) * up +
                          Mathf.Sin(Mathf.Deg2Rad * degree) * vertical_to_vertical_plane);
        }
    }

    Vector3 SampleOnBezierCurve(List<Vector3> control_points, float t)
    {
        int n = control_points.Count - 1;

        Vector3 np = new Vector3(0.0f, 0.0f, 0.0f);
        for (int i = 0; i <= n; ++i)
        {
            float coef = (float)Math.Pow(t, i) * (float)Math.Pow(1 - t, n - i) * globalUtils.Cnk[n, i];
            np += coef * control_points[i];
        }

        return np;
    }

    bool AllVisible(ref List<Vector3> control_points)
    {
        bool all_visible = true;
        float dt = 1.0f / VisibleInRangeSampleCount;
        float t = dt;
        for (int i = 1; i < VisibleInRangeSampleCount - 1; ++i)
        {
            Vector3 on_curve = SampleOnBezierCurve(control_points, t);
            if (!globalUtils.GetPointVisibility(on_curve))
            {
                all_visible = false;
                break;
            }
            t += dt;
        }
        return all_visible;
    }

    bool AllInRange(ref List<Vector3> control_points)
    {
        bool all_in_range = true;
        float dt = 1.0f / VisibleInRangeSampleCount;
        float t = dt;
        for (int i = 1; i < VisibleInRangeSampleCount - 1; ++i)
        {
            Vector3 on_curve = SampleOnBezierCurve(control_points, t);
            if (globalUtils.OutOfRange(on_curve))
            {
                all_in_range = false;
                break;
            }
            t += dt;
        }
        return all_in_range;
    }

    void SampleThreeOrderBezierControlPoint(Vector3 direction, ref List<Vector3> control_points)
    {
        List<Vector3> beizer_control_points = new List<Vector3>() { p1, p2, p2 };
        for (samplePointIndex = 1; samplePointIndex < SampleCount - 1; samplePointIndex++)
        {
            Vector3 cur_p = scaleToVec(samplePointIndex);
            beizer_control_points[1] = cur_p;
            float step_length = 0.01f;
            // int times = 0;

            while (true)
            {
                bool all_visible = AllVisible(ref beizer_control_points), 
                    all_in_range = AllInRange(ref beizer_control_points);
                
                if (!all_in_range)
                {
                    cur_p -= (step_length / 1.2f) * direction;
                    if (line_index == control_points.Count)
                    {
                        Debug.Log("out of range!!!");
                    }
                    break;
                }

                if (all_visible)
                {
                    cur_p -= (step_length / 1.2f) * direction;
                    do
                    {
                        cur_p += 0.02f * direction;
                        beizer_control_points[1] = cur_p;
                    } 
                    while (!AllVisible(ref beizer_control_points));
                    break;
                }

                cur_p += step_length * direction;
                beizer_control_points[1] = cur_p;
                step_length *= 1.2f;

                // times++;
                // Debug.LogFormat("times {0}", times);
            }
            control_points.Add(cur_p); 
        }
    }

    void SampleFourOrderBezierControlPoint(Vector3 direction, ref List<Vector3> control_points)
    {
        float step_length = 0.02f;
        Vector3 length = step_length * direction;
        int max_step = -1, index = -1;
        Vector3 t = new Vector3();
        for (samplePointIndex = 1; samplePointIndex < SampleCount - 1; samplePointIndex++)
        {
            Vector3 cur_p = scaleToVec(samplePointIndex);
            if (globalUtils.OutOfRange(cur_p)) continue;

            int step = 0;
            while (!globalUtils.GetPointVisibility(cur_p) && !globalUtils.OutOfRange(cur_p))
            {
                cur_p += length;
                step++;
            }
            if (step > max_step)
            {
                t = cur_p;
                max_step = step;
                index = samplePointIndex;
            }
        }

        if (max_step == -1) return;

        visible[0].transform.position = t;
        

        List<Vector3> beizer_control_points = new List<Vector3>() { p1, t, t, p2 };
        for (samplePointIndex = 1; samplePointIndex < SampleCount - 1; samplePointIndex++)
        {
            if (samplePointIndex == index) continue;
            int another_control_index = (samplePointIndex < index) ? 1 : 2;

            Vector3 cur_p = scaleToVec(samplePointIndex);
            beizer_control_points[another_control_index] = cur_p;
            beizer_control_points[3 - another_control_index] = t;

            while (true)
            {
                bool all_visible = AllVisible(ref beizer_control_points),
                    all_in_range = AllInRange(ref beizer_control_points);

                if (!all_in_range)
                {
                    cur_p -= (step_length / 1.2f) * direction;
                    beizer_control_points[another_control_index] = cur_p;
                    break;
                }

                if (all_visible)
                {
                    cur_p -= (step_length / 1.2f) * direction;
                    do
                    {
                        cur_p += 0.02f * direction;
                        beizer_control_points[another_control_index] = cur_p;
                    }
                    while (!AllVisible(ref beizer_control_points));
                    break;
                }

                cur_p += step_length * direction;
                beizer_control_points[another_control_index] = cur_p;
                step_length *= 1.2f;
            }

            control_points.Add(beizer_control_points[1]);
            control_points.Add(beizer_control_points[2]);
        }
    }

    void BezierCurveScore()
    {
        List<Vector3> directions = new List<Vector3>();
        SampleDirection(ref directions);

        List<Vector3> three_order_control_point = new List<Vector3>();
        foreach (var direction in directions)
        {
            SampleThreeOrderBezierControlPoint(direction, ref three_order_control_point);
        }

        List<Vector3> four_order_control_point = new List<Vector3>();
        foreach (var direction in directions)
        {
            SampleFourOrderBezierControlPoint(direction, ref four_order_control_point);
        }

        if (next)
        {
            next = false;
            line_index += 1;
        }
        if (previous)
        {
            previous = false;
            line_index -= 1;
        }
        if (line_index < 0) line_index = 0;
        if (line_index >= three_order_control_point.Count) line_index = three_order_control_point.Count - 1;
        // control.transform.position = three_order_control_point[line_index];
        // control1.transform.position = four_order_control_point[line_index * 2];
        // control2.transform.position = four_order_control_point[line_index * 2 + 1];

        List<Vector3> control_points = new List<Vector3>() { p1, three_order_control_point[line_index], p2 };
        if (four_order)
        {
            control_points = new List<Vector3>() { p1, four_order_control_point[line_index * 2],
                four_order_control_point[line_index * 2 + 1], p2 };
        }

        /*Debug.LogFormat("the curvature of current line is {0}, the control point is {1}, {2}, {3}", 
            BezierCurvature(control_points), p1.ToString("f4"), three_order_control_point[line_index].ToString("f4"), 
            p2.ToString("f4"));*/
        DrawBezierCurve(ref control_points);

        float dt = 1.0f / SampleCount;
        float t = dt;
        for (int i = 1; i < SampleCount - 1; ++i)
        {
            Vector3 on_curve = SampleOnBezierCurve(control_points, t);
            // visible[i].transform.position = on_curve;
            t += dt;
        }
    }

    void NewGlobalCurve()
    {
        Vector3 direction = new Vector3(0, 1.0f, 0);
        List<Vector3> beizer_control_points = new List<Vector3>() { p1, (p1 + p2) / 2, p2 };

        Vector3 cur_p = (p1 + p2) / 2;
        float step_length = 0.01f;
        int step = 0;
        while (true)
        {
            bool all_visible = AllVisible(ref beizer_control_points);

            if (all_visible)
            {
                cur_p -= (step_length / 1.2f) * direction;
                do
                {
                    cur_p += 0.02f * direction;
                    beizer_control_points[1] = cur_p;
                }
                while (!AllVisible(ref beizer_control_points));
                break;
            }

            cur_p += step_length * direction;
            beizer_control_points[1] = cur_p;
            step_length *= 1.2f;

            step++;
            if (step >= test_max_step) break;
        }

        DrawBezierCurve(ref beizer_control_points);

    }
}
