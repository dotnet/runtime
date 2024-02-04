// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Tests;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexUnicodeCharTests
    {
        private const int MaxUnicodeRange = 2 << 15;

        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task RegexUnicodeChar(RegexEngine engine)
        {
            // Regex engine is Unicode aware now for the \w and \d character classes
            // \s is not - i.e. it still only recognizes the ASCII space separators, not Unicode ones
            // The new character classes for this:
            // [\p{L1}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Pc}]
            List<char> validChars = new List<char>();
            List<char> invalidChars = new List<char>();
            for (int i = 0; i < MaxUnicodeRange; i++)
            {
                char c = (char)i;
                switch (CharUnicodeInfo.GetUnicodeCategory(c))
                {
                    case UnicodeCategory.UppercaseLetter:        //Lu
                    case UnicodeCategory.LowercaseLetter:        //Li
                    case UnicodeCategory.TitlecaseLetter:        // Lt
                    case UnicodeCategory.ModifierLetter:         // Lm
                    case UnicodeCategory.OtherLetter:            // Lo
                    case UnicodeCategory.DecimalDigitNumber:     // Nd
                                                                 //                    case UnicodeCategory.LetterNumber:           // ??
                                                                 //                    case UnicodeCategory.OtherNumber:            // ??
                    case UnicodeCategory.NonSpacingMark:
                    //                    case UnicodeCategory.SpacingCombiningMark:   // Mc
                    case UnicodeCategory.ConnectorPunctuation:   // Pc
                        validChars.Add(c);
                        break;
                    default:
                        invalidChars.Add(c);
                        break;
                }
            }

            // \w - we will create strings from valid characters that form \w and make sure that the regex engine catches this.
            // Build a random string with valid characters followed by invalid characters
            Random random = new Random(-55);
            Regex regex = await RegexHelpers.GetRegexAsync(engine, @"\w*");

            int validCharLength = 10;
            int invalidCharLength = 15;

            for (int i = 0; i < 100; i++)
            {
                var builder1 = new StringBuilder();
                var builder2 = new StringBuilder();

                for (int j = 0; j < validCharLength; j++)
                {
                    char c = validChars[random.Next(validChars.Count)];
                    builder1.Append(c);
                    builder2.Append(c);
                }

                for (int j = 0; j < invalidCharLength; j++)
                {
                    builder1.Append(invalidChars[random.Next(invalidChars.Count)]);
                }

                string input = builder1.ToString();
                Match match = regex.Match(input);
                Assert.True(match.Success);

                Assert.Equal(builder2.ToString(), match.Value);
                Assert.Equal(0, match.Index);
                Assert.Equal(validCharLength, match.Length);

                match = match.NextMatch();
                do
                {
                    // We get empty matches for each of the non-matching characters of input to match
                    // the * wildcard in regex pattern.
                    Assert.Equal(string.Empty, match.Value);
                    Assert.Equal(0, match.Length);
                    match = match.NextMatch();
                } while (match.Success);
            }

            // Build a random string with invalid characters followed by valid characters and then again invalid
            random = new Random(-55);
            regex = await RegexHelpers.GetRegexAsync(engine, @"\w+");

            validCharLength = 10;
            invalidCharLength = 15;

            for (int i = 0; i < 500; i++)
            {
                var builder1 = new StringBuilder();
                var builder2 = new StringBuilder();

                for (int j = 0; j < invalidCharLength; j++)
                {
                    builder1.Append(invalidChars[random.Next(invalidChars.Count)]);
                }

                for (int j = 0; j < validCharLength; j++)
                {
                    char c = validChars[random.Next(validChars.Count)];
                    builder1.Append(c);
                    builder2.Append(c);
                }

                for (int j = 0; j < invalidCharLength; j++)
                {
                    builder1.Append(invalidChars[random.Next(invalidChars.Count)]);
                }

                string input = builder1.ToString();

                Match match = regex.Match(input);
                Assert.True(match.Success);

                Assert.Equal(builder2.ToString(), match.Value);
                Assert.Equal(invalidCharLength, match.Index);
                Assert.Equal(validCharLength, match.Length);

                match = match.NextMatch();
                Assert.False(match.Success);
            }

            validChars = new List<char>();
            invalidChars = new List<char>();
            for (int i = 0; i < MaxUnicodeRange; i++)
            {
                char c = (char)i;
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.DecimalDigitNumber)
                {
                    validChars.Add(c);
                }
                else
                {
                    invalidChars.Add(c);
                }
            }

            // \d - we will create strings from valid characters that form \d and make sure that the regex engine catches this.
            // Build a random string with valid characters and then again invalid
            regex = await RegexHelpers.GetRegexAsync(engine, @"\d+");

            validCharLength = 10;
            invalidCharLength = 15;

            for (int i = 0; i < 100; i++)
            {
                var builder1 = new StringBuilder();
                var builder2 = new StringBuilder();

                for (int j = 0; j < validCharLength; j++)
                {
                    char c = validChars[random.Next(validChars.Count)];
                    builder1.Append(c);
                    builder2.Append(c);
                }

                for (int j = 0; j < invalidCharLength; j++)
                {
                    builder1.Append(invalidChars[random.Next(invalidChars.Count)]);
                }

                string input = builder1.ToString();
                Match match = regex.Match(input);


                Assert.Equal(builder2.ToString(), match.Value);
                Assert.Equal(0, match.Index);
                Assert.Equal(validCharLength, match.Length);

                match = match.NextMatch();
                Assert.False(match.Success);
            }

            // Build a random string with invalid characters, valid and then again invalid
            regex = await RegexHelpers.GetRegexAsync(engine, @"\d+");

            validCharLength = 10;
            invalidCharLength = 15;

            for (int i = 0; i < 100; i++)
            {
                var builder1 = new StringBuilder();
                var builder2 = new StringBuilder();

                for (int j = 0; j < invalidCharLength; j++)
                {
                    builder1.Append(invalidChars[random.Next(invalidChars.Count)]);
                }

                for (int j = 0; j < validCharLength; j++)
                {
                    char c = validChars[random.Next(validChars.Count)];
                    builder1.Append(c);
                    builder2.Append(c);
                }

                for (int j = 0; j < invalidCharLength; j++)
                {
                    builder1.Append(invalidChars[random.Next(invalidChars.Count)]);
                }

                string input = builder1.ToString();

                Match match = regex.Match(input);
                Assert.True(match.Success);

                Assert.Equal(builder2.ToString(), match.Value);
                Assert.Equal(invalidCharLength, match.Index);
                Assert.Equal(validCharLength, match.Length);

                match = match.NextMatch();
                Assert.False(match.Success);
            }
        }

        [OuterLoop("May take tens of seconds due to large number of cultures tested")]
        [Fact]
        public void ValidateCategoriesParticipatingInCaseConversion()
        {
            // Some optimizations in RegexCompiler rely on only some Unicode categories participating
            // in case conversion.  If this test ever fails, that optimization needs to be revisited,
            // as our assumptions about the Unicode spec may have been invalidated.

            var nonParticipatingCategories = new HashSet<UnicodeCategory>()
            {
                UnicodeCategory.ClosePunctuation,
                UnicodeCategory.ConnectorPunctuation,
                UnicodeCategory.Control,
                UnicodeCategory.DashPunctuation,
                UnicodeCategory.DecimalDigitNumber,
                UnicodeCategory.FinalQuotePunctuation,
                UnicodeCategory.InitialQuotePunctuation,
                UnicodeCategory.LineSeparator,
                UnicodeCategory.OpenPunctuation,
                UnicodeCategory.OtherNumber,
                UnicodeCategory.OtherPunctuation,
                UnicodeCategory.ParagraphSeparator,
                UnicodeCategory.SpaceSeparator,
            };

            foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.AllCultures))
            {
                using (new ThreadCultureChange(ci))
                {
                    for (int i = 0; i <= char.MaxValue; i++)
                    {
                        char ch = (char)i;
                        char upper = char.ToUpper(ch);
                        char lower = char.ToLower(ch);

                        if (nonParticipatingCategories.Contains(char.GetUnicodeCategory(ch)))
                        {
                            // If this character is in one of these categories, make sure it doesn't change case.
                            Assert.Equal(ch, upper);
                            Assert.Equal(ch, lower);
                        }
                        else
                        {
                            // If it's not in one of these categories, make sure it doesn't change case to
                            // something in one of these categories.
                            UnicodeCategory upperCategory = char.GetUnicodeCategory(upper);
                            UnicodeCategory lowerCategory = char.GetUnicodeCategory(lower);
                            Assert.False(nonParticipatingCategories.Contains(upperCategory));
                            Assert.False(nonParticipatingCategories.Contains(lowerCategory));
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task WideLatin(RegexEngine engine)
        {
            const string OrigPattern = @"abc";

            //shift each char in the pattern to the Wide-Latin alphabet of Unicode
            string pattern_WL = new string(Array.ConvertAll(OrigPattern.ToCharArray(), c => (char)(c + 0xFF00 - 32)));
            string pattern = $"({OrigPattern}==={pattern_WL})+";

            var re = await RegexHelpers.GetRegexAsync(engine, pattern, RegexOptions.IgnoreCase);
            string input = $"====={OrigPattern.ToUpper()}==={pattern_WL}{OrigPattern}==={pattern_WL.ToUpper()}==={OrigPattern}==={OrigPattern}";

            var match1 = re.Match(input);
            Assert.True(match1.Success);
            Assert.Equal(5, match1.Index);
            Assert.Equal(2 * (OrigPattern.Length + 3 + pattern_WL.Length), match1.Length);

            var match2 = match1.NextMatch();
            Assert.False(match2.Success);
        }
    }
}
