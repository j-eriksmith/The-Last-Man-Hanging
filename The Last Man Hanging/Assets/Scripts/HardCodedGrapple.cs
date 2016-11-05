﻿using UnityEngine;
using System.Collections;
[RequireComponent(typeof(Rigidbody2D))]
public class HardCodedGrapple : MonoBehaviour {

    // Use this for initialization
    
    [SerializeField] public int PlayerNumber; //must be 1 digit
    bool LMBDepressed;
    bool RMBDepressed;
    [SerializeField] public float maxRopeLength;
    [SerializeField] public float minRopeLength;
    public bool LHookOut;
    public bool RHookOut;
    GameObject hookL;
    GameObject hookR; //hahaha    
    float directionVectorRotation;

    public float slackLength;
    Vector2 velocity;
    float mass;
    Vector2 hookedPosition;
    float distance;
    Vector2 directionVector;
    Vector2 rotatedDirectionVector;
    Vector2 rotatedVelocityVector;
    Vector2 tension;
    float springConstant;
    Vector2 springForce;
    Vector2 impulse;

    bool CLIMBUP = false;
    bool CLIMBDOWN = false;

    void Start ()
    {
        
        LMBDepressed = false;
        RMBDepressed = false;
        LHookOut = false;
        RHookOut = false;
        if (PlayerNumber == 0 )
        {
            PlayerNumber = 1;
        }
        if (slackLength == 0)
        {
            slackLength = 1;
        }
        if(maxRopeLength == 0)
        {
            maxRopeLength = 1;

        }
    }


    Vector2 rotateVectorPlane(Vector2 start, float degrees) //degrees is positive counter clockwise, negative clockwise.
    {
        Vector2 toReturn = new Vector2(0,0);
        
        float tan = Mathf.Tan(degrees* 6.28318530718f/360f);
        toReturn.x = 1;
        toReturn.y = 1;
        //now to determine which quadrant the point will lie in in the new plane
        if((tan*start.x > start.y))
        {
            toReturn.y = -1;
        //point is above the new x axis
        }
        if((-start.y*tan > start.x))  //-1/tan * xy = yp   -yp*tan = xy if -yp*tan < xp
        {
            toReturn.x = -1;
            //point is to the right of the y axis
        }
        toReturn.y *= Mathf.Sqrt(Mathf.Pow( (start.x + start.y *tan)/(tan*(tan + 1/tan)) - start.x,2) + 
            Mathf.Pow((start.x + start.y * tan) / ((tan + 1 / tan)) -start.y ,2)  );
        toReturn.x *= Mathf.Sqrt(Mathf.Pow(start.x, 2) + Mathf.Pow(start.y, 2) - Mathf.Pow(toReturn.y, 2)) ; 
        if (Mathf.Abs(toReturn.x) <= .002 | float.IsNaN(toReturn.x))
        {
            toReturn.x = 0;
        }
        if(degrees < -90 |degrees > 90)
        {
            toReturn.y *= -1;
            toReturn.x *=  -1;
        }
        return toReturn;
    }

    float DotProduct(Vector2 a, Vector2 b)
    {
        return (a.x * b.x + a.y * b.y);
    }
    Vector2 Perpindicularize(Vector2 a, bool clockwise)
    {
        Vector2 b = new Vector2(0,0);
        bool posX = false;
        bool posY = false;
        if(a.x > 0)
        {
            posX = true;
        }
        if(a.y > 0)
        {
            posY = true;
        }
        if(clockwise)
        {
            if(posX)
            {
                if(posY)
                {
                    b.y = -a.x;
                    b.x = a.y;
                }
                if(!posY)
                {
                    b.x = a.y;
                    b.y = -a.x;
                }
            }
            if(!posX)
            {
                if (posY)
                {
                    b.x = a.y;
                    b.y = -a.x;
                }
                if (!posY)
                {
                    b.x = a.y;
                    b.y = -a.x;
                }
            }

        }
        else
        {
            if (posX)
            {
                if (posY)
                {
                    b.x = -a.y;
                    b.y = a.x;
                }
                if (!posY)
                {
                    b.y = a.x;
                    b.x = -a.y;
                }
            }
            if (!posX)
            {
                if (posY)
                {
                    b.x = -a.y;
                    b.y = a.x;
                }
                if (!posY)
                {
                    b.x = -a.y;
                    b.y = a.x;
                }
            }

        }
        return b;

    }
    
