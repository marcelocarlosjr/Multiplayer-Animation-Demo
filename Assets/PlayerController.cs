using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkRigidBody2D))]
public class PlayerController : NetworkComponent
{
    public float Speed;
    public Rigidbody2D MyRig;
    public Vector2 PlayerInput;
    public Vector2 LastMove;
    public float lastDir;
    public float AnimationState;

    float IDLESTATE = 0;
    float RUNSTATE = 1;
    float JUMPSTATE = 2;
    public override void HandleMessage(string flag, string value)
    {
        if (flag == "MOVE" && IsServer)
        {
            string[] args = value.Split(',');
            float h = float.Parse(args[0]);
            float v = float.Parse(args[1]);
            LastMove = new Vector2(h, v);
        }

        if(flag == "FLIP" && IsClient)
        {
            lastDir = float.Parse(value);
            Flip(lastDir);
        }

        if(flag == "STATE" && IsClient)
        {

        }
    }

    public override void NetworkedStart()
    {
        
    }

    public override IEnumerator SlowUpdate()
    {
        while (IsConnected)
        {
            if (IsClient)
            {

            }
            if (IsServer)
            {
                if (IsDirty)
                {
                    SendUpdate("FLIP", lastDir.ToString());
                    IsDirty = false;
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    public void GetMovement(InputAction.CallbackContext context)
    {
        if (IsLocalPlayer)
        {
            PlayerInput = context.ReadValue<Vector2>();
            SendCommand("MOVE", PlayerInput.x + "," + PlayerInput.y);
        }
    }
    void Start()
    {
        MyRig = GetComponent<Rigidbody2D>();
        if (MyRig == null)
        {
            throw new System.Exception("ERROR: Could not find Rigidbody!");
        }
    }

    public void Flip(float dir)
    {
        if(dir == 0)
        {
            this.GetComponent<SpriteRenderer>().flipX = true;
        }
        if(dir == 1)
        {
            this.GetComponent<SpriteRenderer>().flipX = false;
        }
        if (IsServer)
        {
            SendUpdate("FLIP", dir.ToString());
        }
    }

    private void Update()
    {
        if (IsServer)
        {
            MyRig.velocity = LastMove * Speed;

            if(LastMove.x < 0)
            {
                Flip(0);

            }
            if(LastMove.x > 0)
            {
                Flip(1);
            }
        }
    }
}
