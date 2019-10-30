// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Microsoft.VisualBasic.CompilerServices;
using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public class DoubleTypeTests
    {
        [Theory]
        [InlineData(null, 0)]
        [InlineData("&H123", 291)]
        [InlineData("&O123", 83)]
        [InlineData("123", 123)]
        public void FromString(string value, double expected)
        {
            Assert.Equal(expected, DoubleType.FromString(value));
        }

        [Fact]
        public void FromString_Invalid()
        {
            Assert.Throws<InvalidCastException>(() => DoubleType.FromString("abc"));
        }

        [Fact]
        public void FromObject()
        {
            Assert.Equal(0d, DoubleType.FromObject(null));
            Assert.Equal(-1d, DoubleType.FromObject(true));
            Assert.Equal(123d, DoubleType.FromObject((byte)123));
            Assert.Equal(123d, DoubleType.FromObject((short)123));
            Assert.Equal(123d, DoubleType.FromObject((int)123));
            Assert.Equal(123d, DoubleType.FromObject((long)123));
            Assert.Equal(123d, DoubleType.FromObject((float)123));
            Assert.Equal(123d, DoubleType.FromObject((double)123));
            Assert.Equal(123d, DoubleType.FromObject((decimal)123));
            Assert.Equal(123d, DoubleType.FromObject("123"));
        }

        [Fact]
        public void FromObject_Invalid()
        {
            Assert.Throws<InvalidCastException>(() => DoubleType.FromObject('1'));
            Assert.Throws<InvalidCastException>(() => DoubleType.FromObject(DateTime.MinValue));
            Assert.Throws<InvalidCastException>(() => DoubleType.FromObject(Guid.Empty));
        }

        [Theory]
        [InlineData("123", 123)]
        [InlineData("\u00A4123", 123)]
        public void Parse(string value, double expected)
        {
            Assert.Equal(expected, DoubleType.Parse(value, CultureInfo.InvariantCulture.NumberFormat));
        }

        [Fact]
        public void Parse_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => DoubleType.Parse(null, null));
            Assert.Throws<FormatException>(() => DoubleType.Parse("abc", CultureInfo.InvariantCulture.NumberFormat));
        }
    }
}
