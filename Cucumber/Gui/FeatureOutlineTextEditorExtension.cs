using System;
using MonoDevelop.DesignerSupport;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.TypeSystem;
using Gtk;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;
using System.Collections.Generic;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.TypeSystem;

namespace CucumberBinding
{
	public class FeatureOutlineTextEditorExtension : TextEditorExtension, IOutlinedDocument
	{
		ParsedDocument lastCU = null;

		MonoDevelop.Ide.Gui.Components.PadTreeView outlineTreeView;
		TreeStore outlineTreeStore;

		bool refreshingOutline;
		bool disposed;
		bool outlineReady;

		public override bool ExtendsEditor (Document doc, IEditableTextBuffer editor)
		{
			var binding = LanguageBindingService.GetBindingPerFileName (doc.Name);
			return binding is GherkinLanguageBinding;
		}

		public override void Initialize ()
		{
			base.Initialize ();
			if (Document != null)
				Document.DocumentParsed += UpdateDocumentOutline;
		}

		public override void Dispose ()
		{
			if (disposed)
				return;
			disposed = true;
			if (Document != null)
				Document.DocumentParsed -= UpdateDocumentOutline;
			RemoveRefillOutlineStoreTimeout ();
			lastCU = null;
			base.Dispose ();
		}

		Widget IOutlinedDocument.GetOutlineWidget ()
		{
			if (outlineTreeView != null)
				return outlineTreeView;

			outlineTreeStore = new TreeStore (typeof(object));

			outlineTreeView = new MonoDevelop.Ide.Gui.Components.PadTreeView (outlineTreeStore);

			var pixRenderer = new CellRendererImage ();
			pixRenderer.Xpad = 0;
			pixRenderer.Ypad = 0;

			outlineTreeView.TextRenderer.Xpad = 0;
			outlineTreeView.TextRenderer.Ypad = 0;

			TreeViewColumn treeCol = new TreeViewColumn ();
			treeCol.PackStart (pixRenderer, false);

			treeCol.SetCellDataFunc (pixRenderer, new TreeCellDataFunc (OutlineTreeIconFunc));
			treeCol.PackStart (outlineTreeView.TextRenderer, true);

			treeCol.SetCellDataFunc (outlineTreeView.TextRenderer, new TreeCellDataFunc (OutlineTreeTextFunc));
			outlineTreeView.AppendColumn (treeCol);

			outlineTreeView.HeadersVisible = false;

			outlineTreeView.Selection.Changed += delegate {
				JumpToDeclaration (false);
			};

			outlineTreeView.RowActivated += delegate {
				JumpToDeclaration (true);
			};

			this.lastCU = Document.ParsedDocument;

			outlineTreeView.Realized += delegate { RefillOutlineStore (); };
			UpdateSorting ();

			var sw = new CompactScrolledWindow ();
			sw.Add (outlineTreeView);
			sw.ShowAll ();
			return sw;
		}

		IEnumerable<Widget> IOutlinedDocument.GetToolbarWidgets ()
		{
			return null;
		}

		void JumpToDeclaration (bool focusEditor)
		{
			if (!outlineReady)
				return;
			TreeIter iter;
			if (!outlineTreeView.Selection.GetSelected (out iter))
				return;

			object o = outlineTreeStore.GetValue (iter, 0);

//			IdeApp.ProjectOperations.JumpToDeclaration (o as IEntity);
			if (focusEditor)
				IdeApp.Workbench.ActiveDocument.Select ();
		}

		void OutlineTreeIconFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			var pixRenderer = (CellRendererImage)cell;
			object o = model.GetValue (iter, 0);
//			if (o is IEntity) {
//				pixRenderer.Image = ImageService.GetIcon (((IEntity)o).GetStockIcon (), IconSize.Menu);
//			} else if (o is FoldingRegion) {
//				pixRenderer.Image = ImageService.GetIcon (Ide.Gui.Stock.Add, IconSize.Menu);
//			}
		}

