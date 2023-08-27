
using UnityEngine;
using System.Collections.Generic;

public class ControlledObject
{
    public string name;
    public Quaternion orientation;
    public bool orientationSet;
    public Vector3 position;
    public bool positionSet;
    public Dictionary<string, float> floatProperties;

    public ControlledObject()
    {
        orientation     = new Quaternion();
        orientationSet  = false;
        position        = new Vector3();
        positionSet     = false;
        floatProperties = new Dictionary<string, float>();
    }
}