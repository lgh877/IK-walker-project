using Unity.MLAgents;
using UnityEngine;
using System;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.VisualScripting;

public class AgentBehavior : Agent
{
    private Vector3 startingPosition;
    public float cubeSpeed = 0.1f;
    private float currentSpeed;
    public ObstacleGenerator obstacleGenerator;
    public event Action OnReset;
    private Rigidbody rb;
    public override void Initialize()
    {
        startingPosition = transform.position;
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        RequestDecision();
        if (transform.position.y < -1)
        {
            AddReward(-3f);
            EndEpisode();
        }
    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        int action = actions.DiscreteActions[0];
        float mask = (action == 0) ? 0f : 1f;
        AddReward(mask * -0.001f);
        currentSpeed = Mathf.Lerp(currentSpeed, cubeSpeed * (action * 2 - 3), 0.3f * mask);
        transform.position += new Vector3(currentSpeed * mask, 0, 0);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            AddReward(-1f);
            Reset();
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Score"))
        {
            AddReward(3f);
            Destroy(other.gameObject);
        }
    }
    private void Reset()
    {
        transform.position = startingPosition;
        obstacleGenerator.ResetObstacles();
        OnReset?.Invoke();
    }
    public override void OnEpisodeBegin()
    {
        Reset();
    }
}
