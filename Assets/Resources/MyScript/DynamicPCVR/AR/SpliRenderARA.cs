using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpliRenderARA : MonoBehaviour
{
    private MirrorControllerA mirrorController;
    private List<GameObject> split_objs_father;

    public GameObject splitPrefab;

    // Start is called before the first frame update
    void Start()
    {
        mirrorController = GetComponentInParent<MirrorControllerA>();
        split_objs_father = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        if (split_objs_father.Count < mirrorController.syncSplitMeshList.Count) RenderSplitObj();
        else if (split_objs_father.Count > mirrorController.syncSplitMeshList.Count) DeleteSplitObj();

        // UpdateSplitObj();
    }

    void RenderSplitObj()
    {
        for (int i = split_objs_father.Count; i < mirrorController.syncSplitMeshList.Count; ++i)
        {
            DPCSplitPosture current_posture = mirrorController.syncSplitPosList[i];
            if (!current_posture.valid) continue; 

            DPCSplitMesh current_mesh = mirrorController.syncSplitMeshList[i];

            GameObject split_father = new GameObject("SplitRoot");
            split_father.transform.position = current_mesh.center;
            split_objs_father.Add(split_father);

            for (int j = 0; j < mirrorController.syncSplitMeshList[i].vertices.Count; ++j)
            {
                List<Vector3> v = current_mesh.vertices[j];
                List<Color> c = current_mesh.color[j];
                GameObject t = CreateNewObjUsingVertices(ref v, ref c, "Splitpart" + i.ToString());
                t.transform.parent = split_father.transform;
            }

            split_father.transform.position = current_posture.position;
            split_father.transform.rotation = current_posture.rotation;
        }
    }

    void DeleteSplitObj()
    {
        int i = split_objs_father.Count;
        while (i-- > mirrorController.syncSplitMeshList.Count)
        {
            GameObject father = split_objs_father[i];
            DestroyGameObject(father);
            split_objs_father.RemoveAt(split_objs_father.Count - 1);
        }
    }

    void UpdateSplitObj()
    {
        for (int i = 0; i < mirrorController.syncSplitPosList.Count; ++i)
        {
            DPCSplitPosture current_posture = mirrorController.syncSplitPosList[i];
            GameObject current_object = split_objs_father[i];

            current_object.transform.position = current_posture.position;
            current_object.transform.rotation = current_posture.rotation;
        }
    }

    private void DestroyGameObject(GameObject t)
    {
        int j = 0;
        while (j < t.transform.childCount)
        {
            Destroy(t.transform.GetChild(j++).gameObject);
        }
        Destroy(t);
    }

    private GameObject CreateNewObjUsingVertices(ref List<Vector3> vertices, ref List<Color> colors, string name = "", Transform father = null)
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
}
