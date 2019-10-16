// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Tests;
using Xunit;

using static System.Tests.Utf8TestUtilities;

using ustring = System.Utf8String;

namespace System.Text.Tests
{
    public unsafe partial class Utf8SpanTests
    {
        [Fact]
        public static void BytesProperty_FromCustomBytes()
        {
            byte[] bytes = Encoding.UTF8.GetBytes("Hello!");
            Assert.True(bytes.AsSpan() == Utf8Span.UnsafeCreateWithoutValidation(bytes).Bytes);
        }

        [Fact]
        public static void BytesProperty_FromEmpty()
        {
            Assert.True(Utf8Span.Empty.Bytes == ReadOnlySpan<byte>.Empty);
        }

        [Fact]
        public static void BytesProperty_FromUtf8String()
        {
            ustring ustr = u8("Hello!");
            Utf8Span uspan = new Utf8Span(ustr);

            Assert.True(ustr.AsBytes() == uspan.Bytes);
        }

        [Fact]
        public static void EmptyProperty()
        {
            // Act

            Utf8Span span = Utf8Span.Empty;

            // Assert
            // GetPinnableReference should be 'null' to match behavior of empty ROS<byte>.GetPinnableReference();

            Assert.True(span.IsEmpty);
            Assert.Equal(IntPtr.Zero, (IntPtr)(void*)Unsafe.AsPointer(ref Unsafe.AsRef(in span.GetPinnableReference())));
            Assert.Equal(IntPtr.Zero, (IntPtr)(void*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span.Bytes)));
            Assert.Equal(0, span.Length);
        }

