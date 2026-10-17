
using UnityEngine;

[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public sealed class CameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("World Bounds")]
    [SerializeField] private SpriteRenderer groundRenderer;
    [SerializeField] private bool clampToMap = true;

    [Header("Movement")]
    [SerializeField, Min(0f)]
    private float smoothTime = 0.15f;

    private Camera cameraComponent;
    private Vector3 velocity;
    private float cameraZ;

    private void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        cameraZ = transform.position.z;

        if (!cameraComponent.orthographic)
        {
            cameraComponent.orthographic = true;
        }
    }

    private void Start()
    {
        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 destination = GetTargetPosition();

        if (smoothTime <= 0.001f)
        {
            transform.position = destination;
            return;
        }

        transform.position = Vector3.SmoothDamp(
            transform.position,
            destination,
            ref velocity,
            smoothTime
        );
    }

    private Vector3 GetTargetPosition()
    {
        float x = target.position.x;
        float y = target.position.y;

        if (clampToMap && groundRenderer != null)
        {
            Bounds bounds = groundRenderer.bounds;

            float halfHeight =
                cameraComponent.orthographicSize;

            float halfWidth =
                halfHeight * cameraComponent.aspect;

            float minX = bounds.min.x + halfWidth;
            float maxX = bounds.max.x - halfWidth;

            float minY = bounds.min.y + halfHeight;
            float maxY = bounds.max.y - halfHeight;

            if (minX > maxX)
            {
                x = bounds.center.x;
            }
            else
            {
                x = Mathf.Clamp(x, minX, maxX);
            }

            if (minY > maxY)
            {
                y = bounds.center.y;
            }
            else
            {
                y = Mathf.Clamp(y, minY, maxY);
            }
        }

        return new Vector3(x, y, cameraZ);
    }

    public void SnapToTarget()
    {
        if (target == null)
            return;

        velocity = Vector3.zero;
        transform.position = GetTargetPosition();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        SnapToTarget();
    }
}
