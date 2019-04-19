// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualBasic.Tests;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.VisualBasic.CompilerServices.Tests
{
    public class SingleTypeTests
    {
        [Theory]
        [MemberData(nameof(FromObject_TestData))]
        [MemberData(nameof(FromString_TestData))]
        public void FromObject(object value, float expected)
        {
            Assert.Equal(expected, SingleType.FromObject(value));
            Assert.Equal(expected, SingleType.FromObject(value, System.Globalization.NumberFormatInfo.InvariantInfo));
        }

        [Theory]
        [MemberData(nameof(FromString_Other_TestData))]
        public void FromObject_Other(string value, float expected)
        {
            Assert.Equal(expected, SingleType.FromObject(value));
        }

        // The following conversions are not supported.
        [Theory]
        [MemberData(nameof(FromObject_NotSupported_TestData))]
        public void FromObject_NotSupported(object value, float expected)
        {
            Assert.Throws<InvalidCastException>(() => SingleType.FromObject(value));
        }

        [Theory]
        [MemberData(nameof(FromObject_Invalid_TestData))]
        public void FromObject_ThrowsInvalidCastException(object value)
        {
            Assert.Throws<InvalidCastException>(() => SingleType.FromObject(value));
        }

        [Theory]
        [MemberData(nameof(FromObject_Overflow_TestData))]
        public void FromObject_ThrowsOverflowException(object value)
        {
            Assert.Throws<OverflowException>(() => SingleType.FromObject(value));
        }

        [Theory]
        [MemberData(nameof(FromString_TestData))]
        public void FromString(string value, float expected)
        {
            Assert.Equal(expected, SingleType.FromString(value));
            Assert.Equal(expected, SingleType.FromString(value, System.Globalization.NumberFormatInfo.InvariantInfo));
        }

        [Theory]
        [MemberData(nameof(FromString_Other_TestData))]
        public void FromString_Other(string value, float expected)
        {
            Assert.Equal(expected, SingleType.FromString(value));
        }

        // The following conversions are not supported.
        [Theory]
        [MemberData(nameof(FromString_NotSupported_TestData))]
        public void FromString_NotSupported(string value, float expected)
        {
            Assert.Throws<InvalidCastException>(() => SingleType.FromString(value));
        }

        [Theory]
        [MemberData(nameof(FromString_Invalid_TestData))]
        public void FromString_ThrowsInvalidCastException(string value)
        {
            Assert.Throws<InvalidCastException>(() => SingleType.FromString(value));
        }

        [Theory]
        [MemberData(nameof(FromString_Overflow_TestData))]
        public void FromString_ThrowsOverflowException(string value)
        {
            Assert.Throws<OverflowException>(() => SingleType.FromString(value));
        }

        private static IEnumerable<object[]> FromObject_TestData()
        {
            // byte.
            yield return new object[] { byte.MinValue, (float)0 };
            yield return new object[] { (byte)1, (float)1 };
            yield return new object[] { byte.MaxValue, (float)255 };
            yield return new object[] { (ByteEnum)byte.MinValue, (float)0 };
            yield return new object[] { (ByteEnum)1, (float)1 };
            yield return new object[] { (ByteEnum)byte.MaxValue, (float)255 };

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

            // null.
            yield return new object[] { null, (float)0 };
        }

        private static IEnumerable<object[]> FromObject_NotSupported_TestData()
        {
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

            // uint.
            yield return new object[] { uint.MinValue, (float)0 };
            yield return new object[] { (uint)1, (float)1 };
            yield return new object[] { uint.MaxValue, (float)uint.MaxValue };
            yield return new object[] { (UIntEnum)uint.MinValue, (float)0 };
            yield return new object[] { (UIntEnum)1, (float)1 };
            yield return new object[] { (UIntEnum)uint.MaxValue, (float)uint.MaxValue };

            // ulong.
            yield return new object[] { ulong.MinValue, (float)0 };
            yield return new object[] { (ulong)1, (float)1 };
            yield return new object[] { ulong.MaxValue, (float)ulong.MaxValue };
            yield return new object[] { (ULongEnum)ulong.MinValue, (float)0 };
            yield return new object[] { (ULongEnum)1, (float)1 };
            yield return new object[] { (ULongEnum)ulong.MaxValue, (float)ulong.MaxValue };
        }

        private static IEnumerable<object[]> FromObject_Invalid_TestData()
        {
            // char.
            yield return new object[] { char.MinValue };
            yield return new object[] { (char)1 };
            yield return new object[] { char.MaxValue };

            // DateTime.
            yield return new object[] { new DateTime(10) };

            // object.
            yield return new object[] { new object() };
        }

        private static IEnumerable<object[]> FromObject_Overflow_TestData()
        {
            yield break;
        }

        private static IEnumerable<object[]> FromString_TestData()
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
            yield return new object[] { "18446744073709551616", 18446744073709551616.0f };
            yield return new object[] { double.NaN.ToString(), float.NaN };
        }

        private static IEnumerable<object[]> FromString_Other_TestData()
        {
            yield return new object[] { double.PositiveInfinity.ToString(), float.PositiveInfinity };
            yield return new object[] { double.NegativeInfinity.ToString(), float.NegativeInfinity };
        }

        private static IEnumerable<object[]> FromString_NotSupported_TestData()
        {
            yield break;
        }

        private static IEnumerable<object[]> FromString_Invalid_TestData()
        {
            yield return new object[] { "" };
            yield return new object[] { "&" };
            yield return new object[] { "&a" };
            yield return new object[] { "&a0" };
            yield return new object[] { "true" };
            yield return new object[] { "false" };
            yield return new object[] { "invalid" };
        }

        private static IEnumerable<object[]> FromString_Overflow_TestData()
        {
            yield return new object[] { "1844674407370955161618446744073709551616" };
        }
    }
}
