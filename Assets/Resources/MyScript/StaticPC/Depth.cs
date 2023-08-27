using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Depth : MonoBehaviour
{
    // Start is called before the first frame update
    private Camera m_Camera;
    public RenderTexture depthTexture;
    public Texture2D depthTextureRead;
    public Material Mat;

    void Start()
    {
        m_Camera = gameObject.GetComponent<Camera>();
        // 手动设置相机，让它提供场景的深度信息
        // 这样我们就可以在shader中访问_CameraDepthTexture来获取保存的场景的深度信息
        // float depth = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, uv)); 获取某个像素的深度值
        m_Camera.depthTextureMode = DepthTextureMode.Depth;
        depthTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.RFloat);
        depthTexture.enableRandomWrite = true;

        depthTextureRead = new Texture2D(Screen.width, Screen.height, TextureFormat.RFloat, true);
    }


    void OnPostRender()
    {
        Debug.Log("onpostrender");
        RenderTexture source = m_Camera.activeTexture;
        Graphics.Blit(source, depthTexture, Mat);

        RenderTexture currentActiveRT = RenderTexture.active;
        // Set the supplied RenderTexture as the active one
        RenderTexture.active = depthTexture;
        depthTextureRead.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        RenderTexture.active = currentActiveRT;
    }

    void Update()
    {
        // Debug.Log("depth");
        this.transform.position = Camera.main.transform.position;
        this.transform.rotation = Camera.main.transform.rotation;
    }
}