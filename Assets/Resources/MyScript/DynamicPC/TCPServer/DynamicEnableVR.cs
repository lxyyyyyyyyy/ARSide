using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.Extras;

public class DynamicEnableVR : MonoBehaviour
{
    public SteamVR_Action_Boolean ChangeReceiveFrameState;
    public SteamVR_Action_Boolean changeCurrentServer;

    public MyTCPServer TCPServer1;
    public MyTCPServer TCPServer2;
    public MyTCPServer TCPServer3;
    public MyTCPServer TCPServer4;
    private List<MyTCPServer> myTCPServers;


    // Start is called before the first frame update
    void Start()
    {
        TCPServer1 = GameObject.Find("PointCloud(Clone)/TCPserver1").GetComponent<MyTCPServer>();
        TCPServer2 = GameObject.Find("PointCloud(Clone)/TCPserver2").GetComponent<MyTCPServer>();
        TCPServer3 = GameObject.Find("PointCloud(Clone)/TCPserver3").GetComponent<MyTCPServer>();
        TCPServer4 = GameObject.Find("PointCloud(Clone)/TCPserver4").GetComponent<MyTCPServer>();

        myTCPServers = new List<MyTCPServer>();

        myTCPServers.Add(TCPServer1);
        myTCPServers.Add(TCPServer2);
        myTCPServers.Add(TCPServer3);
        myTCPServers.Add(TCPServer4);
    }

    // Update is called once per frame
    void Update()
    {
        if (ChangeReceiveFrameState.GetStateDown(SteamVR_Input_Sources.RightHand))
        {
            Debug.Log("State Change");
            for(int i = 0; i < 4; i++)
            {
                if (myTCPServers[i].myServerNumber == GlobleInfo.CurentServer)
                {
                    for(int j = 0;j< myTCPServers[i].displayPointClouds.Length;j++)
                    {
                        Debug.Log(j+": "+myTCPServers[i].displayPointClouds[j].isRenderFrame);
                        myTCPServers[i].displayPointClouds[j].isRenderFrame = !myTCPServers[i].displayPointClouds[j].isRenderFrame;
                    }
                    Debug.Log(myTCPServers[i].myServerNumber + ": State Change");
                }
            }
            
        }
        if (changeCurrentServer.GetStateDown(SteamVR_Input_Sources.RightHand))
        {
            GlobleInfo.CurentServer = (ServerNumber)(((int)GlobleInfo.CurentServer + 1) % 4);
            Debug.Log("Now TCP Server is :"+ GlobleInfo.CurentServer);
        }
        
    }
}
