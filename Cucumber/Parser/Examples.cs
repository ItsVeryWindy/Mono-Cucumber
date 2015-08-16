namespace CucumberBinding.Parser
{
	public class Examples : Item
	{
		public Table Table {
			get;
			private set;
		}

		public Examples (Table table, string file, int line, int column) : base("Examples", file, line, column)
		{
			Table = table;
		}
	}
}

