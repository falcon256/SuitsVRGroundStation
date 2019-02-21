using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
public class NetworkMeshSource : MonoBehaviour
{

    private static NetworkMeshSource networkMeshSourceSingleton = null;
    public static NetworkMeshSource getSingleton() { return networkMeshSourceSingleton; }

    public int serverPort = 32123;
    public int targetPort = 32123;
    public string targetIP = "192.168.137.1";
    public UdpClient udpClient = null;
    //public Mesh testMesh = null;
    // Start is called before the first frame update
    void Start()
    {
        if(networkMeshSourceSingleton !=null)
        {
            Destroy(this);
            return;
        }
        networkMeshSourceSingleton = this;
        udpClient = new UdpClient(targetPort);
        udpClient.Connect(targetIP, serverPort);
       
    }

    public void sendMesh(Mesh m, Vector3 location, Quaternion rotation)
    {
        List<Mesh> meshes = new List<Mesh>();
        meshes.Add(m);
        //byte[] vectBytes = BitConverter.GetBytes(location);
        //byte[] quatBytes = BitConverter.GetBytes(rotation);
        byte[] bytes = new byte[12+16]; // 4 bytes per float

        Buffer.BlockCopy(bytes, 0, BitConverter.GetBytes(location.x), 0, 4);
        Buffer.BlockCopy(bytes, 4, BitConverter.GetBytes(location.y), 0, 4);
        Buffer.BlockCopy(bytes, 8, BitConverter.GetBytes(location.z), 0, 4);
        Buffer.BlockCopy(bytes, 12, BitConverter.GetBytes(rotation.x), 0, 4);
        Buffer.BlockCopy(bytes, 16, BitConverter.GetBytes(rotation.y), 0, 4);
        Buffer.BlockCopy(bytes, 20, BitConverter.GetBytes(rotation.z), 0, 4);
        Buffer.BlockCopy(bytes, 24, BitConverter.GetBytes(rotation.w), 0, 4);


        Byte[] sendData = Combine(bytes,HoloToolkit.Unity.SimpleMeshSerializer.Serialize(meshes));
        //Byte[] sendData = Encoding.ASCII.GetBytes("WEEEEEE!");
        udpClient.Send(sendData, sendData.Length);
        //Debug.Log("Sent: " + sendData.Length + " bytes");
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

    //stolen useful code.
    public static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] ret = new byte[first.Length + second.Length];
        Buffer.BlockCopy(first, 0, ret, 0, first.Length);
        Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
        return ret;
    }
    public static byte[] Combine(byte[] first, byte[] second, byte[] third)
    {
        byte[] ret = new byte[first.Length + second.Length + third.Length];
        Buffer.BlockCopy(first, 0, ret, 0, first.Length);
        Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
        Buffer.BlockCopy(third, 0, ret, first.Length + second.Length,
                         third.Length);
        return ret;
    }
}
