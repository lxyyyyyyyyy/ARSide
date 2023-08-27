using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TipMessageVRA : MonoBehaviour
{
    public Exp myExp;
    public GameObject Text;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!myExp)
        {
            if (!GameObject.Find("SmartSignA(Clone)/VR")) return;

            myExp = GameObject.Find("SmartSignA(Clone)/VR").GetComponent<Exp>();
        }

        if (!(Text.activeSelf ^ myExp.GetVRExpState()))
        {
            Text.SetActive(!myExp.GetVRExpState());
        }
    }
}
