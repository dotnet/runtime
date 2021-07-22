// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Unicode;
using Microsoft.VisualBasic;
using Xunit;
using Xunit.Sdk;

namespace System.Globalization.Tests
{
    public class GraphemeBreakTest
    {
        private const char BREAK_REQUIRED = '\u00F7'; // DIVISION SIGN
        private const char BREAK_FORBIDDEN = '\u00D7'; // MULTIPLICATION SIGN

        [Fact]
        public void CompareRuntimeImplementationAgainstUnicodeTestData()
        {
            foreach ((Rune[][] clusters, string line) in GetGraphemeBreakTestData())
            {
                // Arrange

                StringBuilder inputString = new StringBuilder();
                List<Range> expectedGraphemeClusterRanges = new List<Range>();

                foreach (Rune[] cluster in clusters)
                {
                    int start = inputString.Length;
                    foreach (Rune rune in cluster)
                    {
                        inputString.Append(rune);
                    }
                    int end = inputString.Length;
                    expectedGraphemeClusterRanges.Add(start..end);
                }

                // Act & assert

                try
                {
                    RunStringInfoTestCase(inputString.ToString(), expectedGraphemeClusterRanges.ToArray());
                }
                catch (Exception ex)
                {
                    // include the failing line from the test case file for ease of debugging
                    throw new Exception("Grapheme break test failed on test case: " + line, ex);
                }
            }
        }

        private static void RunStringInfoTestCase(string input, Range[] expectedGraphemeClusterRanges)
        {
            if (expectedGraphemeClusterRanges.Length == 0)
            {
                // Handle empty inputs

                Assert.Equal(string.Empty, input); // Shouldn't have zero-length expected grapheme clusters for non-empty inputs
                Assert.Equal(0, StringInfo.GetNextTextElementLength(input));
                Assert.Equal(0, StringInfo.GetNextTextElementLength(input, 0));
                Assert.Equal(0, StringInfo.GetNextTextElementLength(input.AsSpan()));
                Assert.Equal(string.Empty, StringInfo.GetNextTextElement(input));
                Assert.Equal(string.Empty, StringInfo.GetNextTextElement(input, 0));

                StringInfo si = new StringInfo(input);
                Assert.Equal(0, si.LengthInTextElements);

                TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(input);
                Assert.False(enumerator.MoveNext());
            }
            else
            {
                // Handle non-empty inputs

                // ParseCombiningCharacters returns the offset of each grapheme cluster start

                int[] combiningCharOffsets = StringInfo.ParseCombiningCharacters(input);
                Assert.Equal(
                    expected: expectedGraphemeClusterRanges.Select(range => range.GetOffsetAndLength(input.Length).Offset).ToArray(),
                    actual: combiningCharOffsets);

                // GetNextTextElement[Length] returns the substring [length] of each grapheme cluster

                foreach (Range range in expectedGraphemeClusterRanges)
                {
                    string expected = input[range];

                    Assert.Equal(expected, StringInfo.GetNextTextElement(input[range.Start..]));
                    Assert.Equal(expected, StringInfo.GetNextTextElement(input, range.GetOffsetAndLength(input.Length).Offset));
                    Assert.Equal(expected.Length, StringInfo.GetNextTextElementLength(input[range.Start..]));
                    Assert.Equal(expected.Length, StringInfo.GetNextTextElementLength(input, range.GetOffsetAndLength(input.Length).Offset));
                    Assert.Equal(expected.Length, StringInfo.GetNextTextElementLength(input.AsSpan()[range]));
                }

                // StringInfo.LengthInTextElements returns the total grapheme cluster count

                Assert.Equal(expectedGraphemeClusterRanges.Length, new StringInfo(input).LengthInTextElements);

                // TextElementEnumerator returns an enumerator over each grapheme cluster

                for (int i = 0; i < expectedGraphemeClusterRanges.Length; i++)
                {
                    Span<Range> remainingRanges = expectedGraphemeClusterRanges.AsSpan(i);

                    int baseOffset = remainingRanges[0].GetOffsetAndLength(input.Length).Offset;
                    TextElementEnumerator enumerator1 = StringInfo.GetTextElementEnumerator(input[baseOffset..]);
                    TextElementEnumerator enumerator2 = StringInfo.GetTextElementEnumerator(input, baseOffset);

                    foreach (Range innerRange in remainingRanges)
                    {
                        Assert.True(enumerator1.MoveNext()); // input string has already been substringed
                        Assert.True(enumerator2.MoveNext()); // input string has been fully provided; enumerator has substringed

                        string expectedSubstring = input[innerRange];
                        int expectedRelativeOffset = innerRange.GetOffsetAndLength(input.Length).Offset - baseOffset;

                        Assert.Equal(expectedRelativeOffset, enumerator1.ElementIndex);
                        Assert.Equal(expectedRelativeOffset, enumerator2.ElementIndex);

                        Assert.Equal(expectedSubstring, enumerator1.Current);
                        Assert.Equal(expectedSubstring, enumerator1.GetTextElement());

                        Assert.Equal(expectedSubstring, enumerator2.Current);
                        Assert.Equal(expectedSubstring, enumerator2.GetTextElement());
                    }

                    Assert.False(enumerator1.MoveNext());
                    Assert.False(enumerator2.MoveNext());
                }
            }
        }

