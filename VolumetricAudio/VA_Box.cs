using UnityEditor;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor.IMGUI.Controls;
#endif

namespace GameAssets.VolumetricAudio
{
	[ExecuteInEditMode]
	[AddComponentMenu("Volumetric Audio/VA Box")]
	public class VA_Box : VA_VolumetricShape
	{
		public Vector3 Center;
		public Vector3 Size = Vector3.one;

		private Matrix4x4 GetMatrix()
		{
			var position = transform.TransformPoint(Center);
			var rotation = transform.rotation;
			var scale = transform.lossyScale;

			scale.x *= Size.x;
			scale.y *= Size.y;
			scale.z *= Size.z;

			return VA_Helper.TranslationMatrix(position) * VA_Helper.RotationMatrix(rotation) * VA_Helper.ScalingMatrix(scale);
		}

		protected override void OnLateUpdate()
		{
			base.OnLateUpdate();

			// Make sure the listener exists
			var listenerPosition = default(Vector3);

			if (VA_Helper.GetListenerPosition(ref listenerPosition) == true)
			{
				var matrix = GetMatrix();
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
					if (LocalPointInBox(localPoint) == true)
					{
						SetInnerPoint(worldPoint, true);

						localPoint = SnapLocalPoint(localPoint);
						worldPoint = matrix.MultiplyPoint(localPoint);

						SetOuterPoint(worldPoint);
					}
					else
					{
						localPoint = ClipLocalPoint(localPoint);
						worldPoint = matrix.MultiplyPoint(localPoint);

						SetInnerOuterPoint(worldPoint, false);
					}
				}
			}
		}

		private bool LocalPointInBox(Vector3 localPoint)
		{
			if (localPoint.x < -0.5f) return false;
			if (localPoint.x > 0.5f) return false;

			if (localPoint.y < -0.5f) return false;
			if (localPoint.y > 0.5f) return false;

			if (localPoint.z < -0.5f) return false;
			if (localPoint.z > 0.5f) return false;

			return true;
		}

		private Vector3 SnapLocalPoint(Vector3 localPoint)
		{
			var x = Mathf.Abs(localPoint.x);
			var y = Mathf.Abs(localPoint.y);
			var z = Mathf.Abs(localPoint.z);

			// X largest?
			if (x > y && x > z)
			{
				localPoint *= VA_Helper.Reciprocal(x * 2.0f);
			}
			// Y largest?
			else if (y > x && y > z)
			{
				localPoint *= VA_Helper.Reciprocal(y * 2.0f);
			}
			// Z largest?
			else
			{
				localPoint *= VA_Helper.Reciprocal(z * 2.0f);
			}

			return localPoint;
		}

		private Vector3 ClipLocalPoint(Vector3 localPoint)
		{
			if (localPoint.x < -0.5f) localPoint.x = -0.5f;
			if (localPoint.x > 0.5f) localPoint.x = 0.5f;

			if (localPoint.y < -0.5f) localPoint.y = -0.5f;
			if (localPoint.y > 0.5f) localPoint.y = 0.5f;

			if (localPoint.z < -0.5f) localPoint.z = -0.5f;
			if (localPoint.z > 0.5f) localPoint.z = 0.5f;

			return localPoint;
		}

        public override CullingRect GetCullRect(float cullMargin)
        {
            Span<Vector3> verts = stackalloc Vector3[]
            {
                new Vector3(.5f, .5f, .5f),
                new Vector3(.5f, .5f, -.5f),
                new Vector3(-.5f, .5f, -.5f),
                new Vector3(-.5f, .5f, .5f),
                new Vector3(.5f, -.5f, .5f),
                new Vector3(.5f, -.5f, -.5f),
                new Vector3(-.5f, -.5f, -.5f),
                new Vector3(-.5f, -.5f, .5f),
            };

			var matrix = GetMatrix();
            var rect = new CullingRect();
            rect.Reset();
			for (int i = 0; i < verts.Length; i++)
				rect.Expand(matrix.MultiplyPoint(verts[i]));
            rect.Expand(cullMargin);
            return rect;
        }

        #region Editor

#if UNITY_EDITOR
        private BoxBoundsHandle handle;

        public override void OnSceneGUI()
		{
			Handles.matrix = transform.localToWorldMatrix;
			handle ??= new BoxBoundsHandle();
			handle.size = Size;
			handle.center = Center;

			EditorGUI.BeginChangeCheck();
			handle.DrawHandle();
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(this, "edit box handle");
				Size = handle.size;
				Center = handle.center;
			}
		}

		protected virtual void OnDrawGizmosSelected()
		{
            Gizmos.color = Color.red;
			Gizmos.matrix = GetMatrix();
			Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }

#endif

		#endregion
	}
}