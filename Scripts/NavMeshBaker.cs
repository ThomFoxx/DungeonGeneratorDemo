using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshBaker : MonoBehaviour
{

    private NavMeshSurface _navMeshSurface;

    public void BakeMesh(GameObject Room)
    {
        _navMeshSurface = Room.transform.Find("Floor").GetComponentInChildren<NavMeshSurface>();
        if (_navMeshSurface == null)
            Debug.LogError("NavMeshSurface is Null.");
        else
            _navMeshSurface.BuildNavMesh();
    }

}
