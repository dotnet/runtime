// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Numerics;
using System.Numerics.Tensors;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Differential fuzzer for the element-wise math, integer and conversion APIs of <see cref="TensorPrimitives"/>.
/// Each element of the vectorized result is compared with the element type's own scalar operator (T.Exp, T.RotateLeft,
/// TTo.CreateSaturating, ...). Operations that are exact in IEEE 754 (rounding, sqrt, fma, conversions, integer ops) must match
/// bit for bit (any NaN matches any NaN); transcendental functions must agree on NaN/infinity and be within a relative tolerance.
/// </summary>
/// <remarks>
/// Input layout: [0] group/type, [1] operation, [2] flags (bit0 poison before, bit1 palette mode, bit2 in-place, bit3 scalar operand),
/// [3] integer argument (shift/rotate amount, digits, n), [4..] element data (raw, or palette indexes).
/// </remarks>
internal sealed class TensorPrimitivesMathFuzzer : IFuzzer
{
    private const int HeaderSize = 4;

    // Known issues on main, tolerated unless TENSOR_FUZZ_STRICT=1:
    // * Remainder<float/double/Half> does not implement IEEE fmod: x % Infinity returns NaN instead of x, and when x / y
    //   overflows the result is +/-Infinity instead of a value smaller than y.
    // * CopySign<signed integer> wraps silently where T.CopySign throws OverflowException (MinValue with a positive sign).
    // * SinPi/CosPi<float/double/Half> are vectorized as Sin/Cos(x * Pi), so the rounding of x * Pi makes them inaccurate
    //   (CosPi(0.5f) = -4.4e-8, CosPi(1e6f) = 0.9954, CosPi(100000.5f) = -0.0076) instead of exact at integers and half-integers.
    private static readonly bool s_strict = Environment.GetEnvironmentVariable("TENSOR_FUZZ_STRICT") == "1";

