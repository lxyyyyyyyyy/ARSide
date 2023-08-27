using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using Valve.VR;
using UnityEngine.XR;

/*
	Documentation: https://mirror-networking.gitbook.io/docs/components/network-manager
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkManager.html
*/

public class MyNewNetworkManager : NetworkManager
{
    [Header("Player Mode")]
    [Tooltip("从AR和VR两种模式中选择仅只选择一个")]
    public bool vrMode = false;
    public bool arMode = false;
    [Header("Sync Environment")]
    [Tooltip("同步场景的开始序号和结束序号")]
    public int syncEnv_start=0;
    public int syncEnv_end = 0;

    #region Unity Callbacks


    public override void OnValidate()
    {
        base.OnValidate(); // 设置最大连接数和检察玩家预制体
        if (vrMode == arMode)
        {
            Debug.LogError("客户端必须从AR和VR中选择一个模式");
        }
    }

    /// <summary>
    /// 在服务器和客户端上运行
    /// 触发此函数时，网络未初始化
    /// </summary>
    public override void Awake()
    {
        base.Awake();
        GlobleInfo.ClientMode = vrMode ? CameraMode.VR : CameraMode.AR;
    }

    /// <summary>
    /// Runs on both Server and Client
    /// Networking is NOT initialized when this fires
    /// </summary>
    public override void Start()
    {
        base.Start();
    }

    /// <summary>
    /// Runs on both Server and Client，会在项目启动后一直运行
    /// </summary>
    public override void LateUpdate()
    {
        base.LateUpdate();
    }

    /// <summary>
    /// Runs on both Server and Client
    /// </summary>
    public override void OnDestroy()
    {
        base.OnDestroy();
    }

    #endregion

    #region Start & Stop

    /// <summary>
    /// Set the frame rate for a headless server.
    /// <para>Override if you wish to disable the behavior or set your own tick rate.</para>
    /// </summary>
    public override void ConfigureHeadlessFrameRate()
    {
        base.ConfigureHeadlessFrameRate();
    }

