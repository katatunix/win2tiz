using System.Collections.Generic;
using System.IO;

namespace win2tiz.visualc
{
	public abstract class AProject
	{
		public AProject(string name, string file)
		{
			m_name = name;
			m_file = file;
		}

		public string getName()
		{
			return m_name;
		}

		public string getDir()
		{
			return Path.GetDirectoryName(m_file);
		}

		public abstract bool load();
		public abstract List<string> getFiles(string config, List<string> ignoredFilters, bool enableExclude);
		public abstract List<string> getIncludePaths(string config);

		protected string m_name;

		/// <summary>
		/// Full path to the .vcproj or .vcxproj file
		/// </summary>
		protected string m_file;
	}
}
