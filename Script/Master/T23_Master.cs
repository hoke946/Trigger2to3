﻿#if UNITY_EDITOR && !COMPILER_UDONSHARP
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UdonSharp;
using UdonSharpEditor;
using VRC.Udon;

public class T23_Master : MonoBehaviour
{
    [Serializable]
    public struct ComponentSet
    {
        public string title;
        public UdonBehaviour component;
    }

    public int groupID = -1;

    public List<string> broadcastTitles = new List<string>();
    public ComponentSet broadcastSet;

    public List<string> triggerTitles = new List<string>();
    public List<ComponentSet> triggerSet = new List<ComponentSet>();

    public List<string> actionTitles = new List<string>();
    public List<ComponentSet> actionSet = new List<ComponentSet>();

    public string interactText = "Use";
    public Component[] components = new Component[0];
    public bool hasObjectSync = true;
    //public bool reliable = true;
    public bool reliable = false;

    public void SetupGroup()
    {
        if (groupID == -1)
        {
            T23_Master[] masters = GetComponents<T23_Master>();
            for (int i = 0; i < masters.Length; i++)
            {
                if (masters[i] == this)
                {
                    groupID = i;
                    break;
                }
            }
        }
    }

    public void CheckComponents()
    {
        bool modified = false;
        bool changed = false;

        Component[] newComponents = GetComponents<Component>();
        if (newComponents.Length != components.Length)
        {
            changed = true;
        }
        for (int i = 0; i < newComponents.Length; i++)
        {
            if (i < components.Length && newComponents[i] != components[i])
            {
                modified = true;
                changed = true;
                break;
            }
        }
        components = newComponents;

        /*
        var objSync = GetComponent<VRC.SDK3.Components.VRCObjectSync>();
        if (objSync)
        {
            if (!hasObjectSync)
            {
                hasObjectSync = true;
                reliable = false;
                modified = true;
            }
        }
        else
        {
            if (hasObjectSync)
            {
                hasObjectSync = false;
                reliable = true;
                modified = true;
            }
        }
        */

        if (modified)
        {
            OrderComponents(changed);
        }
    }

    public void SetBroadcast(UdonSharpProgramAsset program)
    {
        if (broadcastSet.component && broadcastSet.component.programSource == program) { return; }

        if (broadcastTitles.Count > 0)
        {
            broadcastTitles.Clear();
            ChangeBroadcast();
        }

        ComponentSet set = AddUdonComponent(program, broadcastTitles);
        broadcastSet = set;
        broadcastTitles.Add(set.title);
        OrderComponents(true);
    }

    public void JoinBroadcast(UdonSharpBehaviour baseComponent)
    {
        if (broadcastTitles.Count > 0)
        {
            broadcastTitles.Clear();
            ChangeBroadcast();
        }

        ComponentSet set = JoinUdonComponent(baseComponent, broadcastTitles);
        broadcastSet = set;
        broadcastTitles.Add(set.title);
        OrderComponents(true);

        UdonBehaviour udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(baseComponent);
        DestroyImmediate(udon);
    }

    public void ChangeBroadcast()
    {
        if (!broadcastTitles.Contains(broadcastSet.title) || broadcastSet.component == null)
        {
            if (broadcastSet.component)
            {
                DestroyImmediate(broadcastSet.component);
            }
            broadcastSet = new ComponentSet();
        }
    }

    public void RemoveBroadcast()
    {
        if (broadcastSet.component)
        {
            DestroyImmediate(broadcastSet.component);
        }
        broadcastSet = new ComponentSet();
    }

    public void AddTrigger(UdonSharpProgramAsset program)
    {
        ComponentSet set = AddUdonComponent(program, triggerTitles);
        triggerSet.Add(set);
        triggerTitles.Add(set.title);
        OrderComponents(true);
    }

    public void JoinTrigger(UdonSharpBehaviour baseComponent)
    {
        ComponentSet set = JoinUdonComponent(baseComponent, triggerTitles);
        triggerSet.Add(set);
        triggerTitles.Add(set.title);
        OrderComponents(true);

        UdonBehaviour udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(baseComponent);
        DestroyImmediate(udon);
    }

