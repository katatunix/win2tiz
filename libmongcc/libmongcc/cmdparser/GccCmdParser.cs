using libcore;
using System;
using System.Collections.Generic;
using System.IO;

namespace libmongcc.cmdparser
{
	public class GccCmdParser : ICmdParser
	{
		private string m_cmd;

		public GccCmdParser(string cmd)
		{
			m_cmd = cmd;
		}

		/// <summary>
		/// May throw exception
		/// </summary>
		public void getInOutFilePath(string workingDir, out List<string> inputFilePaths, out string outputFilePath)
		{
			inputFilePaths = null;
			string inputFilePath = null;
			outputFilePath = null;

			string[] p = StringUtils.splitBySpaceTab(m_cmd);

			for (int i = 0; i < p.Length; i++)
			{
				if (p[i] == "-c" && i + 1 < p.Length) // TODO: input file path may not follow -c, but is is often case
				{
					inputFilePath = p[i + 1];
					i++;
				}
				else if (p[i] == "-o" && i + 1 < p.Length)
				{
					outputFilePath = p[i + 1];
					i++;
				}
			}

			if (inputFilePath == null) return;

			if (outputFilePath == null)
			{
				outputFilePath = Path.ChangeExtension(inputFilePath, "o");
			}

			inputFilePaths = getDependencyFilePaths(workingDir); // already contains inputFilePath (.cpp file), but
			if (inputFilePaths.Count == 0)
			{
				inputFilePaths.Add(inputFilePath);
			}
		}

		public string getInFileName()
		{
			string[] p = StringUtils.splitBySpaceTab(m_cmd);

			for (int i = 0; i < p.Length; i++)
			{
				if (p[i] == "-c" && i + 1 < p.Length) // TODO
				{
					return Path.GetFileName(p[i + 1]);
				}
			}
			return null;
		}

		public string getAlias()
		{
			return getInFileName();
		}

		public string makeNetworkCmd()
		{
			string resultCmd;
			string outFilePath = removeOutputOption(out resultCmd);

			resultCmd = resultCmd.Replace(" -MD", "");

			return resultCmd;
		}

		public string makeServerCmd(string sessionFolderPath, out string outputFileName, string debugPrefixMapStr)
		{
			outputFileName = null;
			string[] p = StringUtils.splitBySpaceTab(m_cmd);

			string resultCmd = "";
			for (int i = 0; i < p.Length; i++)
			{
				if (p[i].StartsWith("-I"))
				{
					resultCmd += "-I" + sessionFolderPath + PathUtils.makeValidPath(p[i].Substring(2)) + " ";
				}
				else if (p[i] == "-c")
				{
					if (i + 1 < p.Length)
					{
						string filePathInThisMachine = sessionFolderPath + PathUtils.makeValidPath(p[i + 1]);

						// -fdebug-prefix-map=e:\x\10.218.9.115_53590_1\E_\=E:\
						if (!string.IsNullOrEmpty(debugPrefixMapStr))
						{
							resultCmd += debugPrefixMapStr + " ";
						}

						resultCmd += "-c " + filePathInThisMachine + " ";
						outputFileName = Path.GetFileNameWithoutExtension(p[i + 1]) + ".o";

						i++;
					}
				}
				else if (p[i] == "-o")
				{
					if (i + 1 < p.Length)
					{
						i++;
					}
				}
				else
				{
					resultCmd += p[i] + " ";
				}
			}
			resultCmd = resultCmd.Trim();

			return resultCmd;
		}

		public string getLocalSpecificWorkingDir()
		{
			return null;
		}

		public string getLocalSpecificOutputFilePath()
		{
			return null;
		}

		public string getLocalSpecificCommand()
		{
			return null;
		}

		public bool needToLockLocal()
		{
			string sourceFileName = getInFileName();
			return sourceFileName != null && (sourceFileName.StartsWith("UB_") || sourceFileName.StartsWith("CU_"));
		}

		//===============================================================================================
		// Private helpers
		//===============================================================================================

