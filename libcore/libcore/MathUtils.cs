using System;

namespace libcore
{
	public class MathUtils
	{
		public static void shakeOrder(int[] orders)
		{
			if (orders == null || orders.Length <= 0) return;

			int len = orders.Length;
			bool[] cx = new bool[len];
			for (int i = 0; i < len; i++) cx[i] = true;

			int remain = len;
			for (int i = 0; i < len; i++)
			{
				int rand = s_random.Next(remain);
				for (int j = 0; j < len; j++)
				{
					if (cx[j])
					{
						rand--;
						if (rand < 0)
						{
							orders[i] = j;
							cx[j] = false;
							break;
						}
					}
				}
				remain--;
			}
		}

		private static Random s_random = new Random();
	}
}
