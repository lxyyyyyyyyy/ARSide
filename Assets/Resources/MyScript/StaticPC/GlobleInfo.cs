using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// �ֲ�ģʽ��right��ʾ���֣�left��ʾ����
/// </summary>
public enum HandMode { right, left }

/// <summary>
/// ������Ϣ
/// </summary>
public struct RayInfo
{
    public Vector3 startPoint;
    public Vector3 endPoint;
    public bool isActive;
    public HandMode handMode;

}

/// <summary>
/// ���ܱ�ʶ���߶Σ����ھ�̬���Ƴ���
/// </summary>
public struct SegmentInfo
{
    public Vector3 startPoint;
    public Vector3 endPoint;
}


/// <summary>
/// ���ܱ�ʶ����ת��ʶ�Ͱ�ѹ��ʶ�����ھ�̬���Ƴ���
/// </summary>
public struct SymbolInfo
{
    public Vector3 up;
    public Vector3 position;
}

/// <summary>
/// �����Զ����ɫ ��Ϣ�ṹ�壬Ŀǰֻ��ѡ���ɫ�󶨵��������
/// </summary>
public struct CreateMMOCharacterMessage : NetworkMessage
{
    public CameraMode mode;
}

/// <summary>
/// �����������壬ֻ���� AR�˴����������϶���������ʵ����ƥ��
/// </summary>
public struct CreateEnvironmentMessage : NetworkMessage
{
    public int startNumber;
    public int endNumber;
}

/// <summary>
/// ����SmartSign����
/// </summary>
public struct CreateSmartSignMessage : NetworkMessage
{
    public int smartSignNumber;
}

/// <summary>
/// ���ģʽ��AR�������VR���
/// </summary>
public enum CameraMode { AR, VR }

// public enum SymbolMode { ARROW=0,SPLIT,ROTATE,PRESS}
public enum SymbolMode { ARROW = 0, SPLIT, Axes}

/// <summary>
/// [VR�˴������м���]�����ı�ʶ��Ϣ���ݽṹ
/// DPC:dynamic point cloud VR calculate only
/// </summary>
public struct DPCArrow
{
    public int index; // ��Arrow��ͬ���б��е��±�
    // ��ʼ����
    public Vector3 startPoint;// �߶����
    public Vector3 endPoint; // �߶��յ�
    // �����ڵ��ؼ���
    public List<Vector3[]> curvePointList; // ʵʱ����
    // �µ�
    public List<Vector3[]> originPointList;
    public bool startPointVisibility;
};

/// <summary>
/// �������
/// </summary>
public struct CameraParams
{
    public Vector3 position;
    public Quaternion rotation;
}

public struct DPCSymbol
{
    public int index;
    // ��ʼ����
    public Vector3 up;
    public Vector3 position;
    //  �����ڵ��ؼ���
    public Vector3 up_new;
    public Vector3 position_new;
}

public enum ServerNumber
{
    SERVER1 = 0, SERVER2, SERVER3, SERVER4
}

public static class GlobleInfo
{
    public static CameraMode ClientMode;
    public static ServerNumber CurentServer = 0;
    public static bool isReceiveStateChanged = false;
}

public struct DPCSplitMesh
{
    public int index;
    public Vector3 center;
    public List<List<Vector3>> vertices;
    public List<List<Color>> color;
}

public struct DPCSplitPosture
{
    public int index;
    public bool valid;      // ����AR���Ƿ�������Ⱦ
    public Vector3 position;
    public Quaternion rotation;
    public int correspondingLineIndex;
}

public struct DPCAxes
{
    public int index;
    public Vector3 init_position;
    public Quaternion init_rotation;
    public Vector3 end_position;
    public Quaternion end_rotation;
    public int correspondingLineIndex;
}
