using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

// State object for reading client data asynchronously  
public class MyStateObject
{
    // Client  socket.  
    public Socket workSocket = null;
    // Size of receive buffer.  s
    public const int BufferSize = 65536;
    // Receive buffer.  
    public byte[] buffer = new byte[BufferSize];

}

// Class declaration
[System.Serializable]
public class myEvent : UnityEvent<int> { }

public class MyTCPServer : MonoBehaviour
{
    private myEvent OnDevicesArrived;

    public GameObject prefabPointCloud;

    private MyStateObject state;
    private TcpListener TCPListener;

    private Queue frame_queue = new Queue();

    private readonly object frameLock = new object();
    private readonly object aliveLock = new object();
    private bool running = true;

    private byte[] FRAME;

    private bool devices_parameters_arrived = false;
    private byte[] devices_parameters = new byte[104]; // 4 (message_length) + 4 (n_devices) + n_devices * (6 * 4), n_devices <= 4
    private bool header_parameters_arrived = false;

    private int message_length = 0;
    private int n_devices = 0;

    private object pointcloud_lock = new object();
    private bool pointcloud_instantiated = false;

    public DisplayPointCloud[] displayPointClouds;

    private Socket client;
    private Socket listener;

    private int totBytesRead = 0;
    private int pointcloud_index = 0;
    private bool camera_index_arrived = false;

    private PointCloudController pointCloudController;
    private string hostip;
    public int serverPort;
    public ServerNumber myServerNumber;
    public bool isMeReceiveFrame = true;

    private void Awake()
    {
        //DontDestroyOnLoad(this);
    }

    // Start is called before the first frame update
    void Start()
    {
        pointCloudController = GetComponentInParent<PointCloudController>();
        hostip = pointCloudController.hostIP;

        Debug.Log(myServerNumber+" host ip is:" + hostip);
        IPAddress ipAddress = IPAddress.Parse(hostip);
        //IPAddress ipAddress = IPAddress.Parse("10.42.0.8");
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, serverPort); // 11000 ~ 11003

        // Create a TCP/IP socket.  
        listener = new Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

        listener.Bind(localEndPoint);
        listener.Listen(100);

        listener.BeginAccept(
                   new AsyncCallback(AcceptCallback),
                   listener);

