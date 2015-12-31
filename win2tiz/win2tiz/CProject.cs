using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

using libcore;
using win2tiz.visualc;
using libmongcc.cmdparser;

namespace win2tiz
{
	class CProject
	{
		public static List<TCommand> load(
			GccConfig gccConfig, XmlNode projectNode, AProject vcProject,
			bool isReleaseMode, EProjectType type, List<TDepProjectInfo> depProjectInfos,
			bool isSomethingNewFromDepProjects, string slnWorkingDir, string outputFolderPath,
			out string projectNameSpec)
		{
			string projectName = vcProject.getName();

			string prjWorkingDir;
			if (string.IsNullOrEmpty(outputFolderPath))
			{
				string cfg = gccConfig.getConfigName();
				if (cfg != null) cfg = cfg.Trim();
				prjWorkingDir = slnWorkingDir + "\\" +
					(isReleaseMode ? s_kReleaseFolderName : s_kDebugFolderName) +
					(string.IsNullOrEmpty(cfg) ? "" : "\\" + cfg) +
					"\\" + projectName;
			}
			else
			{
				string t = PathUtils.combine(slnWorkingDir, outputFolderPath);
				prjWorkingDir = PathUtils.combine(t, projectName);
			}

			if (!Directory.Exists(prjWorkingDir))
			{
				Directory.CreateDirectory(prjWorkingDir);
			}
			string vcDir = vcProject.getDir();

			#region Declare info need to get from XML file
			projectNameSpec = projectName;
			bool USE_ADDITIONAL_INCLUDE_DIRECTORIES_FROM_VS = true;
			bool USE_EXCLUDEFROMBUILD_VS_FLAG = false;
			string msvcConfiguration = null;

			List<string> ignoredFilters = new List<string>();
			List<string> ignoredFilePatterns = new List<string>();
			List<TFileSpecific> fileSpecificList = new List<TFileSpecific>();
			List<string> addSourceFileToProjectList = new List<string>(); // contains full path
			int unityBuildsNumber = 0;
			List<string> excludeFileFromUnityBuild = new List<string>();

			string[] INCLUDE_PATHS = null;
			string[] LINK_PATHS = null;
			string[] DEFINES = null;
			string[] LDLIBS = null;
			string[] LDFLAGS = null;
			string[] CFLAGS = null;
			#endregion

			#region Get info from XML file
			{
				foreach (XmlNode childNode in projectNode.ChildNodes)
				{
					if (childNode.Name == KXml.s_kMacroTag)
					{
						#region Macro
						XmlAttribute att = childNode.Attributes[KXml.s_kNameAttr];
						if (att == null) continue;

						string macroName = att.Value;
						
						if (macroName == "USE_SPECIFIC_OUTPUT_NAME")
						{
							string val = gccConfig.getValueEscape(CXmlUtils.getXmlValue(childNode, isReleaseMode)).Trim();
							if (!string.IsNullOrEmpty(val))
							{
								projectNameSpec = val;
							}
						}
						else if (macroName == "USE_ADDITIONAL_INCLUDE_DIRECTORIES_FROM_VS")
						{
							USE_ADDITIONAL_INCLUDE_DIRECTORIES_FROM_VS = StringUtils.convertString2Bool(
								gccConfig.getValueEscape(CXmlUtils.getXmlValue(childNode, isReleaseMode))
							);
						}
						else if (macroName == "USE_EXCLUDEFROMBUILD_VS_FLAG")
						{
							USE_EXCLUDEFROMBUILD_VS_FLAG = StringUtils.convertString2Bool(
								gccConfig.getValueEscape(CXmlUtils.getXmlValue(childNode, isReleaseMode))
							);
						}
						else if (macroName == "INCLUDE_PATHS")
						{
							INCLUDE_PATHS = StringUtils.splitBySeparate(
								gccConfig.getValueEscape(CXmlUtils.getXmlValue(childNode, isReleaseMode))
							);
							for (int i = 0; i < INCLUDE_PATHS.Length; i++)
							{
								INCLUDE_PATHS[i] = PathUtils.combine(vcDir, INCLUDE_PATHS[i]);
							}
						}
						else if (macroName == "LINK_PATHS")
						{
							LINK_PATHS = StringUtils.splitBySeparate(
								gccConfig.getValueEscape(CXmlUtils.getXmlValue(childNode, isReleaseMode))
							);
							for (int i = 0; i < LINK_PATHS.Length; i++)
							{
								// TODO: note choice
								//LINK_PATHS[i] = PathUtils.combine(slnWorkingDir, LINK_PATHS[i]);
								LINK_PATHS[i] = PathUtils.combine(vcDir, LINK_PATHS[i]);
							}
						}
						else if (macroName == "DEFINES")
						{
							DEFINES = StringUtils.splitBySeparate(
								gccConfig.getValueEscape(CXmlUtils.getXmlValue(childNode, isReleaseMode))
							);
						}
						else if (macroName == "LDLIBS")
						{
							LDLIBS = StringUtils.splitBySeparate(
								gccConfig.getValueEscape(CXmlUtils.getXmlValue(childNode, isReleaseMode))
							);
						}
						else if (macroName == "LDFLAGS")
						{
							LDFLAGS = StringUtils.splitBySeparate(
								gccConfig.getValueEscape(CXmlUtils.getXmlValue(childNode, isReleaseMode))
							);
						}
						else if (macroName == "CFLAGS")
						{
							CFLAGS = StringUtils.splitBySeparate(
								gccConfig.getValueEscape(CXmlUtils.getXmlValue(childNode, isReleaseMode))
							);
						}
						#endregion
					}
					else if (childNode.Name == KXml.s_kMSVCConfigurationTag)
					{
						#region MSVCConfiguration
						//<MSVCConfiguration Debug="Debug" Release="Release"/>
						XmlAttribute att = childNode.Attributes[isReleaseMode ?
							KXml.s_kMSVCConfiguration_ReleaseAttr : KXml.s_kMSVCConfiguration_DebugAttr];
						if (att != null)
						{
							msvcConfiguration = gccConfig.getValueEscape(att.Value);
						}
						#endregion
					}
					else if (childNode.Name == KXml.s_kIgnoreTag)
					{
						#region Ignore
						//<Ignore>		
						//	<File Name="dummy_main" />
						//	<Filter Name="win32" />
						//</Ignore>
						foreach (XmlNode iNode in childNode.ChildNodes)
						{
							if (iNode.Name == KXml.s_kFileTag)
							{
								XmlAttribute att = iNode.Attributes[KXml.s_kNameAttr];
								if (att != null)
								{
									string fileName = att.Value.Trim();
									if (fileName.Length > 0)
									{
										ignoredFilePatterns.Add(gccConfig.getValueEscape(fileName));
									}
								}
							}
							else if (iNode.Name == KXml.s_kFilterTag)
							{
								XmlAttribute att = iNode.Attributes[KXml.s_kNameAttr];
								if (att != null)
								{
									string filterName = att.Value.Trim();
									if (filterName.Length > 0)
									{
										ignoredFilters.Add(gccConfig.getValueEscape(filterName));
									}
								}
							}
						}
						#endregion
					}
					else if (childNode.Name == KXml.s_kFileSpecificTag)
					{
						#region FileSpecific
						//<FileSpecific>
						//	<File Name="" CFLAGS=""/>
						//	<File Name="" CFLAGS=""/>
						//</FileSpecific>
						foreach (XmlNode fileNode in childNode.ChildNodes)
						{
							if (fileNode.Name == KXml.s_kFileTag)
							{
								XmlAttribute nameAtt = fileNode.Attributes[KXml.s_kNameAttr];
								if (nameAtt == null) continue;
								string name = nameAtt.Value.Trim();
								if (string.IsNullOrEmpty(name)) continue;
								
								XmlAttribute cflagsAtt	= fileNode.Attributes[KXml.s_kFileSpecific_CFLAGS_Attr];
								XmlAttribute definesAtt	= fileNode.Attributes[KXml.s_kFileSpecific_DEFINES_Attr];

								string cflags	= cflagsAtt		== null ? null : cflagsAtt.Value.Trim();
								string defines	= definesAtt	== null ? null : definesAtt.Value.Trim();
								if (string.IsNullOrEmpty(cflags))	cflags	= null;
								if (string.IsNullOrEmpty(defines))	defines	= null;

								if (cflags == null && defines == null) continue;

								fileSpecificList.Add(new TFileSpecific(
									gccConfig.getValueEscape(name),
									cflags == null ? null : StringUtils.splitBySeparate(gccConfig.getValueEscape(cflags)),
									defines == null ? null : StringUtils.splitBySeparate(gccConfig.getValueEscape(defines))
								));
							}
						}
						#endregion
					}
					else if (childNode.Name == KXml.s_kAddSourceFileToProjectTag)
					{
						#region AddSourceFileToProject
						//<AddSourceFileToProject>
						//	<File Name="$(ANDROID_NDK_PATH)\sources\android\cpufeatures\cpu-features.c"/>			
						//	<File Name="$(ANDROID_NDK_PATH)\sources\android\cpufeatures\*.c"/>
						//</AddSourceFileToProject>
						foreach (XmlNode iNode in childNode.ChildNodes)
						{
							if (iNode.Name == KXml.s_kFileTag)
							{
								XmlAttribute nameAtt = iNode.Attributes[KXml.s_kNameAttr];
								if (nameAtt == null) continue;
								string name = nameAtt.Value.Trim();
								if (string.IsNullOrEmpty(name)) continue;

								name = gccConfig.getValueEscape(name);
								// Note that name can be a wild card (e.g. *.c), so we cannot call Path.GetFullPath()
								name = PathUtils.combineSimple(vcDir, name);

								string[] matchedFiles = PathUtils.getFilesWithWildcard(name);
								foreach (string f in matchedFiles)
								{
									// f can be a non full path (e.g. e:\a\b\c\..\d.cpp
									addSourceFileToProjectList.Add( Path.GetFullPath(f) );
								}
							}
						}
						#endregion
					}
					else if (childNode.Name == KXml.s_kUnityBuildsTag)
					{
						#region UnityBuilds
						foreach (XmlNode nodeAutoGenerated in childNode.ChildNodes)
						{
							if (nodeAutoGenerated.Name != KXml.s_kAutoGeneratedTag) continue;
							XmlAttribute att = nodeAutoGenerated.Attributes[KXml.s_kUnityBuildsNumberAttr];
							if (att == null) continue;
							
							unityBuildsNumber = StringUtils.convertString2Int(att.Value);
							if (unityBuildsNumber > 0)
							{
								foreach (XmlNode nodeExcludeFileFromUnityBuild in nodeAutoGenerated.ChildNodes)
								{
									if (nodeExcludeFileFromUnityBuild.Name != KXml.s_kExcludeFileFromUnityBuildTag)
										continue;
									att = nodeExcludeFileFromUnityBuild.Attributes[KXml.s_kNameAttr];
									if (att != null)
									{
										string fileName = att.Value.Trim();
										if (fileName.Length > 0)
										{
											excludeFileFromUnityBuild.Add(fileName);
										}
									}
								}
							}

							break;
						}
						#endregion
					}
				}

				if (msvcConfiguration == null)
				{
					msvcConfiguration = isReleaseMode ?
						KXml.s_kMSVCConfiguration_ReleaseAttr : KXml.s_kMSVCConfiguration_DebugAttr;
				}
			}
			#endregion

			List<string> sourceFilePathList = new List<string>();
			#region Get source files from VC project
			{
				List<string> vcFiles = vcProject.getFiles(msvcConfiguration, ignoredFilters, USE_EXCLUDEFROMBUILD_VS_FLAG);

				foreach (string sourceFilePath in vcFiles)
				{
					string ext = Path.GetExtension(sourceFilePath);
					if (string.IsNullOrEmpty(ext)) continue;
					if (!gccConfig.isCompileFileType(ext.Substring(1))) continue;

					bool ignored = false;
					foreach (string pattern in ignoredFilePatterns)
					{
						if (PathUtils.checkPatternFile(sourceFilePath, pattern, vcDir))
						{
							ignored = true;
							break;
						}
					}
					if (ignored) continue;

					if (!PathUtils.checkPathExistInList(sourceFilePath, sourceFilePathList))
					{
						sourceFilePathList.Add(sourceFilePath);
					}
				}
			}
			#endregion
			#region addSourceFileToProjectList to sourceFilePathList
			{
				foreach (string source in addSourceFileToProjectList)
				{
					// Make sure: source is a full path
					if (!File.Exists(source))
					{
						CConsole.writeWarning("warning: could not found source file " +
							source + " which is declared in the <AddSourceFileToProjectTag> tag, it will be ignored\n");
						continue;
					}

					if (!PathUtils.checkPathExistInList(source, sourceFilePathList))
					{
						sourceFilePathList.Add(source);
					}
				}
			}
			#endregion

			#region Process UnityBuilds
			if (unityBuildsNumber > 0 && sourceFilePathList.Count > unityBuildsNumber)
			{
				List<string> ubList = new List<string>();
				List<string> nmList = new List<string>();
				for (int i = 0; i < sourceFilePathList.Count; i++)
				{
					bool ub = true;
					if ( !PathUtils.isCppExt(Path.GetExtension(sourceFilePathList[i])) )
					{
						ub = false; // this is a C or Assembly file
					}
					else
					{
						foreach (TFileSpecific fs in fileSpecificList)
						{
							if (PathUtils.checkPatternFile(sourceFilePathList[i], fs.name, vcDir))
							{
								ub = false;
								break;
							}
						}
						if (ub)
						{
							foreach (string pattern in excludeFileFromUnityBuild)
							{
								if (PathUtils.checkPatternFile(sourceFilePathList[i], pattern, vcDir))
								{
									ub = false;
									break;
								}
							}
						}
					}

					if (ub)
					{
						ubList.Add(sourceFilePathList[i]);
					}
					else
					{
						nmList.Add(sourceFilePathList[i]);
					}
				}

				sourceFilePathList = nmList; // important

				if (ubList.Count > 0)
				{
					int groupSize = ubList.Count / unityBuildsNumber;
					int extra = ubList.Count % unityBuildsNumber;
					int left = 0, right = -1;
					for (int g = 0; g < unityBuildsNumber; g++)
					{
						string ubFileName = "UB_" + projectName + "_" + g + ".cpp";
						string ubFilePath = prjWorkingDir + "\\" + ubFileName;

						//int left = g * groupSize;
						//int right = g < unityBuildsNumber - 1 ? left + groupSize - 1 : ubList.Count - 1;
						left = right + 1;
						right = left + groupSize - 1;
						if (g < extra) right++;
						string stringToWrite = "// katatunix@gmail.com - This file was auto generated - DO NOT EDIT\r\n";
						for (int k = left; k <= right; k++)
						{
							//stringToWrite += "#include <" + ubList[k] + ">\r\n";
							string rel = PathUtils.getRelativePath(ubList[k], prjWorkingDir);
							stringToWrite += "#include \"" + rel + "\"\r\n";
						}

						bool writeNew = true;

						if (File.Exists(ubFilePath))
						{
							using (StreamReader reader = new StreamReader(ubFilePath))
							{
								if (stringToWrite == reader.ReadToEnd())
								{
									writeNew = false;
								}
							}
						}

						if (writeNew)
						{
							using (StreamWriter writer = new StreamWriter(ubFilePath))
							{
								writer.Write(stringToWrite);
							}
						}

						sourceFilePathList.Add(ubFilePath);
					}
				}

			}
			#endregion

			string gccDefines = "";
			#region DEFINES
			{
				if (gccConfig.get_DEFINES() != null)
				{
					gccDefines += " " + GccCmdParser.makeGccDefinesString(gccConfig.get_DEFINES()).Trim();
					gccDefines = gccDefines.Trim();
				}
				if (DEFINES != null)
				{
					gccDefines += " " + GccCmdParser.makeGccDefinesString(DEFINES).Trim();
					gccDefines = gccDefines.Trim();
				}
			}
			#endregion

			string gccIncludePaths = "";
			#region INCLUDE_PATHS
			{
				if (INCLUDE_PATHS != null)
				{
					gccIncludePaths += " " + GccCmdParser.makeGccIncludePathsString(INCLUDE_PATHS).Trim();
					gccIncludePaths = gccIncludePaths.Trim();
				}
				if (USE_ADDITIONAL_INCLUDE_DIRECTORIES_FROM_VS)
				{
					List<string> incs = vcProject.getIncludePaths(msvcConfiguration);
					if (incs != null)
					{
						gccIncludePaths += " " + GccCmdParser.makeGccIncludePathsString(incs.ToArray()).Trim();
						gccIncludePaths = gccIncludePaths.Trim();
					}
				}
				if (gccConfig.get_INCLUDE_PATHS() != null)
				{
					gccIncludePaths += " " + GccCmdParser.makeGccIncludePathsString(gccConfig.get_INCLUDE_PATHS()).Trim();
					gccIncludePaths = gccIncludePaths.Trim();
				}
			}
			#endregion

			// TODO: note choice
			// force to use -g option when GENERATE_DSYM == true
			//string gccCflags = gccConfig.get_GENERATE_DSYM() ? CUtils.s_kGenDsym : "";
			string gccCflags = "";
			#region CFLAGS
			{
				if (gccConfig.get_CFLAGS() != null)
				{
					gccCflags += " " + GccCmdParser.makeGccItemsString(gccConfig.get_CFLAGS()).Trim();
					gccCflags = gccCflags.Trim();
				}

				if (CFLAGS != null)
				{
					gccCflags += " " + GccCmdParser.makeGccItemsString(CFLAGS).Trim();
					gccCflags = gccCflags.Trim();
				}
			}
			#endregion

			int sourceCount = sourceFilePathList.Count;
			bool[] markDuplicate = new bool[sourceCount];
			#region markDuplicate
			{
				for (int i = 0; i < sourceCount; i++) markDuplicate[i] = false;

				for (int i = 0; i < sourceCount - 1; i++)
				{
					if (!markDuplicate[i])
					{
						bool hasDuplicate = false;
						string f = Path.GetFileNameWithoutExtension(sourceFilePathList[i]);
						int count = 1;
						for (int j = i + 1; j < sourceCount; j++)
						{
							if (!markDuplicate[j])
							{
								if (f == Path.GetFileNameWithoutExtension(sourceFilePathList[j]))
								{
									hasDuplicate = true;
									markDuplicate[j] = true;
									count++;
								}
							}
						}
						if (hasDuplicate)
						{
							markDuplicate[i] = true;
							CConsole.writeWarning("warning: " + count + " source files have the same name " + f + ", they will be always recompiled.\n");
						}
					}
				}
			}
			#endregion

			List<TCommand> commands = new List<TCommand>();
			List<string> objFileNames = new List<string>(); // Used for link command

			#region Check through sourceFilePathList for dependencies and make compile commands list
			{
				CConsole.writeInfoLine("Checking dependencies...");
				for (int i = 0; i < sourceCount; i++)
				{
					CConsole.write("\r" + (i + 1) + "/" + sourceCount);
					
					#region Step
					string sourceFilePath = sourceFilePathList[i];
					string sourceFileNameWithoutExt = Path.GetFileNameWithoutExtension(sourceFilePath);
					string sourceFileExt = Path.GetExtension(sourceFilePath);
					bool isAssembly = PathUtils.isAssemblyExt(sourceFileExt);

					bool recompile = true;

					if (!markDuplicate[i])
					{
						#region Check LastWriteTime of files and dependencies, output: recompile

						string basePath = prjWorkingDir + "\\" + sourceFileNameWithoutExt;
						string objFilePath = basePath + s_kObjFileExt;
						if (File.Exists(objFilePath))
						{
							DateTime lastWriteTime_ObjFile = File.GetLastWriteTime(objFilePath);
							if (File.GetLastWriteTime(sourceFilePath) < lastWriteTime_ObjFile)
							{
								if (isAssembly)
								{
									recompile = false;
								}
								else
								{
									string depFilePath = basePath + s_kDepFileExt;
									if (File.Exists(depFilePath))
									{
										#region Check content of the .d file
										recompile = false;
										using (StreamReader streamReader = new StreamReader(depFilePath))
										{
											string line;
											while ((line = streamReader.ReadLine()) != null)
											{
												// https://gcc.gnu.org/onlinedocs/gcc/Preprocessor-Options.html#Preprocessor-Options
											
												//if (!line.EndsWith(":")) continue;
												//string dFilePath = line.Substring(0, line.Length - 1);
												//if (File.GetLastWriteTime(dFilePath) >= lastWriteTime_ObjFile)
												//{
												//	recompile = true;
												//	break;
												//}

												string[] files = StringUtils.splitBySpaceTab(line);
												foreach (string dFilePath in files)
												{
													if (dFilePath[dFilePath.Length - 1] == ':') continue;
													if (dFilePath == "\\") continue;
													
													if ( !File.Exists(dFilePath) || File.GetLastWriteTime(dFilePath) >= lastWriteTime_ObjFile )
													{
														//CConsole.writeInfoLine("changed file: " + dFilePath);
														recompile = true;
														break;
													}
												}

												if (recompile)
												{
													break;
												}
											}
										}
										#endregion
									}
								}
							}
						}

						#endregion
					}

					if (!recompile)
					{
						objFileNames.Add(sourceFileNameWithoutExt);
						continue;
					}

					TCommand cmd = new TCommand();
					cmd.type = ECommandType.eCompile;
					cmd.prjName = projectName;
					cmd.workingDir = prjWorkingDir;
					cmd.alias = Path.GetFileName(sourceFilePath);

					string compileCommand;
					#region Make compile command
					string _gccCflags = gccCflags;
					string _gccDefines = gccDefines;
					// FileSpecific
					foreach (TFileSpecific fs in fileSpecificList)
					{
						if (PathUtils.checkPatternFile(sourceFilePath, fs.name, vcDir))
						{
							if (fs.cflags != null)
							{
								if (_gccCflags.Length > 0) _gccCflags += " ";
								_gccCflags += GccCmdParser.makeGccItemsString(fs.cflags);
								_gccCflags = _gccCflags.Trim();
							}
							//
							if (fs.defines != null)
							{
								if (_gccDefines.Length > 0) _gccDefines += " ";
								_gccDefines += GccCmdParser.makeGccDefinesString(fs.defines);
								_gccDefines = _gccDefines.Trim();
							}
						}
					}

					string objFileName = sourceFileNameWithoutExt;
					// Duplicate objFileName?
					int count = 0;
					while (objFileNames.Contains(objFileName))
					{
						objFileName = sourceFileNameWithoutExt + "_" + (++count);
					}
					objFileNames.Add(objFileName);
					objFileName += s_kObjFileExt;

					string objFilePathFull = PathUtils.combine(prjWorkingDir, objFileName);

					// Now create compile cmd
					if (PathUtils.isCppExt(sourceFileExt))
					{
						compileCommand = gccConfig.get_COMPILE_CPP_COMMAND_LINE(
							_gccDefines, _gccCflags, gccIncludePaths, sourceFilePath, /*objFileName*/objFilePathFull);
					}
					else
					{
						compileCommand = gccConfig.get_COMPILE_CC_COMMAND_LINE(
							_gccDefines, _gccCflags, gccIncludePaths, sourceFilePath, /*objFileName*/objFilePathFull);
					}					
					#endregion

					cmd.command = compileCommand;
					cmd.verboseString = compileCommand;
					commands.Add(cmd);

					#endregion
				}
				if (sourceCount > 0)
				{
					CConsole.writeLine();
				}
				CConsole.writeInfoLine(commands.Count + " files to be compiled");
			}
			#endregion

			#region Write the compile commands to file CompileCommands.txt
			using (StreamWriter writer = new StreamWriter(prjWorkingDir + "\\CompileCommands.txt"))
			{
				foreach (TCommand cmd in commands)
				{
					writer.WriteLine(cmd.command);
				}
			}
			#endregion

			int beginLinkCmdIndex = commands.Count;

			string originalLinkCommand	= null;
			string linkCommand = null;
			string linkFileName = null;

			string dsymCommand = null;
			string dsymFileName = null;

			string copiedFileName = null;
			bool copiedCommand = false;

			string stripCommand = null;
			string stripFileName = null;

			#region Make link, DSYM, strip commands
			
			string objFilesString = "";
			#region Make objFilesString
			foreach (string objFile in objFileNames)
			{
				if (objFilesString.Length > 0) objFilesString += " ";
				objFilesString += objFile + s_kObjFileExt;
			}
			#endregion

			switch (type)
			{
				case EProjectType.eStaticLib:
				{
					#region Static
					linkFileName = "lib" + projectNameSpec + ".a";
					string linkFilePath = prjWorkingDir + "\\" + linkFileName;
					bool relink = commands.Count > 0 || !File.Exists(linkFilePath);

					if (!relink)
					{
						// Check .o files
						// Sure linkFilePath) is existed
						relink = hasNewerDepFile(prjWorkingDir, objFileNames, s_kObjFileExt, linkFilePath);
					}

					if (relink)
					{
						originalLinkCommand = gccConfig.get_STATIC_LINK_COMMAND_LINE(linkFileName, objFilesString);
						int spaceIndex = originalLinkCommand.IndexOf(" ");
						if (spaceIndex == -1)
						{
							CConsole.writeError("error: Invalid link command: " + originalLinkCommand + "\n");
							return null; //===
						}
						string tmpFilePath = Path.GetTempFileName();
						using (StreamWriter streamWriter = new StreamWriter(tmpFilePath))
						{
							streamWriter.Write(originalLinkCommand.Substring(spaceIndex + 1));
						}
						
						linkCommand = originalLinkCommand.Substring(0, spaceIndex) + " @" + tmpFilePath;

						TCommand cmd = new TCommand(
							linkCommand,
							originalLinkCommand,
							prjWorkingDir,
							linkFileName,
							ECommandType.eLinkStatic,
							projectName
						);
						commands.Add(cmd);
					}
					#endregion

					break;
				}

				case EProjectType.eDynamicLib:
				case EProjectType.eExecutable:
				{
					#region Dynamic and executable

					if (type == EProjectType.eDynamicLib)
					{
						string outFileNameBase = "lib" + projectNameSpec;
						linkFileName = outFileNameBase + ".so.full";
						stripFileName = outFileNameBase + ".so";
						dsymFileName = outFileNameBase + ".dsym";

						copiedFileName = stripFileName;
					}
					else
					{
						string outFileNameBase = projectNameSpec;
						linkFileName = outFileNameBase + ".exe.full";
						stripFileName = outFileNameBase + ".exe";
						dsymFileName = outFileNameBase + ".dsym";

						copiedFileName = stripFileName;
					}

					string linkFilePath = prjWorkingDir + "\\" + linkFileName;

					bool relink = isSomethingNewFromDepProjects || commands.Count > 0 || !File.Exists(linkFilePath);

					if (!relink)
					{
						// Check .o files
						// Sure linkFilePath is existed
						relink = hasNewerDepFile(prjWorkingDir, objFileNames, s_kObjFileExt, linkFilePath);
					}

					if (!relink && depProjectInfos != null && depProjectInfos.Count > 0)
					{
						// Check .a files
						DateTime linkFileDateTime = File.GetLastWriteTime(linkFilePath);
						foreach (TDepProjectInfo info in depProjectInfos)
						{
							string aFilePath = prjWorkingDir + "\\..\\" + info.name + "\\lib" + info.nameSpec + ".a";
							// if aFilePath is not existed, we must relink to show an error message
							if (!File.Exists(aFilePath) || File.GetLastWriteTime(aFilePath) > linkFileDateTime)
							{
								relink = true;
								break;
							}
						}
					}
					
					// Make link cmd
					if (relink)
					{
						string gccLdlibs = "";
						#region LDLIBS
						{
							// TODO: note remove this?
							//if (depProjectInfos != null && depProjectInfos.Count > 0)
							//{
							//	string[] libs = new string[depProjectInfos.Count];
							//	for (int i = 0; i < libs.Length; i++)
							//	{
							//		libs[i] = depProjectInfos[i].nameSpec;
							//	}

							//	gccLdlibs += " " + CUtils.makeGccLinkLibsString(libs).Trim();
							//	gccLdlibs = gccLdlibs.Trim();
							//}
							if (LDLIBS != null)
							{
								gccLdlibs += " " + GccCmdParser.makeGccItemsString(LDLIBS).Trim();
								gccLdlibs = gccLdlibs.Trim();
							}
							if (gccConfig.get_LDLIBS() != null)
							{
								gccLdlibs += " " + GccCmdParser.makeGccItemsString(gccConfig.get_LDLIBS()).Trim();
								gccLdlibs = gccLdlibs.Trim();
							}
						}
						#endregion

						string gccLinkPaths = "";
						#region LINK_PATHS
						{
							if (depProjectInfos != null && depProjectInfos.Count > 0)
							{
								string[] paths = new string[depProjectInfos.Count];
								for (int i = 0; i < paths.Length; i++)
								{
									paths[i] = "..\\" + depProjectInfos[i].name;
								}
								gccLinkPaths += " " + GccCmdParser.makeGccLinkPathsString(paths).Trim();
								gccLinkPaths = gccLinkPaths.Trim();
							}
							if (LINK_PATHS != null)
							{
								gccLinkPaths += " " + GccCmdParser.makeGccLinkPathsString(LINK_PATHS).Trim();
								gccLinkPaths = gccLinkPaths.Trim();
							}
							if (gccConfig.get_LINK_PATHS() != null)
							{
								gccLinkPaths += " " + GccCmdParser.makeGccLinkPathsString(gccConfig.get_LINK_PATHS()).Trim();
								gccLinkPaths = gccLinkPaths.Trim();
							}
						}
						#endregion

						string gccLdflags = "";
						#region LDGFLAS
						if (gccConfig.get_LDFLAGS() != null)
						{
							gccLdflags += " " + GccCmdParser.makeGccItemsString(gccConfig.get_LDFLAGS()).Trim();
							gccLdflags = gccLdflags.Trim();
						}
						if (LDFLAGS != null)
						{
							gccLdflags += " " + GccCmdParser.makeGccItemsString(LDFLAGS).Trim();
							gccLdflags = gccLdflags.Trim();
						}
						#endregion
						
						linkCommand = type == EProjectType.eDynamicLib ?
							gccConfig.get_DYNAMIC_LINK_COMMAND_LINE(linkFileName, objFilesString, gccLdlibs, gccLdflags, gccLinkPaths) :
							gccConfig.get_EXE_LINK_COMMAND_LINE(objFilesString, gccLdlibs, gccLdflags, gccLinkPaths, linkFileName);
						originalLinkCommand = linkCommand;
					}

					// Make DSYM cmd
					if (gccConfig.get_GENERATE_DSYM())
					{
						// .dsym depends on .so.full / .exe.full
						string dsymFilePath = prjWorkingDir + "\\" + dsymFileName;
						bool reGenDsym = relink;

						if (!reGenDsym)
						{
							// Sure linkFilePath is existed because relink == false
							reGenDsym = !File.Exists(dsymFilePath) ||
								File.GetLastWriteTime(linkFilePath) > File.GetLastWriteTime(dsymFilePath);
						}

						if (reGenDsym)
						{
							dsymCommand = gccConfig.get_DSYM_COMMAND_LINE(linkFileName, dsymFileName);
						}
					}

					// Make copy cmd
					{
						// .so (copied) depends on .so.full
						string copiedFilePath = prjWorkingDir + "\\" + copiedFileName;
						bool reCopy = relink;

						if (!reCopy)
						{
							// Sure linkFilePath is existed because relink == false
							reCopy = !File.Exists(copiedFilePath) ||
								File.GetLastWriteTime(linkFilePath) > File.GetLastWriteTime(copiedFilePath);
						}

						if (reCopy)
						{
							copiedCommand = true;
						}
					}

					// Make strip cmd
					if ((isReleaseMode && gccConfig.get_STRIP_DEBUG_SYMBOLS_FOR_RELEASE()) ||
							(!isReleaseMode && gccConfig.get_STRIP_DEBUG_SYMBOLS_FOR_DEBUG()))
					{
						// .so (strip) depends on .so.full
						string stripFilePath = prjWorkingDir + "\\" + stripFileName;
						bool reStrip = relink;

						if (!reStrip)
						{
							// Sure linkFilePath is existed because relink == false
							reStrip = !File.Exists(stripFilePath) ||
								File.GetLastWriteTime(linkFilePath) > File.GetLastWriteTime(stripFilePath);
						}

						if (reStrip)
						{
							stripCommand = gccConfig.get_STRIP_COMMAND_LINE(stripFileName);
						}
					}

					if (linkCommand != null)
					{
						TCommand cmd = new TCommand(
							linkCommand,
							originalLinkCommand,
							prjWorkingDir,
							linkFileName,
							ECommandType.eLinkDynamic,
							projectName
						);
						commands.Add(cmd);
					}

					if (dsymCommand != null)
					{
						TCommand cmd = new TCommand(
							dsymCommand,
							dsymCommand,
							prjWorkingDir,
							dsymFileName,
							ECommandType.eGenerateDsym,
							projectName
						);
						commands.Add(cmd);
					}

					if (copiedCommand)
					{
						TCommand cmd = new TCommand(
							prjWorkingDir + "\\" + linkFileName,
							prjWorkingDir + "\\" + copiedFileName,
							prjWorkingDir,
							linkFileName + " -> " + copiedFileName, // alias
							ECommandType.eCopy,
							projectName
						);
						commands.Add(cmd);
					}

					if (stripCommand != null)
					{
						TCommand cmd = new TCommand(
							stripCommand,
							stripCommand,
							prjWorkingDir,
							stripFileName,
							ECommandType.eStrip,
							projectName
						);
						commands.Add(cmd);
					}

					#endregion
					break;
				}
			}

			#endregion

			#region Write the link commands to file LinkCommands.txt
			using (StreamWriter writer = new StreamWriter(prjWorkingDir + "\\LinkCommands.txt"))
			{
				for (int i = beginLinkCmdIndex; i < commands.Count; i++)
				{
					TCommand cmd = commands[i];
					if (cmd.type == ECommandType.eLinkStatic || cmd.type == ECommandType.eLinkDynamic)
					{
						writer.WriteLine(cmd.command);
					}
				}
			}
			#endregion
			
			return commands;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="workingDir"></param>
		/// <param name="depFileNames"></param>
		/// <param name="depFileExt"></param>
		/// <param name="targetFilePath">Make sure the file is existed</param>
		/// <returns></returns>
		private static bool hasNewerDepFile(string workingDir, List<string> depFileNames, string depFileExt,
			string targetFilePath)
		{
			DateTime targetFileDateTime = File.GetLastWriteTime(targetFilePath);
			foreach (string depFile in depFileNames)
			{
				string depPath = workingDir + "\\" + depFile + depFileExt;
				// if depPath is not existed, we must return true
				if (!File.Exists(depPath) || File.GetLastWriteTime(depPath) > targetFileDateTime)
				{
					return true;
				}
			}
			return false;
		}

		//============================================================================
		//============================================================================

		private static readonly string s_kObjFileExt = ".o";
		private static readonly string s_kDepFileExt = ".d";

		private static readonly string s_kDebugFolderName = "debug";
		private static readonly string s_kReleaseFolderName = "release";
	}
}
