using UnityEngine;

public interface ILimbController
{
    float GetMaxReach();
    // 워커가 초기화될 때 호출할 셋업 메서드
    void InitializeLimb(GameObject parentWalker);

    // 워커가 매 스텝마다 지시할 목표 좌표
    void SetTargetPosition(Vector3 newTargetPosition);

    // 워커가 넘어지거나 에피소드가 끝날 때 관절을 초기화
    void ResetLimb();

    Vector3 GetEndEffectorLocalPosition(); // 워커가 발끝의 위치를 알 수 있게 하는 메서드. limb의 기준으로 해당 발끝의 로컬 좌표를 반환.

    Vector3 GetRootPosition();
}