using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class GridPlatformExample : MonoBehaviour
{
    [HideInInspector]
    public List<GameObject> actorObjs;        // 생성된 오브젝트를 추적하기 위한 리스트

    [HideInInspector]
    public int[] players;                     // 각 오브젝트의 타입 정보 (0 = GreenPlus, 1 = RedEx)

    public GameObject trueAgent;              // 에이전트 (플레이어) 오브젝트
    public GameObject GreenPlusPrefab;        // 초록색 목표 오브젝트 프리팹
    public GameObject RedExPrefab;            // 빨간색 목표 오브젝트 프리팹

    public int numberOfPlus = 1;              // GreenPlus 생성 개수
    public int numberOfEx = 1;                // RedEx 생성 개수

    EnvironmentParameters m_ResetParams;      // ML-Agents에서 전달되는 환경 설정값
    GameObject[] m_Objects;                   // [0] = GreenPlusPrefab, [1] = RedExPrefab

    void Start()
    {
        // ML-Agents 환경 매개변수 초기화
        m_ResetParams = Academy.Instance.EnvironmentParameters;

        // 생성된 오브젝트들을 저장할 리스트 초기화
        actorObjs = new List<GameObject>();

        // 프리팹 배열 설정 (0 = GreenPlus, 1 = RedEx)
        m_Objects = new[] { GreenPlusPrefab, RedExPrefab };
    }

    /*
    @brief 환경을 초기화합니다.
    @details 환경을 초기화하는 메서드입니다.
    @details 환경 크기와 목표 오브젝트의 개수를 설정합니다.
    @details 이전에 생성된 오브젝트를 제거하고 새로운 오브젝트를 생성합니다.
    @details 목표 오브젝트는 중복되지 않는 위치에 생성됩니다.
    @details 에이전트는 마지막 위치에 배치됩니다.
    */
    public void AreaReset()
    {
        // 환경 크기 설정 (기본값: 5x5)
        int gridSize = (int)m_ResetParams.GetWithDefault("gridSize", 5f);

        // 이전에 생성된 오브젝트 제거
        foreach (var obj in actorObjs)
        {
            DestroyImmediate(obj);
        }
        actorObjs.Clear();

        // 목표 오브젝트 타입 배열 설정
        var playerList = new List<int>();
        for (int i = 0; i < (int)m_ResetParams.GetWithDefault("numPlusGoals", numberOfPlus); i++)
            playerList.Add(0);  // GreenPlus
        for (int i = 0; i < (int)m_ResetParams.GetWithDefault("numExGoals", numberOfEx); i++)
            playerList.Add(1);  // RedEx
        players = playerList.ToArray();

        // 중복 없는 무작위 위치 생성
        var usedPositions = new HashSet<int>();
        while (usedPositions.Count < players.Length + 1)
            usedPositions.Add(Random.Range(0, gridSize * gridSize));

        // 위치 값을 Vector3로 변환
        var positions = new List<Vector3>();
        foreach (var pos in usedPositions)
            positions.Add(new Vector3(pos / gridSize, -0.25f, pos % gridSize));

        // 목표 오브젝트 생성 및 배치
        for (int i = 0; i < players.Length; i++)
        {
            var obj = Instantiate(m_Objects[players[i]], transform);
            obj.transform.localPosition = positions[i];
            actorObjs.Add(obj);
        }

        // 에이전트 위치 설정
        trueAgent.transform.localPosition = positions[players.Length];
    }
}
