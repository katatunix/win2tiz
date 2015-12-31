using System;
using System.IO;
using System.Text.RegularExpressions;

namespace win2tiz.visualc
{
	class CSolution2008 : ASolution
	{
		public CSolution2008()
			: base()
		{
		}

		public override bool load(string slnFile)
		{
			m_projects.Clear();
			
			try
			{
				string slnFolder = Path.GetDirectoryName(slnFile);
				using (StreamReader reader = new StreamReader(slnFile))
				{
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						Match matchProjInfo = s_kRegex.Match(line);
						if (matchProjInfo.Success)
						{
							string projFilePath = combinePath(slnFolder, matchProjInfo.Groups[3].Value);
							string ext = Path.GetExtension(projFilePath);
							AProject project = null;
							if (ext == ".vcproj")
							{
								project = new CProject2008(matchProjInfo.Groups[2].Value, projFilePath);
							}
							else if (ext == ".vcxproj")
							{
								project = new CProject2013(matchProjInfo.Groups[2].Value, projFilePath, slnFolder);
							}
                            if (project != null)
							m_projects.Add(project);
						}
					}
				}

				return true;
			}
			catch (Exception)
			{
				m_projects.Clear();
				return false;
			}
		}

		private static string combinePath(string path1, string path2)
		{
			return Path.GetFullPath(Path.Combine(path1, path2));
		}

		private static readonly Regex s_kRegex = new Regex(@"Project\(""\{(.*)\}""\) = ""(.*)"", ""(.*)"", ""\{(.*)\}""");
	}
}
