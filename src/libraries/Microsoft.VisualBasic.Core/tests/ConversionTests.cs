// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        private static IEnumerable<object[]> CTypeDynamic_Byte_TestData() => ConversionsTests.ToByte_Object_TestData();
        private static IEnumerable<object[]> CTypeDynamic_SByte_TestData() => ConversionsTests.ToSByte_Object_TestData();
        private static IEnumerable<object[]> CTypeDynamic_UShort_TestData() => ConversionsTests.ToUShort_Object_TestData();
        private static IEnumerable<object[]> CTypeDynamic_Short_TestData() => ConversionsTests.ToShort_Object_TestData();
        private static IEnumerable<object[]> CTypeDynamic_UInteger_TestData() => ConversionsTests.ToUInteger_Object_TestData();
        private static IEnumerable<object[]> CTypeDynamic_Integer_TestData() => ConversionsTests.ToInteger_Object_TestData();
        private static IEnumerable<object[]> CTypeDynamic_ULong_TestData() => ConversionsTests.ToULong_Object_TestData();
        private static IEnumerable<object[]> CTypeDynamic_Long_TestData() => ConversionsTests.ToLong_Object_TestData();
        private static IEnumerable<object[]> CTypeDynamic_Single_TestData() => ConversionsTests.ToSingle_Object_TestData();
        private static IEnumerable<object[]> CTypeDynamic_Double_TestData() => ConversionsTests.ToDouble_Object_TestData();
        private static IEnumerable<object[]> CTypeDynamic_Decimal_TestData() => ConversionsTests.ToDecimal_Object_TestData();
        private static IEnumerable<object[]> CTypeDynamic_Char_TestData() => ConversionsTests.ToChar_Object_TestData();
        private static IEnumerable<object[]> CTypeDynamic_String_TestData() => ConversionsTests.ToString_Object_TestData();

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
        public void ErrorToString(int errorNumber)
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
        [MemberData(nameof(Fix_Short_TestData))]
        [MemberData(nameof(Fix_Integer_TestData))]
        [MemberData(nameof(Fix_Long_TestData))]
        [MemberData(nameof(Fix_Single_TestData))]
        [MemberData(nameof(Fix_Double_TestData))]
        [MemberData(nameof(Fix_Decimal_TestData))]
        public void Fix(object value, object expected)
        {
            Assert.Equal(expected, Conversion.Fix(value));
        }

        [Theory]
        [MemberData(nameof(Fix_Short_TestData))]
        public void Fix(short value, short expected)
        {
            Assert.Equal(expected, Conversion.Fix(value));
        }

        [Theory]
        [MemberData(nameof(Fix_Integer_TestData))]
        public void Fix(int value, int expected)
        {
            Assert.Equal(expected, Conversion.Fix(value));
        }

        [Theory]
        [MemberData(nameof(Fix_Long_TestData))]
        public void Fix(long value, long expected)
        {
            Assert.Equal(expected, Conversion.Fix(value));
        }

        [Theory]
        [MemberData(nameof(Fix_Single_TestData))]
        public void Fix(float value, float expected)
        {
            Assert.Equal(expected, Conversion.Fix(value));
        }

        [Theory]
        [MemberData(nameof(Fix_Double_TestData))]
        public void Fix(double value, double expected)
        {
            Assert.Equal(expected, Conversion.Fix(value));
        }

        [Theory]
        [MemberData(nameof(Fix_Decimal_TestData))]
        public void Fix(decimal value, decimal expected)
        {
            Assert.Equal(expected, Conversion.Fix(value));
        }

        private static IEnumerable<object[]> Fix_Short_TestData()
        {
            yield return new object[] { (short)0, (short)0 };
            // Add more...
        }

        private static IEnumerable<object[]> Fix_Integer_TestData()
        {
            yield return new object[] { 0, 0 };
            // Add more...
        }

        private static IEnumerable<object[]> Fix_Long_TestData()
        {
            yield return new object[] { 0L, 0L };
            // Add more...
        }

        private static IEnumerable<object[]> Fix_Single_TestData()
        {
            yield return new object[] { 0.0f, 0.0f };
            // Add more...
        }

        private static IEnumerable<object[]> Fix_Double_TestData()
        {
            yield return new object[] { 0.0, 0.0 };
            // Add more...
        }

        private static IEnumerable<object[]> Fix_Decimal_TestData()
        {
            yield return new object[] { (decimal)0, (decimal)0 };
            // Add more...
        }

        [Theory]
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
        public void Hex(object value, string expected)
        {
            Assert.Equal(expected, Conversion.Hex(value));
        }

        [Theory]
        [MemberData(nameof(Hex_Byte_TestData))]
        public void Hex(byte value, string expected)
        {
            Assert.Equal(expected, Conversion.Hex(value));
        }

        [Theory]
        [MemberData(nameof(Hex_SByte_TestData))]
        public void Hex(sbyte value, string expected)
        {
            Assert.Equal(expected, Conversion.Hex(value));
        }

        [Theory]
        [MemberData(nameof(Hex_UShort_TestData))]
        public void Hex(ushort value, string expected)
        {
            Assert.Equal(expected, Conversion.Hex(value));
        }

        [Theory]
        [MemberData(nameof(Hex_Short_TestData))]
        public void Hex(short value, string expected)
        {
            Assert.Equal(expected, Conversion.Hex(value));
        }

        [Theory]
        [MemberData(nameof(Hex_UInteger_TestData))]
        public void Hex(uint value, string expected)
        {
            Assert.Equal(expected, Conversion.Hex(value));
        }

        [Theory]
        [MemberData(nameof(Hex_Integer_TestData))]
        public void Hex(int value, string expected)
        {
            Assert.Equal(expected, Conversion.Hex(value));
        }

        [Theory]
        [MemberData(nameof(Hex_ULong_TestData))]
        public void Hex(ulong value, string expected)
        {
            Assert.Equal(expected, Conversion.Hex(value));
        }

        [Theory]
        [MemberData(nameof(Hex_Long_TestData))]
        public void Hex(long value, string expected)
        {
            Assert.Equal(expected, Conversion.Hex(value));
        }

        [Theory]
        [MemberData(nameof(Hex_Single_TestData))]
        public void Hex(float value, string expected)
        {
            Assert.Equal(expected, Conversion.Hex(value));
        }

        [Theory]
        [MemberData(nameof(Hex_Double_TestData))]
        public void Hex(double value, string expected)
        {
            Assert.Equal(expected, Conversion.Hex(value));
        }

        [Theory]
        [MemberData(nameof(Hex_Decimal_TestData))]
        public void Hex(decimal value, string expected)
        {
            Assert.Equal(expected, Conversion.Hex(value));
        }

        private static IEnumerable<object[]> Hex_Byte_TestData()
        {
            yield break; // Add more...
        }

        private static IEnumerable<object[]> Hex_SByte_TestData()
        {
            yield break; // Add more...
        }

        private static IEnumerable<object[]> Hex_UShort_TestData()
        {
            yield break; // Add more...
        }

        private static IEnumerable<object[]> Hex_Short_TestData()
        {
            yield return new object[] { short.MinValue, "8000" };
            yield return new object[] { (short)-1, "FFFF" };
            yield return new object[] { (short)0, "0" };
            yield return new object[] { (short)1, "1" };
            yield return new object[] { short.MaxValue, "7FFF" };
        }

        private static IEnumerable<object[]> Hex_UInteger_TestData()
        {
            yield break; // Add more...
        }

        private static IEnumerable<object[]> Hex_Integer_TestData()
        {
            yield break; // Add more...
        }

        private static IEnumerable<object[]> Hex_ULong_TestData()
        {
            yield break; // Add more...
        }

        private static IEnumerable<object[]> Hex_Long_TestData()
        {
            yield break; // Add more...
        }

        private static IEnumerable<object[]> Hex_Single_TestData()
        {
            yield break; // Add more...
        }

        private static IEnumerable<object[]> Hex_Double_TestData()
        {
            yield break; // Add more...
        }

        private static IEnumerable<object[]> Hex_Decimal_TestData()
        {
            yield break; // Add more...
        }

        [Theory]
        [MemberData(nameof(Int_Short_TestData))]
        [MemberData(nameof(Int_Integer_TestData))]
        [MemberData(nameof(Int_Long_TestData))]
        [MemberData(nameof(Int_Single_TestData))]
        [MemberData(nameof(Int_Double_TestData))]
        [MemberData(nameof(Int_Decimal_TestData))]
        public void Int(object value, object expected)
        {
            Assert.Equal(expected, Conversion.Int(value));
        }

        [Theory]
        [MemberData(nameof(Int_Short_TestData))]
        public void Int(short value, short expected)
        {
            Assert.Equal(expected, Conversion.Int(value));
        }

        [Theory]
        [MemberData(nameof(Int_Integer_TestData))]
        public void Int(int value, int expected)
        {
            Assert.Equal(expected, Conversion.Int(value));
        }

        [Theory]
        [MemberData(nameof(Int_Long_TestData))]
        public void Int(long value, long expected)
        {
            Assert.Equal(expected, Conversion.Int(value));
        }

        [Theory]
        [MemberData(nameof(Int_Single_TestData))]
        public void Int(float value, float expected)
        {
            Assert.Equal(expected, Conversion.Int(value));
        }

        [Theory]
        [MemberData(nameof(Int_Double_TestData))]
        public void Int(double value, double expected)
        {
            Assert.Equal(expected, Conversion.Int(value));
        }

        [Theory]
        [MemberData(nameof(Int_Decimal_TestData))]
        public void Int(decimal value, decimal expected)
        {
            Assert.Equal(expected, Conversion.Int(value));
        }

        private static IEnumerable<object[]> Int_Short_TestData()
        {
            yield return new object[] { (short)0, (short)0 };
            // Add more...
        }

        private static IEnumerable<object[]> Int_Integer_TestData()
        {
            yield return new object[] { 0, 0 };
            // Add more...
        }

        private static IEnumerable<object[]> Int_Long_TestData()
        {
            yield return new object[] { 0L, 0L };
            // Add more...
        }

        private static IEnumerable<object[]> Int_Single_TestData()
        {
            yield return new object[] { 0.0f, 0.0f };
            // Add more...
        }

        private static IEnumerable<object[]> Int_Double_TestData()
        {
            yield return new object[] { 0.0, 0.0 };
            // Add more...
        }

        private static IEnumerable<object[]> Int_Decimal_TestData()
        {
            yield return new object[] { (decimal)0, (decimal)0 };
            // Add more...
        }

        [Theory]
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
        public void Oct(object value, string expected)
        {
            Assert.Equal(expected, Conversion.Oct(value));
        }

        [Theory]
        [MemberData(nameof(Oct_Byte_TestData))]
        public void Oct(byte value, string expected)
        {
            Assert.Equal(expected, Conversion.Oct(value));
        }

        [Theory]
        [MemberData(nameof(Oct_SByte_TestData))]
        public void Oct(sbyte value, string expected)
        {
            Assert.Equal(expected, Conversion.Oct(value));
        }

        [Theory]
        [MemberData(nameof(Oct_UShort_TestData))]
        public void Oct(ushort value, string expected)
        {
            Assert.Equal(expected, Conversion.Oct(value));
        }

        [Theory]
        [MemberData(nameof(Oct_Short_TestData))]
        public void Oct(short value, string expected)
        {
            Assert.Equal(expected, Conversion.Oct(value));
        }

        [Theory]
        [MemberData(nameof(Oct_UInteger_TestData))]
        public void Oct(uint value, string expected)
        {
            Assert.Equal(expected, Conversion.Oct(value));
        }

        [Theory]
        [MemberData(nameof(Oct_Integer_TestData))]
        public void Oct(int value, string expected)
        {
            Assert.Equal(expected, Conversion.Oct(value));
        }

        [Theory]
        [MemberData(nameof(Oct_ULong_TestData))]
        public void Oct(ulong value, string expected)
        {
            Assert.Equal(expected, Conversion.Oct(value));
        }

        [Theory]
        [MemberData(nameof(Oct_Long_TestData))]
        public void Oct(long value, string expected)
        {
            Assert.Equal(expected, Conversion.Oct(value));
        }

        [Theory]
        [MemberData(nameof(Oct_Single_TestData))]
        public void Oct(float value, string expected)
        {
            Assert.Equal(expected, Conversion.Oct(value));
        }

        [Theory]
        [MemberData(nameof(Oct_Double_TestData))]
        public void Oct(double value, string expected)
        {
            Assert.Equal(expected, Conversion.Oct(value));
        }

        [Theory]
        [MemberData(nameof(Oct_Decimal_TestData))]
        public void Oct(decimal value, string expected)
        {
            Assert.Equal(expected, Conversion.Oct(value));
        }

        private static IEnumerable<object[]> Oct_Byte_TestData()
        {
            yield break; // Add more...
        }

        private static IEnumerable<object[]> Oct_SByte_TestData()
        {
            yield break; // Add more...
        }

        private static IEnumerable<object[]> Oct_UShort_TestData()
        {
            yield break; // Add more...
        }

        private static IEnumerable<object[]> Oct_Short_TestData()
        {
            yield return new object[] { short.MinValue, "100000" };
            yield return new object[] { (short)-1, "177777" };
            yield return new object[] { (short)0, "0" };
            yield return new object[] { (short)1, "1" };
            yield return new object[] { short.MaxValue, "77777" };
        }

        private static IEnumerable<object[]> Oct_UInteger_TestData()
        {
            yield break; // Add more...
        }

        private static IEnumerable<object[]> Oct_Integer_TestData()
        {
            yield break; // Add more...
        }

        private static IEnumerable<object[]> Oct_ULong_TestData()
        {
            yield break; // Add more...
        }

        private static IEnumerable<object[]> Oct_Long_TestData()
        {
            yield break; // Add more...
        }

        private static IEnumerable<object[]> Oct_Single_TestData()
        {
            yield break; // Add more...
        }

        private static IEnumerable<object[]> Oct_Double_TestData()
        {
            yield break; // Add more...
        }

        private static IEnumerable<object[]> Oct_Decimal_TestData()
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

        private static IEnumerable<object[]> Str_TestData()
        {
            yield return new object[] { 0, " 0" };
            yield return new object[] { 1, " 1" };
            yield return new object[] { -1, "-1" };
            // Add more...
        }

        [Theory]
        [MemberData(nameof(Val_Object_TestData))]
        [MemberData(nameof(Val_Char_TestData))]
        [MemberData(nameof(Val_String_TestData))]
        public void Val(object value, double expected)
        {
            Assert.Equal(expected, Conversion.Val(value));
        }

        [Theory]
        [MemberData(nameof(Val_Object_ArgumentException_TestData))]
        public void Val_ArgumentException(object value)
        {
            Assert.Throws<ArgumentException>(() => Conversion.Val(value));
        }

        [Theory]
        [MemberData(nameof(Val_Object_OverflowException_TestData))]
        public void Val_OverflowException(object value)
        {
            Assert.Throws<OverflowException>(() => Conversion.Val(value));
        }

        [Theory]
        [MemberData(nameof(Val_Char_TestData))]
        public void Val(char value, int expected)
        {
            Assert.Equal(expected, Conversion.Val(value));
        }

        [Theory]
        [MemberData(nameof(Val_Char_ArgumentException_TestData))]
        public void Val_ArgumentException(char value)
        {
            Assert.Throws<ArgumentException>(() => Conversion.Val(value));
        }

        [Theory]
        [MemberData(nameof(Val_Char_OverflowException_TestData))]
        public void Val_OverflowException(char value)
        {
            Assert.Throws<OverflowException>(() => Conversion.Val(value));
        }

        [Theory]
        [MemberData(nameof(Val_String_TestData))]
        public void Val(string value, double expected)
        {
            Assert.Equal(expected, Conversion.Val(value));
        }

        [Theory]
        [MemberData(nameof(Val_String_ArgumentException_TestData))]
        public void Val_ArgumentException(string value)
        {
            Assert.Throws<ArgumentException>(() => Conversion.Val(value));
        }

        [Theory]
        [MemberData(nameof(Val_String_OverflowException_TestData))]
        public void Val_OverflowException(string value)
        {
            Assert.Throws<OverflowException>(() => Conversion.Val(value));
        }

        private static IEnumerable<object[]> Val_Object_TestData()
        {
            yield return new object[] { 0, "0" };
            yield return new object[] { 1, " 1" };
            // Add more...
        }

        private static IEnumerable<object[]> Val_Object_ArgumentException_TestData()
        {
            yield break; // Add more...
        }

        private static IEnumerable<object[]> Val_Object_OverflowException_TestData()
        {
            yield break; // Add more...
        }

        private static IEnumerable<object[]> Val_Char_TestData()
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

        private static IEnumerable<object[]> Val_Char_ArgumentException_TestData()
        {
            yield break; // Add more...
        }

        private static IEnumerable<object[]> Val_Char_OverflowException_TestData()
        {
            yield break; // Add more...
        }

        private static IEnumerable<object[]> Val_String_TestData()
        {
            yield return new object[] { null, 0.0 };
            yield return new object[] { "", 0.0 };
            yield return new object[] { "0", 0.0 };
            yield return new object[] { "&H7F", 127.0 };
            yield return new object[] { "-1e20", -1.0e20 };
            yield return new object[] { "1e-3", 1.0e-3 };
        }

        private static IEnumerable<object[]> Val_String_ArgumentException_TestData()
        {
            yield break; // Add more...
        }

        private static IEnumerable<object[]> Val_String_OverflowException_TestData()
        {
            yield break; // Add more...
        }
    }
}
