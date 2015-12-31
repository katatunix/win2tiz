using System;
using System.IO;
using System.Runtime.InteropServices;

namespace libcore
{
	public class IOUtils
	{
		public static int getFileSize(string path)
		{
			FileInfo f = new FileInfo(path);
			return (int)f.Length;
		}

		public static byte[] readFile_Bytes(string path)
		{
			using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
			{
				int fileSize = (int)reader.BaseStream.Length;
				if (fileSize <= 0) return null;

				byte[] data = new byte[fileSize];
				reader.Read(data, 0, fileSize);
				return data;
			}
		}

		public static int readFile_Bytes(string path, byte[] buffer, int offset, int count)
		{
			using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
			{
				int fileSize = (int)reader.BaseStream.Length;
				if (fileSize <= 0) return 0;

				return reader.Read(buffer, offset, count);
			}
		}

		public static string readFile_String(string path)
		{
			using (StreamReader reader = new StreamReader(path))
			{
				return reader.ReadToEnd();
			}
		}

		public static bool writeFile_Bytes(string path, byte[] buffer, int offset, int count)
		{
			try
			{
				using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
				{
					if (buffer != null && count > 0)
					{
						writer.Write(buffer, offset, count);
					}
				}
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static bool writeFile_String(string path, string content)
		{
			try
			{
				using (StreamWriter writer = new StreamWriter(path))
				{
					if (!string.IsNullOrEmpty(content))
					{
						writer.Write(content);
					}
				}
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool WaitNamedPipe(string name, int timeout);

		public static bool namedPipeExist(string pipeName)
		{
			try
			{
				int timeout = 0;
				string normalizedPath = System.IO.Path.GetFullPath(
					string.Format(@"\\.\pipe\{0}", pipeName));
				bool exists = WaitNamedPipe(normalizedPath, timeout);
				if (!exists)
				{
					int error = Marshal.GetLastWin32Error();
					if (error == 0) // pipe does not exist
						return false;
					else if (error == 2) // win32 error code for file not found
						return false;
					// all other errors indicate other issues
				}
				return true;
			}
			catch (Exception)
			{
				return false; // assume it doesn't exist
			}
		}

		public static void clearTempFolder(String path) {
			if (!Directory.Exists(path)) return;

			try {
				String[] subFolders = Directory.GetDirectories(path);
				foreach (string folder in subFolders) {
					deleteFolder(folder);
				}
			} catch (Exception) {

			}
		}

		public static void deleteFolder(string path) {
			try {
				Directory.Delete(path, true);
			} catch (Exception) {
			}
		}


		public static void clearOldTempFolder(string path, int ms)
		{
			if (!Directory.Exists(path)) return;

			try {
				String[] subFolders = Directory.GetDirectories(path);
				foreach (string folder in subFolders) {
					if ((DateTime.Now - Directory.GetLastWriteTime(folder)).Milliseconds >= ms) {
						deleteFolder(folder);
					}
				}
			} catch (Exception) {

			}
		}
	}
}
