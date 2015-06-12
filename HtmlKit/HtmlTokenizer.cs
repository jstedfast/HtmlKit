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

		readonly StringBuilder data = new StringBuilder ();
		readonly StringBuilder cref = new StringBuilder ();

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

		void ReadNumericCharacterReference ()
		{
			int nc, xbase, value = 0, v;
			char c;

			if ((nc = text.Read ()) == -1) {
				TokenizerState = HtmlTokenizerState.EndOfFile;
				data.Append (cref.ToString ());
				return;
			}

			c = (char) nc;

			if (c == 'X' || c == 'x') {
				cref.Append (c);
				xbase = 16;

				if ((nc = text.Read ()) == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					data.Append (cref.ToString ());
					return;
				}
			} else {
				xbase = 10;
			}

			if (c == ';') {
				// parse error, nothing is consumed
				TokenizerState = HtmlTokenizerState.Data;
				data.Append (cref.ToString ());
				return;
			}

			do {
				if (c <= '9') {
					if (c < '0') {
						// parse error
						TokenizerState = HtmlTokenizerState.Data;
						data.Append (cref.ToString ());
						return;
					}

					v = c - '0';
				} else if (xbase == 16) {
					if (c >= 'a') {
						if ((v = c - 'a') > 15) {
							// parse error
							TokenizerState = HtmlTokenizerState.Data;
							data.Append (cref.ToString ());
							return;
						}
					} else if (c >= 'A') {
						if ((v = c - 'A') > 15) {
							// parse error
							TokenizerState = HtmlTokenizerState.Data;
							data.Append (cref.ToString ());
							return;
						}
					} else {
						// parse error
						TokenizerState = HtmlTokenizerState.Data;
						data.Append (cref.ToString ());
						return;
					}
				} else {
					// parse error
					TokenizerState = HtmlTokenizerState.Data;
					data.Append (cref.ToString ());
					return;
				}

				// TODO: check for overflow
				value = value * xbase + v;

				if ((nc = text.Read ()) == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					data.Append (cref.ToString ());
					return;
				}

				c = (char) nc;
			} while (c != ';');

			TokenizerState = HtmlTokenizerState.Data;

			// the following values are parse errors
			switch (value) {
			case 0x00: data.Append ((char) 0xFFFD); return; // REPLACEMENT CHARACTER
			case 0x80: data.Append ((char) 0x20AC); return; // EURO SIGN (€)
			case 0x82: data.Append ((char) 0x201A); return; // SINGLE LOW-9 QUOTATION MARK (‚)
			case 0x83: data.Append ((char) 0x0192); return; // LATIN SMALL LETTER F WITH HOOK (ƒ)
			case 0x84: data.Append ((char) 0x201E); return; // DOUBLE LOW-9 QUOTATION MARK („)
			case 0x85: data.Append ((char) 0x2026); return; // HORIZONTAL ELLIPSIS (…)
			case 0x86: data.Append ((char) 0x2020); return; // DAGGER (†)
			case 0x87: data.Append ((char) 0x2021); return; // DOUBLE DAGGER (‡)
			case 0x88: data.Append ((char) 0x02C6); return; // MODIFIER LETTER CIRCUMFLEX ACCENT (ˆ)
			case 0x89: data.Append ((char) 0x2030); return; // PER MILLE SIGN (‰)
			case 0x8A: data.Append ((char) 0x0160); return; // LATIN CAPITAL LETTER S WITH CARON (Š)
			case 0x8B: data.Append ((char) 0x2039); return; // SINGLE LEFT-POINTING ANGLE QUOTATION MARK (‹)
			case 0x8C: data.Append ((char) 0x0152); return; // LATIN CAPITAL LIGATURE OE (Œ)
			case 0x8E: data.Append ((char) 0x017D); return; // LATIN CAPITAL LETTER Z WITH CARON (Ž)
			case 0x91: data.Append ((char) 0x2018); return; // LEFT SINGLE QUOTATION MARK (‘)
			case 0x92: data.Append ((char) 0x2019); return; // RIGHT SINGLE QUOTATION MARK (’)
			case 0x93: data.Append ((char) 0x201C); return; // LEFT DOUBLE QUOTATION MARK (“)
			case 0x94: data.Append ((char) 0x201D); return; // RIGHT DOUBLE QUOTATION MARK (”)
			case 0x95: data.Append ((char) 0x2022); return; // BULLET (•)
			case 0x96: data.Append ((char) 0x2013); return; // EN DASH (–)
			case 0x97: data.Append ((char) 0x2014); return; // EM DASH (—)
			case 0x98: data.Append ((char) 0x02DC); return; // SMALL TILDE (˜)
			case 0x99: data.Append ((char) 0x2122); return; // TRADE MARK SIGN (™)
			case 0x9A: data.Append ((char) 0x0161); return; // LATIN SMALL LETTER S WITH CARON (š)
			case 0x9B: data.Append ((char) 0x203A); return; // SINGLE RIGHT-POINTING ANGLE QUOTATION MARK (›)
			case 0x9C: data.Append ((char) 0x0153); return; // LATIN SMALL LIGATURE OE (œ)
			case 0x9E: data.Append ((char) 0x017E); return; // LATIN SMALL LETTER Z WITH CARON (ž)
			case 0x9F: data.Append ((char) 0x0178); return; // LATIN CAPITAL LETTER Y WITH DIAERESIS (Ÿ)
			case 0x0000B: case 0x0FFFE: case 0x1FFFE: case 0x1FFFF: case 0x2FFFE: case 0x2FFFF: case 0x3FFFE:
			case 0x3FFFF: case 0x4FFFE: case 0x4FFFF: case 0x5FFFE: case 0x5FFFF: case 0x6FFFE: case 0x6FFFF:
			case 0x7FFFE: case 0x7FFFF: case 0x8FFFE: case 0x8FFFF: case 0x9FFFE: case 0x9FFFF: case 0xAFFFE:
			case 0xAFFFF: case 0xBFFFE: case 0xBFFFF: case 0xCFFFE: case 0xCFFFF: case 0xDFFFE: case 0xDFFFF:
			case 0xEFFFE: case 0xEFFFF: case 0xFFFFE: case 0xFFFFF: case 0x10FFFE: case 0x10FFFF:
				// parse error
				data.Append (cref.ToString ());
				return;
			default:
				if ((value >= 0xD800 && value <= 0xDFFF) || value > 0x10FFFF) {
					// parse error, emit REPLACEMENT CHARACTER
					data.Append ((char) 0xFFFD);
					return;
				}

				if ((value >= 0x0001 && value <= 0x0008) || (value >= 0x000D && value <= 0x001F) ||
					(value >= 0x007F && value <= 0x009F) || (value >= 0xFDD0 && value <= 0xFDEF)) {
					// parse error
					data.Append (cref.ToString ());
					return;
				}
				break;
			}

			data.Append (char.ConvertFromUtf32 (value));
		}

		void ReadCharacterReferenceInData (int additionalAllowedCharacter = -1)
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
				if (nc == additionalAllowedCharacter) {
					TokenizerState = HtmlTokenizerState.Data;
					data.Append ('&');
					return;
				}
				break;
			}

			cref.Append ('&');
			cref.Append (c);
			text.Read ();

			if (c == '#') {
				ReadNumericCharacterReference ();
				cref.Clear ();
				return;
			}

			while (c != ';') {
				if ((nc = text.Read ()) == -1) {
					TokenizerState = HtmlTokenizerState.EndOfFile;
					data.Append (cref.ToString ());
					return;
				}

				c = (char) nc;
			}

			cref.Append (';');

			TokenizerState = HtmlTokenizerState.Data;
			var name = cref.ToString ();
			string value;

			if (HtmlNamedCharacterReferences.TryGetValue (name, out value))
				data.Append (value);
			else
				data.Append (name);

			cref.Clear ();
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
