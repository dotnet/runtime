// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Tests
{
    public abstract class StringBuilderReplaceTests
    {
        [Theory]
        [InlineData("", "a", "!", 0, 0, "")]
        [InlineData("aaaabbbbccccdddd", "a", "!", 0, 16, "!!!!bbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", "a", "!", 2, 3, "aa!!bbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", "a", "!", 4, 1, "aaaabbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", "aab", "!", 2, 2, "aaaabbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", "aab", "!", 2, 3, "aa!bbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", "aa", "!", 0, 16, "!!bbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", "aa", "$!", 0, 16, "$!$!bbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", "aa", "$!$", 0, 16, "$!$$!$bbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", "aaaa", "!", 0, 16, "!bbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", "aaaa", "$!", 0, 16, "$!bbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", "a", "", 0, 16, "bbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", "b", null, 0, 16, "aaaaccccdddd")]
        [InlineData("aaaabbbbccccdddd", "aaaabbbbccccdddd", "", 0, 16, "")]
        [InlineData("aaaabbbbccccdddd", "aaaabbbbccccdddd", "", 16, 0, "aaaabbbbccccdddd")]
        [InlineData("aaaabbbbccccdddd", "aaaabbbbccccdddde", "", 0, 16, "aaaabbbbccccdddd")]
        [InlineData("aaaaaaaaaaaaaaaa", "a", "b", 0, 16, "bbbbbbbbbbbbbbbb")]
        public void Replace_StringBuilder(string value, string oldValue, string newValue, int startIndex, int count, string expected)
        {
            StringBuilder builder;
            if (startIndex == 0 && count == value.Length)
            {
                // Use Replace(string, string) / Replace(ReadOnlySpan<char>, ReadOnlySpan<char>)
                builder = new StringBuilder(value);
                Replace(builder, oldValue, newValue);
                Assert.Equal(expected, builder.ToString());
            }

            // Use Replace(string, string, int, int) / Replace(ReadOnlySpan<char>, ReadOnlySpan<char>, int, int)
            builder = new StringBuilder(value);
            Replace(builder, oldValue, newValue, startIndex, count);
            Assert.Equal(expected, builder.ToString());
        }

        [Fact]
        public void Replace_StringBuilderWithMultipleChunks()
        {
            StringBuilder builder = StringBuilderTests.StringBuilderWithMultipleChunks();
            Replace(builder, "a", "b", builder.Length - 10, 10);
            Assert.Equal(new string('a', builder.Length - 10) + new string('b', 10), builder.ToString());
        }

        [Fact]
        public void Replace_StringBuilderWithMultipleChunks_WholeString()
        {
            StringBuilder builder = StringBuilderTests.StringBuilderWithMultipleChunks();
            Replace(builder, builder.ToString(), "");
            Assert.Same(string.Empty, builder.ToString());
        }

        [Fact]
        public void Replace_StringBuilderWithMultipleChunks_LongString()
        {
            StringBuilder builder = StringBuilderTests.StringBuilderWithMultipleChunks();
            Replace(builder, builder.ToString() + "b", "");
            Assert.Equal(StringBuilderTests.s_chunkSplitSource, builder.ToString());
        }

        [Fact]
        public void Replace_Invalid()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentException>("oldValue", () => Replace(builder, "", "a")); // Old value is empty
            AssertExtensions.Throws<ArgumentException>("oldValue", () => Replace(builder, "", "a", 0, 0)); // Old value is empty

            AssertExtensions.Throws<ArgumentOutOfRangeException>("requiredLength", () => Replace(builder, "o", "oo")); // New length > builder.MaxCapacity
            AssertExtensions.Throws<ArgumentOutOfRangeException>("requiredLength", () => Replace(builder, "o", "oo", 0, 5)); // New length > builder.MaxCapacity

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => Replace(builder, "a", "b", -1, 0)); // Start index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => Replace(builder, "a", "b", 0, -1)); // Count < 0

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => Replace(builder, "a", "b", 6, 0)); // Count + start index > builder.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => Replace(builder, "a", "b", 5, 1)); // Count + start index > builder.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => Replace(builder, "a", "b", 4, 2)); // Count + start index > builder.Length
        }

        protected abstract StringBuilder Replace(StringBuilder builder, string oldValue, string newValue);

        protected abstract StringBuilder Replace(StringBuilder builder, string oldValue, string newValue, int startIndex, int count);
    }

    public class StringBuilderReplaceTests_String : StringBuilderReplaceTests
    {
        [Fact]
        public void Replace_String_Invalid()
        {
            var builder = new StringBuilder(0, 5);
            builder.Append("Hello");

            AssertExtensions.Throws<ArgumentNullException>("oldValue", () => Replace(builder, null, "")); // Old value is null
            AssertExtensions.Throws<ArgumentNullException>("oldValue", () => Replace(builder, null, "a", 0, 0)); // Old value is null
        }

        protected override StringBuilder Replace(StringBuilder builder, string oldValue, string newValue)
            => builder.Replace(oldValue, newValue);

        protected override StringBuilder Replace(StringBuilder builder, string oldValue, string newValue, int startIndex, int count)
            => builder.Replace(oldValue, newValue, startIndex, count);
    }

    public class StringBuilderReplaceTests_Span : StringBuilderReplaceTests
    {
        protected override StringBuilder Replace(StringBuilder builder, string oldValue, string newValue)
            => builder.Replace(oldValue.AsSpan(), newValue.AsSpan());

        protected override StringBuilder Replace(StringBuilder builder, string oldValue, string newValue, int startIndex, int count)
            => builder.Replace(oldValue.AsSpan(), newValue.AsSpan(), startIndex, count);
    }
}
