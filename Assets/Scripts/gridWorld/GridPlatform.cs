using NUnit.Framework;
using Unity.MLAgents;
using UnityEngine;
using System.Collections.Generic;

public class GridPlatform : MonoBehaviour
{
    private EnvironmentParameters m_ResetParams;
    public GameObject[] m_objects;
    public GameObject CrossPrefab;
    public GameObject XPrefab;
    public GameObject Agent;
    public int numberOfCross = 1;
    public int numberOfX = 1;

    [HideInInspector]
    public int[] players;

    [HideInInspector]
    public List<GameObject> actorObjs;

    void Start()
    {
        m_ResetParams = Academy.Instance.EnvironmentParameters;
        actorObjs = new List<GameObject>();
        m_objects = new[] { CrossPrefab, XPrefab };
    }

    public void AreaReset()
    {
        #region 기존 오브젝트 제거
        int gridSize = (int) m_ResetParams.GetWithDefault("grid_size", 5f);
        foreach(var obj in actorObjs)
        {
            DestroyImmediate(obj);
        }
        actorObjs.Clear();
        #endregion
        #region 목표 오브젝트 설정
        List<int> playerList = new List<int>();
        for(int i = 0; i <(int)m_ResetParams.GetWithDefault("numCrossGoals", numberOfCross); i++) playerList.Add(0);
        for (int i = 0; i < (int)m_ResetParams.GetWithDefault("numXGoals", numberOfX); i++) playerList.Add(1);
        players = playerList.ToArray();
        #endregion
        #region 겹치지 않는 위치 선정
        HashSet<int> usedPositions = new HashSet<int>();
        while (usedPositions.Count < players.Length + 1)
        {
            int pos = Random.Range(0, gridSize * gridSize);
            usedPositions.Add(pos);
        }
        #endregion
        #region 위치 값을 벡터로 변환
        List<Vector3> positions = new List<Vector3>();
        foreach (var pos in usedPositions)
        {
            positions.Add(new Vector3(
                pos % gridSize,
                0,
                pos / gridSize)
            );
        }
        #endregion
        #region 오브젝트 생성
        for (int i = 0; i < players.Length; i++)
        {
            GameObject obj = Instantiate(
                m_objects[players[i]],
                transform
            );
            obj.transform.localPosition = positions[i];
            actorObjs.Add(obj);
        }
        #endregion
        #region 에이전트 위치 설정
        Agent.transform.localPosition = positions[players.Length];
        #endregion
    }
}
