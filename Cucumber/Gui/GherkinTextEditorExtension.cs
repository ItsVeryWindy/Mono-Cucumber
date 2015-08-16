using System;
using MonoDevelop.Ide.Gui.Content;
using Mono.TextEditor;
using System.Collections.Generic;
using MonoDevelop.Components;
using MonoDevelop.Ide.TypeSystem;
using System.Linq;

namespace CucumberBinding
{
	public class GherkinTextEditorExtension : CompletionTextEditorExtension, IPathedDocument
	{
		public override string CompletionLanguage {
			get {
				return "Gherkin";
			}
		}

		public override void TextChanged (int startIndex, int endIndex)
		{
			base.TextChanged (startIndex, endIndex);

			var line = Editor.Document.GetLine (Editor.Caret.Line);
			string lineText = Editor.GetLineText (Editor.Caret.Line);
			int lineCursorIndex = Math.Min (lineText.Length, Editor.Caret.Column);
		}
			
		int NextTableSeparator(string lineText, int start)
		{
			int nextPipe = -1;

			for (int j = start; j < lineText.Length; j++) {
				if (lineText [j] == '|' && (start == 0 || lineText [j - 1] != '\\')) {
					nextPipe = j;
					break;
				}
			}

			return nextPipe;
		}

		public int LastWhitespaceCharacterOffset(string lineText, int start, int end)
		{
			int j = start;

			while (j > end && char.IsWhiteSpace (lineText [j - 1]))
				j--;

			return j;
		}

		public bool IsTable(string lineText)
		{
			int lineCursorIndex = Math.Min (lineText.Length, Editor.Caret.Column);

			for (int i = 0; i < lineCursorIndex; i++) {
				if (!char.IsWhiteSpace (lineText [i])) {
					if (lineText [i] == '|')
						return true;

					return false;
				}
			}

			return false;
		}

		public int TableSeparatorIndex(string lineText, int separatorOffset)
		{
			int index = 0;

			for (int i = 0; i < separatorOffset; i++) {
				if (lineText [i] == '|' && (i == 0 || lineText [i - 1] != '\\')) {
					index++;
				}
			}

			return index;
		}

		public List<int> TableSeparatorOffsets(string lineText, int separatorIndex)
		{
			List<int> offsets = new List<int> ();

			for (int i = 0; i < lineText.Length; i++) {
				if (lineText [i] == '|' && (i == 0 || lineText [i - 1] != '\\')) {
					offsets.Add (i);

					if (offsets.Count - 1 == separatorIndex)
						break;
				}
			}

			return offsets;
		}

		public Dictionary<DocumentLine, List<int>> TableLineOffsets(DocumentLine line, int separatorIndex)
		{
			Dictionary<DocumentLine, List<int>> offsets = new Dictionary<DocumentLine, List<int>>();

			offsets.Add(line, TableSeparatorOffsets(Editor.GetLineText(line.LineNumber), separatorIndex));

			DocumentLine previous = line.PreviousLine;

			while (previous != null) {
				string lineText = Editor.GetLineText(previous.LineNumber);

				if(IsTable(lineText))
				{
					offsets.Add(previous, TableSeparatorOffsets(lineText, separatorIndex));
				}
				else
				{
					break;
				}

				previous = previous.PreviousLine;
			}

			DocumentLine next = line.NextLine;

			while (next != null) {
				string lineText = Editor.GetLineText(next.LineNumber);

				if(IsTable(lineText))
				{
					offsets.Add(next, TableSeparatorOffsets(lineText, separatorIndex));
				}
				else
				{
					break;
				}

				next = next.NextLine;
			}

			return offsets;
		}

