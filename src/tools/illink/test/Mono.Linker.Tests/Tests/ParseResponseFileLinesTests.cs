using System.Collections.Generic;
using NUnit.Framework;

namespace Mono.Linker.Tests
{
	[TestFixture]
	public class ParseResponseFileLinesTests
	{
		[Test]
		public void TestOneArg ()
		{
			TestParseResponseFileLines (@"abc", new string[] { @"abc" });
		}

		[Test]
		public void TestTwoArgsOnOneLine ()
		{
			TestParseResponseFileLines (@"abc def", new string[] { @"abc", @"def" });
		}

		[Test]
		public void TestTwoArgsOnTwoLine ()
		{
			TestParseResponseFileLines (@"abc
def", new string[] { @"abc", @"def" });
		}

		[Test]
		public void TestOneSlashWithoutQuote ()
		{
			TestParseResponseFileLines (@"\", new string[] { @"\" });
		}

		[Test]
		public void TestTwoSlashesWithoutQuote ()
		{
			TestParseResponseFileLines (@"\\", new string[] { @"\\" });
		}

		[Test]
		public void TestOneSlashWithQuote ()
		{
			TestParseResponseFileLines (@"""x \"" y""", new string[] { @"x "" y" });
		}

		[Test]
		public void TestTwoSlashesWithQuote ()
		{
			TestParseResponseFileLines (@"""Trailing Slash\\""", new string[] { @"Trailing Slash\" });
		}

		[Test]
		public void TestWindowsPath ()
		{
			TestParseResponseFileLines (@"C:\temp\test.txt", new string[] { @"C:\temp\test.txt" });
		}

		[Test]
		public void TestLinuxPath ()
		{
			TestParseResponseFileLines (@"/tmp/test.txt", new string[] { @"/tmp/test.txt" });
		}

		[Test]
		public void TestEqualsArguments ()
		{
			TestParseResponseFileLines (@"a=b", new string[] { @"a=b" });
		}

		[Test]
		public void TestEqualsArgumentsSpaces ()
		{
			TestParseResponseFileLines (@"a=""b c""", new string[] { @"a=b c" });
		}

		[Test]
		public void TestEqualsKeySpaces ()
		{
			TestParseResponseFileLines (@"""a b""=c", new string[] { @"a b=c" });
		}

		private void TestParseResponseFileLines (string v1, string[] v2)
		{
			var result = new Queue<string> ();
			Driver.ParseResponseFileLines (v1.Split ('\n'), result);
			Assert.That (result, Is.EquivalentTo (v2));
		}
	}
}