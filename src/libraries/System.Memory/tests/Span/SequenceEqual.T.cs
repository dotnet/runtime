// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.SpanTests
{
    public static partial class SpanTests
    {
        [Fact]
        public static void ZeroLengthSequenceEqual()
        {
            int[] a = new int[3];

            Span<int> first = new Span<int>(a, 1, 0);
            Span<int> second = new Span<int>(a, 2, 0);

            Assert.True(first.SequenceEqual(second));
            Assert.True(first.SequenceEqual(second, null));
            Assert.True(first.SequenceEqual(second, EqualityComparer<int>.Default));
        }

        [Fact]
        public static void SameSpanSequenceEqual()
        {
            int[] a = { 4, 5, 6 };
            Span<int> span = new Span<int>(a);

            Assert.True(span.SequenceEqual(span));
            Assert.True(span.SequenceEqual(span, null));
            Assert.True(span.SequenceEqual(span, EqualityComparer<int>.Default));
        }

        [Fact]
        public static void LengthMismatchSequenceEqual()
        {
            int[] a = { 4, 5, 6 };
            Span<int> first = new Span<int>(a, 0, 3);
            Span<int> second = new Span<int>(a, 0, 2);

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

                Span<TInt> firstSpan = new Span<TInt>(first);
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

                    Span<TInt> firstSpan = new Span<TInt>(first);
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

                Span<TInt> firstSpan = new Span<TInt>(first, GuardLength, length);
                Span<TInt> secondSpan = new Span<TInt>(second, GuardLength, length);

                Assert.True(firstSpan.SequenceEqual(secondSpan));
                Assert.True(firstSpan.SequenceEqual(secondSpan, null));
                Assert.True(firstSpan.SequenceEqual(secondSpan, EqualityComparer<TInt>.Default));
            }
        }

        [Theory]
        [MemberData(nameof(TestHelpers.SequenceEqualsNullData), MemberType = typeof(TestHelpers))]
        public static void SequenceEqualsNullData_String(string[] firstInput, string[] secondInput, bool expected)
        {
            Span<string> theStrings = firstInput;

            Assert.Equal(expected, theStrings.SequenceEqual(secondInput));
            Assert.Equal(expected, theStrings.SequenceEqual((ReadOnlySpan<string>)secondInput));

            Assert.Equal(expected, theStrings.SequenceEqual(secondInput, null));
            Assert.Equal(expected, theStrings.SequenceEqual((ReadOnlySpan<string>)secondInput, null));

            Assert.Equal(expected, theStrings.SequenceEqual(secondInput, EqualityComparer<string>.Default));
            Assert.Equal(expected, theStrings.SequenceEqual((ReadOnlySpan<string>)secondInput, EqualityComparer<string>.Default));
        }

        [Theory]
        [InlineData(100)]
        public static void SequenceEquals_OverriddenEqualsReturnsFalse_EqualsFalse(int length)
        {
            Span<StructOverridingEqualsToAlwaysReturnFalse> span1 = Enumerable.Range(0, length).Select(i => new StructOverridingEqualsToAlwaysReturnFalse()).ToArray();
            Assert.False(span1.SequenceEqual(span1.ToArray()));

            Span<StructImplementingIEquatableToAlwaysReturnFalse> span2 = Enumerable.Range(0, length).Select(i => new StructImplementingIEquatableToAlwaysReturnFalse()).ToArray();
            Assert.False(span2.SequenceEqual(span2.ToArray()));
        }

        private struct StructOverridingEqualsToAlwaysReturnFalse
        {
            public override bool Equals([NotNullWhen(true)] object? obj) => false;
            public override int GetHashCode() => 0;
        }

        private struct StructImplementingIEquatableToAlwaysReturnFalse : IEquatable<StructImplementingIEquatableToAlwaysReturnFalse>
        {
            public bool Equals(StructImplementingIEquatableToAlwaysReturnFalse other) => false;
        }

        [Theory]
        [InlineData(100)]
        public static void SequenceEquals_StructWithOddFieldSize_EqualsAsExpected(int length)
        {
            Span<StructWithOddFieldSize> span1 = new StructWithOddFieldSize[length];
            Span<StructWithOddFieldSize> span2 = new StructWithOddFieldSize[length];

            MemoryMarshal.AsBytes(span1).Fill(0);
            MemoryMarshal.AsBytes(span2).Fill(0xFF);

            for (int i = 0; i < length; i++)
            {
                span1[i].Value1 = span2[i].Value1 = (byte)i;
                span1[i].Value2 = span2[i].Value2 = (byte)(i * 2);
                span1[i].Value3 = span2[i].Value3 = (byte)(i * 3);
            }

            Assert.True(span1.SequenceEqual(span2));
            Assert.True(span2.SequenceEqual(span1));

            span1[length / 2].Value2++;

            Assert.False(span1.SequenceEqual(span2));
            Assert.False(span2.SequenceEqual(span1));
        }

        private struct StructWithOddFieldSize
        {
            public byte Value1, Value2, Value3;
        }

        [Theory]
        [InlineData(100)]
        public static void SequenceEquals_StructWithOddFieldSizeAndIEquatable_EqualsAsExpected(int length)
        {
            Span<StructWithOddFieldSizeAndIEquatable> span1 = new StructWithOddFieldSizeAndIEquatable[length];
            Span<StructWithOddFieldSizeAndIEquatable> span2 = new StructWithOddFieldSizeAndIEquatable[length];

            MemoryMarshal.AsBytes(span1).Fill(0);
            MemoryMarshal.AsBytes(span2).Fill(0xFF);

            for (int i = 0; i < length; i++)
            {
                span1[i].Value1 = span2[i].Value1 = (byte)i;
                span1[i].Value2 = span2[i].Value2 = (byte)(i * 2);
                span1[i].Value3 = span2[i].Value3 = (byte)(i * 3);
            }

            Assert.True(span1.SequenceEqual(span2));
            Assert.True(span2.SequenceEqual(span1));

            span1[length / 2].Value2++;

            Assert.False(span1.SequenceEqual(span2));
            Assert.False(span2.SequenceEqual(span1));
        }

        private struct StructWithOddFieldSizeAndIEquatable : IEquatable<StructWithOddFieldSizeAndIEquatable>
        {
            public int Value1;
            public short Value2;
            public byte Value3;

            public bool Equals(StructWithOddFieldSizeAndIEquatable other) =>
                Value1 == other.Value1 &&
                Value2 == other.Value2 &&
                Value3 == other.Value3;

            public override bool Equals([NotNullWhen(true)] object? obj) =>
                obj is StructWithOddFieldSizeAndIEquatable other &&
                Equals(other);

            public override int GetHashCode() =>
                HashCode.Combine(Value1, Value2, Value3);
        }

        [Theory]
        [InlineData(100)]
        public static void SequenceEquals_StructWithExplicitFieldSizeAndNoFields_EqualsAsExpected(int length)
        {
            Span<StructWithExplicitFieldSizeAndNoFields> span1 = new StructWithExplicitFieldSizeAndNoFields[length];
            Span<StructWithExplicitFieldSizeAndNoFields> span2 = new StructWithExplicitFieldSizeAndNoFields[length];

            MemoryMarshal.AsBytes(span1).Fill(0);
            MemoryMarshal.AsBytes(span2).Fill(0xFF);

            Assert.True(span1.SequenceEqual(span2));
            Assert.True(span2.SequenceEqual(span1));
        }

        [StructLayout(LayoutKind.Sequential, Size = 64)]
        private struct StructWithExplicitFieldSizeAndNoFields
        {
        }

        [Theory]
        [InlineData(100)]
        public static void SequenceEquals_StructWithExplicitFieldSizeAndFields_EqualsAsExpected(int length)
        {
            Span<StructWithExplicitFieldSizeAndFields> span1 = new StructWithExplicitFieldSizeAndFields[length];
            Span<StructWithExplicitFieldSizeAndFields> span2 = new StructWithExplicitFieldSizeAndFields[length];

            MemoryMarshal.AsBytes(span1).Fill(0);
            MemoryMarshal.AsBytes(span2).Fill(0xFF);

            for (int i = 0; i < length; i++)
            {
                span1[i].Value = span2[i].Value = i;
            }

            Assert.True(span1.SequenceEqual(span2));
            Assert.True(span2.SequenceEqual(span1));

            span1[length / 2].Value++;

            Assert.False(span1.SequenceEqual(span2));
            Assert.False(span2.SequenceEqual(span1));
        }

        [StructLayout(LayoutKind.Sequential, Size = 64)]
        private struct StructWithExplicitFieldSizeAndFields
        {
            public int Value;
        }

        [Theory]
        [InlineData(100)]
        public static void SequenceEquals_StructWithDoubleField_EqualsAsExpected(int length)
        {
            Span<StructWithDoubleField> span1 = new StructWithDoubleField[length];
            Span<StructWithDoubleField> span2 = new StructWithDoubleField[length];

            MemoryMarshal.AsBytes(span1).Fill(0);
            MemoryMarshal.AsBytes(span2).Fill(0xFF);

            for (int i = 0; i < length; i++)
            {
                span1[i].Value = span2[i].Value = i;
            }

            Assert.True(span1.SequenceEqual(span2));
            Assert.True(span2.SequenceEqual(span1));

            span1[length / 2].Value++;

            Assert.False(span1.SequenceEqual(span2));
            Assert.False(span2.SequenceEqual(span1));
        }

        private struct StructWithDoubleField
        {
            public double Value;
        }
    }
}
