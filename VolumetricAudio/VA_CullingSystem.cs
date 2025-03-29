using UnityEngine;

namespace GameAssets.VolumetricAudio
{
	[System.Serializable]
	public class VA_CullingSystem
	{
		private const int SHAPES_PER_FRAME = 3;

		[SerializeField] private VA_CullingShape[] _shapes = default;
		private int _current;

		public void Init()
		{
			if (_shapes != null)
			{
				foreach (var shape in _shapes)
					shape.Init();
			}
		}

		public void OnFixedUpdate(Transform target)
		{
			if (_shapes == null) return;

			int count = _shapes.Length;
			int iterations = Mathf.Min(SHAPES_PER_FRAME, count);
			var position = target.position;
			for (int i = 0; i < iterations; i++)
			{
				if (_current >= count)
					_current = 0;

				_shapes[_current].Check(position);
				_current += 1;
			}
		}

#if UNITY_EDITOR
		public void CollectTargets()
		{
			_shapes = Object.FindObjectsOfType<VA_CullingShape>(true);
		}
#endif
	}
}