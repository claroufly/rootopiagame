using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARPlaneManager))]
public class AutoPlaceOnPlane : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("A random prefab from this list is chosen for each spawned object.")]
    public GameObject[] objectPrefabs;

    [Header("Spawn Settings")]
    [Tooltip("How many objects to try to place per newly detected plane.")]
    [Min(1)] public int objectsPerPlane = 1;

    [Tooltip("Minimum distance (meters) between spawned objects on the same plane.")]
    public float minSpacing = 0.35f;

    [Tooltip("Random Y-axis rotation applied to each spawned object.")]
    public bool randomizeRotation = true;

    [Tooltip("Uniform scale applied to each spawned object (relative to the prefab's own scale).")]
    public float spawnScale = 0.5f;

    [Header("Global Limit")]
    [Tooltip("Hard cap on total objects spawned across all planes for the whole session.")]
    public int maxTotalSpawns = 5;

    private ARPlaneManager planeManager;
    private readonly HashSet<TrackableId> spawnedPlaneIds = new HashSet<TrackableId>();
    private int totalSpawned = 0;

    void Awake()
    {
        planeManager = GetComponent<ARPlaneManager>();
    }

    void OnEnable()
    {
        planeManager.planesChanged += OnPlanesChanged;
    }

    void OnDisable()
    {
        planeManager.planesChanged -= OnPlanesChanged;
    }

    void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        if (totalSpawned >= maxTotalSpawns)
            return;

        foreach (var plane in args.added)
        {
            if (totalSpawned >= maxTotalSpawns)
                break;

            if (spawnedPlaneIds.Contains(plane.trackableId))
                continue;

            spawnedPlaneIds.Add(plane.trackableId);
            SpawnObjectsOnPlane(plane);
        }
    }

    void SpawnObjectsOnPlane(ARPlane plane)
    {
        if (objectPrefabs == null || objectPrefabs.Length == 0)
        {
            Debug.LogWarning("[AutoPlaceOnPlane] No prefabs assigned.");
            return;
        }

        List<Vector3> placedLocalPositions = new List<Vector3>();
        const int maxAttemptsPerObject = 20;

        for (int i = 0; i < objectsPerPlane; i++)
        {
            // FIX: stop the moment the global cap is hit, even mid-plane.
            if (totalSpawned >= maxTotalSpawns)
                return;

            Vector3 localPoint = Vector3.zero;
            bool foundSpot = false;

            for (int attempt = 0; attempt < maxAttemptsPerObject; attempt++)
            {
                Vector2 randomPoint = GetRandomPointInPlaneBounds(plane);
                Vector3 candidate = new Vector3(randomPoint.x, 0f, randomPoint.y);

                bool tooClose = false;
                foreach (var placed in placedLocalPositions)
                {
                    if (Vector3.Distance(candidate, placed) < minSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    localPoint = candidate;
                    foundSpot = true;
                    break;
                }
            }

            if (!foundSpot)
                continue;

            placedLocalPositions.Add(localPoint);

            Vector3 worldPosition = plane.transform.TransformPoint(localPoint);
            Quaternion rotation = plane.transform.rotation;

            if (randomizeRotation)
                rotation *= Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            GameObject prefabToSpawn = objectPrefabs[Random.Range(0, objectPrefabs.Length)];
            GameObject spawned = Instantiate(prefabToSpawn, worldPosition, rotation);
            spawned.transform.localScale = prefabToSpawn.transform.localScale * spawnScale;

            totalSpawned++;
        }
    }

    Vector2 GetRandomPointInPlaneBounds(ARPlane plane)
    {
        Vector2 extents = plane.extents;
        Vector2 center = plane.centerInPlaneSpace;

        float randomX = Random.Range(-extents.x, extents.x) + center.x;
        float randomZ = Random.Range(-extents.y, extents.y) + center.y;

        return new Vector2(randomX, randomZ);
    }
}