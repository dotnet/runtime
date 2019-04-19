// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualBasic.Tests;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.VisualBasic.CompilerServices.Tests
{
    public class CharTypeTests
    {
        [Theory]
        [MemberData(nameof(FromObject_TestData))]
        [MemberData(nameof(FromString_TestData))]
        public void FromObject(object value, char expected)
        {
            Assert.Equal(expected, CharType.FromObject(value));
        }

        // The following conversions are not supported.
        [Theory]
        [MemberData(nameof(FromObject_NotSupported_TestData))]
        public void FromObject_NotSupported(object value, char expected)
        {
            Assert.Throws<InvalidCastException>(() => CharType.FromObject(value));
        }

        [Theory]
        [MemberData(nameof(FromObject_Invalid_TestData))]
        public void FromObject_ThrowsInvalidCastException(object value)
        {
            Assert.Throws<InvalidCastException>(() => CharType.FromObject(value));
        }

        [Theory]
        [MemberData(nameof(FromObject_Overflow_TestData))]
        public void FromObject_ThrowsOverflowException(object value)
        {
            Assert.Throws<OverflowException>(() => CharType.FromObject(value));
        }

        [Theory]
        [MemberData(nameof(FromString_TestData))]
        public void FromString(string value, char expected)
        {
            Assert.Equal(expected, CharType.FromString(value));
        }

        // The following conversions are not supported.
        [Theory]
        [MemberData(nameof(FromString_NotSupported_TestData))]
        public void FromString_NotSupported(string value, char expected)
        {
            Assert.Throws<InvalidCastException>(() => CharType.FromString(value));
        }

        [Theory]
        [MemberData(nameof(FromString_Invalid_TestData))]
        public void FromString_ThrowsInvalidCastException(string value)
        {
            Assert.Throws<InvalidCastException>(() => CharType.FromString(value));
        }

        [Theory]
        [MemberData(nameof(FromString_Overflow_TestData))]
        public void FromString_ThrowsOverflowException(string value)
        {
            Assert.Throws<OverflowException>(() => CharType.FromString(value));
        }

        private static IEnumerable<object[]> FromObject_TestData()
        {
            // char.
            yield return new object[] { char.MinValue, char.MinValue };
            yield return new object[] { (char)1, (char)1 };
            yield return new object[] { char.MaxValue, char.MaxValue };

            // null.
            yield return new object[] { null, char.MinValue };
        }

        private static IEnumerable<object[]> FromObject_NotSupported_TestData()
        {
            yield break;
        }

        private static IEnumerable<object[]> FromObject_Invalid_TestData()
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

        private static IEnumerable<object[]> FromObject_Overflow_TestData()
        {
            yield break;
        }

        private static IEnumerable<object[]> FromString_TestData()
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

        private static IEnumerable<object[]> FromString_NotSupported_TestData()
        {
            yield break;
        }

        private static IEnumerable<object[]> FromString_Invalid_TestData()
        {
            yield break;
        }

        private static IEnumerable<object[]> FromString_Overflow_TestData()
        {
            yield break;
        }
    }
}
