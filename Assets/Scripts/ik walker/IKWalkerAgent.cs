using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgentsExamples;
using UnityEngine;

public class IKWalkerAgent : Agent
{
    [Header("Limb List (Assign GameObjects here)")]
    // 유니티 인스펙터 할당용
    public List<GameObject> limbObjects;

    // 실제 통제용 인터페이스 리스트
    private List<ILimbController> limbs = new List<ILimbController>();

    [Header("Main Body")]
    public Transform mainBody;
    private Rigidbody bodyRb; // 몸통의 물리적 속도를 측정하기 위해 추가
    private Vector3 targetDirection;

    JointDriveController m_JdController;
    EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {
        m_JdController = GetComponent<JointDriveController>();
        bodyRb = mainBody.GetComponent<Rigidbody>();

        // GameObject에서 인터페이스 추출 및 초기화
        foreach (var obj in limbObjects)
        {
            ILimbController controller = obj.GetComponent<ILimbController>();
            if (controller != null)
            {
                controller.InitializeLimb(gameObject);
                limbs.Add(controller);
            }
            else
            {
                Debug.LogError($"{obj.name}에 ILimbController가 없습니다!");
            }
        }

        m_JdController.SetupBodyPart(mainBody);
        m_ResetParams = Academy.Instance.EnvironmentParameters;

        foreach (var limb in limbs)
        {
            limb.SetTargetPosition(limb.GetEndEffectorLocalPosition());
        }
    }

    float finalScore = 0f;

    public override void OnEpisodeBegin()
    {
        System.Console.WriteLine(finalScore);

        // 1. 워커 몸통 리셋
        foreach (var bp in m_JdController.bodyPartsList)
        {
            bp.Reset(bp);
        }
        /*
        // 2. 하위 Limb 리셋 (누락되었던 부분)
        foreach (var limb in limbs)
        {
            limb.ResetLimb();
        }
        */
        targetDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 워커의 자세와 이동 속도 관측 (균형 및 이동 파악)
        sensor.AddObservation(mainBody.forward);
        sensor.AddObservation(mainBody.up); // 넘어짐 방지용
        sensor.AddObservation(bodyRb.linearVelocity); // 속도 파악용

        // 타겟 방향
        sensor.AddObservation(targetDirection);

        // 각 limb의 현재 발끝 위치 (워커 기준)
        foreach (var limb in limbs)
        {
            sensor.AddObservation(limb.GetEndEffectorLocalPosition());
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var i = -1;
        var continuousActions = actions.ContinuousActions;

        foreach (var limb in limbs)
        {
            // Action 값(-1 ~ 1)을 maxReach 스케일로 변환하여 월드 좌표로 전달
            Vector3 targetOffset = new Vector3(continuousActions[++i], -1 + continuousActions[++i], continuousActions[++i]) * limb.GetMaxReach();
            limb.SetTargetPosition(limb.GetRootPosition() + targetOffset);
        }
    }

    private void FixedUpdate()
    {
        /* [핵심: 이동 속도 기반 보상] */
        // 타겟 방향으로 얼마나 빠르게 이동하고 있는지를 내적(Dot)하여 보상으로 줍니다.
        // 제자리에 멈춰있으면 보상이 0에 수렴하며, 타겟 방향으로 돌진할수록 보상이 커집니다.
        float moveSpeedTowardsTarget = Vector3.Dot(bodyRb.linearVelocity, targetDirection);
        float fallingPenalty = Vector3.Dot(mainBody.up, Vector3.up) - 0.8f;
        finalScore = (moveSpeedTowardsTarget * fallingPenalty);

        if(mainBody.up.y < 0f) // 너무 낮게 떨어지면 에피소드 종료
        {
            //finalScore 출력
            System.Console.WriteLine(finalScore);
            EndEpisode();
            AddReward(-1f); // 넘어짐 패널티
        }

        AddReward(finalScore * 0.1f);
    }
}