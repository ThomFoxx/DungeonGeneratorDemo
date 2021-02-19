using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomInfo : MonoBehaviour
{
    public Transform[] Exits;
    [SerializeField]
    [Range(0,100)]
    [Tooltip("Higher the number the more likely a hall is next.")]
    private int _hallChance;

    public int HallChance()
    {
        return _hallChance;
    }
}
