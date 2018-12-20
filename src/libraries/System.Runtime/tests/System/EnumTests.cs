// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Tests
{
    public partial class EnumTests
    {
        public static IEnumerable<object[]> Parse_TestData()
        {
            // SByte
            yield return new object[] { "Min", false, SByteEnum.Min };
            yield return new object[] { "mAx", true, SByteEnum.Max };
            yield return new object[] { "1", false, SByteEnum.One };
            yield return new object[] { "5", false, (SByteEnum)5 };
            yield return new object[] { sbyte.MinValue.ToString(), false, (SByteEnum)sbyte.MinValue };
            yield return new object[] { sbyte.MaxValue.ToString(), false, (SByteEnum)sbyte.MaxValue };

            // Byte
            yield return new object[] { "Min", false, ByteEnum.Min };
            yield return new object[] { "mAx", true, ByteEnum.Max };
            yield return new object[] { "1", false, ByteEnum.One };
            yield return new object[] { "5", false, (ByteEnum)5 };
            yield return new object[] { byte.MinValue.ToString(), false, (ByteEnum)byte.MinValue };
            yield return new object[] { byte.MaxValue.ToString(), false, (ByteEnum)byte.MaxValue };

            // Int16
            yield return new object[] { "Min", false, Int16Enum.Min };
            yield return new object[] { "mAx", true, Int16Enum.Max };
            yield return new object[] { "1", false, Int16Enum.One };
            yield return new object[] { "5", false, (Int16Enum)5 };
            yield return new object[] { short.MinValue.ToString(), false, (Int16Enum)short.MinValue };
            yield return new object[] { short.MaxValue.ToString(), false, (Int16Enum)short.MaxValue };

            // UInt16
            yield return new object[] { "Min", false, UInt16Enum.Min };
            yield return new object[] { "mAx", true, UInt16Enum.Max };
            yield return new object[] { "1", false, UInt16Enum.One };
            yield return new object[] { "5", false, (UInt16Enum)5 };
            yield return new object[] { ushort.MinValue.ToString(), false, (UInt16Enum)ushort.MinValue };
            yield return new object[] { ushort.MaxValue.ToString(), false, (UInt16Enum)ushort.MaxValue };

            // Int32
            yield return new object[] { "Min", false, Int32Enum.Min };
            yield return new object[] { "mAx", true, Int32Enum.Max };
            yield return new object[] { "1", false, Int32Enum.One };
            yield return new object[] { "5", false, (Int32Enum)5 };
            yield return new object[] { int.MinValue.ToString(), false, (Int32Enum)int.MinValue };
            yield return new object[] { int.MaxValue.ToString(), false, (Int32Enum)int.MaxValue };

            // UInt32
            yield return new object[] { "Min", false, UInt32Enum.Min };
            yield return new object[] { "mAx", true, UInt32Enum.Max };
            yield return new object[] { "1", false, UInt32Enum.One };
            yield return new object[] { "5", false, (UInt32Enum)5 };
            yield return new object[] { uint.MinValue.ToString(), false, (UInt32Enum)uint.MinValue };
            yield return new object[] { uint.MaxValue.ToString(), false, (UInt32Enum)uint.MaxValue };

            // Int64
            yield return new object[] { "Min", false, Int64Enum.Min };
            yield return new object[] { "mAx", true, Int64Enum.Max };
            yield return new object[] { "1", false, Int64Enum.One };
            yield return new object[] { "5", false, (Int64Enum)5 };
            yield return new object[] { long.MinValue.ToString(), false, (Int64Enum)long.MinValue };
            yield return new object[] { long.MaxValue.ToString(), false, (Int64Enum)long.MaxValue };

            // UInt64
            yield return new object[] { "Min", false, UInt64Enum.Min };
            yield return new object[] { "mAx", true, UInt64Enum.Max };
            yield return new object[] { "1", false, UInt64Enum.One };
            yield return new object[] { "5", false, (UInt64Enum)5 };
            yield return new object[] { ulong.MinValue.ToString(), false, (UInt64Enum)ulong.MinValue };
            yield return new object[] { ulong.MaxValue.ToString(), false, (UInt64Enum)ulong.MaxValue };

#if netcoreapp
            // Char
            yield return new object[] { "Value1", false, Enum.ToObject(s_charEnumType, (char)1) };
            yield return new object[] { "vaLue2", true, Enum.ToObject(s_charEnumType, (char)2) };
            yield return new object[] { "1", false, Enum.ToObject(s_charEnumType, '1') };

            // Bool
            yield return new object[] { "Value1", false, Enum.ToObject(s_boolEnumType, true) };
            yield return new object[] { "vaLue2", true, Enum.ToObject(s_boolEnumType, false) };

            // Single - parses successfully, but doesn't properly represent the underlying value
            yield return new object[] { "Value1", false, Enum.GetValues(s_floatEnumType).GetValue(0) };
            yield return new object[] { "vaLue2", true, Enum.GetValues(s_floatEnumType).GetValue(0) };

            // Double - parses successfully, but doesn't properly represent the underlying value
            yield return new object[] { "Value1", false, Enum.GetValues(s_doubleEnumType).GetValue(0) };
            yield return new object[] { "vaLue2", true, Enum.GetValues(s_doubleEnumType).GetValue(0) };
#endif // netcoreapp

            // SimpleEnum
            yield return new object[] { "Red", false, SimpleEnum.Red };
            yield return new object[] { " Red", false, SimpleEnum.Red };
            yield return new object[] { "Red ", false, SimpleEnum.Red };
            yield return new object[] { " red ", true, SimpleEnum.Red };
            yield return new object[] { "B", false, SimpleEnum.B };
            yield return new object[] { "B,B", false, SimpleEnum.B };
            yield return new object[] { " Red , Blue ", false, SimpleEnum.Red | SimpleEnum.Blue };
            yield return new object[] { "Blue,Red,Green", false, SimpleEnum.Red | SimpleEnum.Blue | SimpleEnum.Green };
            yield return new object[] { "Blue,Red,Red,Red,Green", false, SimpleEnum.Red | SimpleEnum.Blue | SimpleEnum.Green };
            yield return new object[] { "Red,Blue,   Green", false, SimpleEnum.Red | SimpleEnum.Blue | SimpleEnum.Green };
            yield return new object[] { "1", false, SimpleEnum.Red };
            yield return new object[] { " 1 ", false, SimpleEnum.Red };
            yield return new object[] { "2", false, SimpleEnum.Blue };
            yield return new object[] { "99", false, (SimpleEnum)99 };
            yield return new object[] { "-42", false, (SimpleEnum)(-42) };
            yield return new object[] { "   -42", false, (SimpleEnum)(-42) };
            yield return new object[] { "   -42 ", false, (SimpleEnum)(-42) };
        }

        [Theory]
        [MemberData(nameof(Parse_TestData))]
        public static void Parse<T>(string value, bool ignoreCase, T expected) where T : struct
        {
            T result;
            if (!ignoreCase)
            {
                Assert.True(Enum.TryParse(value, out result));
                Assert.Equal(expected, result);

                Assert.Equal(expected, Enum.Parse(expected.GetType(), value));
            }

            Assert.True(Enum.TryParse(value, ignoreCase, out result));
            Assert.Equal(expected, result);

            Assert.Equal(expected, Enum.Parse(expected.GetType(), value, ignoreCase));
        }

        public static IEnumerable<object[]> Parse_Invalid_TestData()
        {
            yield return new object[] { null, "", false, typeof(ArgumentNullException) };
            yield return new object[] { typeof(object), "", false, typeof(ArgumentException) };
            yield return new object[] { typeof(int), "", false, typeof(ArgumentException) };

            yield return new object[] { typeof(SimpleEnum), null, false, typeof(ArgumentNullException) };
            yield return new object[] { typeof(SimpleEnum), "", false, typeof(ArgumentException) };
            yield return new object[] { typeof(SimpleEnum), "    \t", false, typeof(ArgumentException) };
            yield return new object[] { typeof(SimpleEnum), " red ", false, typeof(ArgumentException) };
            yield return new object[] { typeof(SimpleEnum), "Purple", false, typeof(ArgumentException) };
            yield return new object[] { typeof(SimpleEnum), ",Red", false, typeof(ArgumentException) };
            yield return new object[] { typeof(SimpleEnum), "Red,", false, typeof(ArgumentException) };
            yield return new object[] { typeof(SimpleEnum), "B,", false, typeof(ArgumentException) };
            yield return new object[] { typeof(SimpleEnum), " , , ,", false, typeof(ArgumentException) };
            yield return new object[] { typeof(SimpleEnum), "Red,Blue,", false, typeof(ArgumentException) };
            yield return new object[] { typeof(SimpleEnum), "Red,,Blue", false, typeof(ArgumentException) };
            yield return new object[] { typeof(SimpleEnum), "Red,Blue, ", false, typeof(ArgumentException) };
            yield return new object[] { typeof(SimpleEnum), "Red Blue", false, typeof(ArgumentException) };
            yield return new object[] { typeof(SimpleEnum), "1,Blue", false, typeof(ArgumentException) };
            yield return new object[] { typeof(SimpleEnum), "1,1", false, typeof(ArgumentException) };
            yield return new object[] { typeof(SimpleEnum), "Blue,1", false, typeof(ArgumentException) };
            yield return new object[] { typeof(SimpleEnum), "Blue, 1", false, typeof(ArgumentException) };

            yield return new object[] { typeof(ByteEnum), "-1", false, typeof(OverflowException) };
            yield return new object[] { typeof(ByteEnum), "256", false, typeof(OverflowException) };

            yield return new object[] { typeof(SByteEnum), "-129", false, typeof(OverflowException) };
            yield return new object[] { typeof(SByteEnum), "128", false, typeof(OverflowException) };

            yield return new object[] { typeof(Int16Enum), "-32769", false, typeof(OverflowException) };
            yield return new object[] { typeof(Int16Enum), "32768", false, typeof(OverflowException) };

            yield return new object[] { typeof(UInt16Enum), "-1", false, typeof(OverflowException) };
            yield return new object[] { typeof(UInt16Enum), "65536", false, typeof(OverflowException) };

            yield return new object[] { typeof(Int32Enum), "-2147483649", false, typeof(OverflowException) };
            yield return new object[] { typeof(Int32Enum), "2147483648", false, typeof(OverflowException) };

            yield return new object[] { typeof(UInt32Enum), "-1", false, typeof(OverflowException) };
            yield return new object[] { typeof(UInt32Enum), "4294967296", false, typeof(OverflowException) };

            yield return new object[] { typeof(Int64Enum), "-9223372036854775809", false, typeof(OverflowException) };
            yield return new object[] { typeof(Int64Enum), "9223372036854775808", false, typeof(OverflowException) };

            yield return new object[] { typeof(UInt64Enum), "-1", false, typeof(OverflowException) };
            yield return new object[] { typeof(UInt64Enum), "18446744073709551616", false, typeof(OverflowException) };

#if netcoreapp
            // Char
            yield return new object[] { s_charEnumType, ((char)1).ToString(), false, typeof(ArgumentException) };
            yield return new object[] { s_charEnumType, ((char)5).ToString(), false, typeof(ArgumentException) };

            // Bool
            yield return new object[] { s_boolEnumType, bool.TrueString, false, typeof(ArgumentException) };
            yield return new object[] { s_boolEnumType, bool.FalseString, false, typeof(ArgumentException) };

            // Single
            yield return new object[] { s_floatEnumType, "1", false, typeof(ArgumentException) };
            yield return new object[] { s_floatEnumType, "5", false, typeof(ArgumentException) };
            yield return new object[] { s_floatEnumType, "1.0", false, typeof(ArgumentException) };

            // Double
            yield return new object[] { s_doubleEnumType, "1", false, typeof(ArgumentException) };
            yield return new object[] { s_doubleEnumType, "5", false, typeof(ArgumentException) };
            yield return new object[] { s_doubleEnumType, "1.0", false, typeof(ArgumentException) };

            // IntPtr
            yield return new object[] { s_intPtrEnumType, "1", false, typeof(InvalidCastException) };
            yield return new object[] { s_intPtrEnumType, "5", false, typeof(InvalidCastException) };

            // UIntPtr
            yield return new object[] { s_uintPtrEnumType, "1", false, typeof(InvalidCastException) };
            yield return new object[] { s_uintPtrEnumType, "5", false, typeof(InvalidCastException) };
#endif // netcoreapp
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(Type enumType, string value, bool ignoreCase, Type exceptionType)
        {
            Type typeArgument = enumType == null || !enumType.GetTypeInfo().IsEnum ? typeof(SimpleEnum) : enumType;
            MethodInfo parseMethod = typeof(EnumTests).GetTypeInfo().GetMethod(nameof(Parse_Generic_Invalid)).MakeGenericMethod(typeArgument);
            parseMethod.Invoke(null, new object[] { enumType, value, ignoreCase, exceptionType });
        }

        public static void Parse_Generic_Invalid<T>(Type enumType, string value, bool ignoreCase, Type exceptionType) where T : struct
        {
            T result;
            if (!ignoreCase)
            {
                Assert.False(Enum.TryParse(value, out result));
                Assert.Equal(default(T), result);

                Assert.Throws(exceptionType, () => Enum.Parse(enumType, value));
            }

            Assert.False(Enum.TryParse(value, ignoreCase, out result));
            Assert.Equal(default(T), result);

            Assert.Throws(exceptionType, () => Enum.Parse(enumType, value, ignoreCase));
        }

        public static IEnumerable<object[]> GetName_TestData()
        {
            // SByte
            yield return new object[] { typeof(SByteEnum), SByteEnum.Min, "Min" };
            yield return new object[] { typeof(SByteEnum), SByteEnum.One, "One" };
            yield return new object[] { typeof(SByteEnum), SByteEnum.Two, "Two" };
            yield return new object[] { typeof(SByteEnum), SByteEnum.Max, "Max" };
            yield return new object[] { typeof(SByteEnum), sbyte.MinValue, "Min" };
            yield return new object[] { typeof(SByteEnum), (sbyte)1, "One" };
            yield return new object[] { typeof(SByteEnum), (sbyte)2, "Two" };
            yield return new object[] { typeof(SByteEnum), sbyte.MaxValue, "Max" };
            yield return new object[] { typeof(SByteEnum), (sbyte)3, null };

            // Byte
            yield return new object[] { typeof(ByteEnum), ByteEnum.Min, "Min" };
            yield return new object[] { typeof(ByteEnum), ByteEnum.One, "One" };
            yield return new object[] { typeof(ByteEnum), ByteEnum.Two, "Two" };
            yield return new object[] { typeof(ByteEnum), ByteEnum.Max, "Max" };
            yield return new object[] { typeof(ByteEnum), byte.MinValue, "Min" };
            yield return new object[] { typeof(ByteEnum), (byte)1, "One" };
            yield return new object[] { typeof(ByteEnum), (byte)2, "Two" };
            yield return new object[] { typeof(ByteEnum), byte.MaxValue, "Max" };
            yield return new object[] { typeof(ByteEnum), (byte)3, null };

            // Int16
            yield return new object[] { typeof(Int16Enum), Int16Enum.Min, "Min" };
            yield return new object[] { typeof(Int16Enum), Int16Enum.One, "One" };
            yield return new object[] { typeof(Int16Enum), Int16Enum.Two, "Two" };
            yield return new object[] { typeof(Int16Enum), Int16Enum.Max, "Max" };
            yield return new object[] { typeof(Int16Enum), short.MinValue, "Min" };
            yield return new object[] { typeof(Int16Enum), (short)1, "One" };
            yield return new object[] { typeof(Int16Enum), (short)2, "Two" };
            yield return new object[] { typeof(Int16Enum), short.MaxValue, "Max" };
            yield return new object[] { typeof(Int16Enum), (short)3, null };

            // UInt16
            yield return new object[] { typeof(UInt16Enum), UInt16Enum.Min, "Min" };
            yield return new object[] { typeof(UInt16Enum), UInt16Enum.One, "One" };
            yield return new object[] { typeof(UInt16Enum), UInt16Enum.Two, "Two" };
            yield return new object[] { typeof(UInt16Enum), UInt16Enum.Max, "Max" };
            yield return new object[] { typeof(UInt16Enum), ushort.MinValue, "Min" };
            yield return new object[] { typeof(UInt16Enum), (ushort)1, "One" };
            yield return new object[] { typeof(UInt16Enum), (ushort)2, "Two" };
            yield return new object[] { typeof(UInt16Enum), ushort.MaxValue, "Max" };
            yield return new object[] { typeof(UInt16Enum), (ushort)3, null };

            // Int32
            yield return new object[] { typeof(Int32Enum), Int32Enum.Min, "Min" };
            yield return new object[] { typeof(Int32Enum), Int32Enum.One, "One" };
            yield return new object[] { typeof(Int32Enum), Int32Enum.Two, "Two" };
            yield return new object[] { typeof(Int32Enum), Int32Enum.Max, "Max" };
            yield return new object[] { typeof(Int32Enum), int.MinValue, "Min" };
            yield return new object[] { typeof(Int32Enum), 1, "One" };
            yield return new object[] { typeof(Int32Enum), 2, "Two" };
            yield return new object[] { typeof(Int32Enum), int.MaxValue, "Max" };
            yield return new object[] { typeof(Int32Enum), 3, null };

            yield return new object[] { typeof(SimpleEnum), 99, null };
            yield return new object[] { typeof(SimpleEnum), SimpleEnum.Red, "Red" };
            yield return new object[] { typeof(SimpleEnum), 1, "Red" };

            // UInt32
            yield return new object[] { typeof(UInt32Enum), UInt32Enum.Min, "Min" };
            yield return new object[] { typeof(UInt32Enum), UInt32Enum.One, "One" };
            yield return new object[] { typeof(UInt32Enum), UInt32Enum.Two, "Two" };
            yield return new object[] { typeof(UInt32Enum), UInt32Enum.Max, "Max" };
            yield return new object[] { typeof(UInt32Enum), uint.MinValue, "Min" };
            yield return new object[] { typeof(UInt32Enum), (uint)1, "One" };
            yield return new object[] { typeof(UInt32Enum), (uint)2, "Two" };
            yield return new object[] { typeof(UInt32Enum), uint.MaxValue, "Max" };
            yield return new object[] { typeof(UInt32Enum), (uint)3, null };

            // Int64
            yield return new object[] { typeof(Int64Enum), Int64Enum.Min, "Min" };
            yield return new object[] { typeof(Int64Enum), Int64Enum.One, "One" };
            yield return new object[] { typeof(Int64Enum), Int64Enum.Two, "Two" };
            yield return new object[] { typeof(Int64Enum), Int64Enum.Max, "Max" };
            yield return new object[] { typeof(Int64Enum), long.MinValue, "Min" };
            yield return new object[] { typeof(Int64Enum), (long)1, "One" };
            yield return new object[] { typeof(Int64Enum), (long)2, "Two" };
            yield return new object[] { typeof(Int64Enum), long.MaxValue, "Max" };
            yield return new object[] { typeof(Int64Enum), (long)3, null };

            // UInt64
            yield return new object[] { typeof(UInt64Enum), UInt64Enum.Min, "Min" };
            yield return new object[] { typeof(UInt64Enum), UInt64Enum.One, "One" };
            yield return new object[] { typeof(UInt64Enum), UInt64Enum.Two, "Two" };
            yield return new object[] { typeof(UInt64Enum), UInt64Enum.Max, "Max" };
            yield return new object[] { typeof(UInt64Enum), ulong.MinValue, "Min" };
            yield return new object[] { typeof(UInt64Enum), 1UL, "One" };
            yield return new object[] { typeof(UInt64Enum), 2UL, "Two" };
            yield return new object[] { typeof(UInt64Enum), ulong.MaxValue, "Max" };
            yield return new object[] { typeof(UInt64Enum), 3UL, null };

#if netcoreapp
            // Char
            yield return new object[] { s_charEnumType, Enum.Parse(s_charEnumType, "Value1"), "Value1" };
            yield return new object[] { s_charEnumType, Enum.Parse(s_charEnumType, "Value2"), "Value2" };
            yield return new object[] { s_charEnumType, (char)1, "Value1"  };
            yield return new object[] { s_charEnumType, (char)2, "Value2" };
            yield return new object[] { s_charEnumType, (char)4, null };

            // Bool
            yield return new object[] { s_boolEnumType, Enum.Parse(s_boolEnumType, "Value1"), "Value1" };
            yield return new object[] { s_boolEnumType, Enum.Parse(s_boolEnumType, "Value2"), "Value2" };
            yield return new object[] { s_boolEnumType, true, "Value1" };
            yield return new object[] { s_boolEnumType, false, "Value2" };
#endif // netcoreapp            
        }

        [Theory]
        [MemberData(nameof(GetName_TestData))]
        public static void GetName(Type enumType, object value, string expected)
        {
            Assert.Equal(expected, Enum.GetName(enumType, value));

            // The format "G" should return the name of the enum case
            if (value.GetType() == enumType)
            {
                ToString_Format((Enum)value, "G", expected);
            }
            else
            {
                Format(enumType, value, "G", expected ?? value.ToString());
            }
        }

        [Fact]
        public static void GetName_MultipleMatches()
        {
            // In the case of multiple matches, GetName returns one of them (which one is an implementation detail.)
            string s = Enum.GetName(typeof(SimpleEnum), 3);
            Assert.True(s == "Green" || s == "Green_a" || s == "Green_b");
        }

        [Fact]
        public static void GetName_Invalid()
        {
            Type t = typeof(SimpleEnum);
            AssertExtensions.Throws<ArgumentNullException>("enumType", () => Enum.GetName(null, 1)); // Enum type is null
            AssertExtensions.Throws<ArgumentNullException>("value", () => Enum.GetName(t, null)); // Value is null

            AssertExtensions.Throws<ArgumentException>(null, () => Enum.GetName(typeof(object), 1)); // Enum type is not an enum
            AssertExtensions.Throws<ArgumentException>("value", () => Enum.GetName(t, "Red")); // Value is not the type of the enum's raw data
            AssertExtensions.Throws<ArgumentException>("value", () => Enum.GetName(t, (IntPtr)0)); // Value is out of range
        }

        [Theory]
        [InlineData(typeof(SByteEnum), 0xffffffffffffff80LU, "Min")]
        [InlineData(typeof(SByteEnum), 0xffffff80u, null)]
        [InlineData(typeof(SByteEnum), unchecked((int)(0xffffff80u)), "Min")]
        [InlineData(typeof(SByteEnum), true, "One")]
        [InlineData(typeof(SByteEnum), (char)1, "One")]
        [InlineData(typeof(SByteEnum), SimpleEnum.Red, "One")] // API doesn't care if you pass in a completely different enum
        public static void GetName_NonIntegralTypes(Type enumType, object value, string expected)
        {
            // Despite what MSDN says, GetName() does not require passing in the exact integral type. 
            // For the purposes of comparison: 
            //  - The enum member value are normalized as follows:
            //      - unsigned ints zero-extended to 64-bits
            //      - signed ints sign-extended to 64-bits
            //  - The value passed in as an argument to GetNames() is normalized as follows:
            //      - unsigned ints zero-extended to 64-bits
            //      - signed ints sign-extended to 64-bits
            // Then comparison is done on all 64 bits.
            Assert.Equal(expected, Enum.GetName(enumType, value));
        }

        [Theory]
        [InlineData(SimpleEnum.Blue, TypeCode.Int32)]
        [InlineData(ByteEnum.Max, TypeCode.Byte)]
        [InlineData(SByteEnum.Min, TypeCode.SByte)]
        [InlineData(UInt16Enum.Max, TypeCode.UInt16)]
        [InlineData(Int16Enum.Min, TypeCode.Int16)]
        [InlineData(UInt32Enum.Max, TypeCode.UInt32)]
        [InlineData(Int32Enum.Min, TypeCode.Int32)]
        [InlineData(UInt64Enum.Max, TypeCode.UInt64)]
        [InlineData(Int64Enum.Min, TypeCode.Int64)]
        public static void GetTypeCode_Enum_ReturnsExpected(Enum e, TypeCode expected)
        {
            Assert.Equal(expected, e.GetTypeCode());
        }

        public static IEnumerable<object[]> IsDefined_TestData()
        {
            // SByte
            yield return new object[] { typeof(SByteEnum), "One", true };
            yield return new object[] { typeof(SByteEnum), "None", false };
            yield return new object[] { typeof(SByteEnum), SByteEnum.One, true };
            yield return new object[] { typeof(SByteEnum), (SByteEnum)99, false };
            yield return new object[] { typeof(SByteEnum), (sbyte)1, true };
            yield return new object[] { typeof(SByteEnum), (sbyte)99, false };

            // Byte
            yield return new object[] { typeof(ByteEnum), "One", true };
            yield return new object[] { typeof(ByteEnum), "None", false };
            yield return new object[] { typeof(ByteEnum), ByteEnum.One, true };
            yield return new object[] { typeof(ByteEnum), (ByteEnum)99, false };
            yield return new object[] { typeof(ByteEnum), (byte)1, true };
            yield return new object[] { typeof(ByteEnum), (byte)99, false };

            // Int16
            yield return new object[] { typeof(Int16Enum), "One", true };
            yield return new object[] { typeof(Int16Enum), "None", false };
            yield return new object[] { typeof(Int16Enum), Int16Enum.One, true };
            yield return new object[] { typeof(Int16Enum), (Int16Enum)99, false };
            yield return new object[] { typeof(Int16Enum), (short)1, true };
            yield return new object[] { typeof(Int16Enum), (short)99, false };

            // UInt16
            yield return new object[] { typeof(UInt16Enum), "One", true };
            yield return new object[] { typeof(UInt16Enum), "None", false };
            yield return new object[] { typeof(UInt16Enum), UInt16Enum.One, true };
            yield return new object[] { typeof(UInt16Enum), (UInt16Enum)99, false };
            yield return new object[] { typeof(UInt16Enum), (ushort)1, true };
            yield return new object[] { typeof(UInt16Enum), (ushort)99, false };

            // Int32
            yield return new object[] { typeof(SimpleEnum), "Red", true };
            yield return new object[] { typeof(SimpleEnum), "Green", true };
            yield return new object[] { typeof(SimpleEnum), "Blue", true };
            yield return new object[] { typeof(SimpleEnum), " Blue ", false };
            yield return new object[] { typeof(SimpleEnum), " blue ", false };
            yield return new object[] { typeof(SimpleEnum), SimpleEnum.Red, true };
            yield return new object[] { typeof(SimpleEnum), (SimpleEnum)99, false };
            yield return new object[] { typeof(SimpleEnum), 1, true };
            yield return new object[] { typeof(SimpleEnum), 99, false };
            yield return new object[] { typeof(Int32Enum), 0x1 | 0x02, false };

            // UInt32
            yield return new object[] { typeof(UInt32Enum), "One", true };
            yield return new object[] { typeof(UInt32Enum), "None", false };
            yield return new object[] { typeof(UInt32Enum), UInt32Enum.One, true };
            yield return new object[] { typeof(UInt32Enum), (UInt32Enum)99, false };
            yield return new object[] { typeof(UInt32Enum), (uint)1, true };
            yield return new object[] { typeof(UInt32Enum), (uint)99, false };

            // Int64
            yield return new object[] { typeof(Int64Enum), "One", true };
            yield return new object[] { typeof(Int64Enum), "None", false };
            yield return new object[] { typeof(Int64Enum), Int64Enum.One, true };
            yield return new object[] { typeof(Int64Enum), (Int64Enum)99, false };
            yield return new object[] { typeof(Int64Enum), (long)1, true };
            yield return new object[] { typeof(Int64Enum), (long)99, false };

            // UInt64
            yield return new object[] { typeof(UInt64Enum), "One", true };
            yield return new object[] { typeof(UInt64Enum), "None", false };
            yield return new object[] { typeof(UInt64Enum), UInt64Enum.One, true };
            yield return new object[] { typeof(UInt64Enum), (UInt64Enum)99, false };
            yield return new object[] { typeof(UInt64Enum), (ulong)1, true };
            yield return new object[] { typeof(UInt64Enum), (ulong)99, false };

#if netcoreapp
            // Char
            yield return new object[] { s_charEnumType, "Value1", true };
            yield return new object[] { s_charEnumType, "None", false };
            yield return new object[] { s_charEnumType, Enum.Parse(s_charEnumType, "Value1"), true };
            yield return new object[] { s_charEnumType, (char)1, true };
            yield return new object[] { s_charEnumType, (char)99, false };

            // Boolean
            yield return new object[] { s_boolEnumType, "Value1", true };
            yield return new object[] { s_boolEnumType, "None", false };
            yield return new object[] { s_boolEnumType, Enum.Parse(s_boolEnumType, "Value1"), true };
            yield return new object[] { s_boolEnumType, "Value1", true };
            yield return new object[] { s_boolEnumType, true, true };
            yield return new object[] { s_boolEnumType, false, true };
#endif // netcoreapp            
        }

        [Theory]
        [MemberData(nameof(IsDefined_TestData))]
        public static void IsDefined(Type enumType, object value, bool expected)
        {
            Assert.Equal(expected, Enum.IsDefined(enumType, value));
        }

        [Fact]
        public static void IsDefined_Invalid()
        {
            Type t = typeof(SimpleEnum);

            AssertExtensions.Throws<ArgumentNullException>("enumType", () => Enum.IsDefined(null, 1)); // Enum type is null
            AssertExtensions.Throws<ArgumentNullException>("value", () => Enum.IsDefined(t, null)); // Value is null

            AssertExtensions.Throws<ArgumentException>(null, () => Enum.IsDefined(t, Int32Enum.One)); // Value is different enum type

            // Value is not a valid type (MSDN claims this should throw InvalidOperationException)
            AssertExtensions.Throws<ArgumentException>(null, () => Enum.IsDefined(t, true));
            AssertExtensions.Throws<ArgumentException>(null, () => Enum.IsDefined(t, 'a'));

            // Non-integers throw InvalidOperationException prior to Win8P.
            Assert.Throws<InvalidOperationException>(() => Enum.IsDefined(t, (IntPtr)0));
            Assert.Throws<InvalidOperationException>(() => Enum.IsDefined(t, 5.5));
            Assert.Throws<InvalidOperationException>(() => Enum.IsDefined(t, 5.5f));
        }

        public static IEnumerable<object[]> HasFlag_TestData()
        {
            // SByte
            yield return new object[] { (SByteEnum)0x36, (SByteEnum)0x30, true };
            yield return new object[] { (SByteEnum)0x36, (SByteEnum)0x06, true };
            yield return new object[] { (SByteEnum)0x36, (SByteEnum)0x10, true };
            yield return new object[] { (SByteEnum)0x36, (SByteEnum)0x00, true };
            yield return new object[] { (SByteEnum)0x36, (SByteEnum)0x36, true };
            yield return new object[] { (SByteEnum)0x36, (SByteEnum)0x05, false };
            yield return new object[] { (SByteEnum)0x36, (SByteEnum)0x46, false };

            // Byte
            yield return new object[] { (ByteEnum)0x36, (ByteEnum)0x30, true };
            yield return new object[] { (ByteEnum)0x36, (ByteEnum)0x06, true };
            yield return new object[] { (ByteEnum)0x36, (ByteEnum)0x10, true };
            yield return new object[] { (ByteEnum)0x36, (ByteEnum)0x00, true };
            yield return new object[] { (ByteEnum)0x36, (ByteEnum)0x36, true };
            yield return new object[] { (ByteEnum)0x36, (ByteEnum)0x05, false };
            yield return new object[] { (ByteEnum)0x36, (ByteEnum)0x46, false };

            // Int16
            yield return new object[] { (Int16Enum)0x3f06, (Int16Enum)0x3000, true };
            yield return new object[] { (Int16Enum)0x3f06, (Int16Enum)0x0f06, true };
            yield return new object[] { (Int16Enum)0x3f06, (Int16Enum)0x1000, true };
            yield return new object[] { (Int16Enum)0x3f06, (Int16Enum)0x0000, true };
            yield return new object[] { (Int16Enum)0x3f06, (Int16Enum)0x3f06, true };
            yield return new object[] { (Int16Enum)0x3f06, (Int16Enum)0x0010, false };
            yield return new object[] { (Int16Enum)0x3f06, (Int16Enum)0x3f16, false };

            // UInt16
            yield return new object[] { (UInt16Enum)0x3f06, (UInt16Enum)0x3000, true };
            yield return new object[] { (UInt16Enum)0x3f06, (UInt16Enum)0x0f06, true };
            yield return new object[] { (UInt16Enum)0x3f06, (UInt16Enum)0x1000, true };
            yield return new object[] { (UInt16Enum)0x3f06, (UInt16Enum)0x0000, true };
            yield return new object[] { (UInt16Enum)0x3f06, (UInt16Enum)0x3f06, true };
            yield return new object[] { (UInt16Enum)0x3f06, (UInt16Enum)0x0010, false };
            yield return new object[] { (UInt16Enum)0x3f06, (UInt16Enum)0x3f16, false };

            // Int32
            yield return new object[] { (Int32Enum)0x3f06, (Int32Enum)0x3000, true };
            yield return new object[] { (Int32Enum)0x3f06, (Int32Enum)0x0f06, true };
            yield return new object[] { (Int32Enum)0x3f06, (Int32Enum)0x1000, true };
            yield return new object[] { (Int32Enum)0x3f06, (Int32Enum)0x0000, true };
            yield return new object[] { (Int32Enum)0x3f06, (Int32Enum)0x3f06, true };
            yield return new object[] { (Int32Enum)0x3f06, (Int32Enum)0x0010, false };
            yield return new object[] { (Int32Enum)0x3f06, (Int32Enum)0x3f16, false };

            // UInt32
            yield return new object[] { (UInt32Enum)0x3f06, (UInt32Enum)0x3000, true };
            yield return new object[] { (UInt32Enum)0x3f06, (UInt32Enum)0x0f06, true };
            yield return new object[] { (UInt32Enum)0x3f06, (UInt32Enum)0x1000, true };
            yield return new object[] { (UInt32Enum)0x3f06, (UInt32Enum)0x0000, true };
            yield return new object[] { (UInt32Enum)0x3f06, (UInt32Enum)0x3f06, true };
            yield return new object[] { (UInt32Enum)0x3f06, (UInt32Enum)0x0010, false };
            yield return new object[] { (UInt32Enum)0x3f06, (UInt32Enum)0x3f16, false };

            // Int64
            yield return new object[] { (Int64Enum)0x3f06, (Int64Enum)0x3000, true };
            yield return new object[] { (Int64Enum)0x3f06, (Int64Enum)0x0f06, true };
            yield return new object[] { (Int64Enum)0x3f06, (Int64Enum)0x1000, true };
            yield return new object[] { (Int64Enum)0x3f06, (Int64Enum)0x0000, true };
            yield return new object[] { (Int64Enum)0x3f06, (Int64Enum)0x3f06, true };
            yield return new object[] { (Int64Enum)0x3f06, (Int64Enum)0x0010, false };
            yield return new object[] { (Int64Enum)0x3f06, (Int64Enum)0x3f16, false };

            // UInt64
            yield return new object[] { (UInt64Enum)0x3f06, (UInt64Enum)0x3000, true };
            yield return new object[] { (UInt64Enum)0x3f06, (UInt64Enum)0x0f06, true };
            yield return new object[] { (UInt64Enum)0x3f06, (UInt64Enum)0x1000, true };
            yield return new object[] { (UInt64Enum)0x3f06, (UInt64Enum)0x0000, true };
            yield return new object[] { (UInt64Enum)0x3f06, (UInt64Enum)0x3f06, true };
            yield return new object[] { (UInt64Enum)0x3f06, (UInt64Enum)0x0010, false };
            yield return new object[] { (UInt64Enum)0x3f06, (UInt64Enum)0x3f16, false };

#if netcoreapp
            // Char
            yield return new object[] { Enum.Parse(s_charEnumType, "Value0x3f06"), Enum.Parse(s_charEnumType, "Value0x3000"), true };
            yield return new object[] { Enum.Parse(s_charEnumType, "Value0x3f06"), Enum.Parse(s_charEnumType, "Value0x0f06"), true };
            yield return new object[] { Enum.Parse(s_charEnumType, "Value0x3f06"), Enum.Parse(s_charEnumType, "Value0x1000"), true };
            yield return new object[] { Enum.Parse(s_charEnumType, "Value0x3f06"), Enum.Parse(s_charEnumType, "Value0x0000"), true };
            yield return new object[] { Enum.Parse(s_charEnumType, "Value0x3f06"), Enum.Parse(s_charEnumType, "Value0x3f06"), true };
            yield return new object[] { Enum.Parse(s_charEnumType, "Value0x3f06"), Enum.Parse(s_charEnumType, "Value0x0010"), false };
            yield return new object[] { Enum.Parse(s_charEnumType, "Value0x3f06"), Enum.Parse(s_charEnumType, "Value0x3f16"), false };

            // Bool
            yield return new object[] { Enum.Parse(s_boolEnumType, "Value1"), Enum.Parse(s_boolEnumType, "Value1"), true };
            yield return new object[] { Enum.Parse(s_boolEnumType, "Value1"), Enum.Parse(s_boolEnumType, "Value2"), true };
            yield return new object[] { Enum.Parse(s_boolEnumType, "Value2"), Enum.Parse(s_boolEnumType, "Value2"), true };
            yield return new object[] { Enum.Parse(s_boolEnumType, "Value2"), Enum.Parse(s_boolEnumType, "Value1"), false };

            // Single
            yield return new object[] { Enum.ToObject(s_floatEnumType, 0x3f06), Enum.ToObject(s_floatEnumType, 0x0000), true };
            yield return new object[] { Enum.ToObject(s_floatEnumType, 0x3f06), Enum.ToObject(s_floatEnumType, 0x0f06), true };
            yield return new object[] { Enum.ToObject(s_floatEnumType, 0x3f06), Enum.ToObject(s_floatEnumType, 0x1000), true };
            yield return new object[] { Enum.ToObject(s_floatEnumType, 0x3f06), Enum.ToObject(s_floatEnumType, 0x0000), true };
            yield return new object[] { Enum.ToObject(s_floatEnumType, 0x3f06), Enum.ToObject(s_floatEnumType, 0x3f06), true };
            yield return new object[] { Enum.ToObject(s_floatEnumType, 0x3f06), Enum.ToObject(s_floatEnumType, 0x0010), false };
            yield return new object[] { Enum.ToObject(s_floatEnumType, 0x3f06), Enum.ToObject(s_floatEnumType, 0x3f16), false };

            // Double
            yield return new object[] { Enum.ToObject(s_doubleEnumType, 0x3f06), Enum.ToObject(s_doubleEnumType, 0x0000), true };
            yield return new object[] { Enum.ToObject(s_doubleEnumType, 0x3f06), Enum.ToObject(s_doubleEnumType, 0x0f06), true };
            yield return new object[] { Enum.ToObject(s_doubleEnumType, 0x3f06), Enum.ToObject(s_doubleEnumType, 0x1000), true };
            yield return new object[] { Enum.ToObject(s_doubleEnumType, 0x3f06), Enum.ToObject(s_doubleEnumType, 0x0000), true };
            yield return new object[] { Enum.ToObject(s_doubleEnumType, 0x3f06), Enum.ToObject(s_doubleEnumType, 0x3f06), true };
            yield return new object[] { Enum.ToObject(s_doubleEnumType, 0x3f06), Enum.ToObject(s_doubleEnumType, 0x0010), false };
            yield return new object[] { Enum.ToObject(s_doubleEnumType, 0x3f06), Enum.ToObject(s_doubleEnumType, 0x3f16), false };

            // IntPtr
            yield return new object[] { Enum.ToObject(s_intPtrEnumType, 0x3f06), Enum.ToObject(s_intPtrEnumType, 0x0000), true };
            yield return new object[] { Enum.ToObject(s_intPtrEnumType, 0x3f06), Enum.ToObject(s_intPtrEnumType, 0x0f06), true };
            yield return new object[] { Enum.ToObject(s_intPtrEnumType, 0x3f06), Enum.ToObject(s_intPtrEnumType, 0x1000), true };
            yield return new object[] { Enum.ToObject(s_intPtrEnumType, 0x3f06), Enum.ToObject(s_intPtrEnumType, 0x0000), true };
            yield return new object[] { Enum.ToObject(s_intPtrEnumType, 0x3f06), Enum.ToObject(s_intPtrEnumType, 0x3f06), true };
            yield return new object[] { Enum.ToObject(s_intPtrEnumType, 0x3f06), Enum.ToObject(s_intPtrEnumType, 0x0010), false };
            yield return new object[] { Enum.ToObject(s_intPtrEnumType, 0x3f06), Enum.ToObject(s_intPtrEnumType, 0x3f16), false };

            // UIntPtr
            yield return new object[] { Enum.ToObject(s_uintPtrEnumType, 0x3f06), Enum.ToObject(s_uintPtrEnumType, 0x0000), true };
            yield return new object[] { Enum.ToObject(s_uintPtrEnumType, 0x3f06), Enum.ToObject(s_uintPtrEnumType, 0x0f06), true };
            yield return new object[] { Enum.ToObject(s_uintPtrEnumType, 0x3f06), Enum.ToObject(s_uintPtrEnumType, 0x1000), true };
            yield return new object[] { Enum.ToObject(s_uintPtrEnumType, 0x3f06), Enum.ToObject(s_uintPtrEnumType, 0x0000), true };
            yield return new object[] { Enum.ToObject(s_uintPtrEnumType, 0x3f06), Enum.ToObject(s_uintPtrEnumType, 0x3f06), true };
            yield return new object[] { Enum.ToObject(s_uintPtrEnumType, 0x3f06), Enum.ToObject(s_uintPtrEnumType, 0x0010), false };
            yield return new object[] { Enum.ToObject(s_uintPtrEnumType, 0x3f06), Enum.ToObject(s_uintPtrEnumType, 0x3f16), false };
#endif // netcoreapp
        }

        [Theory]
        [MemberData(nameof(HasFlag_TestData))]
        public static void HasFlag(Enum e, Enum flag, bool expected)
        {
            Assert.Equal(expected, e.HasFlag(flag));
        }

        [Fact]
        public static void HasFlag_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("flag", () => Int32Enum.One.HasFlag(null)); // Flag is null
            AssertExtensions.Throws<ArgumentException>(null, () => Int32Enum.One.HasFlag((SimpleEnum)0x3000)); // Enum is not the same type as the instance
        }

        public static IEnumerable<object[]> ToObject_TestData()
        {
            // SByte
            yield return new object[] { typeof(SByteEnum), (SByteEnum)0x42, (SByteEnum)0x42 };
            yield return new object[] { typeof(SByteEnum), sbyte.MinValue, SByteEnum.Min };
            yield return new object[] { typeof(SByteEnum), (sbyte)2, SByteEnum.Two };
            yield return new object[] { typeof(SByteEnum), (sbyte)22, (SByteEnum)22 };

            // Byte
            yield return new object[] { typeof(ByteEnum), byte.MaxValue, ByteEnum.Max };
            yield return new object[] { typeof(ByteEnum), (byte)1, ByteEnum.One };
            yield return new object[] { typeof(ByteEnum), (byte)11, (ByteEnum)11 };
            yield return new object[] { typeof(ByteEnum), (ulong)0x0ccccccccccccc2aL, (ByteEnum)0x2a };

            // Int16
            yield return new object[] { typeof(Int16Enum), short.MinValue, Int16Enum.Min };
            yield return new object[] { typeof(Int16Enum), (short)2, Int16Enum.Two };
            yield return new object[] { typeof(Int16Enum), (short)44, (Int16Enum)44 };

            // UInt16
            yield return new object[] { typeof(UInt16Enum), ushort.MaxValue, UInt16Enum.Max };
            yield return new object[] { typeof(UInt16Enum), (ushort)1, UInt16Enum.One };
            yield return new object[] { typeof(UInt16Enum), (ushort)33, (UInt16Enum)33 };

            // Int32
            yield return new object[] { typeof(Int32Enum), int.MinValue, Int32Enum.Min };
            yield return new object[] { typeof(Int32Enum), 2, Int32Enum.Two };
            yield return new object[] { typeof(Int32Enum), 66, (Int32Enum)66 };
            yield return new object[] { typeof(Int32Enum), 'a', (Int32Enum)97 };
            yield return new object[] { typeof(Int32Enum), 'b', (Int32Enum)98 };
            yield return new object[] { typeof(Int32Enum), true, (Int32Enum)1 };

            // UInt32
            yield return new object[] { typeof(UInt32Enum), uint.MaxValue, UInt32Enum.Max };
            yield return new object[] { typeof(UInt32Enum), (uint)1, UInt32Enum.One };
            yield return new object[] { typeof(UInt32Enum), (uint)55, (UInt32Enum)55 };

            // Int64
            yield return new object[] { typeof(Int64Enum), long.MinValue, Int64Enum.Min };
            yield return new object[] { typeof(Int64Enum), (long)2, Int64Enum.Two };
            yield return new object[] { typeof(Int64Enum), (long)88, (Int64Enum)88 };

            // UInt64
            yield return new object[] { typeof(UInt64Enum), ulong.MaxValue, UInt64Enum.Max };
            yield return new object[] { typeof(UInt64Enum), (ulong)1, UInt64Enum.One };
            yield return new object[] { typeof(UInt64Enum), (ulong)77, (UInt64Enum)77 };
            yield return new object[] { typeof(UInt64Enum), (ulong)0x0123456789abcdefL, (UInt64Enum)0x0123456789abcdefL };

#if netcoreapp
            // Char
            yield return new object[] { s_charEnumType, (char)1, Enum.Parse(s_charEnumType, "Value1") };
            yield return new object[] { s_charEnumType, (char)2, Enum.Parse(s_charEnumType, "Value2") };

            // Bool
            yield return new object[] { s_boolEnumType, true, Enum.Parse(s_boolEnumType, "Value1") };
            yield return new object[] { s_boolEnumType, false, Enum.Parse(s_boolEnumType, "Value2") };
#endif // netcoreapp            
        }

        [Theory]
        [MemberData(nameof(ToObject_TestData))]
        public static void ToObject(Type enumType, object value, Enum expected)
        {
            Assert.Equal(expected, Enum.ToObject(enumType, value));
        }

        public static IEnumerable<object[]> ToObject_InvalidEnumType_TestData()
        {
            yield return new object[] { null, typeof(ArgumentNullException) };
            yield return new object[] { typeof(Enum), typeof(ArgumentException) };
            yield return new object[] { typeof(object), typeof(ArgumentException) };
#if netcoreapp
            yield return new object[] { GetNonRuntimeEnumTypeBuilder(typeof(int)), typeof(ArgumentException) };
#endif // netcoreapp            
        }

        [Theory]
        [MemberData(nameof(ToObject_InvalidEnumType_TestData))]
        public static void ToObject_InvalidEnumType_ThrowsException(Type enumType, Type exceptionType)
        {
            Assert.Throws(exceptionType, () => Enum.ToObject(enumType, 5));
            Assert.Throws(exceptionType, () => Enum.ToObject(enumType, (sbyte)5));
            Assert.Throws(exceptionType, () => Enum.ToObject(enumType, (short)5));
            Assert.Throws(exceptionType, () => Enum.ToObject(enumType, (long)5));
            Assert.Throws(exceptionType, () => Enum.ToObject(enumType, (uint)5));
            Assert.Throws(exceptionType, () => Enum.ToObject(enumType, (byte)5));
            Assert.Throws(exceptionType, () => Enum.ToObject(enumType, (ushort)5));
            Assert.Throws(exceptionType, () => Enum.ToObject(enumType, (ulong)5));
            Assert.Throws(exceptionType, () => Enum.ToObject(enumType, 'a'));
            Assert.Throws(exceptionType, () => Enum.ToObject(enumType, true));
        }

        public static IEnumerable<object[]> ToObject_InvalidValue_TestData()
        {
            yield return new object[] { typeof(SimpleEnum), null, typeof(ArgumentNullException) };
            yield return new object[] { typeof(SimpleEnum), "Hello", typeof(ArgumentException) };
#if netcoreapp            
            yield return new object[] { s_floatEnumType, 1.0f, typeof(ArgumentException) };
            yield return new object[] { s_doubleEnumType, 1.0, typeof(ArgumentException) };
            yield return new object[] { s_intPtrEnumType, (IntPtr)1, typeof(ArgumentException) };
            yield return new object[] { s_uintPtrEnumType, (UIntPtr)1, typeof(ArgumentException) };
#endif // netcoreapp
        }

        [Theory]
        [MemberData(nameof(ToObject_InvalidValue_TestData))]
        public static void ToObject_InvalidValue_ThrowsException(Type enumType, object value, Type exceptionType)
        {
            if (exceptionType == typeof(ArgumentNullException))
                AssertExtensions.Throws<ArgumentNullException>("value", () => Enum.ToObject(enumType, value));
            else if (exceptionType == typeof(ArgumentException))
                AssertExtensions.Throws<ArgumentException>("value", () => Enum.ToObject(enumType, value));
            else
                throw new Exception($"Unexpected exception type in {nameof(ToObject_InvalidValue_TestData)} : {exceptionType}");
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            // SByte
            yield return new object[] { SByteEnum.One, SByteEnum.One, true };
            yield return new object[] { SByteEnum.One, SByteEnum.Two, false };
            yield return new object[] { SByteEnum.One, ByteEnum.One, false };
            yield return new object[] { SByteEnum.One, (sbyte)1, false };
            yield return new object[] { SByteEnum.One, new object(), false };
            yield return new object[] { SByteEnum.One, null, false };

            // Byte
            yield return new object[] { ByteEnum.One, ByteEnum.One, true };
            yield return new object[] { ByteEnum.One, ByteEnum.Two, false };
            yield return new object[] { ByteEnum.One, SByteEnum.One, false };
            yield return new object[] { ByteEnum.One, (byte)1, false };
            yield return new object[] { ByteEnum.One, new object(), false };
            yield return new object[] { ByteEnum.One, null, false };

            // Int16
            yield return new object[] { Int16Enum.One, Int16Enum.One, true };
            yield return new object[] { Int16Enum.One, Int16Enum.Two, false };
            yield return new object[] { Int16Enum.One, UInt16Enum.One, false };
            yield return new object[] { Int16Enum.One, (short)1, false };
            yield return new object[] { Int16Enum.One, new object(), false };
            yield return new object[] { Int16Enum.One, null, false };

            // UInt16
            yield return new object[] { UInt16Enum.One, UInt16Enum.One, true };
            yield return new object[] { UInt16Enum.One, UInt16Enum.Two, false };
            yield return new object[] { UInt16Enum.One, Int16Enum.One, false };
            yield return new object[] { UInt16Enum.One, (ushort)1, false };
            yield return new object[] { UInt16Enum.One, new object(), false };
            yield return new object[] { UInt16Enum.One, null, false };

            // Int32
            yield return new object[] { Int32Enum.One, Int32Enum.One, true };
            yield return new object[] { Int32Enum.One, Int32Enum.Two, false };
            yield return new object[] { Int32Enum.One, UInt32Enum.One, false };
            yield return new object[] { Int32Enum.One, (short)1, false };
            yield return new object[] { Int32Enum.One, new object(), false };
            yield return new object[] { Int32Enum.One, null, false };

            // UInt32
            yield return new object[] { UInt32Enum.One, UInt32Enum.One, true };
            yield return new object[] { UInt32Enum.One, UInt32Enum.Two, false };
            yield return new object[] { UInt32Enum.One, Int32Enum.One, false };
            yield return new object[] { UInt32Enum.One, (ushort)1, false };
            yield return new object[] { UInt32Enum.One, new object(), false };
            yield return new object[] { UInt32Enum.One, null, false };

            // Int64
            yield return new object[] { Int64Enum.One, Int64Enum.One, true };
            yield return new object[] { Int64Enum.One, Int64Enum.Two, false };
            yield return new object[] { Int64Enum.One, UInt64Enum.One, false };
            yield return new object[] { Int64Enum.One, (long)1, false };
            yield return new object[] { Int64Enum.One, new object(), false };
            yield return new object[] { Int64Enum.One, null, false };

            // UInt64
            yield return new object[] { UInt64Enum.One, UInt64Enum.One, true };
            yield return new object[] { UInt64Enum.One, UInt64Enum.Two, false };
            yield return new object[] { UInt64Enum.One, Int64Enum.One, false };
            yield return new object[] { UInt64Enum.One, (ulong)1, false };
            yield return new object[] { UInt64Enum.One, new object(), false };
            yield return new object[] { UInt64Enum.One, null, false };

#if netcoreapp
            // Char
            yield return new object[] { Enum.Parse(s_charEnumType, "Value1"), Enum.Parse(s_charEnumType, "Value1"), true };
            yield return new object[] { Enum.Parse(s_charEnumType, "Value1"), Enum.Parse(s_charEnumType, "Value2"), false };
            yield return new object[] { Enum.Parse(s_charEnumType, "Value1"), UInt16Enum.One, false };
            yield return new object[] { Enum.Parse(s_charEnumType, "Value1"), (char)1, false };
            yield return new object[] { Enum.Parse(s_charEnumType, "Value1"), new object(), false };
            yield return new object[] { Enum.Parse(s_charEnumType, "Value1"), null, false };

            // Bool
            yield return new object[] { Enum.Parse(s_boolEnumType, "Value1"), Enum.Parse(s_boolEnumType, "Value1"), true };
            yield return new object[] { Enum.Parse(s_boolEnumType, "Value1"), Enum.Parse(s_boolEnumType, "Value2"), false };
            yield return new object[] { Enum.Parse(s_boolEnumType, "Value1"), UInt16Enum.One, false };
            yield return new object[] { Enum.Parse(s_boolEnumType, "Value1"), true, false };
            yield return new object[] { Enum.Parse(s_boolEnumType, "Value1"), new object(), false };
            yield return new object[] { Enum.Parse(s_boolEnumType, "Value1"), null, false };

            // Single
            yield return new object[] { Enum.ToObject(s_floatEnumType, 1), Enum.ToObject(s_floatEnumType, 1), true };
            yield return new object[] { Enum.ToObject(s_floatEnumType, 1), Enum.ToObject(s_floatEnumType, 2), false };
            yield return new object[] { Enum.ToObject(s_floatEnumType, 1), Enum.ToObject(s_doubleEnumType, 1), false };
            yield return new object[] { Enum.ToObject(s_floatEnumType, 1), 1.0f, false };
            yield return new object[] { Enum.ToObject(s_floatEnumType, 1), new object(), false };
            yield return new object[] { Enum.ToObject(s_floatEnumType, 1), null, false };

            // Double
            yield return new object[] { Enum.ToObject(s_doubleEnumType, 1), Enum.ToObject(s_doubleEnumType, 1), true };
            yield return new object[] { Enum.ToObject(s_doubleEnumType, 1), Enum.ToObject(s_doubleEnumType, 2), false };
            yield return new object[] { Enum.ToObject(s_doubleEnumType, 1), Enum.ToObject(s_floatEnumType, 1), false };
            yield return new object[] { Enum.ToObject(s_doubleEnumType, 1), 1.0, false };
            yield return new object[] { Enum.ToObject(s_doubleEnumType, 1), new object(), false };
            yield return new object[] { Enum.ToObject(s_doubleEnumType, 1), null, false };

            // IntPtr
            yield return new object[] { Enum.ToObject(s_intPtrEnumType, 1), Enum.ToObject(s_intPtrEnumType, 1), true };
            yield return new object[] { Enum.ToObject(s_intPtrEnumType, 1), Enum.ToObject(s_intPtrEnumType, 2), false };
            yield return new object[] { Enum.ToObject(s_intPtrEnumType, 1), Enum.ToObject(s_uintPtrEnumType, 1), false };
            yield return new object[] { Enum.ToObject(s_intPtrEnumType, 1), (IntPtr)1, false };
            yield return new object[] { Enum.ToObject(s_intPtrEnumType, 1), new object(), false };
            yield return new object[] { Enum.ToObject(s_intPtrEnumType, 1), null, false };

            // UIntPtr
            yield return new object[] { Enum.ToObject(s_uintPtrEnumType, 1), Enum.ToObject(s_uintPtrEnumType, 1), true };
            yield return new object[] { Enum.ToObject(s_uintPtrEnumType, 1), Enum.ToObject(s_uintPtrEnumType, 2), false };
            yield return new object[] { Enum.ToObject(s_uintPtrEnumType, 1), Enum.ToObject(s_intPtrEnumType, 1), false };
            yield return new object[] { Enum.ToObject(s_uintPtrEnumType, 1), (UIntPtr)1, false };
            yield return new object[] { Enum.ToObject(s_uintPtrEnumType, 1), new object(), false };
            yield return new object[] { Enum.ToObject(s_uintPtrEnumType, 1), null, false };
#endif // netcoreapp 
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public static void Equals(Enum e, object obj, bool expected)
        {
            Assert.Equal(expected, e.Equals(obj));
            Assert.Equal(e.GetHashCode(), e.GetHashCode());
        }

        public static IEnumerable<object[]> CompareTo_TestData()
        {
            // SByte
            yield return new object[] { SByteEnum.One, SByteEnum.One, 0 };
            yield return new object[] { SByteEnum.One, SByteEnum.Min, 1 };
            yield return new object[] { SByteEnum.One, SByteEnum.Max, -1 };
            yield return new object[] { SByteEnum.One, null, 1 };

            // Byte
            yield return new object[] { ByteEnum.One, ByteEnum.One, 0 };
            yield return new object[] { ByteEnum.One, ByteEnum.Min, 1 };
            yield return new object[] { ByteEnum.One, ByteEnum.Max, -1 };
            yield return new object[] { ByteEnum.One, null, 1 };

            // Int16
            yield return new object[] { Int16Enum.One, Int16Enum.One, 0 };
            yield return new object[] { Int16Enum.One, Int16Enum.Min, 1 };
            yield return new object[] { Int16Enum.One, Int16Enum.Max, -1 };
            yield return new object[] { Int16Enum.One, null, 1 };

            // UInt16
            yield return new object[] { UInt16Enum.One, UInt16Enum.One, 0 };
            yield return new object[] { UInt16Enum.One, UInt16Enum.Min, 1 };
            yield return new object[] { UInt16Enum.One, UInt16Enum.Max, -1 };
            yield return new object[] { UInt16Enum.One, null, 1 };

            // Int32
            yield return new object[] { SimpleEnum.Red, SimpleEnum.Red, 0 };
            yield return new object[] { SimpleEnum.Red, (SimpleEnum)0, 1 };
            yield return new object[] { SimpleEnum.Red, (SimpleEnum)2, -1 };
            yield return new object[] { SimpleEnum.Green, SimpleEnum.Green_a, 0 };
            yield return new object[] { SimpleEnum.Green, null, 1 };

            // UInt32
            yield return new object[] { UInt32Enum.One, UInt32Enum.One, 0 };
            yield return new object[] { UInt32Enum.One, UInt32Enum.Min, 1 };
            yield return new object[] { UInt32Enum.One, UInt32Enum.Max, -1 };
            yield return new object[] { UInt32Enum.One, null, 1 };

            // Int64
            yield return new object[] { Int64Enum.One, Int64Enum.One, 0 };
            yield return new object[] { Int64Enum.One, Int64Enum.Min, 1 };
            yield return new object[] { Int64Enum.One, Int64Enum.Max, -1 };
            yield return new object[] { Int64Enum.One, null, 1 };

            // UInt64
            yield return new object[] { UInt64Enum.One, UInt64Enum.One, 0 };
            yield return new object[] { UInt64Enum.One, UInt64Enum.Min, 1 };
            yield return new object[] { UInt64Enum.One, UInt64Enum.Max, -1 };
            yield return new object[] { UInt64Enum.One, null, 1 };

#if netcoreapp
            // Char
            yield return new object[] { Enum.Parse(s_charEnumType, "Value2"), Enum.Parse(s_charEnumType, "Value2"), 0 };
            yield return new object[] { Enum.Parse(s_charEnumType, "Value2"), Enum.Parse(s_charEnumType, "Value1"), 1 };
            yield return new object[] { Enum.Parse(s_charEnumType, "Value1"), Enum.Parse(s_charEnumType, "Value2"), -1 };
            yield return new object[] { Enum.Parse(s_charEnumType, "Value2"), null, 1 };

            // Bool
            yield return new object[] { Enum.Parse(s_boolEnumType, "Value1"), Enum.Parse(s_boolEnumType, "Value1"), 0 };
            yield return new object[] { Enum.Parse(s_boolEnumType, "Value1"), Enum.Parse(s_boolEnumType, "Value2"), 1 };
            yield return new object[] { Enum.Parse(s_boolEnumType, "Value2"), Enum.Parse(s_boolEnumType, "Value1"), -1 };
            yield return new object[] { Enum.Parse(s_boolEnumType, "Value1"), null, 1 };

            // Single
            yield return new object[] { Enum.ToObject(s_floatEnumType, 1), Enum.ToObject(s_floatEnumType, 1), 0 };
            yield return new object[] { Enum.ToObject(s_floatEnumType, 1), Enum.ToObject(s_floatEnumType, 2), -1 };
            yield return new object[] { Enum.ToObject(s_floatEnumType, 3), Enum.ToObject(s_floatEnumType, 2), 1 };
            yield return new object[] { Enum.ToObject(s_floatEnumType, 1), null, 1 };

            // Double
            yield return new object[] { Enum.ToObject(s_doubleEnumType, 1), Enum.ToObject(s_doubleEnumType, 1), 0 };
            yield return new object[] { Enum.ToObject(s_doubleEnumType, 1), Enum.ToObject(s_doubleEnumType, 2), -1 };
            yield return new object[] { Enum.ToObject(s_doubleEnumType, 3), Enum.ToObject(s_doubleEnumType, 2), 1 };
            yield return new object[] { Enum.ToObject(s_doubleEnumType, 1), null, 1 };

            // IntPtr
            yield return new object[] { Enum.ToObject(s_intPtrEnumType, 1), Enum.ToObject(s_intPtrEnumType, 1), 0 };
            yield return new object[] { Enum.ToObject(s_intPtrEnumType, 1), Enum.ToObject(s_intPtrEnumType, 2), -1 };
            yield return new object[] { Enum.ToObject(s_intPtrEnumType, 3), Enum.ToObject(s_intPtrEnumType, 2), 1 };
            yield return new object[] { Enum.ToObject(s_intPtrEnumType, 1), null, 1 };

            // UIntPtr
            yield return new object[] { Enum.ToObject(s_uintPtrEnumType, 1), Enum.ToObject(s_uintPtrEnumType, 1), 0 };
            yield return new object[] { Enum.ToObject(s_uintPtrEnumType, 1), Enum.ToObject(s_uintPtrEnumType, 2), -1 };
            yield return new object[] { Enum.ToObject(s_uintPtrEnumType, 3), Enum.ToObject(s_uintPtrEnumType, 2), 1 };
            yield return new object[] { Enum.ToObject(s_uintPtrEnumType, 1), null, 1 };
#endif // netcoreapp 
        }

        [Theory]
        [MemberData(nameof(CompareTo_TestData))]
        public static void CompareTo(Enum e, object target, int expected)
        {
            Assert.Equal(expected, Math.Sign(e.CompareTo(target)));
        }

        [Fact]
        public static void CompareTo_ObjectNotEnum_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => SimpleEnum.Red.CompareTo((sbyte)1)); // Target is not an enum type
            AssertExtensions.Throws<ArgumentException>(null, () => SimpleEnum.Red.CompareTo(Int32Enum.One)); // Target is a different enum type
        }

        public static IEnumerable<object[]> GetUnderlyingType_TestData()
        {
            yield return new object[] { typeof(SByteEnum), typeof(sbyte) };
            yield return new object[] { typeof(ByteEnum), typeof(byte) };
            yield return new object[] { typeof(Int16Enum), typeof(short) };
            yield return new object[] { typeof(UInt16Enum), typeof(ushort) };
            yield return new object[] { typeof(Int32Enum), typeof(int) };
            yield return new object[] { typeof(UInt32Enum), typeof(uint) };
            yield return new object[] { typeof(Int64Enum), typeof(long) };
            yield return new object[] { typeof(UInt64Enum), typeof(ulong) };
#if netcoreapp
            yield return new object[] { s_charEnumType, typeof(char) };
            yield return new object[] { s_boolEnumType, typeof(bool) };
            yield return new object[] { s_floatEnumType, typeof(float) };
            yield return new object[] { s_doubleEnumType, typeof(double) };
            yield return new object[] { s_intPtrEnumType, typeof(IntPtr) };
            yield return new object[] { s_uintPtrEnumType, typeof(UIntPtr) };
#endif // netcoreapp
        }

        [Theory]
        [MemberData(nameof(GetUnderlyingType_TestData))]
        public static void GetUnderlyingType(Type enumType, Type expected)
        {
            Assert.Equal(expected, Enum.GetUnderlyingType(enumType));
        }

        [Fact]
        public static void GetUnderlyingType_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("enumType", () => Enum.GetUnderlyingType(null)); // Enum type is null
            AssertExtensions.Throws<ArgumentException>("enumType", () => Enum.GetUnderlyingType(typeof(Enum))); // Enum type is simply an enum
        }

        public static IEnumerable<object[]> GetNames_GetValues_TestData()
        {
            // SimpleEnum
            yield return new object[]
            {
                typeof(SimpleEnum),
                new string[] { "Red", "Blue", "Green", "Green_a", "Green_b", "B" },
                new object[] { SimpleEnum.Red, SimpleEnum.Blue, SimpleEnum.Green, SimpleEnum.Green_a, SimpleEnum.Green_b, SimpleEnum.B }
            };

            // SByte
            yield return new object[]
            {
                typeof(SByteEnum),
                new string[] { "One", "Two", "Max", "Min" },
                new object[] { SByteEnum.One, SByteEnum.Two, SByteEnum.Max, SByteEnum.Min }
            };

            // Byte
            yield return new object[]
            {
                typeof(ByteEnum),
                new string[] { "Min", "One", "Two", "Max" },
                new object[] { ByteEnum.Min, ByteEnum.One, ByteEnum.Two, ByteEnum.Max }
            };

            // Int16
            yield return new object[]
            {
                typeof(Int16Enum),
                new string[] { "One", "Two", "Max", "Min" },
                new object[] { Int16Enum.One, Int16Enum.Two, Int16Enum.Max, Int16Enum.Min }
            };

            // UInt16
            yield return new object[]
            {
                typeof(UInt16Enum),
                new string[] { "Min", "One", "Two", "Max" },
                new object[] { UInt16Enum.Min, UInt16Enum.One, UInt16Enum.Two, UInt16Enum.Max }
            };

            // Int32
            yield return new object[]
            {
                typeof(Int32Enum),
                new string[] { "One", "Two", "Max", "Min" },
                new object[] { Int32Enum.One, Int32Enum.Two, Int32Enum.Max, Int32Enum.Min }
            };

            // UInt32
            yield return new object[]
            {
                typeof(UInt32Enum),
                new string[] { "Min", "One", "Two", "Max" },
                new object[] { UInt32Enum.Min, UInt32Enum.One, UInt32Enum.Two, UInt32Enum.Max }
            };

            // Int64
            yield return new object[]
            {
                typeof(Int64Enum),
                new string[] { "One", "Two", "Max", "Min" },
                new object[] { Int64Enum.One, Int64Enum.Two, Int64Enum.Max, Int64Enum.Min }
            };

            // UInt64
            yield return new object[]
            {
                typeof(UInt64Enum),
                new string[] { "Min", "One", "Two", "Max" },
                new object[] { UInt64Enum.Min, UInt64Enum.One, UInt64Enum.Two, UInt64Enum.Max }
            };

#if netcoreapp
            // Char
            yield return new object[]
            {
                s_charEnumType,
                new string[] { "Value0x0000", "Value1", "Value2", "Value0x0010", "Value0x0f06", "Value0x1000", "Value0x3000", "Value0x3f06", "Value0x3f16" },
                new object[] { Enum.Parse(s_charEnumType, "Value0x0000"), Enum.Parse(s_charEnumType, "Value1"), Enum.Parse(s_charEnumType, "Value2"), Enum.Parse(s_charEnumType, "Value0x0010"), Enum.Parse(s_charEnumType, "Value0x0f06"), Enum.Parse(s_charEnumType, "Value0x1000"), Enum.Parse(s_charEnumType, "Value0x3000"), Enum.Parse(s_charEnumType, "Value0x3f06"), Enum.Parse(s_charEnumType, "Value0x3f16") }
            };

            // Bool
            yield return new object[]
            {
                s_boolEnumType,
                new string[] { "Value2", "Value1" },
                new object[] { Enum.Parse(s_boolEnumType, "Value2"), Enum.Parse(s_boolEnumType, "Value1") }
            };

            // Single
            yield return new object[]
            {
                s_floatEnumType,
                new string[] { "Value1", "Value2", "Value0x3f06", "Value0x3000", "Value0x0f06", "Value0x1000", "Value0x0000", "Value0x0010", "Value0x3f16" },
                new object[] { Enum.Parse(s_floatEnumType, "Value1"), Enum.Parse(s_floatEnumType, "Value2"), Enum.Parse(s_floatEnumType, "Value0x3f06"), Enum.Parse(s_floatEnumType, "Value0x3000"), Enum.Parse(s_floatEnumType, "Value0x0f06"), Enum.Parse(s_floatEnumType, "Value0x1000"), Enum.Parse(s_floatEnumType, "Value0x0000"), Enum.Parse(s_floatEnumType, "Value0x0010"), Enum.Parse(s_floatEnumType, "Value0x3f16") }
            };

            // Double
            yield return new object[]
            {
                s_doubleEnumType,
                new string[] { "Value1", "Value2", "Value0x3f06", "Value0x3000", "Value0x0f06", "Value0x1000", "Value0x0000", "Value0x0010", "Value0x3f16" },
                new object[] { Enum.Parse(s_doubleEnumType, "Value1"), Enum.Parse(s_doubleEnumType, "Value2"), Enum.Parse(s_doubleEnumType, "Value0x3f06"), Enum.Parse(s_doubleEnumType, "Value0x3000"), Enum.Parse(s_doubleEnumType, "Value0x0f06"), Enum.Parse(s_doubleEnumType, "Value0x1000"), Enum.Parse(s_doubleEnumType, "Value0x0000"), Enum.Parse(s_doubleEnumType, "Value0x0010"), Enum.Parse(s_doubleEnumType, "Value0x3f16") }
            };

            // IntPtr
            yield return new object[]
            {
                s_intPtrEnumType,
                new string[0],
                new object[0]
            };

            // UIntPtr
            yield return new object[]
            {
                s_uintPtrEnumType,
                new string[0],
                new object[0]
            };
#endif // netcoreapp
        }

        [Theory]
        [MemberData(nameof(GetNames_GetValues_TestData))]
        public static void GetNames_GetValues(Type enumType, string[] expectedNames, object[] expectedValues)
        {
            Assert.Equal(expectedNames, Enum.GetNames(enumType));
            Assert.Equal(expectedValues, Enum.GetValues(enumType).Cast<object>().ToArray());
        }

        [Fact]
        public static void GetNames_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("enumType", () => Enum.GetNames(null)); // Enum type is null
            AssertExtensions.Throws<ArgumentException>("enumType", () => Enum.GetNames(typeof(object))); // Enum type is not an enum
        }

        [Fact]
        public static void GetValues_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("enumType", () => Enum.GetValues(null)); // Enum type is null
            AssertExtensions.Throws<ArgumentException>("enumType", () => Enum.GetValues(typeof(object))); // Enum type is not an enum
        }

        public static IEnumerable<object[]> ToString_Format_TestData()
        {
            // Format "D": the decimal equivalent of the value is returned.
            // Format "X": value in hex form without a leading "0x"
            // Format "F": value is treated as a bit field that contains one or more flags that consist of one or more bits.
            // If value is equal to a combination of named enumerated constants, a delimiter-separated list of the names
            // of those constants is returned. value is searched for flags, going from the flag with the largest value
            // to the smallest value. For each flag that corresponds to a bit field in value, the name of the constant
            // is concatenated to the delimiter-separated list. The value of that flag is then excluded from further
            // consideration, and the search continues for the next flag.
            // If value is not equal to a combination of named enumerated constants, the decimal equivalent of value is returned.
            // Format "G": if value is equal to a named enumerated constant, the name of that constant is returned.
            // Otherwise, if "[Flags]" present, do as Format "F" - else return the decimal value of "value".

            // "D": SByte
            yield return new object[] { SByteEnum.Min, "D", "-128" };
            yield return new object[] { SByteEnum.One, "D", "1" };
            yield return new object[] { SByteEnum.Two, "D", "2" };
            yield return new object[] { (SByteEnum)99, "D", "99" };
            yield return new object[] { SByteEnum.Max, "D", "127" };

            // "D": Byte
            yield return new object[] { ByteEnum.Min, "D", "0" };
            yield return new object[] { ByteEnum.One, "D", "1" };
            yield return new object[] { ByteEnum.Two, "D", "2" };
            yield return new object[] { (ByteEnum)99, "D", "99" };
            yield return new object[] { ByteEnum.Max, "D", "255" };

            // "D": Int16
            yield return new object[] { Int16Enum.Min, "D", "-32768" };
            yield return new object[] { Int16Enum.One, "D", "1" };
            yield return new object[] { Int16Enum.Two, "D", "2" };
            yield return new object[] { (Int16Enum)99, "D", "99" };
            yield return new object[] { Int16Enum.Max, "D", "32767" };

            // "D": UInt16
            yield return new object[] { UInt16Enum.Min, "D", "0" };
            yield return new object[] { UInt16Enum.One, "D", "1" };
            yield return new object[] { UInt16Enum.Two, "D", "2" };
            yield return new object[] { (UInt16Enum)99, "D", "99" };
            yield return new object[] { UInt16Enum.Max, "D", "65535" };

            // "D": Int32
            yield return new object[] { Int32Enum.Min, "D", "-2147483648" };
            yield return new object[] { Int32Enum.One, "D", "1" };
            yield return new object[] { Int32Enum.Two, "D", "2" };
            yield return new object[] { (Int32Enum)99, "D", "99" };
            yield return new object[] { Int32Enum.Max, "D", "2147483647" };

            // "D": UInt32
            yield return new object[] { UInt32Enum.Min, "D", "0" };
            yield return new object[] { UInt32Enum.One, "D", "1" };
            yield return new object[] { UInt32Enum.Two, "D", "2" };
            yield return new object[] { (UInt32Enum)99, "D", "99" };
            yield return new object[] { UInt32Enum.Max, "D", "4294967295" };

            // "D": Int64
            yield return new object[] { Int64Enum.Min, "D", "-9223372036854775808" };
            yield return new object[] { Int64Enum.One, "D", "1" };
            yield return new object[] { Int64Enum.Two, "D", "2" };
            yield return new object[] { (Int64Enum)99, "D", "99" };
            yield return new object[] { Int64Enum.Max, "D", "9223372036854775807" };

            // "D": UInt64
            yield return new object[] { UInt64Enum.Min, "D", "0" };
            yield return new object[] { UInt64Enum.One, "D", "1" };
            yield return new object[] { UInt64Enum.Two, "D", "2" };
            yield return new object[] { (UInt64Enum)99, "D", "99" };
            yield return new object[] { UInt64Enum.Max, "D", "18446744073709551615" };

#if netcoreapp
            // "D": Char
            yield return new object[] { Enum.ToObject(s_charEnumType, (char)0), "D", ((char)0).ToString() };
            yield return new object[] { Enum.ToObject(s_charEnumType, (char)1), "D", ((char)1).ToString() };
            yield return new object[] { Enum.ToObject(s_charEnumType, (char)2), "D", ((char)2).ToString() };
            yield return new object[] { Enum.ToObject(s_charEnumType, char.MaxValue), "D", char.MaxValue.ToString() };

            // "D:" Bool
            yield return new object[] { Enum.ToObject(s_boolEnumType, true), "D", bool.TrueString };
            yield return new object[] { Enum.ToObject(s_boolEnumType, false), "D", bool.FalseString };
            yield return new object[] { Enum.ToObject(s_boolEnumType, 123), "D", bool.TrueString };

            // "D": Single
            yield return new object[] { Enum.ToObject(s_floatEnumType, 0), "D", "0" };
            yield return new object[] { Enum.ToObject(s_floatEnumType, 1), "D", float.Epsilon.ToString() };
            yield return new object[] { Enum.ToObject(s_floatEnumType, int.MaxValue), "D", float.NaN.ToString() };

            // "D": Double
            yield return new object[] { Enum.ToObject(s_doubleEnumType, 0), "D", "0" };
            yield return new object[] { Enum.ToObject(s_doubleEnumType, 1), "D", double.Epsilon.ToString() };
            yield return new object[] { Enum.ToObject(s_doubleEnumType, long.MaxValue), "D", double.NaN.ToString() };
#endif // netcoreapp

            // "D": SimpleEnum
            yield return new object[] { SimpleEnum.Red, "D", "1" };

            // "X": SByte
            yield return new object[] { SByteEnum.Min, "X", "80" };
            yield return new object[] { SByteEnum.One, "X", "01" };
            yield return new object[] { SByteEnum.Two, "X", "02" };
            yield return new object[] { (SByteEnum)99, "X", "63" };
            yield return new object[] { SByteEnum.Max, "X", "7F" };

            // "X": Byte
            yield return new object[] { ByteEnum.Min, "X", "00" };
            yield return new object[] { ByteEnum.One, "X", "01" };
            yield return new object[] { ByteEnum.Two, "X", "02" };
            yield return new object[] { (ByteEnum)99, "X", "63" };
            yield return new object[] { ByteEnum.Max, "X", "FF" };

            // "X": Int16
            yield return new object[] { Int16Enum.Min, "X", "8000" };
            yield return new object[] { Int16Enum.One, "X", "0001" };
            yield return new object[] { Int16Enum.Two, "X", "0002" };
            yield return new object[] { (Int16Enum)99, "X", "0063" };
            yield return new object[] { Int16Enum.Max, "X", "7FFF" };

            // "X": UInt16
            yield return new object[] { UInt16Enum.Min, "X", "0000" };
            yield return new object[] { UInt16Enum.One, "X", "0001" };
            yield return new object[] { UInt16Enum.Two, "X", "0002" };
            yield return new object[] { (UInt16Enum)99, "X", "0063" };
            yield return new object[] { UInt16Enum.Max, "X", "FFFF" };

            // "X": UInt32
            yield return new object[] { UInt32Enum.Min, "X", "00000000" };
            yield return new object[] { UInt32Enum.One, "X", "00000001" };
            yield return new object[] { UInt32Enum.Two, "X", "00000002" };
            yield return new object[] { (UInt32Enum)99, "X", "00000063" };
            yield return new object[] { UInt32Enum.Max, "X", "FFFFFFFF" };

            // "X": Int32
            yield return new object[] { Int32Enum.Min, "X", "80000000" };
            yield return new object[] { Int32Enum.One, "X", "00000001" };
            yield return new object[] { Int32Enum.Two, "X", "00000002" };
            yield return new object[] { (Int32Enum)99, "X", "00000063" };
            yield return new object[] { Int32Enum.Max, "X", "7FFFFFFF" };

            // "X:" Int64
            yield return new object[] { Int64Enum.Min, "X", "8000000000000000" };
            yield return new object[] { Int64Enum.One, "X", "0000000000000001" };
            yield return new object[] { Int64Enum.Two, "X", "0000000000000002" };
            yield return new object[] { (Int64Enum)99, "X", "0000000000000063" };
            yield return new object[] { Int64Enum.Max, "X", "7FFFFFFFFFFFFFFF" };

            // "X": UInt64
            yield return new object[] { UInt64Enum.Min, "X", "0000000000000000" };
            yield return new object[] { UInt64Enum.One, "X", "0000000000000001" };
            yield return new object[] { UInt64Enum.Two, "X", "0000000000000002" };
            yield return new object[] { (UInt64Enum)99, "X", "0000000000000063" };
            yield return new object[] { UInt64Enum.Max, "X", "FFFFFFFFFFFFFFFF" };

#if netcoreapp
            // "X": Char
            yield return new object[] { Enum.ToObject(s_charEnumType, (char)0), "X", "0000" };
            yield return new object[] { Enum.ToObject(s_charEnumType, (char)1), "X", "0001" };
            yield return new object[] { Enum.ToObject(s_charEnumType, (char)2), "X", "0002" };
            yield return new object[] { Enum.ToObject(s_charEnumType, char.MaxValue), "X", "FFFF" };


            // "X": Bool
            yield return new object[] { Enum.ToObject(s_boolEnumType, false), "X", "00" };
            yield return new object[] { Enum.ToObject(s_boolEnumType, true), "X", "01" };
            yield return new object[] { Enum.ToObject(s_boolEnumType, 123), "X", "01" };
#endif // netcoreapp

            // "X": SimpleEnum
            yield return new object[] { SimpleEnum.Red, "X", "00000001" };

            // "F": SByte
            yield return new object[] { SByteEnum.Min, "F", "Min" };
            yield return new object[] { SByteEnum.One | SByteEnum.Two, "F", "One, Two" };
            yield return new object[] { (SByteEnum)5, "F", "5" };
            yield return new object[] { SByteEnum.Max, "F", "Max" };

            // "F": Byte
            yield return new object[] { ByteEnum.Min, "F", "Min" };
            yield return new object[] { ByteEnum.One | ByteEnum.Two, "F", "One, Two" };
            yield return new object[] { (ByteEnum)5, "F", "5" };
            yield return new object[] { ByteEnum.Max, "F", "Max" };

            // "F": Int16
            yield return new object[] { Int16Enum.Min, "F", "Min" };
            yield return new object[] { Int16Enum.One | Int16Enum.Two, "F", "One, Two" };
            yield return new object[] { (Int16Enum)5, "F", "5" };
            yield return new object[] { Int16Enum.Max, "F", "Max" };

            // "F": UInt16
            yield return new object[] { UInt16Enum.Min, "F", "Min" };
            yield return new object[] { UInt16Enum.One | UInt16Enum.Two, "F", "One, Two" };
            yield return new object[] { (UInt16Enum)5, "F", "5" };
            yield return new object[] { UInt16Enum.Max, "F", "Max" };

            // "F": Int32
            yield return new object[] { Int32Enum.Min, "F", "Min" };
            yield return new object[] { Int32Enum.One | Int32Enum.Two, "F", "One, Two" };
            yield return new object[] { (Int32Enum)5, "F", "5" };
            yield return new object[] { Int32Enum.Max, "F", "Max" };

            // "F": UInt32
            yield return new object[] { UInt32Enum.Min, "F", "Min" };
            yield return new object[] { UInt32Enum.One | UInt32Enum.Two, "F", "One, Two" };
            yield return new object[] { (UInt32Enum)5, "F", "5" };
            yield return new object[] { UInt32Enum.Max, "F", "Max" };

            // "F": Int64
            yield return new object[] { Int64Enum.Min, "F", "Min" };
            yield return new object[] { Int64Enum.One | Int64Enum.Two, "F", "One, Two" };
            yield return new object[] { (Int64Enum)5, "F", "5" };
            yield return new object[] { Int64Enum.Max, "F", "Max" };

            // "F": UInt64
            yield return new object[] { UInt64Enum.Min, "F", "Min" };
            yield return new object[] { UInt64Enum.One | UInt64Enum.Two, "F", "One, Two" };
            yield return new object[] { (UInt64Enum)5, "F", "5" };
            yield return new object[] { UInt64Enum.Max, "F", "Max" };
            
#if netcoreapp
            // "F": Char
            yield return new object[] { Enum.ToObject(s_charEnumType, (char)1), "F", "Value1" };
            yield return new object[] { Enum.ToObject(s_charEnumType, (char)(1 | 2)), "F", "Value1, Value2" };
            yield return new object[] { Enum.ToObject(s_charEnumType, (char)5), "F", ((char)5).ToString() };
            yield return new object[] { Enum.ToObject(s_charEnumType, char.MaxValue), "F", char.MaxValue.ToString() };

            // "F": Bool
            yield return new object[] { Enum.ToObject(s_boolEnumType, true), "F", "Value1" };
            yield return new object[] { Enum.ToObject(s_boolEnumType, false), "F", "Value2" };

            // "F": IntPtr
            yield return new object[] { Enum.ToObject(s_intPtrEnumType, 5), "F", "5" };

            // "F": UIntPtr
            yield return new object[] { Enum.ToObject(s_uintPtrEnumType, 5), "F", "5" };
#endif // netcoreapp

            // "F": SimpleEnum
            yield return new object[] { SimpleEnum.Red, "F", "Red" };
            yield return new object[] { SimpleEnum.Blue, "F", "Blue" };
            yield return new object[] { (SimpleEnum)99, "F", "99" };
            yield return new object[] { (SimpleEnum)0, "F", "0" }; // Not found

            // "F": Flags Attribute
            yield return new object[] { AttributeTargets.Class | AttributeTargets.Delegate, "F", "Class, Delegate" };

#if netcoreapp
            // "G": Char
            yield return new object[] { Enum.ToObject(s_charEnumType, char.MaxValue), "G", char.MaxValue.ToString() };
#endif

            // "G": SByte
            yield return new object[] { SByteEnum.Min, "G", "Min" };
            yield return new object[] { (SByteEnum)3, "G", "3" }; // No [Flags] attribute
            yield return new object[] { SByteEnum.Max, "G", "Max" };

            // "G": Byte
            yield return new object[] { ByteEnum.Min, "G", "Min" };
            yield return new object[] { (ByteEnum)0xff, "G", "Max" };
            yield return new object[] { (ByteEnum)3, "G", "3" }; // No [Flags] attribute
            yield return new object[] { ByteEnum.Max, "G", "Max" };

            // "G": Int16
            yield return new object[] { Int16Enum.Min, "G", "Min" };
            yield return new object[] { (Int16Enum)3, "G", "3" }; // No [Flags] attribute
            yield return new object[] { Int16Enum.Max, "G", "Max" };

            // "G": UInt16
            yield return new object[] { UInt16Enum.Min, "G", "Min" };
            yield return new object[] { (UInt16Enum)3, "G", "3" }; // No [Flags] attribute
            yield return new object[] { UInt16Enum.Max, "G", "Max" };

            // "G": Int32
            yield return new object[] { Int32Enum.Min, "G", "Min" };
            yield return new object[] { (Int32Enum)3, "G", "3" }; // No [Flags] attribute
            yield return new object[] { Int32Enum.Max, "G", "Max" };

            // "G": UInt32
            yield return new object[] { UInt32Enum.Min, "G", "Min" };
            yield return new object[] { (UInt32Enum)3, "G", "3" }; // No [Flags] attribute
            yield return new object[] { UInt32Enum.Max, "G", "Max" };

            // "G": Int64
            yield return new object[] { Int64Enum.Min, "G", "Min" };
            yield return new object[] { (Int64Enum)3, "G", "3" }; // No [Flags] attribute
            yield return new object[] { Int64Enum.Max, "G", "Max" };

            // "G": UInt64
            yield return new object[] { UInt64Enum.Min, "G", "Min" };
            yield return new object[] { (UInt64Enum)3, "G", "3" }; // No [Flags] attribute
            yield return new object[] { UInt64Enum.Max, "G", "Max" };

            // "G": SimpleEnum
            yield return new object[] { (SimpleEnum)99, "G", "99" };
            yield return new object[] { (SimpleEnum)0, "G", "0" }; // Not found

            // "G": Flags Attribute
            yield return new object[] { AttributeTargets.Class | AttributeTargets.Delegate, "G", "Class, Delegate" };
        }

