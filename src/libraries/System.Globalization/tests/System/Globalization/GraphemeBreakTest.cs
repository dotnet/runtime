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

                List<int> expected = new List<int>();
                StringBuilder input = new StringBuilder();

                foreach (Rune[] cluster in clusters)
                {
                    expected.Add(input.Length); // we're about to start a new cluster
                    foreach (Rune scalar in cluster)
                    {
                        input.Append(scalar);
                    }
                }

                // Act

                int[] actual = StringInfo.ParseCombiningCharacters(input.ToString());

                // Assert

                if (!expected.SequenceEqual(actual))
                {
                    throw new AssertActualExpectedException(
                        expected: expected.ToArray(),
                        actual: actual,
                        userMessage: "Grapheme break test failed on test case: " + line);
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
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0:X4} ", (uint)thisChar);
                }
                else
                {
                    char secondChar = enumerator.Current;
                    if (!char.IsLowSurrogate(secondChar))
                    {
                        // previous char was a standalone high surrogate char - send it as-is
                        sb.AppendFormat(CultureInfo.InvariantCulture, "{0:X4} ", (uint)thisChar);
                        goto SawStandaloneChar;
                    }
                    else
                    {
                        // surrogate pair - extract supplementary code point
                        sb.AppendFormat(CultureInfo.InvariantCulture, "{0:X4} ", (uint)char.ConvertToUtf32(thisChar, secondChar));
                    }
                }
            }

            sb.Append("]");
            return sb.ToString();
        }
    }
}
