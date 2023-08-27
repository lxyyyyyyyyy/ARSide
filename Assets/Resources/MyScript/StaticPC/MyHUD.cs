using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Network/MyHUD")]
    [RequireComponent(typeof(NetworkManager))]
    public class MyHUD : MonoBehaviour
    {
        NetworkManager manager;
        public GameObject mixedRealityToolkit;
        public GameObject mixedRealityPlayspace;
        public GameObject mixedRealitySceneContent;
        public GameObject cameraAR;

        public int offsetX;
        public int offsetY;
        public bool isHost, isClient, isServer = false;
        public bool isAR, isVR = false;
        public bool isDebugMode = false;

        private void Awake()
        {
            manager = GetComponent<NetworkManager>(); // 获取NetworkManager组件

            //mixedRealityToolkit = GameObject.Find("MixedRealityToolkit");
            //mixedRealityPlayspace = GameObject.Find("MixedRealityPlayspace");
            //mixedRealitySceneContent = GameObject.Find("MixedRealitySceneContent");
            //cameraAR = GameObject.Find("[CameraRig]");
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10 + offsetX, 40 + offsetY, 215, 9999));
            if (!NetworkClient.isConnected && !NetworkServer.active)
            {
                StartButtons();
            }
            else
            {
                StatusLabels();
            }

            // client ready
            if (NetworkClient.isConnected && !NetworkClient.ready)
            {
                if (GUILayout.Button("Client Ready"))
                {
                    NetworkClient.Ready();
                    if (NetworkClient.localPlayer == null)
                    {
                        NetworkClient.AddPlayer();
                    }
                }
            }

            StopButtons();

            GUILayout.EndArea();
        }

        void StartButtons()
        {
            isHost=GUILayout.Toggle(isHost, "Host(Server+CLient)");
            isClient=GUILayout.Toggle(isClient, "Client");
            isServer=GUILayout.Toggle(isServer, "Server");
            isAR=GUILayout.Toggle(isAR, "AR");
            isVR=GUILayout.Toggle(isVR, "VR");

            if (!NetworkClient.active)
            {
                if (GUILayout.Button("Start"))
                {
                    if (Application.platform != RuntimePlatform.WebGLPlayer)
                    {
                        if (isHost)
                        {
                            SetPlayerMode();
                            manager.StartHost();
                        }
                    }
                    // Client + IP
                    GUILayout.BeginHorizontal();
                    if (isClient)
                    {
                        SetPlayerMode();
                        manager.StartClient();
                    }
                    // This updates networkAddress every frame from the TextField
                    manager.networkAddress = GUILayout.TextField(manager.networkAddress);
                    GUILayout.EndHorizontal();

                    // Server Only
                    if (Application.platform == RuntimePlatform.WebGLPlayer)
                    {
                        // cant be a server in webgl build
                        GUILayout.Box("(  WebGL cannot be server  )");
                    }
                    else if (isServer)
                    {
                        SetPlayerMode();
                        manager.StartServer();
                    }
                }
            }
            else
            {
                // Connecting
                GUILayout.Label($"Connecting to {manager.networkAddress}..");
                if (GUILayout.Button("Cancel Connection Attempt"))
                {
                    manager.StopClient();
                }
            }
        }

        void StatusLabels()
        {
            // host mode
            // display separately because this always confused people:
            //   Server: ...
            //   Client: ...
            if (NetworkServer.active && NetworkClient.active)
            {
                GUILayout.Label($"<b>Host</b>: running via {Transport.activeTransport}");
            }
            // server only
            else if (NetworkServer.active)
            {
                GUILayout.Label($"<b>Server</b>: running via {Transport.activeTransport}");
            }
            // client only
            else if (NetworkClient.isConnected)
            {
                GUILayout.Label($"<b>Client</b>: connected to {manager.networkAddress} via {Transport.activeTransport}");
            }
        }

        void StopButtons()
        {
            // stop host if host mode
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                if (GUILayout.Button("Stop Host"))
                {
                    manager.StopHost();
                }
            }
            // stop client if client-only
            else if (NetworkClient.isConnected)
            {
                if (GUILayout.Button("Stop Client"))
                {
                    manager.StopClient();
                }
            }
            // stop server if server-only
            else if (NetworkServer.active)
            {
                if (GUILayout.Button("Stop Server"))
                {
                    manager.StopServer();
                }
            }
            
      
            cameraAR.SetActive(true);
            mixedRealityToolkit.SetActive(true);
            mixedRealityPlayspace.SetActive(true);
            mixedRealitySceneContent.SetActive(true);
        }

        void SetPlayerMode()
        {
            if (isDebugMode)
            {
                Debug.Log("开始设置游戏模式");
            }
            if (isAR&&!isVR)
            {
                
                cameraAR.SetActive(false);

            }
            if (isVR&&!isAR)
            {
                if (isDebugMode)
                {
                    Debug.Log("开始设置VR模式");
                }
                mixedRealityToolkit.SetActive(false);
                mixedRealityPlayspace.SetActive(false);
                mixedRealitySceneContent.SetActive(false);
                if (isDebugMode)
                {
                    Debug.Log(mixedRealityToolkit.activeSelf);
                }
                
            }
            if (isServer)
            {
                cameraAR.SetActive(false);
                mixedRealityToolkit.SetActive(false);
                mixedRealityPlayspace.SetActive(false);
                mixedRealitySceneContent.SetActive(false);

            }
        }

    }
}

