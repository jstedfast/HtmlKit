//
// HtmlTokenizer.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System.IO;
using System.Text;

namespace HtmlKit {
	public class HtmlTokenizer
	{
		const string DocType = "doctype";
		const string CData = "[CDATA[";

		readonly HtmlEntityDecoder entity = new HtmlEntityDecoder ();
		readonly StringBuilder data = new StringBuilder ();
		readonly StringBuilder name = new StringBuilder ();
		readonly char[] cdata = new char[3];
		HtmlDocTypeToken doctype;
		HtmlAttribute attribute;
		string activeTagName;
		HtmlTagToken tag;
		int cdataIndex;
		bool isEndTag;
		char quote;

		TextReader text;

		public HtmlTokenizer (TextReader reader)
		{
			text = reader;
		}

		/// <summary>
		/// Get the current state of the tokenizer.
		/// </summary>
		/// <remarks>
		/// Gets the current state of the tokenizer.
		/// </remarks>
		/// <value>The current state of the tokenizer.</value>
		public HtmlTokenizerState TokenizerState {
			get; private set;
		}

		static bool IsAlphaNumeric (char c)
		{
			return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');
		}

		static bool IsAsciiUpperLetter (char c)
		{
			return c >= 'A' && c <= 'Z';
		}

		static bool IsAsciiLowerLetter (char c)
		{
			return c >= 'a' && c <= 'z';
		}

		static bool IsAsciiLetter (char c)
		{
			return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
		}

		static char ToLower (char c)
		{
			return (c >= 'A' && c <= 'Z') ? (char) (c + 0x20) : c;
		}

		void EmitTagAttribute ()
		{
			attribute = new HtmlAttribute (name.ToString ());
			tag.Attributes.Add (attribute);
			name.Clear ();
		}

		bool EmitDataToken (out HtmlToken token)
		{
			if (data.Length > 0) {
				token = new HtmlDataToken (data.ToString ());
				data.Clear ();
				return true;
			}

			token = null;

			return false;
		}

		bool EmitTagToken (out HtmlToken token)
		{
			if (!tag.IsEndTag && !tag.IsEmptyElement) {
				switch (tag.Name) {
				case "style": case "xmp": case "iframe": case "noembed": case "noframes":
					TokenizerState = HtmlTokenizerState.RawText;
					activeTagName = tag.Name;
					break;
				case "title": case "textarea":
					TokenizerState = HtmlTokenizerState.RcData;
					activeTagName = tag.Name;
					break;
				case "plaintext":
					TokenizerState = HtmlTokenizerState.PlainText;
					break;
				case "script":
					TokenizerState = HtmlTokenizerState.ScriptData;
					break;
				case "noscript":
					// TODO: only switch into the RawText state if scripting is enabled
					TokenizerState = HtmlTokenizerState.RawText;
					activeTagName = tag.Name;
					break;
				default:
					TokenizerState = HtmlTokenizerState.Data;
					break;
				}
			} else {
				TokenizerState = HtmlTokenizerState.Data;
			}

			data.Clear ();
			token = tag;
			tag = null;

			return true;
		}

		bool ReadCharacterReference (out HtmlToken token, HtmlTokenizerState next)
		{
			int nc = text.Peek ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				data.Append ('&');

				return EmitDataToken (out token);
			}

			c = (char) nc;
			token = null;

			switch (c) {
			case '\t': case '\n': case '\f': case ' ': case '<': case '&':
				// no character is consumed, emit '&'
				TokenizerState = next;
				data.Append ('&');
				return false;
			}

			entity.Push ('&');

			while (entity.Push (c)) {
				text.Read ();

				if ((nc = text.Peek ()) == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					data.Append (entity.GetValue ());
					entity.Reset ();

					return EmitDataToken (out token);
				}

				c = (char) nc;
			}

			TokenizerState = next;

			data.Append (entity.GetValue ());
			entity.Reset ();

			if (c == ';') {
				// consume the ';'
				text.Read ();
			}

			return false;
		}

		bool ReadGenericRawTextLessThan (out HtmlToken token, HtmlTokenizerState rawText, HtmlTokenizerState rawTextEndTagOpen)
		{
			int nc = text.Peek ();

			data.Append ('<');

			switch ((char) nc) {
			case '/':
				TokenizerState = rawTextEndTagOpen;
				data.Append ('/');
				name.Clear ();
				text.Read ();
				break;
			default:
				TokenizerState = rawText;
				break;
			}

			token = null;

			return false;
		}

		bool ReadGenericRawTextEndTagOpen (out HtmlToken token, HtmlTokenizerState rawText, HtmlTokenizerState rawTextEndTagName)
		{
			int nc = text.Peek ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				return EmitDataToken (out token);
			}

			c = (char) nc;

			if (IsAsciiLetter (c)) {
				TokenizerState = rawTextEndTagName;
				name.Append (ToLower (c));
				data.Append (c);
				text.Read ();
			} else {
				TokenizerState = rawText;
			}

			token = null;

