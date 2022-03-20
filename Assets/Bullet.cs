using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class Bullet : NetworkComponent
{
    public Rigidbody2D MyRig;
    public float Speed;
    public override void HandleMessage(string flag, string value)
    {
        
    }

    public override void NetworkedStart()
    {

    }

    public override IEnumerator SlowUpdate()
    {
        yield return new WaitForSeconds(0.1f);
    }

    private void Start()
    {
        if (IsServer)
        {
            MyRig = GetComponent<Rigidbody2D>();
        }
    }

    private void Update()
    {
        if (IsServer)
        {
            MyRig.velocity = this.transform.forward * Speed;
        }
    }
}
