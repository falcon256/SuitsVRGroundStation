using System.Collections;
using System.Collections.Generic;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO.Compression;
using System.IO;
using System.Threading;
using UnityEngine;

public class NetworkController : MonoBehaviour
{
    public static NetworkController networkControllerSingleton = null;
    public int port = 32123;
    //public UdpClient myUDP = null;// new UdpClient(port);
    public TcpListener tcpListener;
    private Thread tcpListenerThread;
    private TcpClient connectedTcpClient;
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

        // Start TcpServer background thread 		
        tcpListenerThread = new Thread(new ThreadStart(ListenForIncommingRequests));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();
        /*

        UdpClient myUDP = new UdpClient(port);
            Debug.Log("UDP PORT " + port + " Bound");
            myUDP.BeginReceive(DataIn, myUDP);
            */
    }

    /*
    private static void DataIn(IAsyncResult ar)
    {
        UdpClient c = (UdpClient)ar.AsyncState;
        IPEndPoint inIPEndPoint = new IPEndPoint(IPAddress.Any, 0);
        Byte[] inBytes = Decompress(c.EndReceive(ar, ref inIPEndPoint));
        Debug.Log("" + inBytes.Length + " Bytes received.");
        float x1 = System.BitConverter.ToSingle(inBytes, 0);
        float y1 = System.BitConverter.ToSingle(inBytes, 4);
        float z1 = System.BitConverter.ToSingle(inBytes, 8);
        float x2 = System.BitConverter.ToSingle(inBytes, 12);
        float y2 = System.BitConverter.ToSingle(inBytes, 16);
        float z2 = System.BitConverter.ToSingle(inBytes, 20);
        float w2 = System.BitConverter.ToSingle(inBytes, 24);

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
    */
    private void FixedUpdate()
    {
        //handle incoming meshes
        if (incomingMeshes.Count > 0)
        {
            try
            {
                HoloToolkit.Unity.SimpleMeshSerializer.MeshData md = incomingMeshes.Dequeue();
                Mesh mesh = new Mesh();

                Debug.Log("Verices: " + md.vertices.Length);
                Debug.Log("Tringle Indices: " + md.triangleIndices.Length);

                mesh.vertices = md.vertices;
                mesh.triangles = md.triangleIndices;
                mesh.RecalculateNormals();
                GameObject go = Instantiate(networkControllerSingleton.inMeshInstancePrefab);
                Vector3 v = new Vector3(md.x1, md.y1, md.z1);
                Quaternion q = new Quaternion(md.x2, md.y2, md.z2, md.w2);
                go.transform.SetPositionAndRotation(v, q);
                //temp for testing.
                //Vector3 v = Vector3.zero;
                //Quaternion q = Quaternion.identity;
                //set game object position here.
                go.GetComponent<InMeshInstance>().updateRenderedMesh(mesh);
            }
            catch (Exception e)
            {
                Debug.LogError("" + e.Message + "\n" + e.StackTrace);
            }
        }
    }
    void OnDestroy()
    {
        /*
        if(myUDP!=null)
        {
            myUDP.Close();
            myUDP = null;
        }*/
    }
    public static byte[] Compress(byte[] raw)
    {
        using (MemoryStream memory = new MemoryStream())
        {
            using (GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, true))
            {
                gzip.Write(raw, 0, raw.Length);
            }
            return memory.ToArray();
        }
    }

    static byte[] Decompress(byte[] gzip)
    {
        using (GZipStream stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
        {
            const int size = 4096;
            byte[] buffer = new byte[size];
            using (MemoryStream memory = new MemoryStream())
            {
                int count = 0;
                do
                {
                    count = stream.Read(buffer, 0, size);
                    if (count > 0)
                    {
                        memory.Write(buffer, 0, count);
                    }
                }
                while (count > 0);
                return memory.ToArray();
            }
        }
    }

    // <summary> 	
    /// Runs in background TcpServerThread; Handles incomming TcpClient requests 	
    /// </summary> 	
    private void ListenForIncommingRequests()
    {
        try
        {
            // Create listener on localhost port 8052. 			
            tcpListener = new TcpListener(IPAddress.Parse("192.168.137.1"), port);
            tcpListener.Start();
            Debug.Log("Server is listening");

            while (true)
            {
                byte[] bytes = new Byte[100000];
                byte[] buffer = null;
                using (connectedTcpClient = tcpListener.AcceptTcpClient())
                {

                    Debug.Log("Connection.");
                    // Get a stream object for reading 					
                    using (NetworkStream stream = connectedTcpClient.GetStream())
                    {
                        while (connectedTcpClient.Connected)
                        {
                            try
                            {
                                bytes = new Byte[100000];
                                buffer = null;
                                byte[] sizeBytes = new byte[4];
                                stream.Read(sizeBytes, 0, sizeBytes.Length);
                                int size = System.BitConverter.ToInt32(sizeBytes, 0);
                                if (size < 4)
                                {
                                    Debug.LogError("IncomingSize is <4!!!!!!!!");
                                }
                                int remaining = size - 4;

                                if (remaining < 28 || remaining > 100000)
                                {
                                    stream.Read(bytes, 0, 100000);//dump what remains and hope we realign.
                                    continue;
                                }

                                // Read incomming stream into byte arrary.
                                while (remaining > 0)
                                {

                                    int length = 0;
                                    if ((length = stream.Read(bytes, 0, remaining)) != 0)
                                    {
                                        Debug.Log("" + length + " bytes read from stream.");
                                        byte[] minBuf = new byte[length];
                                        if (buffer == null)
                                        {
                                            buffer = new byte[length];
                                            Array.Copy(bytes, 0, buffer, 0, length);
                                        }
                                        else
                                        {
                                            Array.Copy(bytes, 0, minBuf, 0, length);
                                            buffer = Combine(buffer, minBuf);
                                        }
                                        if (length > 0)
                                            Debug.Log("" + remaining + " remaining.");

                                    }
                                    remaining -= length;
                                }
                                Debug.Log("" + buffer.Length + " bytes compiled.");

                                var incommingData = new byte[buffer.Length];
                                Array.Copy(buffer, 0, incommingData, 0, buffer.Length);
                                Byte[] inBytes = incommingData;
                                //Debug.Log("" + incommingData.Length + " Compressed bytes received.");
                                //byte[] inBytes = Decompress(incommingData);


                                Debug.Log("" + inBytes.Length + " Bytes recieved. " + (size - 4) + " Expected...");
                                float x1 = System.BitConverter.ToSingle(inBytes, 0);
                                float y1 = System.BitConverter.ToSingle(inBytes, 4);
                                float z1 = System.BitConverter.ToSingle(inBytes, 8);
                                float x2 = System.BitConverter.ToSingle(inBytes, 12);
                                float y2 = System.BitConverter.ToSingle(inBytes, 16);
                                float z2 = System.BitConverter.ToSingle(inBytes, 20);
                                float w2 = System.BitConverter.ToSingle(inBytes, 24);
                                Debug.Log("" + x1 + " " + x2 + " " + y1 + " " + y2 + " " + z1 + " " + z2 + " " + w2 + " " + size + " ");
                                byte[] subset = new byte[inBytes.Length - 28];
                                Array.Copy(inBytes, 28, subset, 0, inBytes.Length - 28);


                                //Debug.Log("" + System.BitConverter.ToInt32(inBytes, 28) + " " + System.BitConverter.ToInt32(inBytes, 32) + " " + System.BitConverter.ToInt32(inBytes, 36) + " " + System.BitConverter.ToInt32(inBytes, 40));

                                HoloToolkit.Unity.SimpleMeshSerializer.MeshData mesh = HoloToolkit.Unity.SimpleMeshSerializer.Deserialize(subset);

                                mesh.x1 = x1;
                                mesh.x2 = x2;
                                mesh.y1 = y1;
                                mesh.y2 = y2;
                                mesh.z1 = z1;
                                mesh.z2 = z2;
                                mesh.w2 = w2;

                                //HoloToolkit.Unity.SimpleMeshSerializer.MeshData mesh = HoloToolkit.Unity.SimpleMeshSerializer.Deserialize(incommingData);
                                networkControllerSingleton.incomingMeshes.Enqueue(mesh);
                                Debug.Log("Mesh enqueued");
                            } catch(Exception e)
                            {
                                Debug.LogError(""+e.Message + "\n" + e.StackTrace);
                            }
                        }
                    }
                }
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("SocketException " + socketException.ToString());
        }
    }
    /// <summary> 	
    /// Send message to client using socket connection. 	
    /// </summary> 	
    private void SendMessage()
    {
        if (connectedTcpClient == null)
        {
            return;
        }

        try
        {
            // Get a stream object for writing. 			
            NetworkStream stream = connectedTcpClient.GetStream();
            if (stream.CanWrite)
            {
                string serverMessage = "This is a message from your server.";
                // Convert string message to byte array.                 
                byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(serverMessage);
                // Write byte array to socketConnection stream.               
                stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);
                Debug.Log("Server sent his message - should be received by client");
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
    }
    //stolen useful code.
    public static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] ret = new byte[first.Length + second.Length];
        System.Buffer.BlockCopy(first, 0, ret, 0, first.Length);
        System.Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
        return ret;
    }
}
