using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
public class NetworkMeshSource : MonoBehaviour
{
    public int serverPort = 32124;
    public int targetPort = 32123;
    public string targetIP = "127.0.0.1";
    public UdpClient udpClient = null;
    public Mesh testMesh = null;
    // Start is called before the first frame update
    void Start()
    {
        udpClient = new UdpClient(32124);
        udpClient.Connect(targetIP, 32123);
        List<Mesh> meshes = new List<Mesh>();
        meshes.Add(testMesh);
        Byte[] sendData = HoloToolkit.Unity.SimpleMeshSerializer.Serialize(meshes);
        //Byte[] sendData = Encoding.ASCII.GetBytes("WEEEEEE!");
        udpClient.Send(sendData, sendData.Length);
        Debug.Log("Sent: " + sendData.Length + " bytes");
        udpClient.Close();

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDestroy()
    {
        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }
    }
}
