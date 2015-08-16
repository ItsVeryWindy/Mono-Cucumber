using System;
using System.Collections.Generic;

namespace CucumberBinding.Parser
{
	public class Background : Scenario
	{
		public Background (IList<GivenWhenThen> actions, string file, int line, int column) : base(actions, "Background", file, line, column)
		{
		}
	}
}

