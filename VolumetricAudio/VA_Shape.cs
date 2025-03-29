using System;
using GameJam.Plugins.QualityOfLife.SceneGUIBehaviour;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameAssets.VolumetricAudio
{
	public abstract class VA_Shape : OnSceneGUIBehaviour
	{
		// Does this shape have an outer point? (the closest point between this shape's outer shell and the audio listener)
		[HideInEditorMode] public bool OuterPointSet;

		// The position of the outer point
		[HideInEditorMode] public Vector3 OuterPoint;

		// The distance between the outer point and the audio listener
		[HideInEditorMode] public float OuterPointDistance;

		[ShowInInspector, NonSerialized] protected bool _editor;

		public virtual bool FinalPointSet => OuterPointSet;
		public virtual Vector3 FinalPoint => OuterPoint;
		public virtual float FinalPointDistance => OuterPointDistance;

		public void SetOuterPoint(Vector3 newOuterPoint)
		{
			// Make sure the listener exists
			Vector3 listenerPosition = default(Vector3);

			if (VA_Helper.GetListenerPosition(ref listenerPosition) == true)
			{
				OuterPointSet = true;
				OuterPoint = newOuterPoint;
				OuterPointDistance = Vector3.Distance(listenerPosition, newOuterPoint);
			}
		}

		protected virtual void LateUpdate()
		{
#if UNITY_EDITOR
			if (!_editor && !Application.isPlaying) return;
#endif
			if (enabled)
				OnLateUpdate();
		}

		protected virtual void OnLateUpdate()
		{
			OuterPointSet = false;
		}

		public override void OnSceneGUI() { }

		public virtual CullingRect GetCullRect(float cullMargin)
		{
			throw new NotImplementedException();
		}

		// protected void OnDrawGizmos()
		// {
		// 	if (_editor)
		// 	{
		// 		LateUpdate();
		// 	}
		// }
	}
}