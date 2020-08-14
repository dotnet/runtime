// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public class ConversionTests
    {
        [Theory]
        [MemberData(nameof(CTypeDynamic_Byte_TestData))]
        public void CTypeDynamic_Byte(object expression, object expected)
        {
            CTypeDynamic<byte>(expression, expected);
        }

        [Theory]
        [MemberData(nameof(CTypeDynamic_SByte_TestData))]
        public void CTypeDynamic_SByte(object expression, object expected)
        {
            CTypeDynamic<sbyte>(expression, expected);
        }

        [Theory]
        [MemberData(nameof(CTypeDynamic_UShort_TestData))]
        public void CTypeDynamic_UShort(object expression, object expected)
        {
            CTypeDynamic<ushort>(expression, expected);
        }

        [Theory]
        [MemberData(nameof(CTypeDynamic_Short_TestData))]
        public void CTypeDynamic_Short(object expression, object expected)
        {
            CTypeDynamic<short>(expression, expected);
        }

        [Theory]
        [MemberData(nameof(CTypeDynamic_UInteger_TestData))]
        public void CTypeDynamic_UInteger(object expression, object expected)
        {
            CTypeDynamic<uint>(expression, expected);
        }

        [Theory]
        [MemberData(nameof(CTypeDynamic_Integer_TestData))]
        public void CTypeDynamic_Integer(object expression, object expected)
        {
            CTypeDynamic<int>(expression, expected);
        }

        [Theory]
        [MemberData(nameof(CTypeDynamic_ULong_TestData))]
        public void CTypeDynamic_ULong(object expression, object expected)
        {
            CTypeDynamic<ulong>(expression, expected);
        }

        [Theory]
        [MemberData(nameof(CTypeDynamic_Long_TestData))]
        public void CTypeDynamic_Long(object expression, object expected)
        {
            CTypeDynamic<long>(expression, expected);
        }

        [Theory]
        [MemberData(nameof(CTypeDynamic_Single_TestData))]
        public void CTypeDynamic_Single(object expression, object expected)
        {
            CTypeDynamic<float>(expression, expected);
        }

        [Theory]
        [MemberData(nameof(CTypeDynamic_Double_TestData))]
        public void CTypeDynamic_Double(object expression, object expected)
        {
            CTypeDynamic<double>(expression, expected);
        }

        [Theory]
        [MemberData(nameof(CTypeDynamic_Decimal_TestData))]
        public void CTypeDynamic_Decimal(object expression, object expected)
        {
            CTypeDynamic<decimal>(expression, expected);
        }

        [Theory]
        [MemberData(nameof(CTypeDynamic_Char_TestData))]
        public void CTypeDynamic_Char(object expression, object expected)
        {
            CTypeDynamic<char>(expression, expected);
        }

        [Theory]
        [MemberData(nameof(CTypeDynamic_String_TestData))]
        public void CTypeDynamic_String(object expression, object expected)
        {
            CTypeDynamic<string>(expression, expected);
        }

        private static void CTypeDynamic<T>(object expression, object expected)
        {
            Assert.Equal(expected, Conversion.CTypeDynamic(expression, typeof(T)));
            Assert.Equal(expected, Conversion.CTypeDynamic<T>(expression));
        }

        public static IEnumerable<object[]> CTypeDynamic_Byte_TestData() => ConversionsTests.ToByte_Object_TestData();
        public static IEnumerable<object[]> CTypeDynamic_SByte_TestData() => ConversionsTests.ToSByte_Object_TestData();
        public static IEnumerable<object[]> CTypeDynamic_UShort_TestData() => ConversionsTests.ToUShort_Object_TestData();
        public static IEnumerable<object[]> CTypeDynamic_Short_TestData() => ConversionsTests.ToShort_Object_TestData();
        public static IEnumerable<object[]> CTypeDynamic_UInteger_TestData() => ConversionsTests.ToUInteger_Object_TestData();
        public static IEnumerable<object[]> CTypeDynamic_Integer_TestData() => ConversionsTests.ToInteger_Object_TestData();
        public static IEnumerable<object[]> CTypeDynamic_ULong_TestData() => ConversionsTests.ToULong_Object_TestData();
        public static IEnumerable<object[]> CTypeDynamic_Long_TestData() => ConversionsTests.ToLong_Object_TestData();
        public static IEnumerable<object[]> CTypeDynamic_Single_TestData() => ConversionsTests.ToSingle_Object_TestData();
        public static IEnumerable<object[]> CTypeDynamic_Double_TestData() => ConversionsTests.ToDouble_Object_TestData();
        public static IEnumerable<object[]> CTypeDynamic_Decimal_TestData() => ConversionsTests.ToDecimal_Object_TestData();
        public static IEnumerable<object[]> CTypeDynamic_Char_TestData() => ConversionsTests.ToChar_Object_TestData();
        public static IEnumerable<object[]> CTypeDynamic_String_TestData() => ConversionsTests.ToString_Object_TestData();

        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        public void CTypeDynamic_ArgumentException(object expression, Type targetType)
        {
            Assert.Throws<ArgumentException>(() => Conversion.CTypeDynamic(expression, targetType));
        }

        [Fact]
        public void ErrorToString()
        {
            Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(new System.IO.FileNotFoundException());
            Assert.NotNull(Conversion.ErrorToString());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(7)]
        [InlineData(int.MinValue)]
        public void ErrorToString_WithErrorNumber(int errorNumber)
        {
            Assert.NotNull(Conversion.ErrorToString(errorNumber));
        }

        [Theory]
        [InlineData(int.MaxValue)]
        public void ErrorToString_ArgumentException(int errorNumber)
        {
            Assert.Throws<ArgumentException>(() => Conversion.ErrorToString(errorNumber));
        }

        [Theory]
        [InlineData(true, 1)]
        [InlineData(false, 0)]
        [InlineData("0", 0.0)]
        [InlineData("1", 1.0)]
        [InlineData("-1", -1.0)]
        [MemberData(nameof(Fix_Short_TestData))]
        [MemberData(nameof(Fix_Integer_TestData))]
        [MemberData(nameof(Fix_Long_TestData))]
        [MemberData(nameof(Fix_Single_TestData))]
        [MemberData(nameof(Fix_Double_TestData))]
        [MemberData(nameof(Fix_Decimal_TestData))]
        public void Fix_Object(object value, object expected)
        {
            Assert.Equal(expected, Conversion.Fix(value));
        }

        [Theory]
        [MemberData(nameof(Fix_Short_TestData))]
        public void Fix_Short(short value, short expected)
        {
            Assert.Equal(expected, Conversion.Fix(value));
        }

        [Theory]
        [MemberData(nameof(Fix_Integer_TestData))]
        public void Fix_Int(int value, int expected)
        {
            Assert.Equal(expected, Conversion.Fix(value));
        }

        [Theory]
        [MemberData(nameof(Fix_Long_TestData))]
        public void Fix_Long(long value, long expected)
        {
            Assert.Equal(expected, Conversion.Fix(value));
        }

        [Theory]
        [MemberData(nameof(Fix_Single_TestData))]
        public void Fix_Float(float value, float expected)
        {
            Assert.Equal(expected, Conversion.Fix(value));
        }

        [Theory]
        [MemberData(nameof(Fix_Double_TestData))]
        public void Fix_Double(double value, double expected)
        {
            Assert.Equal(expected, Conversion.Fix(value));
        }

        [Theory]
        [MemberData(nameof(Fix_Decimal_TestData))]
        public void Fix_Decimal(decimal value, decimal expected)
        {
            Assert.Equal(expected, Conversion.Fix(value));
        }

        [Theory]
        [InlineData(char.MinValue)]
        [InlineData(char.MaxValue)]
        [MemberData(nameof(Various_ArgumentException_TestData))]
        public void Fix_ArgumentException(object value)
        {
            Assert.Throws<ArgumentException>(() => Conversion.Fix(value));
        }

        [Theory]
        [InlineData(null)]
        public void Fix_ArgumentNullException(object value)
        {
            Assert.Throws<ArgumentNullException>(() => Conversion.Fix(value));
        }

        public static IEnumerable<object[]> Fix_Short_TestData()
        {
            yield return new object[] { short.MinValue, short.MinValue };
            yield return new object[] { (short)-1, (short)-1 };
            yield return new object[] { (short)0, (short)0 };
            yield return new object[] { (short)1, (short)1 };
            yield return new object[] { short.MaxValue, short.MaxValue };
        }

        public static IEnumerable<object[]> Fix_Integer_TestData()
        {
            yield return new object[] { int.MinValue, int.MinValue };
            yield return new object[] { -1, -1 };
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 1 };
            yield return new object[] { int.MaxValue, int.MaxValue };
        }

        public static IEnumerable<object[]> Fix_Long_TestData()
        {
            yield return new object[] { long.MinValue, long.MinValue };
            yield return new object[] { -1L, -1L };
            yield return new object[] { 0L, 0L };
            yield return new object[] { 1L, 1L };
            yield return new object[] { long.MaxValue, long.MaxValue };
        }

        public static IEnumerable<object[]> Fix_Single_TestData()
        {
            yield return new object[] { float.MinValue, float.MinValue };
            yield return new object[] { -999.999f, -999.0f };
            yield return new object[] { -1.9f, -1.0f };
            yield return new object[] { 0.0f, 0.0f };
            yield return new object[] { 1.9f, 1.0f };
            yield return new object[] { 999.999f, 999.0f };
            yield return new object[] { float.MaxValue, float.MaxValue };
        }

        public static IEnumerable<object[]> Fix_Double_TestData()
        {
            yield return new object[] { double.MinValue, double.MinValue };
            yield return new object[] { -999.999, -999.0 };
            yield return new object[] { -1.9, -1.0 };
            yield return new object[] { 0.0, 0.0 };
            yield return new object[] { 1.9, 1.0 };
            yield return new object[] { 999.999, 999.0 };
            yield return new object[] { double.MaxValue, double.MaxValue };
            yield return new object[] { Math.E, (double)2 };
            yield return new object[] { Math.PI, (double)3 };
        }

        public static IEnumerable<object[]> Fix_Decimal_TestData()
        {
            yield return new object[] { decimal.MinValue, decimal.MinValue };
            yield return new object[] { (decimal)-999.999, (decimal)-999.0 };
            yield return new object[] { (decimal)-1.9, (decimal)-1.0 };
            yield return new object[] { (decimal)0, (decimal)0 };
            yield return new object[] { (decimal)1.9, (decimal)1.0 };
            yield return new object[] { (decimal)999.999, (decimal)999.0 };
            yield return new object[] { decimal.MaxValue, decimal.MaxValue };
        }

        [Theory]
        [InlineData((long)-1, "FFFFFFFF")] // expected for long overload: "FFFFFFFFFFFFFFFF"
        [InlineData("9223372036854775807", "7FFFFFFFFFFFFFFF")] // long
        [InlineData("18446744073709551615", "FFFFFFFFFFFFFFFF")] // ulong
        [MemberData(nameof(Hex_Byte_TestData))]
        [MemberData(nameof(Hex_SByte_TestData))]
        [MemberData(nameof(Hex_UShort_TestData))]
        [MemberData(nameof(Hex_Short_TestData))]
        [MemberData(nameof(Hex_UInteger_TestData))]
        [MemberData(nameof(Hex_Integer_TestData))]
        [MemberData(nameof(Hex_ULong_TestData))]
        [MemberData(nameof(Hex_Long_TestData))]
        [MemberData(nameof(Hex_Single_TestData))]
        [MemberData(nameof(Hex_Double_TestData))]
        [MemberData(nameof(Hex_Decimal_TestData))]
        public void Hex_Object(object value, string expected)
        {
            Assert.Equal(expected, Conversion.Hex(value));
        }

        [Theory]
        [MemberData(nameof(Hex_Byte_TestData))]
        public void Hex_Byte(byte value, string expected)
        {
            Assert.Equal(expected, Conversion.Hex(value));
        }

        [Theory]
        [MemberData(nameof(Hex_SByte_TestData))]
        public void Hex_Sbyte(sbyte value, string expected)
        {
            Assert.Equal(expected, Conversion.Hex(value));
        }

        [Theory]
        [MemberData(nameof(Hex_UShort_TestData))]
        public void Hex_Ushort(ushort value, string expected)
        {
            Assert.Equal(expected, Conversion.Hex(value));
        }

        [Theory]
        [MemberData(nameof(Hex_Short_TestData))]
        public void Hex_Short(short value, string expected)
        {
            Assert.Equal(expected, Conversion.Hex(value));
        }

        [Theory]
        [MemberData(nameof(Hex_UInteger_TestData))]
        public void Hex_Uint(uint value, string expected)
        {
            Assert.Equal(expected, Conversion.Hex(value));
        }

        [Theory]
        [MemberData(nameof(Hex_Integer_TestData))]
        public void Hex_Int(int value, string expected)
        {
            Assert.Equal(expected, Conversion.Hex(value));
        }

        [Theory]
        [MemberData(nameof(Hex_ULong_TestData))]
        public void Hex_Ulong(ulong value, string expected)
        {
            Assert.Equal(expected, Conversion.Hex(value));
        }

        [Theory]
        [InlineData((long)-1, "FFFFFFFFFFFFFFFF")] // expected for object overload: "FFFFFFFF"
        [MemberData(nameof(Hex_Long_TestData))]
        public void Hex_Long(long value, string expected)
        {
            Assert.Equal(expected, Conversion.Hex(value));
        }

        [Theory]
        [MemberData(nameof(Hex_Single_TestData))]
        public void Hex_Float(float value, string expected)
        {
            Assert.Equal(expected, Conversion.Hex(value));
        }

        [Theory]
        [MemberData(nameof(Hex_Double_TestData))]
        public void Hex_Double(double value, string expected)
        {
            Assert.Equal(expected, Conversion.Hex(value));
        }

        [Theory]
        [MemberData(nameof(Hex_Decimal_TestData))]
        public void Hex_Decimal(decimal value, string expected)
        {
            Assert.Equal(expected, Conversion.Hex(value));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(char.MinValue)]
        [MemberData(nameof(Various_ArgumentException_TestData))]
        public void Hex_ArgumentException(object value)
        {
            Assert.Throws<ArgumentException>(() => Conversion.Hex(value));
        }

        [Theory]
        [InlineData(null)]
        public void Hex_ArgumentNullException(object value)
        {
            Assert.Throws<ArgumentNullException>(() => Conversion.Hex(value));
        }

        public static IEnumerable<object[]> Hex_Byte_TestData()
        {
            yield return new object[] { byte.MinValue, "0" };
            yield return new object[] { (byte)0, "0" };
            yield return new object[] { byte.MaxValue, "FF" };
        }

        public static IEnumerable<object[]> Hex_SByte_TestData()
        {
            yield return new object[] { sbyte.MinValue, "80" };
            yield return new object[] { (sbyte)-1, "FF" };
            yield return new object[] { (sbyte)0, "0" };
            yield return new object[] { (sbyte)1, "1" };
            yield return new object[] { (sbyte)15, "F" };
            yield return new object[] { sbyte.MaxValue, "7F" };
        }

        public static IEnumerable<object[]> Hex_UShort_TestData()
        {
            yield return new object[] { ushort.MinValue, "0" };
            yield return new object[] { (ushort)0, "0" };
            yield return new object[] { (ushort)15, "F" };
            yield return new object[] { ushort.MaxValue, "FFFF" };
        }

        public static IEnumerable<object[]> Hex_Short_TestData()
        {
            yield return new object[] { short.MinValue, "8000" };
            yield return new object[] { (short)-1, "FFFF" };
            yield return new object[] { (short)0, "0" };
            yield return new object[] { (short)1, "1" };
            yield return new object[] { (short)15, "F" };
            yield return new object[] { short.MaxValue, "7FFF" };
        }

        public static IEnumerable<object[]> Hex_UInteger_TestData()
        {
            yield return new object[] { uint.MinValue, "0" };
            yield return new object[] { (uint)0, "0" };
            yield return new object[] { (uint)15, "F" };
            yield return new object[] { uint.MaxValue, "FFFFFFFF" };
        }

        public static IEnumerable<object[]> Hex_Integer_TestData()
        {
            yield return new object[] { int.MinValue, "80000000" };
            yield return new object[] { -1, "FFFFFFFF" };
            yield return new object[] { 0, "0" };
            yield return new object[] { 1, "1" };
            yield return new object[] { 15, "F" };
            yield return new object[] { int.MaxValue, "7FFFFFFF" };
        }

        public static IEnumerable<object[]> Hex_ULong_TestData()
        {
            yield return new object[] { ulong.MinValue, "0" };
            yield return new object[] { (ulong)0, "0" };
            yield return new object[] { (ulong)15, "F" };
            yield return new object[] { ulong.MaxValue, "FFFFFFFFFFFFFFFF" };
        }

        public static IEnumerable<object[]> Hex_Long_TestData()
        {
            yield return new object[] { long.MinValue, "8000000000000000" };
            yield return new object[] { (long)0, "0" };
            yield return new object[] { (long)1, "1" };
            yield return new object[] { (long)15, "F" };
            yield return new object[] { long.MaxValue, "7FFFFFFFFFFFFFFF" };
        }

        public static IEnumerable<object[]> Hex_Single_TestData()
        {
            yield break; // Add more...
        }

        public static IEnumerable<object[]> Hex_Double_TestData()
        {
            yield break; // Add more...
        }

        public static IEnumerable<object[]> Hex_Decimal_TestData()
        {
            yield break; // Add more...
        }

        [Theory]
        [InlineData(true, 1)]
        [InlineData(false, 0)]
        [InlineData("0", 0.0)]
        [InlineData("1", 1.0)]
        [InlineData("-1", -1.0)]
        [MemberData(nameof(Int_Short_TestData))]
        [MemberData(nameof(Int_Integer_TestData))]
        [MemberData(nameof(Int_Long_TestData))]
        [MemberData(nameof(Int_Single_TestData))]
        [MemberData(nameof(Int_Double_TestData))]
        [MemberData(nameof(Int_Decimal_TestData))]
        public void Int_Object_Object(object value, object expected)
        {
            Assert.Equal(expected, Conversion.Int(value));
        }

        [Theory]
        [MemberData(nameof(Int_Short_TestData))]
        public void Int_Short(short value, short expected)
        {
            Assert.Equal(expected, Conversion.Int(value));
        }

        [Theory]
        [MemberData(nameof(Int_Integer_TestData))]
        public void Int_Int(int value, int expected)
        {
            Assert.Equal(expected, Conversion.Int(value));
        }

        [Theory]
        [MemberData(nameof(Int_Long_TestData))]
        public void Int_Long(long value, long expected)
        {
            Assert.Equal(expected, Conversion.Int(value));
        }

        [Theory]
        [MemberData(nameof(Int_Single_TestData))]
        public void Int_Float(float value, float expected)
        {
            Assert.Equal(expected, Conversion.Int(value));
        }

        [Theory]
        [MemberData(nameof(Int_Double_TestData))]
        public void Int_Double(double value, double expected)
        {
            Assert.Equal(expected, Conversion.Int(value));
        }

        [Theory]
        [MemberData(nameof(Int_Decimal_TestData))]
        public void Int_Decimal(decimal value, decimal expected)
        {
            Assert.Equal(expected, Conversion.Int(value));
        }

        [Theory]
        [InlineData(char.MinValue)]
        [InlineData(char.MaxValue)]
        [MemberData(nameof(Various_ArgumentException_TestData))]
        public void Int_ArgumentException(object value)
        {
            Assert.Throws<ArgumentException>(() => Conversion.Int(value));
        }

        [Theory]
        [InlineData(null)]
        public void Int_ArgumentNullException(object value)
        {
            Assert.Throws<ArgumentNullException>(() => Conversion.Int(value));
        }

        public static IEnumerable<object[]> Int_Short_TestData()
        {
            yield return new object[] { short.MinValue, short.MinValue };
            yield return new object[] { (short)0, (short)0 };
            yield return new object[] { short.MaxValue, short.MaxValue };
        }

        public static IEnumerable<object[]> Int_Integer_TestData()
        {
            yield return new object[] { int.MinValue, int.MinValue };
            yield return new object[] { 0, 0 };
            yield return new object[] { int.MaxValue, int.MaxValue };
        }

        public static IEnumerable<object[]> Int_Long_TestData()
        {
            yield return new object[] { long.MinValue, long.MinValue };
            yield return new object[] { 0L, 0L };
            yield return new object[] { long.MaxValue, long.MaxValue };
        }

        public static IEnumerable<object[]> Int_Single_TestData()
        {
            yield return new object[] { float.MinValue, float.MinValue };
            yield return new object[] { -999.999f, -1000.0f };
            yield return new object[] { -1.9f, -2.0f };
            yield return new object[] { 0.0f, 0.0f };
            yield return new object[] { 1.9f, 1.0f };
            yield return new object[] { 999.999f, 999.0f };
            yield return new object[] { float.MaxValue, float.MaxValue };
        }

        public static IEnumerable<object[]> Int_Double_TestData()
        {
            yield return new object[] { double.MinValue, double.MinValue };
            yield return new object[] { -999.999, -1000.0 };
            yield return new object[] { -1.9, -2.0 };
            yield return new object[] { 0.0, 0.0 };
            yield return new object[] { 1.9, 1.0 };
            yield return new object[] { 999.999, 999.0 };
            yield return new object[] { double.MaxValue, double.MaxValue };
            yield return new object[] { Math.E, (double)2 };
            yield return new object[] { Math.PI, (double)3 };
        }

        public static IEnumerable<object[]> Int_Decimal_TestData()
        {
            yield return new object[] { decimal.MinValue, decimal.MinValue };
            yield return new object[] { (decimal)-999.999, (decimal)-1000.0 };
            yield return new object[] { (decimal)-1.9, (decimal)-2.0 };
            yield return new object[] { (decimal)0, (decimal)0 };
            yield return new object[] { (decimal)1.9, (decimal)1.0 };
            yield return new object[] { (decimal)999.999, (decimal)999.0 };
            yield return new object[] { decimal.MaxValue, decimal.MaxValue };
        }

        [Theory]
        [InlineData((long)-1, "37777777777")] // expected for long overload: "1777777777777777777777"
        [InlineData("9223372036854775807", "777777777777777777777")] // long
        [InlineData("18446744073709551615", "1777777777777777777777")] // ulong
        [MemberData(nameof(Oct_Byte_TestData))]
        [MemberData(nameof(Oct_SByte_TestData))]
        [MemberData(nameof(Oct_UShort_TestData))]
        [MemberData(nameof(Oct_Short_TestData))]
        [MemberData(nameof(Oct_UInteger_TestData))]
        [MemberData(nameof(Oct_Integer_TestData))]
        [MemberData(nameof(Oct_ULong_TestData))]
        [MemberData(nameof(Oct_Long_TestData))]
        [MemberData(nameof(Oct_Single_TestData))]
        [MemberData(nameof(Oct_Double_TestData))]
        [MemberData(nameof(Oct_Decimal_TestData))]
        public void Oct_Object(object value, string expected)
        {
            Assert.Equal(expected, Conversion.Oct(value));
        }

        [Theory]
        [MemberData(nameof(Oct_Byte_TestData))]
        public void Oct_Byte(byte value, string expected)
        {
            Assert.Equal(expected, Conversion.Oct(value));
        }

        [Theory]
        [MemberData(nameof(Oct_SByte_TestData))]
        public void Oct_Sbyte(sbyte value, string expected)
        {
            Assert.Equal(expected, Conversion.Oct(value));
        }

        [Theory]
        [MemberData(nameof(Oct_UShort_TestData))]
        public void Oct_Ushort(ushort value, string expected)
        {
            Assert.Equal(expected, Conversion.Oct(value));
        }

        [Theory]
        [MemberData(nameof(Oct_Short_TestData))]
        public void Oct_Short(short value, string expected)
        {
            Assert.Equal(expected, Conversion.Oct(value));
        }

        [Theory]
        [MemberData(nameof(Oct_UInteger_TestData))]
        public void Oct_Uint(uint value, string expected)
        {
            Assert.Equal(expected, Conversion.Oct(value));
        }

        [Theory]
        [MemberData(nameof(Oct_Integer_TestData))]
        public void Oct_Int(int value, string expected)
        {
            Assert.Equal(expected, Conversion.Oct(value));
        }

        [Theory]
        [MemberData(nameof(Oct_ULong_TestData))]
        public void Oct_Ulong(ulong value, string expected)
        {
            Assert.Equal(expected, Conversion.Oct(value));
        }

        [Theory]
        [InlineData((long)-1, "1777777777777777777777")] // expected for object overload: "37777777777"
        [MemberData(nameof(Oct_Long_TestData))]
        public void Oct_Long(long value, string expected)
        {
            Assert.Equal(expected, Conversion.Oct(value));
        }

        [Theory]
        [MemberData(nameof(Oct_Single_TestData))]
        public void Oct_Float(float value, string expected)
        {
            Assert.Equal(expected, Conversion.Oct(value));
        }

        [Theory]
        [MemberData(nameof(Oct_Double_TestData))]
        public void Oct_Double(double value, string expected)
        {
            Assert.Equal(expected, Conversion.Oct(value));
        }

        [Theory]
        [MemberData(nameof(Oct_Decimal_TestData))]
        public void Oct_Decimal(decimal value, string expected)
        {
            Assert.Equal(expected, Conversion.Oct(value));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(char.MinValue)]
        [InlineData(char.MaxValue)]
        [MemberData(nameof(Various_ArgumentException_TestData))]
        public void Oct_ArgumentException(object value)
        {
            Assert.Throws<ArgumentException>(() => Conversion.Oct(value));
        }

        [Theory]
        [InlineData(null)]
        public void Oct_ArgumentNullException(object value)
        {
            Assert.Throws<ArgumentNullException>(() => Conversion.Oct(value));
        }

        public static IEnumerable<object[]> Oct_Byte_TestData()
        {
            yield return new object[] { byte.MinValue, "0" };
            yield return new object[] { (byte)0, "0" };
            yield return new object[] { (byte)15, "17" };
            yield return new object[] { byte.MaxValue, "377" };
        }

        public static IEnumerable<object[]> Oct_SByte_TestData()
        {
            yield return new object[] { sbyte.MinValue, "200" };
            yield return new object[] { (sbyte)-1, "377" };
            yield return new object[] { (sbyte)0, "0" };
            yield return new object[] { (sbyte)1, "1" };
            yield return new object[] { (sbyte)15, "17" };
            yield return new object[] { sbyte.MaxValue, "177" };
        }

        public static IEnumerable<object[]> Oct_UShort_TestData()
        {
            yield return new object[] { ushort.MinValue, "0" };
            yield return new object[] { (ushort)0, "0" };
            yield return new object[] { (ushort)1, "1" };
            yield return new object[] { (ushort)15, "17" };
            yield return new object[] { ushort.MaxValue, "177777" };
        }

        public static IEnumerable<object[]> Oct_Short_TestData()
        {
            yield return new object[] { short.MinValue, "100000" };
            yield return new object[] { (short)-1, "177777" };
            yield return new object[] { (short)0, "0" };
            yield return new object[] { (short)1, "1" };
            yield return new object[] { (ushort)15, "17" };
            yield return new object[] { short.MaxValue, "77777" };
        }

        public static IEnumerable<object[]> Oct_UInteger_TestData()
        {
            yield return new object[] { uint.MinValue, "0" };
            yield return new object[] { (uint)0, "0" };
            yield return new object[] { (uint)1, "1" };
            yield return new object[] { (uint)15, "17" };
            yield return new object[] { uint.MaxValue, "37777777777" };
        }

        public static IEnumerable<object[]> Oct_Integer_TestData()
        {
            yield return new object[] { int.MinValue, "20000000000" };
            yield return new object[] { -1, "37777777777" };
            yield return new object[] { 0, "0" };
            yield return new object[] { 1, "1" };
            yield return new object[] { 15, "17" };
            yield return new object[] { int.MaxValue, "17777777777" };
        }

        public static IEnumerable<object[]> Oct_ULong_TestData()
        {
            yield return new object[] { ulong.MinValue, "0" };
            yield return new object[] { (ulong)0, "0" };
            yield return new object[] { (ulong)1, "1" };
            yield return new object[] { (ulong)15, "17" };
            yield return new object[] { ulong.MaxValue, "1777777777777777777777" };
        }

        public static IEnumerable<object[]> Oct_Long_TestData()
        {
            yield return new object[] { long.MinValue, "10" };
            yield return new object[] { (long)0, "0" };
            yield return new object[] { (long)1, "1" };
            yield return new object[] { (long)15, "17" };
            yield return new object[] { long.MaxValue, "777777777777777777777" };
        }

        public static IEnumerable<object[]> Oct_Single_TestData()
        {
            yield break; // Add more...
        }

        public static IEnumerable<object[]> Oct_Double_TestData()
        {
            yield break; // Add more...
        }

        public static IEnumerable<object[]> Oct_Decimal_TestData()
        {
            yield break; // Add more...
        }

        [Theory]
        [MemberData(nameof(Str_TestData))]
        public void Str(object value, string expected)
        {
            Assert.Equal(expected, Conversion.Str(value));
        }

        [Theory]
        [InlineData(null)]
        public void Str_ArgumentNullException(object value)
        {
            Assert.Throws<ArgumentNullException>(() => Conversion.Str(value));
        }

        [Theory]
        [MemberData(nameof(Str_Object_InvalidCastException_TestData))]
        public void Str_InvalidCastException(object value)
        {
            Assert.Throws<InvalidCastException>(() => Conversion.Str(value));
        }

        public static IEnumerable<object[]> Str_TestData()
        {
            yield return new object[] { 0, " 0" };
            yield return new object[] { 1, " 1" };
            yield return new object[] { -1, "-1" };
            yield return new object[] { DBNull.Value, "Null" };
            yield return new object[] { true, "True" };
            yield return new object[] { false, "False" };
            yield return new object[] { "0", " 0" };
        }

        public static IEnumerable<object[]> Str_Object_InvalidCastException_TestData()
        {
            yield return new object[] { new object() };
            yield return new object[] { string.Empty };
        }

        [Theory]
        [MemberData(nameof(Val_Object_TestData))]
        [MemberData(nameof(Val_Char_TestData))]
        [MemberData(nameof(Val_String_TestData))]
        public void Val_Object_Double(object value, double expected)
        {
            Assert.Equal(expected, Conversion.Val(value));
        }

        [Theory]
        [MemberData(nameof(Val_Object_ArgumentException_TestData))]
        public void Val_ArgumentException_Object(object value)
        {
            Assert.Throws<ArgumentException>(() => Conversion.Val(value));
        }

        [Theory]
        [MemberData(nameof(Val_Object_OverflowException_TestData))]
        public void Val_OverflowException_Object(object value)
        {
            Assert.Throws<OverflowException>(() => Conversion.Val(value));
        }

        [Theory]
        [MemberData(nameof(Val_Char_TestData))]
        public void Val_Char_Int(char value, int expected)
        {
            Assert.Equal(expected, Conversion.Val(value));
        }

        [Theory]
        [MemberData(nameof(Val_Char_ArgumentException_TestData))]
        public void Val_ArgumentException_Char(char value)
        {
            Assert.Throws<ArgumentException>(() => Conversion.Val(value));
        }

        [Theory]
        [MemberData(nameof(Val_Char_OverflowException_TestData))]
        public void Val_OverflowException_Char(char value)
        {
            Assert.Throws<OverflowException>(() => Conversion.Val(value));
        }

        [Theory]
        [MemberData(nameof(Val_String_TestData))]
        public void Val_String_Double(string value, double expected)
        {
            Assert.Equal(expected, Conversion.Val(value));
        }

        [Theory]
        [MemberData(nameof(Val_String_ArgumentException_TestData))]
        public void Val_ArgumentException_String(string value)
        {
            Assert.Throws<ArgumentException>(() => Conversion.Val(value));
        }

        [Theory]
        [MemberData(nameof(Val_String_InvalidCastException_TestData))]
        public void Val_InvalidCastException(string value)
        {
            Assert.Throws<InvalidCastException>(() => Conversion.Val(value));
        }

        [Theory]
        [MemberData(nameof(Val_String_OverflowException_TestData))]
        public void Val_OverflowException_String(string value)
        {
            Assert.Throws<OverflowException>(() => Conversion.Val(value));
        }

        public static IEnumerable<object[]> Val_Object_TestData()
        {
            yield return new object[] { 0, "0" };
            yield return new object[] { 1, " 1" };
        }

        public static IEnumerable<object[]> Val_Object_ArgumentException_TestData()
        {
            yield break; // Add more...
        }

        public static IEnumerable<object[]> Val_Object_OverflowException_TestData()
        {
            yield break; // Add more...
        }

        public static IEnumerable<object[]> Val_Char_TestData()
        {
            yield return new object[] { (char)0, 0 };
            yield return new object[] { '0', 0 };
            yield return new object[] { '1', 1 };
            yield return new object[] { '2', 2 };
            yield return new object[] { '3', 3 };
            yield return new object[] { '4', 4 };
            yield return new object[] { '5', 5 };
            yield return new object[] { '6', 6 };
            yield return new object[] { '7', 7 };
            yield return new object[] { '8', 8 };
            yield return new object[] { '9', 9 };
            yield return new object[] { 'A', 0 };
            yield return new object[] { char.MaxValue, 0 };
        }

        public static IEnumerable<object[]> Val_Char_ArgumentException_TestData()
        {
            yield break; // Add more...
        }

        public static IEnumerable<object[]> Val_Char_OverflowException_TestData()
        {
            yield break; // Add more...
        }

        public static IEnumerable<object[]> Val_String_TestData()
        {
            yield return new object[] { null, 0.0 };
            yield return new object[] { "", 0.0 };
            yield return new object[] { "0", 0.0 };
            yield return new object[] { "1", 1.0 };
            yield return new object[] { "1%", 1.0 };
            yield return new object[] { "1&", 1.0 };
            yield return new object[] { "1!", 1.0 };
            yield return new object[] { "1@", 1.0 };
            yield return new object[] { "-1", -1.0 };
            yield return new object[] { "+1", 1.0 };
            yield return new object[] { ".1", 0.1 };
            yield return new object[] { "1.0", 1.0 };
            yield return new object[] { "1. 1", 1.1 };
            yield return new object[] { "1..1", 1.0 };
            yield return new object[] { "1.1.1", 1.1 };
            yield return new object[] { "&H F", 15.0 };
            yield return new object[] { "&O 7", 7.0 };
            yield return new object[] { "&H7F", 127.0 };
            yield return new object[] { "&hff", 255.0 };
            yield return new object[] { "&O177", 127.0 };
            yield return new object[] { "&o377", 255.0 };
            yield return new object[] { "&H0F0", 240.0 };
            yield return new object[] { "&O070", 56.0 };
            yield return new object[] { "-1e20", -1.0e20 };
            yield return new object[] { "1e-3", 1.0e-3 };
            yield return new object[] { "1e+1+", 1.0e+1 };
            yield return new object[] { "1e+1-", 1.0e+1 };
            yield return new object[] { "1eA", 1.0 };
            yield return new object[] { "1.1e +3", 1.1e+3 };
            yield return new object[] { "\t\r\n \x3000", 0.0 };
            yield return new object[] { "1.\t\r\n \x30001", 1.1 };
            yield return new object[] { "&HFFFF%", -1.0 };
            yield return new object[] { "&HFFFFFFFF&", -1.0 };
            yield return new object[] { "&HFFFFFFFFFFFFFFFF", -1.0 };
            yield return new object[] { "&HFFFFFFFFFFFFFFFF", -1.0 };
            yield return new object[] { "&O177777%", -1.0 };
            yield return new object[] { "&O37777777777&", -1.0 };
        }

        public static IEnumerable<object[]> Val_String_ArgumentException_TestData()
        {
            yield break; // Add more...
        }

        public static IEnumerable<object[]> Val_String_InvalidCastException_TestData()
        {
            yield return new object[] { "1.0%" };
            yield return new object[] { "1.0&" };
        }

        public static IEnumerable<object[]> Val_String_OverflowException_TestData()
        {
            yield break; // Add more...
        }

        public static IEnumerable<object[]> Various_ArgumentException_TestData()
        {
            yield return new object[] { DBNull.Value };
            yield return new object[] { new DateTime(2000, 1, 1) };
        }
    }
}
