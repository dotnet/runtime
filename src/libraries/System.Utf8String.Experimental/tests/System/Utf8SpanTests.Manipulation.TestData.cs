// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Tests;

using static System.Tests.Utf8TestUtilities;

using ustring = System.Utf8String;

namespace System.Text.Tests
{
    public partial class Utf8SpanTests
    {
        public static IEnumerable<object[]> SplitData_CharSeparator()
        {
            foreach (SplitTestData entry in SplitData_All())
            {
                if (!TryParseSearchTermAsChar(entry.SearchTerm, out char searchChar))
                {
                    continue;
                }

                yield return new object[]
                {
                    entry.Source,
                    searchChar,
                    entry.ExpectedRanges
                };
            }
        }

        public static IEnumerable<object[]> SplitData_RuneSeparator()
        {
            foreach (SplitTestData entry in SplitData_All())
            {
                if (!TryParseSearchTermAsRune(entry.SearchTerm, out Rune searchRune))
                {
                    continue;
                }

                yield return new object[]
                {
                    entry.Source,
                    searchRune,
                    entry.ExpectedRanges
                };
            }
        }

        public static IEnumerable<object[]> SplitData_Utf8SpanSeparator()
        {
            foreach (SplitTestData entry in SplitData_All())
            {
                if (!TryParseSearchTermAsUtf8String(entry.SearchTerm, out ustring searchTerm))
                {
                    continue;
                }

                yield return new object[]
                {
                    entry.Source,
                    searchTerm,
                    entry.ExpectedRanges
                };
            }
        }

        private static IEnumerable<SplitTestData> SplitData_All()
        {
            SplitTestData[] testDataEntries = new SplitTestData[]
            {
                new SplitTestData
                {
                    // Empty source, searching for anything results in no match
                    Source = null,
                    SearchTerm = '\0',
                    ExpectedRanges = new[] { 0..0 }
                },
                new SplitTestData
                {
                    // If no match, then return original span
                    Source = u8("Hello"),
                    SearchTerm = 'x',
                    ExpectedRanges = new[] { Range.All }
                },
                new SplitTestData
                {
                    // Match returns multiple spans (some may be empty)
                    Source = u8("Hello"),
                    SearchTerm = 'l',
                    ExpectedRanges = new[] { 0..2, 3..3, 4..5 }
                },
                new SplitTestData
                {
                    // Match returns multiple spans (non-empty)
                    Source = u8("Hello"),
                    SearchTerm = "ell",
                    ExpectedRanges = new[] { 0..1, ^1.. }
                },
                new SplitTestData
                {
                    // Match returns multiple spans (non-empty, with whitespace)
                    Source = u8("aax aaa xxax \u2028\u2029"), // includes LS, PS as whitespace
                    SearchTerm = 'x',
                    ExpectedRanges = new[] { 0..2, 3..8, 9..9, 10..11, 12.. }
                },
                new SplitTestData
                {
                    // Matching on U+1F600 GRINNING FACE (with whitespace)
                    Source = u8("x \U0001F600 y"),
                    SearchTerm = new Rune(0x1F600),
                    ExpectedRanges = new[] { 0..2, ^2.. }
                },
            };

            return testDataEntries;
        }

        public class SplitTestData
        {
            public ustring Source;
            public object SearchTerm;
            public Range[] ExpectedRanges;
        }
    }
}
