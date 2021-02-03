
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class T23_OnNetworkReady : UdonSharpBehaviour
{
    public int groupID;

    private T23_BroadcastLocal broadcastLocal;
    private T23_BroadcastGrobal broadcastGrobal;

    [UdonSynced(UdonSyncMode.None)]
    private bool syncReady;

    private bool synced = false;

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

        if (Networking.IsOwner(gameObject))
        {
            syncReady = true;
        }
    }

    void Update()
    {
        if (!synced && syncReady)
        {
            Trigger();
            synced = true;
        }
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
