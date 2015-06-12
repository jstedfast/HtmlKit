//
// HtmlToken.cs
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

namespace HtmlKit {
	/// <summary>
	/// An abstract HTML token class.
	/// </summary>
	/// <remarks>
	/// An abstract HTML token class.
	/// </remarks>
	public abstract class HtmlToken
	{
		/// <summary>
		/// An HTML token signifying the end of the file has been reached.
		/// </summary>
		/// <remarks>
		/// An HTML token signifying the end of the file has been reached.
		/// </remarks>
		public static readonly HtmlToken EndOfFile = new HtmlEndOfFileToken ();

		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlKit.HtmlToken"/> class.
		/// </summary>
		/// <remarks>
		/// Creates a new <see cref="HtmlToken"/>.
		/// </remarks>
		/// <param name="kind">The kind of token.</param>
		protected HtmlToken (HtmlTokenKind kind)
		{
			Kind = kind;
		}

		/// <summary>
		/// Get the kind of HTML token that this object represents.
		/// </summary>
		/// <remarks>
		/// Gets the kind of HTML token that this object represents.
		/// </remarks>
		/// <value>The kind of token.</value>
		public HtmlTokenKind Kind {
			get; private set;
		}
	}

	/// <summary>
	/// An HTML token signifying the end of the file has been reached.
	/// </summary>
	/// <remarks>
	/// An HTML token signifying the end of the file has been reached.
	/// </remarks>
	public sealed class HtmlEndOfFileToken : HtmlToken
	{
		internal HtmlEndOfFileToken () : base (HtmlTokenKind.EndOfFile)
		{
		}
	}

	public sealed class HtmlDataToken : HtmlToken
	{
		public HtmlDataToken (string data) : base (HtmlTokenKind.Data)
		{
			Data = data;
		}

		public string Data {
			get; private set;
		}
	}

	public sealed class HtmlTagToken : HtmlToken
	{
		public HtmlTagToken (string name, bool isEndTag) : base (HtmlTokenKind.Tag)
		{
			Attributes = new HtmlAttributeCollection ();
			IsEndTag = isEndTag;
			Name = name;
		}

		public HtmlAttributeCollection Attributes {
			get; private set;
		}

		public bool IsEmptyElement {
			get; internal set;
		}

		public bool IsEndTag {
			get; private set;
		}

		public string Name {
			get; private set;
		}
	}

	public sealed class HtmlCommentToken : HtmlToken
	{
		public HtmlCommentToken (string comment) : base (HtmlTokenKind.Comment)
		{
			if (comment == null)
				throw new ArgumentNullException ("comment");

			Comment = comment;
		}

		public string Comment {
			get; set;
		}
	}

	public sealed class HtmlDocTypeToken : HtmlToken
	{
		public HtmlDocTypeToken () : base (HtmlTokenKind.DocType)
		{
		}

		public bool ForceQuirks {
			get; set;
		}

		public string Name {
			get; set;
		}

		public string PublicIdentifier {
			get; set;
		}

		public string SystemIdentifier {
			get; set;
		}
	}
}
