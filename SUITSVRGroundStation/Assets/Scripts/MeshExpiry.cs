using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class MeshExpiry : MonoBehaviour
{
    private float lifeTime = 0.0f;
    private float createTime = 0.0f;
    private float duration = 240.0f;
    // Start is called before the first frame update
    void Start()
    {
        createTime = Time.realtimeSinceStartup;
    }

    // Update is called once per frame
    void Update()
    {

        lifeTime = Time.realtimeSinceStartup - createTime;
        if (lifeTime > duration)
            Destroy(this);

    }
}
