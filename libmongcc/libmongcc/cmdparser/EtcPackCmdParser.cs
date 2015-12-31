using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using libcore;

namespace libmongcc.cmdparser
{
	public class EtcPackCmdParser : ICmdParser
	{
		private string m_cmd;

		public EtcPackCmdParser(string cmd)
		{
			m_cmd = cmd;
		}

		public void getInOutFilePath(string workingDir, out List<string> inputFilePaths, out string outputFilePath)
		{
			inputFilePaths = null;
			string inputFilePath = null;
			outputFilePath = null;
			string[] p = StringUtils.splitBySpaceTab(m_cmd);

			if (p.Length < 3) return;
			inputFilePath = p[1];

			string extension = ".pkm";
			for (int i = 0; i < p.Length; i++)
			{
				if (p[i] == "-ktx")
				{
					extension = ".ktx";
					break;
				}
			}

			outputFilePath = p[2] + "//" + Path.GetFileNameWithoutExtension(inputFilePath) + extension;

			inputFilePaths = new List<string>();
			inputFilePaths.Add(inputFilePath);
		}

		public string getInFileName()
		{
			string[] p = StringUtils.splitBySpaceTab(m_cmd);

			if (p.Length < 3) return null;

			return Path.GetFileName( p[1] );
		}

		public string getAlias()
		{
			return getInFileName();
		}

		public string makeNetworkCmd()
		{
			return m_cmd;
		}

		public string makeServerCmd(string sessionFolderPath, out string outputFileName, string extra)
		{
			outputFileName = null;
			string[] p = StringUtils.splitBySpaceTab(m_cmd);
			if (p.Length < 3) return null;
			
			string resultCmd = "";
			string extension = ".pkm";
			for (int i = 0; i < p.Length; i++)
			{
				if (i == 1)
				{
					resultCmd += sessionFolderPath + PathUtils.makeValidPath(p[i]) + " ";
				}
				else if (i == 2)
				{
					// output folder always is the session folder without slash ending
					string sp = sessionFolderPath;
					if (sp.EndsWith("/") || sp.EndsWith("\\")) sp = sp.Substring(0, sp.Length - 1);
					resultCmd += sp + " ";
				}
				else
				{
					resultCmd += p[i] + " ";
				}

				if (p[i] == "-ktx")
				{
					extension = ".ktx";
				}
			}
			resultCmd = resultCmd.Trim();

			outputFileName = Path.GetFileNameWithoutExtension(p[1]) + extension;

			return resultCmd;
		}

		public string getLocalSpecificWorkingDir()
		{
			string toolPath = StringUtils.getFirstToken(m_cmd);
			return Path.GetDirectoryName(toolPath);
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
			return true;
		}
	}
}
