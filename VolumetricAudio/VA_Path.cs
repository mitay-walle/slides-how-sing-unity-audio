using System;
using System.Collections.Generic;
using Plugins;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace GameAssets.VolumetricAudio
{
	public enum ePath
	{
		Path,
		VolumetricPolygon,
	}

	[ExecuteInEditMode]
	[AddComponentMenu("Volumetric Audio/VA Path")]
	public sealed class VA_Path : VA_VolumetricShape
	{
		[ShowInInspector, HideInPlayMode] private bool _edit;
		[ShowInInspector, HideInPlayMode] private bool _testPoint;
		[SerializeField, HideIf(nameof(_cloneSource))] public List<Vector3> Points = new List<Vector3>();
		[SerializeField] public VA_Path _cloneSource;
		[SerializeField] public ePath _path;
		[SerializeField, ShowIf("@((int)_path) == 1")] public float _depth = 1;

		private Vector3 _testPointValue;
		private bool _isInsidePolygon;
		private bool _isInsideDepth;

		private void Start()
		{
			if (_cloneSource)
			{
				Points.Clear();
				Points.AddRange(_cloneSource.Points);
			}
		}

		protected override void OnLateUpdate()
		{
			base.OnLateUpdate();

			Vector3 listenerPosition = default;

			if (!(VA_Helper.GetListenerPosition(ref listenerPosition) && Points.Count > 1)) return;

			Vector3 worldPoint = listenerPosition;
			Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
			float closestDistanceSq = float.PositiveInfinity;
			Vector3 closestPoint = localPoint;

			switch (_path)
			{
				case ePath.Path:
					{
						closestPoint = ClosestPointOnPath(localPoint);
						worldPoint = transform.TransformPoint(closestPoint);

						SetInnerOuterPoint(worldPoint, false);
						break;
					}

				case ePath.VolumetricPolygon:
					{
						_isInsideDepth = localPoint.y > 0 && localPoint.y < _depth;

						if (!_isInsideDepth)
						{
							localPoint.y = Mathf.Clamp(localPoint.y, 0, _depth);
							closestPoint = localPoint;
						}

						_isInsidePolygon = PolygonContainsPoint(localPoint);
						if (!_isInsidePolygon)
						{
							float oldY = localPoint.y;
							Vector3 flatLocalPoint = localPoint;
							flatLocalPoint.y = 0;
							closestPoint = ClosestPointOnPath(flatLocalPoint);
							closestPoint.y = oldY;
						}

						worldPoint = transform.TransformPoint(closestPoint);
						SetInnerPoint(worldPoint, _isInsidePolygon && _isInsideDepth);
						SetOuterPoint(transform.TransformPoint(SnapLocalPoint(closestPoint, false)));

						break;
					}
			}
		}

		private Vector3 SnapLocalPoint(Vector3 localPoint, bool snapY)
		{
			if (!_isInsideDepth || !_isInsidePolygon) return localPoint;

			float normY = Mathf.InverseLerp(localPoint.y, 0, _depth);
			float resultY = snapY ? (normY > .5f ? _depth : 0) : localPoint.y;
			localPoint.y = 0;
			localPoint = ClosestPointOnPath(localPoint);
			localPoint.y = resultY;

			return localPoint;
		}

		private bool PolygonContainsPoint(Vector3 localPoint)
		{
			int j = Points.Count - 1;
			bool inside = false;
			for (int i = 0; i < Points.Count; j = i++)
			{
				Vector3 pi = Points[i];
				Vector3 pj = Points[j];
				if (((pi.z <= localPoint.z && localPoint.z < pj.z) || (pj.z <= localPoint.z && localPoint.z < pi.z)) &&
					(localPoint.x < MathUtility.SafeDivision((pj.x - pi.x) * (localPoint.z - pi.z), (pj.z - pi.z)) + pi.x))
					inside = !inside;
			}

			return inside;
		}

		private Vector3 ClosestPointOnPath(Vector3 localPoint)
		{
			float closestDistanceSq = float.PositiveInfinity;
			Vector3 closestPoint = Vector3.positiveInfinity;

			for (int i = 1; i < Points.Count; i++)
			{
				Vector3 closePoint = VA_Helper.ClosestPointToLineSegment(Points[i - 1], Points[i], localPoint);
				float closeDistanceSq = (closePoint - localPoint).sqrMagnitude;

				if (closeDistanceSq < closestDistanceSq)
				{
					closestDistanceSq = closeDistanceSq;
					closestPoint = closePoint;
				}
			}

			return closestPoint;
		}

		public override CullingRect GetCullRect(float cullMargin)
		{
			var rect = new CullingRect();
			rect.Reset();
			for (int i = 0; i < Points.Count; i++)
				rect.Expand(transform.TransformPoint(Points[i]));
			rect.Expand(cullMargin);
			return rect;
		}

        #region Editor
#if UNITY_EDITOR

		[Button]
		private void PastePolygonCollider2DPath()
		{
			Undo.RecordObject(this, "PastePolygonCollider2DPath");
			var path = JsonUtility.FromJson<List<Vector2>>(GUIUtility.systemCopyBuffer);
			Points.Clear();
			foreach (Vector2 vector2 in path)
			{
				Points.Add(new Vector3(vector2.x, 0, vector2.y));
			}
		}

		//[Button]
		private void ReversePointsOrder()
		{
			Undo.RecordObject(this, "ReversePointsOrder");
			Points.Reverse();
		}

		[Button, ShowIf(nameof(ScaleNotOne))]
		private void BakeScale()
		{
			Undo.RecordObject(this, "BakeScale");
			Undo.RecordObject(transform, "BakeScale");
			Vector3 localScale = transform.localScale;
			for (int i = 0; i < Points.Count; i++)
			{
				Vector3 point = Points[i];
				point.Scale(localScale);
				Points[i] = point;
			}

			transform.localScale = Vector3.one;
		}

		private bool ScaleNotOne()
		{
			return transform.localScale != Vector3.one;
		}

		public override void OnSceneGUI()
		{
			Handles.color = Color.red;
			Handles.matrix = transform.localToWorldMatrix;

			switch (_path)
			{
				case ePath.Path:
					{
						for (int i = 1; i < Points.Count; i++)
						{
							Handles.DrawLine(Points[i - 1], Points[i]);
						}

						break;
					}

				case ePath.VolumetricPolygon:
					{
						Vector3 offset = Vector3.up * _depth;

						for (int i = 1; i < Points.Count; i++)
						{
							Handles.DrawLine(Points[i - 1], Points[i]);
							Handles.DrawLine(Points[i - 1] + offset, Points[i] + offset);
						}

						break;
					}

				default:
					throw new ArgumentOutOfRangeException();
			}

			if (_testPoint)
			{
				_testPointValue = Handles.PositionHandle(_testPointValue, Quaternion.identity);
				Handles.color = PolygonContainsPoint(_testPointValue) ? Color.green : Color.red;
				Handles.DrawSolidDisc(_testPointValue, Vector3.up, 1);
			}

			if (!_edit)
			{
				return;
			}

			Matrix4x4 matrix = transform.localToWorldMatrix;
			Matrix4x4 inverse = transform.worldToLocalMatrix;

			for (int i = 0; i < Points.Count; i++)
			{
				Vector3 oldPoint = matrix.MultiplyPoint(Points[i]);

				Handles.matrix = VA_Helper.TranslationMatrix(oldPoint) * VA_Helper.ScalingMatrix(0.8f) *
					VA_Helper.TranslationMatrix(oldPoint * -1.0f);

				EditorGUI.BeginChangeCheck();

				Vector3 newPoint = Handles.PositionHandle(oldPoint, Quaternion.identity);

				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(this, "Move Path Point");
					Points[i] = inverse.MultiplyPoint(newPoint);
				}
			}

			Handles.BeginGUI();
			{
				for (int i = 0; i < Points.Count; i++)
				{
					Vector3 point = Points[i];
					string pointName = "Point " + i;
					Vector3 scrPoint = Camera.current.WorldToScreenPoint(matrix.MultiplyPoint(point));
					Rect rect = new Rect(0.0f, 0.0f, 50.0f, 20.0f);
					rect.center = new Vector2(scrPoint.x, Screen.height - scrPoint.y - 35.0f);
					Rect rect1 = rect;
					rect.x += 1.0f;
					Rect rect2 = rect;
					rect.x -= 1.0f;
					Rect rect3 = rect;
					rect.y += 1.0f;
					Rect rect4 = rect;
					rect.y -= 1.0f;

					GUI.Label(rect1, pointName, EditorStyles.miniBoldLabel);
					GUI.Label(rect2, pointName, EditorStyles.miniBoldLabel);
					GUI.Label(rect3, pointName, EditorStyles.miniBoldLabel);
					GUI.Label(rect4, pointName, EditorStyles.miniBoldLabel);
					GUI.Label(rect, pointName, EditorStyles.whiteMiniLabel);
				}

				for (int i = 1; i < Points.Count; i++)
				{
					Vector3 pointA = Points[i - 1];
					Vector3 pointB = Points[i];
					Vector3 midPoint = (pointA + pointB) * 0.5f;
					Vector3 scrPoint = Camera.current.WorldToScreenPoint(matrix.MultiplyPoint(midPoint));

					if (GUI.Button(new Rect(scrPoint.x - 5.0f, Screen.height - scrPoint.y - 45.0f, 20.0f, 20.0f), "+") == true)
					{
						Undo.RecordObject(this, "Split Path");
						Points.Insert(i, midPoint);
						GUI.changed = true;
					}
				}
			}

			Handles.EndGUI();
		}
#endif
		#endregion
	}
}