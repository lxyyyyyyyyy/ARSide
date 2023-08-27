using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;
using TurboJpegWrapper;


public class DisplayPointCloud : MonoBehaviour
{
    [DllImport("UnityC++.dll")]
    static unsafe extern void DepthToPointCloud(byte[] depth, float scale, int width, int height, float fx, float fy, float ppx, float ppy, int sequ, Vector3* vertices);
    [DllImport("Decompressor.dll")]
    static unsafe extern void DecompressRVL(byte[] input, byte[] output, int numPixels, ref int* Pbuffer, ref int word, ref int nibblesWritten);

    //[System.Serializable]
    public class TextureEvent : UnityEvent<Texture> { }
    [Space]
    public TextureEvent textureBinding;
    [HideInInspector]
    public Texture2D rgb_texture;

    //delegate()
    public delegate void NewFrame(Transform displayPointCloudTransform);
    //event  
    public static event NewFrame SendFrame;

    public Shader shader;

    private int NibblesWritten = 0;
    private int Word = 0 ;
    private int[] PBuffer;
    private int NumPixels = 921600;  //1280x720
    private int stride;
    TJDecompressor decompressor = new TJDecompressor();


    private bool new_value = false;

    private Queue frame_queue = new Queue();

    private readonly object frameLock = new object();
    private readonly object markerLock = new object();

    private bool marker_recognition;


    private int color_compressed;
    public int Color_compressed
    {
        get { return color_compressed; }
        set
        {
            color_compressed = value;
            //Debug.Log("color_compressed: " + color_compressed);
        }
    }

    private int depth_compressed;
    public int Depth_compressed
    {
        get { return depth_compressed; }
        set
        {
            depth_compressed = value;
            //Debug.Log("depth_compressed: " + depth_compressed);
        }
    }



    private byte[] frame;
    public byte[] Frame
    {
        get { return frame; }
        set
        {
            frame = value;
            // Debug.Log("Frame size: " + Frame.Length);
        }
    }

    private byte[] depthFrame;
    public byte[] DepthFrame
    {
        get { return depthFrame; }
        set
        {
            depthFrame = value;
            // Debug.Log("DepthFrame size: " + DepthFrame.Length);
        }
    }


    private byte[] colorFrame;
    public byte[] ColorFrame
    {
        get { return colorFrame; }
        set
        {
            colorFrame = value;
            // Debug.Log("ColorFrame size: " + ColorFrame.Length);
        }
    }

    //private readonly int height = 480;
    //private readonly int width = 848;
    private int height;
    public int Height
    {
        get { return height; }
        set
        {
            height = value;
            // Debug.Log("Height: " + height);
        }
    }

    private int width;
    public int Width
    {
        get { return width; }
        set
        {
            width = value;
            // Debug.Log("Width: " + width);
        }
    }

    private float fx;
    public float Fx
    {
        get { return fx; }
        set
        {
            fx = value;
            // Debug.Log("Fx: " + fx);
        }
    }

    private float fy;
    public float Fy
    {
        get { return fy; }
        set
        {
            fy = value;
            // Debug.Log("Fy: " + fy);
        }
    }

    private float ppx;
    public float Ppx
    {
        get { return ppx; }
        set
        {
            ppx = value;
            // Debug.Log("Ppx: " + ppx);
        }
    }

    private float ppy;
    public float Ppy
    {
        get { return ppy; }
        set
        {
            ppy = value;
            // Debug.Log("Ppy: " + ppy);
        }
    }

    private int frameRawDimension;
    public int FrameRawDimension
    {
        get { return frameRawDimension; }
        set
        {
            frameRawDimension = value;
            // Debug.Log("FrameRawDimension: " + FrameRawDimension);
        }
    }

    private int remainingByte;
    public int RemainingByte
    {
        get { return remainingByte; }
        set
        {
            remainingByte = value;
            // Debug.Log("RemainingByte: " + RemainingByte);
        }
    }

    private int id;
    public int ID
    {
        get { return id; }
        set
        {
            id = value;
            // Debug.Log("PointCloud ID: " + id);
        }
    }

    [HideInInspector]
    public GameObject Marker;

    private Vector3[] vertices;
    private Mesh mesh;

    //public UnityEvent UnityEvent;

    public class ColorEvent : UnityEvent<Color> { }
    public ColorEvent colorEvent;

    Material VertexMaterial_1;
    private bool new_frame = false;

    public bool isRenderFrame = true;

    private void OnEnable()
    {
        //UIManager.ButtonPressed += OnMarkerRecognitionStarted;
    }

    private void OnDisable()
    {
        //UIManager.ButtonPressed -= OnMarkerRecognitionStarted;
    }

    private void Awake()
    {
        //DontDestroyOnLoad(this);
    }

    // Start is called before the first frame update
    void Start()
    {
        marker_recognition = false;

        //ResetMesh(width, height);
        VertexMaterial_1 = new Material(shader);

        GetComponent<MeshRenderer>().material = VertexMaterial_1;

        textureBinding = new TextureEvent();
        textureBinding.AddListener(MyAction);

    }

