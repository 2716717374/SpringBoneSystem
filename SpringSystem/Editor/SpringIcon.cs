using UnityEditor;
using UnityEngine;
using UnitySpring;

namespace UnitySpring
{
    // 为SpringManager添加图标
    [InitializeOnLoad]
    public static class SpringIcon
    {
        static SpringIcon()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            var obj = EditorUtility.EntityIdToObject(instanceID) as GameObject;
            if (obj == null) return;

            // 检查是否有SpringManager组件
            if (obj.GetComponent<SpringManager>() != null)
            {
                // 在Hierarchy中显示图标
                Rect iconRect = new Rect(selectionRect.x + selectionRect.width - 20, selectionRect.y, 16, 16);
                GUI.Label(iconRect, EditorGUIUtility.IconContent("d_Animator Icon"));
            }
        }
    }
}