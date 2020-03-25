//
// Program.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2015-2020 Xamarin Inc. (www.xamarin.com)
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
using System.Collections.Generic;

using Newtonsoft.Json;

namespace CodeGenerator {
	class GraphNode
	{
		public readonly List<GraphNode> Children = new List<GraphNode> ();
		public readonly string Name;
		public readonly int State;
		public readonly char Char;
		public string Value;

		public GraphNode (GraphNode parent, int state, char c)
		{
			State = state;
			Char = c;

			if (parent != null) {
				parent.Children.Add (this);
				Name = parent.Name + c;
			} else {
				Name = "&";
			}
		}
	}

	class InnerSwitchLabel
	{
		public readonly int CurrentState;
		public readonly int NextState;
		public readonly string Comment;

		public InnerSwitchLabel (int current, int next, string comment)
		{
			CurrentState = current;
			NextState = next;
			Comment = comment;
		}
	}

	class Program
	{
		static readonly SortedDictionary<char, SortedDictionary<int, InnerSwitchLabel>> OuterSwitchLabels = new SortedDictionary<char, SortedDictionary<int, InnerSwitchLabel>> ();
		static readonly SortedDictionary<int, GraphNode> FinalStates = new SortedDictionary<int, GraphNode> ();
		static readonly GraphNode Root = new GraphNode (null, 0, '\0');

		public static void Main (string[] args)
		{
			int maxEntityLength = 0;
			int state = 0;

			using (var json = new JsonTextReader (new StreamReader ("HtmlEntities.json"))) {
				while (json.Read ()) {
					string name, value;

					if (json.TokenType == JsonToken.StartObject)
						continue;

					if (json.TokenType != JsonToken.PropertyName)
						break;

					name = (string) json.Value;

					// trim leading '&'
					name = name.Substring (1);

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

					var node = Root;

					for (int i = 0; i < name.Length; i++) {
						bool found = false;

						for (int j = 0; j < node.Children.Count; j++) {
							if (node.Children[j].Char == name[i]) {
								node = node.Children[j];
								found = true;
								break;
							}
						}

						if (!found) {
							node = new GraphNode (node, ++state, name[i]);
							continue;
						}
					}

					if (node.Value == null) {
						FinalStates.Add (node.State, node);
						node.Value = value;
					}

					maxEntityLength = Math.Max (maxEntityLength, name.Length + 1);

					if (!json.Read () || json.TokenType != JsonToken.EndObject)
						break;
				}
			}

			GenerateSwitchLabels (Root);

			using (var output = new StreamWriter ("HtmlEntityDecoder.g.cs")) {
				output.WriteLine ("// WARNING: This file is auto-generated. DO NOT EDIT!");
				output.WriteLine ();
				output.WriteLine ("using System.Collections.Generic;");
				output.WriteLine ();
				output.WriteLine ("namespace HtmlKit {");
				output.WriteLine ("\tpublic partial class HtmlEntityDecoder");
				output.WriteLine ("\t{");
				output.WriteLine ("\t\tconst int MaxEntityLength = {0};", maxEntityLength);
				output.WriteLine ();
				GenerateStaticConstructor (output);
				output.WriteLine ();
				GenerateBinarySearchMethod (output);
				output.WriteLine ();
				GeneratePushNamedEntityMethod (output);
				output.WriteLine ();
				GenerateGetNamedEntityValueMethod (output);
				output.WriteLine ("\t}");
				output.WriteLine ("}");
			}
		}

		static void GenerateSwitchLabels (GraphNode node)
		{
			foreach (var child in node.Children) {
				SortedDictionary<int, InnerSwitchLabel> states;
				InnerSwitchLabel inner;

				if (!OuterSwitchLabels.TryGetValue (child.Char, out states))
					OuterSwitchLabels[child.Char] = states = new SortedDictionary<int, InnerSwitchLabel> ();

				if (!states.TryGetValue (node.State, out inner))
					states[node.State] = new InnerSwitchLabel (node.State, child.State, string.Format ("{0} -> {1}", node.Name, child.Name));

				GenerateSwitchLabels (child);
			}
		}

		static string GetTransitionTableName (char c)
		{
			return string.Format ("TransitionTable_{0}", c == ';' ? "semicolon" : c.ToString ());
		}

