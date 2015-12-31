using libcore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace libmongcc.cmdparser
{
	public class PngOptimizerCmdParser : ICmdParser
	{
		private string m_cmd;

		public PngOptimizerCmdParser(string cmd)
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
				if (p[i].StartsWith("-file") && p[i].Length > 7)
				{
					inputFilePath = p[i].Substring(6, p[i].Length - 7);
					break;
				}
			}

			if (inputFilePath == null) return;

			outputFilePath = inputFilePath;

			inputFilePaths = new List<string>();
			inputFilePaths.Add(inputFilePath);
		}

		public string getInFileName()
		{
			string[] p = StringUtils.splitBySpaceTab(m_cmd);

			for (int i = 0; i < p.Length; i++)
			{
				if (p[i].StartsWith("-file") && p[i].Length > 7)
				{
					return Path.GetFileName( p[i].Substring(6, p[i].Length - 7) );
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
			// TODO
			return m_cmd;
		}

		public string makeServerCmd(string sessionFolderPath, out string outputFileName, string extra)
		{
			outputFileName = null;
			string[] p = StringUtils.splitBySpaceTab(m_cmd);
			
			string resultCmd = "";
			string inFilePath = null;
			for (int i = 0; i < p.Length; i++)
			{
				if (p[i].StartsWith("-file") && p[i].Length > 7 && inFilePath == null)
				{
					inFilePath = p[i].Substring(6, p[i].Length - 7);
					resultCmd += "-file\"" + sessionFolderPath + PathUtils.makeValidPath(inFilePath) + "\" ";
				}
				else
				{
					resultCmd += p[i] + " ";
				}
			}

			if (inFilePath == null) return null;

			resultCmd = resultCmd.Trim();

			outputFileName = Path.GetFileName(inFilePath);

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
			return false;
		}
	}
}
