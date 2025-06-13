using UnityEngine;

public class MapSpawner : MonoBehaviour
{
    public MapDatabase mapDatabase;
    public int currentLevel = 1;
    private GameObject spawnedMap;

    void Start()
    {
        SpawnMapForLevel(currentLevel);
    }

    public void SpawnMapForLevel(int level)
    {
        if (spawnedMap != null)
            Destroy(spawnedMap);

        GameObject prefab = mapDatabase.GetMapPrefabByLevel(level);
        if (prefab != null)
        {
            spawnedMap = Instantiate(prefab, Vector3.zero, Quaternion.identity);

            // Optional: Adjust camera here
            var zone = spawnedMap.transform.Find("ZoneBounds");
            if (zone != null)
            {
                FindFirstObjectByType<CameraController>().AdjustCameraToZone(zone.gameObject);
            }
        }
        

    }
}
