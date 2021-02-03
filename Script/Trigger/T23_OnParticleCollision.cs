
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class T23_OnParticleCollision : UdonSharpBehaviour
{
    public int groupID;

    // 用途・処理方法不明
    //[SerializeField]
    //private bool triggerIndividuals = true;

    [SerializeField]
    private LayerMask layers = 1;

    private T23_BroadcastLocal broadcastLocal;
    private T23_BroadcastGrobal broadcastGrobal;

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

    private void OnParticleCollision(GameObject other)
    {
        if ((layers.value & 1 << other.layer) == 0) { return; }

        Trigger();
    }

    public override void OnPlayerParticleCollision(VRCPlayerApi player)
    {
        if (player == Networking.LocalPlayer)
        {
            if ((layers.value & 1 << LayerMask.NameToLayer("PlayerLocal")) == 0) { return; }
        }
        else
        {
            if ((layers.value & 1 << LayerMask.NameToLayer("Player")) == 0) { return; }
        }

        Trigger();
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
