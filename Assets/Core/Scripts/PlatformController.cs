using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformController : RaycastController{

    public LayerMask passengerMask;
    public Vector3 move;

    List<PassangerMovement> passangerMovements;
    Dictionary<Transform, Controller2D> passengerDictionary = new Dictionary<Transform, Controller2D>();

    public override void Start()
    {
        base.Start();
    }


    private void Update()
    {
        UpdateRaycastOrigins();

        Vector3 velocity = move * Time.deltaTime;

        CalcualtePassengerMovement(velocity);

        MovePassangers(true);
        transform.Translate(velocity);
        MovePassangers(false);
    }


    void MovePassangers(bool beforeMovePlatform)
    {
        foreach(PassangerMovement passenger in passangerMovements)
        {
            if (!passengerDictionary.ContainsKey(passenger.transform))
                passengerDictionary.Add(passenger.transform, passenger.transform.GetComponent<Controller2D>());

            if (passenger.moveBeforePlatform == beforeMovePlatform)
            {
                passengerDictionary[passenger.transform].Move(passenger.velocity, passenger.standingOnPlatform);
            }
        }
    }


    void CalcualtePassengerMovement(Vector3 velocity)
    {
        HashSet<Transform> movedPassanger = new HashSet<Transform>();
        passangerMovements = new List<PassangerMovement>();


        float directionX = Mathf.Sign(velocity.x);
        float directionY = Mathf.Sign(velocity.y);

        //Vertially moving platform: Move up + Transform up || Move down + Trasnform down
        if (velocity.y != 0)
        {
            float rayLength = Mathf.Abs(velocity.y) + skinWidth;

            for (int i = 0; i < verticalRayCount; i++)
            {
                Vector2 rayorigin = (directionY == -1) ? raycasOrigins.bottomLeft : raycasOrigins.topLeft;
                rayorigin += Vector2.right * (verticalRaySpacing * i);

                //Raycast to up direction
                RaycastHit2D hit = Physics2D.Raycast(rayorigin, Vector2.up * directionY, rayLength, passengerMask);

                if (hit)
                {
                    if (!movedPassanger.Contains(hit.transform))
                    {

                        movedPassanger.Add(hit.transform);

                        float pushX = (directionY == 1) ? velocity.x : 0;
                        float pushY = velocity.y - (hit.distance - skinWidth) * directionY;


                        passangerMovements.Add(new PassangerMovement(hit.transform, new Vector3(pushX, pushY), directionY == 1, true));
                    }
                }
            }
        }


        //Horizontally moving platform: Pushing th eTrasnform horizontal
        if (velocity.x != 0)
        {
            float rayLength = Mathf.Abs(velocity.x) + skinWidth;

            for (int i = 0; i < horizontalRayCount; i++)
            {
                Vector2 rayorigin = (directionX == -1) ? raycasOrigins.bottomLeft : raycasOrigins.bottomRight;
                rayorigin += Vector2.up * (horizontalRaySpacing * i);

                //Raycast to right direction
                RaycastHit2D hit = Physics2D.Raycast(rayorigin, Vector2.right * directionX, rayLength, passengerMask);

                if (hit)
                {
                    if (!movedPassanger.Contains(hit.transform))
                    {

                        movedPassanger.Add(hit.transform);

                        float pushX = velocity.x - (hit.distance - skinWidth) * directionX;
                        float pushY = -skinWidth; // For prevent NO Y movement and alow calculate collision

                        passangerMovements.Add(new PassangerMovement(hit.transform, new Vector3(pushX, pushY), false, true));
                    }
                }
            }

        }


        //Passagern is on top of a horizontally or downward moving platform
        if(directionY == -1 || (velocity.y == 0 && velocity.x != 0))
        {
            float rayLength = skinWidth*2;

            for (int i = 0; i < verticalRayCount; i++)
            {
                //Only top dir.
                Vector2 rayorigin = raycasOrigins.topLeft + Vector2.right * (verticalRaySpacing * i); ;

                //Raycast to Only! up direction
                RaycastHit2D hit = Physics2D.Raycast(rayorigin, Vector2.up, rayLength, passengerMask);

                if (hit)
                {
                    if (!movedPassanger.Contains(hit.transform))
                    {

                        movedPassanger.Add(hit.transform);

                        float pushX = velocity.x;
                        float pushY = velocity.y;

                        passangerMovements.Add(new PassangerMovement(hit.transform, new Vector3(pushX, pushY), true, false));
                    }
                }
            }
        }

    }


    struct PassangerMovement
    {
        public Transform transform;
        public Vector3 velocity;
        public bool standingOnPlatform;
        public bool moveBeforePlatform;

        public PassangerMovement(Transform _transform, Vector3 _velocity, bool _standingOnPlatform, bool _moveBeforePlatform)
        {
            transform = _transform;
            velocity = _velocity;
            standingOnPlatform = _standingOnPlatform;
            moveBeforePlatform = _moveBeforePlatform;
        }
    }

}
