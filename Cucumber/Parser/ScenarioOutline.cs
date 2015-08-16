using System.Collections.Generic;

namespace CucumberBinding.Parser
{
	public class ScenarioOutline : Scenario
	{
		public Examples Examples {
			get;
			private set;
		}

		public ScenarioOutline (Examples examples, IList<GivenWhenThen> actions, string name, string file, int line, int column) : base(actions, name, file, line, column)
		{
			Examples = examples;
		}
	}
}

