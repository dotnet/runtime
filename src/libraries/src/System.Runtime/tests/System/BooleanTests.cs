// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Tests
{
    public static class BooleanTests
    {
        [Fact]
        public static void TrueString()
        {
            Assert.Equal("True", bool.TrueString);
        }

        [Fact]
        public static void FalseString()
        {
            Assert.Equal("False", bool.FalseString);
        }

        [Theory]
        [InlineData("True", true)]
        [InlineData("true", true)]
        [InlineData("TRUE", true)]
        [InlineData("tRuE", true)]
        [InlineData("  True  ", true)]
        [InlineData("True\0", true)]
        [InlineData(" \0 \0  True   \0 ", true)]
        [InlineData("False", false)]
        [InlineData("false", false)]
        [InlineData("FALSE", false)]
        [InlineData("fAlSe", false)]
        [InlineData("False  ", false)]
        [InlineData("False\0", false)]
        [InlineData("  False \0\0\0  ", false)]
        public static void Parse(string value, bool expected)
        {
            bool result;
            Assert.True(bool.TryParse(value, out result));
            Assert.Equal(expected, result);

            Assert.Equal(expected, bool.Parse(value));
        }

        [Theory]
        [InlineData(null, typeof(ArgumentNullException))]
        [InlineData("", typeof(FormatException))]
        [InlineData(" ", typeof(FormatException))]
        [InlineData("Garbage", typeof(FormatException))]
        [InlineData("True\0Garbage", typeof(FormatException))]
        [InlineData("True\0True", typeof(FormatException))]
        [InlineData("True True", typeof(FormatException))]
        [InlineData("True False", typeof(FormatException))]
        [InlineData("False True", typeof(FormatException))]
        [InlineData("Fa lse", typeof(FormatException))]
        [InlineData("T", typeof(FormatException))]
        [InlineData("0", typeof(FormatException))]
        [InlineData("1", typeof(FormatException))]
        public static void Parse_Invalid(string value, Type exceptionType)
        {
            Assert.Throws(exceptionType, () => bool.Parse(value));

            bool result;
            Assert.False(bool.TryParse(value, out result));
            Assert.False(result);
        }

        [Fact]
        public static void ToStringTest()
        {
            Assert.Equal(bool.TrueString, true.ToString());
            Assert.Equal(bool.FalseString, false.ToString());
        }

        [Theory]
        [InlineData(true, true, 0)]
        [InlineData(true, false, 1)]
        [InlineData(true, null, 1)]
        [InlineData(false, false, 0)]
        [InlineData(false, true, -1)]
        [InlineData(false, null, 1)]
        public static void CompareTo(bool b, object obj, int expected)
        {
            if (obj is bool)
            {
                Assert.Equal(expected, Math.Sign(b.CompareTo((bool)obj)));
            }

            IComparable comparable = b;
            Assert.Equal(expected, Math.Sign(comparable.CompareTo(obj)));
        }

        [Theory]
        [InlineData(true, 1)]
        [InlineData(true, "true")]
        [InlineData(false, 0)]
        [InlineData(false, "false")]
        private static void CompareTo_ObjectNotBool_ThrowsArgumentException(IComparable b, object obj)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => b.CompareTo(obj));
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, false, false)]
        [InlineData(true, "1", false)]
        [InlineData(true, "True", false)]
        [InlineData(true, null, false)]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(false, "0", false)]
        [InlineData(false, "False", false)]
        [InlineData(false, null, false)]
        public static void Equals(bool b1, object obj, bool expected)
        {
            if (obj is bool)
            {
                Assert.Equal(expected, b1.Equals((bool)obj));
                Assert.Equal(expected, b1.GetHashCode().Equals(obj.GetHashCode()));
            }
            Assert.Equal(expected, b1.Equals(obj));
        }

        [Fact]
        public static void GetHashCodeTest()
        {
            Assert.Equal(1, true.GetHashCode());
            Assert.Equal(0, false.GetHashCode());
        }
    }
}
