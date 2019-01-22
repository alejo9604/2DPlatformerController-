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

        //Check is in ground
        if (controller.collisions.above || controller.collisions.below)
            velocity.y = 0;

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        //Jump
        if (Input.GetKeyDown(KeyCode.Space) && controller.collisions.below)
            velocity.y = jumpVelocity;

        //X Smooth movement
        float targetvelocityX = input.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp(velocity.x, targetvelocityX, ref velocityXSmoothing, (controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirbone);

        //Apply gravity
        velocity.y += gravity * Time.deltaTime;

        //Move!
        controller.Move(velocity * Time.deltaTime);
	}
}