    void Swing(char LorR)
        
    {
        mass = GetComponent<Rigidbody2D>().mass;
        velocity = GetComponent<Rigidbody2D>().velocity;
        if (LorR == 'L')
        {
            hookedPosition = hookL.transform.position;
            distance = Vector2.Distance(transform.position, hookedPosition);

            Vector2 directionVector = new Vector2((hookedPosition.x - transform.position.x), (hookedPosition.y - transform.position.y));

            if (directionVector.x >= 0)
            {
                if (directionVector.y > 0)
                {
                    directionVectorRotation = (Mathf.Atan(directionVector.y / directionVector.x) * 360f / 6.283185307f - 90); //q1
                }
                else
                {
                    directionVectorRotation = (Mathf.Atan(directionVector.y / directionVector.x) * 360f / 6.283185307f - 90); // q4
                }
            }
            else
            {
                if (directionVector.y > 0)
                {
                    directionVectorRotation = (Mathf.Atan(directionVector.y / directionVector.x) * 360f / 6.283185307f + 90); // q2
                }
                else
                {
                    directionVectorRotation = (Mathf.Atan(directionVector.y / directionVector.x) * 360f / 6.283185307f + 90); //q3
                }
            }


            if (CLIMBUP)
            {
                slackLength *= .99f;
                CLIMBUP = false;
            }
            if (CLIMBDOWN)
            {
                slackLength *= 1.01f;
                CLIMBDOWN = false;
            }

            rotatedDirectionVector = rotateVectorPlane(directionVector, directionVectorRotation);
            rotatedVelocityVector = rotateVectorPlane(velocity, directionVectorRotation);
            directionVector.Normalize();
            impulse = directionVector * Mathf.Pow(rotatedVelocityVector.x, 2) / distance;
            // print(impulse.x + "       " + impulse.y + "        " + directionVectorRotation + " " + distance);
            GetComponent<Rigidbody2D>().AddForce(mass * impulse * Time.deltaTime, ForceMode2D.Impulse);
            tension = -directionVector * rotatedVelocityVector.y;
            GetComponent<Rigidbody2D>().AddForce(mass * tension * Time.deltaTime, ForceMode2D.Impulse);
            if (distance > slackLength)
            {
                springConstant = 25;
                springForce = directionVector * springConstant * (distance - slackLength) * mass; //if spring force exceeds a value break the connection

                if (distance - slackLength > 25/springConstant & rotatedVelocityVector.y < 0)
                {
                    GetComponent<Rigidbody2D>().velocity = new Vector2((velocity + -rotatedVelocityVector.y * directionVector).x, (velocity + -rotatedVelocityVector.y * directionVector).y);
                }
                //Vector2 dampingForce = -directionVector * dampingPower * rotatedVelocityVector.y;
                GetComponent<Rigidbody2D>().AddForce((springForce) * Time.deltaTime, ForceMode2D.Impulse);
                // transform.position += (distance - slackLength) * new Vector3 (directionVector.x, directionVector.y);
            }
        }
        if (LorR == 'R')
        {
            hookedPosition = hookR.transform.position;

            distance = Mathf.Abs(Vector2.Distance(transform.position, hookedPosition));

            directionVector = new Vector2((hookedPosition.x - transform.position.x), (hookedPosition.y - transform.position.y));

            if (directionVector.x >= 0)
            {
                if(directionVector.y > 0)
                {
                    directionVectorRotation = (Mathf.Atan(directionVector.y / directionVector.x) * 360f / 6.283185307f - 90); //q1
                }
                else
                {
                    directionVectorRotation = (Mathf.Atan(directionVector.y / directionVector.x) * 360f / 6.283185307f - 90); // q4
                }
            }
            else
            {
                if (directionVector.y > 0)
                {
                    directionVectorRotation = (Mathf.Atan(directionVector.y / directionVector.x) * 360f / 6.283185307f + 90); // q2
                }
                else
                {
                    directionVectorRotation = (Mathf.Atan(directionVector.y / directionVector.x) * 360f / 6.283185307f + 90); //q3
                }
            }


            if (CLIMBUP)
            {
                slackLength *= .99f;
                CLIMBUP = false;
            }
            if (CLIMBDOWN)
            {
                slackLength *= 1.01f;
                CLIMBDOWN = false;
            }

            rotatedDirectionVector = rotateVectorPlane(directionVector, directionVectorRotation);
            rotatedVelocityVector = rotateVectorPlane(velocity, directionVectorRotation);
            directionVector.Normalize();
            impulse = directionVector * Mathf.Pow(rotatedVelocityVector.x, 2) / distance;
           // print(impulse.x + "       " + impulse.y + "        " + directionVectorRotation + " " + distance);
            GetComponent<Rigidbody2D>().AddForce(mass * impulse * Time.deltaTime, ForceMode2D.Impulse);
            tension = -directionVector * rotatedVelocityVector.y;
            GetComponent<Rigidbody2D>().AddForce(mass * tension * Time.deltaTime, ForceMode2D.Impulse);
            if (distance > slackLength)
            {
                springConstant = 25;
                springForce = directionVector * springConstant * (distance - slackLength) * mass; //if spring force exceeds a value break the connection
                
                if (distance - slackLength > 25/springConstant  & rotatedVelocityVector.y <0)
                {
                    GetComponent<Rigidbody2D>().velocity = new Vector2((velocity + -rotatedVelocityVector.y * directionVector).x, (velocity + -rotatedVelocityVector.y * directionVector).y);
                }
                //Vector2 dampingForce = -directionVector * dampingPower * rotatedVelocityVector.y;
                GetComponent<Rigidbody2D>().AddForce((springForce )* Time.deltaTime, ForceMode2D.Impulse);
                // transform.position += (distance - slackLength) * new Vector3 (directionVector.x, directionVector.y);
            }
            
        }
    }