        myServerNumber = (ServerNumber)(serverPort - 11000);
    }

    private void Update()
    {

        lock (pointcloud_lock)
        {
            if (devices_parameters_arrived && !pointcloud_instantiated)
            {
                int index = 8;

                bool FRAME_CREATED = false;
                for (int i = 0; i < n_devices; i++)
                {
                    int width = BitConverter.ToInt32(devices_parameters, index);
                    index += 4;
                    int heigth = BitConverter.ToInt32(devices_parameters, index);
                    index += 4;
                    float fx = BitConverter.ToSingle(devices_parameters, index);
                    index += 4;
                    float fy = BitConverter.ToSingle(devices_parameters, index);
                    index += 4;
                    float ppx = BitConverter.ToSingle(devices_parameters, index);
                    index += 4;
                    float ppy = BitConverter.ToSingle(devices_parameters, index);
                    index += 4;

                    GameObject pointcloud = Instantiate(prefabPointCloud);
                    pointcloud.name = "PointCloud_" + i;
                    pointcloud.transform.parent = transform;
                    pointcloud.transform.localPosition = Vector3.zero;
                    pointcloud.transform.localRotation = Quaternion.identity;

                    DisplayPointCloud displayPointCloud = pointcloud.GetComponent<DisplayPointCloud>();
                    displayPointCloud.ID = i;
                    displayPointCloud.Width = width;
                    displayPointCloud.Height = heigth;
                    displayPointCloud.Fx = fx;
                    displayPointCloud.Fy = fy;
                    displayPointCloud.Ppx = ppx;
                    displayPointCloud.Ppy = ppy;
                    displayPointCloud.FrameRawDimension = (width * heigth * (3 + 2));
                    displayPointCloud.RemainingByte = displayPointCloud.FrameRawDimension;

                    displayPointCloud.Frame = new byte[displayPointCloud.FrameRawDimension /*+ MyStateObject.BufferSize*/];
                    displayPointCloud.DepthFrame = new byte[displayPointCloud.Width * displayPointCloud.Height * 2];
                    displayPointCloud.ColorFrame = new byte[displayPointCloud.Width * displayPointCloud.Height * 3];

                    displayPointCloud.Marker = new GameObject();
                    displayPointCloud.Marker.name = pointcloud.name + "_Marker";
                    //displayPointCloud.Marker.transform.parent = displayPointCloud.transform;
                    displayPointCloud.Marker.transform.localPosition = Vector3.zero;
                    displayPointCloud.Marker.transform.localRotation = Quaternion.identity;

                    displayPointCloud.ResetMesh();

                    displayPointClouds[i] = displayPointCloud;

                    if (!FRAME_CREATED)
                    {
                        FRAME = new byte[width * heigth * (3 + 2) + MyStateObject.BufferSize];
                        FRAME_CREATED = true;
                    }


                }

                pointcloud_instantiated = true;

                Send(client); // notify cameras
            }
        }
    }

    public void AcceptCallback(IAsyncResult ar)
    {
        // Get the socket that handles the client request.  
        Socket listener = (Socket)ar.AsyncState;
        Socket handler = listener.EndAccept(ar);

        // Create the state object.  
        state = new MyStateObject();
        state.workSocket = handler;

        Debug.Log(myServerNumber + "is connected");

        handler.BeginReceive(state.buffer, 0, MyStateObject.BufferSize, 0,
            new AsyncCallback(ReadCallback), state);
    }

    private void ReadCallback(IAsyncResult ar)
    {
        // Debug.LogWarning(myServerNumber + ": read call back");
        String content = String.Empty;

        // Retrieve the state object and the handler socket  
        // from the asynchronous state object.  
        MyStateObject state = (MyStateObject)ar.AsyncState;
        Socket handler = state.workSocket;

        // Read data from the client socket.
        int bytesRead = handler.EndReceive(ar);
        // 如果状态改变指令发起了，并且当前是自己
        //if (GlobleInfo.isReceiveStateChanged && myServerNumber == GlobleInfo.CurentServer)
        //{
        //    Debug.Log(myServerNumber + ": " + GlobleInfo.isReceiveStateChanged + ", " + GlobleInfo.CurentServer);
        //    isMeReceiveFrame = !isMeReceiveFrame;
        //    GlobleInfo.isReceiveStateChanged = false; // 谁执行完谁恢复状态
        //    Debug.Log(myServerNumber+":"+isMeReceiveFrame+", "+GlobleInfo.isReceiveStateChanged);
        //}

        //&& isMeReceiveFrame
        if (bytesRead > 0)
        {
            //Debug.Log("The byte read is " + bytesRead);
            #region devices_parameter
            if (!devices_parameters_arrived) // devices parameters not arrived yet
            {
                client = handler;

                Buffer.BlockCopy(state.buffer, 0, devices_parameters, totBytesRead, bytesRead);
                totBytesRead += bytesRead;

                if (totBytesRead >= 8 && !header_parameters_arrived)  // message_length + n_devices
                {
                    message_length = BitConverter.ToInt32(devices_parameters, 0);
                    n_devices = BitConverter.ToInt32(devices_parameters, 4);

                    Debug.Log("message_length: " + message_length);
                    Debug.Log("n_devices: " + n_devices);

                    displayPointClouds = new DisplayPointCloud[n_devices];

                    OnDevicesArrived?.Invoke(n_devices);

                    header_parameters_arrived = true;
                }
                if (totBytesRead >= message_length && header_parameters_arrived) // tutti i parametri sono arrivati
                {

                    lock (pointcloud_lock)
                    {
                        devices_parameters_arrived = true;
                    }

                    totBytesRead = 0;
                }
            }
            #endregion

            #region frame
            else
            {
                Buffer.BlockCopy(state.buffer, 0, FRAME, totBytesRead, bytesRead);
                totBytesRead += bytesRead;

                if (!camera_index_arrived && totBytesRead >= 25) // camera index has arrived
                {
                    pointcloud_index = BitConverter.ToInt32(FRAME, 0);

                    displayPointClouds[pointcloud_index].Depth_compressed = BitConverter.ToInt32(FRAME, 17);
                    displayPointClouds[pointcloud_index].Color_compressed = BitConverter.ToInt32(FRAME, 21);

                    //displayPointClouds[pointcloud_index].Frame = new byte[displayPointClouds[pointcloud_index].Depth_compressed + displayPointClouds[pointcloud_index].Color_compressed + 8];
                    camera_index_arrived = true;

                }

                if (camera_index_arrived && totBytesRead >= displayPointClouds[pointcloud_index].Depth_compressed + displayPointClouds[pointcloud_index].Color_compressed + 25)
                {


                    Buffer.BlockCopy(FRAME, 4, displayPointClouds[pointcloud_index].Frame, 0, displayPointClouds[pointcloud_index].Depth_compressed + displayPointClouds[pointcloud_index].Color_compressed + 21);


                    //Debug.Log("I am here");
                    // rendering
                    displayPointClouds[pointcloud_index].newFrame();

                    int diff = totBytesRead - (displayPointClouds[pointcloud_index].Depth_compressed + displayPointClouds[pointcloud_index].Color_compressed + 25); // is there data of the next frame?

                    totBytesRead = 0;
                    camera_index_arrived = false;

                    if (diff > 0) // next frame data
                    {
                        Buffer.BlockCopy(state.buffer, bytesRead - diff, FRAME, 0, diff);
                        totBytesRead += diff;
                    }
                }

            }
            #endregion
        }
        handler.BeginReceive(state.buffer, 0, MyStateObject.BufferSize, 0,
       new AsyncCallback(ReadCallback), state);

    }

    private void Send(Socket client)
    {
        int reply = -1;
        byte[] byteData = BitConverter.GetBytes(reply);

        // Begin sending the data to the remote device.  
        client.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), client);

    }

    private void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket client = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.  
            int bytesSent = client.EndSend(ar);
            Console.WriteLine("Sent {0} bytes to server.", bytesSent);

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    private void OnDestroy()
    {
        Debug.Log("Stopping Server");

        if (client != null)
            client.Close();
        if (listener != null)
            listener.Close();
    }
}

