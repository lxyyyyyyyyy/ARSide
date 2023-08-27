using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;

public class TestExp : MonoBehaviour
{
    public Button NextButton;
    public Button PreviousButton;
    private bool first = true;
    // 用于预先设好的
    public int currentLineIndex = 0, currentSymbolIndex = 0, currentIndex = -1;
    // 用于记录
    private int currentRecordIndex = 0;
    public Vector3[] p_list;
    public Vector3[] pos, normal;
    public Vector3[] data1, data2;
    public enum OperationType
    {
        AddLine = 0, AddRotate, AddPress
    };
    public List<OperationType> operation_list;

    public int is_wrong;
    public int is_completion;
    private DateTime pre_time;
    private Vector3 pre_pos;
    private float total_dis = 0; 
    public String exper_name;

    public enum ExpType
    {
        CG = 0, EG1
    };
    public ExpType expType = ExpType.CG;

    public GameObject parentObj;
    public GameObject follow_pointcloud_prefab;
    public List<GameObject> follow_pointclout;

    public bool init = false;
    public bool manualDraw = true;
    private TestMirror mirrorController;

    // Start is called before the first frame update
    void Start()
    {
        mirrorController = GameObject.Find("Script").GetComponent<TestMirror>();

        p_list = new Vector3[] 
        {
            new Vector3(-0.8914f, 1.2208f, 0.7485f),
            new Vector3(-1.2592f, 0.9778f, 0.9263f),

            new Vector3(-0.8615f, 0.6689f, 1.0866f),
            new Vector3(-1.1714f, 1.0549f, 0.8040f),

            new Vector3(-0.8105f, 0.9391f, 1.1222f),
            new Vector3(-0.9351f, 1.2320f, 0.8100f),

            new Vector3(-0.7321f, 0.6440f, 1.1815f),
            new Vector3(-0.9302f, 1.3050f, 1.1248f),

            new Vector3(-1.1551f, 0.5618f, 0.9025f),
            new Vector3(-0.7402f, 1.1825f, 1.0262f),

            new Vector3(-0.7587f, 1.0851f, 1.1459f),
            new Vector3(-0.8723f, 1.2223f, 0.7031f),
        };


        operation_list = new List<OperationType> 
        {
            OperationType.AddLine,
            OperationType.AddLine,
            OperationType.AddLine,
            OperationType.AddLine,
            OperationType.AddLine,
            OperationType.AddLine,
        };


        for (int i = 0; i < operation_list.Count; ++i)
        {
            Vector3 t1, t2;
            if (operation_list[i] == OperationType.AddLine)
            {
                t1 = p_list[currentLineIndex * 2];
                t2 = p_list[currentLineIndex * 2 + 1];
                currentLineIndex++;
            }
            else
            {
                t1 = pos[currentSymbolIndex];
                t2 = normal[currentSymbolIndex] + t1;
                currentSymbolIndex++;
            }

            GameObject tp1 = GameObject.Instantiate(follow_pointcloud_prefab, parentObj.transform);
            tp1.transform.position = t1;
            tp1.name = "data" + i.ToString() + "p1";
            follow_pointclout.Add(tp1);

            GameObject tp2 = GameObject.Instantiate(follow_pointcloud_prefab, parentObj.transform);
            tp2.transform.position = t2;
            tp2.name = "data" + i.ToString() + "p2";
            follow_pointclout.Add(tp2);
        }


        NextButton = GameObject.Find("ExpObj/Canvas/Next").GetComponent<Button>();
        // NextButton.onClick.AddListener(ActivateNextLine);
        NextButton.onClick.AddListener(ActiveNextOperation);

        PreviousButton = GameObject.Find("ExpObj/Canvas/Previous").GetComponent<Button>();
        // PreviousButton.onClick.AddListener(ActivatePreviousLine); 
        PreviousButton.onClick.AddListener(ActivatePreviousOperation);
    }

    // Update is called once per frame
    void Update()
    {
        total_dis += Vector3.Distance(pre_pos, Camera.main.transform.position);
        pre_pos = Camera.main.transform.position;

        if (!init)
        {
            init = true;
            GameObject.Find("PointCloud").transform.position = new Vector3(0.643f, 1.396f, 0.472f);
            GameObject.Find("PointCloud").transform.eulerAngles = new Vector3(60.807f, -79.34f, 97.225f);
        }

        if (GameObject.Find("Script").GetComponent<TestLineAR>().origin_line)
        {
            expType = ExpType.CG;
        } 
        else
        {
            expType = ExpType.EG1;
        }
    }

