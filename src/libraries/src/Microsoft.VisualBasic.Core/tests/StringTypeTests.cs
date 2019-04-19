// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualBasic.CompilerServices;
using Microsoft.VisualBasic.Tests;
using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace Microsoft.VisualBasic.CompilerServices.Tests
{
    public class StringTypeTests
    {
        [Theory]
        [MemberData(nameof(FromBoolean_TestData))]
        public void FromBoolean(bool value, string expected)
        {
            Assert.Equal(expected, StringType.FromBoolean(value));
        }

        [Theory]
        [MemberData(nameof(FromByte_TestData))]
        public void FromByte(byte value, string expected)
        {
            Assert.Equal(expected, StringType.FromByte(value));
        }

        [Theory]
        [MemberData(nameof(FromChar_TestData))]
        public void FromChar(char value, string expected)
        {
            Assert.Equal(expected, StringType.FromChar(value));
        }

        [Theory]
        [MemberData(nameof(FromDateTime_TestData))]
        public void FromDate(DateTime value, string expected)
        {
            Assert.Equal(expected, StringType.FromDate(value));
        }

        [Theory]
        [MemberData(nameof(FromDecimal_TestData))]
        public void FromDecimal(decimal value, string expected)
        {
            Assert.Equal(expected, StringType.FromDecimal(value));
            Assert.Equal(expected, StringType.FromDecimal(value, default(NumberFormatInfo)));
        }

        [Theory]
        [MemberData(nameof(FromDecimal_Format_TestData))]
        public void FromDecimal(decimal value, NumberFormatInfo format, string expected)
        {
            Assert.Equal(expected, StringType.FromDecimal(value, format));
        }

        [Theory]
        [MemberData(nameof(FromDouble_TestData))]
        public void FromDouble(double value, string expected)
        {
            Assert.Equal(expected, StringType.FromDouble(value));
            Assert.Equal(expected, StringType.FromDouble(value, default(NumberFormatInfo)));
        }

        [Theory]
        [MemberData(nameof(FromDouble_Format_TestData))]
        public void FromDouble(double value, NumberFormatInfo format, string expected)
        {
            Assert.Equal(expected, StringType.FromDouble(value, format));
        }

        [Theory]
        [MemberData(nameof(FromInt32_TestData))]
        public void FromInteger(int value, string expected)
        {
            Assert.Equal(expected, StringType.FromInteger(value));
        }

        [Theory]
        [MemberData(nameof(FromInt64_TestData))]
        public void FromLong(long value, string expected)
        {
            Assert.Equal(expected, StringType.FromLong(value));
        }

        [Theory]
        [MemberData(nameof(FromByte_TestData))]
        [MemberData(nameof(FromInt16_TestData))]
        [MemberData(nameof(FromInt32_TestData))]
        [MemberData(nameof(FromInt64_TestData))]
        [MemberData(nameof(FromSingle_TestData))]
        [MemberData(nameof(FromDouble_TestData))]
        [MemberData(nameof(FromDecimal_TestData))]
        [MemberData(nameof(FromBoolean_TestData))]
        [MemberData(nameof(FromString_TestData))]
        [MemberData(nameof(FromNull_TestData))]
        [MemberData(nameof(FromChar_TestData))]
        [MemberData(nameof(FromCharArray_TestData))]
        [MemberData(nameof(FromDateTime_TestData))]
        public void FromObject(object value, string expected)
        {
            Assert.Equal(expected, StringType.FromObject(value));
        }

        // The following conversions are not supported.
        [Theory]
        [MemberData(nameof(FromSByte_TestData))]
        [MemberData(nameof(FromUInt16_TestData))]
        [MemberData(nameof(FromUInt32_TestData))]
        [MemberData(nameof(FromUInt64_TestData))]
        public void FromObject_NotSupported(object value, string expected)
        {
            Assert.Throws<InvalidCastException>(() => StringType.FromObject(value));
        }

        [Theory]
        [MemberData(nameof(FromObject_TestData))]
        public void FromObject_InvalidCastException(object value)
        {
            Assert.Throws<InvalidCastException>(() => StringType.FromObject(value));
        }

        [Theory]
        [MemberData(nameof(FromInt16_TestData))]
        public void FromShort(short value, string expected)
        {
            Assert.Equal(expected, StringType.FromShort(value));
        }

        [Theory]
        [MemberData(nameof(FromSingle_TestData))]
        public void FromSingle(float value, string expected)
        {
            Assert.Equal(expected, StringType.FromSingle(value));
            Assert.Equal(expected, StringType.FromSingle(value, default(NumberFormatInfo)));
        }

        [Theory]
        [MemberData(nameof(FromSingle_Format_TestData))]
        public void FromSingle(float value, NumberFormatInfo format, string expected)
        {
            Assert.Equal(expected, StringType.FromSingle(value, format));
        }

        private static IEnumerable<object[]> FromByte_TestData()
        {
            yield return new object[] { byte.MinValue, "0" };
            yield return new object[] { (byte)1, "1" };
            yield return new object[] { byte.MaxValue, "255" };
            yield return new object[] { (ByteEnum)byte.MinValue, "0" };
            yield return new object[] { (ByteEnum)1, "1" };
            yield return new object[] { (ByteEnum)byte.MaxValue, "255" };
        }

        private static IEnumerable<object[]> FromSByte_TestData()
        {
            yield return new object[] { sbyte.MinValue, "-128" };
            yield return new object[] { (sbyte)(-1), "-1" };
            yield return new object[] { (sbyte)0, "0" };
            yield return new object[] { (sbyte)1, "1" };
            yield return new object[] { sbyte.MaxValue, "127" };
            yield return new object[] { (SByteEnum)sbyte.MinValue, "-128" };
            yield return new object[] { (SByteEnum)(-1), "-1" };
            yield return new object[] { (SByteEnum)0, "0" };
            yield return new object[] { (SByteEnum)1, "1" };
            yield return new object[] { (SByteEnum)sbyte.MaxValue, "127" };
        }

        private static IEnumerable<object[]> FromUInt16_TestData()
        {
            yield return new object[] { ushort.MinValue, "0" };
            yield return new object[] { (ushort)1, "1" };
            yield return new object[] { ushort.MaxValue, "65535" };
            yield return new object[] { (UShortEnum)ushort.MinValue, "0" };
            yield return new object[] { (UShortEnum)1, "1" };
            yield return new object[] { (UShortEnum)ushort.MaxValue, "65535" };
        }

        private static IEnumerable<object[]> FromInt16_TestData()
        {
            yield return new object[] { short.MinValue, "-32768" };
            yield return new object[] { (short)(-1), "-1" };
            yield return new object[] { (short)0, "0" };
            yield return new object[] { (short)1, "1" };
            yield return new object[] { short.MaxValue, "32767" };
            yield return new object[] { (ShortEnum)short.MinValue, "-32768" };
            yield return new object[] { (ShortEnum)(-1), "-1" };
            yield return new object[] { (ShortEnum)0, "0" };
            yield return new object[] { (ShortEnum)1, "1" };
            yield return new object[] { (ShortEnum)short.MaxValue, "32767" };
        }

        private static IEnumerable<object[]> FromUInt32_TestData()
        {
            yield return new object[] { uint.MinValue, "0" };
            yield return new object[] { (uint)1, "1" };
            yield return new object[] { uint.MaxValue, "4294967295" };
            yield return new object[] { (UIntEnum)uint.MinValue, "0" };
            yield return new object[] { (UIntEnum)1, "1" };
            yield return new object[] { (UIntEnum)uint.MaxValue, "4294967295" };
        }

        private static IEnumerable<object[]> FromInt32_TestData()
        {
            yield return new object[] { int.MinValue, "-2147483648" };
            yield return new object[] { -1, "-1" };
            yield return new object[] { 0, "0" };
            yield return new object[] { 1, "1" };
            yield return new object[] { int.MaxValue, "2147483647" };
            yield return new object[] { (IntEnum)int.MinValue, "-2147483648" };
            yield return new object[] { (IntEnum)(-1), "-1" };
            yield return new object[] { (IntEnum)0, "0" };
            yield return new object[] { (IntEnum)1, "1" };
            yield return new object[] { (IntEnum)int.MaxValue, "2147483647" };
        }

        private static IEnumerable<object[]> FromUInt64_TestData()
        {
            yield return new object[] { ulong.MinValue, "0" };
            yield return new object[] { (ulong)1, "1" };
            yield return new object[] { ulong.MaxValue, "18446744073709551615" };
            yield return new object[] { (ULongEnum)ulong.MinValue, "0" };
            yield return new object[] { (ULongEnum)1, "1" };
            yield return new object[] { (ULongEnum)ulong.MaxValue, "18446744073709551615" };
        }

        private static IEnumerable<object[]> FromInt64_TestData()
        {
            yield return new object[] { long.MinValue, "-9223372036854775808" };
            yield return new object[] { (long)(-1), "-1" };
            yield return new object[] { (long)0, "0" };
            yield return new object[] { (long)1, "1" };
            yield return new object[] { long.MaxValue, "9223372036854775807" };
            yield return new object[] { (LongEnum)long.MinValue, "-9223372036854775808" };
            yield return new object[] { (LongEnum)(-1), "-1" };
            yield return new object[] { (LongEnum)0, "0" };
            yield return new object[] { (LongEnum)1, "1" };
            yield return new object[] { (LongEnum)long.MaxValue, "9223372036854775807" };
        }

        private static IEnumerable<object[]> FromSingle_TestData()
        {
            yield return new object[] { (float)(-1), "-1" };
            yield return new object[] { (float)0, "0" };
            yield return new object[] { (float)1, "1" };
            yield return new object[] { float.PositiveInfinity, float.PositiveInfinity.ToString() };
            yield return new object[] { float.NegativeInfinity, float.NegativeInfinity.ToString() };
            yield return new object[] { float.NaN, "NaN" };
        }

        private static IEnumerable<object[]> FromDouble_TestData()
        {
            yield return new object[] { (double)(-1), "-1" };
            yield return new object[] { (double)0, "0" };
            yield return new object[] { (double)1, "1" };
            yield return new object[] { double.PositiveInfinity, double.PositiveInfinity.ToString() };
            yield return new object[] { double.NegativeInfinity, double.NegativeInfinity.ToString() };
            yield return new object[] { double.NaN, "NaN" };
        }

        private static IEnumerable<object[]> FromDecimal_TestData()
        {
            yield return new object[] { decimal.MinValue, decimal.MinValue.ToString() };
            yield return new object[] { (decimal)(-1), "-1" };
            yield return new object[] { (decimal)0, "0" };
            yield return new object[] { (decimal)1, "1" };
            yield return new object[] { decimal.MaxValue, decimal.MaxValue.ToString() };
        }

        private static IEnumerable<object[]> FromBoolean_TestData()
        {
            yield return new object[] { true, "True" };
            yield return new object[] { false, "False" };
        }

        private static IEnumerable<object[]> FromString_TestData()
        {
            yield return new object[] { "", "" };
            yield return new object[] { "abc", "abc" };
        }

        private static IEnumerable<object[]> FromNull_TestData()
        {
            yield return new object[] { null, (string)null };
        }

        private static IEnumerable<object[]> FromChar_TestData()
        {
            yield return new object[] { char.MinValue, "\0" };
            yield return new object[] { (char)1, "\u0001" };
            yield return new object[] { 'a', "a" };
            yield return new object[] { char.MaxValue, char.MaxValue.ToString() };
        }

        private static IEnumerable<object[]> FromCharArray_TestData()
        {
            yield return new object[] { new char[0], "" };
            yield return new object[] { new char[] { (char)0 }, "\0" };
            yield return new object[] { new char[] { 'A', 'B' }, "AB" };
        }

        private static IEnumerable<object[]> FromDateTime_TestData()
        {
            yield return new object[] { new DateTime(10), new DateTime(10).ToString("T", null) };
        }

        private static IEnumerable<object[]> FromObject_TestData()
        {
            yield return new object[] { new object() };
        }

        private static IEnumerable<object[]> FromSingle_Format_TestData()
        {
            yield return new object[] { (float)(-1), default(NumberFormatInfo), "-1" };
            yield return new object[] { (float)(-1), new NumberFormatInfo() { NegativeSign = "#" }, "#1" };
        }

        private static IEnumerable<object[]> FromDouble_Format_TestData()
        {
            yield return new object[] { (double)(-1), default(NumberFormatInfo), "-1" };
            yield return new object[] { (double)(-1), new NumberFormatInfo() { NegativeSign = "#" }, "#1" };
        }

        private static IEnumerable<object[]> FromDecimal_Format_TestData()
        {
            yield return new object[] { (decimal)(-1), default(NumberFormatInfo), "-1" };
            yield return new object[] { (decimal)(-1), new NumberFormatInfo() { NegativeSign = "#" }, "#1" };
        }

        [Theory]
        [InlineData("a", 1, 0, null, "a")]
        [InlineData("a", 1, 0, "", "a")]
        [InlineData("a", 1, 1, "", "a")]
        [InlineData("a", 1, 0, "b", "a")]
        [InlineData("a", 1, 1, "b", "b")]
        [InlineData("a", 1, 2, "b", "b")]
        [InlineData("abc", 2, 0, "def", "abc")]
        [InlineData("abc", 2, 1, "def", "adc")]
        [InlineData("abc", 2, 2, "def", "ade")]
        [InlineData("abc", 2, 3, "def", "ade")]
        public void MidStmtStr(string str, int start, int length, string insert, string expected)
        {
            StringType.MidStmtStr(ref str, start, length, insert);
            Assert.Equal(expected, str);
        }

        [Theory]
        [InlineData(null, 1, 0, null)]
        [InlineData(null, 1, 0, "")]
        [InlineData("", 1, 0, null)]
        [InlineData("", -1, 0, "")]
        [InlineData("", 0, 0, "")]
        [InlineData("", 1, 0, "")]
        [InlineData("", 2, 0, "")]
        [InlineData("", 1, -1, "")]
        [InlineData("abc", -1, 0, "")]
        [InlineData("abc", 0, 0, "")]
        [InlineData("abc", 4, 0, "")]
        [InlineData("abc", 1, -3, "")]
        public void MidStmtStr_ArgumentException(string str, int start, int length, string insert)
        {
            Assert.Throws<ArgumentException>(() => StringType.MidStmtStr(ref str, start, length, insert));
        }

        [Theory]
        [InlineData(null, null, 0, 0)]
        [InlineData(null, "", 0, 0)]
        [InlineData("", null, 0, 0)]
        [InlineData(null, "a", -1, -1)]
        [InlineData("a", null, 1, 1)]
        [InlineData("", "a", -97, -1)]
        [InlineData("a", "", 97, 1)]
        [InlineData("a", "a", 0, 0)]
        [InlineData("a", "b", -1, -1)]
        [InlineData("b", "a", 1, 1)]
        [InlineData("a", "ABC", 32, -1)]
        [InlineData("ABC", "a", -32, 1)]
        [InlineData("abc", "ABC", 32, 0)]
        public void StrCmp(string left, string right, int expectedBinaryCompare, int expectedTextCompare)
        {
            Assert.Equal(expectedBinaryCompare, StringType.StrCmp(left, right, TextCompare: false));
            Assert.Equal(expectedTextCompare, StringType.StrCmp(left, right, TextCompare: true));
        }

        [Theory]
        [InlineData(null, null, true, true)]
        [InlineData("", null, true, true)]
        [InlineData("", "*", true, true)]
        [InlineData("", "?", false, false)]
        [InlineData("a", "?", true, true)]
        [InlineData("a3", "[A-Z]#", false, true)]
        [InlineData("A3", "[a-z]#", false, true)]
        public void StrLike(string source, string pattern, bool expectedBinaryCompare, bool expectedTextCompare)
        {
            Assert.Equal(expectedBinaryCompare, StringType.StrLike(source, pattern, CompareMethod.Binary));
            Assert.Equal(expectedTextCompare, StringType.StrLike(source, pattern, CompareMethod.Text));
            Assert.Equal(expectedBinaryCompare, StringType.StrLikeBinary(source, pattern));
            Assert.Equal(expectedTextCompare, StringType.StrLikeText(source, pattern));
        }

        [Theory]
        [InlineData(null, "*")]
        public void StrLike_NullReferenceException(string source, string pattern)
        {
            Assert.Throws<NullReferenceException>(() => StringType.StrLike(source, pattern, CompareMethod.Binary));
            Assert.Throws<NullReferenceException>(() => StringType.StrLike(source, pattern, CompareMethod.Text));
            Assert.Throws<NullReferenceException>(() => StringType.StrLikeBinary(source, pattern));
            Assert.Throws<NullReferenceException>(() => StringType.StrLikeText(source, pattern));
        }
    }
}
