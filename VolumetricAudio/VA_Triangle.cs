﻿using UnityEngine;

namespace GameAssets.VolumetricAudio
{
	[System.Serializable]
	public class VA_Triangle
	{
		// Points
		public Vector3  A;
		public Vector3  B;
		public Vector3  C;

		// Planes
		public VA_Plane PlaneABC;
		public VA_Plane PlaneAB;
		public VA_Plane PlaneBC;
		public VA_Plane PlaneCA;

		public Vector3 Min
		{
			get
			{
				return Vector3.Min(A, Vector3.Min(B, C));
			}
		}

		public Vector3 Max
		{
			get
			{
				return Vector3.Max(A, Vector3.Max(B, C));
			}
		}

		public float MidX
		{
			get
			{
				return (A.x + B.x + C.x) * 0.3333333333333f;
			}
		}

		public float MidY
		{
			get
			{
				return (A.y + B.y + C.y) * 0.3333333333333f;
			}
		}

		public float MidZ
		{
			get
			{
				return (A.z + B.z + C.z) * 0.3333333333333f;
			}
		}

		public void CalculatePlanes()
		{
			PlaneABC = new VA_Plane(A, B, C);
			PlaneAB  = new VA_Plane(A, B, A + PlaneABC.Normal);
			PlaneBC  = new VA_Plane(B, C, B + PlaneABC.Normal);
			PlaneCA  = new VA_Plane(C, A, C + PlaneABC.Normal);
		}

		public Vector3 ClosestTo(Vector3 p)
		{
			if (PlaneAB.SideOf(p) == true) return ClosestPointToLineSegment(A, B, p);
			if (PlaneBC.SideOf(p) == true) return ClosestPointToLineSegment(B, C, p);
			if (PlaneCA.SideOf(p) == true) return ClosestPointToLineSegment(C, A, p);

			return PlaneABC.ClosestTo(p);
		}

		private Vector3 ClosestPointToLineSegment(Vector3 a, Vector3 b, Vector3 p)
		{
			var ab    = b - a;
			var denom = Vector3.Dot(ab, ab);

			if (denom > 0.0f)
			{
				var ap = p - a;
				var d  = MathUtility.SafeDivision(Vector3.Dot(ap, ab),  denom);

				return a + ab * Mathf.Clamp01(d);
			}

			return a;
		}
	}
}