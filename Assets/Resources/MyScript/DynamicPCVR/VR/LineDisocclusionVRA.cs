using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;


public class LineDisocclusionVRA : MonoBehaviour
{
    private MirrorControllerA mirrorController;
    private Vector3 p1, p2;
    private DPCArrow current_line;

    public int VisibilitySampleCount = 20, BezierSampleCount = 30, CatmullRomSampleCount = 30, QuadraticSampleCount = 30;
    private int samplePointIndex;
    private const float DirectChangeDepthVal = 0.0002f;
    private int[,] Cnk;
    public bool[] lineVisibility;
    public float threshold_change_obj = 0.00002f;
    public float threshold_unvisible = 0.00001f;
    public float threshold_edge_point_change_dis = 10.0f;
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

    private GlobalUtils globalUtils;
    // 测试相关
    public bool testChangeDirectly = true, testDrawToEnd = true;
    public GameObject visibleSphere;

    List<Vector3> fuxk = new List<Vector3>();

    private void Start()
    {
        mirrorController = GetComponentInParent<MirrorControllerA>();
        globalUtils = GetComponent<GlobalUtils>();

        InitCnk(VisibilitySampleCount);
        lineVisibility = new bool[VisibilitySampleCount];
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
            visibiltyFilter[] tmp = new visibiltyFilter[VisibilitySampleCount];
            for (int i = 0; i < VisibilitySampleCount; ++i)
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
        Debug.LogWarning(Vector3.Distance(p1, p2));

        DrawArrow();    // arrow
        // AdjustPointOrder();
        // RaisestraightLineLogo();
        DrawVisiblestraightLine();  // origin 不加去遮挡，被遮挡的直线不画的原直线

        // fuxk.Add(p1);
        if (global_curve) GlobalCurve();
        else LocalCurve();
        // fuxk.Add(p2);
        if (fuxk.Count == 0)
        {
            fuxk.Add(p1);
            fuxk.Add(p2);
        }

        Vector3[] t_array = fuxk.ToArray();
        current_line.curvePointList.Add(t_array);

    }

    private void LocalCurve()
    {
        for (samplePointIndex = 0; samplePointIndex < VisibilitySampleCount; samplePointIndex++)
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
        Vector4 np1 = scaleToVec(-1), np2 = scaleToVec(VisibilitySampleCount);
        Vector4 quadratic_func = new Vector4();
        float max_height = float.MinValue;
        for (samplePointIndex = 0; samplePointIndex < VisibilitySampleCount; samplePointIndex++)
        {
            if (!lineVisibility[samplePointIndex])
            {
                Vector3 cur_p = scaleToVec(samplePointIndex);

                float dis = (float)Mathf.Abs(samplePointIndex - VisibilitySampleCount / 2.0f) / VisibilitySampleCount;  // 0 - 1/2
                float proportion = (1 - dis) * (1 - dis);   //后面一个2.0用于调整比例
                // int max_step = (int)(50 * proportion);
                int max_step = (int)(test_max_step * proportion * Vector3.Distance(p1, p2));
                while (!GetPointVisibility(cur_p, 0) && max_step > 0)
                {
                    cur_p += new Vector3(0, 0.001f, 0);
                    max_step--;
                }
                // Debug.LogFormat("up up up {0}", (int)(test_max_step * Vector3.Distance(p1, p2)) - max_step);

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
        while (i < VisibilitySampleCount)
        {
            if (lineVisibility[i])
            {
                Vector3 tp = scaleToVec(i);
                while (i < VisibilitySampleCount && lineVisibility[i]) { i++; }
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
        else if (samplePointIndex == VisibilitySampleCount)
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

        while (samplePointIndex < VisibilitySampleCount - 1 && !lineVisibility[++samplePointIndex]) { }
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
        if (Vector3.Distance(edgeP1, last_edge_p1) < threshold_edge_point_change_dis)
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

        int i = VisibilitySampleCount - 1;
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
        if (Vector3.Distance(edgeP2, last_edge_p2) < threshold_edge_point_change_dis)
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

        for (; samplePointIndex < VisibilitySampleCount; ++samplePointIndex)
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

        if (samplePointIndex == VisibilitySampleCount)
        {
            DetourToP2();
        }
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

    private bool inScreenRange(Vector3 v) =>
        0 <= v.x && v.x < Screen.width && 0 <= v.y && v.y < Screen.height && 0 <= v.z && v.z <= 1;

    private Vector3 scaleToVec(int i) =>
        p1 + (float)i / (VisibilitySampleCount - 1) * (p2 - p1);

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

        int length = 10;
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
        for (int i = 0; i < VisibilitySampleCount; ++i)
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
            while (i < VisibilitySampleCount - 1 && !lineVisibility[++i]) { }
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

        if (!lineVisibility[VisibilitySampleCount - 1])
        {
            int i = VisibilitySampleCount - 1;
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

        int length = 10;
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
        int l = 0, r = VisibilitySampleCount - 1;
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
            if (i + 1 < VisibilitySampleCount && lineVisibility[i] && !lineVisibility[i + 1])
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
        for (int j = 0; j < BezierSampleCount; ++j)
        {
            Vector3 np = new Vector3(0.0f, 0.0f, 0.0f);
            for (int i = 0; i <= n; ++i)
            {
                float coef = (float)Math.Pow(t, i) * (float)Math.Pow(1 - t, n - i) * Cnk[n, i];
                np += coef * controlPoints[i];
            }
            if (testChangeDirectly)
            {
                ChangePointDepth(ref np, 0.0001f);
            }
            // curvePoints[j] = np;
            fuxk.Add(np);
            t += dt;
        }
        // current_line.curvePointList.Add(curvePoints);
    }

    // ====================================================== new method ======================================================
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
        Debug.Log(Mathf.Abs(QuadraticFunctionVal(W, np1.x) - np1.y));
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
        }
        
        UpdateArrow(fuxk[fuxk.Count - 5], current_line.endPoint);
        

    }
}