			return false;
		}

		bool ReadGenericRawTextEndTagName (out HtmlToken token, HtmlTokenizerState rawText)
		{
			var current = TokenizerState;

			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					name.Clear ();

					return EmitDataToken (out token);
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\n': case '\f': case ' ':
					if (name.ToString () == activeTagName) {
						TokenizerState = HtmlTokenizerState.BeforeAttributeName;
						break;
					}

					goto default;
				case '/':
					if (name.ToString () == activeTagName) {
						TokenizerState = HtmlTokenizerState.SelfClosingStartTag;
						break;
					}
					goto default;
				case '>':
					if (name.ToString () == activeTagName) {
						token = new HtmlTagToken (name.ToString (), true);
						TokenizerState = HtmlTokenizerState.Data;
						data.Clear ();
						name.Clear ();
						return true;
					}
					goto default;
				default:
					if (!IsAsciiLetter (c)) {
						TokenizerState = rawText;
						token = null;
						return false;
					}

					name.Append (c == '\0' ? '\uFFFD' : ToLower (c));
					break;
				}
			} while (TokenizerState == current);

			tag = new HtmlTagToken (name.ToString (), true);
			name.Clear ();
			token = null;

			return false;
		}

		bool ReadDataToken (out HtmlToken token)
		{
			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					break;
				}

				c = (char) nc;

				switch (c) {
				case '&':
					TokenizerState = HtmlTokenizerState.CharacterReferenceInData;
					token = null;
					return false;
				case '<':
					TokenizerState = HtmlTokenizerState.TagOpen;
					break;
				//case 0: // parse error, but emit it anyway
				default:
					data.Append (c);

					// Note: we emit at 1024 characters simply to avoid
					// consuming too much memory.
					if (data.Length >= 1024)
						return EmitDataToken (out token);

					break;
				}
			} while (TokenizerState == HtmlTokenizerState.Data);

			return EmitDataToken (out token);
		}

		bool ReadCharacterReferenceInData (out HtmlToken token)
		{
			return ReadCharacterReference (out token, HtmlTokenizerState.Data);
		}

		bool ReadRcData (out HtmlToken token)
		{
			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					break;
				}

				c = (char) nc;

				switch (c) {
				case '&':
					TokenizerState = HtmlTokenizerState.CharacterReferenceInRcData;
					token = null;
					return false;
				case '<':
					TokenizerState = HtmlTokenizerState.RcDataLessThan;
					return EmitDataToken (out token);
				default:
					data.Append (c == '\0' ? '\uFFFD' : c);

					// Note: we emit at 1024 characters simply to avoid
					// consuming too much memory.
					if (data.Length >= 1024)
						return EmitDataToken (out token);

					break;
				}
			} while (TokenizerState == HtmlTokenizerState.RcData);

			if (data.Length > 0)
				return EmitDataToken (out token);

			token = null;

			return false;
		}

		bool ReadCharacterReferenceInRcData (out HtmlToken token)
		{
			return ReadCharacterReference (out token, HtmlTokenizerState.RcData);
		}

		bool ReadRawText (out HtmlToken token)
		{
			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					break;
				}

				c = (char) nc;

				switch (c) {
				case '<':
					TokenizerState = HtmlTokenizerState.RawTextLessThan;
					return EmitDataToken (out token);
				default:
					data.Append (c == '\0' ? '\uFFFD' : c);

					// Note: we emit at 1024 characters simply to avoid
					// consuming too much memory.
					if (data.Length >= 1024)
						return EmitDataToken (out token);

					break;
				}
			} while (TokenizerState == HtmlTokenizerState.RawText);

			if (data.Length > 0)
				return EmitDataToken (out token);

			token = null;

			return false;
		}

		bool ReadScriptData (out HtmlToken token)
		{
			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					break;
				}

				c = (char) nc;

				switch (c) {
				case '<':
					TokenizerState = HtmlTokenizerState.ScriptDataLessThan;
					return EmitDataToken (out token);
				default:
					data.Append (c == '\0' ? '\uFFFD' : c);

					// Note: we emit at 1024 characters simply to avoid
					// consuming too much memory.
					if (data.Length >= 1024)
						return EmitDataToken (out token);

					break;
				}
			} while (TokenizerState == HtmlTokenizerState.ScriptData);

			if (data.Length > 0)
				return EmitDataToken (out token);

			token = null;

			return false;
		}

		bool ReadPlainText (out HtmlToken token)
		{
			int nc = text.Read ();

			while (nc != -1) {
				char c = (char) nc;

				data.Append (c == '\0' ? '\uFFFD' : c);

				// Note: we emit at 1024 characters simply to avoid
				// consuming too much memory.
				if (data.Length >= 1024)
					return EmitDataToken (out token);

				nc = text.Read ();
			}

			TokenizerState = HtmlTokenizerState.EndOfFile;

			return EmitDataToken (out token);
		}

		bool ReadTagOpen (out HtmlToken token)
		{
			int nc = text.Read ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				token = new HtmlDataToken ("<");
				return true;
			}

			token = null;

			c = (char) nc;

			// Note: we save the data in case we hit a parse error and have to emit a data token
			data.Append ('<');
			data.Append (c);

			switch ((c = (char) nc)) {
			case '!': TokenizerState = HtmlTokenizerState.MarkupDeclarationOpen; break;
			case '?': TokenizerState = HtmlTokenizerState.BogusComment; break;
			case '/': TokenizerState = HtmlTokenizerState.EndTagOpen; break;
			default:
				c = ToLower (c);

				if (c >= 'a' && c <= 'z') {
					TokenizerState = HtmlTokenizerState.TagName;
					isEndTag = false;
					name.Append (c);
				} else {
					TokenizerState = HtmlTokenizerState.Data;
					return false;
				}
				break;
			}

			return false;
		}

		bool ReadEndTagOpen (out HtmlToken token)
		{
			int nc = text.Read ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				return EmitDataToken (out token);
			}

			c = (char) nc;
			token = null;

			// Note: we save the data in case we hit a parse error and have to emit a data token
			data.Append (c);

			switch (c) {
			case '>': // parse error
				TokenizerState = HtmlTokenizerState.Data;
				data.Clear ();
				break;
			default:
				c = ToLower (c);

				if (c >= 'a' && c <= 'z') {
					TokenizerState = HtmlTokenizerState.TagName;
					isEndTag = true;
					name.Append (c);
				} else {
					TokenizerState = HtmlTokenizerState.BogusComment;
					return false;
				}
				break;
			}

			return false;
		}

		bool ReadTagName (out HtmlToken token)
		{
			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					name.Clear ();

					return EmitDataToken (out token);
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\n': case '\f': case ' ':
					TokenizerState = HtmlTokenizerState.BeforeAttributeName;
					break;
				case '/':
					TokenizerState = HtmlTokenizerState.SelfClosingStartTag;
					break;
				case '>':
					token = new HtmlTagToken (name.ToString (), isEndTag);
					TokenizerState = HtmlTokenizerState.Data;
					data.Clear ();
					name.Clear ();
					return true;
				default:
					name.Append (c == '\0' ? '\uFFFD' : ToLower (c));
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.TagName);

			tag = new HtmlTagToken (name.ToString (), isEndTag);
			name.Clear ();
			token = null;

			return false;
		}

		bool ReadRcDataLessThan (out HtmlToken token)
		{
			return ReadGenericRawTextLessThan (out token, HtmlTokenizerState.RcData, HtmlTokenizerState.RcDataEndTagOpen);
		}

		bool ReadRcDataEndTagOpen (out HtmlToken token)
		{
			return ReadGenericRawTextEndTagOpen (out token, HtmlTokenizerState.RcData, HtmlTokenizerState.RcDataEndTagName);
		}

		bool ReadRcDataEndTagName (out HtmlToken token)
		{
			return ReadGenericRawTextEndTagName (out token, HtmlTokenizerState.RcData);
		}

		bool ReadRawTextLessThan (out HtmlToken token)
		{
			return ReadGenericRawTextLessThan (out token, HtmlTokenizerState.RawText, HtmlTokenizerState.RawTextEndTagOpen);
		}

		bool ReadRawTextEndTagOpen (out HtmlToken token)
		{
			return ReadGenericRawTextEndTagOpen (out token, HtmlTokenizerState.RawText, HtmlTokenizerState.RawTextEndTagName);
		}

		bool ReadRawTextEndTagName (out HtmlToken token)
		{
			return ReadGenericRawTextEndTagName (out token, HtmlTokenizerState.RawText);
		}

		bool ReadScriptDataLessThan (out HtmlToken token)
		{
			int nc = text.Peek ();

			data.Append ('<');

			switch ((char) nc) {
			case '/':
				TokenizerState = HtmlTokenizerState.ScriptDataEndTagOpen;
				data.Append ('/');
				name.Clear ();
				text.Read ();
				break;
			case '!':
				TokenizerState = HtmlTokenizerState.ScriptDataEscapeStart;
				data.Append ('!');
				text.Read ();
				break;
			default:
				TokenizerState = HtmlTokenizerState.ScriptData;
				break;
			}

			token = null;

			return false;
		}

		bool ReadScriptDataEndTagOpen (out HtmlToken token)
		{
			int nc = text.Peek ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				return EmitDataToken (out token);
			}

			c = (char) nc;

			if (c == 'S' || c == 's') {
				TokenizerState = HtmlTokenizerState.ScriptDataEndTagName;
				name.Append ('s');
				data.Append (c);
				text.Read ();
			} else {
				TokenizerState = HtmlTokenizerState.ScriptData;
			}

			token = null;

			return false;
		}

		bool ReadScriptDataEndTagName (out HtmlToken token)
		{
			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					name.Clear ();

					return EmitDataToken (out token);
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\n': case '\f': case ' ':
					if (name.ToString () == "script") {
						TokenizerState = HtmlTokenizerState.BeforeAttributeName;
						break;
					}

					goto default;
				case '/':
					if (name.ToString () == "script") {
						TokenizerState = HtmlTokenizerState.SelfClosingStartTag;
						break;
					}
					goto default;
				case '>':
					if (name.ToString () == "script") {
						token = new HtmlTagToken (name.ToString (), true);
						TokenizerState = HtmlTokenizerState.Data;
						data.Clear ();
						name.Clear ();
						return true;
					}
					goto default;
				default:
					if (!IsAsciiLetter (c)) {
						TokenizerState = HtmlTokenizerState.ScriptData;
						token = null;
						return false;
					}

					name.Append (c == '\0' ? '\uFFFD' : ToLower (c));
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.ScriptDataEndTagName);

			tag = new HtmlTagToken (name.ToString (), true);
			name.Clear ();
			token = null;

			return false;
		}

		bool ReadScriptDataEscapeStart (out HtmlToken token)
		{
			int nc = text.Peek ();

			if (nc == '-') {
				TokenizerState = HtmlTokenizerState.ScriptDataEscapeStartDash;
				text.Read ();
			} else {
				TokenizerState = HtmlTokenizerState.ScriptData;
			}

			token = null;

			return false;
		}

		bool ReadScriptDataEscapeStartDash (out HtmlToken token)
		{
			int nc = text.Peek ();

			if (nc == '-') {
				TokenizerState = HtmlTokenizerState.ScriptDataEscapedDashDash;
				text.Read ();
			} else {
				TokenizerState = HtmlTokenizerState.ScriptData;
			}

			token = null;

			return false;
		}

		bool ReadScriptDataEscaped (out HtmlToken token)
		{
			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					return EmitDataToken (out token);
				}

				c = (char) nc;

				switch (c) {
				case '-':
					TokenizerState = HtmlTokenizerState.ScriptDataEscapedDash;
					data.Append ('-');
					break;
				case '<':
					TokenizerState = HtmlTokenizerState.ScriptDataEscapedLessThan;
					break;
				default:
					data.Append (c == '\0' ? '\uFFFD' : c);
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.ScriptDataEscaped);

			token = null;

			return false;
		}

		bool ReadScriptDataEscapedDash (out HtmlToken token)
		{
			int nc = text.Peek ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				return EmitDataToken (out token);
			}

			switch ((c = (char) nc)) {
			case '-':
				TokenizerState = HtmlTokenizerState.ScriptDataEscapedDashDash;
				data.Append ('-');
				break;
			case '<':
				TokenizerState = HtmlTokenizerState.ScriptDataEscapedLessThan;
				break;
			default:
				TokenizerState = HtmlTokenizerState.ScriptDataEscaped;
				data.Append (c == '\0' ? '\uFFFD' : c);
				break;
			}

			token = null;

			return false;
		}

		bool ReadScriptDataEscapedDashDash (out HtmlToken token)
		{
			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					return EmitDataToken (out token);
				}

				c = (char) nc;

				switch (c) {
				case '-':
					TokenizerState = HtmlTokenizerState.ScriptDataEscapedDash;
					data.Append ('-');
					break;
				case '<':
					TokenizerState = HtmlTokenizerState.ScriptDataEscapedLessThan;
					break;
				case '>':
					TokenizerState = HtmlTokenizerState.ScriptData;
					data.Append ('>');
					break;
				default:
					TokenizerState = HtmlTokenizerState.ScriptDataEscaped;
					data.Append (c);
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.ScriptDataEscaped);

			token = null;

			return false;
		}

		bool ReadScriptDataEscapedLessThan (out HtmlToken token)
		{
			int nc = text.Peek ();
			char c = (char) nc;

			if (c == '/') {
				TokenizerState = HtmlTokenizerState.ScriptDataEndTagOpen;
				name.Clear ();
				text.Read ();
			} else if (IsAsciiLetter (c)) {
				TokenizerState = HtmlTokenizerState.ScriptDataDoubleEscaped;
				name.Append (ToLower (c));
				data.Append ('<');
				data.Append (c);
				text.Read ();
			} else {
				TokenizerState = HtmlTokenizerState.ScriptDataEscaped;
				data.Append ('<');
			}

			token = null;

			return false;
		}

		bool ReadScriptDataEscapedEndTagOpen (out HtmlToken token)
		{
			int nc = text.Peek ();
			char c;

			data.Append ("</");

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				return EmitDataToken (out token);
			}

			c = (char) nc;

			if (IsAsciiLetter (c)) {
				TokenizerState = HtmlTokenizerState.ScriptDataEscapedEndTagName;
				name.Append (ToLower (c));
				text.Read ();
			} else {
				TokenizerState = HtmlTokenizerState.ScriptDataEscaped;
			}

			token = null;

			return false;
		}

		bool ReadScriptDataEscapedEndTagName (out HtmlToken token)
		{
			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					name.Clear ();

					return EmitDataToken (out token);
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\n': case '\f': case ' ':
					if (name.ToString () == "script") {
						TokenizerState = HtmlTokenizerState.BeforeAttributeName;
						break;
					}

					goto default;
				case '/':
					if (name.ToString () == "script") {
						TokenizerState = HtmlTokenizerState.SelfClosingStartTag;
						break;
					}
					goto default;
				case '>':
					if (name.ToString () == "script") {
						token = new HtmlTagToken (name.ToString (), true);
						TokenizerState = HtmlTokenizerState.Data;
						data.Clear ();
						name.Clear ();
						return true;
					}
					goto default;
				default:
					if (!IsAsciiLetter (c)) {
						TokenizerState = HtmlTokenizerState.ScriptData;
						data.Append (c);
						token = null;
						return false;
					}

					name.Append (ToLower (c));
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.ScriptDataEscapedEndTagName);

			tag = new HtmlTagToken (name.ToString (), true);
			name.Clear ();
			token = null;

			return false;
		}

		bool ReadScriptDataDoubleEscapeStart (out HtmlToken token)
		{
			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					name.Clear ();

					return EmitDataToken (out token);
				}

				c = (char) nc;

				data.Append (c);

				switch (c) {
				case '\t': case '\n': case '\f': case ' ': case '/': case '>':
					if (name.ToString () == "script")
						TokenizerState = HtmlTokenizerState.ScriptDataDoubleEscaped;
					else
						TokenizerState = HtmlTokenizerState.ScriptDataEscaped;
					name.Clear ();
					break;
				default:
					if (!IsAsciiLetter (c))
						TokenizerState = HtmlTokenizerState.ScriptDataEscaped;
					else
						name.Append (ToLower (c));
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.ScriptDataDoubleEscapeStart);

			token = null;

			return false;
		}

		bool ReadScriptDataDoubleEscaped (out HtmlToken token)
		{
			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					return EmitDataToken (out token);
				}

				c = (char) nc;

				switch (c) {
				case '-':
					TokenizerState = HtmlTokenizerState.ScriptDataDoubleEscapedDash;
					data.Append ('-');
					break;
				case '<':
					TokenizerState = HtmlTokenizerState.ScriptDataDoubleEscapedLessThan;
					break;
				default:
					data.Append (c == '\0' ? '\uFFFD' : c);
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.ScriptDataEscaped);

			token = null;

			return false;
		}

		bool ReadScriptDataDoubleEscapedDash (out HtmlToken token)
		{
			int nc = text.Peek ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				return EmitDataToken (out token);
			}

			switch ((c = (char) nc)) {
			case '-':
				TokenizerState = HtmlTokenizerState.ScriptDataDoubleEscapedDashDash;
				data.Append ('-');
				break;
			case '<':
				TokenizerState = HtmlTokenizerState.ScriptDataDoubleEscapedLessThan;
				break;
			default:
				TokenizerState = HtmlTokenizerState.ScriptDataDoubleEscaped;
				data.Append (c == '\0' ? '\uFFFD' : c);
				break;
			}

			token = null;

			return false;
		}

		bool ReadScriptDataDoubleEscapedDashDash (out HtmlToken token)
		{
			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					return EmitDataToken (out token);
				}

				c = (char) nc;

				switch (c) {
				case '-':
					data.Append ('-');
					break;
				case '<':
					TokenizerState = HtmlTokenizerState.ScriptDataDoubleEscapedLessThan;
					data.Append ('<');
					break;
				case '>':
					TokenizerState = HtmlTokenizerState.ScriptData;
					data.Append ('>');
					break;
				default:
					TokenizerState = HtmlTokenizerState.ScriptDataDoubleEscaped;
					data.Append (c);
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.ScriptDataEscaped);

			token = null;

			return false;
		}

		bool ReadScriptDataDoubleEscapedLessThan (out HtmlToken token)
		{
			int nc = text.Peek ();

			if (nc == '/') {
				TokenizerState = HtmlTokenizerState.ScriptDataDoubleEscapeEnd;
				data.Append ('/');
				text.Read ();
			} else {
				TokenizerState = HtmlTokenizerState.ScriptDataDoubleEscaped;
			}

			token = null;

			return false;
		}

		bool ReadScriptDataDoubleEscapeEnd (out HtmlToken token)
		{
			do {
				int nc = text.Peek ();
				char c = (char) nc;

				switch (c) {
				case '\t': case '\n': case '\f': case ' ': case '/': case '>':
					if (name.ToString () == "script")
						TokenizerState = HtmlTokenizerState.ScriptDataEscaped;
					else
						TokenizerState = HtmlTokenizerState.ScriptDataDoubleEscaped;
					data.Append (c);
					text.Read ();
					break;
				default:
					if (!IsAsciiLetter (c)) {
						TokenizerState = HtmlTokenizerState.ScriptDataDoubleEscaped;
					} else {
						name.Append (ToLower (c));
						data.Append (c);
						text.Read ();
					}
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.ScriptDataDoubleEscapeEnd);

			token = null;

			return false;
		}

		bool ReadBeforeAttributeName (out HtmlToken token)
		{
			token = null;

			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					tag = null;

					return EmitDataToken (out token);
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\n': case '\f': case ' ':
					break;
				case '/':
					TokenizerState = HtmlTokenizerState.SelfClosingStartTag;
					return false;
				case '>':
					return EmitTagToken (out token);
				case '"': case '\'': case '<': case '=':
					// parse error
					goto default;
				default:
					TokenizerState = HtmlTokenizerState.AttributeName;
					name.Append (c == '\0' ? '\uFFFD' : ToLower (c));
					return false;
				}
			} while (true);
		}

		bool ReadAttributeName (out HtmlToken token)
		{
			token = null;

			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					name.Clear ();
					tag = null;

					return EmitDataToken (out token);
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\n': case '\f': case ' ':
					TokenizerState = HtmlTokenizerState.AfterAttributeName;
					break;
				case '/':
					TokenizerState = HtmlTokenizerState.SelfClosingStartTag;
					break;
				case '=':
					TokenizerState = HtmlTokenizerState.BeforeAttributeValue;
					break;
				case '>':
					EmitTagAttribute ();

					return EmitTagToken (out token);
				default:
					name.Append (c == '\0' ? '\uFFFD' : ToLower (c));
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.AttributeName);

			EmitTagAttribute ();

			return false;
		}

		bool ReadAfterAttributeName (out HtmlToken token)
		{
			token = null;

			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					tag = null;

					return EmitDataToken (out token);
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\n': case '\f': case ' ':
					break;
				case '/':
					TokenizerState = HtmlTokenizerState.SelfClosingStartTag;
					return false;
				case '=':
					TokenizerState = HtmlTokenizerState.BeforeAttributeValue;
					return false;
				case '>':
					return EmitTagToken (out token);
				case '"': case '\'': case '<':
					// parse error
					goto default;
				default:
					TokenizerState = HtmlTokenizerState.AttributeName;
					name.Append (c == '\0' ? '\uFFFD' : ToLower (c));
					return false;
				}
			} while (true);
		}

		bool ReadBeforeAttributeValue (out HtmlToken token)
		{
			token = null;

			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					tag = null;

					return EmitDataToken (out token);
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\n': case '\f': case ' ':
					break;
				case '"': case '\'': TokenizerState = HtmlTokenizerState.AttributeValueQuoted; quote = c; return false;
				case '&': TokenizerState = HtmlTokenizerState.AttributeValueUnquoted; return false;
				case '/':
					TokenizerState = HtmlTokenizerState.SelfClosingStartTag;
					return false;
				case '>':
					return EmitTagToken (out token);
				case '<': case '=': case '`':
					// parse error
					goto default;
				default:
					TokenizerState = HtmlTokenizerState.AttributeName;
					name.Append (c == '\0' ? '\uFFFD' : ToLower (c));
					return false;
				}
			} while (true);
		}

		bool ReadAttributeValueQuoted (out HtmlToken token)
		{
			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					name.Clear ();

					return EmitDataToken (out token);
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '&':
					TokenizerState = HtmlTokenizerState.CharacterReferenceInAttributeValue;
					token = null;
					return false;
				default:
					if (c == quote) {
						TokenizerState = HtmlTokenizerState.AfterAttributeValueQuoted;
						break;
					}

					name.Append (c == '\0' ? '\uFFFD' : c);
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.AttributeValueQuoted);

			attribute.Value = name.ToString ();
			name.Clear ();
			token = null;

			return false;
		}

		bool ReadAttributeValueUnquoted (out HtmlToken token)
		{
			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					name.Clear ();

					return EmitDataToken (out token);
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\n': case '\f': case ' ':
					TokenizerState = HtmlTokenizerState.BeforeAttributeName;
					break;
				case '&':
					TokenizerState = HtmlTokenizerState.CharacterReferenceInAttributeValue;
					token = null;
					return false;
				case '>':
					return EmitTagToken (out token);
				case '\'': case '<': case '=': case '`':
					// parse error
					goto default;
				default:
					if (c == quote) {
						TokenizerState = HtmlTokenizerState.AfterAttributeValueQuoted;
						break;
					}

					name.Append (c == '\0' ? '\uFFFD' : c);
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.AttributeValueUnquoted);

			attribute.Value = name.ToString ();
			name.Clear ();
			token = null;

			return false;
		}

		bool ReadCharacterReferenceInAttributeValue (out HtmlToken token)
		{
			char additionalAllowedCharacter = quote == '\0' ? '>' : quote;
			int nc = text.Peek ();
			bool consume;
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				data.Append ('&');
				name.Clear ();

				return EmitDataToken (out token);
			}

			c = (char) nc;
			token = null;

			switch (c) {
			case '\t': case '\n': case '\f': case ' ': case '<': case '&':
				// no character is consumed, emit '&'
				data.Append ('&');
				name.Append ('&');
				consume = false;
				break;
			default:
				if (c == additionalAllowedCharacter) {
					// this is not a character reference, nothing is consumed
					data.Append ('&');
					name.Append ('&');
					consume = false;
					break;
				}

				entity.Push ('&');

				while (entity.Push (c)) {
					text.Read ();

					if ((nc = text.Peek ()) == -1) {
						TokenizerState = HtmlTokenizerState.EndOfFile;
						data.Append (entity.GetValue ());
						entity.Reset ();

						return EmitDataToken (out token);
					}

					c = (char) nc;
				}

				string value;
				if (c == '=' || IsAlphaNumeric (c))
					value = entity.GetPushedInput ();
				else
					value = entity.GetValue ();

				data.Append (value);
				name.Append (value);
				consume = c == ';';
				entity.Reset ();
				break;
			}

			if (quote == '\0')
				TokenizerState = HtmlTokenizerState.AttributeValueUnquoted;
			else
				TokenizerState = HtmlTokenizerState.AttributeValueQuoted;

			if (consume)
				text.Read ();

			return false;
		}

		bool ReadAfterAttributeValueQuoted (out HtmlToken token)
		{
			int nc = text.Peek ();
			bool consume;
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				return EmitDataToken (out token);
			}

			c = (char) nc;
			token = null;

			switch (c) {
			case '\t': case '\n': case '\f': case ' ':
				TokenizerState = HtmlTokenizerState.BeforeAttributeName;
				consume = true;
				break;
			case '/':
				TokenizerState = HtmlTokenizerState.SelfClosingStartTag;
				consume = true;
				break;
			case '>':
				EmitTagToken (out token);
				consume = true;
				break;
			default:
				TokenizerState = HtmlTokenizerState.BeforeAttributeName;
				consume = false;
				break;
			}

			if (consume)
				text.Read ();

			return token != null;
		}

		bool ReadSelfClosingStartTag (out HtmlToken token)
		{
			int nc = text.Read ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				return EmitDataToken (out token);
			}

			c = (char) nc;

			if (c == '>') {
				tag.IsEmptyElement = true;

				return EmitTagToken (out token);
			}

			// parse error
			TokenizerState = HtmlTokenizerState.BeforeAttributeName;

			// Note: we save the data in case we hit a parse error and have to emit a data token
			data.Append (c);

			token = null;

			return false;
		}

		bool ReadBogusComment (out HtmlToken token)
		{
			int nc;
			char c;

			if (data.Length > 0) {
				c = data[data.Length - 1];
				data.Length = 1;
				data[0] = c;
			}

			do {
				if ((nc = text.Read ()) == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					break;
				}

				if ((c = (char) nc) == '>')
					break;

				data.Append (c == '\0' ? '\uFFFD' : c);
			} while (true);

			token = new HtmlCommentToken (data.ToString ());
			data.Clear ();

			return true;
		}

		bool ReadMarkupDeclarationOpen (out HtmlToken token)
		{
			int count = 0, nc;
			char c = '\0';

			while (count < 2) {
				if ((nc = text.Peek ()) == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					return EmitDataToken (out token);
				}

				if ((c = (char) nc) != '-')
					break;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);
				text.Read ();
				count++;
			}

			token = null;

			if (count == 2) {
				TokenizerState = HtmlTokenizerState.CommentStart;
				name.Clear ();
				return false;
			}

			if (count == 1) {
				// parse error
				TokenizerState = HtmlTokenizerState.BogusComment;
				return false;
			}

			if (c == 'D' || c == 'd') {
				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);
				text.Read ();
				count = 1;

				while (count < 7) {
					if ((nc = text.Read ()) == -1) {
						TokenizerState = HtmlTokenizerState.EndOfFile;
						return EmitDataToken (out token);
					}

					if (ToLower ((c = (char) nc)) != DocType[count])
						break;

					// Note: we save the data in case we hit a parse error and have to emit a data token
					data.Append (c);
					count++;
				}

				if (count == 7) {
					TokenizerState = HtmlTokenizerState.DocType;
					return false;
				}
			} else if (c == '[') {
				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);
				text.Read ();
				count = 1;

				while (count < 7) {
					if ((nc = text.Read ()) == -1) {
						TokenizerState = HtmlTokenizerState.EndOfFile;
						return EmitDataToken (out token);
					}

					if ((c = (char) nc) != CData[count])
						break;

					// Note: we save the data in case we hit a parse error and have to emit a data token
					data.Append (c);
					count++;
				}

				if (count == 7) {
					TokenizerState = HtmlTokenizerState.CDataSection;
					return false;
				}
			}

			// parse error
			TokenizerState = HtmlTokenizerState.BogusComment;

			return false;
		}

		bool ReadCommentStart (out HtmlToken token)
		{
			int nc = text.Read ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.Data;
				token = new HtmlCommentToken (string.Empty);
				return true;
			}

			c = (char) nc;

			data.Append (c);

			switch (c) {
			case '-':
				TokenizerState = HtmlTokenizerState.CommentStartDash;
				break;
			case '>': // parse error
				TokenizerState = HtmlTokenizerState.Data;
				token = new HtmlCommentToken (string.Empty);
				data.Clear ();
				return true;
			default: // parse error
				TokenizerState = HtmlTokenizerState.Comment;
				name.Append (c == '\0' ? '\uFFFD' : c);
				break;
			}

			token = null;

			return false;
		}

		bool ReadCommentStartDash (out HtmlToken token)
		{
			int nc = text.Read ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.Data;
				token = new HtmlCommentToken (name.ToString ());
				name.Clear ();
				return true;
			}

			c = (char) nc;

			data.Append (c);

			switch (c) {
			case '-':
				TokenizerState = HtmlTokenizerState.CommentEnd;
				break;
			case '>': // parse error
				TokenizerState = HtmlTokenizerState.Data;
				token = new HtmlCommentToken (name.ToString ());
				data.Clear ();
				name.Clear ();
				return true;
			default: // parse error
				TokenizerState = HtmlTokenizerState.Comment;
				name.Append ('-');
				name.Append (c == '\0' ? '\uFFFD' : c);
				break;
			}

			token = null;

			return false;
		}

		bool ReadComment (out HtmlToken token)
		{
			token = null;

			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					token = new HtmlCommentToken (name.ToString ());
					data.Clear ();
					name.Clear ();
					return true;
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '-':
					TokenizerState = HtmlTokenizerState.CommentEndDash;
					return false;
				default:
					name.Append (c == '\0' ? '\uFFFD' : c);
					break;
				}
			} while (true);
		}

		// FIXME: this is exactly the same as ReadCommentStartDash
		bool ReadCommentEndDash (out HtmlToken token)
		{
			int nc = text.Read ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.Data;
				token = new HtmlCommentToken (name.ToString ());
				name.Clear ();
				return true;
			}

			c = (char) nc;

			data.Append (c);

			switch (c) {
			case '-':
				TokenizerState = HtmlTokenizerState.CommentEnd;
				break;
			case '>': // parse error
				TokenizerState = HtmlTokenizerState.Data;
				token = new HtmlCommentToken (name.ToString ());
				data.Clear ();
				name.Clear ();
				return true;
			default: // parse error
				TokenizerState = HtmlTokenizerState.Comment;
				name.Append ('-');
				name.Append (c == '\0' ? '\uFFFD' : c);
				break;
			}

			token = null;

			return false;
		}

		bool ReadCommentEnd (out HtmlToken token)
		{
			token = null;

			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					token = new HtmlCommentToken (name.ToString ());
					data.Clear ();
					name.Clear ();
					return true;
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '>':
					TokenizerState = HtmlTokenizerState.Data;
					token = new HtmlCommentToken (name.ToString ());
					data.Clear ();
					name.Clear ();
					return true;
				case '!': // parse error
					TokenizerState = HtmlTokenizerState.CommentEndBang;
					return false;
				case '-':
					name.Append ('-');
					break;
				default:
					TokenizerState = HtmlTokenizerState.Comment;
					name.Append (c == '\0' ? '\uFFFD' : c);
					return false;
				}
			} while (true);
		}

		bool ReadCommentEndBang (out HtmlToken token)
		{
			int nc = text.Read ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				token = new HtmlCommentToken (name.ToString ());
				data.Clear ();
				name.Clear ();
				return true;
			}

			c = (char) nc;

			data.Append (c);

			switch (c) {
			case '-':
				TokenizerState = HtmlTokenizerState.CommentEndDash;
				name.Append ("--!");
				break;
			case '>':
				TokenizerState = HtmlTokenizerState.Data;
				token = new HtmlCommentToken (name.ToString ());
				data.Clear ();
				name.Clear ();
				return true;
			default: // parse error
				TokenizerState = HtmlTokenizerState.Comment;
				name.Append ("--!");
				name.Append (c == '\0' ? '\uFFFD' : c);
				break;
			}

			token = null;

			return false;
		}

		bool ReadDocType (out HtmlToken token)
		{
			int nc = text.Peek ();
			char c;

			if (nc == -1) {
				token = new HtmlDocTypeToken { ForceQuirksMode = true };
				TokenizerState = HtmlTokenizerState.EndOfFile;
				data.Clear ();
				return true;
			}

			TokenizerState = HtmlTokenizerState.BeforeDocTypeName;
			c = (char) nc;
			token = null;

			switch (c) {
			case '\t': case '\n': case '\f': case ' ':
				data.Append (c);
				text.Read ();
				break;
			}

			return false;
		}

		bool ReadBeforeDocTypeName (out HtmlToken token)
		{
			token = null;

			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					token = new HtmlDocTypeToken { ForceQuirksMode = true };
					TokenizerState = HtmlTokenizerState.EndOfFile;
					data.Clear ();
					return true;
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\n': case '\f': case ' ':
					break;
				case '>':
					token = new HtmlDocTypeToken { ForceQuirksMode = true };
					TokenizerState = HtmlTokenizerState.Data;
					data.Clear ();
					return true;
				case '\0':
					TokenizerState = HtmlTokenizerState.DocTypeName;
					doctype = new HtmlDocTypeToken ();
					name.Append ('\uFFFD');
					return false;
				default:
					TokenizerState = HtmlTokenizerState.DocTypeName;
					doctype = new HtmlDocTypeToken ();
					name.Append (ToLower (c));
					return false;
				}
			} while (true);
		}

		bool ReadDocTypeName (out HtmlToken token)
		{
			token = null;

			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					doctype.Name = name.ToString ();
					doctype.ForceQuirksMode = true;
					token = doctype;
					data.Clear ();
					name.Clear ();
					return true;
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\n': case '\f': case ' ':
					TokenizerState = HtmlTokenizerState.AfterDocTypeName;
					break;
				case '>':
					TokenizerState = HtmlTokenizerState.Data;
					doctype.Name = name.ToString ();
					token = doctype;
					doctype = null;
					data.Clear ();
					name.Clear ();
					return true;
				case '\0':
					name.Append ('\uFFFD');
					break;
				default:
					name.Append (ToLower (c));
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.DocTypeName);

			doctype.Name = name.ToString ();
			name.Clear ();

			return false;
		}

		bool ReadAfterDocTypeName (out HtmlToken token)
		{
			token = null;

			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					doctype.ForceQuirksMode = true;
					token = doctype;
					doctype = null;
					data.Clear ();
					return true;
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\n': case '\f': case ' ':
					break;
				case '>':
					TokenizerState = HtmlTokenizerState.Data;
					token = doctype;
					doctype = null;
					data.Clear ();
					return true;
				default:
					name.Append (ToLower (c));
					if (name.Length < 6)
						break;

					switch (name.ToString ()) {
					case "public": TokenizerState = HtmlTokenizerState.AfterDocTypePublicKeyword; break;
					case "system": TokenizerState = HtmlTokenizerState.AfterDocTypeSystemKeyword; break;
					default: TokenizerState = HtmlTokenizerState.BogusDocType; break;
					}

					name.Clear ();
					return false;
				}
			} while (true);
		}

		public bool ReadAfterDocTypePublicKeyword (out HtmlToken token)
		{
			int nc = text.Read ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				doctype.ForceQuirksMode = true;
				token = doctype;
				doctype = null;
				data.Clear ();
				return true;
			}

			c = (char) nc;

			// Note: we save the data in case we hit a parse error and have to emit a data token
			data.Append (c);

			switch (c) {
			case '\t': case '\n': case '\f': case ' ':
				TokenizerState = HtmlTokenizerState.BeforeDocTypePublicIdentifier;
				break;
			case '"': case '\'': // parse error
				TokenizerState = HtmlTokenizerState.DocTypePublicIdentifierQuoted;
				doctype.PublicIdentifier = string.Empty;
				quote = c;
				break;
			case '>': // parse error
				TokenizerState = HtmlTokenizerState.Data;
				doctype.ForceQuirksMode = true;
				token = doctype;
				doctype = null;
				data.Clear ();
				return true;
			default: // parse error
				TokenizerState = HtmlTokenizerState.BogusDocType;
				doctype.ForceQuirksMode = true;
				break;
			}

			token = null;

			return false;
		}

		public bool ReadBeforeDocTypePublicIdentifier (out HtmlToken token)
		{
			token = null;

			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					doctype.ForceQuirksMode = true;
					token = doctype;
					doctype = null;
					data.Clear ();
					return true;
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\n': case '\f': case ' ':
					break;
				case '"': case '\'':
					TokenizerState = HtmlTokenizerState.DocTypePublicIdentifierQuoted;
					doctype.PublicIdentifier = string.Empty;
					quote = c;
					return false;
				case '>': // parse error
					TokenizerState = HtmlTokenizerState.Data;
					doctype.ForceQuirksMode = true;
					token = doctype;
					doctype = null;
					data.Clear ();
					return true;
				default: // parse error
					TokenizerState = HtmlTokenizerState.BogusDocType;
					doctype.ForceQuirksMode = true;
					return false;
				}
			} while (true);
		}

		bool ReadDocTypePublicIdentifierQuoted (out HtmlToken token)
		{
			token = null;

			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					doctype.PublicIdentifier = name.ToString ();
					doctype.ForceQuirksMode = true;
					token = doctype;
					doctype = null;
					data.Clear ();
					name.Clear ();
					return true;
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\0': // parse error
					name.Append ('\uFFFD');
					break;
				case '>': // parse error
					TokenizerState = HtmlTokenizerState.Data;
					doctype.PublicIdentifier = name.ToString ();
					doctype.ForceQuirksMode = true;
					token = doctype;
					doctype = null;
					data.Clear ();
					name.Clear ();
					return true;
				default:
					if (c == quote) {
						TokenizerState = HtmlTokenizerState.AfterDocTypePublicIdentifier;
						break;
					}

					name.Append (c);
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.DocTypePublicIdentifierQuoted);

			doctype.PublicIdentifier = name.ToString ();
			name.Clear ();

			return false;
		}

		public bool ReadAfterDocTypePublicIdentifier (out HtmlToken token)
		{
			int nc = text.Read ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				doctype.ForceQuirksMode = true;
				token = doctype;
				doctype = null;
				data.Clear ();
				return true;
			}

			c = (char) nc;

			// Note: we save the data in case we hit a parse error and have to emit a data token
			data.Append (c);

			switch (c) {
			case '\t': case '\n': case '\f': case ' ':
				TokenizerState = HtmlTokenizerState.BetweenDocTypePublicAndSystemIdentifiers;
				break;
			case '>':
				TokenizerState = HtmlTokenizerState.Data;
				token = doctype;
				doctype = null;
				data.Clear ();
				return true;
			case '"': case '\'': // parse error
				TokenizerState = HtmlTokenizerState.DocTypeSystemIdentifierQuoted;
				doctype.SystemIdentifier = string.Empty;
				quote = c;
				break;
			default: // parse error
				TokenizerState = HtmlTokenizerState.BogusDocType;
				doctype.ForceQuirksMode = true;
				break;
			}

			token = null;

			return false;
		}

		bool ReadBetweenDocTypePublicAndSystemIdentifiers (out HtmlToken token)
		{
			token = null;

			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					doctype.ForceQuirksMode = true;
					token = doctype;
					doctype = null;
					data.Clear ();
					return true;
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\n': case '\f': case ' ':
					break;
				case '>':
					TokenizerState = HtmlTokenizerState.Data;
					token = doctype;
					doctype = null;
					data.Clear ();
					return true;
				case '"': case '\'':
					TokenizerState = HtmlTokenizerState.DocTypeSystemIdentifierQuoted;
					doctype.SystemIdentifier = string.Empty;
					quote = c;
					return false;
				default: // parse error
					TokenizerState = HtmlTokenizerState.BogusDocType;
					doctype.ForceQuirksMode = true;
					return false;
				}
			} while (true);
		}

		bool ReadAfterDocTypeSystemKeyword (out HtmlToken token)
		{
			int nc = text.Read ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				doctype.ForceQuirksMode = true;
				token = doctype;
				doctype = null;
				data.Clear ();
				return true;
			}

			c = (char) nc;

			// Note: we save the data in case we hit a parse error and have to emit a data token
			data.Append (c);

			switch (c) {
			case '\t': case '\n': case '\f': case ' ':
				TokenizerState = HtmlTokenizerState.BeforeDocTypeSystemIdentifier;
				break;
			case '"': case '\'': // parse error
				TokenizerState = HtmlTokenizerState.DocTypeSystemIdentifierQuoted;
				doctype.SystemIdentifier = string.Empty;
				quote = c;
				break;
			case '>': // parse error
				TokenizerState = HtmlTokenizerState.Data;
				doctype.ForceQuirksMode = true;
				token = doctype;
				doctype = null;
				data.Clear ();
				return true;
			default: // parse error
				TokenizerState = HtmlTokenizerState.BogusDocType;
				doctype.ForceQuirksMode = true;
				break;
			}

			token = null;

			return false;
		}

		bool ReadBeforeDocTypeSystemIdentifier (out HtmlToken token)
		{
			token = null;

			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					doctype.ForceQuirksMode = true;
					token = doctype;
					doctype = null;
					data.Clear ();
					return true;
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\n': case '\f': case ' ':
					break;
				case '"': case '\'':
					TokenizerState = HtmlTokenizerState.DocTypeSystemIdentifierQuoted;
					doctype.SystemIdentifier = string.Empty;
					quote = c;
					return false;
				case '>': // parse error
					TokenizerState = HtmlTokenizerState.Data;
					doctype.ForceQuirksMode = true;
					token = doctype;
					doctype = null;
					data.Clear ();
					return true;
				default: // parse error
					TokenizerState = HtmlTokenizerState.BogusDocType;
					doctype.ForceQuirksMode = true;
					return false;
				}
			} while (true);
		}

		bool ReadDocTypeSystemIdentifierQuoted (out HtmlToken token)
		{
			token = null;

			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					doctype.SystemIdentifier = name.ToString ();
					doctype.ForceQuirksMode = true;
					token = doctype;
					doctype = null;
					data.Clear ();
					name.Clear ();
					return true;
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\0': // parse error
					name.Append ('\uFFFD');
					break;
				case '>': // parse error
					TokenizerState = HtmlTokenizerState.Data;
					doctype.SystemIdentifier = name.ToString ();
					doctype.ForceQuirksMode = true;
					token = doctype;
					doctype = null;
					data.Clear ();
					name.Clear ();
					return true;
				default:
					if (c == quote) {
						TokenizerState = HtmlTokenizerState.AfterDocTypeSystemIdentifier;
						break;
					}

					name.Append (c);
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.DocTypeSystemIdentifierQuoted);

			doctype.SystemIdentifier = name.ToString ();
			name.Clear ();

			return false;
		}

		public bool ReadAfterDocTypeSystemIdentifier (out HtmlToken token)
		{
			token = null;

			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					doctype.ForceQuirksMode = true;
					token = doctype;
					doctype = null;
					data.Clear ();
					return true;
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\n': case '\f': case ' ':
					break;
				case '>':
					TokenizerState = HtmlTokenizerState.Data;
					token = doctype;
					doctype = null;
					data.Clear ();
					return true;
				default: // parse error
					TokenizerState = HtmlTokenizerState.BogusDocType;
					return false;
				}
			} while (true);
		}

		bool ReadBogusDocType (out HtmlToken token)
		{
			token = null;

			do {
				int nc = text.Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					doctype.ForceQuirksMode = true;
					token = doctype;
					doctype = null;
					data.Clear ();
					return true;
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				if (c == '>') {
					TokenizerState = HtmlTokenizerState.Data;
					token = doctype;
					doctype = null;
					data.Clear ();
					return true;
				}
			} while (true);
		}

		bool ReadCDataSection (out HtmlToken token)
		{
			int nc = text.Read ();

			while (nc != -1) {
				char c = (char) nc;

				if (cdataIndex >= 3) {
					data.Append (cdata[0]);
					cdata[0] = cdata[1];
					cdata[1] = cdata[2];
					cdata[2] = c;

					if (cdata[0] == ']' && cdata[1] == ']' && cdata[2] == '>') {
						TokenizerState = HtmlTokenizerState.Data;
						cdataIndex = 0;

						return EmitDataToken (out token);
					}

					if (data.Length > 1024)
						return EmitDataToken (out token);
				} else {
					cdata[cdataIndex++] = c;
				}

				nc = text.Read ();
			}

			TokenizerState = HtmlTokenizerState.EndOfFile;

			for (int i = 0; i < cdataIndex; i++)
				data.Append (cdata[i]);

			cdataIndex = 0;

			return EmitDataToken (out token);
		}

		public bool ReadNextToken (out HtmlToken token)
		{
			do {
				switch (TokenizerState) {
				case HtmlTokenizerState.Data:
					if (ReadDataToken (out token))
						return true;
					break;
				case HtmlTokenizerState.CharacterReferenceInData:
					if (ReadCharacterReferenceInData (out token))
						return true;
					break;
				case HtmlTokenizerState.RcData:
					if (ReadRcData (out token))
						return true;
					break;
				case HtmlTokenizerState.CharacterReferenceInRcData:
					if (ReadCharacterReferenceInRcData (out token))
						return true;
					break;
				case HtmlTokenizerState.RawText:
					if (ReadRawText (out token))
						return true;
					break;
				case HtmlTokenizerState.ScriptData:
					if (ReadScriptData (out token))
						return true;
					break;
				case HtmlTokenizerState.PlainText:
					if (ReadPlainText (out token))
						return true;
					break;
				case HtmlTokenizerState.TagOpen:
					if (ReadTagOpen (out token))
						return true;
					break;
				case HtmlTokenizerState.EndTagOpen:
					if (ReadEndTagOpen (out token))
						return true;
					break;
				case HtmlTokenizerState.TagName:
					if (ReadTagName (out token))
						return true;
					break;
				case HtmlTokenizerState.RcDataLessThan:
					if (ReadRcDataLessThan (out token))
						return true;
					break;
				case HtmlTokenizerState.RcDataEndTagOpen:
					if (ReadRcDataEndTagOpen (out token))
						return true;
					break;
				case HtmlTokenizerState.RcDataEndTagName:
					if (ReadRcDataEndTagName (out token))
						return true;
					break;
				case HtmlTokenizerState.RawTextLessThan:
					if (ReadRawTextLessThan (out token))
						return true;
					break;
				case HtmlTokenizerState.RawTextEndTagOpen:
					if (ReadRawTextEndTagOpen (out token))
						return true;
					break;
				case HtmlTokenizerState.RawTextEndTagName:
					if (ReadRawTextEndTagName (out token))
						return true;
					break;
				case HtmlTokenizerState.ScriptDataLessThan:
					if (ReadScriptDataLessThan (out token))
						return true;
					break;
				case HtmlTokenizerState.ScriptDataEndTagOpen:
					if (ReadScriptDataEndTagOpen (out token))
						return true;
					break;
				case HtmlTokenizerState.ScriptDataEndTagName:
					if (ReadScriptDataEndTagName (out token))
						return true;
					break;
				case HtmlTokenizerState.ScriptDataEscapeStart:
					if (ReadScriptDataEscapeStart (out token))
						return true;
					break;
				case HtmlTokenizerState.ScriptDataEscapeStartDash:
					if (ReadScriptDataEscapeStartDash (out token))
						return true;
					break;
				case HtmlTokenizerState.ScriptDataEscaped:
					if (ReadScriptDataEscaped (out token))
						return true;
					break;
				case HtmlTokenizerState.ScriptDataEscapedDash:
					if (ReadScriptDataEscapedDash (out token))
						return true;
					break;
				case HtmlTokenizerState.ScriptDataEscapedDashDash:
					if (ReadScriptDataEscapedDashDash (out token))
						return true;
					break;
				case HtmlTokenizerState.ScriptDataEscapedLessThan:
					if (ReadScriptDataEscapedLessThan (out token))
						return true;
					break;
				case HtmlTokenizerState.ScriptDataEscapedEndTagOpen:
					if (ReadScriptDataEscapedEndTagOpen (out token))
						return true;
					break;
				case HtmlTokenizerState.ScriptDataEscapedEndTagName:
					if (ReadScriptDataEscapedEndTagName (out token))
						return true;
					break;
				case HtmlTokenizerState.ScriptDataDoubleEscapeStart:
					if (ReadScriptDataDoubleEscapeStart (out token))
						return true;
					break;
				case HtmlTokenizerState.ScriptDataDoubleEscaped:
					if (ReadScriptDataDoubleEscaped (out token))
						return true;
					break;
				case HtmlTokenizerState.ScriptDataDoubleEscapedDash:
					if (ReadScriptDataDoubleEscapedDash (out token))
						return true;
					break;
				case HtmlTokenizerState.ScriptDataDoubleEscapedDashDash:
					if (ReadScriptDataDoubleEscapedDashDash (out token))
						return true;
					break;
				case HtmlTokenizerState.ScriptDataDoubleEscapedLessThan:
					if (ReadScriptDataDoubleEscapedLessThan (out token))
						return true;
					break;
				case HtmlTokenizerState.ScriptDataDoubleEscapeEnd:
					if (ReadScriptDataDoubleEscapeEnd (out token))
						return true;
					break;
				case HtmlTokenizerState.BeforeAttributeName:
					if (ReadBeforeAttributeName (out token))
						return true;
					break;
				case HtmlTokenizerState.AttributeName:
					if (ReadAttributeName (out token))
						return true;
					break;
				case HtmlTokenizerState.AfterAttributeName:
					if (ReadAfterAttributeName (out token))
						return true;
					break;
				case HtmlTokenizerState.BeforeAttributeValue:
					if (ReadBeforeAttributeValue (out token))
						return true;
					break;
				case HtmlTokenizerState.AttributeValueQuoted:
					if (ReadAttributeValueQuoted (out token))
						return true;
					break;
				case HtmlTokenizerState.AttributeValueUnquoted:
					if (ReadAttributeValueUnquoted (out token))
						return true;
					break;
				case HtmlTokenizerState.CharacterReferenceInAttributeValue:
					if (ReadCharacterReferenceInAttributeValue (out token))
						return true;
					break;
				case HtmlTokenizerState.AfterAttributeValueQuoted:
					if (ReadAfterAttributeValueQuoted (out token))
						return true;
					break;
				case HtmlTokenizerState.SelfClosingStartTag:
					if (ReadSelfClosingStartTag (out token))
						return true;
					break;
				case HtmlTokenizerState.BogusComment:
					if (ReadBogusComment (out token))
						return true;
					break;
				case HtmlTokenizerState.MarkupDeclarationOpen:
					if (ReadMarkupDeclarationOpen (out token))
						return true;
					break;
				case HtmlTokenizerState.CommentStart:
					if (ReadCommentStart (out token))
						return true;
					break;
				case HtmlTokenizerState.CommentStartDash:
					if (ReadCommentStartDash (out token))
						return true;
					break;
				case HtmlTokenizerState.Comment:
					if (ReadComment (out token))
						return true;
					break;
				case HtmlTokenizerState.CommentEndDash:
					if (ReadCommentEndDash (out token))
						return true;
					break;
				case HtmlTokenizerState.CommentEnd:
					if (ReadCommentEnd (out token))
						return true;
					break;
				case HtmlTokenizerState.CommentEndBang:
					if (ReadCommentEndBang (out token))
						return true;
					break;
				case HtmlTokenizerState.DocType:
					if (ReadDocType (out token))
						return true;
					break;
				case HtmlTokenizerState.BeforeDocTypeName:
					if (ReadBeforeDocTypeName (out token))
						return true;
					break;
				case HtmlTokenizerState.DocTypeName:
					if (ReadDocTypeName (out token))
						return true;
					break;
				case HtmlTokenizerState.AfterDocTypeName:
					if (ReadAfterDocTypeName (out token))
						return true;
					break;
				case HtmlTokenizerState.AfterDocTypePublicKeyword:
					if (ReadAfterDocTypePublicKeyword (out token))
						return true;
					break;
				case HtmlTokenizerState.BeforeDocTypePublicIdentifier:
					if (ReadBeforeDocTypePublicIdentifier (out token))
						return true;
					break;
				case HtmlTokenizerState.DocTypePublicIdentifierQuoted:
					if (ReadDocTypePublicIdentifierQuoted (out token))
						return true;
					break;
				case HtmlTokenizerState.AfterDocTypePublicIdentifier:
					if (ReadAfterDocTypePublicIdentifier (out token))
						return true;
					break;
				case HtmlTokenizerState.BetweenDocTypePublicAndSystemIdentifiers:
					if (ReadBetweenDocTypePublicAndSystemIdentifiers (out token))
						return true;
					break;
				case HtmlTokenizerState.AfterDocTypeSystemKeyword:
					if (ReadAfterDocTypeSystemKeyword (out token))
						return true;
					break;
				case HtmlTokenizerState.BeforeDocTypeSystemIdentifier:
					if (ReadBeforeDocTypeSystemIdentifier (out token))
						return true;
					break;
				case HtmlTokenizerState.DocTypeSystemIdentifierQuoted:
					if (ReadDocTypeSystemIdentifierQuoted (out token))
						return true;
					break;
				case HtmlTokenizerState.AfterDocTypeSystemIdentifier:
					if (ReadAfterDocTypeSystemIdentifier (out token))
						return true;
					break;
				case HtmlTokenizerState.BogusDocType:
					if (ReadBogusDocType (out token))
						return true;
					break;
				case HtmlTokenizerState.CDataSection:
					if (ReadCDataSection (out token))
						return true;
					break;
				case HtmlTokenizerState.EndOfFile:
					token = null;
					return false;
				}
			} while (true);
		}
	}
}
