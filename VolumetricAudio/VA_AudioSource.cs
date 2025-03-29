using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameAssets.VolumetricAudio
{
	[ExecuteInEditMode]
	[AddComponentMenu("Volumetric Audio/VA Audio Source")]
	public class VA_AudioSource : MonoBehaviour
	{
		private const string TOOLTIP1 = "The shapes you want the audio source to emit from";
		private const string TOOLTIP2 = "The shapes you want the audio source to be excluded from";
		private const string TOOLTIP3 = "Should this sound have its position update?";
		private const string TOOLTIP4 = "The speed at which the sound position changes (0 = instant)";
		private const string TOOLTIP5 = "Should this sound have its Spatial Blend update?";
		private const string TOOLTIP6 = "The distance at which the sound becomes fuly mono";
		private const string TOOLTIP7 = "The distance at which the sound becomes fuly stereo";
		private const string TOOLTIP8 = "The distribution of the mono to stereo ratio";
		private const string TOOLTIP9 = "Should this sound have its volume update?";
		private const string TOOLTIP10 = "The base volume of the audio source";
		private const string TOOLTIP11 = "The zone this sound is associated with";
		private const string TOOLTIP12 = "Should the volume fade based on distance?";
		private const string TOOLTIP13 = "The distance at which the sound fades to maximum volume";
		private const string TOOLTIP14 = "The distance at which the sound fades to minimum volume";
		private const string TOOLTIP15 = "The distribution of volume based on its scaled distance";
		private const string TOOLTIP16 = "Should this sound be blocked when behind other objects?";
		private const string TOOLTIP17 = "The raycast style against the occlusion groups";
		private const string TOOLTIP18 = "Check for VA_Material instances attached to the occlusion object";
		private const string TOOLTIP19 = "How quickly the sound fades in/out when behind an object";
		private const string TOOLTIP20 = "The amount of occlusion checks";
		private const string TOOLTIP21 = "Set duration of playing";

		private static Keyframe[] defaultBlendCurveKeys = { new Keyframe(0.0f, 0.0f), new Keyframe(1.0f, 1.0f) };
		private static Keyframe[] defaultVolumeCurveKeys = { new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 0.0f) };

		[ShowInInspector] private bool _editor;

		[Tooltip(TOOLTIP1), Required] public List<VA_Shape> Shapes;
		[Tooltip(TOOLTIP2), Required] public List<VA_VolumetricShape> ExcludedShapes;
		[Tooltip(TOOLTIP11)] public VA_Zone Zone;

		[BoxGroup("Position")] [Tooltip(TOOLTIP3)] public bool Position = true;
		[BoxGroup("Position")] [Tooltip(TOOLTIP4)] [ShowIf(nameof(Position))] public float PositionDampening;
		[BoxGroup("Spatial")] [Tooltip(TOOLTIP5)] public bool Blend;
		[BoxGroup("Spatial")] [Tooltip(TOOLTIP6)] [ShowIf(nameof(Blend))] public float BlendMinDistance = 0.0f;
		[BoxGroup("Spatial")] [Tooltip(TOOLTIP7)] [ShowIf(nameof(Blend))] public float BlendMaxDistance = 5.0f;
		[BoxGroup("Spatial")] [Tooltip(TOOLTIP8)] [ShowIf(nameof(Blend))] public AnimationCurve BlendCurve;
		[BoxGroup("Volume")] [Tooltip(TOOLTIP9)] public bool Volume = true;
		[BoxGroup("Volume")] [Tooltip(TOOLTIP10)] [Range(0.0f, 1.0f)] [ShowIf(nameof(Volume))] public float BaseVolume = 1.0f;
		[BoxGroup("Volume")] [SerializeField] public float FadeIn;
		[BoxGroup("Volume")] [SerializeField] public float FadeOut;
		[BoxGroup("Volume")] [Tooltip(TOOLTIP12)] [ShowIf(nameof(Volume))] public bool Fade;
		[BoxGroup("Volume")] [Tooltip(TOOLTIP13)] [ShowIf(nameof(Volume))] public float FadeMinDistance = 0.0f;
		[BoxGroup("Volume")] [Tooltip(TOOLTIP14)] [ShowIf(nameof(Volume))] public float FadeMaxDistance = 5.0f;
		[BoxGroup("Volume")] [Tooltip(TOOLTIP15)] [ShowIf(nameof(Volume))] public AnimationCurve FadeCurve;
		[BoxGroup("Occlusion")] [Tooltip(TOOLTIP16)] public bool Occlude;
		[BoxGroup("Occlusion")] [Tooltip(TOOLTIP17)] [ShowIf(nameof(Occlude))] public OccludeType OccludeMethod;
		[BoxGroup("Occlusion")] [Tooltip(TOOLTIP18)] [ShowIf(nameof(Occlude))] public bool OccludeMaterial;
		[BoxGroup("Occlusion")] [Tooltip(TOOLTIP19)] [ShowIf(nameof(Occlude))] public float OccludeDampening = 5.0f;
		[BoxGroup("Occlusion")] [Tooltip(TOOLTIP20)] [ShowIf(nameof(Occlude))] public List<OccludeGroup> OccludeGroups;
		[BoxGroup("Occlusion")] [ShowIf(nameof(Occlude))] public float OccludeAmount = 1.0f;
		[Tooltip(TOOLTIP21)] [SerializeField] public float Duration;

		[NonSerialized, ShowInInspector] private VA_Shape closestShape;
		[NonSerialized, ShowInInspector] private Vector3 closestPoint;
		[NonSerialized, ShowInInspector] private float closestDistance = float.PositiveInfinity;
		[NonSerialized] private AudioSource audioSource;
		private float _startTime;

		public void UpdateDuration(float duration)
		{
			_startTime = Time.time;
			Duration = duration;
		}

		protected virtual void Start()
		{
			if (BlendCurve == null)
			{
				BlendCurve = new AnimationCurve(defaultBlendCurveKeys);
			}

			if (FadeCurve == null)
			{
				FadeCurve = new AnimationCurve(defaultVolumeCurveKeys);
			}

			_startTime = Time.time;
		}

		protected virtual void LateUpdate()
		{
#if UNITY_EDITOR
			if (!_editor && !Application.isPlaying) return;
#endif
			if (Duration > 0f && Time.time > _startTime + Duration)
			{
				SetVolume(0);
				return;
			}

			// Make sure the listener exists
			Vector3 listenerPosition = default(Vector3);

			if (VA_Helper.GetListenerPosition(ref listenerPosition))
			{
				if (Position)
				{
					closestShape = null;
					closestPoint = listenerPosition;
					closestDistance = float.PositiveInfinity;

					if (Shapes != null)
					{
						// Find closest point to all shapes

						for (int i = Shapes.Count - 1; i >= 0; i--)
						{
							VA_Shape shape = Shapes[i];

							if (VA_Helper.Enabled(shape) && shape.FinalPointSet && shape.FinalPointDistance < closestDistance)
							{
								closestDistance = shape.FinalPointDistance;
								closestPoint = shape.FinalPoint;
								closestShape = shape;
							}
						}
					}

					if (ExcludedShapes != null)
					{
						// If the closest point is closer than the excluded point, then make the excluded point the closest

						for (int i = ExcludedShapes.Count - 1; i >= 0; i--)
						{
							VA_VolumetricShape excludedShape = ExcludedShapes[i];

							if (VA_Helper.Enabled(excludedShape) && !excludedShape.IsHollow && excludedShape.InnerPointInside)
							{
								if (excludedShape.OuterPointSet && excludedShape.OuterPointDistance > closestDistance)
								{
									closestDistance = excludedShape.OuterPointDistance;
									closestPoint = excludedShape.OuterPoint;
									closestShape = excludedShape;

									break;
								}
							}
						}
					}

					if (closestShape != null)
					{
						if (PositionDampening <= 0.0f)
						{
							transform.position = closestPoint;
						}
						else
						{
							transform.position = VA_Helper.Dampen3(transform.position, closestPoint, PositionDampening, Time.deltaTime);
						}
					}
					else
					{
						closestPoint = transform.position;
						closestDistance = Vector3.SqrMagnitude(closestPoint - listenerPosition);
					}
				}

				if (Blend)
				{
					// Modify the blend?

					float distance = Vector3.Distance(transform.position, listenerPosition);
					float distance01 = Mathf.InverseLerp(BlendMinDistance, BlendMaxDistance, distance);

					SetPanLevel(BlendCurve.Evaluate(distance01));
				}

				if (Volume)
				{
					// Modify the volume?

					float finalVolume = BaseVolume;

					if (Zone != null)
					{
						// Modify via zone?
						finalVolume *= Zone.Volume;
					}

					if (Fade)
					{
						// Modify via distance?

						float distance = Vector3.Distance(transform.position, listenerPosition);
						float distance01 = Mathf.InverseLerp(FadeMinDistance, FadeMaxDistance, distance);

						finalVolume *= FadeCurve.Evaluate(distance01);
					}

					if (FadeIn > 0f && Time.time > _startTime && Time.time < _startTime + FadeIn)
					{
						float step = Mathf.InverseLerp(0f, 1f, MathUtility.SafeDivision(Time.time - _startTime, FadeIn));
						finalVolume *= step;
					}

					if (FadeOut > 0f && Time.time > _startTime + (Duration - FadeOut) && Time.time < _startTime + Duration)
					{
						float step = Mathf.InverseLerp(1f, 0f, MathUtility.SafeDivision(Time.time - (_startTime + (Duration - FadeOut)),  FadeOut));
						finalVolume *= step;
					}

					if (Occlude)
					{
						// Modify via occlusion?

						Vector3 direction = listenerPosition - transform.position;
						float targetAmount = 1.0f;

						if (OccludeGroups != null)
						{
							for (int i = OccludeGroups.Count - 1; i >= 0; i--)
							{
								OccludeGroup group = OccludeGroups[i];

								switch (OccludeMethod)
								{
									case OccludeType.Raycast:
									{
										RaycastHit hit = default(RaycastHit);

										if (Physics.Raycast(transform.position, direction, out hit, direction.magnitude, @group.Layers))
										{
											targetAmount *= GetOcclusionVolume(group, hit);
										}
									}
										break;

									case OccludeType.RaycastAll:
									{
										// TODO: NonAlloc
										RaycastHit[] hits = Physics.RaycastAll(transform.position, direction, direction.magnitude, group.Layers);

										for (int j = hits.Length - 1; j >= 0; j--)
										{
											targetAmount *= GetOcclusionVolume(group, hits[j]);
										}
									}
										break;
								}
							}
						}

						OccludeAmount = VA_Helper.Dampen(OccludeAmount, targetAmount, OccludeDampening, Time.deltaTime, 0.1f);

						finalVolume *= OccludeAmount;
					}

					SetVolume(finalVolume);
				}
			}
		}

		private float GetOcclusionVolume(OccludeGroup group, RaycastHit hit)
		{
			if (OccludeMaterial)
			{
				VA_Material material = hit.collider.GetComponentInParent<VA_Material>();

				if (material != null)
				{
					return material.OcclusionVolume;
				}
			}

			return group.Volume;
		}

		// If you're not using Unity's built-in audio system, then modify the code below, or make a new component that inherits VA_AudioSource and overrides this method
		protected virtual void SetPanLevel(float newPanLevel)
		{
			if (audioSource == null) audioSource = GetComponent<AudioSource>();

			if (audioSource != null)
			{
				audioSource.spatialBlend = newPanLevel;
			}
		}

		// If you're not using Unity's built-in audio system, then modify the code below, or make a new component that inherits VA_AudioSource and overrides this method
		protected virtual void SetVolume(float newVolume)
		{
			if (audioSource == null) audioSource = GetComponent<AudioSource>();

			if (audioSource != null)
			{
				audioSource.volume = newVolume;
			}
		}

		public enum OccludeType
		{
			Raycast,
			RaycastAll,
		}

		[Serializable]
		public class OccludeGroup
		{
			public LayerMask Layers;

			[Range(0.0f, 1.0f)] public float Volume;
		}

		#region Editor

#if UNITY_EDITOR
		protected virtual void OnDrawGizmos()
		{
			if (_editor)
			{
				LateUpdate();
			}
		}

		protected virtual void OnDrawGizmosSelected()
		{
			// Draw red lines from this audio source to all shapes
			Gizmos.color = Color.red;

			if (Shapes != null)
			{
				for (int i = Shapes.Count - 1; i >= 0; i--)
				{
					VA_Shape shape = Shapes[i];

					if (VA_Helper.Enabled(shape) && shape.FinalPointSet)
					{
						Gizmos.DrawLine(transform.position, shape.FinalPoint);
					}
				}
			}

			// Draw green spheres for blend distances
			if (Blend)
			{
				for (int i = 0; i <= 50; i++)
				{
					float frac = i * 0.02f;
					Gizmos.color = new Color(0.0f, 1.0f, 0.0f, BlendCurve.Evaluate(frac));
					Gizmos.DrawWireSphere(transform.position, Mathf.Lerp(BlendMinDistance, BlendMaxDistance, frac));
				}
			}

			// Draw blue spheres for volume distances
			if (Fade)
			{
				for (int i = 0; i <= 50; i++)
				{
					float frac = i * 0.02f;
					Gizmos.color = new Color(0.0f, 0.0f, 1.0f, BlendCurve.Evaluate(frac));
					Gizmos.DrawWireSphere(transform.position, Mathf.Lerp(FadeMinDistance, FadeMaxDistance, frac));
				}
			}
		}
#endif

		#endregion
	}
}