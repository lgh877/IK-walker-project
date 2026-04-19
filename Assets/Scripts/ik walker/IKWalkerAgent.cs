using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgentsExamples;
using UnityEngine;

public class IKWalkerAgent : Agent
{
    [Header("Limb Count")]
    public const int limbCount = 4;

    [Header("Limb List")] //지금은 inverseKineticAgent를 받지만, 추후 수식 기반 I.K도 적용 가능해지면 인터페이스 형식으로 받을것임
    public List<InverseKineticAgent> limbs = new List<InverseKineticAgent>(limbCount);

    [Header("Main Body")]
    public Transform mainBody; // 워커의 위치 및 쳐다보는 방향을 파악하는 데에 사용될 변수
    private Vector3 targetDirection; // 목표 방향
    JointDriveController m_JdController;
    EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {
        m_JdController = GetComponent<JointDriveController>();
        m_ResetParams = Academy.Instance.EnvironmentParameters;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //워커가 쳐다보는 방향
        sensor.AddObservation(mainBody.forward);
        //타겟 방향
        sensor.AddObservation(targetDirection);

        //각 limb의 targetPosition
        foreach (var limb in limbs)
        {
            sensor.AddObservation(limb.targetPosition - limb.rootPosition); // 타겟 위치에서 루트 위치를 빼서 limb가 발을 뻗었을 위치를 입력
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var i = -1;
        var continuousActions = actions.ContinuousActions;

        // jointDriveController의 각 파트에 회전 및 힘 적용
        foreach (var limb in limbs)
        {
            limb.targetPosition = limb.rootPosition + new Vector3(continuousActions[++i], continuousActions[++i], continuousActions[++i]) * limb.maxReach;
        }
    }
}
