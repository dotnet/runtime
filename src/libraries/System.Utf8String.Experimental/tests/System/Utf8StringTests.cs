// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

using static System.Tests.Utf8TestUtilities;

namespace System.Tests
{
    public unsafe partial class Utf8StringTests
    {
        [Fact]
        public static void Empty_HasLengthZero()
        {
            Assert.Equal(0, Utf8String.Empty.Length);
            SpanAssert.Equal(ReadOnlySpan<byte>.Empty, Utf8String.Empty.AsBytes());
        }

        [Fact]
        public static void Empty_ReturnsSingleton()
        {
            Assert.Same(Utf8String.Empty, Utf8String.Empty);
        }

        [Theory]
        [InlineData(null, null, true)]
        [InlineData("", null, false)]
        [InlineData(null, "", false)]
        [InlineData("hello", null, false)]
        [InlineData(null, "hello", false)]
        [InlineData("hello", "hello", true)]
        [InlineData("hello", "Hello", false)]
        [InlineData("hello there", "hello", false)]
        public static void Equality_Ordinal(string aString, string bString, bool expected)
        {
            Utf8String a = u8(aString);
            Utf8String b = u8(bString);

            // Operators

            Assert.Equal(expected, a == b);
            Assert.NotEqual(expected, a != b);

            // Static methods

            Assert.Equal(expected, Utf8String.Equals(a, b));
            Assert.Equal(expected, Utf8String.Equals(a, b, StringComparison.Ordinal));

            // Instance methods

            if (a != null)
            {
                Assert.Equal(expected, a.Equals((object)b));
                Assert.Equal(expected, a.Equals(b));
                Assert.Equal(expected, a.Equals(b, StringComparison.Ordinal));
            }
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
                Utf8String ustr = new Utf8String(new byte[i]);

                Assert.True(seenHashCodes.Add(ustr.GetHashCode()), "This hash code was previously seen.");
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
                Utf8String ustr = u8("ababaaAA");

                Assert.Equal(ustr[0..2].GetHashCode(StringComparison.Ordinal), ustr[2..4].GetHashCode(StringComparison.Ordinal));
                Assert.NotEqual(ustr[4..6].GetHashCode(StringComparison.Ordinal), ustr[6..8].GetHashCode(StringComparison.Ordinal));
                Assert.Equal(Utf8String.Empty.GetHashCode(StringComparison.Ordinal), ustr[^0..].GetHashCode(StringComparison.Ordinal));
            }

            // OrdinalIgnoreCase

            {
                Utf8String ustr = u8("ababaaAA");

                Assert.Equal(ustr[0..2].GetHashCode(StringComparison.OrdinalIgnoreCase), ustr[2..4].GetHashCode(StringComparison.OrdinalIgnoreCase));
                Assert.Equal(ustr[4..6].GetHashCode(StringComparison.OrdinalIgnoreCase), ustr[6..8].GetHashCode(StringComparison.OrdinalIgnoreCase));
                Assert.NotEqual(ustr[0..2].GetHashCode(StringComparison.OrdinalIgnoreCase), ustr[6..8].GetHashCode(StringComparison.OrdinalIgnoreCase));
                Assert.Equal(Utf8String.Empty.GetHashCode(StringComparison.OrdinalIgnoreCase), ustr[^0..].GetHashCode(StringComparison.OrdinalIgnoreCase));
            }

            // InvariantCulture

            {
                Utf8String ustr = u8("ae\u00e6AE\u00c6"); // U+00E6 = 'æ' LATIN SMALL LETTER AE, U+00E6 = 'Æ' LATIN CAPITAL LETTER AE

                Assert.Equal(ustr[0..2].GetHashCode(StringComparison.InvariantCulture), ustr[2..4].GetHashCode(StringComparison.InvariantCulture));
                Assert.NotEqual(ustr[0..2].GetHashCode(StringComparison.InvariantCulture), ustr[4..6].GetHashCode(StringComparison.InvariantCulture));
                Assert.Equal(Utf8String.Empty.GetHashCode(StringComparison.InvariantCulture), ustr[^0..].GetHashCode(StringComparison.InvariantCulture));
            }

