using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameAssets.VolumetricAudio
{
	public abstract class VA_VolumetricShape : VA_Shape
	{
		[Tooltip("If you set this, then sound will only emit from the thin shell around the shape, else it will emit from inside too")]
		public bool IsHollow;

		// Does this shape have an inner point? (the closest point between this shape's volume and the audio listener)
		[HideInEditorMode] public bool InnerPointSet;

		// The position of the inner point
		[HideInEditorMode] public Vector3 InnerPoint;

		// The distance between the inner point and the audio listener
		[HideInEditorMode] public float InnerPointDistance;

		// If the inner point is inside the volume of the shape
		[HideInEditorMode] public bool InnerPointInside;

		public override bool FinalPointSet => IsHollow == true ? OuterPointSet : InnerPointSet;
		public override Vector3 FinalPoint => IsHollow == true ? OuterPoint : InnerPoint;
		public override float FinalPointDistance => IsHollow == true ? OuterPointDistance : InnerPointDistance;

		public void SetInnerPoint(Vector3 newInnerPoint, bool inside)
		{
			// Make sure the listener exists
			var listenerPosition = default(Vector3);

			if (VA_Helper.GetListenerPosition(ref listenerPosition) == true)
			{
				InnerPointSet = true;
				InnerPoint = newInnerPoint;
				InnerPointDistance = Vector3.Distance(listenerPosition, newInnerPoint);
				InnerPointInside = inside;
			}
		}

		public void SetInnerOuterPoint(Vector3 newInnerOuterPoint, bool inside)
		{
			SetInnerPoint(newInnerOuterPoint, inside);
			SetOuterPoint(newInnerOuterPoint);
		}

		protected override void OnLateUpdate()
		{
			base.OnLateUpdate();

			InnerPointSet = false;
		}
	}
}