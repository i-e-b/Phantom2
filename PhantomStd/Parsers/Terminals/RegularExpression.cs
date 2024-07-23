using System.Text.RegularExpressions;
using PhantomStd.Parsers.Interfaces;

namespace PhantomStd.Parsers.Terminals
{
	/// <summary>
	/// Parse for a regular expression match.
	/// NOTE: It's best to keep the regular expressions simple, and deal with any recursion in the Parser structure.
	/// </summary>
	public class RegularExpression : Parser, IMatchingParser
	{
		private readonly string _pattern;
		private readonly Regex _test;

		/// <summary>
		/// Create a regular expression parser with default options
		/// </summary>
		/// <param name="pattern">Regex pattern to match</param>
		public RegularExpression(string pattern)
		{
			this._pattern = pattern;
			_test = new Regex(pattern);
		}

		/// <summary>
		/// Create a regular expression parser with customised options
		/// </summary>
		/// <param name="pattern">Regex pattern to match</param>
		/// <param name="options">Options set to use</param>
		public RegularExpression(string pattern, RegexOptions options)
		{
			this._pattern = pattern;
			_test = new Regex(pattern, options);
		}

		/// <summary>
		/// Test the regular expression.
		/// </summary>
		/// <remarks>This is done on the entire input.
		/// This might cause problems with file-stream parsing.</remarks>
		public ParserMatch TryMatch(IScanner scan)
		{
			int offset = scan.Offset;

			string remains = scan.RemainingData();
			var result = _test.Match(remains);

			if (result.Success && result.Index == 0)
			{
				scan.Seek(offset + result.Length);
				return scan.CreateMatch(this, offset, result.Length);
			}

			return scan.NoMatch;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return "/"+_pattern+"/";
		}
	}
}