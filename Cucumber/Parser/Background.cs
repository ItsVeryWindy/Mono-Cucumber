using System;
using System.Collections.Generic;

namespace CucumberBinding.Parser
{
	public class Background : Scenario
	{
		public Background (IList<Step> actions, IList<string> tags, string file, int line, int column) : base(actions, tags, "Background", file, line, column)
		{
		}
	}
}

