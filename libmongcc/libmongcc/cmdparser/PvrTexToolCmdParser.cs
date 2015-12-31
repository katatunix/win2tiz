using libcore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace libmongcc.cmdparser
{
	public class PvrTexToolCmdParser : ICmdParser
	{
		private string m_cmd;

		public PvrTexToolCmdParser(string cmd)
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
				if (p[i].StartsWith("-i"))
				{
					inputFilePath = p[i].Substring(2);
				}
				else if (p[i].StartsWith("-o"))
				{
					outputFilePath = p[i].Substring(2);
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
				if (p[i].StartsWith("-i"))
				{
					return Path.GetFileName( p[i].Substring(2) );
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
				if (p[i].StartsWith("-o"))
				{
					return Path.GetFileName( p[i].Substring(2) );
				}
				if (p[i].StartsWith("-i"))
				{
					inputFileName = Path.GetFileName(p[i].Substring(2));
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
				if (p[i].StartsWith("-i"))
				{
					outputFileName = Path.GetFileName(p[i].Substring(2)) + ".pvr";
					break;
				}
			}

			if (outputFileName == null) return null;

			string resultCmd = "";
			bool hasOut = false;
			for (int i = 0; i < p.Length; i++)
			{
				if (p[i].StartsWith("-i"))
				{
					resultCmd += "-i" + sessionFolderPath + PathUtils.makeValidPath(p[i].Substring(2)) + " ";
				}
				else if (p[i].StartsWith("-o"))
				{
					hasOut = true;
					resultCmd += "-o" + sessionFolderPath + outputFileName + " ";
				}
				else
				{
					resultCmd += p[i] + " ";
				}
			}

			if (!hasOut)
			{
				resultCmd += "-o" + sessionFolderPath + outputFileName;
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
				if (p[i].StartsWith("-o"))
				{
					return p[i].Substring(2) + ".pvr";
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
				if (p[i].StartsWith("-o"))
				{
					newPath = p[i].Substring(2) + ".pvr";
					break;
				}
			}

			if (newPath == null) return null;

			string res = "";
			for (int i = 0; i < p.Length; i++)
			{
				if (p[i].StartsWith("-o"))
				{
					res += "-o" + newPath + " ";
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
