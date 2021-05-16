﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class T23_SetVoiceParameters : UdonSharpBehaviour
{
    public int groupID;

    [SerializeField]
    private float distanceFar;

    [SerializeField]
    private float distanceNear;

    [SerializeField]
    private float gain;

    [SerializeField]
    private bool lowpass;

    [SerializeField]
    private float volumetricRadius;

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

        this.enabled = false;
    }

    public void Action()
    {
        if (!RandomJudgement()) { return; }

        Networking.LocalPlayer.SetVoiceDistanceFar(distanceFar);
        Networking.LocalPlayer.SetVoiceDistanceNear(distanceNear);
        Networking.LocalPlayer.SetVoiceGain(gain);
        Networking.LocalPlayer.SetVoiceLowpass(lowpass);
        Networking.LocalPlayer.SetVoiceVolumetricRadius(volumetricRadius);
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
