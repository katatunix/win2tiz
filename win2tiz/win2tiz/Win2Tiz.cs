using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Threading;

using libcore;
using win2tiz.visualc;
using libmongcc.cmdparser;

namespace win2tiz
{
	class Win2Tiz
	{
		/// <summary>
		/// This method should be called ONCE after this object is created,
		/// because we cannot signal to stop it again (see the signalToStop() method).
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public int main(string[] args)
		{
			printAbout();

			bool result = true;

			if (args.Length < 1)
			{
				printUsage();
			}
			else if (args[0] == "--cmd")
			{
				result = buildCommandFile(args);
			}
			else
			{
				result = buildConfigFile(args);
			}

			#region temp
			//Console.WriteLine();
			//Console.WriteLine("Press any key to exit...");
			//Console.ReadLine();
			#endregion

			return result ? 0 : 1;
		}

		/// <summary>
		/// Signal to stop the main() method.
		/// Must be called ONCE because the program is killed after the first call,
		/// so the second call will never be excecuted or it will be interrupted in the middle.
		/// </summary>
		public void signalToStop()
		{
			if (m_alreadyCallSignalToStop) return;
			m_alreadyCallSignalToStop = true;

			CConsole.writeWarning("Force stop...\n");

			lock (m_lock)
			{
				m_signalToStop = true;
				if (m_builder != null)
				{
					m_builder.signalToStop();
				}
			}
		}

		//===========================================================================================
		private bool buildConfigFile(string[] args)
		{
			#region Get input
			string inputFile = null;
			string typeOfBuild = "release";
			string projectToBuild = "all";
			string gccConfig = "";
			bool isVerbose = false;
			string outputFolderPath = "";
			
			int jobs = s_kDefaultJobs;

			for (int i = 0; i < args.Length; i++)
			{
				string arg = args[i];
				if (arg == "-i")
				{
					if (i + 1 >= args.Length) break;
					inputFile = args[i + 1];
					i++;
				}
				else if (arg == "-t")
				{
					if (i + 1 >= args.Length) break;
					if (args[i + 1] != "release" && args[i + 1] != "debug") break;
					typeOfBuild = args[i + 1];
					i++;
				}
				else if (arg == "-p")
				{
					if (i + 1 >= args.Length) break;
					projectToBuild = args[i + 1];
					i++;
				}
				else if (arg == "-g")
				{
					if (i + 1 >= args.Length) break;
					gccConfig = args[i + 1];
					i++;
				}
				else if (arg == "-v")
				{
					isVerbose = true;
				}
				else if (arg == "-j")
				{
					if (i + 1 >= args.Length) break;
					jobs = StringUtils.convertString2Int(args[i + 1]);
				}
				else if (arg == "-o")
				{
					if (i + 1 >= args.Length) break;
					outputFolderPath = args[i + 1];
					i++;
				}
			}

			if (jobs <= 0 || jobs > s_kDefaultJobs)
			{
				jobs = s_kDefaultJobs;
			}

			if (inputFile == null)
			{
				CConsole.writeError("error: The value for the option '-i' must be specified.\n");
				printUsage();
				return false;
			}
			#endregion

			return make(
				Path.GetFullPath(inputFile),
				typeOfBuild == "release",
				gccConfig,
				jobs,
				projectToBuild,
				isVerbose,
				outputFolderPath
			);
		}

