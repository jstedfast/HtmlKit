//
// CharBuffer.cs
//
// Author: Jeffrey Stedfast <jestedfa@microsoft.com>
//
// Copyright (c) 2015-2023 Jeffrey Stedfast <jestedfa@microsoft.com>
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
using System.Runtime.CompilerServices;

namespace HtmlKit {
	class CharBuffer2
	{
		char[] buffer;
		int start;
		int length;

		public CharBuffer2 (int capacity)
		{
			buffer = new char[capacity];
		}

		public int Capacity {
			get => buffer.Length;
		}

		public void Reset()
		{
			start = 0;
			length = 0;
		}

		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		public void Next()
		{
			start += length;
			length = 0;
		}

		public int Length {
			[MethodImpl (MethodImplOptions.AggressiveInlining)]
			get => length;
		}

		public char this[int index] {
			[MethodImpl (MethodImplOptions.AggressiveInlining)]
			get { return buffer[index]; }
			[MethodImpl (MethodImplOptions.AggressiveInlining)]
			set { buffer[index] = value; }
		}

		public void MoveBack()
		{
			length--;
		}

		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		void EnsureCapacity (int length)
		{
			int totalLength = start + length;
			if (totalLength < buffer.Length)
				return;

			int capacity = buffer.Length << 1;
			while (capacity <= totalLength)
				capacity <<= 1;

			Array.Resize (ref buffer, capacity);
		}

		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		public void Append (char c)
		{
			EnsureCapacity (Length + 1);
			buffer[start + length++] = c;
		}

		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		public void Append (string str)
		{
			EnsureCapacity (Length + str.Length);
			str.CopyTo (0, buffer, start + length, str.Length);
			length += str.Length;
		}

		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		public override string ToString ()
		{
#if NETSTANDARD2_0
			unsafe {
				fixed (char* pointer = buffer) {
					return new string (pointer, start, Length);
				}
			}
#else
			ReadOnlySpan<char> slice = buffer.AsSpan ().Slice (start, length);

			return new string (slice);
#endif
		}

		//public static implicit operator string (CharBuffer buffer)
		//{
		//	return buffer.ToString ();
		//}
	}
}
