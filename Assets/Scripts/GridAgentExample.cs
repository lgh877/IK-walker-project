using System;
using UnityEngine;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class GridAgentExample : Agent
{
    public GridPlatformExample area;                // 에이전트의 훈련 영역
    public float timeBetweenDecisionsAtInference;   // 에이전트가 추론을 수행하는 간격
    float m_TimeSinceDecision;                      // 마지막 결정 이후 경과한 시간      

    public Camera renderCamera;                     // 에이전트의 카메라    


    VectorSensorComponent m_GoalSensor;             // 목표 감지기    


    // 에이전트의 목표
    public enum GridGoal
    {
        GreenPlus,
        RedEx,
    }

    public GameObject GreenBottom;
    public GameObject RedBottom;

    GridGoal m_CurrentGoal;

    /*
    @brief 에이전트의 목표를 설정합니다.
    @details 에이전트의 목표는 그리드에서 이동할 수 있는 위치를 나타냅니다.
    @details 목표는 GreenPlus 또는 RedEx 중 하나일 수 있습니다.
    */
    public GridGoal CurrentGoal
    {
        get { return m_CurrentGoal; }
        set
        {
            switch (value)
            {
                case GridGoal.GreenPlus:
                    GreenBottom.SetActive(true);
                    RedBottom.SetActive(false);
                    break;
                case GridGoal.RedEx:
                    GreenBottom.SetActive(false);
                    RedBottom.SetActive(true);
                    break;
            }
            m_CurrentGoal = value;
        }
    }

    // 에이전트의 행동을 마스킹할지 여부를 설정합니다.
    public bool maskActions = true;
    const int k_NoAction = 0;  // do nothing!
    const int k_Up = 1;
    const int k_Down = 2;
    const int k_Left = 3;
    const int k_Right = 4;

    EnvironmentParameters m_ResetParams;    // 환경 매개변수


    /*
    @brief 에이전트의 초기화 메서드입니다.
    @details 에이전트의 초기화 작업을 수행합니다.
    @details VectorSensorComponent를 가져와서 m_GoalSensor에 할당합니다.
    @details Academy.Instance.EnvironmentParameters를 가져와서 m_ResetParams에 할당합니다.
    */
    public override void Initialize()
    {
        m_GoalSensor = this.GetComponent<VectorSensorComponent>();
        m_ResetParams = Academy.Instance.EnvironmentParameters;
    }

    
    /*
    @brief 에이전트의 관측을 추가하는 메서드입니다.
    @details 에이전트의 관측을 추가합니다.
    @details 에이전트의 위치와 목표 위치를 관측으로 추가합니다.
    @details 에이전트의 목표 위치는 m_GoalSensor에 추가됩니다.
    */
    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        if (maskActions)
        {
            var positionX = (int)transform.localPosition.x;
            var positionZ = (int)transform.localPosition.z;
            var maxPosition = (int)m_ResetParams.GetWithDefault("gridSize", 5f) - 1;

            if (positionX == 0)
            {
                actionMask.SetActionEnabled(0, k_Left, false);
            }

            if (positionX == maxPosition)
            {
                actionMask.SetActionEnabled(0, k_Right, false);
            }

            if (positionZ == 0)
            {
                actionMask.SetActionEnabled(0, k_Down, false);
            }

            if (positionZ == maxPosition)
            {
                actionMask.SetActionEnabled(0, k_Up, false);
            }
        }
    }

    /*
    @brief 에이전트의 행동을 수행하는 메서드입니다.
    @details 에이전트의 행동을 수행합니다.
    @details 에이전트의 행동은 DiscreteActions[0]으로 가져옵니다.
    @details 에이전트의 행동에 따라 이동할 위치를 계산합니다.
    @details 이동할 위치에 장애물이 없으면 에이전트를 이동합니다.
    @details 이동한 위치에 목표가 있으면 보상을 제공합니다.
    @details 목표가 GreenPlus이면 2점을 보상합니다.
    @details 목표가 RedEx이면 -1점을 보상합니다.
    @details 목표가 없으면 -0.01점을 보상합니다.
    */
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        AddReward(-0.01f);
        var action = actionBuffers.DiscreteActions[0];

        var targetPos = transform.position;
        switch (action)
        {
            case k_NoAction:
                // do nothing
                break;
            case k_Right:
                targetPos = transform.position + new Vector3(1f, 0, 0f);
                break;
            case k_Left:
                targetPos = transform.position + new Vector3(-1f, 0, 0f);
                break;
            case k_Up:
                targetPos = transform.position + new Vector3(0f, 0, 1f);
                break;
            case k_Down:
                targetPos = transform.position + new Vector3(0f, 0, -1f);
                break;
            default:
                throw new ArgumentException("Invalid action value");
        }

        var hit = Physics.OverlapBox(
            targetPos, new Vector3(0.3f, 0.3f, 0.3f));
        if (hit.Where(col => col.gameObject.CompareTag("Wall")).ToArray().Length == 0)
        {
            transform.position = targetPos;

            if (hit.Where(col => col.gameObject.CompareTag("Green")).ToArray().Length == 1)
            {
                ProvideReward(GridGoal.GreenPlus);
                EndEpisode();
            }
            else if (hit.Where(col => col.gameObject.CompareTag("Red")).ToArray().Length == 1)
            {
                ProvideReward(GridGoal.RedEx);
                EndEpisode();
            }
        }
    }

    private void ProvideReward(GridGoal hitObject)
    {
        if (CurrentGoal == hitObject)
        {
            Debug.Log("Hit the correct goal: " + hitObject);
            SetReward(2f);
        }
        else
        {
            Debug.Log("Hit the wrong goal: " + hitObject);
            SetReward(-1f);
        }
    }


    /*
    @brief 에이전트의 행동을 수동으로 설정하는 메서드입니다.
    @details 에이전트의 행동을 수동으로 설정합니다.
    @details 키보드 입력을 사용하여 에이전트의 행동을 설정합니다.
    @details W, A, S, D 키를 사용하여 에이전트를 이동합니다.
    */
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = k_NoAction;
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[0] = k_Right;
        }
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = k_Up;
        }
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[0] = k_Left;
        }
        if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = k_Down;
        }
    }

    /*
    @brief 에이전트의 에피소드를 시작하는 메서드입니다.
    @details 에이전트의 에피소드를 시작합니다.
    @details 에이전트의 위치를 초기화하고 목표를 설정합니다.
    @details 목표는 GreenPlus 또는 RedEx 중 하나일 수 있습니다.
    @details 목표는 랜덤하게 설정됩니다.
    @details 에이전트의 목표는 m_GoalSensor에 추가됩니다.
    @details 에이전트의 목표는 Academy.Instance.EnvironmentParameters에 추가됩니다.
    */
    public override void OnEpisodeBegin()
    {
        area.AreaReset();
        Array values = Enum.GetValues(typeof(GridGoal));
        if (m_GoalSensor is object)
        {
            CurrentGoal = (GridGoal)values.GetValue(UnityEngine.Random.Range(0, values.Length));
        }
        else
        {
            CurrentGoal = GridGoal.GreenPlus;
        }
    }

    public void FixedUpdate()
    {
        WaitTimeInference();
    }


    /*
    @brief 에이전트의 추론을 대기하는 메서드입니다.
    @details 에이전트의 추론을 대기합니다.
    @details renderCamera가 null이 아니고 GraphicsDeviceType이 Null이 아니면 renderCamera를 렌더링합니다.
    @details Academy.Instance.IsCommunicatorOn이 true이면 RequestDecision()을 호출합니다.
    @details Academy.Instance.IsCommunicatorOn이 false이면 m_TimeSinceDecision이 timeBetweenDecisionsAtInference보다 크거나 같으면 RequestDecision()을 호출합니다.
    @details 그렇지 않으면 m_TimeSinceDecision에 Time.fixedDeltaTime을 더합니다.
    @details m_TimeSinceDecision은 에이전트의 마지막 결정 이후 경과한 시간을 나타냅니다.
    @details RequestDecision()은 에이전트의 행동을 요청하는 메서드입니다.
    */
    void WaitTimeInference()
    {
        if (renderCamera != null && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
        {
            renderCamera.Render();
        }

        
        if (Academy.Instance.IsCommunicatorOn)
        {
            RequestDecision();
        }
        else
        {
            if (m_TimeSinceDecision >= timeBetweenDecisionsAtInference)
            {
                m_TimeSinceDecision = 0f;
                RequestDecision();
            }
            else
            {
                m_TimeSinceDecision += Time.fixedDeltaTime;
            }
        }
    }
}
