using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkRigidBody2D))]
public class PlayerController : NetworkComponent
{
    public Rigidbody2D MyRig;
    public Animator AnimationController;

    public float Speed;
    public Vector2 PlayerInput;
    public Vector2 LastMove;
    public float JumpInput;
    public float FireInput;
    public float lastDir;

    float STATE;
    float IDLESTATE = 0;
    float RUNSTATE = 1;
    float JUMPSTATE = 2;
    float FALLSTATE = 3;
    public LayerMask GroundLayer;
    public override void HandleMessage(string flag, string value)
    {
        if (flag == "MOVE" && IsServer)
        {
            string[] args = value.Split(',');
            float h = float.Parse(args[0]);
            float v = float.Parse(args[1]);
            LastMove = new Vector2(h, v);
            JumpInput = v;
        }

        if(flag == "FLIP" && IsClient)
        {
            lastDir = float.Parse(value);
            Flip(lastDir);
        }

        if(flag == "STATE" && IsClient)
        {
            STATE = float.Parse(value);
            AnimationController.SetFloat("State", STATE);
        }

        if(flag == "FIRE" && IsServer)
        {
            FireInput = float.Parse(value);
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
                    SendUpdate("STATE", STATE.ToString());
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
            SendCommand("MOVE", PlayerInput.x + "," + JumpInput);
        }
    }

    public void GetJump(InputAction.CallbackContext context)
    {
        if (IsLocalPlayer)
        {
            JumpInput = context.ReadValue<float>();
            SendCommand("MOVE", PlayerInput.x + "," + JumpInput);
        }
    }

    public void GetFire(InputAction.CallbackContext context)
    {
        if (IsLocalPlayer)
        {
            FireInput = context.ReadValue<float>();
            SendCommand("FIRE", FireInput.ToString());
        }
    }

    public IEnumerator Fire()
    {
        MyCore.NetCreateObject(1, this.Owner, this.transform.position, Quaternion.Euler(this.transform.forward));
        yield return new WaitForSeconds(.6f);
    }
    void Start()
    {
        MyRig = GetComponent<Rigidbody2D>();
        AnimationController = GetComponent<Animator>();
        if (MyRig == null)
        {
            throw new System.Exception("ERROR: Could not find Rigidbody!");
        }
        if (AnimationController == null)
        {
            throw new System.Exception("ERROR: Could not find Animator!");
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

    bool IsGrounded()
    {
        Vector2 position = transform.position;
        Vector2 direction = Vector2.down;
        float distance = 1.5f;

        RaycastHit2D hit = Physics2D.Raycast(position, direction, distance, GroundLayer);
        if(hit.collider != null)
        {
            return true;
        }
        return false;
    }

    private void Update()
    {
        if (IsServer)
        {
            if(FireInput > 0)
            {
                StartCoroutine(Fire());
            }

            MyRig.velocity = new Vector2(LastMove.x * Speed, MyRig.velocity.y);

            if(JumpInput > 0 && IsGrounded())
            {
                MyRig.AddForce(new Vector2(0, 1), ForceMode2D.Impulse);
            }

            if(LastMove.x < 0)
            {
                Flip(0);
            }
            if(LastMove.x > 0)
            {
                Flip(1);
            }
            if(LastMove.x == 0 && IsGrounded())
            {
                STATE = IDLESTATE;
            }
            if(LastMove.x != 0 && IsGrounded())
            {
                STATE = RUNSTATE;
            }
            if (MyRig.velocity.y > 0)
            {
                STATE = JUMPSTATE;
            }
            if (MyRig.velocity.y < 0)
            {
                STATE = FALLSTATE;
            }
            
            AnimationController.SetFloat("State", STATE);
            SendUpdate("STATE", STATE.ToString());
        }
    }
}