            // InvariantCultureIgnoreCase

            {
                Utf8String ustr = u8("ae\u00e6AE\u00c6EA");

                Assert.Equal(ustr[0..2].GetHashCode(StringComparison.InvariantCultureIgnoreCase), ustr[2..4].GetHashCode(StringComparison.InvariantCultureIgnoreCase));
                Assert.Equal(ustr[0..2].GetHashCode(StringComparison.InvariantCultureIgnoreCase), ustr[6..8].GetHashCode(StringComparison.InvariantCultureIgnoreCase));
                Assert.NotEqual(ustr[0..2].GetHashCode(StringComparison.InvariantCultureIgnoreCase), ustr[8..10].GetHashCode(StringComparison.InvariantCultureIgnoreCase));
                Assert.Equal(Utf8String.Empty.GetHashCode(StringComparison.InvariantCultureIgnoreCase), ustr[^0..].GetHashCode(StringComparison.InvariantCultureIgnoreCase));
            }

            // Invariant culture should not match Turkish I case conversion

            {
                Utf8String ustr = u8("i\u0130"); // U+0130 = 'İ' LATIN CAPITAL LETTER I WITH DOT ABOVE

                Assert.NotEqual(ustr[0..1].GetHashCode(StringComparison.InvariantCultureIgnoreCase), ustr[1..3].GetHashCode(StringComparison.InvariantCultureIgnoreCase));
            }

            // CurrentCulture (we'll use tr-TR)

            RunOnDedicatedThread(() =>
            {
                Utf8String ustr = u8("i\u0131\u0130Ii\u0131\u0130I"); // U+0131 = 'ı' LATIN SMALL LETTER DOTLESS I

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");

                Assert.Equal(ustr[0..6].GetHashCode(StringComparison.CurrentCulture), ustr[6..12].GetHashCode(StringComparison.CurrentCulture));
                Assert.NotEqual(ustr[0..1].GetHashCode(StringComparison.CurrentCulture), ustr[1..3].GetHashCode(StringComparison.CurrentCulture));
                Assert.Equal(Utf8String.Empty.GetHashCode(StringComparison.CurrentCulture), ustr[^0..].GetHashCode(StringComparison.CurrentCulture));
            });

            // CurrentCultureIgnoreCase (we'll use tr-TR)

            RunOnDedicatedThread(() =>
            {
                Utf8String ustr = u8("i\u0131\u0130Ii\u0131\u0130I");

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");

                Assert.Equal(ustr[0..6].GetHashCode(StringComparison.CurrentCultureIgnoreCase), ustr[6..12].GetHashCode(StringComparison.CurrentCultureIgnoreCase));
                Assert.NotEqual(ustr[0..1].GetHashCode(StringComparison.CurrentCultureIgnoreCase), ustr[1..3].GetHashCode(StringComparison.CurrentCultureIgnoreCase)); // 'i' shouldn't match 'ı'
                Assert.Equal(ustr[0..1].GetHashCode(StringComparison.CurrentCultureIgnoreCase), ustr[3..5].GetHashCode(StringComparison.CurrentCultureIgnoreCase)); // 'i' should match 'İ'
                Assert.NotEqual(ustr[0..1].GetHashCode(StringComparison.CurrentCultureIgnoreCase), ustr[5..6].GetHashCode(StringComparison.CurrentCultureIgnoreCase)); // 'i' shouldn't match 'I'
                Assert.Equal(Utf8String.Empty.GetHashCode(StringComparison.CurrentCultureIgnoreCase), ustr[^0..].GetHashCode(StringComparison.CurrentCultureIgnoreCase));
            });
        }

        [Fact]
        public static void GetPinnableReference_CalledMultipleTimes_ReturnsSameValue()
        {
            var utf8 = u8("Hello!");

            fixed (byte* pA = utf8)
            fixed (byte* pB = utf8)
            {
                Assert.True(pA == pB);
            }
        }

