// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void SplitNoMatchSingleResult()
        {
            ReadOnlySpan<char> value = "a b";

            string expected = value.ToString();
            var enumerator = value.Split(',');
            Assert.True(enumerator.MoveNext());
            Assert.Equal(expected, value[enumerator.Current].ToString());
        }

        [Theory]
        [InlineData("", ',', new[] { "" })]
        [InlineData(",", ',', new[] { "", "" })]
        [InlineData(",,", ',', new[] { "", "", "" })]
        [InlineData("ab", ',', new[] { "ab" })]
        [InlineData("a,b", ',', new[] { "a", "b" })]
        [InlineData("a,", ',', new[] { "a", "" })]
        [InlineData(",b", ',', new[] { "", "b" })]
        [InlineData(",a,b", ',', new[] { "", "a", "b" })]
        [InlineData("a,b,", ',', new[] { "a", "b", "" })]
        [InlineData("a,b,c", ',', new[] { "a", "b", "c" })]
        [InlineData("a,,c", ',', new[] { "a", "", "c" })]
        [InlineData(",a,b,c", ',', new[] { "", "a", "b", "c" })]
        [InlineData("a,b,c,", ',', new[] { "a", "b", "c", "" })]
        [InlineData(",a,b,c,", ',', new[] { "", "a", "b", "c", "" })]
        [InlineData("first,second", ',', new[] { "first", "second" })]
        [InlineData("first,", ',', new[] { "first", "" })]
        [InlineData(",second", ',', new[] { "", "second" })]
        [InlineData(",first,second", ',', new[] { "", "first", "second" })]
        [InlineData("first,second,", ',', new[] { "first", "second", "" })]
        [InlineData("first,second,third", ',', new[] { "first", "second", "third" })]
        [InlineData("first,,third", ',', new[] { "first", "", "third" })]
        [InlineData(",first,second,third", ',', new[] { "", "first", "second", "third" })]
        [InlineData("first,second,third,", ',', new[] { "first", "second", "third", "" })]
        [InlineData(",first,second,third,", ',', new[] { "", "first", "second", "third", "" })]
        [InlineData("Foo Bar Baz", ' ', new[] { "Foo", "Bar", "Baz" })]
        [InlineData("Foo Bar Baz ", ' ', new[] { "Foo", "Bar", "Baz", "" })]
        [InlineData(" Foo Bar Baz ", ' ', new[] { "", "Foo", "Bar", "Baz", "" })]
        [InlineData(" Foo  Bar Baz ", ' ', new[] { "", "Foo", "", "Bar", "Baz", "" })]
        public static void SpanSplitCharSeparator(string valueParam, char separator, string[] expectedParam)
        {
            char[][] expected = expectedParam.Select(x => x.ToCharArray()).ToArray();
            AssertEqual(valueParam, valueParam.AsSpan().Split(separator), expected);
        }

        [Theory]
        [InlineData(" Foo Bar Baz,", ", ", new[] { " Foo Bar Baz," })]
        [InlineData(" Foo Bar Baz, ", ", ", new[] { " Foo Bar Baz", "" })]
        [InlineData(", Foo Bar Baz, ", ", ", new[] { "", "Foo Bar Baz", "" })]
        [InlineData(", Foo, Bar, Baz, ", ", ", new[] { "", "Foo", "Bar", "Baz", "" })]
        [InlineData(", , Foo Bar, Baz", ", ", new[] { "", "", "Foo Bar", "Baz" })]
        [InlineData(", , Foo Bar, Baz, , ", ", ", new[] { "", "", "Foo Bar", "Baz", "", "" })]
        [InlineData(", , , , , ", ", ", new[] { "", "", "", "", "", "" })]
        [InlineData("  Foo, Bar  Baz  ", "  ", new[] { "", "Foo, Bar", "Baz", "" })]
        public static void SpanSplitStringSeparator(string valueParam, string separator, string[] expectedParam)
        {
            char[][] expected = expectedParam.Select(x => x.ToCharArray()).ToArray();
            AssertEqual(valueParam, valueParam.AsSpan().Split(separator), expected);
        }

        private static void AssertEqual<T>(ReadOnlySpan<T> orig, SpanSplitEnumerator<T> source, T[][] items) where T : IEquatable<T>
        {
            foreach (var item in items)
            {
                Assert.True(source.MoveNext());
                var slice = orig[source.Current];
                Assert.Equal(item, slice.ToArray());
            }
            Assert.False(source.MoveNext());
        }
    }
}
