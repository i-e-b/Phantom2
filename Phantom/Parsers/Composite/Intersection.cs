using Phantom.Scanners;

namespace Phantom.Parsers.Composite
{
	/// <summary>
	/// Create an Intersection parser from two sub-parsers.
	/// </summary>
	class Intersection : Binary, ICompositeParser
	{
		public Intersection(Parser first, Parser second)
			: base(first, second)
		{
		}

		public override ParserMatch ParseMain(IScanner scan)
		{
			int offset = scan.Offset;
			//ParserMatch m = scan.NoMatch;

			ParserMatch left = bLeftParser.Parse(scan);


			if (left.Success)
			{
				ParserMatch right = bRightParser.Parse(scan);
				if (right.Success)
				{
					//m.Concat(m2);
					return ParserMatch.Concat(this, left, right);
				}
			}
			else
			{
				scan.Seek(offset);
				left = bRightParser.Parse(scan);
				if (left.Success)
				{
					ParserMatch right = bLeftParser.Parse(scan);
					if (right.Success)
					{
						//m.Concat(m2);
						return ParserMatch.Concat(this, left, right);
					}
				}
			}

			scan.Seek(offset);
			return scan.NoMatch;
		}

		public override string ToString()
		{
			return LeftParser + "&" + RightParser;
		}
	}
}