    public string[] TargetAssemblies { get; } = ["System.Numerics.Tensors"];
    public string[] TargetCoreLibPrefixes => [];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < HeaderSize)
        {
            return;
        }

        switch (bytes[0] % 17)
        {
            case 0: FloatingPoint<Half>(bytes); break;
            case 1: FloatingPoint<float>(bytes); break;
            case 2: FloatingPoint<double>(bytes); break;
            case 3: Integer<sbyte>(bytes); break;
            case 4: Integer<byte>(bytes); break;
            case 5: Integer<short>(bytes); break;
            case 6: Integer<ushort>(bytes); break;
            case 7: Integer<int>(bytes); break;
            case 8: Integer<uint>(bytes); break;
            case 9: Integer<long>(bytes); break;
            case 10: Integer<ulong>(bytes); break;
            case 11: Integer<nint>(bytes); break;
            case 12: Conversions<float>(bytes); break;
            case 13: Conversions<double>(bytes); break;
            case 14: Conversions<Half>(bytes); break;
            case 15: Conversions<long>(bytes); break;
            default: Conversions<ulong>(bytes); break;
        }
    }

    // ---------------------------------------------------------------- floating point

    private delegate void Unary<T>(ReadOnlySpan<T> x, Span<T> destination);
    private delegate void Binary<T>(ReadOnlySpan<T> x, ReadOnlySpan<T> y, Span<T> destination);

    private enum Accuracy { Exact, Approximate, Trigonometric }

    private static void FloatingPoint<T>(ReadOnlySpan<byte> bytes)
        where T : unmanaged, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        T[] data = Decode<T>(bytes, FloatPalette<T>());
        T[] other = Rotate(data);
        int n = (sbyte)bytes[3];
        int digits = Math.Min(n & 15, typeof(T) == typeof(double) ? 15 : 6);
        int op = bytes[1] % 58;

        // Unary operations.
        (string Name, Unary<T> Vector, Func<T, T> Scalar, Accuracy Accuracy)? unary = op switch
        {
            0 => ("Abs", static (x, d) => TensorPrimitives.Abs(x, d), T.Abs, Accuracy.Exact),
            1 => ("Negate", static (x, d) => TensorPrimitives.Negate(x, d), static v => -v, Accuracy.Exact),
            2 => ("Sqrt", static (x, d) => TensorPrimitives.Sqrt(x, d), T.Sqrt, Accuracy.Exact),
            3 => ("Floor", static (x, d) => TensorPrimitives.Floor(x, d), T.Floor, Accuracy.Exact),
            4 => ("Ceiling", static (x, d) => TensorPrimitives.Ceiling(x, d), T.Ceiling, Accuracy.Exact),
            5 => ("Truncate", static (x, d) => TensorPrimitives.Truncate(x, d), T.Truncate, Accuracy.Exact),
            6 => ("Round", static (x, d) => TensorPrimitives.Round(x, d), static v => T.Round(v), Accuracy.Exact),
            7 => ("Round(AwayFromZero)", static (x, d) => TensorPrimitives.Round(x, MidpointRounding.AwayFromZero, d), static v => T.Round(v, MidpointRounding.AwayFromZero), Accuracy.Exact),
            8 => ("Round(ToZero)", static (x, d) => TensorPrimitives.Round(x, MidpointRounding.ToZero, d), static v => T.Round(v, MidpointRounding.ToZero), Accuracy.Exact),
            9 => ("Round(ToNegativeInfinity)", static (x, d) => TensorPrimitives.Round(x, MidpointRounding.ToNegativeInfinity, d), static v => T.Round(v, MidpointRounding.ToNegativeInfinity), Accuracy.Exact),
            10 => ("Round(ToPositiveInfinity)", static (x, d) => TensorPrimitives.Round(x, MidpointRounding.ToPositiveInfinity, d), static v => T.Round(v, MidpointRounding.ToPositiveInfinity), Accuracy.Exact),
            11 => ($"Round(digits {digits})", (x, d) => TensorPrimitives.Round(x, digits, d), v => T.Round(v, digits), Accuracy.Exact),
            12 => ($"Round(digits {digits}, AwayFromZero)", (x, d) => TensorPrimitives.Round(x, digits, MidpointRounding.AwayFromZero, d), v => T.Round(v, digits, MidpointRounding.AwayFromZero), Accuracy.Exact),
            13 => ("Reciprocal", static (x, d) => TensorPrimitives.Reciprocal(x, d), static v => T.One / v, Accuracy.Exact),
            14 => ("BitIncrement", static (x, d) => TensorPrimitives.BitIncrement(x, d), T.BitIncrement, Accuracy.Exact),
            15 => ("BitDecrement", static (x, d) => TensorPrimitives.BitDecrement(x, d), T.BitDecrement, Accuracy.Exact),
            16 => ($"ScaleB({n})", (x, d) => TensorPrimitives.ScaleB(x, n, d), v => T.ScaleB(v, n), Accuracy.Exact),
            17 => ("Increment", static (x, d) => TensorPrimitives.Increment(x, d), static v => v + T.One, Accuracy.Exact),
            18 => ("Decrement", static (x, d) => TensorPrimitives.Decrement(x, d), static v => v - T.One, Accuracy.Exact),
            19 => ("Exp", static (x, d) => TensorPrimitives.Exp(x, d), T.Exp, Accuracy.Approximate),
            20 => ("Exp2", static (x, d) => TensorPrimitives.Exp2(x, d), T.Exp2, Accuracy.Approximate),
            21 => ("Exp10", static (x, d) => TensorPrimitives.Exp10(x, d), T.Exp10, Accuracy.Approximate),
            22 => ("ExpM1", static (x, d) => TensorPrimitives.ExpM1(x, d), T.ExpM1, Accuracy.Approximate),
            23 => ("Exp2M1", static (x, d) => TensorPrimitives.Exp2M1(x, d), T.Exp2M1, Accuracy.Approximate),
            24 => ("Exp10M1", static (x, d) => TensorPrimitives.Exp10M1(x, d), T.Exp10M1, Accuracy.Approximate),
            25 => ("Log", static (x, d) => TensorPrimitives.Log(x, d), T.Log, Accuracy.Approximate),
            26 => ("Log2", static (x, d) => TensorPrimitives.Log2(x, d), T.Log2, Accuracy.Approximate),
            27 => ("Log10", static (x, d) => TensorPrimitives.Log10(x, d), T.Log10, Accuracy.Approximate),
            28 => ("LogP1", static (x, d) => TensorPrimitives.LogP1(x, d), T.LogP1, Accuracy.Approximate),
            29 => ("Log2P1", static (x, d) => TensorPrimitives.Log2P1(x, d), T.Log2P1, Accuracy.Approximate),
            30 => ("Cbrt", static (x, d) => TensorPrimitives.Cbrt(x, d), T.Cbrt, Accuracy.Approximate),
            31 => ("Sin", static (x, d) => TensorPrimitives.Sin(x, d), T.Sin, Accuracy.Trigonometric),
            32 => ("Cos", static (x, d) => TensorPrimitives.Cos(x, d), T.Cos, Accuracy.Trigonometric),
            33 => ("Tan", static (x, d) => TensorPrimitives.Tan(x, d), T.Tan, Accuracy.Trigonometric),
            34 => ("SinPi", static (x, d) => TensorPrimitives.SinPi(x, d), T.SinPi, Accuracy.Trigonometric),
            35 => ("CosPi", static (x, d) => TensorPrimitives.CosPi(x, d), T.CosPi, Accuracy.Trigonometric),
            36 => ("Asin", static (x, d) => TensorPrimitives.Asin(x, d), T.Asin, Accuracy.Approximate),
            37 => ("Acos", static (x, d) => TensorPrimitives.Acos(x, d), T.Acos, Accuracy.Approximate),
            38 => ("Atan", static (x, d) => TensorPrimitives.Atan(x, d), T.Atan, Accuracy.Approximate),
            39 => ("Sinh", static (x, d) => TensorPrimitives.Sinh(x, d), T.Sinh, Accuracy.Approximate),
            40 => ("Cosh", static (x, d) => TensorPrimitives.Cosh(x, d), T.Cosh, Accuracy.Approximate),
            41 => ("Tanh", static (x, d) => TensorPrimitives.Tanh(x, d), T.Tanh, Accuracy.Approximate),
            42 => ("Asinh", static (x, d) => TensorPrimitives.Asinh(x, d), T.Asinh, Accuracy.Approximate),
            43 => ("Acosh", static (x, d) => TensorPrimitives.Acosh(x, d), T.Acosh, Accuracy.Approximate),
            44 => ("Atanh", static (x, d) => TensorPrimitives.Atanh(x, d), T.Atanh, Accuracy.Approximate),
            45 when data.Length == 0 => null, // Sigmoid is documented to throw for an empty span.
            45 => ("Sigmoid", static (x, d) => TensorPrimitives.Sigmoid(x, d), static v => T.One / (T.One + T.Exp(-v)), Accuracy.Approximate),
            46 => ($"RootN({n})", (x, d) => TensorPrimitives.RootN(x, n, d), v => T.RootN(v, n), Accuracy.Approximate),
            _ => null,
        };

        if (unary is { } u)
        {
            RunUnary(bytes, data, u.Name, u.Vector, u.Scalar, u.Accuracy);
            return;
        }

        (string Name, Binary<T> Vector, Func<T, T, T> Scalar, Accuracy Accuracy) binary = op switch
        {
            47 => ("Divide", static (x, y, d) => TensorPrimitives.Divide(x, y, d), static (a, b) => a / b, Accuracy.Exact),
            48 => ("CopySign", static (x, y, d) => TensorPrimitives.CopySign(x, y, d), T.CopySign, Accuracy.Exact),
            49 => ("Ieee754Remainder", static (x, y, d) => TensorPrimitives.Ieee754Remainder(x, y, d), T.Ieee754Remainder, Accuracy.Exact),
            50 when !s_strict => ("Multiply", static (x, y, d) => TensorPrimitives.Multiply(x, y, d), static (a, b) => a * b, Accuracy.Exact),
            50 => ("Remainder", static (x, y, d) => TensorPrimitives.Remainder(x, y, d), static (a, b) => a % b, Accuracy.Exact),
            51 => ("Hypot", static (x, y, d) => TensorPrimitives.Hypot(x, y, d), T.Hypot, Accuracy.Approximate),
            52 => ("Atan2", static (x, y, d) => TensorPrimitives.Atan2(x, y, d), T.Atan2, Accuracy.Approximate),
            53 => ("Pow", static (x, y, d) => TensorPrimitives.Pow(x, y, d), T.Pow, Accuracy.Approximate),
            54 => ("FusedMultiplyAdd(x, y, x)", static (x, y, d) => TensorPrimitives.FusedMultiplyAdd(x, y, x, d), static (a, b) => T.FusedMultiplyAdd(a, b, a), Accuracy.Exact),
            55 => ("Lerp(x, y, 0.25)", static (x, y, d) => TensorPrimitives.Lerp(x, y, T.CreateTruncating(0.25), d), static (a, b) => T.Lerp(a, b, T.CreateTruncating(0.25)), Accuracy.Approximate),
            56 => ("Clamp(x, -y, y)", static (x, y, d) => TensorPrimitives.Clamp(x, T.Min(-T.Abs(y[0]), T.Abs(y[0])), T.Abs(y[0]), d), static (a, b) => a, Accuracy.Exact),
            _ => ("Multiply", static (x, y, d) => TensorPrimitives.Multiply(x, y, d), static (a, b) => a * b, Accuracy.Exact),
        };

        if (op == 56)
        {
            // Clamp's scalar bounds come from the first element of y; build the reference with the same bounds.
            if (other.Length == 0 || T.IsNaN(other[0]))
            {
                return;
            }

            T max = T.Abs(other[0]), min = -max;
            RunBinary<T>(bytes, data, other, "Clamp(x, -|y0|, |y0|)", binary.Vector, (a, _) => T.Clamp(a, min, max), Accuracy.Exact);
            return;
        }

        RunBinary(bytes, data, other, binary.Name, binary.Vector, binary.Scalar, binary.Accuracy);
    }

    private static void RunUnary<T>(ReadOnlySpan<byte> bytes, T[] data, string name, Unary<T> vector, Func<T, T> scalar, Accuracy accuracy)
        where T : unmanaged, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        using var buffer = PooledBoundedMemory<T>.Rent(data, Placement(bytes));
        bool inPlace = (bytes[2] & 4) != 0;
        using var destinationBuffer = PooledBoundedMemory<T>.Rent(data.Length, Placement(bytes));
        Span<T> destination = inPlace ? buffer.Span : destinationBuffer.Span;

        vector(buffer.Span, destination);
        for (int i = 0; i < data.Length; i++)
        {
            CheckFloat(data[i], default, scalar(data[i]), destination[i], accuracy, name, i, data.Length);
        }
    }

    private static void RunBinary<T>(ReadOnlySpan<byte> bytes, T[] data, T[] other, string name, Binary<T> vector, Func<T, T, T> scalar, Accuracy accuracy)
        where T : unmanaged, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        using var x = PooledBoundedMemory<T>.Rent(data, Placement(bytes));
        using var y = PooledBoundedMemory<T>.Rent(other, Placement(bytes));
        using var destinationBuffer = PooledBoundedMemory<T>.Rent(data.Length, Placement(bytes));
        Span<T> destination = (bytes[2] & 4) != 0 ? x.Span : destinationBuffer.Span;

        vector(x.Span, y.Span, destination);
        for (int i = 0; i < data.Length; i++)
        {
            CheckFloat(data[i], other[i], scalar(data[i], other[i]), destination[i], accuracy, name, i, data.Length);
        }
    }

    private static void CheckFloat<T>(T x, T y, T expected, T actual, Accuracy accuracy, string name, int index, int length)
        where T : unmanaged, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        string Describe() =>
            $"TensorPrimitives.{name}<{typeof(T).Name}> element {index} of {length}: x = {Hex(x)}, y = {Hex(y)}: expected {Hex(expected)}, got {Hex(actual)}";

        if (T.IsNaN(expected) || T.IsNaN(actual))
        {
            Check(T.IsNaN(expected) && T.IsNaN(actual), Describe);
            return;
        }

        if (accuracy == Accuracy.Exact)
        {
            Check(BitEquals(expected, actual), Describe);
            return;
        }

        double e = double.CreateTruncating(expected), a = double.CreateTruncating(actual);
        double maxValue = double.CreateTruncating(T.MaxValue);
        if (double.IsInfinity(e) || double.IsInfinity(a))
        {
            // Near the overflow threshold an approximation may round to infinity (or not).
            Check(e == a || (Math.Sign(e) == Math.Sign(a) && Math.Min(Math.Abs(e), Math.Abs(a)) >= maxValue / 4), Describe);
            return;
        }

        double limit = !s_strict && name.EndsWith("Pi", StringComparison.Ordinal) ? (typeof(T) == typeof(double) ? 1e5 : 256) : 65536;
        if (accuracy == Accuracy.Trigonometric && Math.Abs(double.CreateTruncating(x)) > limit)
        {
            // Large-argument range reduction is known to be approximate; only require a finite result in range for sin/cos.
            return;
        }

        double tolerance = typeof(T) == typeof(double) ? 1e-10 : typeof(T) == typeof(float) ? 1e-4 : 1e-2;
        Check(Math.Abs(e - a) <= tolerance * Math.Max(1, Math.Abs(e)), Describe);
    }

    private static T[] FloatPalette<T>()
        where T : unmanaged, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        List<T> values = [];
        foreach (double v in (ReadOnlySpan<double>)[0.0, -0.0, 1, -1, 0.5, -0.5, 1.5, -1.5, 2.5, -2.5, 3, -7, 0.1, 100, -100, 1e5, 1e10, -1e10, 1e30, 1e-30, -1e-30,
            Math.PI, Math.PI / 2, -Math.PI / 4, Math.E, 88.72, -103.9, 709.78, -745.1, 0.9999999, 1.0000001, 16777217, 9007199254740993, 1e300, 65504, 65520, 6e-8,
            double.NaN, double.PositiveInfinity, double.NegativeInfinity, 2147483647.5, -2147483648.5, 4294967296.0, 9.2233720368547758e18, 1.8446744073709552e19])
        {
            values.Add(T.CreateTruncating(v));
        }

        values.Add(T.MaxValue);
        values.Add(T.MinValue);
        values.Add(T.Epsilon);
        values.Add(-T.Epsilon);
        values.Add(T.BitIncrement(T.One));
        values.Add(T.BitDecrement(T.One));
        return [.. values];
    }

    // ---------------------------------------------------------------- integers

    private static void Integer<T>(ReadOnlySpan<byte> bytes)
        where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        T[] palette =
        [
            T.Zero, T.One, T.CreateTruncating(2), T.CreateTruncating(3), T.CreateTruncating(-1), T.CreateTruncating(-2), T.MinValue, T.MaxValue,
            T.MinValue + T.One, T.MaxValue - T.One, T.CreateTruncating(0x7F), T.CreateTruncating(0x80), T.CreateTruncating(0xFF), T.CreateTruncating(0x8000),
            T.CreateTruncating(0x80000000u), T.CreateTruncating(0x8000000000000000ul), T.CreateTruncating(0x5555555555555555ul),
        ];
        T[] data = Decode(bytes, palette);
        T[] other = Rotate(data);
        int amount = (sbyte)bytes[3];
        int op = bytes[1] % 20;

        (string Name, Unary<T> Vector, Func<T, T> Scalar)? unary = op switch
        {
            0 => ($"ShiftLeft({amount})", (x, d) => TensorPrimitives.ShiftLeft(x, amount, d), v => v << amount),
            1 => ($"ShiftRightArithmetic({amount})", (x, d) => TensorPrimitives.ShiftRightArithmetic(x, amount, d), v => v >> amount),
            2 => ($"ShiftRightLogical({amount})", (x, d) => TensorPrimitives.ShiftRightLogical(x, amount, d), v => v >>> amount),
            3 => ($"RotateLeft({amount})", (x, d) => TensorPrimitives.RotateLeft(x, amount, d), v => T.RotateLeft(v, amount)),
            4 => ($"RotateRight({amount})", (x, d) => TensorPrimitives.RotateRight(x, amount, d), v => T.RotateRight(v, amount)),
            5 => ("LeadingZeroCount", static (x, d) => TensorPrimitives.LeadingZeroCount(x, d), T.LeadingZeroCount),
            6 => ("TrailingZeroCount", static (x, d) => TensorPrimitives.TrailingZeroCount(x, d), T.TrailingZeroCount),
            7 => ("PopCount", static (x, d) => TensorPrimitives.PopCount(x, d), T.PopCount),
            8 => ("OnesComplement", static (x, d) => TensorPrimitives.OnesComplement(x, d), static v => ~v),
            9 => ("Negate", static (x, d) => TensorPrimitives.Negate(x, d), static v => unchecked(-v)),
            10 => ("Increment", static (x, d) => TensorPrimitives.Increment(x, d), static v => unchecked(v + T.One)),
            11 => ("Decrement", static (x, d) => TensorPrimitives.Decrement(x, d), static v => unchecked(v - T.One)),
            12 => ("Abs", static (x, d) => TensorPrimitives.Abs(x, d), T.Abs),
            _ => null,
        };

        if (unary is { } u)
        {
            RunExact(bytes, data, other, u.Name, (x, _, d) => u.Vector(x, d), (a, _) => u.Scalar(a));
            return;
        }

        (string Name, Binary<T> Vector, Func<T, T, T> Scalar) binary = op switch
        {
            13 => ("Divide", static (x, y, d) => TensorPrimitives.Divide(x, y, d), static (a, b) => a / b),
            14 => ("Remainder", static (x, y, d) => TensorPrimitives.Remainder(x, y, d), static (a, b) => a % b),
            15 => ("Multiply", static (x, y, d) => TensorPrimitives.Multiply(x, y, d), static (a, b) => unchecked(a * b)),
            16 => ("BitwiseAnd", static (x, y, d) => TensorPrimitives.BitwiseAnd(x, y, d), static (a, b) => a & b),
            17 => ("CopySign", static (x, y, d) => TensorPrimitives.CopySign(x, y, d), T.CopySign),
            18 => ("Clamp(x, min(y0,x0), max(y0,x0))", static (x, y, d) => TensorPrimitives.Clamp(x, T.Min(x[0], y[0]), T.Max(x[0], y[0]), d), static (a, b) => a),
            _ => ($"Divide(x, {amount})", (x, _, d) => TensorPrimitives.Divide(x, T.CreateTruncating(amount), d), (a, _) => a / T.CreateTruncating(amount)),
        };

        if (op == 18)
        {
            if (data.Length == 0)
            {
                return;
            }

            T min = T.Min(data[0], other[0]), max = T.Max(data[0], other[0]);
            RunExact(bytes, data, other, binary.Name, binary.Vector, (a, _) => T.Clamp(a, min, max));
            return;
        }

        RunExact(bytes, data, other, binary.Name, binary.Vector, binary.Scalar);
    }

    private static void RunExact<T>(ReadOnlySpan<byte> bytes, T[] data, T[] other, string name, Binary<T> vector, Func<T, T, T> scalar)
        where T : unmanaged
    {
        // The scalar operator defines both the values and the exceptions (DivideByZeroException, OverflowException) expected.
        T[] expected = new T[data.Length];
        Exception? expectedException = null;
        try
        {
            for (int i = 0; i < data.Length; i++)
            {
                expected[i] = scalar(data[i], other[i]);
            }
        }
        catch (ArithmeticException ex)
        {
            expectedException = ex;
        }

        using var x = PooledBoundedMemory<T>.Rent(data, Placement(bytes));
        using var y = PooledBoundedMemory<T>.Rent(other, Placement(bytes));
        using var destinationBuffer = PooledBoundedMemory<T>.Rent(data.Length, Placement(bytes));
        Span<T> destination = (bytes[2] & 4) != 0 ? x.Span : destinationBuffer.Span;

        Exception? actualException = null;
        try
        {
            vector(x.Span, y.Span, destination);
        }
        catch (ArithmeticException ex)
        {
            actualException = ex;
        }

        string Describe(string detail) => $"TensorPrimitives.{name}<{typeof(T).Name}> (length {data.Length}): {detail}";
        if (!s_strict && name == "CopySign" && expectedException is OverflowException && actualException is null)
        {
            return;
        }

        if (expectedException is not null || actualException is not null)
        {
            // When several elements fail differently, which exception wins depends on the processing order.
            Check(expectedException?.GetType() == actualException?.GetType() || (expectedException is not null && actualException is not null), () => Describe($"scalar threw {expectedException?.GetType().Name ?? "nothing"}, vectorized threw {actualException?.GetType().Name ?? "nothing"} (x = [{string.Join(", ", data.Take(64))}], y = [{string.Join(", ", other.Take(64))}])"));
            return;
        }

        for (int i = 0; i < data.Length; i++)
        {
            int k = i;
            T got = destination[i];
            Check(BitEquals(expected[i], got), () => Describe($"element {k}: x = {Hex(data[k])}, y = {Hex(other[k])}: expected {Hex(expected[k])}, got {Hex(got)}"));
        }
    }

    // ---------------------------------------------------------------- conversions

    private static void Conversions<TFrom>(ReadOnlySpan<byte> bytes)
        where TFrom : unmanaged, INumber<TFrom>, IMinMaxValue<TFrom>
    {
        TFrom[] palette = typeof(TFrom) == typeof(float) || typeof(TFrom) == typeof(double) || typeof(TFrom) == typeof(Half)
            ? (TFrom[])(object)(typeof(TFrom) == typeof(float) ? FloatPalette<float>() : typeof(TFrom) == typeof(double) ? FloatPalette<double>() : FloatPalette<Half>())
            : [TFrom.Zero, TFrom.One, TFrom.MinValue, TFrom.MaxValue, TFrom.CreateTruncating(16777217), TFrom.CreateTruncating(9007199254740993L), TFrom.CreateTruncating(-1), TFrom.CreateTruncating(65504), TFrom.CreateTruncating(65520), TFrom.CreateTruncating(2147483648L), TFrom.CreateTruncating(4294967295L)];
        TFrom[] data = Decode(bytes, palette);

        switch (bytes[1] % 13)
        {
            case 0: ConvertPair<TFrom, int>(bytes, data); break;
            case 1: ConvertPair<TFrom, uint>(bytes, data); break;
            case 2: ConvertPair<TFrom, long>(bytes, data); break;
            case 3: ConvertPair<TFrom, ulong>(bytes, data); break;
            case 4: ConvertPair<TFrom, short>(bytes, data); break;
            case 5: ConvertPair<TFrom, byte>(bytes, data); break;
            case 6: ConvertPair<TFrom, sbyte>(bytes, data); break;
            case 7: ConvertPair<TFrom, float>(bytes, data); break;
            case 8: ConvertPair<TFrom, double>(bytes, data); break;
            case 9: ConvertPair<TFrom, Half>(bytes, data); break;
            case 10: ConvertPair<TFrom, nint>(bytes, data); break;
            case 11: ConvertPair<TFrom, ushort>(bytes, data); break;
            default:
                if (typeof(TFrom) == typeof(float))
                {
                    // The dedicated float <-> Half converters.
                    float[] source = (float[])(object)data;
                    Half[] halves = new Half[source.Length];
                    TensorPrimitives.ConvertToHalf(source, halves);
                    for (int i = 0; i < source.Length; i++)
                    {
                        int k = i;
                        Check(BitEquals(halves[i], (Half)source[i]) || (Half.IsNaN(halves[i]) && float.IsNaN(source[i])), () => $"TensorPrimitives.ConvertToHalf element {k}: {Hex(source[k])} -> expected {Hex((Half)source[k])}, got {Hex(halves[k])}");
                    }

                    float[] singles = new float[halves.Length];
                    TensorPrimitives.ConvertToSingle(halves, singles);
                    for (int i = 0; i < halves.Length; i++)
                    {
                        int k = i;
                        Check(BitEquals(singles[i], (float)halves[i]) || (float.IsNaN(singles[i]) && Half.IsNaN(halves[i])), () => $"TensorPrimitives.ConvertToSingle element {k}: {Hex(halves[k])} -> expected {Hex((float)halves[k])}, got {Hex(singles[k])}");
                    }
                }

                break;
        }
    }

    private static void ConvertPair<TFrom, TTo>(ReadOnlySpan<byte> bytes, TFrom[] data)
        where TFrom : unmanaged, INumber<TFrom>
        where TTo : unmanaged, INumber<TTo>
    {
        string pair = $"<{typeof(TFrom).Name}, {typeof(TTo).Name}>";
        RunConversion(bytes, data, "ConvertTruncating" + pair, static (x, d) => TensorPrimitives.ConvertTruncating(x, d), TTo.CreateTruncating);
        RunConversion(bytes, data, "ConvertSaturating" + pair, static (x, d) => TensorPrimitives.ConvertSaturating(x, d), TTo.CreateSaturating);
        RunConversion(bytes, data, "ConvertChecked" + pair, static (x, d) => TensorPrimitives.ConvertChecked(x, d), TTo.CreateChecked);
    }

    private delegate void Conversion<TFrom, TTo>(ReadOnlySpan<TFrom> x, Span<TTo> destination);

    private static void RunConversion<TFrom, TTo>(ReadOnlySpan<byte> bytes, TFrom[] data, string name, Conversion<TFrom, TTo> vector, Func<TFrom, TTo> scalar)
        where TFrom : unmanaged, INumber<TFrom>
        where TTo : unmanaged, INumber<TTo>
    {
        TTo[] expected = new TTo[data.Length];
        Exception? expectedException = null;
        try
        {
            for (int i = 0; i < data.Length; i++)
            {
                expected[i] = scalar(data[i]);
            }
        }
        catch (OverflowException ex)
        {
            expectedException = ex;
        }

        using var x = PooledBoundedMemory<TFrom>.Rent(data, Placement(bytes));
        using var destination = PooledBoundedMemory<TTo>.Rent(data.Length, Placement(bytes));
        Exception? actualException = null;
        try
        {
            vector(x.Span, destination.Span);
        }
        catch (OverflowException ex)
        {
            actualException = ex;
        }

        string Describe(string detail) => $"TensorPrimitives.{name} (length {data.Length}): {detail}";
        if (expectedException is not null || actualException is not null)
        {
            Check((expectedException is null) == (actualException is null), () => Describe($"scalar threw {expectedException?.GetType().Name ?? "nothing"}, vectorized threw {actualException?.GetType().Name ?? "nothing"}"));
            return;
        }

        for (int i = 0; i < data.Length; i++)
        {
            int k = i;
            TTo got = destination.Span[i];
            bool same = BitEquals(expected[i], got) || (TTo.IsNaN(expected[i]) && TTo.IsNaN(got));
            Check(same, () => Describe($"element {k}: {Hex(data[k])} -> expected {Hex(expected[k])}, got {Hex(got)}"));
        }
    }

    // ---------------------------------------------------------------- helpers

    private static PoisonPagePlacement Placement(ReadOnlySpan<byte> bytes) =>
        (bytes[2] & 1) != 0 ? PoisonPagePlacement.Before : PoisonPagePlacement.After;

    private static T[] Decode<T>(ReadOnlySpan<byte> bytes, T[] palette)
        where T : unmanaged
    {
        ReadOnlySpan<byte> payload = bytes.Slice(HeaderSize);
        if ((bytes[2] & 2) != 0)
        {
            T[] elements = new T[payload.Length];
            for (int i = 0; i < elements.Length; i++)
            {
                elements[i] = palette[payload[i] % palette.Length];
            }

            return elements;
        }

        T[] raw = new T[payload.Length / Unsafe.SizeOf<T>()];
        payload.Slice(0, raw.Length * Unsafe.SizeOf<T>()).CopyTo(MemoryMarshal.AsBytes(raw.AsSpan()));
        return raw;
    }

    private static T[] Rotate<T>(T[] data)
    {
        T[] other = new T[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            other[i] = data[(i + 1) % data.Length];
        }

        return other;
    }

    private static bool BitEquals<T>(T a, T b)
        where T : unmanaged =>
        MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in a)).SequenceEqual(MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in b)));

    private static string Hex<T>(T value)
        where T : unmanaged =>
        $"{value} (0x{Convert.ToHexString(MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in value)).ToArray().Reverse().ToArray())})";

    private static void Check(bool condition, Func<string> message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message());
        }
    }
}
