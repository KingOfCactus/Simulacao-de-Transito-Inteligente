using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
// (02/08/22)

// Not the git ones
[System.Serializable]
public struct PoolRequest
{
    public GameObject item;
    public int amount;
    public string tag;
    public bool expandable;
}

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance;
    void Awake() => Instance = this;

    // Not the git ones
    public List<PoolRequest> poolRequests; 
    Dictionary<string, List<GameObject>> pools;


    void Start()
    {
        // Create the pools
        pools = new Dictionary<string, List<GameObject>>(poolRequests.Count);

        // Create a pool for each PoolRequest in the requests array
        poolRequests.ForEach(r => CreatePool(r)); 
    }


    #region Create Pools
    
    // Create and fill a pool with certain items
    private void CreatePool(PoolRequest data)
    {
        // Get data
        GameObject prefab = data.item;
        int size = data.amount;
        string tag = data.tag;

        // Initialize the pool with null values
        List<GameObject> newPool = new List<GameObject>(new GameObject[size]);

        // Fill the pool
        for (int i = 0; i < size; i++)
            newPool[i] = SpawnPoolItem(prefab);

        pools.Add(tag, newPool);
    }

    // Spawn a item to be pooled
    GameObject SpawnPoolItem(GameObject obj)
    {
        GameObject item = Instantiate(obj, transform);
        item.SetActive(false);
        return item;
    }

    #endregion

    #region Get Items

    // Gets a item from a pool using
    public GameObject GetPooledItem(string tag)
    {
        List<GameObject> pool;
        GameObject item;

        // Check if targetPool does exist
        if (!pools.TryGetValue(tag, out pool))
        {
            Debug.LogError($"[OBJECT POOLER] Error: The '{tag}' pool don't exist");
            return null;
        }

        // Check if targetPool has any available object
        if (pool[0].activeInHierarchy)
        {
            // Update pool order
            pool = pool.OrderBy(g => g.activeInHierarchy).ToList();

            // Exit if theres none available object
            if (pool[0].activeInHierarchy)
            {
                Debug.LogError($"[OBJECT POOLER] Error: The '{tag}' pool don't has any available object");
                return null;
            }
        }

        // Grab and "spawn" the item
        item = pool[0];
        item.SetActive(true);

        // Moves the item to the bottom of the list
        pool.RemoveAt(0);
        pool.Insert(pool.Count, item);

        // Calls OnSpawn() if item implements IPooledObject interface
        if (item.TryGetComponent(out IPoolSpawnable pooledObj))
            pooledObj.OnSpawn();

        return item;
    }

    // Gets a item from a pool and change it parent
    public GameObject GetPooledItem(string tag, Transform parent)
    {
        List<GameObject> pool;
        GameObject item;

        // Check if targetPool does exist
        if (!pools.TryGetValue(tag, out pool))
        {
            Debug.LogError($"[OBJECT POOLER] Error: The '{tag}' pool don't exist");
            return null;
        }

        // Check if targetPool has any available object
        if (pool[0].activeInHierarchy)
        {
            // Update pool order
            pool = pool.OrderBy(g => g.activeInHierarchy).ToList();

            // Exit if theres none available object
            if (pool[0].activeInHierarchy)
            {
                Debug.LogError($"[OBJECT POOLER] Error: The '{tag}' pool don't has any available object");
                return null;
            }
        }

        // Grab and "spawn" the item
        item = pool[0];
        item.SetActive(true);

        // Update item transform
        item.transform.position = parent.position;
        item.transform.rotation = parent.rotation;
        item.transform.parent = parent;

        // Moves the item to the bottom of the list
        pool.RemoveAt(0);
        pool.Insert(pool.Count, item);

        // Calls OnSpawn() if item implements IPooledObject interface
        if (item.TryGetComponent(out IPoolSpawnable pooledObj))
            pooledObj.OnSpawn();

        return item;
    }

    #endregion
}
