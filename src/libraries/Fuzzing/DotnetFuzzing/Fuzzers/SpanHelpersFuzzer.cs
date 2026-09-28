// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Numerics;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Differential fuzzer for the MemoryExtensions search and comparison methods (SpanHelpers, which read through
/// Unsafe.ReadUnaligned and Vector128/256/512 loads): IndexOf/LastIndexOf of values and sequences, IndexOfAny/LastIndexOfAny with
/// 2, 3, 4, 5 and more values, the *Except and *InRange variants, Contains*, Count, SequenceEqual, SequenceCompareTo,
/// CommonPrefixLength, StartsWith/EndsWith and Replace, for byte, sbyte, char, short, int, long, float and double. Inputs sit
/// right before a guard page, and every answer is compared with a naive loop using EqualityComparer&lt;T&gt;.Default (so NaN finds
/// NaN and -0.0 finds 0.0) or the type's comparison operators.
/// </summary>
/// <remarks>Input layout: [0] element type, [1] palette size, [2..] elements (one byte each, mapped through a palette).</remarks>
internal sealed class SpanHelpersFuzzer : IFuzzer
{
    public string[] TargetAssemblies => [];
    public string[] TargetCoreLibPrefixes { get; } = ["System.SpanHelpers", "System.MemoryExtensions", "System.PackedSpanHelpers"];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4)
        {
            return;
        }

        int palette = 2 + bytes[1] % 30;
        ReadOnlySpan<byte> data = bytes.Slice(2);
        switch (bytes[0] % 8)
        {
            case 0: Run(data, palette, b => (byte)(b % palette * 37)); break;
            case 1: Run(data, palette, b => (sbyte)(b % palette * 37 - 128)); break;
            case 2: Run(data, palette, b => (char)(b % palette * 0x1111 ^ (b & 0x80))); break;
            case 3: Run(data, palette, b => (short)(b % palette * 0x3001 - 0x4000)); break;
            case 4: Run(data, palette, b => b % palette * 0x10001001 - 0x20000000); break;
            case 5: Run(data, palette, b => (long)(b % palette) * 0x0100_0000_0001_0001L - 0x3000_0000_0000_0000L); break;
            case 6: Run(data, palette, b => FloatValue(b % palette)); break;
            default: Run(data, palette, b => (double)FloatValue(b % palette)); break;
        }
    }

    private static float FloatValue(int i) => i switch
    {
        0 => 0f,
        1 => -0f,
        2 => float.NaN,
        3 => -float.NaN,
        4 => BitConverter.Int32BitsToSingle(0x7FC00001),
        5 => float.PositiveInfinity,
        6 => float.NegativeInfinity,
        7 => float.Epsilon,
        _ => i - 10,
    };

    private static void Run<T>(ReadOnlySpan<byte> data, int palette, Func<byte, T> map) where T : unmanaged, INumberBase<T>, IComparisonOperators<T, T, bool>, IComparable<T>
    {
        // The last few bytes pick the needles; the rest is the haystack.
        int needleCount = Math.Min(8, data.Length / 4);
        T[] needles = new T[needleCount + 1];
        for (int i = 0; i < needles.Length; i++)
        {
            needles[i] = map(i < data.Length ? data[data.Length - 1 - i] : (byte)0);
        }

        T[] hay = new T[data.Length - needleCount];
        for (int i = 0; i < hay.Length; i++)
        {
            hay[i] = map(data[i]);
        }

        using PooledBoundedMemory<T> memory = PooledBoundedMemory<T>.Rent(hay, PoisonPagePlacement.After);
        ReadOnlySpan<T> span = memory.Span;
        T v0 = needles[0], v1 = needles.Length > 1 ? needles[1] : v0, v2 = needles.Length > 2 ? needles[2] : v1;
        EqualityComparer<T> eq = EqualityComparer<T>.Default;
        string type = typeof(T).Name;
        string Describe() => $"{type}[{hay.Length}] palette {palette}, needles [{string.Join(", ", needles)}]";

        // Single values.
        Same(span.IndexOf(v0), First(hay, x => eq.Equals(x, v0)), "IndexOf(value)", Describe);
        Same(span.LastIndexOf(v0), Last(hay, x => eq.Equals(x, v0)), "LastIndexOf(value)", Describe);
        Same(span.IndexOfAny(v0, v1), First(hay, x => eq.Equals(x, v0) || eq.Equals(x, v1)), "IndexOfAny(2)", Describe);
        Same(span.LastIndexOfAny(v0, v1), Last(hay, x => eq.Equals(x, v0) || eq.Equals(x, v1)), "LastIndexOfAny(2)", Describe);
        Same(span.IndexOfAny(v0, v1, v2), First(hay, x => eq.Equals(x, v0) || eq.Equals(x, v1) || eq.Equals(x, v2)), "IndexOfAny(3)", Describe);
        Same(span.LastIndexOfAny(v0, v1, v2), Last(hay, x => eq.Equals(x, v0) || eq.Equals(x, v1) || eq.Equals(x, v2)), "LastIndexOfAny(3)", Describe);
        Same(span.IndexOfAnyExcept(v0), First(hay, x => !eq.Equals(x, v0)), "IndexOfAnyExcept(1)", Describe);
        Same(span.LastIndexOfAnyExcept(v0), Last(hay, x => !eq.Equals(x, v0)), "LastIndexOfAnyExcept(1)", Describe);
        Same(span.IndexOfAnyExcept(v0, v1), First(hay, x => !eq.Equals(x, v0) && !eq.Equals(x, v1)), "IndexOfAnyExcept(2)", Describe);
        Same(span.LastIndexOfAnyExcept(v0, v1), Last(hay, x => !eq.Equals(x, v0) && !eq.Equals(x, v1)), "LastIndexOfAnyExcept(2)", Describe);
        Same(span.IndexOfAnyExcept(v0, v1, v2), First(hay, x => !eq.Equals(x, v0) && !eq.Equals(x, v1) && !eq.Equals(x, v2)), "IndexOfAnyExcept(3)", Describe);
        Same(span.LastIndexOfAnyExcept(v0, v1, v2), Last(hay, x => !eq.Equals(x, v0) && !eq.Equals(x, v1) && !eq.Equals(x, v2)), "LastIndexOfAnyExcept(3)", Describe);
        Same(span.Count(v0), hay.Count(x => eq.Equals(x, v0)), "Count(value)", Describe);
        Check(span.Contains(v0) == hay.Any(x => eq.Equals(x, v0)) && span.ContainsAny(v0, v1) == hay.Any(x => eq.Equals(x, v0) || eq.Equals(x, v1))
            && span.ContainsAnyExcept(v0) == hay.Any(x => !eq.Equals(x, v0)), () => $"Contains* disagrees: {Describe()}");

        // Value sets of every size (4 and 5 have dedicated paths).
        for (int count = 1; count <= needles.Length; count++)
        {
            ReadOnlySpan<T> set = needles.AsSpan(0, count);
            T[] setArray = needles[..count];
            bool InSet(T x) => setArray.Any(n => eq.Equals(x, n));
            Same(span.IndexOfAny(set), First(hay, InSet), $"IndexOfAny(span of {count})", Describe);
            Same(span.LastIndexOfAny(set), Last(hay, InSet), $"LastIndexOfAny(span of {count})", Describe);
            Same(span.IndexOfAnyExcept(set), First(hay, x => !InSet(x)), $"IndexOfAnyExcept(span of {count})", Describe);
            Same(span.LastIndexOfAnyExcept(set), Last(hay, x => !InSet(x)), $"LastIndexOfAnyExcept(span of {count})", Describe);
            Check(span.ContainsAny(set) == hay.Any(InSet) && span.ContainsAnyExcept(set) == hay.Any(x => !InSet(x)), () => $"ContainsAny(span of {count}) disagrees: {Describe()}");
        }

        // Ranges (not for floating point, where NaN has no place in a range).
        if (typeof(T) != typeof(float) && typeof(T) != typeof(double))
        {
            T low = v0 < v1 ? v0 : v1, high = v0 < v1 ? v1 : v0;
            bool InRange(T x) => x >= low && x <= high;
            Same(span.IndexOfAnyInRange(low, high), First(hay, InRange), "IndexOfAnyInRange", Describe);
            Same(span.LastIndexOfAnyInRange(low, high), Last(hay, InRange), "LastIndexOfAnyInRange", Describe);
            Same(span.IndexOfAnyExceptInRange(low, high), First(hay, x => !InRange(x)), "IndexOfAnyExceptInRange", Describe);
            Same(span.LastIndexOfAnyExceptInRange(low, high), Last(hay, x => !InRange(x)), "LastIndexOfAnyExceptInRange", Describe);
            Check(span.ContainsAnyInRange(low, high) == hay.Any(InRange) && span.ContainsAnyExceptInRange(low, high) == hay.Any(x => !InRange(x)),
                () => $"ContainsAny*InRange disagrees: {Describe()}");

            // Inverted ranges match nothing.
            if (low != high)
            {
                Same(span.IndexOfAnyInRange(high, low), -1, "IndexOfAnyInRange(high, low)", Describe);
            }
        }

        // Sequences: a slice of the haystack (so it's found) and the needles (usually not).
        int start = hay.Length == 0 ? 0 : data[0] % hay.Length;
        int length = hay.Length == 0 ? 0 : Math.Min(hay.Length - start, 1 + data[^1] % 9);
        foreach (T[] sequence in new[] { hay.AsSpan(start, length).ToArray(), needles, needles[..1] })
        {
            Same(span.IndexOf(sequence), FirstSequence(hay, sequence), $"IndexOf(sequence of {sequence.Length})", Describe);
            Same(span.LastIndexOf(sequence), LastSequence(hay, sequence), $"LastIndexOf(sequence of {sequence.Length})", Describe);
            Same(span.Count(sequence), CountSequence(hay, sequence), $"Count(sequence of {sequence.Length})", Describe);
            Check(span.StartsWith(sequence) == (sequence.Length <= hay.Length && Matches(hay, 0, sequence))
                && span.EndsWith(sequence) == (sequence.Length <= hay.Length && Matches(hay, hay.Length - sequence.Length, sequence)),
                () => $"StartsWith/EndsWith(sequence of {sequence.Length}) disagrees: {Describe()}");
        }

        // Comparisons against a copy with one element changed.
        T[] other = (T[])hay.Clone();
        if (other.Length > 0)
        {
            other[data[0] % other.Length] = v2;
        }

        using PooledBoundedMemory<T> otherMemory = PooledBoundedMemory<T>.Rent(other, PoisonPagePlacement.After);
        ReadOnlySpan<T> otherSpan = otherMemory.Span;
        int prefix = 0;
        while (prefix < hay.Length && eq.Equals(hay[prefix], other[prefix]))
        {
            prefix++;
        }

        Same(span.CommonPrefixLength(otherSpan), prefix, "CommonPrefixLength", Describe);
        Check(span.SequenceEqual(otherSpan) == (prefix == hay.Length), () => $"SequenceEqual disagrees: {Describe()}");
        if (typeof(T) != typeof(float) && typeof(T) != typeof(double))
        {
            int expectedCompare = prefix == hay.Length ? 0 : Comparer<T>.Default.Compare(hay[prefix], other[prefix]);
            int shorter = hay.Length > 0 ? span.Slice(0, hay.Length - 1).SequenceCompareTo(otherSpan) : 0;
            Check(Math.Sign(span.SequenceCompareTo(otherSpan)) == Math.Sign(expectedCompare), () => $"SequenceCompareTo disagrees: {Describe()}");
            Check(hay.Length == 0 || Math.Sign(shorter) == Math.Sign(prefix >= hay.Length - 1 ? -1 : Comparer<T>.Default.Compare(hay[prefix], other[prefix])),
                () => $"SequenceCompareTo of a shorter span disagrees: {Describe()}");
        }

        // Replace, copying and in place.
        T[] replaced = new T[hay.Length];
        span.Replace(replaced, v0, v1);
        T[] inPlace = (T[])hay.Clone();
        inPlace.AsSpan().Replace(v0, v1);
        for (int i = 0; i < hay.Length; i++)
        {
            T expected = eq.Equals(hay[i], v0) ? v1 : hay[i];
            Check(eq.Equals(replaced[i], expected) && eq.Equals(inPlace[i], expected), () => $"Replace disagrees at {i}: {Describe()}");
        }
    }

    private static int First<T>(T[] values, Func<T, bool> predicate) => Array.FindIndex(values, x => predicate(x));
    private static int Last<T>(T[] values, Func<T, bool> predicate) => Array.FindLastIndex(values, x => predicate(x));

    private static bool Matches<T>(T[] hay, int start, T[] sequence)
    {
        for (int i = 0; i < sequence.Length; i++)
        {
            if (!EqualityComparer<T>.Default.Equals(hay[start + i], sequence[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static int FirstSequence<T>(T[] hay, T[] sequence)
    {
        for (int i = 0; i + sequence.Length <= hay.Length; i++)
        {
            if (Matches(hay, i, sequence))
            {
                return i;
            }
        }

        return -1;
    }

    private static int LastSequence<T>(T[] hay, T[] sequence)
    {
        if (sequence.Length == 0)
        {
            return hay.Length;
        }

        for (int i = hay.Length - sequence.Length; i >= 0; i--)
        {
            if (Matches(hay, i, sequence))
            {
                return i;
            }
        }

        return -1;
    }

    private static int CountSequence<T>(T[] hay, T[] sequence)
    {
        if (sequence.Length == 0)
        {
            return hay.Length + 1;
        }

        int count = 0;
        for (int i = 0; i + sequence.Length <= hay.Length;)
        {
            if (Matches(hay, i, sequence))
            {
                count++;
                i += sequence.Length;
            }
            else
            {
                i++;
            }
        }

        return count;
    }

    private static void Same(int actual, int expected, string what, Func<string> describe)
    {
        if (actual != expected)
        {
            throw new InvalidOperationException($"{what} = {actual}, expected {expected}: {describe()}");
        }
    }

    private static void Check(bool condition, Func<string> message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message());
        }
    }
}
