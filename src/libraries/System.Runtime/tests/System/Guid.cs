// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public static class GuidTests
{
    private static readonly Guid s_testGuid = new Guid("a8a110d5-fc49-43c5-bf46-802db8f843ff");

    [Fact]
    public static void TestEmpty()
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
    public static void TestCtor_ByteArray(byte[] b, Guid expected)
    {
        Assert.Equal(expected, new Guid(b));
    }

    [Fact]
    public static void TestCtor_ByteArray_Invalid()
    {
        Assert.Throws<ArgumentNullException>("b", () => new Guid((byte[])null)); // Byte array is null
        Assert.Throws<ArgumentException>(null, () => new Guid(new byte[15])); // Byte array is not 16 bytes long
        Assert.Throws<ArgumentException>(null, () => new Guid(new byte[17])); // Byte array is not 16 bytes long
    }

    [Theory]
    [MemberData(nameof(GuidStrings_Valid_TestData))]
    public static void TestCtor_String(string input, string _, Guid expected)
    {
        Assert.Equal(expected, new Guid(input));
    }

    [Theory]
    [MemberData(nameof(GuidStrings_Invalid_TestData))]
    public static void TestCtor_String_Invalid(string value, Type exceptionType)
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
    public static void TestCtor_Int_Short_Short_ByteArray(int a, short b, short c, byte[] d, Guid expected)
    {
        Assert.Equal(expected, new Guid(a, b, c, d));
        Assert.Equal(expected, new Guid(a, b, c, d[0], d[1], d[2], d[3], d[4], d[5], d[6], d[7]));
    }

    [Fact]
    public static void TestCtor_Int_Short_Short_ByteArray_Invalid()
    {
        Assert.Throws<ArgumentNullException>("d", () => new Guid(0, 0, 0, null)); // Byte array is null
        Assert.Throws<ArgumentException>(null, () => new Guid(0, 0, 0, new byte[7])); // Byte array is not 8 bytes long
        Assert.Throws<ArgumentException>(null, () => new Guid(0, 0, 0, new byte[9])); // Byte array is not 8 bytes long
    }

    [Fact]
    public static void TestCtor_UInt_UShort_UShort_Byte_Byte_Byte_Byte_Byte_Byte_Byte_Byte()
    {
        var guid = new Guid(0xa8a110d5, 0xfc49, 0x43c5, 0xbf, 0x46, 0x80, 0x2d, 0xb8, 0xf8, 0x43, 0xff);
        Assert.Equal(s_testGuid, guid);
    }

    [Fact]
    public static void TestNewGuid()
    {
        Guid guid1 = Guid.NewGuid();
        Assert.NotEqual(Guid.Empty, guid1);

        Guid guid2 = Guid.NewGuid();
        Assert.NotEqual(guid1, guid2);
    }

    [Fact]
    public static void TestNewGuid_Randomness()
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
    public static void TestParse(string input, string format, Guid expected)
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
    public static void TestParse_Invalid(string input, Type exceptionType)
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
    public static void TestParseExact_Invalid(string input, string format, Type exceptionType)
    {
        Assert.Throws(exceptionType, () => Guid.ParseExact(input, format));

        Guid result;
        Assert.False(Guid.TryParseExact(input, format, out result));
    }

    public static IEnumerable<object[]> CompareTo_TestData()
    {
        yield return new object[] { s_testGuid, s_testGuid, 0 };
        yield return new object[] { s_testGuid, new Guid("a8a110d5-fc49-43c5-bf46-802db8f843ff"), 0 };
        yield return new object[] { s_testGuid, Guid.Empty, 1 };
        yield return new object[] { s_testGuid, new Guid("98a110d5-fc49-43c5-bf46-802db8f843ff"), 1 };
        yield return new object[] { s_testGuid, new Guid("e8a110d5-fc49-43c5-bf46-802db8f843ff"), -1 };

        yield return new object[] { s_testGuid, null, 1 };
    }

    [Theory]
    [MemberData(nameof(CompareTo_TestData))]
    public static void TestCompareTo(Guid guid, object obj, int expected)
    {
        if (obj is Guid)
        {
            Assert.Equal(expected, Math.Sign(guid.CompareTo((Guid)obj)));
        }
        IComparable comparable = guid;
        Assert.Equal(expected, Math.Sign(comparable.CompareTo(obj)));
    }

    [Fact]
    public static void TestCompareTo_Invalid()
    {
        IComparable comparable = s_testGuid;
        Assert.Throws<ArgumentException>(null, () => comparable.CompareTo("a8a110d5-fc49-43c5-bf46-802db8f843ff")); // Obj is not a guid
    }

    public static IEnumerable<object[]> Equals_TestData()
    {
        yield return new object[] { s_testGuid, s_testGuid, true };
        yield return new object[] { s_testGuid, new Guid("a8a110d5-fc49-43c5-bf46-802db8f843ff"), true };
        yield return new object[] { s_testGuid, Guid.Empty, false };

        yield return new object[] { s_testGuid, "a8a110d5-fc49-43c5-bf46-802db8f843ff", false };
        yield return new object[] { s_testGuid, null, false };
    }

    [Theory]
    [MemberData(nameof(Equals_TestData))]
    public static void TestEquals(Guid guid1, object obj, bool expected)
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
    public static void TestToByteArray()
    {
        Assert.Equal(new byte[] { 0xd5, 0x10, 0xa1, 0xa8, 0x49, 0xfc, 0xc5, 0x43, 0xbf, 0x46, 0x80, 0x2d, 0xb8, 0xf8, 0x43, 0xff }, s_testGuid.ToByteArray());
    }

    public static IEnumerable<object[]> ToString_TestData()
    {
        yield return new object[] { s_testGuid, "N", "a8a110d5fc4943c5bf46802db8f843ff" };
        yield return new object[] { s_testGuid, "D", "a8a110d5-fc49-43c5-bf46-802db8f843ff" };
        yield return new object[] { s_testGuid, "B", "{a8a110d5-fc49-43c5-bf46-802db8f843ff}" };
        yield return new object[] { s_testGuid, "P", "(a8a110d5-fc49-43c5-bf46-802db8f843ff)" };
        yield return new object[] { s_testGuid, "X", "{0xa8a110d5,0xfc49,0x43c5,{0xbf,0x46,0x80,0x2d,0xb8,0xf8,0x43,0xff}}" };

        yield return new object[] { s_testGuid, null, "a8a110d5-fc49-43c5-bf46-802db8f843ff" };
        yield return new object[] { s_testGuid, "", "a8a110d5-fc49-43c5-bf46-802db8f843ff" };
    }

    [Theory]
    [MemberData(nameof(ToString_TestData))]
    public static void TestToString(Guid guid, string format, string expected)
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

    [Fact]
    public static void TestToString_Invalid()
    {
        Assert.Throws<FormatException>(() => s_testGuid.ToString("Y")); // Invalid format
        Assert.Throws<FormatException>(() => s_testGuid.ToString("XX")); // Invalid format
    }

    public static IEnumerable<object[]> GuidStrings_Valid_TestData()
    {
        yield return new object[] { "a8a110d5fc4943c5bf46802db8f843ff", "N", s_testGuid };
        yield return new object[] { "a8a110d5-fc49-43c5-bf46-802db8f843ff", "D", s_testGuid };
        yield return new object[] { "{a8a110d5-fc49-43c5-bf46-802db8f843ff}", "B", s_testGuid };
        yield return new object[] { "(a8a110d5-fc49-43c5-bf46-802db8f843ff)", "P", s_testGuid };
        yield return new object[] { "{0xa8a110d5,0xfc49,0x43c5,{0xbf,0x46,0x80,0x2d,0xb8,0xf8,0x43,0xff}}", "X", s_testGuid };
    }

    public static IEnumerable<object[]> GuidStrings_Invalid_TestData()
    {
        yield return new object[] { null, typeof(ArgumentNullException) }; // String is null

        yield return new object[] { "", typeof(FormatException) }; // String is invalid
        yield return new object[] { "     \t", typeof(FormatException) }; // String is invalid

        yield return new object[] { "ddddddddddddddddddddddddddddddd", typeof(FormatException) }; // Length < 32
        yield return new object[] { "ddddddddddddddddddddddddddddddddd", typeof(FormatException) }; // Length > 32
        yield return new object[] { "{dddddddddddddddddddddddddddddddd}", typeof(FormatException) }; // Surrounded by braces

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

        yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd}", typeof(FormatException) }; // 8-4-4
        yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} - missing group
        yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2-2} - extra group

        yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} with leading brace only
        yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} with leading brace only

        yield return new object[] { "0xdddddddd, 0xdddd, 0xdddd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} with trailing brace only
        yield return new object[] { "0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} with trailing brace only

        yield return new object[] { "(0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd))", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} with parentheses
        yield return new object[] { "(0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd))", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} with parentheses

        yield return new object[] { "{0xdddddddd 0xdddd 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(OverflowException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without comma
        yield return new object[] { "{0xdddddddd, 0xdddd 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without comma
        yield return new object[] { "{0xdddddddd, 0xdddd 0xdddd{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without comma

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
        yield return new object[] { "{0xddddddddd, 0x, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(OverflowException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
        yield return new object[] { "{0xddddddddd, 0xdddd, 0x,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(OverflowException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
        yield return new object[] { "{0xddddddddd, 0xdddd, 0xdddd,{0x,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(OverflowException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
        yield return new object[] { "{0xddddddddd, 0xdddd, 0xdddd,{0xdd,0x,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(OverflowException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
        yield return new object[] { "{0xddddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0x,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(OverflowException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
        yield return new object[] { "{0xddddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0x,0xdd,0xdd,0xdd,0xdd}}", typeof(OverflowException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
        yield return new object[] { "{0xddddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0x,0xdd,0xdd,0xdd}}", typeof(OverflowException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
        yield return new object[] { "{0xddddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0x,0xdd,0xdd}}", typeof(OverflowException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
        yield return new object[] { "{0xddddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0x,0xdd}}", typeof(OverflowException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x
        yield return new object[] { "{0xddddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0x}}", typeof(OverflowException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without string after 0x

        yield return new object[] { "{0xddddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(OverflowException) }; // 9-4-4-{2-2-2-2-2-2-2-2}
        yield return new object[] { "{0xdddddddd, 0xddddd, 0xdddd,{0xdd,0xddd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-5-4-{2-2-2-2-2-2-2-2}
        yield return new object[] { "{0xdddddddd, 0xdddd, 0xddddd,{0xdd,0xddd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-5-4-{2-2-2-2-2-2-2-2}
        yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xddd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{3-2-2-2-2-2-2-2}
        yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xddd,0xdd,0xddd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-3-2-2-2-2-2-2}
        yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xddd,0xdd,0xddd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-3-2-2-2-2-2}
        yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xddd,0xdd,0xdd,0xddd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-3-2-2-2-2}
        yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xddd,0xddd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-3-2-2}
        yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xddd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-3-2}
        yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xddd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-3}

        yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without comma
        yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd0xdd,0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without comma
        yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd0xdd,0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without comma
        yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd0xdd,0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without comma
        yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd0xdd,0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without comma
        yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd0xdd,0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without comma
        yield return new object[] { "{0xdddddddd, 0xdddd, 0xdddd,{0xdd,0xdd,0xdd,0xdd,0xdd,0xdd,0xdd0xdd}}", typeof(FormatException) }; // 8-4-4-{2-2-2-2-2-2-2-2} without comma
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
}
