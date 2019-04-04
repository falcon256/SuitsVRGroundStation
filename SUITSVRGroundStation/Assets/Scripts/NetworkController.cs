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
    public int udpPort = 32124;
    public UdpClient myUDP = null;// new UdpClient(port);
    public float udpLastSendTime = 0;
    public bool udpBound = false;
    public TcpListener tcpListener;
    private Thread tcpListenerThread;
    private TcpClient connectedTcpClient;
    public GameObject inMeshInstancePrefab = null;
    public Queue<HoloToolkit.Unity.SimpleMeshSerializer.MeshData> incomingMeshes = null;

    public Vector3 camv = new Vector3();
    public Quaternion camq = new Quaternion();

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

        UdpClient myUDP = new UdpClient();
        try
        {
            myUDP.Client.Bind(new IPEndPoint(IPAddress.Any, udpPort));   
            Debug.Log("UDP PORT " + udpPort + " Bound");
            udpBound = true;
        }
        catch (Exception e)
        {
            Debug.Log("Unable to bind udp port.");
        }


    }

    public static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }

    private void FixedUpdate()
    {
        if(myUDP==null||!udpBound)
        {
            try
            {
                myUDP.Client.Bind(new IPEndPoint(IPAddress.Any, (9000)));
                Debug.Log("UDP PORT " + udpPort + " Bound");
                udpBound = true;
            }
            catch (Exception e)
            {
                //Debug.Log("Unable to bind udp port.");
            }
        }



        if((udpLastSendTime+10.0f > Time.realtimeSinceStartup)&&(myUDP != null)&&(udpBound))
        {
            byte[] myIP = System.Text.Encoding.UTF8.GetBytes(GetLocalIPAddress());
            myUDP.Send(myIP, myIP.Length, "255.255.255.255", udpPort);
            udpLastSendTime = Time.realtimeSinceStartup;
            Debug.Log("Broadcast " + GetLocalIPAddress() + " as target ip.");
        }
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
        
        if(myUDP!=null)
        {
            myUDP.Close();
            myUDP = null;
        }
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
            tcpListener = new TcpListener(System.Net.IPAddress.Any, port);
            //tcpListener.AllowNatTraversal(true);
            tcpListener.Start();
            Debug.Log("Server is listening");

            while (true)
            {
                byte[] bytes = new Byte[1000000];
                byte[] buffer = null;
                using (connectedTcpClient = tcpListener.AcceptTcpClient())
                {
                    //connectedTcpClient.ReceiveTimeout = 60000;
                    //connectedTcpClient.SendTimeout = 60000;
                    //connectedTcpClient.SendBufferSize = 65536;

                    Debug.Log("Connection.");
                    // Get a stream object for reading 					
                    using (NetworkStream stream = connectedTcpClient.GetStream())
                    {
                        while (connectedTcpClient.Connected)
                        {
                            try
                            {

                                bytes = new Byte[1000000];
                                buffer = null;
                                byte[] sizeBytes = new byte[4];
                                stream.Read(sizeBytes, 0, sizeBytes.Length);
                                int size = System.BitConverter.ToInt32(sizeBytes, 0);
                                if (size < 4)
                                {
                                    Debug.LogError("IncomingSize is <4!!!!!!!!");
                                }
                                int remaining = size - 4;

                                if (remaining < 32 || remaining > 1000000)
                                {
                                    Debug.Log((remaining < 32) ? "<32" : ">1mil");
                                    stream.Read(bytes, 0, 1000000);//dump what remains and hope we realign.
                                    debugByteOut(bytes);
                                    continue;
                                }

                                // Read incomming stream into byte arrary.
                                while (remaining > 0)
                                {

                                    int length = 0;
                                    if ((length = stream.Read(bytes, 0, remaining)) != 0)
                                    {
                                        //Debug.Log("" + length + " bytes read from stream.");
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
                                        //if (length > 0)
                                        //    Debug.Log("" + remaining + " remaining.");

                                    }
                                    remaining -= length;
                                }
                                //Debug.Log("" + buffer.Length + " bytes compiled.");

                                var incommingData = new byte[buffer.Length];
                                Array.Copy(buffer, 0, incommingData, 0, buffer.Length);
                                Byte[] inBytes = incommingData;
                                Debug.Log("" + incommingData.Length + " Compressed bytes received.");
                                //byte[] inBytes = Decompress(incommingData);


                                //Debug.Log("" + inBytes.Length + " Bytes recieved. " + (size - 4) + " Expected...");
                                int packetType = System.BitConverter.ToInt32(inBytes, 0);
                                switch(packetType)
                                {
                                    case (1):
                                        //Debug.Log("Packet Type Mesh");
                                        break;
                                    case (2):
                                        break;
                                    case (3):
                                        break;
                                    default:
                                        Debug.LogError("BAD PACKET TYPE ENCOUNTERED: " + packetType);
                                        debugByteOut(inBytes);
                                        continue;
                                        //break;//redundant
                                }
                                float x1 = System.BitConverter.ToSingle(inBytes, 4);
                                float y1 = System.BitConverter.ToSingle(inBytes, 8);
                                float z1 = System.BitConverter.ToSingle(inBytes, 12);
                                float x2 = System.BitConverter.ToSingle(inBytes, 16);
                                float y2 = System.BitConverter.ToSingle(inBytes, 20);
                                float z2 = System.BitConverter.ToSingle(inBytes, 24);
                                float w2 = System.BitConverter.ToSingle(inBytes, 28);
                                //Debug.Log("" + x1 + " " + x2 + " " + y1 + " " + y2 + " " + z1 + " " + z2 + " " + w2 + " " + size + " ");
                                byte[] subset = new byte[inBytes.Length - 32];
                                Array.Copy(inBytes, 32, subset, 0, inBytes.Length - 32);


                                //Debug.Log("" + System.BitConverter.ToInt32(inBytes, 28) + " " + System.BitConverter.ToInt32(inBytes, 32) + " " + System.BitConverter.ToInt32(inBytes, 36) + " " + System.BitConverter.ToInt32(inBytes, 40));

                                if(packetType==3)
                                {
                                    Vector3 v = new Vector3(x1, y1, z1);
                                    Quaternion q = new Quaternion(x2, y2, z2, w2);
                                    camv = v;
                                    camq = q;
                                }

                                if (packetType != 1)
                                    continue;
                                HoloToolkit.Unity.SimpleMeshSerializer.MeshData mesh = HoloToolkit.Unity.SimpleMeshSerializer.Deserialize(subset);

                                mesh.x1 = x1;
                                mesh.x2 = x2;
                                mesh.y1 = y1;
                                mesh.y2 = y2;
                                mesh.z1 = z1;
                                mesh.z2 = z2;
                                mesh.w2 = w2;

                                //Vector3 v = new Vector3(x1, y1, z1);
                                //Quaternion q = new Quaternion(x2, y2, z2, w2);
                                //Camera.main.transform.rotation = q;
                                //Camera.main.transform.position = v;
                                //camv = v;
                                //camq = q;
                                //HoloToolkit.Unity.SimpleMeshSerializer.MeshData mesh = HoloToolkit.Unity.SimpleMeshSerializer.Deserialize(incommingData);
                                networkControllerSingleton.incomingMeshes.Enqueue(mesh);
                                //Debug.Log("Mesh enqueued");
                            } catch(Exception e)
                            {
                                Debug.LogError("Bug in Dans Code:"+e.Message + "\n" + e.StackTrace);
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

    private void debugByteOut(byte[] bb)
    {
        string derp = "";
        for(int i = 0; i+4<bb.Length; i+=4)
        {
            derp+=System.BitConverter.ToInt32(bb, i)+" ";
        }
        derp += "\n";
        Debug.Log(derp);
    }


    public void SendLineRenderer(LineRenderer lr)
    {
        if (connectedTcpClient == null)
        {
            return;
        }

        try
        {
            Vector3[] verts = new Vector3[lr.positionCount];
            lr.GetPositions(verts);
            byte[] conversionArray = null;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                ArrayProxy<Vector3>.Serialize(
                           memoryStream,
                           verts,
                           Vector3Proxy.Serialize);

                //Here is the result
                conversionArray = memoryStream.ToArray();
            }
            if (conversionArray == null)// TODO throw error here
                return;

            byte[] bytes = new byte[4 + 12 + 20]; // 4 bytes per float
            System.Buffer.BlockCopy(BitConverter.GetBytes(36 + (conversionArray.Length)), 0, bytes, 0, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(4), 0, bytes, 4, 4);//type of packet
            System.Buffer.BlockCopy(BitConverter.GetBytes(lr.material.color.r), 0, bytes, 8, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(lr.material.color.g), 0, bytes, 12, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(lr.material.color.b), 0, bytes, 16, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(lr.material.color.a), 0, bytes, 20, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(lr.positionCount), 0, bytes, 24, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(lr.startWidth), 0, bytes, 28, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(lr.endWidth), 0, bytes, 32, 4);
            bytes = Combine(bytes, conversionArray);
            // Get a stream object for writing. 			
            NetworkStream stream = connectedTcpClient.GetStream();
            if (stream.CanWrite)
            {
             
                stream.Write(bytes, 0, bytes.Length);
                Debug.Log("LineRenderDataSent of length "+ conversionArray.Length);
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
    }


    public void SendLineUndoRenderer()
    {
        if (connectedTcpClient == null)
        {
            return;
        }

        try
        {
           
            Vector3 location = new Vector3();
            Quaternion rotation = new Quaternion();
            byte[] bytes = new byte[4 + 12 + 20]; // 4 bytes per float
            System.Buffer.BlockCopy(BitConverter.GetBytes(36), 0, bytes, 0, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(5), 0, bytes, 4, 4);//type of packet
            System.Buffer.BlockCopy(BitConverter.GetBytes(location.x), 0, bytes, 8, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(location.y), 0, bytes, 12, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(location.z), 0, bytes, 16, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(rotation.x), 0, bytes, 20, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(rotation.y), 0, bytes, 24, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(rotation.z), 0, bytes, 28, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(rotation.w), 0, bytes, 32, 4);
            // Get a stream object for writing. 			
            NetworkStream stream = connectedTcpClient.GetStream();
            if (stream.CanWrite)
            {
                stream.Write(bytes, 0, bytes.Length);
                Debug.Log("Undo Sent");
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

    public static class ArrayProxy<T>
    {
        public static void Serialize(Stream bytes, T[] instance, Action<Stream, T> serialization)
        {
            UShortProxy.Serialize(bytes, (ushort)instance.Length);
            foreach (T arg in instance)
            {
                serialization(bytes, arg);
            }
        }

        public static T[] Deserialize(Stream bytes, ArrayProxy<T>.Deserializer<T> serialization)
        {
            ushort num = UShortProxy.Deserialize(bytes);
            T[] array = new T[(int)num];
            for (int i = 0; i < (int)num; i++)
            {
                array[i] = serialization(bytes);
            }
            return array;
        }

        public delegate void Serializer<U>(Stream stream, U instance);

        public delegate U Deserializer<U>(Stream stream);
    }

    public static class UShortProxy
    {
        public static void Serialize(Stream bytes, ushort instance)
        {
            byte[] bytes2 = BitConverter.GetBytes(instance);
            bytes.Write(bytes2, 0, bytes2.Length);
        }

        public static ushort Deserialize(Stream bytes)
        {
            byte[] array = new byte[2];
            bytes.Read(array, 0, 2);
            return BitConverter.ToUInt16(array, 0);
        }
    }

    public static class Vector3Proxy
    {
        public static void Serialize(Stream bytes, Vector3 instance)
        {
            bytes.Write(BitConverter.GetBytes(instance.x), 0, 4);
            bytes.Write(BitConverter.GetBytes(instance.y), 0, 4);
            bytes.Write(BitConverter.GetBytes(instance.z), 0, 4);
        }

        public static Vector3 Deserialize(Stream bytes)
        {
            byte[] array = new byte[12];
            bytes.Read(array, 0, 12);
            return new Vector3(BitConverter.ToSingle(array, 0), BitConverter.ToSingle(array, 4),
                BitConverter.ToSingle(array, 8));
        }
    }

    /*
     *  byte[] conversionArray = yourConvertedByteArray;
 
 using (MemoryStream memoryStream = new MemoryStream(conversionArray))
 {
          //Here is the result
     Vector3[] data = ArrayProxy<Vector3>.Deserialize(memoryStream, Vector3Proxy.Deserialize);
 }*/
}
