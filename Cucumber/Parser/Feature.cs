using MonoDevelop.Projects;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CucumberBinding.Parser
{
	public class Feature : TaggedItem
	{
		public ReadOnlyCollection<Scenario> Scenarios {
			get;
			private set;
		}

		public Feature (IList<Scenario> scenarios, IList<string> tags, string name, string file, int line, int column) : base(tags, name, file, line, column)
		{
			Scenarios = new ReadOnlyCollection<Scenario> (scenarios);
		}
	}
}

