using Phantom.Parsers.Composite.Abstracts;

namespace Phantom.Parsers.Composite
{
	/// <summary>
	/// Create an Difference parser from two sub-parsers.
	/// Should match left but not right.
	/// </summary>
	public class Difference : Binary
	{
		public Difference(IParser left, IParser right)
			: base(left, right)
		{
		}

		public override ParserMatch TryMatch(IScanner scan)
		{
			int offset = scan.Offset;

			var m = LeftParser.Parse(scan);

			int goodOffset = scan.Offset;

			if (!m.Success)
			{
				scan.Seek(offset);
				return scan.NoMatch;
			}

			// doing difference
			scan.Seek(offset);
			var m2 = RightParser.Parse(scan);
			if (m2.Success)
			{
				// fail: must match left but NOT right
				scan.Seek(offset);
				return scan.NoMatch;
			}

			// Good match
			scan.Seek(goodOffset);
			return m;
		}

		public override string ToString()
		{
			return LeftParser + " not "+ RightParser;
		}
	}
}