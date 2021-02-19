using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    private Image _title;
    [SerializeField]
    private Camera _mainCam;
    [SerializeField]
    private TMP_Text _roomCount;
    [SerializeField]
    private TMP_Text _time;
    [SerializeField]
    private GameObject _miniMap;
    [SerializeField]
    private Generator _dg;
    private int _lastCount;

    // Start is called before the first frame update
    void Start()
    {
        _title.enabled = true;
        _miniMap.SetActive(false);
        _roomCount.color = Color.black;
        _roomCount.text = "Rooms: 0";
        _time.color = Color.black;
        _time.text = "0";
    }

    // Update is called once per frame
    void Update()
    {
        if (_lastCount != _dg.RoomCount())
        {
            _lastCount = _dg.RoomCount();
            _roomCount.text = "Rooms: " + _lastCount;
            _time.text = Time.realtimeSinceStartup.ToString() + " sec";            
        } 
        
    }

    public void PlayModeUI()
    {
        _roomCount.color = Color.red;
        _time.color = Color.red;
        _miniMap.SetActive(true);
        _title.enabled = false;
        _mainCam.gameObject.SetActive(true);
    }

}
