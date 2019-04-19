// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public void FromObject_NotSupported(object value, DateTime expected)
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
        [MemberData(nameof(FromObject_Overflow_TestData))]
        public void FromObject_ThrowsOverflowException(object value)
        {
            Assert.Throws<OverflowException>(() => DateType.FromObject(value));
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
        public void FromString_NotSupported(string value, DateTime expected)
        {
            Assert.Throws<InvalidCastException>(() => DateType.FromString(value));
        }

        [Theory]
        [MemberData(nameof(FromString_Invalid_TestData))]
        public void FromString_ThrowsInvalidCastException(string value)
        {
            Assert.Throws<InvalidCastException>(() => DateType.FromString(value));
        }

        [Theory]
        [MemberData(nameof(FromString_Overflow_TestData))]
        public void FromString_ThrowsOverflowException(string value)
        {
            Assert.Throws<OverflowException>(() => DateType.FromString(value));
        }

        private static IEnumerable<object[]> FromObject_TestData()
        {
            // null.
            yield return new object[] { null, default(DateTime) };
        }

        private static IEnumerable<object[]> FromObject_NotSupported_TestData()
        {
            // byte.
            yield return new object[] { byte.MinValue, default(DateTime) };
            yield return new object[] { (byte)1, default(DateTime) };
            yield return new object[] { byte.MaxValue, default(DateTime) };
            yield return new object[] { (ByteEnum)byte.MinValue, default(DateTime) };
            yield return new object[] { (ByteEnum)1, default(DateTime) };
            yield return new object[] { (ByteEnum)byte.MaxValue, default(DateTime) };

            // sbyte.
            yield return new object[] { sbyte.MinValue, default(DateTime) };
            yield return new object[] { (sbyte)(-1), default(DateTime) };
            yield return new object[] { (sbyte)0, default(DateTime) };
            yield return new object[] { (sbyte)1, default(DateTime) };
            yield return new object[] { sbyte.MaxValue, default(DateTime) };
            yield return new object[] { (SByteEnum)sbyte.MinValue, default(DateTime) };
            yield return new object[] { (SByteEnum)(-1), default(DateTime) };
            yield return new object[] { (SByteEnum)0, default(DateTime) };
            yield return new object[] { (SByteEnum)1, default(DateTime) };
            yield return new object[] { (SByteEnum)sbyte.MaxValue, default(DateTime) };

            // ushort.
            yield return new object[] { ushort.MinValue, default(DateTime) };
            yield return new object[] { (ushort)1, default(DateTime) };
            yield return new object[] { ushort.MaxValue, default(DateTime) };
            yield return new object[] { (UShortEnum)ushort.MinValue, default(DateTime) };
            yield return new object[] { (UShortEnum)1, default(DateTime) };
            yield return new object[] { (UShortEnum)ushort.MaxValue, default(DateTime) };

            // short.
            yield return new object[] { short.MinValue, default(DateTime) };
            yield return new object[] { (short)(-1), default(DateTime) };
            yield return new object[] { (short)0, default(DateTime) };
            yield return new object[] { (short)1, default(DateTime) };
            yield return new object[] { short.MaxValue, default(DateTime) };
            yield return new object[] { (ShortEnum)short.MinValue, default(DateTime) };
            yield return new object[] { (ShortEnum)(-1), default(DateTime) };
            yield return new object[] { (ShortEnum)0, default(DateTime) };
            yield return new object[] { (ShortEnum)1, default(DateTime) };
            yield return new object[] { (ShortEnum)short.MaxValue, default(DateTime) };

            // uint.
            yield return new object[] { uint.MinValue, default(DateTime) };
            yield return new object[] { (uint)1, default(DateTime) };
            yield return new object[] { (UIntEnum)uint.MinValue, default(DateTime) };
            yield return new object[] { (UIntEnum)1, default(DateTime) };

            // int.
            yield return new object[] { int.MinValue, default(DateTime) };
            yield return new object[] { -1, default(DateTime) };
            yield return new object[] { 0, default(DateTime) };
            yield return new object[] { 1, default(DateTime) };
            yield return new object[] { int.MaxValue, default(DateTime) };
            yield return new object[] { (IntEnum)int.MinValue, default(DateTime) };
            yield return new object[] { (IntEnum)(-1), default(DateTime) };
            yield return new object[] { (IntEnum)0, default(DateTime) };
            yield return new object[] { (IntEnum)1, default(DateTime) };
            yield return new object[] { (IntEnum)int.MaxValue, default(DateTime) };

            // ulong.
            yield return new object[] { ulong.MinValue, default(DateTime) };
            yield return new object[] { (ulong)1, default(DateTime) };
            yield return new object[] { (ULongEnum)ulong.MinValue, default(DateTime) };
            yield return new object[] { (ULongEnum)1, default(DateTime) };

            // long.
            yield return new object[] { (long)(-1), default(DateTime) };
            yield return new object[] { (long)0, default(DateTime) };
            yield return new object[] { (long)1, default(DateTime) };
            yield return new object[] { (LongEnum)(-1), default(DateTime) };
            yield return new object[] { (LongEnum)0, default(DateTime) };
            yield return new object[] { (LongEnum)1, default(DateTime) };

            // float.
            yield return new object[] { (float)(-1), default(DateTime) };
            yield return new object[] { (float)0, default(DateTime) };
            yield return new object[] { (float)1, default(DateTime) };

            // double.
            yield return new object[] { (double)(-1), default(DateTime) };
            yield return new object[] { (double)0, default(DateTime) };
            yield return new object[] { (double)1, default(DateTime) };

            // decimal.
            yield return new object[] { (decimal)(-1), default(DateTime) };
            yield return new object[] { (decimal)0, default(DateTime) };
            yield return new object[] { (decimal)1, default(DateTime) };

            // bool.
            yield return new object[] { true, default(DateTime) };
            yield return new object[] { false, default(DateTime) };
        }

        private static IEnumerable<object[]> FromObject_Invalid_TestData()
        {
            yield break;
        }

        private static IEnumerable<object[]> FromObject_Overflow_TestData()
        {
            yield break;
        }

        private static IEnumerable<object[]> FromString_TestData()
        {
            yield return new object[] { "12:00:00 AM", new DateTime(0) };
        }

        private static IEnumerable<object[]> FromString_NotSupported_TestData()
        {
            yield return new object[] { null, default(DateTime) };
            yield return new object[] { "-1", default(DateTime) };
            yield return new object[] { "0", default(DateTime) };
            yield return new object[] { "1", default(DateTime) };
            yield return new object[] { "&h5", default(DateTime) };
            yield return new object[] { "&h0", default(DateTime) };
            yield return new object[] { "&o5", default(DateTime) };
            yield return new object[] { " &o5", default(DateTime) };
            yield return new object[] { "&o0", default(DateTime) };
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
            yield break;
        }
    }
}
