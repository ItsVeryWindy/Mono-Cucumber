using System.Collections.Generic;

namespace CucumberBinding.Parser
{
	public class ScenarioOutline : Scenario
	{
		public Examples Examples {
			get;
			private set;
		}

		public ScenarioOutline (Examples examples, IList<Step> actions, IList<string> tags, string name, string file, int line, int column) : base(actions, tags, name, file, line, column)
		{
			Examples = examples;
		}
	}
}

