// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Tests
{
    public static partial class EnumTests
    {
        [Theory]
        [InlineData("Red", false, SimpleEnum.Red)]
        [InlineData(" Red", false, SimpleEnum.Red)]
        [InlineData("Red ", false, SimpleEnum.Red)]
        [InlineData(" red ", true, SimpleEnum.Red)]
        [InlineData("B", false, SimpleEnum.B)]
        [InlineData("B,B", false, SimpleEnum.B)]
        [InlineData(" Red , Blue ", false, SimpleEnum.Red | SimpleEnum.Blue)]
        [InlineData("Blue,Red,Green", false, SimpleEnum.Red | SimpleEnum.Blue | SimpleEnum.Green)]
        [InlineData("Blue,Red,Red,Red,Green", false, SimpleEnum.Red | SimpleEnum.Blue | SimpleEnum.Green)]
        [InlineData("Red,Blue,   Green", false, SimpleEnum.Red | SimpleEnum.Blue | SimpleEnum.Green)]
        [InlineData("1", false, SimpleEnum.Red)]
        [InlineData(" 1 ", false, SimpleEnum.Red)]
        [InlineData("2", false, SimpleEnum.Blue)]
        [InlineData("99", false, (SimpleEnum)99)]
        [InlineData("-42", false, (SimpleEnum)(-42))]
        [InlineData("   -42", false, (SimpleEnum)(-42))]
        [InlineData("   -42 ", false, (SimpleEnum)(-42))]
        public static void Parse(string value, bool ignoreCase, Enum expected)
        {
            SimpleEnum result;
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

        [Theory]
        [InlineData(null, "", false, typeof(ArgumentNullException))]
        [InlineData(typeof(SimpleEnum), null, false, typeof(ArgumentNullException))]
        [InlineData(typeof(object), "", false, typeof(ArgumentException))]
        [InlineData(typeof(SimpleEnum), "", false, typeof(ArgumentException))]
        [InlineData(typeof(SimpleEnum), "    \t", false, typeof(ArgumentException))]
        [InlineData(typeof(SimpleEnum), " red ", false, typeof(ArgumentException))]
        [InlineData(typeof(SimpleEnum), "Purple", false, typeof(ArgumentException))]
        [InlineData(typeof(SimpleEnum), ",Red", false, typeof(ArgumentException))]
        [InlineData(typeof(SimpleEnum), "Red,", false, typeof(ArgumentException))]
        [InlineData(typeof(SimpleEnum), "B,", false, typeof(ArgumentException))]
        [InlineData(typeof(SimpleEnum), " , , ,", false, typeof(ArgumentException))]
        [InlineData(typeof(SimpleEnum), "Red,Blue,", false, typeof(ArgumentException))]
        [InlineData(typeof(SimpleEnum), "Red,,Blue", false, typeof(ArgumentException))]
        [InlineData(typeof(SimpleEnum), "Red,Blue, ", false, typeof(ArgumentException))]
        [InlineData(typeof(SimpleEnum), "Red Blue", false, typeof(ArgumentException))]
        [InlineData(typeof(SimpleEnum), "1,Blue", false, typeof(ArgumentException))]
        [InlineData(typeof(SimpleEnum), "Blue,1", false, typeof(ArgumentException))]
        [InlineData(typeof(SimpleEnum), "Blue, 1", false, typeof(ArgumentException))]
        [InlineData(typeof(SimpleEnum), "2147483649", false, typeof(ArgumentException))]
        [InlineData(typeof(SimpleEnum), "2147483648", false, typeof(OverflowException))]
        public static void Parse_Invalid(Type enumType, string value, bool ignoreCase, Type exceptionType)
        {
            SimpleEnum result;
            if (!ignoreCase)
            {
                Assert.False(Enum.TryParse(value, out result));
                Assert.Equal(default(SimpleEnum), result);

                Assert.Throws(exceptionType, () => Enum.Parse(enumType, value));
            }

            Assert.False(Enum.TryParse(value, ignoreCase, out result));
            Assert.Equal(default(SimpleEnum), result);

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
            yield return new object[] { typeof(UInt64Enum), (ulong)1UL, "One" };
            yield return new object[] { typeof(UInt64Enum), (ulong)2UL, "Two" };
            yield return new object[] { typeof(UInt64Enum), ulong.MaxValue, "Max" };
            yield return new object[] { typeof(UInt64Enum), (ulong)3UL, null };

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
                Format(enumType, value, "G", expected);
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
            Assert.Throws<ArgumentNullException>("enumType", () => Enum.GetName(null, 1)); // Enum type is null
            Assert.Throws<ArgumentNullException>("value", () => Enum.GetName(t, null)); // Value is null

            Assert.Throws<ArgumentException>(null, () => Enum.GetName(typeof(object), 1)); // Enum type is not an enum
            Assert.Throws<ArgumentException>("value", () => Enum.GetName(t, "Red")); // Value is not the type of the enum's raw data
            Assert.Throws<ArgumentException>("value", () => Enum.GetName(t, (IntPtr)0)); // Value is out of range
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
            yield return new object[] { typeof(ByteEnum), (Byte)1, true };
            yield return new object[] { typeof(ByteEnum), (Byte)99, false };

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

            Assert.Throws<ArgumentNullException>("enumType", () => Enum.IsDefined(null, 1)); // Enum type is null
            Assert.Throws<ArgumentNullException>("value", () => Enum.IsDefined(t, null)); // Value is null

            Assert.Throws<ArgumentException>(null, () => Enum.IsDefined(t, Int32Enum.One)); // Value is different enum type

            // Value is not a valid type (MSDN claims this should throw InvalidOperationException)
            Assert.Throws<ArgumentException>(null, () => Enum.IsDefined(t, true));
            Assert.Throws<ArgumentException>(null, () => Enum.IsDefined(t, 'a'));

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
            Int32Enum e = (Int32Enum)0x3f06;

            Assert.Throws<ArgumentNullException>("flag", () => e.HasFlag(null)); // Flag is null
            Assert.Throws<ArgumentException>(null, () => e.HasFlag((SimpleEnum)0x3000)); // Enum is not the same type as the instance
        }

        [Theory]
        [InlineData(typeof(SByteEnum), (sbyte)42, (SByteEnum)42)]
        [InlineData(typeof(SByteEnum), (SByteEnum)0x42, (SByteEnum)0x42)]
        [InlineData(typeof(UInt64Enum), (ulong)0x0123456789abcdefL, (UInt64Enum)0x0123456789abcdefL)]
        [InlineData(typeof(ByteEnum), (ulong)0x0ccccccccccccc2aL, (ByteEnum)0x2a)] // Value overflows
        [InlineData(typeof(Int32Enum), false, (Int32Enum)0)] // Value is a bool
        [InlineData(typeof(Int32Enum), true, (Int32Enum)1)] // Value is a bool
        [InlineData(typeof(Int32Enum), 'a', (Int32Enum)97)] // Value is a char
        [InlineData(typeof(Int32Enum), 'b', (Int32Enum)98)] // Value is a char
        public static void ToObject(Type enumType, object value, object expected)
        {
            Assert.Equal(expected, Enum.ToObject(enumType, value));
        }

        public static IEnumerable<object[]> ToObject_InvalidEnumType_TestData()
        {
            yield return new object[] { null, typeof(ArgumentNullException) };
            yield return new object[] { typeof(Enum), typeof(ArgumentException) };
            yield return new object[] { typeof(object), typeof(ArgumentException) };
            yield return new object[] { GetNonRuntimeEnumTypeBuilder(typeof(int)).AsType(), typeof(ArgumentException) };
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

        [Fact]
        public static void ToObject_InvalidValue_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>("value", () => Enum.ToObject(typeof(SimpleEnum), null)); // Value is null
            Assert.Throws<ArgumentException>("value", () => Enum.ToObject(typeof(SimpleEnum), "Hello")); // Value is not a supported enum type
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            // SByte
            yield return new object[] { SByteEnum.One, SByteEnum.One, true };
            yield return new object[] { SByteEnum.One, SByteEnum.Two, false };
            yield return new object[] { SByteEnum.One, ByteEnum.One, false };
            yield return new object[] { SByteEnum.One, (sbyte)1, false };
            yield return new object[] { SByteEnum.One, null, false };

            // Byte
            yield return new object[] { ByteEnum.One, ByteEnum.One, true };
            yield return new object[] { ByteEnum.One, ByteEnum.Two, false };
            yield return new object[] { ByteEnum.One, SByteEnum.One, false };
            yield return new object[] { ByteEnum.One, (byte)1, false };
            yield return new object[] { ByteEnum.One, null, false };

            // Int16
            yield return new object[] { Int16Enum.One, Int16Enum.One, true };
            yield return new object[] { Int16Enum.One, Int16Enum.Two, false };
            yield return new object[] { Int16Enum.One, UInt16Enum.One, false };
            yield return new object[] { Int16Enum.One, (short)1, false };
            yield return new object[] { Int16Enum.One, null, false };

            // UInt16
            yield return new object[] { UInt16Enum.One, UInt16Enum.One, true };
            yield return new object[] { UInt16Enum.One, UInt16Enum.Two, false };
            yield return new object[] { UInt16Enum.One, Int16Enum.One, false };
            yield return new object[] { UInt16Enum.One, (ushort)1, false };
            yield return new object[] { UInt16Enum.One, null, false };

            // Int32
            yield return new object[] { Int32Enum.One, Int32Enum.One, true };
            yield return new object[] { Int32Enum.One, Int32Enum.Two, false };
            yield return new object[] { Int32Enum.One, UInt32Enum.One, false };
            yield return new object[] { Int32Enum.One, (short)1, false };
            yield return new object[] { Int32Enum.One, null, false };

            // UInt32
            yield return new object[] { UInt32Enum.One, UInt32Enum.One, true };
            yield return new object[] { UInt32Enum.One, UInt32Enum.Two, false };
            yield return new object[] { UInt32Enum.One, Int32Enum.One, false };
            yield return new object[] { UInt32Enum.One, (ushort)1, false };
            yield return new object[] { UInt32Enum.One, null, false };

            // Int64
            yield return new object[] { Int64Enum.One, Int64Enum.One, true };
            yield return new object[] { Int64Enum.One, Int64Enum.Two, false };
            yield return new object[] { Int64Enum.One, UInt64Enum.One, false };
            yield return new object[] { Int64Enum.One, (long)1, false };
            yield return new object[] { Int64Enum.One, null, false };

            // UInt64
            yield return new object[] { UInt64Enum.One, UInt64Enum.One, true };
            yield return new object[] { UInt64Enum.One, UInt64Enum.Two, false };
            yield return new object[] { UInt64Enum.One, Int64Enum.One, false };
            yield return new object[] { UInt64Enum.One, (ulong)1, false };
            yield return new object[] { UInt64Enum.One, null, false };

            // Char
            yield return new object[] { Enum.Parse(s_charEnumType, "Value1"), Enum.Parse(s_charEnumType, "Value1"), true };
            yield return new object[] { Enum.Parse(s_charEnumType, "Value1"), Enum.Parse(s_charEnumType, "Value2"), false };
            yield return new object[] { Enum.Parse(s_charEnumType, "Value1"), UInt16Enum.One, false };
            yield return new object[] { Enum.Parse(s_charEnumType, "Value1"), (char)1, false };
            yield return new object[] { Enum.Parse(s_charEnumType, "Value1"), null, false };

            // Bool
            yield return new object[] { Enum.Parse(s_boolEnumType, "Value1"), Enum.Parse(s_boolEnumType, "Value1"), true };
            yield return new object[] { Enum.Parse(s_boolEnumType, "Value1"), Enum.Parse(s_boolEnumType, "Value2"), false };
            yield return new object[] { Enum.Parse(s_boolEnumType, "Value1"), UInt16Enum.One, false };
            yield return new object[] { Enum.Parse(s_boolEnumType, "Value1"), true, false };
            yield return new object[] { Enum.Parse(s_boolEnumType, "Value1"), null, false };
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

            // Char
            yield return new object[] { Enum.Parse(s_charEnumType, "Value2"), Enum.Parse(s_charEnumType, "Value2"), 0 };
            yield return new object[] { Enum.Parse(s_charEnumType, "Value2"), Enum.Parse(s_charEnumType, "Value1"), 1 };
            yield return new object[] { Enum.Parse(s_charEnumType, "Value2"), Enum.Parse(s_charEnumType, "Value3"), -1 };
            yield return new object[] { Enum.Parse(s_charEnumType, "Value2"), null, 1 };

            // Bool
            yield return new object[] { Enum.Parse(s_boolEnumType, "Value1"), Enum.Parse(s_boolEnumType, "Value1"), 0 };
            yield return new object[] { Enum.Parse(s_boolEnumType, "Value1"), Enum.Parse(s_boolEnumType, "Value2"), 1 };
            yield return new object[] { Enum.Parse(s_boolEnumType, "Value2"), Enum.Parse(s_boolEnumType, "Value1"), -1 };
            yield return new object[] { Enum.Parse(s_boolEnumType, "Value1"), null, 1 };
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
            Assert.Throws<ArgumentException>(null, () => SimpleEnum.Red.CompareTo((sbyte)1)); // Target is not an enum type
            Assert.Throws<ArgumentException>(null, () => SimpleEnum.Red.CompareTo(Int32Enum.One)); //Target is a different enum type
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
            yield return new object[] { s_boolEnumType, typeof(bool) };
            yield return new object[] { s_charEnumType, typeof(char) };
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
            Assert.Throws<ArgumentNullException>("enumType", () => Enum.GetUnderlyingType(null)); // Enum type is null
            Assert.Throws<ArgumentException>("enumType", () => Enum.GetUnderlyingType(typeof(Enum))); // Enum type is simply an enum
        }

        [Theory]
        [InlineData(typeof(SimpleEnum),
                new string[] { "Red", "Blue", "Green", "Green_a", "Green_b", "B" },
                new SimpleEnum[] { SimpleEnum.Red, SimpleEnum.Blue, SimpleEnum.Green, SimpleEnum.Green_a, SimpleEnum.Green_b, SimpleEnum.B })]

        [InlineData(typeof(ByteEnum),
                new string[] { "Min", "One", "Two", "Max" },
                new ByteEnum[] { ByteEnum.Min, ByteEnum.One, ByteEnum.Two, ByteEnum.Max })]

        [InlineData(typeof(SByteEnum),
                new string[] { "One", "Two", "Max", "Min" },
                new SByteEnum[] { SByteEnum.One, SByteEnum.Two, SByteEnum.Max, SByteEnum.Min })]

        [InlineData(typeof(UInt16Enum),
                new string[] { "Min", "One", "Two", "Max" },
                new UInt16Enum[] { UInt16Enum.Min, UInt16Enum.One, UInt16Enum.Two, UInt16Enum.Max })]

        [InlineData(typeof(Int16Enum),
                new string[] { "One", "Two", "Max", "Min" },
                new Int16Enum[] { Int16Enum.One, Int16Enum.Two, Int16Enum.Max, Int16Enum.Min })]

        [InlineData(typeof(UInt32Enum),
                new string[] { "Min", "One", "Two", "Max" },
                new UInt32Enum[] { UInt32Enum.Min, UInt32Enum.One, UInt32Enum.Two, UInt32Enum.Max })]

        [InlineData(typeof(Int32Enum),
                new string[] { "One", "Two", "Max", "Min" },
                new Int32Enum[] { Int32Enum.One, Int32Enum.Two, Int32Enum.Max, Int32Enum.Min })]

        [InlineData(typeof(UInt64Enum),
                new string[] { "Min", "One", "Two", "Max" },
                new UInt64Enum[] { UInt64Enum.Min, UInt64Enum.One, UInt64Enum.Two, UInt64Enum.Max })]

        [InlineData(typeof(Int64Enum),
                new string[] { "One", "Two", "Max", "Min" },
                new Int64Enum[] { Int64Enum.One, Int64Enum.Two, Int64Enum.Max, Int64Enum.Min })]
        public static void GetNames_GetValues(Type enumType, string[] expectedNames, Array expectedValues)
        {
            Assert.Equal(expectedNames, Enum.GetNames(enumType));
            Assert.Equal(expectedValues, Enum.GetValues(enumType));
        }

        [Fact]
        public static void GetNames_Invalid()
        {
            Assert.Throws<ArgumentNullException>("enumType", () => Enum.GetNames(null)); // Enum type is null
            Assert.Throws<ArgumentException>("enumType", () => Enum.GetNames(typeof(object))); // Enum type is not an enum
        }

        [Fact]
        public static void GetValues_Invalid()
        {
            Assert.Throws<ArgumentNullException>("enumType", () => Enum.GetValues(null)); // Enum type is null
            Assert.Throws<ArgumentException>("enumType", () => Enum.GetValues(typeof(object))); // Enum type is not an enum
        }

        // Byte
        [InlineData(typeof(ByteEnum), byte.MaxValue, ByteEnum.Max)]
        [InlineData(typeof(ByteEnum), (byte)1, ByteEnum.One)]
        [InlineData(typeof(ByteEnum), (byte)11, (ByteEnum)11)]

        // SByte
        [InlineData(typeof(SByteEnum), sbyte.MinValue, SByteEnum.Min)]
        [InlineData(typeof(SByteEnum), (sbyte)2, SByteEnum.Two)]
        [InlineData(typeof(SByteEnum), (sbyte)22, (SByteEnum)22)]

        // UInt16
        [InlineData(typeof(UInt16Enum), ushort.MaxValue, UInt16Enum.Max)]
        [InlineData(typeof(UInt16Enum), (ushort)1, UInt16Enum.One)]
        [InlineData(typeof(UInt16Enum), (ushort)33, (UInt16Enum)33)]

        // Int16
        [InlineData(typeof(Int16Enum), short.MinValue, Int16Enum.Min)]
        [InlineData(typeof(Int16Enum), (short)2, Int16Enum.Two)]
        [InlineData(typeof(Int16Enum), (short)44, (Int16Enum)44)]

        // UInt32
        [InlineData(typeof(UInt32Enum), uint.MaxValue, UInt32Enum.Max)]
        [InlineData(typeof(UInt32Enum), (uint)1, UInt32Enum.One)]
        [InlineData(typeof(UInt32Enum), (uint)55, (UInt32Enum)55)]

        // Int32
        [InlineData(typeof(Int32Enum), int.MinValue, Int32Enum.Min)]
        [InlineData(typeof(Int32Enum), 2, Int32Enum.Two)]
        [InlineData(typeof(Int32Enum), 66, (Int32Enum)66)]

        // UInt64
        [InlineData(typeof(UInt64Enum), ulong.MaxValue, UInt64Enum.Max)]
        [InlineData(typeof(UInt64Enum), (ulong)1, UInt64Enum.One)]
        [InlineData(typeof(UInt64Enum), (ulong)77, (UInt64Enum)77)]

        // Int64
        [InlineData(typeof(Int64Enum), long.MinValue, Int64Enum.Min)]
        [InlineData(typeof(Int64Enum), (long)2, Int64Enum.Two)]
        [InlineData(typeof(Int64Enum), (long)88, (Int64Enum)88)]
        public static void ToObject(Type type, object value, Enum expected)
        {
            var typedValue = Convert.ChangeType(value, value.GetType());
            var enumObject = Enum.ToObject(type, typedValue);
            Assert.Equal(expected, enumObject);
        }

        public static IEnumerable<object[]> ToString_Format_TestData()
        {
            // Format: "D"
            yield return new object[] { ByteEnum.Min, "D", "0" };
            yield return new object[] { ByteEnum.One, "D", "1" };
            yield return new object[] { ByteEnum.Two, "D", "2" };
            yield return new object[] { (ByteEnum)99, "D", "99" };
            yield return new object[] { ByteEnum.Max, "D", "255" };

            yield return new object[] { SByteEnum.Min, "D", "-128" };
            yield return new object[] { SByteEnum.One, "D", "1" };
            yield return new object[] { SByteEnum.Two, "D", "2" };
            yield return new object[] { (SByteEnum)99, "D", "99" };
            yield return new object[] { SByteEnum.Max, "D", "127" };

            yield return new object[] { UInt16Enum.Min, "D", "0" };
            yield return new object[] { UInt16Enum.One, "D", "1" };
            yield return new object[] { UInt16Enum.Two, "D", "2" };
            yield return new object[] { (UInt16Enum)99, "D", "99" };
            yield return new object[] { UInt16Enum.Max, "D", "65535" };

            yield return new object[] { Int16Enum.Min, "D", "-32768" };
            yield return new object[] { Int16Enum.One, "D", "1" };
            yield return new object[] { Int16Enum.Two, "D", "2" };
            yield return new object[] { (Int16Enum)99, "D", "99" };
            yield return new object[] { Int16Enum.Max, "D", "32767" };

            yield return new object[] { UInt32Enum.Min, "D", "0" };
            yield return new object[] { UInt32Enum.One, "D", "1" };
            yield return new object[] { UInt32Enum.Two, "D", "2" };
            yield return new object[] { (UInt32Enum)99, "D", "99" };
            yield return new object[] { UInt32Enum.Max, "D", "4294967295" };

            yield return new object[] { Int32Enum.Min, "D", "-2147483648" };
            yield return new object[] { Int32Enum.One, "D", "1" };
            yield return new object[] { Int32Enum.Two, "D", "2" };
            yield return new object[] { (Int32Enum)99, "D", "99" };
            yield return new object[] { Int32Enum.Max, "D", "2147483647" };

            yield return new object[] { UInt64Enum.Min, "D", "0" };
            yield return new object[] { UInt64Enum.One, "D", "1" };
            yield return new object[] { UInt64Enum.Two, "D", "2" };
            yield return new object[] { (UInt64Enum)99, "D", "99" };
            yield return new object[] { UInt64Enum.Max, "D", "18446744073709551615" };

            yield return new object[] { Int64Enum.Min, "D", "-9223372036854775808" };
            yield return new object[] { Int64Enum.One, "D", "1" };
            yield return new object[] { Int64Enum.Two, "D", "2" };
            yield return new object[] { (Int64Enum)99, "D", "99" };
            yield return new object[] { Int64Enum.Max, "D", "9223372036854775807" };

            // Format "X": value in hex form without a leading "0x"
            yield return new object[] { ByteEnum.Min, "X", "00" };
            yield return new object[] { ByteEnum.One, "X", "01" };
            yield return new object[] { ByteEnum.Two, "X", "02" };
            yield return new object[] { (ByteEnum)99, "X", "63" };
            yield return new object[] { ByteEnum.Max, "X", "FF" };

            yield return new object[] { SByteEnum.Min, "X", "80" };
            yield return new object[] { SByteEnum.One, "X", "01" };
            yield return new object[] { SByteEnum.Two, "X", "02" };
            yield return new object[] { (SByteEnum)99, "X", "63" };
            yield return new object[] { SByteEnum.Max, "X", "7F" };

            yield return new object[] { UInt16Enum.Min, "X", "0000" };
            yield return new object[] { UInt16Enum.One, "X", "0001" };
            yield return new object[] { UInt16Enum.Two, "X", "0002" };
            yield return new object[] { (UInt16Enum)99, "X", "0063" };
            yield return new object[] { UInt16Enum.Max, "X", "FFFF" };

            yield return new object[] { Int16Enum.Min, "X", "8000" };
            yield return new object[] { Int16Enum.One, "X", "0001" };
            yield return new object[] { Int16Enum.Two, "X", "0002" };
            yield return new object[] { (Int16Enum)99, "X", "0063" };
            yield return new object[] { Int16Enum.Max, "X", "7FFF" };

            yield return new object[] { UInt32Enum.Min, "X", "00000000" };
            yield return new object[] { UInt32Enum.One, "X", "00000001" };
            yield return new object[] { UInt32Enum.Two, "X", "00000002" };
            yield return new object[] { (UInt32Enum)99, "X", "00000063" };
            yield return new object[] { UInt32Enum.Max, "X", "FFFFFFFF" };

            yield return new object[] { Int32Enum.Min, "X", "80000000" };
            yield return new object[] { Int32Enum.One, "X", "00000001" };
            yield return new object[] { Int32Enum.Two, "X", "00000002" };
            yield return new object[] { (Int32Enum)99, "X", "00000063" };
            yield return new object[] { Int32Enum.Max, "X", "7FFFFFFF" };

            yield return new object[] { UInt64Enum.Min, "X", "0000000000000000" };
            yield return new object[] { UInt64Enum.One, "X", "0000000000000001" };
            yield return new object[] { UInt64Enum.Two, "X", "0000000000000002" };
            yield return new object[] { (UInt64Enum)99, "X", "0000000000000063" };
            yield return new object[] { UInt64Enum.Max, "X", "FFFFFFFFFFFFFFFF" };

            yield return new object[] { Int64Enum.Min, "X", "8000000000000000" };
            yield return new object[] { Int64Enum.One, "X", "0000000000000001" };
            yield return new object[] { Int64Enum.Two, "X", "0000000000000002" };
            yield return new object[] { (Int64Enum)99, "X", "0000000000000063" };
            yield return new object[] { Int64Enum.Max, "X", "7FFFFFFFFFFFFFFF" };

            // Format "F". value is treated as a bit field that contains one or more flags that consist of one or more bits.
            // If value is equal to a combination of named enumerated constants, a delimiter-separated list of the names 
            // of those constants is returned. value is searched for flags, going from the flag with the largest value 
            // to the smallest value. For each flag that corresponds to a bit field in value, the name of the constant 
            // is concatenated to the delimiter-separated list. The value of that flag is then excluded from further 
            // consideration, and the search continues for the next flag.
            //
            // If value is not equal to a combination of named enumerated constants, the decimal equivalent of value is returned. 
            yield return new object[] { SimpleEnum.Red, "F", "Red" };
            yield return new object[] { SimpleEnum.Blue, "F", "Blue" };
            yield return new object[] { (SimpleEnum)99, "F", "99" };
            yield return new object[] { (SimpleEnum)0, "F", "0" }; // Not found

            yield return new object[] { (ByteEnum)0, "F", "Min" };
            yield return new object[] { (ByteEnum)3, "F", "One, Two" };
            yield return new object[] { (ByteEnum)0xff, "F", "Max" }; // Larger values take precedence (and remove the bits from consideration.)

            // Format "G": If value is equal to a named enumerated constant, the name of that constant is returned.
            // Otherwise, if "[Flags]" present, do as Format "F" - else return the decimal value of "value".
            yield return new object[] { (SimpleEnum)99, "G", "99" };
            yield return new object[] { (SimpleEnum)0, "G", "0" }; // Not found

            yield return new object[] { (ByteEnum)(byte)0, "G", "Min" };
            yield return new object[] { (ByteEnum)0xff, "G", "Max" };
                    
            yield return new object[] { (ByteEnum)(byte)3, "G", "3" }; // No [Flags] attribute
            yield return new object[] { ByteEnum.Max, "G", "Max" };
                    
            yield return new object[] { (SByteEnum)(sbyte)3, "G", "3" }; // No [Flags] attribute
            yield return new object[] { SByteEnum.Max, "G", "Max" };
                    
            yield return new object[] { (UInt16Enum)(ushort)3, "G", "3" }; // No [Flags] attribute
            yield return new object[] { UInt16Enum.Max, "G", "Max" };
                    
            yield return new object[] { (Int16Enum)(short)3, "G", "3" }; // No [Flags] attribute
            yield return new object[] { Int16Enum.Max, "G", "Max" };
                    
            yield return new object[] { (UInt32Enum)(UInt32)3, "G", "3" }; // No [Flags] attribute
            yield return new object[] { UInt32Enum.Max, "G", "Max" };
                    
            yield return new object[] { (Int32Enum)(Int32)3, "G", "3" }; // No [Flags] attribute
            yield return new object[] { Int32Enum.Max, "G", "Max" };
                    
            yield return new object[] { (UInt64Enum)(UInt64)3, "G", "3" }; // No [Flags] attribute
            yield return new object[] { UInt64Enum.Max, "G", "Max" };
                    
            yield return new object[] { (Int64Enum)(Int64)3, "G", "3" }; // No [Flags] attribute
            yield return new object[] { Int64Enum.Max, "G", "Max" };

            yield return new object[] { AttributeTargets.Class | AttributeTargets.Delegate, "F", "Class, Delegate" }; // [Flags] attribute
        }

        [Theory]
        [MemberData(nameof(ToString_Format_TestData))]
        public static void ToString_Format(Enum e, string format, string expected)
        {
            if (format.ToUpperInvariant() == "G")
            {
                string nullString = null;

                Assert.Equal(expected, e.ToString());
                Assert.Equal(expected, e.ToString(""));
                Assert.Equal(expected, e.ToString(nullString));
            }
            // Format string is non-case-sensitive
            Assert.Equal(expected, e.ToString(format));
            Assert.Equal(expected, e.ToString(format.ToUpperInvariant()));
            Assert.Equal(expected, e.ToString(format.ToLowerInvariant()));

            Format(e.GetType(), e, format, expected);
        }

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

        [Theory]
        // Format: D
        [InlineData(typeof(SimpleEnum), SimpleEnum.Red, "D", "1")]
        [InlineData(typeof(SimpleEnum), 1, "D", "1")]
        // Format: X
        [InlineData(typeof(SimpleEnum), SimpleEnum.Red, "X", "00000001")]
        [InlineData(typeof(SimpleEnum), 1, "X", "00000001")]
        // Format: F
        [InlineData(typeof(SimpleEnum), SimpleEnum.Red, "F", "Red")]
        [InlineData(typeof(SimpleEnum), 1, "F", "Red")]
        public static void Format(Type enumType, object value, string format, string expected)
        {
            // Format string is case insensitive
            Assert.Equal(expected, Enum.Format(enumType, value, format.ToUpperInvariant()));
            Assert.Equal(expected, Enum.Format(enumType, value, format.ToLowerInvariant()));
        }

        [Fact]
        public static void Format_Invalid()
        {
            Assert.Throws<ArgumentNullException>("enumType", () => Enum.Format(null, (Int32Enum)1, "F")); // Enum type is null
            Assert.Throws<ArgumentNullException>("value", () => Enum.Format(typeof(SimpleEnum), null, "F")); // Value is null
            Assert.Throws<ArgumentNullException>("format", () => Enum.Format(typeof(SimpleEnum), SimpleEnum.Red, null)); // Format is null

            Assert.Throws<ArgumentException>("enumType", () => Enum.Format(typeof(object), 1, "F")); // Enum type is not an enum type

            Assert.Throws<ArgumentException>(null, () => Enum.Format(typeof(SimpleEnum), (Int32Enum)1, "F")); // Value is of the wrong enum type

            Assert.Throws<ArgumentException>(null, () => Enum.Format(typeof(SimpleEnum), (short)1, "F")); // Value is of the wrong integral
            Assert.Throws<ArgumentException>(null, () => Enum.Format(typeof(SimpleEnum), "Red", "F")); // Value is of the wrong integral

            Assert.Throws<FormatException>(() => Enum.Format(typeof(SimpleEnum), SimpleEnum.Red, "")); // Format is empty
            Assert.Throws<FormatException>(() => Enum.Format(typeof(SimpleEnum), SimpleEnum.Red, "   \t")); // Format is whitespace
            Assert.Throws<FormatException>(() => Enum.Format(typeof(SimpleEnum), SimpleEnum.Red, "t")); // No such format
        }

        private enum SimpleEnum
        {
            Red = 1,
            Blue = 2,
            Green = 3,
            Green_a = 3,
            Green_b = 3,
            B = 4
        }

        private enum ByteEnum : byte
        {
            Min = byte.MinValue,
            One = 1,
            Two = 2,
            Max = byte.MaxValue,
        }

        private enum SByteEnum : sbyte
        {
            Min = sbyte.MinValue,
            One = 1,
            Two = 2,
            Max = sbyte.MaxValue,
        }

        private enum UInt16Enum : ushort
        {
            Min = ushort.MinValue,
            One = 1,
            Two = 2,
            Max = ushort.MaxValue,
        }

        private enum Int16Enum : short
        {
            Min = short.MinValue,
            One = 1,
            Two = 2,
            Max = short.MaxValue,
        }

        private enum UInt32Enum : uint
        {
            Min = uint.MinValue,
            One = 1,
            Two = 2,
            Max = uint.MaxValue,
        }

        private enum Int32Enum : int
        {
            Min = int.MinValue,
            One = 1,
            Two = 2,
            Max = int.MaxValue,
        }

        private enum UInt64Enum : ulong
        {
            Min = ulong.MinValue,
            One = 1,
            Two = 2,
            Max = ulong.MaxValue,
        }

        private enum Int64Enum : long
        {
            Min = long.MinValue,
            One = 1,
            Two = 2,
            Max = long.MaxValue,
        }
        
        private static EnumBuilder GetNonRuntimeEnumTypeBuilder(Type underlyingType)
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.Run);
            ModuleBuilder module = assembly.DefineDynamicModule("Name");

            return module.DefineEnum("TestName", TypeAttributes.Public, underlyingType);
        }

        private static Type s_boolEnumType = GetBoolEnumType();
        private static Type GetBoolEnumType()
        {
            EnumBuilder enumBuilder = GetNonRuntimeEnumTypeBuilder(typeof(bool));
            enumBuilder.DefineLiteral("Value1", true);
            enumBuilder.DefineLiteral("Value2", false);

            return enumBuilder.CreateTypeInfo().AsType();
        }

        private static Type s_charEnumType = GetCharEnumType();
        private static Type GetCharEnumType()
        {
            EnumBuilder enumBuilder = GetNonRuntimeEnumTypeBuilder(typeof(char));
            enumBuilder.DefineLiteral("Value1", (char)1);
            enumBuilder.DefineLiteral("Value2", (char)2);
            enumBuilder.DefineLiteral("Value3", (char)3);

            enumBuilder.DefineLiteral("Value0x3f06", (char)0x3f06);
            enumBuilder.DefineLiteral("Value0x3000", (char)0x3000);
            enumBuilder.DefineLiteral("Value0x0f06", (char)0x0f06);
            enumBuilder.DefineLiteral("Value0x1000", (char)0x1000);
            enumBuilder.DefineLiteral("Value0x0000", (char)0x0000);
            enumBuilder.DefineLiteral("Value0x0010", (char)0x0010);
            enumBuilder.DefineLiteral("Value0x3f16", (char)0x3f16);

            return enumBuilder.CreateTypeInfo().AsType();
        }
    }
}
