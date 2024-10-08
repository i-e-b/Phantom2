using Gool.Parsers.Composite.Abstracts;
using Gool.Results;
using Gool.Scanners;

namespace Gool.Parsers.Composite;

/// <summary>
/// Create an Exclusive-OR parser from two sub-parsers.
/// Can match first or second, but not both
/// </summary>
public class Exclusive : Binary
{
	/// <summary>
	/// Create an Exclusive-OR parser from two sub-parsers.
	/// Can match first or second, but not both
	/// </summary>
	public Exclusive(IParser first, IParser second)
		: base(first, second)
	{
	}

	/// <inheritdoc />
	internal override ParserMatch TryMatch(IScanner scan, ParserMatch? previousMatch, bool allowAutoAdvance)
	{
		// apply the first parser
		var leftMatch = LeftParser.Parse(scan, previousMatch, allowAutoAdvance);

		// apply the second parser
		var rightMatch = RightParser.Parse(scan, previousMatch, allowAutoAdvance);

		if (rightMatch.Success && leftMatch.Success)
		{
			// FAIL! they are not exclusive
			return scan.NoMatch(this, previousMatch);
		}

		// now return whichever one succeeded
		if (leftMatch.Success) return leftMatch.Through(this, previousMatch);
		if (rightMatch.Success) return rightMatch.Through(this, previousMatch);

		// neither were matched!
		return scan.NoMatch(this, previousMatch);
	}

	/// <inheritdoc />
	public override bool IsOptional() => LeftParser.IsOptional() || RightParser.IsOptional();

	/// <inheritdoc />
	public override string ToString()
	{
		if (Tag is null) return LeftParser + " ^ " + RightParser;
		return LeftParser + " ^ " + RightParser + " Tag='" + Tag + "'";
	}

	/// <inheritdoc />
	public override string ShortDescription(int depth)
	{
		if (depth < 1) return GetType().Name;
		return LeftParser.ShortDescription(depth - 1) + " ^ " + RightParser.ShortDescription(depth - 1);
	}
}