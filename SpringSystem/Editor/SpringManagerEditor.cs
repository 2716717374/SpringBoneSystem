using UnitySpring;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpringManager))]
public class SpringManagerEditor : Editor
{
    private SpringManager manager;

    private void OnEnable()
    {
        manager = (SpringManager)target;
    }

    public override void OnInspectorGUI()
    {
        // 标题
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🌱 Spring System Manager", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // 绘制默认属性
        DrawDefaultInspector();

        // 分隔线
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space(5);

        // 工具按钮
        EditorGUILayout.LabelField("⚙️ Tools", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Find All Bones"))
        {
            FindAllSpringBones();
        }
        if (GUILayout.Button("Clear Bones"))
        {
            manager.springBones = new SpringBone[0];
            EditorUtility.SetDirty(manager);
        }
        GUILayout.EndHorizontal();

        // 显示统计信息
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox($"Spring Bones: {manager.springBones?.Length ?? 0}", MessageType.Info);
    }

    private void FindAllSpringBones()
    {
        // 查找所有子物体中的SpringBone
        var bones = manager.GetComponentsInChildren<SpringBone>();
        manager.springBones = bones;
        EditorUtility.SetDirty(manager);
        Debug.Log($"✅ 找到 {bones.Length} 个Spring Bones");
    }
}