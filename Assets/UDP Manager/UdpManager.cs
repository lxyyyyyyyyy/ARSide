using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Xml;
using System.Collections.Generic;

public class UdpManager : MonoBehaviour
{
    public Int32 listenPort = 8585;

    private UdpClient udpClient;
    private IPEndPoint ipEndPoint;
    private Thread receiveThread;
    private List<ControlledObject> controlledObjects;

    void Awake()
    {
        controlledObjects = new List<ControlledObject>();

        udpClient  = new UdpClient( listenPort );
        ipEndPoint = new IPEndPoint( IPAddress.Any, 0 );

        receiveThread = new Thread( new ThreadStart(ReceiveThreadMethod) );
        receiveThread.Start();
    }
    public void ReceiveThreadMethod()
    {
        while(true)
        {
            byte[] receivedBytes = udpClient.Receive(ref ipEndPoint);
            ParseData( receivedBytes );
        }
    }

    public void ParseData( byte[] udpPacketData )
    {
        lock( controlledObjects )
        {
            controlledObjects.Clear();

            string xmlString = Encoding.UTF8.GetString( udpPacketData );
            XmlDocument xmlDocument = new XmlDocument();

            try
            {
                xmlDocument.LoadXml( xmlString );
            }
            catch
            {
                Debug.Log( "Received packet not XML format: " + xmlString );
                return;
            }

            XmlNode root = xmlDocument.FirstChild;

            if( root.Name != "UpdateObject" )
            {
                Debug.Log( "Wrong format in received XML packet: " + xmlString );
                return;
            }

            foreach( XmlNode objectNode in root.ChildNodes )
            {
                ControlledObject controlledObject = new ControlledObject();
                controlledObjects.Add( controlledObject );

                controlledObject.name = objectNode.Attributes.Item(0).Value;

                foreach( XmlNode propertyNode in objectNode.ChildNodes )
                {
                    if( propertyNode.Name == "Position" )
                    {
                        controlledObject.position.x  = -float.Parse( propertyNode.ChildNodes.Item(0).InnerText );
                        controlledObject.position.y  =  float.Parse( propertyNode.ChildNodes.Item(1).InnerText );
                        controlledObject.position.z  =  float.Parse( propertyNode.ChildNodes.Item(2).InnerText );
                        controlledObject.positionSet =  true;  
                    }
                    else if( propertyNode.Name == "Orientation" )
                    {
                        controlledObject.orientation.x  =  float.Parse( propertyNode.ChildNodes.Item(0).InnerText );
                        controlledObject.orientation.y  = -float.Parse( propertyNode.ChildNodes.Item(1).InnerText );
                        controlledObject.orientation.z  = -float.Parse( propertyNode.ChildNodes.Item(2).InnerText );
                        controlledObject.orientation.w  =  float.Parse( propertyNode.ChildNodes.Item(3).InnerText );
                        controlledObject.orientationSet =  true;
                    }
                    else
                    {
                        controlledObject.floatProperties.Add( propertyNode.Name, float.Parse( propertyNode.InnerText ) );
                    }
                }
            }
        }
    }

    void Update()
    {
        lock( controlledObjects )
        {
            foreach( ControlledObject controlledObject in controlledObjects )
            {
                GameObject gameObject = GameObject.Find( controlledObject.name );

                if( gameObject )
                {
                    if( controlledObject.positionSet    ) gameObject.transform.position = controlledObject.position;
                    if( controlledObject.orientationSet ) gameObject.transform.rotation = controlledObject.orientation;

                    foreach( KeyValuePair<string, float> kvp in controlledObject.floatProperties )
                    {
                        gameObject.SendMessage( kvp.Key, kvp.Value, SendMessageOptions.DontRequireReceiver );
                    }
                }
            }
            controlledObjects.Clear();
        }
    }

}