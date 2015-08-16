using MonoDevelop.Projects;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CucumberBinding.Parser
{
	public class Feature : Item
	{
		public ReadOnlyCollection<Scenario> Scenarios {
			get;
			private set;
		}

		public Feature (IList<Scenario> scenarios, string name, string file, int line, int column) : base(name, file, line, column)
		{
			Scenarios = new ReadOnlyCollection<Scenario> (scenarios);
		}
	}
}

