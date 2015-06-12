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

using System;
using System.IO;
using System.Text;

namespace HtmlKit {
	public class HtmlTokenizer
	{
		const string AlphaChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
		const string HexAlphabet = "0123456789ABCDEF";
		const string Numeric = "0123456789";

		readonly HtmlEntityDecoder entity = new HtmlEntityDecoder ();
		readonly StringBuilder data = new StringBuilder ();

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

		static int ToUpper (int c)
		{
			return (c >= 0x41) && (c <= 0x7A) ? (c - 0x20) : c;
		}

		bool ReadDataToken (out HtmlToken token)
		{
			do {
				int c = text.Read ();

				switch (c) {
				case 0x26: // &
					TokenizerState = HtmlTokenizerState.CharacterReferenceInData;
					break;
				case 0x3C: // <
					TokenizerState = HtmlTokenizerState.TagOpen;
					break;
				case -1:
					TokenizerState = HtmlTokenizerState.EndOfFile;
					break;
				case 0:
					// if strict, emit parse error
				default:
					data.Append ((char) c);
					break;
				}
			} while (TokenizerState == HtmlTokenizerState.Data);

			if (data.Length > 0) {
				token = new HtmlDataToken (data.ToString ());
				data.Clear ();
				return true;
			}

			token = null;

			return false;
		}

		void ReadCharacterReference (bool attribute)
		{
			int nc = text.Peek ();
			char c;

			if (nc == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				data.Append ('&');
				return;
			}

			c = (char) nc;

			switch (c) {
			case '\t': case '\n': case '\f': case ' ': case '<': case '&':
				// no character is consumed, emit '&'
				TokenizerState = HtmlTokenizerState.Data;
				data.Append ('&');
				return;
			default:
//				if (nc == additionalAllowedCharacter) {
//					TokenizerState = HtmlTokenizerState.Data;
//					data.Append ('&');
//					return;
//				}
				break;
			}

			while (entity.Push (c)) {
				text.Read ();

				if ((nc = text.Peek ()) == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					data.Append ('&');
					data.Append (entity.GetValue ());
					entity.Reset ();
					return;
				}

				c = (char) nc;
			}

			TokenizerState = HtmlTokenizerState.Data;

			data.Append (entity.GetValue ());
			entity.Reset ();

			if (c == ';') {
				// consume the ';'
				text.Read ();
				return;
			}

			if (attribute && c == '=') {
				// consume the '='
				text.Read ();
				return;
			}
		}

		public bool ReadNextToken (out HtmlToken token)
		{
			do {
				switch (TokenizerState) {
				case HtmlTokenizerState.EndOfFile:
					token = HtmlToken.EndOfFile;
					return true;
				case HtmlTokenizerState.Data:
					if (ReadDataToken (out token))
						return true;
					break;
				}
			} while (true);
		}
	}
}
