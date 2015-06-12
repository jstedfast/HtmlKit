//
// HtmlNamedCharacterReferences.cs
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
using System.Collections.Generic;

namespace HtmlKit {
	/// <summary>
	/// An HTML named character reference.
	/// </summary>
	/// <remarks>
	/// An HTML named character reference.
	/// </remarks>
	public static class HtmlNamedCharacterReferences
	{
		static readonly Dictionary<string, int> references;

		static HtmlNamedCharacterReferences ()
		{
			references = new Dictionary<string, int> ();
		}

		/// <summary>
		/// Get the named character reference value, if it exists.
		/// </summary>
		/// <remarks>
		/// Gets the named character reference value, if it exists.
		/// </remarks>
		/// <returns><c>true</c>, if get value exists; otherwise, <c>false</c>.</returns>
		/// <param name="name">The name of the character reference.</param>
		/// <param name="codepoint">The UTF-32 codepoint.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="name"/> is <c>null</c>.
		/// </exception>
		public static bool TryGetValue (string name, out int codepoint)
		{
			if (name == null)
				throw new ArgumentNullException ("name");

			return references.TryGetValue (name, out codepoint);
		}

		/// <summary>
		/// Get the named character reference value, if it exists.
		/// </summary>
		/// <remarks>
		/// Gets the named character reference value, if it exists.
		/// </remarks>
		/// <returns><c>true</c>, if get value exists; otherwise, <c>false</c>.</returns>
		/// <param name="name">The name of the character reference.</param>
		/// <param name="value">The unicode character(s).</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="name"/> is <c>null</c>.
		/// </exception>
		public static bool TryGetValue (string name, out string value)
		{
			int codepoint;

			if (name == null)
				throw new ArgumentNullException ("name");

			if (references.TryGetValue (name, out codepoint))
				value = char.ConvertFromUtf32 (codepoint);

			return value != null;
		}
	}
}
