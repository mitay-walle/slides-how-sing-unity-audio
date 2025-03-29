using Sirenix.OdinInspector;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GameAssets.VolumetricAudio
{
    public class VA_CullingShape : MonoBehaviour
    {
        [ShowInInspector] private static bool _drawGizmos = false;


        [SerializeField] private float _margin = 50f;
        private CullingRect _rect = default;
        private VA_Shape _shape = default;

        public void Init()
        {
            _shape = GetComponent<VA_Shape>();
            _rect = _shape.GetCullRect(_margin);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Check(Vector3 point)
        {
            _shape.enabled = _rect.IsPointIn(point);
        }

        private void OnDrawGizmosSelected()
        {
            if (_drawGizmos && TryGetComponent(out VA_Shape shape))
            {
                Gizmos.color = Color.magenta;
                shape.GetCullRect(_margin).Draw(10f);
            }
        }
    }
}
