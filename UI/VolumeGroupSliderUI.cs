using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Plugins.Audio
{
	[RequireComponent(typeof(Slider))]
	public class VolumeGroupSliderUI : MonoBehaviour
	{
		private Slider _slider;
		[SerializeField, Required] private AudioMixer _mixer;
		[SerializeField, Required] private AudioMixerGroup _group;

		[ShowInInspector] private string AudioMixerPropertyKey => $"{_group?.name} Volume";

		private void Start() => Init();
		private void Init()
		{
			_slider = GetComponent<Slider>();
			_slider.onValueChanged.RemoveListener(OnValueChanged);
			_slider.onValueChanged.AddListener(OnValueChanged);
			_slider.SetValueWithoutNotify(GetVolume());
		}

		private void OnValueChanged(float value)
		{
			if (!_mixer) return;
			_mixer.SetFloat(AudioMixerPropertyKey, Mathf.Log10(Mathf.Max(0.0001f, value)) * 20);
			PlayerPrefs.SetFloat(AudioMixerPropertyKey, value);
			PlayerPrefs.Save();
		}

		private float GetVolume()
		{
			if (!_mixer) return 0;
			return PlayerPrefs.GetFloat(AudioMixerPropertyKey, 1);
		}

		[OnInspectorGUI]
		private void OnInspectorGUI()
		{
			if (!_mixer || !_group) return;
			_mixer.GetFloat(AudioMixerPropertyKey, out float value);
		}
	}
}