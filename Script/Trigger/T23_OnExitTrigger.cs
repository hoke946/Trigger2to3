
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class T23_OnExitTrigger : UdonSharpBehaviour
{
    public int groupID;

    [SerializeField]
    private bool triggerIndividuals = true;

    [SerializeField]
    private LayerMask layers = 1;

    private T23_BroadcastLocal broadcastLocal;
    private T23_BroadcastGrobal broadcastGrobal;

    private Collider enterCollider;
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

    private void OnTriggerEnter(Collider other)
    {
        if ((layers.value & 1 << other.gameObject.layer) == 0) { return; }

        if (!triggerIndividuals)
        {
            if (enterCollider) { return; }
            if (enterPlayer != null) { return; }
        }

        enterCollider = other;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!enterCollider || enterCollider && enterCollider != other) { return; }

        Trigger();

        enterCollider = null;
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
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
            if (enterCollider) { return; }
            if (enterPlayer != null) { return; }
        }

        enterPlayer = player;
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (enterPlayer == null || enterPlayer != null && enterPlayer != player) { return; }

        Trigger();

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