		private bool buildCommandFile(string[] args)
		{
			DateTime totalTimeBegin = DateTime.Now;

			string file = null;
			int jobs = s_kDefaultJobs;
			bool verbose = false;

			for (int i = 1; i < args.Length; i++)
			{
				if (args[i] == "-file")
				{
					if (i + 1 >= args.Length) break;
					file = args[i + 1];
					i++;
				}
				else if (args[i] == "-jobs")
				{
					if (i + 1 >= args.Length) break;
					jobs = StringUtils.convertString2Int(args[i + 1]);
					i++;
				}
				else if (args[i] == "-verbose")
				{
					verbose = true;
				}
			}
			if (jobs <= 0 || jobs > s_kDefaultJobs)
			{
				jobs = s_kDefaultJobs;
			}

			if (file == null)
			{
				CConsole.writeError("error: The value for the option '-file' must be specified.\n");
				printUsage();
				return false;
			}

			file = Path.GetFullPath(file);

			if (!File.Exists(file))
			{
				CConsole.writeError("error: Could not found the file " + file + ".\n");
				return false;
			}

			lock (m_lock)
			{
				m_builder = new Builder();
			}

			string workingDir = Path.GetDirectoryName(file);

			using (StreamReader reader = new StreamReader(file))
			{
				string cmd;
				while ((cmd = reader.ReadLine()) != null)
				{
					cmd = cmd.Trim();
					if (cmd.Length == 0 || cmd.StartsWith("//"))
					{
						continue;
					}

					string project = "Project";

					if (cmd[0] == '*')
					{
						int index = cmd.IndexOf(' ');
						if (index == -1)
						{
							CConsole.writeError("error: Invalid compile command " + cmd + ".\n");
							return false;
						}
						project = cmd.Substring(1, index - 1);
						cmd = cmd.Substring(index + 1).Trim();
						if (cmd.Length == 0)
						{
							continue;
						}
					}

					// Parse the cmd
					ICmdParser parser = CmdParserFactory.createCmdParser(cmd);
					string alias = parser.getAlias();

					if (alias == null)
					{
						CConsole.writeError("error: Invalid command " + cmd + "\n");
						return false;
					}

					TCommand tCommand = new TCommand(cmd, cmd, workingDir, alias, ECommandType.eCompile, project);
					m_builder.addCommand(tCommand);
				}
			}

			bool result = m_builder.build(verbose, jobs);

			m_builder = null;

			CConsole.writeTime("Total time: " + (DateTime.Now - totalTimeBegin).ToString() + "\n\n");

			return result;
		}

