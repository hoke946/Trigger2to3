
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class T23_BroadcastLocal : UdonSharpBehaviour
{
    public int groupID;

    [SerializeField]
    private float delayInSeconds;

    public bool randomize;

    private UdonSharpBehaviour[] actions;
    
    private bool synced = false;
    private bool fired = false;
    private float timer = 0;

    [HideInInspector]
    public float randomTotal;

    [HideInInspector]
    public float randomValue;

    void Start()
    {
        this.enabled = false;
    }

    public void Trigger()
    {
        if (delayInSeconds > 0)
        {
            fired = true;
            this.enabled = true;
            timer = 0;
        }
        else
        {
            Fire(false);
        }
    }

    void Update()
    {
        if (fired)
        {
            timer += Time.deltaTime;
            if (timer > delayInSeconds)
            {
                Fire(false);
                fired = false;
                this.enabled = false;
            }
        }
    }

    private void Fire(bool local)
    {
        if (actions == null) { return; }

        if (randomize && randomTotal > 0)
        {
            randomValue = Random.Range(0, Mathf.Max(1, randomTotal));
        }

        for (int i = 0; i < actions.Length; i++)
        {
            actions[i].SendCustomEvent("Action");
        }
    }

    public void AddActions(UdonSharpBehaviour actionTarget)
    {
        if (actions == null)
        {
            actions = new UdonSharpBehaviour[1];
            actions[0] = actionTarget;
        }
        else
        {
            actions = AddUdonSharpBehaviourArray(actions, actionTarget);
        }
    }

    private UdonSharpBehaviour[] AddUdonSharpBehaviourArray(UdonSharpBehaviour[] array, UdonSharpBehaviour value)
    {
        UdonSharpBehaviour[] new_array = new UdonSharpBehaviour[array.Length + 1];
        array.CopyTo(new_array, 0);
        new_array[new_array.Length - 1] = value;
        return new_array;
    }
}