        [Fact]
        public void VisualBasicReverseString()
        {
            foreach ((Rune[][] clusters, string line) in GetGraphemeBreakTestData())
            {
                // Arrange

                string forwardActual = string.Concat(clusters.SelectMany(cluster => cluster).Select(rune => rune.ToString()));
                string reverseExpected = string.Concat(clusters.Reverse().SelectMany(cluster => cluster).Select(rune => rune.ToString()));

                // Act

                string reverseActual = Strings.StrReverse(forwardActual);

                // Assert

                if (reverseExpected != reverseActual)
                {
                    throw new AssertActualExpectedException(
                        expected: PrintCodePointsForDebug(reverseExpected),
                        actual: PrintCodePointsForDebug(reverseActual),
                        userMessage: "Grapheme break test failed on test case: " + line);
                }
            }
        }

        [Fact]
        public void ReplacementCharHasTypeOther()
        {
            // We rely on U+FFFD REPLACEMENT CHARACTER having certain properties
            // (such as a grapheme boundary property of "Other-XX"), since unpaired
            // UTF-16 surrogate code points and other ill-formed subsequences are normalized
            // to this character. If we ingest a new version of the UCD files where this
            // has been changed, we probably need to update the logic in StringInfo
            // and related types.

            Assert.Equal(GraphemeClusterBreakProperty.Other, UnicodeData.GetData('\ud800').GraphemeClusterBreakProperty);
        }

        private static IEnumerable<(Rune[][] clusters, string line)> GetGraphemeBreakTestData()
        {
            using Stream stream = typeof(GraphemeBreakTest).Assembly.GetManifestResourceStream("GraphemeBreakTest.txt");
            using StreamReader reader = new StreamReader(stream);

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                // Skip blank or comment-only lines

                if (string.IsNullOrEmpty(line) || line[0] == '#')
                {
                    continue;
                }

                // Line has format "÷ (XXXX (× YYYY)* ÷)+ # <comment>"
                // We'll yield return a Rune[][], representing a collection of clusters, where each cluster contains a collection of Runes.
                //
                // Example: "÷ AAAA ÷ BBBB × CCCC × DDDD ÷ EEEE × FFFF ÷ # <comment>"
                // -> [ [ AAAA ], [ BBBB, CCCC, DDDD ], [ EEEE, FFFF ] ]
                //
                // We also return the line for ease of debugging any test failures.

                string[] clusters = line[..line.IndexOf('#')].Trim().Split(BREAK_REQUIRED, StringSplitOptions.RemoveEmptyEntries);

                yield return (Array.ConvertAll(clusters, cluster =>
                {
                    string[] scalarsWithinClusterAsStrings = cluster.Split(BREAK_FORBIDDEN, StringSplitOptions.RemoveEmptyEntries);
                    uint[] scalarsWithinClusterAsUInt32s = Array.ConvertAll(scalarsWithinClusterAsStrings, scalar => uint.Parse(scalar, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                    Rune[] scalarsWithinClusterAsRunes = Array.ConvertAll(scalarsWithinClusterAsUInt32s, scalar => new Rune(scalar));
                    return scalarsWithinClusterAsRunes;
                }), line);
            }
        }

        // Given a sequence of UTF-16 code points, prints them in "[ XXXX YYYY ZZZZ ]" form, combining surrogates where possible
        private static string PrintCodePointsForDebug(IEnumerable<char> input)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[ ");

            IEnumerator<char> enumerator = input.GetEnumerator();
            while (enumerator.MoveNext())
            {
            SawStandaloneChar:
                char thisChar = enumerator.Current;

                if (!char.IsHighSurrogate(thisChar) || !enumerator.MoveNext())
                {
                    // not a high surrogate, or a high surrogate at the end of the sequence - it goes as-is
                    sb.Append($"{(uint)thisChar:X4} ");
                }
                else
                {
                    char secondChar = enumerator.Current;
                    if (!char.IsLowSurrogate(secondChar))
                    {
                        // previous char was a standalone high surrogate char - send it as-is
                        sb.Append($"{(uint)thisChar:X4} ");
                        goto SawStandaloneChar;
                    }
                    else
                    {
                        // surrogate pair - extract supplementary code point
                        sb.Append($"{(uint)char.ConvertToUtf32(thisChar, secondChar):X4} ");
                    }
                }
            }

            sb.Append("]");
            return sb.ToString();
        }
    }
}
