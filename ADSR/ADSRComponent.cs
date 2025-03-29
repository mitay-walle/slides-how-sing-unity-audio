using System;
using UnityEngine;

namespace Plugins.Audio
{
	public class ADSRComponent : MonoBehaviour
	{
		[SerializeField] private Setup[] _setups;

		[Serializable]
		private class Setup
		{
			public string Key;
			public bool IsPressed;
			public ADSR ADSR;

			public void OnUpdate()
			{
				ADSR.OnUpdate(IsPressed, Time.deltaTime);
			}
		}
		private void Update()
		{
			foreach (Setup setup in _setups)
			{
				setup.OnUpdate();
			}
		}

		public void SetPressedTrue() => SetPressed(_setups[0].Key, true);
		public void SetPressedFalse() => SetPressed(_setups[0].Key, false);
		public void SetPressedTrue(string key) => SetPressed(key, true);
		public void SetPressedFalse(string key) => SetPressed(key, false);
		public void SetPressed(string key, bool isPressed)
		{
			foreach (Setup setup in _setups)
			{
				if (setup.Key == key)
				{
					setup.IsPressed = isPressed;
				}
			}
		}
	}
}