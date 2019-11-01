// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Buffers;
using System.Collections.Generic;
using System.Tests;
using Xunit;

using static System.Tests.Utf8TestUtilities;

using ustring = System.Utf8String;

namespace System.Text.Tests
{
    public partial class Utf8SpanTests
    {
        private delegate Utf8Span.SplitResult Utf8SpanSplitDelegate(Utf8Span span, Utf8StringSplitOptions splitOptions);

        [Fact]
        public static void Split_EmptySearchSpan_Throws()
        {
            // Shouldn't be able to split on an empty UTF-8 span.
            // Such an enumerator would iterate forever, so we forbid it.

            var ex = Assert.Throws<ArgumentException>(() => { u8("Hello").AsSpan().Split(Utf8Span.Empty); });
            Assert.Equal("separator", ex.ParamName);
        }

        [Fact]
        public static void Split_InvalidChar_Throws()
        {
            // Shouldn't be able to split on a standalone surrogate char
            // Other search methods (TryFind) return false when given a standalone surrogate char as input,
            // but the Split methods returns a complex data structure instead of a simple bool. So to keep
            // the logic of that data structure relatively simple we'll forbid the bad char at the call site.

            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => { u8("Hello").AsSpan().Split('\ud800'); });
            Assert.Equal("separator", ex.ParamName);
        }

        [Fact]
        public static void Split_Char_NullInput()
        {
            // First, make sure that <null>.Split(',') yields a single-element [ <null> ].

            Utf8Span source = Utf8Span.Empty;

            var enumerator = source.Split(',').GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.True(source.Bytes == enumerator.Current.Bytes); // referential equality
            Assert.False(enumerator.MoveNext());

            // Next, make sure that if "remove empty entries" is specified, yields the empty set [ ].

            enumerator = source.Split(',', Utf8StringSplitOptions.RemoveEmptyEntries).GetEnumerator();
            Assert.False(enumerator.MoveNext());
        }

        [Theory]
        [MemberData(nameof(SplitData_CharSeparator))]
        public static void Split_Char(ustring source, char separator, Range[] expectedRanges)
        {
            SplitTest_Common(source, (span, splitOptions) => span.Split(separator, splitOptions), expectedRanges);
        }

        [Fact]
        public static void Split_Deconstruct()
        {
            using BoundedUtf8Span boundedSpan = new BoundedUtf8Span("a,b,c,d,e");
            Utf8Span span = boundedSpan.Span;

            // Note referential equality checks below (since we want to know exact slices
            // into the original buffer), not deep (textual) equality checks.

            {
                (Utf8Span a, Utf8Span b) = span.Split('x'); // not found
                Assert.True(a.Bytes == span.Bytes, "Expected referential equality of input.");
                Assert.True(b.Bytes == default);
            }

            {
                (Utf8Span a, Utf8Span b) = span.Split(',');
                Assert.True(a.Bytes == span.Bytes[..1]); // "a"
                Assert.True(b.Bytes == span.Bytes[2..]); // "b,c,d,e"
            }

            {
                (Utf8Span a, Utf8Span b, Utf8Span c, Utf8Span d, Utf8Span e) = span.Split(',');
                Assert.True(a.Bytes == span.Bytes[0..1]); // "a"
                Assert.True(b.Bytes == span.Bytes[2..3]); // "b"
                Assert.True(c.Bytes == span.Bytes[4..5]); // "c"
                Assert.True(d.Bytes == span.Bytes[6..7]); // "d"
                Assert.True(e.Bytes == span.Bytes[8..9]); // "e"
            }

            {
                (Utf8Span a, Utf8Span b, Utf8Span c, Utf8Span d, Utf8Span e, Utf8Span f, Utf8Span g, Utf8Span h) = span.Split(',');
                Assert.True(a.Bytes == span.Bytes[0..1]); // "a"
                Assert.True(b.Bytes == span.Bytes[2..3]); // "b"
                Assert.True(c.Bytes == span.Bytes[4..5]); // "c"
                Assert.True(d.Bytes == span.Bytes[6..7]); // "d"
                Assert.True(e.Bytes == span.Bytes[8..9]); // "e"
                Assert.True(f.Bytes == default);
                Assert.True(g.Bytes == default);
                Assert.True(h.Bytes == default);
            }
        }

