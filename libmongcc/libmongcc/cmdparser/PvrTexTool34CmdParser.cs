using libcore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace libmongcc.cmdparser
{
	public class PvrTexTool34CmdParser : ICmdParser
	{
		private string m_cmd;

		public PvrTexTool34CmdParser(string cmd)
		{
			m_cmd = cmd;
		}

		public void getInOutFilePath(string workingDir, out List<string> inputFilePaths, out string outputFilePath)
		{
			inputFilePaths = null;
			string inputFilePath = null;
			outputFilePath = null;
			string[] p = StringUtils.splitBySpaceTab(m_cmd);

			for (int i = 0; i < p.Length; i++)
			{
				if (p[i] == "-i")
				{
					if (i + 1 < p.Length)
						inputFilePath = p[i + 1];
					i++;
				}
				else if (p[i] == "-o")
				{
					if (i + 1 < p.Length)
						outputFilePath = p[i + 1];
					i++;
				}
				if (inputFilePath != null && outputFilePath != null) break;
			}

			if (inputFilePath != null && outputFilePath == null)
			{
				outputFilePath = Path.ChangeExtension(inputFilePath, "pvr");
			}

			inputFilePaths = new List<string>();
			inputFilePaths.Add(inputFilePath);
		}

		public string getInFileName()
		{
			string[] p = StringUtils.splitBySpaceTab(m_cmd);

			for (int i = 0; i < p.Length; i++)
			{
				if (p[i] == "-i" && i + 1 < p.Length)
				{
					return Path.GetFileName(p[i + 1]);
				}
			}

			return null;
		}

		public string getAlias()
		{
			string[] p = StringUtils.splitBySpaceTab(m_cmd);
			string inputFileName = null;

			for (int i = 0; i < p.Length; i++)
			{
				if (p[i] == "-o" && i + 1 < p.Length)
				{
					return Path.GetFileName(p[i + 1]);
				}
				if (p[i] == "-i" && i + 1 < p.Length)
				{
					inputFileName = Path.GetFileName(p[i + 1]);
				}
			}

			if (inputFileName != null)
			{
				return inputFileName;
			}

			return null;
		}

		public string makeNetworkCmd()
		{
			return m_cmd;
		}

		public string makeServerCmd(string sessionFolderPath, out string outputFileName, string extra)
		{
			string[] p = StringUtils.splitBySpaceTab(m_cmd);
			outputFileName = null;

			for (int i = 0; i < p.Length; i++)
			{
				if (p[i] == "-i" && i + 1 < p.Length)
				{
					outputFileName = Path.GetFileName(p[i + 1]) + ".pvr";
					break;
				}
			}

			if (outputFileName == null) return null;

			string resultCmd = "";
			bool hasOut = false;
			for (int i = 0; i < p.Length; i++)
			{
				if (p[i] == "-i" && i + 1 < p.Length)
				{
					resultCmd += "-i " + sessionFolderPath + PathUtils.makeValidPath(p[i + 1]) + " ";
					i++;
				}
				else if (p[i] == "-o" && i + 1 < p.Length)
				{
					hasOut = true;
					resultCmd += "-o " + sessionFolderPath + outputFileName + " ";
					i++;
				}
				else
				{
					resultCmd += p[i] + " ";
				}
			}

			if (!hasOut)
			{
				resultCmd += "-o " + sessionFolderPath + outputFileName;
			}
			else
			{
				resultCmd = resultCmd.Trim();
			}

			return resultCmd;
		}

		public string getLocalSpecificWorkingDir()
		{
			return null;
		}

		public string getLocalSpecificOutputFilePath()
		{
			string[] p = StringUtils.splitBySpaceTab(m_cmd);

			for (int i = 0; i < p.Length; i++)
			{
				if (p[i] == "-o" && i + 1 < p.Length)
				{
					return p[i + 1] + ".pvr";
				}
			}

			return null;
		}

		public string getLocalSpecificCommand()
		{
			string[] p = StringUtils.splitBySpaceTab(m_cmd);

			string newPath = null;
			for (int i = 0; i < p.Length; i++)
			{
				if (p[i] == "-o" && i + 1 < p.Length)
				{
					newPath = p[i + 1] + ".pvr";
					break;
				}
			}

			if (newPath == null) return null;

			string res = "";
			for (int i = 0; i < p.Length; i++)
			{
				if (p[i] == "-o" && i + 1 < p.Length)
				{
					res += "-o " + newPath + " ";
					i++;
				}
				else
				{
					res += p[i] + " ";
				}
			}

			res = res.Trim();
			return res;
		}

		public bool needToLockLocal()
		{
			return false;
		}
	}
}
