using System;

namespace CucumberBinding.Parser
{
	public class Placeholder : Item
	{
		public Placeholder (string name, string file, int line, int column) : base(name, file, line, column)
		{
		}
	}
}