		private bool make(
			string	configFilePath, // sure this is full path
			bool	isReleaseMode,
			string	gccConfigName,
			int		jobs,
			string	projectNameToBuild,
			bool	isVerbose,
            string	outputFolderPath)
		{
			CConsole.writeInfoLine("Config file: " + configFilePath);
			CConsole.writeInfoLine("Type of build: " + (isReleaseMode ? "release" : "debug"));
			CConsole.writeInfoLine("GCC config name: " + (string.IsNullOrEmpty(gccConfigName) ? "<default>" : gccConfigName));
			CConsole.writeInfoLine("Jobs: " + jobs);
			CConsole.writeInfoLine("Project to build: " + (string.IsNullOrEmpty(projectNameToBuild) ? "all" : projectNameToBuild));
			CConsole.writeInfoLine("Is verbose: " + (isVerbose ? "yes" : "no"));
			CConsole.writeInfoLine("Out folder: " + (string.IsNullOrEmpty(outputFolderPath) ? "<default>" : outputFolderPath));

			CConsole.writeLine();

			DateTime totalTimeBegin = DateTime.Now;
			bool result = true;

			XmlDocument win2TizXmlDoc;
			#region Load config file
			{
				CConsole.writeInfoLine("Load config file: " + configFilePath);
				win2TizXmlDoc = new XmlDocument();
				try
				{
					win2TizXmlDoc.Load(configFilePath);
				}
				catch (Exception)
				{
					CConsole.writeError("error: could not load config file " + configFilePath + "\n");
					result = false;
					goto my_end;
				}
			}
			#endregion

			string slnWorkingDir; // sure this is full path
			ASolution vcSolution;
			#region Load Visual solution file
			{
				slnWorkingDir = Path.GetDirectoryName(configFilePath);
				XmlNodeList nodes = win2TizXmlDoc.GetElementsByTagName(KXml.s_kSolutionTag);
				if (nodes == null || nodes.Count <= 0)
				{
					CConsole.writeError("error: tag " + KXml.s_kSolutionTag + " is not found\n");
					result = false;
					goto my_end;
				}

				XmlAttribute attr = nodes[0].Attributes[KXml.s_kSolutionPathAttr];
				if (attr == null)
				{
					CConsole.writeError("error: attribute " + KXml.s_kSolutionPathAttr + " of tag " + KXml.s_kSolutionTag + " is not found\n");
					result = false;
					goto my_end;
				}

				vcSolution = CFactory.createSolution();

				string vcSlnFilePath = PathUtils.combine(slnWorkingDir, attr.Value);
				CConsole.writeInfoLine("Load Visual solution file: " + vcSlnFilePath);
				if (!vcSolution.load(vcSlnFilePath))
				{
					CConsole.writeError("error: could not load Visual solution file " + vcSlnFilePath + "\n");
					result = false;
					goto my_end;
				}
			}
			#endregion

			GccConfig gccConfig;
			#region Load GccConfig node
			{
				XmlNodeList nodes = win2TizXmlDoc.GetElementsByTagName(KXml.s_kCommonGccConfigTag);
				if (nodes == null || nodes.Count <= 0)
				{
					CConsole.writeError("error: tag " + KXml.s_kCommonGccConfigTag + " is not found\n");
					result = false;
					goto my_end;
				}

				XmlNode nodeGccConfig = null;
				foreach (XmlNode node in nodes[0].ChildNodes)
				{
					if (node.Name == KXml.s_kGccConfigTag && node.Attributes[KXml.s_kNameAttr] != null &&
						node.Attributes[KXml.s_kNameAttr].Value == gccConfigName)
					{
						nodeGccConfig = node;
						break;
					}
				}

				if (nodeGccConfig == null)
				{
					CConsole.writeWarning("warning: this is an old-style config file, the <CommonGccConfig> node will be used as <GccConfig> node\n");
					nodeGccConfig = nodes[0];
					gccConfigName = null;
				}

				gccConfig = new GccConfig(gccConfigName);
				if (!gccConfig.load(nodeGccConfig, isReleaseMode, slnWorkingDir))
				{
					// TODO: when does it return false?
				}
			}
			CConsole.writeLine();
			#endregion

			string mainProjectName = gccConfig.get_MAIN_PROJECT(); // Can be null
			// Build only main project? It means buill all
			if (projectNameToBuild == mainProjectName) projectNameToBuild = null;
			bool onlyBuildOneProject =
				projectNameToBuild != null &&
				projectNameToBuild != "" &&
				projectNameToBuild != "all";

			List<XmlNode> projectNodesToBuild = new List<XmlNode>();

			#region Make projectNodesToBuild
			{
				XmlNodeList projectNodes = win2TizXmlDoc.GetElementsByTagName(KXml.s_kProjectTag);
				if (onlyBuildOneProject)
				{
					foreach (XmlNode projectNode in projectNodes)
					{
						XmlAttribute nameAttr = projectNode.Attributes[KXml.s_kNameAttr];
						if (nameAttr == null) continue;
						if (nameAttr.Value == projectNameToBuild)
						{
							projectNodesToBuild.Add(projectNode);
							break;
						}
					}
					if (projectNodesToBuild.Count == 0)
					{
						CConsole.writeError("error: project " + projectNameToBuild + " is not found in the config file\n");
						result = false;
						goto my_end;
					}
				}
				else
				{
					XmlNode mainProjectNode = null;
					foreach (XmlNode projectNode in projectNodes)
					{
						XmlAttribute nameAttr = projectNode.Attributes[KXml.s_kNameAttr];
						if (nameAttr == null) continue;
						if (nameAttr.Value == mainProjectName)
						{
							if (mainProjectNode == null) mainProjectNode = projectNode;
						}
						else
						{
							projectNodesToBuild.Add(projectNode);
						}
					}
					// mainProjectNode can be null
					projectNodesToBuild.Add(mainProjectNode);
				}
			}
			#endregion

			//
			lock (m_lock)
			{
				m_builder = new Builder();
			}

			int projectCount = projectNodesToBuild.Count; // Sure projectCount >= 1
			List<TDepProjectInfo> depProjectInfos = new List<TDepProjectInfo>();
			bool isSomethingNewFromDepProjects = false;

			#region Loop through projectNodesToBuild to get list of commands to execute
			for (int i = 0; i < projectCount; i++)
			{
				XmlNode projectNode = projectNodesToBuild[i];
				if (projectNode == null) continue;

				string projectName = projectNode.Attributes[KXml.s_kNameAttr].Value;
				string projectNameSpec = projectName;

				CConsole.writeProject(projectName);

				AProject vcProject = vcSolution.getProject(projectName);
				if (vcProject == null)
				{
					if (onlyBuildOneProject)
					{
						CConsole.writeError("error: project " + projectName + " is not found in the Visual solution, it will be ignored\n\n");
						result = false;
						goto my_end;
					}

					CConsole.writeWarning("warning: project " + projectName + " is not found in the Visual solution, it will be ignored\n\n");
					continue;
				}

				#region Process S2G file
				{
					XmlAttribute useS2GFileAttr = projectNode.Attributes[KXml.s_kUseS2GFileTag];
					if (useS2GFileAttr != null)
					{
						XmlDocument s2gDoc = new XmlDocument();
						try
						{
							s2gDoc.Load(PathUtils.combine(slnWorkingDir, useS2GFileAttr.Value));
							XmlNodeList nodes = s2gDoc.GetElementsByTagName(KXml.s_kProjectTag);
							if (nodes == null || nodes.Count <= 0)
							{
								CConsole.writeWarning("warning: could not found tag " + KXml.s_kProjectTag + " in the S2G file " +
										useS2GFileAttr.Value + " for project " + projectName + ", it will not be used\n");
							}
							else
							{
								projectNode = nodes[0]; //===
							}
						}
						catch (Exception)
						{
							CConsole.writeWarning("warning: could not found S2G file " +
								useS2GFileAttr.Value + " for project " + projectName + ", it will not be used\n");
						}
					}
				}
				#endregion

				bool isMainProject = !onlyBuildOneProject && i == projectCount - 1;
				EProjectType type = EProjectType.eStaticLib;
				if (isMainProject)
				{
					XmlAttribute tot = projectNode.Attributes["TargetOutType"];
					type = (tot != null && tot.Value == "exe") ? EProjectType.eExecutable : EProjectType.eDynamicLib;
				}

				List<TCommand> projectCommands = CProject.load(
					gccConfig, projectNode, vcProject, isReleaseMode,
					type, depProjectInfos, isSomethingNewFromDepProjects,
					slnWorkingDir, outputFolderPath,
					out projectNameSpec);

				if (projectCommands == null)
				{
					CConsole.writeError("error: something went wrong with project " + projectName + ", please double check\n\n");
					
					result = false;
					goto my_end;
				}

				CConsole.writeLine();

				if (isSignalToStop())
				{
					result = false;
					goto my_end;
				}

				//----------------------------------------------------------------------------------------
				m_builder.addCommands(projectCommands);
				//----------------------------------------------------------------------------------------

				if (!isMainProject)
				{
					depProjectInfos.Add(new TDepProjectInfo(projectName, projectNameSpec));
					if (projectCommands.Count > 0)
					{
						isSomethingNewFromDepProjects = true;
					}
				}
			}
			#endregion

			result = m_builder.build(isVerbose, jobs);
			
			my_end:

			m_builder = null;

			CConsole.writeTime("Total time: " + (DateTime.Now - totalTimeBegin).ToString() + "\n");

			return result;
		}