		public void CorrectLinePadding(DocumentLine currentLine, int currentIndex, Dictionary<DocumentLine, List<int>> lineOffsets)
		{
			int i = 0;

			var lines = lineOffsets.Where (e => i < e.Value.Count).ToList();

			var currentLineOffset = lineOffsets [currentLine];

			var caret = Editor.Caret.Column;

			while (lines.Count > 0)
			{
				int minOffset = lines.Max (e => LastWhitespaceCharacterOffset (Editor.GetLineText (e.Key.LineNumber), e.Value [i], i - 1 >= 0 ? e.Value [i - 1] : 0) + (i > 0 ? 1 : 0));

				minOffset = Math.Max(minOffset, currentLineOffset [i]);

				foreach (var line in lines) {
					int offset = line.Value [i];

					int numOfSpaces = minOffset - offset;

					if (numOfSpaces > 0) {
						Editor.Insert (line.Key.Offset + offset, new string (' ', numOfSpaces));
					} else {
						Editor.Remove (line.Key.Offset + offset + numOfSpaces, -numOfSpaces);
					}

					Editor.Document.CommitLineUpdate (line.Key);
				}

				i++;

				lines = lineOffsets.Where (e => i < e.Value.Count).ToList();
			}

			Editor.Caret.Column = caret;
		}

		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			if (Editor.IsSomethingSelected && Editor.IsMultiLineSelection) {
				return base.KeyPress (key, keyChar, modifier);
			}

			var line = Editor.Document.GetLine (Editor.Caret.Line);
			string lineText = Editor.GetLineText (Editor.Caret.Line);
			int pipeIndex = -1;
			bool changeMade = false;

			if(Editor.Caret.Offset - line.Offset + 1 < line.Length && IsTable(lineText))
			{
				int start = (Editor.IsSomethingSelected ? Editor.SelectionRange.EndOffset : Editor.Caret.Offset) - line.Offset;
				int end = (Editor.IsSomethingSelected ? Editor.SelectionRange.Offset : Editor.Caret.Offset) - line.Offset;
				int nextPipe = NextTableSeparator(lineText, end);

				if(nextPipe >= 0) {

					pipeIndex = TableSeparatorIndex (lineText, nextPipe);

					int j = LastWhitespaceCharacterOffset(lineText, nextPipe, end);

					int numberOfWhiteSpace = nextPipe - j;

					if (keyChar > 0) {
						int sLength = Editor.SelectionRange.Length;

						if (sLength >= 1) {
							Editor.Insert (line.Offset + nextPipe, new string(' ', sLength - 1));
							Editor.Document.CommitLineUpdate (line);
						} else {
							if (numberOfWhiteSpace > 1) {
								Editor.Remove (line.Offset + nextPipe - 1, 1);
								Editor.Document.CommitLineUpdate (line);
							}
						}

						changeMade = true;
					} else if (key == Gdk.Key.BackSpace && start >= 0 && start <= j) {
						
						int sLength = Editor.SelectionRange.Length;

						if (sLength >= 1) {
							Editor.Insert (line.Offset + nextPipe, new string(' ', sLength));
						} else if(start != nextPipe && (start + 1 == nextPipe && !char.IsWhiteSpace(lineText[nextPipe - 1]))) {
							Editor.Insert (line.Offset + nextPipe, " ");
						}

						Editor.Document.CommitLineUpdate (line);

						changeMade = true;
					}
				}
			}

			bool result = base.KeyPress (key, keyChar, modifier);

			if(pipeIndex >= 0 && changeMade)
			{
				var lineOffsets = TableLineOffsets (line, pipeIndex);

				CorrectLinePadding (line, pipeIndex, lineOffsets);
			}

			return result;
		}

		public event EventHandler<DocumentPathChangedEventArgs> PathChanged;

		public Gtk.Widget CreatePathWidget (int index)
		{
			PathEntry[] path = CurrentPath;

			if (null == path || 0 > index || path.Length <= index) {
				return null;
			}

			var tag = path[index].Tag;

			DropDownBoxListWindow.IListDataProvider provider = null;

			if (tag is ParsedDocument) {
				//provider = new CompilationUnitDataProvider (Document);
			} else {
				//provider = new DataProvider (Document, tag, GetAmbience ());
			}

			var window = new DropDownBoxListWindow (provider);

			window.SelectItem (tag);

			return window;
		}

		public PathEntry[] CurrentPath {
			get;
			private set;
		}

		protected virtual void OnPathChanged (DocumentPathChangedEventArgs args)
		{
			if (PathChanged != null)
				PathChanged (this, args);
		}

		public override void Initialize ()
		{
			base.Initialize ();
			//document.Editor.Caret.PositionChanged += UpdatePath;
			//Document.DocumentParsed += delegate { UpdatePath (null, null); };
		}
	}
}

