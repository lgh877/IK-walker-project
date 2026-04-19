using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class ObstacleGenerator : MonoBehaviour
{
    public Transform[] spawnPoints;
    public GameObject obstaclePrefab;
    public GameObject scorePrefab;
    public GameObject agent;
    public float forwardForce = 15f;
    public float timeBetWaves = 1f;
    private List<GameObject> obstacles = new List<GameObject>();
    
    public void GenerateObstacle()
    {
        int randomIndex = Random.Range(0, spawnPoints.Length);

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if(randomIndex != i)
            {
                GameObject obstacle = Instantiate(obstaclePrefab, spawnPoints[i].position, Quaternion.identity);
                obstacle.tag = "Obstacle";
                obstacle.SetActive(true);
                obstacles.Add(obstacle);
            }
            else
            {
                GameObject score = Instantiate(scorePrefab, spawnPoints[i].position, Quaternion.identity);
                score.tag = "Score";
                score.SetActive(true);
                obstacles.Add(score);
            }
        }
    }

    void Start()
    {
        InvokeRepeating("GenerateObstacle", 0f, timeBetWaves);
    }

    private void FixedUpdate()
    {
        for (int i = obstacles.Count - 1; i >= 0; i--)
        {
            if (obstacles[i] == null) continue;

            obstacles[i].transform.Translate(Vector3.back * forwardForce * Time.fixedDeltaTime);

            if (obstacles[i].transform.position.z < agent.transform.position.z - 3)
            {
                Destroy(obstacles[i]);
                obstacles.RemoveAt(i);
            }
        }
    }

    public void ResetObstacles()
    {
        for (int i = 0; i < obstacles.Count; i++)
        {
            Destroy(obstacles[i]);
        }
        obstacles.Clear();
    }
}
