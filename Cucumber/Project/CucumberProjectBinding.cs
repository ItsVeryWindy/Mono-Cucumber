using System;
using MonoDevelop.Projects;
using System.Xml;
using System.IO;

namespace CucumberBinding
{
	public class CucumberProjectBinding : IProjectBinding
	{
		public Project CreateProject (ProjectCreateInformation info, XmlElement projectOptions)
		{
			return new CucumberProject (info, projectOptions);
		}

		public Project CreateSingleFileProject (string sourceFile)
		{
			var info = new ProjectCreateInformation () {
				ProjectName = Path.GetFileNameWithoutExtension (sourceFile),
				SolutionPath = Path.GetDirectoryName (sourceFile),
				ProjectBasePath = Path.GetDirectoryName (sourceFile),
			};

			Project project = new CucumberProject (info, null);
			project.Files.Add (new ProjectFile (sourceFile));
			return project;
		}

		public bool CanCreateSingleFileProject (string sourceFile)
		{
			return Path.GetExtension (sourceFile.ToLower ()) == ".feature";
		}

		public string Name {
			get {
				return "Cucumber";
			}
		}
	}
}

