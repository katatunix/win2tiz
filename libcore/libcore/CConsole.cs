using System;

namespace libcore
{
	public class CConsole
	{
		public static void write(string txt)
		{
			lock (s_lock)
			{
				Console.BackgroundColor = ConsoleColor.Black;
				makeColor();
				Console.Write(txt);
				Console.ResetColor();
			}
		}

		public static void writeLine(string txt)
		{
			lock (s_lock)
			{
				Console.BackgroundColor = ConsoleColor.Black;
				makeColor();
				Console.WriteLine(txt);
				Console.ResetColor();
			}
		}

		public static void writeLine()
		{
			lock (s_lock)
			{
				Console.WriteLine();
			}
		}

		public static void writeError(string txt)
		{
			lock (s_lock)
			{
				Console.BackgroundColor = ConsoleColor.Black;
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Write(txt);
				Console.ResetColor();
			}
		}

		public static void writeWarning(string txt)
		{
			lock (s_lock)
			{
				Console.BackgroundColor = ConsoleColor.Black;
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.Write(txt);
				Console.ResetColor();
			}
		}

		public static void writeInfo(string txt)
		{
			lock (s_lock)
			{
				Console.BackgroundColor = ConsoleColor.Black;
				Console.ForegroundColor = s_kInfoColor;
				Console.Write(txt);
				Console.ResetColor();
			}
		}

		public static void writeInfoLine(string txt)
		{
			lock (s_lock)
			{
				Console.BackgroundColor = ConsoleColor.Black;
				Console.ForegroundColor = s_kInfoColor;
				Console.WriteLine(txt);
				Console.ResetColor();
			}
		}

		public static void writeVerbose(string txt)
		{
			lock (s_lock)
			{
				Console.BackgroundColor = ConsoleColor.Black;
				Console.ForegroundColor = ConsoleColor.White;
				Console.Write(txt);
				Console.ResetColor();
			}
		}

		public static void writeSuccess(string txt)
		{
			lock (s_lock)
			{
				Console.BackgroundColor = ConsoleColor.Black;
				Console.ForegroundColor = ConsoleColor.Blue;
				Console.Write(txt);
				Console.ResetColor();
			}
		}

		public static void writeTime(string txt)
		{
			lock (s_lock)
			{
				Console.BackgroundColor = ConsoleColor.Black;
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.Write(txt);
				Console.ResetColor();
			}
		}

		public static void writeSpentTime(string txt)
		{
			lock (s_lock)
			{
				Console.BackgroundColor = ConsoleColor.Black;
				Console.ForegroundColor = ConsoleColor.White;
				Console.Write(txt);
				Console.ResetColor();
			}
		}

		public static void writeProject(string name)
		{
			lock (s_lock)
			{
				Console.BackgroundColor = ConsoleColor.Black;
				Console.ForegroundColor = ConsoleColor.Magenta;

				Console.Write("Load project: " + name + "\n");

				Console.ResetColor();
			}
		}

		public static void writeMongcc(string txt)
		{
			lock (s_lock)
			{
				Console.BackgroundColor = ConsoleColor.Black;
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.Write(txt);
				Console.ResetColor();
			}
		}

		public static void writeOutputText(string txt)
		{
			lock (s_lock)
			{
				Console.BackgroundColor = ConsoleColor.Black;

				string[] keys = {
										"error:",
										"warning:",
										"undefined reference to",
										"multiple definition of",
										"note:"
							};
				ConsoleColor[] colors = {
										ConsoleColor.Red,
										ConsoleColor.Yellow,
										ConsoleColor.Red,
										ConsoleColor.Red,
										ConsoleColor.Yellow
									};
				int count = keys.Length;
				int[] a = new int[count];

				while (true)
				{
					for (int i = 0; i < count; i++)
					{
						a[i] = txt.IndexOf(keys[i], StringComparison.CurrentCultureIgnoreCase);
						if (a[i] == -1) a[i] = int.MaxValue;
					}

					int minIndex = 0;
					for (int i = 1; i < count; i++)
					{
						if (a[i] < a[minIndex]) minIndex = i;
					}

					if (a[minIndex] == int.MaxValue)
					{
						Console.ForegroundColor = ConsoleColor.Gray;
						Console.Write(txt);
						break;
					}
					else
					{
						Console.ForegroundColor = ConsoleColor.Gray;
						Console.Write(txt.Substring(0, a[minIndex]));

						Console.ForegroundColor = colors[minIndex];
						Console.Write(keys[minIndex]);
						txt = txt.Substring(a[minIndex] + keys[minIndex].Length);
					}
				}

				Console.ResetColor();
			}
		}

		public static void setColor(EConsoleColor color)
		{
			m_curColor = color;
		}

		//========================================================================================================

		private static void makeColor()
		{
			switch (m_curColor)
			{
				case EConsoleColor.eWhite:
					Console.ForegroundColor = ConsoleColor.White;
					break;
				case EConsoleColor.eRed:
					Console.ForegroundColor = ConsoleColor.Red;
					break;
				case EConsoleColor.eGreen:
					Console.ForegroundColor = ConsoleColor.Green;
					break;
			}
		}

		//========================================================================================================

		private static EConsoleColor m_curColor = EConsoleColor.eWhite;

		private static Object s_lock = new Object();
		private static readonly ConsoleColor s_kInfoColor = ConsoleColor.Green;
	}
}