        [Fact]
        public static void GetHashCode_Ordinal()
        {
            // Generate 17 all-null strings and make sure they have unique hash codes.
            // Assuming Marvin32 is a good PRF and has a full 32-bit output domain, we should
            // expect this test to fail only once every ~30 million runs due to the birthday paradox.
            // That should be good enough for inclusion as a unit test.

            HashSet<int> seenHashCodes = new HashSet<int>();

            for (int i = 0; i <= 16; i++)
            {
                using BoundedUtf8Span boundedSpan = new BoundedUtf8Span(new byte[i]);
                Utf8Span span = boundedSpan.Span;

                Assert.True(seenHashCodes.Add(span.GetHashCode()), "This hash code was previously seen.");
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void GetHashCode_WithComparison()
        {
            // Since hash code generation is randomized, it's possible (though unlikely) that
            // we might see unanticipated collisions. It's ok if this unit test fails once in
            // every few million runs, but if the unit test becomes truly flaky then that would
            // be indicative of a larger problem with hash code generation.
            //
            // These tests also make sure that the hash code is computed over the buffer rather
            // than over the reference.

            // Ordinal

            {
                using BoundedUtf8Span boundedSpan = new BoundedUtf8Span("ababaaAA");
                Utf8Span span = boundedSpan.Span;

                Assert.Equal(span[0..2].GetHashCode(StringComparison.Ordinal), span[2..4].GetHashCode(StringComparison.Ordinal));
                Assert.NotEqual(span[4..6].GetHashCode(StringComparison.Ordinal), span[6..8].GetHashCode(StringComparison.Ordinal));
                Assert.Equal(Utf8Span.Empty.GetHashCode(StringComparison.Ordinal), span[^0..].GetHashCode(StringComparison.Ordinal)); // null should equal empty
            }

            // OrdinalIgnoreCase

            {
                using BoundedUtf8Span boundedSpan = new BoundedUtf8Span("ababaaAA");
                Utf8Span span = boundedSpan.Span;

                Assert.Equal(span[0..2].GetHashCode(StringComparison.OrdinalIgnoreCase), span[2..4].GetHashCode(StringComparison.OrdinalIgnoreCase));
                Assert.Equal(span[4..6].GetHashCode(StringComparison.OrdinalIgnoreCase), span[6..8].GetHashCode(StringComparison.OrdinalIgnoreCase));
                Assert.NotEqual(span[0..2].GetHashCode(StringComparison.OrdinalIgnoreCase), span[6..8].GetHashCode(StringComparison.OrdinalIgnoreCase));
                Assert.Equal(Utf8Span.Empty.GetHashCode(StringComparison.OrdinalIgnoreCase), span[^0..].GetHashCode(StringComparison.OrdinalIgnoreCase)); // null should equal empty
            }

            // InvariantCulture

            {
                using BoundedUtf8Span boundedSpan = new BoundedUtf8Span("ae\u00e6AE\u00c6"); // U+00E6 = 'æ' LATIN SMALL LETTER AE, U+00E6 = 'Æ' LATIN CAPITAL LETTER AE
                Utf8Span span = boundedSpan.Span;

                Assert.Equal(span[0..2].GetHashCode(StringComparison.InvariantCulture), span[2..4].GetHashCode(StringComparison.InvariantCulture));
                Assert.NotEqual(span[0..2].GetHashCode(StringComparison.InvariantCulture), span[4..6].GetHashCode(StringComparison.InvariantCulture));
                Assert.Equal(Utf8Span.Empty.GetHashCode(StringComparison.InvariantCulture), span[^0..].GetHashCode(StringComparison.InvariantCulture)); // null should equal empty
            }

            // InvariantCultureIgnoreCase

            {
                using BoundedUtf8Span boundedSpan = new BoundedUtf8Span("ae\u00e6AE\u00c6EA");
                Utf8Span span = boundedSpan.Span;

                Assert.Equal(span[0..2].GetHashCode(StringComparison.InvariantCultureIgnoreCase), span[2..4].GetHashCode(StringComparison.InvariantCultureIgnoreCase));
                Assert.Equal(span[0..2].GetHashCode(StringComparison.InvariantCultureIgnoreCase), span[6..8].GetHashCode(StringComparison.InvariantCultureIgnoreCase));
                Assert.NotEqual(span[0..2].GetHashCode(StringComparison.InvariantCultureIgnoreCase), span[8..10].GetHashCode(StringComparison.InvariantCultureIgnoreCase));
                Assert.Equal(Utf8Span.Empty.GetHashCode(StringComparison.InvariantCultureIgnoreCase), span[^0..].GetHashCode(StringComparison.InvariantCultureIgnoreCase)); // null should equal empty
            }

            // Invariant culture should not match Turkish I case conversion

            {
                using BoundedUtf8Span boundedSpan = new BoundedUtf8Span("i\u0130"); // U+0130 = 'İ' LATIN CAPITAL LETTER I WITH DOT ABOVE
                Utf8Span span = boundedSpan.Span;

                Assert.NotEqual(span[0..1].GetHashCode(StringComparison.InvariantCultureIgnoreCase), span[1..3].GetHashCode(StringComparison.InvariantCultureIgnoreCase));
            }

            // CurrentCulture (we'll use tr-TR)

            RunOnDedicatedThread(() =>
            {
                using BoundedUtf8Span boundedSpan = new BoundedUtf8Span("i\u0131\u0130Ii\u0131\u0130I"); // U+0131 = 'ı' LATIN SMALL LETTER DOTLESS I
                Utf8Span span = boundedSpan.Span;

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");

                Assert.Equal(span[0..6].GetHashCode(StringComparison.CurrentCulture), span[6..12].GetHashCode(StringComparison.CurrentCulture));
                Assert.NotEqual(span[0..1].GetHashCode(StringComparison.CurrentCulture), span[1..3].GetHashCode(StringComparison.CurrentCulture));
                Assert.Equal(Utf8Span.Empty.GetHashCode(StringComparison.CurrentCulture), span[^0..].GetHashCode(StringComparison.CurrentCulture)); // null should equal empty
            });

            // CurrentCultureIgnoreCase (we'll use tr-TR)

            RunOnDedicatedThread(() =>
            {
                using BoundedUtf8Span boundedSpan = new BoundedUtf8Span("i\u0131\u0130Ii\u0131\u0130I");
                Utf8Span span = boundedSpan.Span;

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");

                Assert.Equal(span[0..6].GetHashCode(StringComparison.CurrentCultureIgnoreCase), span[6..12].GetHashCode(StringComparison.CurrentCultureIgnoreCase));
                Assert.NotEqual(span[0..1].GetHashCode(StringComparison.CurrentCultureIgnoreCase), span[1..3].GetHashCode(StringComparison.CurrentCultureIgnoreCase)); // 'i' shouldn't match 'ı'
                Assert.Equal(span[0..1].GetHashCode(StringComparison.CurrentCultureIgnoreCase), span[3..5].GetHashCode(StringComparison.CurrentCultureIgnoreCase)); // 'i' should match 'İ'
                Assert.NotEqual(span[0..1].GetHashCode(StringComparison.CurrentCultureIgnoreCase), span[5..6].GetHashCode(StringComparison.CurrentCultureIgnoreCase)); // 'i' shouldn't match 'I'
                Assert.Equal(Utf8Span.Empty.GetHashCode(StringComparison.CurrentCultureIgnoreCase), span[^0..].GetHashCode(StringComparison.CurrentCultureIgnoreCase)); // null should equal empty
            });
        }

        [Theory]
        [InlineData("", true)]
        [InlineData("Hello", true)]
        [InlineData("\u1234", false)]
        public static void IsAscii(string input, bool expected)
        {
            using BoundedUtf8Span boundedSpan = new BoundedUtf8Span(input);

            Assert.Equal(expected, boundedSpan.Span.IsAscii());
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData(" \u2028\u2029\t\v", true)]
        [InlineData(" x\r\n", false)]
        [InlineData("\r\nhello\r\n", false)]
        [InlineData("\r\n\0\r\n", false)]
        [InlineData("\r\n\r\n", true)]
        public static void IsEmptyOrWhiteSpace(string input, bool expected)
        {
            using BoundedUtf8Span boundedSpan = new BoundedUtf8Span(input);

            Assert.Equal(expected, boundedSpan.Span.IsEmptyOrWhiteSpace());
        }

        [Theory]
        [InlineData("", "..")]
        [InlineData("Hello", "1..")]
        [InlineData("Hello", "1..2")]
        [InlineData("Hello", "1..^2")]
        [InlineData("Hello", "^2..")]
        [InlineData("Hello", "^0..")]
        [InlineData("résumé", "1..^2")] // include first 'é', exclude last 'é'
        [InlineData("résumé", "^2..")] // include only last 'é'
        public static void Indexer_Success(string input, string rangeExpression)
        {
            Range range = ParseRangeExpr(rangeExpression);

            using BoundedUtf8Span boundedSpan = new BoundedUtf8Span(input);
            Utf8Span originalSpan = boundedSpan.Span;
            Utf8Span slicedSpan = originalSpan[range]; // shouldn't throw

            ref byte startOfOriginalSpan = ref MemoryMarshal.GetReference(originalSpan.Bytes);
            ref byte startOfSlicedSpan = ref MemoryMarshal.GetReference(slicedSpan.Bytes);

            // Now ensure the slice was correctly produced by comparing the references directly.

            (int offset, int length) = range.GetOffsetAndLength(originalSpan.Length);
            Assert.True(Unsafe.AreSame(ref startOfSlicedSpan, ref Unsafe.Add(ref startOfOriginalSpan, offset)));
            Assert.Equal(length, slicedSpan.Length);
        }

        [Theory]
        [InlineData("résumé", "2..")] // try to split the first 'é'
        [InlineData("résumé", "..^1")] // try to split the last 'é'
        public static void Indexer_ThrowsIfTryToSplitMultiByteSubsequence(string input, string rangeExpression)
        {
            Range range = ParseRangeExpr(rangeExpression);

            using BoundedUtf8Span boundedSpan = new BoundedUtf8Span(input);

            Assert.Throws<InvalidOperationException>(() => { var _ = boundedSpan.Span[range]; });
        }


        [Theory]
        [MemberData(nameof(TranscodingTestData))]
        public static void ToCharArrayTest(string expected)
        {
            // Arrange

            using BoundedUtf8Span boundedSpan = new BoundedUtf8Span(expected);
            Utf8Span span = boundedSpan.Span;

            // Act

            char[] returned = span.ToCharArray();

            // Assert

            Assert.Equal(expected, returned); // IEnumerable<char>
        }

        [Fact]
        public static void ToCharArrayTest_Null()
        {
            Assert.Same(Array.Empty<char>(), Utf8Span.Empty.ToCharArray());
        }

        [Theory]
        [MemberData(nameof(TranscodingTestData))]
        public static void ToCharsTest(string expected)
        {
            // Arrange

            using BoundedUtf8Span boundedSpan = new BoundedUtf8Span(expected);
            Utf8Span span = boundedSpan.Span;

            // Act & assert, first with improperly-sized buffer

            if (expected.Length > 0)
            {
                using BoundedMemory<char> boundedMemory = BoundedMemory.Allocate<char>(expected.Length - 1);
                Assert.Equal(-1, span.ToChars(boundedMemory.Span));
            }

            // Then with properly-sized buffer and too-large buffer

            for (int i = expected.Length; i <= expected.Length + 1; i++)
            {
                using BoundedMemory<char> boundedMemory = BoundedMemory.Allocate<char>(i);
                Assert.Equal(expected.Length, span.ToChars(boundedMemory.Span));
                Assert.True(boundedMemory.Span.Slice(0, expected.Length).SequenceEqual(expected));
            }
        }

        [Fact]
        public static void ToCharsTest_Null()
        {
            for (int i = 0; i <= 1; i++) // test both with properly-sized buffer and with too-large buffer
            {
                using BoundedMemory<char> boundedMemory = BoundedMemory.Allocate<char>(i);
                Assert.Equal(0, Utf8Span.Empty.ToChars(boundedMemory.Span));
            }
        }

        [Theory]
        [MemberData(nameof(TranscodingTestData))]
        public static void ToStringTest(string expected)
        {
            // Arrange

            using BoundedUtf8Span boundedSpan = new BoundedUtf8Span(expected);
            Utf8Span span = boundedSpan.Span;

            // Act & assert

            Assert.Equal(expected, span.ToString());
        }

        [Fact]
        public static void ToStringTest_Null()
        {
            Assert.Same(string.Empty, Utf8Span.Empty.ToString());
        }

        [Theory]
        [MemberData(nameof(TranscodingTestData))]
        public static void ToUtf8StringTest(string expected)
        {
            // Arrange

            ustring utf8 = u8(expected);

            using BoundedUtf8Span boundedSpan = new BoundedUtf8Span(expected);
            Utf8Span span = boundedSpan.Span;

            // Act & assert

            Assert.Equal(utf8, span.ToUtf8String());
        }

        [Fact]
        public static void ToUtf8StringTest_Null()
        {
            Assert.Same(ustring.Empty, Utf8Span.Empty.ToUtf8String());
        }

        public static IEnumerable<object[]> TranscodingTestData()
        {
            yield return new object[] { "" }; // empty
            yield return new object[] { "Hello" }; // simple ASCII
            yield return new object[] { "a\U00000123b\U00001234c\U00101234d" }; // with multi-byte sequences of varying lengths
            yield return new object[] { "\uF8FF\uE000\U000FFFFF" }; // with scalars from the private use areas
        }
    }
}
