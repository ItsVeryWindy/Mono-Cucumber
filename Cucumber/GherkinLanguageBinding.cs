using System;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace CucumberBinding
{
	public class GherkinLanguageBinding : ILanguageBinding
	{
		public string Language {
			get { return "Gherkin"; }
		}

		public string SingleLineCommentTag { get { return "#"; } }
		public string BlockCommentStartTag { get { return null; } }
		public string BlockCommentEndTag { get { return null; } }

		public bool IsSourceCodeFile (FilePath fileName)
		{
			return fileName.ToString ().EndsWith (".feature", StringComparison.OrdinalIgnoreCase);
		}

		public FilePath GetFileName (FilePath baseName)
		{
			return baseName + ".feature";
		}
	}
}

