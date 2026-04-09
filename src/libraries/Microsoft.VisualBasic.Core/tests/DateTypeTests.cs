// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.VisualBasic.Tests;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.VisualBasic.CompilerServices.Tests
{
    public class DateTypeTests
    {
        [Theory]
        [MemberData(nameof(FromObject_TestData))]
        [MemberData(nameof(FromString_TestData))]
        public void FromObject(object value, DateTime expected)
        {
            Assert.Equal(expected, DateType.FromObject(value));
        }

        // The following conversions are not supported.
        [Theory]
        [MemberData(nameof(FromObject_NotSupported_TestData))]
        public void FromObject_NotSupported(object value)
        {
            Assert.Throws<InvalidCastException>(() => DateType.FromObject(value));
        }

        [Theory]
        [MemberData(nameof(FromObject_Invalid_TestData))]
        public void FromObject_ThrowsInvalidCastException(object value)
        {
            Assert.Throws<InvalidCastException>(() => DateType.FromObject(value));
        }

        [Theory]
        [MemberData(nameof(FromString_TestData))]
        public void FromString(string value, DateTime expected)
        {
            Assert.Equal(expected, DateType.FromString(value));
            Assert.Equal(expected, DateType.FromString(value, System.Globalization.CultureInfo.InvariantCulture));
        }

        // The following conversions are not supported.
        [Theory]
        [MemberData(nameof(FromString_NotSupported_TestData))]
        public void FromString_NotSupported(string value)
        {
            Assert.Throws<InvalidCastException>(() => DateType.FromString(value));
        }

        [Theory]
        [MemberData(nameof(FromString_Invalid_TestData))]
        public void FromString_ThrowsInvalidCastException(string value)
        {
            Assert.Throws<InvalidCastException>(() => DateType.FromString(value));
        }

        public static IEnumerable<object[]> FromObject_TestData()
        {
            // null.
            yield return new object[] { null, default(DateTime) };
        }

        public static IEnumerable<object[]> FromObject_NotSupported_TestData()
        {
            // byte.
            yield return new object[] { byte.MinValue };
            yield return new object[] { (byte)1 };
            yield return new object[] { byte.MaxValue };
            yield return new object[] { (ByteEnum)byte.MinValue };
            yield return new object[] { (ByteEnum)1 };
            yield return new object[] { (ByteEnum)byte.MaxValue };

            // sbyte.
            yield return new object[] { sbyte.MinValue };
            yield return new object[] { (sbyte)(-1) };
            yield return new object[] { (sbyte)0 };
            yield return new object[] { (sbyte)1 };
            yield return new object[] { sbyte.MaxValue };
            yield return new object[] { (SByteEnum)sbyte.MinValue };
            yield return new object[] { (SByteEnum)(-1) };
            yield return new object[] { (SByteEnum)0 };
            yield return new object[] { (SByteEnum)1 };
            yield return new object[] { (SByteEnum)sbyte.MaxValue };

            // ushort.
            yield return new object[] { ushort.MinValue };
            yield return new object[] { (ushort)1 };
            yield return new object[] { ushort.MaxValue };
            yield return new object[] { (UShortEnum)ushort.MinValue };
            yield return new object[] { (UShortEnum)1 };
            yield return new object[] { (UShortEnum)ushort.MaxValue };

            // short.
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

            // uint.
            yield return new object[] { uint.MinValue };
            yield return new object[] { (uint)1 };
            yield return new object[] { (UIntEnum)uint.MinValue };
            yield return new object[] { (UIntEnum)1 };

            // int.
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

            // ulong.
            yield return new object[] { ulong.MinValue };
            yield return new object[] { (ulong)1 };
            yield return new object[] { (ULongEnum)ulong.MinValue };
            yield return new object[] { (ULongEnum)1 };

            // long.
            yield return new object[] { (long)(-1) };
            yield return new object[] { (long)0 };
            yield return new object[] { (long)1 };
            yield return new object[] { (LongEnum)(-1) };
            yield return new object[] { (LongEnum)0 };
            yield return new object[] { (LongEnum)1 };

            // float.
            yield return new object[] { (float)(-1) };
            yield return new object[] { (float)0 };
            yield return new object[] { (float)1 };

            // double.
            yield return new object[] { (double)(-1) };
            yield return new object[] { (double)0 };
            yield return new object[] { (double)1 };

            // decimal.
            yield return new object[] { (decimal)(-1) };
            yield return new object[] { (decimal)0 };
            yield return new object[] { (decimal)1 };

            // bool.
            yield return new object[] { true };
            yield return new object[] { false };
        }

        public static IEnumerable<object[]> FromObject_Invalid_TestData()
        {
            yield return new object[] { new object() };
        }

        public static IEnumerable<object[]> FromString_TestData()
        {
            yield return new object[] { "12:00:00 AM", new DateTime(0) };
        }

        public static IEnumerable<object[]> FromString_NotSupported_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { "-1" };
            yield return new object[] { "0" };
            yield return new object[] { "1" };
            yield return new object[] { "&h5" };
            yield return new object[] { "&h0" };
            yield return new object[] { "&o5" };
            yield return new object[] { " &o5" };
            yield return new object[] { "&o0" };
        }

        public static IEnumerable<object[]> FromString_Invalid_TestData()
        {
            yield return new object[] { "" };
            yield return new object[] { "&" };
            yield return new object[] { "&a" };
            yield return new object[] { "&a0" };
            yield return new object[] { "true" };
            yield return new object[] { "false" };
            yield return new object[] { "invalid" };
        }
    }
}
