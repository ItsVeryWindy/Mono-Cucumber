using System;

namespace CucumberBinding.Parser
{
	public class DocString : Item
	{
		public string Content {
			get;
			private set;
		}

		public int Lines {
			get;
			private set;
		}

		public DocString (string content, int lines, string file, int line, int column) : base("DocString", file, line, column)
		{
			Content = content;
			Lines = lines;
		}
	}
}

