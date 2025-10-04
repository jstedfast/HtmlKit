﻿//
// HtmlTokenizerBenchmarks.cs
//
// Author: Jeffrey Stedfast <jestedfa@microsoft.com>
//
// Copyright (c) 2013-2025 Jeffrey Stedfast <jestedfa@microsoft.com>
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
using System.Xml;

using BenchmarkDotNet.Attributes;

using HtmlKit;

using ASHtmlToken = AngleSharp.Html.Parser.Tokens.HtmlToken;
using ASHtmlTokenizer = AngleSharp.Html.Parser.HtmlTokenizer;
using ASHtmlTokenType = AngleSharp.Html.Parser.HtmlTokenType;

namespace Benchmarks
{
	public class HtmlTokenizerBenchmarks
	{
		static readonly string HtmlDataDir = Path.Combine (BenchmarkHelper.ProjectDir, "TestData", "html");

		#region HtmlKit

		static void HtmlKit_TokenizeTextReader (string fileName)
		{
			var path = Path.Combine (HtmlDataDir, fileName);
			using var reader = new StreamReader (path);
			var tokenizer = new HtmlTokenizer (reader);

			while (tokenizer.ReadNextToken (out var token))
				;
		}

		[Benchmark]
		public void HtmlKit_TextReader_Xamarin3 ()
		{
			HtmlKit_TokenizeTextReader ("xamarin3.xhtml");
		}

		static void HtmlKit_TokenizeStream (string fileName)
		{
			var path = Path.Combine (HtmlDataDir, fileName);
			using var stream = File.OpenRead (path);
			var tokenizer = new HtmlTokenizer (stream);

			while (tokenizer.ReadNextToken (out var token))
				;
		}

		[Benchmark]
		public void HtmlKit_Stream_Xamarin3 ()
		{
			HtmlKit_TokenizeStream ("xamarin3.xhtml");
		}

		#endregion HtmlKit

		#region HtmlPerformanceKit

		static void HtmlPerformanceKit_TokenizeFile (string fileName)
		{
			var path = Path.Combine (HtmlDataDir, fileName);
			using var reader = new StreamReader (path);
			var tokenizer = new HtmlPerformanceKit.HtmlReader (reader);

			while (tokenizer.Read ()) {
				switch (tokenizer.TokenKind) {
				case HtmlPerformanceKit.HtmlTokenKind.Doctype:
					for (int i = 0; i < tokenizer.AttributeCount; i++) {
						var attrName = tokenizer.GetAttributeName (i);
						var attrValue = tokenizer.GetAttribute (i);
					}
					break;
				case HtmlPerformanceKit.HtmlTokenKind.Tag:
					var tagName = tokenizer.Name;
					for (int i = 0; i < tokenizer.AttributeCount; i++) {
						var attrName = tokenizer.GetAttributeName (i);
						var attrValue = tokenizer.GetAttribute (i);
					}
					break;
				case HtmlPerformanceKit.HtmlTokenKind.EndTag:
					var endTagName = tokenizer.Name;
					break;
				case HtmlPerformanceKit.HtmlTokenKind.Comment:
					var comment = tokenizer.Text;
					break;
				case HtmlPerformanceKit.HtmlTokenKind.Text:
					var text = tokenizer.Text;
					break;
				}
			}
		}

		[Benchmark]
		public void HtmlPerformanceKit_Xamarin3 ()
		{
			HtmlPerformanceKit_TokenizeFile ("xamarin3.xhtml");
		}

		#endregion HtmlPerformanceKit

		#region AngleSharp

		static void AngleSharp_TokenizeFile (string fileName)
		{
			var path = Path.Combine (HtmlDataDir, fileName);
			using var stream = File.OpenRead (path);
			using var source = new AngleSharp.Text.TextSource (stream);

			var tokenizer = new ASHtmlTokenizer (source, AngleSharp.Html.HtmlEntityProvider.Resolver);
			ASHtmlToken token;

			do {
				token = tokenizer.Get ();
			} while (token.Type != ASHtmlTokenType.EndOfFile);
		}

		[Benchmark]
		public void AngleSharp_Xamarin3 ()
		{
			AngleSharp_TokenizeFile ("xamarin3.xhtml");
		}

		#endregion AngleSharp

		#region XmlReader

		static void XmlReader_TokenizeFile (string fileName)
		{
			var path = Path.Combine (HtmlDataDir, fileName);
			using var stream = File.OpenRead (path);

			var settings = new XmlReaderSettings () {
				DtdProcessing = DtdProcessing.Parse
			};

			var reader = XmlReader.Create (stream, settings);

			while (reader.Read ()) {
				switch (reader.NodeType) {
				case XmlNodeType.Attribute:
					var attrName = reader.Name;
					var attrValue = reader.Value;
					break;
				case XmlNodeType.Element:
				case XmlNodeType.EndElement:
					var tagName = reader.Name;
					break;
				case XmlNodeType.EntityReference:
					var entityReference = reader.Value;
					break;
				case XmlNodeType.CDATA:
				case XmlNodeType.Text:
				case XmlNodeType.Whitespace:
				case XmlNodeType.SignificantWhitespace:
					var text = reader.ReadContentAsString ();
					break;
				case XmlNodeType.DocumentType:
					var lang = reader.XmlLang;
					break;
				}
			}
		}

		[Benchmark]
		public void XmlReader_Xamarin3 ()
		{
			XmlReader_TokenizeFile ("xamarin3.xhtml");
		}

		#endregion XmlReader
	}
}
