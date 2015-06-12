//
// Program.cs
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
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace CodeGenerator {
	class Program
	{
		static readonly Dictionary<string, int> StateNameToInt32 = new Dictionary<string, int> ();
		static readonly Dictionary<int, string> Int32ToValue = new Dictionary<int, string> ();
		static readonly List<string> StateNames = new List<string> ();

		public static void Main (string[] args)
		{
			using (var json = new JsonTextReader (new StreamReader ("HtmlEntities.json"))) {
				while (json.Read ()) {
					string name, value;

					if (json.TokenType == JsonToken.StartObject)
						continue;

					if (json.TokenType != JsonToken.PropertyName)
						break;

					name = (string) json.Value;

					// trim leading '&' and trailing ';'
					name = name.Substring (1, name.Length - 2);

					if (!json.Read () || json.TokenType != JsonToken.StartObject)
						break;

					// read to the "codepoints" property
					if (!json.Read () || json.TokenType != JsonToken.PropertyName)
						break;

					// skip the array of integers...
					if (!json.Read () || json.TokenType != JsonToken.StartArray)
						break;

					while (json.Read ()) {
						if (json.TokenType == JsonToken.EndArray)
							break;
					}

					// the property should be "characters" - this is what we want
					if (!json.Read () || json.TokenType != JsonToken.PropertyName)
						break;

					value = json.ReadAsString ();

					var state = new char[name.Length];
					for (int i = 0; i < name.Length; i++) {
						state[i] = name[i];

						var key = new string (state, 0, i + 1);

						if (!StateNameToInt32.ContainsKey (key)) {
							StateNameToInt32.Add (key, StateNameToInt32.Count);
							StateNames.Add (key);
						}
					}

					Int32ToValue[StateNameToInt32[name]] = value;

					if (!json.Read () || json.TokenType != JsonToken.EndObject)
						break;
				}
			}

			GeneratePushNamedEntityMethod ();
			GenerateGetNamedEntityValueMethod ();
		}

		static string GetNextChars (string prefix)
		{
			var next = new StringBuilder ();

			foreach (var state in StateNames) {
				if (state.Length != prefix.Length + 1)
					continue;

				if (!state.StartsWith (prefix, StringComparison.Ordinal))
					continue;

				next.Append (state[state.Length - 1]);
			}

			return next.ToString ();
		}

		static void GeneratePushNamedEntityMethod ()
		{
			Console.WriteLine ("bool PushNamedEntity (char c)");
			Console.WriteLine ('{');
			Console.WriteLine ("\tswitch (state) {");
			foreach (var state in StateNames) {
				var next = GetNextChars (state);

				if (next.Length == 0)
					continue;

				if (next.Length == 1) {
					#if COMPACT
					Console.WriteLine ("\tcase {0}: if (c != '{1}') return false; state = {2}; break;", StateNameToInt32[state], next[0], StateNameToInt32[state + next[0]]);
					#else
					Console.WriteLine ("\tcase {0}:", StateNameToInt32[state]);
					Console.WriteLine ("\t\tif (c != '{0}') return false;", next[0]);
					Console.WriteLine ("\t\tstate = {0};", StateNameToInt32[state + next[0]]);
					Console.WriteLine ("\t\tbreak;");
					#endif
				} else {
					Console.WriteLine ("\tcase {0}:", StateNameToInt32[state]);
					Console.WriteLine ("\t\tswitch (c) {");
					foreach (var c in next)
						Console.WriteLine ("\t\tcase '{0}': state = {1}; break;", c, StateNameToInt32[state + c]);
					Console.WriteLine ("\t\tdefault: return false;");
					Console.WriteLine ("\t\t}"); // end switch (c)
					Console.WriteLine ("\t\tbreak;");
				}
			}
			Console.WriteLine ("\tdefault: return false;");
			Console.WriteLine ("\t}"); // end switch (state)
			Console.WriteLine ();
			Console.WriteLine ("\tpushed[index++] = c;");
			Console.WriteLine ();
			Console.WriteLine ("\treturn true;");
			Console.WriteLine ('}');
		}

		static void GenerateGetNamedEntityValueMethod ()
		{
			Console.WriteLine ("string GetNamedEntityValue ()");
			Console.WriteLine ('{');
			Console.WriteLine ("\tswitch (state) {");
			foreach (var state in StateNames) {
				int index = StateNameToInt32[state];
				string value;

				if (!Int32ToValue.TryGetValue (index, out value))
					continue;

				Console.Write ("\tcase {0}: return \"", index);
				for (int i = 0; i < value.Length; i++)
					Console.Write ("\\u{0:X4}", (int) value[i]);
				Console.WriteLine ("\"; // {0}", state);
			}
			Console.WriteLine ("\tdefault: return new string (pushed, 0, index);");
			Console.WriteLine ("\t}");
			Console.WriteLine ('}');
		}
	}
}
