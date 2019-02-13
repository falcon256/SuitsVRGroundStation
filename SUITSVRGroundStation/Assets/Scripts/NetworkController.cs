using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

using UnityEngine;

public class NetworkController : MonoBehaviour
{
    public static NetworkController networkControllerSingleton = null;
    public int port = 32123;
    public UdpClient myUDP = null;// new UdpClient(port);
    public GameObject inMeshInstancePrefab = null;
    public Queue<HoloToolkit.Unity.SimpleMeshSerializer.MeshData> incomingMeshes = null;
    public NetworkController()
    {
        if (networkControllerSingleton != null)
            Debug.LogError("Singleton already created, tried to make a second one. Bad!");
        if (networkControllerSingleton == null)
        {
            networkControllerSingleton = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        incomingMeshes = new Queue<HoloToolkit.Unity.SimpleMeshSerializer.MeshData>();
        UdpClient myUDP = new UdpClient(port);
        Debug.Log("UDP PORT " + port + " Bound");
        myUDP.BeginReceive(DataIn, myUDP);
    }


    private static void DataIn(IAsyncResult ar)
    {
        UdpClient c = (UdpClient)ar.AsyncState;
        IPEndPoint inIPEndPoint = new IPEndPoint(IPAddress.Any, 0);
        Byte[] inBytes = c.EndReceive(ar, ref inIPEndPoint);
        Debug.Log("" + inBytes.Length + " Bytes received.");
        float x1 = System.BitConverter.ToSingle(mybyteArray, 0);
        float y1 = System.BitConverter.ToSingle(mybyteArray, 4);
        float z1 = System.BitConverter.ToSingle(mybyteArray, 8);
        float x2 = System.BitConverter.ToSingle(mybyteArray, 12);
        float y2 = System.BitConverter.ToSingle(mybyteArray, 16);
        float z2 = System.BitConverter.ToSingle(mybyteArray, 20);
        float w2 = System.BitConverter.ToSingle(mybyteArray, 24);

        Byte[] subset = new byte[inBytes.Length - 24];
        Array.Copy(inBytes, 24, subset, 0, subset.Length);

        Debug.Log("Gameobject made.");
        HoloToolkit.Unity.SimpleMeshSerializer.MeshData mesh = HoloToolkit.Unity.SimpleMeshSerializer.Deserialize(subset);
        mesh.x1 = x1;
        mesh.x2 = x2;
        mesh.y1 = y1;
        mesh.y2 = y2;
        mesh.z1 = z1;
        mesh.z2 = z2;
        mesh.w2 = w2;
        networkControllerSingleton.incomingMeshes.Enqueue(mesh);
        Debug.Log("Mesh enqueued");

    }
    private void FixedUpdate()
    {
        //handle incoming meshes
        if (incomingMeshes.Count>0)
        {
            HoloToolkit.Unity.SimpleMeshSerializer.MeshData md = incomingMeshes.Dequeue();
            Mesh mesh = new Mesh();
            mesh.vertices = md.vertices;
            mesh.triangles = md.triangleIndices;
            mesh.RecalculateNormals();
            GameObject go = Instantiate(networkControllerSingleton.inMeshInstancePrefab);
            Vector3 v = new Vector3(md.x1, md.y1, md.z1);
            Quaternion q = new Quaternion(md.x2, md.y2, md.z2, md.w2);
            //set game object position here.
            go.GetComponent<InMeshInstance>().updateRenderedMesh(mesh);
        }
    }
    void OnDestroy()
    {
        if(myUDP!=null)
        {
            myUDP.Close();
            myUDP = null;
        }
    }
}
