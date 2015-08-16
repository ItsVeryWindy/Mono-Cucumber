using System;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;

namespace CucumberBinding
{
	[System.ComponentModel.Category("Cucumber")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class GeneralOptionsPanel : Gtk.Bin
	{
		public GeneralOptionsPanel ()
		{
			this.Build ();

			cucumberEntry.Text = PropertyService.Get<string> ("CucumberBinding.CucumberExecutable", "cucumber");
		}

		public bool Store ()
		{
			PropertyService.Set ("CucumberBinding.CucumberExecutable", cucumberEntry.Text.Trim ());
			PropertyService.SaveProperties ();

			return true;
		}

		protected virtual void OnCucumberBrowseClicked (object sender, EventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog (GettextCatalog.GetString ("Choose cucumber executable"), Gtk.FileChooserAction.Open);
			if (dialog.Run ())
				cucumberEntry.Text = dialog.SelectedFile;
		}
	}
			
	public class GeneralOptionsPanelBinding : OptionsPanel
	{
		private GeneralOptionsPanel panel;

		public override Gtk.Widget CreatePanelWidget ()
		{
			panel = new GeneralOptionsPanel ();
			return panel;
		}

		public override void ApplyChanges ()
		{
			panel.Store ();
		}
	}
}

