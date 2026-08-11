using UnityEngine;
using System.Collections;

namespace UnitySpring
{
    [AddComponentMenu("Spring System/Spring Bone")]
    public class SpringBone : MonoBehaviour
    {
        public Transform child;

        // 改为私有，自动计算
        private Vector3 boneAxis;

        // 保留一个开关：是否手动指定 boneAxis
        public bool manualBoneAxis = false;
        public Vector3 manualBoneAxisValue = new Vector3(-1.0f, 0.0f, 0.0f);

        public float radius = 0.05f;
        public bool isUseEachBoneForceSettings = false;
        public float stiffnessForce = 0.01f;
        public float dragForce = 0.4f;
        public Vector3 springForce = new Vector3(0.0f, -0.0001f, 0.0f);
        public SpringCollider[] colliders;
        public bool debug = true;
        public float threshold = 0.01f;

        private float springLength;
        private Quaternion localRotation;
        private Transform trs;
        private Vector3 currTipPos;
        private Vector3 prevTipPos;
        private Transform org;
        private SpringManager managerRef;

        private void Awake()
        {
            trs = transform;
            localRotation = transform.localRotation;
            managerRef = GetParentSpringManager(transform);
        }

        private SpringManager GetParentSpringManager(Transform t)
        {
            var springManager = t.GetComponent<SpringManager>();
            if (springManager != null)
                return springManager;
            if (t.parent != null)
                return GetParentSpringManager(t.parent);
            return null;
        }

        private void Start()
        {
            // ========== 自动计算 boneAxis ==========
            if (!manualBoneAxis)
            {
                AutoCalculateBoneAxis();
            }
            else
            {
                boneAxis = manualBoneAxisValue;
            }

            springLength = Vector3.Distance(trs.position, child.position);
            currTipPos = child.position;
            prevTipPos = child.position;
        }

        /// <summary>
        /// 自动计算骨骼朝向轴
        /// </summary>
        private void AutoCalculateBoneAxis()
        {
            if (child != null)
            {
                // 指向子骨骼的方向（本地空间）
                Vector3 dir = child.position - trs.position;
                if (dir.magnitude > 0.001f)
                {
                    boneAxis = trs.InverseTransformDirection(dir).normalized;
                    return;
                }
            }

            // 保底方案1：指向父骨骼的反方向
            if (trs.parent != null)
            {
                Vector3 dir = trs.position - trs.parent.position;
                if (dir.magnitude > 0.001f)
                {
                    boneAxis = trs.InverseTransformDirection(dir).normalized;
                    return;
                }
            }

            // 保底方案2：世界下方向
            boneAxis = trs.InverseTransformDirection(Vector3.down).normalized;

            // 保底方案3：实在不行就用 Z 轴
            if (boneAxis.magnitude < 0.001f)
            {
                boneAxis = Vector3.forward;
            }
        }

        public void UpdateSpring()
        {
            org = trs;
            trs.localRotation = Quaternion.identity * localRotation;

            float sqrDt = Time.deltaTime * Time.deltaTime;

            Vector3 force = trs.rotation * (boneAxis * stiffnessForce) / sqrDt;
            force += (prevTipPos - currTipPos) * dragForce / sqrDt;
            force += springForce / sqrDt;

            Vector3 temp = currTipPos;
            currTipPos = (currTipPos - prevTipPos) + currTipPos + (force * sqrDt);
            currTipPos = ((currTipPos - trs.position).normalized * springLength) + trs.position;

            for (int i = 0; i < colliders.Length; i++)
            {
                if (Vector3.Distance(currTipPos, colliders[i].transform.position) <= (radius + colliders[i].radius))
                {
                    Vector3 normal = (currTipPos - colliders[i].transform.position).normalized;
                    currTipPos = colliders[i].transform.position + (normal * (radius + colliders[i].radius));
                    currTipPos = ((currTipPos - trs.position).normalized * springLength) + trs.position;
                }
            }

            prevTipPos = temp;

            Vector3 aimVector = trs.TransformDirection(boneAxis);
            Quaternion aimRotation = Quaternion.FromToRotation(aimVector, currTipPos - trs.position);
            Quaternion secondaryRotation = aimRotation * trs.rotation;
            trs.rotation = Quaternion.Lerp(org.rotation, secondaryRotation, managerRef.dynamicRatio);
        }

        private void OnDrawGizmos()
        {
            if (debug)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(currTipPos, radius);
            }
        }
    }
}