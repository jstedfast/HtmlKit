//
// HtmlWriterTests.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (www.xamarin.com)
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

using NUnit.Framework;

using HtmlKit;

namespace UnitTests {
	[TestFixture]
	public class HtmlWriterTests
	{
		[Test]
		public void TestArgumentExceptions ()
		{
			Assert.Throws<ArgumentNullException> (() => new HtmlWriter (null, Encoding.UTF8));
			Assert.Throws<ArgumentNullException> (() => new HtmlWriter (new MemoryStream (), null));
			Assert.Throws<ArgumentNullException> (() => new HtmlWriter (null));

			using (var html = new HtmlWriter (new StringWriter ())) {
				Assert.Throws<ArgumentNullException> (() => html.WriteAttribute (null));
				Assert.Throws<ArgumentNullException> (() => html.WriteAttribute (null, string.Empty));
				Assert.Throws<ArgumentNullException> (() => html.WriteAttribute ("name", null));
				Assert.Throws<ArgumentException> (() => html.WriteAttribute (string.Empty, null));
				Assert.Throws<ArgumentException> (() => html.WriteAttribute ("a b c", null));
				Assert.Throws<ArgumentNullException> (() => html.WriteAttribute (null, new char[1], 0, 1));
				Assert.Throws<ArgumentNullException> (() => html.WriteAttribute ("name", null, 0, 0));
				Assert.Throws<ArgumentOutOfRangeException> (() => html.WriteAttribute ("name", new char[0], -1, 0));
				Assert.Throws<ArgumentOutOfRangeException> (() => html.WriteAttribute ("name", new char[0], 0, 1));
				Assert.Throws<ArgumentException> (() => html.WriteAttribute (HtmlAttributeId.Unknown, new char[1], 0, 1));
				Assert.Throws<ArgumentNullException> (() => html.WriteAttribute (HtmlAttributeId.Alt, null, 0, 0));
				Assert.Throws<ArgumentOutOfRangeException> (() => html.WriteAttribute (HtmlAttributeId.Alt, new char[0], -1, 0));
				Assert.Throws<ArgumentOutOfRangeException> (() => html.WriteAttribute (HtmlAttributeId.Alt, new char[0], 0, 1));

				Assert.Throws<ArgumentException> (() => html.WriteAttributeName (HtmlAttributeId.Unknown));
				Assert.Throws<ArgumentNullException> (() => html.WriteAttributeName (null));

				Assert.Throws<ArgumentNullException> (() => html.WriteAttributeValue (null));
				Assert.Throws<ArgumentNullException> (() => html.WriteAttributeValue (null, 0, 0));
				Assert.Throws<ArgumentOutOfRangeException> (() => html.WriteAttributeValue (new char[0], -1, 0));
				Assert.Throws<ArgumentOutOfRangeException> (() => html.WriteAttributeValue (new char[0], 0, 1));

				Assert.Throws<ArgumentException> (() => html.WriteEmptyElementTag (HtmlTagId.Unknown));
				Assert.Throws<ArgumentNullException> (() => html.WriteEmptyElementTag (null));
				Assert.Throws<ArgumentException> (() => html.WriteEmptyElementTag (string.Empty));
				Assert.Throws<ArgumentException> (() => html.WriteEmptyElementTag ("a b c"));

				Assert.Throws<ArgumentException> (() => html.WriteEndTag (HtmlTagId.Unknown));
				Assert.Throws<ArgumentNullException> (() => html.WriteEndTag (null));
				Assert.Throws<ArgumentException> (() => html.WriteEndTag (string.Empty));
				Assert.Throws<ArgumentException> (() => html.WriteEndTag ("a b c"));

				Assert.Throws<ArgumentNullException> (() => html.WriteMarkupText (null));
				Assert.Throws<ArgumentNullException> (() => html.WriteMarkupText (null, 0, 0));
				Assert.Throws<ArgumentOutOfRangeException> (() => html.WriteMarkupText (new char[0], -1, 0));
				Assert.Throws<ArgumentOutOfRangeException> (() => html.WriteMarkupText (new char[0], 0, 1));

				Assert.Throws<ArgumentException> (() => html.WriteStartTag (HtmlTagId.Unknown));
				Assert.Throws<ArgumentNullException> (() => html.WriteStartTag (null));
				Assert.Throws<ArgumentException> (() => html.WriteStartTag (string.Empty));
				Assert.Throws<ArgumentException> (() => html.WriteStartTag ("a b c"));

				Assert.Throws<ArgumentNullException> (() => html.WriteText (null));
				Assert.Throws<ArgumentNullException> (() => html.WriteText (null, 0, 0));
				Assert.Throws<ArgumentOutOfRangeException> (() => html.WriteText (new char[0], -1, 0));
				Assert.Throws<ArgumentOutOfRangeException> (() => html.WriteText (new char[0], 0, 1));

				Assert.Throws<ArgumentNullException> (() => html.WriteToken (null));
			}
		}
	}
}