#pragma warning disable 618 // ToString with IFormatProvider is marked as Obsolete.
        [Theory]
        [MemberData(nameof(ToString_Format_TestData))]
        public static void ToString_Format(Enum e, string format, string expected)
        {
            if (format.ToUpperInvariant() == "G")
            {
                Assert.Equal(expected, e.ToString());
                Assert.Equal(expected, e.ToString(string.Empty));
                Assert.Equal(expected, e.ToString((string)null));

                Assert.Equal(expected, e.ToString((IFormatProvider)null));
            }

            // Format string is case-insensitive.
            Assert.Equal(expected, e.ToString(format));
            Assert.Equal(expected, e.ToString(format.ToUpperInvariant()));
            Assert.Equal(expected, e.ToString(format.ToLowerInvariant()));

            Assert.Equal(expected, e.ToString(format, (IFormatProvider)null));

            Format(e.GetType(), e, format, expected);
        }
#pragma warning restore 618

        [Fact]
        public static void ToString_Format_MultipleMatches()
        {
            string s = ((SimpleEnum)3).ToString("F");
            Assert.True(s == "Green" || s == "Green_a" || s == "Green_b");

            s = ((SimpleEnum)3).ToString("G");
            Assert.True(s == "Green" || s == "Green_a" || s == "Green_b");
        }

        [Fact]
        public static void ToString_InvalidFormat_ThrowsFormatException()
        {
            SimpleEnum e = SimpleEnum.Red;

            Assert.Throws<FormatException>(() => e.ToString("   \t")); // Format is whitepsace
            Assert.Throws<FormatException>(() => e.ToString("y")); // No such format
        }

        public static IEnumerable<object[]> Format_TestData()
        {
            // Format: D
            yield return new object[] { typeof(SimpleEnum), 1, "D", "1" };

            // Format: X
            yield return new object[] { typeof(SimpleEnum), SimpleEnum.Red, "X", "00000001" };
            yield return new object[] { typeof(SimpleEnum), 1, "X", "00000001" };

            // Format: F
            yield return new object[] { typeof(SimpleEnum), 1, "F", "Red" };
        }

        [Theory]
        [MemberData(nameof(Format_TestData))]
        public static void Format(Type enumType, object value, string format, string expected)
        {
            // Format string is case insensitive
            Assert.Equal(expected, Enum.Format(enumType, value, format.ToUpperInvariant()));
            Assert.Equal(expected, Enum.Format(enumType, value, format.ToLowerInvariant()));
        }

        [Fact]
        public static void Format_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("enumType", () => Enum.Format(null, (Int32Enum)1, "F")); // Enum type is null
            AssertExtensions.Throws<ArgumentNullException>("value", () => Enum.Format(typeof(SimpleEnum), null, "F")); // Value is null
            AssertExtensions.Throws<ArgumentNullException>("format", () => Enum.Format(typeof(SimpleEnum), SimpleEnum.Red, null)); // Format is null

            AssertExtensions.Throws<ArgumentException>("enumType", () => Enum.Format(typeof(object), 1, "F")); // Enum type is not an enum type

            AssertExtensions.Throws<ArgumentException>(null, () => Enum.Format(typeof(SimpleEnum), (Int32Enum)1, "F")); // Value is of the wrong enum type

            AssertExtensions.Throws<ArgumentException>(null, () => Enum.Format(typeof(SimpleEnum), (short)1, "F")); // Value is of the wrong integral
            AssertExtensions.Throws<ArgumentException>(null, () => Enum.Format(typeof(SimpleEnum), "Red", "F")); // Value is of the wrong integral

            Assert.Throws<FormatException>(() => Enum.Format(typeof(SimpleEnum), SimpleEnum.Red, "")); // Format is empty
            Assert.Throws<FormatException>(() => Enum.Format(typeof(SimpleEnum), SimpleEnum.Red, "   \t")); // Format is whitespace
            Assert.Throws<FormatException>(() => Enum.Format(typeof(SimpleEnum), SimpleEnum.Red, "t")); // No such format
        }
    }
}
