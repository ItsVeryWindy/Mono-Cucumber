using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CucumberBinding.Parser
{
	public class GivenWhenThen : Item
	{
		public Table Table {
			get;
			private set;
		}

		public DocString DocString {
			get;
			private set;
		}

		public ReadOnlyCollection<Placeholder> Placeholders {
			get;
			private set;
		}

		public GivenWhenThen (IList<Placeholder> placeholders, Table table, DocString docString, string name, string file, int line, int column) : base(name, file, line, column)
		{
			Placeholders = new ReadOnlyCollection<Placeholder> (placeholders);
			Table = table;
			DocString = docString;
		}
	}
}

