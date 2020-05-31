using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkCameraFollower : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //TODO Fix this after RPC
        //this.gameObject.transform.position = NetworkController.networkControllerSingleton.camv;
        //this.gameObject.transform.rotation = NetworkController.networkControllerSingleton.camq;
    }
}
