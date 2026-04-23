// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Mono.Linker.Tests
{
    public class ParseResponseFileLinesTests
    {
        [Fact]
        public void TestOneArg()
        {
            TestParseResponseFileLines(@"abc", new string[] { @"abc" });
        }

        [Fact]
        public void TestTwoArgsOnOneLine()
        {
            TestParseResponseFileLines(@"abc def", new string[] { @"abc", @"def" });
        }

        [Fact]
        public void TestTwoArgsOnTwoLine()
        {
            TestParseResponseFileLines(@"abc
def", new string[] { @"abc", @"def" });
        }

        [Fact]
        public void TestOneSlashWithoutQuote()
        {
            TestParseResponseFileLines(@"\", new string[] { @"\" });
        }

        [Fact]
        public void TestTwoSlashesWithoutQuote()
        {
            TestParseResponseFileLines(@"\\", new string[] { @"\\" });
        }

        [Fact]
        public void TestOneSlashWithQuote()
        {
            TestParseResponseFileLines(@"""x \"" y""", new string[] { @"x "" y" });
        }

        [Fact]
        public void TestTwoSlashesWithQuote()
        {
            TestParseResponseFileLines(@"""Slashes \\ In Quote""", new string[] { @"Slashes \\ In Quote" });
        }

        [Fact]
        public void TestTwoSlashesAtEndOfQuote()
        {
            TestParseResponseFileLines(@"""Trailing Slash\\""", new string[] { @"Trailing Slash\" });
        }

        [Fact]
        public void TestWindowsPath()
        {
            TestParseResponseFileLines(@"C:\temp\test.txt", new string[] { @"C:\temp\test.txt" });
        }

        [Fact]
        public void TestLinuxPath()
        {
            TestParseResponseFileLines(@"/tmp/test.txt", new string[] { @"/tmp/test.txt" });
        }

        [Fact]
        public void TestEqualsArguments()
        {
            TestParseResponseFileLines(@"a=b", new string[] { @"a=b" });
        }

        [Fact]
        public void TestEqualsArgumentsSpaces()
        {
            TestParseResponseFileLines(@"a=""b c""", new string[] { @"a=b c" });
        }

        [Fact]
        public void TestEqualsKeySpaces()
        {
            TestParseResponseFileLines(@"""a b""=c", new string[] { @"a b=c" });
        }

        [Fact]
        public void TestEscapedQuoteWithBackslash()
        {
            TestParseResponseFileLines(@"""a \"" b""", new string[] { @"a "" b" });
        }

        [Fact]
        public void TestEscapedQuoteSequence()
        {
            TestParseResponseFileLines(@"""a """" b""", new string[] { @"a "" b" });
        }

        [Fact]
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
            Assert.Equal(v2.OrderBy(x => x), result.ToArray().OrderBy(x => x));
        }
    }
}