        [Fact]
        public static void Split_Deconstruct_WithOptions()
        {
            using BoundedUtf8Span boundedSpan = new BoundedUtf8Span("a, , b, c,, d, e");
            Utf8Span span = boundedSpan.Span;

            // Note referential equality checks below (since we want to know exact slices
            // into the original buffer), not deep (textual) equality checks.

            {
                (Utf8Span a, Utf8Span b) = span.Split(',', Utf8StringSplitOptions.RemoveEmptyEntries);
                Assert.True(a.Bytes == span.Bytes[..1]); // "a"
                Assert.True(b.Bytes == span.Bytes[2..]); // " , b, c,, d, e"
            }

            {
                (Utf8Span a, Utf8Span x, Utf8Span b, Utf8Span c, Utf8Span d, Utf8Span e) = span.Split(',', Utf8StringSplitOptions.RemoveEmptyEntries);
                Assert.True(a.Bytes == span.Bytes[0..1]); // "a"
                Assert.True(x.Bytes == span.Bytes[2..3]); // " "
                Assert.True(b.Bytes == span.Bytes[4..6]); // " b"
                Assert.True(c.Bytes == span.Bytes[7..9]); // " c"
                Assert.True(d.Bytes == span.Bytes[11..13]); // " d"
                Assert.True(e.Bytes == span.Bytes[14..]); // " e"
            }

            {
                (Utf8Span a, Utf8Span b, Utf8Span c, Utf8Span d, Utf8Span e, Utf8Span f, Utf8Span g, Utf8Span h) = span.Split(',', Utf8StringSplitOptions.RemoveEmptyEntries | Utf8StringSplitOptions.TrimEntries);
                Assert.True(a.Bytes == span.Bytes[0..1]); // "a"
                Assert.True(b.Bytes == span.Bytes[5..6]); // "b"
                Assert.True(c.Bytes == span.Bytes[8..9]); // "c"
                Assert.True(d.Bytes == span.Bytes[12..13]); // "d"
                Assert.True(e.Bytes == span.Bytes[15..]); // "e"
                Assert.True(f.Bytes == default);
                Assert.True(g.Bytes == default);
                Assert.True(h.Bytes == default);
            }
        }

        [Theory]
        [MemberData(nameof(SplitData_RuneSeparator))]
        public static void Split_Rune(ustring source, Rune separator, Range[] expectedRanges)
        {
            SplitTest_Common(source, (span, splitOptions) => span.Split(separator, splitOptions), expectedRanges);
        }

        [Theory]
        [MemberData(nameof(SplitData_Utf8SpanSeparator))]
        public static void Split_Utf8Span(ustring source, ustring separator, Range[] expectedRanges)
        {
            SplitTest_Common(source, (span, splitOptions) => span.Split(separator.AsSpan(), splitOptions), expectedRanges);
        }

        private static void SplitTest_Common(ustring source, Utf8SpanSplitDelegate splitAction, Range[] expectedRanges)
        {
            using BoundedUtf8Span boundedSpan = new BoundedUtf8Span(source.AsBytes());
            Utf8Span span = boundedSpan.Span;
            int totalSpanLengthInBytes = span.Length;
            source = null; // to avoid inadvertently using this for the remainder of the method

            // First, run the split with default options and make sure the ranges are equivalent

            List<Range> actualRanges = new List<Range>();
            foreach (Utf8Span slice in splitAction(span, Utf8StringSplitOptions.None))
            {
                actualRanges.Add(GetRangeOfSubspan(span, slice));
            }

            Assert.Equal(expectedRanges, actualRanges, new RangeEqualityComparer(totalSpanLengthInBytes));

            // Next, run the split with empty entries removed

            actualRanges = new List<Range>();
            foreach (Utf8Span slice in splitAction(span, Utf8StringSplitOptions.RemoveEmptyEntries))
            {
                actualRanges.Add(GetRangeOfSubspan(span, slice));
            }

            Assert.Equal(expectedRanges.Where(range => !range.IsEmpty(totalSpanLengthInBytes)), actualRanges, new RangeEqualityComparer(totalSpanLengthInBytes));

            // Next, run the split with results trimmed (but allowing empty results)

            expectedRanges = (Range[])expectedRanges.Clone(); // clone the array since we're about to mutate it
            for (int i = 0; i < expectedRanges.Length; i++)
            {
                expectedRanges[i] = GetRangeOfSubspan(span, span[expectedRanges[i]].Trim());
            }

            actualRanges = new List<Range>();
            foreach (Utf8Span slice in splitAction(span, Utf8StringSplitOptions.TrimEntries))
            {
                actualRanges.Add(GetRangeOfSubspan(span, slice));
            }

            Assert.Equal(expectedRanges, actualRanges, new RangeEqualityComparer(totalSpanLengthInBytes));

            // Finally, run the split both trimmed and with empty entries removed

            actualRanges = new List<Range>();
            foreach (Utf8Span slice in splitAction(span, Utf8StringSplitOptions.TrimEntries | Utf8StringSplitOptions.RemoveEmptyEntries))
            {
                actualRanges.Add(GetRangeOfSubspan(span, slice));
            }

            Assert.Equal(expectedRanges.Where(range => !range.IsEmpty(totalSpanLengthInBytes)), actualRanges, new RangeEqualityComparer(totalSpanLengthInBytes));
        }

