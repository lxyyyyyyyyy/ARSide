using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AxesRenderARA : MonoBehaviour
{
    private MirrorControllerA mirrorController;
    private List<GameObject> init_axes_father;
    private List<GameObject> end_axes_father;

    public GameObject axesPrefab;

    // Start is called before the first frame update
    void Start()
    {
        mirrorController = GetComponentInParent<MirrorControllerA>();
        init_axes_father = new List<GameObject>();
        end_axes_father = new List<GameObject>();
    }
    
    // Update is called once per frame
    void Update()
    {
        if (init_axes_father.Count < mirrorController.syncAxesList.Count) RenderAxesObj();
        else if (init_axes_father.Count > mirrorController.syncAxesList.Count) DeleteAxesObj();
        // else UpdateAxesObj();
    }

    void UpdateAxesObj()
    {
        for (int i = 0; i < mirrorController.syncAxesList.Count; ++i)
        {
            DPCAxes axes = mirrorController.syncAxesList[i];

            init_axes_father[i].transform.position = axes.init_position;
            init_axes_father[i].transform.rotation = axes.init_rotation;

            end_axes_father[i].transform.position = axes.end_position;
            end_axes_father[i].transform.rotation = axes.end_rotation;
        }
    }
    
    void RenderAxesObj()
    {
        for (int i = init_axes_father.Count; i < mirrorController.syncAxesList.Count; ++i)
        {
            DPCAxes axes = mirrorController.syncAxesList[i];

            GameObject init_axes = Instantiate(axesPrefab);
            init_axes.transform.position = axes.init_position;
            init_axes.transform.rotation = axes.init_rotation;
            init_axes_father.Add(init_axes);

            GameObject end_axes = Instantiate(axesPrefab);
            end_axes.transform.position = axes.end_position;
            end_axes.transform.rotation = axes.end_rotation;
            end_axes_father.Add(end_axes);
        }
    }

    void DeleteAxesObj()
    {
        int i = init_axes_father.Count;
        while (i-- > mirrorController.syncAxesList.Count)
        {
            GameObject axes1 = init_axes_father[i];
            DestroyGameObject(axes1);
            init_axes_father.RemoveAt(init_axes_father.Count - 1);

            GameObject axes2 = end_axes_father[i];
            DestroyGameObject(axes2);
            end_axes_father.RemoveAt(end_axes_father.Count - 1);
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
}
