using System.Collections.Generic;

namespace CucumberBinding.Parser
{
	public class ProjectInformation : FileInformation
	{
		private Dictionary<string, FileInformation> _files = new Dictionary<string, FileInformation> ();

		public ProjectInformation ()
		{
		}

		public FileInformation GetFile (string fileName)
		{
			FileInformation info;

			if (_files.TryGetValue (fileName, out info))
				return info;

			info = new FileInformation ();

			_files.Add (fileName, info);

			return info;
		}
	}
}

