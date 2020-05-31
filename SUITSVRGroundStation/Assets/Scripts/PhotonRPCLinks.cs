using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;
using System;

public class PhotonRPCLinks : MonoBehaviourPun
{
    public static PhotonRPCLinks singleton = null;
    public static PhotonRPCLinks getSingleton() { return singleton; }
    public Material lineRendererDefaultMaterial = null;
    private Stack<LineRenderer> lineRenderers = null;

    // Start is called before the first frame update
    void Start()
    {
        if (!singleton)
            singleton = this;
        else
            Debug.LogError("PhotonRPCLinks DUPLICATE SINGLETONS ATTEMPTED!");
        lineRenderers = new Stack<LineRenderer>();
    }

    public void SendLineRenderer(LineRenderer lr)
    {
        Vector3[] verts = new Vector3[lr.positionCount];
        lr.GetPositions(verts);
        PhotonView pv = this.photonView;

        Color32 col = lr.material.color;
        float r = col.r;
        float g = col.g;
        float b = col.b;

        float width = lr.startWidth;
        pv.RPC("receiveLineRenderer", RpcTarget.Others, (object) r, (object) g, (object) b, (object)verts, (object)width);
    }

    [PunRPC]
    void receiveLineRenderer(Single r, Single g, Single b, Vector3[] verts, Single width)
    {
        Debug.Log("Got a line renderer of length " + verts.Length);
        GameObject go = new GameObject();
        go.transform.parent = this.gameObject.transform;
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = new Material(lineRendererDefaultMaterial);//copy
        lr.material.color = new Color(r,g,b);
        lr.startWidth = width;
        lr.endWidth = width;
        lr.widthMultiplier = 1.0f;
        lr.endColor = lr.startColor = new Color(r, g, b);
        lr.positionCount = verts.Length;
        lr.SetVertexCount(verts.Length);
        for (int i = 0; i < verts.Length; i++)
        {
            lr.SetPosition(i, verts[i]);
        }

        go.SetActive(true);
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(lr.material.color, 0.0f), new GradientColorKey(lr.material.color, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
        );
        lr.colorGradient = gradient;
        lineRenderers.Push(lr);
    }

    public void SendLineRendererUndo()
    {
        PhotonView pv = this.photonView;
        pv.RPC("receiveLineRendererUndo", RpcTarget.Others);
    }

    [PunRPC]
    void receiveLineRendererUndo()
    {
        Debug.Log("Line renderer undo called");
        LineRenderer lr = lineRenderers.Pop();
        lr.enabled = false;
        Destroy(lr.gameObject);
    }
}
