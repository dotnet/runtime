// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Buffers.Text.Tests
{
    public partial class AsciiUnitTests
    {
        public static IEnumerable<(string input, Range trimmedRange)> TrimBytesTestData()
        {
            yield return (input: string.Empty, trimmedRange: ..);
            yield return (input: "hello", trimmedRange: ..);
            yield return (input: " hello ", trimmedRange: 1..^1);
            yield return (input: " \rhello \n", trimmedRange: 2..^2);
            yield return (input: " \rhello\u00A0\n", trimmedRange: 2..^1); // U+00A0 is whitespace but isn't trimmed since it's not ASCII
            yield return (input: "\0hello\0", trimmedRange: ..); // U+0000 shouldn't count as whitespace
            yield return (input: "\t\n\v\f\r hello \r\f\v\n\t", trimmedRange: 6..^6);
            yield return (input: "\t\n\v\f\r hello \r\f\0\v\n\t", trimmedRange: 6..^3); // U+0000 shouldn't count as whitespace
        }

        public static IEnumerable<(string input, Range trimmedRange)> TrimCharsTestData()
        {
            yield return (input: "\r\u2028hello\n\u2029\n", trimmedRange: 1..^1); // U+2028 and U+2029 are whitespace but aren't trimmed since not ASCII
        }

        [Theory]
        [TupleMemberData(nameof(TrimBytesTestData))]
        public void TrimBytes_Tests(string input, [Alias("trimmedRange")] Range expectedTrimmedRange)
        {
            using BoundedMemory<byte> mem = BoundedMemory.AllocateFromExistingData(CharsToAsciiBytesChecked(input));
            mem.MakeReadonly();

            // First, call Trim()

            Range actualTrimmedRange = Ascii.Trim(mem.Span);
            AssertExtensions.RangesEqual(mem.Span, expectedTrimmedRange, actualTrimmedRange);

            // Then call TrimStart()

            actualTrimmedRange = Ascii.TrimStart(mem.Span);
            AssertExtensions.RangesEqual(mem.Span, (expectedTrimmedRange.Start..), actualTrimmedRange);

            // Then call TrimEnd()
            // Special-case when the input contains all-whitespace data, since we want to
            // return a zero-length slice at the *beginning* of the span, not the end of the span.

            actualTrimmedRange = Ascii.TrimEnd(mem.Span);
            if (expectedTrimmedRange.Start.GetOffset(mem.Length) == mem.Length)
            {
                AssertExtensions.RangesEqual(mem.Span, (0..0), actualTrimmedRange);
            }
            else
            {
                AssertExtensions.RangesEqual(mem.Span, (..expectedTrimmedRange.End), actualTrimmedRange);
            }
        }

        [Theory]
        [TupleMemberData(nameof(TrimBytesTestData))]
        [TupleMemberData(nameof(TrimCharsTestData))]
        public void TrimChars_Tests(string input, [Alias("trimmedRange")] Range expectedTrimmedRange)
        {
            using BoundedMemory<char> mem = BoundedMemory.AllocateFromExistingData<char>(input);
            mem.MakeReadonly();

            // First, call Trim()

            Range actualTrimmedRange = Ascii.Trim(mem.Span);
            AssertExtensions.RangesEqual(mem.Span, expectedTrimmedRange, actualTrimmedRange);

            // Then call TrimStart()

            actualTrimmedRange = Ascii.TrimStart(mem.Span);
            AssertExtensions.RangesEqual(mem.Span, (expectedTrimmedRange.Start..), actualTrimmedRange);

            // Then call TrimEnd()
            // Special-case when the input contains all-whitespace data, since we want to
            // return a zero-length slice at the *beginning* of the span, not the end of the span.

            actualTrimmedRange = Ascii.TrimEnd(mem.Span);
            if (expectedTrimmedRange.Start.GetOffset(mem.Length) == mem.Length)
            {
                AssertExtensions.RangesEqual(mem.Span, (0..0), actualTrimmedRange);
            }
            else
            {
                AssertExtensions.RangesEqual(mem.Span, (..expectedTrimmedRange.End), actualTrimmedRange);
            }
        }
    }
}
