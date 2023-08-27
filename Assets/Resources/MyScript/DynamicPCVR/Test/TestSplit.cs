using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestSplit : MonoBehaviour
{
    private TestGlobalUtils globalUtils;

    public bool test_smooth;
    public GameObject sphere;
    public float threshold_angle;

    public GameObject ShowNormalPrefab;
    public GameObject AxesPrefab;

    public Color outline_color;
    public Color picture_color;
    public bool test_picture;

    // Start is called before the first frame update
    void Start()
    {
        globalUtils = GameObject.Find("Script").GetComponent<TestGlobalUtils>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowNormal(ref List<Vector3> normal, ref List<Vector3> position, ref List<Color> color)
    {
        GameObject split_target = Instantiate(ShowNormalPrefab);
        split_target.name = "ShowNormal";

        var indices = new int[position.Count];
        for (int i = 0; i < position.Count; i++)
            indices[i] = i;

        if (color.Count == 0)
        {
            for (int i = 0; i < normal.Count; ++i)
            {
                color.Add(new Color(0, 0, 0));
            }
        }

        Mesh m = new Mesh();
        m.SetVertices(position);
        m.SetNormals(normal);
        m.SetColors(color);
        m.SetIndices(indices, MeshTopology.Points, 0, false);
        split_target.GetComponent<MeshFilter>().mesh = m;
    }

    public void ShowNormalClustered(ref List<List<Vector3>> normal_clustered, ref List<List<Vector3>> position_clustered)
    {
        List<Vector3> normal = new List<Vector3>();
        List<Vector3> color_clustered = new List<Vector3>();
        foreach (var plane in normal_clustered)
        {
            int count = 0;
            Vector3 joint_normal = new Vector3();
            foreach (var n in plane)
            {
                normal.Add(n);
                joint_normal += n;
                count += 1;
            }
            color_clustered.Add(Vector3.Normalize(joint_normal / count));
            // Debug.LogFormat("COLOR {0}", color_clustered[color_clustered.Count - 1].ToString("f4"));
        }

        List<Vector3> position = new List<Vector3>();
        foreach (var plane in position_clustered)
        {
            foreach (var p in plane)
            {
                position.Add(p);
            }
        }

        List<Color> color = new List<Color>();
        for (int i = 0; i < color_clustered.Count; ++i)
        {
            for (int j = 0; j < normal_clustered[i].Count; ++j)
            {
                color.Add(new Color(Mathf.Abs(color_clustered[i].x),
                                    Mathf.Abs(color_clustered[i].y),
                                    Mathf.Abs(color_clustered[i].z)));
            }
        }

        ShowNormal(ref normal, ref position, ref color);
    }

    public void NormalClustering(ref List<Vector3> normal, ref List<Vector3> position, ref Vector3 center, ref Vector3 forward)
    {
        if (normal.Count == 0) return;

        float threshold = Mathf.Cos((float)(threshold_angle / 180.0 * Mathf.PI));

        List<int> normal_count = new List<int>();       // 每个平面有几条法线
        List<Vector3> plane_normal = new List<Vector3>();       // 每个平面的法线
        List<Vector3> plane_center = new List<Vector3>();       // 平面的中心
        List<Vector3> accumulate_plane_normal = new List<Vector3>();    
        List<List<Vector3>> normal_cluster = new List<List<Vector3>>();     // 属于每个平面的法线
        List<List<Vector3>> position_cluster = new List<List<Vector3>>();   // 属于每个平面的位置
        

        plane_normal.Add(normal[0]);
        plane_center.Add(position[0]);
        accumulate_plane_normal.Add(normal[0]);
        normal_count.Add(1);
        normal_cluster.Add(new List<Vector3>() { normal[0] });
        position_cluster.Add(new List<Vector3>() { position[0] });

        for (int i = 1; i < normal.Count; ++i)
        {
            Vector3 current_normal = normal[i];
            Vector3 current_position = position[i];
            int cloest_index = 0;
            float cloest_cos_val = Vector3.Dot(current_normal, plane_normal[0]);
            for (int j = 1; j < plane_normal.Count; ++j)
            {
                float t = Vector3.Dot(current_normal, plane_normal[j]);
                if (t > cloest_cos_val)
                {
                    cloest_cos_val = t;
                    cloest_index = j;
                }
            }
            // Debug.LogFormat("min cos val {0}, index {1}", cloest_cos_val, cloest_index);
            if (cloest_cos_val > threshold)
            {
                accumulate_plane_normal[cloest_index] += current_normal;
                normal_count[cloest_index] += 1;
                plane_center[cloest_index] += current_position;
                normal_cluster[cloest_index].Add(current_normal);
                position_cluster[cloest_index].Add(current_position);
                // plane_normal[cloest_index] = Vector3.Normalize(accumulate_plane_normal[cloest_index]);
            } 
            else
            {
                normal_count.Add(1);
                plane_normal.Add(current_normal);
                plane_center.Add(current_position);
                normal_cluster.Add(new List<Vector3>() { current_normal });
                position_cluster.Add(new List<Vector3>() { current_position });
                accumulate_plane_normal.Add(current_normal);
            }
        }

        int max_plane = 0, max_normal_count = normal_count[0];
        for (int i = 1; i < normal_count.Count; ++i)
        {
            if (normal_count[i] > max_normal_count)
            {
                max_normal_count = normal_count[i];
                max_plane = i;
            }
        }

        center = plane_center[max_plane] / max_normal_count;
        forward = accumulate_plane_normal[max_plane] / max_normal_count;
        float step = 0.01f;
        while (!globalUtils.GetPointVisibility(center))
        {
            center += step * forward;
            step *= 2;
        }
        center -= (step / 2.0f) * forward;
        while (!globalUtils.GetPointVisibility(center))
        {
            center += 0.01f * forward;
        }

        Debug.LogFormat("plane count {0}", plane_normal.Count);

        // ShowNormalClustered(ref normal_cluster, ref position_cluster);

        // GameObject ttt = Instantiate(sphere);
        // ttt.transform.position = plane_center[max_plane] / max_normal_count;
    }

    public void SplitCPU(List<Vector3> points)
    {
        float xmin = float.MaxValue, xmax = float.MinValue,
            ymin = float.MaxValue, ymax = float.MinValue;

        List<Vector3> plane_points = new List<Vector3>();
        foreach (Vector3 p in points)
        {
            Vector3 plane_p = globalUtils.MWorldToScreenPointDepth(p);
            plane_points.Add(plane_p);
            xmin = Mathf.Max(Mathf.Min(xmin, plane_p.x), 0.0f);
            xmax = Mathf.Min(Mathf.Max(xmax, plane_p.x), Screen.width);
            ymin = Mathf.Max(Mathf.Min(ymin, plane_p.y), 0.0f);
            ymax = Mathf.Min(Mathf.Max(ymax, plane_p.y), Screen.height);
        }

        int split_part_index = 0, m_vertices = 50000, t_vertices = 0;   //当前子物体Index,每个子物体的最大顶点数
        List<List<Vector3>> vertices = new List<List<Vector3>>();
        List<List<Color>> color = new List<List<Color>>();
        Vector3 obj_center = new Vector3();

        List<Vector3> normal = new List<Vector3>();
        List<Vector3> world_pos = new List<Vector3>();

        float d = -1;
        vertices.Add(new List<Vector3>()); color.Add(new List<Color>());
        for (int i = (int)xmin; i < xmax; ++i)
        {
            for (int j = (int)ymin; j < ymax; ++j)
            {
                if (pointInPolygon(new Vector2(i, j), plane_points))
                {
                    d = globalUtils.GetDepth(i, j);
                    float smooth_d = globalUtils.GetSmoothDepth(i, j);                    
                    if (d == 1.0f) continue;

                    // i dont know why
                    Color t = globalUtils.GetColor(i, j);
                    Color c = new Color(Mathf.Pow(t.r, 2.2f), Mathf.Pow(t.g, 2.2f), Mathf.Pow(t.b, 2.2f), 1.0f);

                    // if (c.r < 0.03f && c.b < 0.07f && c.g < 0.12f && c.g > 1.8f * c.b && c.g > 2.5f * c.r) continue;

                    if (c.r < 0.15f && c.b < 0.25f && c.g < 0.25f && (c.g > 4 * c.r && c.g > c.b)) continue;
                    Debug.Log(c);


                    if (test_picture) {
                        float gray = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
                        c = picture_color * c.grayscale;
                    }
                    

                    Vector3 world_p = globalUtils.MScreenToWorldPointDepth(new Vector3(i, j, d));
                    Vector3 smooth_world_p = globalUtils.MScreenToWorldPointDepth(new Vector3(i, j, d));
                    Vector3 normal_p = globalUtils.GetNormal(i, j, smooth_world_p);

                    normal.Add(normal_p);
                    // Debug.LogFormat("normal p {0}", normal_p.ToString("f4"));
                    world_pos.Add(smooth_world_p);

                    obj_center += smooth_world_p; 
                    ++t_vertices;

                    vertices[split_part_index].Add(smooth_world_p);
                    color[split_part_index].Add(c);

                    if (vertices[split_part_index].Count == m_vertices)
                    {
                        vertices.Add(new List<Vector3>()); color.Add(new List<Color>());
                        split_part_index++;
                    }
                }
            }
        }

        obj_center /= t_vertices;
        GameObject split_father = new GameObject("SplitRoot");
        split_father.transform.position = obj_center;
        for (int i = 0; i < vertices.Count; ++i)
        {
            List<Vector3> v = vertices[i];
            List<Color> c = color[i];
            GameObject t = globalUtils.CreateNewObjUsingVertices(ref v, ref c, "Splitpart" + i.ToString());
            t.transform.parent = split_father.transform;
        }

        GameObject outline = new GameObject("outline");
        outline.transform.position = obj_center;
        for (int i = 0; i < vertices.Count; ++i)
        {
            List<Vector3> v = vertices[i];
            List<Color> c = new List<Color>();
            for (int j = 0; j < v.Count; j++)
            {
                c.Add(outline_color);
            }
            GameObject t = globalUtils.CreateNewObjUsingVertices(ref v, ref c, "Splitpart" + i.ToString());
            t.transform.parent = outline.transform;
        }
        outline.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
        outline.transform.parent = split_father.transform;
        split_father.AddComponent<NoMshObjOutline>();
        split_father.GetComponent<NoMshObjOutline>().g = outline;
        split_father.GetComponent<NoMshObjOutline>().bias = 0.001f;
        // GameObject 

        /*Vector3 center = new Vector3(), forward = new Vector3();
        NormalClustering(ref normal, ref world_pos, ref center, ref forward);
        GameObject axes = Instantiate(AxesPrefab);
        axes.transform.position = center;
        axes.transform.up = forward;
        axes.transform.parent = split_father.transform;*/
    }

    void SplitGPU(List<Vector3> points)
    {

    }

    bool IntersectHorizontalRight(Vector2 start_p, Vector3 line_p1, Vector3 line_p2)
    {
        float x1 = line_p1.x, y1 = line_p1.y,
           x2 = line_p2.x, y2 = line_p2.y,
           x = start_p.x, y = start_p.y;

        if (Mathf.Abs(x1 - x2) < 0.0001) return ((y1 < y && y < y2) || (y2 < y && y < y1)) && (x <= x1 || x <= x2);
        if (Mathf.Abs(y1 - y2) < 0.0001) return false;

        // x = ky + b
        float k = (x2 - x1) / (y2 - y1);
        float b = (x1 * y2 - x2 * y1) / (y2 - y1);
        float intersect_x = k * y + b;
        return ((x1 < intersect_x && intersect_x < x2) || (x2 < intersect_x && intersect_x < x1)) && (x < intersect_x);
    }

    bool IntersectHorizontalRight(Vector2 start_p, float k, float b, Vector3 line_p1, Vector3 line_p2)
    {
        float x1 = line_p1.x, y1 = line_p1.y,
           x2 = line_p2.x, y2 = line_p2.y,
           x = start_p.x, y = start_p.y;

        if (Mathf.Abs(x1 - x2) < 0.0001) return ((y1 < y && y < y2) || (y2 < y && y < y1)) && (x <= x1 || x <= x2);
        if (Mathf.Abs(y1 - y2) < 0.0001) return false;

        float intersect_x = k * y + b;
        return ((x1 < intersect_x && intersect_x < x2) || (x2 < intersect_x && intersect_x < x1)) && (x < intersect_x);
    }

    void Calculkb(Vector3 line_p1, Vector3 line_p2, ref float k, ref float b)
    {
        float x1 = line_p1.x, y1 = line_p1.y,
           x2 = line_p2.x, y2 = line_p2.y;

        if (Mathf.Abs(y1 - y2) > 0.0001)
        {
            k = (x2 - x1) / (y2 - y1);
            b = (x1 * y2 - x2 * y1) / (y2 - y1);
        } 
    }

    bool pointInPolygon(Vector2 p, List<Vector3> polygon_points)
    {
        List<float> k = new List<float>();
        List<float> b = new List<float>();
        for (int i = 0; i < polygon_points.Count - 1; ++i)
        {
            float tk = 0, tb = 0;
            Calculkb(polygon_points[i], polygon_points[i + 1], ref tk, ref tb);
            k.Add(tk); b.Add(tb);
        }

        int intersetion_count = 0;
        for (int i = 0; i < polygon_points.Count - 1; ++i)
        {
            Vector3 line_p1 = polygon_points[i],
                line_p2 = polygon_points[i + 1];

            if (IntersectHorizontalRight(p, k[i], b[i], line_p1, line_p2)) 
                intersetion_count++;
        }
        return (intersetion_count % 2 == 1);
    }
}
