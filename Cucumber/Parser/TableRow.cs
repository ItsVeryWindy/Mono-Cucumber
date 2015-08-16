using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CucumberBinding.Parser
{
	public class TableRow
	{
		readonly List<string> _cells = new List<string>();

		public ReadOnlyCollection<string> Cells {
			get;
			private set;
		}

		public TableRow (IList<string> cells)
		{
			Cells = new ReadOnlyCollection<string> (cells);
		}
	}
}

