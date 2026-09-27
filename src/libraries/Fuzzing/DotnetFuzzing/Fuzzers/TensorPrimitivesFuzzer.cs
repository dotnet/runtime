// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Numerics;
using System.Numerics.Tensors;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Differential fuzzer for the span-based <see cref="TensorPrimitives"/> APIs.
/// Every vectorized result is compared against a scalar reference built from the element type's own generic-math
/// operators (T.Max, T.MaxMagnitude, T.IsNaN, ...), which is what the TensorPrimitives docs define their semantics by.
/// Inputs live in guard-paged memory so that any read or write outside the spans faults.
/// </summary>
/// <remarks>
/// Input layout:
///   [0] element type selector
///   [1] flags: bit0 poison page before (else after), bit1 palette mode, bit2 tile mode, bit3 y is shifted x
///   [2] element-wise operation selector
///   [3] tile factor (tile mode) / front skip
///   [4] destination offset for the element-wise operation (signed, relative to x)
///   [5..] element data: raw bytes, or one palette index per byte in palette mode
/// Palette mode maps every byte to one of a small set of interesting values (±0, NaN payloads, ±Inf, min/max, subnormals, ...)
/// so ties, signed zeros and NaNs are frequent. Tile mode repeats the data many times so inputs span several vector blocks.
/// </remarks>
internal sealed class TensorPrimitivesFuzzer : IFuzzer
{
    private const int HeaderSize = 5;
    private const int MaxElements = 1 << 18;

    // Known issues on main that would otherwise mask everything else; set TENSOR_FUZZ_STRICT=1 to check them too:
    // * The *Number reductions (MaxNumber, MinNumber, MaxMagnitudeNumber, MinMagnitudeNumber) return NaN whenever the input
    //   contains a NaN, instead of ignoring NaNs as IEEE 754 maximumNumber/minimumNumber require (since 9.0.0).
    // * The NaN-propagating reductions return some NaN, not necessarily the first one as documented, and for Half a
    //   signaling NaN can come back quieted.
    // * HammingDistance<float/double> counts NaN vs NaN as unequal in its vectorized path (Vector.Equals) but as equal in its scalar
    //   path, so the result depends on the span length; it is documented to use EqualityComparer<T>.Default (NaN equals NaN).
    private static readonly bool s_strict = Environment.GetEnvironmentVariable("TENSOR_FUZZ_STRICT") == "1";

