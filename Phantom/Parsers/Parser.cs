using System;
using System.Text.RegularExpressions;
using Phantom.Parsers.Composite;
using Phantom.Parsers.Terminals;
using Phantom.Scanners;

namespace Phantom.Parsers
{
	/// <summary>
	/// Superclass for all parsers.
	/// </summary>
	public abstract class Parser : Rule
	{
		/// <summary>
		/// Atom flag and object, or null if not atomic
		/// </summary>
		public Atom AtomFlag { get; set; }

		/// <summary>
		/// Returns true is this Parser is marked as atomic. False otherwise.
		/// </summary>
		public bool IsAtomic
		{
			get { return AtomFlag != null; }
		}

		/// <summary>
		/// Parser triggered event handler
		/// </summary>
		public event SemanticAction Actor;

		/// <summary>Core parsing method</summary>
		/// <param name="scan">Scanner to parse from</param>
		/// <returns>Match (success of failure) of the parser against the scanner</returns>
		public abstract ParserMatch ParseMain(IScanner scan);

		/// <summary>
		/// Public scanner method. Test scanner input for this parser's patterns.
		/// </summary>
		/// <remarks>Most parsers won't need to override this method</remarks>
		/// <param name="scan">Scanner to parse from</param>
		/// <returns>Match (success of failure) of the parser against the scanne</returns>
		public virtual ParserMatch Parse(IScanner scan)
		{
			scan.Normalise();

			/**/ //System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();
			/**/ //scan.StackStats(st.FrameCount);

			if (scan.RecursionCheck(this, scan.Offset))
				if (!(this is HoldingParser))
					return scan.NoMatch;

			ParserMatch m = ParseMain(scan);
			if (m.Success)
			{
				scan.ClearFailures();
			}
			else
			{
				//scan.AddFailure(this, scan.Offset);
			}
			return m;
		}

		/// <summary>
		/// Fires this parser's action (if any) with the given match data.
		/// </summary>
		/// <param name="m">Parser match that fired the event</param>
		public virtual SemanticActionArgs OnAction(ParserMatch m)
		{
			if (Actor != null)
			{
				var args = new SemanticActionArgs(m);
				Actor(this, args);
				return args;
			}
			return null;
		}

		#region Operators

		// There are a few changes compared to Spirit, all due to overloading
		// restrictions in C#.
		//   1) >> replaced with >			(C# needs one operand of >> to be an integer)
		//   2) * replaced with -			(C# has no pointer math, so no unary * )

		/// <summary>
		/// Specify an action for a parser.
		/// </summary>
		/// <example>
		/// Create a new paser than calls myaction() when it matches 'A':
		/// Parser MyParser = Terminals.LiteralCharater('A')[myaction];
		/// </example>
		public Parser this[SemanticAction action]
		{
			get
			{
				Actor += action; // add the handler
				return this; // return the whole parser (we're not really indexing, remember?)
			}
		}

		/// <summary>
		/// Convert a LiteralString parser into a Regular expression parser, using the given options.
		/// </summary>
		/// <param name="opts">Regular expression options for the converted parser to use.</param>
		/// <returns>Converted RegularExpression parser</returns>
		public Parser this[RegexOptions opts]
		{
			get
			{
				if (this is LiteralString)
				{
					var incoming = (LiteralString) this;
					return new RegularExpression(incoming.MatchLiteral, opts);
				}
				else if (this is RegularExpression)
				{
					return this;
				}
				else
				{
					throw new ArgumentException("Tried to convert a non-string terminal into a Regular Expression");
				}
			}
		}

		#region Magic!

		/// <summary>
		/// Convert a character into a parser
		/// </summary>
		/// <remarks>
		/// This is a lovely feature of C#. It makes a lot of redundant code disappear,
		/// and lets us get away with linguistic murder!
		/// Whoever made this part of the language, I salute you!
		/// </remarks>
		public static implicit operator Parser(char c)
		{
			return new LiteralCharacter(c);
		}

