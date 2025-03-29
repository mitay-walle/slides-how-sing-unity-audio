using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace GameAssets.VolumetricAudio
{
	[ExecuteInEditMode]
	public sealed class VA_Path2D : VA_VolumetricShape
	{
		[ShowInInspector, HideInPlayMode] private bool _edit;
		[ShowInInspector, HideInPlayMode] private bool _testPoint;
		[SerializeField, HideIf(nameof(_cloneSource))] private List<Vector3> Points = new List<Vector3>();
		[SerializeField] private VA_Path2D _cloneSource;
		[SerializeField] private ePath _path;
		[SerializeField, ShowIf("@((int)_path) == 1")] private float _depth = 1;

		private bool _has2dCopy;
		private Vector2[] _points2D;

		private Vector3 _testPointValue;
		private bool _isInsidePolygon;
		private bool _isInsideDepth;

		[Button]
		public void Copy()
		{
			var other = GetComponent<VA_Path>();
			Points = other.Points;
			_path = other._path;
			_depth = other._depth;

			if (other._cloneSource)
				_cloneSource = other._cloneSource.gameObject.GetComponent<VA_Path2D>();
		}

		private void Start()
		{
			if (_cloneSource != null)
			{
				_cloneSource.Create2dCopy();
				_points2D = _cloneSource._points2D;
				_has2dCopy = true;
			}
			else
				Create2dCopy();
		}

		private void Create2dCopy()
		{
			if (_has2dCopy)
				return;

			int count = Points.Count;
			_points2D = new Vector2[count];
			for (int i = 0; i < count; i++)
			{
				var worldPoint = transform.TransformPoint(Points[i]);
				_points2D[i] = new Vector2(worldPoint.x, worldPoint.z);
			}
			_has2dCopy = true;

#if UNITY_EDITOR
			//��������� �� ��������� �����, ����� ������ �������� ������� 0
			float minMagnitude = .001f;
			for (int i = 1; i < count; i++)
			{
				if ((_points2D[i - 1] - _points2D[i]).magnitude < minMagnitude)
					Debug.LogError($"VA_Path2D {name} ����� {i - 1} � {i} ����������� ������� ������", gameObject);
			}

			if ((_points2D[count - 1] - _points2D[0]).magnitude > minMagnitude)
				Debug.LogError($"VA_Path2D {name} ����� ������� ����������� ������� ������ � �� �������� �������", gameObject);
#endif
		}

		protected override void OnLateUpdate()
		{
			base.OnLateUpdate();

			Vector3 listenerPosition = default;
			if (!(VA_Helper.GetListenerPosition(ref listenerPosition) && Points.Count > 1))
				return;

			Vector3 worldPoint = listenerPosition;
			float minHeight = transform.position.y;
			float maxHeight = minHeight + _depth;

			Vector3 inner = worldPoint;
			Vector3 outer = worldPoint;

			var closePoint2d = ClosestPointOnPath2D(worldPoint);
			var closePoint = new Vector3(closePoint2d.x, minHeight, closePoint2d.y);

			switch (_path)
			{
				case ePath.Path:
					SetInnerOuterPoint(closePoint, false);
					break;

				case ePath.VolumetricPolygon:

					_isInsideDepth = worldPoint.y > minHeight && worldPoint.y < maxHeight;
					_isInsidePolygon = PolygonContainsPoint(worldPoint);

					if (!_isInsideDepth)
					{
						inner.y = Mathf.Clamp(inner.y, minHeight, maxHeight);
					}
					else
					{
						float midle = (maxHeight + minHeight) * 0.5f;
						outer.y = outer.y > midle ? maxHeight : minHeight;
					}

					if (!_isInsidePolygon)
					{
						inner.x = closePoint.x;
						inner.z = closePoint.z;
					}
					else
					{
						outer.x = closePoint.x;
						outer.z = closePoint.z;
					}

					SetInnerPoint(inner, _isInsidePolygon && _isInsideDepth);
					SetOuterPoint(outer);

					break;
			}
		}

		private bool PolygonContainsPoint(Vector3 localPoint)
		{
			var tx = localPoint.x;
			var ty = localPoint.z;
			int count = _points2D.Length;
			int j = count - 1;

			bool inside = false;
			for (int i = 0; i < count; j = i++)
			{
				var pi = _points2D[i];
				var pj = _points2D[j];
				if (((pi.y <= ty && ty < pj.y) || (pj.y <= ty && ty < pi.y))
					&& (tx < MathUtility.SafeDivision((pj.x - pi.x) * (ty - pi.y), (pj.y - pi.y)) + pi.x))
					inside = !inside;
			}

			return inside;
		}

		private Vector2 ClosestPointOnPath2D(Vector3 localPoint)
		{
			var closestDistanceSq = float.PositiveInfinity;
			var closestPoint = Vector2.positiveInfinity;
			var point = new Vector2(localPoint.x, localPoint.z);
			var last = _points2D.Length - 1;

			for (int i = 0; i < last; i++)
			{
				var delta = _points2D[i] - _points2D[i + 1];
				var l = delta.magnitude;
				var d = delta / l;

				var dot = Vector2.Dot(point - _points2D[i], d);
				Vector2 closePoint = _points2D[i] + Mathf.Clamp(dot, 0.0f, l) * d;
				float closeDistanceSq = Vector2.SqrMagnitude(closePoint - point);

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
			List<Vector2> path = JsonUtility.FromJson<List<Vector2>>(GUIUtility.systemCopyBuffer);
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