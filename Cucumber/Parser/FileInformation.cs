using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MonoDevelop.Projects;
using System.Resources;
using System.Globalization;

namespace CucumberBinding.Parser
{
	public class FileInformation
	{
		private List<Feature> _features = new List<Feature>();

		private string Language {
			get;
			set;
		}

		public ResourceSet GetLanguage()
		{
			return Languages.ResourceManager.GetResourceSet(new CultureInfo(Language), true, true);
		}

		public ICollection<Feature> Features {
			get;
			set;
		}

		public FileInformation ()
		{
			Features = _features;
			Language = "en";
		}
	}
}

