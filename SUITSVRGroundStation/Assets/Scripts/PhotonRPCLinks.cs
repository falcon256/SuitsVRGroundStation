using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;

public class PhotonRPCLinks : MonoBehaviour
{
    public static PhotonRPCLinks singleton = null;
    // Start is called before the first frame update
    void Start()
    {
        if (!singleton)
            singleton = this;
        else
            Debug.LogError("PhotonRPCLinks DUPLICATE SINGLETONS ATTEMPTED!");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
