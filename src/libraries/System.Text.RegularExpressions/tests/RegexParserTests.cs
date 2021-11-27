// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;
using Xunit.Sdk;

namespace System.Text.RegularExpressions.Tests
{
    public partial class RegexParserTests
    {
        [Theory]
        [InlineData("?", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid `?` operator, nothing to make optional"
        [InlineData("?|?", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("?abc", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("(?Pabc", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid group name. Missing `<`\n" &     "(?Pabc\n" &     "^"
        [InlineData("(?u-q)", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid group flag, found -q but " &     "expected one of: -i, -m, -s, -U or -u"
        [InlineData("(?uq)", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid group flag, found q but " &     "expected one of: i, m, s, U or u"
        [InlineData("(+)", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid `+` operator, nothing to repeat"
        [InlineData("(a)b)", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid capturing group. " &     "Found too many closing symbols"
        [InlineData("(b(a)", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid capturing group. " &     "Found too many opening symbols"
        [InlineData("[-", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("[-a", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("[[:abc:]]", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid ascii set. `abc` is not a valid name\n" &     "[[:abc:]]\n" &     " ^"
        [InlineData("[[:alnum:", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid ascii set. Expected [:name:]\n" &     "[[:alnum:\n" &     " ^"
        [InlineData("[[:alnum]]", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid ascii set. Expected [:name:]\n" &     "[[:alnum]]\n" &     " ^"
        [InlineData("[]", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("[]a", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("[]abc", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid set. Missing `]`\n" &     "[]abc\n" &     "^"
        [InlineData("[\\", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("[^]", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("[^]", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid set. Missing `]`\n" &     "[^]\n" &     "^"
        [InlineData("[a-", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("[a-\w]", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid set range. Range can't contain " &     "a character-class or assertion\n" &     "[a-\\w]\n" &     "   ^"
        [InlineData("[a", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("[abc", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid set. Missing `]`\n" &     "[abc\n" &     "^"
        [InlineData("[d-c]", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid set range. " &     "Start must be lesser than end\n" &     "[d-c]\n" &     "   ^"
        [InlineData("[z-[:alnum:]]", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid set range. " &     "Start must be lesser than end\n" &     "[z-[:alnum:]]\n" &     "   ^"
        [InlineData("{10}", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid repeition range, " &     "nothing to repeat"
        [InlineData("*", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid `*` operator, nothing to repeat"
        [InlineData("*abc", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("\12", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid octal literal. Expected 3 octal digits, but found 2\n" &     "\\12\n" &     "^"
        [InlineData("\12@", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid octal literal. Expected octal digit, but found @\n" &     "\\12@\n" &     "^"
        [InlineData("\p{11", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid unicode name. Expected `}`\n" &     "\\p{11\n" &     "^"
        [InlineData("\p{11}", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid unicode name. Expected chars in {'a'..'z', 'A'..'Z'}\n" &     "\\p{11}\n" &     "^"
        [InlineData("\p{Bb}", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid unicode name. Found Bb\n" &     "\\p{Bb}\n" &     "^"
        [InlineData("\p11", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid unicode name. Found 1\n" &     "\\p11\n" &     "^"
        [InlineData("\pB", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid unicode name. Found B\n" &     "\\pB\n" &     "^"
        [InlineData("\u123", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid unicode literal. Expected 4 hex digits, but found 3\n" &     "\\u123\n" &     "^"
        [InlineData("\U123", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid unicode literal. Expected 8 hex digits, but found 3\n" &     "\\U123\n" &     "^"
        [InlineData("\U123@a", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid unicode literal. Expected hex digit, but found @\n" &     "\\U123@a\n" &     "^"
        [InlineData("\u123@abc", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid unicode literal. Expected hex digit, but found @\n" &     "\\u123@abc\n" &     "^"
        [InlineData("\UFFFFFFFF", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid unicode literal. FFFFFFFF value is too big\n" &     "\\UFFFFFFFF\n" &     "^"
        [InlineData("\x{00000000A}", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid unicode literal. Expected at most 8 chars, found 9\n" &     "\\x{00000000A}\n" &     "^"
        [InlineData("\x{2f894", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid unicode literal. Expected `}`\n" &     "\\x{2f894\n" &     "^"
        [InlineData("\x{61@}", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid unicode literal. Expected hex digit, but found @\n" &     "\\x{61@}\n" &     " ^"
        [InlineData("\x{FFFFFFFF}", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("+", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("+abc", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a???", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a??*", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a??+", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a?*", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a?+", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a(?P<>abc)", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid group name. Name can't be empty\n" &     "a(?P<>abc)\n" &     " ^"
        [InlineData("a(?P<asd)", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid group name. Expected char in " &     "{'a'..'z', 'A'..'Z', '0'..'9', '-', '_'}, " &     "but found `)`\n" &     "a(?P<asd)\n" &     " ^"
        [InlineData("a{,}", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a{,1}", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a{0,101}", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid repetition range. Expected 100 repetitions " &     "or less, but found: 101\n" &     "a{0,101}\n" &     " ^"
        [InlineData("a{0,a}", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a{0,bad}", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid repetition range. Range can only contain digits\n" &     "a{0,bad}\n" &     " ^"
        [InlineData("a{1,,,2}", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a{1,,}", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a{1,,2}", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a{1,", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a{1,}??", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a{1,}*", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a{1,}+", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a{1,x}", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a{1", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a{1}??", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a{1}*", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a{1}+", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a{1111111111}", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid repetition range. Max value is 32767\n" &     "a{1111111111}\n" &     " ^"
        [InlineData("a{1x}", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a*??", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a*{,}", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a*{0}", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a*{1}", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a**", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a*****", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a*+", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a+??", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a+*", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a++", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a|?", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a|?b", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a|*", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a|*b", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a|+", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("a|+b", RegexOptions.None, (RegexParseError)9999, -1)]
        [InlineData("aaa(?Pabc", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid group name. Missing `<`\n" &     "aaa(?Pabc\n" &     "   ^"
        [InlineData("abc(?P<abc", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid group name. Missing `>`\n" &     "abc(?P<abc\n" &     "   ^"
        [InlineData("abc(?Pabc)", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid group name. Missing `<`\n" &     "abc(?Pabc)\n" &     "   ^"
        [InlineData("abc(?q)", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid group. Unknown group type\n" &     "abc(?q)\n" &     "   ^"
        [InlineData("abc[]", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid set. Missing `]`\n" &     "abc[]\n" &     "   ^"
        [InlineData("abc\A{10}", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid repetition range, either " &     "char, shorthand (i.e: \\w), group, or set " &     "expected before repetition range"
        [InlineData("弢(?Pabc", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid group name. Missing `<`\n" &     "~1 chars~(?Pabc\n" &     "         ^"
        [InlineData("弢aaa(?Pabc", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid group name. Missing `<`\n" &     "~4 chars~(?Pabc\n" &     "         ^"
        [InlineData("弢弢弢(?Pabc", RegexOptions.None, (RegexParseError)9999, -1)] //      "Invalid group name. Missing `<`\n" &     "~3 chars~(?Pabc\n" &     "         ^"
        [InlineData(@"(?P<abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_>abc", RegexOptions.None, null)
        [InlineData(@"(\b)", RegexOptions.None, null)]
        [InlineData(@"[a-\b]", RegexOptions.None, null)]
        [InlineData(@"\b?", RegexOptions.None, null)]
        [InlineData(@"\b*", RegexOptions.None, null)]
        [InlineData(@"\b+", RegexOptions.None, null)]
        [InlineData(@"\x{7fffffff}", RegexOptions.None, null)]
        [InlineData(@"a{1,}?", RegexOptions.None, null)]
        [InlineData(@"a{1,101}", RegexOptions.None, null)]
        [InlineData(@"a{1}?", RegexOptions.None, null)]

        public void ParseCheckOffset(string pattern, RegexOptions options, RegexParseError? error, int offset = -1)
        {
            Parse(pattern, options, error, offset);
        }

        [Fact]
        public void ParseCheckOffsetInsufficientParens()
        {
            Parse(new string('(', 1183), RegexOptions.None, RegexParseError.InsufficientClosingParentheses, 1183);
        }

        private static void Parse(string pattern, RegexOptions options, RegexParseError? error, int offset = -1)
        {
            if (error != null)
            {
                //Assert.InRange(offset, 0, int.MaxValue);
                Throws(pattern, options, error.Value, offset, () => new Regex(pattern, options));
                return;
            }

            Assert.Equal(-1, offset);
            LogActual(pattern, options, RegexParseError.Unknown, -1);

            // Nothing to assert here without having access to internals.
            new Regex(pattern, options); // Does not throw

            ParsePatternFragments(pattern, options);
        }

        private static void LogActual(string pattern, RegexOptions options, RegexParseError error, int offset)
        {
            // [InlineData(@"[a-z-[b", RegexOptions.None, RegexParseError.UnterminatedBracket, 7)]                
            string s = (error == RegexParseError.Unknown) ?
                @$"        [InlineData(@""{pattern}"", RegexOptions.{options.ToString()}, null)]" :
                @$"        [InlineData(@""{pattern}"", RegexOptions.{options.ToString()}, RegexParseError.{error.ToString()}, {offset})]";

            File.AppendAllText(@"/tmp/out.cs", s + "\n"); // for updating baseline
        }

        private static void ParsePatternFragments(string pattern, RegexOptions options)
        {
            // Trim the input in various places and parse.
            // Verify that if it throws, it's the correct exception type
            for (int i = pattern.Length - 1; i > 0; i--)
            {
                string str = pattern.Substring(0, i);
                MayThrow(() => new Regex(str, options));
            }

            for (int i = 1; i < pattern.Length; i++)
            {
                string str = pattern.Substring(i);
                MayThrow(() => new Regex(str, options));
            }

            for (int i = 1; i < pattern.Length; i++)
            {
                string str = pattern.Substring(0, i) + pattern.Substring(i + 1);
                MayThrow(() => new Regex(str, options));
            }
        }

        /// <summary>
        /// Checks that action throws either a RegexParseException or an ArgumentException depending on the
        /// environment and the supplied error.
        /// </summary>
        /// <param name="error">The expected parse error</param>
        /// <param name="action">The action to invoke.</param>
        static partial void Throws(string pattern, RegexOptions options, RegexParseError error, int offset, Action action);

        /// <summary>
        /// Checks that action succeeds or throws either a RegexParseException or an ArgumentException depending on the
        // environment and the action.
        /// </summary>
        /// <param name="action">The action to invoke.</param>
        static partial void MayThrow(Action action);
    }
}
