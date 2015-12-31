using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.VisualStudio.VCProjectEngine;

namespace win2tiz.visualc
{
	class CProject2008 : AProject
	{
		public CProject2008(string name, string file)
			: base(name, file)
		{
			m_vcProject = null;
			m_projectDir = Path.GetDirectoryName(file);

			if (s_engine == null)
			{
				s_engine = new VCProjectEngineObject();
			}
		}

		public override bool load()
		{
			if (m_vcProject != null) return true;

			m_vcProject = s_engine.LoadProject(m_file) as VCProject;
			return m_vcProject != null;
		}

		public override List<string> getFiles(string config, List<string> ignoredFilters, bool enableExclude)
		{
			if (m_vcProject == null) return null;

			List<string> list = new List<string>();

			foreach (VCFile vcFile in (m_vcProject.Files as IVCCollection))
			{
				bool pass = true;
				if (enableExclude)
				{
					foreach (VCFileConfiguration vcFileConfig in (vcFile.FileConfigurations as IVCCollection))
					{
						if (vcFileConfig.ExcludedFromBuild)
						{
							VCConfiguration vcConfig = vcFileConfig.ProjectConfiguration as VCConfiguration;
							if (vcConfig.Name == config || vcConfig.ConfigurationName == config)
							{
								pass = false;
								break;
							}
						}
					}
				}

				if (!pass) continue;

				if (ignoredFilters != null && ignoredFilters.Count > 0)
				{
					if (isVcFileBelongToFilters(vcFile, ignoredFilters))
					{
						pass = false;
					}
				}

				if (!pass) continue;

				list.Add(vcFile.FullPath);
			}

			return list;
		}

		public override List<string> getIncludePaths(string config)
		{
			if (m_vcProject == null) return null;

			List<string> list = new List<string>();

			foreach (VCConfiguration vcConfig in (IVCCollection)m_vcProject.Configurations)
			{
				if (vcConfig.Name != config && vcConfig.ConfigurationName != config) continue;
				VCCLCompilerTool compilerTool = (VCCLCompilerTool)(vcConfig.Tools as IVCCollection).Item("VCCLCompilerTool");
				if (compilerTool == null) continue;

				string[] paths = compilerTool.FullIncludePath.Split(s_kSeparateStrings, StringSplitOptions.RemoveEmptyEntries);
				for (int i = 0; i < paths.Length; i++)
				{
					if (paths[i].IndexOf(" ") == -1)
					{
						list.Add(
							combinePath( m_projectDir, paths[i].Replace("\"", "") )
						);
					}
				}
				break;
			}

			return list;
		}

		// Helper

		private static bool isVcFileBelongToFilters(VCFile vcFile, List<string> filters)
		{
			Object obj = vcFile.Parent;
			while (obj != null)
			{
				VCFilter vcFilter = obj as VCFilter;
				if (vcFilter == null) break;
				if (filters.Contains(vcFilter.Name)) return true;
				obj = vcFilter.Parent;
			}
			return false;
		}

		private static string combinePath(string path1, string path2)
		{
			return Path.GetFullPath(Path.Combine(path1, path2));
		}

		private VCProject m_vcProject;
		private string m_projectDir;

		private static VCProjectEngine s_engine = null;
		private static readonly string[] s_kSeparateStrings = { ";" };
	}
}
