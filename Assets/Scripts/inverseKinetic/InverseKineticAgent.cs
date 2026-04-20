using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgentsExamples;
using BodyPart = Unity.MLAgentsExamples.BodyPart;
using Unity.VisualScripting;

public class InverseKineticAgent : Agent, ILimbController
{
    [Header("Normalization Settings")]
    [Tooltip("관절을 최대로 뻗었을 때의 대략적인 총 길이 (이 거리를 벗어나면 -1에 수렴)")]
    public float maxReach = 5.0f; // 에이전트 크기에 맞게 인스펙터에서 수정하세요.

    private float lastScore = 0;

    [Header("Limb part Count")]
    public const int limbPartCount = 3;

    [Header("Limb Parts")]
    // 관절 부위 리스트. 첫 번째[0]가 Root(예: Thigh), 마지막[Count-1]이 End Effector(예: Foot)
    public List<Transform> limbParts = new List<Transform>(limbPartCount);
    private List<BodyPart> bodyParts = new List<BodyPart>(limbPartCount);

    [Header("Target Settings")]
    public Vector3 rootPosition;
    public Vector3 targetPosition;

    [Header("Parent GameObject")]
    public GameObject parentGameObject; // 컨트롤러를 받아올 부모 게임오브젝트 (예: HumanoidAgent)

    JointDriveController jointDriveController;
    EnvironmentParameters m_ResetParams;

    Vector3 currentPosition;
    Vector3 directionBetRootAndLast;
    Vector3 directionBetRootAndTarget; // 추가: 시작점과 목적지 사이의 방향 벡터

    public float GetMaxReach()
    {
        return maxReach;
    }

    public void InitializeLimb(GameObject parentWalker)
    {
        parentGameObject = parentWalker;
        jointDriveController = parentGameObject.GetComponent<JointDriveController>();
        rootPosition = limbParts[0].position;
        for (int i = 0; i < limbParts.Count; i++)
        {
            SetUpBodyPart(limbParts[i]);
        }
    }

    public void SetTargetPosition(Vector3 newTargetPosition)
    {
        targetPosition = newTargetPosition;
    }

    // 인터페이스 구현 3: 관절 초기화 (워커가 호출함)
    public void ResetLimb()
    {
        foreach (var bodyPart in bodyParts)
        {
            bodyPart.Reset(bodyPart);
        }
    }

    public Vector3 GetEndEffectorLocalPosition()
    {
        return limbParts[limbParts.Count - 1].position - limbParts[0].position;
    }

    public Vector3 GetRootPosition()
    {
        return limbParts[0].position;
    }
    /*
    public void Start()
    {
        jointDriveController = parentGameObject.GetComponent<JointDriveController>();
        rootPosition = limbParts[0].position; // 프로토타입이라 이렇게 해둠. 추후 워커 혹은 학습 프로그램이 알맞게 지정해줄 예정

        // limbParts 리스트를 기반으로 관절 컨트롤러 셋업
        for (int i = 0; i < limbParts.Count; i++)
        {
            SetUpBodyPart(limbParts[i]);
        }

        m_ResetParams = Academy.Instance.EnvironmentParameters;
    }*/
    /*
    public override void Initialize()
    {
        jointDriveController = parentGameObject.GetComponent<JointDriveController>();
        rootPosition = limbParts[0].position; // 프로토타입이라 이렇게 해둠. 추후 워커 혹은 학습 프로그램이 알맞게 지정해줄 예정

        // limbParts 리스트를 기반으로 관절 컨트롤러 셋업
        for (int i = 0; i < limbParts.Count; i++)
        {
            SetUpBodyPart(limbParts[i]);
        }

        m_ResetParams = Academy.Instance.EnvironmentParameters;
    }
    */
    public void SetUpBodyPart(Transform input)
    {
        var bp = new BodyPart
        {
            rb = input.GetComponent<Rigidbody>(),
            joint = input.GetComponent<ConfigurableJoint>(),
            startingPos = input.position,
            startingRot = input.rotation
        };
        jointDriveController.SetupBodyPart(bp);
        bodyParts.Add(bp);
    }

    public override void OnEpisodeBegin()
    {
        //targetPosition = rootPosition + new Vector3(Random.Range(-3f, 3f), -3.5f + Random.Range(0, 2f), Random.Range(-3f, 3f));
        //resetBodyParts();
    }

    private void resetBodyParts()
    {
        foreach (var bodyPart in bodyParts)
        {
            bodyPart.Reset(bodyPart);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 rootPos = limbParts[0].position;
        Vector3 endEffectorPos = limbParts[limbParts.Count - 1].position;
        Vector3 root2Target = targetPosition - rootPos;
        Vector3 root2End = endEffectorPos - rootPos;

        sensor.AddObservation(root2Target.normalized);
        sensor.AddObservation(root2End.normalized);
        sensor.AddObservation(Vector3.Dot(root2Target.normalized, root2End.normalized));

        float distToEndTarget = Vector3.Distance(endEffectorPos, targetPosition);

        // [핵심 로직] 거리를 1 ~ -1의 연속적인 점수로 변환 (유리 함수 방식 사용)
        // 거리가 0일 때 1, 거리가 maxReach일 때 0, 무한히 멀어지면 -1에 수렴
        float distanceScore = 1f - 2f * (distToEndTarget / (distToEndTarget + maxReach));

        sensor.AddObservation(distanceScore);

        foreach (var bodyPart in bodyParts)
        {
            sensor.AddObservation(bodyPart.rb.transform.localRotation);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var i = -1;
        var continuousActions = actions.ContinuousActions;

        // jointDriveController의 각 파트에 회전 및 힘 적용
        foreach (var bodyPart in bodyParts)
        {
            bodyPart.SetJointTargetRotation(continuousActions[++i], continuousActions[++i], continuousActions[++i]);
            bodyPart.SetJointStrength(continuousActions[++i]);
        }
    }

    private void FixedUpdate()
    {
        currentPosition = limbParts[0].position; // Root
        Vector3 endEffectorPos = limbParts[limbParts.Count - 1].position; // Target Group (목표 관절)

        directionBetRootAndLast = endEffectorPos - currentPosition;
        directionBetRootAndTarget = targetPosition - currentPosition;

        float distToEndTarget = Vector3.Distance(endEffectorPos, targetPosition);
        float distanceScore = 1f - 2f * (distToEndTarget / (distToEndTarget + maxReach));

        lastScore = Vector3.Dot(directionBetRootAndTarget.normalized, directionBetRootAndLast.normalized) - 0.8f + distanceScore * 0.5f;

        float alignReward = lastScore * 0.1f;

        AddReward(alignReward);
    }
}