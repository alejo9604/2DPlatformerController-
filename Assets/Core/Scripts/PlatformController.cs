using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformController : RaycastController {

    public LayerMask passengerMask;
    

    public Vector3[] localWaypoints;
    Vector3[] globalWaypoints;

    public float speed;
    public bool cyclic;
    public float waitTime;
    float nextMoveTime;
    [Range(0,2)]
    public float easeAmount;

    int fromWaypointIndex;
    float percentBetweenWaypoints;


    List<PassangerMovement> passangerMovements;
    Dictionary<Transform, Controller2D> passengerDictionary = new Dictionary<Transform, Controller2D>();

    public override void Start()
    {
        base.Start();

        globalWaypoints = new Vector3[localWaypoints.Length];
        for (int i = 0; i < globalWaypoints.Length; i++)
        {
            globalWaypoints[i] = localWaypoints[i] + transform.position;
        }
    }


    private void Update()
    {
        UpdateRaycastOrigins();

        Vector3 velocity = CalculatePlatformMovement() ;

        CalcualtePassengerMovement(velocity);

        MovePassangers(true);
        transform.Translate(velocity);
        MovePassangers(false);
    }


    float Ease(float x)
    {
        float a = easeAmount + 1;
        //TODO: Not use Mathf.pow
        return Mathf.Pow(x, a) / (Mathf.Pow(x, a) + Mathf.Pow(1 - x, a));
    }

    Vector3 CalculatePlatformMovement()
    {
        if (Time.time < nextMoveTime)
            return Vector3.zero;

        fromWaypointIndex %= globalWaypoints.Length;
        int toWaypointindex = (fromWaypointIndex + 1) % globalWaypoints.Length;
        float distanceBetweenWaypoints = Vector3.Distance(globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointindex]);
        percentBetweenWaypoints += Time.deltaTime * speed/distanceBetweenWaypoints;
        percentBetweenWaypoints = Mathf.Clamp01(percentBetweenWaypoints);

        float easePercentBetweenWaypoints = Ease(percentBetweenWaypoints);

        Vector3 newPosition = Vector3.Lerp(globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointindex], easePercentBetweenWaypoints);

        if(percentBetweenWaypoints >= 1)
        {
            percentBetweenWaypoints = 0;
            fromWaypointIndex++;

            if (!cyclic)
            {

                if (fromWaypointIndex >= globalWaypoints.Length - 1)
                {
                    fromWaypointIndex = 0;
                    System.Array.Reverse(globalWaypoints);
                }
            }
            nextMoveTime = Time.time + waitTime;
        }

        return newPosition - transform.position;
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

                if (hit && hit.distance != 0)
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

                if (hit && hit.distance != 0)
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

                if (hit && hit.distance != 0)
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



    private void OnDrawGizmos()
    {
        if(localWaypoints != null)
        {
            Gizmos.color = Color.red;
            float size = .3f;

            for (int i = 0; i < localWaypoints.Length; i++)
            {
                Vector3 globalWaypointPosition =  (Application.isPlaying) ? globalWaypoints[i] : localWaypoints[i] + transform.position;
                Gizmos.DrawLine(globalWaypointPosition - Vector3.up * size, globalWaypointPosition + Vector3.up * size);
                Gizmos.DrawLine(globalWaypointPosition - Vector3.left * size, globalWaypointPosition + Vector3.left * size);
            }
        }
    }
}
