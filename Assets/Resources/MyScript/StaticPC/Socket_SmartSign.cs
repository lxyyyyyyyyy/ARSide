using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System;

public class Socket_SmartSign : MonoBehaviour
{
    public string BackEndIP;
    public int BackEndPort;
    private bool connectToBackEnd;

    private Socket VREndSocket;

    private GameObject plyModel;

    // straight Line
    public GameObject[] straightLineCurve;
    private LineRenderer[] straightLineCurveRender; // 从straightLineCurve中获取
    public Material straightLineMaterial; // 曲线材质
    public float straightLineThickness = 0.01f; // 曲线的粗细


    public GameObject[] sphere;
    private byte[] buffer;
    private Stack<string> msg_stack;

    private int interval;

    // qinwen code
    public Mirror_MyController mirror_MyController;
    public SegmentInfo currentSegment;
    public bool isStartConnect = false;
    

    void Awake()
    {
        if (GlobleInfo.ClientMode.Equals(CameraMode.VR)) { return; }
        connectToBackEnd = false;
        VREndSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    }

    // Start is called before the first frame update
    void Start()
    {
        if (GlobleInfo.ClientMode.Equals(CameraMode.VR)) { return; }

        plyModel = GameObject.Find("Environment/QinwenTable"); // 获取场景模型
        
        // 生成三个带renderer的物体
        // 2022-03-11
        straightLineCurve = new GameObject[]
        {
            CreateNewLine("curve_1"),
            CreateNewLine("curve_2"),
            CreateNewLine("curve_3")
        };

        straightLineCurveRender = new LineRenderer[] {
            straightLineCurve[0].GetComponent<LineRenderer>(),
            straightLineCurve[1].GetComponent<LineRenderer>(),
            straightLineCurve[2].GetComponent<LineRenderer>()
        };

        Debug.Log("AR端已启动!");
        try
        {
            Debug.Log("开始连接后端---------- " + BackEndIP + ":" + BackEndPort);
            VREndSocket.Connect(new IPEndPoint(IPAddress.Parse(BackEndIP), BackEndPort)); //配置服务器IP与端口  
            connectToBackEnd = true;
            Debug.Log("连接后端成功");

            Thread receiveThread = new Thread(ReceiveMessage);
            receiveThread.Start();
        }
        catch
        {
            Debug.Log("连接后端失败");
            
        }        

        buffer = new byte[20000];
        msg_stack = new Stack<string>(0);
        interval = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (GlobleInfo.ClientMode.Equals(CameraMode.VR)) { return; }
        if (isStartConnect)
        {
            if (!connectToBackEnd || interval++ < 100)
            {
                return;
            }
            interval = 0;
            // camera position
            string cameraPos = "camera pos:" + Vec3toStr(Camera.main.transform.position);
            SendMessageToBackEnd(cameraPos);

            // camera front
            // string cameraFront = "camera front" + Vec3toStr(UNITY_MATRIX_V[2].xyz);
            string cameraFront = "camera front:" + Vec3toStr(Camera.main.transform.forward);
            SendMessageToBackEnd(cameraFront);

            // camera up
            // string cameraUp = "camera up" + Vec3toStr(UNITY_MATRIX_V[1].xyz);
            string cameraUp = "camera up:" + Vec3toStr(Camera.main.transform.up);
            SendMessageToBackEnd(cameraUp);

            // camera right
            // string cameraRight = "camera right" + Vec3toStr(UNITY_MATRIX_V[0].xyz);
            string cameraRight = "camera right:" + Vec3toStr(Camera.main.transform.right);
            SendMessageToBackEnd(cameraRight);

            // ply model rotation
            string plyRot = "ply rot:" + Vec3toStr(plyModel.transform.eulerAngles);
            SendMessageToBackEnd(plyRot);

            // ply model position
            string plyPos = "ply pos:" + Vec3toStr(plyModel.transform.position);
            SendMessageToBackEnd(plyPos);

            // straight line p1
            string straightLineP1Pos = "straight line p1 pos:" + Vec3toStr(currentSegment.startPoint);
            SendMessageToBackEnd(straightLineP1Pos);

            // straight line p2
            string straightLineP2Pos = "straight line p2 pos:" + Vec3toStr(currentSegment.endPoint);
            SendMessageToBackEnd(straightLineP2Pos);

            if (msg_stack.Count != 0)
            {
                ParseMessage();
            }
        }
    }

