using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleSpawner : MonoBehaviour
{
    #region Vars

    [System.Serializable]
    public class Path
    {
        public Waypoint[] nodes;
    }

    public List<Path> possiblePaths;
    public float spawnFrequency;

    ObjectPooler pool;
    float spawnDelay;

    #endregion


    void Start()
    {
        pool = ObjectPooler.Instance;
        StartCoroutine("SpawerUpdater");
        GetComponentInParent<RoadSegment>().vehiclesPerMinute = spawnFrequency;
    }


    IEnumerator SpawerUpdater()
    {
        Transform _vehicle;
        spawnDelay = 1 / (spawnFrequency / 60);

        while (true)
        {
            yield return new WaitForSeconds(spawnDelay);
            _vehicle = SpawnVehicle();

            yield return new WaitUntil(() => Vector3.Distance(_vehicle.position, transform.position) >= 6f);
        }
    }

    Transform SpawnVehicle()
    {
        // Get vehicle from pool
        Vehicle _vehicle = pool.GetPooledItem("cars", transform).GetComponent<Vehicle>();
        _vehicle.transform.SetParent(null);

        // Set a random path for the vehicle
        Path _path = possiblePaths[Random.Range(0, possiblePaths.Count)];
        _vehicle.nodes = _path.nodes;

        return _vehicle.transform;
    }
}
