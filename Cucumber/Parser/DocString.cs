using System;

namespace CucumberBinding.Parser
{
	public class DocString : Item
	{
		public DocString (string file, int line, int column) : base("DocString", file, line, column)
		{
		}
	}
}

