using System;
using System.Text;

namespace libcore
{
	public class StringUtils
	{
		public static string[] splitByComma(string v)
		{
			return v.Split(s_kSeparate_Comma, StringSplitOptions.RemoveEmptyEntries);
		}

		public static string[] splitBySpaceTab(string v)
		{
			return v.Split(s_kSeparate_SpaceTab, StringSplitOptions.RemoveEmptyEntries);
		}

		public static string[] splitBySeparate(string v)
		{
			if (v == null) return null;
			return v.Split(s_kSeparate_Separate, StringSplitOptions.RemoveEmptyEntries);
		}

		public static string createStringFromBytes(byte[] bytes, int offset, int count)
		{
			return ASCIIEncoding.ASCII.GetString(bytes, offset, count);
		}

		public static void getBytesFromString(string str, byte[] bytes, int offset = 0)
		{
			ASCIIEncoding.ASCII.GetBytes(str, 0, str.Length, bytes, offset);
		}

		public static bool convertString2Bool(string val)
		{
			if (val == null) return false;
			val = val.ToLower().Trim();
			return val == "true" || val == "1";
		}

		public static int convertString2Int(string val)
		{
			try
			{
				return int.Parse(val);
			}
			catch (Exception)
			{
				return 0;
			}
		}

		public static string concat(string[] a, string separate)
		{
			string res = "";
			for (int i = 0; i < a.Length; i++)
			{
				res += a[i];
				if (i < a.Length - 1)
				{
					res += separate;
				}
			}
			return res;
		}

		public static string getFirstToken(string str)
		{
			int spaceIndex = str.IndexOf(" ");
			return spaceIndex == -1 ? str : str.Substring(0, spaceIndex);
		}

		private static readonly char[]		s_kSeparate_Comma		= { ',' };
		private static readonly char[]		s_kSeparate_SpaceTab	= { ' ', '\t' };
		private static readonly string[]	s_kSeparate_Separate	= { ";", " ", "\t", "\r\n" };
	}
}
