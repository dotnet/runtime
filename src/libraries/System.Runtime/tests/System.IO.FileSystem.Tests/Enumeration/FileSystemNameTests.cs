// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Enumeration;
using Xunit;

namespace System.IO.Tests
{
    public class FileSystemNameTests
    {
        [Theory,
            MemberData(nameof(SimpleMatchData)),
            MemberData(nameof(EscapedSimpleMatchData)),
            MemberData(nameof(Win32MatchData)),
            MemberData(nameof(EscapedWin32MatchData))]
        public static void Win32Match(string expression, string name, bool ignoreCase, bool expected)
        {
            Assert.Equal(expected, FileSystemName.MatchesWin32Expression(expression, name.AsSpan(), ignoreCase));
        }

        [Theory,
            MemberData(nameof(SimpleMatchData)),
            MemberData(nameof(EscapedSimpleMatchData))]
        public static void SimpleMatch(string expression, string name, bool ignoreCase, bool expected)
        {
            Assert.Equal(expected, FileSystemName.MatchesSimpleExpression(expression, name.AsSpan(), ignoreCase));
        }

        public static TheoryData<string, string, bool, bool> EscapedSimpleMatchData => new TheoryData<string, string, bool, bool>
        {
            // Trailing escape matches as it is considered "invisible"
            { "\\", "\\", false, true },
            { "\\", "\\", true, true },
            { "\\\\", "\\", false, true },
            { "\\\\", "\\", true, true },

            { "\\*", "a", false, false },
            { "\\*", "a", true, false },
            { "\\*", "*", false, true },
            { "\\*", "*", true, true },
            { "*\\*", "***", false, true },
            { "*\\*", "***", true, true },
            { "*\\*", "ABC*", false, true },
            { "*\\*", "ABC*", true, true },
            { "*\\*", "***A", false, false },
            { "*\\*", "***A", true, false },
            { "*\\*", "ABC*A", false, false },
            { "*\\*", "ABC*A", true, false },
        };

        public static TheoryData<string, string, bool, bool> EscapedWin32MatchData => new TheoryData<string, string, bool, bool>
        {
            { "\\\"", "a", false, false },
            { "\\\"", "a", true, false },
            { "\\\"", "\"", false, true },
            { "\\\"", "\"", true, true },
        };

        public static TheoryData<string, string, bool, bool> SimpleMatchData => new TheoryData<string, string, bool, bool>
        {
            { null, "", false, false },
            { null, "", true, false },
            { "*", "", false, false },
            { "*", "", true, false },
            { "*", "ab", false, true },
            { "*", "AB", true, true },
            { "*foo", "foo", false, true },
            { "*foo", "foo", true, true },
            { "*foo", "FOO", false, false },
            { "*foo", "FOO", true, true },
            { "*foo", "nofoo", true, true },
            { "*foo", "NoFOO", true, true },
            { "*foo", "noFOO", false, false },
            { @"*", @"foo.txt", true, true },
            { @".", @"foo.txt", true, false },
            { @".", @"footxt", true, false },
            { @"*.*", @"foo.txt", true, true },
            { @"*.*", @"foo.", true, true },
            { @"*.*", @".foo", true, true },
            { @"*.*", @"footxt", true, false },
        };

