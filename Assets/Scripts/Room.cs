using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    [SerializeField]
    private GameObject[] _miniMapTiles;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            foreach (GameObject Tile in _miniMapTiles)
            {
                Tile.SetActive(true);
            }
        }
    }
}
