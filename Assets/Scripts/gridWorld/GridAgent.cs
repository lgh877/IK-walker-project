using System;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Rendering;

public class GridAgent : Agent
{
    public enum GridGoal
    {
        Black,
        Red,
    }

    public GameObject BlackBottom;
    public GameObject RedBottom;

    GridGoal m_CurrentGoal;

    public GridGoal CurrentGoal
    {
        get { return m_CurrentGoal; }
        set
        {
            switch (value)
            {
                case GridGoal.Black:
                    BlackBottom.SetActive(true);
                    RedBottom.SetActive(false);
                    break;
                case GridGoal.Red:
                    BlackBottom.SetActive(false);
                    RedBottom.SetActive(true);
                    break;
            }
            m_CurrentGoal = value;
        }
    }

    public GridPlatform gridPlatform;
    public float timeBetweenDecisionsAtInference;   // 에이전트가 추론을 수행하는 간격
    float m_TimeSinceDecision;                      // 마지막 결정 이후 경과한 시간      

    public Camera renderCamera;                     // 에이전트의 카메라    

    VectorSensorComponent m_GoalSensor;             // 목표 감지기

    EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {
        m_GoalSensor = this.GetComponent<VectorSensorComponent>();
        m_ResetParams = Academy.Instance.EnvironmentParameters;
    }

    public override void OnEpisodeBegin()
    {
        gridPlatform.AreaReset();
        Array values = Enum.GetValues(typeof(GridGoal));
        if (m_GoalSensor is object)
        {
            CurrentGoal = (GridGoal)values.GetValue(UnityEngine.Random.Range(0, values.Length));
        }
        else
        {
            CurrentGoal = GridGoal.Black;
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        AddReward(-0.01f);
        int move = actions.DiscreteActions[0];

        Vector3 targetPos = transform.position;

        #region move값에 따른 행동 수행
        switch (move)
        {
            case 0:
            case 1:
                targetPos += transform.forward * (1 - move * 2);
                break;
            case 2:
            case 3:
                targetPos += transform.right * (move - 3) * -2;
                break;
            case 4:
                // 아무 행동도 하지 않음
                break;
            default:
                throw new System.ArgumentOutOfRangeException("Invalid action value");
        }
        #endregion

        Collider[] hit = Physics.OverlapBox(targetPos, new Vector3(0.3f,0.3f,0.3f));

        if(hit.Where(col => col.gameObject.CompareTag("Wall")).ToArray().Length == 0)
        {
            transform.position = targetPos;
            if (hit.Where(col => col.gameObject.CompareTag("Black")).ToArray().Length == 1)
            {
                ProvideReward(GridGoal.Black);
                EndEpisode();
            }
            else if (hit.Where(col => col.gameObject.CompareTag("Red")).ToArray().Length == 1)
            {
                ProvideReward(GridGoal.Red);
                EndEpisode();
            }
        }
    }
    void ProvideReward(GridGoal hitObject)
    {
        AddReward(hitObject == CurrentGoal ? 2 : -1);
    }

    public void FixedUpdate()
    {
        WaitTimeInference();
    }

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
