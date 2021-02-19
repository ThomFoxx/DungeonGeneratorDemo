using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExitInfo : MonoBehaviour
{
    public Vector3 offset;
    [SerializeField]
    private bool _validExit;
    [SerializeField]
    private Transform _connectedExit;
    private bool _isConnected = false;
    [SerializeField]
    private bool _useExitTypes;

    private void OnEnable()
    {
        if (_useExitTypes)
        {
            ExitUpdate();
        }
        else if (!_useExitTypes)
            transform.gameObject.SetActive(!_validExit);
    }

    public void SetExit(bool isValid)
    {
        _validExit = isValid;
    }

    public bool ExitState()
    {
        return _validExit;
    }

    public void ConnectExit(Transform Exit)
    {
        _connectedExit = Exit;
        _isConnected = true;
    }

    public bool IsConnected()
    {
        return _isConnected;
    }

    public void ExitUpdate()
    {
        if (IsConnected())
        {
            transform.GetChild(0).gameObject.SetActive(false);
            transform.GetChild(1).gameObject.SetActive(true);
        }
        else
        {
            transform.GetChild(0).gameObject.SetActive(true);
            transform.GetChild(1).gameObject.SetActive(false);
        }
        gameObject.SetActive(true);
    }
}
