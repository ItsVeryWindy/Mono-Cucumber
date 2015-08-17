using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CucumberBinding.Parser
{
	public class TaggedItem : Item
	{
		public ReadOnlyCollection<string> Tags {
			get;
			private set;
		}

		protected TaggedItem (IList<string> tags, string name, string file, int line, int column) : base(name, file, line, column)
		{
			Tags = new ReadOnlyCollection<string> (tags);
		}
	}
}

