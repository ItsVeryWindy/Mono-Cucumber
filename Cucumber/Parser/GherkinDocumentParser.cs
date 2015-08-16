using System;
using MonoDevelop.Ide.TypeSystem;
using System.IO;
using MonoDevelop.Projects;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using MonoDevelop.Core;
using ICSharpCode.NRefactory.TypeSystem;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace CucumberBinding.Parser
{
	public class GherkinDocumentParser : TypeSystemParser
	{
		public override ParsedDocument Parse (bool storeAst, string fileName, TextReader reader, Project project = null)
		{
			var doc = new DefaultParsedDocument (fileName);
			doc.Flags |= ParsedDocumentFlags.NonSerializable;

			ProjectInformation pi = ProjectInformationManager.Instance.Get (project);

			var content = reader.ReadToEnd ();

			var contentLines = content.Split (new []{Environment.NewLine}, StringSplitOptions.None);

			lock(pi)
			{
				var file = pi.GetFile(fileName);

				var features = new List<Feature> ();

				AddFeature (doc, fileName, features, doc.TopLevelTypeDefinitions, contentLines);

				file.Features = features;
			}

			return doc;
		}

		static void AddFeature (ParsedDocument doc, string nameSpace, List<Feature> features, IList<IUnresolvedTypeDefinition> collection, string[] contentLines)
		{
			int line;
			int startColumn;
			string keyword;

			for (var i = 0; i < contentLines.Length; i++) {
				if (!FindItemStart (contentLines, i, contentLines.Length, false, true, out line, out startColumn, out keyword, "Feature:")) {
					return;
				}

				var scenarios = new List<Scenario> ();

				var feature = new Feature (scenarios, contentLines[line - 1].Substring(startColumn + keyword.Length - 1).Trim(), doc.FileName, line, startColumn);

				features.Add (feature);

				var newNameSpace = nameSpace + "." + feature.Line;

				var def = new DefaultUnresolvedTypeDefinition (newNameSpace, feature.Name);

				i = FindItemEnd (contentLines, feature.Line, contentLines.Length, true, "Feature:") - 1;

				AddScenario (doc, newNameSpace, feature.Line, i + 1, scenarios, def.NestedTypes, contentLines);

				def.Region = new DomRegion ((int)feature.Line, 1, i + 2, 1);
				def.Kind = TypeKind.Class;

				collection.Add (def);
			}
		}

		static void AddScenario (ParsedDocument doc, string nameSpace, int startLine, int endLine, List<Scenario> scenarios, IList<IUnresolvedTypeDefinition> collection, string[] contentLines)
		{
			int line;
			int startColumn;
			string keyword;

			for (var i = startLine; i < endLine; i++) {
				if (!FindItemStart (contentLines, i, endLine, true, true, out line, out startColumn, out keyword, "Background:", "Scenario Outline:", "Scenario:")) {
					return;
				}
					
				i = FindItemEnd (contentLines, line, endLine, true, "Background:", "Scenario Outline:", "Scenario:") - 1;

				var actions = new List<GivenWhenThen> ();

				var def = new DefaultUnresolvedTypeDefinition (nameSpace + "." + line, "");

				def.Region = new DomRegion (line, 1, i + 2, 1);
				def.Kind = TypeKind.Class;

				int endSteps;

				var result = AddSteps (doc, keyword == "Scenario Outline:", line, i + 1, actions, def, contentLines, out endSteps);

				Scenario scenario = null;

				switch (keyword) {
					case "Scenario Outline:":
						var examples = result ? AddExamples (doc, endSteps, i + 1, contentLines) : null;

						if (examples == null && actions.Count > 0 && result) {
							doc.Errors.Add (new Error (ErrorType.Error, "Expecting Examples for Scenario Outline", new DomRegion(line, startColumn, line, startColumn + keyword.Length)));
						}

						if(examples != null && examples.Table != null && examples.Table.Header != null)
						{
							foreach(var j in actions) {
								foreach(var k in j.Placeholders) {
									if (!examples.Table.Header.Columns.Contains (k.Name)) {
										doc.Errors.Add(new Error(ErrorType.Warning, "Placeholder does not match column in table", new DomRegion(k.Line, k.Column, line, k.Column + k.Name.Length)));
									}
								}
							}
						}

						scenario = new ScenarioOutline (examples, actions, contentLines [line - 1].Substring (startColumn + keyword.Length - 1).Trim (), doc.FileName, line, startColumn);
						break;
					case "Scenario:":
						scenario = new Scenario (actions, contentLines [line - 1].Substring (startColumn + keyword.Length - 1).Trim (), doc.FileName, line, startColumn);
						break;
					case "Background:":
						scenario = new Background (actions, doc.FileName, line, startColumn);
						break;
				}

				scenarios.Add (scenario);

				collection.Add (def);
			}
		}

		static Regex parameterExpression = new Regex ("<([^>]*)>", RegexOptions.Compiled);

		static bool AddSteps (ParsedDocument doc, bool isScenarioOutline, int startLine, int endLine, List<GivenWhenThen> actions, IUnresolvedTypeDefinition type, string[] contentLines, out int endSteps)
		{
			int line;
			int startColumn;
			string keyword;
			endSteps = 0;

			for (var i = startLine; i < endLine; i++) {
				if (i > startLine && isScenarioOutline) {
					if (!FindItemStart (contentLines, i, endLine, false, true, out line, out startColumn, out keyword, "Given", "When", "Then", "Examples:", "And", "But")) {
						doc.Errors.Add (new Error (ErrorType.Error, "Expected Given, When or Then", new DomRegion(line, 1, line, 1 + contentLines[line - 1].Length)));
						return false;
					}

					if (keyword == "Examples:") {
						endSteps = line - 1;
						return true;
					}
				} else {
					if (!FindItemStart (contentLines, i, endLine, false, true, out line, out startColumn, out keyword, "Given", "When", "Then")) {
						doc.Errors.Add (new Error (ErrorType.Error, "Expected Given, When or Then", new DomRegion(line, 1, line, 1 + contentLines[line - 1].Length)));
						return false;
					}
				}

				i = FindItemEnd (contentLines, line, endLine, false, "Examples:", "Given", "When", "Then") - 1;

				var table = AddTable (doc, line, endLine, contentLines);

				if (table != null) {
					if (table.Rows != null && table.Header != null && table.Rows.Any (e => e.Cells.Count != table.Header.Columns.Count)) {
						doc.Errors.Add (new Error (ErrorType.Error, "Table is uneven", new DomRegion (table.Line, table.Column, table.Line, 1 + contentLines [table.Line - 1].Length)));
					}

					if (table.Header != null)
						i++;
					if (table.Rows != null)
						i += table.Rows.Count;
				}

				var docString = table == null ? AddDocString(doc, line, contentLines) : null;

				if (docString != null) {
					i += docString.Lines + 2;
				}

				var placeholders = new List<Placeholder>();

				var matches = parameterExpression.Matches(contentLines[line - 1]);

				foreach (Match match in matches) {
					var p = match.Groups [1];

					placeholders.Add (new Placeholder(p.ToString(), doc.FileName, line, p.Index + 1));
				}

				var action = new GivenWhenThen(placeholders, table, docString, contentLines [line - 1].Substring (startColumn + keyword.Length - 1).Trim (), doc.FileName, line, startColumn);

				actions.Add (action);

				var def = new DefaultUnresolvedMethod (type, action.Name);

				def.Region = new DomRegion (action.Line, 1, i + 2, 1);

				type.Members.Add (def);
			}

			endSteps = endLine;

			return true;
		}

		static Examples AddExamples (ParsedDocument doc, int startLine, int endLine, string[] contentLines)
		{
			int line;
			int startColumn;
			string keyword;

			if (!FindItemStart (contentLines, startLine, endLine, false, true, out line, out startColumn, out keyword, "Examples:")) {
				return null;
			}

			var table = AddTable (doc, line, endLine, contentLines);

			if (table == null) {
				doc.Errors.Add(new Error(ErrorType.Error, "Expecting Table for Examples", line, startColumn));
			}
			else if (table.Rows != null && table.Header != null && table.Rows.Any (e => e.Cells.Count != table.Header.Columns.Count)) {
				doc.Errors.Add (new Error (ErrorType.Error, "Table is uneven", new DomRegion (table.Line, table.Column, table.Line, 1 + contentLines [table.Line - 1].Length)));
			}

			var examples = new Examples (table, doc.FileName, line, startColumn);
		
			return examples;
		}

		static Table AddTable (ParsedDocument doc, int startLine, int endLine, string[] contentLines)
		{
			int line;
			int startColumn;
			string keyword;

			if (!FindItemStart (contentLines, startLine, endLine, false, false, out line, out startColumn, out keyword, "|")) {
				return null;
			}

			var header = AddTableHeader (contentLines[line - 1]);

			var rows = new List<TableRow> ();

			for (var i = line; i < endLine; i++) {
				var row = AddTableRow (contentLines[i]);

				if (row == null)
					break;

				rows.Add (row);
			}

			var table = new Table (header, rows, doc.FileName, line, startColumn);

			return table;
		}

		static bool GetNextCell(string line, int start, out int index)
		{
			index = 0;

			for (int i = start; i < line.Length; i++) {
				if (line [i] == '|' && (i == 0 || line [i - 1] != '\\')) {
					index = i;
					return true;
				}
			}

			return false;
		}

		static TableHeader AddTableHeader(string line)
		{
			var columns = new List<string> ();

			int a;
			int b;

			if (!GetNextCell (line, 0, out a)) {
				return null;
			}

			a++;

			for (; a < line.Length; a++) {
				if (!GetNextCell (line, a, out b)) {
					return null;
				}

				columns.Add (line.Substring (a, b - a).Trim());

				a = b;
			}

			return new TableHeader (columns);
		}

		static TableRow AddTableRow(string line)
		{
			var cells = new List<string> ();

			int a;
			int b;

			if (!GetNextCell (line, 0, out a)) {
				return null;
			}

			a++;

			for (; a < line.Length; a++) {
				if (!GetNextCell (line, a, out b)) {
					return null;
				}

				cells.Add (line.Substring (a, b - a).Trim());

				a = b;
			}

			return new TableRow (cells);
		}

		static DocString AddDocString (ParsedDocument doc, int startLine, string[] contentLines)
		{
			int line;
			int startColumn;
			string keyword;

			if (!FindItemStart (contentLines, startLine, startLine + 1, false, true, out line, out startColumn, out keyword, "\"\"\"")) {
				return null;
			}

			var end = FindItemEnd (contentLines, line, contentLines.Length, true, "\"\"\"");

			var content = string.Join (Environment.NewLine, contentLines, line, end - line);

			var docString = new DocString (content, end - line, doc.FileName, line, startColumn);

			return docString;
		}

//		static void AddAction (DefaultUnresolvedTypeDefinition parent, GivenWhenThen action, string[] contentLines)
//		{
//			var def = new DefaultUnresolvedMethod(parent, action.Name);
//
//			def.Region = new DomRegion ((int)action.Line, 1, FindItemEnd (contentLines, action.Line, "Given", "When", "Then", "And", "But", "Scenario:", "Scenario Outline:", "Background:", "Examples:"), 1);
//
//			AddParameters(action, def);
//
//			parent.Members.Add (def);
//		}

		static void AddPlaceholders(GivenWhenThen action, IUnresolvedMethod method)
		{
			var parameters = new List<IUnresolvedParameter> ();

			int index = 0;

			foreach (var placeholder in action.Placeholders) {
				var typeRef = new DefaultUnresolvedTypeDefinition (placeholder.Name);

				var p =  new DefaultUnresolvedParameter (typeRef, "p" + index);

				method.Parameters.Add (p);

				index++;
			}
		}

//		static Regex languageRegex = new Regex ("# language: ([a-z]){2}", RegexOptions.Compiled);
//
//		static ResourceSet GetLanguage(string line, CucumberProject)
//		{
//			var match = languageRegex.Match (line);
//
//			string 
//
//			if (match.Success) {
//			}
//
//			Languages.ResourceManager.GetResourceSet(new CultureInfo(), true, true);
//		}

		static bool FindItemStart (string[] content, int startLine, int endLine, bool cont, bool isWord, out int line, out int startColumn, out string keyword, params string[] keywords) {
			line = 0;
			startColumn = 0;
			keyword = null;

			for (var i = startLine; i < endLine; i++)
			{
				string lineStr = content [i];

				line = i + 1;

				for(int j = 0 ; j < lineStr.Length; j++)
				{
					char c = lineStr [j];

					if (char.IsWhiteSpace (c))
						continue;

					if (c == '#')
						break;

					foreach(string key in keywords)
					{
						if ((!isWord || j + key.Length >= lineStr.Length || char.IsWhiteSpace(lineStr[j + key.Length])) && j + key.Length <= lineStr.Length && lineStr.IndexOf (key, j, key.Length) >= 0) {
							startColumn = j + 1;
							keyword = key;
							return true;
						}
					}

					if(!cont)
						return false;

					break;
				}	
			}

			return false;
		}

		static int FindItemEnd (string[] content, int startLine, int endLine, bool cont, params string[] keywords) {
			for (var i = startLine; i < endLine; i++)
			{
				string line = content [i];

				for(int j = 0 ; j < content[i].Length; j++)
				{
					char c = line [j];

					if (char.IsWhiteSpace (c))
						continue;

					if (c == '#')
						break;

					foreach(string keyword in keywords)
					{
						if (j + keyword.Length <= line.Length && line.IndexOf (keyword, j, keyword.Length) >= 0) {
							return i;
						}
					}
						
					if(!cont)
						return i;

					break;
				}	
			}

			return endLine;
		}
	}
}

