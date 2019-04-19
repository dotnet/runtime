// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualBasic.Tests;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.VisualBasic.CompilerServices.Tests
{
    public class LongTypeTests
    {
        [Theory]
        [MemberData(nameof(FromObject_TestData))]
        [MemberData(nameof(FromString_TestData))]
        public void FromObject(object value, long expected)
        {
            Assert.Equal(expected, LongType.FromObject(value));
        }

        // The following conversions are not supported.
        [Theory]
        [MemberData(nameof(FromObject_NotSupported_TestData))]
        public void FromObject_NotSupported(object value, long expected)
        {
            Assert.Throws<InvalidCastException>(() => LongType.FromObject(value));
        }

        [Theory]
        [MemberData(nameof(FromObject_Invalid_TestData))]
        public void FromObject_ThrowsInvalidCastException(object value)
        {
            Assert.Throws<InvalidCastException>(() => LongType.FromObject(value));
        }

        [Theory]
        [MemberData(nameof(FromObject_Overflow_TestData))]
        public void FromObject_ThrowsOverflowException(object value)
        {
            Assert.Throws<OverflowException>(() => LongType.FromObject(value));
        }

        [Theory]
        [MemberData(nameof(FromString_TestData))]
        public void FromString(string value, long expected)
        {
            Assert.Equal(expected, LongType.FromString(value));
        }

        // The following conversions are not supported.
        [Theory]
        [MemberData(nameof(FromString_NotSupported_TestData))]
        public void FromString_NotSupported(string value, long expected)
        {
            Assert.Throws<InvalidCastException>(() => LongType.FromString(value));
        }

        [Theory]
        [MemberData(nameof(FromString_Invalid_TestData))]
        public void FromString_ThrowsInvalidCastException(string value)
        {
            Assert.Throws<InvalidCastException>(() => LongType.FromString(value));
        }

        [Theory]
        [MemberData(nameof(FromString_Overflow_TestData))]
        public void FromString_ThrowsOverflowException(string value)
        {
            Assert.Throws<OverflowException>(() => LongType.FromString(value));
        }

        private static IEnumerable<object[]> FromObject_TestData()
        {
            // byte.
            yield return new object[] { byte.MinValue, (long)0 };
            yield return new object[] { (byte)1, (long)1 };
            yield return new object[] { byte.MaxValue, (long)255 };
            yield return new object[] { (ByteEnum)byte.MinValue, (long)0 };
            yield return new object[] { (ByteEnum)1, (long)1 };
            yield return new object[] { (ByteEnum)byte.MaxValue, (long)255 };

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

            // null.
            yield return new object[] { null, (long)0 };
        }

        private static IEnumerable<object[]> FromObject_NotSupported_TestData()
        {
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

            // uint.
            yield return new object[] { uint.MinValue, (long)0 };
            yield return new object[] { (uint)1, (long)1 };
            yield return new object[] { uint.MaxValue, (long)4294967295 };
            yield return new object[] { (UIntEnum)uint.MinValue, (long)0 };
            yield return new object[] { (UIntEnum)1, (long)1 };
            yield return new object[] { (UIntEnum)uint.MaxValue, (long)4294967295 };

            // ulong.
            yield return new object[] { ulong.MinValue, (long)0 };
            yield return new object[] { (ulong)1, (long)1 };
            yield return new object[] { (ULongEnum)ulong.MinValue, (long)0 };
            yield return new object[] { (ULongEnum)1, (long)1 };
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

            // ulong.
            yield return new object[] { ulong.MaxValue };
            yield return new object[] { (ULongEnum)ulong.MaxValue };
        }

        private static IEnumerable<object[]> FromObject_Overflow_TestData()
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
            yield return new object[] { decimal.MinValue };
            yield return new object[] { decimal.MaxValue };
        }

        private static IEnumerable<object[]> FromString_TestData()
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
            yield return new object[] { double.PositiveInfinity.ToString() };
            yield return new object[] { double.NegativeInfinity.ToString() };
            yield return new object[] { double.NaN.ToString() };
        }

        private static IEnumerable<object[]> FromString_Overflow_TestData()
        {
            yield break;
        }
    }
}
