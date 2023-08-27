using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestEverything : MonoBehaviour
{
   

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    private Vector3 intersectBox(Vector3 p1, GameObject t)
    {
        Vector3 p2 = t.transform.position;
        Bounds tAABB = new Bounds();
        Renderer[] renderers = t.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            tAABB.Encapsulate(renderers[i].bounds);
        }

        float x = tAABB.extents.x, y = tAABB.extents.y, z = tAABB.extents.z;
        float scale = 1.0f;
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

        /*
         * Test Correction of AABB Bounds
         * 
         * int index = 0;
        foreach (var v in vAABB)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);//ÀàÐÍ 
            sphere.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
            sphere.name = "sphere" + index.ToString();
            sphere.transform.position = v;

            index++;
        }*/

        // sphere
        float dis = Vector3.Distance(p1, p2);
        Vector3 dir = (p2 - p1).normalized;
        float r = Mathf.Sqrt(x * x + y * y + z * z) * 0.8f;
        Vector3 intersect_sphere = p1 + (dis - r) * dir;

        // cube
        float x_min = Mathf.Min((vAABB[0].x - p1.x) / dir.x, (vAABB[7].x - p1.x) / dir.x);
        float y_min = Mathf.Min((vAABB[0].y - p1.y) / dir.y, (vAABB[7].y - p1.y) / dir.y);
        float z_min = Mathf.Min((vAABB[0].z - p1.z) / dir.z, (vAABB[7].z - p1.z) / dir.z);
        float t_max = Mathf.Max(Mathf.Max(x_min, y_min), z_min);
        Vector3 intersect_cube = p1 + t_max * dir;

        return intersect_cube;
    }
}
