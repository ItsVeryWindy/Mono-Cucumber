using System;
using Mono.TextEditor.Highlighting;
using Mono.TextEditor;
using System.Linq;
using System.Collections.Generic;

namespace CucumberBinding.Highlighting
{
	public class GherkinSyntaxMode : SyntaxMode
	{
		public GherkinSyntaxMode()
		{
			ResourceStreamProvider provider = new ResourceStreamProvider (typeof (GherkinSyntaxMode).Assembly, "GherkinSyntaxHighlightingMode.xml");
			using (var reader = provider.Open ()) {
				SyntaxMode baseMode = SyntaxMode.Read (reader);
				this.rules = new List<Rule> (baseMode.Rules);
				this.keywords = new List<Keywords> (baseMode.Keywords);
				this.spans = baseMode.Spans;
				this.matches = baseMode.Matches;
				this.prevMarker = baseMode.PrevMarker;
				this.SemanticRules = new List<SemanticRule> (baseMode.SemanticRules);
				this.keywordTable = baseMode.keywordTable;
				this.keywordTableIgnoreCase = baseMode.keywordTableIgnoreCase;
			}
		}

		public override SpanParser CreateSpanParser (Mono.TextEditor.DocumentLine line, CloneableStack<Span> spanStack)
		{
			return new GherkinSpanParser (this, spanStack ?? line.StartSpan.Clone ());
		}

		class GherkinSpanParser : SpanParser
		{
			public GherkinSpanParser(SyntaxMode mode, CloneableStack<Span> spanStack) : base(mode, spanStack)
			{
			}

			protected override bool ScanSpan (ref int i)
			{
				int textOffset = i - StartOffset;

				Span[] spans = CurRule.Spans;
				for (int j = 0; j < spans.Length; j++) {
					Span span = spans [j];

					if ((span.BeginFlags & SpanBeginFlags.StartsLine) == SpanBeginFlags.StartsLine) {
						if (textOffset != 0) {
							char ch = CurText [textOffset - 1];
							if (ch != '\n' && ch != '\r')
								continue;
						}
					} 

					RegexMatch match = span.Begin.TryMatch (CurText, textOffset);
					if (!match.Success || ((span.Rule == "Table" || span.Rule == "TableHeader") && CurText[textOffset] != span.Begin.Pattern[0])) {
						continue;
					}

					// scan for span exit which cancels the span.
					if ((span.ExitFlags & SpanExitFlags.CancelSpan) == SpanExitFlags.CancelSpan && span.Exit != null) {
						bool foundEnd = false;
						for (int k = i + match.Length; k < CurText.Length; k++) {
							if (span.Exit.TryMatch (CurText, k - StartOffset).Success)
								return false;
							if (span.End.TryMatch (CurText, k - StartOffset).Success) {
								foundEnd = true;
								break;
							}
						}
						if (!foundEnd)
							return false;
					}

					bool mismatch = false;

					if (span.Rule == "Table" || span.Rule == "TableHeader") {
						bool found = false;

						for (int k = textOffset + 1; k < CurText.Length; k++) {
							if (CurText [k] == span.End.Pattern [0] && (k == 0 || CurText [k - 1] != '\\')) {
								found = true;
								break;
							}
						}

						mismatch = !found || CurText.Take (textOffset).Any (ch => !char.IsWhiteSpace (ch));
					}

					if ((span.BeginFlags & SpanBeginFlags.FirstNonWs) == SpanBeginFlags.FirstNonWs)
						mismatch = CurText.Take (textOffset).Any (ch => !char.IsWhiteSpace (ch));
					if ((span.BeginFlags & SpanBeginFlags.NewWord) == SpanBeginFlags.NewWord) {
						if (textOffset - 1 > 0 && textOffset - 1 < CurText.Length)
							mismatch = !char.IsWhiteSpace (CurText [textOffset - 1]);
						else if (textOffset + match.Length < CurText.Length)
							mismatch = !char.IsWhiteSpace (CurText [textOffset + match.Length]);
							
					}
					if (mismatch)
						continue;
					if (span.Rule == "Table" || span.Rule == "TableHeader") {
						FoundSpanBegin (span, i, 1);
					} else {
						FoundSpanBegin (span, i, match.Length);
						i += System.Math.Max (0, match.Length - 1);
					}
					return true;
				}
				return false;
			}

			protected override bool ScanSpanEnd (Span cur, ref int i)
			{
				int textOffset = i - StartOffset;

				if (cur.End != null && (cur.Rule == "Table" || cur.Rule == "TableHeader")) {
					if (cur.Rule == "Table") {
						if (CurText [textOffset] != cur.End.Pattern [0] || CurText.Skip (textOffset + 1).Any (e => e == cur.End.Pattern [0])) {
							return false;
						}
					}

					if (cur.Rule == "TableHeader") {
						if (textOffset != 0 || (!string.IsNullOrWhiteSpace(CurText) && CurText.First (ch => !char.IsWhiteSpace (ch)) == cur.End.Pattern [0])) {
							return false;
						}
					}

					FoundSpanEnd (cur, i, 1);
					return true;
				}
					
				if (cur.End != null) {
					RegexMatch match = cur.End.TryMatch (CurText, textOffset);
					if (match.Success) {
						FoundSpanEnd (cur, i, match.Length);
						i += Math.Max (0, match.Length - 1);
						return true;
					}
				}

				if (cur.Exit != null) {
					RegexMatch match = cur.Exit.TryMatch (CurText, textOffset);
					if (match.Success) {
						FoundSpanExit (cur, i, match.Length);
						i += -1;
						return true;
					}
				}

				return false;
			}
		}
	}
}

