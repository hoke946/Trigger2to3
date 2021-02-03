
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class T23_OnEnterCollider : UdonSharpBehaviour
{
    public int groupID;

    [SerializeField]
    private bool triggerIndividuals = true;
    
    [SerializeField]
    private LayerMask layers = 1;

    private T23_BroadcastLocal broadcastLocal;
    private T23_BroadcastGrobal broadcastGrobal;

    private Collision enterCollider;
    private VRCPlayerApi enterPlayer;

    void Start()
    {
        T23_BroadcastLocal[] broadcastLocals = GetComponents<T23_BroadcastLocal>();
        for (int i = 0; i < broadcastLocals.Length; i++)
        {
            if (broadcastLocals[i].groupID == groupID)
            {
                broadcastLocal = broadcastLocals[i];
                break;
            }
        }

        if (!broadcastLocal)
        {
            T23_BroadcastGrobal[] broadcastGrobals = GetComponents<T23_BroadcastGrobal>();
            for (int i = 0; i < broadcastGrobals.Length; i++)
            {
                if (broadcastGrobals[i].groupID == groupID)
                {
                    broadcastGrobal = broadcastGrobals[i];
                    break;
                }
            }
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if ((layers.value & 1 << other.gameObject.layer) == 0) { return; }

        if (!triggerIndividuals)
        {
            if (enterCollider != null) { return; }
            if (enterPlayer != null) { return; }
        }

        Trigger();

        enterCollider = other;
    }

    private void OnCollisionExit(Collision other)
    {
        if (enterCollider == null || enterCollider != null && enterCollider != other) { return; }

        enterCollider = null;
    }

    public override void OnPlayerCollisionEnter(VRCPlayerApi player)
    {
        if (player == Networking.LocalPlayer)
        {
            if ((layers.value & 1 << LayerMask.NameToLayer("PlayerLocal")) == 0) { return; }
        }
        else
        {
            if ((layers.value & 1 << LayerMask.NameToLayer("Player")) == 0) { return; }
        }

        if (!triggerIndividuals)
        {
            if (enterCollider != null) { return; }
            if (enterPlayer != null) { return; }
        }

        Trigger();

        enterPlayer = player;
    }

    public override void OnPlayerCollisionExit(VRCPlayerApi player)
    {
        if (enterPlayer == null || enterPlayer != null && enterPlayer != player) { return; }

        enterPlayer = null;
    }

    private void Trigger()
    {
        if (broadcastLocal)
        {
            broadcastLocal.Trigger();
        }
        else if (broadcastGrobal)
        {
            broadcastGrobal.Trigger();
        }
    }
}