		private bool isSignalToStop()
		{
			lock (m_lock)
			{
				return m_signalToStop;
			}
		}

		//==============================================================================================
		
		private static void printUsage()
		{
			CConsole.setColor(EConsoleColor.eGreen);

			CConsole.writeLine("Usage for compile from XML config file:");
			CConsole.writeLine("win2tiz.exe -i <str> [-t <str>] [-p <str>] [-g <str>] [-v] [-j <num>] [-o <str>]");
			CConsole.writeLine("  -i      input path/filename to the config file (e.g. win2tiz.xml), mandatory");
			CConsole.writeLine("  -t      type of build <release|debug>, default: release");
			CConsole.writeLine("  -p      <project name> to build or <all>, default: all");
			CConsole.writeLine("  -g      the <GccConfig> choosed from the config file, default: empty");
			CConsole.writeLine("  -v      verbose, print a lot of info, default: false");
			CConsole.writeLine("  -j      jobs or how many simultaneous processes, default: 4");
			CConsole.writeLine("  -o      output folder path, relative from config file, default: current");
			CConsole.writeLine();

			CConsole.writeLine("Usage for compile from command text file:");
			CConsole.writeLine("win2tiz.exe --cmd -file <str> [-jobs <num>] [-verbose]");
			CConsole.writeLine("  -file      input path/filename to the text command file, mandatory");
			CConsole.writeLine("  -jobs      jobs or how many simultaneous processes, default: 4");
			CConsole.writeLine("  -verbose   verbose, print a lot of info, default: false");
		}

		private static void printAbout()
		{
			CConsole.setColor(EConsoleColor.eWhite);
			CConsole.writeLine();
			CConsole.writeLine("win2tiz (c) katatunix@gmail.com, Summer 2014 - FIFA World Cup Brazil");
			CConsole.writeLine("(Since Spring 2013)");
			CConsole.writeLine();
		}

		//==============================================================================================

		private static readonly int s_kDefaultJobs = 8;

		//==============================================================================================

		private Builder m_builder = null;

		private bool m_signalToStop = false;
		private Object m_lock = new Object();

		private bool m_alreadyCallSignalToStop = false;
	}
}
