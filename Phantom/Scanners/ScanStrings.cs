using System;
using System.Collections.Generic;
using Phantom.Parsers;

namespace Phantom.Scanners
{
	public class ScanStrings : IScanner
	{
		readonly string input_string;
		List<ParserPoint> failure_points;
		int max_stack_depth;
		Dictionary<object, int> parser_points; // Parser => Offset
		string right_most_match;
		int right_most_point;
		int scanner_offset;
		bool skip_whitespace;

		/// <summary>
		/// Create a new scanner from an input string.
		/// </summary>
		/// <param name="Input">String to scan</param>
		public ScanStrings(string Input):this(Input, 0) { }

		/// <summary>
		/// Create a new scanner from an input string with an initial offset
		/// </summary>
		/// <param name="Input">String to scan</param>
		/// <param name="InitialOffset">offset from start of input</param>
		public ScanStrings(string Input, int InitialOffset)
		{
			right_most_point = 0;
			input_string = Input;

			if (scanner_offset >= input_string.Length)
				throw new ArgumentException("Initial offset beyond string end");

			max_stack_depth = 0;
			scanner_offset = InitialOffset;
			Transform = new NoTransform();
			skip_whitespace = false;
		}

		/// <summary>
		/// Gets or sets a boolean value that controls whitespace skipping.
		/// If set to true, white space will be skipped whenever Normalised() is called.
		/// </summary>
		public bool SkipWhitespace
		{
			get { return skip_whitespace; }
			set { skip_whitespace = value; }
		}

		/// <summary>
		/// Get the original input string
		/// </summary>
		public String InputString
		{
			get { return input_string; }
		}

		#region IScanner Members

		public string FurthestMatch()
		{
			return right_most_match;
		}

		public void AddFailure(object tester, int position)
		{
			if (failure_points == null) failure_points = new List<ParserPoint>();
			failure_points.Add(new ParserPoint(tester, position));
		}

		public void ClearFailures()
		{
			if (failure_points != null) failure_points.Clear();
		}

		public List<string> ListFailures()
		{
			var lst = new List<string>();

			foreach (ParserPoint p in failure_points)
			{
				string chunk = input_string.Substring(p.pos);
				if (chunk.Length > 5) chunk = chunk.Substring(0, 5);
				lst.Add(p.parser + " --> " + chunk);
			}

			return lst;
		}

		public string BadPatch(int length)
		{
			int l = length + (InputString.Length - (right_most_point + length));
			return InputString.Substring(right_most_point, l);
		}

		public int StackStats(int CurrentDepth)
		{
			if (CurrentDepth > max_stack_depth)
				max_stack_depth = CurrentDepth;

			return max_stack_depth;
		}

		public bool RecursionCheck(object accessor, int offset)
		{
			if (parser_points == null) parser_points = new Dictionary<object, int>();

			if (parser_points.ContainsKey(accessor))
				if (parser_points[accessor] == offset)
				{
					return true; /*throw new Exception("recursion loop");*/
				}

			parser_points[accessor] = offset;
			return false;
		}

		public bool EOF
		{
			get
			{
				if (input_string == null) return true;
				return scanner_offset >= input_string.Length;
			}
		}

		public bool Read()
		{
			if (EOF) return false;

			scanner_offset++;

			return !EOF;
		}

		public char Peek()
		{
			return Transform.Transform(input_string[scanner_offset]);
		}

		/// <summary>
		/// If skip whitespace is set and current position is whitespace,
		/// seek forward until on non-whitespace position or EOF.
		/// </summary>
		public void Normalise()
		{
			if (!skip_whitespace) return;
			if (EOF) return;
			while (Char.IsWhiteSpace(Peek()))
			{
				if (!Read()) break;
			}
		}

		public int Offset
		{
			get { return scanner_offset; }
			set
			{
				if (value < 0 || value > input_string.Length)
					throw new Exception("Scanner offset out of bounds");
				scanner_offset = value;
			}
		}

		public void Seek(int offset)
		{
			if (offset < 0 || offset > input_string.Length + 1)
				throw new Exception("Scanner seek offset out of bounds");

			scanner_offset = offset;
		}

		public string Substring(int offset, int length)
		{
			return  Transform.Transform(input_string.Substring(offset, Math.Min(length, input_string.Length - offset)));
		}

		public string RemainingData()
		{
			return Transform.Transform(input_string.Substring(Offset));
		}

		public ITransform Transform { get; set; }

		public ParserMatch NoMatch
		{
			get { return new ParserMatch(null, this, 0, -1); }
		}

		public ParserMatch EmptyMatch
		{
			get { return new ParserMatch(null, this, Offset, 0); }
		}

		public ParserMatch CreateMatch(Parser source, int offset, int length)
		{
			if ((offset + length) > right_most_point)
			{
				right_most_point = offset + length;
				right_most_match = InputString.Substring(offset, length);
			}
			return new ParserMatch(source, this, offset, length);
		}

		#endregion
	}
}