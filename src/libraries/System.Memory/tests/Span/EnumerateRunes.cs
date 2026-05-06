// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void EnumerateRunes_DefaultAndEmpty()
        {
            SpanRuneEnumerator enumerator = default;
            TestGI(enumerator);
            TestI(enumerator);
            Assert.Equal(default, enumerator.Current);
            Assert.False(enumerator.MoveNext());
            Assert.Equal(default, enumerator.Current);

            enumerator = MemoryExtensions.EnumerateRunes(ReadOnlySpan<char>.Empty).GetEnumerator();
            TestGI(enumerator);
            TestI(enumerator);
            Assert.Equal(default, enumerator.Current);
            Assert.False(enumerator.MoveNext());
            Assert.Equal(default, enumerator.Current);

            enumerator = MemoryExtensions.EnumerateRunes(Span<char>.Empty).GetEnumerator();
            TestGI(enumerator);
            TestI(enumerator);
            Assert.Equal(default, enumerator.Current);
            Assert.False(enumerator.MoveNext());
            Assert.Equal(default, enumerator.Current);

            static void TestGI<TEnumerator>(TEnumerator enumerator) where TEnumerator : IEnumerator<Rune>, allows ref struct
            {
                Assert.Equal(default, enumerator.Current);
                try { enumerator.Reset(); } catch (NotSupportedException) { }
                enumerator.Dispose();
                Assert.Equal(default, enumerator.Current);

                Assert.False(enumerator.MoveNext());
                try { enumerator.Reset(); } catch (NotSupportedException) { }
                enumerator.Dispose();
                Assert.Equal(default, enumerator.Current);
                Assert.False(enumerator.MoveNext());
            }

            static void TestI<TEnumerator>(TEnumerator enumerator) where TEnumerator : IEnumerator, allows ref struct
            {
                Assert.Equal(default(Rune), enumerator.Current);
                try { enumerator.Reset(); } catch (NotSupportedException) { }
                Assert.Equal(default(Rune), enumerator.Current);

                Assert.False(enumerator.MoveNext());
                try { enumerator.Reset(); } catch (NotSupportedException) { }
                Assert.Equal(default(Rune), enumerator.Current);
                Assert.False(enumerator.MoveNext());
            }
        }

        [Theory]
        [InlineData(new char[0], new int[0])] // empty
        [InlineData(new char[] { 'x', 'y', 'z' }, new int[] { 'x', 'y', 'z' })]
        [InlineData(new char[] { 'x', '\uD86D', '\uDF54', 'y' }, new int[] { 'x', 0x2B754, 'y' })] // valid surrogate pair
        [InlineData(new char[] { 'x', '\uD86D', 'y' }, new int[] { 'x', 0xFFFD, 'y' })] // standalone high surrogate
        [InlineData(new char[] { 'x', '\uDF54', 'y' }, new int[] { 'x', 0xFFFD, 'y' })] // standalone low surrogate
        [InlineData(new char[] { 'x', '\uD86D' }, new int[] { 'x', 0xFFFD })] // standalone high surrogate at end of string
        [InlineData(new char[] { 'x', '\uDF54' }, new int[] { 'x', 0xFFFD })] // standalone low surrogate at end of string
        [InlineData(new char[] { 'x', '\uD86D', '\uD86D', 'y' }, new int[] { 'x', 0xFFFD, 0xFFFD, 'y' })] // two high surrogates should be two replacement chars
        [InlineData(new char[] { 'x', '\uFFFD', 'y' }, new int[] { 'x', 0xFFFD, 'y' })] // literal U+FFFD
        public static void EnumerateRunes_Battery(char[] chars, int[] expected)
        {
            // Test data is smuggled as char[] instead of straight-up string since the test framework
            // doesn't like invalid UTF-16 literals.

            // first, test Span<char>

            List<int> enumeratedValues = new List<int>();
            foreach (Rune rune in ((Span<char>)chars).EnumerateRunes())
            {
                enumeratedValues.Add(rune.Value);
            }
            Assert.Equal(expected, enumeratedValues.ToArray());
            Assert.Equal(expected, EnumerateRunes_TestGI(((Span<char>)chars).EnumerateRunes()).ToArray());
            Assert.Equal(expected, EnumerateRunes_TestI(((Span<char>)chars).EnumerateRunes()).ToArray());

            // next, ROS<char>

            enumeratedValues.Clear();
            foreach (Rune rune in ((ReadOnlySpan<char>)chars).EnumerateRunes())
            {
                enumeratedValues.Add(rune.Value);
            }
            Assert.Equal(expected, enumeratedValues.ToArray());
            Assert.Equal(expected, EnumerateRunes_TestGI(((ReadOnlySpan<char>)chars).EnumerateRunes()).ToArray());
            Assert.Equal(expected, EnumerateRunes_TestI(((ReadOnlySpan<char>)chars).EnumerateRunes()).ToArray());
        }

        [Fact]
        public static void EnumerateRunes_DoesNotReadPastEndOfSpan()
        {
            // As an optimization, reading scalars from a string *may* read past the end of the string
            // to the terminating null. This optimization is invalid for arbitrary spans, so this test
            // ensures that we're not performing this optimization here.

            ReadOnlySpan<char> span = "xy\U0002B754z".AsSpan(1, 2); // well-formed string, but span splits surrogate pair

            List<int> enumeratedValues = new List<int>();
            foreach (Rune rune in span.EnumerateRunes())
            {
                enumeratedValues.Add(rune.Value);
            }
            Assert.Equal(new int[] { 'y', '\uFFFD' }, enumeratedValues.ToArray());
            Assert.Equal(new int[] { 'y', '\uFFFD' }, EnumerateRunes_TestGI(span.EnumerateRunes()).ToArray());
            Assert.Equal(new int[] { 'y', '\uFFFD' }, EnumerateRunes_TestI(span.EnumerateRunes()).ToArray());
        }

        private static List<int> EnumerateRunes_TestGI<TEnumerator>(TEnumerator enumerator) where TEnumerator : IEnumerator<Rune>, allows ref struct
        {
            List<int> enumeratedValues = new List<int>();

            Assert.Equal(default, enumerator.Current);
            try { enumerator.Reset(); } catch (NotSupportedException) { }
            enumerator.Dispose();
            Assert.Equal(default, enumerator.Current);

            while (enumerator.MoveNext())
            {
                try { enumerator.Reset(); } catch (NotSupportedException) { }
                enumerator.Dispose();

                enumeratedValues.Add(enumerator.Current.Value);

                try { enumerator.Reset(); } catch (NotSupportedException) { }
                enumerator.Dispose();
            }

            Assert.Equal(default, enumerator.Current);
            try { enumerator.Reset(); } catch (NotSupportedException) { }
            enumerator.Dispose();
            Assert.Equal(default, enumerator.Current);

            return enumeratedValues;
        }

        private static List<int> EnumerateRunes_TestI<TEnumerator>(TEnumerator enumerator) where TEnumerator : IEnumerator, allows ref struct
        {
            List<int> enumeratedValues = new List<int>();

            Assert.Equal(default(Rune), enumerator.Current);
            try { enumerator.Reset(); } catch (NotSupportedException) { }
            Assert.Equal(default(Rune), enumerator.Current);

            while (enumerator.MoveNext())
            {
                try { enumerator.Reset(); } catch (NotSupportedException) { }

                enumeratedValues.Add(((Rune)enumerator.Current).Value);

                try { enumerator.Reset(); } catch (NotSupportedException) { }
            }

            Assert.Equal(default(Rune), enumerator.Current);
            try { enumerator.Reset(); } catch (NotSupportedException) { }
            Assert.Equal(default(Rune), enumerator.Current);
            Assert.False(enumerator.MoveNext());

            return enumeratedValues;
        }
    }
}
