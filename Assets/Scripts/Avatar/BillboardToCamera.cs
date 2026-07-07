using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    private void LateUpdate()
    {
        Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
        if (cameraToUse == null)
        {
            return;
        }

        Vector3 direction = transform.position - cameraToUse.transform.position;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }
}
