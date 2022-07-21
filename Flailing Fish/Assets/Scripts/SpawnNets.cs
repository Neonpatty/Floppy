using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnNets : MonoBehaviour
{
    #region Variables

    //NON-SERIALIZED
    private Vector2 spawnPos;
    private float yBounds;
    private float xBounds;
    private float spawnTime;

    //SERIALIZED
    [SerializeField] private GameObject toSpawn;
    [SerializeField] private float maxSpawnTime;
    [SerializeField] private BoxCollider2D bounds;

    //REFERENCES
    [SerializeField] private GameManager gM;

    #endregion

    // Update is called once per frame
    void Update()
    {
        if (gM.GetCurrentGameState() == GameManager.GameState.Gameplay)
        {
            spawnTime += Time.deltaTime;

            if (spawnTime > maxSpawnTime)
            {
                yBounds = Random.Range(bounds.bounds.min.y, bounds.bounds.max.y);
                xBounds = (bounds.bounds.center.x) + (bounds.bounds.center.y);
                spawnPos = new Vector2(xBounds, yBounds);
                Instantiate(toSpawn, spawnPos, toSpawn.transform.rotation);
                spawnTime = 0;
            }
        }

    }
}