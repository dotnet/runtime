// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.SpanTests;

public static class CommonSuffixLengthTests
{
    public static IEnumerable<object[]> Lengths()
    {
        for (int length = 0; length <= 33; length++)
        {
            yield return new object[] { length };
        }

        foreach (int length in new[] { 63, 64, 65, 127, 128, 129, 257 })
        {
            yield return new object[] { length };
        }
    }

    [Theory]
    [MemberData(nameof(Lengths))]
    public static void EveryMismatchPosition(int length)
    {
        ValidatePositions(length, i => (byte)(i == 0 ? 0 : i % 251 + 1));
        ValidatePositions(length, i => (char)i);
        ValidatePositions(length, i => (short)i);
        ValidatePositions(length, i => i);
        ValidatePositions(length, i => (long)i);
        ValidatePositions(length, i => (ulong)i);
        ValidatePositions(length, i => (DayOfWeek)i);
        ValidatePositions(length, i => (int?)i);
        ValidatePositions(length, i => new EquatableValue(i));
    }

    private static void ValidatePositions<T>(int length, Func<int, T> create)
    {
        T[] first = new T[length + 3];
        T[] second = new T[length + 5];
        for (int i = 0; i < first.Length; i++)
        {
            first[i] = create(i + 1);
        }

        first.AsSpan(1, length).CopyTo(second.AsSpan(3, length));
        for (int suffix = 0; suffix <= length; suffix++)
        {
            T[] changed = (T[])second.Clone();
            if (suffix < length)
            {
                changed[3 + length - suffix - 1] = create(0);
            }

            Validate<T>(first.AsSpan(1, length), changed.AsSpan(3, length), suffix);
            Validate<T>(first.AsSpan(1, length), changed.AsSpan(1, length + 2), suffix);
            Validate<T>(changed.AsSpan(1, length + 2), first.AsSpan(1, length), suffix);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(8)]
    public static void Empty(int length)
    {
        ReadOnlySpan<int> empty = default;
        ReadOnlySpan<int> nullBacked = new ReadOnlySpan<int>((int[])null);
        ReadOnlySpan<int> values = new int[length];
        Validate(empty, values, 0);
        Validate(values, empty, 0);
        Validate(nullBacked, values, 0);
        Validate(values, nullBacked, 0);
        Validate(values.Slice(length), values, 0);

        EqualityComparer<int> comparer = EqualityComparer<int>.Create((x, y) => throw new InvalidOperationException());
        Assert.Equal(0, empty.CommonSuffixLength(values, comparer));
        Assert.Equal(0, values.CommonSuffixLength(nullBacked, comparer));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public static void SameAndOverlappingMemory(int offset)
    {
        ReadOnlySpan<int> values = new[] { 1, 2, 1, 2, 1 };
        Validate(values, values, values.Length);
        Validate(values.Slice(offset), values, values.Length - offset);
        Validate(values, values.Slice(offset), values.Length - offset);
        Validate(values.Slice(0, 3), values.Slice(2), 3);

        int calls = 0;
        EqualityComparer<int> comparer = EqualityComparer<int>.Create((x, y) => { calls++; return false; });
        Assert.Equal(0, values.CommonSuffixLength(values, comparer));
        Assert.Equal(1, calls);
    }

    [Fact]
    public static void DefaultEquality()
    {
        Validate<string>(new[] { "x", "a", null, "b" }, new[] { "y", "a", null, "b" }, 3);
        Validate<string>(new[] { "a", null }, new[] { "a", "b" }, 0);
        Validate<int?>(new int?[] { 1, null, 2 }, new int?[] { 0, 1, null, 2 }, 3);
        Validate<int?>(new int?[] { 1, null }, new int?[] { 1, 0 }, 0);
        Validate<NonEquatableValue>(
            new[] { new NonEquatableValue(1), new NonEquatableValue(2) },
            new[] { new NonEquatableValue(3), new NonEquatableValue(2) }, 1);
        Validate<EqualityObject>(
            new[] { new EqualityObject(1), null, new EqualityObject(2) },
            new[] { new EqualityObject(3), null, new EqualityObject(2) }, 2);
        object instance = new object();
        Validate<object>(new[] { new object(), instance }, new[] { new object(), instance }, 1);
    }

    [Fact]
    public static void FloatingPointEquality()
    {
        float nan1 = BitConverter.Int32BitsToSingle(unchecked((int)0x7FC00001));
        float nan2 = BitConverter.Int32BitsToSingle(unchecked((int)0xFFC00002));
        Validate<float>(new[] { 1f, nan1, -0f, float.PositiveInfinity, float.NegativeInfinity },
            new[] { 2f, nan2, +0f, float.PositiveInfinity, float.NegativeInfinity }, 4);
        double doubleNan1 = BitConverter.Int64BitsToDouble(0x7FF8000000000001);
        double doubleNan2 = BitConverter.Int64BitsToDouble(unchecked((long)0xFFF8000000000002));
        Validate<double>(new[] { 1d, doubleNan1, -0d, double.PositiveInfinity, double.NegativeInfinity },
            new[] { 2d, doubleNan2, +0d, double.PositiveInfinity, double.NegativeInfinity }, 4);
        Validate<float>(new[] { float.PositiveInfinity }, new[] { float.NegativeInfinity }, 0);
        Validate<double>(new[] { double.NaN }, new[] { 1d }, 0);
    }

    [Fact]
    public static void CustomEquality()
    {
        ReadOnlySpan<string> first = new[] { "x", "a", null, "b" };
        ReadOnlySpan<string> second = new[] { "y", "A", null, "B" };
        Assert.Equal(3, first.CommonSuffixLength(second, StringComparer.OrdinalIgnoreCase));
        Assert.Equal(0, first.CommonSuffixLength(second));

        ReadOnlySpan<int> integers = new[] { 1, 2, -3, 4 };
        EqualityComparer<int> comparer = EqualityComparer<int>.Create((x, y) => Math.Abs(x) == Math.Abs(y));
        Assert.Equal(3, integers.CommonSuffixLength(new[] { 0, -2, 3, -4 }, comparer));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(0, 4)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(1, 4)]
    [InlineData(2, 0)]
    [InlineData(2, 1)]
    [InlineData(2, 4)]
    public static void ComparerOrderAndStopping(int longer, int suffix)
    {
        int[] first = longer == 1 ? new[] { 99, 1, 2, 3, 4 } : new[] { 1, 2, 3, 4 };
        int[] second = longer == 2 ? new[] { 99, 11, 12, 13, 14 } : new[] { 11, 12, 13, 14 };
        var calls = new List<(int, int)>();
        EqualityComparer<int> comparer = EqualityComparer<int>.Create((x, y) =>
        {
            calls.Add((x, y));
            return x + 10 == y && calls.Count <= suffix;
        });
        Assert.Equal(suffix, ((ReadOnlySpan<int>)first).CommonSuffixLength(second, comparer));
        int expectedCalls = Math.Min(suffix + 1, 4);
        Assert.Equal(expectedCalls, calls.Count);
        for (int i = 0; i < expectedCalls; i++)
        {
            Assert.Equal((4 - i, 14 - i), calls[i]);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public static void ComparerException(int throwOnCall)
    {
        int calls = 0;
        var exception = new InvalidOperationException();
        EqualityComparer<int> comparer = EqualityComparer<int>.Create((x, y) =>
        {
            if (++calls == throwOnCall)
            {
                throw exception;
            }

            return true;
        });
        int[] values = new[] { 1, 2, 3 };
        Assert.Same(exception, Assert.Throws<InvalidOperationException>(
            () => ((ReadOnlySpan<int>)values).CommonSuffixLength(values, comparer)));
        Assert.Equal(throwOnCall, calls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(42)]
    public static void Differential(int seed)
    {
        var random = new Random(seed);
        EqualityComparer<int> comparer = EqualityComparer<int>.Create((x, y) => Math.Abs(x) == Math.Abs(y));
        for (int iteration = 0; iteration < 300; iteration++)
        {
            int[] first = new int[random.Next(150)];
            int[] second = new int[random.Next(150)];
            for (int i = 0; i < first.Length; i++)
            {
                first[i] = random.Next(-3, 4);
            }

            for (int i = 0; i < second.Length; i++)
            {
                second[i] = random.Next(-3, 4);
            }

            int copyLength = random.Next(Math.Min(first.Length, second.Length) + 1);
            first.AsSpan(first.Length - copyLength).CopyTo(second.AsSpan(second.Length - copyLength));
            Validate<int>(first, second, Oracle(first, second, EqualityComparer<int>.Default));
            Assert.Equal(Oracle(first, second, comparer), ((ReadOnlySpan<int>)first).CommonSuffixLength(second, comparer));
        }
    }

    private static int Oracle(int[] first, int[] second, IEqualityComparer<int> comparer)
    {
        int run = 0;
        int offset = second.Length - first.Length;
        // Scan forwards, resetting the matching run at each mismatch.
        for (int i = Math.Max(0, -offset); i < first.Length; i++)
        {
            run = comparer.Equals(first[i], second[i + offset]) ? run + 1 : 0;
        }

        return run;
    }

    private static void Validate<T>(ReadOnlySpan<T> first, ReadOnlySpan<T> second, int expected)
    {
        Assert.Equal(expected, first.CommonSuffixLength(second));
        Assert.Equal(expected, first.CommonSuffixLength(second, null));
        Assert.Equal(expected, first.CommonSuffixLength(second, EqualityComparer<T>.Default));
        EqualityComparer<T> comparer = EqualityComparer<T>.Create((x, y) => EqualityComparer<T>.Default.Equals(x, y));
        Assert.Equal(expected, first.CommonSuffixLength(second, comparer));
    }

    private readonly struct NonEquatableValue(int value)
    {
        public readonly int Value = value;
    }

    private readonly struct EquatableValue(int value) : IEquatable<EquatableValue>
    {
        private readonly int _value = value;
        public bool Equals(EquatableValue other) => _value == other._value;
        public override bool Equals(object obj) => obj is EquatableValue other && Equals(other);
        public override int GetHashCode() => _value;
    }

    private sealed class EqualityObject(int value)
    {
        private readonly int _value = value;
        public override bool Equals(object obj) => obj is EqualityObject other && _value == other._value;
        public override int GetHashCode() => _value;
    }
}
