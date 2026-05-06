// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexReplaceTests
    {
        public static IEnumerable<object[]> Replace_String_TestData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, @"a", "bbbb", "c", RegexOptions.None, 4, 3, "bbbb" };
                yield return new object[] { engine, @"", "   ", "123", RegexOptions.None, 4, 0, "123 123 123 123" };
                yield return new object[] { engine, "icrosoft", "MiCrOsOfT", "icrosoft", RegexOptions.IgnoreCase, 9, 0, "Microsoft" };
                yield return new object[] { engine, "dog", "my dog has fleas", "CAT", RegexOptions.IgnoreCase, 16, 0, "my CAT has fleas" };
                yield return new object[] { engine, "a", "aaaaa", "b", RegexOptions.None, 2, 0, "bbaaa" };
                yield return new object[] { engine, "a", "aaaaa", "b", RegexOptions.None, 2, 3, "aaabb" };

                // Stress
                yield return new object[] { engine, ".", new string('a', 999), "b", RegexOptions.None, 999, 0, new string('b', 999) };

                // Undefined groups
                yield return new object[] { engine, @"(?<256>cat)\s*(?<512>dog)", "slkfjsdcat dogkljeah", "STARTcat$2048$1024dogEND", RegexOptions.None, 20, 0, "slkfjsdSTARTcat$2048$1024dogENDkljeah" };
                yield return new object[] { engine, @"(?<cat>cat)\s*(?<dog>dog)", "slkfjsdcat dogkljeah", "START${catTWO}dogcat${dogTWO}END", RegexOptions.None, 20, 0, "slkfjsdSTART${catTWO}dogcat${dogTWO}ENDkljeah" };

                // Replace with group numbers
                yield return new object[] { engine, "([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z])))))))))))))))", "abcdefghiklmnop", "$15", RegexOptions.None, 15, 0, "p" };
                yield return new object[] { engine, "([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z])))))))))))))))", "abcdefghiklmnop", "$3", RegexOptions.None, 15, 0, "cdefghiklmnop" };
                yield return new object[] { engine, @"D\.(.+)", "D.Bau", "David $1", RegexOptions.None, 5, 0, "David Bau" };

                // Stress
                string pattern = string.Concat(Enumerable.Repeat("([a-z]", 999).Concat(Enumerable.Repeat(")", 999)));
                string input = string.Concat(Enumerable.Repeat("abcde", 200));
                yield return new object[] { engine, pattern, input, "$999", RegexOptions.None, input.Length, 0, "de" };
                yield return new object[] { engine, pattern, input, "$1", RegexOptions.None, input.Length, 0, input };

                // Undefined group
                yield return new object[] { engine, "([a_z])(.+)", "abc", "$3", RegexOptions.None, 3, 0, "$3" };
                yield return new object[] { engine, @"(?<256>cat)\s*(?<512>dog)", "slkfjsdcat dogkljeah", "STARTcat$2048$1024dogEND", RegexOptions.None, 20, 0, "slkfjsdSTARTcat$2048$1024dogENDkljeah" };

                // Valid cases
                yield return new object[] { engine, @"[^ ]+\s(?<time>)", "08/10/99 16:00", "${time}", RegexOptions.None, 14, 0, "16:00" };

                yield return new object[] { engine, @"(?<cat>cat)\s*(?<dog>dog)", "cat dog", "${cat}est ${dog}est", RegexOptions.None, 7, 0, "catest dogest" };
                yield return new object[] { engine, @"(?<cat>cat)\s*(?<dog>dog)", "slkfjsdcat dogkljeah", "START${cat}dogcat${dog}END", RegexOptions.None, 20, 0, "slkfjsdSTARTcatdogcatdogENDkljeah" };
                yield return new object[] { engine, @"(?<512>cat)\s*(?<256>dog)", "slkfjsdcat dogkljeah", "START${512}dogcat${256}END", RegexOptions.None, 20, 0, "slkfjsdSTARTcatdogcatdogENDkljeah" };
                yield return new object[] { engine, @"(?<256>cat)\s*(?<512>dog)", "slkfjsdcat dogkljeah", "START${256}dogcat${512}END", RegexOptions.None, 20, 0, "slkfjsdSTARTcatdogcatdogENDkljeah" };
                yield return new object[] { engine, @"(?<512>cat)\s*(?<256>dog)", "slkfjsdcat dogkljeah", "STARTcat$256$512dogEND", RegexOptions.None, 20, 0, "slkfjsdSTARTcatdogcatdogENDkljeah" };
                yield return new object[] { engine, @"(?<256>cat)\s*(?<512>dog)", "slkfjsdcat dogkljeah", "STARTcat$512$256dogEND", RegexOptions.None, 20, 0, "slkfjsdSTARTcatdogcatdogENDkljeah" };

                yield return new object[] { engine, @"(hello)cat\s+dog(world)", "hellocat dogworld", "$1$$$2", RegexOptions.None, 19, 0, "hello$world" };
                yield return new object[] { engine, @"(cat)\s+(dog)", "before textcat dogafter text", ". The following should be dog and it is $+. ", RegexOptions.None, 28, 0, "before text. The following should be dog and it is dog. after text" };

                yield return new object[] { engine, @"(hello)\s+(world)", "What the hello world goodby", "$&, how are you?", RegexOptions.None, 27, 0, "What the hello world, how are you? goodby" };
                yield return new object[] { engine, @"(hello)\s+(world)", "What the hello world goodby", "$0, how are you?", RegexOptions.None, 27, 0, "What the hello world, how are you? goodby" };
                yield return new object[] { engine, @"(hello)\s+(world)", "What the hello world goodby", "$`cookie are you doing", RegexOptions.None, 27, 0, "What the What the cookie are you doing goodby" };
                yield return new object[] { engine, @"(cat)\s+(dog)", "before textcat dogafter text", ". This is the $' and ", RegexOptions.None, 28, 0, "before text. This is the after text and after text" };
                yield return new object[] { engine, @"(cat)\s+(dog)", "before textcat dogafter text", ". The following should be the entire string '$_'. ", RegexOptions.None, 28, 0, "before text. The following should be the entire string 'before textcat dogafter text'. after text" };

                yield return new object[] { engine, @"(hello)\s+(world)", "START hello    world END", "$2 $1 $1 $2 $3$4", RegexOptions.None, 24, 0, "START world hello hello world $3$4 END" };
                yield return new object[] { engine, @"(hello)\s+(world)", "START hello    world END", "$2 $1 $1 $2 $123$234", RegexOptions.None, 24, 0, "START world hello hello world $123$234 END" };

                yield return new object[] { engine, @"(d)(o)(g)(\s)(c)(a)(t)(\s)(h)(a)(s)", "My dog cat has fleas.", "$01$02$03$04$05$06$07$08$09$10$11", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Multiline, 21, 0, "My dog cat has fleas." };
                yield return new object[] { engine, @"(d)(o)(g)(\s)(c)(a)(t)(\s)(h)(a)(s)", "My dog cat has fleas.", "$05$06$07$04$01$02$03$08$09$10$11", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Multiline, 21, 0, "My cat dog has fleas." };

                // Error cases
                yield return new object[] { engine, @"(?<256>cat)\s*(?<512>dog)", "slkfjsdcat dogkljeah", "STARTcat$512$", RegexOptions.None, 20, 0, "slkfjsdSTARTcatdog$kljeah" };

                if (!RegexHelpers.IsNonBacktracking(engine))
                {
                    // ECMAScript
                    yield return new object[] { engine, @"(?<512>cat)\s*(?<256>dog)", "slkfjsdcat dogkljeah", "STARTcat${256}${512}dogEND", RegexOptions.ECMAScript, 20, 0, "slkfjsdSTARTcatdogcatdogENDkljeah" };
                    yield return new object[] { engine, @"(?<256>cat)\s*(?<512>dog)", "slkfjsdcat dogkljeah", "STARTcat${512}${256}dogEND", RegexOptions.ECMAScript, 20, 0, "slkfjsdSTARTcatdogcatdogENDkljeah" };
                    yield return new object[] { engine, @"(?<1>cat)\s*(?<2>dog)", "slkfjsdcat dogkljeah", "STARTcat$2$1dogEND", RegexOptions.ECMAScript, 20, 0, "slkfjsdSTARTcatdogcatdogENDkljeah" };
                    yield return new object[] { engine, @"(?<2>cat)\s*(?<1>dog)", "slkfjsdcat dogkljeah", "STARTcat$1$2dogEND", RegexOptions.ECMAScript, 20, 0, "slkfjsdSTARTcatdogcatdogENDkljeah" };
                    yield return new object[] { engine, @"(?<512>cat)\s*(?<256>dog)", "slkfjsdcat dogkljeah", "STARTcat$256$512dogEND", RegexOptions.ECMAScript, 20, 0, "slkfjsdSTARTcatdogcatdogENDkljeah" };
                    yield return new object[] { engine, @"(?<256>cat)\s*(?<512>dog)", "slkfjsdcat dogkljeah", "START${256}dogcat${512}END", RegexOptions.ECMAScript, 20, 0, "slkfjsdSTARTcatdogcatdogENDkljeah" };

                    yield return new object[] { engine, @"(hello)\s+world", "START hello    world END", "$234 $1 $1 $234 $3$4", RegexOptions.ECMAScript, 24, 0, "START $234 hello hello $234 $3$4 END" };
                    yield return new object[] { engine, @"(hello)\s+(world)", "START hello    world END", "$2 $1 $1 $2 $3$4", RegexOptions.ECMAScript, 24, 0, "START world hello hello world $3$4 END" };
                    yield return new object[] { engine, @"(hello)\s+(world)", "START hello    world END", "$2 $1 $1 $2 $123$234", RegexOptions.ECMAScript, 24, 0, "START world hello hello world hello23world34 END" };
                    yield return new object[] { engine, @"(?<12>hello)\s+(world)", "START hello    world END", "$1 $12 $12 $1 $123$134", RegexOptions.ECMAScript, 24, 0, "START world hello hello world hello3world34 END" };
                    yield return new object[] { engine, @"(?<123>hello)\s+(?<23>world)", "START hello    world END", "$23 $123 $123 $23 $123$234", RegexOptions.ECMAScript, 24, 0, "START world hello hello world helloworld4 END" };
                    yield return new object[] { engine, @"(?<123>hello)\s+(?<234>world)", "START hello    world END", "$234 $123 $123 $234 $123456$234567", RegexOptions.ECMAScript, 24, 0, "START world hello hello world hello456world567 END" };

                    yield return new object[] { engine, @"(d)(o)(g)(\s)(c)(a)(t)(\s)(h)(a)(s)", "My dog cat has fleas.", "$01$02$03$04$05$06$07$08$09$10$11", RegexOptions.CultureInvariant | RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline, 21, 0, "My dog cat has fleas." };
                    yield return new object[] { engine, @"(d)(o)(g)(\s)(c)(a)(t)(\s)(h)(a)(s)", "My dog cat has fleas.", "$05$06$07$04$01$02$03$08$09$10$11", RegexOptions.CultureInvariant | RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline, 21, 0, "My cat dog has fleas." };

                    // RightToLeft
                    yield return new object[] { engine, @"a", "bbbb", "c", RegexOptions.RightToLeft, 4, 3, "bbbb" };
                    yield return new object[] { engine, @"", "   ", "123", RegexOptions.RightToLeft, 4, 3, "123 123 123 123" };
                    yield return new object[] { engine, @"foo\s+", "0123456789foo4567890foo         ", "bar", RegexOptions.RightToLeft, 32, 32, "0123456789foo4567890bar" };
                    yield return new object[] { engine, @"\d", "0123456789foo4567890foo         ", "#", RegexOptions.RightToLeft, 17, 32, "##########foo#######foo         " };
                    yield return new object[] { engine, @"\d", "0123456789foo4567890foo         ", "#", RegexOptions.RightToLeft, 7, 32, "0123456789foo#######foo         " };
                    yield return new object[] { engine, @"\d", "0123456789foo4567890foo         ", "#", RegexOptions.RightToLeft, 0, 32, "0123456789foo4567890foo         " };
                    yield return new object[] { engine, @"\d", "0123456789foo4567890foo         ", "#", RegexOptions.RightToLeft, -1, 32, "##########foo#######foo         " };

                    yield return new object[] { engine, "([1-9])([1-9])([1-9])def", "abc123def!", "$0", RegexOptions.RightToLeft, -1, 10, "abc123def!" };
                    yield return new object[] { engine, "([1-9])([1-9])([1-9])def", "abc123def!", "$1", RegexOptions.RightToLeft, -1, 10, "abc1!" };
                    yield return new object[] { engine, "([1-9])([1-9])([1-9])def", "abc123def!", "$2", RegexOptions.RightToLeft, -1, 10, "abc2!" };
                    yield return new object[] { engine, "([1-9])([1-9])([1-9])def", "abc123def!", "$3", RegexOptions.RightToLeft, -1, 10, "abc3!" };
                    yield return new object[] { engine, "([1-9])([1-9])([1-9])def", "abc123def!", "$4", RegexOptions.RightToLeft, -1, 10, "abc$4!" };

                    yield return new object[] { engine, "([1-9])([1-9])([1-9])def", "abc123def!", "$$", RegexOptions.RightToLeft, -1, 10, "abc$!" };
                    yield return new object[] { engine, "([1-9])([1-9])([1-9])def", "abc123def!", "$&", RegexOptions.RightToLeft, -1, 10, "abc123def!" };
                    yield return new object[] { engine, "([1-9])([1-9])([1-9])def", "abc123def!", "$`", RegexOptions.RightToLeft, -1, 10, "abcabc!" };
                    yield return new object[] { engine, "([1-9])([1-9])([1-9])def", "abc123def!", "$'", RegexOptions.RightToLeft, -1, 10, "abc!!" };

                    yield return new object[] { engine, "([1-9])([1-9])([1-9])def", "abc123def!", "$+", RegexOptions.RightToLeft, -1, 10, "abc3!" };
                    yield return new object[] { engine, "([1-9])([1-9])([1-9])def", "abc123def!", "$_", RegexOptions.RightToLeft, -1, 10, "abcabc123def!!" };

                    // Anchors
                    yield return new object[] { engine, @"\Ga", "aaaaa", "b", RegexOptions.None, 5, 0, "bbbbb" };
                }
            }
        }

        [Theory]
        [MemberData(nameof(Replace_String_TestData))]
        public async Task Replace(RegexEngine engine, string pattern, string input, string replacement, RegexOptions options, int count, int start, string expected)
        {
            Regex r = await RegexHelpers.GetRegexAsync(engine, pattern, options);

            bool isDefaultStart = RegexHelpers.IsDefaultStart(input, options, start);
            bool isDefaultCount = RegexHelpers.IsDefaultCount(input, options, count);

            if (isDefaultStart)
            {
                if (isDefaultCount)
                {
                    Assert.Equal(expected, r.Replace(input, replacement));
                    Assert.Equal(expected, Regex.Replace(input, pattern, replacement, options));
                }

                Assert.Equal(expected, r.Replace(input, replacement, count));
            }

            Assert.Equal(expected, r.Replace(input, replacement, count, start));
        }

        public static IEnumerable<object[]> Replace_MatchEvaluator_TestData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, "a", "bbbb", new MatchEvaluator(match => "uhoh"), RegexOptions.None, 4, 0, "bbbb" };
                yield return new object[] { engine, "(Big|Small)", "Big mountain", new MatchEvaluator(MatchEvaluator1), RegexOptions.None, 12, 0, "Huge mountain" };
                yield return new object[] { engine, "(Big|Small)", "Small village", new MatchEvaluator(MatchEvaluator1), RegexOptions.None, 13, 0, "Tiny village" };

                if ("i".ToUpper() == "I")
                {
                    yield return new object[] { engine, "(Big|Small)", "bIG horse", new MatchEvaluator(MatchEvaluator1), RegexOptions.IgnoreCase, 9, 0, "Huge horse" };
                }

                yield return new object[] { engine, "(Big|Small)", "sMaLl dog", new MatchEvaluator(MatchEvaluator1), RegexOptions.IgnoreCase, 9, 0, "Tiny dog" };

                yield return new object[] { engine, ".+", "XSP_TEST_FAILURE", new MatchEvaluator(MatchEvaluator2), RegexOptions.None, 16, 0, "SUCCESS" };
                yield return new object[] { engine, "[abcabc]", "abcabc", new MatchEvaluator(MatchEvaluator3), RegexOptions.None, 6, 0, "ABCABC" };
                yield return new object[] { engine, "[abcabc]", "abcabc", new MatchEvaluator(MatchEvaluator3), RegexOptions.None, 3, 0, "ABCabc" };
                yield return new object[] { engine, "[abcabc]", "abcabc", new MatchEvaluator(MatchEvaluator3), RegexOptions.None, 3, 2, "abCABc" };

                if (!RegexHelpers.IsNonBacktracking(engine))
                {
                    // Regression test:
                    // Regex treating Devanagari matra characters as matching "\b"
                    // Unicode characters in the "Mark, NonSpacing" Category, U+0902=Devanagari sign anusvara, U+0947=Devanagri vowel sign E
                    string boldInput = "\u092f\u0939 \u0915\u0930 \u0935\u0939 \u0915\u0930\u0947\u0902 \u0939\u0948\u0964";
                    string boldExpected = "\u092f\u0939 <b>\u0915\u0930</b> \u0935\u0939 <b>\u0915\u0930\u0947\u0902</b> \u0939\u0948\u0964";
                    yield return new object[] { engine, @"\u0915\u0930.*?\b", boldInput, new MatchEvaluator(MatchEvaluatorBold), RegexOptions.CultureInvariant | RegexOptions.Singleline, boldInput.Length, 0, boldExpected };

                    // RighToLeft
                    yield return new object[] { engine, "a", "bbbb", new MatchEvaluator(match => "uhoh"), RegexOptions.RightToLeft, 4, 3, "bbbb" };
                    yield return new object[] { engine, @"foo\s+", "0123456789foo4567890foo         ", new MatchEvaluator(MatchEvaluatorBar), RegexOptions.RightToLeft, 32, 32, "0123456789foo4567890bar" };
                    yield return new object[] { engine, @"\d", "0123456789foo4567890foo         ", new MatchEvaluator(MatchEvaluatorPoundSign), RegexOptions.RightToLeft, 17, 32, "##########foo#######foo         " };
                    yield return new object[] { engine, @"\d", "0123456789foo4567890foo         ", new MatchEvaluator(MatchEvaluatorPoundSign), RegexOptions.RightToLeft, 7, 32, "0123456789foo#######foo         " };
                    yield return new object[] { engine, @"\d", "0123456789foo4567890foo         ", new MatchEvaluator(MatchEvaluatorPoundSign), RegexOptions.RightToLeft, 0, 32, "0123456789foo4567890foo         " };
                    yield return new object[] { engine, @"\d", "0123456789foo4567890foo         ", new MatchEvaluator(MatchEvaluatorPoundSign), RegexOptions.RightToLeft, -1, 32, "##########foo#######foo         " };
                }
            }
        }

        [Theory]
        [MemberData(nameof(Replace_MatchEvaluator_TestData))]
        public async Task Replace_MatchEvaluator_Test(RegexEngine engine, string pattern, string input, MatchEvaluator evaluator, RegexOptions options, int count, int start, string expected)
        {
            bool isDefaultStart = RegexHelpers.IsDefaultStart(input, options, start);
            bool isDefaultCount = RegexHelpers.IsDefaultCount(input, options, count);

            Regex r = await RegexHelpers.GetRegexAsync(engine, pattern, options);

            if (isDefaultStart && isDefaultCount)
            {
                Assert.Equal(expected, r.Replace(input, evaluator));
            }

            if (isDefaultStart)
            {
                Assert.Equal(expected, r.Replace(input, evaluator, count));
            }

            Assert.Equal(expected, r.Replace(input, evaluator, count, start));
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Doesn't support NonBacktracking")]
        [Fact]
        public void Replace_MatchEvaluator_Test_NonBacktracking_Matra()
        {
            // Regression test carried over from above to NonBacktracking mode:
            // Regex treating Devanagari matra characters as matching "\b"
            // Unicode characters in the "Mark, NonSpacing" Category, U+0902=Devanagari sign anusvara, U+0947=Devanagri vowel sign E
            string boldInput = "\u092f\u0939 \u0915\u0930 \u0935\u0939 \u0915\u0930\u0947\u0902 \u0939\u0948\u0964";
            string boldExpected = "\u092f\u0939 <b>\u0915\u0930</b> \u0935\u0939 <b>\u0915\u0930\u0947\u0902</b> \u0939\u0948\u0964";
            string pattern = @"\u0915\u0930.*?\b";
            var re = new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexHelpers.RegexOptionNonBacktracking);
            Assert.Equal(boldExpected, re.Replace(boldInput, new MatchEvaluator(MatchEvaluatorBold)));
        }

        public static IEnumerable<object[]> NoneCompiledBacktracking()
        {
            yield return new object[] { RegexOptions.None };
            yield return new object[] { RegexOptions.Compiled };
            if (PlatformDetection.IsNetCore)
            {
                yield return new object[] { RegexHelpers.RegexOptionNonBacktracking };
            }
        }

        [Theory]
        [MemberData(nameof(NoneCompiledBacktracking))]
        public void Replace_NoMatch(RegexOptions options)
        {
            string input = "";
            Assert.Same(input, Regex.Replace(input, "no-match", "replacement", options));
            Assert.Same(input, Regex.Replace(input, "no-match", new MatchEvaluator(MatchEvaluator1), options));
        }

        [Theory]
        [MemberData(nameof(NoneCompiledBacktracking))]
        public void Replace_MatchEvaluator_UniqueMatchObjects(RegexOptions options)
        {
            const string Input = "abcdefghijklmnopqrstuvwxyz";

            var matches = new List<Match>();

            string result = Regex.Replace(Input, @"[a-z]", match =>
            {
                Assert.Equal(((char)('a' + matches.Count)).ToString(), match.Value);
                matches.Add(match);
                return match.Value.ToUpperInvariant();
            }, options);

            Assert.Equal(26, matches.Count);
            Assert.Equal("ABCDEFGHIJKLMNOPQRSTUVWXYZ", result);

            Assert.Equal(Input, string.Concat(matches.Cast<Match>().Select(m => m.Value)));
        }

        [Theory]
        [MemberData(nameof(NoneCompiledBacktracking))]
        [InlineData(RegexOptions.RightToLeft)]
        public void Replace_MatchEvaluatorReturnsNullOrEmpty(RegexOptions options)
        {
            string result = Regex.Replace("abcde", @"[abcd]", (Match match) =>
            {
                return match.Value switch
                {
                    "a" => "x",
                    "b" => null,
                    "c" => "",
                    "d" => "y",
                    _ => throw new InvalidOperationException()
                };
            }, options);

            Assert.Equal("xye", result);
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

        public static string MatchEvaluator1(Match match) => match.Value.ToLower() == "big" ? "Huge" : "Tiny";

        public static string MatchEvaluator2(Match match) => "SUCCESS";

        public static string MatchEvaluator3(Match match)
        {
            if (match.Value is "a" or "b" or "c")
                return match.Value.ToUpperInvariant();
            return string.Empty;
        }

        public static string MatchEvaluatorBold(Match match) => string.Format("<b>{0}</b>", match.Value);

        private static string MatchEvaluatorBar(Match match) => "bar";
        private static string MatchEvaluatorPoundSign(Match match) => "#";

        public static IEnumerable<object[]> TestReplaceCornerCases_TestData()
        {
            foreach (object[] data in NoneCompiledBacktracking())
            {
                RegexOptions options = (RegexOptions)data[0];
                yield return new object[] { "[ab]+", "012aaabb34bba56", "###", 0, "012aaabb34bba56", options };
                yield return new object[] { "[ab]+", "012aaabb34bba56", "###", -1, "012###34###56", options };
                yield return new object[] { @"\b", "Hello World!", "#", 2, "#Hello# World!", options };
                yield return new object[] { "[ab]+", "012aaabb34bba56", "###", 1, "012###34bba56", options };
                yield return new object[] { @"\b", "Hello World!", "#$$#", -1, "#$#Hello#$# #$#World#$#!", options };
                yield return new object[] { @"", "hej", "  ", -1, "  h  e  j  ", options };
                if ((options & RegexHelpers.RegexOptionNonBacktracking) == 0)
                {
                    yield return new object[] { @"\bis\b", "this is it", "${2}", -1, "this ${2} it", options };
                }
            }
        }
        [Theory]
        [MemberData(nameof(TestReplaceCornerCases_TestData))]
        private void TestReplaceCornerCases(string pattern, string input, string replacement, int count, string expectedoutput, RegexOptions opt)
        {
            var regex = new Regex(pattern, opt);
            var output = regex.Replace(input, replacement, count);
            Assert.Equal(expectedoutput, output);
        }

        public static IEnumerable<object[]> TestReplaceWithSubstitution_TestData()
        {
            foreach (object[] data in NoneCompiledBacktracking())
            {
                RegexOptions options = (RegexOptions)data[0];
                yield return new object[] { @"(\$\d+):(\d+)", "it costs $500000:55 I think", "$$???:${2}", "it costs $???:55 I think", options };
                yield return new object[] { @"(\d+)([a-z]+)", "---12345abc---", "$2$1", "---abc12345---", options };
            }
        }
        [Theory]
        [MemberData(nameof(TestReplaceWithSubstitution_TestData))]
        private void TestReplaceWithSubstitution(string pattern, string input, string replacement, string expectedoutput, RegexOptions opt)
        {
            var output = new Regex(pattern, opt).Replace(input, replacement, -1);
            Assert.Equal(expectedoutput, output);
            var output2 = Regex.Replace(input, pattern, replacement, opt);
            Assert.Equal(expectedoutput, output2);
        }

        public static IEnumerable<object[]> TestReplaceWithToUpperMatchEvaluator_TestData()
        {
            foreach (object[] data in NoneCompiledBacktracking())
            {
                RegexOptions options = (RegexOptions)data[0];
                yield return new object[] { @"(\bis\b)", "this is it", "this IS it", options };
            }
        }
        [Theory]
        [MemberData(nameof(TestReplaceWithToUpperMatchEvaluator_TestData))]
        private void TestReplaceWithToUpperMatchEvaluator(string pattern, string input, string expectedoutput, RegexOptions opt)
        {
            MatchEvaluator f = new MatchEvaluator(m => m.Value.ToUpper());
            var output = new Regex(pattern, opt).Replace(input, f);
            Assert.Equal(expectedoutput, output);
            var output2 = Regex.Replace(input, pattern, f, opt);
            Assert.Equal(expectedoutput, output2);
        }
    }
}
