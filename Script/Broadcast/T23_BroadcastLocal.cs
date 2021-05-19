
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
    private int[] priorities;

    private bool synced = false;
    private bool fired = false;
    private float timer = 0;
    private int actionIndex = 0;

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

        actionIndex = 0;
        if (actionIndex < actions.Length)
        {
            actions[actionIndex].SendCustomEvent("Action");
        }
    }

    public void NextAction()
    {
        actionIndex++;
        if (actionIndex < actions.Length)
        {
            actions[actionIndex].SendCustomEvent("Action");
        }
    }

    public void AddActions(UdonSharpBehaviour actionTarget, int priority)
    {
        if (actions == null)
        {
            actions = new UdonSharpBehaviour[1];
            actions[0] = actionTarget;
            priorities = new int[1];
            priorities[0] = priority;
        }
        else
        {
            int i = 0;
            while (i < actions.Length)
            {
                if (priorities[i] > priority)
                {
                    break;
                }
                i++;
            }
            actions = AddUdonSharpBehaviourArray(actions, actionTarget, i);
            priorities = AddIntArray(priorities, priority, i);
        }
    }

    private UdonSharpBehaviour[] AddUdonSharpBehaviourArray(UdonSharpBehaviour[] array, UdonSharpBehaviour value, int index)
    {
        UdonSharpBehaviour[] new_array = new UdonSharpBehaviour[array.Length + 1];
        array.CopyTo(new_array, 0);
        for (int i = 0; i < index; i++)
        {
            new_array[i] = array[i];
        }
        new_array[index] = value;
        for (int i = index + 1; i < new_array.Length; i++)
        {
            new_array[i] = array[i - 1];
        }
        return new_array;
    }

    private int[] AddIntArray(int[] array, int value, int index)
    {
        int[] new_array = new int[array.Length + 1];
        array.CopyTo(new_array, 0);
        for (int i = 0; i < index; i++)
        {
            new_array[i] = array[i];
        }
        new_array[index] = value;
        for (int i = index + 1; i < new_array.Length; i++)
        {
            new_array[i] = array[i - 1];
        }
        return new_array;
    }
}
