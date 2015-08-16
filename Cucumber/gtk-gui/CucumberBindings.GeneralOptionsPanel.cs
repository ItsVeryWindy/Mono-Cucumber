
// This file has been generated by the GUI designer. Do not modify.
namespace CucumberBinding
{
	public partial class GeneralOptionsPanel
	{
		private global::Gtk.Table table1;
		
		private global::Gtk.Button cucumberBrowse;
		
		private global::Gtk.Entry cucumberEntry;
		
		private global::Gtk.Label label1;

		protected virtual void Build ()
		{
			global::Stetic.Gui.Initialize (this);
			// Widget CucumberBindings.GeneralOptionsPanel
			global::Stetic.BinContainer.Attach (this);
			this.Name = "CucumberBindings.GeneralOptionsPanel";
			// Container child CucumberBindings.GeneralOptionsPanel.Gtk.Container+ContainerChild
			this.table1 = new global::Gtk.Table (((uint)(1)), ((uint)(3)), false);
			this.table1.Name = "table1";
			this.table1.RowSpacing = ((uint)(6));
			this.table1.ColumnSpacing = ((uint)(6));
			// Container child table1.Gtk.Table+TableChild
			this.cucumberBrowse = new global::Gtk.Button ();
			this.cucumberBrowse.CanFocus = true;
			this.cucumberBrowse.Name = "cucumberBrowse";
			this.cucumberBrowse.UseUnderline = true;
			this.cucumberBrowse.Label = global::Mono.Unix.Catalog.GetString ("Browse");
			global::Gtk.Image w1 = new global::Gtk.Image ();
			w1.Pixbuf = global::Stetic.IconLoader.LoadIcon (this, "gtk-open", global::Gtk.IconSize.Menu);
			this.cucumberBrowse.Image = w1;
			this.table1.Add (this.cucumberBrowse);
			global::Gtk.Table.TableChild w2 = ((global::Gtk.Table.TableChild)(this.table1 [this.cucumberBrowse]));
			w2.LeftAttach = ((uint)(2));
			w2.RightAttach = ((uint)(3));
			w2.XOptions = ((global::Gtk.AttachOptions)(4));
			w2.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.cucumberEntry = new global::Gtk.Entry ();
			this.cucumberEntry.CanFocus = true;
			this.cucumberEntry.Name = "cucumberEntry";
			this.cucumberEntry.IsEditable = true;
			this.cucumberEntry.InvisibleChar = '●';
			this.table1.Add (this.cucumberEntry);
			global::Gtk.Table.TableChild w3 = ((global::Gtk.Table.TableChild)(this.table1 [this.cucumberEntry]));
			w3.LeftAttach = ((uint)(1));
			w3.RightAttach = ((uint)(2));
			w3.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label1 = new global::Gtk.Label ();
			this.label1.Name = "label1";
			this.label1.Xpad = 10;
			this.label1.Xalign = 1F;
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString ("Cucumber: ");
			this.label1.Justify = ((global::Gtk.Justification)(1));
			this.table1.Add (this.label1);
			global::Gtk.Table.TableChild w4 = ((global::Gtk.Table.TableChild)(this.table1 [this.label1]));
			w4.XOptions = ((global::Gtk.AttachOptions)(4));
			w4.YOptions = ((global::Gtk.AttachOptions)(4));
			this.Add (this.table1);
			if ((this.Child != null)) {
				this.Child.ShowAll ();
			}
			this.Hide ();
			this.cucumberBrowse.Clicked += new global::System.EventHandler (this.OnCucumberBrowseClicked);
		}
	}
}