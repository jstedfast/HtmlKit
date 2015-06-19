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
	/// <summary>
	/// An HTML tokenizer.
	/// </summary>
	/// <remarks>
	/// Tokenizes HTML text, emitting an <see cref="HtmlToken"/> for each token it encounters.
	/// </remarks>
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

		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlTokenizer"/> class.
		/// </summary>
		/// <remarks>
		/// Creates a new <see cref="HtmlTokenizer"/>.
		/// </remarks>
		/// <param name="reader">The <see cref="TextReader"/>.</param>
		public HtmlTokenizer (TextReader reader)
		{
			DecodeCharacterReferences = true;
			LinePosition = 1;
			LineNumber = 1;
			text = reader;
		}

		/// <summary>
		/// Get or set whether or not the tokenizer should decode character references.
		/// </summary>
		/// <remarks>
		/// <para>Gets or sets whether or not the tokenizer should decode character references.</para>
		/// <para>Note: Character references in attribute values will still be decoded even if this
		/// value is set to <c>false</c>.</para>
		/// </remarks>
		/// <value><c>true</c> if character references should be decoded; otherwise, <c>false</c>.</value>
		public bool DecodeCharacterReferences {
			get; set;
		}

		/// <summary>
		/// Get the current HTML namespace detected by the tokenizer.
		/// </summary>
		/// <remarks>
		/// Gets the current HTML namespace detected by the tokenizer.
		/// </remarks>
		/// <value>The html namespace.</value>
		public HtmlNamespace HtmlNamespace {
			get; private set;
		}

		/// <summary>
		/// Gets the current line number.
		/// </summary>
		/// <remarks>
		/// <para>This property is most commonly used for error reporting, but can be called
		/// at any time. The starting value for this property is <c>1</c>.</para>
		/// <para>Combined with <see cref="LinePosition"/>, a value of <c>1,1</c> indicates
		/// the start of the document.</para>
		/// </remarks>
		/// <value>The current line number.</value>
		public int LineNumber {
			get; private set;
		}

		/// <summary>
		/// Gets the current line position.
		/// </summary>
		/// <remarks>
		/// <para>This property is most commonly used for error reporting, but can be called
		/// at any time. The starting value for this property is <c>1</c>.</para>
		/// <para>Combined with <see cref="LineNumber"/>, a value of <c>1,1</c> indicates
		/// the start of the document.</para>
		/// </remarks>
		/// <value>The current line number.</value>
		public int LinePosition {
			get; private set;
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

		/// <summary>
		/// Create a DOCTYPE token.
		/// </summary>
		/// <remarks>
		/// Creates a DOCTYPE token.
		/// </remarks>
		/// <returns>The DOCTYPE token.</returns>
		protected virtual HtmlDocTypeToken CreateDocType ()
		{
			return new HtmlDocTypeToken ();
		}

		HtmlDocTypeToken CreateDocTypeToken (string rawTagName)
		{
			var token = CreateDocType ();
			token.RawTagName = rawTagName;
			return token;
		}

		/// <summary>
		/// Create an HTML comment token.
		/// </summary>
		/// <remarks>
		/// Creates an HTML comment token.
		/// </remarks>
		/// <returns>The HTML comment token.</returns>
		/// <param name="comment">The comment.</param>
		protected virtual HtmlCommentToken CreateCommentToken (string comment)
		{
			return new HtmlCommentToken (comment);
		}

		/// <summary>
		/// Create an HTML character data token.
		/// </summary>
		/// <remarks>
		/// Creates an HTML character data token.
		/// </remarks>
		/// <returns>The HTML character data token.</returns>
		/// <param name="data">The character data.</param>
		protected virtual HtmlDataToken CreateDataToken (string data)
		{
			return new HtmlDataToken (data);
		}

		/// <summary>
		/// Create an HTML character data token.
		/// </summary>
		/// <remarks>
		/// Creates an HTML character data token.
		/// </remarks>
		/// <returns>The HTML character data token.</returns>
		/// <param name="data">The character data.</param>
		protected virtual HtmlCDataToken CreateCDataToken (string data)
		{
			return new HtmlCDataToken (data);
		}

		/// <summary>
		/// Create an HTML script data token.
		/// </summary>
		/// <remarks>
		/// Creates an HTML script data token.
		/// </remarks>
		/// <returns>The HTML script data token.</returns>
		/// <param name="data">The script data.</param>
		protected virtual HtmlScriptDataToken CreateScriptDataToken (string data)
		{
			return new HtmlScriptDataToken (data);
		}

		/// <summary>
		/// Create an HTML tag token.
		/// </summary>
		/// <remarks>
		/// Creates an HTML tag token.
		/// </remarks>
		/// <returns>The HTML tag token.</returns>
		/// <param name="name">The tag name.</param>
		/// <param name="isEndTag"><c>true</c> if the tag is an end tag; otherwise, <c>false</c>.</param>
		protected virtual HtmlTagToken CreateTagToken (string name, bool isEndTag = false)
		{
			return new HtmlTagToken (name, isEndTag);
		}

		/// <summary>
		/// Create an attribute.
		/// </summary>
		/// <remarks>
		/// Creates an attribute.
		/// </remarks>
		/// <returns>The attribute.</returns>
		/// <param name="name">THe attribute name.</param>
		protected virtual HtmlAttribute CreateAttribute (string name)
		{
			return new HtmlAttribute (name);
		}

		static bool IsAlphaNumeric (char c)
		{
			return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');
		}

		static bool IsAsciiLetter (char c)
		{
			return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
		}

		static char ToLower (char c)
		{
			return (c >= 'A' && c <= 'Z') ? (char) (c + 0x20) : c;
		}

		int Peek ()
		{
			return text.Peek ();
		}

		int Read ()
		{
			int c;

			if ((c = text.Read ()) == -1)
				return -1;

			if (c == '\n') {
				LinePosition = 1;
				LineNumber++;
			} else {
				LinePosition++;
			}

			return c;
		}

		// Note: value must be lowercase
		bool NameIs (string value)
		{
			if (name.Length != value.Length)
				return false;

			for (int i = 0; i < name.Length; i++) {
				if (ToLower (name[i]) != value[i])
					return false;
			}

			return true;
		}

		void EmitTagAttribute ()
		{
			attribute = CreateAttribute (name.ToString ());
			tag.Attributes.Add (attribute);
			name.Length = 0;
		}

		HtmlToken EmitCommentToken (string comment)
		{
			var token = CreateCommentToken (comment);
			data.Length = 0;
			name.Length = 0;
			return token;
		}

		HtmlToken EmitCommentToken (StringBuilder comment)
		{
			return EmitCommentToken (comment.ToString ());
		}

		HtmlToken EmitDocType ()
		{
			var token = doctype;
			data.Length = 0;
			doctype = null;
			return token;
		}

		HtmlToken EmitDataToken (bool encodeEntities)
		{
			if (data.Length == 0)
				return null;

			var token = CreateDataToken (data.ToString ());
			token.EncodeEntities = encodeEntities;
			data.Length = 0;

			return token;
		}

		HtmlToken EmitCDataToken ()
		{
			if (data.Length == 0)
				return null;

			var token = CreateCDataToken (data.ToString ());
			data.Length = 0;

			return token;
		}

		HtmlToken EmitScriptDataToken ()
		{
			if (data.Length == 0)
				return null;

			var token = CreateScriptDataToken (data.ToString ());
			data.Length = 0;

			return token;
		}

		HtmlToken EmitTagToken ()
		{
			if (!tag.IsEndTag && !tag.IsEmptyElement) {
				switch (tag.Id) {
				case HtmlTagId.Style: case HtmlTagId.Xmp: case HtmlTagId.IFrame: case HtmlTagId.NoEmbed: case HtmlTagId.NoFrames:
					TokenizerState = HtmlTokenizerState.RawText;
					activeTagName = tag.Name;
					break;
				case HtmlTagId.Title: case HtmlTagId.TextArea:
					TokenizerState = HtmlTokenizerState.RcData;
					activeTagName = tag.Name;
					break;
				case HtmlTagId.PlainText:
					TokenizerState = HtmlTokenizerState.PlainText;
					break;
				case HtmlTagId.Script:
					TokenizerState = HtmlTokenizerState.ScriptData;
					break;
				case HtmlTagId.NoScript:
					// TODO: only switch into the RawText state if scripting is enabled
					TokenizerState = HtmlTokenizerState.RawText;
					activeTagName = tag.Name;
					break;
				case HtmlTagId.Html:
					TokenizerState = HtmlTokenizerState.Data;

					for (int i = tag.Attributes.Count; i > 0; i--) {
						var attr = tag.Attributes[i - 1];

						if (attr.Id == HtmlAttributeId.XmlNS && attr.Value != null) {
							HtmlNamespace = tag.Attributes[i].Value.ToHtmlNamespace ();
							break;
						}
					}
					break;
				default:
					TokenizerState = HtmlTokenizerState.Data;
					break;
				}
			} else {
				TokenizerState = HtmlTokenizerState.Data;
			}

			var token = tag;
			data.Length = 0;
			tag = null;

			return token;
		}
		/// <summary>
		/// 8.2.4.69 Tokenizing character references
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#tokenizing-character-references"/> 
		/// </summary>
		/// <returns>The character reference.</returns>
		/// <param name="next">Next.</param>
		HtmlToken ReadCharacterReference (HtmlTokenizerState next)
		{
			int nc = Peek ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				data.Append ('&');

				return EmitDataToken (true);
			}

			c = (char) nc;

			switch (c) {
			case '\t': case '\r': case '\n': case '\f': case ' ': case '<': case '&':
				// no character is consumed, emit '&'
				TokenizerState = next;
				data.Append ('&');
				return null;
			}

			entity.Push ('&');

			while (entity.Push (c)) {
				Read ();

				if ((nc = Peek ()) == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					data.Append (entity.GetPushedInput ());
					entity.Reset ();

					return EmitDataToken (true);
				}

				c = (char) nc;
			}

			TokenizerState = next;

			data.Append (entity.GetValue ());
			entity.Reset ();

			if (c == ';') {
				// consume the ';'
				Read ();
			}

			return null;
		}

		HtmlToken ReadGenericRawTextLessThan (HtmlTokenizerState rawText, HtmlTokenizerState rawTextEndTagOpen)
		{
			int nc = Peek ();

			data.Append ('<');

			switch ((char) nc) {
			case '/':
				TokenizerState = rawTextEndTagOpen;
				data.Append ('/');
				name.Length = 0;
				Read ();
				break;
			default:
				TokenizerState = rawText;
				break;
			}

			return null;
		}

		HtmlToken ReadGenericRawTextEndTagOpen (bool decoded, HtmlTokenizerState rawText, HtmlTokenizerState rawTextEndTagName)
		{
			int nc = Peek ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				return EmitDataToken (decoded);
			}

			c = (char) nc;

			if (IsAsciiLetter (c)) {
				TokenizerState = rawTextEndTagName;
				name.Append (c);
				data.Append (c);
				Read ();
			} else {
				TokenizerState = rawText;
			}

			return null;
		}

		HtmlToken ReadGenericRawTextEndTagName (bool decoded, HtmlTokenizerState rawText)
		{
			var current = TokenizerState;

			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					name.Length = 0;

					return EmitDataToken (decoded);
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\r': case '\n': case '\f': case ' ':
					if (NameIs (activeTagName)) {
						TokenizerState = HtmlTokenizerState.BeforeAttributeName;
						break;
					}

					goto default;
				case '/':
					if (NameIs (activeTagName)) {
						TokenizerState = HtmlTokenizerState.SelfClosingStartTag;
						break;
					}
					goto default;
				case '>':
					if (NameIs (activeTagName)) {
						var token = CreateTagToken (name.ToString (), true);
						TokenizerState = HtmlTokenizerState.Data;
						data.Length = 0;
						name.Length = 0;
						return token;
					}
					goto default;
				default:
					if (!IsAsciiLetter (c)) {
						TokenizerState = rawText;
						return null;
					}

					name.Append (c == '\0' ? '\uFFFD' : c);
					break;
				}
			} while (TokenizerState == current);

			tag = CreateTagToken (name.ToString (), true);
			name.Length = 0;

			return null;
		}
		/// <summary>
		/// 8.2.4.1 Data state 
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#data-state"/> 
		/// </summary>
		/// <returns>The data token.</returns>
		HtmlToken R01_DataToken ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					break;
				}

				c = (char) nc;

				switch (c) {
				case '&':
					if (DecodeCharacterReferences) {
						TokenizerState = HtmlTokenizerState.CharacterReferenceInData;
						return null;
					}

					goto default;
				case '<':
					TokenizerState = HtmlTokenizerState.TagOpen;
					break;
				//case 0: // parse error, but emit it anyway
				default:
					data.Append (c);
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.Data);

			return EmitDataToken (DecodeCharacterReferences);
		}
		/// <summary>
		/// 8.2.4.2 Character reference in data state
		/// <see cref=" http://www.w3.org/TR/html5/syntax.html#character-reference-in-data-state"/>
		/// </summary>
		/// <returns>The character reference in data.</returns>
		HtmlToken R02_CharacterReferenceInData ()
		{
			return ReadCharacterReference (HtmlTokenizerState.Data);
		}
		/// <summary>
		/// 8.2.4.3 RCDATA state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#rcdata-state"/> 
		/// </summary>
		/// <returns>The rc data.</returns>
		HtmlToken R03_RcData ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					break;
				}

				c = (char) nc;

				switch (c) {
				case '&':
					if (DecodeCharacterReferences) {
						TokenizerState = HtmlTokenizerState.CharacterReferenceInRcData;
						return null;
					}

					goto default;
				case '<':
					TokenizerState = HtmlTokenizerState.RcDataLessThan;
					return EmitDataToken (DecodeCharacterReferences);
				default:
					data.Append (c == '\0' ? '\uFFFD' : c);
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.RcData);

			return EmitDataToken (DecodeCharacterReferences);
		}
		/// <summary>
		/// 8.2.4.4 Character reference in RCDATA state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#character-reference-in-rcdata-state"/> 
		/// </summary>
		/// <returns>The character reference in rc data.</returns>
		HtmlToken R04_CharacterReferenceInRcData ()
		{
			return ReadCharacterReference (HtmlTokenizerState.RcData);
		}
		/// <summary>
		/// 8.2.4.5 RAWTEXT state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#rawtext-state"/> 
		/// </summary>
		/// <returns>The raw text.</returns>
		HtmlToken R05_RawText ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					break;
				}

				c = (char) nc;

				switch (c) {
				case '<':
					TokenizerState = HtmlTokenizerState.RawTextLessThan;
					return EmitDataToken (false);
				default:
					data.Append (c == '\0' ? '\uFFFD' : c);
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.RawText);

			return EmitDataToken (false);
		}
		/// <summary>
		/// 8.2.4.6 Script data state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#script-data-state"/> 
		/// </summary>
		/// <returns>The script data.</returns>
		HtmlToken R06_ScriptData ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					break;
				}

				c = (char) nc;

				switch (c) {
				case '<':
					TokenizerState = HtmlTokenizerState.ScriptDataLessThan;
					return EmitScriptDataToken ();
				default:
					data.Append (c == '\0' ? '\uFFFD' : c);
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.ScriptData);

			return EmitScriptDataToken ();
		}
		/// <summary>
		/// 8.2.4.7 PLAINTEXT state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#plaintext-state"/> 
		/// </summary>
		/// <returns>The plain text.</returns>
		HtmlToken R07_PlainText ()
		{
			int nc = Read ();

			while (nc != -1) {
				char c = (char) nc;

				data.Append (c == '\0' ? '\uFFFD' : c);
				nc = Read ();
			}

			TokenizerState = HtmlTokenizerState.EndOfFile;

			return EmitDataToken (false);
		}
		/// <summary>
		/// 8.2.4.8 Tag open state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#tag-open-state"/> 
		/// </summary>
		/// <returns>The tag open.</returns>
		HtmlToken R08_TagOpen ()
		{
			int nc = Read ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				var token = CreateDataToken ("<");
				return token;
			}

			c = (char) nc;

			// Note: we save the data in case we hit a parse error and have to emit a data token
			data.Append ('<');
			data.Append (c);

			switch ((c = (char) nc)) {
			case '!': TokenizerState = HtmlTokenizerState.MarkupDeclarationOpen; break;
			case '?': TokenizerState = HtmlTokenizerState.BogusComment; break;
			case '/': TokenizerState = HtmlTokenizerState.EndTagOpen; break;
			default:
				if (IsAsciiLetter (c)) {
					TokenizerState = HtmlTokenizerState.TagName;
					isEndTag = false;
					name.Append (c);
				} else {
					TokenizerState = HtmlTokenizerState.Data;
				}
				break;
			}

			return null;
		}
		/// <summary>
		/// 8.2.4.9 End tag open state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#end-tag-open-state"/> 
		/// </summary>
		/// <returns>The end tag open.</returns>
		HtmlToken R09_EndTagOpen ()
		{
			int nc = Read ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				return EmitDataToken (false);
			}

			c = (char) nc;

			// Note: we save the data in case we hit a parse error and have to emit a data token
			data.Append (c);

			switch (c) {
			case '>': // parse error
				TokenizerState = HtmlTokenizerState.Data;
				data.Length = 0;
				break;
			default:
				if (IsAsciiLetter (c)) {
					TokenizerState = HtmlTokenizerState.TagName;
					isEndTag = true;
					name.Append (c);
				} else {
					TokenizerState = HtmlTokenizerState.BogusComment;
				}
				break;
			}

			return null;
		}
		/// <summary>
		/// 8.2.4.10 Tag name state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#tag-name-state"/> 
		/// </summary>
		/// <returns>The tag name.</returns>
		HtmlToken R10_TagName ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					name.Length = 0;

					return EmitDataToken (false);
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\r': case '\n': case '\f': case ' ':
					TokenizerState = HtmlTokenizerState.BeforeAttributeName;
					break;
				case '/':
					TokenizerState = HtmlTokenizerState.SelfClosingStartTag;
					break;
				case '>':
					tag = CreateTagToken (name.ToString (), isEndTag);
					data.Length = 0;
					name.Length = 0;

					return EmitTagToken ();
				default:
					name.Append (c == '\0' ? '\uFFFD' : c);
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.TagName);

			tag = CreateTagToken (name.ToString (), isEndTag);
			name.Length = 0;

			return null;
		}
		/// <summary>
		/// 8.2.4.11 RCDATA less-than sign state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#rcdata-less-than-sign-state"/> 
		/// </summary>
		/// <returns>The rc data less than.</returns>
		HtmlToken R11_ReadRcDataLessThan ()
		{
			return ReadGenericRawTextLessThan (HtmlTokenizerState.RcData, HtmlTokenizerState.RcDataEndTagOpen);
		}
		/// <summary>
		/// 8.2.4.12 RCDATA end tag open state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#rcdata-end-tag-open-state"/> 
		/// </summary>
		/// <returns>The rc data end tag open.</returns>
		HtmlToken R12_RcDataEndTagOpen ()
		{
			return ReadGenericRawTextEndTagOpen (DecodeCharacterReferences, HtmlTokenizerState.RcData, HtmlTokenizerState.RcDataEndTagName);
		}
		/// <summary>
		/// 8.2.4.13 RCDATA end tag name state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#rcdata-end-tag-name-state"/> 
		/// </summary>
		/// <returns>The rc data end tag name.</returns>
		HtmlToken R13_RcDataEndTagName ()
		{
			return ReadGenericRawTextEndTagName (DecodeCharacterReferences, HtmlTokenizerState.RcData);
		}
		/// <summary>
		/// 8.2.4.14 RAWTEXT less-than sign state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#rawtext-less-than-sign-state"/> 
		/// </summary>
		/// <returns>The raw text less than.</returns>
		HtmlToken R14_RawTextLessThan ()
		{
			return ReadGenericRawTextLessThan (HtmlTokenizerState.RawText, HtmlTokenizerState.RawTextEndTagOpen);
		}
		/// <summary>
		/// 8.2.4.15 RAWTEXT end tag open state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#rawtext-end-tag-open-state"/> 
		/// </summary>
		/// <returns>The raw text end tag open.</returns>
		HtmlToken R15_RawTextEndTagOpen ()
		{
			return ReadGenericRawTextEndTagOpen (false, HtmlTokenizerState.RawText, HtmlTokenizerState.RawTextEndTagName);
		}
		/// <summary>
		/// 8.2.4.16 RAWTEXT end tag name state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#rawtext-end-tag-name-state"/> 
		/// </summary>
		/// <returns>The raw text end tag name.</returns>
		HtmlToken R16_RawTextEndTagName ()
		{
			return ReadGenericRawTextEndTagName (false, HtmlTokenizerState.RawText);
		}
		/// <summary>
		/// 8.2.4.17 Script data less-than sign state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#script-data-less-than-sign-state"/> 
		/// </summary>
		/// <returns>The script data less than.</returns>
		HtmlToken R17_ScriptDataLessThan ()
		{
			int nc = Peek ();

			data.Append ('<');

			switch ((char) nc) {
			case '/':
				TokenizerState = HtmlTokenizerState.ScriptDataEndTagOpen;
				data.Append ('/');
				name.Length = 0;
				Read ();
				break;
			case '!':
				TokenizerState = HtmlTokenizerState.ScriptDataEscapeStart;
				data.Append ('!');
				Read ();
				break;
			default:
				TokenizerState = HtmlTokenizerState.ScriptData;
				break;
			}

			return null;
		}
		/// <summary>
		/// 8.2.4.18 Script data end tag open state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#script-data-end-tag-open-state"/> 
		/// </summary>
		/// <returns>The script data end tag open.</returns>
		HtmlToken R18_ScriptDataEndTagOpen ()
		{
			int nc = Peek ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				return EmitScriptDataToken ();
			}

			c = (char) nc;

			if (c == 'S' || c == 's') {
				TokenizerState = HtmlTokenizerState.ScriptDataEndTagName;
				name.Append ('s');
				data.Append (c);
				Read ();
			} else {
				TokenizerState = HtmlTokenizerState.ScriptData;
			}

			return null;
		}
		/// <summary>
		/// 8.2.4.19 Script data end tag name state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#script-data-end-tag-name-state"/> 
		/// </summary>
		/// <returns>The script data end tag name.</returns>
		HtmlToken ReadScriptDataEndTagName ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					name.Length = 0;

					return EmitScriptDataToken ();
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\r': case '\n': case '\f': case ' ':
					if (NameIs ("script")) {
						TokenizerState = HtmlTokenizerState.BeforeAttributeName;
						break;
					}

					goto default;
				case '/':
					if (NameIs ("script")) {
						TokenizerState = HtmlTokenizerState.SelfClosingStartTag;
						break;
					}
					goto default;
				case '>':
					if (NameIs ("script")) {
						var token = CreateTagToken (name.ToString (), true);
						TokenizerState = HtmlTokenizerState.Data;
						data.Length = 0;
						name.Length = 0;
						return token;
					}
					goto default;
				default:
					if (!IsAsciiLetter (c)) {
						TokenizerState = HtmlTokenizerState.ScriptData;
						return null;
					}

					name.Append (c == '\0' ? '\uFFFD' : c);
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.ScriptDataEndTagName);

			tag = CreateTagToken (name.ToString (), true);
			name.Length = 0;

			return null;
		}
		/// <summary>
		/// 8.2.4.20 Script data escape start state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#script-data-escape-start-state"/> 
		/// </summary>
		/// <returns>The script data escape start.</returns>
		HtmlToken R20_ScriptDataEscapeStart ()
		{
			int nc = Peek ();

			if (nc == '-') {
				TokenizerState = HtmlTokenizerState.ScriptDataEscapeStartDash;
				Read ();
			} else {
				TokenizerState = HtmlTokenizerState.ScriptData;
			}

			return null;
		}
		/// <summary>
		/// 8.2.4.21 Script data escape start dash state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#script-data-escape-start-dash-state"/> 
		/// </summary>
		/// <returns>The script data escape start dash.</returns>
		HtmlToken R21_ReadScriptDataEscapeStartDash ()
		{
			int nc = Peek ();

			if (nc == '-') {
				TokenizerState = HtmlTokenizerState.ScriptDataEscapedDashDash;
				Read ();
			} else {
				TokenizerState = HtmlTokenizerState.ScriptData;
			}

			return null;
		}
		/// <summary>
		/// 8.2.4.22 Script data escaped state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#script-data-escaped-state"/> 
		/// </summary>
		/// <returns>The script data escaped.</returns>
		HtmlToken R22_ReadScriptDataEscaped ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					return EmitScriptDataToken ();
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

			return null;
		}
		/// <summary>
		/// Reads the script data escaped dash.
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#script-data-escaped-dash-state"/> 
		/// </summary>
		/// <returns>The script data escaped dash.</returns>
		HtmlToken R23_ReadScriptDataEscapedDash ()
		{
			int nc = Peek ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				return EmitScriptDataToken ();
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

			return null;
        } 
		/// <summary>
		/// 8.2.4.24 Script data escaped dash dash state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#script-data-escaped-dash-dash-state"/> 
		/// </summary>
		/// <returns>The script data escaped dash dash.</returns>
        HtmlToken R24_ScriptDataEscapedDashDash ()
		{
            
            do
            {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					return EmitScriptDataToken ();
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

			return null;
		}
		/// <summary>
		/// 8.2.4.25 Script data escaped less-than sign state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#script-data-escaped-dash-dash-state"/> 
		/// </summary>
		/// <returns>The script data escaped less than.</returns>
		HtmlToken R25_ScriptDataEscapedLessThan ()
		{
			int nc = Peek ();
			char c = (char) nc;

			if (c == '/') {
				TokenizerState = HtmlTokenizerState.ScriptDataEndTagOpen;
				name.Length = 0;
				Read ();
			} else if (IsAsciiLetter (c)) {
				TokenizerState = HtmlTokenizerState.ScriptDataDoubleEscaped;
				data.Append ('<');
				data.Append (c);
				name.Append (c);
				Read ();
			} else {
				TokenizerState = HtmlTokenizerState.ScriptDataEscaped;
				data.Append ('<');
			}

			return null;
		}
		/// <summary>
		/// 8.2.4.26 Script data escaped end tag open state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#script-data-escaped-end-tag-open-state"/> 
		/// </summary>
		/// <returns>The script data escaped end tag open.</returns>
		HtmlToken R26_ScriptDataEscapedEndTagOpen ()
		{
			int nc = Peek ();
			char c;

			data.Append ("</");

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				return EmitScriptDataToken ();
			}

			c = (char) nc;

			if (IsAsciiLetter (c)) {
				TokenizerState = HtmlTokenizerState.ScriptDataEscapedEndTagName;
				name.Append (c);
				Read ();
			} else {
				TokenizerState = HtmlTokenizerState.ScriptDataEscaped;
			}

			return null;
		}
		/// <summary>
		/// 8.2.4.27 Script data escaped end tag name state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#script-data-escaped-end-tag-name-state"/> 
		/// </summary>
		/// <returns>The script data escaped end tag name.</returns>
		HtmlToken R27_ScriptDataEscapedEndTagName ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					name.Length = 0;

					return EmitScriptDataToken ();
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\r': case '\n': case '\f': case ' ':
					if (NameIs ("script")) {
						TokenizerState = HtmlTokenizerState.BeforeAttributeName;
						break;
					}

					goto default;
				case '/':
					if (NameIs ("script")) {
						TokenizerState = HtmlTokenizerState.SelfClosingStartTag;
						break;
					}
					goto default;
				case '>':
					if (NameIs ("script")) {
						var token = CreateTagToken (name.ToString (), true);
						TokenizerState = HtmlTokenizerState.Data;
						data.Length = 0;
						name.Length = 0;
						return token;
					}
					goto default;
				default:
					if (!IsAsciiLetter (c)) {
						TokenizerState = HtmlTokenizerState.ScriptData;
						data.Append (c);
						return null;
					}

					name.Append (c);
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.ScriptDataEscapedEndTagName);

			tag = CreateTagToken (name.ToString (), true);
			name.Length = 0;

			return null;
		}
		/// <summary>
		/// 8.2.4.28 Script data double escape start state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#script-data-double-escape-start-state"/> 
		/// </summary>
		/// <returns>The script data double escape start.</returns>
		HtmlToken R28_ScriptDataDoubleEscapeStart ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					name.Length = 0;

					return EmitScriptDataToken ();
				}

				c = (char) nc;

				data.Append (c);

				switch (c) {
				case '\t': case '\r': case '\n': case '\f': case ' ': case '/': case '>':
					if (NameIs ("script"))
						TokenizerState = HtmlTokenizerState.ScriptDataDoubleEscaped;
					else
						TokenizerState = HtmlTokenizerState.ScriptDataEscaped;
					name.Length = 0;
					break;
				default:
					if (!IsAsciiLetter (c))
						TokenizerState = HtmlTokenizerState.ScriptDataEscaped;
					else
						name.Append (c);
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.ScriptDataDoubleEscapeStart);

			return null;
		}
		/// <summary>
		/// 8.2.4.29 Script data double escaped state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#script-data-double-escaped-state"/> 
		/// </summary>
		/// <returns>The script data double escaped.</returns>
		HtmlToken R29_ScriptDataDoubleEscaped ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					return EmitScriptDataToken ();
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

			return null;
		}
		/// <summary>
		/// 8.2.4.30 Script data double escaped dash state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#script-data-double-escaped-dash-state"/> 
		/// </summary>
		/// <returns>The script data double escaped dash.</returns>
		HtmlToken R30_ScriptDataDoubleEscapedDash ()
		{
			int nc = Peek ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				return EmitScriptDataToken ();
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

			return null;
		}
		/// <summary>
		/// 8.2.4.31 Script data double escaped dash dash state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#script-data-double-escaped-dash-dash-state"/> 
		/// </summary>
		/// <returns>The script data double escaped dash dash.</returns>
		HtmlToken R31_ScriptDataDoubleEscapedDashDash ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					return EmitScriptDataToken ();
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

			return null;
		}
		/// <summary>
		/// 8.2.4.32 Script data double escaped less-than sign state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#script-data-double-escaped-less-than-sign-state"/> 
		/// </summary>
		/// <returns>The script data double escaped less than.</returns>
		HtmlToken R32_ScriptDataDoubleEscapedLessThan ()
		{
			int nc = Peek ();

			if (nc == '/') {
				TokenizerState = HtmlTokenizerState.ScriptDataDoubleEscapeEnd;
				data.Append ('/');
				Read ();
			} else {
				TokenizerState = HtmlTokenizerState.ScriptDataDoubleEscaped;
			}

			return null;
		}
		/// <summary>
		/// 8.2.4.33 Script data double escape end state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#script-data-double-escape-end-state"/> 
		/// </summary>
		/// <returns>The script data double escape end.</returns>
		HtmlToken R33_ScriptDataDoubleEscapeEnd ()
		{
			do {
				int nc = Peek ();
				char c = (char) nc;

				switch (c) {
				case '\t': case '\r': case '\n': case '\f': case ' ': case '/': case '>':
					if (NameIs ("script"))
						TokenizerState = HtmlTokenizerState.ScriptDataEscaped;
					else
						TokenizerState = HtmlTokenizerState.ScriptDataDoubleEscaped;
					data.Append (c);
					Read ();
					break;
				default:
					if (!IsAsciiLetter (c)) {
						TokenizerState = HtmlTokenizerState.ScriptDataDoubleEscaped;
					} else {
						name.Append (c);
						data.Append (c);
						Read ();
					}
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.ScriptDataDoubleEscapeEnd);

			return null;
		}
		/// <summary>
		/// 8.2.4.34 Before attribute name state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#before-attribute-name-state"/> 
		/// </summary>
		/// <returns>The before attribute name.</returns>
		HtmlToken R34_BeforeAttributeName ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					tag = null;

					return EmitDataToken (false);
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\r': case '\n': case '\f': case ' ':
					break;
				case '/':
					TokenizerState = HtmlTokenizerState.SelfClosingStartTag;
					return null;
				case '>':
					return EmitTagToken ();
				case '"': case '\'': case '<': case '=':
					// parse error
					goto default;
				default:
					TokenizerState = HtmlTokenizerState.AttributeName;
					name.Append (c == '\0' ? '\uFFFD' : c);
					return null;
				}
			} while (true);
		}
		/// <summary>
		/// 8.2.4.35 Attribute name state
		/// <seealso cref="http://www.w3.org/TR/html5/syntax.html#attribute-name-state"/> 
		/// </summary>
		/// <returns>The attribute name.</returns>
		HtmlToken R35_AttributeName ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					name.Length = 0;
					tag = null;

					return EmitDataToken (false);
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\r': case '\n': case '\f': case ' ':
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

					return EmitTagToken ();
				default:
					name.Append (c == '\0' ? '\uFFFD' : c);
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.AttributeName);

			EmitTagAttribute ();

			return null;
		}
		/// <summary>
		/// 8.2.4.36 After attribute name state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#after-attribute-name-state"/> 
		/// </summary>
		/// <returns>The after attribute name.</returns>
		HtmlToken R36_AfterAttributeName ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					tag = null;

					return EmitDataToken (false);
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\r': case '\n': case '\f': case ' ':
					break;
				case '/':
					TokenizerState = HtmlTokenizerState.SelfClosingStartTag;
					return null;
				case '=':
					TokenizerState = HtmlTokenizerState.BeforeAttributeValue;
					return null;
				case '>':
					return EmitTagToken ();
				case '"': case '\'': case '<':
					// parse error
					goto default;
				default:
					TokenizerState = HtmlTokenizerState.AttributeName;
					name.Append (c == '\0' ? '\uFFFD' : c);
					return null;
				}
			} while (true);
		}
		/// <summary>
		/// 8.2.4.37 Before attribute value state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#before-attribute-value-state"/> 
		/// </summary>
		/// <returns>The before attribute value.</returns>
		HtmlToken R37_BeforeAttributeValue ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					tag = null;

					return EmitDataToken (false);
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\r': case '\n': case '\f': case ' ':
					break;
				case '"': case '\'':
					TokenizerState = HtmlTokenizerState.AttributeValueQuoted;
					quote = c;
					return null;
				case '&':
					TokenizerState = HtmlTokenizerState.AttributeValueUnquoted;
					return null;
				case '/':
					TokenizerState = HtmlTokenizerState.SelfClosingStartTag;
					return null;
				case '>':
					return EmitTagToken ();
				case '<': case '=': case '`':
					// parse error
					goto default;
				default:
					TokenizerState = HtmlTokenizerState.AttributeName;
					name.Append (c == '\0' ? '\uFFFD' : c);
					return null;
				}
			} while (true);
		}
		/// <summary>
		/// 8.2.4.38 Attribute value (double-quoted) state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#attribute-value-%28double-quoted%29-state"/> 
		/// 8.2.4.39 Attribute value (single-quoted) state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#attribute-value-%28single-quoted%29-state"/> 
		/// </summary>
		/// <returns>The attribute value quoted.</returns>
		HtmlToken R38_39_AttributeValueQuoted ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					name.Length = 0;

					return EmitDataToken (false);
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '&':
					TokenizerState = HtmlTokenizerState.CharacterReferenceInAttributeValue;
					return null;
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
			name.Length = 0;

			return null;
		}
		/// <summary>
		/// 8.2.4.40 Attribute value (unquoted) state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#attribute-value-%28unquoted%29-state"/> 
		/// </summary>
		/// <returns>The attribute value unquoted.</returns>
		HtmlToken R40_AttributeValueUnquoted ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					name.Length = 0;

					return EmitDataToken (false);
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\r': case '\n': case '\f': case ' ':
					TokenizerState = HtmlTokenizerState.BeforeAttributeName;
					break;
				case '&':
					TokenizerState = HtmlTokenizerState.CharacterReferenceInAttributeValue;
					return null;
				case '>':
					return EmitTagToken ();
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
			name.Length = 0;

			return null;
		}
		/// <summary>
		/// 8.2.4.41 Character reference in attribute value state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#character-reference-in-attribute-value-state"/> 
		/// </summary>
		/// <returns>The character reference in attribute value.</returns>
		HtmlToken R41_CharacterReferenceInAttributeValue ()
		{
			char additionalAllowedCharacter = quote == '\0' ? '>' : quote;
			int nc = Peek ();
			bool consume;
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				data.Append ('&');
				name.Length = 0;

				return EmitDataToken (false);
			}

			c = (char) nc;

			switch (c) {
			case '\t': case '\r': case '\n': case '\f': case ' ': case '<': case '&':
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
					Read ();

					if ((nc = Peek ()) == -1) {
						TokenizerState = HtmlTokenizerState.EndOfFile;
						data.Append (entity.GetPushedInput ());
						entity.Reset ();

						return EmitDataToken (false);
					}

					c = (char) nc;
				}

				var pushed = entity.GetPushedInput ();
				string value;

				if (c == '=' || IsAlphaNumeric (c))
					value = pushed;
				else
					value = entity.GetValue ();

				data.Append (pushed);
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
				Read ();

			return null;
		}
		/// <summary>
		/// 8.2.4.42 After attribute value (quoted) state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#after-attribute-value-%28quoted%29-state"/> 
		/// </summary>
		/// <returns>The after attribute value quoted.</returns>
		HtmlToken R42_AfterAttributeValueQuoted ()
		{
			HtmlToken token = null;
			int nc = Peek ();
			bool consume;
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				return EmitDataToken (false);
			}

			c = (char) nc;

			switch (c) {
			case '\t': case '\r': case '\n': case '\f': case ' ':
				TokenizerState = HtmlTokenizerState.BeforeAttributeName;
				consume = true;
				break;
			case '/':
				TokenizerState = HtmlTokenizerState.SelfClosingStartTag;
				consume = true;
				break;
			case '>':
				token = EmitTagToken ();
				consume = true;
				break;
			default:
				TokenizerState = HtmlTokenizerState.BeforeAttributeName;
				consume = false;
				break;
			}

			if (consume)
				Read ();

			return token;
		}
		/// <summary>
		/// 8.2.4.43 Self-closing start tag state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#self-closing-start-tag-state"/> 
		/// </summary>
		/// <returns>The self closing start tag.</returns>
		HtmlToken R43_SelfClosingStartTag ()
		{
			int nc = Read ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				return EmitDataToken (false);
			}

			c = (char) nc;

			if (c == '>') {
				tag.IsEmptyElement = true;

				return EmitTagToken ();
			}

			// parse error
			TokenizerState = HtmlTokenizerState.BeforeAttributeName;

			// Note: we save the data in case we hit a parse error and have to emit a data token
			data.Append (c);

			return null;
		}
		/// <summary>
		/// 8.2.4.44 Bogus comment state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#bogus-comment-state"/> 
		/// </summary>
		/// <returns>The bogus comment.</returns>
		HtmlToken R44_BogusComment ()
		{
			int nc;
			char c;

			if (data.Length > 0) {
				c = data[data.Length - 1];
				data.Length = 1;
				data[0] = c;
			}

			do {
				if ((nc = Read ()) == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					break;
				}

				if ((c = (char) nc) == '>')
					break;

				data.Append (c == '\0' ? '\uFFFD' : c);
			} while (true);

			return EmitCommentToken (data);
		}
		/// <summary>
		/// 8.2.4.45 Markup declaration open state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#markup-declaration-open-state"/> 
		/// </summary>
		/// <returns>The markup declaration open.</returns>
		HtmlToken R45_MarkupDeclarationOpen ()
		{
			int count = 0, nc;
			char c = '\0';

			while (count < 2) {
				if ((nc = Peek ()) == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					return EmitDataToken (false);
				}

				if ((c = (char) nc) != '-')
					break;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);
				Read ();
				count++;
			}

			if (count == 2) {
				TokenizerState = HtmlTokenizerState.CommentStart;
				name.Length = 0;
				return null;
			}

			if (count == 1) {
				// parse error
				TokenizerState = HtmlTokenizerState.BogusComment;
				return null;
			}

			if (c == 'D' || c == 'd') {
				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);
				name.Append (c);
				count = 1;
				Read ();

				while (count < 7) {
					if ((nc = Read ()) == -1) {
						TokenizerState = HtmlTokenizerState.EndOfFile;
						return EmitDataToken (false);
					}

					if (ToLower ((c = (char) nc)) != DocType[count])
						break;

					// Note: we save the data in case we hit a parse error and have to emit a data token
					data.Append (c);
					name.Append (c);
					count++;
				}

				if (count == 7) {
					doctype = CreateDocTypeToken (name.ToString ());
					TokenizerState = HtmlTokenizerState.DocType;
					name.Length = 0;
					return null;
				}

				name.Length = 0;
			} else if (c == '[') {
				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);
				count = 1;
				Read ();

				while (count < 7) {
					if ((nc = Read ()) == -1) {
						TokenizerState = HtmlTokenizerState.EndOfFile;
						return EmitDataToken (false);
					}

					if ((c = (char) nc) != CData[count])
						break;

					// Note: we save the data in case we hit a parse error and have to emit a data token
					data.Append (c);
					count++;
				}

				if (count == 7) {
					TokenizerState = HtmlTokenizerState.CDataSection;
					return null;
				}
			}

			// parse error
			TokenizerState = HtmlTokenizerState.BogusComment;

			return null;
		}
		/// <summary>
		/// 8.2.4.46 Comment start state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#comment-start-state"/> 
		/// </summary>
		/// <returns>The comment start.</returns>
		HtmlToken R46_CommentStart ()
		{
			int nc = Read ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.Data;

				return EmitCommentToken (string.Empty);
			}

			c = (char) nc;

			data.Append (c);

			switch (c) {
			case '-':
				TokenizerState = HtmlTokenizerState.CommentStartDash;
				break;
			case '>': // parse error
				TokenizerState = HtmlTokenizerState.Data;
				return EmitCommentToken (string.Empty);
			default: // parse error
				TokenizerState = HtmlTokenizerState.Comment;
				name.Append (c == '\0' ? '\uFFFD' : c);
				break;
			}

			return null;
		}
		/// <summary>
		/// 8.2.4.47 Comment start dash state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#comment-start-dash-state"/> 
		/// </summary>
		/// <returns>The comment start dash.</returns>
		HtmlToken R47_CommentStartDash ()
		{
			int nc = Read ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.Data;
				return EmitCommentToken (name);
			}

			c = (char) nc;

			data.Append (c);

			switch (c) {
			case '-':
				TokenizerState = HtmlTokenizerState.CommentEnd;
				break;
			case '>': // parse error
				TokenizerState = HtmlTokenizerState.Data;
				return EmitCommentToken (name);
			default: // parse error
				TokenizerState = HtmlTokenizerState.Comment;
				name.Append ('-');
				name.Append (c == '\0' ? '\uFFFD' : c);
				break;
			}

			return null;
		}
		/// <summary>
		/// 8.2.4.48 Comment state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#comment-state"/> 
		/// </summary>
		/// <returns>The comment.</returns>
		HtmlToken R48_Comment ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					return EmitCommentToken (name);
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '-':
					TokenizerState = HtmlTokenizerState.CommentEndDash;
					return null;
				default:
					name.Append (c == '\0' ? '\uFFFD' : c);
					break;
				}
			} while (true);
		}

		// FIXME: this is exactly the same as ReadCommentStartDash
		/// <summary>
		/// 8.2.4.49 Comment end dash state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#comment-end-dash-state"/> 
		/// </summary>
		/// <returns>The comment end dash.</returns>
		HtmlToken R49_CommentEndDash ()
		{
			int nc = Read ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.Data;
				return EmitCommentToken (name);
			}

			c = (char) nc;

			data.Append (c);

			switch (c) {
			case '-':
				TokenizerState = HtmlTokenizerState.CommentEnd;
				break;
			case '>': // parse error
				TokenizerState = HtmlTokenizerState.Data;
				return EmitCommentToken (name);
			default: // parse error
				TokenizerState = HtmlTokenizerState.Comment;
				name.Append ('-');
				name.Append (c == '\0' ? '\uFFFD' : c);
				break;
			}

			return null;
		}
		/// <summary>
		/// 8.2.4.50 Comment end state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#comment-end-state"/> 
		/// </summary>
		/// <returns>The comment end.</returns>
		HtmlToken R50_CommentEnd ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					return EmitCommentToken (name);
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '>':
					TokenizerState = HtmlTokenizerState.Data;
					return EmitCommentToken (name);
				case '!': // parse error
					TokenizerState = HtmlTokenizerState.CommentEndBang;
					return null;
				case '-':
					name.Append ('-');
					break;
				default:
					TokenizerState = HtmlTokenizerState.Comment;
					name.Append (c == '\0' ? '\uFFFD' : c);
					return null;
				}
			} while (true);
		}
		/// <summary>
		/// 8.2.4.51 Comment end bang state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#comment-end-bang-state"/> 
		/// </summary>
		/// <returns>The comment end bang.</returns>
		HtmlToken R51_CommentEndBang ()
		{
			int nc = Read ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				return EmitCommentToken (name);
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
				return EmitCommentToken (name);
			default: // parse error
				TokenizerState = HtmlTokenizerState.Comment;
				name.Append ("--!");
				name.Append (c == '\0' ? '\uFFFD' : c);
				break;
			}

			return null;
		}
		/// <summary>
		/// 8.2.4.52 DOCTYPE state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#doctype-state"/> 
		/// </summary>
		/// <returns>The document type.</returns>
		HtmlToken R52_DocType ()
		{
			int nc = Peek ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				doctype.ForceQuirksMode = true;
				name.Length = 0;

				return EmitDocType ();
			}

			TokenizerState = HtmlTokenizerState.BeforeDocTypeName;
			c = (char) nc;

			switch (c) {
			case '\t': case '\r': case '\n': case '\f': case ' ':
				data.Append (c);
				Read ();
				break;
			}

			return null;
		}
		/// <summary>
		/// 8.2.4.53 Before DOCTYPE name state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#before-doctype-name-state"/> 
		/// </summary>
		/// <returns>The before document type name.</returns>
		HtmlToken R53_BeforeDocTypeName ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					doctype.ForceQuirksMode = true;
					return EmitDocType ();
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\r': case '\n': case '\f': case ' ':
					break;
				case '>':
					TokenizerState = HtmlTokenizerState.Data;
					doctype.ForceQuirksMode = true;
					return EmitDocType ();
				case '\0':
					TokenizerState = HtmlTokenizerState.DocTypeName;
					name.Append ('\uFFFD');
					return null;
				default:
					TokenizerState = HtmlTokenizerState.DocTypeName;
					name.Append (c);
					return null;
				}
			} while (true);
		}
		/// <summary>
		/// 8.2.4.54 DOCTYPE name state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#doctype-name-state"/> 
		/// </summary>
		/// <returns>The document type name.</returns>
		HtmlToken R54_DocTypeName ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					doctype.Name = name.ToString ();
					doctype.ForceQuirksMode = true;
					name.Length = 0;

					return EmitDocType ();
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\r': case '\n': case '\f': case ' ':
					TokenizerState = HtmlTokenizerState.AfterDocTypeName;
					break;
				case '>':
					TokenizerState = HtmlTokenizerState.Data;
					doctype.Name = name.ToString ();
					name.Length = 0;

					return EmitDocType ();
				case '\0':
					name.Append ('\uFFFD');
					break;
				default:
					name.Append (c);
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.DocTypeName);

			doctype.Name = name.ToString ();
			name.Length = 0;

			return null;
		}
		/// <summary>
		/// 8.2.4.55 After DOCTYPE name state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#after-doctype-name-state"/> 
		/// </summary>
		/// <returns>The after document type name.</returns>
		HtmlToken R55_AfterDocTypeName ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					doctype.ForceQuirksMode = true;
					return EmitDocType ();
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\r': case '\n': case '\f': case ' ':
					break;
				case '>':
					TokenizerState = HtmlTokenizerState.Data;
					return EmitDocType ();
				default:
					name.Append (c);
					if (name.Length < 6)
						break;

					if (NameIs ("public")) {
						TokenizerState = HtmlTokenizerState.AfterDocTypePublicKeyword;
						doctype.PublicKeyword = name.ToString ();
					} else if (NameIs ("system")) {
						TokenizerState = HtmlTokenizerState.AfterDocTypeSystemKeyword;
						doctype.SystemKeyword = name.ToString ();
					} else {
						TokenizerState = HtmlTokenizerState.BogusDocType;
					}

					name.Length = 0;
					return null;
				}
			} while (true);
		}
		/// <summary>
		/// 8.2.4.56 After DOCTYPE public keyword state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#after-doctype-public-keyword-state"/> 
		/// </summary>
		/// <returns>The after document type public keyword.</returns>
		HtmlToken R56_AfterDocTypePublicKeyword ()
		{
			int nc = Read ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				doctype.ForceQuirksMode = true;
				return EmitDocType ();
			}

			c = (char) nc;

			// Note: we save the data in case we hit a parse error and have to emit a data token
			data.Append (c);

			switch (c) {
			case '\t': case '\r': case '\n': case '\f': case ' ':
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
				return EmitDocType ();
			default: // parse error
				TokenizerState = HtmlTokenizerState.BogusDocType;
				doctype.ForceQuirksMode = true;
				break;
			}

			return null;
		}
		/// <summary>
		/// 8.2.4.57 Before DOCTYPE public identifier state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#before-doctype-public-identifier-state"/> 
		/// </summary>
		/// <returns>The before document type public identifier.</returns>
		HtmlToken R57_BeforeDocTypePublicIdentifier ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					doctype.ForceQuirksMode = true;
					return EmitDocType ();
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\r': case '\n': case '\f': case ' ':
					break;
				case '"': case '\'':
					TokenizerState = HtmlTokenizerState.DocTypePublicIdentifierQuoted;
					doctype.PublicIdentifier = string.Empty;
					quote = c;
					return null;
				case '>': // parse error
					TokenizerState = HtmlTokenizerState.Data;
					doctype.ForceQuirksMode = true;
					return EmitDocType ();
				default: // parse error
					TokenizerState = HtmlTokenizerState.BogusDocType;
					doctype.ForceQuirksMode = true;
					return null;
				}
			} while (true);
		}
		/// <summary>
		/// 8.2.4.58 DOCTYPE public identifier (double-quoted) state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#doctype-public-identifier-%28double-quoted%29-state"/> 
		/// 8.2.4.59 DOCTYPE public identifier (single-quoted) state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#doctype-public-identifier-%28single-quoted%29-state"/> 
		/// </summary>
		/// <returns>The document type public identifier quoted.</returns>
		HtmlToken R58_59_DocTypePublicIdentifierQuoted ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					doctype.PublicIdentifier = name.ToString ();
					doctype.ForceQuirksMode = true;
					name.Length = 0;

					return EmitDocType ();
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
					name.Length = 0;

					return EmitDocType ();
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
			name.Length = 0;

			return null;
		}
		/// <summary>
		/// 8.2.4.60 After DOCTYPE public identifier state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#after-doctype-public-identifier-state"/> 
		/// </summary>
		/// <returns>The after document type public identifier.</returns>
		HtmlToken R60_AfterDocTypePublicIdentifier ()
		{
			int nc = Read ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				doctype.ForceQuirksMode = true;
				return EmitDocType ();
			}

			c = (char) nc;

			// Note: we save the data in case we hit a parse error and have to emit a data token
			data.Append (c);

			switch (c) {
			case '\t': case '\r': case '\n': case '\f': case ' ':
				TokenizerState = HtmlTokenizerState.BetweenDocTypePublicAndSystemIdentifiers;
				break;
			case '>':
				TokenizerState = HtmlTokenizerState.Data;
				return EmitDocType ();
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

			return null;
		}
		/// <summary>
		/// 8.2.4.61 Between DOCTYPE public and system identifiers state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#between-doctype-public-and-system-identifiers-state"/> 
		/// </summary>
		/// <returns>The between document type public and system identifiers.</returns>
		HtmlToken R61_BetweenDocTypePublicAndSystemIdentifiers ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					doctype.ForceQuirksMode = true;
					return EmitDocType ();
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\r': case '\n': case '\f': case ' ':
					break;
				case '>':
					TokenizerState = HtmlTokenizerState.Data;
					return EmitDocType ();
				case '"': case '\'':
					TokenizerState = HtmlTokenizerState.DocTypeSystemIdentifierQuoted;
					doctype.SystemIdentifier = string.Empty;
					quote = c;
					return null;
				default: // parse error
					TokenizerState = HtmlTokenizerState.BogusDocType;
					doctype.ForceQuirksMode = true;
					return null;
				}
			} while (true);
		}
		/// <summary>
		/// 8.2.4.62 After DOCTYPE system keyword state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#after-doctype-system-keyword-state"/> 
		/// </summary>
		/// <returns>The after document type system keyword.</returns>
		HtmlToken R62_AfterDocTypeSystemKeyword ()
		{
			int nc = Read ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				doctype.ForceQuirksMode = true;
				return EmitDocType ();
			}

			c = (char) nc;

			// Note: we save the data in case we hit a parse error and have to emit a data token
			data.Append (c);

			switch (c) {
			case '\t': case '\r': case '\n': case '\f': case ' ':
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
				return EmitDocType ();
			default: // parse error
				TokenizerState = HtmlTokenizerState.BogusDocType;
				doctype.ForceQuirksMode = true;
				break;
			}

			return null;
		}
		/// <summary>
		/// 8.2.4.63 Before DOCTYPE system identifier state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#before-doctype-system-identifier-state"/> 
		/// </summary>
		/// <returns>The before document type system identifier.</returns>
		HtmlToken R63_BeforeDocTypeSystemIdentifier ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					doctype.ForceQuirksMode = true;
					return EmitDocType ();
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case '\r': case '\n': case '\f': case ' ':
					break;
				case '"': case '\'':
					TokenizerState = HtmlTokenizerState.DocTypeSystemIdentifierQuoted;
					doctype.SystemIdentifier = string.Empty;
					quote = c;
					return null;
				case '>': // parse error
					TokenizerState = HtmlTokenizerState.Data;
					doctype.ForceQuirksMode = true;
					return EmitDocType ();
				default: // parse error
					TokenizerState = HtmlTokenizerState.BogusDocType;
					doctype.ForceQuirksMode = true;
					return null;
				}
			} while (true);
		}
		/// <summary>
		/// 8.2.4.64 DOCTYPE system identifier (double-quoted) state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#doctype-system-identifier-%28double-quoted%29-state"/> 
		/// 8.2.4.65 DOCTYPE system identifier (single-quoted) state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#doctype-system-identifier-%28single-quoted%29-state"/> 
		/// </summary>
		/// <returns>The document type system identifier quoted.</returns>
		HtmlToken R64_65_DocTypeSystemIdentifierQuoted ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					doctype.SystemIdentifier = name.ToString ();
					doctype.ForceQuirksMode = true;
					name.Length = 0;

					return EmitDocType ();
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
					name.Length = 0;

					return EmitDocType ();
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
			name.Length = 0;

			return null;
		}
		/// <summary>
		/// 8.2.4.66 After DOCTYPE system identifier state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#after-doctype-system-identifier-state"/> 
		/// </summary>
		/// <returns>The after document type system identifier.</returns>
		HtmlToken R66_AfterDocTypeSystemIdentifier ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					doctype.ForceQuirksMode = true;
					return EmitDocType ();
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				switch (c) {
				case '\t': case'\r': case '\n': case '\f': case ' ':
					break;
				case '>':
					TokenizerState = HtmlTokenizerState.Data;
					return EmitDocType ();
				default: // parse error
					TokenizerState = HtmlTokenizerState.BogusDocType;
					return null;
				}
			} while (true);
		}
		/// <summary>
		/// 8.2.4.67 Bogus DOCTYPE state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#bogus-doctype-state"/> 
		/// </summary>
		/// <returns>The bogus document type.</returns>
		HtmlToken R67_BogusDocType ()
		{
			do {
				int nc = Read ();
				char c;

				if (nc == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					doctype.ForceQuirksMode = true;
					return EmitDocType ();
				}

				c = (char) nc;

				// Note: we save the data in case we hit a parse error and have to emit a data token
				data.Append (c);

				if (c == '>') {
					TokenizerState = HtmlTokenizerState.Data;
					return EmitDocType ();
				}
			} while (true);
		}
		/// <summary>
		/// 8.2.4.68 CDATA section state
		/// <see cref="http://www.w3.org/TR/html5/syntax.html#cdata-section-state"/> 
		/// </summary>
		/// <returns>The C data section.</returns>
		HtmlToken R68_CDataSection ()
		{
			int nc = Read ();

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

						return EmitCDataToken ();
					}
				} else {
					cdata[cdataIndex++] = c;
				}

				nc = Read ();
			}

			TokenizerState = HtmlTokenizerState.EndOfFile;

			for (int i = 0; i < cdataIndex; i++)
				data.Append (cdata[i]);

			cdataIndex = 0;

			return EmitCDataToken ();
		}

		public bool ReadNextToken (out HtmlToken token)
		{
			do {
				switch (TokenizerState) {
				case HtmlTokenizerState.Data:
					token = R01_DataToken ();
					break;
				case HtmlTokenizerState.CharacterReferenceInData:
					token = R02_CharacterReferenceInData ();
					break;
				case HtmlTokenizerState.RcData:
					token = R03_RcData ();
					break;
				case HtmlTokenizerState.CharacterReferenceInRcData:
					token = R04_CharacterReferenceInRcData ();
					break;
				case HtmlTokenizerState.RawText:
					token = R05_RawText ();
					break;
				case HtmlTokenizerState.ScriptData:
					token = R06_ScriptData ();
					break;
				case HtmlTokenizerState.PlainText:
					token = R07_PlainText ();
					break;
				case HtmlTokenizerState.TagOpen:
					token = R08_TagOpen ();
					break;
				case HtmlTokenizerState.EndTagOpen:
					token = R09_EndTagOpen ();
					break;
				case HtmlTokenizerState.TagName:
					token = R10_TagName ();
					break;
				case HtmlTokenizerState.RcDataLessThan:
					token = R11_ReadRcDataLessThan ();
					break;
				case HtmlTokenizerState.RcDataEndTagOpen:
					token = R12_RcDataEndTagOpen ();
					break;
				case HtmlTokenizerState.RcDataEndTagName:
					token = R13_RcDataEndTagName ();
					break;
				case HtmlTokenizerState.RawTextLessThan:
					token = R14_RawTextLessThan ();
					break;
				case HtmlTokenizerState.RawTextEndTagOpen:
					token = R15_RawTextEndTagOpen ();
					break;
				case HtmlTokenizerState.RawTextEndTagName:
					token = R16_RawTextEndTagName ();
					break;
				case HtmlTokenizerState.ScriptDataLessThan:
					token = R17_ScriptDataLessThan ();
					break;
				case HtmlTokenizerState.ScriptDataEndTagOpen:
					token = R18_ScriptDataEndTagOpen ();
					break;
				case HtmlTokenizerState.ScriptDataEndTagName:
					token = ReadScriptDataEndTagName ();
					break;
				case HtmlTokenizerState.ScriptDataEscapeStart:
					token = R20_ScriptDataEscapeStart ();
					break;
				case HtmlTokenizerState.ScriptDataEscapeStartDash:
					token = R21_ReadScriptDataEscapeStartDash ();
					break;
				case HtmlTokenizerState.ScriptDataEscaped:
					token = R22_ReadScriptDataEscaped ();
					break;
				case HtmlTokenizerState.ScriptDataEscapedDash:
					token = R23_ReadScriptDataEscapedDash ();
					break;
				case HtmlTokenizerState.ScriptDataEscapedDashDash:
					token = R24_ScriptDataEscapedDashDash ();
					break;
				case HtmlTokenizerState.ScriptDataEscapedLessThan:
					token = R25_ScriptDataEscapedLessThan ();
					break;
				case HtmlTokenizerState.ScriptDataEscapedEndTagOpen:
					token = R26_ScriptDataEscapedEndTagOpen ();
					break;
				case HtmlTokenizerState.ScriptDataEscapedEndTagName:
					token = R27_ScriptDataEscapedEndTagName ();
					break;
				case HtmlTokenizerState.ScriptDataDoubleEscapeStart:
					token = R28_ScriptDataDoubleEscapeStart ();
					break;
				case HtmlTokenizerState.ScriptDataDoubleEscaped:
					token = R29_ScriptDataDoubleEscaped ();
					break;
				case HtmlTokenizerState.ScriptDataDoubleEscapedDash:
					token = R30_ScriptDataDoubleEscapedDash ();
					break;
				case HtmlTokenizerState.ScriptDataDoubleEscapedDashDash:
					token = R31_ScriptDataDoubleEscapedDashDash ();
					break;
				case HtmlTokenizerState.ScriptDataDoubleEscapedLessThan:
					token = R32_ScriptDataDoubleEscapedLessThan ();
					break;
				case HtmlTokenizerState.ScriptDataDoubleEscapeEnd:
					token = R33_ScriptDataDoubleEscapeEnd ();
					break;
				case HtmlTokenizerState.BeforeAttributeName:
					token = R34_BeforeAttributeName ();
					break;
				case HtmlTokenizerState.AttributeName:
					token = R35_AttributeName ();
					break;
				case HtmlTokenizerState.AfterAttributeName:
					token = R36_AfterAttributeName ();
					break;
				case HtmlTokenizerState.BeforeAttributeValue:
					token = R37_BeforeAttributeValue ();
					break;
				case HtmlTokenizerState.AttributeValueQuoted:
					token = R38_39_AttributeValueQuoted ();
					break;
				case HtmlTokenizerState.AttributeValueUnquoted:
					token = R40_AttributeValueUnquoted ();
					break;
				case HtmlTokenizerState.CharacterReferenceInAttributeValue:
					token = R41_CharacterReferenceInAttributeValue ();
					break;
				case HtmlTokenizerState.AfterAttributeValueQuoted:
					token = R42_AfterAttributeValueQuoted ();
					break;
				case HtmlTokenizerState.SelfClosingStartTag:
					token = R43_SelfClosingStartTag ();
					break;
				case HtmlTokenizerState.BogusComment:
					token = R44_BogusComment ();
					break;
				case HtmlTokenizerState.MarkupDeclarationOpen:
					token = R45_MarkupDeclarationOpen ();
					break;
				case HtmlTokenizerState.CommentStart:
					token = R46_CommentStart ();
					break;
				case HtmlTokenizerState.CommentStartDash:
					token = R47_CommentStartDash ();
					break;
				case HtmlTokenizerState.Comment:
					token = R48_Comment ();
					break;
				case HtmlTokenizerState.CommentEndDash:
					token = R49_CommentEndDash ();
					break;
				case HtmlTokenizerState.CommentEnd:
					token = R50_CommentEnd ();
					break;
				case HtmlTokenizerState.CommentEndBang:
					token = R51_CommentEndBang ();
					break;
				case HtmlTokenizerState.DocType:
					token = R52_DocType ();
					break;
				case HtmlTokenizerState.BeforeDocTypeName:
					token = R53_BeforeDocTypeName ();
					break;
				case HtmlTokenizerState.DocTypeName:
					token = R54_DocTypeName ();
					break;
				case HtmlTokenizerState.AfterDocTypeName:
					token = R55_AfterDocTypeName ();
					break;
				case HtmlTokenizerState.AfterDocTypePublicKeyword:
					token = R56_AfterDocTypePublicKeyword ();
					break;
				case HtmlTokenizerState.BeforeDocTypePublicIdentifier:
					token = R57_BeforeDocTypePublicIdentifier ();
					break;
				case HtmlTokenizerState.DocTypePublicIdentifierQuoted:
					token = R58_59_DocTypePublicIdentifierQuoted ();
					break;
				case HtmlTokenizerState.AfterDocTypePublicIdentifier:
					token = R60_AfterDocTypePublicIdentifier ();
					break;
				case HtmlTokenizerState.BetweenDocTypePublicAndSystemIdentifiers:
					token = R61_BetweenDocTypePublicAndSystemIdentifiers ();
					break;
				case HtmlTokenizerState.AfterDocTypeSystemKeyword:
					token = R62_AfterDocTypeSystemKeyword ();
					break;
				case HtmlTokenizerState.BeforeDocTypeSystemIdentifier:
					token = R63_BeforeDocTypeSystemIdentifier ();
					break;
				case HtmlTokenizerState.DocTypeSystemIdentifierQuoted:
					token = R64_65_DocTypeSystemIdentifierQuoted ();
					break;
				case HtmlTokenizerState.AfterDocTypeSystemIdentifier:
					token = R66_AfterDocTypeSystemIdentifier ();
					break;
				case HtmlTokenizerState.BogusDocType:
					token = R67_BogusDocType ();
					break;
				case HtmlTokenizerState.CDataSection:
					token = R68_CDataSection ();
					break;
				case HtmlTokenizerState.EndOfFile:
				default:
					token = null;
					return false;
				}
			} while (token == null);

			return true;
		}
	}
}
