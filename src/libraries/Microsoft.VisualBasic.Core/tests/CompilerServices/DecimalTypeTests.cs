// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Microsoft.VisualBasic.CompilerServices;
using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public class DecimalTypeTests
    {
        [Theory]
        [InlineData(true, -1)]
        [InlineData(false, 0)]
        public void FromBoolean(bool value, decimal expected)
        {
            Assert.Equal(expected, DecimalType.FromBoolean(value));
        }

        [Theory]
        [InlineData(null, 0)]
        [InlineData("&H123", 291)]
        [InlineData("&O123", 83)]
        [InlineData("123", 123)]
        public void FromString(string value, decimal expected)
        {
            Assert.Equal(expected, DecimalType.FromString(value));
        }

        [Fact]
        public void FromString_Invalid()
        {
            Assert.Throws<OverflowException>(() => DecimalType.FromString("9999999999999999999999999999999999999"));
            Assert.Throws<InvalidCastException>(() => DecimalType.FromString("abc"));
        }

        [Fact]
        public void FromObject()
        {
            Assert.Equal(0m, DecimalType.FromObject(null));
            Assert.Equal(-1m, DecimalType.FromObject(true));
            Assert.Equal(123m, DecimalType.FromObject((byte)123));
            Assert.Equal(123m, DecimalType.FromObject((short)123));
            Assert.Equal(123m, DecimalType.FromObject((int)123));
            Assert.Equal(123m, DecimalType.FromObject((long)123));
            Assert.Equal(123m, DecimalType.FromObject((float)123));
            Assert.Equal(123m, DecimalType.FromObject((double)123));
            Assert.Equal(123m, DecimalType.FromObject((decimal)123));
            Assert.Equal(123m, DecimalType.FromObject("123"));
        }

        [Fact]
        public void FromObject_Invalid()
        {
            Assert.Throws<InvalidCastException>(() => DecimalType.FromObject('1'));
            Assert.Throws<InvalidCastException>(() => DecimalType.FromObject(DateTime.MinValue));
            Assert.Throws<InvalidCastException>(() => DecimalType.FromObject(Guid.Empty));
        }

        [Theory]
        [InlineData("123", 123)]
        [InlineData("\u00A4123", 123)]
        public void Parse(string value, decimal expected)
        {
            Assert.Equal(expected, DecimalType.Parse(value, CultureInfo.InvariantCulture.NumberFormat));
        }

        [Fact]
        public void Parse_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => DecimalType.Parse(null, null));
            Assert.Throws<FormatException>(() => DecimalType.Parse("abc", CultureInfo.InvariantCulture.NumberFormat));
        }
    }
}
