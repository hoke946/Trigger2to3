
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

public class T23_CallUdonMethod : UdonSharpBehaviour
{
    public int groupID;

    [SerializeField]
    private UdonBehaviour udonBehaviour;
    
    [SerializeField]
    private string method;

    [SerializeField]
    private bool network;

    [SerializeField]
    private NetworkEventTarget target;

    [SerializeField]
    [Tooltip("if local")]
    private bool takeOwnership;

    private bool executing = false;
    private bool executed;
    private float waitTimer;

    [SerializeField, Range(0, 1)]
    private float randomAvg;

    private float randomMin = 0;
    private float randomMax = 0;

    private T23_BroadcastLocal broadcastLocal;
    private T23_BroadcastGrobal broadcastGrobal;

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

        if (broadcastLocal)
        {
            broadcastLocal.AddActions(this);

            if (broadcastLocal.randomize)
            {
                randomMin = broadcastLocal.randomTotal;
                broadcastLocal.randomTotal += randomAvg;
                randomMax = broadcastLocal.randomTotal;
            }
        }
        else
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

            if (broadcastGrobal)
            {
                broadcastGrobal.AddActions(this);

                if (broadcastGrobal.randomize)
                {
                    randomMin = broadcastGrobal.randomTotal;
                    broadcastGrobal.randomTotal += randomAvg;
                    randomMax = broadcastGrobal.randomTotal;
                }
            }
        }

#if UNITY_EDITOR
        // local simulation
        takeOwnership = false;
#endif
    }

    void Update()
    {
        if (executing)
        {
            bool failure = false;
            if (!executed)
            {
                if (Networking.IsOwner(udonBehaviour.gameObject))
                {
                    Execute();
                    executed = true;
                }
                else
                {
                    failure = true;
                }
            }

            if (!failure)
            {
                executing = false;
            }

            waitTimer += Time.deltaTime;
            if (waitTimer > 5)
            {
                executing = false;
            }
        }
    }

    public void Action()
    {
        if (!RandomJudgement()) { return; }

        if (network)
        {
#if UNITY_EDITOR
            // local simulation
            udonBehaviour.SendCustomEvent(method);
#else
            udonBehaviour.SendCustomNetworkEvent(target, method);
#endif
        }
        else
        {
            if (takeOwnership)
            {
                Networking.SetOwner(Networking.LocalPlayer, udonBehaviour.gameObject);
                executing = true;
                executed = false;
                waitTimer = 0;
            }
            else
            {
                Execute();
            }
        }
    }

    private void Execute()
    {
        udonBehaviour.SendCustomEvent(method);
    }

    private bool RandomJudgement()
    {
        if (broadcastLocal)
        {
            if (!broadcastLocal.randomize || (broadcastLocal.randomValue >= randomMin && broadcastLocal.randomValue < randomMax))
            {
                return true;
            }
        }
        else if (broadcastGrobal)
        {
            if (!broadcastGrobal.randomize || (broadcastGrobal.randomValue >= randomMin && broadcastGrobal.randomValue < randomMax))
            {
                return true;
            }
        }

        return false;
    }
}
