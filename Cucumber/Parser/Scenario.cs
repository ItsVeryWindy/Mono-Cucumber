using MonoDevelop.Projects;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CucumberBinding.Parser
{
	public class Scenario : TaggedItem
	{
		public ReadOnlyCollection<Step> Actions {
			get;
			private set;
		}

		public Scenario (IList<Step> actions, IList<string> tags, string name, string file, int line, int column) : base(tags, name, file, line, column)
		{
			Actions = new ReadOnlyCollection<Step> (actions);
		}
	}
}

