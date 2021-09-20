
using UdonSharp;
using UnityEngine;

public class T23_ConnectFromUdon : UdonSharpBehaviour
{
    public GameObject target;
    public string customTriggerName;

    private T23_CustomTrigger targetTrigger;

    void Start()
    {
        this.enabled = false;
    }

    public void ActiveCustomTrigger()
    {
        if (!target) { return; }
        if (targetTrigger)
        {
            targetTrigger.Trigger();
        }
        else
        {
            T23_CustomTrigger[] customTriggers = target.GetComponents<T23_CustomTrigger>();
            for (int i = 0; i < customTriggers.Length; i++)
            {
                if (customTriggers[i].Name == customTriggerName)
                {
                    customTriggers[i].Trigger();
                    targetTrigger = customTriggers[i];
                    return;
                }
            }
        }
    }
}