        [Theory]
        [MemberData(nameof(Trim_TestData))]
        public static void Trim(string input)
        {
            // Arrange

            using BoundedUtf8Span boundedSpan = new BoundedUtf8Span(input);
            Utf8Span span = boundedSpan.Span;

            // Act

            Utf8Span trimmed = span.Trim();

            // Assert
            // Compute the trim manually and ensure it matches the trimmed span's characteristics.

            ReadOnlySpan<byte> utf8Bytes = span.Bytes;
            while (!utf8Bytes.IsEmpty)
            {
                OperationStatus status = Rune.DecodeFromUtf8(utf8Bytes, out Rune decodedRune, out int bytesConsumed);
                Assert.Equal(OperationStatus.Done, status);

                if (!Rune.IsWhiteSpace(decodedRune))
                {
                    break;
                }

                utf8Bytes = utf8Bytes.Slice(bytesConsumed);
            }
            while (!utf8Bytes.IsEmpty)
            {
                OperationStatus status = Rune.DecodeLastFromUtf8(utf8Bytes, out Rune decodedRune, out int bytesConsumed);
                Assert.Equal(OperationStatus.Done, status);

                if (!Rune.IsWhiteSpace(decodedRune))
                {
                    break;
                }

                utf8Bytes = utf8Bytes[..^bytesConsumed];
            }

            Assert.True(trimmed.Bytes == utf8Bytes); // must be an exact buffer match (address + length)
        }

        [Theory]
        [MemberData(nameof(Trim_TestData))]
        public static void TrimEnd(string input)
        {
            // Arrange

            using BoundedUtf8Span boundedSpan = new BoundedUtf8Span(input);
            Utf8Span span = boundedSpan.Span;

            // Act

            Utf8Span trimmed = span.TrimEnd();

            // Assert
            // Compute the trim manually and ensure it matches the trimmed span's characteristics.

            ReadOnlySpan<byte> utf8Bytes = span.Bytes;
            while (!utf8Bytes.IsEmpty)
            {
                OperationStatus status = Rune.DecodeLastFromUtf8(utf8Bytes, out Rune decodedRune, out int bytesConsumed);
                Assert.Equal(OperationStatus.Done, status);

                if (!Rune.IsWhiteSpace(decodedRune))
                {
                    break;
                }

                utf8Bytes = utf8Bytes[..^bytesConsumed];
            }

            Assert.True(trimmed.Bytes == utf8Bytes); // must be an exact buffer match (address + length)
        }

        [Theory]
        [MemberData(nameof(Trim_TestData))]
        public static void TrimStart(string input)
        {
            // Arrange

            using BoundedUtf8Span boundedSpan = new BoundedUtf8Span(input);
            Utf8Span span = boundedSpan.Span;

            // Act

            Utf8Span trimmed = span.TrimStart();

            // Assert
            // Compute the trim manually and ensure it matches the trimmed span's characteristics.

            ReadOnlySpan<byte> utf8Bytes = span.Bytes;
            while (!utf8Bytes.IsEmpty)
            {
                OperationStatus status = Rune.DecodeFromUtf8(utf8Bytes, out Rune decodedRune, out int bytesConsumed);
                Assert.Equal(OperationStatus.Done, status);

                if (!Rune.IsWhiteSpace(decodedRune))
                {
                    break;
                }

                utf8Bytes = utf8Bytes.Slice(bytesConsumed);
            }

            Assert.True(trimmed.Bytes == utf8Bytes); // must be an exact buffer match (address + length)
        }
    }
}
