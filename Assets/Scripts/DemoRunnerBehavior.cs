using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DemoRunnerBehavior : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Transform[] _targets;
    [SerializeField]
    private Transform _destination;
    [SerializeField]
    private GameObject _levelManager;
    private bool _newTarget = false;

    // Start is called before the first frame update
    void Start()
    {
        _levelManager = GameObject.Find("LevelManager");
        _targets = new Transform[_levelManager.transform.childCount];
        _agent = GetComponent<NavMeshAgent>();
        for (int i = 0; i < _levelManager.transform.childCount; i++)
        {
            _targets[i] = _levelManager.transform.GetChild(i).transform.Find("NavTarget").transform;
        }
        Shuffle();
        _destination = _targets[0];
        _agent.SetDestination(_destination.position);
    }

    public void Update()
    {
        if (_agent.remainingDistance <= .35f)
        {
            RandomTarget();
        }
        if (_agent.destination !=null && _destination.position != _agent.destination)
        {
            _agent.SetDestination(_destination.position);
        }
    }

    public void RandomTarget()
    {
        int RNG = Random.Range(0, _targets.Length);
        _destination = _targets[RNG];
    }

    public void Shuffle()
    {
        Transform temp;

        for (int i = 0; i < _targets.Length; i++)
        {
            int rnd = Random.Range(0, _targets.Length);
            temp = _targets[rnd];
            _targets[rnd] = _targets[i];
            _targets[i] = temp;
        }
    }

    
}