        [Fact]
        public static void GetPinnableReference_Empty()
        {
            fixed (byte* pStr = Utf8String.Empty)
            {
                Assert.True(pStr != null);
                Assert.Equal((byte)0, *pStr); // should point to null terminator
            }
        }

        [Fact]
        public static void GetPinnableReference_NotEmpty()
        {
            fixed (byte* pStr = u8("Hello!"))
            {
                Assert.True(pStr != null);

                Assert.Equal((byte)'H', pStr[0]);
                Assert.Equal((byte)'e', pStr[1]);
                Assert.Equal((byte)'l', pStr[2]);
                Assert.Equal((byte)'l', pStr[3]);
                Assert.Equal((byte)'o', pStr[4]);
                Assert.Equal((byte)'!', pStr[5]);
                Assert.Equal((byte)'\0', pStr[6]);
            }
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("\r\n", false)]
        [InlineData("not empty", false)]
        public static void IsNullOrEmpty(string value, bool expectedIsNullOrEmpty)
        {
            Assert.Equal(expectedIsNullOrEmpty, Utf8String.IsNullOrEmpty(u8(value)));
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData(" \u2028\u2029\t\v", true)]
        [InlineData(" x\r\n", false)]
        [InlineData("\r\nhello\r\n", false)]
        [InlineData("\r\n\0\r\n", false)]
        [InlineData("\r\n\r\n", true)]
        public static void IsNullOrWhiteSpace(string input, bool expected)
        {
            Assert.Equal(expected, Utf8String.IsNullOrWhiteSpace(u8(input)));
        }

        [Fact]
        public static void ToByteArray_Empty()
        {
            Assert.Same(Array.Empty<byte>(), Utf8String.Empty.ToByteArray());
        }

        [Fact]
        public static void ToByteArray_NotEmpty()
        {
            Assert.Equal(new byte[] { (byte)'H', (byte)'i' }, u8("Hi").ToByteArray());
        }

        [Fact]
        public static void ToCharArray_NotEmpty()
        {
            Assert.Equal("Hi".ToCharArray(), u8("Hi").ToCharArray());
        }

        [Fact]
        public static void ToCharArray_Empty()
        {
            Assert.Same(Array.Empty<char>(), Utf8String.Empty.ToCharArray());
        }

        [Theory]
        [InlineData("")]
        [InlineData("Hello!")]
        public static void ToString_ReturnsUtf16(string value)
        {
            Assert.Equal(value, u8(value).ToString());
        }

        [Theory]
        [InlineData("Hello", "6..")]
        [InlineData("Hello", "3..7")]
        [InlineData("Hello", "2..1")]
        [InlineData("Hello", "^10..")]
        public static void Indexer_Range_ArgOutOfRange_Throws(string strAsUtf16, string rangeAsString)
        {
            Utf8String utf8String = u8(strAsUtf16);
            Range range = ParseRangeExpr(rangeAsString);

            Assert.Throws<ArgumentOutOfRangeException>(() => utf8String[range]);
        }

        [Fact]
        public static void Indexer_Range_Success()
        {
            Utf8String utf8String = u8("Hello\u0800world.");

            Assert.Equal(u8("Hello"), utf8String[..5]);
            Assert.Equal(u8("world."), utf8String[^6..]);
            Assert.Equal(u8("o\u0800w"), utf8String[4..9]);

            Assert.Same(utf8String, utf8String[..]); // don't allocate new instance if slicing to entire string
            Assert.Same(Utf8String.Empty, utf8String[1..1]); // don't allocare new zero-length string instane
            Assert.Same(Utf8String.Empty, utf8String[6..6]); // ok to have a zero-length slice within a multi-byte sequence
        }

        [Fact]
        public static void Indexer_Range_TriesToSplitMultiByteSequence_Throws()
        {
            Utf8String utf8String = u8("Hello\u0800world.");

            Assert.Throws<InvalidOperationException>(() => utf8String[..6]);
            Assert.Throws<InvalidOperationException>(() => utf8String[6..]);
            Assert.Throws<InvalidOperationException>(() => utf8String[7..8]);
        }
    }
}
