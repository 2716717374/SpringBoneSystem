using UnityEngine;
using System.Collections;

namespace UnitySpring
{
    [AddComponentMenu("Spring System/Spring Manager")]
    public class SpringManager : MonoBehaviour
    {
        // Kobayashi
        // DynamicRatio 是动态动画激活程度的参数（0~1）
        public float dynamicRatio = 1.0f;

        // Ebata
        public float stiffnessForce; // 弹性力（基础值）
        public AnimationCurve stiffnessCurve; // 弹性力曲线（按骨骼索引分布）
        public float dragForce; // 阻尼力（基础值）
        public AnimationCurve dragCurve; // 阻尼力曲线（按骨骼索引分布）
        public SpringBone[] springBones; // 所有 SpringBone 组件数组

        void Start()
        {
            // 确保有骨骼才更新参数
            if (springBones != null && springBones.Length > 0)
            {
                UpdateParameters();
            }
        }

        void Update()
        {
#if UNITY_EDITOR
            // Kobayashi：在 Editor 中限制 dynamicRatio 范围在 0~1 之间
            if (dynamicRatio >= 1.0f)
                dynamicRatio = 1.0f;
            else if (dynamicRatio <= 0.0f)
                dynamicRatio = 0.0f;
            // Ebata：在 Editor 中实时更新参数
            UpdateParameters();
#endif
        }

        // 在 LateUpdate 中更新所有弹簧（确保在动画和物理之后执行）
        private void LateUpdate()
        {
            // Kobayashi：如果 dynamicRatio 不为 0，更新所有弹簧
            if (dynamicRatio != 0.0f)
            {
                for (int i = 0; i < springBones.Length; i++)
                {
                    // 只有 dynamicRatio 大于该骨骼的 threshold 时才更新
                    if (dynamicRatio > springBones[i].threshold)
                    {
                        springBones[i].UpdateSpring();
                    }
                }
            }
        }

        // 更新所有 SpringBone 的参数（使用曲线分配值）
        private void UpdateParameters()
        {
            if (springBones == null || springBones.Length == 0)
                return;

            UpdateParameter("stiffnessForce", stiffnessForce, stiffnessCurve);
            UpdateParameter("dragForce", dragForce, dragCurve);
        }

        // 根据曲线更新指定字段的值
        private void UpdateParameter(string fieldName, float baseValue, AnimationCurve curve)
        {
            // ✅ 安全检查
            if (springBones == null || springBones.Length == 0)
                return;

            // ✅ 检查曲线是否有效
            if (curve == null || curve.keys.Length == 0)
                return;

            var start = curve.keys[0].time;
            var end = curve.keys[curve.length - 1].time;

            var prop = springBones[0].GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            for (int i = 0; i < springBones.Length; i++)
            {
                if (!springBones[i].isUseEachBoneForceSettings)
                {
                    var scale = curve.Evaluate(start + (end - start) * i / (springBones.Length - 1));
                    prop.SetValue(springBones[i], baseValue * scale);
                }
            }
        }
    }
}