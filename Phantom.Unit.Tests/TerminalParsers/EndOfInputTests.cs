﻿using NUnit.Framework;
using Phantom.Parsers;
using Phantom.Parsers.Interfaces;
using Phantom.Parsers.Terminals;
using Phantom.Scanners;

namespace Phantom.Unit.Tests.TerminalParsers
{
	[TestFixture]
	public class EndOfInputTests
	{
		private IScanner scanner;
		private IMatchingParser subject;
		private const string Input = "This is my input";

		[SetUp]
		public void a_string_scanner_with_some_text ()
		{
			scanner = new ScanStrings(Input);
			subject = new EndOfInput();
		}

		[Test]
		public void fails_if_there_is_a_character_available ()
		{
			for (int i = 0; i < Input.Length; i++)
			{
				var result = subject.TryMatch(scanner);
				Assert.That(result.Success, Is.False);
			}
		}

		[Test]
		public void succeeds_at_end_of_input ()
		{
			scanner.Offset = Input.Length - 1;
			scanner.Read();

			var result = subject.TryMatch(scanner);
			Assert.That(result.Success, Is.True);
		}

		[Test]
		public void successful_result_is_zero_length ()
		{
			scanner.Offset = Input.Length - 1;
			scanner.Read();

			var result = subject.TryMatch(scanner);
			Assert.That(result.Length, Is.EqualTo(0));
		}
	}
}
