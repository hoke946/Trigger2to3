#if UNITY_EDITOR && !COMPILER_UDONSHARP
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UdonSharp;
using UdonSharpEditor;

public class T23_HierarchyMenu
{
    [MenuItem("GameObject/Trigger2to3/AddCommonBuffer", false, 20)]
    public static void AddCommonBuffer()
    {
        var commonBuffer = T23_EditorUtility.AddCommonBuffer();
        Selection.activeGameObject = commonBuffer.gameObject;
    }
}
#endif
