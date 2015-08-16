using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace CucumberBinding.Parser
{
	public class Table : Item
	{
		public TableHeader Header {
			get;
			private set;
		}

		public ReadOnlyCollection<TableRow> Rows {
			get;
			private set;
		}

		public Table (TableHeader header, IList<TableRow> rows, string file, int line, int column) : base("Table", file, line, column)
		{
			Header = header;
			Rows = new ReadOnlyCollection<TableRow> (rows);
		}
	}
}

