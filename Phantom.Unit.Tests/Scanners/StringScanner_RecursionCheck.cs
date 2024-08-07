﻿using NUnit.Framework;
using Phantom.Scanners;

namespace Phantom.Unit.Tests.Scanners
{
	[TestFixture]
	public class StringScanner_RecursionCheck
	{
		private IScanner subject;
		private const string Input = "This is my input";

		[SetUp]
		public void a_string_scanner_with_some_text ()
		{
			subject = new ScanStrings(Input);
		}

		[Test]
		public void recursion_check_returns_false_if_a_given_object_changes_offset()
		{
			var key = new object();

			for (int i = 0; i < 10; i++)
			{
				var result = subject.RecursionCheck(key, i);
				Assert.That(result, Is.False);
			}
		}

		[Test]
		public void recursion_check_returns_true_if_a_given_object_repeats_an_offset()
		{
			var key = new object();

			subject.RecursionCheck(key, 3);
			var result = subject.RecursionCheck(key, 3);
			Assert.That(result, Is.True);
		}

		[Test]
		public void recursion_check_treats_each_key_separately ()
		{
			var k1 = new object();
			var k2 = new object();

			Assert.That(subject.RecursionCheck(k1, 0), Is.False);
			Assert.That(subject.RecursionCheck(k2, 0), Is.False);
			Assert.That(subject.RecursionCheck(k1, 1), Is.False);
			Assert.That(subject.RecursionCheck(k2, 0), Is.True);
		}

		[Test]
		public void recursion_can_continue_after_returning_true()
		{
			var key = new object();

			for (int i = 0; i < 10; i++)
			{
				Assert.That(subject.RecursionCheck(key, i), Is.False);
				Assert.That(subject.RecursionCheck(key, i), Is.True);
			}
		}
	}
}
