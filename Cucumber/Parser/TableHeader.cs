using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CucumberBinding.Parser
{
	public class TableHeader
	{
		public ReadOnlyCollection<string> Columns {
			get;
			private set;
		}

		public TableHeader (IList<string> columns)
		{
			Columns = new ReadOnlyCollection<string> (columns);
		}
	}
}