    public void SendMessageToBackEnd(string msg)
    {
        // 向服务器发送数据，需要发送中文则需要使用Encoding.UTF8.GetBytes()，否则会乱码
        VREndSocket.Send(Encoding.UTF8.GetBytes("#" + msg));
        // Debug.Log("向服务器发送消息：" + msg);
    }

    void ReceiveMessage()
    {
        while (true)
        {
            int receiveBytes = VREndSocket.Receive(buffer);
            if (receiveBytes == 0)
            {
                Debug.Log("No Message!");
                return;
            }
            string recvStr = Encoding.UTF8.GetString(buffer, 0, receiveBytes);
            Debug.Log(recvStr);
            string[] singleStr = recvStr.Split('%');
            foreach (string s in singleStr)
            {
                if (s.Length == 0)
                    continue;

                if (s[0] != '#')
                {      // 被上一次的截断
                    string last = msg_stack.Pop();
                    System.Diagnostics.Debug.Assert(last[0] == '#');
                    last += s;
                    msg_stack.Push(last);
                }
                else
                {
                    // Debug.Log(s);
                    msg_stack.Push(s);
                }
            }
        }

    }

    void ParseMessage()
    {
        string incompleteMsg = "";
        while (msg_stack.Count != 0)
        {
            string t = msg_stack.Pop();
            if (t[t.Length - 1] != '$')
            {   // 不完整
                incompleteMsg = t;
                continue;
            }

            // Debug.Log(t);
            System.Diagnostics.Debug.Assert(t[0] == '#');
            System.Diagnostics.Debug.Assert(t[t.Length - 1] == '$');
            string substr = t.Substring(1, t.Length - 2);
            string[] threeCurve = substr.Split('@');

            System.Diagnostics.Debug.Assert(threeCurve.Length == 3);
            for (int i = 0; i < 3; ++i)
            {
                string[] pos = threeCurve[i].Split(' ');
                int pointCount = Convert.ToInt32(pos[0]);
                straightLineCurveRender[i].SetVertexCount(pointCount);
                Vector3[] points = new Vector3[pointCount];

                System.Diagnostics.Debug.Assert(pos.Length == pointCount + 1);
                int index = 1;
                while (index <= pointCount)
                {
                    string[] p = pos[index].Split(',');
                    points[index - 1] = new Vector3(Convert.ToSingle(p[0]), Convert.ToSingle(p[1]), Convert.ToSingle(p[2]));
                    index++;
                }

                straightLineCurveRender[i].SetPositions(points);
            }

            break;
        }

        while (msg_stack.Count != 0)
        {
            msg_stack.Pop();
        }

        if (incompleteMsg.Length != 0)
        {
            msg_stack.Push(incompleteMsg);
        }
    }

    string Vec3toStr(Vector3 _vec)
    {
        string precision = "0.000";
        return _vec.x.ToString(precision) + "," + _vec.y.ToString(precision) + "," + _vec.z.ToString(precision);
    }

    string QuatoStr(Quaternion _q)
    {
        string precision = "0.000";
        return _q.x.ToString(precision) + "," + _q.y.ToString(precision) + "," + _q.z.ToString(precision) + "," + _q.w.ToString(precision);
    }

    private GameObject CreateNewLine(string objName)
    {
        GameObject lineObj = new GameObject(objName);
        lineObj.transform.SetParent(this.transform);
        LineRenderer curveRender = lineObj.AddComponent<LineRenderer>();
        curveRender.material = straightLineMaterial;
        // 设置颜色的没写
        curveRender.startWidth = straightLineThickness;
        curveRender.endWidth = straightLineThickness;
        return lineObj;
    }

}
