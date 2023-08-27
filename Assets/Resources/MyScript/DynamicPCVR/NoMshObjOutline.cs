using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoMshObjOutline : MonoBehaviour
{
    public float bias;
    public GameObject g;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!g) return;
        g.transform.position = transform.position + bias * Vector3.Normalize(transform.position - Camera.main.transform.position);
    }
}
