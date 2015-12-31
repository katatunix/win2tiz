using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace win2tiz.visualc
{
	public class CProject2013 : AProject
	{
		public CProject2013(string name, string file, string slnDir)
			: base(name, file)
		{
			m_slnDir = slnDir;

			m_mainXml = new XmlDocument();
			m_filtersXml = new XmlDocument();

			m_projectDir = Path.GetDirectoryName(m_file) + "\\";
			m_hasLoad = false;
		}

		public override bool load()
		{
			if (m_hasLoad) return true;
			try
			{
				m_mainXml.Load(m_file);
				m_filtersXml.Load(m_file + ".filters");
				m_hasLoad = true;
				return true;
			}
			catch (Exception)
			{
				m_hasLoad = false;
				return false;
			}
		}

		public override List<string> getFiles(string config, List<string> ignoredFilters, bool enableExclude)
		{
			config = normalizeConfigName(config);
			return parseSrcFilesInDoc(m_mainXml, m_projectDir, config, enableExclude, ignoredFilters);
		}

		public override List<string> getIncludePaths(string config)
		{
			config = normalizeConfigName(config);
			return parseIncPathsInDoc(m_mainXml, m_projectDir, config);
		}

		//=======================================================================================================

		/// <summary>
		/// Preconditions: doc, dir must be valid
		/// 
		/// </summary>
		/// <param name="doc"></param>
		/// <param name="dir">Dir path which contains the doc</param>
		/// <param name="config"></param>
		/// <returns></returns>
		private List<string> parseIncPathsInDoc(XmlDocument doc, string dir, string config)
		{
			List<string> result = new List<string>();

			foreach (XmlNode importNode in doc.GetElementsByTagName("Import"))
			{
				if (isPassCondition(importNode, config, true))
				{
					XmlAttribute att = importNode.Attributes["Project"];
					if (att != null)
					{
						
						string importFilePath = combinePath( dir, escape(att.Value, config) );
						string importDirPath = Path.GetDirectoryName(importFilePath);

						XmlDocument importDoc = new XmlDocument();
						try
						{
							importDoc.Load(importFilePath);
						}
						catch (Exception)
						{
							importDoc = null;
						}

						if (importDoc != null)
						{
							result.AddRange(parseIncPathsInDoc(importDoc, importDirPath, config));
						}
					}
				}
			}

			foreach (XmlNode aidNode in doc.GetElementsByTagName("AdditionalIncludeDirectories"))
			{
				if (isPassCondition(aidNode, config, true))
				{
					string[] p = escape(aidNode.InnerText, config)
									.Replace("\"", "")
									.Replace("%(AdditionalIncludeDirectories)", "")
									.Split(s_kSeparateInc, StringSplitOptions.RemoveEmptyEntries);
					for (int i = 0; i < p.Length; i++)
					{
						string path = p[i].Trim();
						if (!string.IsNullOrEmpty(path) && path.IndexOf(" ") == -1 && path.IndexOf('$') == -1)
						{
							result.Add( combinePath(m_projectDir, p[i].Trim()) );
						}
					}
					
				}
			}
			
			return result;
		}

		private List<string> parseSrcFilesInDoc(XmlDocument doc, string dir, string config,
												bool enableExclude, List<string> ignoredFilters)
		{
			List<string> result = new List<string>();

			foreach (XmlNode importNode in doc.GetElementsByTagName("Import"))
			{
				if (isPassCondition(importNode, config, true))
				{
					XmlAttribute att = importNode.Attributes["Project"];
					if (att != null)
					{
						string importFilePath = combinePath( dir, escape(att.Value, config) );
						string importDirPath = Path.GetDirectoryName(importFilePath);

						XmlDocument importDoc = new XmlDocument();
						try
						{
							importDoc.Load(importFilePath);
						}
						catch (Exception)
						{
							importDoc = null;
						}

						if (importDoc != null)
						{
							result.AddRange(parseSrcFilesInDoc(importDoc, importDirPath, config,
																	enableExclude, ignoredFilters));
						}
					}
				}
			}

			foreach (XmlNode cNode in doc.GetElementsByTagName("ClCompile"))
			{
				if (isPassCondition(cNode, config, true))
				{
					if (cNode.Attributes == null) continue;

					XmlAttribute att = cNode.Attributes["Include"];
					if (att == null) continue;

					bool ok = true;

					// check Exclude
					if (enableExclude)
					{
						foreach (XmlNode nodeExcludedFromBuild in cNode.ChildNodes)
						{
							if (nodeExcludedFromBuild.Name != "ExcludedFromBuild") continue;
							if (!isPassCondition(nodeExcludedFromBuild, config, false)) continue;
							if (nodeExcludedFromBuild.InnerText.ToLower() == "true")
							{
								ok = false;
								break;
							}
						}
					}

					if (!ok) continue;

					string sourcePathFull = combinePath(dir, escape(att.Value, config));

					// check ignoredFilters
					if (ignoredFilters != null && ignoredFilters.Count > 0)
					{
						if (isFileBelongToFilters(sourcePathFull, ignoredFilters))
						{
							ok = false;
						}
					}

					if (!ok) continue;

					result.Add( sourcePathFull );
				}
			}
			
			return result;
		}

		private bool isFileBelongToFilters(string sourcePathFull, List<string> filters)
		{
			sourcePathFull = sourcePathFull.ToLower();
			// get filter path of this source file
			string filterPath = null;
			foreach (XmlNode nodeClCompile in m_filtersXml.GetElementsByTagName("ClCompile"))
			{
				string sPath = combinePath(m_projectDir, nodeClCompile.Attributes["Include"].Value).ToLower();
				if (sourcePathFull != sPath) continue;

				foreach (XmlNode nodeFilter in nodeClCompile.ChildNodes)
				{
					if (nodeFilter.Name != "Filter") continue;
					filterPath = nodeFilter.InnerText;
					break;
				}

				break;
			}

			// now check
			if (filterPath != null)
			{
				string[] sFilters = filterPath.Split(s_kSeparateFilter, StringSplitOptions.RemoveEmptyEntries);
				foreach (string f in sFilters)
				{
					if (filters.Contains(f))
					{
						return true;
					}
				}
			}

			return false;
		}

		private bool isPassCondition(XmlNode node, string config, bool bubble)
		{
			while (node != null)
			{
				if (node.Attributes != null)
				{
					XmlAttribute att = node.Attributes["Condition"];
					if (att != null)
					{
						string cond = escape(att.Value, config);
						int i = cond.IndexOf("==");
						if (i > -1)
						{
							string s1 = cond.Substring(0, i);
							string s2 = cond.Substring(i + 2);
							if (s1 != s2) return false;
						}
					}
				}
				node = bubble ? node.ParentNode : null;
			}
			
			return true;
		}

		private string escape(string str, string config)
		{
			return str.Replace("$(Configuration)", config)
				.Replace("$(Platform)", "Win32")
				.Replace("$(ProjectDir)", m_projectDir)
				.Replace("$(SolutionDir)", m_slnDir);
		}

		//=======================================================================================================
		// Utility methods

		private static string normalizeConfigName(string config)
		{
			int i = config.IndexOf('|');
			if (i > -1)
			{
				config = config.Substring(0, i);
			}
			return config;
		}

		private static string combinePath(string path1, string path2)
		{
			return Path.GetFullPath(Path.Combine(path1, path2));
		}

		//=======================================================================================================
		// Properties

		private XmlDocument m_mainXml;
		private XmlDocument m_filtersXml;
		private string m_projectDir;
		private string m_slnDir;

		private bool m_hasLoad;

		private static readonly char[] s_kSeparateInc = { ';' };
		private static readonly char[] s_kSeparateFilter = { '\\', '/' };
	}
}
