using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MirrorController : NetworkBehaviour
{
    public GameObject VRController;
    public GameObject ARController;

    // These lists are maintained on the client
    public List<SegmentInfo> clientSegmentList;
    public List<SymbolInfo> clientRotationList;
    public List<SymbolInfo> clientPressList;

    #region syncvar

    [SyncVar(hook = nameof(AddNewSegmentToList))]
    private SegmentInfo segment;

    [SyncVar(hook = nameof(DeleteLastSegmentInList))]
    private int deleteSegment = 0;

    [SyncVar(hook = nameof(AddNewRotationToList))]
    private SymbolInfo rotationSymbol;

    [SyncVar(hook = nameof(AddNewPressToList))]
    private SymbolInfo pressSymbol;

    #endregion

    #region command

    [Command]
    public void CmdUpdateSegmentInfo(SegmentInfo newSegment)
    {
        Debug.Log("Server request received: Update Segment");
        segment = newSegment;
    }

    [Command]
    public void CmdUpdateRotationInfo(SymbolInfo newRotation)
    {
        Debug.Log("Server request received: Update Rotation");
        rotationSymbol = newRotation;
    }

    [Command]
    public void CmdUpdatePressInfo(SymbolInfo newPress)
    {
        Debug.Log("Server request received: Update Press");
        pressSymbol = newPress;
    }

    [Command]
    public void CmdDeleteSegmentInfo()
    {
        Debug.Log("Server request received: Delete Last Segment");
        deleteSegment++; 
    }
    #endregion

    #region hook
    public void AddNewSegmentToList(SegmentInfo oldSegment, SegmentInfo newSegment)
    {
        Debug.Log("Client request received: Add New Segment");
        clientSegmentList.Add(newSegment);
    }

    public void DeleteLastSegmentInList(int oldvar, int newvar)
    {
        Debug.Log("Client request received: Delete Last Segment");
        clientSegmentList.RemoveAt(clientSegmentList.Count - 1);
    }


    public void AddNewRotationToList(SymbolInfo oldRotation, SymbolInfo newRotation)
    {
        Debug.Log("Client request received: Add New Rotation");
        clientRotationList.Add(newRotation);
    }

    public void AddNewPressToList(SymbolInfo oldPress, SymbolInfo newPress)
    {
        Debug.Log("Client request received: Add New press");
        clientPressList.Add(newPress);
    }
    #endregion

    void Awake()
    {

        Debug.Log("Smart Sign Start");
        clientSegmentList = new List<SegmentInfo>();
        clientRotationList = new List<SymbolInfo>();
        clientPressList = new List<SymbolInfo>();

        // control subobject according to Client Mode

        if (GlobleInfo.ClientMode.Equals(CameraMode.VR))
        {
            VRController.SetActive(true);
            ARController.SetActive(false);
        }
        if (GlobleInfo.ClientMode.Equals(CameraMode.AR))
        {
            VRController.SetActive(false);
            ARController.SetActive(true);
        }


    }
}