    public string[] TargetAssemblies { get; } = ["System.Numerics.Tensors"];
    public string[] TargetCoreLibPrefixes => [];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < HeaderSize)
        {
            return;
        }

        switch (bytes[0] % 14)
        {
            case 0: RunBinaryInteger<byte>(bytes); break;
            case 1: RunBinaryInteger<sbyte>(bytes); break;
            case 2: RunBinaryInteger<short>(bytes); break;
            case 3: RunBinaryInteger<ushort>(bytes); break;
            case 4: RunBinaryInteger<int>(bytes); break;
            case 5: RunBinaryInteger<uint>(bytes); break;
            case 6: RunBinaryInteger<long>(bytes); break;
            case 7: RunBinaryInteger<ulong>(bytes); break;
            case 8: RunBinaryInteger<nint>(bytes); break;
            case 9: RunBinaryInteger<nuint>(bytes); break;
            case 10: RunFloatingPoint<Half>(bytes); break;
            case 11: RunFloatingPoint<float>(bytes); break;
            case 12: RunFloatingPoint<double>(bytes); break;
            case 13: RunBinaryInteger<char>(bytes); break;
        }
    }

    private static void RunBinaryInteger<T>(ReadOnlySpan<byte> bytes)
        where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        T[] data = Decode<T>(bytes);
        using var x = PooledBoundedMemory<T>.Rent(data, Placement(bytes));
        using var y = PooledBoundedMemory<T>.Rent(SecondOperand(bytes, data), Placement(bytes));

        TestNumber<T>(x.Span, y.Span);
        TestBinaryInteger<T>(x.Span, y.Span);
        TestElementWise<T>(bytes, data, y.Span);
    }

    private static void RunFloatingPoint<T>(ReadOnlySpan<byte> bytes)
        where T : unmanaged, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        T[] data = Decode<T>(bytes);
        using var x = PooledBoundedMemory<T>.Rent(data, Placement(bytes));
        using var y = PooledBoundedMemory<T>.Rent(SecondOperand(bytes, data), Placement(bytes));

        TestNumber<T>(x.Span, y.Span);
        TestElementWise<T>(bytes, data, y.Span);
    }

    private static PoisonPagePlacement Placement(ReadOnlySpan<byte> bytes) =>
        (bytes[1] & 1) != 0 ? PoisonPagePlacement.Before : PoisonPagePlacement.After;

    private static T[] SecondOperand<T>(ReadOnlySpan<byte> bytes, T[] data)
        where T : unmanaged
    {
        // Either x rotated by one (so y shares most values and ties with x), or x reversed.
        T[] y = new T[data.Length];
        if ((bytes[1] & 8) != 0)
        {
            for (int i = 0; i < data.Length; i++)
            {
                y[i] = data[(i + 1) % data.Length];
            }
        }
        else
        {
            data.AsSpan().CopyTo(y);
            y.AsSpan().Reverse();
        }

        return y;
    }

    private static T[] Decode<T>(ReadOnlySpan<byte> bytes)
        where T : unmanaged, INumber<T>, IMinMaxValue<T>
    {
        byte flags = bytes[1];
        ReadOnlySpan<byte> payload = bytes.Slice(HeaderSize);

        T[] elements;
        if ((flags & 2) != 0)
        {
            T[] palette = Palette<T>();
            elements = new T[payload.Length];
            for (int i = 0; i < payload.Length; i++)
            {
                elements[i] = palette[payload[i] % palette.Length];
            }
        }
        else
        {
            elements = new T[payload.Length / Unsafe.SizeOf<T>()];
            payload.Slice(0, elements.Length * Unsafe.SizeOf<T>()).CopyTo(MemoryMarshal.AsBytes(elements.AsSpan()));
        }

        if ((flags & 4) != 0 && elements.Length != 0)
        {
            // Tile mode: repeat the elements so the input spans many vectors/blocks, and so equal values recur at
            // positions that straddle vector and block boundaries.
            int factor = 1 + bytes[3] % 96;
            int length = Math.Min(elements.Length * factor, MaxElements);
            T[] tiled = new T[length];
            for (int i = 0; i < length; i++)
            {
                tiled[i] = elements[i % elements.Length];
            }

            elements = tiled;
        }
        else
        {
            // Otherwise drop a few leading elements to vary the alignment of the span.
            int skip = Math.Min(bytes[3] % 8, elements.Length);
            elements = elements.AsSpan(skip).ToArray();
        }

        return elements;
    }

    private static T[] Palette<T>()
        where T : unmanaged, INumber<T>, IMinMaxValue<T>
    {
        List<T> values =
        [
            T.Zero, T.One, T.CreateTruncating(2), T.CreateTruncating(3), T.CreateTruncating(7),
            T.CreateTruncating(-1), T.CreateTruncating(-2), T.CreateTruncating(-3),
            T.MinValue, T.MaxValue, T.MinValue + T.One, T.MaxValue - T.One,
            T.CreateTruncating(0x7F), T.CreateTruncating(0x80), T.CreateTruncating(0xFF), T.CreateTruncating(0x100),
            T.CreateTruncating(0x7FFF), T.CreateTruncating(0x8000), T.CreateTruncating(0x7FFFFFFF), T.CreateTruncating(0x80000000),
        ];

        if (typeof(T) == typeof(float))
        {
            foreach (uint bits in (ReadOnlySpan<uint>)[0x80000000, 0x7FC00000, 0xFFC00000, 0x7FC00001, 0x7F800001, 0xFFFFFFFF, 0x7F800000, 0xFF800000, 0x00000001, 0x807FFFFF, 0x00800000, 0x3F000000, 0xBF000000])
            {
                values.Add(Unsafe.BitCast<uint, T>(bits));
            }
        }
        else if (typeof(T) == typeof(double))
        {
            foreach (ulong bits in (ReadOnlySpan<ulong>)[0x8000000000000000, 0x7FF8000000000000, 0xFFF8000000000000, 0x7FF8000000000001, 0x7FF0000000000001, 0xFFFFFFFFFFFFFFFF, 0x7FF0000000000000, 0xFFF0000000000000, 0x0000000000000001, 0x800FFFFFFFFFFFFF, 0x0010000000000000, 0x3FE0000000000000, 0xBFE0000000000000])
            {
                values.Add(Unsafe.BitCast<ulong, T>(bits));
            }
        }
        else if (typeof(T) == typeof(Half))
        {
            foreach (ushort bits in (ReadOnlySpan<ushort>)[0x8000, 0x7E00, 0xFE00, 0x7E01, 0x7C01, 0xFFFF, 0x7C00, 0xFC00, 0x0001, 0x83FF, 0x0400, 0x3800, 0xB800])
            {
                values.Add(Unsafe.BitCast<ushort, T>(bits));
            }
        }

        return values.ToArray();
    }

    private static void TestNumber<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y)
        where T : unmanaged, INumber<T>
    {
        // Value-returning min/max reductions.
        CheckReduction(x, "Max", static x => TensorPrimitives.Max(x), static (a, b) => T.Max(a, b));
        CheckReduction(x, "Min", static x => TensorPrimitives.Min(x), static (a, b) => T.Min(a, b));
        CheckReduction(x, "MaxMagnitude", static x => TensorPrimitives.MaxMagnitude(x), static (a, b) => T.MaxMagnitude(a, b));
        CheckReduction(x, "MinMagnitude", static x => TensorPrimitives.MinMagnitude(x), static (a, b) => T.MinMagnitude(a, b));
        CheckReduction(x, "MaxNumber", static x => TensorPrimitives.MaxNumber(x), static (a, b) => T.MaxNumber(a, b), ignoresNaN: true);
        CheckReduction(x, "MinNumber", static x => TensorPrimitives.MinNumber(x), static (a, b) => T.MinNumber(a, b), ignoresNaN: true);
        CheckReduction(x, "MaxMagnitudeNumber", static x => TensorPrimitives.MaxMagnitudeNumber(x), static (a, b) => T.MaxMagnitudeNumber(a, b), ignoresNaN: true);
        CheckReduction(x, "MinMagnitudeNumber", static x => TensorPrimitives.MinMagnitudeNumber(x), static (a, b) => T.MinMagnitudeNumber(a, b), ignoresNaN: true);

        // Index-returning min/max reductions.
        CheckIndex(x, "IndexOfMax", TensorPrimitives.IndexOfMax(x), static (a, b) => T.Max(a, b));
        CheckIndex(x, "IndexOfMin", TensorPrimitives.IndexOfMin(x), static (a, b) => T.Min(a, b));
        CheckIndex(x, "IndexOfMaxMagnitude", TensorPrimitives.IndexOfMaxMagnitude(x), static (a, b) => T.MaxMagnitude(a, b));
        CheckIndex(x, "IndexOfMinMagnitude", TensorPrimitives.IndexOfMinMagnitude(x), static (a, b) => T.MinMagnitude(a, b));

        // Any/All predicates.
        CheckPredicate(x, "IsNaN", TensorPrimitives.IsNaNAll(x), TensorPrimitives.IsNaNAny(x), static v => T.IsNaN(v));
        CheckPredicate(x, "IsFinite", TensorPrimitives.IsFiniteAll(x), TensorPrimitives.IsFiniteAny(x), static v => T.IsFinite(v));
        CheckPredicate(x, "IsInfinity", TensorPrimitives.IsInfinityAll(x), TensorPrimitives.IsInfinityAny(x), static v => T.IsInfinity(v));
        CheckPredicate(x, "IsPositiveInfinity", TensorPrimitives.IsPositiveInfinityAll(x), TensorPrimitives.IsPositiveInfinityAny(x), static v => T.IsPositiveInfinity(v));
        CheckPredicate(x, "IsNegativeInfinity", TensorPrimitives.IsNegativeInfinityAll(x), TensorPrimitives.IsNegativeInfinityAny(x), static v => T.IsNegativeInfinity(v));
        CheckPredicate(x, "IsNegative", TensorPrimitives.IsNegativeAll(x), TensorPrimitives.IsNegativeAny(x), static v => T.IsNegative(v));
        CheckPredicate(x, "IsPositive", TensorPrimitives.IsPositiveAll(x), TensorPrimitives.IsPositiveAny(x), static v => T.IsPositive(v));
        CheckPredicate(x, "IsZero", TensorPrimitives.IsZeroAll(x), TensorPrimitives.IsZeroAny(x), static v => T.IsZero(v));
        CheckPredicate(x, "IsNormal", TensorPrimitives.IsNormalAll(x), TensorPrimitives.IsNormalAny(x), static v => T.IsNormal(v));
        CheckPredicate(x, "IsSubnormal", TensorPrimitives.IsSubnormalAll(x), TensorPrimitives.IsSubnormalAny(x), static v => T.IsSubnormal(v));
        CheckPredicate(x, "IsInteger", TensorPrimitives.IsIntegerAll(x), TensorPrimitives.IsIntegerAny(x), static v => T.IsInteger(v));
        CheckPredicate(x, "IsEvenInteger", TensorPrimitives.IsEvenIntegerAll(x), TensorPrimitives.IsEvenIntegerAny(x), static v => T.IsEvenInteger(v));
        CheckPredicate(x, "IsOddInteger", TensorPrimitives.IsOddIntegerAll(x), TensorPrimitives.IsOddIntegerAny(x), static v => T.IsOddInteger(v));
        CheckPredicate(x, "IsRealNumber", TensorPrimitives.IsRealNumberAll(x), TensorPrimitives.IsRealNumberAny(x), static v => T.IsRealNumber(v));
        CheckPredicate(x, "IsCanonical", TensorPrimitives.IsCanonicalAll(x), TensorPrimitives.IsCanonicalAny(x), static v => T.IsCanonical(v));
        CheckPredicate(x, "IsComplexNumber", TensorPrimitives.IsComplexNumberAll(x), TensorPrimitives.IsComplexNumberAny(x), static v => T.IsComplexNumber(v));
        CheckPredicate(x, "IsImaginaryNumber", TensorPrimitives.IsImaginaryNumberAll(x), TensorPrimitives.IsImaginaryNumberAny(x), static v => T.IsImaginaryNumber(v));

        // HammingDistance is documented as counting !EqualityComparer<T>.Default.Equals(x[i], y[i]) (NaN equals NaN).
        if (!x.IsEmpty)
        {
            int expected = 0, expectedIeee = 0;
            for (int i = 0; i < x.Length; i++)
            {
                expected += EqualityComparer<T>.Default.Equals(x[i], y[i]) ? 0 : 1;
                expectedIeee += x[i] == y[i] ? 0 : 1;
            }

            int actual = TensorPrimitives.HammingDistance(x, y);
            Check(actual == expected || (!s_strict && actual == expectedIeee), x, "HammingDistance", $"expected {expected} (IEEE equality: {expectedIeee}), got {actual}");
        }
    }

    private static void TestBinaryInteger<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y)
        where T : unmanaged, IBinaryInteger<T>
    {
        // Integer arithmetic reductions are exact (wrapping), so the result must match a sequential fold bit for bit.
        T sum = T.Zero, sumOfSquares = T.Zero, dot = T.Zero, product = T.One;
        long popCount = 0, hammingBits = 0;
        for (int i = 0; i < x.Length; i++)
        {
            sum = unchecked(sum + x[i]);
            sumOfSquares = unchecked(sumOfSquares + x[i] * x[i]);
            dot = unchecked(dot + x[i] * y[i]);
            product = unchecked(product * x[i]);
            popCount += long.CreateTruncating(T.PopCount(x[i]));
            hammingBits += long.CreateTruncating(T.PopCount(x[i] ^ y[i]));
        }

        if (typeof(T) == typeof(char))
        {
            return; // char only supports the INumber-based APIs above.
        }

        Check(BitEquals(sum, TensorPrimitives.Sum(x)), x, "Sum", $"expected {sum}, got {TensorPrimitives.Sum(x)}");
        Check(BitEquals(sumOfSquares, TensorPrimitives.SumOfSquares(x)), x, "SumOfSquares", $"expected {sumOfSquares}, got {TensorPrimitives.SumOfSquares(x)}");
        Check(BitEquals(dot, TensorPrimitives.Dot(x, y)), x, "Dot", $"expected {dot}, got {TensorPrimitives.Dot(x, y)}");
        Check(popCount == TensorPrimitives.PopCount(x), x, "PopCount", $"expected {popCount}, got {TensorPrimitives.PopCount(x)}");
        if (!x.IsEmpty)
        {
            Check(BitEquals(product, TensorPrimitives.Product(x)), x, "Product", $"expected {product}, got {TensorPrimitives.Product(x)}");
            Check(hammingBits == TensorPrimitives.HammingBitDistance(x, y), x, "HammingBitDistance", $"expected {hammingBits}, got {TensorPrimitives.HammingBitDistance(x, y)}");
        }
    }

    private static void TestElementWise<T>(ReadOnlySpan<byte> bytes, T[] data, ReadOnlySpan<T> y)
        where T : unmanaged, INumber<T>
    {
        if (typeof(T) == typeof(char))
        {
            return;
        }

        int length = data.Length;
        int offset = (sbyte)bytes[4] % (length + 1);

        // x and destination live in one guarded buffer, destination starting 'offset' elements after x
        // (in place for 0, partially overlapping for |offset| < length, disjoint otherwise).
        using var buffer = PooledBoundedMemory<T>.Rent(length + Math.Abs(offset), Placement(bytes));
        Span<T> x = buffer.Span.Slice(offset < 0 ? -offset : 0, length);
        Span<T> destination = buffer.Span.Slice(offset < 0 ? 0 : offset, length);
        data.CopyTo(x);

        bool overlapsPartially = offset != 0 && Math.Abs(offset) < length;
        int op = bytes[2] % 12;

        T[] expected = new T[length];
        Exception? expectedException = null;
        try
        {
            for (int i = 0; i < length; i++)
            {
                expected[i] = ScalarElementWise(op, data[i], y[i]);
            }
        }
        catch (OverflowException ex)
        {
            expectedException = ex;
        }

        Exception? actualException = null;
        try
        {
            VectorElementWise(op, x, y, destination);
        }
        catch (Exception ex) when (ex is ArgumentException or OverflowException)
        {
            actualException = ex;
        }

        string name = $"ElementWise[{op}] offset={offset}";
        if (overlapsPartially)
        {
            Check(actualException is ArgumentException, (ReadOnlySpan<T>)data, name, $"expected ArgumentException for a partially overlapping destination, got {actualException?.GetType().Name ?? "no exception"}");
            return;
        }

        if (expectedException is not null)
        {
            Check(actualException is OverflowException, (ReadOnlySpan<T>)data, name, $"expected OverflowException, got {actualException?.GetType().Name ?? "no exception"}");
            return;
        }

        Check(actualException is null, (ReadOnlySpan<T>)data, name, $"unexpected {actualException}");
        for (int i = 0; i < length; i++)
        {
            Check(SameValue(expected[i], destination[i]), (ReadOnlySpan<T>)data, name, $"destination[{i}]: expected {expected[i]}, got {destination[i]}");
        }
    }

    private static T ScalarElementWise<T>(int op, T x, T y)
        where T : INumber<T> => op switch
        {
            0 => T.Max(x, y),
            1 => T.Min(x, y),
            2 => T.MaxMagnitude(x, y),
            3 => T.MinMagnitude(x, y),
            4 => T.MaxNumber(x, y),
            5 => T.MinNumber(x, y),
            6 => T.MaxMagnitudeNumber(x, y),
            7 => T.MinMagnitudeNumber(x, y),
            8 => unchecked(x + y),
            9 => unchecked(x - y),
            10 => unchecked(x * y),
            _ => T.Abs(x),
        };

    private static void VectorElementWise<T>(int op, ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination)
        where T : INumber<T>
    {
        switch (op)
        {
            case 0: TensorPrimitives.Max(x, y, destination); break;
            case 1: TensorPrimitives.Min(x, y, destination); break;
            case 2: TensorPrimitives.MaxMagnitude(x, y, destination); break;
            case 3: TensorPrimitives.MinMagnitude(x, y, destination); break;
            case 4: TensorPrimitives.MaxNumber(x, y, destination); break;
            case 5: TensorPrimitives.MinNumber(x, y, destination); break;
            case 6: TensorPrimitives.MaxMagnitudeNumber(x, y, destination); break;
            case 7: TensorPrimitives.MinMagnitudeNumber(x, y, destination); break;
            case 8: TensorPrimitives.Add(x, y, destination); break;
            case 9: TensorPrimitives.Subtract(x, y, destination); break;
            case 10: TensorPrimitives.Multiply(x, y, destination); break;
            default: TensorPrimitives.Abs(x, destination); break;
        }
    }

    private static void CheckReduction<T>(ReadOnlySpan<T> x, string name, ReductionFunc<T> actual, Func<T, T, T> scalar, bool ignoresNaN = false)
        where T : unmanaged, INumber<T>
    {
        if (x.IsEmpty)
        {
            bool threw = false;
            try
            {
                actual(x);
            }
            catch (ArgumentException)
            {
                threw = true;
            }

            Check(threw, x, name, "expected ArgumentException for an empty span");
            return;
        }

        T expected = x[0];
        for (int i = 1; i < x.Length; i++)
        {
            expected = scalar(expected, x[i]);
        }

        T result = actual(x);
        if (!s_strict && !BitEquals(expected, result))
        {
            if (ignoresNaN && ContainsNaN(x))
            {
                return;
            }

            if (T.IsNaN(expected) && T.IsNaN(result))
            {
                return;
            }
        }

        Check(BitEquals(expected, result), x, name, $"expected {Describe(expected)}, got {Describe(result)}");
    }

    private static void CheckIndex<T>(ReadOnlySpan<T> x, string name, int actual, Func<T, T, T> scalar)
        where T : unmanaged, INumber<T>
    {
        int expected = x.IsEmpty ? -1 : 0;
        for (int i = 1; i < x.Length; i++)
        {
            if (T.IsNaN(x[expected]))
            {
                break; // The first NaN wins.
            }

            if (T.IsNaN(x[i]))
            {
                expected = i;
                break;
            }

            // Move to i only if the scalar operator strictly prefers x[i]; equal elements keep the earliest index.
            if (!BitEquals(x[i], x[expected]) && BitEquals(scalar(x[expected], x[i]), x[i]))
            {
                expected = i;
            }
        }

        Check(expected == actual, x, name, $"expected index {expected} ({(expected >= 0 ? Describe(x[expected]) : "-")}), got {actual} ({(actual >= 0 && actual < x.Length ? Describe(x[actual]) : "out of range")})");
    }

    private static void CheckPredicate<T>(ReadOnlySpan<T> x, string name, bool all, bool any, Func<T, bool> predicate)
        where T : unmanaged
    {
        bool expectedAll = !x.IsEmpty, expectedAny = false;
        foreach (T value in x)
        {
            bool matches = predicate(value);
            expectedAll &= matches;
            expectedAny |= matches;
        }

        Check(expectedAll == all, x, name + "All", $"expected {expectedAll}, got {all}");
        Check(expectedAny == any, x, name + "Any", $"expected {expectedAny}, got {any}");
    }

    private delegate T ReductionFunc<T>(ReadOnlySpan<T> x);

    private static bool ContainsNaN<T>(ReadOnlySpan<T> x)
        where T : INumber<T>
    {
        foreach (T value in x)
        {
            if (T.IsNaN(value))
            {
                return true;
            }
        }

        return false;
    }

    private static bool BitEquals<T>(T a, T b)
        where T : unmanaged =>
        MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in a)).SequenceEqual(MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in b)));

    // Element-wise arithmetic can legitimately produce a different NaN payload than the scalar operator.
    private static bool SameValue<T>(T a, T b)
        where T : unmanaged, INumber<T> =>
        BitEquals(a, b) || (T.IsNaN(a) && T.IsNaN(b));

    private static string Describe<T>(T value)
        where T : unmanaged =>
        $"{value} [0x{Convert.ToHexString(MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in value)))}]";

    private static void Check<T>(bool condition, ReadOnlySpan<T> x, string name, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(
                $"TensorPrimitives.{name}<{typeof(T).Name}> mismatch (length {x.Length}, vector bits {(Vector512.IsHardwareAccelerated ? 512 : Vector256.IsHardwareAccelerated ? 256 : 128)}): {message}");
        }
    }
}