    void MyAction(Texture t)
    {
        VertexMaterial_1.mainTexture = t;
    }

    // Update is called once per frame
    void Update()
    {
        byte[] frame = null;
        lock (frameLock)
        {
          
            if (frame_queue.Count > 0)
            {
                frame = (byte[])frame_queue.Dequeue();
                new_value = true;
                
            }
        }
        if (new_value)
        {

            // frame 1 depth
            byte[] SerialNumber = new byte[13];
          
            Buffer.BlockCopy(frame, 0, SerialNumber, 0, 13);
            int Depth_compressed = BitConverter.ToInt32(frame, 13);
            int Color_compressed = BitConverter.ToInt32(frame, 17);

            Dictionary<string, int> Sequence = new Dictionary<string, int>();
            Sequence.Add("043422252343", 0);
            Sequence.Add("125322062463", 1);
            Sequence.Add("125322061236", 2);
            Sequence.Add("125322064652", 3);
            Sequence.Add("125322063996", 4);
            Sequence.Add("125322061904", 5);
            Sequence.Add("125322064404", 6);
            Sequence.Add("125322063997", 7);
            

            byte[] DepthFrameCompressed = new byte[Depth_compressed];
            byte[] ColorFrameCompressed = new byte[Color_compressed];

            Buffer.BlockCopy(frame, 21, DepthFrameCompressed, 0, DepthFrameCompressed.Length);
              unsafe
            {
                int* Pbuffer = null;
                DecompressRVL(DepthFrameCompressed, DepthFrame, NumPixels, ref Pbuffer, ref Word, ref NibblesWritten);


            }

            Buffer.BlockCopy(frame, DepthFrameCompressed.Length + 21, ColorFrameCompressed, 0, ColorFrameCompressed.Length);

          

            ColorFrame = decompressor.Decompress(ColorFrameCompressed, TJPixelFormat.RGB, TJFlags.FastDct, out width, out height, out stride);
           
            unsafe
            {
                fixed (Vector3* pVector = vertices)
                {

                    foreach(string key in Sequence.Keys)
                    {
                     

                        if (System.Text.Encoding.ASCII.GetString(SerialNumber).CompareTo(key) == 0)
                        {
                         
                            DepthToPointCloud(DepthFrame, 0.001f, Width, Height, Fx, Fy, Ppx, Ppy, Sequence[key], pVector);

                        }

                    }

                }

            }
             

            mesh.vertices = vertices;

            try
            {
                rgb_texture.LoadRawTextureData(ColorFrame);
                rgb_texture.Apply();

                textureBinding.Invoke(rgb_texture);

                lock (markerLock)
                {
                    if (marker_recognition)
                    {
                        transform.parent = null;
                        transform.localRotation = Quaternion.identity;
                        transform.position = Vector3.zero;

                        Marker.transform.parent = null;
                        Marker.transform.localRotation = Quaternion.identity;
                        Marker.transform.position = Vector3.zero;

                        SendFrame?.Invoke(transform); 
                        marker_recognition = false;
                    }
                }

            }
            catch (Exception e)
            {
                Debug.Log(e);
            }

            new_value = false;

        }

    }

    public void newFrame(ref byte[] frame)
    {
       
        lock (frameLock)
        {
            frame_queue.Enqueue(frame);
        }
    }

    public void newFrame()
    {
        if (!isRenderFrame) { return; }
        byte[] copy = new byte[Depth_compressed + Color_compressed + 21];
        Buffer.BlockCopy(Frame, 0, copy, 0, Depth_compressed + Color_compressed + 21);
        lock (frameLock)
        {

            frame_queue.Enqueue(copy);
        }
        if (isRenderFrame)
        {
            isRenderFrame = false;
        }
     }

    public void ResetMesh()
    {


        rgb_texture = new Texture2D(Width, Height, TextureFormat.RGB24, false, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };



        mesh = new Mesh()
        {
            indexFormat = IndexFormat.UInt32,
        };
        GetComponent<MeshFilter>().mesh = mesh;
        //vertices = new Vector3[101760];
        vertices = new Vector3[Width * Height];

        var indices = new int[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            indices[i] = i;

        var uvs = new Vector2[Width * Height];

        Array.Clear(uvs, 0, uvs.Length);
        for (int j = 0; j < Height; j++)
        {
            for (int i = Width - 1; i >= 0; i--)
            {
                uvs[i + j * Width].x = i / (float)Width;
                uvs[i + j * Width].y = j / (float)Height;
            }
        }

        mesh.MarkDynamic();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.SetIndices(indices, MeshTopology.Points, 0, false);
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10f);
    }

    private void OnMarkerRecognitionStarted(bool value)
    {
        lock (markerLock)
        {
            this.marker_recognition = value;
        }
    }


}