    public void ChangeTrigger()
    {
        List<ComponentSet> deleteSet = new List<ComponentSet>();
        foreach (ComponentSet set in triggerSet)
        {
            if (!triggerTitles.Contains(set.title) || set.component == null)
            {
                deleteSet.Add(set);
            }
        }
        foreach (ComponentSet set in deleteSet)
        {
            if (set.component)
            {
                DestroyImmediate(set.component);
            }
            triggerSet.Remove(set);
        }
        OrderComponents(true);
    }

    public void AddAction(UdonSharpProgramAsset program)
    {
        ComponentSet set = AddUdonComponent(program, actionTitles);
        actionSet.Add(set);
        actionTitles.Add(set.title);
        OrderComponents(true);
    }

    public void JoinAction(UdonSharpBehaviour baseComponent)
    {
        ComponentSet set = JoinUdonComponent(baseComponent, actionTitles);
        actionSet.Add(set);
        actionTitles.Add(set.title);
        OrderComponents(true);

        UdonBehaviour udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(baseComponent);
        DestroyImmediate(udon);
    }

    public void ChangeAction()
    {
        List<ComponentSet> deleteSet = new List<ComponentSet>();
        foreach (ComponentSet set in actionSet)
        {
            if (!actionTitles.Contains(set.title) || set.component == null)
            {
                deleteSet.Add(set);
            }
        }
        foreach (ComponentSet set in deleteSet)
        {
            if (set.component)
            {
                DestroyImmediate(set.component);
            }
            actionSet.Remove(set);
        }
        OrderComponents(true);
    }

    private ComponentSet AddUdonComponent(UdonSharpProgramAsset program, List<string> titleArray)
    {
        string title = ConfirmTitle(program.GetClass().Name.Replace("T23_", ""), titleArray);

        Type t23Type = program.GetClass();
        Component newComponent = gameObject.AddComponent(t23Type);

        FieldInfo groupField = t23Type.GetField("groupID");
        if (groupField != null)
        {
            groupField.SetValue(newComponent, groupID);
        }

        FieldInfo titleField = t23Type.GetField("title");
        if (titleField != null)
        {
            titleField.SetValue(newComponent, title);
        }

        UdonSharpBehaviour[] usharpArray = { (UdonSharpBehaviour)newComponent };
        UdonBehaviour[] udon = UdonSharpEditorUtility.ConvertToUdonBehaviours(usharpArray);

        ComponentSet res = new ComponentSet();
        res.title = title;
        res.component = udon[0];

        return res;
    }

    private ComponentSet JoinUdonComponent(UdonSharpBehaviour baseComponent, List<string> titleArray)
    {
        string title = ConfirmTitle(baseComponent.GetType().Name.Replace("T23_", ""), titleArray);

        Type t23Type = baseComponent.GetType();
        Component newComponent = gameObject.AddComponent(t23Type);

        FieldInfo[] fields = t23Type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (FieldInfo field in fields)
        {
            field.SetValue(newComponent, field.GetValue(baseComponent));
        }

        FieldInfo groupField = t23Type.GetField("groupID");
        if (groupField != null)
        {
            groupField.SetValue(newComponent, groupID);
        }

        FieldInfo titleField = t23Type.GetField("title");
        if (titleField != null)
        {
            titleField.SetValue(newComponent, title);
        }

        UdonSharpBehaviour[] usharpArray = { (UdonSharpBehaviour)newComponent };
        UdonBehaviour[] udon = UdonSharpEditorUtility.ConvertToUdonBehaviours(usharpArray);

        ComponentSet res = new ComponentSet();
        res.title = title;
        res.component = udon[0];

        return res;
    }

    private string ConfirmTitle(string className, List<string> titleArray)
    {
        string title = "";
        int cnt = 1;
        while (true)
        {
            title = className.Replace("T23_", "");
            if (cnt > 1) { title += " (" + cnt + ")"; }
            if (titleArray == null)
            {
                break;
            }
            else
            {
                if (!titleArray.Contains(title))
                {
                    break;
                }
            }
            cnt++;
        }
        return title;
    }

    private ComponentSet GetComponentSet(List<ComponentSet> list, string title)
    {
        foreach (ComponentSet set in list)
        {
            if (set.title == title)
            {
                return set;
            }
        }
        return new ComponentSet();
    }

