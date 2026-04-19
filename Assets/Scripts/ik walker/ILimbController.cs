using UnityEngine;

public interface ILimbController
{
    // 워커가 초기화될 때 호출할 셋업 메서드
    void InitializeLimb(GameObject parentWalker);

    // 워커가 매 스텝마다 지시할 목표 좌표
    void SetTargetPosition(Vector3 newTargetPosition);

    // 워커가 넘어지거나 에피소드가 끝날 때 관절을 초기화
    void ResetLimb();
}