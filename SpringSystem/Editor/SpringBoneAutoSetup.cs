using UnityEngine;
using UnityEditor;
using UnitySpring;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class SpringBoneAutoSetup : EditorWindow
{
    [MenuItem("Spring System/Auto Setup Bones")]
    public static void ShowWindow()
    {
        GetWindow<SpringBoneAutoSetup>("Spring Bone Setup");
    }

    private GameObject targetCharacter;
    private Transform skeletonRoot;
    private bool includeStandardBones = false;
    private float defaultStiffness = 0.01f;
    private float defaultDrag = 0.4f;
    private float defaultRadius = 0.05f;

    // 缓存Humanoid骨骼的Transform引用
    private HashSet<Transform> humanoidBoneTransforms = new HashSet<Transform>();
    private HashSet<Transform> humanoidDescendants = new HashSet<Transform>();

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Spring Bone Auto Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // ========== 目标角色 ==========
        EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
        targetCharacter = (GameObject)EditorGUILayout.ObjectField(
            "Character", targetCharacter, typeof(GameObject), true);

        EditorGUILayout.Space();

        // ========== 骨骼根节点（手动选择） ==========
        EditorGUILayout.LabelField("Skeleton Root", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "请选择骨骼的根节点（通常是Hips或Root骨骼）\n" +
            "系统将只处理这个节点以下的骨骼",
            MessageType.Info);

        skeletonRoot = (Transform)EditorGUILayout.ObjectField(
            "Root Bone", skeletonRoot, typeof(Transform), true);

        if (skeletonRoot != null)
        {
            int childCount = skeletonRoot.GetComponentsInChildren<Transform>().Length;
            EditorGUILayout.HelpBox(
                $"✅ 已选择: {skeletonRoot.name} (包含 {childCount} 个子节点)",
                MessageType.None);
        }

        EditorGUILayout.Space();

        // ========== 参数设置 ==========
        EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);
        includeStandardBones = EditorGUILayout.Toggle(
            "Include Standard Bones (Not Recommended)", includeStandardBones);

        EditorGUILayout.Space();
        defaultStiffness = EditorGUILayout.FloatField("Stiffness", defaultStiffness);
        defaultDrag = EditorGUILayout.FloatField("Drag", defaultDrag);
        defaultRadius = EditorGUILayout.FloatField("Radius", defaultRadius);

        EditorGUILayout.Space();

        // ========== 统计信息 ==========
        if (targetCharacter != null)
        {
            int existingBones = targetCharacter.GetComponentsInChildren<SpringBone>().Length;
            EditorGUILayout.HelpBox($"现有SpringBone数量: {existingBones}", MessageType.Info);
        }

        EditorGUILayout.Space();

        // ========== 按钮 ==========
        if (GUILayout.Button("1. Auto Find Skeleton Root"))
        {
            AutoFindSkeletonRoot();
        }

        if (GUILayout.Button("2. Cache Humanoid Bones (Preview)"))
        {
            CacheHumanoidBoneTransforms();
            Debug.Log($"📋 缓存了 {humanoidBoneTransforms.Count} 个Humanoid骨骼");
            Debug.Log($"📋 缓存了 {humanoidDescendants.Count} 个Humanoid子孙节点");
        }

        if (GUILayout.Button("3. Auto Setup Spring Bones"))
        {
            AutoSetupBones();
        }

        if (GUILayout.Button("4. Find and Setup Child Bones"))
        {
            FindAndSetupChildBones();
        }

        if (GUILayout.Button("5. Clear All Spring Bones"))
        {
            ClearAllSpringBones();
        }
    }

    // ============================================
    // 1. 自动查找骨骼根节点
    // ============================================
    private void AutoFindSkeletonRoot()
    {
        if (targetCharacter == null)
        {
            Debug.LogError("请先选择目标角色！");
            return;
        }

        Animator animator = targetCharacter.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("角色没有Animator组件！");
            return;
        }

        // 方法1：通过Humanoid骨骼查找Hips
        if (animator.avatar != null && animator.avatar.isHuman)
        {
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips != null)
            {
                if (hips.parent != null)
                {
                    skeletonRoot = hips.parent;
                    Debug.Log($"✅ 找到骨骼根节点: {skeletonRoot.name} (通过Hips查找)");
                    Selection.activeObject = skeletonRoot.gameObject;
                    return;
                }
                else
                {
                    skeletonRoot = hips;
                    Debug.Log($"✅ 找到骨骼根节点: {skeletonRoot.name} (Hips本身就是根)");
                    Selection.activeObject = skeletonRoot.gameObject;
                    return;
                }
            }
        }

        // 方法2：查找名字包含 "Root" 或 "Hips" 的节点
        Transform[] allTransforms = targetCharacter.GetComponentsInChildren<Transform>();
        foreach (Transform t in allTransforms)
        {
            string lowerName = t.name.ToLower();
            if (lowerName.Contains("root") || lowerName.Contains("hips"))
            {
                skeletonRoot = t;
                Debug.Log($"✅ 找到骨骼根节点: {skeletonRoot.name} (通过名称查找)");
                Selection.activeObject = skeletonRoot.gameObject;
                return;
            }
        }

        // 方法3：取第一个有子节点的节点作为根
        foreach (Transform t in allTransforms)
        {
            if (t.childCount > 1 && t != targetCharacter.transform)
            {
                skeletonRoot = t;
                Debug.Log($"✅ 找到骨骼根节点: {skeletonRoot.name} (通过子节点数量查找)");
                Selection.activeObject = skeletonRoot.gameObject;
                return;
            }
        }

        Debug.LogError("❌ 无法自动找到骨骼根节点，请手动选择！");
    }

    // ============================================
    // 2. 缓存Humanoid骨骼
    // ============================================
    private void CacheHumanoidBoneTransforms()
    {
        humanoidBoneTransforms.Clear();
        humanoidDescendants.Clear();

        Animator animator = targetCharacter.GetComponent<Animator>();
        if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
        {
            Debug.LogWarning("⚠️ 角色不是Humanoid或没有Animator组件！");
            return;
        }

        HumanBodyBones[] allBones = System.Enum.GetValues(typeof(HumanBodyBones)) as HumanBodyBones[];

        foreach (HumanBodyBones bone in allBones)
        {
            if (bone == HumanBodyBones.LastBone) continue;

            Transform t = animator.GetBoneTransform(bone);
            if (t != null)
            {
                humanoidBoneTransforms.Add(t);

                foreach (Transform descendant in t.GetComponentsInChildren<Transform>())
                {
                    if (descendant != t)
                    {
                        humanoidDescendants.Add(descendant);
                    }
                }
            }
        }

        Debug.Log($"📋 缓存了 {humanoidBoneTransforms.Count} 个Humanoid骨骼");
        Debug.Log($"📋 缓存了 {humanoidDescendants.Count} 个Humanoid子孙节点");
    }

    // ============================================
    // 3. 判断是否属于Humanoid骨骼体系
    // ============================================
    private bool IsHumanoidOrDescendant(Transform t)
    {
        if (t == null) return false;

        // 如果自身是Humanoid骨骼 → 跳过
        if (humanoidBoneTransforms.Contains(t)) return true;

        // 检查父级链
        Transform parent = t.parent;
        while (parent != null)
        {
            if (humanoidBoneTransforms.Contains(parent))
            {
                // 父级是Humanoid骨骼，判断当前节点是骨骼还是附加物体
                return IsBoneLikeName(t.name) || IsBoneChain(t);
            }
            parent = parent.parent;
        }

        return false;
    }

    // ============================================
    // 4. 判断名字是否像骨骼（而不是附加物体）
    // ============================================
    private bool IsBoneLikeName(string name)
    {
        string[] bonePatterns = {
            "UpLeg", "Leg", "Foot", "Toe",
            "Shoulder", "Arm", "ForeArm", "Hand",
            "Index", "Middle", "Pinky", "Ring", "Thumb",
            "Spine", "Neck", "Head", "Hips",
            "Chest", "Clavicle", "Eye"
        };

        foreach (string pattern in bonePatterns)
        {
            if (name.Contains(pattern)) return true;
        }
        return false;
    }

    // ============================================
    // 5. 判断是否是一串骨骼链
    // ============================================
    private bool IsBoneChain(Transform t)
    {
        // 如果子节点数量为1，且子节点名字包含数字（如 01, 02）
        if (t.childCount == 1)
        {
            Transform child = t.GetChild(0);
            if (Regex.IsMatch(child.name, @"\d{2}$"))
            {
                return true;
            }
        }

        // 如果名字本身包含数字后缀（如 HairBackA01）
        if (Regex.IsMatch(t.name, @"\d{2}$"))
        {
            return true;
        }

        return false;
    }

    // ============================================
    // 6. 判断是否是附加物体
    // ============================================
    private bool IsAttachedObject(string name)
    {
        string[] attachedPatterns = {
            "Bag", "Hair", "Cloth", "Skirt", "Tail",
            "Earring", "Necklace", "Belt", "Ribbon",
            "Wing", "Dress", "Cape", "Scarf",
            "Hood", "String", "Boots", "Goggle",
            "Hat", "Glasses", "Accessory", "Decoration",
            "Fastener", "Bust"
        };

        foreach (string pattern in attachedPatterns)
        {
            if (name.Contains(pattern)) return true;
        }
        return false;
    }

    // ============================================
    // 7. 判断是否应该添加SpringBone
    // ============================================
    private bool ShouldAddSpringBone(Transform t)
    {
        // 跳过根节点
        if (t == skeletonRoot) return false;

        // 跳过没有子节点的
        if (t.childCount == 0) return false;

        // 跳过Humanoid骨骼本身
        if (humanoidBoneTransforms.Contains(t)) return false;

        // 如果父级是Humanoid骨骼
        if (t.parent != null && humanoidBoneTransforms.Contains(t.parent))
        {
            // 如果名字像骨骼（如 LeftArm、Spine01），跳过
            if (IsBoneLikeName(t.name)) return false;

            // 如果名字包含数字后缀且像骨骼链（如 Spine01），跳过
            if (IsBoneChain(t)) return false;

            // 如果是附加物体（背包、头发等），保留
            if (IsAttachedObject(t.name)) return true;

            // 默认跳过（可能是其他骨骼子级）
            return false;
        }

        // 检查名字是否包含排除关键字
        if (ShouldExcludeByName(t.name)) return false;

        // 其他情况，如果是附加物体则保留
        if (IsAttachedObject(t.name)) return true;

        return false;
    }

    // ============================================
    // 8. 排除关键字
    // ============================================
    private bool ShouldExcludeByName(string name)
    {
        string[] excludeNames = {
            "Root", "Bone", "Joint", "Pivot",
            "Collider", "Mesh", "Render", "Renderer",
            "IK", "Constraint", "Target",
            "Controller", "Rig", "Proxy",
            "Shadow", "LOD", "Level"
        };

        foreach (string exclude in excludeNames)
        {
            if (name.Contains(exclude)) return true;
        }
        return false;
    }

    // ============================================
    // 9. 查找最佳子节点
    // ============================================
    private Transform FindBestChildForSpring(Transform parent)
    {
        if (parent.childCount == 0) return null;

        Transform bestChild = null;
        int maxChildCount = -1;
        int maxDepth = -1;

        foreach (Transform child in parent)
        {
            // 跳过Humanoid骨骼
            if (humanoidBoneTransforms.Contains(child)) continue;

            int childCount = child.childCount;
            int depth = GetChildDepth(child);

            if (childCount > maxChildCount || (childCount == maxChildCount && depth > maxDepth))
            {
                maxChildCount = childCount;
                maxDepth = depth;
                bestChild = child;
            }
        }

        if (bestChild == null)
        {
            foreach (Transform child in parent)
            {
                if (child.childCount > 0 && !humanoidBoneTransforms.Contains(child))
                {
                    bestChild = child;
                    break;
                }
            }
        }

        return bestChild;
    }

    // ============================================
    // 10. 查找最深非Humanoid子节点
    // ============================================
    private Transform FindDeepestNonHumanoidChild(Transform parent)
    {
        if (parent.childCount == 0)
            return humanoidBoneTransforms.Contains(parent) ? null : parent;

        Transform deepest = null;
        int maxDepth = -1;

        foreach (Transform child in parent)
        {
            if (humanoidBoneTransforms.Contains(child)) continue;

            int depth = GetChildDepth(child);
            if (depth > maxDepth)
            {
                maxDepth = depth;
                deepest = child;
            }
        }

        if (deepest == null) return null;

        Transform result = deepest;
        while (result.childCount > 0)
        {
            Transform next = null;
            foreach (Transform child in result)
            {
                if (!humanoidBoneTransforms.Contains(child))
                {
                    next = child;
                    break;
                }
            }
            if (next == null) break;
            result = next;
        }

        return result;
    }

    // ============================================
    // 11. 获取子节点深度
    // ============================================
    private int GetChildDepth(Transform t)
    {
        if (t.childCount == 0) return 1;
        int maxDepth = 0;
        foreach (Transform child in t)
        {
            maxDepth = Mathf.Max(maxDepth, GetChildDepth(child));
        }
        return maxDepth + 1;
    }

    // ============================================
    // 12. 核心：自动设置弹簧骨骼
    // ============================================
    private void AutoSetupBones()
    {
        if (targetCharacter == null)
        {
            Debug.LogError("请选择目标角色！");
            return;
        }

        if (skeletonRoot == null)
        {
            Debug.LogError("请先选择骨骼根节点！\n点击 'Auto Find Skeleton Root' 或手动选择。");
            return;
        }

        // 获取或创建SpringManager
        SpringManager manager = targetCharacter.GetComponent<SpringManager>();
        if (manager == null)
        {
            manager = targetCharacter.AddComponent<SpringManager>();
        }

        CacheHumanoidBoneTransforms();

        Transform[] skeletonTransforms = skeletonRoot.GetComponentsInChildren<Transform>();

        List<SpringBone> springBones = new List<SpringBone>();
        List<string> addedBones = new List<string>();
        List<string> skippedBones = new List<string>();

        Debug.Log($"🔍 扫描骨骼: {skeletonRoot.name} (共 {skeletonTransforms.Length} 个节点)");

        foreach (Transform t in skeletonTransforms)
        {
            if (t == skeletonRoot)
            {
                skippedBones.Add($"{t.name} (骨骼根节点，跳过)");
                continue;
            }

            // 使用判断逻辑
            if (!ShouldAddSpringBone(t))
            {
                // 判断跳过原因，便于调试
                if (humanoidBoneTransforms.Contains(t))
                {
                    skippedBones.Add($"{t.name} (Humanoid骨骼，跳过)");
                }
                else if (t.parent != null && humanoidBoneTransforms.Contains(t.parent) && !IsAttachedObject(t.name))
                {
                    skippedBones.Add($"{t.name} (Humanoid骨骼的子级，跳过)");
                }
                else if (t.childCount == 0)
                {
                    skippedBones.Add($"{t.name} (没有子节点，跳过)");
                }
                else if (ShouldExcludeByName(t.name))
                {
                    skippedBones.Add($"{t.name} (排除关键字，跳过)");
                }
                else
                {
                    skippedBones.Add($"{t.name} (其他原因，跳过)");
                }
                continue;
            }

            // 查找合适的子节点
            Transform child = FindBestChildForSpring(t);
            if (child == null)
            {
                skippedBones.Add($"{t.name} (没有合适的子节点，跳过)");
                continue;
            }

            // 检查子节点是否在Humanoid体系中
            if (humanoidBoneTransforms.Contains(child))
            {
                Transform deeperChild = FindDeepestNonHumanoidChild(child);
                if (deeperChild != null && deeperChild != child)
                {
                    child = deeperChild;
                }
                else
                {
                    skippedBones.Add($"{t.name} (子节点是Humanoid骨骼，跳过)");
                    continue;
                }
            }

            // 添加SpringBone
            SpringBone bone = t.gameObject.GetComponent<SpringBone>();
            if (bone == null)
            {
                bone = t.gameObject.AddComponent<SpringBone>();
            }

            bone.child = child;
            bone.stiffnessForce = defaultStiffness;
            bone.dragForce = defaultDrag;
            bone.radius = defaultRadius;

            springBones.Add(bone);
            addedBones.Add($"{t.name} → {child.name}");
        }

        // 更新Manager
        manager.springBones = springBones.ToArray();
        EditorUtility.SetDirty(manager);

        Debug.Log($"✅ 自动设置了 {springBones.Count} 个Spring Bones (在 {skeletonRoot.name} 下)");
        foreach (string log in addedBones)
        {
            Debug.Log($"   📌 {log}");
        }

        Debug.Log($"⏭️ 跳过了 {skippedBones.Count} 个节点");
        foreach (string log in skippedBones)
        {
            Debug.Log($"   ⏭️ {log}");
        }
    }

    // ============================================
    // 13. 通过关键字匹配添加
    // ============================================
    private void FindAndSetupChildBones()
    {
        if (targetCharacter == null)
        {
            Debug.LogError("请选择目标角色！");
            return;
        }

        if (skeletonRoot == null)
        {
            Debug.LogError("请先选择骨骼根节点！");
            return;
        }

        CacheHumanoidBoneTransforms();

        string[] springKeywords = {
            "Hair", "Cloth", "Skirt", "Tail",
            "Earring", "Necklace", "Belt", "Ribbon",
            "Wing", "Dress", "Cape", "Scarf",
            "Hood", "String", "Bag", "Boots",
            "Dynamic", "Physics", "Spring",
            "Goggle", "Fastener", "Bust"
        };

        int count = 0;
        List<string> addedLogs = new List<string>();

        Transform[] skeletonTransforms = skeletonRoot.GetComponentsInChildren<Transform>();

        foreach (Transform t in skeletonTransforms)
        {
            if (t == skeletonRoot) continue;
            if (humanoidBoneTransforms.Contains(t)) continue;
            if (t.childCount == 0) continue;

            bool shouldAdd = false;
            foreach (string keyword in springKeywords)
            {
                if (t.name.Contains(keyword))
                {
                    shouldAdd = true;
                    break;
                }
            }

            if (shouldAdd && t.GetComponent<SpringBone>() == null)
            {
                Transform child = FindBestChildForSpring(t);
                if (child != null && !humanoidBoneTransforms.Contains(child))
                {
                    SpringBone bone = t.gameObject.AddComponent<SpringBone>();
                    bone.child = child;
                    bone.stiffnessForce = defaultStiffness;
                    bone.dragForce = defaultDrag;
                    bone.radius = defaultRadius;
                    count++;
                    addedLogs.Add($"{t.name} → {child.name}");
                }
            }
        }

        Debug.Log($"✅ 通过关键字匹配添加了 {count} 个Spring Bones");
        foreach (string log in addedLogs)
        {
            Debug.Log($"   📌 {log}");
        }
    }

    // ============================================
    // 14. 清除所有SpringBone
    // ============================================
    private void ClearAllSpringBones()
    {
        if (targetCharacter == null)
        {
            Debug.LogError("请选择目标角色！");
            return;
        }

        SpringBone[] bones = targetCharacter.GetComponentsInChildren<SpringBone>();
        foreach (SpringBone bone in bones)
        {
            DestroyImmediate(bone);
        }

        SpringManager manager = targetCharacter.GetComponent<SpringManager>();
        if (manager != null)
        {
            manager.springBones = new SpringBone[0];
            EditorUtility.SetDirty(manager);
        }

        Debug.Log($"🧹 清除了 {bones.Length} 个Spring Bones");
    }
}