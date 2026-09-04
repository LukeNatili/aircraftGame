//using UnityEngine;

//public class GameManager : MonoBehaviour
//{
//    public static GameManager Instance { get; private set; }

//    public MissionMarkerSelector MarkerSelector;
//    public SideMissionSpawner SideSpawner;

//    void Awake()
//    {
//        if (Instance != null && Instance != this)
//        {
//            Destroy(gameObject);
//            return;
//        }
//        Instance = this;
//        DontDestroyOnLoad(gameObject);
//    }
//    // Start is called once before the first execution of Update after the MonoBehaviour is created
//    void Start()
//    {
//        var handPlaced = new System.Collections.Generic.List<MissionMarker>(
//            MarkerSelector.GetComponentsInChildren<MissionMarker>());

//        SideSpawner.SpawnSideMission(handPlaced);
//        MarkerSelector.RefreshMarkers();

//        MarkerSelector.OnMissionSelected += HandleMissionSelected;
        
//    }

//    void HandleMissionSelected(MissionMarker marker)
//    {
//        Debug.Log($"Selected mission: {marker.MissionName}");
//    }

//    // Update is called once per frame
//    void Update()
//    {
        
//    }
//}
