// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualBasic.Tests;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.VisualBasic.CompilerServices.Tests
{
    public class ByteTypeTests
    {
        [Theory]
        [MemberData(nameof(FromObject_TestData))]
        [MemberData(nameof(FromString_TestData))]
        public void FromObject(object value, byte expected)
        {
            Assert.Equal(expected, ByteType.FromObject(value));
        }

        // The following conversions are not supported.
        [Theory]
        [MemberData(nameof(FromObject_NotSupported_TestData))]
        public void FromObject_NotSupported(object value, byte expected)
        {
            Assert.Throws<InvalidCastException>(() => ByteType.FromObject(value));
        }

        [Theory]
        [MemberData(nameof(FromObject_Invalid_TestData))]
        public void FromObject_ThrowsInvalidCastException(object value)
        {
            Assert.Throws<InvalidCastException>(() => ByteType.FromObject(value));
        }

        [Theory]
        [MemberData(nameof(FromObject_Overflow_TestData))]
        public void FromObject_ThrowsOverflowException(object value)
        {
            Assert.Throws<OverflowException>(() => ByteType.FromObject(value));
        }

        [Theory]
        [MemberData(nameof(FromString_TestData))]
        public void FromString(string value, byte expected)
        {
            Assert.Equal(expected, ByteType.FromString(value));
        }

        // The following conversions are not supported.
        [Theory]
        [MemberData(nameof(FromString_NotSupported_TestData))]
        public void FromString_NotSupported(string value, byte expected)
        {
            Assert.Throws<InvalidCastException>(() => ByteType.FromString(value));
        }

        [Theory]
        [MemberData(nameof(FromString_Invalid_TestData))]
        public void FromString_ThrowsInvalidCastException(string value)
        {
            Assert.Throws<InvalidCastException>(() => ByteType.FromString(value));
        }

        [Theory]
        [MemberData(nameof(FromString_Overflow_TestData))]
        public void FromString_ThrowsOverflowException(string value)
        {
            Assert.Throws<OverflowException>(() => ByteType.FromString(value));
        }

        private static IEnumerable<object[]> FromObject_TestData()
        {
            // byte.
            yield return new object[] { byte.MinValue, byte.MinValue };
            yield return new object[] { (byte)1, (byte)1 };
            yield return new object[] { byte.MaxValue, byte.MaxValue };
            yield return new object[] { (ByteEnum)byte.MinValue, byte.MinValue };
            yield return new object[] { (ByteEnum)1, (byte)1 };
            yield return new object[] { (ByteEnum)byte.MaxValue, byte.MaxValue };

            // short.
            yield return new object[] { (short)0, byte.MinValue };
            yield return new object[] { (short)1, (byte)1 };
            yield return new object[] { (ShortEnum)0, byte.MinValue };
            yield return new object[] { (ShortEnum)1, (byte)1 };

            // int.
            yield return new object[] { 0, byte.MinValue };
            yield return new object[] { 1, (byte)1 };
            yield return new object[] { (IntEnum)0, byte.MinValue };
            yield return new object[] { (IntEnum)1, (byte)1 };

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

            // null.
            yield return new object[] { null, byte.MinValue };
        }

        private static IEnumerable<object[]> FromObject_NotSupported_TestData()
        {
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

            // uint.
            yield return new object[] { uint.MinValue, byte.MinValue };
            yield return new object[] { (uint)1, (byte)1 };
            yield return new object[] { (UIntEnum)uint.MinValue, byte.MinValue };
            yield return new object[] { (UIntEnum)1, (byte)1 };

            // ulong.
            yield return new object[] { ulong.MinValue, byte.MinValue };
            yield return new object[] { (ulong)1, (byte)1 };
            yield return new object[] { (ULongEnum)ulong.MinValue, byte.MinValue };
            yield return new object[] { (ULongEnum)1, (byte)1 };
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

            // ushort.
            yield return new object[] { ushort.MaxValue };
            yield return new object[] { (UShortEnum)ushort.MaxValue };
        }

        private static IEnumerable<object[]> FromObject_Overflow_TestData()
        {
            yield return new object[] { short.MaxValue };
            yield return new object[] { (ShortEnum)short.MaxValue };
        }

        private static IEnumerable<object[]> FromString_TestData()
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
            yield return new object[] { "256" };
        }
    }
}
