// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace System.Tests
{
    public partial class DecimalTests : RemoteExecutorTestBase
    {
        public static IEnumerable<object[]> FromOACurrency_TestData()
        {
            yield return new object[] { 0L, 0m };
            yield return new object[] { 1L, 0.0001m };
            yield return new object[] { 100000L, 10m };
            yield return new object[] { 10000000000L, 1000000m };
            yield return new object[] { 1000000000000000000L, 100000000000000m };
            yield return new object[] { 9223372036854775807L, 922337203685477.5807m };
            yield return new object[] { -9223372036854775808L, -922337203685477.5808m };
            yield return new object[] { 123456789L, 12345.6789m };
            yield return new object[] { 1234567890000L, 123456789m };
            yield return new object[] { 1234567890987654321L, 123456789098765.4321m };
            yield return new object[] { 4294967295L, 429496.7295m };
        }

        [Theory]
        [MemberData(nameof(FromOACurrency_TestData))]
        public static void FromOACurrency(long oac, decimal expected)
        {
            Assert.Equal(expected, decimal.FromOACurrency(oac));
        }

        public static IEnumerable<object[]> ToOACurrency_TestData()
        {
            yield return new object[] { 0m, 0L };
            yield return new object[] { 1m, 10000L };
            yield return new object[] { 1.000000000000000m, 10000L };
            yield return new object[] { 10000000000m, 100000000000000L };
            yield return new object[] { 10000000000.00000000000000000m, 100000000000000L };
            yield return new object[] { 0.000000000123456789m, 0L };
            yield return new object[] { 0.123456789m, 1235L };
            yield return new object[] { 123456789m, 1234567890000L };
            yield return new object[] { 4294967295m, 42949672950000L };
            yield return new object[] { -79.228162514264337593543950335m, -792282L };
            yield return new object[] { -79228162514264.337593543950335m, -792281625142643376L };
        }

        [Theory]
        [MemberData(nameof(ToOACurrency_TestData))]
        public static void ToOACurrency(decimal value, long expected)
        {
            Assert.Equal(expected, decimal.ToOACurrency(value));
        }
        public static IEnumerable<object[]> Round_Valid_TestData()
        {
            yield return new object[] { 0m, 0m };
            yield return new object[] { 0.1m, 0m };
            yield return new object[] { 0.5m, 0m };
            yield return new object[] { 0.7m, 1m };
            yield return new object[] { 1.3m, 1m };
            yield return new object[] { 1.5m, 2m };
            yield return new object[] { -0.1m, 0m };
            yield return new object[] { -0.5m, 0m };
            yield return new object[] { -0.7m, -1m };
            yield return new object[] { -1.3m, -1m };
            yield return new object[] { -1.5m, -2m };
        }

        [Theory]
        [MemberData(nameof(Round_Valid_TestData))]
        public static void Round(decimal d1, decimal expected)
        {
            Assert.Equal(expected, decimal.Round(d1));
        }

        public static IEnumerable<object[]> Round_Digit_Valid_TestData()
        {
            yield return new object[] { 1.45m, 1, 1.4m };
            yield return new object[] { 1.55m, 1, 1.6m };
            yield return new object[] { 123.456789m, 4, 123.4568m };
            yield return new object[] { 123.456789m, 6, 123.456789m };
            yield return new object[] { 123.456789m, 8, 123.456789m };
            yield return new object[] { -123.456m, 0, -123m };
            yield return new object[] { -123.0000000m, 3, -123.000m };
            yield return new object[] { -123.0000000m, 11, -123.0000000m };
            yield return new object[] { -9999999999.9999999999, 9, -10000000000.000000000m };
            yield return new object[] { -9999999999.9999999999, 10, -9999999999.9999999999 };
        }

        [Theory]
        [MemberData(nameof(Round_Digit_Valid_TestData))]
        public static void Round(decimal d1, int digits, decimal expected)
        {
            Assert.Equal(expected, decimal.Round(d1, digits));
        }

        public static IEnumerable<object[]> Round_Digit_Mid_Valid_TestData()
        {
            yield return new object[] { 1.45m, 1, MidpointRounding.ToEven, 1.4m };
            yield return new object[] { 1.45m, 1, MidpointRounding.AwayFromZero, 1.5m };
            yield return new object[] { 1.55m, 1, MidpointRounding.ToEven, 1.6m };
            yield return new object[] { 1.55m, 1, MidpointRounding.AwayFromZero, 1.6m };
            yield return new object[] { -1.45m, 1, MidpointRounding.ToEven, -1.4m };
            yield return new object[] { -1.45m, 1, MidpointRounding.AwayFromZero, -1.5m };
            yield return new object[] { 123.456789m, 4, MidpointRounding.ToEven, 123.4568m };
            yield return new object[] { 123.456789m, 4, MidpointRounding.AwayFromZero, 123.4568m };
            yield return new object[] { 123.456789m, 6, MidpointRounding.ToEven, 123.456789m };
            yield return new object[] { 123.456789m, 6, MidpointRounding.AwayFromZero, 123.456789m };
            yield return new object[] { 123.456789m, 8, MidpointRounding.ToEven, 123.456789m };
            yield return new object[] { 123.456789m, 8, MidpointRounding.AwayFromZero, 123.456789m };
            yield return new object[] { -123.456m, 0, MidpointRounding.ToEven, -123m };
            yield return new object[] { -123.456m, 0, MidpointRounding.AwayFromZero, -123m };
            yield return new object[] { -123.0000000m, 3, MidpointRounding.ToEven, -123.000m };
            yield return new object[] { -123.0000000m, 3, MidpointRounding.AwayFromZero, -123.000m };
            yield return new object[] { -123.0000000m, 11, MidpointRounding.ToEven, -123.0000000m };
            yield return new object[] { -123.0000000m, 11, MidpointRounding.AwayFromZero, -123.0000000m };
            yield return new object[] { -9999999999.9999999999, 9, MidpointRounding.ToEven, -10000000000.000000000m };
            yield return new object[] { -9999999999.9999999999, 9, MidpointRounding.AwayFromZero, -10000000000.000000000m };
            yield return new object[] { -9999999999.9999999999, 10, MidpointRounding.ToEven, -9999999999.9999999999 };
            yield return new object[] { -9999999999.9999999999, 10, MidpointRounding.AwayFromZero, -9999999999.9999999999 };
        }

        [Theory]
        [MemberData(nameof(Round_Digit_Mid_Valid_TestData))]
        public static void Round(decimal d1, int digits, MidpointRounding m, decimal expected)
        {
            Assert.Equal(expected, decimal.Round(d1, digits, m));
        }

        public static IEnumerable<object[]> Round_Mid_Valid_TestData()
        {
            yield return new object[] { 0m, MidpointRounding.ToEven, 0m };
            yield return new object[] { 0m, MidpointRounding.AwayFromZero, 0m };
            yield return new object[] { 0.1m, MidpointRounding.ToEven, 0m };
            yield return new object[] { 0.1m, MidpointRounding.AwayFromZero, 0m };
            yield return new object[] { 0.5m, MidpointRounding.ToEven, 0m };
            yield return new object[] { 0.5m, MidpointRounding.AwayFromZero, 1m };
            yield return new object[] { 0.7m, MidpointRounding.ToEven, 1m };
            yield return new object[] { 0.7m, MidpointRounding.AwayFromZero, 1m };
            yield return new object[] { 1.3m, MidpointRounding.ToEven, 1m };
            yield return new object[] { 1.3m, MidpointRounding.AwayFromZero, 1m };
            yield return new object[] { 1.5m, MidpointRounding.ToEven, 2m };
            yield return new object[] { 1.5m, MidpointRounding.AwayFromZero, 2m };
            yield return new object[] { -0.1m, MidpointRounding.ToEven, 0m };
            yield return new object[] { -0.1m, MidpointRounding.AwayFromZero, 0m };
            yield return new object[] { -0.5m, MidpointRounding.ToEven, 0m };
            yield return new object[] { -0.5m, MidpointRounding.AwayFromZero, -1m };
            yield return new object[] { -0.7m, MidpointRounding.ToEven, -1m };
            yield return new object[] { -0.7m, MidpointRounding.AwayFromZero, -1m };
            yield return new object[] { -1.3m, MidpointRounding.ToEven, -1m };
            yield return new object[] { -1.3m, MidpointRounding.AwayFromZero, -1m };
            yield return new object[] { -1.5m, MidpointRounding.ToEven, -2m };
            yield return new object[] { -1.5m, MidpointRounding.AwayFromZero, -2m };
        }

        [Theory]
        [MemberData(nameof(Round_Mid_Valid_TestData))]
        public static void Round(decimal d1, MidpointRounding m, decimal expected)
        {
            Assert.Equal(expected, decimal.Round(d1, m));
        }
    }
}