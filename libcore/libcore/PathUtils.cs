using System;
using System.Collections.Generic;
using System.IO;

namespace libcore
{
	public class PathUtils
	{
		/// <summary>
		/// Make sure pathFull is an absolute path
		/// </summary>
		/// <param name="pathFull"></param>
		/// <returns></returns>
		public static string normalizePath(string pathFull)
		{
			pathFull = Path.GetFullPath(pathFull);
			while (pathFull.EndsWith("\\") || pathFull.EndsWith("/"))
			{
				pathFull = pathFull.Substring(0, pathFull.Length - 1);
			}
			return pathFull.ToLower();
		}

		public static string combine(string p1, string p2)
		{
			return Path.GetFullPath( Path.Combine(p1, p2) );
		}

		public static string combineSimple(string p1, string p2)
		{
			return Path.Combine(p1, p2);
		}

		public static bool isSamePath(string path1, string path2)
		{
			return normalizePath(path1) == normalizePath(path2);
		}

		public static string makeValidPath(string path)
		{
			return path.Replace(':', '_');
		}

		public static bool isCppExt(string ext)
		{
			return ext == ".cpp" ||
					ext == ".cc" ||
					ext == ".cxx" ||
					ext == ".C";
		}

		public static bool isAssemblyExt(string ext)
		{
			return ext == ".s" || ext == ".S";
		}

		public static string[] getFilesWithWildcard(string path)
		{
			int i = path.LastIndexOfAny(s_kSlashes);
			if (i == -1) return new string[] { path };

			string folderPath = path.Substring(0, i);
			string pattern = path.Substring(i + 1);

			return Directory.GetFiles(folderPath, pattern, SearchOption.TopDirectoryOnly);
		}

		public static bool checkPatternFile(string sourcePath, string pattern, string vcDir)
		{
			// *.c *.hpp
			if (pattern.StartsWith("*."))
			{
				string ext = pattern.Substring(1); // .c
				return ext == Path.GetExtension(sourcePath);
			}

			if (pattern.StartsWith(".") || Path.IsPathRooted(pattern))
			{
				string patternPath = combine(vcDir, pattern);

				if (string.IsNullOrEmpty(Path.GetExtension(patternPath)))
				{
					string sourcePathWithoutExt = Path.GetDirectoryName(sourcePath) + "\\" +
						Path.GetFileNameWithoutExtension(sourcePath);
					return isSamePath(sourcePathWithoutExt, patternPath);
				}
				return isSamePath(sourcePath, patternPath);
			}

			if (string.IsNullOrEmpty(Path.GetExtension(pattern)))
			{
				return pattern.ToLower() == Path.GetFileNameWithoutExtension(sourcePath).ToLower();
			}

			return pattern.ToLower() == Path.GetFileName(sourcePath).ToLower();
		}

		public static bool checkPathExistInList(string path, List<string> list)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (isSamePath(list[i], path))
				{
					return true;
				}
			}
			return false;
		}

		public static string getRelativePath(string filespec, string folder)
		{
			Uri pathUri = new Uri(filespec);
			// Folders must end in a slash
			if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				folder += Path.DirectorySeparatorChar;
			}
			Uri folderUri = new Uri(folder);
			return Uri.UnescapeDataString(
				folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar)
			);
		}

		private static readonly char[] s_kSlashes = { '\\', '/' };
	}
}
