using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

using System.IO;

using libcore;
using libmongcc.cmdparser;

namespace win2tiz
{
	class GccConfig
	{
		public GccConfig(string name)
		{
			m_configName = name; // can be null or empty
		}

		public bool load(XmlNode nodeGccConfig, bool isReleaseMode, string configDirPath)
		{
			m_nodeGccConfig = nodeGccConfig;
			m_isReleaseMode = isReleaseMode;

			//
			m_MAIN_PROJECT = getMacroValueEscape("MAIN_PROJECT");

			m_GENERATE_DSYM = StringUtils.convertString2Bool(getMacroValueEscape("GENERATE_DSYM"));

			m_STRIP_DEBUG_SYMBOLS_FOR_RELEASE	= StringUtils.convertString2Bool(getMacroValueEscape("STRIP_DEBUG_SYMBOLS_FOR_RELEASE"));
			m_STRIP_DEBUG_SYMBOLS_FOR_DEBUG		= StringUtils.convertString2Bool(getMacroValueEscape("STRIP_DEBUG_SYMBOLS_FOR_DEBUG"));

			m_TYPES_OF_FILES_TO_BE_COMPILED = StringUtils.splitBySeparate(getMacroValueEscape("TYPES_OF_FILES_TO_BE_COMPILED"));

			m_DEFINES	= StringUtils.splitBySeparate(getMacroValueEscape("DEFINES"));
			m_CFLAGS	= StringUtils.splitBySeparate(getMacroValueEscape("CFLAGS"));

			m_INCLUDE_PATHS = StringUtils.splitBySeparate(getMacroValueEscape("INCLUDE_PATHS"));
			for (int i = 0; i < m_INCLUDE_PATHS.Length; i++)
			{
				m_INCLUDE_PATHS[i] = PathUtils.combine(configDirPath, m_INCLUDE_PATHS[i]);
			}

			m_LDLIBS = StringUtils.splitBySeparate(getMacroValueEscape("LDLIBS"));
			m_LDFLAGS = StringUtils.splitBySeparate(getMacroValueEscape("LDFLAGS"));

			m_LINK_PATHS = StringUtils.splitBySeparate(getMacroValueEscape("LINK_PATHS"));
			for (int i = 0; i < m_LINK_PATHS.Length; i++)
			{
				m_LINK_PATHS[i] = PathUtils.combine(configDirPath, m_LINK_PATHS[i]);
			}

			m_COMPILE_CPP_COMMAND_LINE		= getMacroValueNoEscape("COMPILE_CPP_COMMAND_LINE")	+ " " + GccCmdParser.s_kMakeDependencies;
			m_COMPILE_CC_COMMAND_LINE		= getMacroValueNoEscape("COMPILE_CC_COMMAND_LINE")	+ " " + GccCmdParser.s_kMakeDependencies;
			m_DYNAMIC_LINK_COMMAND_LINE		= getMacroValueNoEscape("DYNAMIC_LINK_COMMAND_LINE");
			m_STATIC_LINK_COMMAND_LINE		= getMacroValueNoEscape("STATIC_LINK_COMMAND_LINE");
			m_EXE_LINK_COMMAND_LINE			= getMacroValueNoEscape("EXE_LINK_COMMAND_LINE");
			m_DSYM_COMMAND_LINE				= getMacroValueNoEscape("DSYM_COMMAND_LINE");
			m_STRIP_COMMAND_LINE			= getMacroValueNoEscape("STRIP_COMMAND_LINE");

			return true; // TODO: when does it return false?
		}

		public string getValueEscape(string value)
		{
			string result = "";
			for (int i = 0; i < value.Length; i++)
			{
				if (value[i] == '$' && i + 1 < value.Length && value[i + 1] == '(')
				{
					for (int j = i + 2; j < value.Length; j++)
					{
						if (value[j] == ')')
						{
							string macro = value.Substring(i + 2, j - i - 2);
							string val = getMacroValueEscape(macro);
							// TODO: note choice
							//result += val == null ? "$(" + macro + ")" : val;
							result += val == null ? "" : val;

							i = j;
							break;
						}
					}
				}
				else
				{
					result += value[i];
				}
			}
			return result;
		}

		public string getConfigName()
		{
			return m_configName;
		}
		
		public string get_MAIN_PROJECT()
		{
			return m_MAIN_PROJECT;
		}

		public bool get_GENERATE_DSYM()
		{
			return m_GENERATE_DSYM;
		}

		public bool get_STRIP_DEBUG_SYMBOLS_FOR_RELEASE()
		{
			return m_STRIP_DEBUG_SYMBOLS_FOR_RELEASE;
		}

		public bool get_STRIP_DEBUG_SYMBOLS_FOR_DEBUG()
		{
			return m_STRIP_DEBUG_SYMBOLS_FOR_DEBUG;
		}

		public bool isCompileFileType(string fileType)
		{
			return m_TYPES_OF_FILES_TO_BE_COMPILED.Contains(fileType);
		}

		public string[] get_DEFINES()
		{
			return m_DEFINES;
		}

		public string[] get_CFLAGS()
		{
			return m_CFLAGS;
		}

		public string[] get_INCLUDE_PATHS()
		{
			return m_INCLUDE_PATHS;
		}

		public string[] get_LDLIBS()
		{
			return m_LDLIBS;
		}

		public string[] get_LDFLAGS()
		{
			return m_LDFLAGS;
		}

