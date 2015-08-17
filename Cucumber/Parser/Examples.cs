using System.Collections.Generic;

namespace CucumberBinding.Parser
{
	public class Examples : TaggedItem
	{
		public Table Table {
			get;
			private set;
		}

		public Examples (Table table, IList<string> tags, string file, int line, int column) : base(tags, "Examples", file, line, column)
		{
			Table = table;
		}
	}
}

