using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameJam.Plugins.Audio
{
	public class PlayRandomSound : MonoBehaviour
	{
		public enum eTime
		{
			Normal,
			Unscaled,
		}

		[FoldoutGroup("References"), SerializeField] private AudioSource AS;
		[FoldoutGroup("References"), SerializeField] private AudioClip[] _clips;
		[FoldoutGroup("Settings"), SerializeField] private bool _playOnEnable;
		[FoldoutGroup("Settings"), SerializeField] private bool _oneShot;
		[FoldoutGroup("Settings"), SerializeField] private bool _repeat;
		[FoldoutGroup("Settings"), SerializeField] private bool _randomTime;
		[FoldoutGroup("Settings"), SerializeField] private bool _useVolume;
		[ShowIf(nameof(_useVolume))]
		[FoldoutGroup("Settings"), SerializeField] private Vector2 _volume = Vector2.one;
		[FoldoutGroup("Settings"), SerializeField] private bool _usePitch;
		[ShowIf(nameof(_usePitch))]
		[FoldoutGroup("Settings"), SerializeField] private Vector2 _pitch = Vector2.one;
		[FoldoutGroup("Settings"), SerializeField] private AudioLowPassFilter _lowFilter;
		[ShowIf(nameof(_lowFilter))] [FoldoutGroup("Settings"), SerializeField] private bool _useRandomLowPass;
		[ShowIf(nameof(_lowFilter))] [FoldoutGroup("Settings"), SerializeField] private Vector2 _randomLowPass = new(300, 3000);

		[FoldoutGroup("Settings"), SerializeField] private AudioHighPassFilter _highFilter;
		[ShowIf(nameof(_highFilter))] [FoldoutGroup("Settings"), SerializeField] private bool _useRandomHighPass;
		[ShowIf(nameof(_highFilter))] [FoldoutGroup("Settings"), SerializeField] private Vector2 _randomHighPass = new(3000, 7000);
		[FoldoutGroup("Settings"), SerializeField] private Vector2 _delay;
		[FoldoutGroup("Settings"), SerializeField] private float _delayBetweenPlays;
		[FoldoutGroup("Settings"), SerializeField] private float _fadeIn;
		[FoldoutGroup("Settings"), SerializeField] private float _fadeOut;
		[FoldoutGroup("Settings"), SerializeField] private eTime _time;

		private float _delayBetweenPlaysLastTime = float.MinValue;

		public void OnEnable()
		{
			if (_playOnEnable)
			{
				PlayDelayed();
			}
		}

		[Button(ButtonSizes.Large)] public void PlayDelayed()
		{
			if (_delay.x > 0 || _delay.y > 0)
			{
				Invoke(nameof(Play), _delay.Random());
			}
			else
			{
				Play();
			}
		}

		[Button(ButtonSizes.Large)] public void PlayRandomChange(int chance)
		{
			if (Random.value * 100 < chance)
			{
				PlayDelayed();
			}
		}

		[Button(ButtonSizes.Large)] public void Play()
		{
			if (_delayBetweenPlays > 0)
			{
				if (GetTime() - _delayBetweenPlaysLastTime < _delayBetweenPlays) return;
				_delayBetweenPlaysLastTime = GetTime();
			}

			if (!AS) Reset();
			if (!AS) return;

			if (_usePitch) AS.pitch = _pitch.Random();

			AudioClip clip = AS.clip;

			if (_clips != null && _clips.Length > 0)
			{
				clip = _clips.Random();
			}

			if (_oneShot)
			{
				float volume = 1;
				if (_useVolume) volume = _volume.Random();
				AS.PlayOneShot(clip, volume);
			}
			else
			{
				if (_useVolume) AS.volume = _volume.Random();
				AS.clip = clip;
				AS.Play();
				FadeInOut();
			}

			if (_randomTime) AS.time = Random.Range(0, AS.clip.length);

			if (_lowFilter && _useRandomLowPass)
			{
				_lowFilter.cutoffFrequency = _randomLowPass.Random();
			}
			if (_highFilter && _useRandomHighPass)
			{
				_highFilter.cutoffFrequency = _randomHighPass.Random();
			}
			if (_repeat && gameObject.activeInHierarchy)
			{
				Invoke(nameof(Play), clip.length - AS.time);
			}
		}

		private void FadeInOut()
		{
			if (gameObject.activeInHierarchy)
			{
				StartCoroutine(FadeCor());
			}
		}

		private IEnumerator FadeCor()
		{
			yield return FadeVolumeCor(0, AS.volume, _fadeIn);
			yield return FadeVolumeCor(AS.volume, 0, _fadeOut);
		}

		private IEnumerator FadeVolumeCor(float fromVolume, float volume, float duration)
		{
			if (duration == 0)
			{
				yield break;
			}
			var startTime = GetTime();
			var time = GetTime() - startTime;
			while (time < duration)
			{
				time = GetTime() - startTime;
				var normTime = time / duration;
				AS.volume = Mathf.Lerp(fromVolume, volume, normTime);
				yield return null;
			}
		}

		private float GetTime()
		{
			return _time switch
			{
				eTime.Normal => Time.time,
				eTime.Unscaled => Time.unscaledTime,
				_ => Time.time,
			};
		}

		[Button]
		protected virtual void Reset()
		{
			AS = GetComponent<AudioSource>();
			if (!AS) AS = GetComponentInChildren<AudioSource>(true);
			if (!AS) AS = GetComponentInChildren<AudioSource>(true);
			if (!AS) AS = GetComponentInParent<AudioSource>();
			if (!AS) AS = gameObject.AddComponent<AudioSource>();
		}

		public void FadeOut(bool stop = false)
		{
			if (gameObject.activeInHierarchy)
			{
				StartCoroutine(FadeOutCor());
			}
		}

		private IEnumerator FadeOutCor(bool stop = false)
		{
			yield return FadeVolumeCor(AS.volume, 0, _fadeOut);
			if (stop) AS.Stop();
		}

		public void FadeIn(float fade, float volume)
		{
			if (gameObject.activeInHierarchy)
			{
				StartCoroutine(FadeVolumeCor(AS.volume, volume, fade));
			}
		}
	}

	public static class RandomExtension
	{
		public static float Random(this Vector2 range) => UnityEngine.Random.Range(range.x, range.y);

		public static T Random<T>(this IList<T> list) => list[UnityEngine.Random.Range(0, list.Count)];
	}
}