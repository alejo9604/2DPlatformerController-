using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Controller2D))]
public class Player : MonoBehaviour {

    public float maxJumpHeight = 4;
    public float minJumpHeight = 1;
    public float timeToJumpApex = .4f;

    float accelerationTimeAirbone = .2f;
    float accelerationTimeGrounded = .1f;

    float moveSpeed = 6;

    public Vector2 wallJumpClimb;
    public Vector2 wallJumpOff;
    public Vector2 wallLeave;

    public float wallSlideSpeedMax = 3;
    public float wallStickTime = .25f;
    float timeToWallUnstick;

    float gravity;
    float maxJumpVelocity;
    float minJumpVelocity;

    Vector3 velocity;
    float velocityXSmoothing;

    Controller2D controller;

    //Inputs
    Vector2 directionalInput;
    int wallDirX;
    bool wallSliding;



    public void SetDirectionalInput(Vector2 input)
    {
        directionalInput = input;
    }


    void Start () {
        controller = GetComponent<Controller2D>();

        //Gravity: deltaMovement = initVel * Time + (acel * time^2 )/2
        //initVel = 0 | acel = gravity | time = timeToJumpApex | deltaMovemnt = jumpHeight
        gravity = - (2 * maxJumpHeight) / (timeToJumpApex * timeToJumpApex);

        //JumVel: endVel = initVel + acel * Time
        //initVel = 0 | endVel = jumpVelocity | acel = gravity | Time = timeToJumpApex
        maxJumpVelocity = Mathf.Abs( gravity ) * timeToJumpApex;


        //Vf = Vi^2 = 2*Accl*displacement
        //Vf = minJumpVelocity | Vi = 0 | Accl = gavit | displacement = minJumpHeight
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);

        //Debug.Log("Gravity: " + gravity + " Jump Velopcity: " + jumpVelocity);
	}
	
	
	void Update () {
        CalculateVelocity();
        HandleWallSlinding();

        //Move!
        controller.Move(velocity * Time.deltaTime, directionalInput);

        //Check is in ground
        if (controller.collisions.above || controller.collisions.below)
        {
            if (controller.collisions.slidingDownMaxSlope)
                velocity.y += controller.collisions.slopeNormal.y * -gravity * Time.deltaTime;
            else
                velocity.y = 0;
        }
    }


    void CalculateVelocity()
    {
        float targetVelocityX = directionalInput.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirbone);
        //Apply gravity
        velocity.y += gravity * Time.deltaTime;
    }

    void HandleWallSlinding()
    {
        wallDirX = (controller.collisions.left) ? -1 : 1;
        wallSliding = false;
        if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0)
        {
            wallSliding = true;

            if (velocity.y < -wallSlideSpeedMax)
                velocity.y = -wallSlideSpeedMax;

            if (timeToWallUnstick > 0)
            {
                velocityXSmoothing = 0;
                velocity.x = 0;
                if (directionalInput.x != wallDirX && directionalInput.x != 0)
                    timeToWallUnstick -= Time.deltaTime;
                else
                    timeToWallUnstick = wallStickTime;
            }
            else
            {
                timeToWallUnstick = wallStickTime;
            }
        }

    }

    public void OnJumpInputDown()
    {

        if (wallSliding)
        {
            if (wallDirX == directionalInput.x)
            {
                velocity.x = -wallDirX * wallJumpClimb.x;
                velocity.y = wallJumpClimb.y;
            }
            else if (directionalInput.x == 0)
            {
                velocity.x = -wallDirX * wallJumpOff.x;
                velocity.y = wallJumpOff.y;
            }
            else
            {
                velocity.x = -wallDirX * wallLeave.x;
                velocity.y = wallLeave.y;
            }
        }

        if (controller.collisions.below)
            if (controller.collisions.slidingDownMaxSlope)
            {
                if (directionalInput.x != -Mathf.Sign(controller.collisions.slopeNormal.x)) //Not jumping against max slope
                {
                    velocity.y = maxJumpVelocity * controller.collisions.slopeNormal.y;
                    velocity.x = maxJumpVelocity * controller.collisions.slopeNormal.x;
                }
            }
            else
            {
                velocity.y = maxJumpVelocity;
            }
    }

    public void OnJumpInputUp()
    {
        if (velocity.y > minJumpVelocity)
        {
            velocity.y = minJumpVelocity;
        }
    }
}
