using UnityEngine;

namespace GameAssets.VolumetricAudio
{
	[ExecuteInEditMode]
	[AddComponentMenu("Volumetric Audio/VA Mesh")]
	public class VA_Mesh : VA_VolumetricShape
	{
		[SerializeField] private MeshCollider MeshCollider;
		[SerializeField] private MeshFilter MeshFilter;
		[SerializeField] private Mesh Mesh;

		[Tooltip("How far apart each volume checking ray should be separated to avoid miscalculations. This value should be based on the size of your mesh, but be kept quite low")]
		private float RaySeparation = 0.1f;

		[SerializeField] private VA_MeshTree tree = new VA_MeshTree();

		public bool IsBaked
		{
			get { return tree != null && tree.Nodes != null && tree.Nodes.Count > 0; }
		}

		public void ClearBake()
		{
			if (tree != null)
			{
				tree.Clear();
			}
		}

		public void Bake()
		{
			if (tree == null) tree = new VA_MeshTree();

			tree.Bake(Mesh);
		}

		protected virtual void Reset()
		{
			IsHollow = true; // NOTE: This is left as true by default to prevent applying volume to meshes with holes
			MeshCollider = GetComponent<MeshCollider>();
			MeshFilter = GetComponent<MeshFilter>();
		}

		protected override void OnLateUpdate()
		{
			base.OnLateUpdate();

			// Make sure the listener exists
			Vector3 listenerPosition = default(Vector3);

			if (VA_Helper.GetListenerPosition(ref listenerPosition) == true)
			{
				UpdateFields();

				Vector3 worldPoint = listenerPosition;
				Vector3 localPoint = transform.InverseTransformPoint(worldPoint);

				if (Mesh != null)
				{
					if (IsHollow == true)
					{
						localPoint = SnapLocalPoint(localPoint);
						worldPoint = transform.TransformPoint(localPoint);

						SetOuterPoint(worldPoint);
					}
					else
					{
						if (PointInMesh(localPoint, worldPoint) == true)
						{
							SetInnerPoint(worldPoint, true);

							localPoint = SnapLocalPoint(localPoint);
							worldPoint = transform.TransformPoint(localPoint);

							SetOuterPoint(worldPoint);
						}
						else
						{
							localPoint = SnapLocalPoint(localPoint);
							worldPoint = transform.TransformPoint(localPoint);

							SetInnerOuterPoint(worldPoint, false);
						}
					}
				}
			}
		}

#if UNITY_EDITOR
		protected virtual void OnDrawGizmosSelected()
		{
			UpdateFields();

			Gizmos.color = Color.red;
			Gizmos.matrix = transform.localToWorldMatrix;

			if (IsBaked == true)
			{
				for (int i = tree.Triangles.Count - 1; i >= 0; i--)
				{
					VA_Triangle triangle = tree.Triangles[i];

					Gizmos.DrawLine(triangle.A, triangle.B);
					Gizmos.DrawLine(triangle.B, triangle.C);
					Gizmos.DrawLine(triangle.C, triangle.A);
				}
			}
			else
			{
				if (Mesh == null) return;
				Vector3[] positions = Mesh.vertices;

				for (int i = 0; i < Mesh.subMeshCount; i++)
				{
					if (Mesh.GetTopology(i) != MeshTopology.Triangles) continue;

					int[] indices = Mesh.GetTriangles(i);

					for (int j = 0; j < indices.Length; j += 3)
					{
						Vector3 point1 = positions[indices[j + 0]];
						Vector3 point2 = positions[indices[j + 1]];
						Vector3 point3 = positions[indices[j + 2]];

						Gizmos.DrawLine(point1, point2);
						Gizmos.DrawLine(point2, point3);
						Gizmos.DrawLine(point3, point1);
					}
				}
			}
		}
#endif

		private Vector3 FindClosestLocalPoint(Vector3 localPoint)
		{
			// Tree search?
			if (IsBaked == true)
			{
				return tree.FindClosestPoint(localPoint);
			}
			// Linear search?
			else
			{
				return VA_MeshHelper.FindClosestPoint(Mesh, localPoint);
			}
		}

		private void UpdateFields()
		{
			if (MeshCollider != null)
			{
				Mesh = MeshCollider.sharedMesh;
			}
			else if (MeshFilter != null)
			{
				Mesh = MeshFilter.sharedMesh;
			}
		}

		private int RaycastHitCount(Vector3 origin, Vector3 direction, float separation)
		{
			int hitCount = 0;

			if (MeshCollider != null && separation > 0.0f)
			{
				float meshSize = Vector3.Magnitude(MeshCollider.bounds.size);
				float lengthA = meshSize;
				float lengthB = meshSize;
				Ray rayA = new Ray(origin, direction);
				Ray rayB = new Ray(origin + direction * meshSize, -direction);
				RaycastHit hit = default(RaycastHit);

				for (int i = 0; i < 50; i++)
				{
					if (MeshCollider.Raycast(rayA, out hit, lengthA) == true)
					{
						lengthA -= hit.distance + separation;

						rayA.origin = hit.point + rayA.direction * separation;
						hitCount += 1;
					}
					else
					{
						break;
					}
				}

				for (int i = 0; i < 50; i++)
				{
					if (MeshCollider.Raycast(rayB, out hit, lengthB) == true)
					{
						lengthB -= hit.distance + separation;

						rayB.origin = hit.point + rayB.direction * separation;
						hitCount += 1;
					}
					else
					{
						break;
					}
				}
			}

			return hitCount;
		}

		private bool PointInMesh(Vector3 localPoint, Vector3 worldPoint)
		{
			if (Mesh.bounds.Contains(localPoint) == false) return false;

			int hitCount = RaycastHitCount(worldPoint, Vector3.up, RaySeparation);

			if (hitCount == 0 || MathUtility.SafeRemainder(hitCount, 2) == 0) return false;

			return true;
		}

		private Vector3 SnapLocalPoint(Vector3 localPoint)
		{
			return FindClosestLocalPoint(localPoint);
		}
	}
}