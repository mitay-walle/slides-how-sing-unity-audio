using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
#endif

namespace Plugins.Audio
{
	[CreateAssetMenu]
	public class ADSRProfile : ScriptableObject
	{
		public AnimationCurve Attack = AnimationCurve.Linear(0, 0, 1, 1);
		public AnimationCurve Decay = AnimationCurve.Linear(0, 1, 1, .5f);
		public AnimationCurve Sustain = AnimationCurve.Constant(0, 1, .5f);
		public AnimationCurve Release = AnimationCurve.Linear(0, 1, 1, 0);
		[PropertyRange(0, 1)] public float Lerp = .5f;
		public float Multiplier = 1;
	}

	public enum eADSR
	{
		None,
		Attack,
		Decay,
		Sustain,
		Release,
	}

	[Serializable]
	public class ADSR
	{
		[SerializeField, Required] private ADSRProfile _profile = default;

		[ShowInInspector, ReadOnly, PropertyRange(-5, 5)] private float _value = 0;
		[ShowInInspector, ReadOnly] private eADSR _state;
		[ShowInInspector, ReadOnly] private float _time = float.MaxValue;
		[ShowInInspector, ReadOnly] private float _releaseTime = 0;
		[ShowInInspector, ReadOnly] private float _target = 0;

		private bool _lastIsPressed;

		public float Time => _time;
		public float Value => _value;

		public UnityEvent<float> OnApply;
		
		
		public void OnUpdate(bool isPressed, float deltaTime)
		{
			float attack = GetLastTime(_profile.Attack);
			float decay = GetLastTime(_profile.Decay);
			float release = GetLastTime(_profile.Release);

			if (_lastIsPressed != isPressed)
			{
				if (isPressed)
				{
					_time = 0;
				}
				else
				{
					_releaseTime = _time;
				}
			}
			_lastIsPressed = isPressed;
			_target = _value;

			_time += deltaTime;
			if (isPressed)
			{
				if (_time < attack)
				{
					_target = _profile.Attack.Evaluate(_time) * _profile.Multiplier;
					_state = eADSR.Attack;
					Lerp();
					return;
				}

				if (_time < decay + attack)
				{
					_target = _profile.Decay.Evaluate(_time - attack) * _profile.Multiplier;
					_state = eADSR.Decay;
					Lerp();
					return;
				}

				_target = _profile.Sustain.Evaluate(_time - attack - decay) * _profile.Multiplier;
				_state = eADSR.Sustain;
				Lerp();
				return;
			}
			_target = _profile.Release.Evaluate(_time - _releaseTime) * _profile.Multiplier;
			_state = _time - _releaseTime > release ? eADSR.None : eADSR.Release;
			Lerp();
		}

		private void Lerp()
		{
			_value = Mathf.Lerp(_value, _target, _profile.Lerp);
			OnApply?.Invoke(_value);
		}
		private float GetLastTime(AnimationCurve curve) => curve[curve.length - 1].time;
#if UNITY_EDITOR
		[OnInspectorGUI] private void OnInspectorGUI() => GUIHelper.RequestRepaint();
#endif
	}
}