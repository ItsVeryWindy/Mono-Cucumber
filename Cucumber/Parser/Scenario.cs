using MonoDevelop.Projects;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CucumberBinding.Parser
{
	public class Scenario : Item
	{
		public ReadOnlyCollection<GivenWhenThen> Actions {
			get;
			private set;
		}

		public Scenario (IList<GivenWhenThen> actions, string name, string file, int line, int column) : base(name, file, line, column)
		{
			Actions = new ReadOnlyCollection<GivenWhenThen> (actions);
		}
	}
}

