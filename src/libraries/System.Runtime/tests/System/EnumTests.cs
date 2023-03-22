// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Tests
{
    public class EnumTests
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

            if (PlatformDetection.IsReflectionEmitSupported && PlatformDetection.IsRareEnumsSupported)
            {
                // Char
                yield return new object[] { "Value1", false, Enum.ToObject(s_charEnumType, (char)1) };
                yield return new object[] { "vaLue2", true, Enum.ToObject(s_charEnumType, (char)2) };
                yield return new object[] { "1", false, Enum.ToObject(s_charEnumType, '1') };

                // Single
                yield return new object[] { "Value1", false, Enum.GetValues(s_floatEnumType).GetValue(1) };
                yield return new object[] { "vaLue2", true, Enum.GetValues(s_floatEnumType).GetValue(2) };
                yield return new object[] { "1", false, Enum.GetValues(s_floatEnumType).GetValue(1) };
                yield return new object[] { "1.0", false, Enum.GetValues(s_floatEnumType).GetValue(1) };

                // Double
                yield return new object[] { "Value1", false, Enum.GetValues(s_doubleEnumType).GetValue(1) };
                yield return new object[] { "vaLue2", true, Enum.GetValues(s_doubleEnumType).GetValue(2) };
                yield return new object[] { "1", false, Enum.GetValues(s_doubleEnumType).GetValue(1) };
                yield return new object[] { "1.0", false, Enum.GetValues(s_doubleEnumType).GetValue(1) };
            }

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
                Assert.True(Enum.TryParse(value.AsSpan(), out result));
                Assert.Equal(expected, result);

                Assert.True(Enum.TryParse(value, out result));
                Assert.Equal(expected, result);


                Assert.Equal(expected, Enum.Parse(expected.GetType(), value.AsSpan()));
                Assert.Equal(expected, Enum.Parse(expected.GetType(), value));

                Assert.Equal(expected, Enum.Parse<T>(value.AsSpan()));
                Assert.Equal(expected, Enum.Parse<T>(value));
            }

            Assert.True(Enum.TryParse(value.AsSpan(), ignoreCase, out result));
            Assert.Equal(expected, result);

            Assert.True(Enum.TryParse(value, ignoreCase, out result));
            Assert.Equal(expected, result);


            Assert.Equal(expected, Enum.Parse(expected.GetType(), value.AsSpan(), ignoreCase));
            Assert.Equal(expected, Enum.Parse(expected.GetType(), value, ignoreCase));

            Assert.Equal(expected, Enum.Parse<T>(value.AsSpan(), ignoreCase));
            Assert.Equal(expected, Enum.Parse<T>(value, ignoreCase));
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

            if (PlatformDetection.IsReflectionEmitSupported && PlatformDetection.IsRareEnumsSupported)
            {
                // Char
                yield return new object[] { s_charEnumType, ((char)1).ToString(), false, typeof(ArgumentException) };
                yield return new object[] { s_charEnumType, ((char)5).ToString(), false, typeof(ArgumentException) };

                // IntPtr
                yield return new object[] { s_intPtrEnumType, "1", false, typeof(InvalidCastException) };
                yield return new object[] { s_intPtrEnumType, "5", false, typeof(InvalidCastException) };

                // UIntPtr
                yield return new object[] { s_uintPtrEnumType, "1", false, typeof(InvalidCastException) };
                yield return new object[] { s_uintPtrEnumType, "5", false, typeof(InvalidCastException) };
            }
        }

        [Theory]
        [MemberData(nameof(Parse_Invalid_TestData))]
        public static void Parse_Invalid(Type enumType, string value, bool ignoreCase, Type exceptionType)
        {
            Type typeArgument = enumType == null || !enumType.GetTypeInfo().IsEnum ? typeof(SimpleEnum) : enumType;
            MethodInfo parseMethod = typeof(EnumTests).GetTypeInfo().GetMethod(nameof(Parse_Generic_Invalid), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(typeArgument);
            parseMethod.Invoke(null, new object[] { enumType, value, ignoreCase, exceptionType });
        }

        private static void Parse_Generic_Invalid<T>(Type enumType, string value, bool ignoreCase, Type exceptionType) where T : struct
        {
            object result = null;
            if (!ignoreCase)
            {
                if (enumType != null && enumType.IsEnum)
                {
                    Assert.False(Enum.TryParse(enumType, value, out result));
                    Assert.Equal(default(object), result);

                    if (value != null)
                        Assert.Throws(exceptionType, () => Enum.Parse<T>(value.AsSpan()));

                    Assert.Throws(exceptionType, () => Enum.Parse<T>(value));
                }
                else
                {
                    if (value != null)
                        Assert.Throws(exceptionType, () => Enum.TryParse(enumType, value.AsSpan(), out result));

                    Assert.Throws(exceptionType, () => Enum.TryParse(enumType, value, out result));
                    Assert.Equal(default(object), result);
                }
            }

            if (enumType != null && enumType.IsEnum)
            {
                Assert.False(Enum.TryParse(enumType, value, ignoreCase, out result));
                Assert.Equal(default(object), result);

                if (value != null)
                    Assert.Throws(exceptionType, () => Enum.Parse<T>(value.AsSpan(), ignoreCase));

                Assert.Throws(exceptionType, () => Enum.Parse<T>(value, ignoreCase));
            }
            else
            {
                if (value != null)
                    Assert.Throws(exceptionType, () => Enum.TryParse(enumType, value.AsSpan(), ignoreCase, out result));

                Assert.Throws(exceptionType, () => Enum.TryParse(enumType, value, ignoreCase, out result));
                Assert.Equal(default(object), result);
            }
        }

        [Theory]
        [InlineData("Yellow")]
        [InlineData("Yellow,Orange")]
        public static void Parse_NonExistentValue_IncludedInErrorMessage(string value)
        {
            ArgumentException e = Assert.Throws<ArgumentException>(() => Enum.Parse(typeof(SimpleEnum), value));
            Assert.Contains(value, e.Message);
        }

        [Theory]
        [InlineData(SByteEnum.Min, "Min")]
        [InlineData(SByteEnum.One, "One")]
        [InlineData(SByteEnum.Two, "Two")]
        [InlineData(SByteEnum.Max, "Max")]
        [InlineData(sbyte.MinValue, "Min")]
        [InlineData((sbyte)1, "One")]
        [InlineData((sbyte)2, "Two")]
        [InlineData(sbyte.MaxValue, "Max")]
        [InlineData((sbyte)3, null)]
        public void GetName_InvokeSByteEnum_ReturnsExpected(object value, string expected)
        {
            TestGetName(typeof(SByteEnum), value, expected);
            if (!(value is SByteEnum enumValue))
            {
                enumValue = (SByteEnum)(sbyte)value;
            }
            Assert.Equal(expected, Enum.GetName<SByteEnum>(enumValue));
        }

        [Theory]
        [InlineData(ByteEnum.Min, "Min")]
        [InlineData(ByteEnum.One, "One")]
        [InlineData(ByteEnum.Two, "Two")]
        [InlineData(ByteEnum.Max, "Max")]
        [InlineData(byte.MinValue, "Min")]
        [InlineData((byte)1, "One")]
        [InlineData((byte)2, "Two")]
        [InlineData(byte.MaxValue, "Max")]
        [InlineData((byte)3, null)]
        public void GetName_InvokeByteEnum_ReturnsExpected(object value, string expected)
        {
            TestGetName(typeof(ByteEnum), value, expected);
            if (!(value is ByteEnum enumValue))
            {
                enumValue = (ByteEnum)(byte)value;
            }
            Assert.Equal(expected, Enum.GetName<ByteEnum>(enumValue));
        }

        [Theory]
        [InlineData(Int16Enum.Min, "Min")]
        [InlineData(Int16Enum.One, "One")]
        [InlineData(Int16Enum.Two, "Two")]
        [InlineData(Int16Enum.Max, "Max")]
        [InlineData(short.MinValue, "Min")]
        [InlineData((short)1, "One")]
        [InlineData((short)2, "Two")]
        [InlineData(short.MaxValue, "Max")]
        [InlineData((short)3, null)]
        public void GetName_InvokeInt16Enum_ReturnsExpected(object value, string expected)
        {
            TestGetName(typeof(Int16Enum), value, expected);
            if (!(value is Int16Enum enumValue))
            {
                enumValue = (Int16Enum)(short)value;
            }
            Assert.Equal(expected, Enum.GetName<Int16Enum>(enumValue));
        }

        [Theory]
        [InlineData(UInt16Enum.Min, "Min")]
        [InlineData(UInt16Enum.One, "One")]
        [InlineData(UInt16Enum.Two, "Two")]
        [InlineData(UInt16Enum.Max, "Max")]
        [InlineData(ushort.MinValue, "Min")]
        [InlineData((ushort)1, "One")]
        [InlineData((ushort)2, "Two")]
        [InlineData(ushort.MaxValue, "Max")]
        [InlineData((ushort)3, null)]
        public void GetName_InvokeUInt16Enum_ReturnsExpected(object value, string expected)
        {
            TestGetName(typeof(UInt16Enum), value, expected);
            if (!(value is UInt16Enum enumValue))
            {
                enumValue = (UInt16Enum)(ushort)value;
            }
            Assert.Equal(expected, Enum.GetName<UInt16Enum>(enumValue));
        }

        [Theory]
        [InlineData(Int32Enum.Min, "Min")]
        [InlineData(Int32Enum.One, "One")]
        [InlineData(Int32Enum.Two, "Two")]
        [InlineData(Int32Enum.Max, "Max")]
        [InlineData(int.MinValue, "Min")]
        [InlineData(1, "One")]
        [InlineData(2, "Two")]
        [InlineData(int.MaxValue, "Max")]
        [InlineData(3, null)]
        public void GetName_InvokeInt32Enum_ReturnsExpected(object value, string expected)
        {
            TestGetName(typeof(Int32Enum), value, expected);
            if (!(value is Int32Enum enumValue))
            {
                enumValue = (Int32Enum)(int)value;
            }
            Assert.Equal(expected, Enum.GetName<Int32Enum>(enumValue));
        }

        [Theory]
        [InlineData(UInt32Enum.Min, "Min")]
        [InlineData(UInt32Enum.One, "One")]
        [InlineData(UInt32Enum.Two, "Two")]
        [InlineData(UInt32Enum.Max, "Max")]
        [InlineData(uint.MinValue, "Min")]
        [InlineData((uint)1, "One")]
        [InlineData((uint)2, "Two")]
        [InlineData(uint.MaxValue, "Max")]
        [InlineData((uint)3, null)]
        public void GetName_InvokeUInt32Enum_ReturnsExpected(object value, string expected)
        {
            TestGetName(typeof(UInt32Enum), value, expected);
            if (!(value is UInt32Enum enumValue))
            {
                enumValue = (UInt32Enum)(uint)value;
            }
            Assert.Equal(expected, Enum.GetName<UInt32Enum>(enumValue));
        }

        [Theory]
        [InlineData(Int64Enum.Min, "Min")]
        [InlineData(Int64Enum.One, "One")]
        [InlineData(Int64Enum.Two, "Two")]
        [InlineData(Int64Enum.Max, "Max")]
        [InlineData(long.MinValue, "Min")]
        [InlineData((long)1, "One")]
        [InlineData((long)2, "Two")]
        [InlineData(long.MaxValue, "Max")]
        [InlineData((long)3, null)]
        public void GetName_InvokeInt64Enum_ReturnsExpected(object value, string expected)
        {
            TestGetName(typeof(Int64Enum), value, expected);
            if (!(value is Int64Enum enumValue))
            {
                enumValue = (Int64Enum)(long)value;
            }
            Assert.Equal(expected, Enum.GetName<Int64Enum>(enumValue));
        }

        [Theory]
        [InlineData(UInt64Enum.Min, "Min")]
        [InlineData(UInt64Enum.One, "One")]
        [InlineData(UInt64Enum.Two, "Two")]
        [InlineData(UInt64Enum.Max, "Max")]
        [InlineData(ulong.MinValue, "Min")]
        [InlineData(1UL, "One")]
        [InlineData(2UL, "Two")]
        [InlineData(ulong.MaxValue, "Max")]
        [InlineData(3UL, null)]
        public void GetName_InvokeUInt64Enum_ReturnsExpected(object value, string expected)
        {
            TestGetName(typeof(UInt64Enum), value, expected);
            if (!(value is UInt64Enum enumValue))
            {
                enumValue = (UInt64Enum)(ulong)value;
            }
            Assert.Equal(expected, Enum.GetName<UInt64Enum>(enumValue));
        }

        public static IEnumerable<object[]> GetName_CharEnum_TestData()
        {
            yield return new object[] { Enum.Parse(s_charEnumType, "Value1"), "Value1" };
            yield return new object[] { Enum.Parse(s_charEnumType, "Value2"), "Value2" };
            yield return new object[] { (char)1, "Value1" };
            yield return new object[] { (char)2, "Value2" };
            yield return new object[] { (char)4, null };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported), nameof(PlatformDetection.IsRareEnumsSupported))]
        [MemberData(nameof(GetName_CharEnum_TestData))]
        public void GetName_InvokeCharEnum_ReturnsExpected(object value, string expected)
        {
            TestGetName(s_charEnumType, value, expected);
        }

        private void TestGetName(Type enumType, object value, string expected)
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
        public void GetName_NullEnumType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("enumType", () => Enum.GetName(null, 1));
        }

        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(int))]
        [InlineData(typeof(ValueType))]
        [InlineData(typeof(Enum))]
        public void GetName_EnumTypeNotEnum_ThrowsArgumentException(Type enumType)
        {
            AssertExtensions.Throws<ArgumentException>("enumType", () => Enum.GetName(enumType, 1));
        }

        [Fact]
        public void GetName_NullValue_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => Enum.GetName(typeof(SimpleEnum), null));
        }

        public static IEnumerable<object[]> GetName_InvalidValue_TestData()
        {
            yield return new object[] { "Red" };
            yield return new object[] { IntPtr.Zero };
        }

        [Theory]
        [MemberData(nameof(GetName_InvalidValue_TestData))]
        public void GetName_InvalidValue_ThrowsArgumentException(object value)
        {
            AssertExtensions.Throws<ArgumentException>("value", () => Enum.GetName(typeof(SimpleEnum), value));
        }

        [Theory]
        [InlineData(typeof(SByteEnum), 0xffffffffffffff80LU, "Min")]
        [InlineData(typeof(SByteEnum), 0xffffff80u, null)]
        [InlineData(typeof(SByteEnum), unchecked((int)(0xffffff80u)), "Min")]
        [InlineData(typeof(SByteEnum), (char)1, "One")]
        [InlineData(typeof(SByteEnum), SimpleEnum.Red, "One")] // API doesn't care if you pass in a completely different enum
        public static void GetName_NonIntegralTypes_ReturnsExpected(Type enumType, object value, string expected)
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

        [Theory]
        [InlineData("One", true)]
        [InlineData("None", false)]
        [InlineData(SByteEnum.One, true)]
        [InlineData((SByteEnum)99, false)]
        [InlineData((sbyte)1, true)]
        [InlineData((sbyte)99, false)]
        public static void IsDefined_InvokeSByteEnum_ReturnsExpected(object value, bool expected)
        {
            Assert.Equal(expected, Enum.IsDefined(typeof(SByteEnum), value));
            if (value is SByteEnum enumValue)
            {
                Assert.Equal(expected, Enum.IsDefined<SByteEnum>(enumValue));
            }
        }

        [Theory]
        [InlineData("One", true)]
        [InlineData("None", false)]
        [InlineData(ByteEnum.One, true)]
        [InlineData((ByteEnum)99, false)]
        [InlineData((byte)1, true)]
        [InlineData((byte)99, false)]
        public static void IsDefined_InvokeByteEnum_ReturnsExpected(object value, bool expected)
        {
            Assert.Equal(expected, Enum.IsDefined(typeof(ByteEnum), value));
            if (value is ByteEnum enumValue)
            {
                Assert.Equal(expected, Enum.IsDefined<ByteEnum>(enumValue));
            }
        }

        [Theory]
        [InlineData("One", true)]
        [InlineData("None", false)]
        [InlineData(Int16Enum.One, true)]
        [InlineData((Int16Enum)99, false)]
        [InlineData((short)1, true)]
        [InlineData((short)99, false)]
        public static void IsDefined_InvokeInt16Enum_ReturnsExpected(object value, bool expected)
        {
            Assert.Equal(expected, Enum.IsDefined(typeof(Int16Enum), value));
            if (value is Int16Enum enumValue)
            {
                Assert.Equal(expected, Enum.IsDefined<Int16Enum>(enumValue));
            }
        }

        [Theory]
        [InlineData("One", true)]
        [InlineData("None", false)]
        [InlineData(UInt16Enum.One, true)]
        [InlineData((UInt16Enum)99, false)]
        [InlineData((ushort)1, true)]
        [InlineData((ushort)99, false)]
        public static void IsDefined_InvokeUInt16Enum_ReturnsExpected(object value, bool expected)
        {
            Assert.Equal(expected, Enum.IsDefined(typeof(UInt16Enum), value));
            if (value is UInt16Enum enumValue)
            {
                Assert.Equal(expected, Enum.IsDefined<UInt16Enum>(enumValue));
            }
        }

        [Theory]
        [InlineData("Red", true)]
        [InlineData("Green", true)]
        [InlineData("Blue", true)]
        [InlineData(" Blue ", false)]
        [InlineData(" blue ", false)]
        [InlineData(SimpleEnum.Red, true)]
        [InlineData((SimpleEnum)99, false)]
        [InlineData(1, true)]
        [InlineData(99, false)]
        public static void IsDefined_InvokeInt32Enum_ReturnsExpected(object value, bool expected)
        {
            Assert.Equal(expected, Enum.IsDefined(typeof(SimpleEnum), value));
            if (value is SimpleEnum enumValue)
            {
                Assert.Equal(expected, Enum.IsDefined<SimpleEnum>(enumValue));
            }
        }

        [Theory]
        [InlineData("One", true)]
        [InlineData("None", false)]
        [InlineData(UInt32Enum.One, true)]
        [InlineData((UInt32Enum)99, false)]
        [InlineData((uint)1, true)]
        [InlineData((uint)99, false)]
        public static void IsDefined_InvokeUInt32Enum_ReturnsExpected(object value, bool expected)
        {
            Assert.Equal(expected, Enum.IsDefined(typeof(UInt32Enum), value));
            if (value is UInt32Enum enumValue)
            {
                Assert.Equal(expected, Enum.IsDefined<UInt32Enum>(enumValue));
            }
        }

        [Theory]
        [InlineData("One", true)]
        [InlineData("None", false)]
        [InlineData(Int64Enum.One, true)]
        [InlineData((Int64Enum)99, false)]
        [InlineData((long)1, true)]
        [InlineData((long)99, false)]
        public static void IsDefined_InvokeInt64Enum_ReturnsExpected(object value, bool expected)
        {
            Assert.Equal(expected, Enum.IsDefined(typeof(Int64Enum), value));
            if (value is Int64Enum enumValue)
            {
                Assert.Equal(expected, Enum.IsDefined<Int64Enum>(enumValue));
            }
        }

        [Theory]
        [InlineData("One", true)]
        [InlineData("None", false)]
        [InlineData(UInt64Enum.One, true)]
        [InlineData((UInt64Enum)99, false)]
        [InlineData((ulong)1, true)]
        [InlineData((ulong)99, false)]
        public static void IsDefined_InvokeUInt64Enum_ReturnsExpected(object value, bool expected)
        {
            Assert.Equal(expected, Enum.IsDefined(typeof(UInt64Enum), value));
            if (value is UInt64Enum enumValue)
            {
                Assert.Equal(expected, Enum.IsDefined<UInt64Enum>(enumValue));
            }
        }

        public static IEnumerable<object[]> IsDefined_CharEnum_TestData()
        {
            yield return new object[] { "Value1", true };
            yield return new object[] { "None", false };
            yield return new object[] { Enum.Parse(s_charEnumType, "Value1"), true };
            yield return new object[] { (char)1, true };
            yield return new object[] { (char)99, false };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported), nameof(PlatformDetection.IsRareEnumsSupported))]
        [MemberData(nameof(IsDefined_CharEnum_TestData))]
        public void IsDefined_InvokeCharEnum_ReturnsExpected(object value, bool expected)
        {
            Assert.Equal(expected, Enum.IsDefined(s_charEnumType, value));
        }

        [Fact]
        public void IsDefined_NullEnumType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("enumType", () => Enum.IsDefined(null, 1));
        }

        [Fact]
        public void IsDefined_NullValue_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => Enum.IsDefined(typeof(SimpleEnum), null));
        }

        [Theory]
        [InlineData(Int32Enum.One)]
        [InlineData('a')]
        public void IsDefined_InvalidValue_ThrowsArgumentException(object value)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Enum.IsDefined(typeof(SimpleEnum), value));
        }

        public static IEnumerable<object[]> IsDefined_NonIntegerValue_TestData()
        {
            yield return new object[] { IntPtr.Zero };
            yield return new object[] { 5.5 };
            yield return new object[] { 5.5f };
        }

        [Theory]
        [MemberData(nameof(IsDefined_NonIntegerValue_TestData))]
        public void IsDefined_NonIntegerValue_ThrowsThrowsInvalidOperationException(object value)
        {
            Assert.Throws<InvalidOperationException>(() => Enum.IsDefined(typeof(SimpleEnum), value));
        }

        [Fact]
        public void IsDefined_LargeEnum_AllValuesFound()
        {
            for (int i = 0; i < 256; i++)
            {
                Assert.True(Enum.IsDefined(typeof(CompleteSByteEnum), (CompleteSByteEnum)i));
                Assert.True(Enum.IsDefined((CompleteSByteEnum)i));

                Assert.True(Enum.IsDefined(typeof(CompleteSByteRandomOrderEnum), (CompleteSByteRandomOrderEnum)i));
                Assert.True(Enum.IsDefined((CompleteSByteRandomOrderEnum)i));
            }
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

            if (PlatformDetection.IsReflectionEmitSupported && PlatformDetection.IsRareEnumsSupported)
            {
                // Char
                yield return new object[] { Enum.Parse(s_charEnumType, "Value0x3f06"), Enum.Parse(s_charEnumType, "Value0x3000"), true };
                yield return new object[] { Enum.Parse(s_charEnumType, "Value0x3f06"), Enum.Parse(s_charEnumType, "Value0x0f06"), true };
                yield return new object[] { Enum.Parse(s_charEnumType, "Value0x3f06"), Enum.Parse(s_charEnumType, "Value0x1000"), true };
                yield return new object[] { Enum.Parse(s_charEnumType, "Value0x3f06"), Enum.Parse(s_charEnumType, "Value0x0000"), true };
                yield return new object[] { Enum.Parse(s_charEnumType, "Value0x3f06"), Enum.Parse(s_charEnumType, "Value0x3f06"), true };
                yield return new object[] { Enum.Parse(s_charEnumType, "Value0x3f06"), Enum.Parse(s_charEnumType, "Value0x0010"), false };
                yield return new object[] { Enum.Parse(s_charEnumType, "Value0x3f06"), Enum.Parse(s_charEnumType, "Value0x3f16"), false };

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
            }
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

            if (PlatformDetection.IsReflectionEmitSupported && PlatformDetection.IsRareEnumsSupported)
            {
                // Char
                yield return new object[] { s_charEnumType, (char)1, Enum.Parse(s_charEnumType, "Value1") };
                yield return new object[] { s_charEnumType, (char)2, Enum.Parse(s_charEnumType, "Value2") };

                // Float
                yield return new object[] { s_floatEnumType, 1.0f, Enum.Parse(s_floatEnumType, "Value1") };
                yield return new object[] { s_floatEnumType, 2.0f, Enum.Parse(s_floatEnumType, "Value2") };

                // Double
                yield return new object[] { s_doubleEnumType, 1.0, Enum.Parse(s_doubleEnumType, "Value1") };
                yield return new object[] { s_doubleEnumType, 2.0, Enum.Parse(s_doubleEnumType, "Value2") };
            }
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

            if (PlatformDetection.IsReflectionEmitSupported && PlatformDetection.IsRareEnumsSupported)
                yield return new object[] { GetNonRuntimeEnumTypeBuilder(typeof(int)), typeof(ArgumentException) };
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

            if (PlatformDetection.IsReflectionEmitSupported && PlatformDetection.IsRareEnumsSupported)
            {
                yield return new object[] { s_intPtrEnumType, (IntPtr)1, typeof(ArgumentException) };
                yield return new object[] { s_uintPtrEnumType, (UIntPtr)1, typeof(ArgumentException) };
            }
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

            if (PlatformDetection.IsReflectionEmitSupported && PlatformDetection.IsRareEnumsSupported)
            {
                // Char
                yield return new object[] { Enum.Parse(s_charEnumType, "Value1"), Enum.Parse(s_charEnumType, "Value1"), true };
                yield return new object[] { Enum.Parse(s_charEnumType, "Value1"), Enum.Parse(s_charEnumType, "Value2"), false };
                yield return new object[] { Enum.Parse(s_charEnumType, "Value1"), UInt16Enum.One, false };
                yield return new object[] { Enum.Parse(s_charEnumType, "Value1"), (char)1, false };
                yield return new object[] { Enum.Parse(s_charEnumType, "Value1"), new object(), false };
                yield return new object[] { Enum.Parse(s_charEnumType, "Value1"), null, false };

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
            }
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public static void EqualsTest(Enum e, object obj, bool expected)
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

            if (PlatformDetection.IsReflectionEmitSupported && PlatformDetection.IsRareEnumsSupported)
            {
                // Char
                yield return new object[] { Enum.Parse(s_charEnumType, "Value2"), Enum.Parse(s_charEnumType, "Value2"), 0 };
                yield return new object[] { Enum.Parse(s_charEnumType, "Value2"), Enum.Parse(s_charEnumType, "Value1"), 1 };
                yield return new object[] { Enum.Parse(s_charEnumType, "Value1"), Enum.Parse(s_charEnumType, "Value2"), -1 };
                yield return new object[] { Enum.Parse(s_charEnumType, "Value2"), null, 1 };

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
            }
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

            if (PlatformDetection.IsReflectionEmitSupported && PlatformDetection.IsRareEnumsSupported)
            {
                yield return new object[] { s_charEnumType, typeof(char) };
                yield return new object[] { s_boolEnumType, typeof(bool) };
                yield return new object[] { s_floatEnumType, typeof(float) };
                yield return new object[] { s_doubleEnumType, typeof(double) };
                yield return new object[] { s_intPtrEnumType, typeof(IntPtr) };
                yield return new object[] { s_uintPtrEnumType, typeof(UIntPtr) };
            }
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

        [Fact]
        public void GetNames_InvokeSimpleEnum_ReturnsExpected()
        {
            var expected = new string[] { "Red", "Blue", "Green", "Green_a", "Green_b", "B" };
            Assert.Equal(expected, Enum.GetNames(typeof(SimpleEnum)));
            Assert.NotSame(Enum.GetNames(typeof(SimpleEnum)), Enum.GetNames(typeof(SimpleEnum)));
            Assert.Equal(expected, Enum.GetNames<SimpleEnum>());
        }

        [Fact]
        public void GetNames_InvokeSByteEnum_ReturnsExpected()
        {
            var expected = new string[] { "One", "Two", "Max", "Min" };
            Assert.Equal(expected, Enum.GetNames(typeof(SByteEnum)));
            Assert.NotSame(Enum.GetNames(typeof(SByteEnum)), Enum.GetNames(typeof(SByteEnum)));
            Assert.Equal(expected, Enum.GetNames<SByteEnum>());
        }

        [Fact]
        public void GetNames_InvokeByteEnum_ReturnsExpected()
        {
            var expected = new string[] { "Min", "One", "Two", "Max" };
            Assert.Equal(expected, Enum.GetNames(typeof(ByteEnum)));
            Assert.NotSame(Enum.GetNames(typeof(ByteEnum)), Enum.GetNames(typeof(ByteEnum)));
            Assert.Equal(expected, Enum.GetNames<ByteEnum>());
        }

        [Fact]
        public void GetNames_InvokeInt16Enum_ReturnsExpected()
        {
            var expected = new string[] { "One", "Two", "Max", "Min" };
            Assert.Equal(expected, Enum.GetNames(typeof(Int16Enum)));
            Assert.NotSame(Enum.GetNames(typeof(Int16Enum)), Enum.GetNames(typeof(Int16Enum)));
            Assert.Equal(expected, Enum.GetNames<Int16Enum>());
        }

        [Fact]
        public void GetNames_InvokeUInt16Enum_ReturnsExpected()
        {
            var expected = new string[] { "Min", "One", "Two", "Max" };
            Assert.Equal(expected, Enum.GetNames(typeof(UInt16Enum)));
            Assert.NotSame(Enum.GetNames(typeof(UInt16Enum)), Enum.GetNames(typeof(UInt16Enum)));
            Assert.Equal(expected, Enum.GetNames<UInt16Enum>());
        }

        [Fact]
        public void GetNames_InvokeInt32Enum_ReturnsExpected()
        {
            var expected = new string[] { "One", "Two", "Max", "Min" };
            Assert.Equal(expected, Enum.GetNames(typeof(Int32Enum)));
            Assert.NotSame(Enum.GetNames(typeof(Int32Enum)), Enum.GetNames(typeof(Int32Enum)));
            Assert.Equal(expected, Enum.GetNames<Int32Enum>());
        }

        [Fact]
        public void GetNames_InvokeUInt32Enum_ReturnsExpected()
        {
            var expected = new string[] { "Min", "One", "Two", "Max" };
            Assert.Equal(expected, Enum.GetNames(typeof(UInt32Enum)));
            Assert.NotSame(Enum.GetNames(typeof(UInt32Enum)), Enum.GetNames(typeof(UInt32Enum)));
            Assert.Equal(expected, Enum.GetNames<UInt32Enum>());
        }

        [Fact]
        public void GetNames_InvokeInt64Enum_ReturnsExpected()
        {
            var expected = new string[] { "One", "Two", "Max", "Min" };
            Assert.Equal(expected, Enum.GetNames(typeof(Int64Enum)));
            Assert.NotSame(Enum.GetNames(typeof(Int64Enum)), Enum.GetNames(typeof(Int64Enum)));
            Assert.Equal(expected, Enum.GetNames<Int64Enum>());
        }

        [Fact]
        public void GetNames_InvokeUInt64Enum_ReturnsExpected()
        {
            var expected = new string[] { "Min", "One", "Two", "Max" };
            Assert.Equal(expected, Enum.GetNames(typeof(UInt64Enum)));
            Assert.NotSame(Enum.GetNames(typeof(UInt64Enum)), Enum.GetNames(typeof(UInt64Enum)));
            Assert.Equal(expected, Enum.GetNames<UInt64Enum>());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported), nameof(PlatformDetection.IsRareEnumsSupported))]
        public void GetNames_InvokeCharEnum_ReturnsExpected()
        {
            var expected = new string[] { "Value0x0000", "Value1", "Value2", "Value0x0010", "Value0x0f06", "Value0x1000", "Value0x3000", "Value0x3f06", "Value0x3f16" };
            Assert.Equal(expected, Enum.GetNames(s_charEnumType));
            Assert.NotSame(Enum.GetNames(s_charEnumType), Enum.GetNames(s_charEnumType));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported), nameof(PlatformDetection.IsRareEnumsSupported))]
        public void GetNames_InvokeSingleEnum_ReturnsExpected()
        {
            var expected = new string[] { "Value0x0000", "Value1", "Value2", "Value0x0010", "Value0x0f06", "Value0x1000", "Value0x3000", "Value0x3f06", "Value0x3f16" };
            Assert.Equal(expected, Enum.GetNames(s_floatEnumType));
            Assert.NotSame(Enum.GetNames(s_floatEnumType), Enum.GetNames(s_floatEnumType));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported), nameof(PlatformDetection.IsRareEnumsSupported))]
        public void GetNames_InvokeDoubleEnum_ReturnsExpected()
        {
            var expected = new string[] { "Value0x0000", "Value1", "Value2", "Value0x0010", "Value0x0f06", "Value0x1000", "Value0x3000", "Value0x3f06", "Value0x3f16" };
            Assert.Equal(expected, Enum.GetNames(s_doubleEnumType));
            Assert.NotSame(Enum.GetNames(s_doubleEnumType), Enum.GetNames(s_doubleEnumType));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported), nameof(PlatformDetection.IsRareEnumsSupported))]
        public void GetNames_InvokeIntPtrEnum_ReturnsExpected()
        {
            var expected = new string[0];
            Assert.Equal(expected, Enum.GetNames(s_intPtrEnumType));
            Assert.Same(Enum.GetNames(s_intPtrEnumType), Enum.GetNames(s_intPtrEnumType));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported), nameof(PlatformDetection.IsRareEnumsSupported))]
        public void GetNames_InvokeUIntPtrEnum_ReturnsExpected()
        {
            var expected = new string[0];
            Assert.Equal(expected, Enum.GetNames(s_uintPtrEnumType));
            Assert.Same(Enum.GetNames(s_uintPtrEnumType), Enum.GetNames(s_uintPtrEnumType));
        }

        [Fact]
        public static void GetNames_NullEnumType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("enumType", () => Enum.GetNames(null));
        }

        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(int))]
        [InlineData(typeof(ValueType))]
        [InlineData(typeof(Enum))]
        public void GetNames_EnumTypeNotEnum_ThrowsArgumentException(Type enumType)
        {
            AssertExtensions.Throws<ArgumentException>("enumType", () => Enum.GetNames(enumType));
        }

        [Fact]
        public void GetValues_InvokeSimpleEnumEnum_ReturnsExpected()
        {
            var expected = new SimpleEnum[] { SimpleEnum.Red, SimpleEnum.Blue, SimpleEnum.Green, SimpleEnum.Green_a, SimpleEnum.Green_b, SimpleEnum.B };
            Assert.Equal(expected, Enum.GetValues(typeof(SimpleEnum)));
            Assert.NotSame(Enum.GetValues(typeof(SimpleEnum)), Enum.GetValues(typeof(SimpleEnum)));
            Assert.Equal(expected, Enum.GetValues<SimpleEnum>());
        }

        [Fact]
        public void GetValues_InvokeSByteEnum_ReturnsExpected()
        {
            var expected = new SByteEnum[] { SByteEnum.One, SByteEnum.Two, SByteEnum.Max, SByteEnum.Min };
            Assert.Equal(expected, Enum.GetValues(typeof(SByteEnum)));
            Assert.NotSame(Enum.GetValues(typeof(SByteEnum)), Enum.GetValues(typeof(SByteEnum)));
            Assert.Equal(expected, Enum.GetValues<SByteEnum>());
        }

        [Fact]
        public void GetValues_InvokeByteEnum_ReturnsExpected()
        {
            var expected = new ByteEnum[] { ByteEnum.Min, ByteEnum.One, ByteEnum.Two, ByteEnum.Max };
            Assert.Equal(expected, Enum.GetValues(typeof(ByteEnum)));
            Assert.NotSame(Enum.GetValues(typeof(ByteEnum)), Enum.GetValues(typeof(ByteEnum)));
            Assert.Equal(expected, Enum.GetValues<ByteEnum>());
        }

        [Fact]
        public void GetValues_InvokeInt16Enum_ReturnsExpected()
        {
            var expected = new Int16Enum[] { Int16Enum.One, Int16Enum.Two, Int16Enum.Max, Int16Enum.Min };
            Assert.Equal(expected, Enum.GetValues(typeof(Int16Enum)));
            Assert.NotSame(Enum.GetValues(typeof(Int16Enum)), Enum.GetValues(typeof(Int16Enum)));
            Assert.Equal(expected, Enum.GetValues<Int16Enum>());
        }

        [Fact]
        public void GetValues_InvokeUInt16Enum_ReturnsExpected()
        {
            var expected = new UInt16Enum[] { UInt16Enum.Min, UInt16Enum.One, UInt16Enum.Two, UInt16Enum.Max };
            Assert.Equal(expected, Enum.GetValues(typeof(UInt16Enum)));
            Assert.NotSame(Enum.GetValues(typeof(UInt16Enum)), Enum.GetValues(typeof(UInt16Enum)));
            Assert.Equal(expected, Enum.GetValues<UInt16Enum>());
        }

        [Fact]
        public void GetValues_InvokeInt32Enum_ReturnsExpected()
        {
            var expected = new Int32Enum[] { Int32Enum.One, Int32Enum.Two, Int32Enum.Max, Int32Enum.Min };
            Assert.Equal(expected, Enum.GetValues(typeof(Int32Enum)));
            Assert.NotSame(Enum.GetValues(typeof(Int32Enum)), Enum.GetValues(typeof(Int32Enum)));
            Assert.Equal(expected, Enum.GetValues<Int32Enum>());
        }

        [Fact]
        public void GetValues_InvokeUInt32Enum_ReturnsExpected()
        {
            var expected = new UInt32Enum[] { UInt32Enum.Min, UInt32Enum.One, UInt32Enum.Two, UInt32Enum.Max };
            Assert.Equal(expected, Enum.GetValues(typeof(UInt32Enum)));
            Assert.NotSame(Enum.GetValues(typeof(UInt32Enum)), Enum.GetValues(typeof(UInt32Enum)));
            Assert.Equal(expected, Enum.GetValues<UInt32Enum>());
        }

        [Fact]
        public void GetValues_InvokeInt64Enum_ReturnsExpected()
        {
            var expected = new Int64Enum[] { Int64Enum.One, Int64Enum.Two, Int64Enum.Max, Int64Enum.Min };
            Assert.Equal(expected, Enum.GetValues(typeof(Int64Enum)));
            Assert.NotSame(Enum.GetValues(typeof(Int64Enum)), Enum.GetValues(typeof(Int64Enum)));
            Assert.Equal(expected, Enum.GetValues<Int64Enum>());
        }

        [Fact]
        public void GetValues_InvokeUInt64Enum_ReturnsExpected()
        {
            var expected = new UInt64Enum[] { UInt64Enum.Min, UInt64Enum.One, UInt64Enum.Two, UInt64Enum.Max };
            Assert.Equal(expected, Enum.GetValues(typeof(UInt64Enum)));
            Assert.NotSame(Enum.GetValues(typeof(UInt64Enum)), Enum.GetValues(typeof(UInt64Enum)));
            Assert.Equal(expected, Enum.GetValues<UInt64Enum>());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported), nameof(PlatformDetection.IsRareEnumsSupported))]
        public void GetValues_InvokeCharEnum_ReturnsExpected()
        {
            var expected = new object[] { Enum.Parse(s_charEnumType, "Value0x0000"), Enum.Parse(s_charEnumType, "Value1"), Enum.Parse(s_charEnumType, "Value2"), Enum.Parse(s_charEnumType, "Value0x0010"), Enum.Parse(s_charEnumType, "Value0x0f06"), Enum.Parse(s_charEnumType, "Value0x1000"), Enum.Parse(s_charEnumType, "Value0x3000"), Enum.Parse(s_charEnumType, "Value0x3f06"), Enum.Parse(s_charEnumType, "Value0x3f16") };
            Assert.Equal(expected, Enum.GetValues(s_charEnumType));
            Assert.NotSame(Enum.GetValues(s_charEnumType), Enum.GetValues(s_charEnumType));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported), nameof(PlatformDetection.IsRareEnumsSupported))]
        public void GetValues_InvokeSingleEnum_ReturnsExpected()
        {
            var expected = new object[] { Enum.Parse(s_floatEnumType, "Value0x0000"), Enum.Parse(s_floatEnumType, "Value1"), Enum.Parse(s_floatEnumType, "Value2"), Enum.Parse(s_floatEnumType, "Value0x0010"), Enum.Parse(s_floatEnumType, "Value0x0f06"), Enum.Parse(s_floatEnumType, "Value0x1000"), Enum.Parse(s_floatEnumType, "Value0x3000"), Enum.Parse(s_floatEnumType, "Value0x3f06"), Enum.Parse(s_floatEnumType, "Value0x3f16") };
            Assert.Equal(expected, Enum.GetValues(s_floatEnumType));
            Assert.NotSame(Enum.GetValues(s_floatEnumType), Enum.GetValues(s_floatEnumType));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported), nameof(PlatformDetection.IsRareEnumsSupported))]
        public void GetValues_InvokeDoubleEnum_ReturnsExpected()
        {
            var expected = new object[] { Enum.Parse(s_doubleEnumType, "Value0x0000"), Enum.Parse(s_doubleEnumType, "Value1"), Enum.Parse(s_doubleEnumType, "Value2"), Enum.Parse(s_doubleEnumType, "Value0x0010"), Enum.Parse(s_doubleEnumType, "Value0x0f06"), Enum.Parse(s_doubleEnumType, "Value0x1000"), Enum.Parse(s_doubleEnumType, "Value0x3000"), Enum.Parse(s_doubleEnumType, "Value0x3f06"), Enum.Parse(s_doubleEnumType, "Value0x3f16") };
            Assert.Equal(expected, Enum.GetValues(s_doubleEnumType));
            Assert.NotSame(Enum.GetValues(s_doubleEnumType), Enum.GetValues(s_doubleEnumType));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported), nameof(PlatformDetection.IsRareEnumsSupported))]
        public void GetValues_InvokeIntPtrEnum_ReturnsExpected()
        {
            var expected = new object[0];
            Assert.Equal(expected, Enum.GetValues(s_intPtrEnumType));
            Assert.NotSame(Enum.GetValues(s_intPtrEnumType), Enum.GetValues(s_intPtrEnumType));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported), nameof(PlatformDetection.IsRareEnumsSupported))]
        public void GetValues_InvokeUIntPtrEnum_ReturnsExpected()
        {
            var expected = new object[0];
            Assert.Equal(expected, Enum.GetValues(s_uintPtrEnumType));
            Assert.NotSame(Enum.GetValues(s_uintPtrEnumType), Enum.GetValues(s_uintPtrEnumType));
        }

        [Fact]
        public static void GetValues_NullEnumType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("enumType", () => Enum.GetValues(null));
        }

        [Fact]
        public void GetValuesAsUnderlyingType_InvokeSByteEnum_ReturnsExpected()
        {
            Array expected = new sbyte[] { 1, 2, sbyte.MaxValue, sbyte.MinValue};
            Assert.Equal(expected, Enum.GetValuesAsUnderlyingType(typeof(SByteEnum)));
            Assert.Equal(expected, Enum.GetValuesAsUnderlyingType<SByteEnum>());
        }

        [Fact]
        public void GetValuesAsUnderlyingType_InvokeByteEnum_ReturnsExpected()
        {
            Array expected = new byte[] { byte.MinValue, 1, 2, byte.MaxValue };
            Assert.Equal(expected, Enum.GetValuesAsUnderlyingType(typeof(ByteEnum)));
            Assert.Equal(expected, Enum.GetValuesAsUnderlyingType<ByteEnum>());
        }

        [Fact]
        public void GetValuesAsUnderlyingType_InvokeInt16Enum_ReturnsExpected()
        {
            Array expected = new short[] { 1, 2, short.MaxValue, short.MinValue };
            Assert.Equal(expected, Enum.GetValuesAsUnderlyingType(typeof(Int16Enum)));
            Assert.Equal(expected, Enum.GetValuesAsUnderlyingType<Int16Enum>());
        }

        [Fact]
        public void GetValuesAsUnderlyingType_InvokeUInt16Enum_ReturnsExpected()
        {
            Array expected = new ushort[] { ushort.MinValue, 1, 2, ushort.MaxValue };
            Assert.Equal(expected, Enum.GetValuesAsUnderlyingType(typeof(UInt16Enum)));
            Assert.Equal(expected, Enum.GetValuesAsUnderlyingType<UInt16Enum>());
        }

        [Fact]
        public void GetValuesAsUnderlyingType_InvokeInt32Enum_ReturnsExpected()
        {
            Array expected = new int[] { 1, 2, int.MaxValue, int.MinValue };
            Assert.Equal(expected, Enum.GetValuesAsUnderlyingType(typeof(Int32Enum)));
            Assert.Equal(expected, Enum.GetValuesAsUnderlyingType<Int32Enum>());
        }

        [Fact]
        public void GetValuesAsUnderlyingType_InvokeUInt32Enum_ReturnsExpected()
        {
            Array expected = new uint[] { uint.MinValue, 1, 2, uint.MaxValue };
            Assert.Equal(expected, Enum.GetValuesAsUnderlyingType(typeof(UInt32Enum)));
            Assert.Equal(expected, Enum.GetValuesAsUnderlyingType<UInt32Enum>());
        }

        [Fact]
        public void GetValuesAsUnderlyingType_InvokeInt64Enum_ReturnsExpected()
        {
            Array expected = new long[] { 1, 2, long.MaxValue, long.MinValue };
            Assert.Equal(expected, Enum.GetValuesAsUnderlyingType(typeof(Int64Enum)));
            Assert.Equal(expected, Enum.GetValuesAsUnderlyingType<Int64Enum>());
        }

        [Fact]
        public void GetValuesAsUnderlyingType_InvokeUInt64Enum_ReturnsExpected()
        {
            Array expected = new ulong[] { ulong.MinValue, 1, 2, ulong.MaxValue };
            Assert.Equal(expected, Enum.GetValuesAsUnderlyingType(typeof(UInt64Enum)));
            Assert.Equal(expected, Enum.GetValuesAsUnderlyingType<UInt64Enum>());
        }

        [Fact]
        public static void GetValuesAsUnderlyingType_NullEnumType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("enumType", () => Enum.GetValuesAsUnderlyingType(null));
        }

        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(int))]
        [InlineData(typeof(ValueType))]
        [InlineData(typeof(Enum))]
        public void GetValues_EnumTypeNotEnum_ThrowsArgumentException(Type enumType)
        {
            AssertExtensions.Throws<ArgumentException>("enumType", () => Enum.GetValues(enumType));
        }

        private class ClassWithEnumConstraint<T> where T : Enum { }

        [Fact]
        public void EnumConstraint_ThrowsArgumentException()
        {
            Type genericArgumentWithEnumConstraint = typeof(ClassWithEnumConstraint<>).GetGenericArguments()[0];
            Assert.True(genericArgumentWithEnumConstraint.IsEnum);

            Assert.Throws<ArgumentException>(() => Enum.GetUnderlyingType(genericArgumentWithEnumConstraint));
            Assert.Throws<ArgumentException>(() => Enum.IsDefined(genericArgumentWithEnumConstraint, 1));
            Assert.Throws<ArgumentException>(() => Enum.GetName(genericArgumentWithEnumConstraint, 1));
            Assert.Throws<ArgumentException>(() => Enum.GetNames(genericArgumentWithEnumConstraint));
            Assert.Throws<ArgumentException>(() => Enum.GetValues(genericArgumentWithEnumConstraint));
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

            if (PlatformDetection.IsReflectionEmitSupported && PlatformDetection.IsRareEnumsSupported)
            {
                // "D": Char
                yield return new object[] { Enum.ToObject(s_charEnumType, (char)0), "D", ((char)0).ToString() };
                yield return new object[] { Enum.ToObject(s_charEnumType, (char)1), "D", ((char)1).ToString() };
                yield return new object[] { Enum.ToObject(s_charEnumType, (char)2), "D", ((char)2).ToString() };
                yield return new object[] { Enum.ToObject(s_charEnumType, char.MaxValue), "D", char.MaxValue.ToString() };

                // "D": Single
                yield return new object[] { Enum.ToObject(s_floatEnumType, 0), "D", "0" };
                yield return new object[] { Enum.ToObject(s_floatEnumType, 1), "D", float.Epsilon.ToString() };
                yield return new object[] { Enum.ToObject(s_floatEnumType, int.MaxValue), "D", float.NaN.ToString() };

                // "D": Double
                yield return new object[] { Enum.ToObject(s_doubleEnumType, 0), "D", "0" };
                yield return new object[] { Enum.ToObject(s_doubleEnumType, 1), "D", double.Epsilon.ToString() };
                yield return new object[] { Enum.ToObject(s_doubleEnumType, long.MaxValue), "D", double.NaN.ToString() };
            }

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

            if (PlatformDetection.IsReflectionEmitSupported && PlatformDetection.IsRareEnumsSupported)
            {
                // "X": Char
                yield return new object[] { Enum.ToObject(s_charEnumType, (char)0), "X", "0000" };
                yield return new object[] { Enum.ToObject(s_charEnumType, (char)1), "X", "0001" };
                yield return new object[] { Enum.ToObject(s_charEnumType, (char)2), "X", "0002" };
                yield return new object[] { Enum.ToObject(s_charEnumType, char.MaxValue), "X", "FFFF" };
            }

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

            if (PlatformDetection.IsReflectionEmitSupported && PlatformDetection.IsRareEnumsSupported)
            {
                // "F": Char
                yield return new object[] { Enum.ToObject(s_charEnumType, (char)1), "F", "Value1" };
                yield return new object[] { Enum.ToObject(s_charEnumType, (char)(1 | 2)), "F", "Value1, Value2" };
                yield return new object[] { Enum.ToObject(s_charEnumType, (char)5), "F", ((char)5).ToString() };
                yield return new object[] { Enum.ToObject(s_charEnumType, char.MaxValue), "F", char.MaxValue.ToString() };

                // "F": IntPtr
                yield return new object[] { Enum.ToObject(s_intPtrEnumType, 5), "F", "5" };

                // "F": UIntPtr
                yield return new object[] { Enum.ToObject(s_uintPtrEnumType, 5), "F", "5" };
            }

            // "F": SimpleEnum
            yield return new object[] { SimpleEnum.Red, "F", "Red" };
            yield return new object[] { SimpleEnum.Blue, "F", "Blue" };
            yield return new object[] { (SimpleEnum)99, "F", "99" };
            yield return new object[] { (SimpleEnum)0, "F", "0" }; // Not found

            // "F": Flags Attribute
            yield return new object[] { AttributeTargets.Class | AttributeTargets.Delegate, "F", "Class, Delegate" };

            if (PlatformDetection.IsReflectionEmitSupported && PlatformDetection.IsRareEnumsSupported)
            {
                // "G": Char
                yield return new object[] { Enum.ToObject(s_charEnumType, char.MaxValue), "G", char.MaxValue.ToString() };
            }

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

            yield return new object[] { FlagsSByteEnumWithNegativeValues.A, "G", "A" };
            yield return new object[] { FlagsSByteEnumWithNegativeValues.C, "G", "C" };
            yield return new object[] { FlagsSByteEnumWithNegativeValues.I, "G", "I" };
            yield return new object[] { FlagsSByteEnumWithNegativeValues.C | FlagsSByteEnumWithNegativeValues.D, "G", "C, D" };
            yield return new object[] { FlagsSByteEnumWithNegativeValues.A | FlagsSByteEnumWithNegativeValues.C | FlagsSByteEnumWithNegativeValues.D, "G", "C, D" };

            yield return new object[] { FlagsInt32EnumWithOverlappingNegativeValues.A, "G", "A" };
            yield return new object[] { FlagsInt32EnumWithOverlappingNegativeValues.B, "G", "B" };
            yield return new object[] { FlagsInt32EnumWithOverlappingNegativeValues.C, "G", "C" };
            yield return new object[] { FlagsInt32EnumWithOverlappingNegativeValues.A | FlagsInt32EnumWithOverlappingNegativeValues.B, "G", "B, A" };
            yield return new object[] { (FlagsInt32EnumWithOverlappingNegativeValues)(-1), "G", "B, C" };
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public static void ToString_TryFormat(bool validateDestinationSpanSizeCheck, bool validateExtraSpanSpaceNotFilled)
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
            TryFormat(SByteEnum.Min, "D", "-128", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(SByteEnum.One, "D", "1", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(SByteEnum.Two, "D", "2", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat((SByteEnum)99, "D", "99", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(SByteEnum.Max, "D", "127", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            // "D": Byte
            TryFormat(ByteEnum.Min, "D", "0", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(ByteEnum.One, "D", "1", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(ByteEnum.Two, "D", "2", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat((ByteEnum)99, "D", "99", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(ByteEnum.Max, "D", "255", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            // "D": Int16
            TryFormat(Int16Enum.Min, "D", "-32768", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(Int16Enum.One, "D", "1", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(Int16Enum.Two, "D", "2", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat((Int16Enum)99, "D", "99", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(Int16Enum.Max, "D", "32767", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            // "D": UInt16
            TryFormat(UInt16Enum.Min, "D", "0", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(UInt16Enum.One, "D", "1", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(UInt16Enum.Two, "D", "2", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat((UInt16Enum)99, "D", "99", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(UInt16Enum.Max, "D", "65535", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            // "D": Int32
            TryFormat(Int32Enum.Min, "D", "-2147483648", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(Int32Enum.One, "D", "1", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(Int32Enum.Two, "D", "2", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat((Int32Enum)99, "D", "99", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(Int32Enum.Max, "D", "2147483647", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            // "D": UInt32
            TryFormat(UInt32Enum.Min, "D", "0", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(UInt32Enum.One, "D", "1", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(UInt32Enum.Two, "D", "2", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat((UInt32Enum)99, "D", "99", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(UInt32Enum.Max, "D", "4294967295", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            // "D": Int64
            TryFormat(Int64Enum.Min, "D", "-9223372036854775808", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(Int64Enum.One, "D", "1", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(Int64Enum.Two, "D", "2", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat((Int64Enum)99, "D", "99", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(Int64Enum.Max, "D", "9223372036854775807", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            // "D": UInt64
            TryFormat(UInt64Enum.Min, "D", "0", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(UInt64Enum.One, "D", "1", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(UInt64Enum.Two, "D", "2", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat((UInt64Enum)99, "D", "99", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(UInt64Enum.Max, "D", "18446744073709551615", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            // "D": SimpleEnum
            TryFormat(SimpleEnum.Red, "D", "1", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            // "X": SByte
            TryFormat(SByteEnum.Min, "X", "80", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(SByteEnum.One, "X", "01", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(SByteEnum.Two, "X", "02", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat((SByteEnum)99, "X", "63", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(SByteEnum.Max, "X", "7F", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            // "X": Byte
            TryFormat(ByteEnum.Min, "X", "00", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(ByteEnum.One, "X", "01", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(ByteEnum.Two, "X", "02", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat((ByteEnum)99, "X", "63", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(ByteEnum.Max, "X", "FF", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            // "X": Int16
            TryFormat(Int16Enum.Min, "X", "8000", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(Int16Enum.One, "X", "0001", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(Int16Enum.Two, "X", "0002", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat((Int16Enum)99, "X", "0063", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(Int16Enum.Max, "X", "7FFF", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            // "X": UInt16
            TryFormat(UInt16Enum.Min, "X", "0000", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(UInt16Enum.One, "X", "0001", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(UInt16Enum.Two, "X", "0002", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat((UInt16Enum)99, "X", "0063", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(UInt16Enum.Max, "X", "FFFF", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            // "X": UInt32
            TryFormat(UInt32Enum.Min, "X", "00000000", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(UInt32Enum.One, "X", "00000001", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(UInt32Enum.Two, "X", "00000002", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat((UInt32Enum)99, "X", "00000063", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(UInt32Enum.Max, "X", "FFFFFFFF", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            // "X": Int32
            TryFormat(Int32Enum.Min, "X", "80000000", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(Int32Enum.One, "X", "00000001", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(Int32Enum.Two, "X", "00000002", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat((Int32Enum)99, "X", "00000063", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(Int32Enum.Max, "X", "7FFFFFFF", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            // "X:" Int64
            TryFormat(Int64Enum.Min, "X", "8000000000000000", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(Int64Enum.One, "X", "0000000000000001", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(Int64Enum.Two, "X", "0000000000000002", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat((Int64Enum)99, "X", "0000000000000063", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(Int64Enum.Max, "X", "7FFFFFFFFFFFFFFF", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            // "X": UInt64
            TryFormat(UInt64Enum.Min, "X", "0000000000000000", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(UInt64Enum.One, "X", "0000000000000001", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(UInt64Enum.Two, "X", "0000000000000002", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat((UInt64Enum)99, "X", "0000000000000063", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(UInt64Enum.Max, "X", "FFFFFFFFFFFFFFFF", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            // "X": SimpleEnum
            TryFormat(SimpleEnum.Red, "X", "00000001", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            // "F": SByte
            TryFormat(SByteEnum.Min, "F", "Min", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(SByteEnum.One | SByteEnum.Two, "F", "One, Two", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat((SByteEnum)5, "F", "5", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(SByteEnum.Max, "F", "Max", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            // "F": Byte
            TryFormat(ByteEnum.Min, "F", "Min", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(ByteEnum.One | ByteEnum.Two, "F", "One, Two", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat((ByteEnum)5, "F", "5", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(ByteEnum.Max, "F", "Max", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            // "F": Int16
            TryFormat(Int16Enum.Min, "F", "Min", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(Int16Enum.One | Int16Enum.Two, "F", "One, Two", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat((Int16Enum)5, "F", "5", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(Int16Enum.Max, "F", "Max", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            // "F": UInt16
            TryFormat(UInt16Enum.Min, "F", "Min", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(UInt16Enum.One | UInt16Enum.Two, "F", "One, Two", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat((UInt16Enum)5, "F", "5", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(UInt16Enum.Max, "F", "Max", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            // "F": Int32
            TryFormat(Int32Enum.Min, "F", "Min", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(Int32Enum.One | Int32Enum.Two, "F", "One, Two", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat((Int32Enum)5, "F", "5", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(Int32Enum.Max, "F", "Max", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            // "F": UInt32
            TryFormat(UInt32Enum.Min, "F", "Min", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(UInt32Enum.One | UInt32Enum.Two, "F", "One, Two", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat((UInt32Enum)5, "F", "5", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(UInt32Enum.Max, "F", "Max", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            // "F": Int64
            TryFormat(Int64Enum.Min, "F", "Min", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(Int64Enum.One | Int64Enum.Two, "F", "One, Two", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat((Int64Enum)5, "F", "5", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(Int64Enum.Max, "F", "Max", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            // "F": UInt64
            TryFormat(UInt64Enum.Min, "F", "Min", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(UInt64Enum.One | UInt64Enum.Two, "F", "One, Two", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat((UInt64Enum)5, "F", "5", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(UInt64Enum.Max, "F", "Max", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            // "F": SimpleEnum
            TryFormat(SimpleEnum.Red, "F", "Red", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat(SimpleEnum.Blue, "F", "Blue", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat((SimpleEnum)99, "F", "99", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            TryFormat((SimpleEnum)0, "F", "0", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled); // Not found

            // "F": Flags Attribute
            TryFormat(AttributeTargets.Class | AttributeTargets.Delegate, "F", "Class, Delegate", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

            foreach (string defaultFormatSpecifier in new[] { "G", null, "" })
            {
                // "G": SByte
                TryFormat(SByteEnum.Min, defaultFormatSpecifier, "Min", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
                TryFormat((SByteEnum)3, defaultFormatSpecifier, "3", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled); // No [Flags] attribute
                TryFormat(SByteEnum.Max, defaultFormatSpecifier, "Max", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

                // "G": Byte
                TryFormat(ByteEnum.Min, defaultFormatSpecifier, "Min", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
                TryFormat((ByteEnum)0xff, defaultFormatSpecifier, "Max", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
                TryFormat((ByteEnum)3, defaultFormatSpecifier, "3", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled); // No [Flags] attribute
                TryFormat(ByteEnum.Max, defaultFormatSpecifier, "Max", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

                // "G": Int16
                TryFormat(Int16Enum.Min, defaultFormatSpecifier, "Min", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
                TryFormat((Int16Enum)3, defaultFormatSpecifier, "3", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled); // No [Flags] attribute
                TryFormat(Int16Enum.Max, defaultFormatSpecifier, "Max", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

                // "G": UInt16
                TryFormat(UInt16Enum.Min, defaultFormatSpecifier, "Min", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
                TryFormat((UInt16Enum)3, defaultFormatSpecifier, "3", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled); // No [Flags] attribute
                TryFormat(UInt16Enum.Max, defaultFormatSpecifier, "Max", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

                // "G": Int32
                TryFormat(Int32Enum.Min, defaultFormatSpecifier, "Min", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
                TryFormat((Int32Enum)3, defaultFormatSpecifier, "3", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled); // No [Flags] attribute
                TryFormat(Int32Enum.Max, defaultFormatSpecifier, "Max", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

                // "G": UInt32
                TryFormat(UInt32Enum.Min, defaultFormatSpecifier, "Min", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
                TryFormat((UInt32Enum)3, defaultFormatSpecifier, "3", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled); // No [Flags] attribute
                TryFormat(UInt32Enum.Max, defaultFormatSpecifier, "Max", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

                // "G": Int64
                TryFormat(Int64Enum.Min, defaultFormatSpecifier, "Min", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
                TryFormat((Int64Enum)3, defaultFormatSpecifier, "3", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled); // No [Flags] attribute
                TryFormat(Int64Enum.Max, defaultFormatSpecifier, "Max", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

                // "G": UInt64
                TryFormat(UInt64Enum.Min, defaultFormatSpecifier, "Min", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
                TryFormat((UInt64Enum)3, defaultFormatSpecifier, "3", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled); // No [Flags] attribute
                TryFormat(UInt64Enum.Max, defaultFormatSpecifier, "Max", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);

                // "G": SimpleEnum
                TryFormat((SimpleEnum)99, defaultFormatSpecifier, "99", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
                TryFormat((SimpleEnum)99, defaultFormatSpecifier, "99", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
                TryFormat((SimpleEnum)0, defaultFormatSpecifier, "0", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled); // Not found

                // "G": Flags Attribute
                TryFormat(AttributeTargets.Class | AttributeTargets.Delegate, defaultFormatSpecifier, "Class, Delegate", validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            }
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

            // Format: G with Flags Attribute
            yield return new object[] { typeof(AttributeTargets), (int)(AttributeTargets.Class | AttributeTargets.Delegate), "G", "Class, Delegate" };

            // nint/nuint types
            if (PlatformDetection.IsReflectionEmitSupported && PlatformDetection.IsRareEnumsSupported)
            {
                yield return new object[] { s_intPtrEnumType, (nint)1, "G", "1" };
                yield return new object[] { s_uintPtrEnumType, (nuint)2, "F", "2" };
                yield return new object[] { s_floatEnumType, 1.4f, "G", (1.4f).ToString() };
                yield return new object[] { s_doubleEnumType, 2.5, "F", (2.5).ToString() };
            }
        }

        [Theory]
        [MemberData(nameof(Format_TestData))]
        public static void Format(Type enumType, object value, string format, string expected)
        {
            // Format string is case insensitive
            Assert.Equal(expected, Enum.Format(enumType, value, format.ToUpperInvariant()));
            Assert.Equal(expected, Enum.Format(enumType, value, format.ToLowerInvariant()));
        }

        // Select test here, to avoid rewriting input params, as MemberData will does work for generic methods
        private static void TryFormat<TEnum>(TEnum value, ReadOnlySpan<char> format, string expected, bool validateDestinationSpanSizeCheck, bool validateExtraSpanSpaceNotFilled) where TEnum : struct, Enum
        {
            if (validateDestinationSpanSizeCheck)
            {
                TryFormat_WithDestinationSpanSizeTooSmall_ReturnsFalseWithNoCharsWritten(value, format, expected);
            }
            if (validateExtraSpanSpaceNotFilled)
            {
                TryFormat_WithDestinationSpanLargerThanExpected_ReturnsTrueWithExpectedAndExtraSpaceNotFilled(value, format, expected);
            }
            else
            {
                TryFormat_WithValidParameters_ReturnsTrueWithExpected(value, format, expected);
            }

            if (format.Length == 1 && char.IsAsciiLetterUpper(format[0]))
            {
                TryFormat(value, (ReadOnlySpan<char>)new char[1] { char.ToLowerInvariant(format[0]) }, expected, validateDestinationSpanSizeCheck, validateExtraSpanSpaceNotFilled);
            }
        }

        private static void TryFormat_WithValidParameters_ReturnsTrueWithExpected<TEnum>(TEnum value, ReadOnlySpan<char> format, string expected) where TEnum : struct, Enum
        {
            Span<char> destination = new char[expected.Length];

            Assert.True(Enum.TryFormat(value, destination, out int charsWritten, format));
            Assert.Equal(expected, destination.ToString());
            Assert.Equal(expected.Length, charsWritten);
        }

        private static void TryFormat_WithDestinationSpanSizeTooSmall_ReturnsFalseWithNoCharsWritten<TEnum>(TEnum value, ReadOnlySpan<char> format, string expected) where TEnum : struct, Enum
        {
            int oneLessThanExpectedLength = expected.Length - 1;
            Span<char> destination = new char[oneLessThanExpectedLength];

            Assert.False(Enum.TryFormat(value, destination, out int charsWritten, format));
            Assert.Equal(new string('\0', oneLessThanExpectedLength), destination.ToString());
            Assert.Equal(0, charsWritten);
        }

        private static void TryFormat_WithDestinationSpanLargerThanExpected_ReturnsTrueWithExpectedAndExtraSpaceNotFilled<TEnum>(TEnum value, ReadOnlySpan<char> format, string expected) where TEnum : struct, Enum
        {
            int oneMoreThanExpectedLength = expected.Length + 1;
            Span<char> destination = new char[oneMoreThanExpectedLength];

            Assert.True(Enum.TryFormat(value, destination, out int charsWritten, format));
            Assert.Equal(expected + '\0', destination.ToString());
            Assert.Equal(expected.Length, charsWritten);
        }

        [Fact]
        private static void TryFormat_WithEmptySpan_ReturnsFalseWithNoCharsWritten()
        {
            Span<char> destination = new char[0];

            Assert.False(Enum.TryFormat(SimpleEnum.Green, destination, out int charsWritten, "G"));
            Assert.Equal("", destination.ToString());
            Assert.Equal(0, charsWritten);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("  ")]
        [InlineData("   \t")]
        [InlineData("a")]
        [InlineData("ab")]
        [InlineData("abc")]
        [InlineData("gg")]
        [InlineData("dd")]
        [InlineData("xx")]
        [InlineData("ff")]
        private static void TryFormat_WithInvalidFormat_ThrowsWithNoCharsWritten(string format)
        {
            SimpleEnum expecedEnum = SimpleEnum.Green;
            string expected = nameof(expecedEnum);
            int charsWritten = 0;
            char[] destination = new char[expected.Length];

            Assert.Throws<FormatException>(() => Enum.TryFormat(expecedEnum, destination, out charsWritten, format));
            Assert.Equal(new string('\0', expected.Length), new string(destination));
            Assert.Equal(0, charsWritten);
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

        public static IEnumerable<object[]> UnsupportedEnum_TestData()
        {
            yield return new object[] { Enum.ToObject(s_floatEnumType, 1) };
            yield return new object[] { Enum.ToObject(s_doubleEnumType, 2) };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported), nameof(PlatformDetection.IsRareEnumsSupported))]
        [MemberData(nameof(UnsupportedEnum_TestData))]
        public static void ToString_UnsupportedEnumType_ThrowsArgumentException(Enum e)
        {
            Exception formatXException = Assert.ThrowsAny<Exception>(() => e.ToString("X"));
            string formatXExceptionName = formatXException.GetType().Name;
            Assert.True(formatXExceptionName == nameof(InvalidOperationException) || formatXExceptionName == "ContractException");
        }

        private static EnumBuilder GetNonRuntimeEnumTypeBuilder(Type underlyingType)
        {
            if (!PlatformDetection.IsReflectionEmitSupported || !PlatformDetection.IsRareEnumsSupported)
                return null;

            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.Run);
            ModuleBuilder module = assembly.DefineDynamicModule("Name");

            return module.DefineEnum("TestName_" + underlyingType.Name, TypeAttributes.Public, underlyingType);
        }

        private static Type s_boolEnumType = GetBoolEnumType();
        private static Type GetBoolEnumType()
        {
            EnumBuilder enumBuilder = GetNonRuntimeEnumTypeBuilder(typeof(bool));
            if (enumBuilder == null)
                return null;

            enumBuilder.DefineLiteral("Value1", true);
            enumBuilder.DefineLiteral("Value2", false);

            return enumBuilder.CreateType();
        }

        private static Type s_charEnumType = GetCharEnumType();
        private static Type GetCharEnumType()
        {
            EnumBuilder enumBuilder = GetNonRuntimeEnumTypeBuilder(typeof(char));
            if (enumBuilder == null)
                return null;

            enumBuilder.DefineLiteral("Value1", (char)1);
            enumBuilder.DefineLiteral("Value2", (char)2);

            enumBuilder.DefineLiteral("Value0x3f06", (char)0x3f06);
            enumBuilder.DefineLiteral("Value0x3000", (char)0x3000);
            enumBuilder.DefineLiteral("Value0x0f06", (char)0x0f06);
            enumBuilder.DefineLiteral("Value0x1000", (char)0x1000);
            enumBuilder.DefineLiteral("Value0x0000", (char)0x0000);
            enumBuilder.DefineLiteral("Value0x0010", (char)0x0010);
            enumBuilder.DefineLiteral("Value0x3f16", (char)0x3f16);

            return enumBuilder.CreateType();
        }

        private static Type s_floatEnumType = GetFloatEnumType();
        private static Type GetFloatEnumType()
        {
            EnumBuilder enumBuilder = GetNonRuntimeEnumTypeBuilder(typeof(float));
            if (enumBuilder == null)
                return null;

            enumBuilder.DefineLiteral("Value1", 1.0f);
            enumBuilder.DefineLiteral("Value2", 2.0f);

            enumBuilder.DefineLiteral("Value0x3f06", (float)0x3f06);
            enumBuilder.DefineLiteral("Value0x3000", (float)0x3000);
            enumBuilder.DefineLiteral("Value0x0f06", (float)0x0f06);
            enumBuilder.DefineLiteral("Value0x1000", (float)0x1000);
            enumBuilder.DefineLiteral("Value0x0000", (float)0x0000);
            enumBuilder.DefineLiteral("Value0x0010", (float)0x0010);
            enumBuilder.DefineLiteral("Value0x3f16", (float)0x3f16);

            return enumBuilder.CreateType();
        }

        private static Type s_doubleEnumType = GetDoubleEnumType();
        private static Type GetDoubleEnumType()
        {
            EnumBuilder enumBuilder = GetNonRuntimeEnumTypeBuilder(typeof(double));
            if (enumBuilder == null)
                return null;

            enumBuilder.DefineLiteral("Value1", 1.0);
            enumBuilder.DefineLiteral("Value2", 2.0);

            enumBuilder.DefineLiteral("Value0x3f06", (double)0x3f06);
            enumBuilder.DefineLiteral("Value0x3000", (double)0x3000);
            enumBuilder.DefineLiteral("Value0x0f06", (double)0x0f06);
            enumBuilder.DefineLiteral("Value0x1000", (double)0x1000);
            enumBuilder.DefineLiteral("Value0x0000", (double)0x0000);
            enumBuilder.DefineLiteral("Value0x0010", (double)0x0010);
            enumBuilder.DefineLiteral("Value0x3f16", (double)0x3f16);

            return enumBuilder.CreateType();
        }

        private static Type s_intPtrEnumType = GetIntPtrEnumType();
        private static Type GetIntPtrEnumType()
        {
            EnumBuilder enumBuilder = GetNonRuntimeEnumTypeBuilder(typeof(IntPtr));
            if (enumBuilder == null)
                return null;

            return enumBuilder.CreateType();
        }

        private static Type s_uintPtrEnumType = GetUIntPtrEnumType();
        private static Type GetUIntPtrEnumType()
        {
            EnumBuilder enumBuilder = GetNonRuntimeEnumTypeBuilder(typeof(UIntPtr));
            if (enumBuilder == null)
                return null;

            return enumBuilder.CreateType();
        }
    }
}
