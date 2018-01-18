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

        private static IEnumerable<object[]> Append_StringBuilder_TestData()
        {
            string mediumString = new string('a', 30);
            string largeString = new string('b', 1000);

            var sb1 = new StringBuilder("Hello");
            var sb2 = new StringBuilder("one");
            var sb3 = new StringBuilder(20).Append(mediumString);

            yield return new object[] { new StringBuilder("Hello"), sb1, "HelloHello" };
            yield return new object[] { new StringBuilder("Hello"), sb2, "Helloone" };
            yield return new object[] { new StringBuilder("Hello"), new StringBuilder(), "Hello" };

            yield return new object[] { new StringBuilder("one"), sb3, "one" + mediumString };

            yield return new object[] { new StringBuilder(20).Append(mediumString), sb3, mediumString + mediumString };
            yield return new object[] { new StringBuilder(10).Append(mediumString), sb3, mediumString + mediumString };

            yield return new object[] { new StringBuilder(20).Append(largeString), sb3, largeString + mediumString };
            yield return new object[] { new StringBuilder(10).Append(largeString), sb3, largeString + mediumString };

            yield return new object[] { new StringBuilder(10), sb3, mediumString };
            yield return new object[] { new StringBuilder(30), sb3, mediumString };
            yield return new object[] { new StringBuilder(10), new StringBuilder(20), string.Empty};

            yield return new object[] { sb1, null, "Hello" };
            yield return new object[] { sb1, sb1, "HelloHello" };
        }

        [Theory]
        [MemberData(nameof(Append_StringBuilder_TestData))]
        public static void Append_StringBuilder(StringBuilder s1, StringBuilder s2, string s)
        {
            Assert.Equal(s, s1.Append(s2).ToString());
        }

        private static IEnumerable<object[]> Append_StringBuilder_Substring_TestData()
        {
            string mediumString = new string('a', 30);
            string largeString = new string('b', 1000);

            var sb1 = new StringBuilder("Hello");
            var sb2 = new StringBuilder("one");
            var sb3 = new StringBuilder(20).Append(mediumString);

            yield return new object[] { new StringBuilder("Hello"), sb1, 0, 5, "HelloHello" };
            yield return new object[] { new StringBuilder("Hello"), sb1, 0, 0, "Hello" };
            yield return new object[] { new StringBuilder("Hello"), sb1, 2, 3, "Hellollo" };
            yield return new object[] { new StringBuilder("Hello"), sb1, 2, 2, "Helloll" };
            yield return new object[] { new StringBuilder("Hello"), sb1, 2, 0, "Hello" };
            yield return new object[] { new StringBuilder("Hello"), new StringBuilder(), 0, 0, "Hello" };
            yield return new object[] { new StringBuilder("Hello"), null, 0, 0, "Hello" };
            yield return new object[] { new StringBuilder(), new StringBuilder("Hello"), 2, 3, "llo" };
            yield return new object[] { new StringBuilder("Hello"), sb2, 0, 3, "Helloone" };

            yield return new object[] { new StringBuilder("one"), sb3, 5, 25, "one" + new string('a', 25) };
            yield return new object[] { new StringBuilder("one"), sb3, 5, 20, "one" + new string('a', 20) };
            yield return new object[] { new StringBuilder("one"), sb3, 10, 10, "one" + new string('a', 10) };

            yield return new object[] { new StringBuilder(20).Append(mediumString), sb3, 20, 10, new string('a', 40) };
            yield return new object[] { new StringBuilder(10).Append(mediumString), sb3, 10, 10, new string('a', 40) };

            yield return new object[] { new StringBuilder(20).Append(largeString), new StringBuilder(20).Append(largeString), 100, 50, largeString + new string('b', 50) };
            yield return new object[] { new StringBuilder(10).Append(mediumString), new StringBuilder(20).Append(largeString), 20, 10, mediumString + new string('b', 10) };
            yield return new object[] { new StringBuilder(10).Append(mediumString), new StringBuilder(20).Append(largeString), 100, 50, mediumString + new string('b', 50) };

            yield return new object[] { sb1, sb1, 2, 3, "Hellollo" };
            yield return new object[] { sb2, sb2, 2, 0, "one" };
        }

        [Theory]
        [MemberData(nameof(Append_StringBuilder_Substring_TestData))]
        public static void Append_StringBuilder_Substring(StringBuilder s1, StringBuilder s2, int startIndex, int count, string s)
        {
            Assert.Equal(s, s1.Append(s2, startIndex, count).ToString());
        }

        [Fact]
        public static void Append_StringBuilder_InvalidInput()
        {
            StringBuilder sb = new StringBuilder(5, 5).Append("Hello");

            Assert.Throws<ArgumentOutOfRangeException>(() => sb.Append(sb, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => sb.Append(sb, 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => sb.Append(sb, 4, 5));
            
            Assert.Throws<ArgumentNullException>(() => sb.Append( (StringBuilder)null, 2, 2));
            Assert.Throws<ArgumentNullException>(() => sb.Append((StringBuilder)null, 2, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StringBuilder(3, 6).Append("Hello").Append(sb));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StringBuilder(3, 6).Append("Hello").Append("Hello"));

            Assert.Throws<ArgumentOutOfRangeException>(() => sb.Append(sb));
        }

        public static IEnumerable<object[]> Equals_String_TestData()
        {
            string mediumString = new string('a', 30);
            string largeString = new string('a', 1000);
            string extraLargeString = new string('a', 41000); // 8000 is the maximum chunk size

            var sb1 = new StringBuilder("Hello");
            var sb2 = new StringBuilder(20).Append(mediumString);
            var sb3 = new StringBuilder(20).Append(largeString);
            var sb4 = new StringBuilder(20).Append(extraLargeString);

            yield return new object[] { sb1, "Hello", true };
            yield return new object[] { sb1, "Hel", false };
            yield return new object[] { sb1, "Hellz", false };
            yield return new object[] { sb1, "Helloz", false };
            yield return new object[] { sb1, "", false };
            yield return new object[] { new StringBuilder(), "", true };
            yield return new object[] { new StringBuilder(), "Hello", false };
            yield return new object[] { sb2, mediumString, true };
            yield return new object[] { sb2, "H", false };
            yield return new object[] { sb3, largeString, true };
            yield return new object[] { sb3, "H", false };
            yield return new object[] { sb3, new string('a', 999) + 'b', false };
            yield return new object[] { sb4, extraLargeString, true };
            yield return new object[] { sb4, "H", false };
        }

        [Theory]
        [MemberData(nameof(Equals_String_TestData))]
        public static void Equals(StringBuilder sb1, string value, bool expected)
        {
            Assert.Equal(expected, sb1.Equals(value.AsReadOnlySpan()));
        }
    }
}
