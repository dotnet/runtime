// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Xunit;

namespace System.Runtime.Serialization.Formatters.Tests
{
    public class FormatterConverterTests
    {
        [Fact]
        public void InvalidArguments_ThrowExceptions()
        {
            var f = new FormatterConverter();
            Assert.Throws<ArgumentNullException>("value", () => f.Convert(null, typeof(int)));
            Assert.Throws<ArgumentNullException>("value", () => f.Convert(null, TypeCode.Char));
            Assert.Throws<ArgumentNullException>("value", () => f.ToBoolean(null));
            Assert.Throws<ArgumentNullException>("value", () => f.ToByte(null));
            Assert.Throws<ArgumentNullException>("value", () => f.ToChar(null));
            Assert.Throws<ArgumentNullException>("value", () => f.ToDateTime(null));
            Assert.Throws<ArgumentNullException>("value", () => f.ToDecimal(null));
            Assert.Throws<ArgumentNullException>("value", () => f.ToDouble(null));
            Assert.Throws<ArgumentNullException>("value", () => f.ToInt16(null));
            Assert.Throws<ArgumentNullException>("value", () => f.ToInt32(null));
            Assert.Throws<ArgumentNullException>("value", () => f.ToInt64(null));
            Assert.Throws<ArgumentNullException>("value", () => f.ToSByte(null));
            Assert.Throws<ArgumentNullException>("value", () => f.ToSingle(null));
            Assert.Throws<ArgumentNullException>("value", () => f.ToString(null));
            Assert.Throws<ArgumentNullException>("value", () => f.ToUInt16(null));
            Assert.Throws<ArgumentNullException>("value", () => f.ToUInt32(null));
            Assert.Throws<ArgumentNullException>("value", () => f.ToUInt64(null));
        }

        [Fact]
        public void ToMethods_ExpectedValue()
        {
            Assert.True(new FormatterConverter().ToBoolean("true"));
            Assert.Equal((byte)42, new FormatterConverter().ToByte("42"));
            Assert.Equal('c', new FormatterConverter().ToChar("c"));
            Assert.Equal(new DateTime(2000, 1, 1), new FormatterConverter().ToDateTime(new DateTime(2000, 1, 1).ToString(CultureInfo.InvariantCulture)));
            Assert.Equal(1.2m, new FormatterConverter().ToDecimal("1.2"));
            Assert.Equal(1.2, new FormatterConverter().ToDouble("1.2"));
            Assert.Equal((short)42, new FormatterConverter().ToInt16("42"));
            Assert.Equal(42, new FormatterConverter().ToInt32("42"));
            Assert.Equal(42, new FormatterConverter().ToInt64("42"));
            Assert.Equal((sbyte)42, new FormatterConverter().ToSByte("42"));
            Assert.Equal(1.2f, new FormatterConverter().ToSingle("1.2"));
            Assert.Equal("1.2", new FormatterConverter().ToString("1.2"));
            Assert.Equal((ushort)42, new FormatterConverter().ToUInt16("42"));
            Assert.Equal((uint)42, new FormatterConverter().ToUInt32("42"));
            Assert.Equal((ulong)42, new FormatterConverter().ToUInt64("42"));
            Assert.Equal(42, new FormatterConverter().Convert("42", TypeCode.Int32));
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        [InlineData("42", (byte)42)]
        [InlineData("c", 'c')]
        [InlineData("1.2", 1.2)]
        [InlineData("42", (short)42)]
        [InlineData("42", 42)]
        [InlineData("42", (long)42)]
        [InlineData("42", (sbyte)42)]
        [InlineData("1.2", (float)1.2)]
        [InlineData("1.2", "1.2")]
        [InlineData("42", (ushort)42)]
        [InlineData("42", (uint)42)]
        [InlineData("42", (ulong)42)]
        public void Convert_ExpectedValue(string input, object expected)
        {
            Assert.Equal(expected, new FormatterConverter().Convert(input, expected.GetType()));
        }
    }
}
