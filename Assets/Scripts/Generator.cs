using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Generator : MonoBehaviour
{
    [SerializeField]
    private bool _debugOn;
    [SerializeField]
    private GameObject[] _starterRoomPrefabs;
    [SerializeField]
    private GameObject[] _roomPrefabs;
    [SerializeField]
    private GameObject[] _hallPrefabs;
    private GameObject _currentRoom;
    private RoomInfo _currentRoomScript;
    private Queue<GameObject> _testingRooms = new Queue<GameObject>();
    private Transform _joinedExit;
    private int _roomCount = 0;
    private Queue<GameObject> _rooms = new Queue<GameObject>();
    private Queue<Transform> _exits = new Queue<Transform>();
    private Transform[] _exitsTemp;
    [SerializeField]
    [Range(0.001f, 1f)]
    private float _loopDelay = 0.019f;
    [SerializeField]
    [Range(0, 100)]
    private int _exitChance = 50;
    [SerializeField]
    [Range(0, 20)]
    private int _checkDistance = 10;
    [SerializeField]
    [Range(1, 25)]
    private int _checkLimit = 5;
    [SerializeField]
    private Transform _grave;
    [SerializeField]
    private Transform _levelMap;

    [SerializeField]
    private int _roomLimit;

    [SerializeField]
    [Range(0f, .5f)]
    private float _colliderReSize = 0.1f;

    #region Spin Radius Variables
    private float _spinRadius;
    private BoxCollider _collider;
    private Collider[] _otherColliders;
    #endregion

   
    [SerializeField]
    private NavMeshBaker _navMeshBaker;
    [SerializeField]
    private GameObject _demoRunner;

    [SerializeField]
    private UIManager _UIManager;

    private void Awake()
    {
#if UNITY_EDITOR       
        if (_debugOn)
            Debug.unityLogger.logEnabled = true;
        else
            Debug.unityLogger.logEnabled = false;
#else
        Debug.unityLogger.logEnabled = false;
#endif
    }

    private void Start()
    {
        if (_roomCount == 0)
        {
            _currentRoom = SpawnNewRoom(_starterRoomPrefabs);
            _currentRoom.name = "Start Room";
            _rooms.Enqueue(_currentRoom);
            PositionRoom(_currentRoom, null);
            OpenRoomExits(_currentRoom);
            StartCoroutine(GenerationLoop());
        }
    }

    public int RoomCount()
    {
        return _roomCount;
    }

    private IEnumerator GenerationLoop()
    {
        while (_roomCount <= _roomLimit)
        {
            if (_exits.Count() > 0)
            {
                RandomizeExits();
                foreach (Transform Exit in _exits)
                {
                    int RNG = Random.Range(0, 100);
                    if (RNG <= Exit.parent.GetComponent<RoomInfo>().HallChance())
                        _currentRoom = SpawnNewRoom(_hallPrefabs);
                    else
                        _currentRoom = SpawnNewRoom(_roomPrefabs);
                    PositionRoom(_currentRoom, Exit);
                    _testingRooms.Enqueue(_currentRoom);
                    yield return new WaitForSeconds(_loopDelay);
                    RoomCheck(_currentRoom, Exit);
                }
                yield return new WaitForEndOfFrame();
                _exits.Clear();
            }
            else
            {
                StopCoroutine(GenerationLoop());
                break;
            }
            if (_testingRooms.Count() >0)
            {
                foreach (GameObject Room in _testingRooms)
                {
                    if (Room != null)
                        OpenRoomExits(Room);
                }
                _testingRooms.Clear();
            }
        }
        foreach(GameObject Room in _rooms)
        {
            ManageRoomChildren(Room, true);
        }
        StopCoroutine(GenerationLoop());
        _UIManager.PlayModeUI();
        Debug.Log(_levelMap.transform.GetChild(0).gameObject.name);
        _navMeshBaker.BakeMesh(_levelMap.transform.GetChild(0).gameObject);
        Instantiate(_demoRunner, this.transform.position, Quaternion.identity);
    }

    private void RandomizeExits()
    {
        int range = _exits.Count();
        _exitsTemp = new Transform[range];
        foreach (Transform Exit in _exits)
        {
Retry:
            int rng = Random.Range(0, range);
            Debug.Log("RNG: " + rng);
            if (_exitsTemp[rng] != null)
                goto Retry;
            else
                _exitsTemp[rng] = Exit;
        }
        _exits.Clear();
        foreach (Transform Exit in _exitsTemp)
        {
            _exits.Enqueue(Exit);
        }
        _exitsTemp = new Transform[0];
    }

    private GameObject SpawnNewRoom(GameObject[] RoomPool)
    {
        int RNG = Random.Range(0, RoomPool.Length);
        GameObject Spawn = Instantiate(RoomPool[RNG], this.transform);
        _collider = Spawn.GetComponent<BoxCollider>();
        _collider.size = new Vector3(_collider.size.x - _colliderReSize, _collider.size.y - _colliderReSize, _collider.size.z - _colliderReSize);
        
        return Spawn;
    }

    private void PositionRoom(GameObject Room, Transform Exit)
    {
        if (_roomCount == 0)
        { //This should only fire for the Starter Room
            //This will need to change for multilevel dungeons
            Room.transform.position = Vector3.zero; 
            Room.transform.parent = _levelMap;
            _collider = Room.GetComponent<BoxCollider>();
            _collider.size = new Vector3(_collider.size.x - _colliderReSize, _collider.size.y - _colliderReSize, _collider.size.z - _colliderReSize);
            _roomCount++;
            ManageRoomChildren(Room, true);
            return;
        }

        if (Exit != null)
        {
            Debug.Log(Room.name + " picking Exit to Connect.");
            RoomInfo RoomScript = Room.GetComponent<RoomInfo>();
            int RNG = Random.Range(0, RoomScript.Exits.Length);
            _joinedExit = RoomScript.Exits[RNG];
            RoomRotation(Exit, _joinedExit);
            ManageRoomChildren(Room, true);
        }
    }

    private void OpenRoomExits(GameObject Room)
    {//Scan through Current Room's Exits to determine if they will be Used
        Debug.Log("Random Exits for " + Room);
        _currentRoomScript = Room.GetComponent<RoomInfo>();
        for (int i = 0; i < _currentRoomScript.Exits.Length; i++)
        {
            ExitInfo ExitInfo = _currentRoomScript.Exits[i].gameObject.GetComponent<ExitInfo>();
            int RND = Random.Range(0, 100);
            if (!ExitInfo.ExitState() && RND <= _exitChance)
            {
                if (ExitCheck(_currentRoomScript.Exits[i]))
                {
                    ExitInfo.SetExit(true);
                    _exits.Enqueue(_currentRoomScript.Exits[i]);                    
                }
            }
        }
    }

    private void ManageRoomChildren(GameObject Parent, bool isActive)
    {
        Debug.Log("Managae Children for " + Parent.name);
        for (int i = 0; i < Parent.transform.childCount; i++)
        {
            Parent.transform.GetChild(i).gameObject.SetActive(isActive);
        }
    }

    private bool ExitCheck(Transform Exit)
    {//Returns True if Exit is Clear   
        if (Exit.GetComponent<ExitInfo>().IsConnected())
        {
            Exit.gameObject.SetActive(false);
            Debug.Log("This Exit is Connected to Another");
            return false;
        }

        for (int i = 0; i < 3; i++)
        {
            Vector3 center = new Vector3(Exit.position.x, i, Exit.position.z);

            bool HitDetect = Physics.Raycast(center-(-Exit.forward), Exit.forward, _checkDistance);
            
            if (HitDetect)            
                return false;
        }
        return true;
    }

    private void RoomCheck(GameObject Room, Transform Exit)
    {
        bool Check = ColliderCheck(Room);
        if (Check)
        {//Good room            
            _rooms.Enqueue(Room);
            Room.transform.parent = _levelMap;
            Room.transform.name = _roomCount.ToString();
            ExitInfo EI = _joinedExit.GetComponent<ExitInfo>();
            EI.SetExit(true);
            EI.ConnectExit(Exit);
            EI.ExitUpdate();
            Exit.GetComponent<ExitInfo>().ConnectExit(_joinedExit);
            Exit.GetComponent<ExitInfo>().ExitUpdate();
            _roomCount++;
        }
        else
        {//Invalid Exit Close and move on
            Debug.Log("Invalid Exit Close and move on");
            Room.transform.position = _grave.position;
            Room.transform.parent = _grave;            
            Exit.GetComponent<ExitInfo>().SetExit(false);
            ManageRoomChildren(Room, false);
            Room.GetComponent<BoxCollider>().enabled = false;
            Destroy(Room.gameObject);
        }
    } 
    
    private bool ColliderCheck(GameObject RoomToCheck)
    {//returns TRUE if free of Collisions
        BoxCollider _collider = RoomToCheck.GetComponent<BoxCollider>();
        _collider.enabled = true;
        _otherColliders = default;
        GetSpinRadius(_collider);
        _otherColliders = Physics.OverlapSphere(RoomToCheck.transform.position, _spinRadius * 2);
        
        foreach (Collider Other in _otherColliders)
        {            
            //if (Other.GetType() != typeof(BoxCollider))
                //continue;
            if (Other == _collider)
                continue;
            if (Other.transform.parent == _collider.transform)
                continue;
            if (!Other.transform.CompareTag("Room"))
                continue;
            if (Other.bounds.Intersects(_collider.bounds))
            {
                Debug.Log("Collison Detected. " + RoomToCheck.name + " with " + Other.name, this);
                return false;
            }
        }
        Debug.Log(RoomToCheck.name + " Passed Collision Check.");
        return true;
    }

    private void GetSpinRadius(BoxCollider Box)
    {//Using Circular Radius with this Project
        #region Circular Radius

        //Calculates Corner to Center for Rotation Clearance on one Axis (Y in this case)
        _spinRadius = Mathf.Sqrt(Mathf.Pow(Box.bounds.size.x, 2) + Mathf.Pow(Box.bounds.size.z, 2)) / 2;


        #endregion        
        #region Spherical Radius
        //Calculates Corner to Center for Rotation Clearance on all Axes
        /*
        float c1;
        float c2;

        c1 = Mathf.Sqrt(Mathf.Pow(_collide.bounds.size.x, 2) + Mathf.Pow(_collide.bounds.size.y, 2));
        if (_collide.bounds.size.x < _collide.bounds.size.y)
        {
            c2 = Mathf.Sqrt(Mathf.Pow(_collide.bounds.size.x, 2) + Mathf.Pow(_collide.bounds.size.z, 2));
        }
        else
        {
            c2 = Mathf.Sqrt(Mathf.Pow(_collide.bounds.size.y, 2) + Mathf.Pow(_collide.bounds.size.z, 2));
        }

        SpinRadius = (Mathf.Sqrt(Mathf.Pow(c1, 2) + Mathf.Pow(c2, 2))/2);
        */
        #endregion
    }

    private void RoomRotation(Transform Exit, Transform JoinedExit)
    {//Rotates and Moves Parent Room of JoinedExit to match up with Exit
        
            Vector3 TempVector = Exit.position;
            Vector3 Offset = JoinedExit.GetComponent<ExitInfo>().offset;

            float TempX = Offset.x;
            float TempZ = Offset.z;
            int TempRot = 0;

            int ExitRoomAngle = GetCardinalDirection(Exit.parent, false);
            int ExitAngle = GetCardinalDirection(Exit, true);
            int JoinedAngle = GetCardinalDirection(JoinedExit, true);

            if (ExitAngle + ExitRoomAngle == 0 || ExitAngle + ExitRoomAngle == 360)
            {
                switch (JoinedAngle)
                {
                    case 0:
                        TempVector.x = TempVector.x + TempX;
                        TempVector.z = TempVector.z + TempZ;
                        TempRot = 180;// + ExitRoomAngle;
                        Debug.Log("Room Exit " + (ExitAngle + ExitRoomAngle) + " & Joined Exit " + JoinedAngle);
                        break;
                    case 90:
                        TempVector.x = TempVector.x - TempZ;
                        TempVector.z = TempVector.z + TempX;
                        TempRot = 90;// + ExitRoomAngle;
                        Debug.Log("Room Exit " + (ExitAngle + ExitRoomAngle) + " & Joined Exit " + JoinedAngle);
                        break;
                    case 180:
                        TempVector.x = TempVector.x - TempX;
                        TempVector.z = TempVector.z - TempZ;
                        TempRot = 0;// + ExitRoomAngle;
                        Debug.Log("Room Exit " + (ExitAngle + ExitRoomAngle) + " & Joined Exit " + JoinedAngle);
                        break;
                    case 270:
                        TempVector.x = TempVector.x + TempZ;
                        TempVector.z = TempVector.z - TempX;
                        TempRot = 270;//+ ExitRoomAngle;
                        Debug.Log("Room Exit " + (ExitAngle + ExitRoomAngle) + " & Joined Exit " + JoinedAngle);
                        break;
                    default:
                        Debug.Log("Something went Wrong. Rot0");
                        break;
                }
            }
            else if (ExitAngle + ExitRoomAngle == 90 || ExitAngle + ExitRoomAngle == 450)
            {
                switch (JoinedAngle)
                {
                    case 0:
                        TempVector.x = TempVector.x + TempZ;
                        TempVector.z = TempVector.z - TempX;
                        TempRot = 270;// + ExitRoomAngle;
                        Debug.Log("Room Exit " + (ExitAngle + ExitRoomAngle) + " & Joined Exit " + JoinedAngle);
                        break;
                    case 90:
                        TempVector.x = TempVector.x + TempX;
                        TempVector.z = TempVector.z + TempZ;
                        TempRot = 180;// + ExitRoomAngle;
                        Debug.Log("Room Exit " + (ExitAngle + ExitRoomAngle) + " & Joined Exit " + JoinedAngle);
                        break;
                    case 180:
                        TempVector.x = TempVector.x - TempZ;
                        TempVector.z = TempVector.z + TempX;
                        TempRot = 90;// + ExitRoomAngle;
                        Debug.Log("Room Exit " + (ExitAngle + ExitRoomAngle) + " & Joined Exit " + JoinedAngle);
                        break;
                    case 270:
                        TempVector.x = TempVector.x - TempX;
                        TempVector.z = TempVector.z - TempZ;
                        TempRot = 0;// + ExitRoomAngle;
                        Debug.Log("Room Exit " + (ExitAngle + ExitRoomAngle) + " & Joined Exit " + JoinedAngle);
                        break;
                    default:
                        Debug.Log("Something went Wrong. Rot90");
                        break;
                }
            }
            else if (ExitAngle + ExitRoomAngle == 180 || ExitAngle + ExitRoomAngle == 540)
            {
                switch (JoinedAngle)
                {
                    case 0:
                        TempVector.x = TempVector.x - TempX;
                        TempVector.z = TempVector.z - TempZ;
                        TempRot = 0;// + ExitRoomAngle;
                        Debug.Log("Room Exit " + (ExitAngle + ExitRoomAngle) + " & Joined Exit " + JoinedAngle);
                        break;
                    case 90:
                        TempVector.x = TempVector.x + TempZ;
                        TempVector.z = TempVector.z - TempX;
                        TempRot = 270;// + ExitRoomAngle;
                        Debug.Log("Room Exit " + (ExitAngle + ExitRoomAngle) + " & Joined Exit " + JoinedAngle);
                        break;
                    case 180:
                        TempVector.x = TempVector.x + TempX;
                        TempVector.z = TempVector.z + TempZ;
                        TempRot = 180;// + ExitRoomAngle;
                        Debug.Log("Room Exit " + (ExitAngle + ExitRoomAngle) + " & Joined Exit " + JoinedAngle);
                        break;
                    case 270:
                        TempVector.x = TempVector.x - TempZ;
                        TempVector.z = TempVector.z + TempX;
                        TempRot = 90;// + ExitRoomAngle;
                        Debug.Log("Room Exit " + (ExitAngle + ExitRoomAngle) + " & Joined Exit " + JoinedAngle);
                        break;
                    default:
                        Debug.Log("Something went Wrong. Rot180");
                        break;
                }
            }
            else if (ExitAngle + ExitRoomAngle == 270 || ExitAngle + ExitRoomAngle == 630)
            {
                switch (JoinedAngle)
                {
                    case 0:
                        TempVector.x = TempVector.x - TempZ;
                        TempVector.z = TempVector.z + TempX;
                        TempRot = 90;// + ExitRoomAngle;
                        Debug.Log("Room Exit " + (ExitAngle + ExitRoomAngle) + " & Joined Exit " + JoinedAngle);
                        break;
                    case 90:
                        TempVector.x = TempVector.x - TempX;
                        TempVector.z = TempVector.z - TempZ;
                        TempRot = 0;// + ExitRoomAngle;
                        Debug.Log("Room Exit " + (ExitAngle + ExitRoomAngle) + " & Joined Exit " + JoinedAngle);
                        break;
                    case 180:
                        TempVector.x = TempVector.x + TempZ;
                        TempVector.z = TempVector.z - TempX;
                        TempRot = 270;//+ ExitRoomAngle;
                        Debug.Log("Room Exit " + (ExitAngle + ExitRoomAngle) + " & Joined Exit " + JoinedAngle);
                        break;
                    case 270:
                        TempVector.x = TempVector.x + TempX;
                        TempVector.z = TempVector.z + TempZ;
                        TempRot = 180;// + ExitRoomAngle;
                        Debug.Log("Room Exit " + (ExitAngle + ExitRoomAngle) + " & Joined Exit " + JoinedAngle);
                        break;
                    default:
                        Debug.Log("Something went Wrong. Rot270");
                        break;
                }
            }
            else
            {
                Debug.Log("Problem with Rotation " + (ExitAngle + ExitRoomAngle) + " " + JoinedAngle);
            }

            _currentRoom.transform.rotation = Quaternion.Euler(0, TempRot, 0);
            _currentRoom.transform.position = TempVector; ///New Room Position
    }

    private int GetCardinalDirection(Transform checkedObject, bool local)
    {
        //Returns a INT angle 'Normalized' to the N,E,S,W on the Y Axis
        float CheckAngle = 0;

        if (local)
            CheckAngle = checkedObject.localRotation.eulerAngles.y;
        else
            CheckAngle = checkedObject.rotation.eulerAngles.y;

        int ReturnedAngle = 0;

        if ((CheckAngle <= 45 && CheckAngle >= -45) || (CheckAngle >= 315 && CheckAngle <= 360))
            ReturnedAngle = 0;
        else if (CheckAngle > 45 && CheckAngle <= 135)
            ReturnedAngle = 90;
        else if ((CheckAngle > 135 && CheckAngle <= 225) || (CheckAngle >= -135 && CheckAngle <= -180))
            ReturnedAngle = 180;
        else if ((CheckAngle > 225 && CheckAngle < 315) || (CheckAngle <= -45 && CheckAngle >= 135))
            ReturnedAngle = 270;
        else
            Debug.Log("Problem in Cardinal Check");

        //Debug.Log(checkedObject.name + "'s angle = " + ReturnedAngle, checkedObject);

        return ReturnedAngle;
    }
}