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
    public bool Shooting;
    public bool Jumping;
    public float FaceDir;
    public bool FireAnimation;

    float STATE;
    float IDLESTATE = 0;
    float RUNSTATE = 1;
    float JUMPSTATE = 2;
    float FALLSTATE = 3;
    float ATTACKSTATE = 4;
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
            if (FireInput > 0)
            {
                if (!Shooting)
                {
                    StartCoroutine(Fire());
                }
            }
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

    public IEnumerator FireAnim()
    {
        FireAnimation = true;
        STATE = ATTACKSTATE;
        yield return new WaitForSeconds(0.3f);
        FireAnimation = false;
    }

    public IEnumerator Fire()
    {
        if (IsServer)
        {
            Shooting = true;
            StartCoroutine(FireAnim());
            MyCore.NetCreateObject(1, this.Owner, this.transform.position, Quaternion.Euler(new Vector3 (0,0,FaceDir)));
            yield return new WaitForSeconds(1f);
            Shooting = false;
        }
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
            FaceDir = 180;
        }
        if(dir == 1)
        {
            this.GetComponent<SpriteRenderer>().flipX = false;
            FaceDir = 0;
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
        float distance = 1.2f;

        RaycastHit2D hit = Physics2D.Raycast(position, direction, distance, GroundLayer);
        if(hit.collider != null)
        {
            return true;
        }
        return false;
    }
    public IEnumerator Jump()
    {
        if (IsServer)
        {
            Jumping = true;
            MyRig.AddForce(new Vector2(0, 12), ForceMode2D.Impulse);
            yield return new WaitForSeconds(.2f);
            Jumping = false;
        }
    }

    private void Update()
    {
        if (IsServer)
        {
            MyRig.velocity = new Vector2(LastMove.x * Speed, MyRig.velocity.y);

            if(JumpInput > 0 && IsGrounded())
            {
                if (!Jumping)
                {
                    StartCoroutine(Jump());
                }
            }
            if(LastMove.x < 0)
            {
                Flip(0);
            }
            if(LastMove.x > 0)
            {
                Flip(1);
            }
            if(LastMove.x == 0 && IsGrounded() && !FireAnimation)
            {
                STATE = IDLESTATE;
            }
            if(LastMove.x != 0 && IsGrounded() && !FireAnimation)
            {
                STATE = RUNSTATE;
            }
            if (MyRig.velocity.y > 0 && !FireAnimation)
            {
                STATE = JUMPSTATE;
            }
            if (MyRig.velocity.y < 0 && !FireAnimation)
            {
                STATE = FALLSTATE;
            }
            
            AnimationController.SetFloat("State", STATE);
            SendUpdate("STATE", STATE.ToString());
        }
    }
}
