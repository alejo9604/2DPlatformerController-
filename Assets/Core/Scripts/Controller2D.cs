using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Controller2D : RaycastController {

    public float maxSlopeAngle = 80;
        
    public CollisionInfo collisions;
    [HideInInspector]
    public Vector2 plyerInput;

    public override void Start()
    {
        base.Start();
        collisions.faceDir = 1;
    }

    public void Move(Vector2 moveAmount, bool standingonPlatform = false)
    {
        Move(moveAmount, Vector2.zero, standingonPlatform);
    }

    public void Move(Vector2 moveAmount, Vector2 input, bool standingonPlatform = false)
    {
        UpdateRaycastOrigins();
        collisions.Reset();

        //Store old moveAmount
        collisions.moveAmountOld = moveAmount;


        plyerInput = input;

        //Check if is descending slope
        if(moveAmount.y < 0)
        {
            DescendSlope(ref moveAmount);
        }

        //Get direction after Slope (Dir. can change in Auto desc. slope)
        if (moveAmount.x != 0)
            collisions.faceDir = (int)Mathf.Sign(moveAmount.x);


        //Update collision in X and moveAmount
        //if(moveAmount.x != 0)
        HorizontalCollision(ref moveAmount);

        //Update collision in Y and moveAmount
        if (moveAmount.y != 0)
            VerticalCollision(ref moveAmount);

        transform.Translate(moveAmount);

        if (standingonPlatform)
            collisions.below = true;
    }

    void ClimSlope(ref Vector2 moveAmount, float slopeAngle, Vector2 slopeNormal)
    {
        //Hypotenuse
        float moveDistance = Mathf.Abs(moveAmount.x);
        float climbmoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

        //No Jumping!
        if (moveAmount.y <= climbmoveAmountY) {

            //Calculate new moveAmount in X and Y: The previus moveAmount.x is now the hypotenuse value
            moveAmount.y = climbmoveAmountY;
            moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);

            //Assume we are in the ground
            collisions.below = true;
            collisions.climbSlope = true;
            collisions.slopeAngle = slopeAngle;
            collisions.slopeNormal = slopeNormal;
        }
    }

    void DescendSlope( ref Vector2 moveAmount)
    {
        RaycastHit2D maxSlopleHitLeft = Physics2D.Raycast(raycasOrigins.bottomLeft, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, collisionMask);
        RaycastHit2D maxSlopleHitRight = Physics2D.Raycast(raycasOrigins.bottomRight, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, collisionMask);

        //Only one side detect collision && Avoid when player is part in normal ground 
        if (maxSlopleHitLeft ^ maxSlopleHitRight)
        {
            SlideDownMaxSlope(maxSlopleHitLeft, ref moveAmount);
            SlideDownMaxSlope(maxSlopleHitRight, ref moveAmount);
        }

        if (!collisions.slidingDownMaxSlope)
        {

            float directionX = Mathf.Sign(moveAmount.x);
            Vector2 rayorigin = ((directionX == -1) ? raycasOrigins.bottomRight : raycasOrigins.bottomLeft);
            RaycastHit2D hit = Physics2D.Raycast(rayorigin, -Vector2.up, Mathf.Infinity, collisionMask);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != 0 && slopeAngle <= maxSlopeAngle)
                {
                    //If slope is in the same direction
                    if (Mathf.Sign(hit.normal.x) == directionX)
                    {

                        //If distance is less than how we have to move in Y (Close enough)
                        if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x))
                        {

                            //Again, calculate new moveAmount in X and Y: The previus moveAmount.x is now the hypotenuse value
                            float moveDistance = Mathf.Abs(moveAmount.x);
                            float descendmoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

                            moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
                            moveAmount.y -= descendmoveAmountY;

                            //Assume we are in the ground
                            collisions.slopeAngle = slopeAngle;
                            collisions.descendingSlope = true;
                            collisions.below = true;
                            collisions.slopeNormal = hit.normal;
                        }
                    }
                }
            }
        }
    }


    void SlideDownMaxSlope(RaycastHit2D hit, ref Vector2 moveAmount)
    {
        if (hit)
        {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if(slopeAngle > maxSlopeAngle)
            {
                moveAmount.x = hit.normal.x *  (Mathf.Abs(moveAmount.y) - hit.distance) / Mathf.Tan(slopeAngle * Mathf.Deg2Rad);

                collisions.slopeAngle = slopeAngle;
                collisions.slidingDownMaxSlope = true;
                collisions.slopeNormal = hit.normal;
            }
        }
    }

    void HorizontalCollision(ref Vector2 moveAmount)
    {
        float directionX = collisions.faceDir;
        float rayLength = Mathf.Abs(moveAmount.x) + skinWidth;

        if(Mathf.Abs( moveAmount.x ) < skinWidth)
        {
            rayLength = 2 * skinWidth;
        }


        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayorigin = (directionX == -1) ? raycasOrigins.bottomLeft : raycasOrigins.bottomRight;
            rayorigin += Vector2.up * (horizontalRaySpacing * i);

            //Raycast to right direction
            RaycastHit2D hit = Physics2D.Raycast(rayorigin, Vector2.right * directionX, rayLength, collisionMask);

            Debug.DrawRay(rayorigin, Vector2.right * directionX, Color.red);

            if (hit)
            {

                if (hit.distance == 0)
                    continue;

                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                //Climb Slope
                if (i == 0 && slopeAngle <= maxSlopeAngle)
                {

                    //Store actual moveAmount
                    if (!collisions.descendingSlope)
                    {
                        collisions.descendingSlope = false;
                        moveAmount = collisions.moveAmountOld;
                    }

                    //Subtract distance from moveAmount to prevent wear behavior
                    float distanceToSlopeStart = 0;
                    if(slopeAngle != collisions.slopeAngleOld)
                    {
                        distanceToSlopeStart = hit.distance - skinWidth;
                        moveAmount.x -= distanceToSlopeStart * directionX;
                    }

                    ClimSlope(ref moveAmount, slopeAngle, hit.normal);
                    //Restore moveAmount (Add distance)
                    moveAmount.x += distanceToSlopeStart * directionX;
                }



                if (!collisions.climbSlope || slopeAngle > maxSlopeAngle)
                {
                    //Normal moveAmount
                    moveAmount.x = (hit.distance - skinWidth) * directionX;
                    rayLength = hit.distance;

                    //Collision when is climbSlope, we need to recalculated Y moveAmount (avoid wear jump)
                    //Maybe, this is duplicated with ClimSlope() method ????
                    if (collisions.climbSlope)
                    {
                        moveAmount.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x);
                    }

                    collisions.left = directionX == -1;
                    collisions.right = directionX == 1;
                }
            }
        }
    }
       
    void VerticalCollision(ref Vector2 moveAmount)
    {
        float directionY = Mathf.Sign(moveAmount.y);
        float rayLength = Mathf.Abs(moveAmount.y) + skinWidth;

        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayorigin = (directionY == -1) ? raycasOrigins.bottomLeft : raycasOrigins.topLeft;
            //Start from the point where we will be (+ moveAmount.x)
            rayorigin += Vector2.right * (verticalRaySpacing * i + moveAmount.x);

            //Raycast to up direction
            RaycastHit2D hit = Physics2D.Raycast(rayorigin, Vector2.up * directionY, rayLength, collisionMask);

            Debug.DrawRay(rayorigin, Vector2.up * directionY, Color.red);

            if (hit)
            {

                //Through platforms
                if (hit.collider.tag == "Through")
                {
                    if (directionY == 1 || hit.distance == 0)
                        continue;

                    if (collisions.fallingThroughPlatform)
                        continue;

                    if (plyerInput.y == -1)
                    {
                        collisions.fallingThroughPlatform = true;
                        Invoke("ResetFallingThroughPlatform", .5f);
                        continue;
                    }
                }

                moveAmount.y = (hit.distance - skinWidth) * directionY;
                rayLength = hit.distance;

                //Collision when is climbSlope, we need to recalculated X moveAmount (avoid wear jump)
                if (collisions.climbSlope)
                {
                    moveAmount.x = moveAmount.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sin(moveAmount.x);
                }

                collisions.below = directionY == -1;
                collisions.above = directionY == 1;
            }
        }

        
        //Check from new slope
        if (collisions.climbSlope)
        {
            float directionX = Mathf.Sign(moveAmount.x);
            rayLength = Mathf.Abs(moveAmount.x) + skinWidth;

            //Start from the point where we will be (+ moveAmount.y)
            Vector2 rayOrigin = ((directionX == -1) ? raycasOrigins.bottomLeft : raycasOrigins.bottomRight) + Vector2.up*moveAmount.y;

            //Raycast to right direction
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                //Climb a new slope
                if(slopeAngle != collisions.slopeAngle)
                {
                    moveAmount.x = (hit.distance - skinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                    collisions.slopeNormal = hit.normal;
                }
            }
        }
    }

    void ResetFallingThroughPlatform()
    {
        collisions.fallingThroughPlatform = false;
    }


    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;

        public bool climbSlope, descendingSlope, slidingDownMaxSlope;
        public float slopeAngle, slopeAngleOld;
        public Vector2 slopeNormal;

        public Vector2 moveAmountOld;

        public int faceDir;

        public bool fallingThroughPlatform;

        public void Reset()
        {
            above = below = false;
            left = right = false;
            climbSlope = descendingSlope = slidingDownMaxSlope = false;
            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
            slopeNormal = Vector2.zero;
        }
    }
}
