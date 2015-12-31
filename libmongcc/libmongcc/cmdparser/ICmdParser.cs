using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace libmongcc.cmdparser
{
	public interface ICmdParser
	{
		/// <summary>
		/// May throw exception
		/// </summary>
		/// <param name="workingDir"></param>
		/// <param name="inputFilePaths"></param>
		/// <param name="outputFilePath"></param>
		void getInOutFilePath(string workingDir, out List<string> inputFilePaths, out string outputFilePath);

		string getInFileName();
		string getAlias();
		string makeNetworkCmd();
		string makeServerCmd(string sessionFolderPath, out string outputFileName, string extra);

		string getLocalSpecificWorkingDir();
		string getLocalSpecificOutputFilePath();
		string getLocalSpecificCommand();

		bool needToLockLocal();
	}
}
