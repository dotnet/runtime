// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Numerics;
using Xunit;

namespace System.SpanTests
{
    public static partial class IndexerTests
    {
        [Fact]
        public static void IndexerWithIndexTest()
        {
            ReadOnlySpan<char> span = "Hello".AsSpan();
            Assert.Equal('H', span[new Index(0, fromEnd: false)]);
            Assert.Equal('o', span[new Index(1, fromEnd: true)]);
            Assert.Equal(span[new Index(2, fromEnd: false)], span[new Index(span.Length - 2, fromEnd: true)]);

            Assert.Throws<IndexOutOfRangeException>(() => "Hello".AsSpan()[new Index(0, fromEnd: true)]);

            Span<char> span1 = new Span<char>(new char [] { 'H', 'e', 'l', 'l', 'o'});
            Assert.Equal('e', span1[new Index(1, fromEnd: false)]);
            Assert.Equal('l', span1[new Index(2, fromEnd: true)]);
            Assert.Equal(span1[new Index(2, fromEnd: false)], span1[new Index(span.Length - 2, fromEnd: true)]);

            Assert.Throws<IndexOutOfRangeException>(() =>
                new Span<char>(new char [] { 'H', 'e', 'l', 'l', 'o'})[new Index(0, fromEnd: true)]);
        }

        [Fact]
        public static void IndexerWithRangeTest()
        {
            ReadOnlySpan<char> span = "Hello".AsSpan();
            ReadOnlySpan<char> sliced = span[new Range(new Index(1, fromEnd: false), new Index(1, fromEnd: true))];
            Assert.True(span.Slice(1, 3) == sliced);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                { ReadOnlySpan<char> s = "Hello".AsSpan()[new Range(new Index(1, fromEnd: true), new Index(1, fromEnd: false))]; });

            Span<char> span1 = new Span<char>(new char [] { 'H', 'e', 'l', 'l', 'o'});
            Span<char> sliced1 = span1[new Range(new Index(2, fromEnd: false), new Index(1, fromEnd: true))];
            Assert.True(span1.Slice(2, 2) == sliced1);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                { Span<char> s = new Span<char>(new char [] { 'H', 'i' })[new Range(new Index(0, fromEnd: true), new Index(1, fromEnd: false))]; });
        }

        [Fact]
        public static void SlicingUsingIndexAndRangeTest()
        {
            Range range;
            string s = "0123456789ABCDEF";
            ReadOnlySpan<char> roSpan = s.AsSpan();
            Span<char> span = new Span<char>(s.ToCharArray());

            for (int i = 0; i < span.Length; i++)
            {
                Assert.True(span.Slice(i) == span[i..]);
                Assert.True(span.Slice(span.Length - i - 1) == span[^(i + 1)..]);

                Assert.True(roSpan.Slice(i) == roSpan[i..]);
                Assert.True(roSpan.Slice(roSpan.Length - i - 1) == roSpan[^(i + 1)..]);

                range = new Range(Index.FromStart(i), Index.FromEnd(0));
                Assert.True(span.Slice(i, span.Length - i) == span[range]);
                Assert.True(roSpan.Slice(i, roSpan.Length - i) == roSpan[range]);
            }

            range = new Range(Index.FromStart(0), Index.FromStart(span.Length + 1));

            Assert.Throws<ArgumentOutOfRangeException>(delegate() { var spp = new Span<char>(s.ToCharArray())[range]; });
            Assert.Throws<ArgumentOutOfRangeException>(delegate() { var spp = s.AsSpan()[range]; });
        }
    }
}