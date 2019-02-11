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
        
        Debug.Log("Gameobject made.");
        HoloToolkit.Unity.SimpleMeshSerializer.MeshData mesh = HoloToolkit.Unity.SimpleMeshSerializer.Deserialize(inBytes);
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
