// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.VisualBasic.CompilerServices;
using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public class ConversionsTests
    {
        private static bool? s_reflectionEmitSupported = null;

        public static bool ReflectionEmitSupported
        {
            get
            {
                if (s_reflectionEmitSupported == null)
                {
                    try
                    {
                        object o = FloatEnum;
                        s_reflectionEmitSupported = true;
                    }
                    catch (PlatformNotSupportedException)
                    {
                        s_reflectionEmitSupported = false;
                    }
                }

                return s_reflectionEmitSupported.Value;
            }
        }

        public static IEnumerable<object[]> InvalidString_TestData()
        {
            yield return new object[] { "" };
            yield return new object[] { "&" };
            yield return new object[] { "&a" };
            yield return new object[] { "&a0" };
            yield return new object[] { "true" };
            yield return new object[] { "false" };
            yield return new object[] { "invalid" };
        }

        public static IEnumerable<object[]> InvalidBool_TestData()
        {
            if (ReflectionEmitSupported)
            {
                yield return new object[] { FloatEnum };
                yield return new object[] { DoubleEnum };
                yield return new object[] { IntPtrEnum };
                yield return new object[] { UIntPtrEnum };
            }
        }

        public static IEnumerable<object[]> InvalidNumberObject_TestData()
        {
            yield return new object[] { char.MinValue };
            yield return new object[] { (char)1 };
            yield return new object[] { char.MaxValue };
            yield return new object[] { new DateTime(10) };
            yield return new object[] { new object() };
            if (ReflectionEmitSupported)
            {
                yield return new object[] { CharEnum };
            }
        }

        public static IEnumerable<object[]> ToByte_String_TestData()
        {
            yield return new object[] { null, byte.MinValue };
            yield return new object[] { "0", byte.MinValue };
            yield return new object[] { "1", (byte)1 };
            yield return new object[] { "&h5", (byte)5 };
            yield return new object[] { "&h0", byte.MinValue };
            yield return new object[] { "&o5", (byte)5 };
            yield return new object[] { " &o5", (byte)5 };
            yield return new object[] { "&o0", byte.MinValue };
            yield return new object[] { 1.1.ToString(), (byte)1 };
        }

        [Theory]
        [MemberData(nameof(ToByte_String_TestData))]
        public void ToByte_String_ReturnsExpected(string value, byte expected)
        {
            AssertEqual(expected, Conversions.ToByte(value));
        }

        [Theory]
        [MemberData(nameof(InvalidString_TestData))]
        public void ToByte_InvalidString_ThrowsInvalidCastException(string value)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToByte(value));
        }

        public static IEnumerable<object[]> ToByte_OverflowString_TestData()
        {
            yield return new object[] { "256" };
        }

        [Theory]
        [MemberData(nameof(ToByte_OverflowString_TestData))]
        [MemberData(nameof(ToUShort_OverflowString_TestData))]
        [MemberData(nameof(ToUInteger_OverflowString_TestData))]
        [MemberData(nameof(ToULong_OverflowString_TestData))]
        public void ToByte_OverflowString_ThrowsOverflowException(string value)
        {
            Assert.Throws<OverflowException>(() => Conversions.ToByte(value));
        }

        public static IEnumerable<object[]> ToByte_Object_TestData()
        {
            // byte.
            yield return new object[] { byte.MinValue, byte.MinValue };
            yield return new object[] { (byte)1, (byte)1 };
            yield return new object[] { byte.MaxValue, byte.MaxValue };
            yield return new object[] { (ByteEnum)byte.MinValue, byte.MinValue };
            yield return new object[] { (ByteEnum)1, (byte)1 };
            yield return new object[] { (ByteEnum)byte.MaxValue, byte.MaxValue };

            // sbyte.
            yield return new object[] { (sbyte)0, byte.MinValue };
            yield return new object[] { (sbyte)1, (byte)1 };
            yield return new object[] { sbyte.MaxValue, (byte)127 };
            yield return new object[] { (SByteEnum)0, byte.MinValue };
            yield return new object[] { (SByteEnum)1, (byte)1 };
            yield return new object[] { (SByteEnum)sbyte.MaxValue, (byte)127 };

            // ushort.
            yield return new object[] { ushort.MinValue, byte.MinValue };
            yield return new object[] { (ushort)1, (byte)1 };
            yield return new object[] { (UShortEnum)ushort.MinValue, byte.MinValue };
            yield return new object[] { (UShortEnum)1, (byte)1 };

            // short.
            yield return new object[] { (short)0, byte.MinValue };
            yield return new object[] { (short)1, (byte)1 };
            yield return new object[] { (ShortEnum)0, byte.MinValue };
            yield return new object[] { (ShortEnum)1, (byte)1 };

            // uint.
            yield return new object[] { uint.MinValue, byte.MinValue };
            yield return new object[] { (uint)1, (byte)1 };
            yield return new object[] { (UIntEnum)uint.MinValue, byte.MinValue };
            yield return new object[] { (UIntEnum)1, (byte)1 };

            // int.
            yield return new object[] { 0, byte.MinValue };
            yield return new object[] { 1, (byte)1 };
            yield return new object[] { (IntEnum)0, byte.MinValue };
            yield return new object[] { (IntEnum)1, (byte)1 };

            // ulong.
            yield return new object[] { ulong.MinValue, byte.MinValue };
            yield return new object[] { (ulong)1, (byte)1 };
            yield return new object[] { (ULongEnum)ulong.MinValue, byte.MinValue };
            yield return new object[] { (ULongEnum)1, (byte)1 };

            // long.
            yield return new object[] { (long)0, byte.MinValue };
            yield return new object[] { (long)1, (byte)1 };
            yield return new object[] { (LongEnum)0, byte.MinValue };
            yield return new object[] { (LongEnum)1, (byte)1 };

            // float.
            yield return new object[] { (float)0, byte.MinValue };
            yield return new object[] { (float)1, (byte)1 };

            // double.
            yield return new object[] { (double)0, byte.MinValue };
            yield return new object[] { (double)1, (byte)1 };

            // decimal.
            yield return new object[] { (decimal)0, byte.MinValue };
            yield return new object[] { (decimal)1, (byte)1 };

            // bool.
            yield return new object[] { true, byte.MaxValue };
            yield return new object[] { false, byte.MinValue };
            if (ReflectionEmitSupported)
            {
                yield return new object[] { BoolEnum, byte.MinValue };
            }

            // null.
            yield return new object[] { null, byte.MinValue };
        }

        [Theory]
        [MemberData(nameof(ToByte_Object_TestData))]
        [MemberData(nameof(ToByte_String_TestData))]
        public void ToByte_Object_ReturnsExpected(IConvertible value, byte expected)
        {
            AssertEqual(expected, Conversions.ToByte(value));
            if (value != null)
            {
                AssertEqual(expected, Conversions.ToByte(new ConvertibleWrapper(value)));
            }
        }

        [Theory]
        [MemberData(nameof(InvalidNumberObject_TestData))]
        [MemberData(nameof(InvalidString_TestData))]
        public void ToByte_InvalidObject_ThrowsInvalidCastException(object value)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToByte(value));
        }

        [Theory]
        [MemberData(nameof(InvalidBool_TestData))]
        public void ToByte_InvalidBool_ThrowsInvalidOperationException(object value)
        {
            Assert.Throws<InvalidOperationException>(() => Conversions.ToByte(value));
        }

        public static IEnumerable<object[]> ToByte_OverflowObject_TestData()
        {
            yield return new object[] { ushort.MaxValue };
            yield return new object[] { (UShortEnum)ushort.MaxValue };
            yield return new object[] { short.MaxValue };
            yield return new object[] { (ShortEnum)short.MaxValue };
        }

        [Theory]
        [MemberData(nameof(ToByte_OverflowObject_TestData))]
        [MemberData(nameof(ToUShort_OverflowObject_TestData))]
        [MemberData(nameof(ToUInteger_OverflowObject_TestData))]
        [MemberData(nameof(ToULong_OverflowObject_TestData))]
        [MemberData(nameof(ToByte_OverflowString_TestData))]
        [MemberData(nameof(ToUShort_OverflowString_TestData))]
        [MemberData(nameof(ToUInteger_OverflowString_TestData))]
        [MemberData(nameof(ToULong_OverflowString_TestData))]
        public void ToByte_OverflowObject_ThrowsOverflowException(object value)
        {
            Assert.Throws<OverflowException>(() => Conversions.ToByte(value));
        }

        public static IEnumerable<object[]> ToSByte_String_TestData()
        {
            yield return new object[] { null, (sbyte)0 };
            yield return new object[] { "-1", (sbyte)(-1) };
            yield return new object[] { "0", (sbyte)0 };
            yield return new object[] { "1", (sbyte)1 };
            yield return new object[] { "&h5", (sbyte)5 };
            yield return new object[] { "&h0", (sbyte)0 };
            yield return new object[] { "&o5", (sbyte)5 };
            yield return new object[] { " &o5", (sbyte)5 };
            yield return new object[] { "&o0", (sbyte)0 };
            yield return new object[] { 1.1.ToString(), (sbyte)1 };
        }

        [Theory]
        [MemberData(nameof(ToSByte_String_TestData))]
        public void ToSByte_String_ReturnsExpected(string value, sbyte expected)
        {
            AssertEqual(expected, Conversions.ToSByte(value));
        }

        [Theory]
        [MemberData(nameof(InvalidString_TestData))]
        public void ToSByte_InvalidString_ThrowsInvalidCastException(string value)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToSByte(value));
        }

        public static IEnumerable<object[]> ToSByte_OverflowString_TestData()
        {
            yield return new object[] { "128" };
        }

        [Theory]
        [MemberData(nameof(ToSByte_OverflowString_TestData))]
        [MemberData(nameof(ToShort_OverflowString_TestData))]
        [MemberData(nameof(ToInteger_OverflowString_TestData))]
        [MemberData(nameof(ToLong_OverflowString_TestData))]
        public void ToSByte_OverflowString_ThrowsOverflowException(string value)
        {
            Assert.Throws<OverflowException>(() => Conversions.ToSByte(value));
        }

        public static IEnumerable<object[]> ToSByte_Object_TestData()
        {
            // byte.
            yield return new object[] { byte.MinValue, (sbyte)0 };
            yield return new object[] { (byte)1, (sbyte)1 };
            yield return new object[] { (ByteEnum)byte.MinValue, (sbyte)0 };
            yield return new object[] { (ByteEnum)1, (sbyte)1 };

            // sbyte.
            yield return new object[] { sbyte.MinValue, sbyte.MinValue };
            yield return new object[] { (sbyte)(-1), (sbyte)(-1) };
            yield return new object[] { (sbyte)0, (sbyte)0 };
            yield return new object[] { (sbyte)1, (sbyte)1 };
            yield return new object[] { sbyte.MaxValue, sbyte.MaxValue };
            yield return new object[] { (SByteEnum)sbyte.MinValue, sbyte.MinValue };
            yield return new object[] { (SByteEnum)(-1), (sbyte)(-1) };
            yield return new object[] { (SByteEnum)0, (sbyte)0 };
            yield return new object[] { (SByteEnum)1, (sbyte)1 };
            yield return new object[] { (SByteEnum)sbyte.MaxValue, sbyte.MaxValue };

            // ushort.
            yield return new object[] { ushort.MinValue, (sbyte)0 };
            yield return new object[] { (ushort)1, (sbyte)1 };
            yield return new object[] { (UShortEnum)ushort.MinValue, (sbyte)0 };
            yield return new object[] { (UShortEnum)1, (sbyte)1 };

            // short.
            yield return new object[] { (short)(-1), (sbyte)(-1) };
            yield return new object[] { (short)0, (sbyte)0 };
            yield return new object[] { (short)1, (sbyte)1 };
            yield return new object[] { (ShortEnum)(-1), (sbyte)(-1) };
            yield return new object[] { (ShortEnum)0, (sbyte)0 };
            yield return new object[] { (ShortEnum)1, (sbyte)1 };

            // uint.
            yield return new object[] { uint.MinValue, (sbyte)0 };
            yield return new object[] { (uint)1, (sbyte)1 };
            yield return new object[] { (UIntEnum)uint.MinValue, (sbyte)0 };
            yield return new object[] { (UIntEnum)1, (sbyte)1 };

            // int.
            yield return new object[] { -1, (sbyte)(-1) };
            yield return new object[] { 0, (sbyte)0 };
            yield return new object[] { 1, (sbyte)1 };
            yield return new object[] { (IntEnum)(-1), (sbyte)(-1) };
            yield return new object[] { (IntEnum)0, (sbyte)0 };
            yield return new object[] { (IntEnum)1, (sbyte)1 };

            // ulong.
            yield return new object[] { ulong.MinValue, (sbyte)0 };
            yield return new object[] { (ulong)1, (sbyte)1 };
            yield return new object[] { (ULongEnum)ulong.MinValue, (sbyte)0 };
            yield return new object[] { (ULongEnum)1, (sbyte)1 };

            // long.
            yield return new object[] { (long)(-1), (sbyte)(-1) };
            yield return new object[] { (long)0, (sbyte)0 };
            yield return new object[] { (long)1, (sbyte)1 };
            yield return new object[] { (LongEnum)(-1), (sbyte)(-1) };
            yield return new object[] { (LongEnum)0, (sbyte)0 };
            yield return new object[] { (LongEnum)1, (sbyte)1 };

            // float.
            yield return new object[] { (float)(-1), (sbyte)(-1) };
            yield return new object[] { (float)0, (sbyte)0 };
            yield return new object[] { (float)1, (sbyte)1 };

            // double.
            yield return new object[] { (double)(-1), (sbyte)(-1) };
            yield return new object[] { (double)0, (sbyte)0 };
            yield return new object[] { (double)1, (sbyte)1 };

            // decimal.
            yield return new object[] { (decimal)(-1), (sbyte)(-1) };
            yield return new object[] { (decimal)0, (sbyte)0 };
            yield return new object[] { (decimal)1, (sbyte)1 };

            // bool.
            yield return new object[] { true, (sbyte)(-1) };
            yield return new object[] { false, (sbyte)0 };
            if (ReflectionEmitSupported)
            {
                yield return new object[] { BoolEnum, (sbyte)0 };
            }

            // null.
            yield return new object[] { null, (sbyte)0 };
        }

        [Theory]
        [MemberData(nameof(ToSByte_Object_TestData))]
        [MemberData(nameof(ToSByte_String_TestData))]
        public void ToSByte_Object_ReturnsExpected(IConvertible value, sbyte expected)
        {
            AssertEqual(expected, Conversions.ToSByte(value));
            if (value != null)
            {
                AssertEqual(expected, Conversions.ToSByte(new ConvertibleWrapper(value)));
            }
        }

        [Theory]
        [MemberData(nameof(InvalidNumberObject_TestData))]
        [MemberData(nameof(InvalidString_TestData))]
        public void ToSByte_InvalidObject_ThrowsInvalidCastException(object value)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToSByte(value));
        }

        [Theory]
        [MemberData(nameof(InvalidBool_TestData))]
        public void ToSByte_InvalidBool_ThrowsInvalidOperationException(object value)
        {
            Assert.Throws<InvalidOperationException>(() => Conversions.ToSByte(value));
        }

        public static IEnumerable<object[]> ToSByte_OverflowObject_TestData()
        {
            yield return new object[] { byte.MaxValue };
            yield return new object[] { (ByteEnum)byte.MaxValue };
            yield return new object[] { short.MinValue };
            yield return new object[] { short.MaxValue };
            yield return new object[] { (ShortEnum)short.MinValue };
            yield return new object[] { (ShortEnum)short.MaxValue };
        }

        [Theory]
        [MemberData(nameof(ToSByte_OverflowObject_TestData))]
        [MemberData(nameof(ToShort_OverflowObject_TestData))]
        [MemberData(nameof(ToInteger_OverflowObject_TestData))]
        [MemberData(nameof(ToLong_OverflowObject_TestData))]
        [MemberData(nameof(ToSByte_OverflowString_TestData))]
        [MemberData(nameof(ToShort_OverflowString_TestData))]
        [MemberData(nameof(ToInteger_OverflowString_TestData))]
        [MemberData(nameof(ToLong_OverflowString_TestData))]
        public void ToSByte_OverflowObject_ThrowsOverflowException(object value)
        {
            Assert.Throws<OverflowException>(() => Conversions.ToSByte(value));
        }

        public static IEnumerable<object[]> ToUShort_String_TestData()
        {
            yield return new object[] { null, ushort.MinValue };
            yield return new object[] { "0", ushort.MinValue };
            yield return new object[] { "1", (ushort)1 };
            yield return new object[] { "&h5", (ushort)5 };
            yield return new object[] { "&h0", ushort.MinValue };
            yield return new object[] { "&o5", (ushort)5 };
            yield return new object[] { " &o5", (ushort)5 };
            yield return new object[] { "&o0", ushort.MinValue };
            yield return new object[] { 1.1.ToString(), (ushort)1 };
        }

        [Theory]
        [MemberData(nameof(ToUShort_String_TestData))]
        public void ToUShort_String_ReturnsExpected(string value, ushort expected)
        {
            AssertEqual(expected, Conversions.ToUShort(value));
        }

        [Theory]
        [MemberData(nameof(InvalidString_TestData))]
        public void ToUShort_InvalidString_ThrowsInvalidCastException(string value)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToUShort(value));
        }

        public static IEnumerable<object[]> ToUShort_OverflowString_TestData()
        {
            yield return new object[] { "65536" };
        }

        [Theory]
        [MemberData(nameof(ToUShort_OverflowString_TestData))]
        [MemberData(nameof(ToUInteger_OverflowString_TestData))]
        [MemberData(nameof(ToULong_OverflowString_TestData))]
        public void ToUShort_OverflowString_ThrowsOverflowException(string value)
        {
            Assert.Throws<OverflowException>(() => Conversions.ToUShort(value));
        }

        public static IEnumerable<object[]> ToUShort_Object_TestData()
        {
            // byte.
            yield return new object[] { byte.MinValue, ushort.MinValue };
            yield return new object[] { (byte)1, (ushort)1 };
            yield return new object[] { byte.MaxValue, (ushort)255 };
            yield return new object[] { (ByteEnum)byte.MinValue, ushort.MinValue };
            yield return new object[] { (ByteEnum)1, (ushort)1 };
            yield return new object[] { (ByteEnum)byte.MaxValue, (ushort)255 };

            // sbyte.
            yield return new object[] { (sbyte)0, ushort.MinValue };
            yield return new object[] { (sbyte)1, (ushort)1 };
            yield return new object[] { sbyte.MaxValue, (ushort)127 };
            yield return new object[] { (SByteEnum)0, ushort.MinValue };
            yield return new object[] { (SByteEnum)1, (ushort)1 };
            yield return new object[] { (SByteEnum)sbyte.MaxValue, (ushort)127 };

            // ushort.
            yield return new object[] { ushort.MinValue, ushort.MinValue };
            yield return new object[] { (ushort)1, (ushort)1 };
            yield return new object[] { ushort.MaxValue, ushort.MaxValue };
            yield return new object[] { (UShortEnum)ushort.MinValue, ushort.MinValue };
            yield return new object[] { (UShortEnum)1, (ushort)1 };
            yield return new object[] { (UShortEnum)ushort.MaxValue, ushort.MaxValue };

            // short.
            yield return new object[] { (short)0, ushort.MinValue };
            yield return new object[] { (short)1, (ushort)1 };
            yield return new object[] { short.MaxValue, (ushort)32767 };
            yield return new object[] { (ShortEnum)0, ushort.MinValue };
            yield return new object[] { (ShortEnum)1, (ushort)1 };
            yield return new object[] { (ShortEnum)short.MaxValue, (ushort)32767 };

            // uint.
            yield return new object[] { uint.MinValue, ushort.MinValue };
            yield return new object[] { (uint)1, (ushort)1 };
            yield return new object[] { (UIntEnum)uint.MinValue, ushort.MinValue };
            yield return new object[] { (UIntEnum)1, (ushort)1 };

            // int.
            yield return new object[] { 0, ushort.MinValue };
            yield return new object[] { 1, (ushort)1 };
            yield return new object[] { (IntEnum)0, ushort.MinValue };
            yield return new object[] { (IntEnum)1, (ushort)1 };

            // ulong.
            yield return new object[] { ulong.MinValue, ushort.MinValue };
            yield return new object[] { (ulong)1, (ushort)1 };
            yield return new object[] { (ULongEnum)ulong.MinValue, ushort.MinValue };
            yield return new object[] { (ULongEnum)1, (ushort)1 };

            // long.
            yield return new object[] { (long)0, ushort.MinValue };
            yield return new object[] { (long)1, (ushort)1 };
            yield return new object[] { (LongEnum)0, ushort.MinValue };
            yield return new object[] { (LongEnum)1, (ushort)1 };

            // float.
            yield return new object[] { (float)0, ushort.MinValue };
            yield return new object[] { (float)1, (ushort)1 };

            // double.
            yield return new object[] { (double)0, ushort.MinValue };
            yield return new object[] { (double)1, (ushort)1 };

            // decimal.
            yield return new object[] { (decimal)0, ushort.MinValue };
            yield return new object[] { (decimal)1, (ushort)1 };

            // bool.
            yield return new object[] { true, ushort.MaxValue };
            yield return new object[] { false, ushort.MinValue };
            if (ReflectionEmitSupported)
            {
                yield return new object[] { BoolEnum, ushort.MinValue };
            }

            // null.
            yield return new object[] { null, ushort.MinValue };
        }

        [Theory]
        [MemberData(nameof(ToUShort_Object_TestData))]
        [MemberData(nameof(ToUShort_String_TestData))]
        public void ToUShort_Object_ReturnsExpected(IConvertible value, ushort expected)
        {
            AssertEqual(expected, Conversions.ToUShort(value));
            if (value != null)
            {
                AssertEqual(expected, Conversions.ToUShort(new ConvertibleWrapper(value)));
            }
        }

        [Theory]
        [MemberData(nameof(InvalidNumberObject_TestData))]
        [MemberData(nameof(InvalidString_TestData))]
        public void ToUShort_InvalidObject_ThrowsInvalidCastException(object value)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToUShort(value));
        }

        [Theory]
        [MemberData(nameof(InvalidBool_TestData))]
        public void ToUShort_InvalidBool_ThrowsInvalidOperationException(object value)
        {
            Assert.Throws<InvalidOperationException>(() => Conversions.ToUShort(value));
        }

        public static IEnumerable<object[]> ToUShort_OverflowObject_TestData()
        {
            yield return new object[] { int.MaxValue };
            yield return new object[] { (IntEnum)int.MaxValue };
            yield return new object[] { uint.MaxValue };
            yield return new object[] { (UIntEnum)uint.MaxValue };
        }

        [Theory]
        [MemberData(nameof(ToUShort_OverflowString_TestData))]
        [MemberData(nameof(ToUInteger_OverflowString_TestData))]
        [MemberData(nameof(ToULong_OverflowString_TestData))]
        [MemberData(nameof(ToUShort_OverflowObject_TestData))]
        [MemberData(nameof(ToUInteger_OverflowObject_TestData))]
        [MemberData(nameof(ToULong_OverflowObject_TestData))]
        public void ToUShort_OverflowObject_ThrowsOverflowException(object value)
        {
            Assert.Throws<OverflowException>(() => Conversions.ToUShort(value));
        }

        public static IEnumerable<object[]> ToShort_String_TestData()
        {
            yield return new object[] { null, (short)0 };
            yield return new object[] { "-1", (short)(-1) };
            yield return new object[] { "0", (short)0 };
            yield return new object[] { "1", (short)1 };
            yield return new object[] { "&h5", (short)5 };
            yield return new object[] { "&h0", (short)0 };
            yield return new object[] { "&o5", (short)5 };
            yield return new object[] { " &o5", (short)5 };
            yield return new object[] { "&o0", (short)0 };
            yield return new object[] { 1.1.ToString(), (short)1 };
        }

        [Theory]
        [MemberData(nameof(ToShort_String_TestData))]
        public void ToShort_String_ReturnsExpected(string value, short expected)
        {
            AssertEqual(expected, Conversions.ToShort(value));
        }

        [Theory]
        [MemberData(nameof(InvalidString_TestData))]
        public void ToShort_InvalidString_ThrowsInvalidCastException(string value)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToShort(value));
        }

        public static IEnumerable<object[]> ToShort_OverflowString_TestData()
        {
            yield return new object[] { "32768" };
        }

        [Theory]
        [MemberData(nameof(ToShort_OverflowString_TestData))]
        [MemberData(nameof(ToInteger_OverflowString_TestData))]
        [MemberData(nameof(ToLong_OverflowString_TestData))]
        public void ToShort_OverflowString_ThrowsOverflowException(string value)
        {
            Assert.Throws<OverflowException>(() => Conversions.ToShort(value));
        }

        public static IEnumerable<object[]> ToShort_Object_TestData()
        {
            // byte.
            yield return new object[] { byte.MinValue, (short)0 };
            yield return new object[] { (byte)1, (short)1 };
            yield return new object[] { byte.MaxValue, (short)255 };
            yield return new object[] { (ByteEnum)byte.MinValue, (short)0 };
            yield return new object[] { (ByteEnum)1, (short)1 };
            yield return new object[] { (ByteEnum)byte.MaxValue, (short)255 };

            // sbyte.
            yield return new object[] { sbyte.MinValue, (short)(-128) };
            yield return new object[] { (sbyte)(-1), (short)(-1) };
            yield return new object[] { (sbyte)0, (short)0 };
            yield return new object[] { (sbyte)1, (short)1 };
            yield return new object[] { sbyte.MaxValue, (short)127 };
            yield return new object[] { (SByteEnum)sbyte.MinValue, (short)(-128) };
            yield return new object[] { (SByteEnum)(-1), (short)(-1) };
            yield return new object[] { (SByteEnum)0, (short)0 };
            yield return new object[] { (SByteEnum)1, (short)1 };
            yield return new object[] { (SByteEnum)sbyte.MaxValue, (short)127 };

            // ushort.
            yield return new object[] { ushort.MinValue, (short)0 };
            yield return new object[] { (ushort)1, (short)1 };
            yield return new object[] { (UShortEnum)ushort.MinValue, (short)0 };
            yield return new object[] { (UShortEnum)1, (short)1 };

            // short.
            yield return new object[] { short.MinValue, short.MinValue };
            yield return new object[] { (short)(-1), (short)(-1) };
            yield return new object[] { (short)0, (short)0 };
            yield return new object[] { (short)1, (short)1 };
            yield return new object[] { short.MaxValue, short.MaxValue };
            yield return new object[] { (ShortEnum)short.MinValue, short.MinValue };
            yield return new object[] { (ShortEnum)(-1), (short)(-1) };
            yield return new object[] { (ShortEnum)0, (short)0 };
            yield return new object[] { (ShortEnum)1, (short)1 };
            yield return new object[] { (ShortEnum)short.MaxValue, short.MaxValue };

            // uint.
            yield return new object[] { uint.MinValue, (short)0 };
            yield return new object[] { (uint)1, (short)1 };
            yield return new object[] { (UIntEnum)uint.MinValue, (short)0 };
            yield return new object[] { (UIntEnum)1, (short)1 };

            // int.
            yield return new object[] { -1, (short)(-1) };
            yield return new object[] { 0, (short)0 };
            yield return new object[] { 1, (short)1 };
            yield return new object[] { (IntEnum)(-1), (short)(-1) };
            yield return new object[] { (IntEnum)0, (short)0 };
            yield return new object[] { (IntEnum)1, (short)1 };

            // ulong.
            yield return new object[] { ulong.MinValue, (short)0 };
            yield return new object[] { (ulong)1, (short)1 };
            yield return new object[] { (ULongEnum)ulong.MinValue, (short)0 };
            yield return new object[] { (ULongEnum)1, (short)1 };

            // long.
            yield return new object[] { (long)(-1), (short)(-1) };
            yield return new object[] { (long)0, (short)0 };
            yield return new object[] { (long)1, (short)1 };
            yield return new object[] { (LongEnum)(-1), (short)(-1) };
            yield return new object[] { (LongEnum)0, (short)0 };
            yield return new object[] { (LongEnum)1, (short)1 };

            // float.
            yield return new object[] { (float)(-1), (short)(-1) };
            yield return new object[] { (float)0, (short)0 };
            yield return new object[] { (float)1, (short)1 };

            // double.
            yield return new object[] { (double)(-1), (short)(-1) };
            yield return new object[] { (double)0, (short)0 };
            yield return new object[] { (double)1, (short)1 };

            // decimal.
            yield return new object[] { (decimal)(-1), (short)(-1) };
            yield return new object[] { (decimal)0, (short)0 };
            yield return new object[] { (decimal)1, (short)1 };

            // bool.
            yield return new object[] { true, (short)(-1) };
            yield return new object[] { false, (short)0 };
            if (ReflectionEmitSupported)
            {
                yield return new object[] { BoolEnum, (short)0 };
            }

            // null.
            yield return new object[] { null, (short)0 };
        }

        [Theory]
        [MemberData(nameof(ToShort_Object_TestData))]
        [MemberData(nameof(ToShort_String_TestData))]
        public void ToShort_Object_ReturnsExpected(IConvertible value, short expected)
        {
            AssertEqual(expected, Conversions.ToShort(value));
            if (value != null)
            {
                AssertEqual(expected, Conversions.ToShort(new ConvertibleWrapper(value)));
            }
        }

        [Theory]
        [MemberData(nameof(InvalidNumberObject_TestData))]
        [MemberData(nameof(InvalidString_TestData))]
        public void ToShort_InvalidObject_ThrowsInvalidCastException(object value)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToShort(value));
        }

        [Theory]
        [MemberData(nameof(InvalidBool_TestData))]
        public void ToShort_InvalidBool_ThrowsInvalidOperationException(object value)
        {
            Assert.Throws<InvalidOperationException>(() => Conversions.ToShort(value));
        }

        public static IEnumerable<object[]> ToShort_OverflowObject_TestData()
        {
            yield return new object[] { ushort.MaxValue };
            yield return new object[] { (UShortEnum)ushort.MaxValue };
            yield return new object[] { int.MinValue };
            yield return new object[] { int.MaxValue };
            yield return new object[] { (IntEnum)int.MinValue };
            yield return new object[] { (IntEnum)int.MaxValue };
        }

        [Theory]
        [MemberData(nameof(ToShort_OverflowObject_TestData))]
        [MemberData(nameof(ToInteger_OverflowObject_TestData))]
        [MemberData(nameof(ToLong_OverflowObject_TestData))]
        [MemberData(nameof(ToShort_OverflowString_TestData))]
        [MemberData(nameof(ToInteger_OverflowString_TestData))]
        [MemberData(nameof(ToLong_OverflowString_TestData))]
        public void ToShort_OverflowObject_ThrowsOverflowException(object value)
        {
            Assert.Throws<OverflowException>(() => Conversions.ToShort(value));
        }

        public static IEnumerable<object[]> ToUInteger_String_TestData()
        {
            yield return new object[] { null, uint.MinValue };
            yield return new object[] { "0", uint.MinValue };
            yield return new object[] { "1", (uint)1 };
            yield return new object[] { "&h5", (uint)5 };
            yield return new object[] { "&h0", uint.MinValue };
            yield return new object[] { "&o5", (uint)5 };
            yield return new object[] { " &o5", (uint)5 };
            yield return new object[] { "&o0", uint.MinValue };
            yield return new object[] { 1.1.ToString(), (uint)1 };
        }

        [Theory]
        [MemberData(nameof(ToUInteger_String_TestData))]
        public void ToUInteger_String_ReturnsExpected(string value, uint expected)
        {
            AssertEqual(expected, Conversions.ToUInteger(value));
        }

        [Theory]
        [MemberData(nameof(InvalidString_TestData))]
        public void ToUInteger_InvalidString_ThrowsInvalidCastException(string value)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToUInteger(value));
        }

        public static IEnumerable<object[]> ToUInteger_OverflowString_TestData()
        {
            yield return new object[] { "4294967296" };
            yield return new object[] { double.PositiveInfinity.ToString() };
            yield return new object[] { double.NegativeInfinity.ToString() };
            yield return new object[] { double.NaN.ToString() };
        }

        [Theory]
        [MemberData(nameof(ToUInteger_OverflowString_TestData))]
        [MemberData(nameof(ToULong_OverflowString_TestData))]
        public void ToUInteger_OverflowString_ThrowsOverflowException(string value)
        {
            Assert.Throws<OverflowException>(() => Conversions.ToUInteger(value));
        }

        public static IEnumerable<object[]> ToUInteger_Object_TestData()
        {
            // byte.
            yield return new object[] { byte.MinValue, uint.MinValue };
            yield return new object[] { (byte)1, (uint)1 };
            yield return new object[] { byte.MaxValue, (uint)255 };
            yield return new object[] { (ByteEnum)byte.MinValue, uint.MinValue };
            yield return new object[] { (ByteEnum)1, (uint)1 };
            yield return new object[] { (ByteEnum)byte.MaxValue, (uint)255 };

            // sbyte.
            yield return new object[] { (sbyte)0, uint.MinValue };
            yield return new object[] { (sbyte)1, (uint)1 };
            yield return new object[] { sbyte.MaxValue, (uint)127 };
            yield return new object[] { (SByteEnum)0, uint.MinValue };
            yield return new object[] { (SByteEnum)1, (uint)1 };
            yield return new object[] { (SByteEnum)sbyte.MaxValue, (uint)127 };

            // ushort.
            yield return new object[] { ushort.MinValue, uint.MinValue };
            yield return new object[] { (ushort)1, (uint)1 };
            yield return new object[] { ushort.MaxValue, (uint)65535 };
            yield return new object[] { (UShortEnum)ushort.MinValue, uint.MinValue };
            yield return new object[] { (UShortEnum)1, (uint)1 };
            yield return new object[] { (UShortEnum)ushort.MaxValue, (uint)65535 };

            // short.
            yield return new object[] { (short)0, uint.MinValue };
            yield return new object[] { (short)1, (uint)1 };
            yield return new object[] { short.MaxValue, (uint)32767 };
            yield return new object[] { (ShortEnum)0, uint.MinValue };
            yield return new object[] { (ShortEnum)1, (uint)1 };
            yield return new object[] { (ShortEnum)short.MaxValue, (uint)32767 };

            // uint.
            yield return new object[] { uint.MinValue, uint.MinValue };
            yield return new object[] { (uint)1, (uint)1 };
            yield return new object[] { uint.MaxValue, uint.MaxValue };
            yield return new object[] { (UIntEnum)uint.MinValue, uint.MinValue };
            yield return new object[] { (UIntEnum)1, (uint)1 };
            yield return new object[] { (UIntEnum)uint.MaxValue, uint.MaxValue };

            // int.
            yield return new object[] { 0, uint.MinValue };
            yield return new object[] { 1, (uint)1 };
            yield return new object[] { int.MaxValue, (uint)2147483647 };
            yield return new object[] { (IntEnum)0, uint.MinValue };
            yield return new object[] { (IntEnum)1, (uint)1 };
            yield return new object[] { (IntEnum)int.MaxValue, (uint)2147483647 };

            // ulong.
            yield return new object[] { ulong.MinValue, uint.MinValue };
            yield return new object[] { (ulong)1, (uint)1 };
            yield return new object[] { (ULongEnum)ulong.MinValue, uint.MinValue };
            yield return new object[] { (ULongEnum)1, (uint)1 };

            // long.
            yield return new object[] { (long)0, uint.MinValue };
            yield return new object[] { (long)1, (uint)1 };
            yield return new object[] { (LongEnum)0, uint.MinValue };
            yield return new object[] { (LongEnum)1, (uint)1 };

            // float.
            yield return new object[] { (float)0, uint.MinValue };
            yield return new object[] { (float)1, (uint)1 };

            // double.
            yield return new object[] { (double)0, uint.MinValue };
            yield return new object[] { (double)1, (uint)1 };

            // decimal.
            yield return new object[] { (decimal)0, uint.MinValue };
            yield return new object[] { (decimal)1, (uint)1 };

            // bool.
            yield return new object[] { true, uint.MaxValue };
            yield return new object[] { false, uint.MinValue };
            if (ReflectionEmitSupported)
            {
                yield return new object[] { BoolEnum, uint.MinValue };
            }

            // null.
            yield return new object[] { null, uint.MinValue };
        }

        [Theory]
        [MemberData(nameof(ToUInteger_Object_TestData))]
        [MemberData(nameof(ToUInteger_String_TestData))]
        public void ToUInteger_Object_ReturnsExpected(IConvertible value, uint expected)
        {
            AssertEqual(expected, Conversions.ToUInteger(value));
            if (value != null)
            {
                AssertEqual(expected, Conversions.ToUInteger(new ConvertibleWrapper(value)));
            }
        }

        [Theory]
        [MemberData(nameof(InvalidNumberObject_TestData))]
        [MemberData(nameof(InvalidString_TestData))]
        public void ToUInteger_InvalidObject_ThrowsInvalidCastException(object value)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToUInteger(value));
        }

        [Theory]
        [MemberData(nameof(InvalidBool_TestData))]
        public void ToUInteger_InvalidBool_ThrowsInvalidOperationException(object value)
        {
            Assert.Throws<InvalidOperationException>(() => Conversions.ToUInteger(value));
        }

        public static IEnumerable<object[]> ToUInteger_OverflowObject_TestData()
        {
            yield return new object[] { ulong.MaxValue };
            yield return new object[] { (ULongEnum)ulong.MaxValue };
            yield return new object[] { long.MaxValue };
            yield return new object[] { (LongEnum)long.MaxValue };
        }

        [Theory]
        [MemberData(nameof(ToUInteger_OverflowObject_TestData))]
        [MemberData(nameof(ToULong_OverflowObject_TestData))]
        [MemberData(nameof(ToUInteger_OverflowString_TestData))]
        [MemberData(nameof(ToULong_OverflowString_TestData))]
        public void ToUInteger_OverflowObject_ThrowsOverflowException(object value)
        {
            Assert.Throws<OverflowException>(() => Conversions.ToUInteger(value));
        }

        public static IEnumerable<object[]> ToInteger_String_TestData()
        {
            yield return new object[] { null, 0 };
            yield return new object[] { "-1", -1 };
            yield return new object[] { "0", 0 };
            yield return new object[] { "1", 1 };
            yield return new object[] { "&h5", 5 };
            yield return new object[] { "&h0", 0 };
            yield return new object[] { "&o5", 5 };
            yield return new object[] { " &o5", 5 };
            yield return new object[] { "&o0", 0 };
            yield return new object[] { 1.1.ToString(), 1 };
        }

        [Theory]
        [MemberData(nameof(ToInteger_String_TestData))]
        public void ToInteger_String_ReturnsExpected(string value, int expected)
        {
            AssertEqual(expected, Conversions.ToInteger(value));
        }

        [Theory]
        [MemberData(nameof(InvalidString_TestData))]
        public void ToInteger_InvalidString_ThrowsInvalidCastException(string value)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToInteger(value));
        }

        public static IEnumerable<object[]> ToInteger_OverflowString_TestData()
        {
            yield return new object[] { "2147483648" };
            yield return new object[] { double.PositiveInfinity.ToString() };
            yield return new object[] { double.NegativeInfinity.ToString() };
            yield return new object[] { double.NaN.ToString() };
        }

        [Theory]
        [MemberData(nameof(ToInteger_OverflowString_TestData))]
        [MemberData(nameof(ToLong_OverflowString_TestData))]
        public void ToInteger_OverflowString_ThrowsOverflowException(string value)
        {
            Assert.Throws<OverflowException>(() => Conversions.ToInteger(value));
        }

        public static IEnumerable<object[]> ToInteger_Object_TestData()
        {
            // byte.
            yield return new object[] { byte.MinValue, 0 };
            yield return new object[] { (byte)1, 1 };
            yield return new object[] { byte.MaxValue, 255 };
            yield return new object[] { (ByteEnum)byte.MinValue, 0 };
            yield return new object[] { (ByteEnum)1, 1 };
            yield return new object[] { (ByteEnum)byte.MaxValue, 255 };

            // sbyte.
            yield return new object[] { sbyte.MinValue, -128 };
            yield return new object[] { (sbyte)(-1), -1 };
            yield return new object[] { (sbyte)0, 0 };
            yield return new object[] { (sbyte)1, 1 };
            yield return new object[] { sbyte.MaxValue, 127 };
            yield return new object[] { (SByteEnum)sbyte.MinValue, -128 };
            yield return new object[] { (SByteEnum)(-1), -1 };
            yield return new object[] { (SByteEnum)0, 0 };
            yield return new object[] { (SByteEnum)1, 1 };
            yield return new object[] { (SByteEnum)sbyte.MaxValue, 127 };

            // ushort.
            yield return new object[] { ushort.MinValue, 0 };
            yield return new object[] { (ushort)1, 1 };
            yield return new object[] { ushort.MaxValue, 65535 };
            yield return new object[] { (UShortEnum)ushort.MinValue, 0 };
            yield return new object[] { (UShortEnum)1, 1 };
            yield return new object[] { (UShortEnum)ushort.MaxValue, 65535 };

            // short.
            yield return new object[] { short.MinValue, -32768 };
            yield return new object[] { (short)(-1), -1 };
            yield return new object[] { (short)0, 0 };
            yield return new object[] { (short)1, 1 };
            yield return new object[] { short.MaxValue, 32767 };
            yield return new object[] { (ShortEnum)short.MinValue, -32768 };
            yield return new object[] { (ShortEnum)(-1), -1 };
            yield return new object[] { (ShortEnum)0, 0 };
            yield return new object[] { (ShortEnum)1, 1 };
            yield return new object[] { (ShortEnum)short.MaxValue, 32767 };

            // uint.
            yield return new object[] { uint.MinValue, 0 };
            yield return new object[] { (uint)1, 1 };
            yield return new object[] { (UIntEnum)uint.MinValue, 0 };
            yield return new object[] { (UIntEnum)1, 1 };

            // int.
            yield return new object[] { int.MinValue, int.MinValue };
            yield return new object[] { -1, -1 };
            yield return new object[] { 0, 0 };
            yield return new object[] { 1, 1 };
            yield return new object[] { int.MaxValue, int.MaxValue };
            yield return new object[] { (IntEnum)int.MinValue, int.MinValue };
            yield return new object[] { (IntEnum)(-1), -1 };
            yield return new object[] { (IntEnum)0, 0 };
            yield return new object[] { (IntEnum)1, 1 };
            yield return new object[] { (IntEnum)int.MaxValue, int.MaxValue };

            // ulong.
            yield return new object[] { ulong.MinValue, 0 };
            yield return new object[] { (ulong)1, 1 };
            yield return new object[] { (ULongEnum)ulong.MinValue, 0 };
            yield return new object[] { (ULongEnum)1, 1 };

            // long.
            yield return new object[] { (long)(-1), -1 };
            yield return new object[] { (long)0, 0 };
            yield return new object[] { (long)1, 1 };
            yield return new object[] { (LongEnum)(-1), -1 };
            yield return new object[] { (LongEnum)0, 0 };
            yield return new object[] { (LongEnum)1, 1 };

            // float.
            yield return new object[] { (float)(-1), -1 };
            yield return new object[] { (float)0, 0 };
            yield return new object[] { (float)1, 1 };

            // double.
            yield return new object[] { (double)(-1), -1 };
            yield return new object[] { (double)0, 0 };
            yield return new object[] { (double)1, 1 };

            // decimal.
            yield return new object[] { (decimal)(-1), -1 };
            yield return new object[] { (decimal)0, 0 };
            yield return new object[] { (decimal)1, 1 };

            // bool.
            yield return new object[] { true, -1 };
            yield return new object[] { false, 0 };
            if (ReflectionEmitSupported)
            {
                yield return new object[] { BoolEnum, 0 };
            }

            // null.
            yield return new object[] { null, 0 };
        }

        [Theory]
        [MemberData(nameof(ToInteger_Object_TestData))]
        [MemberData(nameof(ToInteger_String_TestData))]
        public void ToInteger_Object_ReturnsExpected(IConvertible value, int expected)
        {
            AssertEqual(expected, Conversions.ToInteger(value));
            if (value != null)
            {
                AssertEqual(expected, Conversions.ToInteger(new ConvertibleWrapper(value)));
            }
        }

        [Theory]
        [MemberData(nameof(InvalidNumberObject_TestData))]
        [MemberData(nameof(InvalidString_TestData))]
        public void ToInteger_InvalidObject_ThrowsInvalidCastException(object value)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToInteger(value));
        }

        [Theory]
        [MemberData(nameof(InvalidBool_TestData))]
        public void ToInteger_InvalidBool_ThrowsInvalidOperationException(object value)
        {
            Assert.Throws<InvalidOperationException>(() => Conversions.ToInteger(value));
        }

        public static IEnumerable<object[]> ToInteger_OverflowObject_TestData()
        {
            yield return new object[] { uint.MaxValue };
            yield return new object[] { (UIntEnum)uint.MaxValue };
            yield return new object[] { long.MinValue };
            yield return new object[] { long.MaxValue };
            yield return new object[] { (LongEnum)long.MinValue };
            yield return new object[] { (LongEnum)long.MaxValue };
        }

        [Theory]
        [MemberData(nameof(ToInteger_OverflowObject_TestData))]
        [MemberData(nameof(ToLong_OverflowObject_TestData))]
        [MemberData(nameof(ToInteger_OverflowString_TestData))]
        [MemberData(nameof(ToLong_OverflowString_TestData))]
        public void ToInteger_OverflowObject_ThrowsOverflowException(object value)
        {
            Assert.Throws<OverflowException>(() => Conversions.ToInteger(value));
        }

        public static IEnumerable<object[]> ToULong_String_TestData()
        {
            yield return new object[] { null, ulong.MinValue };
            yield return new object[] { "0", ulong.MinValue };
            yield return new object[] { "1", (ulong)1 };
            yield return new object[] { "&h5", (ulong)5 };
            yield return new object[] { "&h0", ulong.MinValue };
            yield return new object[] { "&o5", (ulong)5 };
            yield return new object[] { " &o5", (ulong)5 };
            yield return new object[] { "&o0", ulong.MinValue };
            yield return new object[] { 1.1.ToString(), (ulong)1 };
        }

        [Theory]
        [MemberData(nameof(ToULong_String_TestData))]
        public void ToULong_String_ReturnsExpected(string value, ulong expected)
        {
            AssertEqual(expected, Conversions.ToULong(value));
        }

        public static IEnumerable<object[]> ToULong_InvalidString_TestData()
        {
            yield return new object[] { double.PositiveInfinity.ToString() };
            yield return new object[] { double.NegativeInfinity.ToString() };
            yield return new object[] { double.NaN.ToString() };
        }

        [Theory]
        [MemberData(nameof(ToULong_InvalidString_TestData))]
        [MemberData(nameof(InvalidString_TestData))]
        public void ToULong_InvalidString_ThrowsInvalidCastException(string value)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToULong(value));
        }

        public static IEnumerable<object[]> ToULong_OverflowString_TestData()
        {
            yield return new object[] { "-1" };
            yield return new object[] { "18446744073709551616" };
            yield return new object[] { "1844674407370955161618446744073709551616" };
        }

        [Theory]
        [MemberData(nameof(ToULong_OverflowString_TestData))]
        public void ToULong_OverflowString_ThrowsOverflowException(string value)
        {
            Assert.Throws<OverflowException>(() => Conversions.ToULong(value));
        }

        public static IEnumerable<object[]> ToULong_Object_TestData()
        {
            // byte.
            yield return new object[] { byte.MinValue, ulong.MinValue };
            yield return new object[] { (byte)1, (ulong)1 };
            yield return new object[] { byte.MaxValue, (ulong)255 };
            yield return new object[] { (ByteEnum)byte.MinValue, ulong.MinValue };
            yield return new object[] { (ByteEnum)1, (ulong)1 };
            yield return new object[] { (ByteEnum)byte.MaxValue, (ulong)255 };

            // sbyte.
            yield return new object[] { (sbyte)0, ulong.MinValue };
            yield return new object[] { (sbyte)1, (ulong)1 };
            yield return new object[] { sbyte.MaxValue, (ulong)127 };
            yield return new object[] { (SByteEnum)0, ulong.MinValue };
            yield return new object[] { (SByteEnum)1, (ulong)1 };
            yield return new object[] { (SByteEnum)sbyte.MaxValue, (ulong)127 };

            // ushort.
            yield return new object[] { ushort.MinValue, ulong.MinValue };
            yield return new object[] { (ushort)1, (ulong)1 };
            yield return new object[] { ushort.MaxValue, (ulong)65535 };
            yield return new object[] { (UShortEnum)ushort.MinValue, ulong.MinValue };
            yield return new object[] { (UShortEnum)1, (ulong)1 };
            yield return new object[] { (UShortEnum)ushort.MaxValue, (ulong)65535 };

            // short.
            yield return new object[] { (short)0, ulong.MinValue };
            yield return new object[] { (short)1, (ulong)1 };
            yield return new object[] { short.MaxValue, (ulong)32767 };
            yield return new object[] { (ShortEnum)0, ulong.MinValue };
            yield return new object[] { (ShortEnum)1, (ulong)1 };
            yield return new object[] { (ShortEnum)short.MaxValue, (ulong)32767 };

            // uint.
            yield return new object[] { uint.MinValue, ulong.MinValue };
            yield return new object[] { (uint)1, (ulong)1 };
            yield return new object[] { uint.MaxValue, (ulong)4294967295 };
            yield return new object[] { (UIntEnum)uint.MinValue, ulong.MinValue };
            yield return new object[] { (UIntEnum)1, (ulong)1 };
            yield return new object[] { (UIntEnum)uint.MaxValue, (ulong)4294967295 };

            // int.
            yield return new object[] { 0, ulong.MinValue };
            yield return new object[] { 1, (ulong)1 };
            yield return new object[] { int.MaxValue, (ulong)2147483647 };
            yield return new object[] { (IntEnum)0, ulong.MinValue };
            yield return new object[] { (IntEnum)1, (ulong)1 };
            yield return new object[] { (IntEnum)int.MaxValue, (ulong)2147483647 };

            // ulong.
            yield return new object[] { ulong.MinValue, ulong.MinValue };
            yield return new object[] { (ulong)1, (ulong)1 };
            yield return new object[] { ulong.MaxValue, ulong.MaxValue };
            yield return new object[] { (ULongEnum)ulong.MinValue, ulong.MinValue };
            yield return new object[] { (ULongEnum)1, (ulong)1 };
            yield return new object[] { (ULongEnum)ulong.MaxValue, ulong.MaxValue };

            // long.
            yield return new object[] { (long)0, ulong.MinValue };
            yield return new object[] { (long)1, (ulong)1 };
            yield return new object[] { long.MaxValue, (ulong)9223372036854775807 };
            yield return new object[] { (LongEnum)0, ulong.MinValue };
            yield return new object[] { (LongEnum)1, (ulong)1 };
            yield return new object[] { (LongEnum)long.MaxValue, (ulong)9223372036854775807 };

            // float.
            yield return new object[] { (float)0, ulong.MinValue };
            yield return new object[] { (float)1, (ulong)1 };

            // double.
            yield return new object[] { (double)0, ulong.MinValue };
            yield return new object[] { (double)1, (ulong)1 };

            // decimal.
            yield return new object[] { (decimal)0, ulong.MinValue };
            yield return new object[] { (decimal)1, (ulong)1 };

            // bool.
            yield return new object[] { true, ulong.MaxValue };
            yield return new object[] { false, ulong.MinValue };
            if (ReflectionEmitSupported)
            {
                yield return new object[] { BoolEnum, ulong.MinValue };
            }

            // null.
            yield return new object[] { null, ulong.MinValue };
        }

        [Theory]
        [MemberData(nameof(ToULong_Object_TestData))]
        [MemberData(nameof(ToULong_String_TestData))]
        public void ToULong_Object_ReturnsExpected(IConvertible value, ulong expected)
        {
            AssertEqual(expected, Conversions.ToULong(value));
            if (value != null)
            {
                AssertEqual(expected, Conversions.ToULong(new ConvertibleWrapper(value)));
            }
        }

        [Theory]
        [MemberData(nameof(ToULong_InvalidString_TestData))]
        [MemberData(nameof(InvalidNumberObject_TestData))]
        [MemberData(nameof(InvalidString_TestData))]
        public void ToULong_InvalidObject_ThrowsInvalidCastException(object value)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToULong(value));
        }

        [Theory]
        [MemberData(nameof(InvalidBool_TestData))]
        public void ToULong_InvalidBool_ThrowsInvalidOperationException(object value)
        {
            Assert.Throws<InvalidOperationException>(() => Conversions.ToULong(value));
        }

        public static IEnumerable<object[]> ToULong_OverflowObject_TestData()
        {
            yield return new object[] { sbyte.MinValue };
            yield return new object[] { (sbyte)(-1) };
            yield return new object[] { (SByteEnum)sbyte.MinValue };
            yield return new object[] { (SByteEnum)(-1) };
            yield return new object[] { short.MinValue };
            yield return new object[] { (short)(-1) };
            yield return new object[] { (ShortEnum)short.MinValue };
            yield return new object[] { (ShortEnum)(-1) };
            yield return new object[] { int.MinValue };
            yield return new object[] { -1 };
            yield return new object[] { (IntEnum)int.MinValue };
            yield return new object[] { (IntEnum)(-1) };
            yield return new object[] { long.MinValue };
            yield return new object[] { (long)(-1) };
            yield return new object[] { (LongEnum)long.MinValue };
            yield return new object[] { (LongEnum)(-1) };
            yield return new object[] { float.MinValue };
            yield return new object[] { (float)(-1) };
            yield return new object[] { float.MaxValue };
            yield return new object[] { float.PositiveInfinity };
            yield return new object[] { float.NegativeInfinity };
            yield return new object[] { float.NaN };
            yield return new object[] { double.MinValue };
            yield return new object[] { (double)(-1) };
            yield return new object[] { double.MaxValue };
            yield return new object[] { double.PositiveInfinity };
            yield return new object[] { double.NegativeInfinity };
            yield return new object[] { double.NaN };
            yield return new object[] { decimal.MinValue };
            yield return new object[] { (decimal)(-1) };
            yield return new object[] { decimal.MaxValue };
        }

        [Theory]
        [MemberData(nameof(ToULong_OverflowObject_TestData))]
        [MemberData(nameof(ToULong_OverflowString_TestData))]
        public void ToULong_OverflowObject_ThrowsOverflowException(object value)
        {
            Assert.Throws<OverflowException>(() => Conversions.ToULong(value));
        }

        public static IEnumerable<object[]> ToLong_String_TestData()
        {
            yield return new object[] { null, (long)0 };
            yield return new object[] { "-1", (long)(-1) };
            yield return new object[] { "0", (long)0 };
            yield return new object[] { "1", (long)1 };
            yield return new object[] { "&h5", (long)5 };
            yield return new object[] { "&h0", (long)0 };
            yield return new object[] { "&o5", (long)5 };
            yield return new object[] { " &o5", (long)5 };
            yield return new object[] { "&o0", (long)0 };
            yield return new object[] { 1.1.ToString(), (long)1 };
        }

        [Theory]
        [MemberData(nameof(ToLong_String_TestData))]
        public void ToLong_String_ReturnsExpected(string value, long expected)
        {
            AssertEqual(expected, Conversions.ToLong(value));
        }

        public static IEnumerable<object[]> ToLong_InvalidString_TestData()
        {
            yield return new object[] { double.PositiveInfinity.ToString() };
            yield return new object[] { double.NegativeInfinity.ToString() };
            yield return new object[] { double.NaN.ToString() };
        }

        [Theory]
        [MemberData(nameof(ToLong_InvalidString_TestData))]
        [MemberData(nameof(InvalidString_TestData))]
        public void ToLong_InvalidString_ThrowsInvalidCastException(string value)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToLong(value));
        }

        public static IEnumerable<object[]> ToLong_OverflowString_TestData()
        {
            yield return new object[] { "9223372036854775808" };
            yield return new object[] { "18446744073709551616" };
            yield return new object[] { "1844674407370955161618446744073709551616" };
        }

        [Theory]
        [MemberData(nameof(ToLong_OverflowString_TestData))]
        public void ToLong_OverflowString_ThrowsOverflowException(string value)
        {
            Assert.Throws<OverflowException>(() => Conversions.ToLong(value));
        }

        public static IEnumerable<object[]> ToLong_Object_TestData()
        {
            // byte.
            yield return new object[] { byte.MinValue, (long)0 };
            yield return new object[] { (byte)1, (long)1 };
            yield return new object[] { byte.MaxValue, (long)255 };
            yield return new object[] { (ByteEnum)byte.MinValue, (long)0 };
            yield return new object[] { (ByteEnum)1, (long)1 };
            yield return new object[] { (ByteEnum)byte.MaxValue, (long)255 };

            // sbyte.
            yield return new object[] { sbyte.MinValue, (long)(-128) };
            yield return new object[] { (sbyte)(-1), (long)(-1) };
            yield return new object[] { (sbyte)0, (long)0 };
            yield return new object[] { (sbyte)1, (long)1 };
            yield return new object[] { sbyte.MaxValue, (long)127 };
            yield return new object[] { (SByteEnum)sbyte.MinValue, (long)(-128) };
            yield return new object[] { (SByteEnum)(-1), (long)(-1) };
            yield return new object[] { (SByteEnum)0, (long)0 };
            yield return new object[] { (SByteEnum)1, (long)1 };
            yield return new object[] { (SByteEnum)sbyte.MaxValue, (long)127 };

            // ushort.
            yield return new object[] { ushort.MinValue, (long)0 };
            yield return new object[] { (ushort)1, (long)1 };
            yield return new object[] { ushort.MaxValue, (long)65535 };
            yield return new object[] { (UShortEnum)ushort.MinValue, (long)0 };
            yield return new object[] { (UShortEnum)1, (long)1 };
            yield return new object[] { (UShortEnum)ushort.MaxValue, (long)65535 };

            // short.
            yield return new object[] { short.MinValue, (long)(-32768) };
            yield return new object[] { (short)(-1), (long)(-1) };
            yield return new object[] { (short)0, (long)0 };
            yield return new object[] { (short)1, (long)1 };
            yield return new object[] { short.MaxValue, (long)32767 };
            yield return new object[] { (ShortEnum)short.MinValue, (long)(-32768) };
            yield return new object[] { (ShortEnum)(-1), (long)(-1) };
            yield return new object[] { (ShortEnum)0, (long)0 };
            yield return new object[] { (ShortEnum)1, (long)1 };
            yield return new object[] { (ShortEnum)short.MaxValue, (long)32767 };

            // uint.
            yield return new object[] { uint.MinValue, (long)0 };
            yield return new object[] { (uint)1, (long)1 };
            yield return new object[] { uint.MaxValue, (long)4294967295 };
            yield return new object[] { (UIntEnum)uint.MinValue, (long)0 };
            yield return new object[] { (UIntEnum)1, (long)1 };
            yield return new object[] { (UIntEnum)uint.MaxValue, (long)4294967295 };

            // int.
            yield return new object[] { int.MinValue, (long)(-2147483648) };
            yield return new object[] { -1, (long)(-1) };
            yield return new object[] { 0, (long)0 };
            yield return new object[] { 1, (long)1 };
            yield return new object[] { int.MaxValue, (long)2147483647 };
            yield return new object[] { (IntEnum)int.MinValue, (long)(-2147483648) };
            yield return new object[] { (IntEnum)(-1), (long)(-1) };
            yield return new object[] { (IntEnum)0, (long)0 };
            yield return new object[] { (IntEnum)1, (long)1 };
            yield return new object[] { (IntEnum)int.MaxValue, (long)2147483647 };

            // ulong.
            yield return new object[] { ulong.MinValue, (long)0 };
            yield return new object[] { (ulong)1, (long)1 };
            yield return new object[] { (ULongEnum)ulong.MinValue, (long)0 };
            yield return new object[] { (ULongEnum)1, (long)1 };

            // long.
            yield return new object[] { long.MinValue, long.MinValue };
            yield return new object[] { (long)(-1), (long)(-1) };
            yield return new object[] { (long)0, (long)0 };
            yield return new object[] { (long)1, (long)1 };
            yield return new object[] { long.MaxValue, long.MaxValue };
            yield return new object[] { (LongEnum)long.MinValue, long.MinValue };
            yield return new object[] { (LongEnum)(-1), (long)(-1) };
            yield return new object[] { (LongEnum)0, (long)0 };
            yield return new object[] { (LongEnum)1, (long)1 };
            yield return new object[] { (LongEnum)long.MaxValue, long.MaxValue };

            // float.
            yield return new object[] { (float)(-1), (long)(-1) };
            yield return new object[] { (float)0, (long)0 };
            yield return new object[] { (float)1, (long)1 };

            // double.
            yield return new object[] { (double)(-1), (long)(-1) };
            yield return new object[] { (double)0, (long)0 };
            yield return new object[] { (double)1, (long)1 };

            // decimal.
            yield return new object[] { (decimal)(-1), (long)(-1) };
            yield return new object[] { (decimal)0, (long)0 };
            yield return new object[] { (decimal)1, (long)1 };

            // bool.
            yield return new object[] { true, (long)(-1) };
            yield return new object[] { false, (long)0 };
            if (ReflectionEmitSupported)
            {
                yield return new object[] { BoolEnum, (long)0 };
            }

            // null.
            yield return new object[] { null, (long)0 };
        }

        [Theory]
        [MemberData(nameof(ToLong_Object_TestData))]
        [MemberData(nameof(ToLong_String_TestData))]
        public void ToLong_Object_ReturnsExpected(IConvertible value, long expected)
        {
            AssertEqual(expected, Conversions.ToLong(value));
            if (value != null)
            {
                AssertEqual(expected, Conversions.ToLong(new ConvertibleWrapper(value)));
            }
        }

        public static IEnumerable<object[]> ToLong_InvalidObject_TestData()
        {
            yield return new object[] { double.PositiveInfinity.ToString() };
            yield return new object[] { double.NegativeInfinity.ToString() };
            yield return new object[] { double.NaN.ToString() };
        }

        [Theory]
        [MemberData(nameof(ToLong_InvalidObject_TestData))]
        [MemberData(nameof(InvalidNumberObject_TestData))]
        [MemberData(nameof(InvalidString_TestData))]
        public void ToLong_InvalidObject_ThrowsInvalidCastException(object value)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToLong(value));
        }

        [Theory]
        [MemberData(nameof(InvalidBool_TestData))]
        public void ToLong_InvalidBool_ThrowsInvalidOperationException(object value)
        {
            Assert.Throws<InvalidOperationException>(() => Conversions.ToLong(value));
        }

        public static IEnumerable<object[]> ToLong_OverflowObject_TestData()
        {
            yield return new object[] { ulong.MaxValue };
            yield return new object[] { (ULongEnum)ulong.MaxValue };
            yield return new object[] { float.MinValue };
            yield return new object[] { float.MaxValue };
            yield return new object[] { float.PositiveInfinity };
            yield return new object[] { float.NegativeInfinity };
            yield return new object[] { float.NaN };
            yield return new object[] { double.MinValue };
            yield return new object[] { double.MaxValue };
            yield return new object[] { double.PositiveInfinity };
            yield return new object[] { double.NegativeInfinity };
            yield return new object[] { double.NaN };
            yield return new object[] { decimal.MinValue };
            yield return new object[] { decimal.MaxValue };
        }

        [Theory]
        [MemberData(nameof(ToLong_OverflowObject_TestData))]
        [MemberData(nameof(ToLong_OverflowString_TestData))]
        public void ToLong_OverflowObject_ThrowsOverflowException(object value)
        {
            Assert.Throws<OverflowException>(() => Conversions.ToLong(value));
        }

        public static IEnumerable<object[]> ToSingle_String_TestData()
        {
            yield return new object[] { null, (float)0 };
            yield return new object[] { "-1", (float)(-1) };
            yield return new object[] { "0", (float)0 };
            yield return new object[] { "1", (float)1 };
            yield return new object[] { "&h5", (float)5 };
            yield return new object[] { "&h0", (float)0 };
            yield return new object[] { "&o5", (float)5 };
            yield return new object[] { " &o5", (float)5 };
            yield return new object[] { "&o0", (float)0 };
            yield return new object[] { 1.1.ToString(), (float)1.1 };
            yield return new object[] { "18446744073709551616", 18446744073709551616.0f };
            yield return new object[] { double.PositiveInfinity.ToString(), float.PositiveInfinity };
            yield return new object[] { double.NegativeInfinity.ToString(), float.NegativeInfinity };
            yield return new object[] { double.NaN.ToString(), float.NaN };
        }

        [Theory]
        [MemberData(nameof(ToSingle_String_TestData))]
        public void ToSingle_String_ReturnsExpected(string value, float expected)
        {
            AssertEqual(expected, Conversions.ToSingle(value));
        }

        [Theory]
        [MemberData(nameof(InvalidString_TestData))]
        public void ToSingle_InvalidString_ThrowsInvalidCastException(string value)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToSingle(value));
        }

        public static IEnumerable<object[]> ToSingle_OverflowString_TestData()
        {
            yield return new object[] { "1844674407370955161618446744073709551616" };
        }

        [Theory]
        [MemberData(nameof(ToSingle_OverflowString_TestData))]
        public void ToSingle_OverflowString_ThrowsOverflowException(string value)
        {
            Assert.Throws<OverflowException>(() => Conversions.ToSingle(value));
        }

        public static IEnumerable<object[]> ToSingle_Object_TestData()
        {
            // byte.
            yield return new object[] { byte.MinValue, (float)0 };
            yield return new object[] { (byte)1, (float)1 };
            yield return new object[] { byte.MaxValue, (float)255 };
            yield return new object[] { (ByteEnum)byte.MinValue, (float)0 };
            yield return new object[] { (ByteEnum)1, (float)1 };
            yield return new object[] { (ByteEnum)byte.MaxValue, (float)255 };

            // sbyte.
            yield return new object[] { sbyte.MinValue, (float)(-128) };
            yield return new object[] { (sbyte)(-1), (float)(-1) };
            yield return new object[] { (sbyte)0, (float)0 };
            yield return new object[] { (sbyte)1, (float)1 };
            yield return new object[] { sbyte.MaxValue, (float)127 };
            yield return new object[] { (SByteEnum)sbyte.MinValue, (float)(-128) };
            yield return new object[] { (SByteEnum)(-1), (float)(-1) };
            yield return new object[] { (SByteEnum)0, (float)0 };
            yield return new object[] { (SByteEnum)1, (float)1 };
            yield return new object[] { (SByteEnum)sbyte.MaxValue, (float)127 };

            // ushort.
            yield return new object[] { ushort.MinValue, (float)0 };
            yield return new object[] { (ushort)1, (float)1 };
            yield return new object[] { ushort.MaxValue, (float)65535 };
            yield return new object[] { (UShortEnum)ushort.MinValue, (float)0 };
            yield return new object[] { (UShortEnum)1, (float)1 };
            yield return new object[] { (UShortEnum)ushort.MaxValue, (float)65535 };

            // short.
            yield return new object[] { short.MinValue, (float)(-32768) };
            yield return new object[] { (short)(-1), (float)(-1) };
            yield return new object[] { (short)0, (float)0 };
            yield return new object[] { (short)1, (float)1 };
            yield return new object[] { short.MaxValue, (float)32767 };
            yield return new object[] { (ShortEnum)short.MinValue, (float)(-32768) };
            yield return new object[] { (ShortEnum)(-1), (float)(-1) };
            yield return new object[] { (ShortEnum)0, (float)0 };
            yield return new object[] { (ShortEnum)1, (float)1 };
            yield return new object[] { (ShortEnum)short.MaxValue, (float)32767 };

            // uint.
            yield return new object[] { uint.MinValue, (float)0 };
            yield return new object[] { (uint)1, (float)1 };
            yield return new object[] { uint.MaxValue, (float)uint.MaxValue };
            yield return new object[] { (UIntEnum)uint.MinValue, (float)0 };
            yield return new object[] { (UIntEnum)1, (float)1 };
            yield return new object[] { (UIntEnum)uint.MaxValue, (float)uint.MaxValue };

            // int.
            yield return new object[] { int.MinValue, (float)int.MinValue };
            yield return new object[] { -1, (float)(-1) };
            yield return new object[] { 0, (float)0 };
            yield return new object[] { 1, (float)1 };
            yield return new object[] { int.MaxValue, (float)int.MaxValue };
            yield return new object[] { (IntEnum)int.MinValue, (float)int.MinValue };
            yield return new object[] { (IntEnum)(-1), (float)(-1) };
            yield return new object[] { (IntEnum)0, (float)0 };
            yield return new object[] { (IntEnum)1, (float)1 };
            yield return new object[] { (IntEnum)int.MaxValue, (float)int.MaxValue };

            // ulong.
            yield return new object[] { ulong.MinValue, (float)0 };
            yield return new object[] { (ulong)1, (float)1 };
            yield return new object[] { ulong.MaxValue, (float)ulong.MaxValue };
            yield return new object[] { (ULongEnum)ulong.MinValue, (float)0 };
            yield return new object[] { (ULongEnum)1, (float)1 };
            yield return new object[] { (ULongEnum)ulong.MaxValue, (float)ulong.MaxValue };

            // long.
            yield return new object[] { long.MinValue, (float)long.MinValue };
            yield return new object[] { (long)(-1), (float)(-1) };
            yield return new object[] { (long)0, (float)0 };
            yield return new object[] { (long)1, (float)1 };
            yield return new object[] { long.MaxValue, (float)long.MaxValue };
            yield return new object[] { (LongEnum)long.MinValue, (float)long.MinValue };
            yield return new object[] { (LongEnum)(-1), (float)(-1) };
            yield return new object[] { (LongEnum)0, (float)0 };
            yield return new object[] { (LongEnum)1, (float)1 };
            yield return new object[] { (LongEnum)long.MaxValue, (float)long.MaxValue };

            // float.
            yield return new object[] { float.MinValue, float.MinValue };
            yield return new object[] { (float)(-1), (float)(-1) };
            yield return new object[] { (float)0, (float)0 };
            yield return new object[] { (float)1, (float)1 };
            yield return new object[] { float.MaxValue, float.MaxValue };
            yield return new object[] { float.PositiveInfinity, float.PositiveInfinity };
            yield return new object[] { float.NegativeInfinity, float.NegativeInfinity };
            yield return new object[] { float.NaN, float.NaN };

            // double.
            yield return new object[] { double.MinValue, float.NegativeInfinity };
            yield return new object[] { (double)(-1), (float)(-1) };
            yield return new object[] { (double)0, (float)0 };
            yield return new object[] { (double)1, (float)1 };
            yield return new object[] { double.MaxValue, float.PositiveInfinity };
            yield return new object[] { double.PositiveInfinity, float.PositiveInfinity };
            yield return new object[] { double.NegativeInfinity, float.NegativeInfinity };
            yield return new object[] { double.NaN, float.NaN };

            // decimal.
            yield return new object[] { decimal.MinValue, (float)decimal.MinValue };
            yield return new object[] { (decimal)(-1), (float)(-1) };
            yield return new object[] { (decimal)0, (float)0 };
            yield return new object[] { (decimal)1, (float)1 };
            yield return new object[] { decimal.MaxValue, (float)decimal.MaxValue };

            // bool.
            yield return new object[] { true, (float)(-1) };
            yield return new object[] { false, (float)0 };
            if (ReflectionEmitSupported)
            {
                yield return new object[] { BoolEnum, (float)0 };
            }

            // null.
            yield return new object[] { null, (float)0 };
        }

        [Theory]
        [MemberData(nameof(ToSingle_Object_TestData))]
        [MemberData(nameof(ToSingle_String_TestData))]
        public void ToSingle_Object_ReturnsExpected(IConvertible value, float expected)
        {
            AssertEqual(expected, Conversions.ToSingle(value));
            if (value != null)
            {
                AssertEqual(expected, Conversions.ToSingle(new ConvertibleWrapper(value)));
            }
        }

        [Theory]
        [MemberData(nameof(InvalidNumberObject_TestData))]
        [MemberData(nameof(InvalidString_TestData))]
        public void ToSingle_InvalidObject_ThrowsInvalidCastException(object value)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToSingle(value));
        }

        [Theory]
        [MemberData(nameof(InvalidBool_TestData))]
        public void ToSingle_InvalidBool_ThrowsInvalidOperationException(object value)
        {
            Assert.Throws<InvalidOperationException>(() => Conversions.ToSingle(value));
        }

        public static IEnumerable<object[]> ToSingle_OverflowObject_TestData()
        {
            yield return new object[] { "1844674407370955161618446744073709551616" };
        }

        [Theory]
        [MemberData(nameof(ToSingle_OverflowObject_TestData))]
        public void ToSingle_OverflowObject_ThrowsOverflowException(object value)
        {
            Assert.Throws<OverflowException>(() => Conversions.ToSingle(value));
        }

        public static IEnumerable<object[]> ToDouble_String_TestData()
        {
            yield return new object[] { null, (double)0 };
            yield return new object[] { "-1", (double)(-1) };
            yield return new object[] { "0", (double)0 };
            yield return new object[] { "1", (double)1 };
            yield return new object[] { "&h5", (double)5 };
            yield return new object[] { "&h0", (double)0 };
            yield return new object[] { "&o5", (double)5 };
            yield return new object[] { " &o5", (double)5 };
            yield return new object[] { "&o0", (double)0 };
            yield return new object[] { 1.1.ToString(), (double)1.1 };
            yield return new object[] { "18446744073709551616", 18446744073709551616.0 };
            yield return new object[] { "1844674407370955161618446744073709551616", 1844674407370955161618446744073709551616.0 };
            yield return new object[] { double.PositiveInfinity.ToString(), double.PositiveInfinity };
            yield return new object[] { double.NegativeInfinity.ToString(), double.NegativeInfinity };
            yield return new object[] { double.NaN.ToString(), double.NaN };
        }

        [Theory]
        [MemberData(nameof(ToDouble_String_TestData))]
        public void ToDouble_String_ReturnsExpected(string value, double expected)
        {
            AssertEqual(expected, Conversions.ToDouble(value));
        }

        [Theory]
        [MemberData(nameof(InvalidString_TestData))]
        public void ToDouble_InvalidString_ThrowsInvalidCastException(string value)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToDouble(value));
        }

        public static IEnumerable<object[]> ToDouble_Object_TestData()
        {
            // byte.
            yield return new object[] { byte.MinValue, (double)0 };
            yield return new object[] { (byte)1, (double)1 };
            yield return new object[] { byte.MaxValue, (double)255 };
            yield return new object[] { (ByteEnum)byte.MinValue, (double)0 };
            yield return new object[] { (ByteEnum)1, (double)1 };
            yield return new object[] { (ByteEnum)byte.MaxValue, (double)255 };

            // sbyte.
            yield return new object[] { sbyte.MinValue, (double)(-128) };
            yield return new object[] { (sbyte)(-1), (double)(-1) };
            yield return new object[] { (sbyte)0, (double)0 };
            yield return new object[] { (sbyte)1, (double)1 };
            yield return new object[] { sbyte.MaxValue, (double)127 };
            yield return new object[] { (SByteEnum)sbyte.MinValue, (double)(-128) };
            yield return new object[] { (SByteEnum)(-1), (double)(-1) };
            yield return new object[] { (SByteEnum)0, (double)0 };
            yield return new object[] { (SByteEnum)1, (double)1 };
            yield return new object[] { (SByteEnum)sbyte.MaxValue, (double)127 };

            // ushort.
            yield return new object[] { ushort.MinValue, (double)0 };
            yield return new object[] { (ushort)1, (double)1 };
            yield return new object[] { ushort.MaxValue, (double)65535 };
            yield return new object[] { (UShortEnum)ushort.MinValue, (double)0 };
            yield return new object[] { (UShortEnum)1, (double)1 };
            yield return new object[] { (UShortEnum)ushort.MaxValue, (double)65535 };

            // short.
            yield return new object[] { short.MinValue, (double)(-32768) };
            yield return new object[] { (short)(-1), (double)(-1) };
            yield return new object[] { (short)0, (double)0 };
            yield return new object[] { (short)1, (double)1 };
            yield return new object[] { short.MaxValue, (double)32767 };
            yield return new object[] { (ShortEnum)short.MinValue, (double)(-32768) };
            yield return new object[] { (ShortEnum)(-1), (double)(-1) };
            yield return new object[] { (ShortEnum)0, (double)0 };
            yield return new object[] { (ShortEnum)1, (double)1 };
            yield return new object[] { (ShortEnum)short.MaxValue, (double)32767 };

            // uint.
            yield return new object[] { uint.MinValue, (double)0 };
            yield return new object[] { (uint)1, (double)1 };
            yield return new object[] { uint.MaxValue, (double)4294967295 };
            yield return new object[] { (UIntEnum)uint.MinValue, (double)0 };
            yield return new object[] { (UIntEnum)1, (double)1 };
            yield return new object[] { (UIntEnum)uint.MaxValue, (double)4294967295 };

            // int.
            yield return new object[] { int.MinValue, (double)(-2147483648) };
            yield return new object[] { -1, (double)(-1) };
            yield return new object[] { 0, (double)0 };
            yield return new object[] { 1, (double)1 };
            yield return new object[] { int.MaxValue, (double)2147483647 };
            yield return new object[] { (IntEnum)int.MinValue, (double)(-2147483648) };
            yield return new object[] { (IntEnum)(-1), (double)(-1) };
            yield return new object[] { (IntEnum)0, (double)0 };
            yield return new object[] { (IntEnum)1, (double)1 };
            yield return new object[] { (IntEnum)int.MaxValue, (double)2147483647 };

            // ulong.
            yield return new object[] { ulong.MinValue, (double)0 };
            yield return new object[] { (ulong)1, (double)1 };
            yield return new object[] { ulong.MaxValue, (double)ulong.MaxValue };
            yield return new object[] { (ULongEnum)ulong.MinValue, (double)0 };
            yield return new object[] { (ULongEnum)1, (double)1 };
            yield return new object[] { (ULongEnum)ulong.MaxValue, (double)ulong.MaxValue };

            // long.
            yield return new object[] { long.MinValue, (double)long.MinValue };
            yield return new object[] { (long)(-1), (double)(-1) };
            yield return new object[] { (long)0, (double)0 };
            yield return new object[] { (long)1, (double)1 };
            yield return new object[] { long.MaxValue, (double)long.MaxValue };
            yield return new object[] { (LongEnum)long.MinValue, (double)long.MinValue };
            yield return new object[] { (LongEnum)(-1), (double)(-1) };
            yield return new object[] { (LongEnum)0, (double)0 };
            yield return new object[] { (LongEnum)1, (double)1 };
            yield return new object[] { (LongEnum)long.MaxValue, (double)long.MaxValue };

            // float.
            yield return new object[] { float.MinValue, (double)float.MinValue };
            yield return new object[] { (float)(-1), (double)(-1) };
            yield return new object[] { (float)0, (double)0 };
            yield return new object[] { (float)1, (double)1 };
            yield return new object[] { float.MaxValue, (double)float.MaxValue };
            yield return new object[] { float.PositiveInfinity, double.PositiveInfinity };
            yield return new object[] { float.NegativeInfinity, double.NegativeInfinity };
            yield return new object[] { float.NaN, double.NaN };

            // double.
            yield return new object[] { double.MinValue, double.MinValue };
            yield return new object[] { (double)(-1), (double)(-1) };
            yield return new object[] { (double)0, (double)0 };
            yield return new object[] { (double)1, (double)1 };
            yield return new object[] { double.MaxValue, double.MaxValue };
            yield return new object[] { double.PositiveInfinity, double.PositiveInfinity };
            yield return new object[] { double.NegativeInfinity, double.NegativeInfinity };
            yield return new object[] { double.NaN, double.NaN };

            // decimal.
            yield return new object[] { decimal.MinValue, (double)decimal.MinValue };
            yield return new object[] { (decimal)(-1), (double)(-1) };
            yield return new object[] { (decimal)0, (double)0 };
            yield return new object[] { (decimal)1, (double)1 };
            yield return new object[] { decimal.MaxValue, (double)decimal.MaxValue };

            // bool.
            yield return new object[] { true, (double)(-1) };
            yield return new object[] { false, (double)0 };
            if (ReflectionEmitSupported)
            {
                yield return new object[] { BoolEnum, (double)0 };
            }

            // null.
            yield return new object[] { null, (double)0 };
        }

        [Theory]
        [MemberData(nameof(ToDouble_Object_TestData))]
        [MemberData(nameof(ToDouble_String_TestData))]
        public void ToDouble_Object_ReturnsExpected(IConvertible value, double expected)
        {
            AssertEqual(expected, Conversions.ToDouble(value));
            if (value != null)
            {
                AssertEqual(expected, Conversions.ToDouble(new ConvertibleWrapper(value)));
            }
        }

        public static IEnumerable<object[]> ToDouble_InvalidObject_TestData()
        {
            yield return new object[] { char.MinValue };
            yield return new object[] { (char)1 };
            yield return new object[] { char.MaxValue };
            yield return new object[] { new DateTime(10) };
            yield return new object[] { new object() };
        }

        [Theory]
        [MemberData(nameof(ToDouble_InvalidObject_TestData))]
        [MemberData(nameof(InvalidString_TestData))]
        public void ToDouble_InvalidObject_ThrowsInvalidCastException(object value)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToDouble(value));
        }

        [Theory]
        [MemberData(nameof(InvalidBool_TestData))]
        public void ToDouble_InvalidBool_ThrowsInvalidOperationException(object value)
        {
            Assert.Throws<InvalidOperationException>(() => Conversions.ToDouble(value));
        }

        public static IEnumerable<object[]> ToDecimal_String_TestData()
        {
            yield return new object[] { null, (decimal)0 };
            yield return new object[] { "-1", (decimal)(-1) };
            yield return new object[] { "0", (decimal)0 };
            yield return new object[] { "1", (decimal)1 };
            yield return new object[] { "&h5", (decimal)5 };
            yield return new object[] { "&h0", (decimal)0 };
            yield return new object[] { "&o5", (decimal)5 };
            yield return new object[] { " &o5", (decimal)5 };
            yield return new object[] { "&o0", (decimal)0 };
            yield return new object[] { 1.1.ToString(), (decimal)1.1 };
            yield return new object[] { "18446744073709551616", 18446744073709551616.0m };
        }

        [Theory]
        [MemberData(nameof(ToDecimal_String_TestData))]
        public void ToDecimal_String_ReturnsExpected(string value, decimal expected)
        {
            AssertEqual(expected, Conversions.ToDecimal(value));
        }

        public static IEnumerable<object[]> ToDecimal_InvalidString_TestData()
        {
            yield return new object[] { double.PositiveInfinity.ToString() };
            yield return new object[] { double.NegativeInfinity.ToString() };
            yield return new object[] { double.NaN.ToString() };
        }

        [Theory]
        [MemberData(nameof(ToDecimal_InvalidString_TestData))]
        [MemberData(nameof(InvalidString_TestData))]
        public void ToDecimal_InvalidString_ThrowsInvalidCastException(string value)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToDecimal(value));
        }

        public static IEnumerable<object[]> ToDecimal_OverflowString_TestData()
        {
            yield return new object[] { "1844674407370955161618446744073709551616" };
        }

        [Theory]
        [MemberData(nameof(ToDecimal_OverflowString_TestData))]
        public void ToDecimal_OverflowString_ThrowsOverflowException(string value)
        {
            Assert.Throws<OverflowException>(() => Conversions.ToDecimal(value));
        }

        public static IEnumerable<object[]> ToDecimal_Object_TestData()
        {
            // byte.
            yield return new object[] { byte.MinValue, (decimal)0 };
            yield return new object[] { (byte)1, (decimal)1 };
            yield return new object[] { byte.MaxValue, (decimal)255 };
            yield return new object[] { (ByteEnum)byte.MinValue, (decimal)0 };
            yield return new object[] { (ByteEnum)1, (decimal)1 };
            yield return new object[] { (ByteEnum)byte.MaxValue, (decimal)255 };

            // sbyte.
            yield return new object[] { sbyte.MinValue, (decimal)(-128) };
            yield return new object[] { (sbyte)(-1), (decimal)(-1) };
            yield return new object[] { (sbyte)0, (decimal)0 };
            yield return new object[] { (sbyte)1, (decimal)1 };
            yield return new object[] { sbyte.MaxValue, (decimal)127 };
            yield return new object[] { (SByteEnum)sbyte.MinValue, (decimal)(-128) };
            yield return new object[] { (SByteEnum)(-1), (decimal)(-1) };
            yield return new object[] { (SByteEnum)0, (decimal)0 };
            yield return new object[] { (SByteEnum)1, (decimal)1 };
            yield return new object[] { (SByteEnum)sbyte.MaxValue, (decimal)127 };

            // ushort.
            yield return new object[] { ushort.MinValue, (decimal)0 };
            yield return new object[] { (ushort)1, (decimal)1 };
            yield return new object[] { ushort.MaxValue, (decimal)65535 };
            yield return new object[] { (UShortEnum)ushort.MinValue, (decimal)0 };
            yield return new object[] { (UShortEnum)1, (decimal)1 };
            yield return new object[] { (UShortEnum)ushort.MaxValue, (decimal)65535 };

            // short.
            yield return new object[] { short.MinValue, (decimal)(-32768) };
            yield return new object[] { (short)(-1), (decimal)(-1) };
            yield return new object[] { (short)0, (decimal)0 };
            yield return new object[] { (short)1, (decimal)1 };
            yield return new object[] { short.MaxValue, (decimal)32767 };
            yield return new object[] { (ShortEnum)short.MinValue, (decimal)(-32768) };
            yield return new object[] { (ShortEnum)(-1), (decimal)(-1) };
            yield return new object[] { (ShortEnum)0, (decimal)0 };
            yield return new object[] { (ShortEnum)1, (decimal)1 };
            yield return new object[] { (ShortEnum)short.MaxValue, (decimal)32767 };

            // uint.
            yield return new object[] { uint.MinValue, (decimal)0 };
            yield return new object[] { (uint)1, (decimal)1 };
            yield return new object[] { uint.MaxValue, (decimal)4294967295 };
            yield return new object[] { (UIntEnum)uint.MinValue, (decimal)0 };
            yield return new object[] { (UIntEnum)1, (decimal)1 };
            yield return new object[] { (UIntEnum)uint.MaxValue, (decimal)4294967295 };

            // int.
            yield return new object[] { int.MinValue, (decimal)(-2147483648) };
            yield return new object[] { -1, (decimal)(-1) };
            yield return new object[] { 0, (decimal)0 };
            yield return new object[] { 1, (decimal)1 };
            yield return new object[] { int.MaxValue, (decimal)2147483647 };
            yield return new object[] { (IntEnum)int.MinValue, (decimal)(-2147483648) };
            yield return new object[] { (IntEnum)(-1), (decimal)(-1) };
            yield return new object[] { (IntEnum)0, (decimal)0 };
            yield return new object[] { (IntEnum)1, (decimal)1 };
            yield return new object[] { (IntEnum)int.MaxValue, (decimal)2147483647 };

            // ulong.
            yield return new object[] { ulong.MinValue, (decimal)0 };
            yield return new object[] { (ulong)1, (decimal)1 };
            yield return new object[] { ulong.MaxValue, (decimal)ulong.MaxValue };
            yield return new object[] { (ULongEnum)ulong.MinValue, (decimal)0 };
            yield return new object[] { (ULongEnum)1, (decimal)1 };
            yield return new object[] { (ULongEnum)ulong.MaxValue, (decimal)ulong.MaxValue };

            // long.
            yield return new object[] { long.MinValue, (decimal)(-9223372036854775808) };
            yield return new object[] { (long)(-1), (decimal)(-1) };
            yield return new object[] { (long)0, (decimal)0 };
            yield return new object[] { (long)1, (decimal)1 };
            yield return new object[] { long.MaxValue, (decimal)9223372036854775807 };
            yield return new object[] { (LongEnum)long.MinValue, (decimal)(-9223372036854775808) };
            yield return new object[] { (LongEnum)(-1), (decimal)(-1) };
            yield return new object[] { (LongEnum)0, (decimal)0 };
            yield return new object[] { (LongEnum)1, (decimal)1 };
            yield return new object[] { (LongEnum)long.MaxValue, (decimal)9223372036854775807 };

            // float.
            yield return new object[] { (float)(-1), (decimal)(-1) };
            yield return new object[] { (float)0, (decimal)0 };
            yield return new object[] { (float)1, (decimal)1 };

            // double.
            yield return new object[] { (double)(-1), (decimal)(-1) };
            yield return new object[] { (double)0, (decimal)0 };
            yield return new object[] { (double)1, (decimal)1 };

            // decimal.
            yield return new object[] { decimal.MinValue, decimal.MinValue };
            yield return new object[] { (decimal)(-1), (decimal)(-1) };
            yield return new object[] { (decimal)0, (decimal)0 };
            yield return new object[] { (decimal)1, (decimal)1 };
            yield return new object[] { decimal.MaxValue, decimal.MaxValue };

            // bool.
            yield return new object[] { true, (decimal)(-1) };
            yield return new object[] { false, (decimal)0 };
            if (ReflectionEmitSupported)
            {
                yield return new object[] { BoolEnum, (decimal)0 };
            }

            // null.
            yield return new object[] { null, (decimal)0 };
        }

        [Theory]
        [MemberData(nameof(ToDecimal_Object_TestData))]
        [MemberData(nameof(ToDecimal_String_TestData))]
        public void ToDecimal_Object_ReturnsExpected(IConvertible value, decimal expected)
        {
            AssertEqual(expected, Conversions.ToDecimal(value));
            if (value != null)
            {
                AssertEqual(expected, Conversions.ToDecimal(new ConvertibleWrapper(value)));
            }
        }

        public static IEnumerable<object[]> ToDecimal_InvalidObject_TestData()
        {
            yield return new object[] { char.MinValue };
            yield return new object[] { (char)1 };
            yield return new object[] { char.MaxValue };
            yield return new object[] { new DateTime(10) };
            yield return new object[] { double.PositiveInfinity.ToString() };
            yield return new object[] { double.NegativeInfinity.ToString() };
            yield return new object[] { double.NaN.ToString() };
            yield return new object[] { new object() };
        }

        [Theory]
        [MemberData(nameof(ToDecimal_InvalidObject_TestData))]
        [MemberData(nameof(InvalidString_TestData))]
        public void ToDecimal_InvalidObject_ThrowsInvalidCastException(object value)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToDecimal(value));
        }

        [Theory]
        [MemberData(nameof(InvalidBool_TestData))]
        public void ToDecimal_InvalidBool_ThrowsInvalidOperationException(object value)
        {
            Assert.Throws<InvalidOperationException>(() => Conversions.ToDecimal(value));
        }

        public static IEnumerable<object[]> ToDecimal_OverflowObject_TestData()
        {
            yield return new object[] { float.MinValue };
            yield return new object[] { float.MaxValue };
            yield return new object[] { float.PositiveInfinity };
            yield return new object[] { float.NegativeInfinity };
            yield return new object[] { float.NaN };
            yield return new object[] { double.MinValue };
            yield return new object[] { double.MaxValue };
            yield return new object[] { double.PositiveInfinity };
            yield return new object[] { double.NegativeInfinity };
            yield return new object[] { double.NaN };
            yield return new object[] { "1844674407370955161618446744073709551616" };
        }

        [Theory]
        [MemberData(nameof(ToDecimal_OverflowObject_TestData))]
        public void ToDecimal_OverflowObject_ThrowsOverflowException(object value)
        {
            Assert.Throws<OverflowException>(() => Conversions.ToDecimal(value));
        }

        public static IEnumerable<object[]> ToBoolean_String_TestData()
        {
            yield return new object[] { "-1", true };
            yield return new object[] { "0", false };
            yield return new object[] { "1", true };
            yield return new object[] { "&h5", true };
            yield return new object[] { "&h0", false };
            yield return new object[] { "&o5", true };
            yield return new object[] { " &o5", true };
            yield return new object[] { "&o0", false };
            yield return new object[] { 1.1.ToString(), true };
            yield return new object[] { "true", true };
            yield return new object[] { "false", false };
            yield return new object[] { "18446744073709551616", true };
            yield return new object[] { "1844674407370955161618446744073709551616", true };
            yield return new object[] { double.PositiveInfinity.ToString(), true };
            yield return new object[] { double.NegativeInfinity.ToString(), true };
            yield return new object[] { double.NaN.ToString(), true };
        }

        [Theory]
        [MemberData(nameof(ToBoolean_String_TestData))]
        public void ToBoolean_String_ReturnsExpected(string value, bool expected)
        {
            AssertEqual(expected, Conversions.ToBoolean(value));
        }

        public static IEnumerable<object[]> ToBoolean_InvalidString_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { "" };
            yield return new object[] { "&" };
            yield return new object[] { "&a" };
            yield return new object[] { "&a0" };
            yield return new object[] { "invalid" };
        }

        [Theory]
        [MemberData(nameof(ToBoolean_InvalidString_TestData))]
        public void ToBoolean_InvalidString_ThrowsInvalidCastException(string value)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToBoolean(value));
        }

        public static IEnumerable<object[]> ToBoolean_Object_TestData()
        {
            // byte.
            yield return new object[] { byte.MinValue, false };
            yield return new object[] { (byte)1, true };
            yield return new object[] { byte.MaxValue, true };
            yield return new object[] { (ByteEnum)byte.MinValue, false };
            yield return new object[] { (ByteEnum)1, true };
            yield return new object[] { (ByteEnum)byte.MaxValue, true };

            // sbyte.
            yield return new object[] { sbyte.MinValue, true };
            yield return new object[] { (sbyte)(-1), true };
            yield return new object[] { (sbyte)0, false };
            yield return new object[] { (sbyte)1, true };
            yield return new object[] { sbyte.MaxValue, true };
            yield return new object[] { (SByteEnum)sbyte.MinValue, true };
            yield return new object[] { (SByteEnum)(-1), true };
            yield return new object[] { (SByteEnum)0, false };
            yield return new object[] { (SByteEnum)1, true };
            yield return new object[] { (SByteEnum)sbyte.MaxValue, true };

            // ushort.
            yield return new object[] { ushort.MinValue, false };
            yield return new object[] { (ushort)1, true };
            yield return new object[] { ushort.MaxValue, true };
            yield return new object[] { (UShortEnum)ushort.MinValue, false };
            yield return new object[] { (UShortEnum)1, true };
            yield return new object[] { (UShortEnum)ushort.MaxValue, true };

            // short.
            yield return new object[] { short.MinValue, true };
            yield return new object[] { (short)(-1), true };
            yield return new object[] { (short)0, false };
            yield return new object[] { (short)1, true };
            yield return new object[] { short.MaxValue, true };
            yield return new object[] { (ShortEnum)short.MinValue, true };
            yield return new object[] { (ShortEnum)(-1), true };
            yield return new object[] { (ShortEnum)0, false };
            yield return new object[] { (ShortEnum)1, true };
            yield return new object[] { (ShortEnum)short.MaxValue, true };

            // uint.
            yield return new object[] { uint.MinValue, false };
            yield return new object[] { (uint)1, true };
            yield return new object[] { uint.MaxValue, true };
            yield return new object[] { (UIntEnum)uint.MinValue, false };
            yield return new object[] { (UIntEnum)1, true };
            yield return new object[] { (UIntEnum)uint.MaxValue, true };

            // int.
            yield return new object[] { int.MinValue, true };
            yield return new object[] { -1, true };
            yield return new object[] { 0, false };
            yield return new object[] { 1, true };
            yield return new object[] { int.MaxValue, true };
            yield return new object[] { (IntEnum)int.MinValue, true };
            yield return new object[] { (IntEnum)(-1), true };
            yield return new object[] { (IntEnum)0, false };
            yield return new object[] { (IntEnum)1, true };
            yield return new object[] { (IntEnum)int.MaxValue, true };

            // ulong.
            yield return new object[] { ulong.MinValue, false };
            yield return new object[] { (ulong)1, true };
            yield return new object[] { ulong.MaxValue, true };
            yield return new object[] { (ULongEnum)ulong.MinValue, false };
            yield return new object[] { (ULongEnum)1, true };
            yield return new object[] { (ULongEnum)ulong.MaxValue, true };

            // long.
            yield return new object[] { long.MinValue, true };
            yield return new object[] { (long)(-1), true };
            yield return new object[] { (long)0, false };
            yield return new object[] { (long)1, true };
            yield return new object[] { long.MaxValue, true };
            yield return new object[] { (LongEnum)long.MinValue, true };
            yield return new object[] { (LongEnum)(-1), true };
            yield return new object[] { (LongEnum)0, false };
            yield return new object[] { (LongEnum)1, true };
            yield return new object[] { (LongEnum)long.MaxValue, true };

            // float.
            yield return new object[] { float.MinValue, true };
            yield return new object[] { (float)(-1), true };
            yield return new object[] { (float)0, false };
            yield return new object[] { (float)1, true };
            yield return new object[] { float.MaxValue, true };
            yield return new object[] { float.PositiveInfinity, true };
            yield return new object[] { float.NegativeInfinity, true };
            yield return new object[] { float.NaN, true };

            // double.
            yield return new object[] { double.MinValue, true };
            yield return new object[] { (double)(-1), true };
            yield return new object[] { (double)0, false };
            yield return new object[] { (double)1, true };
            yield return new object[] { double.MaxValue, true };
            yield return new object[] { double.PositiveInfinity, true };
            yield return new object[] { double.NegativeInfinity, true };
            yield return new object[] { double.NaN, true };

            // decimal.
            yield return new object[] { decimal.MinValue, true };
            yield return new object[] { (decimal)(-1), true };
            yield return new object[] { (decimal)0, false };
            yield return new object[] { (decimal)1, true };
            yield return new object[] { decimal.MaxValue, true };

            // bool.
            yield return new object[] { true, true };
            yield return new object[] { false, false };
            if (ReflectionEmitSupported)
            {
                yield return new object[] { BoolEnum, false };
            }

            // null.
            yield return new object[] { null, false };
        }

        [Theory]
        [MemberData(nameof(ToBoolean_Object_TestData))]
        [MemberData(nameof(ToBoolean_String_TestData))]
        public void ToBoolean_Object_ReturnsExpected(IConvertible value, bool expected)
        {
            AssertEqual(expected, Conversions.ToBoolean(value));
            if (value != null)
            {
                AssertEqual(expected, Conversions.ToBoolean(new ConvertibleWrapper(value)));
            }
        }

        public static IEnumerable<object[]> ToBoolean_InvalidObject_TestData()
        {
            yield return new object[] { "" };
            yield return new object[] { "&" };
            yield return new object[] { "&a" };
            yield return new object[] { "&a0" };
            yield return new object[] { "invalid" };
        }

        [Theory]
        [MemberData(nameof(ToBoolean_InvalidObject_TestData))]
        [MemberData(nameof(InvalidNumberObject_TestData))]
        public void ToBoolean_InvalidObject_ThrowsInvalidCastException(object value)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToBoolean(value));
        }

        [Theory]
        [MemberData(nameof(InvalidBool_TestData))]
        public void ToBoolean_InvalidBool_ThrowsInvalidOperationException(object value)
        {
            Assert.Throws<InvalidOperationException>(() => Conversions.ToBoolean(value));
        }

        public static IEnumerable<object[]> ToChar_String_TestData()
        {
            yield return new object[] { null, char.MinValue };
            yield return new object[] { "", char.MinValue };
            yield return new object[] { "-1", (char)45 };
            yield return new object[] { "0", '0' };
            yield return new object[] { "1", '1' };
            yield return new object[] { "&h5", (char)38 };
            yield return new object[] { "&h0", (char)38 };
            yield return new object[] { "&o5", (char)38 };
            yield return new object[] { " &o5", (char)32 };
            yield return new object[] { "&o0", (char)38 };
            yield return new object[] { "&", (char)38 };
            yield return new object[] { "&a", (char)38 };
            yield return new object[] { "&a0", (char)38 };
            yield return new object[] { 1.1.ToString(), '1' };
            yield return new object[] { "true", 't' };
            yield return new object[] { "false", 'f' };
            yield return new object[] { "invalid", 'i' };
            yield return new object[] { "18446744073709551616", '1' };
            yield return new object[] { "1844674407370955161618446744073709551616", '1' };
        }

        [Theory]
        [MemberData(nameof(ToChar_String_TestData))]
        public void ToChar_String_ReturnsExpected(string value, char expected)
        {
            AssertEqual(expected, Conversions.ToChar(value));
        }

        public static IEnumerable<object[]> ToChar_Object_TestData()
        {
            // char.
            yield return new object[] { char.MinValue, char.MinValue };
            yield return new object[] { (char)1, (char)1 };
            yield return new object[] { char.MaxValue, char.MaxValue };

            // null.
            yield return new object[] { null, char.MinValue };
        }

        [Theory]
        [MemberData(nameof(ToChar_Object_TestData))]
        [MemberData(nameof(ToChar_String_TestData))]
        public void ToChar_Object_ReturnsExpected(IConvertible value, char expected)
        {
            AssertEqual(expected, Conversions.ToChar(value));
            if (value != null)
            {
                AssertEqual(expected, Conversions.ToChar(new ConvertibleWrapper(value)));
            }
        }

        public static IEnumerable<object[]> ToChar_InvalidObject_TestData()
        {
            yield return new object[] { byte.MinValue };
            yield return new object[] { (byte)1 };
            yield return new object[] { byte.MaxValue };
            yield return new object[] { (ByteEnum)byte.MinValue };
            yield return new object[] { (ByteEnum)1 };
            yield return new object[] { (ByteEnum)byte.MaxValue };
            yield return new object[] { sbyte.MinValue };
            yield return new object[] { (sbyte)(-1) };
            yield return new object[] { (sbyte)0 };
            yield return new object[] { (sbyte)1 };
            yield return new object[] { (sbyte)1 };
            yield return new object[] { sbyte.MaxValue };
            yield return new object[] { (SByteEnum)sbyte.MinValue };
            yield return new object[] { (SByteEnum)(-1) };
            yield return new object[] { (SByteEnum)0 };
            yield return new object[] { (SByteEnum)1 };
            yield return new object[] { (SByteEnum)1 };
            yield return new object[] { (SByteEnum)sbyte.MaxValue };
            yield return new object[] { ushort.MinValue };
            yield return new object[] { (ushort)1 };
            yield return new object[] { ushort.MaxValue };
            yield return new object[] { (UShortEnum)ushort.MinValue };
            yield return new object[] { (UShortEnum)1 };
            yield return new object[] { (UShortEnum)ushort.MaxValue };
            yield return new object[] { short.MinValue };
            yield return new object[] { (short)(-1) };
            yield return new object[] { (short)0 };
            yield return new object[] { (short)1 };
            yield return new object[] { short.MaxValue };
            yield return new object[] { (ShortEnum)short.MinValue };
            yield return new object[] { (ShortEnum)(-1) };
            yield return new object[] { (ShortEnum)0 };
            yield return new object[] { (ShortEnum)1 };
            yield return new object[] { (ShortEnum)short.MaxValue };
            yield return new object[] { uint.MinValue };
            yield return new object[] { (uint)1 };
            yield return new object[] { uint.MaxValue };
            yield return new object[] { (UIntEnum)uint.MinValue };
            yield return new object[] { (UIntEnum)1 };
            yield return new object[] { (UIntEnum)uint.MaxValue };
            yield return new object[] { int.MinValue };
            yield return new object[] { -1 };
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { int.MaxValue };
            yield return new object[] { (IntEnum)int.MinValue };
            yield return new object[] { (IntEnum)(-1) };
            yield return new object[] { (IntEnum)0 };
            yield return new object[] { (IntEnum)1 };
            yield return new object[] { (IntEnum)int.MaxValue };
            yield return new object[] { ulong.MinValue };
            yield return new object[] { (ulong)1 };
            yield return new object[] { ulong.MaxValue };
            yield return new object[] { (ULongEnum)ulong.MinValue };
            yield return new object[] { (ULongEnum)1 };
            yield return new object[] { (ULongEnum)ulong.MaxValue };
            yield return new object[] { long.MinValue };
            yield return new object[] { (long)(-1) };
            yield return new object[] { (long)0 };
            yield return new object[] { (long)1 };
            yield return new object[] { long.MaxValue };
            yield return new object[] { (LongEnum)long.MinValue };
            yield return new object[] { (LongEnum)(-1) };
            yield return new object[] { (LongEnum)0 };
            yield return new object[] { (LongEnum)1 };
            yield return new object[] { (LongEnum)long.MaxValue };
            yield return new object[] { float.MinValue };
            yield return new object[] { (float)(-1) };
            yield return new object[] { (float)0 };
            yield return new object[] { (float)1 };
            yield return new object[] { float.MaxValue };
            yield return new object[] { float.PositiveInfinity };
            yield return new object[] { float.NegativeInfinity };
            yield return new object[] { float.NaN };
            yield return new object[] { double.MinValue };
            yield return new object[] { (double)(-1) };
            yield return new object[] { (double)0 };
            yield return new object[] { (double)1 };
            yield return new object[] { double.MaxValue };
            yield return new object[] { double.PositiveInfinity };
            yield return new object[] { double.NegativeInfinity };
            yield return new object[] { double.NaN };
            yield return new object[] { decimal.MinValue };
            yield return new object[] { (decimal)(-1) };
            yield return new object[] { (decimal)0 };
            yield return new object[] { (decimal)1 };
            yield return new object[] { decimal.MaxValue };
            yield return new object[] { true };
            yield return new object[] { false };
            yield return new object[] { new DateTime(10) };
            yield return new object[] { new object() };
        }

        [Theory]
        [MemberData(nameof(ToChar_InvalidObject_TestData))]
        public void ToChar_InvalidObject_ThrowsInvalidCastException(object value)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToChar(value));
        }

        [Theory]
        [MemberData(nameof(InvalidBool_TestData))]
        public void ToChar_InvalidBool_ThrowsInvalidOperationException(object value)
        {
            Assert.Throws<InvalidOperationException>(() => Conversions.ToChar(value));
        }

        public static IEnumerable<object[]> ToString_IConvertible_TestData()
        {
            // byte.
            yield return new object[] { byte.MinValue, "0" };
            yield return new object[] { (byte)1, "1" };
            yield return new object[] { byte.MaxValue, "255" };
            yield return new object[] { (ByteEnum)byte.MinValue, "0" };
            yield return new object[] { (ByteEnum)1, "1" };
            yield return new object[] { (ByteEnum)byte.MaxValue, "255" };

            // sbyte.
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

            // ushort.
            yield return new object[] { ushort.MinValue, "0" };
            yield return new object[] { (ushort)1, "1" };
            yield return new object[] { ushort.MaxValue, "65535" };
            yield return new object[] { (UShortEnum)ushort.MinValue, "0" };
            yield return new object[] { (UShortEnum)1, "1" };
            yield return new object[] { (UShortEnum)ushort.MaxValue, "65535" };

            // short.
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

            // uint.
            yield return new object[] { uint.MinValue, "0" };
            yield return new object[] { (uint)1, "1" };
            yield return new object[] { uint.MaxValue, "4294967295" };
            yield return new object[] { (UIntEnum)uint.MinValue, "0" };
            yield return new object[] { (UIntEnum)1, "1" };
            yield return new object[] { (UIntEnum)uint.MaxValue, "4294967295" };

            // int.
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

            // ulong.
            yield return new object[] { ulong.MinValue, "0" };
            yield return new object[] { (ulong)1, "1" };
            yield return new object[] { ulong.MaxValue, "18446744073709551615" };
            yield return new object[] { (ULongEnum)ulong.MinValue, "0" };
            yield return new object[] { (ULongEnum)1, "1" };
            yield return new object[] { (ULongEnum)ulong.MaxValue, "18446744073709551615" };

            // long.
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

            // float.
            yield return new object[] { (float)(-1), "-1" };
            yield return new object[] { (float)0, "0" };
            yield return new object[] { (float)1, "1" };
            yield return new object[] { float.PositiveInfinity, float.PositiveInfinity.ToString() };
            yield return new object[] { float.NegativeInfinity, float.NegativeInfinity.ToString() };
            yield return new object[] { float.NaN, "NaN" };

            // double.
            yield return new object[] { (double)(-1), "-1" };
            yield return new object[] { (double)0, "0" };
            yield return new object[] { (double)1, "1" };
            yield return new object[] { double.PositiveInfinity, double.PositiveInfinity.ToString() };
            yield return new object[] { double.NegativeInfinity, double.NegativeInfinity.ToString() };
            yield return new object[] { double.NaN, "NaN" };

            // decimal.
            yield return new object[] { decimal.MinValue, decimal.MinValue.ToString() };
            yield return new object[] { (decimal)(-1), "-1" };
            yield return new object[] { (decimal)0, "0" };
            yield return new object[] { (decimal)1, "1" };
            yield return new object[] { decimal.MaxValue, decimal.MaxValue.ToString() };

            // bool.
            yield return new object[] { true, "True" };
            yield return new object[] { false, "False" };
            if (ReflectionEmitSupported)
            {
                yield return new object[] { BoolEnum, "False" };
            }

            // string.
            yield return new object[] { "", "" };
            yield return new object[] { "abc", "abc" };

            // null.
            yield return new object[] { null, (string)null };

            // char.
            yield return new object[] { char.MinValue, "\0" };
            yield return new object[] { (char)1, "\u0001" };
            yield return new object[] { 'a', "a" };
            yield return new object[] { char.MaxValue, char.MaxValue.ToString() };

            // DateTime.
            yield return new object[] { new DateTime(10), new DateTime(10).ToString("T", null) };
        }

        [Theory]
        [MemberData(nameof(ToString_IConvertible_TestData))]
        public void ToString_IConvertible_ReturnsExpected(IConvertible value, string expected)
        {
            AssertEqual(expected, Conversions.ToString(value));
            if (value != null)
            {
                AssertEqual(expected, Conversions.ToString(new ConvertibleWrapper(value)));
            }
        }

        public static IEnumerable<object[]> ToString_Object_TestData()
        {
            // char[]
            yield return new object[] { new char[0], "" };
            yield return new object[] { new char[] { (char)0 }, "\0" };
            yield return new object[] { new char[] { 'A', 'B' }, "AB" };
        }

        [Theory]
        [MemberData(nameof(ToString_Object_TestData))]
        public void ToString_Object_ReturnsExpected(object value, string expected)
        {
            AssertEqual(expected, Conversions.ToString(value));
        }

        public static IEnumerable<object[]> ToString_InvalidObject_TestData()
        {
            yield return new object[] { new object() };
        }

        [Theory]
        [MemberData(nameof(ToString_InvalidObject_TestData))]
        public void ToString_InvalidObject_ThrowsInvalidCastException(object value)
        {
            Assert.Throws<InvalidCastException>(() => Conversions.ToString(value));
        }

        [Theory]
        [MemberData(nameof(InvalidBool_TestData))]
        public void ToString_InvalidBool_ThrowsInvalidOperationException(object value)
        {
            Assert.Throws<InvalidOperationException>(() => Conversions.ToString(value));
        }

        private static object s_floatEnum;

        public static object FloatEnum
        {
            get
            {
                if (s_floatEnum == null)
                {
                    AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.RunAndCollect);
                    ModuleBuilder module = assembly.DefineDynamicModule("Name");

                    EnumBuilder eb = module.DefineEnum("CharEnumType", TypeAttributes.Public, typeof(float));
                    eb.DefineLiteral("A", 1.0f);
                    eb.DefineLiteral("B", 2.0f);
                    eb.DefineLiteral("C", 3.0f);

                    s_floatEnum = Activator.CreateInstance(eb.CreateTypeInfo());
                }

                return s_floatEnum;
            }
        }

        private static object s_doubleEnum;

        public static object DoubleEnum
        {
            get
            {
                if (s_doubleEnum == null)
                {
                    AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.RunAndCollect);
                    ModuleBuilder module = assembly.DefineDynamicModule("Name");

                    EnumBuilder eb = module.DefineEnum("CharEnumType", TypeAttributes.Public, typeof(double));
                    eb.DefineLiteral("A", 1.0);
                    eb.DefineLiteral("B", 2.0);
                    eb.DefineLiteral("C", 3.0);

                    s_doubleEnum = Activator.CreateInstance(eb.CreateTypeInfo());
                }

                return s_doubleEnum;
            }
        }

        private static object s_boolEnum;

        public static object BoolEnum
        {
            get
            {
                if (s_boolEnum == null)
                {
                    AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.RunAndCollect);
                    ModuleBuilder module = assembly.DefineDynamicModule("Name");

                    EnumBuilder eb = module.DefineEnum("BoolEnumType", TypeAttributes.Public, typeof(bool));
                    eb.DefineLiteral("False", false);
                    eb.DefineLiteral("True", true);

                    s_boolEnum = Activator.CreateInstance(eb.CreateTypeInfo());
                }

                return s_boolEnum;
            }
        }

        private static object s_charEnum;

        public static object CharEnum
        {
            get
            {
                if (s_charEnum == null)
                {
                    AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.RunAndCollect);
                    ModuleBuilder module = assembly.DefineDynamicModule("Name");

                    EnumBuilder eb = module.DefineEnum("CharEnumType", TypeAttributes.Public, typeof(char));
                    eb.DefineLiteral("A", 'A');
                    eb.DefineLiteral("B", 'B');
                    eb.DefineLiteral("C", 'C');

                    s_charEnum = Activator.CreateInstance(eb.CreateTypeInfo());
                }

                return s_charEnum;
            }
        }

        private static object s_intPtrEnum;

        public static object IntPtrEnum
        {
            get
            {
                if (s_intPtrEnum == null)
                {
                    AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.RunAndCollect);
                    ModuleBuilder module = assembly.DefineDynamicModule("Name");

                    EnumBuilder eb = module.DefineEnum("CharEnumType", TypeAttributes.Public, typeof(IntPtr));

                    s_intPtrEnum = Activator.CreateInstance(eb.CreateTypeInfo());
                }

                return s_intPtrEnum;
            }
        }

        private static object s_uintPtrEnum;

        public static object UIntPtrEnum
        {
            get
            {
                if (s_uintPtrEnum == null)
                {
                    AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.RunAndCollect);
                    ModuleBuilder module = assembly.DefineDynamicModule("Name");

                    EnumBuilder eb = module.DefineEnum("CharEnumType", TypeAttributes.Public, typeof(UIntPtr));

                    s_uintPtrEnum = Activator.CreateInstance(eb.CreateTypeInfo());
                }

                return s_uintPtrEnum;
            }
        }

        private static void AssertEqual(object expected, object actual)
        {
            if (expected is double expectedDouble && actual is double actualDouble)
            {
                Assert.Equal(expected.ToString(), actual.ToString());
            }
            else  if (expected is float expectedFloat && actual is float actualFloat)
            {
                Assert.Equal(expected.ToString(), actual.ToString());
            }
            else
            {
                Assert.Equal(expected, actual);
            }
        }
    }

    public enum ByteEnum : byte { Value = 1 }

    public enum SByteEnum : sbyte { Value = 1 }

    public enum UShortEnum : ushort { Value = 1 }

    public enum ShortEnum : short { Value = 1 }

    public enum UIntEnum : uint { Value = 1 }

    public enum IntEnum : int { Value = 1 }

    public enum ULongEnum : ulong { Value = 1 }

    public enum LongEnum : long { Value = 1 }
}
