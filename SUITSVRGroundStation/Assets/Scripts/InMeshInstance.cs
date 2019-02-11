using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InMeshInstance : MonoBehaviour
{
    public Mesh myMesh = null;
    public MeshFilter myMeshFilter = null;
    public MeshRenderer myMeshRenderer = null;

    public void updateRenderedMesh(Mesh newMesh)
    {
        myMeshFilter.mesh = myMesh = newMesh;
    }
}
