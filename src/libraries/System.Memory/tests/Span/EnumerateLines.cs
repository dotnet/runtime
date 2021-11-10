// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        // newline chars given by Unicode Standard, Sec. 5.8, Recommendation R4 and Table 5-2
        public static IEnumerable<object[]> NewLineChars => new object[][]
        {
            new object[] { '\r' },
            new object[] { '\n' },
            new object[] { '\f' },
            new object[] { '\u0085' },
            new object[] { '\u2028' },
            new object[] { '\u2029' },
        };

        [Fact]
        public static void EnumerateLines_Empty()
        {
            // Enumerations over empty inputs should return a single empty element

            var enumerator = Span<char>.Empty.EnumerateLines().GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal("", enumerator.Current.ToString());
            Assert.False(enumerator.MoveNext());

            enumerator = ReadOnlySpan<char>.Empty.EnumerateLines().GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal("", enumerator.Current.ToString());
            Assert.False(enumerator.MoveNext());
        }

        [Theory]
        [InlineData(null, new[] { ".." })]
        [InlineData("", new[] { ".." })]
        [InlineData("abc", new[] { ".." })]
        [InlineData("<CR>", new[] { "..0", "1.." })] // empty sequences before and after the CR
        [InlineData("<CR><CR>", new[] { "0..0", "1..1", "2..2" })] // empty sequences before and after the CR (CR doesn't consume CR)
        [InlineData("<CR><LF>", new[] { "..0", "^0.." })] // CR should swallow any LF which follows
        [InlineData("a<CR><LF><LF>z", new[] { "..1", "3..3", "4.." })] // CR should swallow only a single LF which follows
        [InlineData("a<CR>b<LF>c", new[] { "..1", "2..3", "4.." })] // CR shouldn't swallow anything other than LF
        [InlineData("aa<CR>bb<LF><CR>cc", new[] { "..2", "3..5", "6..6", "7.." })] // LF shouldn't swallow CR which follows
        [InlineData("a<CR>b<VT>c<LF>d<NEL>e<FF>f<PS>g<LS>h", new[] { "..1", "2..5", "6..7", "8..9", "10..11", "12..13", "14.." })] // VT not recognized as NLF
        [InlineData("xyz<NEL>", new[] { "..3", "^0.." })] // sequence at end produces empty string
        [InlineData("<NEL>xyz", new[] { "..0", "^3.." })] // sequence at beginning produces empty string
        [InlineData("abc<NAK>%def", new[] { ".." })] // we don't recognize EBCDIC encodings for LF (see Unicode Standard, Sec. 5.8, Table 5-1)
        public static void EnumerateLines_Battery(string input, string[] expectedRanges)
        {
            // This test is similar to the string.ReplaceLineEndings test, but it checks ranges instead of substrings,
            // as we want to ensure that the method under test points to very specific slices within the original input string.

            input = FixupSequences(input);
            Range[] expectedRangesNormalized = expectedRanges.Select(element =>
            {
                Range parsed = ParseRange(element);
                (int actualOffset, int actualLength) = parsed.GetOffsetAndLength(input?.Length ?? 0);
                return actualOffset..(actualOffset + actualLength);
            }).ToArray();

            List<Range> actualRangesNormalized = new List<Range>();
            foreach (ReadOnlySpan<char> line in input.AsSpan().EnumerateLines())
            {
                actualRangesNormalized.Add(GetNormalizedRangeFromSubspan(input, line));
            }

            Assert.Equal(expectedRangesNormalized, actualRangesNormalized);

            static unsafe Range GetNormalizedRangeFromSubspan<T>(ReadOnlySpan<T> outer, ReadOnlySpan<T> inner)
            {
                // We can't use MemoryExtensions.Overlaps because it doesn't handle empty spans in the way we need.

                ref T refOuter = ref MemoryMarshal.GetReference(outer);
                ref T refInner = ref MemoryMarshal.GetReference(inner);

                fixed (byte* pOuterStart = &Unsafe.As<T, byte>(ref refOuter))
                fixed (byte* pInnerStart = &Unsafe.As<T, byte>(ref refInner))
                {
                    byte* pOuterEnd = pOuterStart + (uint)outer.Length * (nuint)Unsafe.SizeOf<T>();
                    byte* pInnerEnd = pInnerStart + (uint)inner.Length * (nuint)Unsafe.SizeOf<T>();

                    Assert.True(pOuterStart <= pInnerStart && pInnerStart <= pOuterEnd, "Inner span begins outside outer span.");
                    Assert.True(pOuterStart <= pInnerEnd && pInnerEnd <= pOuterEnd, "Inner span ends outside outer span.");

                    nuint byteOffset = (nuint)(pInnerStart - pOuterStart);
                    Assert.Equal((nuint)0, byteOffset % (nuint)Unsafe.SizeOf<T>()); // Unaligned elements; cannot compute offset
                    nuint elementOffset = byteOffset / (nuint)Unsafe.SizeOf<T>();
                    return checked((int)elementOffset)..checked((int)elementOffset + inner.Length);
                }
            }

            static string FixupSequences(string input)
            {
                // We use <XYZ> markers so that the original strings show up better in the xunit test runner
                // <VT> is included as a negative test; we *do not* want ReplaceLineEndings to honor it

                if (input is null) { return null; }
                return input.Replace("<CR>", "\r")
                    .Replace("<LF>", "\n")
                    .Replace("<VT>", "\v")
                    .Replace("<FF>", "\f")
                    .Replace("<NAK>", "\u0015")
                    .Replace("<NEL>", "\u0085")
                    .Replace("<LS>", "\u2028")
                    .Replace("<PS>", "\u2029");
            }

            static Range ParseRange(string input)
            {
                var idxOfDots = input.IndexOf("..", StringComparison.Ordinal);
                if (idxOfDots < 0) { throw new ArgumentException(); }

                ReadOnlySpan<char> begin = input.AsSpan(0, idxOfDots).Trim();
                Index beginIdx = (begin.IsEmpty) ? Index.Start : ParseIndex(begin);
                ReadOnlySpan<char> end = input.AsSpan(idxOfDots + 2).Trim();
                Index endIdx = (end.IsEmpty) ? Index.End : ParseIndex(end);
                return beginIdx..endIdx;

                static Index ParseIndex(ReadOnlySpan<char> input)
                {
                    bool fromEnd = false;
                    if (!input.IsEmpty && input[0] == '^') { fromEnd = true; input = input.Slice(1); }
                    return new Index(int.Parse(input, NumberStyles.Integer, CultureInfo.InvariantCulture), fromEnd);
                }
            }
        }

        [Theory]
        [MemberData(nameof(NewLineChars))]
        public static void EnumerateLines_EnumerationIsNotPolynomialComplexity(char newlineChar)
        {
            // This test ensures that the complexity of any call to MoveNext is O(i), where i is the
            // index of the first occurrence of any NLF within the span; rather than O(n), where
            // n is the length of the span. See comments in SpanLineEnumerator.MoveNext and
            // string.IndexOfNewlineChar for more information.
            //
            // We test this by utilizing the BoundedMemory infrastructure to allocate a poison page
            // after the scratch buffer, then we intentionally use MemoryMarshal to manipulate the
            // scratch buffer so that it extends into the poison page. If the runtime skips the first
            // occurrence of the newline char and attempts to read all the way to the end of the span,
            // this will manifest as an AV within this unit test.

            using var boundedMem = BoundedMemory.Allocate<char>(4096, PoisonPagePlacement.After);
            Span<char> span = boundedMem.Span;
            span.Fill('a');
            span[512] = newlineChar;
            boundedMem.MakeReadonly();

            span = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(span), span.Length + 4096);

            var enumerator = span.EnumerateLines().GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(512, enumerator.Current.Length);

            enumerator = ((ReadOnlySpan<char>)span).EnumerateLines().GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(512, enumerator.Current.Length);
        }
    }
}
