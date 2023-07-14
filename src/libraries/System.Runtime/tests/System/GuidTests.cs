// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Tests
{
    public static class GuidTests
    {
        private static readonly Guid s_testGuid = new Guid("a8a110d5-fc49-43c5-bf46-802db8f843ff");
        private static readonly Guid s_fullGuid = new Guid(uint.MaxValue, ushort.MaxValue, ushort.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

        [Fact]
        public static void Empty()
        {
            Assert.Equal(new Guid(0, 0, 0, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }), Guid.Empty);
        }

        public static IEnumerable<object[]> Ctor_ByteArray_TestData()
        {
            yield return new object[] { new byte[16], Guid.Empty };
            yield return new object[] { new byte[] { 0x44, 0x33, 0x22, 0x11, 0x66, 0x55, 0x88, 0x77, 0x99, 0x00, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF }, new Guid("11223344-5566-7788-9900-aabbccddeeff") };
            yield return new object[] { new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF }, new Guid("44332211-6655-8877-9900-aabbccddeeff") };
            yield return new object[] { s_testGuid.ToByteArray(), s_testGuid };
        }

        [Theory]
        [MemberData(nameof(Ctor_ByteArray_TestData))]
        public static void Ctor_ByteArray(byte[] b, Guid expected)
        {
            Assert.Equal(expected, new Guid(b));
        }

        public static IEnumerable<object[]> Ctor_ByteArray_BigEndian_TestData()
        {
            yield return new object[] { new byte[16], true, Guid.Empty };
            yield return new object[] { new byte[16], false, Guid.Empty };
            yield return new object[] { new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF }, true, new Guid("11223344-5566-7788-9900-aabbccddeeff") };
            yield return new object[] { new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF }, false, new Guid("44332211-6655-8877-9900-aabbccddeeff") };
            yield return new object[] { new byte[] { 0x44, 0x33, 0x22, 0x11, 0x66, 0x55, 0x88, 0x77, 0x99, 0x00, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF }, true, new Guid("44332211-6655-8877-9900-aabbccddeeff") };
            yield return new object[] { new byte[] { 0x44, 0x33, 0x22, 0x11, 0x66, 0x55, 0x88, 0x77, 0x99, 0x00, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF }, false, new Guid("11223344-5566-7788-9900-aabbccddeeff") };
            yield return new object[] { s_testGuid.ToByteArray(true), true, s_testGuid };
            yield return new object[] { s_testGuid.ToByteArray(), false, s_testGuid };
        }

        [Theory]
        [MemberData(nameof(Ctor_ByteArray_BigEndian_TestData))]
        public static void Ctor_ByteArray_BigEndian(byte[] b, bool bigEndian, Guid expected)
        {
            Assert.Equal(expected, new Guid(b, bigEndian));
        }

        [Fact]
        public static void Ctor_NullByteArray_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("b", () => new Guid((byte[])null));
        }

        [Theory]
        [InlineData(15)]
        [InlineData(17)]
        public static void Ctor_InvalidLengthByteArray_ThrowsArgumentException(int length)
        {
            AssertExtensions.Throws<ArgumentException>("b", null, () => new Guid(new byte[length]));
        }

        [Theory]
        [MemberData(nameof(GuidStrings_Valid_TestData))]
        public static void Ctor_String(string input, string format, Guid expected)
        {
            _ = format;
            Assert.Equal(expected, new Guid(input));
        }

        [Theory]
        [MemberData(nameof(GuidStrings_Invalid_TestData))]
        [MemberData(nameof(GuidStrings_TryParseThrows_TestData))]
        public static void Ctor_String_Invalid(string value, Type exceptionType)
        {
            Assert.Throws(exceptionType, () => new Guid(value));
        }

        public static IEnumerable<object[]> Ctor_Int_Short_Short_ByteArray_TestData()
        {
            yield return new object[] { unchecked((int)0xa8a110d5), unchecked((short)0xfc49), 0x43c5, new byte[] { 0xbf, 0x46, 0x80, 0x2d, 0xb8, 0xf8, 0x43, 0xff }, s_testGuid };
            yield return new object[] { 1, 2, 3, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }, new Guid("00000001-0002-0003-0001-020304050607") };
            yield return new object[] { 2147483647, 32767, 32767, new byte[] { 0xA, 0xB, 0xC, 0xD, 0xE, 0xF, 0xAA, 0xBB }, new Guid("7fffffff-7fff-7fff-0a0b-0c0d0e0faabb") };
        }

        [Theory]
        [MemberData(nameof(Ctor_Int_Short_Short_ByteArray_TestData))]
        public static void Ctor_Int_Short_Short_ByteArray(int a, short b, short c, byte[] d, Guid expected)
        {
            Assert.Equal(expected, new Guid(a, b, c, d));
            Assert.Equal(expected, new Guid(a, b, c, d[0], d[1], d[2], d[3], d[4], d[5], d[6], d[7]));
        }

        [Fact]
        public static void Ctor_Int_Short_Short_ByteArray_NullByteArray_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("d", () => new Guid(0, 0, 0, null));
        }

        [Fact]
        public static void Ctor_Int_Short_Short_ByteArray_InvalidLengthByteArray_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("d", null, () => new Guid(0, 0, 0, new byte[7]));
            AssertExtensions.Throws<ArgumentException>("d", null, () => new Guid(0, 0, 0, new byte[9]));
        }

        [Fact]
        public static void Ctor_UInt_UShort_UShort_Byte_Byte_Byte_Byte_Byte_Byte_Byte_Byte()
        {
            var guid = new Guid(0xa8a110d5, 0xfc49, 0x43c5, 0xbf, 0x46, 0x80, 0x2d, 0xb8, 0xf8, 0x43, 0xff);
            Assert.Equal(s_testGuid, guid);
        }

        [Fact]
        public static void NewGuid()
        {
            Guid guid1 = Guid.NewGuid();
            Assert.NotEqual(Guid.Empty, guid1);

            Guid guid2 = Guid.NewGuid();
            Assert.NotEqual(guid1, guid2);
        }

        [Fact]
        public static void NewGuid_Randomness()
        {
            const int Iterations = 100;
            const int GuidSize = 16;
            var random = new byte[GuidSize * Iterations];

            for (int i = 0; i < Iterations; i++)
            {
                // Get a new Guid
                Guid guid = Guid.NewGuid();
                byte[] bytes = guid.ToByteArray();

                // Make sure it's different from all of the previously created ones
                for (int j = 0; j < i; j++)
                {
                    Assert.False(bytes.SequenceEqual(new ArraySegment<byte>(random, j * GuidSize, GuidSize)));
                }

                // Copy it to our randomness array
                Array.Copy(bytes, 0, random, i * GuidSize, GuidSize);
            }

            // Verify the randomness of the data in the array. Guid has some small bias in it
            // due to several bits fixed based on the format, but that bias is small enough and
            // the variability allowed by VerifyRandomDistribution large enough that we don't do
            // anything special for it.
            RandomDataGenerator.VerifyRandomDistribution(random);
        }

        [Theory]
        [MemberData(nameof(GuidStrings_Valid_TestData))]
        public static void Parse(string input, string format, Guid expected)
        {
            Assert.Equal(expected, Guid.Parse(input));
            Assert.Equal(expected, Guid.ParseExact(input, format.ToUpperInvariant()));
            Assert.Equal(expected, Guid.ParseExact(input, format.ToLowerInvariant())); // Format should be case insensitive

            Guid result1;
            Assert.True(Guid.TryParse(input, out result1));
            Assert.Equal(expected, result1);

            Guid result2;
            Assert.True(Guid.TryParseExact(input, format.ToUpperInvariant(), out result2));
            Assert.Equal(expected, result2);

            Guid result3;
            Assert.True(Guid.TryParseExact(input, format.ToLowerInvariant(), out result3)); // Format should be case insensitive
            Assert.Equal(expected, result3);
        }

        [Theory]
        [MemberData(nameof(GuidStrings_Invalid_TestData))]
        public static void Parse_Invalid(string input, Type exceptionType)
        {
            // Overflow exceptions throw as format exceptions in Parse
            if (exceptionType.Equals(typeof(OverflowException)))
            {
                exceptionType = typeof(FormatException);
            }
            Assert.Throws(exceptionType, () => Guid.Parse(input));
            Assert.Throws(exceptionType, () => Guid.ParseExact(input, "N"));
            Assert.Throws(exceptionType, () => Guid.ParseExact(input, "D"));
            Assert.Throws(exceptionType, () => Guid.ParseExact(input, "B"));
            Assert.Throws(exceptionType, () => Guid.ParseExact(input, "P"));
            Assert.Throws(exceptionType, () => Guid.ParseExact(input, "X"));

            Guid result;

            Assert.False(Guid.TryParse(input, out result));
            Assert.Equal(Guid.Empty, result);

            Assert.False(Guid.TryParseExact(input, "N", out result));
            Assert.Equal(Guid.Empty, result);

            Assert.False(Guid.TryParseExact(input, "D", out result));
            Assert.Equal(Guid.Empty, result);

            Assert.False(Guid.TryParseExact(input, "B", out result));
            Assert.Equal(Guid.Empty, result);

            Assert.False(Guid.TryParseExact(input, "P", out result));
            Assert.Equal(Guid.Empty, result);

            Assert.False(Guid.TryParseExact(input, "X", out result));
            Assert.Equal(Guid.Empty, result);
        }

        [Theory]
        [MemberData(nameof(GuidStrings_Format_Invalid_TestData))]
        public static void ParseExact_Invalid(string input, string format, Type exceptionType)
        {
            Assert.Throws(exceptionType, () => Guid.ParseExact(input, format));

            Guid result;
            Assert.False(Guid.TryParseExact(input, format, out result));
            Assert.Equal(Guid.Empty, result);
        }

        [Theory]
        [MemberData(nameof(GuidStrings_Valid_TestData))]
        public static void Parse_Span_ValidInput_Success(string input, string format, Guid expected)
        {
            Assert.Equal(expected, Guid.Parse(input.AsSpan()));
            Assert.Equal(expected, Guid.ParseExact(input.AsSpan(), format.ToUpperInvariant()));
            Assert.Equal(expected, Guid.ParseExact(input.AsSpan(), format.ToLowerInvariant())); // Format should be case insensitive

            Guid result;

            Assert.True(Guid.TryParse(input.AsSpan(), out result));
            Assert.Equal(expected, result);

            Assert.True(Guid.TryParseExact(input.AsSpan(), format.ToUpperInvariant(), out result));
            Assert.Equal(expected, result);

            Assert.True(Guid.TryParseExact(input.AsSpan(), format.ToLowerInvariant(), out result)); // Format should be case insensitive
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(GuidStrings_Invalid_TestData))]
        public static void Parse_Span_InvalidInput_Fails(string input, Type exceptionType)
        {
            if (input == null)
            {
                return;
            }

            // Overflow exceptions throw as format exceptions in Parse
            if (exceptionType.Equals(typeof(OverflowException)))
            {
                exceptionType = typeof(FormatException);
            }
            Assert.Throws(exceptionType, () => Guid.Parse(input.AsSpan()));

            Assert.False(Guid.TryParse(input.AsSpan(), out Guid result));
            Assert.Equal(Guid.Empty, result);

            foreach (string format in new[] { "N", "D", "B", "P", "X" })
            {
                Assert.Throws(exceptionType, () => Guid.ParseExact(input.AsSpan(), format));

                Assert.False(Guid.TryParseExact(input.AsSpan(), format, out result));
                Assert.Equal(Guid.Empty, result);
            }
        }
        
        public static IEnumerable<object[]> CompareTo_TestData()
        {
            yield return new object[] { s_testGuid, s_testGuid, 0 };
            yield return new object[] { s_testGuid, new Guid("a8a110d5-fc49-43c5-bf46-802db8f843ff"), 0 };
            yield return new object[] { s_testGuid, Guid.Empty, 1 };
            yield return new object[] { s_testGuid, new Guid("98a110d5-fc49-43c5-bf46-802db8f843ff"), 1 };
            yield return new object[] { s_testGuid, new Guid("e8a110d5-fc49-43c5-bf46-802db8f843ff"), -1 };

            // a
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), 1 };
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(2, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), -1 };

            // b
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 1, 3, 4, 5, 6, 7, 8, 9, 10, 11), 1 };
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 3, 3, 4, 5, 6, 7, 8, 9, 10, 11), -1 };

            // c
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 2, 4, 5, 6, 7, 8, 9, 10, 11), 1 };
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 4, 4, 5, 6, 7, 8, 9, 10, 11), -1 };

            // d
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 3, 3, 5, 6, 7, 8, 9, 10, 11), 1 };
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 3, 5, 4, 6, 7, 8, 9, 10, 11), -1 };

            // e
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 3, 4, 4, 6, 7, 8, 9, 10, 11), 1 };
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 3, 4, 6, 6, 7, 8, 9, 10, 11), -1 };

            // f
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 3, 4, 5, 5, 7, 8, 9, 10, 11), 1 };
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 3, 4, 5, 7, 7, 8, 9, 10, 11), -1 };

            // g
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 3, 4, 5, 6, 6, 8, 9, 10, 11), 1 };
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 3, 4, 5, 6, 8, 8, 9, 10, 11), -1 };

            // h
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 3, 4, 5, 6, 7, 7, 9, 10, 11), 1 };
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 3, 4, 5, 6, 7, 9, 9, 10, 11), -1 };

            // i
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 3, 4, 5, 6, 7, 8, 8, 10, 11), 1 };
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 3, 4, 5, 6, 7, 8, 10, 10, 11), -1 };

            // j
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 9, 11), 1 };
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 11, 11), -1 };

            // k
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 10), 1 };
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12), -1 };

            yield return new object[] { s_testGuid, null, 1 };
        }

        [Theory]
        [MemberData(nameof(CompareTo_TestData))]
        public static void CompareTo(Guid guid, object obj, int expected)
        {
            if (obj is Guid)
            {
                Assert.Equal(expected, Math.Sign(guid.CompareTo((Guid)obj)));
            }
            IComparable comparable = guid;
            Assert.Equal(expected, Math.Sign(comparable.CompareTo(obj)));
        }

        [Fact]
        public static void CompareTo_ValueNotGuid_ThrowsArgumentException()
        {
            IComparable comparable = s_testGuid;
            AssertExtensions.Throws<ArgumentException>("value", null, () => comparable.CompareTo("a8a110d5-fc49-43c5-bf46-802db8f843ff"));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { s_testGuid, s_testGuid, true };
            yield return new object[] { s_testGuid, new Guid("a8a110d5-fc49-43c5-bf46-802db8f843ff"), true };
            yield return new object[] { s_testGuid, Guid.Empty, false };

            yield return new object[] { s_testGuid, "a8a110d5-fc49-43c5-bf46-802db8f843ff", false };

            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), true };
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), false };
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 0, 3, 4, 5, 6, 7, 8, 9, 10, 11), false };
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 0, 4, 5, 6, 7, 8, 9, 10, 11), false };
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 3, 0, 5, 6, 7, 8, 9, 10, 11), false };
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 3, 4, 0, 6, 7, 8, 9, 10, 11), false };
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 3, 4, 5, 0, 7, 8, 9, 10, 11), false };
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 3, 4, 5, 6, 0, 8, 9, 10, 11), false };
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 3, 4, 5, 6, 7, 0, 9, 10, 11), false };
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 3, 4, 5, 6, 7, 8, 0, 10, 11), false };
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 11), false };
            yield return new object[] { new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 0), false };

            yield return new object[] { s_testGuid, new object(), false};
            yield return new object[] { s_testGuid, null, false};
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public static void EqualsTest(Guid guid1, object obj, bool expected)
        {
            if (obj is Guid)
            {
                Guid guid2 = (Guid)obj;
                Assert.Equal(expected, guid1.Equals(guid2));
                Assert.Equal(expected, guid1 == guid2);
                Assert.Equal(!expected, guid1 != guid2);
                Assert.Equal(expected, guid1.GetHashCode().Equals(guid2.GetHashCode()));
            }
            Assert.Equal(expected, guid1.Equals(obj));
        }

        [Fact]
        public static void ToByteArray()
        {
            Assert.Equal(new byte[] { 0xd5, 0x10, 0xa1, 0xa8, 0x49, 0xfc, 0xc5, 0x43, 0xbf, 0x46, 0x80, 0x2d, 0xb8, 0xf8, 0x43, 0xff }, s_testGuid.ToByteArray());
            Assert.Equal(new byte[] { 0xd5, 0x10, 0xa1, 0xa8, 0x49, 0xfc, 0xc5, 0x43, 0xbf, 0x46, 0x80, 0x2d, 0xb8, 0xf8, 0x43, 0xff }, s_testGuid.ToByteArray(false));
            Assert.Equal(new byte[] { 0xa8, 0xa1, 0x10, 0xd5, 0xfc, 0x49, 0x43, 0xc5, 0xbf, 0x46, 0x80, 0x2d, 0xb8, 0xf8, 0x43, 0xff }, s_testGuid.ToByteArray(true));
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { s_testGuid, "N", "a8a110d5fc4943c5bf46802db8f843ff"};
            yield return new object[] { s_testGuid, "D", "a8a110d5-fc49-43c5-bf46-802db8f843ff"};
            yield return new object[] { s_testGuid, "B", "{a8a110d5-fc49-43c5-bf46-802db8f843ff}"};
            yield return new object[] { s_testGuid, "P", "(a8a110d5-fc49-43c5-bf46-802db8f843ff)"};
            yield return new object[] { s_testGuid, "X", "{0xa8a110d5,0xfc49,0x43c5,{0xbf,0x46,0x80,0x2d,0xb8,0xf8,0x43,0xff}}"};

            yield return new object[] { s_testGuid, null, "a8a110d5-fc49-43c5-bf46-802db8f843ff"};
            yield return new object[] { s_testGuid, "", "a8a110d5-fc49-43c5-bf46-802db8f843ff"};
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToStringTest(Guid guid, string format, string expected)
        {
            IFormattable formattable = guid;
            if (string.IsNullOrEmpty(format) || format == "D")
            {
                Assert.Equal(expected, guid.ToString());
                Assert.Equal(expected, formattable.ToString());
            }
            Assert.Equal(expected, guid.ToString(format));
            Assert.Equal(expected, formattable.ToString(format, null));
        }

        public static IEnumerable<object[]> InvalidFormat_TestData()
        {
            yield return new object[] { "a" };
            yield return new object[] { "c" };
            yield return new object[] { "e" };
            yield return new object[] { "m" };
            yield return new object[] { "o" };
            yield return new object[] { "q" };
            yield return new object[] { "w" };
            yield return new object[] { "y" };
            yield return new object[] { "xx" };
        }

        [Theory]
        [MemberData(nameof(InvalidFormat_TestData))]
        public static void ToString_InvalidFormat_ThrowsFormatException(string format)
        {
            Assert.Throws<FormatException>(() => s_testGuid.ToString(format));
            Assert.Throws<FormatException>(() => s_testGuid.ToString(format.ToUpperInvariant()));
        }

        public static IEnumerable<object[]> GuidStrings_Valid_TestData()
        {
            yield return new object[] { "a8a110d5fc4943c5bf46802db8f843ff", "N", s_testGuid };
            yield return new object[] { "  \r \n \t a8a110d5fc4943c5bf46802db8f843ff   \r \n \t  ", "N", s_testGuid };

            yield return new object[] { "00000000000000000000000000000000", "N", Guid.Empty };
            yield return new object[] { "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", "N", s_fullGuid };
            yield return new object[] { "ffffffffffffffffffffffffffffffff", "N", s_fullGuid };

            yield return new object[] { "a8a110d5-fc49-43c5-bf46-802db8f843ff", "D", s_testGuid };
            yield return new object[] { "  \r \n \t a8a110d5-fc49-43c5-bf46-802db8f843ff   \r \n \t  ", "D", s_testGuid };

            yield return new object[] { "00000000-0000-0000-0000-000000000000", "D", Guid.Empty };
            yield return new object[] { "FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF", "D", s_fullGuid };
            yield return new object[] { "ffffffff-ffff-ffff-ffff-ffffffffffff", "D", s_fullGuid };

            yield return new object[] { "+ddddddd-+ddd-+ddd-+ddd-+ddddddddddd", "D", Guid.Parse("0ddddddd-0ddd-0ddd-0ddd-0ddddddddddd") };
            yield return new object[] { "0xdddddd-0xdd-0xdd-0xdd-0xdddddddddd", "D", Guid.Parse("00dddddd-00dd-00dd-00dd-00dddddddddd") };
            yield return new object[] { "+0Xddddd-+0Xd-+0Xd-+0Xd-+0Xddddddddd", "D", Guid.Parse("000ddddd-000d-000d-000d-000ddddddddd") };

            yield return new object[] { "{a8a110d5-fc49-43c5-bf46-802db8f843ff}", "B", s_testGuid };
            yield return new object[] { "  \r \n \t {a8a110d5-fc49-43c5-bf46-802db8f843ff}   \r \n \t  ", "B", s_testGuid };

            yield return new object[] { "{00000000-0000-0000-0000-000000000000}", "B", Guid.Empty };
            yield return new object[] { "{FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF}", "B", s_fullGuid };
            yield return new object[] { "{ffffffff-ffff-ffff-ffff-ffffffffffff}", "B", s_fullGuid };

            yield return new object[] { "{+ddddddd-+ddd-+ddd-+ddd-+ddddddddddd}", "B", Guid.Parse("0ddddddd-0ddd-0ddd-0ddd-0ddddddddddd") };
            yield return new object[] { "{0xdddddd-0xdd-0xdd-0xdd-0xdddddddddd}", "B", Guid.Parse("00dddddd-00dd-00dd-00dd-00dddddddddd") };
            yield return new object[] { "{+0Xddddd-+0Xd-+0Xd-+0Xd-+0Xddddddddd}", "B", Guid.Parse("000ddddd-000d-000d-000d-000ddddddddd") };

            yield return new object[] { "(a8a110d5-fc49-43c5-bf46-802db8f843ff)", "P", s_testGuid };
            yield return new object[] { "  \r \n \t (a8a110d5-fc49-43c5-bf46-802db8f843ff)   \r \n \t  ", "P", s_testGuid };

            yield return new object[] { "(00000000-0000-0000-0000-000000000000)", "P", Guid.Empty };
            yield return new object[] { "(FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF)", "P", s_fullGuid };
            yield return new object[] { "(ffffffff-ffff-ffff-ffff-ffffffffffff)", "P", s_fullGuid };

            yield return new object[] { "(+ddddddd-+ddd-+ddd-+ddd-+ddddddddddd)", "P", Guid.Parse("0ddddddd-0ddd-0ddd-0ddd-0ddddddddddd") };
            yield return new object[] { "(0xdddddd-0xdd-0xdd-0xdd-0xdddddddddd)", "P", Guid.Parse("00dddddd-00dd-00dd-00dd-00dddddddddd") };
            yield return new object[] { "(+0Xddddd-+0Xd-+0Xd-+0Xd-+0Xddddddddd)", "P", Guid.Parse("000ddddd-000d-000d-000d-000ddddddddd") };

            yield return new object[] { "{0xa8a110d5,0xfc49,0x43c5,{0xbf,0x46,0x80,0x2d,0xb8,0xf8,0x43,0xff}}", "X", s_testGuid };
            yield return new object[] { " { 0 x a 8\t a 1 1 0 d 5 , 0 x f c 4\r 9 , 0 x 4 3 c 5 , { 0 x b f , 0 x 4 6 , 0 x 8 0 , 0\n x 2 d , 0 x b 8 , 0 x f 8 , 0 x 4 3 , 0 x f f } }   ", "X", s_testGuid };

            yield return new object[] { "{0x0,0x0,0x0,{0x0,0x0,0x0,0x0,0x0,0x0,0x0,0x0}}", "X", Guid.Empty };
            yield return new object[] { "{0xFFFFFFFF,0xFFFF,0xFFFF,{0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF}}", "X", s_fullGuid };
            yield return new object[] { "{0xffffffff,0xffff,0xffff,{0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff}}", "X", s_fullGuid };

            yield return new object[] { "{0x+dddddddd,0x+dddd,0x+dddd,{0x+dd,0x+dd,0x+dd,0x+dd,0x+dd,0x+dd,0x+dd,0x+dd}}", "X", Guid.Parse("{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}") };
            yield return new object[] { "{0x0xdddddddd,0x0xdddd,0x0xdddd,{0x0xdd,0x0xdd,0x0xdd,0x0xdd,0x0xdd,0x0xdd,0x0xdd,0x0xdd}}", "X", Guid.Parse("{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}") };
            yield return new object[] { "{0x+0Xdddddddd,0x+0Xdddd,0x+0Xdddd,{0x+0Xdd,0x+0Xdd,0x+0Xdd,0x+0Xdd,0x+0Xdd,0x+0Xdd,0x+0Xdd,0x+0Xdd}}", "X", Guid.Parse("{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}") };
        }

        public static IEnumerable<object[]> GuidStrings_Invalid_TestData()
        {
            yield return new object[] { null, typeof(ArgumentNullException) }; // String is null

            yield return new object[] { "", typeof(FormatException) }; // String is invalid
            yield return new object[] { "     \t", typeof(FormatException) }; // String is invalid

            yield return new object[] { "ddddddddddddddddddddddddddddddd", typeof(FormatException) }; // Length < 32
            yield return new object[] { "ddddddddddddddddddddddddddddddddd", typeof(FormatException) }; // Length > 32
            yield return new object[] { "{dddddddddddddddddddddddddddddddd}", typeof(FormatException) }; // Surrounded by braces

            // Can't contain inner whitespace
            yield return new object[] { "d d d d d  dd d d d d d dd d d d dd d d dd d dd d d d d d d", typeof(FormatException) };
            yield return new object[] { "{d d d d d  dd d d d d d dd d d d dd d d dd d dd d d d d d d}", typeof(FormatException) };
            yield return new object[] { "(d d d d d  dd d d d d d dd d d d dd d d dd d dd d d d d d d)", typeof(FormatException) };

            yield return new object[] { "dddddddd-dddddddd-dddddddd", typeof(FormatException) }; // 8-8-8
            yield return new object[] { "dddddddd-dddddddd-dddddddd-ddddddddd", typeof(FormatException) }; // 8-8-8
            yield return new object[] { "dddddddd-dddddddd-dddddddd-dddddddd-dddddddd", typeof(FormatException) }; // 8-8-8-8

            yield return new object[] { "ddddddd-dddd-dddd-dddd-dddddddd", typeof(FormatException) }; // 7-4-4-4-8
            yield return new object[] { "ddddddddd-dddd-dddd-dddd-dddddddd", typeof(FormatException) }; // 9-4-4-4-8

            yield return new object[] { "dddddddd-ddd-dddd-dddd-dddddddd", typeof(FormatException) }; // 8-3-4-4-8
            yield return new object[] { "dddddddd-ddddd-dddd-dddd-dddddddd", typeof(FormatException) }; // 8-5-4-4-8

            yield return new object[] { "dddddddd-dddd-ddd-dddd-dddddddd", typeof(FormatException) }; // 8-4-3-4-8
            yield return new object[] { "dddddddd-dddd-ddddd-dddd-dddddddd", typeof(FormatException) }; // 8-4-5-4-8

            yield return new object[] { "dddddddd-dddd-dddd-ddd-dddddddd", typeof(FormatException) }; // 8-4-4-3-8
            yield return new object[] { "dddddddd-dddd-dddd-ddddd-dddddddd", typeof(FormatException) }; // 8-4-4-5-8

            yield return new object[] { "dddddddd-dddd-dddd-dddd-ddddddd", typeof(FormatException) }; // 8-4-4-4-7
            yield return new object[] { "dddddddd-dddd-dddd-dddd-ddddddddd", typeof(FormatException) }; // 8-4-4-9

            yield return new object[] { "{dddddddd-dddd-dddd-dddd-dddddddd", typeof(FormatException) }; // 8-4-4-4-8 with leading brace only
            yield return new object[] { "dddddddd-dddd-dddd-dddd-dddddddd}", typeof(FormatException) }; // 8-4-4-4-8 with trailing brace only

            yield return new object[] { "(dddddddd-dddd-dddd-dddd-dddddddd", typeof(FormatException) }; // 8-4-4-4-8 with leading parenthesis only
            yield return new object[] { "dddddddd-dddd-dddd-dddd-dddddddd)", typeof(FormatException) }; // 8-4-4-4-8 with trailing parenthesis only

            yield return new object[] { "(dddddddd-dddd-dddd-dddd-dddddddd}", typeof(FormatException) }; // 8-4-4-4-8 with leading parenthesis and trailing brace
            yield return new object[] { "{dddddddd-dddd-dddd-dddd-dddddddd)", typeof(FormatException) }; // 8-4-4-4-8 with trailing parenthesis and leading brace

            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} - missing group

            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without a trailing brace
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without a trailing brace

            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without a leading brace
            yield return new object[] { "0xdddddddd, 0xdddd,{0xdddd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without a leading brace
            yield return new object[] { "{0xdddddddd, 0xdddd,0xdddd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without a leading brace

            yield return new object[] { "(0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd))", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} with parentheses
            yield return new object[] { "(0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd})", typeof(FormatException) };// 8-4-4-{2-2-2-2-2-2-2-2} with parentheses

            yield return new object[] { "{0xdddddddd 0xdddd 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(OverflowException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without comma
            yield return new object[] { "{0xdddddddd, 0xdddd 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without comma
            yield return new object[] { "{0xdddddddd, 0xdddd 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without comma
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without comma

            yield return new object[] { "{dddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without 0x prefix
            yield return new object[] { "{0xdddddddd, dddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without 0x prefix
            yield return new object[] { "{0xdddddddd, 0xdddd, dddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without 0x prefix
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{dd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without 0x prefix
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,dd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without 0x prefix
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,dd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without 0x prefix
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,dd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without 0x prefix
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,dd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without 0x prefix
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,dd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without 0x prefix
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,dd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without 0x prefix
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,dd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without 0x prefix

            yield return new object[] { "{0x, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
            yield return new object[] { "{0xdddddddd, 0x, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
            yield return new object[] { "{0xdddddddd, 0xdddd, 0x,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0x,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0x,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0x,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0x,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0x,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0x,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0x,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0x}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x

            // Invalid length of 1st argument
            yield return new object[] { "{0xddddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(OverflowException) }; // 9-4-4-{2-2-2-2-2-2-2-2}

            // Invalid length of 2nd agument
            yield return new object[] { "{0xdddddddd, 0xddddddddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(OverflowException) };
            yield return new object[] { "{0xdddddddd, 0xddddddd, 0xdddd,{0xdd,0xddd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd, 0xddddd, 0xdddd,{0xdd,0xddd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };

            // Invalid length of 3rd argument
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xddddddddd,{0xdd,0xddd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(OverflowException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xddddddd,{0xdd,0xddd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xddddd,{0xdd,0xddd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };

            // Invalid length of 4th argument
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdddddddd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xddd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };

            // Invalid length of 5th argument
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdddddddd,0xdd,0xddd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xddd,0xdd,0xddd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };

            // Invalid length of 6th argument
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdddddddd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xddd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };

            // Invalid length of 7th argument
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdddddddd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xddd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };

            // Invalid length of 8th argument
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdddddddd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xddd,0xdd,0xdd,0xdd}}", typeof(FormatException) };

            // Invalid length of 9th argument
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xddd,0xdd,0xdd}}", typeof(FormatException) };

            // Invalid length of 10th argument
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdddddddd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xddd,0xdd}}", typeof(FormatException) };

            // Invalid length of 11th argument
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdddddddd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xddd}}", typeof(FormatException) };

            // Missing a comma
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd0xdd}}", typeof(FormatException) };

            // Extra characters at the end
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}abc", typeof(FormatException) };

            // Missing all components (ordered)
            yield return new object[] { "{", typeof(FormatException) };
            yield return new object[] { "{0x", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0x", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0x", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0x}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0x}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0x}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0x}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0x}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0x}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0x", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0x", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}", typeof(FormatException) };

            // Invalid hex
            yield return new object[] { "Gddddddddddddddddddddddddddddddd", typeof(FormatException) };
            yield return new object[] { "ddddddddGddddddddddddddddddddddd", typeof(FormatException) };
            yield return new object[] { "ddddddddddddGddddddddddddddddddd", typeof(FormatException) };
            yield return new object[] { "ddddddddddddddddGddddddddddddddd", typeof(FormatException) };
            yield return new object[] { "ddddddddddddddddddddGddddddddddd", typeof(FormatException) };

            yield return new object[] { "-ddddddd-dddd-dddd-dddd-dddddddddddd", typeof(FormatException) };
            yield return new object[] { "(-ddddddd-dddd-dddd-dddd-dddddddddddd)", typeof(FormatException) };
            yield return new object[] { "{-ddddddd-dddd-dddd-dddd-dddddddddddd}", typeof(FormatException) };
            yield return new object[] { "Gddddddd-dddd-dddd-dddd-dddddddddddd", typeof(FormatException) };
            yield return new object[] { "dGdddddd-dddd-dddd-dddd-dddddddddddd", typeof(FormatException) };

            yield return new object[] { "dddddddd--ddd-dddd-dddd-dddddddddddd", typeof(FormatException) };
            yield return new object[] { "{dddddddd--ddd-dddd-dddd-dddddddddddd)", typeof(FormatException) };
            yield return new object[] { "(dddddddd--ddd-dddd-dddd-dddddddddddd}", typeof(FormatException) };
            yield return new object[] { "dddddddd-Gddd-dddd-dddd-dddddddddddd", typeof(FormatException) };
            yield return new object[] { "dddddddd-dGdd-dddd-dddd-dddddddddddd", typeof(FormatException) };

            yield return new object[] { "dddddddd-dddd--ddd-dddd-dddddddddddd", typeof(FormatException) };
            yield return new object[] { "{dddddddd-dddd--ddd-dddd-dddddddddddd)", typeof(FormatException) };
            yield return new object[] { "(dddddddd-dddd--ddd-dddd-dddddddddddd}", typeof(FormatException) };
            yield return new object[] { "dddddddd-dddd-Gddd-dddd-dddddddddddd", typeof(FormatException) };
            yield return new object[] { "dddddddd-dddd-dGdd-dddd-dddddddddddd", typeof(FormatException) };

            yield return new object[] { "dddddddd-dddd-dddd--ddd-dddddddddddd", typeof(FormatException) };
            yield return new object[] { "{dddddddd-dddd-dddd--ddd-dddddddddddd}", typeof(FormatException) };
            yield return new object[] { "(dddddddd-dddd-dddd--ddd-dddddddddddd)", typeof(FormatException) };
            yield return new object[] { "dddddddd-dddd-dddd-Gddd-dddddddddddd", typeof(FormatException) };
            yield return new object[] { "dddddddd-dddd-dddd-dGdd-dddddddddddd", typeof(FormatException) };

            yield return new object[] { "dddddddd-dddd-dddd-dddd--ddddddddddd", typeof(FormatException) };
            yield return new object[] { "{dddddddd-dddd-dddd-dddd--ddddddddddd}", typeof(FormatException) };
            yield return new object[] { "(dddddddd-dddd-dddd-dddd--ddddddddddd)", typeof(FormatException) };
            yield return new object[] { "dddddddd-dddd-dddd-dddd-Gddddddddddd", typeof(FormatException) };
            yield return new object[] { "dddddddd-dddd-dddd-dddd-dGdddddddddd", typeof(FormatException) };

            yield return new object[] { "{0xGddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdGdddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0x-dddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };

            yield return new object[] { "{0xdddddddd,0xGddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdGdd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xddddddd,0x-dddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };

            yield return new object[] { "{0xdddddddd,0xdddd,0xGddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdGdd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xddddddd,0xdddd,0x-dddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xddddddd,0xdddd,0xdddd,{0x-dd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0x-dd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0x-dd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0x-dd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0x-dd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0x-dd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0x-dd,0xdd}}", typeof(FormatException) };

            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0x-dd}}", typeof(FormatException) };
        }

        public static IEnumerable<object[]> GuidStrings_TryParseThrows_TestData()
        {
            // 8-4-4-{2-2-2-2-2-2-2-2} - extra group
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };

            // Missing a comma
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd0xdddddddd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd0xdd,0xdd}}", typeof(FormatException) };

            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xGd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdG,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };

            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xGd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdG,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };

            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xGd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdG,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };

            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xGd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdG,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) };

            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xGd,0xdd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdG,0xdd,0xdd,0xdd}}", typeof(FormatException) };

            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xGd,0xdd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdG,0xdd,0xdd}}", typeof(FormatException) };

            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xGd,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdG,0xdd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xGd}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd,0xdddd,0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdG}}", typeof(FormatException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xddddddddd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(OverflowException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xddddddddd,0xdd,0xddd,0xdd,0xdd,0xdd,0xdd}}", typeof(OverflowException) };
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xddddddddd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(OverflowException) };

            // Invalid length of 7th element
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xddddddddd,0xdd,0xdd,0xdd,0xdd}}", typeof(OverflowException) };

            // Invalid length of 8th element
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xddddddddd,0xdd,0xdd,0xdd}}", typeof(OverflowException) };

            // Invalid length of 9th element
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xddddddddd,0xdd,0xdd}}", typeof(OverflowException) };

            // Invalid length of 10th element
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xddddddddd,0xdd}}", typeof(OverflowException) };

            // Invalid length of 11th element
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xddddddddd}}", typeof(OverflowException) };
        }

        public static IEnumerable<object[]> GuidStrings_Format_Invalid_TestData()
        {
            yield return new object[] { null, "D", typeof(ArgumentNullException) }; // String is null
            yield return new object[] { "", null, typeof(ArgumentNullException) }; // Format is null

            yield return new object[] { "", "", typeof(FormatException) }; // Format is invalid
            yield return new object[] { "", "Y", typeof(FormatException) }; // Format is invalid
            yield return new object[] { "", "XX", typeof(FormatException) }; // Format is invalid

            yield return new object[] { "dddddddddddddddddddddddddddddddd", "D", typeof(FormatException) }; // 32 digits
            yield return new object[] { "dddddddddddddddddddddddddddddddd", "B", typeof(FormatException) }; // 32 digits
            yield return new object[] { "dddddddddddddddddddddddddddddddd", "P", typeof(FormatException) }; // 32 digits
            yield return new object[] { "dddddddddddddddddddddddddddddddd", "X", typeof(FormatException) }; // 32 digits

            yield return new object[] { "dddddddd-dddd-dddd-dddd-dddddddd", "N", typeof(FormatException) }; // 32 digits with hyphens
            yield return new object[] { "dddddddd-dddd-dddd-dddd-dddddddd", "B", typeof(FormatException) }; // 32 digits with hyphens
            yield return new object[] { "dddddddd-dddd-dddd-dddd-dddddddd", "P", typeof(FormatException) }; // 32 digits with hyphens
            yield return new object[] { "dddddddd-dddd-dddd-dddd-dddddddd", "X", typeof(FormatException) }; // 32 digits with hyphens

            yield return new object[] { "{dddddddd-dddd-dddd-dddd-dddddddd}", "N", typeof(FormatException) }; // 32 digits with hyphens and braces
            yield return new object[] { "{dddddddd-dddd-dddd-dddd-dddddddd}", "D", typeof(FormatException) }; // 32 digits with hyphens and braces
            yield return new object[] { "{dddddddd-dddd-dddd-dddd-dddddddd}", "P", typeof(FormatException) }; // 32 digits with hyphens and braces
            yield return new object[] { "{dddddddd-dddd-dddd-dddd-dddddddd}", "X", typeof(FormatException) }; // 32 digits with hyphens and braces

            yield return new object[] { "(dddddddd-dddd-dddd-dddd-dddddddd)", "N", typeof(FormatException) }; // 32 digits with hyphens and parentheses
            yield return new object[] { "(dddddddd-dddd-dddd-dddd-dddddddd)", "D", typeof(FormatException) }; // 32 digits with hyphens and parentheses
            yield return new object[] { "(dddddddd-dddd-dddd-dddd-dddddddd)", "B", typeof(FormatException) }; // 32 digits with hyphens and parentheses
            yield return new object[] { "(dddddddd-dddd-dddd-dddd-dddddddd)", "X", typeof(FormatException) }; // 32 digits with hyphens and parentheses

            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", "N", typeof(FormatException) }; // Hex values
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", "D", typeof(FormatException) }; // Hex values
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", "B", typeof(FormatException) }; // Hex values
            yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", "P", typeof(FormatException) }; // Hex values
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void ToString_Provider(Guid guid, string format, string expected)
        {
            // Format provider parameter is ignored
            Assert.Equal(expected, guid.ToString(format, CultureInfo.CurrentCulture));
        }

        [Theory]
        [MemberData(nameof(CompareTo_TestData))]
        public static void CompareTo_Object(Guid guid, object obj, int expected)
        {
            Assert.Equal(expected, Math.Sign(guid.CompareTo(obj)));
        }

        [Theory]
        [MemberData(nameof(Ctor_ByteArray_TestData))]
        public static void Ctor_ReadOnlySpan(byte[] b, Guid expected)
        {
            Assert.Equal(expected, new Guid(new ReadOnlySpan<byte>(b)));
        }

        [Theory]
        [InlineData(15)]
        [InlineData(17)]
        public static void CtorSpan_InvalidLengthByteArray_ThrowsArgumentException(int length)
        {
            AssertExtensions.Throws<ArgumentException>("b", null, () => new Guid(new ReadOnlySpan<byte>(new byte[length])));
        }

        [Theory]
        [MemberData(nameof(Ctor_ByteArray_TestData))]
        public static void TryWriteBytes_ValidLength_ReturnsTrue(byte[] b, Guid guid)
        {
            var bytes = new byte[16];
            Assert.True(guid.TryWriteBytes(new Span<byte>(bytes)));
            Assert.Equal(b, bytes);
        }

        [Theory]
        [MemberData(nameof(Ctor_ByteArray_BigEndian_TestData))]
        public static void TryWriteBytes_BigEndian_ValidLength_ReturnsTrue(byte[] b, bool bigEndian, Guid guid)
        {
            var bytes = new byte[16];
            Assert.True(guid.TryWriteBytes(new Span<byte>(bytes), bigEndian, out int bytesWritten));
            Assert.Equal(b, bytes);
            Assert.Equal(16, bytesWritten);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(15)]
        public static void TryWriteBytes_LengthTooShort_ReturnsFalse(int length)
        {
            Assert.False(s_testGuid.TryWriteBytes(new Span<byte>(new byte[length])));
            Assert.False(s_testGuid.TryWriteBytes(new Span<byte>(new byte[length]), true, out int bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.False(s_testGuid.TryWriteBytes(new Span<byte>(new byte[length]), false, out bytesWritten));
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [MemberData(nameof(InvalidFormat_TestData))]
        public static void TryFormat_InvalidFormat_ThrowsFormatException(string format)
        {
            Assert.Throws<FormatException>(() => s_testGuid.TryFormat(new Span<char>(), out int charsWritten, format));
            Assert.Throws<FormatException>(() => s_testGuid.TryFormat(new Span<char>(), out int charsWritten, format.ToUpperInvariant()));

            Assert.Throws<FormatException>(() => ((ISpanFormattable)s_testGuid).TryFormat(new Span<char>(), out int charsWritten, format, null));
            Assert.Throws<FormatException>(() => ((ISpanFormattable)s_testGuid).TryFormat(new Span<char>(), out int charsWritten, format.ToUpperInvariant(), null));

            Assert.Throws<FormatException>(() => s_testGuid.TryFormat(new Span<byte>(), out int bytesWritten, format));
            Assert.Throws<FormatException>(() => s_testGuid.TryFormat(new Span<byte>(), out int bytesWritten, format.ToUpperInvariant()));
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void TryFormat_LengthTooSmall_ReturnsFalse(Guid guid, string format, string expected)
        {
            _ = expected;

            Assert.False(guid.TryFormat(new Span<char>(new char[guid.ToString(format).Length - 1]), out int charsWritten, format));
            Assert.Equal(0, charsWritten);

            Assert.False(((ISpanFormattable)guid).TryFormat(new Span<char>(new char[guid.ToString(format).Length - 1]), out charsWritten, format, null));
            Assert.Equal(0, charsWritten);

            Assert.False(guid.TryFormat(new Span<byte>(new byte[guid.ToString(format).Length - 1]), out int bytesWritten, format));
            Assert.Equal(0, bytesWritten);

            Assert.False(((IUtf8SpanFormattable)guid).TryFormat(new Span<byte>(new byte[guid.ToString(format).Length - 1]), out bytesWritten, format, null));
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void TryFormat_CharsWritten_EqualsZero_WhenSpanTooSmall(Guid guid, string format, string expected)
        {
            _ = expected;

            Assert.False(guid.TryFormat(new Span<char>(new char[guid.ToString(format).Length - 1]), out int charsWritten, format));
            Assert.Equal(0, charsWritten);

            Assert.False(((ISpanFormattable)guid).TryFormat(new Span<char>(new char[guid.ToString(format).Length - 1]), out charsWritten, format, null));
            Assert.Equal(0, charsWritten);

            Assert.False(guid.TryFormat(new Span<byte>(new byte[guid.ToString(format).Length - 1]), out int bytesWritten, format));
            Assert.Equal(0, bytesWritten);

            Assert.False(((IUtf8SpanFormattable)guid).TryFormat(new Span<byte>(new byte[guid.ToString(format).Length - 1]), out bytesWritten, format, null));
            Assert.Equal(0, bytesWritten);
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public static void TryFormat_ValidLength_ReturnsTrue(Guid guid, string format, string expected)
        {
            for (int additionalSpace = 0; additionalSpace < 2; additionalSpace++)
            {
                char[] chars = new char[expected.Length + additionalSpace];
                byte[] bytes = new byte[expected.Length + additionalSpace];

                Assert.True(guid.TryFormat(new Span<char>(chars), out int charsWritten, format));
                Assert.Equal(expected.Length, charsWritten);
                Assert.Equal(expected, chars.AsSpan(0, charsWritten).ToString());

                Assert.True(((ISpanFormattable)guid).TryFormat(new Span<char>(chars), out charsWritten, format, null));
                Assert.Equal(expected.Length, charsWritten);
                Assert.Equal(expected, chars.AsSpan(0, charsWritten).ToString());

                Assert.True(guid.TryFormat(new Span<byte>(bytes), out int bytesWritten, format));
                Assert.Equal(expected.Length, bytesWritten);
                Assert.Equal(expected, Encoding.UTF8.GetString(bytes.AsSpan(0, bytesWritten)));

                Assert.True(((IUtf8SpanFormattable)guid).TryFormat(new Span<byte>(bytes), out bytesWritten, format, null));
                Assert.Equal(expected.Length, bytesWritten);
                Assert.Equal(expected, Encoding.UTF8.GetString(bytes.AsSpan(0, bytesWritten)));
            }
        }
    }
}
