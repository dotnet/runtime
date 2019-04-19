// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualBasic.Tests;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.VisualBasic.CompilerServices.Tests
{
    public class IntegerTypeTests
    {
        [Theory]
        [MemberData(nameof(FromObject_TestData))]
        [MemberData(nameof(FromString_TestData))]
        public void FromObject(object value, int expected)
        {
            Assert.Equal(expected, IntegerType.FromObject(value));
        }

        // The following conversions are not supported.
        [Theory]
        [MemberData(nameof(FromObject_NotSupported_TestData))]
        public void FromObject_NotSupported(object value, int expected)
        {
            Assert.Throws<InvalidCastException>(() => IntegerType.FromObject(value));
        }

        [Theory]
        [MemberData(nameof(FromObject_Invalid_TestData))]
        public void FromObject_ThrowsInvalidCastException(object value)
        {
            Assert.Throws<InvalidCastException>(() => IntegerType.FromObject(value));
        }

        [Theory]
        [MemberData(nameof(FromObject_Overflow_TestData))]
        public void FromObject_ThrowsOverflowException(object value)
        {
            Assert.Throws<OverflowException>(() => IntegerType.FromObject(value));
        }

        [Theory]
        [MemberData(nameof(FromString_TestData))]
        public void FromString(string value, int expected)
        {
            Assert.Equal(expected, IntegerType.FromString(value));
        }

        // The following conversions are not supported.
        [Theory]
        [MemberData(nameof(FromString_NotSupported_TestData))]
        public void FromString_NotSupported(string value, int expected)
        {
            Assert.Throws<InvalidCastException>(() => IntegerType.FromString(value));
        }

        [Theory]
        [MemberData(nameof(FromString_Invalid_TestData))]
        public void FromString_ThrowsInvalidCastException(string value)
        {
            Assert.Throws<InvalidCastException>(() => IntegerType.FromString(value));
        }

        [Theory]
        [MemberData(nameof(FromString_Overflow_TestData))]
        public void FromString_ThrowsOverflowException(string value)
        {
            Assert.Throws<OverflowException>(() => IntegerType.FromString(value));
        }

        private static IEnumerable<object[]> FromObject_TestData()
        {
            // byte.
            yield return new object[] { byte.MinValue, 0 };
            yield return new object[] { (byte)1, 1 };
            yield return new object[] { byte.MaxValue, 255 };
            yield return new object[] { (ByteEnum)byte.MinValue, 0 };
            yield return new object[] { (ByteEnum)1, 1 };
            yield return new object[] { (ByteEnum)byte.MaxValue, 255 };

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

            // null.
            yield return new object[] { null, 0 };
        }

        private static IEnumerable<object[]> FromObject_NotSupported_TestData()
        {
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

            // uint.
            yield return new object[] { uint.MinValue, 0 };
            yield return new object[] { (uint)1, 1 };
            yield return new object[] { (UIntEnum)uint.MinValue, 0 };
            yield return new object[] { (UIntEnum)1, 1 };

            // ulong.
            yield return new object[] { ulong.MinValue, 0 };
            yield return new object[] { (ulong)1, 1 };
            yield return new object[] { (ULongEnum)ulong.MinValue, 0 };
            yield return new object[] { (ULongEnum)1, 1 };
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

            // uint.
            yield return new object[] { uint.MaxValue };
            yield return new object[] { (UIntEnum)uint.MaxValue };
        }

        private static IEnumerable<object[]> FromObject_Overflow_TestData()
        {
            yield return new object[] { long.MinValue };
            yield return new object[] { long.MaxValue };
            yield return new object[] { (LongEnum)long.MinValue };
            yield return new object[] { (LongEnum)long.MaxValue };
        }

        private static IEnumerable<object[]> FromString_TestData()
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
            yield return new object[] { "2147483648" };
            yield return new object[] { double.PositiveInfinity.ToString() };
            yield return new object[] { double.NegativeInfinity.ToString() };
            yield return new object[] { double.NaN.ToString() };
        }
    }
}
