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

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        int wallDirX = (controller.collisions.left) ? -1 : 1;


            if(controller.collisions.below)
                velocity.y = jumpVelocity;
        }



        bool wallSliding = false;
        if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0)
        {
            wallSliding = true;

            if (velocity.y < -wallSlideSpeedMax)
                velocity.y = -wallSlideSpeedMax;

            if(timeToWallUnstick > 0)
            {
                velocityXSmoothing = 0;
                velocity.x = 0;
                if (input.x != wallDirX && input.x != 0)
                    timeToWallUnstick -= Time.deltaTime;
                else
                    timeToWallUnstick = wallStickTime;
            }
            else
            {
                timeToWallUnstick = wallStickTime;
            }
        }

        //Jump
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (wallSliding)
            {
                if(wallDirX == input.x)
                {
                    velocity.x = -wallDirX * wallJumpClimb.x;
                    velocity.y = wallJumpClimb.y;
                }
                else if(input.x == 0)
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

            if(controller.collisions.below)
                velocity.y = maxJumpVelocity;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (velocity.y > minJumpVelocity)
            {
                velocity.y = minJumpVelocity;
            }
        }

        
        //Apply gravity
        velocity.y += gravity * Time.deltaTime;

        //Move!
        controller.Move(velocity * Time.deltaTime, input);



        //Check is in ground
        if (controller.collisions.above || controller.collisions.below)
            velocity.y = 0;
    }
}