        public static TheoryData<string, string, bool, bool> Win32MatchData => new TheoryData<string, string, bool, bool>
        {
            { "<\"*", @"footxt", true, true },              // DOS equivalent of *.*
            { "<\"*", @"foo.txt", true, true },             // DOS equivalent of *.*
            { "<\"*", @".foo", true, true },                // DOS equivalent of *.*
            { "<\"*", @"foo.", true, true },                // DOS equivalent of *.*
            { ">\">", @"a.b", true, true },                 // DOS equivalent of ?.?
            { ">\">", @"a.", true, true },                  // DOS equivalent of ?.?
            { ">\">", @"a", true, true },                   // DOS equivalent of ?.?
            { ">\">", @"ab", true, false },                 // DOS equivalent of ?.?
            { ">\">", @"a.bc", true, false },               // DOS equivalent of ?.?
            { ">\">", @"ab.c", true, false },               // DOS equivalent of ?.?
            { ">>\">>", @"a.b", true, true },               // DOS equivalent of ??.??
            { ">>\"\">>", @"a.b", true, false },            // Not possible to do from DOS ??""??
            { ">>\">>", @"a.bc", true, true },              // DOS equivalent of ??.??
            { ">>\">>", @"ab.ba", true, true },             // DOS equivalent of ??.??
            { ">>\">>", @"ab.", true, true },               // DOS equivalent of ??.??
            { ">>\"\"\">>", @"ab.", true, true },           // Not possible to do from DOS ??"""??
            { ">>b\">>", @"ab.ba", true, false },           // DOS equivalent of ??b.??
            { "a>>\">>", @"ab.ba", true, true },            // DOS equivalent of a??.??
            { ">>\">>a", @"ab.ba", true, false },           // DOS equivalent of ??.??a
            { ">>\"b>>", @"ab.ba", true, true },            // DOS equivalent of ??.b??
            { ">>\"b>>", @"ab.b", true, true },             // DOS equivalent of ??.b??
            { ">>b.>>", @"ab.ba", true, false },
            { "a>>.>>", @"ab.ba", true, true },
            { ">>.>>a", @"ab.ba", true, false },
            { ">>.b>>", @"ab.ba", true, true },
            { ">>.b>>", @"ab.b", true, true },
            { ">>\">>\">>", @"ab.ba", true, true },         // DOS equivalent of ??.??.?? (The last " is an optional period)
            { ">>\">>\">>", @"abba", true, false },         // DOS equivalent of ??.??.?? (The first " isn't, so this doesn't match)
            { ">>\"ab\"ba", @"ab.ba", true, false },        // DOS equivalent of ??.ab.ba
            { "ab\"ba\">>", @"ab.ba", true, true },         // DOS equivalent of ab.ba.??
            { "ab\">>\"ba", @"ab.ba", true, false },        // DOS equivalent of ab.??.ba
            { ">>\">>\">>>", @"ab.ba.cab", true, true },    // DOS equivalent of ??.??.???
            { "a>>\"b>>\"c>>>", @"ab.ba.cab", true, true }, // DOS equivalent of a??.b??.c???
            { @"<", @"a", true, true },                     // DOS equivalent of *.
            { @"<", @"a.", true, true },                    // DOS equivalent of *.
            { @"<", @"a. ", true, false },                  // DOS equivalent of *.
            { @"<", @"a.b", true, false },                  // DOS equivalent of *.
            { @"foo<", @"foo.", true, true },               // DOS equivalent of foo*.
            { @"foo<", @"foo. ", true, false },             // DOS equivalent of foo*.
            { @"<<", @"a.b", true, true },
            { @"<<", @"a.b.c", true, true },
            { "<\"", @"a.b.c", true, false },
            { @"<.", @"a", true, false },
            { @"<.", @"a.", true, true },
            { @"<.", @"a.b", true, false },
        };

        [Theory,
            InlineData("", "*"),
            InlineData("*.*", "*"),
            InlineData("*", "*"),
            InlineData(".", "."),
            InlineData("?", ">"),
            InlineData("*.", "<"),
            InlineData("?.?", ">\">"),
            InlineData("foo*.", "foo<")]
        public void TranslateExpression(string expression, string expected)
        {
            Assert.Equal(expected, FileSystemName.TranslateWin32Expression(expression));
        }

        [Fact]
        public void TranslateVeryLongExpression()
        {
            string longString = new string('a', 10_000_000);
            Assert.Equal(longString, FileSystemName.TranslateWin32Expression(longString));
        }
    }
}
