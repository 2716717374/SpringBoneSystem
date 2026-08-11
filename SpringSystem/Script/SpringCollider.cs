
using UnityEngine;
using System.Collections;

namespace UnitySpring
{
    [AddComponentMenu("Spring System/Spring Collider")]
    public class SpringCollider : MonoBehaviour
    {
        // 碰撞体半径
        public float radius = 0.5f;

        // 在 Scene 视图中选中时绘制 Gizmos
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}