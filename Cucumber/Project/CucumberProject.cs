using MonoDevelop.Projects;
using System.Xml;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;
namespace CucumberBinding
{
	[DataInclude(typeof(CucumberProjectConfiguration))]
	public class CucumberProject : Project
	{
		public CucumberProject ()
		{
		}

		public CucumberProject (ProjectCreateInformation info,
			XmlElement projectOptions)
		{
			if (info != null) {
				Name = info.ProjectName;
			}

			CucumberProjectConfiguration configuration =
				(CucumberProjectConfiguration)CreateConfiguration ("Default");

			Configurations.Add (configuration);
		}

		public override string[] SupportedLanguages {
			get { return new [] { "Gherkin" }; }
		}
	
		public override bool IsCompileable (string fileName)
		{
			return false;
		}

		protected override BuildResult DoBuild (IProgressMonitor monitor, ConfigurationSelector configuration)
		{
			return new BuildResult ();
		}

		protected override bool OnGetCanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			return false;
		}

		public override SolutionItemConfiguration CreateConfiguration (string name)
		{
			CucumberProjectConfiguration conf = new CucumberProjectConfiguration ();

			conf.Name = name;

			return conf;
		}

		public override System.Collections.Generic.IEnumerable<string> GetProjectTypes ()
		{
			yield return "Native";
		}
	}
}

