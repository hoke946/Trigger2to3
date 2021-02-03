
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class T23_SetParticlePlaying : UdonSharpBehaviour
{
    public int groupID;

    [SerializeField]
    private GameObject[] recievers;

    [SerializeField]
    private bool toggle;

    [SerializeField]
    [Tooltip("if not toggle")]
    private bool operation;

    [SerializeField]
    private bool takeOwnership;

    private bool executing = false;
    private bool[] executed;
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
            for (int i = 0; i < recievers.Length; i++)
            {
                if (recievers[i])
                {
                    if (!executed[i])
                    {
                        if (Networking.IsOwner(recievers[i]))
                        {
                            Execute(recievers[i]);
                            executed[i] = true;
                        }
                        else
                        {
                            failure = true;
                        }
                    }
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

        for (int i = 0; i < recievers.Length; i++)
        {
            if (recievers[i])
            {
                if (takeOwnership)
                {
                    Networking.SetOwner(Networking.LocalPlayer, recievers[i]);
                    executing = true;
                    executed = new bool[recievers.Length];
                    waitTimer = 0;
                }
                else
                {
                    Execute(recievers[i]);
                }
            }
        }
    }

    private void Execute(GameObject target)
    {
        ParticleSystem particle = target.GetComponent<ParticleSystem>();

        if (toggle)
        {
            if (particle.isPlaying)
            {
                particle.Stop();
            }
            else
            {
                particle.Play();
            }
        }
        else
        {
            if (operation)
            {
                particle.Play();
            }
            else
            {
                particle.Stop();
            }
        }
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