		static void GenerateStaticConstructor (TextWriter output)
		{
			int index;

			output.WriteLine ("\t\tstatic readonly Dictionary<int, string> NamedEntities;");
			output.WriteLine ();
			output.WriteLine ("\t\tstruct Transition");
			output.WriteLine ("\t\t{");
			output.WriteLine ("\t\t\tpublic readonly int From;");
			output.WriteLine ("\t\t\tpublic readonly int To;");
			output.WriteLine ();
			output.WriteLine ("\t\t\tpublic Transition (int from, int to)");
			output.WriteLine ("\t\t\t{");
			output.WriteLine ("\t\t\t\tFrom = from;");
			output.WriteLine ("\t\t\t\tTo = to;");
			output.WriteLine ("\t\t\t}");
			output.WriteLine ("\t\t}");
			output.WriteLine ();
			foreach (var outer in OuterSwitchLabels)
				output.WriteLine ("\t\tstatic readonly Transition[] {0};", GetTransitionTableName (outer.Key));
			output.WriteLine ();
			output.WriteLine ("\t\tstatic HtmlEntityDecoder ()");
			output.WriteLine ("\t\t{");
			foreach (var outer in OuterSwitchLabels) {
				var tableName = GetTransitionTableName (outer.Key);
				index = 0;

				output.WriteLine ("\t\t\t{0} = new Transition[{1}] {{", tableName, outer.Value.Count);
				foreach (var state in outer.Value) {
					var comma = index + 1 < outer.Value.Count ? "," : string.Empty;
					var value = state.Value;

					output.WriteLine ("\t\t\t\tnew Transition ({0}, {1}){2} // {3}", value.CurrentState, value.NextState, comma, value.Comment);
					index++;
				}
				output.WriteLine ("\t\t\t};");
			}
			output.WriteLine ();
			output.WriteLine ("\t\t\tNamedEntities = new Dictionary<int, string> {");
			index = 0;
			foreach (var kvp in FinalStates) {
				var comma = index < FinalStates.Count ? "," : string.Empty;
				var state = kvp.Value;

				output.Write ("\t\t\t\t[{0}] = \"", state.State);
				for (int i = 0; i < state.Value.Length; i++)
					output.Write ("\\u{0:X4}", (int) state.Value[i]);
				output.WriteLine ("\"{0} // {1}", comma, state.Name);
				index++;
			}
			output.WriteLine ("\t\t\t};");
			output.WriteLine ("\t\t}");
		}

		static void GenerateBinarySearchMethod (TextWriter output)
		{
			output.WriteLine ("\t\tstatic int BinarySearchNextState (Transition[] transitions, int state)");
			output.WriteLine ("\t\t{");
			output.WriteLine ("\t\t\tint min = 0, max = transitions.Length;");
			output.WriteLine ();
			output.WriteLine ("\t\t\tdo {");
			output.WriteLine ("\t\t\t\tint i = min + ((max - min) / 2);");
			output.WriteLine ();
			output.WriteLine ("\t\t\t\tif (state > transitions[i].From) {");
			output.WriteLine ("\t\t\t\t\tmin = i + 1;");
			output.WriteLine ("\t\t\t\t} else if (state < transitions[i].From) {");
			output.WriteLine ("\t\t\t\t\tmax = i;");
			output.WriteLine ("\t\t\t\t} else {");
			output.WriteLine ("\t\t\t\t\treturn transitions[i].To;");
			output.WriteLine ("\t\t\t\t}");
			output.WriteLine ("\t\t\t} while (min < max);");
			output.WriteLine ();
			output.WriteLine ("\t\t\treturn -1;");
			output.WriteLine ("\t\t}");
		}

		static void GeneratePushNamedEntityMethod (TextWriter output)
		{
			output.WriteLine ("\t\tbool PushNamedEntity (char c)");
			output.WriteLine ("\t\t{");
			output.WriteLine ("\t\t\tint next, state = states[index - 1];");
			output.WriteLine ("\t\t\tTransition[] table = null;");
			output.WriteLine ();
			output.WriteLine ("\t\t\tswitch (c) {");
			foreach (var outer in OuterSwitchLabels)
				output.WriteLine ("\t\t\tcase '{0}': table = {1}; break;", outer.Key, GetTransitionTableName (outer.Key));
			output.WriteLine ("\t\t\tdefault: return false;");
			output.WriteLine ("\t\t\t}"); // end switch (c)
			output.WriteLine ();
			output.WriteLine ("\t\t\tif ((next = BinarySearchNextState (table, state)) == -1)");
			output.WriteLine ("\t\t\t\treturn false;");
			output.WriteLine ();
			output.WriteLine ("\t\t\tstates[index] = next;");
			output.WriteLine ("\t\t\tpushed[index] = c;");
			output.WriteLine ("\t\t\tindex++;");
			output.WriteLine ();
			output.WriteLine ("\t\t\treturn true;");
			output.WriteLine ("\t\t}");
		}

		static void GenerateGetNamedEntityValueMethod (TextWriter output)
		{
			output.WriteLine ("\t\tstring GetNamedEntityValue ()");
			output.WriteLine ("\t\t{");
			output.WriteLine ("\t\t\tint startIndex = index;");
			output.WriteLine ("\t\t\tstring decoded = null;");
			output.WriteLine ();
			output.WriteLine ("\t\t\twhile (startIndex > 0) {");
			output.WriteLine ("\t\t\t\tif (NamedEntities.TryGetValue (states[startIndex - 1], out decoded))");
			output.WriteLine ("\t\t\t\t\tbreak;");
			output.WriteLine ();
			output.WriteLine ("\t\t\t\tstartIndex--;");
			output.WriteLine ("\t\t\t}");
			output.WriteLine ();
			output.WriteLine ("\t\t\tif (decoded == null)");
			output.WriteLine ("\t\t\t\tdecoded = string.Empty;");
			output.WriteLine ();
			output.WriteLine ("\t\t\tif (startIndex < index)");
			output.WriteLine ("\t\t\t\tdecoded += new string (pushed, startIndex, index - startIndex);");
			output.WriteLine ();
			output.WriteLine ("\t\t\treturn decoded;");
			output.WriteLine ("\t\t}");
		}
	}
}
