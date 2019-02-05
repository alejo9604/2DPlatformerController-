using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Controller2D))]
public class Player : MonoBehaviour {

    public float jumpHeight = 4;
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
    float jumpVelocity;

    Vector3 velocity;
    float velocityXSmoothing;

    Controller2D controller;

	void Start () {
        controller = GetComponent<Controller2D>();

        //Gravity: deltaMovement = initVel * Time + (acel * time^2 )/2
        //initVel = 0 | acel = gravity | time = timeToJumpApex | deltaMovemnt = jumpHeight
        gravity = - (2 * jumpHeight) / (timeToJumpApex * timeToJumpApex);

        //JumVel: endVel = initVel + acel * Time
        //initVel = 0 | endVel = jumpVelocity | acel = gravity | Time = timeToJumpApex
        jumpVelocity = Mathf.Abs( gravity ) * timeToJumpApex;

        //Debug.Log("Gravity: " + gravity + " Jump Velopcity: " + jumpVelocity);
	}
	
	
	void Update () {

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        int wallDirX = (controller.collisions.left) ? -1 : 1;


        //X Smooth movement
        float targetvelocityX = input.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp(velocity.x, targetvelocityX, ref velocityXSmoothing, (controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirbone);



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

        //Check is in ground
        if (controller.collisions.above || controller.collisions.below)
            velocity.y = 0;


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
                velocity.y = jumpVelocity;
        }

        
        //Apply gravity
        velocity.y += gravity * Time.deltaTime;

        //Move!
        controller.Move(velocity * Time.deltaTime);
	}
}