    public void OrderComponents(bool moveComponent)
    {
        List<Component> orderList = new List<Component>();
        if (broadcastSet.component)
        {
            orderList.Add(broadcastSet.component);
        }
        else
        {
            broadcastTitles.Remove(broadcastSet.title);
            broadcastSet = new ComponentSet();
        }

        List<ComponentSet> deleteSet = new List<ComponentSet>();
        foreach (string title in triggerTitles)
        {
            ComponentSet set = GetComponentSet(triggerSet, title);
            if (set.component)
            {
                orderList.Add(set.component);
            }
            else
            {
                deleteSet.Add(set);
            }
        }
        foreach (ComponentSet set in deleteSet)
        {
            triggerTitles.Remove(set.title);
            triggerSet.Remove(set);
        }

        deleteSet = new List<ComponentSet>();
        foreach (string title in actionTitles)
        {
            ComponentSet set = GetComponentSet(actionSet, title);
            if (set.component)
            {
                orderList.Add(set.component);
            }
            else
            {
                deleteSet.Add(set);
            }
        }
        foreach (ComponentSet set in deleteSet)
        {
            actionTitles.Remove(set.title);
            actionSet.Remove(set);
        }

        foreach (var component in orderList)
        {
            UdonBehaviour udon = (UdonBehaviour)component;
            udon.interactText = interactText;
            udon.Reliable = reliable;
        }
        
        if (moveComponent)
        {
            List<Component> afterList = new List<Component>();
            bool pass = false;
            foreach (Component component in GetComponents<Component>())
            {
                if (component == this)
                {
                    pass = true;
                    continue;
                }
                if (orderList.Contains(component))
                {
                    continue;
                }

                if (pass)
                {
                    afterList.Add(component);
                }
            }

            orderList.AddRange(afterList);
            foreach (Component component in orderList)
            {
                int idx = GetComponentIndex(GetComponents<Component>(), component);
                while (true)
                {
                    ComponentUtility.MoveComponentDown(component);
                    int newidx = GetComponentIndex(GetComponents<Component>(), component);
                    if (newidx == idx || newidx == -1)
                    {
                        break;
                    }
                    idx = newidx;
                }
            }
        }
    }

    private int GetComponentIndex(Component[] array, Component target)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == target) { return i; }
        }
        return -1;
    }

    public void TestTrigger()
    {
        if (triggerTitles.Count > 0)
        {
            ComponentSet set = GetComponentSet(triggerSet, triggerTitles[0]);
            UdonSharpBehaviour proxy = UdonSharpEditorUtility.FindProxyBehaviour(set.component);
            UdonSharpEditorUtility.CopyProxyToUdon(proxy, ProxySerializationPolicy.All);
            set.component.SendCustomEvent("Trigger");
            UdonSharpEditorUtility.CopyUdonToProxy(proxy, ProxySerializationPolicy.All);
        }
    }

    public static void JoinMaster(UdonSharpBehaviour body, int gid, int category)
    {
        T23_Master master = GetMaster(body, gid, category, false);
        if (!master)
        {
            master = body.gameObject.AddComponent<T23_Master>();
            master.groupID = gid;
        }

        switch (category)
        {
            case 0:
                master.JoinBroadcast(body);
                break;
            case 1:
                master.JoinTrigger(body);
                break;
            case 2:
                master.JoinAction(body);
                break;
        }
    }

    public static T23_Master GetMaster(UdonSharpBehaviour body, int gid, int category, bool fixTitle, string title = "")
    {
        T23_Master[] masters = body.transform.GetComponents<T23_Master>();
        for (int i = 0; i < masters.Length; i++)
        {
            if (masters[i].groupID == gid)
            {
                if (!fixTitle)
                {
                    return masters[i];
                }
                else
                {
                    switch (category)
                    {
                        case 0:
                            if (title == masters[i].broadcastSet.title)
                            {
                                return masters[i];
                            }
                            break;
                        case 1:
                            if (masters[i].triggerTitles.Contains(title))
                            {
                                return masters[i];
                            }
                            break;
                        case 2:
                            if (masters[i].actionTitles.Contains(title))
                            {
                                return masters[i];
                            }
                            break;
                    }
                }
            }
        }
        return null;
    }
}
#endif