	void Update ()
    {

        if (Input.GetKey(KeyCode.Mouse0) & ! LHookOut) 
        {            
            hookL = (GameObject)(Instantiate(Resources.Load("HookPrefab")));
            LHookOut = true;
            hookL.GetComponent<HookObject>().Throw(gameObject, 'L');
        }
        LMBDepressed = Input.GetKey(KeyCode.Mouse0);
        if(LHookOut)
        {
            hookL.GetComponent<HookObject>().playerPosition = transform.position;
            if(!LMBDepressed & !hookL.GetComponent<HookObject>().RETURN)
            {
                hookL.GetComponent<HookObject>().RETURN = true;
            }
            if (LMBDepressed & hookL.GetComponent<HookObject>().isTensioned)
            {
                Swing('L');
            }
        }
        // same thing for right hook
        if (Input.GetKey(KeyCode.Mouse1) & !RHookOut) // check that last frame lmb wasnt down and now it is.
        {
            hookR = (GameObject)(Instantiate(Resources.Load("HookPrefab")));
            RHookOut = true;
            hookR.GetComponent<HookObject>().Throw(gameObject , 'R');
        }
        RMBDepressed = Input.GetKey(KeyCode.Mouse1);
        if (RHookOut)
        {
            hookR.GetComponent<HookObject>().playerPosition = transform.position;
            if (!RMBDepressed & !hookR.GetComponent<HookObject>().RETURN)
            {
                hookR.GetComponent<HookObject>().RETURN = true;
            }
            if (RMBDepressed & hookR.GetComponent<HookObject>().isTensioned)
            {

                Swing('R');
            }
        }
        if(Input.GetKey(KeyCode.W) &  slackLength>minRopeLength*1.02)
        {
            CLIMBUP = true;
        }
        if(Input.GetKey(KeyCode.S) & slackLength<maxRopeLength*.98)
        {
            CLIMBDOWN = true; 
        }




    }
}
/*SO lets iron out the conditions im doing. On keypress Left Click, spawn a Grapple Object with Velocity V and trajectory
to the mouse pointer.

 The Grapple Object is has a rigidbody. The only thing necessary in its script is that it attaches , and sets its center to 
 the things center that it grappled on to. It is generated from a prefab on buttonpress at the character location, 
 using     GameObject go = (GameObject)Instantiate(Resources.Load("MyPrefab")); ;
 fixes itself to whatever it hits as long as the button is being held down, and comes back at a fixed velocity. The
 player should have a counter for each one he has out, and if it is at 2, he cannot make more. the counter is incremented when
 one is thrown, and decremented when it is lost. 

 
 When it is attached and the player is at a distance d from the point, the rope becomes tensioned and the player starts to 
 swing, enacting a new set of forces on the player.*/