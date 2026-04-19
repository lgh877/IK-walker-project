using UnityEngine;

public class TargetPositionSynchronizer : MonoBehaviour
{
    public InverseKineticAgent parentObject;

    private void FixedUpdate()
    {
        if ((Time.frameCount & 3) == 0)
        {
            transform.position = parentObject.targetPosition;
        }
    }
}