		public string[] get_LINK_PATHS()
		{
			return m_LINK_PATHS;
		}

		public string get_COMPILE_CPP_COMMAND_LINE(string defines, string cflags,
			string includePaths, string srcFile, string objFile)
		{
			return getValueEscape( m_COMPILE_CPP_COMMAND_LINE
				.Replace("$(DEFINES)",			defines)
				.Replace("$(CFLAGS)",			cflags)
				.Replace("$(INCLUDE_PATHS)",	includePaths)
				.Replace("$(SRC_FILE)",			srcFile)
				.Replace("$(OBJ_FILE)",			objFile) );
		}

		public string get_COMPILE_CC_COMMAND_LINE(string defines, string cflags,
			string includePaths, string srcFile, string objFile)
		{
			string cmd = m_COMPILE_CC_COMMAND_LINE
						.Replace("$(DEFINES)",			defines)
						.Replace("$(CFLAGS)",			cflags)
						.Replace("$(INCLUDE_PATHS)",	includePaths)
						.Replace("$(SRC_FILE)",			srcFile)
						.Replace("$(OBJ_FILE)",			objFile);

			return getValueEscape(cmd);
		}

		public string get_DYNAMIC_LINK_COMMAND_LINE(string outFile, string objFiles, string ldLibs,
			string ldFlags, string linkPaths)
		{
			return getValueEscape( m_DYNAMIC_LINK_COMMAND_LINE
				.Replace("$(OUT)",				outFile)
				.Replace("$(OBJ_FILES)",		objFiles)
				.Replace("$(LDLIBS)",			ldLibs)
				.Replace("$(LDFLAGS)",			ldFlags)
				.Replace("$(LINK_PATHS)",		linkPaths) );
		}

		public string get_STATIC_LINK_COMMAND_LINE(string outFile, string objFiles)
		{
			return getValueEscape( m_STATIC_LINK_COMMAND_LINE
				.Replace("$(OUT)",				outFile)
				.Replace("$(OBJ_FILES)",		objFiles) );
		}

		public string get_EXE_LINK_COMMAND_LINE(string objFiles, string ldLibs, string ldFlags,
			string linkPaths, string outFiles)
		{
			return getValueEscape( m_EXE_LINK_COMMAND_LINE
				.Replace("$(OBJ_FILES)",		objFiles)
				.Replace("$(LDLIBS)",			ldLibs)
				.Replace("$(LDFLAGS)",			ldFlags)
				.Replace("$(LINK_PATHS)",		linkPaths)
				.Replace("$(OUT)",				outFiles) );
		}

		public string get_DSYM_COMMAND_LINE(string inFile, string outFile)
		{
			return getValueEscape( m_DSYM_COMMAND_LINE
				.Replace("$(INPUT)",			inFile)
				.Replace("$(OUT)",				outFile) );
		}

		public string get_STRIP_COMMAND_LINE(string inFile)
		{
			return getValueEscape( m_STRIP_COMMAND_LINE
				.Replace("$(INPUT)",			inFile) );
		}

		//===================================================================================
		//===================================================================================

		private string getMacroValueEscape(string macro)
		{
			string rawVal = getMacroValueNoEscape(macro);
			if (rawVal == null) return null;

			return getValueEscape(rawVal);
		}

		/// <summary>
		/// </summary>
		/// <param name="macro"></param>
		/// <returns>null if not found the macro</returns>
		private string getMacroValueNoEscape(string macro)
		{
			XmlNode macroNode = CXmlUtils.findMacroChildNode(m_nodeGccConfig, macro);
			if (macroNode != null)
			{
				return CXmlUtils.getXmlValue(macroNode, m_isReleaseMode);
			}

			string value = Environment.GetEnvironmentVariable(macro, EnvironmentVariableTarget.Process);
			if (value != null) return value;

			value = Environment.GetEnvironmentVariable(macro, EnvironmentVariableTarget.User);
			if (value != null) return value;

			return Environment.GetEnvironmentVariable(macro, EnvironmentVariableTarget.Machine);
		}

		//===================================================================================
		//===================================================================================
		
		private XmlNode m_nodeGccConfig = null;
		private string m_configName;
		private bool m_isReleaseMode = false;

		private string m_MAIN_PROJECT = "";

		private bool m_GENERATE_DSYM = false;

		private bool m_STRIP_DEBUG_SYMBOLS_FOR_RELEASE = false;
		private bool m_STRIP_DEBUG_SYMBOLS_FOR_DEBUG = false;

		private string[] m_TYPES_OF_FILES_TO_BE_COMPILED = null;

		private string[] m_DEFINES			= null;
		private string[] m_CFLAGS			= null;
		private string[] m_INCLUDE_PATHS	= null;
		private string[] m_LDLIBS			= null;
		private string[] m_LDFLAGS			= null;
		private string[] m_LINK_PATHS		= null;

		private string m_COMPILE_CPP_COMMAND_LINE		= "";
		private string m_COMPILE_CC_COMMAND_LINE		= "";
		private string m_DYNAMIC_LINK_COMMAND_LINE		= "";
		private string m_STATIC_LINK_COMMAND_LINE		= "";
		private string m_EXE_LINK_COMMAND_LINE			= "";
		private string m_DSYM_COMMAND_LINE				= "";
		private string m_STRIP_COMMAND_LINE				= "";
		
	}
}
