using System.Collections.Generic;

namespace win2tiz.visualc
{
	public abstract class ASolution
	{
		public ASolution()
		{
			m_projects = new List<AProject>();
		}

		public abstract bool load(string slnFile);

		public AProject getProject(string projectName)
		{
			foreach (AProject proj in m_projects)
			{
				if (proj.getName() == projectName)
				{
					proj.load();
					return proj;
				}
			}
			return null;
		}

		public bool hasProject(string projectName)
		{
			foreach (AProject proj in m_projects)
			{
				if (proj.getName() == projectName)
				{
					return true;
				}
			}
			return false;
		}

		protected List<AProject> m_projects;
	}
}
