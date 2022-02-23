#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class T23_AutoUpdateCommonBuffers
{
    static T23_AutoUpdateCommonBuffers()
    {
        EditorApplication.hierarchyWindowItemOnGUI += delegate (int instanceID, Rect selectionRect)
        {
            if (Event.current.commandName == "Duplicate" || Event.current.commandName == "SoftDelete" || Event.current.commandName == "UndoRedoPerformed")
            {
                T23_EditorUtility.UpdateAllCommonBuffersRelate();
            }
        };
    }
}
#endif