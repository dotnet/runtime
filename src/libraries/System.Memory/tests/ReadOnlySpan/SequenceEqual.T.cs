// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        [Fact]
        public static void ZeroLengthSequenceEqual()
        {
            int[] a = new int[3];

            ReadOnlySpan<int> first = new ReadOnlySpan<int>(a, 1, 0);
            ReadOnlySpan<int> second = new ReadOnlySpan<int>(a, 2, 0);

            Assert.True(first.SequenceEqual(second));
            Assert.True(first.SequenceEqual(second, null));
            Assert.True(first.SequenceEqual(second, EqualityComparer<int>.Default));
        }

        [Fact]
        public static void SameSpanSequenceEqual()
        {
            int[] a = { 4, 5, 6 };
            ReadOnlySpan<int> span = new ReadOnlySpan<int>(a);

            Assert.True(span.SequenceEqual(span));
            Assert.True(span.SequenceEqual(span, null));
            Assert.True(span.SequenceEqual(span, EqualityComparer<int>.Default));
        }

        [Fact]
        public static void LengthMismatchSequenceEqual()
        {
            int[] a = { 4, 5, 6 };
            ReadOnlySpan<int> first = new ReadOnlySpan<int>(a, 0, 3);
            ReadOnlySpan<int> second = new ReadOnlySpan<int>(a, 0, 2);

            Assert.False(first.SequenceEqual(second));
            Assert.False(first.SequenceEqual(second, null));
            Assert.False(first.SequenceEqual(second, EqualityComparer<int>.Default));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public static void OnSequenceEqualOfEqualSpansMakeSureEveryElementIsCompared(int mode)
        {
            for (int length = 0; length < 100; length++)
            {
                TIntLog log = new TIntLog();

                TInt[] first = new TInt[length];
                TInt[] second = new TInt[length];
                for (int i = 0; i < length; i++)
                {
                    first[i] = second[i] = new TInt(10 * (i + 1), log);
                }

                ReadOnlySpan<TInt> firstSpan = new ReadOnlySpan<TInt>(first);
                ReadOnlySpan<TInt> secondSpan = new ReadOnlySpan<TInt>(second);

                Assert.True(mode switch
                {
                    0 => firstSpan.SequenceEqual(secondSpan),
                    1 => firstSpan.SequenceEqual(secondSpan, null),
                    _ => firstSpan.SequenceEqual(secondSpan, EqualityComparer<TInt>.Default)
                });

                // Make sure each element of the array was compared once. (Strictly speaking, it would not be illegal for
                // SequenceEqual to compare an element more than once but that would be a non-optimal implementation and
                // a red flag. So we'll stick with the stricter test.)
                Assert.Equal(first.Length, log.Count);
                foreach (TInt elem in first)
                {
                    int numCompares = log.CountCompares(elem.Value, elem.Value);
                    Assert.True(numCompares == 1, $"Expected {numCompares} == 1 for element {elem.Value}.");
                }
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public static void SequenceEqualNoMatch(int mode)
        {
            for (int length = 1; length < 32; length++)
            {
                for (int mismatchIndex = 0; mismatchIndex < length; mismatchIndex++)
                {
                    TIntLog log = new TIntLog();

                    TInt[] first = new TInt[length];
                    TInt[] second = new TInt[length];
                    for (int i = 0; i < length; i++)
                    {
                        first[i] = second[i] = new TInt(10 * (i + 1), log);
                    }

                    second[mismatchIndex] = new TInt(second[mismatchIndex].Value + 1, log);

                    ReadOnlySpan<TInt> firstSpan = new ReadOnlySpan<TInt>(first);
                    ReadOnlySpan<TInt> secondSpan = new ReadOnlySpan<TInt>(second);

                    Assert.False(mode switch
                    {
                        0 => firstSpan.SequenceEqual(secondSpan),
                        1 => firstSpan.SequenceEqual(secondSpan, null),
                        _ => firstSpan.SequenceEqual(secondSpan, EqualityComparer<TInt>.Default)
                    });

                    Assert.Equal(1, log.CountCompares(first[mismatchIndex].Value, second[mismatchIndex].Value));
                }
            }
        }

        [Fact]
        public static void MakeSureNoSequenceEqualChecksGoOutOfRange()
        {
            const int GuardValue = 77777;
            const int GuardLength = 50;

            Action<int, int> checkForOutOfRangeAccess =
                delegate (int x, int y)
                {
                    if (x == GuardValue || y == GuardValue)
                        throw new Exception("Detected out of range access in IndexOf()");
                };

            for (int length = 0; length < 100; length++)
            {
                TInt[] first = new TInt[GuardLength + length + GuardLength];
                TInt[] second = new TInt[GuardLength + length + GuardLength];
                for (int i = 0; i < first.Length; i++)
                {
                    first[i] = second[i] = new TInt(GuardValue, checkForOutOfRangeAccess);
                }

                for (int i = 0; i < length; i++)
                {
                    first[GuardLength + i] = second[GuardLength + i] = new TInt(10 * (i + 1), checkForOutOfRangeAccess);
                }

                ReadOnlySpan<TInt> firstSpan = new ReadOnlySpan<TInt>(first, GuardLength, length);
                ReadOnlySpan<TInt> secondSpan = new ReadOnlySpan<TInt>(second, GuardLength, length);

                Assert.True(firstSpan.SequenceEqual(secondSpan));
                Assert.True(firstSpan.SequenceEqual(secondSpan, null));
                Assert.True(firstSpan.SequenceEqual(secondSpan, EqualityComparer<TInt>.Default));
            }
        }

        [Theory]
        [MemberData(nameof(TestHelpers.SequenceEqualsNullData), MemberType = typeof(TestHelpers))]
        public static void SequenceEqualsNullData_String(string[] firstInput, string[] secondInput, bool expected)
        {
            ReadOnlySpan<string> theStrings = firstInput;

            Assert.Equal(expected, theStrings.SequenceEqual(secondInput));
            Assert.Equal(expected, theStrings.SequenceEqual(secondInput, null));
            Assert.Equal(expected, theStrings.SequenceEqual(secondInput, EqualityComparer<string>.Default));
        }

        [Fact]
        public static void SequenceEqual_AlwaysTrueComparer()
        {
            Assert.False(((ReadOnlySpan<int>)new int[1]).SequenceEqual(new int[2], new AlwaysComparer<int>(true)));
            Assert.True(((ReadOnlySpan<int>)new int[2]).SequenceEqual(new int[2], new AlwaysComparer<int>(true)));
            Assert.True(((ReadOnlySpan<int>)new int[2] { 1, 3 }).SequenceEqual(new int[2] { 2, 4 }, new AlwaysComparer<int>(true)));
        }

        [Fact]
        public static void SequenceEqual_AlwaysFalseComparer()
        {
            Assert.False(((ReadOnlySpan<int>)new int[1]).SequenceEqual(new int[2], new AlwaysComparer<int>(false)));
            Assert.False(((ReadOnlySpan<int>)new int[1]).SequenceEqual(new int[2], new AlwaysComparer<int>(false)));
            Assert.False(((ReadOnlySpan<int>)new int[2] { 1, 3 }).SequenceEqual(new int[2] { 2, 4 }, new AlwaysComparer<int>(false)));
        }

        [Fact]
        public static void SequenceEqual_IgnoreCaseComparer()
        {
            string[] lower = new[] { "hello", "world" };
            string[] upper = new[] { "HELLO", "WORLD" };
            string[] different = new[] { "hello", "wurld" };

            Assert.True(((ReadOnlySpan<string>)lower).SequenceEqual(lower));
            Assert.False(((ReadOnlySpan<string>)lower).SequenceEqual(upper));
            Assert.True(((ReadOnlySpan<string>)upper).SequenceEqual(upper));

            Assert.True(((ReadOnlySpan<string>)lower).SequenceEqual(lower, StringComparer.OrdinalIgnoreCase));
            Assert.True(((ReadOnlySpan<string>)lower).SequenceEqual(upper, StringComparer.OrdinalIgnoreCase));
            Assert.True(((ReadOnlySpan<string>)upper).SequenceEqual(upper, StringComparer.OrdinalIgnoreCase));

            Assert.False(((ReadOnlySpan<string>)lower).SequenceEqual(different));
            Assert.False(((ReadOnlySpan<string>)lower).SequenceEqual(different, StringComparer.OrdinalIgnoreCase));
        }

        private sealed class AlwaysComparer<T> : IEqualityComparer<T>
        {
            private readonly bool _result;
            public AlwaysComparer(bool result) => _result = result;
            public bool Equals(T? x, T? y) => _result;
            public int GetHashCode([DisallowNull] T obj) => 0;
        }
    }
}