		void OutlineTreeTextFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			CellRendererText txtRenderer = (CellRendererText)cell;
			object o = model.GetValue (iter, 0);
			Ambience am = GetAmbience ();
//			if (o is IEntity) {
//				txtRenderer.Text = am.GetString ((IEntity)o, OutputFlags.ClassBrowserEntries);
//			} else if (o is FoldingRegion) {
//				string name = ((FoldingRegion)o).Name.Trim ();
//				if (string.IsNullOrEmpty (name))
//					name = "#region";
//				txtRenderer.Text = name;
//			}
		}

		void MonoDevelop.DesignerSupport.IOutlinedDocument.ReleaseOutlineWidget ()
		{
			if (outlineTreeView == null)
				return;
			var w = (ScrolledWindow)outlineTreeView.Parent;
			w.Destroy ();
			if (outlineTreeStore != null) {
				outlineTreeStore.Dispose ();
				outlineTreeStore = null;
			}
			outlineTreeView = null;
		}

		void RemoveRefillOutlineStoreTimeout ()
		{
			if (refillOutlineStoreId == 0)
				return;
			GLib.Source.Remove (refillOutlineStoreId);
			refillOutlineStoreId = 0;
		}

		uint refillOutlineStoreId;
		void UpdateDocumentOutline (object sender, EventArgs args)
		{
			lastCU = Document.ParsedDocument;
			//limit update rate to 3s
			if (!refreshingOutline) {
				refreshingOutline = true;
				refillOutlineStoreId = GLib.Timeout.Add (3000, RefillOutlineStore);
			}
		}

		bool RefillOutlineStore ()
		{
			DispatchService.AssertGuiThread ();
			Gdk.Threads.Enter ();
			refreshingOutline = false;
			if (outlineTreeStore == null || !outlineTreeView.IsRealized) {
				refillOutlineStoreId = 0;
				return false;
			}

			outlineReady = false;
			outlineTreeStore.Clear ();
			if (lastCU != null) {
				BuildTreeChildren (outlineTreeStore, TreeIter.Zero, lastCU);
				TreeIter it;
				if (outlineTreeStore.GetIterFirst (out it))
					outlineTreeView.Selection.SelectIter (it);

				outlineTreeView.ExpandAll ();
			}
			outlineReady = true;

			Gdk.Threads.Leave ();

			//stop timeout handler
			refillOutlineStoreId = 0;
			return false;
		}

		void BuildTreeChildren (TreeStore store, TreeIter parent, ParsedDocument parsedDocument)
		{
			if (parsedDocument == null)
				return;

			foreach (var unresolvedCls in parsedDocument.TopLevelTypeDefinitions) {
				TreeIter childIter;
				if (!parent.Equals (TreeIter.Zero))
					childIter = store.AppendValues (parent, unresolvedCls);
				else
					childIter = store.AppendValues (unresolvedCls);

				AddTreeClassContents (store, childIter, parsedDocument, unresolvedCls, unresolvedCls);
			}
		}

		static void AddTreeClassContents (TreeStore store, TreeIter parent, ParsedDocument parsedDocument, IUnresolvedTypeDefinition cls, IUnresolvedTypeDefinition part)
		{
			List<object> items = new List<object> ();
	
			foreach (var o in cls.NestedTypes) {
				items.Add (o);
			}

			TreeIter currentParent = parent;
			foreach (object item in items) {
				TreeIter childIter = store.AppendValues (currentParent, item);

				var unresolvedTypeDefinition = item as IUnresolvedTypeDefinition;

				if (unresolvedTypeDefinition != null)
					AddTreeClassContents (store, childIter, parsedDocument, unresolvedTypeDefinition, part);
			} 
		}

		void UpdateSorting ()
		{
			outlineTreeView.Model = outlineTreeStore;
			outlineTreeView.ExpandAll ();
		}
	}
}

