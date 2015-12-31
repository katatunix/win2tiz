using libcore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace libmongcc.cmdparser
{
	public class MojitoCmdParser : ICmdParser
	{
		private string m_cmd;

		public MojitoCmdParser(string cmd)
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

			inputFilePath	= p[p.Length - 2];
			outputFilePath	= p[p.Length - 1];

			inputFilePaths = new List<string>();
			inputFilePaths.Add(inputFilePath);
		}

		public string getInFileName()
		{
			string[] p = StringUtils.splitBySpaceTab(m_cmd);

			if (p.Length < 3) return null;

			return Path.GetFileName( p[p.Length - 2] );
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
			int i = 0;
			for (; i <= p.Length - 3; i++)
			{
				resultCmd += p[i] + " ";
			}

			resultCmd += sessionFolderPath + PathUtils.makeValidPath(p[i++]) + " ";

			outputFileName = Path.GetFileName(p[i]);

			resultCmd += sessionFolderPath + outputFileName;

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
