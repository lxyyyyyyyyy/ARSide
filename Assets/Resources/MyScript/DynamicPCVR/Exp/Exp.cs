using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;

public class Exp : MonoBehaviour
{
    // 用于计算实验数据
    private DateTime vr_start_time, ar_start_time;
    
    // 积木+装配场景都需要记录的数据

    // 积木场景需要记录的数据
    public float total_dis;
    public int is_wrong;
    // 装配场景需要记录的数据
    public List<GameObject> dot_objs;
    private string dots_init_rot, dots_end_rot;
    private Vector3 init_rotation;
    private Vector3 end_rotation;

    // 实验者姓名，
    public String exper_name;
    public enum ExpType { CG = 0, EG1 };
    public ExpType exp_type = ExpType.CG;
    public enum SceneType { BLOCK = 0, ASSEMBLY };
    public SceneType scene_type = SceneType.BLOCK;

    // 开始点一下这个进行预对齐
    public bool PointcloudAlignment = false;
    private bool initial_exp_start = false;     // 实验是否是初次启动

    private bool vrExpStart;    // Vr端操作是否开始  

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (PointcloudAlignment)
        {
            PointcloudAlignment = false;
            GameObject.Find("PointCloud").transform.position = new Vector3(0.643f, 1.396f, 0.472f);
            GameObject.Find("PointCloud").transform.eulerAngles = new Vector3(60.807f, -79.34f, 97.225f);
        }
    }


    private void writeFile(string s)
    {
        string dir = "Data/";
        string exp_dir = "Block/";
        if (scene_type == SceneType.ASSEMBLY) exp_dir = "Assembly";
        string ExpType_dir = "CG/";
        if (exp_type == ExpType.EG1) ExpType_dir = "EG1/";
        string file_dir = dir + exp_dir + ExpType_dir + exper_name + ".csv";

        StreamWriter wf = File.AppendText(file_dir);

        wf.WriteLine(s);
        wf.Flush();
        wf.Close();

        Debug.Log("write success");
    }

    public void VRBeginAREnd()
    {
        vrExpStart = true;

        vr_start_time = DateTime.Now;

        if (!initial_exp_start)
        {
            initial_exp_start = true;
            return;
        }

        dots_end_rot = "";
        foreach (var o in dot_objs) dots_end_rot += o.transform.eulerAngles.ToString("f4");
        DateTime ar_end_time = DateTime.Now;
        TimeSpan ar_ope_time = ar_end_time.Subtract(ar_start_time).Duration();
        writeFile("ar:" + "," + ar_ope_time.TotalMilliseconds.ToString() + "," + dots_init_rot + "," + dots_end_rot);
    }

    public void VREndARBegin()
    {
        vrExpStart = false;

        ar_start_time = DateTime.Now;
        dots_init_rot = "";
        foreach (var o in dot_objs) dots_init_rot += o.transform.eulerAngles.ToString("f4");

        DateTime vr_end_time = DateTime.Now;
        TimeSpan vr_ope_time = vr_end_time.Subtract(vr_start_time).Duration();
        writeFile("vr:" + "," + vr_ope_time.TotalMilliseconds.ToString() + "," + init_rotation.ToString("f4") + "," + end_rotation.ToString("f4"));
    }

    public void RecordObjInitRot(Vector3 init_rot)
    {
        init_rotation = init_rot;
    }

    public void RecordObjEndRot(Vector3 end_rot)
    {
        end_rotation = end_rot;
    }

    public bool GetVRExpState() => vrExpStart;
}
