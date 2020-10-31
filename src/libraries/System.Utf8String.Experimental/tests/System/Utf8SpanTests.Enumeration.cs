// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Tests;
using Xunit;

namespace System.Text.Tests
{
    public unsafe partial class Utf8SpanTests
    {
        [Fact]
        public static void CharsProperty_FromData()
        {
            using BoundedUtf8Span boundedSpan = new BoundedUtf8Span("\U00000012\U00000123\U00001234\U00101234\U00000012\U00000123\U00001234\U00101234");
            Utf8Span span = boundedSpan.Span;

            var charsEnumerator = span.Chars.GetEnumerator();

            Assert.True(charsEnumerator.MoveNext());
            Assert.Equal('\U00000012', charsEnumerator.Current);
            Assert.True(charsEnumerator.MoveNext());
            Assert.Equal('\U00000123', charsEnumerator.Current);
            Assert.True(charsEnumerator.MoveNext());
            Assert.Equal('\U00001234', charsEnumerator.Current);
            Assert.True(charsEnumerator.MoveNext());
            Assert.Equal('\uDBC4', charsEnumerator.Current);
            Assert.True(charsEnumerator.MoveNext());
            Assert.Equal('\uDE34', charsEnumerator.Current);
            Assert.True(charsEnumerator.MoveNext());
            Assert.Equal('\U00000012', charsEnumerator.Current);
            Assert.True(charsEnumerator.MoveNext());
            Assert.Equal('\U00000123', charsEnumerator.Current);
            Assert.True(charsEnumerator.MoveNext());
            Assert.Equal('\U00001234', charsEnumerator.Current);
            Assert.True(charsEnumerator.MoveNext());
            Assert.Equal('\uDBC4', charsEnumerator.Current);
            Assert.True(charsEnumerator.MoveNext());
            Assert.Equal('\uDE34', charsEnumerator.Current);
            Assert.False(charsEnumerator.MoveNext());
        }

        [Fact]
        public static void CharsProperty_FromEmpty()
        {
            Assert.False(Utf8Span.Empty.Chars.GetEnumerator().MoveNext());
        }

        [Fact]
        public static void RunesProperty_FromData()
        {
            using BoundedUtf8Span boundedSpan = new BoundedUtf8Span("\U00000012\U00000123\U00001234\U00101234\U00000012\U00000123\U00001234\U00101234");
            Utf8Span span = boundedSpan.Span;

            var runesEnumerator = span.Runes.GetEnumerator();

            Assert.True(runesEnumerator.MoveNext());
            Assert.Equal(new Rune(0x0012), runesEnumerator.Current);
            Assert.True(runesEnumerator.MoveNext());
            Assert.Equal(new Rune(0x0123), runesEnumerator.Current);
            Assert.True(runesEnumerator.MoveNext());
            Assert.Equal(new Rune(0x1234), runesEnumerator.Current);
            Assert.True(runesEnumerator.MoveNext());
            Assert.Equal(new Rune(0x101234), runesEnumerator.Current);
            Assert.True(runesEnumerator.MoveNext());
            Assert.Equal(new Rune(0x0012), runesEnumerator.Current);
            Assert.True(runesEnumerator.MoveNext());
            Assert.Equal(new Rune(0x0123), runesEnumerator.Current);
            Assert.True(runesEnumerator.MoveNext());
            Assert.Equal(new Rune(0x1234), runesEnumerator.Current);
            Assert.True(runesEnumerator.MoveNext());
            Assert.Equal(new Rune(0x101234), runesEnumerator.Current);
            Assert.False(runesEnumerator.MoveNext());
        }

        [Fact]
        public static void RunesProperty_FromEmpty()
        {
            Assert.False(Utf8Span.Empty.Runes.GetEnumerator().MoveNext());
        }
    }
}
