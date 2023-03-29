using System.Diagnostics;
using System.Text;

using HtmlKit;

namespace ConsoleApp1
{
	internal class Program
	{
		static void Main (string[] args)
		{
			var input = File.ReadAllText ("UGGRoyale.mjml");

			var v1 = TestV1(input);
			var v2 = TestV1 (input);

			var equals = v1.Equals (v2);
			Console.WriteLine (equals);

			ReadV1 (input);

			Console.WriteLine ("V2");
			ReadV2 (input);
		}

		private static string TestV1 (string input)
		{
			var sb = new StringBuilder ();

			var reader = new HtmlTokenizer (new StringReader (input));

			while (reader.ReadNextToken (out var token)) {
				sb.Append (token.ToString ());
			}

			return sb.ToString ();

		}

		private static string TestV2 (string input)
		{
			var sb = new StringBuilder ();

			using var reader = new HtmlTokenizer2 (new StringReader (input));

			while (reader.ReadNextToken (out var token)) {
				sb.Append (token.ToString ());
			}

			return sb.ToString ();
		}

		private static void ReadV2 (string input)
		{
			var watch = Stopwatch.StartNew ();

			for (var i = 0; i < 1000; i++) {

				var result = new List<object> ();
				using var reader = new HtmlTokenizer2 (new StringReader (input));

				while (reader.ReadNextToken (out var token)) {
					result.Add (token);
				}

			}

			Console.WriteLine (watch.Elapsed);
			watch.Stop ();
		}

		private static void ReadV1 (string input)
		{
			var watch = Stopwatch.StartNew ();

			for (var i = 0; i < 1000; i++) {

				var result = new List<object> ();
				var reader = new HtmlTokenizer2 (new StringReader (input));

				while (reader.ReadNextToken (out var token)) {
					result.Add (token);
				}

			}

			Console.WriteLine (watch.Elapsed);
			watch.Stop ();
		}
	}
}
