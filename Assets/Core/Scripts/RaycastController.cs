using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class RaycastController : MonoBehaviour {

    protected const float skinWidth = .015f;

    const float distBetweenRays = .25f;

    [HideInInspector]
    public int horizontalRayCount = 4;
    [HideInInspector]
    public int verticalRayCount = 4;

    protected float horizontalRaySpacing;
    protected float verticalRaySpacing;

    public BoxCollider2D collider;
    protected RaycasOrigins raycasOrigins;

    public LayerMask collisionMask;

    public virtual void Awake()
    {
        collider = GetComponent<BoxCollider2D>();
    }

    public virtual void Start()
    {
        CalculateRaySpacing();
    }

    protected void UpdateRaycastOrigins()
    {
        Bounds bounds = collider.bounds;
        bounds.Expand(skinWidth * -2);

        raycasOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        raycasOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        raycasOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        raycasOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }


    protected void CalculateRaySpacing()
    {
        Bounds bounds = collider.bounds;
        bounds.Expand(skinWidth * -2);

        float boundsWidth = bounds.size.x;
        float boundsHeight = bounds.size.y;

        horizontalRayCount = Mathf.RoundToInt( boundsHeight / distBetweenRays );
        verticalRayCount = Mathf.RoundToInt(boundsWidth / distBetweenRays);

        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }


    public struct RaycasOrigins
    {
        public Vector2 topLeft, topRight;
        public Vector2 bottomLeft, bottomRight;
    }
}
