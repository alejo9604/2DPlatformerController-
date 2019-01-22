using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Controller2D : MonoBehaviour {

    const float skinWidth = .015f;

    public int horizontalRayCount = 4;
    public int verticalRayCount = 4;

    float maxClimbAngle = 80;
    float maxDescendAngle = 80;

    float horizontalRaySpacing;
    float verticalRaySpacing;

    BoxCollider2D collider;
    RaycasOrigins raycasOrigins;

    public LayerMask collisionMask;

    public CollisionInfo collisions;

    void Start () {
        collider = GetComponent<BoxCollider2D>();
        CalculateRaySpacing();
    }
	
	
	


    void UpdateRaycastOrigins()
    {
        Bounds bounds = collider.bounds;
        bounds.Expand(skinWidth * -2);

        raycasOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        raycasOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        raycasOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        raycasOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }


    void CalculateRaySpacing()
    {
        Bounds bounds = collider.bounds;
        bounds.Expand(skinWidth * -2);

        horizontalRayCount = Mathf.Clamp(horizontalRayCount, 2, int.MaxValue);
        verticalRayCount = Mathf.Clamp(verticalRayCount, 2, int.MaxValue);

        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }




    public void Move(Vector3 velocity)
    {
        UpdateRaycastOrigins();
        collisions.Reset();

        //Store old velocity
        collisions.velocityOld = velocity;

        //Check if is descending slope
        if(velocity.y < 0)
        {
            DescendSlope(ref velocity);
        }

        //Update collision in X and velocity
        if(velocity.x != 0)
            HorizontalCollision(ref velocity);

        //Update collision in Y and velocity
        if (velocity.y != 0)
            VerticalCollision(ref velocity);

        transform.Translate(velocity);
    }


    void ClimSlope(ref Vector3 velocity, float slopeAngle)
    {
        //Hypotenuse
        float moveDistance = Mathf.Abs(velocity.x);
        float climbvelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

        //No Jumping!
        if (velocity.y <= climbvelocityY) {

            //Calculate new Velocity in X and Y: The previus velocity.x is now the hypotenuse value
            velocity.y = climbvelocityY;
            velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);

            //Assume we are in the ground
            collisions.below = true;
            collisions.climbSlope = true;
            collisions.slopeAngle = slopeAngle;
        }
    }

    void DescendSlope( ref Vector3 velocity)
    {
        float directionX = Mathf.Sign(velocity.x);
        Vector2 rayorigin = ((directionX == -1) ? raycasOrigins.bottomRight : raycasOrigins.bottomLeft);
        RaycastHit2D hit = Physics2D.Raycast(rayorigin, -Vector2.up, Mathf.Infinity, collisionMask);

        if (hit)
        {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if(slopeAngle != 0 && slopeAngle <= maxDescendAngle)
            {
                //If slope is in the same direction
                if(Mathf.Sign(hit.normal.x) == directionX)
                {

                    //If distance is less than how we have to move in Y (Close enough)
                    if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x))
                    {
                        
                        //Again, calculate new Velocity in X and Y: The previus velocity.x is now the hypotenuse value
                        float moveDistance = Mathf.Abs(velocity.x);
                        float descendVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

                        velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
                        velocity.y -= descendVelocityY;

                        //Assume we are in the ground
                        collisions.slopeAngle = slopeAngle;
                        collisions.descendingSlope = true;
                        collisions.below = true;

                    }
                }
            }
        }
    }



    void HorizontalCollision(ref Vector3 velocity)
    {
        float directionX = Mathf.Sign(velocity.x);
        float rayLength = Mathf.Abs(velocity.x) + skinWidth;

        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayorigin = (directionX == -1) ? raycasOrigins.bottomLeft : raycasOrigins.bottomRight;
            rayorigin += Vector2.up * (horizontalRaySpacing * i);

            //Raycast to right direction
            RaycastHit2D hit = Physics2D.Raycast(rayorigin, Vector2.right * directionX, rayLength, collisionMask);

            Debug.DrawRay(rayorigin, Vector2.right * directionX * rayLength, Color.red);

            if (hit)
            {

                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                //Climb Slope
                if (i == 0 && slopeAngle <= maxClimbAngle)
                {

                    //Store actual velocity
                    if (!collisions.descendingSlope)
                    {
                        collisions.descendingSlope = false;
                        velocity = collisions.velocityOld;
                    }

                    //Subtract distance from velocity to prevent wear behavior
                    float distanceToSlopeStart = 0;
                    if(slopeAngle != collisions.slopeAngleOld)
                    {
                        distanceToSlopeStart = hit.distance - skinWidth;
                        velocity.x -= distanceToSlopeStart * directionX;
                    }

                    ClimSlope(ref velocity, slopeAngle);
                    //Restore velocity (Add distance)
                    velocity.x += distanceToSlopeStart * directionX;
                }



                if (!collisions.climbSlope || slopeAngle > maxClimbAngle)
                {
                    //Normal velocity
                    velocity.x = (hit.distance - skinWidth) * directionX;
                    rayLength = hit.distance;

                    //Collision when is climbSlope, we need to recalculated Y velocity (avoid wear jump)
                    //Maybe, this is duplicated with ClimSlope() method ????
                    if (collisions.climbSlope)
                    {
                        velocity.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x);
                    }

                    collisions.left = directionX == -1;
                    collisions.right = directionX == 1;
                }
            }
        }
    }
       
    void VerticalCollision(ref Vector3 velocity)
    {
        float directionY = Mathf.Sign(velocity.y);
        float rayLength = Mathf.Abs(velocity.y) + skinWidth;

        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayorigin = (directionY == -1) ? raycasOrigins.bottomLeft : raycasOrigins.topLeft;
            //Start from the point where we will be (+ velocity.x)
            rayorigin += Vector2.right * (verticalRaySpacing * i + velocity.x);

            //Raycast to up direction
            RaycastHit2D hit = Physics2D.Raycast(rayorigin, Vector2.up * directionY, rayLength, collisionMask);

            Debug.DrawRay(rayorigin, Vector2.up * directionY * rayLength, Color.red);

            if (hit)
            {
                velocity.y = (hit.distance - skinWidth) * directionY;
                rayLength = hit.distance;

                //Collision when is climbSlope, we need to recalculated X velocity (avoid wear jump)
                if (collisions.climbSlope)
                {
                    velocity.x = velocity.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sin(velocity.x);
                }

                collisions.below = directionY == -1;
                collisions.above = directionY == 1;
            }
        }

        
        //Check from new slope
        if (collisions.climbSlope)
        {
            float directionX = Mathf.Sign(velocity.x);
            rayLength = Mathf.Abs(velocity.x) + skinWidth;

            //Start from the point where we will be (+ velocity.y)
            Vector2 rayOrigin = ((directionX == -1) ? raycasOrigins.bottomLeft : raycasOrigins.bottomRight) + Vector2.up*velocity.y;

            //Raycast to right direction
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                //Climb a new slope
                if(slopeAngle != collisions.slopeAngle)
                {
                    velocity.x = (hit.distance - skinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                }
            }
        }
    }


   



    struct RaycasOrigins
    {
        public Vector2 topLeft, topRight;
        public Vector2 bottomLeft, bottomRight;
    }

    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;

        public bool climbSlope, descendingSlope;
        public float slopeAngle, slopeAngleOld;

        public Vector3 velocityOld;

        public void Reset()
        {
            above = below = false;
            left = right = false;
            climbSlope = descendingSlope = false;
            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
        }
    }
}