		private List<string> getDependencyFilePaths(string workingDir)
		{
			// Generate the .d file
			string genDepCmd = null;
			string depFilePath = null;

			genDepCmd = makeGenDepCmd(out depFilePath); // may throw

			TProcessResult pr = ProcessHelper.executeStatic(genDepCmd, workingDir);
			if (!pr.wasExec || pr.exitCode != 0)
			{
				throw new Exception(pr.outputText);
			}

			List<string> list = new List<string>();

			depFilePath = PathUtils.combine(workingDir, depFilePath);
			if ( File.Exists(depFilePath) )
			{
				using (StreamReader reader = new StreamReader(depFilePath))
				{
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						if (string.IsNullOrEmpty(line)) continue;

						string[] files = StringUtils.splitBySpaceTab(line);
						foreach (string f in files)
						{
							// Make sure f is a absolute path
							if (f[f.Length - 1] == ':' || f == "\\") continue;

							list.Add(Path.GetFullPath(f));
						}
					}
				}
			}

			return list;
		}
		
		private string makeGenDepCmd(out string depFilePath)
		{
			string resultCmd;
			string outFilePath = removeOutputOption(out resultCmd);

			depFilePath = Path.ChangeExtension(outFilePath, "d");
			resultCmd = resultCmd.Replace(" " + s_kMakeDependencies, " -M -E -MF " + depFilePath);

			return resultCmd;
		}

		private string removeOutputOption(out string resultCmd)
		{
			string outFilePath = null;
			string sourceFilePath = null;
			string[] p = StringUtils.splitBySpaceTab(m_cmd);

			resultCmd = "";
			for (int i = 0; i < p.Length; i++)
			{
				if (p[i] == "-o")
				{
					// Remove the -o option as we don't need to compile
					if (i + 1 < p.Length)
					{
						outFilePath = p[i + 1];
					}
					i++;
				}
				else if (p[i] == "-c")
				{
					resultCmd += p[i] + " ";
					if (i + 1 < p.Length)
					{
						sourceFilePath = p[i + 1];
						resultCmd += p[i + 1] + " ";
					}
					i++;
				}
				else
				{
					resultCmd += p[i] + " ";
				}
			}
			resultCmd = resultCmd.Trim();

			if (outFilePath == null && sourceFilePath == null)
			{
				throw new ArgumentException();
			}

			if (outFilePath == null)
			{
				outFilePath = Path.ChangeExtension(sourceFilePath, "o");
			}

			return outFilePath;
		}

		//===============================================================================================
		// Public utils
		//===============================================================================================

		public static string makeDebugPrefixMapString(string path1, string path2)
		{
			// -fdebug-prefix-map=e:\x\10.218.9.115_53590_1\E_\=E:\
			return "-fdebug-prefix-map=" + path1 + "=" + path2;
		}

		public static string makeGccDefinesString(string[] defines)
		{
			string res = "";
			foreach (string item in defines)
			{
				if (res.Length > 0) res += " ";
				res += "-D" + item;
			}
			return res;
		}

		public static string makeGccIncludePathsString(string[] includePaths)
		{
			string res = "";
			foreach (string item in includePaths)
			{
				if (res.Length > 0) res += " ";
				res += "-I" + item;
			}
			return res;
		}

		public static string makeGccLinkPathsString(string[] linkPaths)
		{
			string res = "";
			foreach (string item in linkPaths)
			{
				if (res.Length > 0) res += " ";
				res += "-L" + item;
			}
			return res;
		}

		public static string makeGccLinkLibsString(string[] linkLibs)
		{
			string res = "";
			foreach (string item in linkLibs)
			{
				if (res.Length > 0) res += " ";
				res += "-l" + item;
			}
			return res;
		}

		public static string makeGccItemsString(string[] items)
		{
			string res = "";
			foreach (string item in items)
			{
				if (res.Length > 0) res += " ";
				res += item;
			}
			return res;
		}

		public static readonly string s_kMakeDependencies = "-MD";
	}
}
