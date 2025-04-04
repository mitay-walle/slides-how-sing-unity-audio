using UnityEditor;
using UnityEngine;

namespace GameAssets.VolumetricAudio
{
#if UNITY_EDITOR
	[CanEditMultipleObjects]
	[CustomEditor(typeof(VA_Sphere))]
	public class VA_Sphere_Editor : VA_Editor<VA_Sphere>
	{
		protected override void OnInspector()
		{
			DrawDefault("SphereCollider");

			if (Any(t => t.SphereCollider == null))
			{
				DrawDefault("Center");
				DrawDefault("Radius");
			}

			DrawDefault("IsHollow");
		}
	}
#endif

	[ExecuteInEditMode]
	[AddComponentMenu("Volumetric Audio/VA Sphere")]
	public class VA_Sphere : VA_VolumetricShape
	{
		[Tooltip("If you set this, then all shape settings will automatically be copied from the collider")]
		public SphereCollider SphereCollider;

		[Tooltip("The center of the sphere shape (if you set SphereCollider, this will be automatically overwritten)")]
		public Vector3 Center;

		[Tooltip("The radius of the sphere shape (if you set SphereCollider, this will be automatically overwritten)")]
		public float Radius = 1.0f;

		public Matrix4x4 GetMatrix()
		{
			var position = transform.TransformPoint(Center);
			var rotation = transform.rotation;
			var scale    = transform.lossyScale;

			return VA_Helper.TranslationMatrix(position) * VA_Helper.RotationMatrix(rotation) * VA_Helper.ScalingMatrix(scale);
		}

		protected virtual void Reset()
		{
			SphereCollider = GetComponent<SphereCollider>();
		}

		protected override void OnLateUpdate()
		{
			base.OnLateUpdate();

			// Make sure the listener exists
			var listenerPosition = default(Vector3);

			if (VA_Helper.GetListenerPosition(ref listenerPosition) == true)
			{
				UpdateFields();

				var matrix     = GetMatrix();
				var worldPoint = listenerPosition;
				var localPoint = matrix.inverse.MultiplyPoint(worldPoint);

				if (IsHollow == true)
				{
					localPoint = SnapLocalPoint(localPoint);
					worldPoint = matrix.MultiplyPoint(localPoint);

					SetOuterPoint(worldPoint);
				}
				else
				{
					if (LocalPointInSphere(localPoint) == true)
					{
						SetInnerPoint(worldPoint, true);

						localPoint = SnapLocalPoint(localPoint);
						worldPoint = matrix.MultiplyPoint(localPoint);

						SetOuterPoint(worldPoint);
					}
					else
					{
						localPoint = SnapLocalPoint(localPoint);
						worldPoint = matrix.MultiplyPoint(localPoint);

						SetInnerOuterPoint(worldPoint, false);
					}
				}
			}
		}

#if UNITY_EDITOR
		protected virtual void OnDrawGizmosSelected()
		{
			if (VA_Helper.Enabled(this) == true)
			{
				UpdateFields();

				Gizmos.color  = Color.red;
				Gizmos.matrix = GetMatrix();
				Gizmos.DrawWireSphere(Vector3.zero, Radius);
			}
		}
#endif

		private void UpdateFields()
		{
			if (SphereCollider != null)
			{
				Center = SphereCollider.center;
				Radius = SphereCollider.radius;
			}
		}

		private bool LocalPointInSphere(Vector3 localPoint)
		{
			return localPoint.sqrMagnitude < Radius * Radius;
		}

		private Vector3 SnapLocalPoint(Vector3 localPoint)
		{
			return localPoint.normalized * Radius;
		}
	}
}