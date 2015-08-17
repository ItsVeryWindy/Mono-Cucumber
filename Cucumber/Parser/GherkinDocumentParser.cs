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
		class ParserContext
		{
			public IList<Feature> Features { get; set; }
			public IList<string> Tags { get; set; }
			public Func<Feature> Feature { get; set; }
			public IList<Scenario> Scenarios { get; set; }
			public int Column { get; set; }
			public bool IsWord { get; set; }
			public string Keyword { get; set; }
			public int LineNumber { get; set; }
			public string Content { get; set; }
			public string FileName { get; set; }
			public IList<Error> Errors { get; set; }
			public Func<Scenario> Scenario { get; set; }
			public Func<Examples> Examples { get; set; }
			public IList<Step> Steps { get; set; }
			public Func<Table> Table { get; set; }
			public Func<Step> Step { get; set; }
			public Func<DocString> DocString { get; set; }
			public List<TableRow> TableRows { get; set; }

			public string RemainingContent {
				get {
					return Content.Substring (Column + Keyword.Length - 1).Trim ();
				}
			}
		}

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

				var keys = new List<Tuple<Func<ParserContext, bool>, Func<ParserContext, bool>, HashSet<string>>> ();

				AddParser (keys, e => e.DocString == null, AddTag, "@");
				AddParser (keys, e => e.DocString == null, AddFeature, "Feature:");
				AddParser (keys, e => e.DocString == null && e.Feature != null, AddScenario, "Scenario:", "Scenario Outline:", "Background:");
				AddParser (keys, e => e.DocString == null && e.Scenario != null, AddExamples, "Examples:");
				AddParser (keys, e => e.DocString == null && e.Scenario != null && e.Examples == null, AddSteps, "Given", "When", "Then", "And", "But");
				AddParser (keys, e => e.DocString == null && e.Scenario != null && (e.Step != null || e.Examples != null) && e.Table == null, AddTable, "|");
				AddParser (keys, e => e.DocString == null && e.Scenario != null && (e.Step != null || e.Examples != null) && e.Table != null, AddTableRow, "|");
				AddParser (keys, e => e.Scenario != null && e.Step != null, AddDocString, "\"\"\"");

				var context = new ParserContext
				{
					FileName = doc.FileName,
					Errors = doc.Errors,
					Tags = new List<string>(),
					Features = new List<Feature>()
				};

				ExecuteParsers (context, keys, contentLines);

				file.Features = context.Features;
			}

			return doc;
		}

		void AddParser (List<Tuple<Func<ParserContext, bool>, Func<ParserContext, bool>, HashSet<string>>> keys, Func<ParserContext, bool> predicate, Func<ParserContext, bool> action, params string[] keywords)
		{
			keys.Add (new Tuple<Func<ParserContext, bool>, Func<ParserContext, bool>, HashSet<string>> (predicate, action, new HashSet<string>(keywords)));
		}

		void ExecuteParsers(ParserContext context, List<Tuple<Func<ParserContext, bool>, Func<ParserContext, bool>, HashSet<string>>> keys, string[] contentLines)
		{
			for (var i = 0; i < contentLines.Length; i++)
			{
				string line = contentLines [i];

				for(int j = 0; j < line.Length; j++)
				{
					char c = line [j];

					if (char.IsWhiteSpace (c))
						continue;

					if (c == '#')
						break;

					bool found = false;
					bool failed = false;

					foreach(var key in keys)
					{
						if (!key.Item1 (context))
							continue;
						
						foreach (var keyword in key.Item3) {
							if (j + keyword.Length > line.Length)
								continue;

							if (line.IndexOf (keyword, j, keyword.Length) < 0)
								continue;

							var isWord = j + keyword.Length >= line.Length || char.IsWhiteSpace (line [j + keyword.Length]);

							context.Content = line;
							context.LineNumber = i + 1;
							context.Keyword = keyword;
							context.IsWord = isWord;
							context.Column = j + 1;

							failed = !key.Item2 (context);

							found = true;

							break;
						}

						if (found)
							break;
					}
						
					if (found && !failed)
						break;
					else if (!string.IsNullOrWhiteSpace (line)) {
						AddDefault (context);
						break;
					}
				}	
			}

			if(contentLines.Length > 0) {
				foreach(var key in keys)
				{
					if (!key.Item1 (context))
						continue;

					context.Content = null;
					context.LineNumber = contentLines.Length;
					context.Keyword = null;
					context.Column = contentLines[contentLines.Length - 1].Length;

					key.Item2 (context);

				}
			}
		}
			
		static Regex tagExpression = new Regex ("(?:^|\\s*)(@[^\\s]+)(?:$|\\s*)", RegexOptions.Compiled);

		static void AddDefault(ParserContext context)
		{
			if (context.Scenario != null) {
				if (context.Steps.Count == 0) {
					context.Errors.Add (new Error (ErrorType.Error, "Expecting Given, When or Then", new DomRegion (context.LineNumber, context.Column, context.LineNumber, 1 + context.Content.Length)));
				}
			}
		}

		static bool AddTag(ParserContext context)
		{
			if (context.Keyword == null)
				return false;

			var matches = tagExpression.Matches(context.Content);

			foreach (Match match in matches) {
				var p = match.Groups [1];

				context.Tags.Add (p.ToString());
			}

			return true;
		}

		static bool AddFeature (ParserContext context)
		{
			if (context.Feature != null)
				context.Features.Add (context.Feature ());

			if (context.Step != null)
				context.Steps.Add (context.Step ());

			if (context.Scenario != null) {
				context.Scenarios.Add (context.Scenario ());
			}

			context.Scenario = null;
			context.Step = null;

			if (context.Keyword == null || !context.IsWord)
				return false;
			
			var scenarios = new List<Scenario> ();
			context.Scenarios = scenarios;

			var tags = context.Tags;
			context.Tags = new List<string> ();

			var content = context.RemainingContent;
			var fileName = context.FileName;
			var line = context.LineNumber;
			var column = context.Column;

			context.Feature = () => new Feature (scenarios, tags, content, fileName, line, column);

			return true;
		}

		static bool AddScenario (ParserContext context)
		{
			if (context.Scenario != null)
				context.Scenarios.Add (context.Scenario ());
			
			if (context.Keyword == null || !context.IsWord)
				return false;

			var steps = new List<Step> ();

			context.Steps = steps;
			context.Examples = null;
			context.Table = null;

			var tags = context.Tags;
			context.Tags = new List<string> ();

			var content = context.RemainingContent;
			var fileName = context.FileName;
			var line = context.LineNumber;
			var column = context.Column;
			var keyword = context.Keyword;

			switch (context.Keyword) {
				case "Scenario Outline:":
					context.Scenario = () => {
						if (context.Examples == null && steps.Count > 0) {
							context.Errors.Add (new Error (ErrorType.Error, "Expecting Examples for Scenario Outline", new DomRegion (line, column, line, column + keyword.Length)));
						}

						Examples examples = null;

						if (context.Examples != null) {
							examples = context.Examples();
					
							if (examples.Table != null && examples.Table.Header != null) {
								foreach (var j in steps) {
									foreach (var k in j.Placeholders) {
										if (!examples.Table.Header.Columns.Contains (k.Name)) {
											context.Errors.Add (new Error (ErrorType.Warning, "Placeholder does not match column in table", new DomRegion (k.Line, k.Column, line, k.Column + k.Name.Length)));
										}
									}
								}
							}
						}

						return new ScenarioOutline (examples, steps, tags, content, fileName, line, column);
					};
					break;
				case "Scenario:":
				context.Scenario = () => new Scenario (steps, tags, content, fileName, line, column);
					break;
				case "Background:":
				context.Scenario = () => new Background (steps, tags, fileName, line, column);
					break;
			}

			return true;
		}

		static Regex parameterExpression = new Regex ("<([^>]*)>", RegexOptions.Compiled);

		static bool AddSteps (ParserContext context)
		{
			if (context.Step != null)
				context.Steps.Add (context.Step ());

			if (context.Keyword == null || !context.IsWord)
				return false;

			context.Table = null;

			var placeholders = new List<Placeholder>();
			var fileName = context.FileName;
			var line = context.LineNumber;
			var column = context.Column;
			var content = context.RemainingContent;

			var matches = parameterExpression.Matches(context.Content);

			foreach (Match match in matches) {
				var p = match.Groups [1];

				placeholders.Add (new Placeholder(p.ToString(), fileName, line, p.Index + 1));
			}

			context.Step = () => {

				Table table = null;

				if (context.Table != null) {
					table = context.Table ();
				}

				DocString docString = null;

				if (table == null && context.DocString != null) {
					docString = context.DocString ();
				}

				return new Step (placeholders, table, docString, content, fileName, line, column);
			};

			return true;
		}

		static bool AddExamples (ParserContext context)
		{
			if (context.Keyword == null || !context.IsWord)
				return false;

			if (context.Step != null)
				context.Steps.Add (context.Step ());			

			context.Table = null;

			var tags = context.Tags;
			context.Tags = new List<string> ();

			var fileName = context.FileName;
			var line = context.LineNumber;
			var column = context.Column;

			if (context.Steps.Count == 0) {
				context.Errors.Add (new Error (ErrorType.Error, "Expecting Given, When or Then", new DomRegion (context.LineNumber, context.Column, context.LineNumber, 1 + context.Content.Length)));
			}

			context.Examples = () => {
				Table table = null;

				if (context.Table == null) {
					context.Errors.Add(new Error(ErrorType.Error, "Expecting Table for Examples", line, column));
				}
				else
				{
					table = context.Table();
				}

				return new Examples (table, tags, fileName, line, column);
			};

			return true;
		}

		static bool AddTable (ParserContext context)
		{
			if (context.Keyword == null)
				return false;

			var header = AddTableHeader(context.Content);

			var rows = new List<TableRow> ();

			context.TableRows = rows;

			var fileName = context.FileName;
			var line = context.LineNumber;
			var column = context.Column;

			context.Table = () => {
				if (rows.Any (e => e.Cells.Count != header.Columns.Count)) {
					context.Errors.Add (new Error (ErrorType.Error, "Table is uneven", new DomRegion (line, column, line + 1, 1)));
				}

				return new Table (header, rows, fileName, line, column);
			};

			return true;
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

		static bool AddTableRow(ParserContext context)
		{
			if (context.Keyword == null)
				return false;

			var cells = new List<string> ();

			int a;
			int b;

			if (!GetNextCell (context.Content, 0, out a)) {
				return false;
			}

			a++;

			for (; a < context.Content.Length; a++) {
				if (!GetNextCell (context.Content, a, out b)) {
					return false;
				}

				cells.Add (context.Content.Substring (a, b - a).Trim());

				a = b;
			}

			context.TableRows.Add(new TableRow (cells));

			return true;
		}

		static bool AddDocString (ParserContext context)
		{
			if (context.Keyword == null ||!context.IsWord)
				return false;

			if (context.DocString != null) {
				context.DocString = null;
				return true;
			}
				
			var fileName = context.FileName;
			var line = context.LineNumber;
			var column = context.Column;

			context.DocString = () => new DocString (fileName, line, column);

			return true;
		}

		static void AddPlaceholders(Step action, IUnresolvedMethod method)
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