    void ActivateNextLine()
    {
        if (currentLineIndex >= p_list.Length / 2)
        {
            return;
        }

        if (currentLineIndex != 0)
        {
            TimeSpan abs = DateTime.Now.Subtract(pre_time).Duration();
            writeFile(currentLineIndex.ToString() + "," + abs.TotalMilliseconds.ToString());
        }
        pre_time = DateTime.Now;

        if (mirrorController.syncArrowList.Count > 0)
        {
            mirrorController.syncArrowList.RemoveAt(0);
        }

        mirrorController.syncArrowList.Add(new DPCArrow()
        {
            index = 0,
            startPoint = follow_pointclout[currentLineIndex * 2].transform.position,
            endPoint = follow_pointclout[currentLineIndex * 2 + 1].transform.position,
            curvePointList = new List<Vector3[]>()
        });
        currentLineIndex++;
    }

    void ActivatePreviousLine()
    {
        if (currentLineIndex <= 1) return;

        if (mirrorController.syncArrowList.Count > 0)
        {
            mirrorController.syncArrowList.RemoveAt(0);
        }

        currentLineIndex -= 2;
        mirrorController.syncArrowList.Add(new DPCArrow()
        {
            index = 0,
            startPoint = follow_pointclout[currentLineIndex * 2].transform.position,
            endPoint = follow_pointclout[currentLineIndex * 2 + 1].transform.position,
            curvePointList = new List<Vector3[]>()
        });
        currentLineIndex++;
    }

    void ActiveNextOperation()
    {
        if (currentIndex >= operation_list.Count - 1) return;

        if (GameObject.Find("PointCloud/TCPserver4/PointCloud_1"))
        {
            GameObject.Find("PointCloud/TCPserver4/PointCloud_1").GetComponent<DisplayPointCloud>().isRenderFrame = true;
        }

        RecodeData();
        currentIndex++;
        SwitchData();
    }
 
    void ActivatePreviousOperation()
    {
        if (currentIndex <= 0) return;
        
        currentIndex--;
        SwitchData();
    }

    void SwitchData()
    {
        Clear();

        OperationType current_type = operation_list[currentIndex];
        if (current_type == OperationType.AddLine)
        {
            mirrorController.syncArrowList.Add(new DPCArrow()
            {
                index = 0,
                startPoint = follow_pointclout[currentIndex * 2].transform.position,
                endPoint = follow_pointclout[currentIndex * 2 + 1].transform.position,
                curvePointList = new List<Vector3[]>(),
                originPointList = new List<Vector3[]>(),
            });
        }
        else if (current_type == OperationType.AddRotate)
        {
            mirrorController.syncRotationList.Add(new DPCSymbol()
            {
                index = 0,
                position = follow_pointclout[currentIndex * 2].transform.position,
                up = follow_pointclout[currentIndex * 2 + 1].transform.position - follow_pointclout[currentIndex * 2].transform.position
            });
        }
        else if (current_type == OperationType.AddPress)
        {
            mirrorController.syncPressList.Add(new DPCSymbol()
            {
                index = 0,
                position = follow_pointclout[currentIndex * 2].transform.position,
                up = follow_pointclout[currentIndex * 2 + 1].transform.position - follow_pointclout[currentIndex * 2].transform.position
            });
        }
    }

    public void ManualBegin()
    {
        RecodeData();
    }

    public void ManualEnd()
    {
        pre_time = DateTime.Now;
        Clear();
        first = false;
    }

    public void Clear()
    {
        /*mirrorController.syncArrowList.Clear();
        mirrorController.syncRotationList.Clear();
        mirrorController.syncPressList.Clear();
        is_wrong = 0;
        is_completion = 0;*/
    }

    public void RecodeData()
    {
        if (currentIndex >= 0 || !first)
        {
            TimeSpan abs = DateTime.Now.Subtract(pre_time).Duration();
            writeFile(currentRecordIndex.ToString() + "," + 
                abs.TotalMilliseconds.ToString() + "," + 
                total_dis + "," + is_wrong + "," + is_completion);
            currentRecordIndex++;
        }
        pre_time = DateTime.Now;
        total_dis = 0;
    }

    public void writeFile(string s)
    {
        /*string dir = "BlockData/";
        string ExpType_dir = "CG/";
        if (expType == ExpType.EG1)
        {
            ExpType_dir = "EG1/";
        } 
        string file_dir = dir + ExpType_dir + exper_name + ".csv";

        StreamWriter wf = File.AppendText(file_dir);

        wf.WriteLine(s);
        wf.Flush();    
        wf.Close();    
        
        Debug.Log("write success");*/
    }

    public void OnDestroy()
    {
        RecodeData();
    }
}
