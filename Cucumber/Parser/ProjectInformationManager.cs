using System;
using System.Collections.Generic;
using MonoDevelop.Projects;

namespace CucumberBinding.Parser
{
	public class ProjectInformationManager
	{
		private static ProjectInformationManager instance;
		private Dictionary<Project, ProjectInformation> projects = new Dictionary<Project, ProjectInformation> ();
		private ProjectInformation _nullProject;

		private ProjectInformationManager ()
		{
		}

		public ProjectInformation Get (Project project)
		{
			if (project == null) {
				if(_nullProject == null) _nullProject = new ProjectInformation ();

				return _nullProject;
			}

			ProjectInformation info;

			if (projects.TryGetValue (project, out info)) {
				return info;
			}

			ProjectInformation newinfo = new ProjectInformation ();

			projects.Add (project, newinfo);

			return newinfo;
		}

		public static ProjectInformationManager Instance {
			get {
				if (instance == null)
					instance = new ProjectInformationManager ();

				return instance;
			}
		}
	}
}

