
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class T23_OnTimer : UdonSharpBehaviour
{
    public int groupID;

    [SerializeField]
    private bool repeat;

    [SerializeField]
    private bool resetOnEnable;

    [SerializeField]
    private float lowPeriodTime;

    [SerializeField]
    private float highPeriodTime;

    private T23_BroadcastLocal broadcastLocal;
    private T23_BroadcastGrobal broadcastGrobal;

    private float timer;
    private float nextPeriodTime;
    private bool finished;

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

        ResetTime();
    }

    void Update()
    {
        if (finished) { return; }

        timer += Time.deltaTime;

        if (timer > nextPeriodTime)
        {
            Trigger();

            if (repeat)
            {
                ResetTime();
            }
            else
            {
                finished = true;
            }
        }
    }

    private void ResetTime()
    {
        finished = false;
        timer = 0;
        if (highPeriodTime > lowPeriodTime)
        {
            nextPeriodTime = Random.Range(lowPeriodTime, highPeriodTime);
        }
        else
        {
            nextPeriodTime = lowPeriodTime;
        }
    }

    private void OnEnable()
    {
        if (resetOnEnable)
        {
            ResetTime();
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
