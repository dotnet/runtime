// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace System.Text.Tests
{
    public partial class StringBuilderTests : RemoteExecutorTestBase
    {
        [Fact]
        public static void AppendJoin_NullValues_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("values", () => new StringBuilder().AppendJoin('|', (object[])null));
            AssertExtensions.Throws<ArgumentNullException>("values", () => new StringBuilder().AppendJoin('|', (IEnumerable<object>)null));
            AssertExtensions.Throws<ArgumentNullException>("values", () => new StringBuilder().AppendJoin('|', (string[])null));
            AssertExtensions.Throws<ArgumentNullException>("values", () => new StringBuilder().AppendJoin("|", (object[])null));
            AssertExtensions.Throws<ArgumentNullException>("values", () => new StringBuilder().AppendJoin("|", (IEnumerable<object>)null));
            AssertExtensions.Throws<ArgumentNullException>("values", () => new StringBuilder().AppendJoin("|", (string[])null));
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
                AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => CreateBuilderWithNoSpareCapacity().AppendJoin(separator[0], values));
                AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => CreateBuilderWithNoSpareCapacity().AppendJoin(separator[0], enumerable));
                AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => CreateBuilderWithNoSpareCapacity().AppendJoin(separator[0], stringValues));
            }
            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => CreateBuilderWithNoSpareCapacity().AppendJoin(separator, values));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => CreateBuilderWithNoSpareCapacity().AppendJoin(separator, enumerable));
            AssertExtensions.Throws<ArgumentOutOfRangeException>(s_noCapacityParamName, () => CreateBuilderWithNoSpareCapacity().AppendJoin(separator, stringValues));
        }

        [Theory]
        [InlineData("Hello", new char[] { 'a' }, "Helloa")]
        [InlineData("Hello", new char[] { 'b', 'c', 'd' }, "Hellobcd")]
        [InlineData("Hello", new char[] { 'b', '\0', 'd' }, "Hellob\0d")]
        [InlineData("", new char[] { 'e', 'f', 'g' }, "efg")]
        [InlineData("Hello", new char[0], "Hello")]
        public static void Append_CharSpan(string original, char[] value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Append(new ReadOnlySpan<char>(value));
            Assert.Equal(expected, builder.ToString());
        }

        [Theory]
        [InlineData("Hello", 0, new char[] { '\0', '\0', '\0', '\0', '\0' }, 5, new char[] { 'H', 'e', 'l', 'l', 'o' })]
        [InlineData("Hello", 0, new char[] { '\0', '\0', '\0', '\0' }, 4, new char[] { 'H', 'e', 'l', 'l' })]
        [InlineData("Hello", 1, new char[] { '\0', '\0', '\0', '\0', '\0' }, 4, new char[] { 'e', 'l', 'l', 'o', '\0' })]
        public static void CopyTo_CharSpan(string value, int sourceIndex, char[] destination, int count, char[] expected)
        {
            var builder = new StringBuilder(value);
            builder.CopyTo(sourceIndex, new Span<char>(destination), count);
            Assert.Equal(expected, destination);
        }

        [Fact]
        public static void CopyTo_CharSpan_StringBuilderWithMultipleChunks()
        {
            StringBuilder builder = StringBuilderWithMultipleChunks();
            char[] destination = new char[builder.Length];
            builder.CopyTo(0, new Span<char>(destination), destination.Length);
            Assert.Equal(s_chunkSplitSource.ToCharArray(), destination);
        }

        [Fact]
        public static void CopyTo_CharSpan_Invalid()
        {
            var builder = new StringBuilder("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>("sourceIndex", () => builder.CopyTo(-1, new Span<char>(new char[10]), 0)); // Source index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("sourceIndex", () => builder.CopyTo(6, new Span<char>(new char[10]), 0)); // Source index > builder.Length

            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => builder.CopyTo(0, new Span<char>(new char[10]), -1)); // Count < 0

            AssertExtensions.Throws<ArgumentException>(null, () => builder.CopyTo(5, new Span<char>(new char[10]), 1)); // Source index + count > builder.Length
            AssertExtensions.Throws<ArgumentException>(null, () => builder.CopyTo(4, new Span<char>(new char[10]), 2)); // Source index + count > builder.Length

            AssertExtensions.Throws<ArgumentException>(null, () => builder.CopyTo(0, new Span<char>(new char[10]), 11)); // count > destinationArray.Length
        }

        [Theory]
        [InlineData("Hello", 0, new char[] { '\0' }, "\0Hello")]
        [InlineData("Hello", 3, new char[] { 'a', 'b', 'c' }, "Helabclo")]
        [InlineData("Hello", 5, new char[] { 'd', 'e', 'f' }, "Hellodef")]
        [InlineData("Hello", 0, new char[0], "Hello")]
        public static void Insert_CharSpan(string original, int index, char[] value, string expected)
        {
            var builder = new StringBuilder(original);
            builder.Insert(index, new ReadOnlySpan<char>(value));
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public static void Insert_CharSpan_Invalid()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(-1, new ReadOnlySpan<char>(new char[0]))); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(builder.Length + 1, new ReadOnlySpan<char>(new char[0]))); // Index > builder.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("requiredLength", () => builder.Insert(builder.Length, new ReadOnlySpan<char>(new char[1]))); // New length > builder.MaxCapacity
        }
    }
}
