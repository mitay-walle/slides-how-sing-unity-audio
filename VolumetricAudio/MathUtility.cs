namespace GameAssets.VolumetricAudio
{
	public static class MathUtility
	{
		public static float SafeDivision(float divided, float divisor)
		{
			if (divisor == 0)
			{
				return divided;
			}
			return divided / divisor;
		}

		public static int SafeDivision(int divided, int divisor)
		{
			if (divisor == 0)
			{
				return divided;
			}
			return divided / divisor;
		}

		public static int SafeRemainder(int divided, int divisor)
		{
			if (divisor == 0)
			{
				return divided;
			}
			return divided % divisor;
		}
	}
}