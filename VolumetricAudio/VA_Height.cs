using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameAssets.VolumetricAudio
{
	public enum eComparsionCondition
	{
		Lower,
		Higher,
	}

	public sealed class VA_Height : VA_VolumetricShape
	{
		[SerializeField] private eComparsionCondition _comparsion;
		[SerializeField] private float _height;
		[ShowInInspector] private Color _gizmo = Color.red;

		protected override void OnLateUpdate()
		{
			base.OnLateUpdate();
			var listenerPosition = default(Vector3);
			if (VA_Helper.GetListenerPosition(ref listenerPosition) == true)
			{
				Vector3 outer = listenerPosition;
				Vector3 inner = listenerPosition;
				bool inside = false;
				switch (_comparsion)
				{
					case eComparsionCondition.Lower:
					{
						inside = listenerPosition.y < _height;
						if (inside)
						{
							outer.y = _height;
						}
						else
						{
							inner.y = _height;
						}

						break;
					}

					case eComparsionCondition.Higher:
					default:
					{
						inside = listenerPosition.y > _height;
						if (inside)
						{
							outer.y = _height;
						}
						else
						{
							inner.y = _height;
						}

						break;
					}
				}

				SetOuterPoint(outer);
				SetInnerPoint(inner, inside);
			}
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = _gizmo;
			var center = new Vector3(transform.position.x, _height, transform.position.z);
			Gizmos.DrawLine(center + Vector3.right * 1000, center - Vector3.right * 1000);
			Gizmos.DrawLine(center + Vector3.forward * 1000, center - Vector3.forward * 1000);
			if (_comparsion == eComparsionCondition.Lower)
			{
				Gizmos.DrawLine(center, center + Vector3.down);
			}
			else
			{
				Gizmos.DrawLine(center, center + Vector3.up);
			}
		}
	}
}