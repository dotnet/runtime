// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Text.Tests
{
    public static partial class StringBuilderTests
    {
        [Fact]
        public static void AppendJoin_NullValues_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("values", () => new StringBuilder().AppendJoin('|', (object[])null));
            Assert.Throws<ArgumentNullException>("values", () => new StringBuilder().AppendJoin('|', (IEnumerable<object>)null));
            Assert.Throws<ArgumentNullException>("values", () => new StringBuilder().AppendJoin('|', (string[])null));
            Assert.Throws<ArgumentNullException>("values", () => new StringBuilder().AppendJoin("|", (object[])null));
            Assert.Throws<ArgumentNullException>("values", () => new StringBuilder().AppendJoin("|", (IEnumerable<object>)null));
            Assert.Throws<ArgumentNullException>("values", () => new StringBuilder().AppendJoin("|", (string[])null));
        }

        [Theory]
        [InlineData(new object[0], "")]
        [InlineData(new object[] { null }, "")]
        [InlineData(new object[] { 10 }, "10")]
        [InlineData(new object[] { null, null }, "|")]
        [InlineData(new object[] { null, 20 }, "|20")]
        [InlineData(new object[] { 10, null }, "10|")]
        [InlineData(new object[] { 10, 20 }, "10|20")]
        [InlineData(new object[] { null, null, null }, "||")]
        [InlineData(new object[] { null, null, 30 }, "||30")]
        [InlineData(new object[] { null, 20, null }, "|20|")]
        [InlineData(new object[] { null, 20, 30 }, "|20|30")]
        [InlineData(new object[] { 10, null, null }, "10||")]
        [InlineData(new object[] { 10, null, 30 }, "10||30")]
        [InlineData(new object[] { 10, 20, null }, "10|20|")]
        [InlineData(new object[] { 10, 20, 30 }, "10|20|30")]
        [InlineData(new object[] { "" }, "")]
        [InlineData(new object[] { "", "" }, "|")]
        public static void AppendJoin_TestValues(object[] values, string expected)
        {
            var stringValues = Array.ConvertAll(values, _ => _?.ToString());
            var enumerable = values.Select(_ => _);

            Assert.Equal(expected, new StringBuilder().AppendJoin('|', values).ToString());
            Assert.Equal(expected, new StringBuilder().AppendJoin('|', enumerable).ToString());
            Assert.Equal(expected, new StringBuilder().AppendJoin('|', stringValues).ToString());
            Assert.Equal(expected, new StringBuilder().AppendJoin("|", values).ToString());
            Assert.Equal(expected, new StringBuilder().AppendJoin("|", enumerable).ToString());
            Assert.Equal(expected, new StringBuilder().AppendJoin("|", stringValues).ToString());
        }

        [Fact]
        public static void AppendJoin_NullToStringValues()
        {
            AppendJoin_TestValues(new object[] { new NullToStringObject() }, "");
            AppendJoin_TestValues(new object[] { new NullToStringObject(), new NullToStringObject() }, "|");
        }

        private sealed class NullToStringObject
        {
            public override string ToString() => null;
        }

        [Theory]
        [InlineData(null, "123")]
        [InlineData("", "123")]
        [InlineData(" ", "1 2 3")]
        [InlineData(", ", "1, 2, 3")]
        public static void AppendJoin_TestStringSeparators(string separator, string expected)
        {
            Assert.Equal(expected, new StringBuilder().AppendJoin(separator, new object[] { 1, 2, 3 }).ToString());
            Assert.Equal(expected, new StringBuilder().AppendJoin(separator, Enumerable.Range(1, 3)).ToString());
            Assert.Equal(expected, new StringBuilder().AppendJoin(separator, new string[] { "1", "2", "3" }).ToString());
        }


        private static StringBuilder CreateBuilderWithNoSpareCapacity()
        {
            return new StringBuilder(0, 5).Append("Hello");
        }

        [Theory]
        [InlineData(null, new object[] { null, null })]
        [InlineData("", new object[] { "", "" })]
        [InlineData(" ", new object[] { })]
        [InlineData(", ", new object[] { "" })]
        public static void AppendJoin_NoValues_NoSpareCapacity_DoesNotThrow(string separator, object[] values)
        {
            var stringValues = Array.ConvertAll(values, _ => _?.ToString());
            var enumerable = values.Select(_ => _);

            if (separator?.Length == 1)
            {
                CreateBuilderWithNoSpareCapacity().AppendJoin(separator[0], values);
                CreateBuilderWithNoSpareCapacity().AppendJoin(separator[0], enumerable);
                CreateBuilderWithNoSpareCapacity().AppendJoin(separator[0], stringValues);
            }
            CreateBuilderWithNoSpareCapacity().AppendJoin(separator, values);
            CreateBuilderWithNoSpareCapacity().AppendJoin(separator, enumerable);
            CreateBuilderWithNoSpareCapacity().AppendJoin(separator, stringValues);
        }

        [Theory]
        [InlineData(null, new object[] { " " })]
        [InlineData(" ", new object[] { " " })]
        [InlineData(" ", new object[] { null, null })]
        [InlineData(" ", new object[] { "", "" })]
        public static void AppendJoin_NoSpareCapacity_ThrowsArgumentOutOfRangeException(string separator, object[] values)
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            var stringValues = Array.ConvertAll(values, _ => _?.ToString());
            var enumerable = values.Select(_ => _);

            if (separator?.Length == 1)
            {
                Assert.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => CreateBuilderWithNoSpareCapacity().AppendJoin(separator[0], values));
                Assert.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => CreateBuilderWithNoSpareCapacity().AppendJoin(separator[0], enumerable));
                Assert.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => CreateBuilderWithNoSpareCapacity().AppendJoin(separator[0], stringValues));
            }
            Assert.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => CreateBuilderWithNoSpareCapacity().AppendJoin(separator, values));
            Assert.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => CreateBuilderWithNoSpareCapacity().AppendJoin(separator, enumerable));
            Assert.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => CreateBuilderWithNoSpareCapacity().AppendJoin(separator, stringValues));
        }
    }
}
