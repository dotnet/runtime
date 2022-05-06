// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using System.Linq;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Tests
{
    public partial class RegexMultipleMatchTests
    {
        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Matches_MultipleCapturingGroups(RegexEngine engine)
        {
            string[] expectedGroupValues = { "abracadabra", "abra", "cad" };
            string[] expectedGroupCaptureValues = { "abracad", "abra" };

            // Another example - given by Brad Merril in an article on RegularExpressions
            Regex regex = await RegexHelpers.GetRegexAsync(engine, @"(abra(cad)?)+");
            string input = "abracadabra1abracadabra2abracadabra3";
            Match match = regex.Match(input);
            while (match.Success)
            {
                string expected = "abracadabra";
                RegexAssert.Equal(expected, match);
                if (!RegexHelpers.IsNonBacktracking(engine))
                {
                    Assert.Equal(3, match.Groups.Count);
                    for (int i = 0; i < match.Groups.Count; i++)
                    {
                        RegexAssert.Equal(expectedGroupValues[i], match.Groups[i]);
                        if (i == 1)
                        {
                            Assert.Equal(2, match.Groups[i].Captures.Count);
                            for (int j = 0; j < match.Groups[i].Captures.Count; j++)
                            {
                                RegexAssert.Equal(expectedGroupCaptureValues[j], match.Groups[i].Captures[j]);
                            }
                        }
                        else if (i == 2)
                        {
                            Assert.Equal(1, match.Groups[i].Captures.Count);
                            RegexAssert.Equal("cad", match.Groups[i].Captures[0]);
                        }
                    }
                    Assert.Equal(1, match.Captures.Count);
                    RegexAssert.Equal("abracadabra", match.Captures[0]);
                }
                match = match.NextMatch();
            }
        }

        public static IEnumerable<object[]> Matches_TestData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[]
                {
                    engine,
                    "[0-9]", "12345asdfasdfasdfljkhsda67890", RegexOptions.None,
                    new CaptureData[]
                    {
                        new CaptureData("1", 0, 1),
                        new CaptureData("2", 1, 1),
                        new CaptureData("3", 2, 1),
                        new CaptureData("4", 3, 1),
                        new CaptureData("5", 4, 1),
                        new CaptureData("6", 24, 1),
                        new CaptureData("7", 25, 1),
                        new CaptureData("8", 26, 1),
                        new CaptureData("9", 27, 1),
                        new CaptureData("0", 28, 1),
                    }
                };

                yield return new object[]
                {
                    engine,
                    "[a-z0-9]+", "[token1]? GARBAGEtoken2GARBAGE ;token3!", RegexOptions.None,
                    new CaptureData[]
                    {
                        new CaptureData("token1", 1, 6),
                        new CaptureData("token2", 17, 6),
                        new CaptureData("token3", 32, 6)
                    }
                };

                yield return new object[]
                {
                    engine,
                    "(abc){2}", " !abcabcasl  dkfjasiduf 12343214-//asdfjzpiouxoifzuoxpicvql23r\\` #$3245,2345278 :asdfas & 100% @daeeffga (ryyy27343) poiweurwabcabcasdfalksdhfaiuyoiruqwer{234}/[(132387 + x)]'aaa''?", RegexOptions.None,
                    new CaptureData[]
                    {
                        new CaptureData("abcabc", 2, 6),
                        new CaptureData("abcabc", 125, 6)
                    }
                };

                yield return new object[]
                {
                    engine,
                    @"\b\w*\b", "handling words of various lengths", RegexOptions.None,
                    new CaptureData[]
                    {
                        new CaptureData("handling", 0, 8),
                        new CaptureData("", 8, 0),
                        new CaptureData("words", 9, 5),
                        new CaptureData("", 14, 0),
                        new CaptureData("of", 15, 2),
                        new CaptureData("", 17, 0),
                        new CaptureData("various", 18, 7),
                        new CaptureData("", 25, 0),
                        new CaptureData("lengths", 26, 7),
                        new CaptureData("", 33, 0),
                    }
                };

                yield return new object[]
                {
                    engine,
                    @"\b\w{2}\b", "handling words of various lengths", RegexOptions.None,
                    new CaptureData[]
                    {
                        new CaptureData("of", 15, 2),
                    }
                };

                yield return new object[]
                {
                    engine,
                    @"\w{6,}", "handling words of various lengths", RegexOptions.None,
                    new CaptureData[]
                    {
                        new CaptureData("handling", 0, 8),
                        new CaptureData("various", 18, 7),
                        new CaptureData("lengths", 26, 7),
                    }
                };

                yield return new object[]
                {
                    engine,
                    "[a-z]", "a", RegexOptions.None,
                    new CaptureData[]
                    {
                        new CaptureData("a", 0, 1)
                    }
                };

                yield return new object[]
                {
                    engine,
                    "[a-z]", "a1bc", RegexOptions.None,
                    new CaptureData[]
                    {
                        new CaptureData("a", 0, 1),
                        new CaptureData("b", 2, 1),
                        new CaptureData("c", 3, 1)
                    }
                };

                yield return new object[]
                {
                    engine,
                    "(?:ab|cd|ef|gh|i)j", "abj    cdj  efj           ghjij", RegexOptions.None,
                    new CaptureData[]
                    {
                        new CaptureData("abj", 0, 3),
                        new CaptureData("cdj", 7, 3),
                        new CaptureData("efj", 12, 3),
                        new CaptureData("ghj", 26, 3),
                        new CaptureData("ij", 29, 2),
                    }
                };

                // Using ^ and $ with multiline
                yield return new object[]
                {
                    engine,
                    "^", "", RegexOptions.Multiline,
                    new[] { new CaptureData("", 0, 0) }
                };

                yield return new object[]
                {
                    engine,
                    "^", "\n\n\n", RegexOptions.Multiline,
                    new[]
                    {
                        new CaptureData("", 0, 0),
                        new CaptureData("", 1, 0),
                        new CaptureData("", 2, 0),
                        new CaptureData("", 3, 0)
                    }
                };

                yield return new object[]
                {
                    engine,
                    "^abc", "abc\nabc \ndef abc \nab\nabc", RegexOptions.Multiline,
                    new[]
                    {
                        new CaptureData("abc", 0, 3),
                        new CaptureData("abc", 4, 3),
                        new CaptureData("abc", 21, 3),
                    }
                };

                yield return new object[]
                {
                    engine,
                    @"^\w{5}", "abc\ndefg\n\nhijkl\n", RegexOptions.Multiline,
                    new[]
                    {
                        new CaptureData("hijkl", 10, 5),
                    }
                };

                yield return new object[]
                {
                    engine,
                    @"^.*$", "abc\ndefg\n\nhijkl\n", RegexOptions.Multiline,
                    new[]
                    {
                        new CaptureData("abc", 0, 3),
                        new CaptureData("defg", 4, 4),
                        new CaptureData("", 9, 0),
                        new CaptureData("hijkl", 10, 5),
                        new CaptureData("", 16, 0),
                    }
                };

                yield return new object[]
                {
                    engine,
                    ".*", "abc", RegexOptions.None,
                    new[]
                    {
                        new CaptureData("abc", 0, 3),
                        new CaptureData("", 3, 0)
                    }
                };

                yield return new object[]
                {
                    engine,
                     @"^[^a]a", "bar\n", RegexOptions.Multiline,
                     new[]
                     {
                         new CaptureData("ba", 0, 2)
                     }
                };

                yield return new object[]
                {
                    engine,
                     @"^[^a]a", "car\nbar\n", RegexOptions.Multiline,
                     new[]
                     {
                         new CaptureData("ca", 0, 2),
                         new CaptureData("ba", 4, 2)
                     }
                };

                yield return new object[]
                {
                    engine,
                     @"[0-9]cat$", "1cat\n2cat", RegexOptions.Multiline,
                     new[]
                     {
                         new CaptureData("1cat", 0, 4),
                         new CaptureData("2cat", 5, 4)
                     }
                };

                if (!PlatformDetection.IsNetFramework)
                {
                    // .NET Framework missing fix in https://github.com/dotnet/runtime/pull/1075
                    yield return new object[]
                    {
                        engine,
                        @"[a -\-\b]", "a #.", RegexOptions.None,
                        new CaptureData[]
                        {
                            new CaptureData("a", 0, 1),
                            new CaptureData(" ", 1, 1),
                            new CaptureData("#", 2, 1),
                        }
                    };
                }

                if (!RegexHelpers.IsNonBacktracking(engine))
                {
                    yield return new object[]
                    {
                        engine,
                        @"foo\d+", "0123456789foo4567890foo1foo  0987", RegexOptions.RightToLeft,
                        new CaptureData[]
                        {
                            new CaptureData("foo1", 20, 4),
                            new CaptureData("foo4567890", 10, 10),
                        }
                    };

                    yield return new object[]
                    {
                        engine,
                        "(?(A)A123|C789)", "A123 B456 C789", RegexOptions.None,
                        new CaptureData[]
                        {
                            new CaptureData("A123", 0, 4),
                            new CaptureData("C789", 10, 4),
                        }
                    };

                    yield return new object[]
                    {
                        engine,
                        "(?(A)A123|C789)", "A123 B456 C789", RegexOptions.None,
                        new CaptureData[]
                        {
                            new CaptureData("A123", 0, 4),
                            new CaptureData("C789", 10, 4),
                        }
                    };

                    yield return new object[]
                    {
                            engine,
                            @"(?(\w+)\w+|)", "abcd", RegexOptions.None,
                            new CaptureData[]
                            {
                                new CaptureData("abcd", 0, 4),
                                new CaptureData("", 4, 0),
                            }
                    };

                    if (!PlatformDetection.IsNetFramework)
                    {
                        // .NET Framework has some behavioral inconsistencies when there's no else branch.
                        yield return new object[]
                        {
                            engine,
                            @"(?(\w+)\w+)", "abcd", RegexOptions.None,
                            new CaptureData[]
                            {
                                new CaptureData("abcd", 0, 4),
                                new CaptureData("", 4, 0),
                            }
                        };
                    }

                    yield return new object[]
                    {
                        engine,
                        @"^.*$", "abc\ndefg\n\nhijkl\n", RegexOptions.Multiline | RegexOptions.RightToLeft,
                        new[]
                        {
                            new CaptureData("", 16, 0),
                            new CaptureData("hijkl", 10, 5),
                            new CaptureData("", 9, 0),
                            new CaptureData("defg", 4, 4),
                            new CaptureData("abc", 0, 3),
                        }
                    };

                    if (!PlatformDetection.IsNetFramework)
                    {
                        // .NET Framework missing fix in https://github.com/dotnet/runtime/pull/993
                        yield return new object[]
                        {
                            engine,
                            "[^]", "every", RegexOptions.ECMAScript,
                            new CaptureData[]
                            {
                                new CaptureData("e", 0, 1),
                                new CaptureData("v", 1, 1),
                                new CaptureData("e", 2, 1),
                                new CaptureData("r", 3, 1),
                                new CaptureData("y", 4, 1),
                            }
                        };
                    }
                }

#if !NETFRAMEWORK // these tests currently fail on .NET Framework, and we need to check IsDynamicCodeCompiled but that doesn't exist on .NET Framework
                if (engine != RegexEngine.Interpreter && // these tests currently fail with RegexInterpreter
                    RuntimeFeature.IsDynamicCodeCompiled) // if dynamic code isn't compiled, RegexOptions.Compiled falls back to the interpreter, for which these tests currently fail
                {
                    // Fails on interpreter and .NET Framework: [ActiveIssue("https://github.com/dotnet/runtime/issues/62094")]
                    yield return new object[]
                    {
                        engine, "@(a*)+?", "@", RegexOptions.None, new[]
                        {
                            new CaptureData("@", 0, 1)
                        }
                    };

                    // Fails on interpreter and .NET Framework: [ActiveIssue("https://github.com/dotnet/runtime/issues/62094")]
                    yield return new object[]
                    {
                        engine, @"(?:){93}", "x", RegexOptions.None, new[]
                        {
                            new CaptureData("", 0, 0),
                            new CaptureData("", 1, 0)
                        }
                    };

                    if (!RegexHelpers.IsNonBacktracking(engine)) // atomic subexpressions aren't supported
                    {
                        // Fails on interpreter and .NET Framework: [ActiveIssue("https://github.com/dotnet/runtime/issues/62094")]
                        yield return new object[]
                        {
                            engine, @"()(?>\1+?).\b", "xxxx", RegexOptions.None, new[]
                            {
                                new CaptureData("x", 3, 1),
                            }
                        };
                    }
                }
#endif
            }
        }

        [Theory]
        [MemberData(nameof(Matches_TestData))]
        public async Task Matches(RegexEngine engine, string pattern, string input, RegexOptions options, CaptureData[] expected)
        {
            Regex regexAdvanced = await RegexHelpers.GetRegexAsync(engine, pattern, options);
            VerifyMatches(regexAdvanced.Matches(input), expected);
            VerifyMatches(regexAdvanced.Match(input), expected);
        }

        private static void VerifyMatches(Match match, CaptureData[] expected)
        {
            for (int i = 0; match.Success; i++, match = match.NextMatch())
            {
                VerifyMatch(match, expected[i]);
            }
        }

        private static void VerifyMatches(MatchCollection matches, CaptureData[] expected)
        {
            Assert.Equal(expected.Length, matches.Count);
            for (int i = 0; i < matches.Count; i++)
            {
                VerifyMatch(matches[i], expected[i]);
            }
        }

        private static void VerifyMatch(Match match, CaptureData expected)
        {
            Assert.True(match.Success);
            Assert.Equal(expected.Index, match.Index);
            Assert.Equal(expected.Length, match.Length);
            RegexAssert.Equal(expected.Value, match);

            Assert.Equal(expected.Index, match.Groups[0].Index);
            Assert.Equal(expected.Length, match.Groups[0].Length);
            RegexAssert.Equal(expected.Value, match.Groups[0]);

            Assert.Equal(1, match.Captures.Count);
            Assert.Equal(expected.Index, match.Captures[0].Index);
            Assert.Equal(expected.Length, match.Captures[0].Length);
            RegexAssert.Equal(expected.Value, match.Captures[0]);
        }

        [Fact]
        public void Matches_Invalid()
        {
            // Input is null
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.Matches(null, "pattern"));
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.Matches(null, "pattern", RegexOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("input", () => Regex.Matches(null, "pattern", RegexOptions.None, TimeSpan.FromSeconds(1)));

            AssertExtensions.Throws<ArgumentNullException>("input", () => new Regex("pattern").Matches(null));
            AssertExtensions.Throws<ArgumentNullException>("input", () => new Regex("pattern").Matches(null, 0));

            // Pattern is null
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Matches("input", null));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Matches("input", null, RegexOptions.None));
            AssertExtensions.Throws<ArgumentNullException>("pattern", () => Regex.Matches("input", null, RegexOptions.None, TimeSpan.FromSeconds(1)));

            // Options are invalid
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => Regex.Matches("input", "pattern", (RegexOptions)(-1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => Regex.Matches("input", "pattern", (RegexOptions)(-1), TimeSpan.FromSeconds(1)));

            // 0x400 is new NonBacktracking mode that is now valid, 0x800 is still invalid
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => Regex.Matches("input", "pattern", (RegexOptions)0x800));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("options", () => Regex.Matches("input", "pattern", (RegexOptions)0x800, TimeSpan.FromSeconds(1)));

            // MatchTimeout is invalid
            AssertExtensions.Throws<ArgumentOutOfRangeException>("matchTimeout", () => Regex.Matches("input", "pattern", RegexOptions.None, TimeSpan.Zero));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("matchTimeout", () => Regex.Matches("input", "pattern", RegexOptions.None, TimeSpan.Zero));

            // Start is invalid
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startat", () => new Regex("pattern").Matches("input", -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startat", () => new Regex("pattern").Matches("input", 6));
        }

        [Fact]
        public void NextMatch_EmptyMatch_ReturnsEmptyMatch()
        {
            Assert.Same(Match.Empty, Match.Empty.NextMatch());
        }
    }
}
