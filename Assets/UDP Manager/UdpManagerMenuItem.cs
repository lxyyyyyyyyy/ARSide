using UnityEditor;
using UnityEngine;
using UnityEditor;using UnityEngine;

public class UdpManagerMenuItem
{
    [MenuItem("GameObject/UDP Manager", false, 10)]
    public static void CreateTextArea()
    {
        GameObject gameObject = GameObject.Find( "UdpManager" );

        if( gameObject )
        {
            if( !EditorUtility.DisplayDialog
            (
                "There is already a UDP Manager in the scene.",
                "Are you sure you want to place it?", "Place", "Do Not Place"
            ) )
            {
                return;
            }
        }

        GameObject addedGameObject = new GameObject("UdpManager");
        addedGameObject.AddComponent<UdpManager>();
    }
}