// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexReplaceTests
    {
        public static IEnumerable<object[]> Replace_String_TestData()
        {
            yield return new object[] { @"[^ ]+\s(?<time>)", "08/10/99 16:00", "${time}", RegexOptions.None, 14, 0, "16:00" };
            yield return new object[] { "icrosoft", "MiCrOsOfT", "icrosoft", RegexOptions.IgnoreCase, 9, 0, "Microsoft" };
            yield return new object[] { "dog", "my dog has fleas", "CAT", RegexOptions.IgnoreCase, 16, 0, "my CAT has fleas" };
            yield return new object[] { @"D\.(.+)", "D.Bau", "David $1", RegexOptions.None, 5, 0, "David Bau" };
            yield return new object[] { "a", "aaaaa", "b", RegexOptions.None, 2, 0, "bbaaa" };
            yield return new object[] { "a", "aaaaa", "b", RegexOptions.None, 2, 3, "aaabb" };

            // Replace with group numbers
            yield return new object[] { "([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z])))))))))))))))", "abcdefghiklmnop", "$15", RegexOptions.None, 15, 0, "p" };
            yield return new object[] { "([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z])))))))))))))))", "abcdefghiklmnop", "$3", RegexOptions.None, 15, 0, "cdefghiklmnop" };
            
            // Stress
            string pattern = string.Empty;
            for (int i = 0; i < 1000; i++)
                pattern += "([a-z]";
            for (int i = 0; i < 1000; i++)
                pattern += ")";
            string input = string.Empty;
            for (int i = 0; i < 200; i++)
                input += "abcde";
            yield return new object[] { pattern, input, "$1000", RegexOptions.None, input.Length, 0, "e" };

            string expected = string.Empty;
            for (int i = 0; i < 200; i++)
                expected += "abcde";
            yield return new object[] { pattern, input, "$1", RegexOptions.None, input.Length, 0, expected };

            // Undefined group
            yield return new object[] { "([a_z])(.+)", "abc", "$3", RegexOptions.None, 3, 0, "$3" };

            // Valid cases
            yield return new object[] { @"(?<cat>cat)\s*(?<dog>dog)", "cat dog", "${cat}est ${dog}est", RegexOptions.None, 7, 0, "catest dogest" };
            yield return new object[] { @"(?<cat>cat)\s*(?<dog>dog)", "slkfjsdcat dogkljeah", "START${cat}dogcat${dog}END", RegexOptions.None, 20, 0, "slkfjsdSTARTcatdogcatdogENDkljeah" };
            yield return new object[] { @"(?<512>cat)\s*(?<256>dog)", "slkfjsdcat dogkljeah", "START${512}dogcat${256}END", RegexOptions.None, 20, 0, "slkfjsdSTARTcatdogcatdogENDkljeah" };
            yield return new object[] { @"(?<256>cat)\s*(?<512>dog)", "slkfjsdcat dogkljeah", "START${256}dogcat${512}END", RegexOptions.None, 20, 0, "slkfjsdSTARTcatdogcatdogENDkljeah" };
            yield return new object[] { @"(?<512>cat)\s*(?<256>dog)", "slkfjsdcat dogkljeah", "STARTcat$256$512dogEND", RegexOptions.None, 20, 0, "slkfjsdSTARTcatdogcatdogENDkljeah" };
            yield return new object[] { @"(?<256>cat)\s*(?<512>dog)", "slkfjsdcat dogkljeah", "STARTcat$512$256dogEND", RegexOptions.None, 20, 0, "slkfjsdSTARTcatdogcatdogENDkljeah" };

            yield return new object[] { @"(hello)cat\s+dog(world)", "hellocat dogworld", "$1$$$2", RegexOptions.None, 19, 0, "hello$world" };
            yield return new object[] { @"(hello)\s+(world)", "What the hello world goodby", "$&, how are you?", RegexOptions.None, 27, 0, "What the hello world, how are you? goodby" };
            yield return new object[] { @"(hello)\s+(world)", "What the hello world goodby", "$`cookie are you doing", RegexOptions.None, 27, 0, "What the What the cookie are you doing goodby" };
            yield return new object[] { @"(cat)\s+(dog)", "before textcat dogafter text", ". This is the $' and ", RegexOptions.None, 28, 0, "before text. This is the after text and after text" };
            yield return new object[] { @"(cat)\s+(dog)", "before textcat dogafter text", ". The following should be dog and it is $+. ", RegexOptions.None, 28, 0, "before text. The following should be dog and it is dog. after text" };
            yield return new object[] { @"(cat)\s+(dog)", "before textcat dogafter text", ". The following should be the entire string '$_'. ", RegexOptions.None, 28, 0, "before text. The following should be the entire string 'before textcat dogafter text'. after text" };

            yield return new object[] { @"(hello)\s+(world)", "START hello    world END", "$2 $1 $1 $2 $3$4", RegexOptions.None, 24, 0, "START world hello hello world $3$4 END" };
            yield return new object[] { @"(hello)\s+(world)", "START hello    world END", "$2 $1 $1 $2 $123$234", RegexOptions.None, 24, 0, "START world hello hello world $123$234 END" };

            yield return new object[] { @"(d)(o)(g)(\s)(c)(a)(t)(\s)(h)(a)(s)", "My dog cat has fleas.", "$01$02$03$04$05$06$07$08$09$10$11", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Multiline, 21, 0, "My dog cat has fleas." };
            yield return new object[] { @"(d)(o)(g)(\s)(c)(a)(t)(\s)(h)(a)(s)", "My dog cat has fleas.", "$05$06$07$04$01$02$03$08$09$10$11", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Multiline, 21, 0, "My cat dog has fleas." };

            // ECMAScript
            yield return new object[] { @"(?<512>cat)\s*(?<256>dog)", "slkfjsdcat dogkljeah", "STARTcat${256}${512}dogEND", RegexOptions.ECMAScript, 20, 0, "slkfjsdSTARTcatdogcatdogENDkljeah" };
            yield return new object[] { @"(?<256>cat)\s*(?<512>dog)", "slkfjsdcat dogkljeah", "STARTcat${512}${256}dogEND", RegexOptions.ECMAScript, 20, 0, "slkfjsdSTARTcatdogcatdogENDkljeah" };
            yield return new object[] { @"(?<1>cat)\s*(?<2>dog)", "slkfjsdcat dogkljeah", "STARTcat$2$1dogEND", RegexOptions.ECMAScript, 20, 0, "slkfjsdSTARTcatdogcatdogENDkljeah" };
            yield return new object[] { @"(?<2>cat)\s*(?<1>dog)", "slkfjsdcat dogkljeah", "STARTcat$1$2dogEND", RegexOptions.ECMAScript, 20, 0, "slkfjsdSTARTcatdogcatdogENDkljeah" };
            yield return new object[] { @"(?<512>cat)\s*(?<256>dog)", "slkfjsdcat dogkljeah", "STARTcat$256$512dogEND", RegexOptions.ECMAScript, 20, 0, "slkfjsdSTARTcatdogcatdogENDkljeah" };
            yield return new object[] { @"(?<256>cat)\s*(?<512>dog)", "slkfjsdcat dogkljeah", "START${256}dogcat${512}END", RegexOptions.ECMAScript, 20, 0, "slkfjsdSTARTcatdogcatdogENDkljeah" };

            yield return new object[] { @"(hello)\s+world", "START hello    world END", "$234 $1 $1 $234 $3$4", RegexOptions.ECMAScript, 24, 0, "START $234 hello hello $234 $3$4 END" };
            yield return new object[] { @"(hello)\s+(world)", "START hello    world END", "$2 $1 $1 $2 $3$4", RegexOptions.ECMAScript, 24, 0, "START world hello hello world $3$4 END" };
            yield return new object[] { @"(hello)\s+(world)", "START hello    world END", "$2 $1 $1 $2 $123$234", RegexOptions.ECMAScript, 24, 0, "START world hello hello world hello23world34 END" };
            yield return new object[] { @"(?<12>hello)\s+(world)", "START hello    world END", "$1 $12 $12 $1 $123$134", RegexOptions.ECMAScript, 24, 0, "START world hello hello world hello3world34 END" };
            yield return new object[] { @"(?<123>hello)\s+(?<23>world)", "START hello    world END", "$23 $123 $123 $23 $123$234", RegexOptions.ECMAScript, 24, 0, "START world hello hello world helloworld4 END" };
            yield return new object[] { @"(?<123>hello)\s+(?<234>world)", "START hello    world END", "$234 $123 $123 $234 $123456$234567", RegexOptions.ECMAScript, 24, 0, "START world hello hello world hello456world567 END" };

            yield return new object[] { @"(d)(o)(g)(\s)(c)(a)(t)(\s)(h)(a)(s)", "My dog cat has fleas.", "$01$02$03$04$05$06$07$08$09$10$11", RegexOptions.CultureInvariant | RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline, 21, 0, "My dog cat has fleas." };
            yield return new object[] { @"(d)(o)(g)(\s)(c)(a)(t)(\s)(h)(a)(s)", "My dog cat has fleas.", "$05$06$07$04$01$02$03$08$09$10$11", RegexOptions.CultureInvariant | RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline, 21, 0, "My cat dog has fleas." };

            // Error cases
            yield return new object[] { @"(?<256>cat)\s*(?<512>dog)", "slkfjsdcat dogkljeah", "STARTcat$512$", RegexOptions.None, 20, 0, "slkfjsdSTARTcatdog$kljeah" };
            yield return new object[] { @"(?<256>cat)\s*(?<512>dog)", "slkfjsdcat dogkljeah", "STARTcat$2048$1024dogEND", RegexOptions.None, 20, 0, "slkfjsdSTARTcat$2048$1024dogENDkljeah" };
            yield return new object[] { @"(?<cat>cat)\s*(?<dog>dog)", "slkfjsdcat dogkljeah", "START${catTWO}dogcat${dogTWO}END", RegexOptions.None, 20, 0, "slkfjsdSTART${catTWO}dogcat${dogTWO}ENDkljeah" };

            // RightToLeft
            yield return new object[] { @"foo\s+", "0123456789foo4567890foo         ", "bar", RegexOptions.RightToLeft, 32, 32, "0123456789foo4567890bar" };
            yield return new object[] { @"\d", "0123456789foo4567890foo         ", "#", RegexOptions.RightToLeft, 17, 32, "##########foo#######foo         " };
            yield return new object[] { @"\d", "0123456789foo4567890foo         ", "#", RegexOptions.RightToLeft, 7, 32, "0123456789foo#######foo         " };
            yield return new object[] { @"\d", "0123456789foo4567890foo         ", "#", RegexOptions.RightToLeft, 0, 32, "0123456789foo4567890foo         " };
            yield return new object[] { @"\d", "0123456789foo4567890foo         ", "#", RegexOptions.RightToLeft, -1, 32, "##########foo#######foo         " };

            yield return new object[] { "([1-9])([1-9])([1-9])def", "abc123def!", "$0", RegexOptions.RightToLeft, -1, 10, "abc123def!" };
            yield return new object[] { "([1-9])([1-9])([1-9])def", "abc123def!", "$1", RegexOptions.RightToLeft, -1, 10, "abc1!" };
            yield return new object[] { "([1-9])([1-9])([1-9])def", "abc123def!", "$2", RegexOptions.RightToLeft, -1, 10, "abc2!" };
            yield return new object[] { "([1-9])([1-9])([1-9])def", "abc123def!", "$3", RegexOptions.RightToLeft, -1, 10, "abc3!" };
            yield return new object[] { "([1-9])([1-9])([1-9])def", "abc123def!", "$4", RegexOptions.RightToLeft, -1, 10, "abc$4!" };

            yield return new object[] { "([1-9])([1-9])([1-9])def", "abc123def!", "$$", RegexOptions.RightToLeft, -1, 10, "abc$!" };
            yield return new object[] { "([1-9])([1-9])([1-9])def", "abc123def!", "$&", RegexOptions.RightToLeft, -1, 10, "abc123def!" };
            yield return new object[] { "([1-9])([1-9])([1-9])def", "abc123def!", "$`", RegexOptions.RightToLeft, -1, 10, "abcabc!" };
            yield return new object[] { "([1-9])([1-9])([1-9])def", "abc123def!", "$'", RegexOptions.RightToLeft, -1, 10, "abc!!" };

            yield return new object[] { "([1-9])([1-9])([1-9])def", "abc123def!", "$+", RegexOptions.RightToLeft, -1, 10, "abc3!" };
            yield return new object[] { "([1-9])([1-9])([1-9])def", "abc123def!", "$_", RegexOptions.RightToLeft, -1, 10, "abcabc123def!!" };
        }

        [Theory]
        [MemberData(nameof(Replace_String_TestData))]
        [MemberData(nameof(RegexCompilationHelper.TransformRegexOptions), nameof(Replace_String_TestData), 3, MemberType = typeof(RegexCompilationHelper))]
        public void Replace(string pattern, string input, string replacement, RegexOptions options, int count, int start, string expected)
        {
            bool isDefaultStart = RegexHelpers.IsDefaultStart(input, options, start);
            bool isDefaultCount = RegexHelpers.IsDefaultCount(input, options, count);
            if (options == RegexOptions.None)
            {
                if (isDefaultStart && isDefaultCount)
                {
                    // Use Replace(string, string) or Replace(string, string, string)
                    Assert.Equal(expected, new Regex(pattern).Replace(input, replacement));
                    Assert.Equal(expected, Regex.Replace(input, pattern, replacement));
                }
                if (isDefaultStart)
                {
                    // Use Replace(string, string, string, int)
                    Assert.Equal(expected, new Regex(pattern).Replace(input, replacement, count));
                }
                // Use Replace(string, string, int, int)
                Assert.Equal(expected, new Regex(pattern).Replace(input, replacement, count, start));
            }
            if (isDefaultStart && isDefaultCount)
            {
                // Use Replace(string, string) or Replace(string, string, string, RegexOptions)
                Assert.Equal(expected, new Regex(pattern, options).Replace(input, replacement));
                Assert.Equal(expected, Regex.Replace(input, pattern, replacement, options));
            }
            if (isDefaultStart)
            {
                // Use Replace(string, string, string, int)
                Assert.Equal(expected, new Regex(pattern, options).Replace(input, replacement, count));
            }
            // Use Replace(string, string, int, int)
            Assert.Equal(expected, new Regex(pattern, options).Replace(input, replacement, count, start));
        }

        public static IEnumerable<object[]> Replace_MatchEvaluator_TestData()
        {
            yield return new object[] { "(Big|Small)", "Big mountain", new MatchEvaluator(MatchEvaluator1), RegexOptions.None, 12, 0, "Huge mountain" };
            yield return new object[] { "(Big|Small)", "Small village", new MatchEvaluator(MatchEvaluator1), RegexOptions.None, 13, 0, "Tiny village" };

            if ("i".ToUpper() == "I")
            {
                yield return new object[] { "(Big|Small)", "bIG horse", new MatchEvaluator(MatchEvaluator1), RegexOptions.IgnoreCase, 9, 0, "Huge horse" };
            }

            yield return new object[] { "(Big|Small)", "sMaLl dog", new MatchEvaluator(MatchEvaluator1), RegexOptions.IgnoreCase, 9, 0, "Tiny dog" };

            yield return new object[] { ".+", "XSP_TEST_FAILURE", new MatchEvaluator(MatchEvaluator2), RegexOptions.None, 16, 0, "SUCCESS" };
            yield return new object[] { "[abcabc]", "abcabc", new MatchEvaluator(MatchEvaluator3), RegexOptions.None, 6, 0, "ABCABC" };
            yield return new object[] { "[abcabc]", "abcabc", new MatchEvaluator(MatchEvaluator3), RegexOptions.None, 3, 0, "ABCabc" };
            yield return new object[] { "[abcabc]", "abcabc", new MatchEvaluator(MatchEvaluator3), RegexOptions.None, 3, 2, "abCABc" };

            // Regression test:
            // Regex treating Devanagari matra characters as matching "\b"
            // Unicode characters in the "Mark, NonSpacing" Category, U+0902=Devanagari sign anusvara, U+0947=Devanagri vowel sign E
            string boldInput = "\u092f\u0939 \u0915\u0930 \u0935\u0939 \u0915\u0930\u0947\u0902 \u0939\u0948\u0964";
            string boldExpected = "\u092f\u0939 <b>\u0915\u0930</b> \u0935\u0939 <b>\u0915\u0930\u0947\u0902</b> \u0939\u0948\u0964";
            yield return new object[] { @"\u0915\u0930.*?\b", boldInput, new MatchEvaluator(MatchEvaluatorBold), RegexOptions.CultureInvariant | RegexOptions.Singleline, boldInput.Length, 0, boldExpected };

            // RighToLeft
            yield return new object[] { @"foo\s+", "0123456789foo4567890foo         ", new MatchEvaluator(MatchEvaluatorBar), RegexOptions.RightToLeft, 32, 32, "0123456789foo4567890bar" };
            yield return new object[] { @"\d", "0123456789foo4567890foo         ", new MatchEvaluator(MatchEvaluatorPoundSign), RegexOptions.RightToLeft, 17, 32, "##########foo#######foo         " };
            yield return new object[] { @"\d", "0123456789foo4567890foo         ", new MatchEvaluator(MatchEvaluatorPoundSign), RegexOptions.RightToLeft, 7, 32, "0123456789foo#######foo         " };
            yield return new object[] { @"\d", "0123456789foo4567890foo         ", new MatchEvaluator(MatchEvaluatorPoundSign), RegexOptions.RightToLeft, 0, 32, "0123456789foo4567890foo         " };
            yield return new object[] { @"\d", "0123456789foo4567890foo         ", new MatchEvaluator(MatchEvaluatorPoundSign), RegexOptions.RightToLeft, -1, 32, "##########foo#######foo         " };
        }

        [Theory]
        [MemberData(nameof(Replace_MatchEvaluator_TestData))]
        [MemberData(nameof(RegexCompilationHelper.TransformRegexOptions), nameof(Replace_MatchEvaluator_TestData), 3, MemberType = typeof(RegexCompilationHelper))]
        public void Replace(string pattern, string input, MatchEvaluator evaluator, RegexOptions options, int count, int start, string expected)
        {
            bool isDefaultStart = RegexHelpers.IsDefaultStart(input, options, start);
            bool isDefaultCount = RegexHelpers.IsDefaultCount(input, options, count);
            if (options == RegexOptions.None)
            {
                if (isDefaultStart && isDefaultCount)
                {
                    // Use Replace(string, MatchEvaluator) or Replace(string, string, MatchEvaluator)
                    Assert.Equal(expected, new Regex(pattern).Replace(input, evaluator));
                    Assert.Equal(expected, Regex.Replace(input, pattern, evaluator));
                }
                if (isDefaultStart)
                {
                    // Use Replace(string, MatchEvaluator, string, int)
                    Assert.Equal(expected, new Regex(pattern).Replace(input, evaluator, count));
                }
                // Use Replace(string, MatchEvaluator, int, int)
                Assert.Equal(expected, new Regex(pattern).Replace(input, evaluator, count, start));
            }
            if (isDefaultStart && isDefaultCount)
            {
                // Use Replace(string, MatchEvaluator) or Replace(string, MatchEvaluator, RegexOptions)
                Assert.Equal(expected, new Regex(pattern, options).Replace(input, evaluator));
                Assert.Equal(expected, Regex.Replace(input, pattern, evaluator, options));
            }
            if (isDefaultStart)
            {
                // Use Replace(string, MatchEvaluator, string, int)
                Assert.Equal(expected, new Regex(pattern, options).Replace(input, evaluator, count));
            }
            // Use Replace(string, MatchEvaluator, int, int)
            Assert.Equal(expected, new Regex(pattern, options).Replace(input, evaluator, count, start));
        }

        [Fact]
        public void Replace_NoMatch()
        {
            string input = "";
            Assert.Same(input, Regex.Replace(input, "no-match", "replacement"));
            Assert.Same(input, Regex.Replace(input, "no-match", new MatchEvaluator(MatchEvaluator1)));
        }

        [Fact]
        public void Replace_Invalid()
        {
            // Input is null
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.Replace(null, "pattern", "replacement"));
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.Replace(null, "pattern", "replacement", RegexOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.Replace(null, "pattern", "replacement", RegexOptions.None, TimeSpan.FromMilliseconds(1)));
            AssertExtensions.Throws<ArgumentNullException>("input", () => new Regex("pattern").Replace(null, "replacement"));
            AssertExtensions.Throws<ArgumentNullException>("input", () => new Regex("pattern").Replace(null, "replacement", 0));
            AssertExtensions.Throws<ArgumentNullException>("input", () => new Regex("pattern").Replace(null, "replacement", 0, 0));

            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.Replace(null, "pattern", new MatchEvaluator(MatchEvaluator1)));
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.Replace(null, "pattern", new MatchEvaluator(MatchEvaluator1), RegexOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.Replace(null, "pattern", new MatchEvaluator(MatchEvaluator1), RegexOptions.None, TimeSpan.FromMilliseconds(1)));
            AssertExtensions.Throws<ArgumentNullException>("input", () => new Regex("pattern").Replace(null, new MatchEvaluator(MatchEvaluator1)));
            AssertExtensions.Throws<ArgumentNullException>("input", () => new Regex("pattern").Replace(null, new MatchEvaluator(MatchEvaluator1), 0));
            AssertExtensions.Throws<ArgumentNullException>("input", () => new Regex("pattern").Replace(null, new MatchEvaluator(MatchEvaluator1), 0, 0));

            // Pattern is null
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Replace("input", null, "replacement"));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Replace("input", null, "replacement", RegexOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Replace("input", null, "replacement", RegexOptions.None, TimeSpan.FromMilliseconds(1)));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Replace("input", null, new MatchEvaluator(MatchEvaluator1)));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Replace("input", null, new MatchEvaluator(MatchEvaluator1), RegexOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Replace("input", null, new MatchEvaluator(MatchEvaluator1), RegexOptions.None, TimeSpan.FromMilliseconds(1)));

            // Replacement is null
            AssertExtensions.Throws<ArgumentNullException>("replacement", () => Regex.Replace("input", "pattern", (string)null));
            AssertExtensions.Throws<ArgumentNullException>("replacement", () => Regex.Replace("input", "pattern", (string)null, RegexOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("replacement", () => Regex.Replace("input", "pattern", (string)null, RegexOptions.None, TimeSpan.FromMilliseconds(1)));
            AssertExtensions.Throws<ArgumentNullException>("replacement", () => new Regex("pattern").Replace("input", (string)null));
            AssertExtensions.Throws<ArgumentNullException>("replacement", () => new Regex("pattern").Replace("input", (string)null, 0));
            AssertExtensions.Throws<ArgumentNullException>("replacement", () => new Regex("pattern").Replace("input", (string)null, 0, 0));

            AssertExtensions.Throws<ArgumentNullException>("evaluator", () => Regex.Replace("input", "pattern", (MatchEvaluator)null));
            AssertExtensions.Throws<ArgumentNullException>("evaluator", () => Regex.Replace("input", "pattern", (MatchEvaluator)null, RegexOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("evaluator", () => Regex.Replace("input", "pattern", (MatchEvaluator)null, RegexOptions.None, TimeSpan.FromMilliseconds(1)));
            AssertExtensions.Throws<ArgumentNullException>("evaluator", () => new Regex("pattern").Replace("input", (MatchEvaluator)null));
            AssertExtensions.Throws<ArgumentNullException>("evaluator", () => new Regex("pattern").Replace("input", (MatchEvaluator)null, 0));
            AssertExtensions.Throws<ArgumentNullException>("evaluator", () => new Regex("pattern").Replace("input", (MatchEvaluator)null, 0, 0));

            // Count is invalid
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => new Regex("pattern").Replace("input", "replacement", -2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => new Regex("pattern").Replace("input", "replacement", -2, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => new Regex("pattern").Replace("input", new MatchEvaluator(MatchEvaluator1), -2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => new Regex("pattern").Replace("input", new MatchEvaluator(MatchEvaluator1), -2, 0));

            // Start is invalid
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startat", () => new Regex("pattern").Replace("input", "replacement", 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startat", () => new Regex("pattern").Replace("input", new MatchEvaluator(MatchEvaluator1), 0, -1));
            
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startat", () => new Regex("pattern").Replace("input", "replacement", 0, 6));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startat", () => new Regex("pattern").Replace("input", new MatchEvaluator(MatchEvaluator1), 0, 6));
        }

        public static string MatchEvaluator1(Match match) => match.Value.ToLower() == "big" ? "Huge": "Tiny";

        public static string MatchEvaluator2(Match match) => "SUCCESS";

        public static string MatchEvaluator3(Match match)
        {
            if (match.Value == "a" || match.Value == "b" || match.Value == "c")
                return match.Value.ToUpperInvariant();
            return string.Empty;
        }

        public static string MatchEvaluatorBold(Match match) => string.Format("<b>{0}</b>", match.Value);

        private static string MatchEvaluatorBar(Match match) => "bar";
        private static string MatchEvaluatorPoundSign(Match match) => "#";
    }
}