		/// <summary>
		/// Convert a string into a parser.
		/// If the string starts with '#' it will be treated as a regular expression
		/// unless it also starts with '##'. If the string starts with '#', that
		/// character will be removed.
		/// </summary>
		public static implicit operator Parser(string s)
		{
			string pattern;
			if (s.StartsWith("#"))
			{
				pattern = s.Substring(1);
				if (!pattern.StartsWith("#"))
				{
					return new RegularExpression(pattern, false);
				}
			}
			else
			{
				pattern = s;
			}
			return new LiteralString(pattern);
		}

		#endregion

		#region Sequence

		/// <summary>
		/// Create a sequential parser that matches _a_ then _b_
		/// </summary>
		public static Parser operator >(Parser a, Parser b)
		{
			if (a == null)
				throw new ArgumentNullException("Left side of sequence parser is null");
			if (b == null)
				throw new ArgumentNullException("Right side of sequence parser is null");

			return new Sequence(a, b);
		}

		/// <summary>
		/// Create a loop parser that matches a list of _a_, each being terminated by _b_
		/// The last item _a_ may be terminated, but need not be.
		/// </summary>
		public static Parser operator <(Parser a, Parser b)
		{
			if (a == null)
				throw new ArgumentNullException("Left side of list parser is null");
			if (b == null)
				throw new ArgumentNullException("Right side of list parser is null");

			return new TerminatedList(a, b);
		}

		#endregion

		#region Loop And Option

		/// <summary>
		/// Create a loop parser that matches zero or more _a_
		/// </summary>
		public static Parser operator -(Parser a)
		{
			if (a == null)
				throw new ArgumentNullException("Loop parser is null");

			return new Repetition(a, 0, uint.MaxValue);
		}

		/// <summary>
		/// Create a loop parser that matches one or more _a_
		/// </summary>
		public static Parser operator +(Parser a)
		{
			if (a == null)
				throw new ArgumentNullException("Loop parser is null");

			return new Repetition(a, 1, uint.MaxValue);
		}

		/// <summary>
		/// Create a optional parser that matches zero or one _a_
		/// </summary>
		public static Parser operator !(Parser a)
		{
			if (a == null)
				throw new ArgumentNullException("Option parser is null");

			return new Repetition(a, 0, 1);
		}

		/// <summary>
		/// Create a loop parser that matches a list of _a_, delimited by _b_
		/// </summary>
		public static Parser operator %(Parser a, Parser b)
		{
			if (a == null)
				throw new ArgumentNullException("Left side of list parser is null");
			if (b == null)
				throw new ArgumentNullException("Right side of list parser is null");

			return new DelimitedList(a, b);
		}

		#endregion

		#region Sets

		/// <summary>
		/// Create a Union/Alternative parser that matches _a_ or _b_
		/// </summary>
		public static Parser operator |(Parser a, Parser b)
		{
			if (a == null)
				throw new ArgumentNullException("Left side of union parser is null");
			if (b == null)
				throw new ArgumentNullException("Right side of union parser is null");

			return new Union(a, b);
		}

		/// <summary>
		/// Create an Intersection parser that matches both _a_ and _b_
		/// </summary>
		public static Parser operator &(Parser a, Parser b)
		{
			if (a == null)
				throw new ArgumentNullException("Left side of intersection parser is null");
			if (b == null)
				throw new ArgumentNullException("Right side of intersection parser is null");

			return new Intersection(a, b);
		}

		/// <summary>
		/// Create an Exclusive-Or parser that matches _a_ or _b_ but not both
		/// </summary>
		public static Parser operator ^(Parser a, Parser b)
		{
			if (a == null)
				throw new ArgumentNullException("Left side of Exclusive-Or parser is null");
			if (b == null)
				throw new ArgumentNullException("Right side of Exclusive-Or parser is null");

			return new Exclusive(a, b);
		}

		#endregion

		#endregion
	}
}