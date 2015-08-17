using System;

namespace CucumberBinding.Parser
{
	public class Item
	{
		public string Name {
			get;
			private set;
		}

		public int Line {
			get;
			private set;
		}

		public int Column {
			get;
			private set;
		}

		public string File {
			get;
			private set;
		}

		protected Item(string name, string file, int line, int column)
		{
			Name = name;
			File = file;
			Line = line;
			Column = column;
		}
	}
}

