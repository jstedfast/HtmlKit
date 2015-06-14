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
using System.IO;
using System.Text;
using System.Collections.Generic;

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
	/// An HTML comment token.
	/// </summary>
	/// <remarks>
	/// An HTML comment token.
	/// </remarks>
	public sealed class HtmlCommentToken : HtmlToken
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlKit.HtmlCommentToken"/> class.
		/// </summary>
		/// <remarks>
		/// Creates a new <see cref="HtmlCommentToken"/>.
		/// </remarks>
		/// <param name="comment">The comment text.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="comment"/> is <c>null</c>.
		/// </exception>
		public HtmlCommentToken (string comment) : base (HtmlTokenKind.Comment)
		{
			if (comment == null)
				throw new ArgumentNullException ("comment");

			Comment = comment;
		}

		/// <summary>
		/// Get the comment.
		/// </summary>
		/// <remarks>
		/// Gets the comment.
		/// </remarks>
		/// <value>The comment.</value>
		public string Comment {
			get; internal set;
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="HtmlKit.HtmlCommentToken"/>.
		/// </summary>
		/// <remarks>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="HtmlKit.HtmlCommentToken"/>.
		/// </remarks>
		/// <returns>A <see cref="System.String"/> that represents the current <see cref="HtmlKit.HtmlCommentToken"/>.</returns>
		public override string ToString ()
		{
			return string.Format ("<!--{0}-->", Comment);
		}
	}

	/// <summary>
	/// An HTML token constisting of character data.
	/// </summary>
	/// <remarks>
	/// An HTML token consisting of character data.
	/// </remarks>
	public sealed class HtmlDataToken : HtmlToken
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlKit.HtmlDataToken"/> class.
		/// </summary>
		/// <remarks>
		/// Creates a new <see cref="HtmlDataToken"/>.
		/// </remarks>
		/// <param name="data">The character data.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="data"/> is <c>null</c>.
		/// </exception>
		public HtmlDataToken (string data) : base (HtmlTokenKind.Data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");

			Data = data;
		}

		/// <summary>
		/// Get the character data.
		/// </summary>
		/// <remarks>
		/// Gets the character data.
		/// </remarks>
		/// <value>The character data.</value>
		public string Data {
			get; private set;
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="HtmlKit.HtmlDataToken"/>.
		/// </summary>
		/// <remarks>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="HtmlKit.HtmlDataToken"/>.
		/// </remarks>
		/// <returns>A <see cref="System.String"/> that represents the current <see cref="HtmlKit.HtmlDataToken"/>.</returns>
		public override string ToString ()
		{
			return HtmlUtils.HtmlEncode (Data);
		}
	}

	/// <summary>
	/// An HTML tag token.
	/// </summary>
	/// <remarks>
	/// An HTML tag token.
	/// </remarks>
	public sealed class HtmlTagToken : HtmlToken
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlKit.HtmlTagToken"/> class.
		/// </summary>
		/// <remarks>
		/// Creates a new <see cref="HtmlTagToken"/>.
		/// </remarks>
		/// <param name="name">The name of the tag.</param>
		/// <param name="attributes">The attributes.</param>
		/// <param name="isEmptyElement"><c>true</c> if the tag is an empty element; otherwise, <c>false</c>.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <para><paramref name="name"/> is <c>null</c>.</para>
		/// <para>-or-</para>
		/// <para><paramref name="attributes"/> is <c>null</c>.</para>
		/// </exception>
		public HtmlTagToken (string name, IEnumerable<HtmlAttribute> attributes, bool isEmptyElement) : base (HtmlTokenKind.Tag)
		{
			if (name == null)
				throw new ArgumentNullException ("name");

			if (attributes == null)
				throw new ArgumentNullException ("attributes");

			Attributes = new HtmlAttributeCollection (attributes);
			IsEmptyElement = isEmptyElement;
			Id = name.ToHtmlTagId ();
			Name = name;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlKit.HtmlTagToken"/> class.
		/// </summary>
		/// <remarks>
		/// Creates a new <see cref="HtmlTagToken"/>.
		/// </remarks>
		/// <param name="name">The name of the tag.</param>
		/// <param name="isEndTag"><c>true</c> if the tag is an end tag; otherwise, <c>false</c>.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="name"/> is <c>null</c>.
		/// </exception>
		public HtmlTagToken (string name, bool isEndTag) : base (HtmlTokenKind.Tag)
		{
			if (name == null)
				throw new ArgumentNullException ("name");

			Attributes = new HtmlAttributeCollection ();
			Id = name.ToHtmlTagId ();
			IsEndTag = isEndTag;
			Name = name;
		}

		/// <summary>
		/// Get the attributes.
		/// </summary>
		/// <remarks>
		/// Gets the attributes.
		/// </remarks>
		/// <value>The attributes.</value>
		public HtmlAttributeCollection Attributes {
			get; private set;
		}

		/// <summary>
		/// Get the HTML tag identifier.
		/// </summary>
		/// <remarks>
		/// Gets the HTML tag identifier.
		/// </remarks>
		/// <value>The HTML tag identifier.</value>
		public HtmlTagId Id {
			get; private set;
		}

		/// <summary>
		/// Get whether or not the tag is an empty element.
		/// </summary>
		/// <remarks>
		/// Gets whether or not the tag is an empty element.
		/// </remarks>
		/// <value><c>true</c> if the tag is an empty element; otherwise, <c>false</c>.</value>
		public bool IsEmptyElement {
			get; internal set;
		}

		/// <summary>
		/// Get whether or not the tag is an end tag.
		/// </summary>
		/// <remarks>
		/// Gets whether or not the tag is an end tag.
		/// </remarks>
		/// <value><c>true</c> if the tag is an end tag; otherwise, <c>false</c>.</value>
		public bool IsEndTag {
			get; private set;
		}

		/// <summary>
		/// Get the name of the tag.
		/// </summary>
		/// <remarks>
		/// Gets the name of the tag.
		/// </remarks>
		/// <value>The name.</value>
		public string Name {
			get; private set;
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="HtmlKit.HtmlTagToken"/>.
		/// </summary>
		/// <remarks>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="HtmlKit.HtmlTagToken"/>.
		/// </remarks>
		/// <returns>A <see cref="System.String"/> that represents the current <see cref="HtmlKit.HtmlTagToken"/>.</returns>
		public override string ToString ()
		{
			var encoded = new StringBuilder ();

			using (var output = new StringWriter (encoded)) {
				output.Write ('<');
				if (IsEndTag)
					output.Write ('/');
				output.Write (Name);
				for (int i = 0; i < Attributes.Count; i++) {
					output.Write (' ');
					output.Write (Attributes[i].Name);
					if (Attributes[i].Value != null) {
						output.Write ('=');
						HtmlUtils.HtmlEncodeAttribute (output, Attributes[i].Value);
					}
				}
				if (IsEmptyElement)
					output.Write ('/');
				output.Write ('>');
			}

			return encoded.ToString ();
		}
	}

	/// <summary>
	/// An HTML DOCTYPE token.
	/// </summary>
	/// <remarks>
	/// An HTML DOCTYPE token.
	/// </remarks>
	public sealed class HtmlDocTypeToken : HtmlToken
	{
		string publicIdentifier;
		string systemIdentifier;
		string tagName;

		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlKit.HtmlDocTypeToken"/> class.
		/// </summary>
		/// <remarks>
		/// Creates a new <see cref="HtmlDocTypeToken"/>.
		/// </remarks>
		internal HtmlDocTypeToken (string doctype) : base (HtmlTokenKind.DocType)
		{
			tagName = doctype;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlKit.HtmlDocTypeToken"/> class.
		/// </summary>
		/// <remarks>
		/// Creates a new <see cref="HtmlDocTypeToken"/>.
		/// </remarks>
		public HtmlDocTypeToken () : base (HtmlTokenKind.DocType)
		{
			tagName = "DOCTYPE";
		}

		/// <summary>
		/// Get whether or not quirks-mode should be forced.
		/// </summary>
		/// <remarks>
		/// Gets whether or not quirks-mode should be forced.
		/// </remarks>
		/// <value><c>true</c> if quirks-mode should be forced; otherwise, <c>false</c>.</value>
		public bool ForceQuirksMode {
			get; set;
		}

		/// <summary>
		/// Get or set the DOCTYPE name.
		/// </summary>
		/// <remarks>
		/// Gets or sets the DOCTYPE name.
		/// </remarks>
		/// <value>The name.</value>
		public string Name {
			get; set;
		}

		/// <summary>
		/// Get or set the public identifier.
		/// </summary>
		/// <remarks>
		/// Gets or sets the public identifier.
		/// </remarks>
		/// <value>The public identifier.</value>
		public string PublicIdentifier {
			get { return publicIdentifier; }
			set {
				publicIdentifier = value;
				if (value != null) {
					if (PublicKeyword == null)
						PublicKeyword = "PUBLIC";
				} else {
					if (systemIdentifier != null)
						SystemKeyword = "SYSTEM";
				}
			}
		}

		/// <summary>
		/// Get the public keyword that was used.
		/// </summary>
		/// <remarks>
		/// Gets the public keyword that was used.
		/// </remarks>
		/// <value>The public keyword or <c>null</c> if it wasn't used.</value>
		public string PublicKeyword {
			get; internal set;
		}

		/// <summary>
		/// Get or set the system identifier.
		/// </summary>
		/// <remarks>
		/// Gets or sets the system identifier.
		/// </remarks>
		/// <value>The system identifier.</value>
		public string SystemIdentifier {
			get { return systemIdentifier; }
			set {
				systemIdentifier = value;
				if (value != null) {
					if (publicIdentifier == null && SystemKeyword == null)
						SystemKeyword = "SYSTEM";
				} else {
					SystemKeyword = null;
				}
			}
		}

		/// <summary>
		/// Get the system keyword that was used.
		/// </summary>
		/// <remarks>
		/// Gets the system keyword that was used.
		/// </remarks>
		/// <value>The system keyword or <c>null</c> if it wasn't used.</value>
		public string SystemKeyword {
			get; internal set;
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="HtmlKit.HtmlDocTypeToken"/>.
		/// </summary>
		/// <remarks>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="HtmlKit.HtmlDocTypeToken"/>.
		/// </remarks>
		/// <returns>A <see cref="System.String"/> that represents the current <see cref="HtmlKit.HtmlDocTypeToken"/>.</returns>
		public override string ToString ()
		{
			var encoded = new StringBuilder ();

			encoded.Append ("<!");
			encoded.Append (tagName);
			if (Name != null) {
				encoded.Append (' ');
				encoded.Append (Name);
			}
			if (PublicIdentifier != null) {
				encoded.Append (' ');
				encoded.Append (PublicKeyword);
				encoded.Append (" \"");
				encoded.Append (PublicIdentifier);
				encoded.Append ('"');
				if (SystemIdentifier != null) {
					encoded.Append (" \"");
					encoded.Append (SystemIdentifier);
					encoded.Append ('"');
				}
			} else if (SystemIdentifier != null) {
				encoded.Append (' ');
				encoded.Append (SystemKeyword);
				encoded.Append (" \"");
				encoded.Append (SystemIdentifier);
				encoded.Append ('"');
			}
			encoded.Append ('>');

			return encoded.ToString ();
		}
	}
}