    /// <summary>
    /// called when quitting the application by closing the window / pressing stop in the editor
    /// </summary>
    public override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
    }

    #endregion

    #region Scene Management

    /// <summary>
    /// This causes the server to switch scenes and sets the networkSceneName.
    /// <para>Clients that connect to this server will automatically switch to this scene. This is called automatically if onlineScene or offlineScene are set, but it can be called from user code to switch scenes again while the game is in progress. This automatically sets clients to be not-ready. The clients must call NetworkClient.Ready() again to participate in the new scene.</para>
    /// </summary>
    /// <param name="newSceneName"></param>
    public override void ServerChangeScene(string newSceneName)
    {
        base.ServerChangeScene(newSceneName);
    }

    /// <summary>
    /// Called from ServerChangeScene immediately before SceneManager.LoadSceneAsync is executed
    /// <para>This allows server to do work / cleanup / prep before the scene changes.</para>
    /// </summary>
    /// <param name="newSceneName">Name of the scene that's about to be loaded</param>
    public override void OnServerChangeScene(string newSceneName) 
    { 
    }



    /// <summary>
    /// Called on the server when a scene is completed loaded, when the scene load was initiated by the server with ServerChangeScene().
    /// </summary>
    /// <param name="sceneName">The name of the new scene.</param>
    public override void OnServerSceneChanged(string sceneName) {
    }


    /// <summary>
    /// Called from ClientChangeScene immediately before SceneManager.LoadSceneAsync is executed
    /// <para>This allows client to do work / cleanup / prep before the scene changes.</para>
    /// </summary>
    /// <param name="newSceneName">Name of the scene that's about to be loaded</param>
    /// <param name="sceneOperation">Scene operation that's about to happen</param>
    /// <param name="customHandling">true to indicate that scene loading will be handled through overrides</param>
    public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling) {
    }

    /// <summary>
    /// Called on clients when a scene has completed loaded, when the scene load was initiated by the server.
    /// <para>Scene changes can cause player objects to be destroyed. The default implementation of OnClientSceneChanged in the NetworkManager is to add a player object for the connection if no player object exists.</para>
    /// </summary>
    public override void OnClientSceneChanged()
    {
       base.OnClientSceneChanged();
    }

    #endregion

    #region Server System Callbacks

    /// <summary>
    /// Called on the server when a new client connects.
    /// <para>Unity calls this on the Server when a Client connects to the Server. Use an override to tell the NetworkManager what to do when a client connects to the server.</para>
    /// </summary>
    /// <param name="conn">Connection from client.</param>
    public override void OnServerConnect(NetworkConnection conn) {

        // NetworkServer.Spawn(Instantiate(spawnPrefabs[2]), conn);
        
    }

    /// <summary>
    /// Called on the server when a client is ready.
    /// <para>The default implementation of this function calls NetworkServer.SetClientReady() to continue the network setup process.</para>
    /// </summary>
    /// <param name="conn">Connection from client.</param>
    public override void OnServerReady(NetworkConnection conn)
    {
        base.OnServerReady(conn);
    }



    /// <summary>
    /// Called on the server when a client adds a new player with ClientScene.AddPlayer.
    /// <para>The default implementation for this function creates a new player object from the playerPrefab.</para>
    /// </summary>
    /// <param name="conn">Connection from client.</param>
    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        base.OnServerAddPlayer(conn);
    }

    /// <summary>
    /// Called on the server when a client disconnects.
    /// <para>This is called on the Server when a Client disconnects from the Server. Use an override to decide what should happen when a disconnection is detected.</para>
    /// </summary>
    /// <param name="conn">Connection from client.</param>
    public override void OnServerDisconnect(NetworkConnection conn)
    {
        base.OnServerDisconnect(conn);
    }

    /// <summary>
    /// Called on server when transport raises an exception.
    /// <para>NetworkConnection may be null.</para>
    /// </summary>
    /// <param name="conn">Connection of the client...may be null</param>
    /// <param name="exception">Exception thrown from the Transport.</param>
    public override void OnServerError(NetworkConnection conn, Exception exception) { }

    #endregion

    #region Client System Callbacks

    /// <summary>
    /// 当client连接到server时被调用
    /// <para>此函数的默认实现将client设置为ready，并添加一个player。重写该函数以指示client连接时发生的情况。</para>
    /// </summary>
    public override void OnClientConnect()
    {
        base.OnClientConnect();

        CreateMMOCharacterMessage characterMessage = new CreateMMOCharacterMessage
        {
            mode = GlobleInfo.ClientMode
        };

        NetworkClient.Send(characterMessage);

        if (GlobleInfo.ClientMode.Equals(CameraMode.VR))
        {
            if (syncEnv_end >= syncEnv_start)
            {
                CreateEnvironmentMessage syncObjMessage = new CreateEnvironmentMessage
                {
                    startNumber = syncEnv_start,
                    endNumber = syncEnv_end
                };
                NetworkClient.Send(syncObjMessage);
            }
            
        }

        if (GlobleInfo.ClientMode.Equals(CameraMode.VR))
        {
            CreateSmartSignMessage smartSignMessage = new CreateSmartSignMessage
            {
                smartSignNumber = 2
            };
            NetworkClient.Send(smartSignMessage);
        }


        if (GlobleInfo.ClientMode.Equals(CameraMode.AR))
        {
            CreateEnvironmentMessage syncDepthCameraMessage = new CreateEnvironmentMessage
            {
                startNumber = 3,
                endNumber = 3
            };
            NetworkClient.Send(syncDepthCameraMessage);
        }
    }

    /// <summary>
    /// Called on clients when disconnected from a server.
    /// <para>This is called on the client when it disconnects from the server. Override this function to decide what happens when the client disconnects.</para>
    /// </summary>
    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
    }

    /// <summary>
    /// Called on clients when a servers tells the client it is no longer ready.
    /// <para>This is commonly used when switching scenes.</para>
    /// </summary>
    public override void OnClientNotReady() { }

    /// <summary>
    /// Called on client when transport raises an exception.</summary>
    /// </summary>
    /// <param name="exception">Exception thrown from the Transport.</param>
    public override void OnClientError(Exception exception) { }

    #endregion

    #region Start & Stop Callbacks

    // Since there are multiple versions of StartServer, StartClient and StartHost, to reliably customize
    // their functionality, users would need override all the versions. Instead these callbacks are invoked
    // from all versions, so users only need to implement this one case.

    /// <summary>
    /// This is invoked when a host is started.
    /// <para>StartHost has multiple signatures, but they all cause this hook to be called.</para>
    /// </summary>
    public override void OnStartHost() { }

    /// <summary>
    /// 这在服务器启动时调用，包括主机启动时。
    /// <para>StartServer has multiple signatures, but they all cause this hook to be called.</para>
    /// </summary>
    public override void OnStartServer() {
        base.OnStartServer();
        NetworkServer.RegisterHandler<CreateMMOCharacterMessage>(OnCreateCharacter);
        NetworkServer.RegisterHandler<CreateEnvironmentMessage>(OnSyncObject);
        NetworkServer.RegisterHandler<CreateSmartSignMessage>(OnSmartSign);
    }

    /// <summary>
    /// This is invoked when the client is started.
    /// </summary>
    public override void OnStartClient() {
    }

    /// <summary>
    /// This is called when a host is stopped.
    /// </summary>
    public override void OnStopHost() { }

    /// <summary>
    /// This is called when a server is stopped - including when a host is stopped.
    /// </summary>
    public override void OnStopServer() { }

    /// <summary>
    /// This is called when a client is stopped.
    /// </summary>
    public override void OnStopClient() { }

    public void OnCreateCharacter(NetworkConnection conn, CreateMMOCharacterMessage message)
    {

        // 根据message的信息选择playerPrefab 
        GameObject clientPlayerPrefab = null;
        if (message.mode == CameraMode.AR)
        {
            clientPlayerPrefab = spawnPrefabs[0];
        }
        else if (message.mode == CameraMode.VR)
        {
            clientPlayerPrefab = spawnPrefabs[1];
        }
        if (clientPlayerPrefab == null) { Debug.LogError("客户端玩家预制体没有加载成功"); }
        
        
        // 调用该函数以使用当前gameObject 作为主要控制器
        NetworkServer.AddPlayerForConnection(conn, Instantiate(clientPlayerPrefab));
    }

    private void OnSyncObject(NetworkConnection conn, CreateEnvironmentMessage message)
    {
        for (int i = message.startNumber;i<= message.endNumber; i++)
        {
            NetworkServer.Spawn(Instantiate(spawnPrefabs[i]), conn);
        }
    }

    private void OnSmartSign(NetworkConnection conn,CreateSmartSignMessage message)
    {
        NetworkServer.Spawn(Instantiate(spawnPrefabs[message.smartSignNumber]), conn);
    }

    #endregion


}
