// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mono.Linker.Tests
{
    [TestClass]
    public class ParseResponseFileLinesTests
    {
        [TestMethod]
        public void TestOneArg()
        {
            TestParseResponseFileLines(@"abc", new string[] { @"abc" });
        }

        [TestMethod]
        public void TestTwoArgsOnOneLine()
        {
            TestParseResponseFileLines(@"abc def", new string[] { @"abc", @"def" });
        }

        [TestMethod]
        public void TestTwoArgsOnTwoLine()
        {
            TestParseResponseFileLines(@"abc
def", new string[] { @"abc", @"def" });
        }

        [TestMethod]
        public void TestOneSlashWithoutQuote()
        {
            TestParseResponseFileLines(@"\", new string[] { @"\" });
        }

        [TestMethod]
        public void TestTwoSlashesWithoutQuote()
        {
            TestParseResponseFileLines(@"\\", new string[] { @"\\" });
        }

        [TestMethod]
        public void TestOneSlashWithQuote()
        {
            TestParseResponseFileLines(@"""x \"" y""", new string[] { @"x "" y" });
        }

        [TestMethod]
        public void TestTwoSlashesWithQuote()
        {
            TestParseResponseFileLines(@"""Slashes \\ In Quote""", new string[] { @"Slashes \\ In Quote" });
        }

        [TestMethod]
        public void TestTwoSlashesAtEndOfQuote()
        {
            TestParseResponseFileLines(@"""Trailing Slash\\""", new string[] { @"Trailing Slash\" });
        }

        [TestMethod]
        public void TestWindowsPath()
        {
            TestParseResponseFileLines(@"C:\temp\test.txt", new string[] { @"C:\temp\test.txt" });
        }

        [TestMethod]
        public void TestLinuxPath()
        {
            TestParseResponseFileLines(@"/tmp/test.txt", new string[] { @"/tmp/test.txt" });
        }

        [TestMethod]
        public void TestEqualsArguments()
        {
            TestParseResponseFileLines(@"a=b", new string[] { @"a=b" });
        }

        [TestMethod]
        public void TestEqualsArgumentsSpaces()
        {
            TestParseResponseFileLines(@"a=""b c""", new string[] { @"a=b c" });
        }

        [TestMethod]
        public void TestEqualsKeySpaces()
        {
            TestParseResponseFileLines(@"""a b""=c", new string[] { @"a b=c" });
        }

        [TestMethod]
        public void TestEscapedQuoteWithBackslash()
        {
            TestParseResponseFileLines(@"""a \"" b""", new string[] { @"a "" b" });
        }

        [TestMethod]
        public void TestEscapedQuoteSequence()
        {
            TestParseResponseFileLines(@"""a """" b""", new string[] { @"a "" b" });
        }

        [TestMethod]
        public void TestQuotedNewline()
        {
            TestParseResponseFileLines(@"""a
b""", new string[] { @"a
b" });
        }

        private static void TestParseResponseFileLines(string v1, string[] v2)
        {
            var result = new Queue<string>();
            using (var reader = new StringReader(v1))
                Driver.ParseResponseFile(reader, result);
            CollectionAssert.AreEquivalent(result.ToArray(), v2);
        }
    }
}
