// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Numerics;
using System.Text;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Fuzzes <see cref="Complex{T}"/> (and through it the non-generic <see cref="Complex"/>, which delegates to Complex&lt;double&gt;).
/// <list type="bullet">
/// <item>Complex&lt;float&gt; and Complex&lt;Half&gt; results are compared with Complex&lt;double&gt; on the same (exactly widened)
/// inputs: a component the double result puts well inside T's range must come out finite, and finite results must be close.</item>
/// <item>On the real axis (and for Sin/Cos/Sinh/Cosh on the imaginary axis) results must stay on the axis, as the scalar
/// functions say, without NaNs from infinity times zero.</item>
/// <item>C23 Annex G: conj symmetry, odd/even symmetry, and G.5.1 infinity recovery for * and /.</item>
/// <item>ToString("R")/TryFormat/Parse round trips.</item>
/// </list>
/// </summary>
/// <remarks>Input layout: [0] operation, [1] element type, then four operand encodings (a palette byte or raw bits).</remarks>
internal sealed class ComplexFuzzer : IFuzzer
{
    // Known issues on main, tolerated unless TENSOR_FUZZ_STRICT=1 (shared switch name with the other new fuzzers):
    // * Exp, Sinh, Cosh, Sin, Cos (and Pow, which goes through Exp) compute e^x separately from cis(y), so once e^x overflows
    //   the result has NaN parts from infinity times zero on the axes, and spurious infinities where the true value is finite.
    // * Asin and Acos ignore the sign of a zero imaginary part on the real-axis branch cuts (|x| > 1), and Atan takes the sign
    //   of the real part from y instead of from the zero real part on the imaginary-axis cuts (|y| > 1).
    // * Asin, Acos, Atan and Reciprocal lose the sign of zero result components.
    // * Tan and Tanh return (INF, NaN) at the representable points nearest their poles (cos 2x + cosh 2y cancels to zero) and
    //   NaN for huge arguments (2x overflows).
    // * Division (Smith's formula) overflows in the denominator when the divisor is near MaxValue and returns zero.
    // * For float and Half, Log, Log10 and Atan overflow when |z| exceeds MaxValue; double scales correctly.
    // * A huge finite value divided by an infinity gives (0, NaN) instead of zero (G.5.2).
    // * Sqrt of a huge negative real part with a subnormal imaginary part returns the wrong sign of the imaginary part.
    // Not a bug report: float/Half Sqrt loses precision for subnormal inputs, so the accuracy check skips those.
    private static readonly bool s_strict = Environment.GetEnvironmentVariable("TENSOR_FUZZ_STRICT") == "1";

    public string[] TargetAssemblies { get; } = ["System.Runtime.Numerics"];
    public string[] TargetCoreLibPrefixes => [];

    private static readonly double[] s_palette =
    [
        0.0, -0.0, 1, -1, 0.5, -0.5, 2, 3, Math.PI / 2, Math.PI, -Math.PI / 2, Math.PI / 4, 1e-300, double.Epsilon, 1e300, -1e300,
        709.78, 710, 711, -745.2, 88.7, 89, -104, 11.1, 12, -17, double.PositiveInfinity, double.NegativeInfinity, double.NaN,
        float.MaxValue, 65504, 1e-8, 1e10, 1e20, 1e38, 1e155, 1e-155, 0.75, 1.5, 1e5, -1e5, 355, 22, 7,
    ];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4)
        {
            return;
        }

        int op = bytes[0];
        int position = 2;
        double v0 = ReadValue(bytes, ref position), v1 = ReadValue(bytes, ref position), v2 = ReadValue(bytes, ref position), v3 = ReadValue(bytes, ref position);
        switch (bytes[1] % 3)
        {
            case 0:
                Run<float>(op, (float)v0, (float)v1, (float)v2, (float)v3, precision: 24);
                break;
            case 1:
                Run<Half>(op, (Half)v0, (Half)v1, (Half)v2, (Half)v3, precision: 11);
                break;
            default:
                RunDouble(op, v0, v1, v2, v3);
                break;
        }
    }

    private static double ReadValue(ReadOnlySpan<byte> bytes, ref int position)
    {
        byte b = position < bytes.Length ? bytes[position++] : (byte)0;
        if (b < 0xE0)
        {
            double value = s_palette[b % s_palette.Length];
            return (b / s_palette.Length) % 2 == 1 ? -value : value;
        }

        ulong raw = 0;
        for (int i = 0; i < 8; i++)
        {
            raw |= (ulong)(position < bytes.Length ? bytes[position++] : 0) << (8 * i);
        }

        return BitConverter.UInt64BitsToDouble(raw);
    }

    private static (string Name, Func<Complex<T>, Complex<T>, Complex<T>> Op) GetOperation<T>(int op) where T : IFloatingPointIeee754<T>, IMinMaxValue<T> =>
        (op % 22) switch
        {
            0 => ("Exp", (z, _) => Complex<T>.Exp(z)),
            1 => ("Log", (z, _) => Complex<T>.Log(z)),
            2 => ("Log10", (z, _) => Complex<T>.Log10(z)),
            3 => ("Sqrt", (z, _) => Complex<T>.Sqrt(z)),
            4 => ("Sin", (z, _) => Complex<T>.Sin(z)),
            5 => ("Cos", (z, _) => Complex<T>.Cos(z)),
            6 => ("Tan", (z, _) => Complex<T>.Tan(z)),
            7 => ("Sinh", (z, _) => Complex<T>.Sinh(z)),
            8 => ("Cosh", (z, _) => Complex<T>.Cosh(z)),
            9 => ("Tanh", (z, _) => Complex<T>.Tanh(z)),
            10 => ("Asin", (z, _) => Complex<T>.Asin(z)),
            11 => ("Acos", (z, _) => Complex<T>.Acos(z)),
            12 => ("Atan", (z, _) => Complex<T>.Atan(z)),
            13 => ("Reciprocal", (z, _) => Complex<T>.Reciprocal(z)),
            14 => ("Multiply", (z, w) => z * w),
            15 => ("Divide", (z, w) => z / w),
            16 => ("Pow", (z, w) => Complex<T>.Pow(z, w)),
            17 => ("PowReal", (z, w) => Complex<T>.Pow(z, w.Real)),
            18 => ("MultiplyReal", (z, w) => z * w.Real),
            19 => ("DivideReal", (z, w) => z / w.Real),
            20 => ("FromPolarCoordinates", (z, _) => Complex<T>.FromPolarCoordinates(z.Real, z.Imaginary)),
            _ => ("Abs", (z, _) => new Complex<T>(Complex<T>.Abs(z), T.Zero)),
        };

    private static void Run<T>(int op, T a, T b, T c, T d, int precision) where T : IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        var z = new Complex<T>(a, b);
        var w = new Complex<T>(c, d);
        (string name, Func<Complex<T>, Complex<T>, Complex<T>> f) = GetOperation<T>(op);
        (_, Func<Complex<double>, Complex<double>, Complex<double>> g) = GetOperation<double>(op);
        Complex<T> actual = f(z, w);
        Complex<double> reference = g(Widen(z), Widen(w));

        // Compare with the double computation on the same inputs.
        string Describe() => $"Complex<{typeof(T).Name}>.{name}({Format(z)}{(name.Contains("Multiply") || name.Contains("Divide") || name.StartsWith("Pow") ? ", " + Format(w) : "")}) = {Format(actual)}; Complex<double> gives {Format(reference)}";
        double max = double.CreateTruncating(T.MaxValue) / 4;
        // When one component of the true result overflows T, Annex G allows the other to be NaN (it's still an infinity).
        bool referenceFinite = double.IsFinite(reference.Real) && double.IsFinite(reference.Imaginary)
            && Math.Abs(reference.Real) <= double.CreateTruncating(T.MaxValue) && Math.Abs(reference.Imaginary) <= double.CreateTruncating(T.MaxValue);
        if (referenceFinite)
        {
            double re = double.CreateTruncating(actual.Real), im = double.CreateTruncating(actual.Imaginary);
            if (Math.Abs(reference.Real) <= max)
            {
                Check(double.IsFinite(re) || !s_strict && IsKnownOverflow(name, z, w), () => $"real part isn't finite: {Describe()}");
            }

            if (Math.Abs(reference.Imaginary) <= max)
            {
                Check(double.IsFinite(im) || !s_strict && IsKnownOverflow(name, z, w), () => $"imaginary part isn't finite: {Describe()}");
            }

            // Normwise relative error, when everything is comfortably inside T's normal range.
            double magnitude = Math.Max(Math.Abs(reference.Real), Math.Abs(reference.Imaginary));
            double minNormal = Math.ScaleB(1, precision == 24 ? -126 : -14) * Math.ScaleB(1, precision);
            bool subnormalInput = T.IsSubnormal(z.Real) || T.IsSubnormal(z.Imaginary) || T.IsSubnormal(w.Real) || T.IsSubnormal(w.Imaginary);
            if (double.IsFinite(re) && double.IsFinite(im) && magnitude <= max && magnitude >= minNormal && !subnormalInput && IsWellConditioned(name, z, w))
            {
                double error = Math.Max(Math.Abs(re - reference.Real), Math.Abs(im - reference.Imaginary)) / magnitude;
                double tolerance = Math.ScaleB(1, 8 - precision); // 256 ulps
                Check(error <= tolerance || !s_strict && IsKnownInaccuracy(name, z, w), () => $"relative error {error:E2} (tolerance {tolerance:E2}): {Describe()}");
            }
        }

        CommonChecks(name, z, w, actual, f);
    }

    private static void RunDouble(int op, double a, double b, double c, double d)
    {
        var z = new Complex<double>(a, b);
        var w = new Complex<double>(c, d);
        (string name, Func<Complex<double>, Complex<double>, Complex<double>> f) = GetOperation<double>(op);
        Complex<double> actual = f(z, w);
        CommonChecks(name, z, w, actual, f);

        // The non-generic Complex delegates to Complex<double> and must agree bit for bit.
        var zz = new Complex(a, b);
        var ww = new Complex(c, d);
        Complex? legacy = name switch
        {
            "Exp" => Complex.Exp(zz),
            "Log" => Complex.Log(zz),
            "Sqrt" => Complex.Sqrt(zz),
            "Sin" => Complex.Sin(zz),
            "Cos" => Complex.Cos(zz),
            "Tan" => Complex.Tan(zz),
            "Sinh" => Complex.Sinh(zz),
            "Cosh" => Complex.Cosh(zz),
            "Tanh" => Complex.Tanh(zz),
            "Asin" => Complex.Asin(zz),
            "Acos" => Complex.Acos(zz),
            "Atan" => Complex.Atan(zz),
            "Reciprocal" => Complex.Reciprocal(zz),
            "Multiply" => zz * ww,
            "Divide" => zz / ww,
            "Pow" => Complex.Pow(zz, ww),
            "PowReal" => Complex.Pow(zz, c),
            _ => null,
        };
        if (legacy is Complex l)
        {
            Check(Same(l.Real, actual.Real) && Same(l.Imaginary, actual.Imaginary),
                () => $"Complex.{name}({Format(z)}) = ({l.Real:R}, {l.Imaginary:R}) but Complex<double>.{name} = {Format(actual)}");
        }
    }

    private static void CommonChecks<T>(string name, Complex<T> z, Complex<T> w, Complex<T> actual, Func<Complex<T>, Complex<T>, Complex<T>> f)
        where T : IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        string Describe() => $"Complex<{typeof(T).Name}>.{name}({Format(z)}, {Format(w)}) = {Format(actual)}";

        // Real axis: finite real input with a zero imaginary part stays on the real axis where the scalar function is real.
        if (T.IsFinite(z.Real) && T.IsZero(z.Imaginary))
        {
            T x = z.Real;
            T expected = name switch
            {
                "Exp" => T.Exp(x),
                "Sinh" => T.Sinh(x),
                "Cosh" => T.Cosh(x),
                "Tanh" => T.Tanh(x),
                "Sin" => T.Sin(x),
                "Cos" => T.Cos(x),
                "Atan" => T.Atan(x),
                "Sqrt" when x >= T.Zero => T.Sqrt(x),
                "Log" when x > T.Zero => T.Log(x),
                _ => T.NaN,
            };

            if (!T.IsNaN(expected))
            {
                Check(T.IsZero(actual.Imaginary) || !s_strict && IsKnownAxisIssue(name, z),
                    () => $"imaginary part should be zero on the real axis: {Describe()} (scalar {name} = {expected})");
                Check(CloseOrSameSpecial(actual.Real, expected) || !s_strict && IsKnownAxisIssue(name, z),
                    () => $"real part should match the scalar function ({expected}): {Describe()}");
            }
        }

        // Imaginary axis: Sin(iy) = i Sinh(y), Cos(iy) = Cosh(y), Sinh(iy) = i Sin(y), Cosh(iy) = Cos(y).
        if (T.IsZero(z.Real) && T.IsFinite(z.Imaginary))
        {
            T y = z.Imaginary;
            (bool realResult, T expected) = name switch
            {
                "Sin" => (false, T.Sinh(y)),
                "Cos" => (true, T.Cosh(y)),
                "Sinh" => (false, T.Sin(y)),
                "Cosh" => (true, T.Cos(y)),
                _ => (false, T.NaN),
            };

            if (!T.IsNaN(expected))
            {
                T onAxis = realResult ? actual.Real : actual.Imaginary;
                T offAxis = realResult ? actual.Imaginary : actual.Real;
                Check((T.IsZero(offAxis) && CloseOrSameSpecial(onAxis, expected)) || !s_strict && IsKnownAxisIssue(name, z),
                    () => $"result should be {(realResult ? "real" : "imaginary")} {expected}: {Describe()}");
            }
        }

        // Annex G symmetries: f(conj z) = conj f(z) for all these functions; odd and even ones also under negation.
        bool conjSymmetric = name is "Exp" or "Log" or "Log10" or "Sqrt" or "Sin" or "Cos" or "Tan" or "Sinh" or "Cosh" or "Tanh" or "Asin" or "Acos" or "Atan" or "Reciprocal";
        if (conjSymmetric && !T.IsNaN(z.Imaginary))
        {
            Complex<T> fromConjugate = f(Complex<T>.Conjugate(z), w);
            Check((Same(fromConjugate.Real, actual.Real) && Same(fromConjugate.Imaginary, -actual.Imaginary))
                || IsAllowedSymmetryDifference(name, z, fromConjugate, Complex<T>.Conjugate(actual)),
                () => $"{name}(conj z) != conj({name}(z)): {Describe()}, {name}({Format(Complex<T>.Conjugate(z))}) = {Format(fromConjugate)}");
        }

        bool odd = name is "Sin" or "Tan" or "Sinh" or "Tanh" or "Asin" or "Atan";
        bool even = name is "Cos" or "Cosh";
        if (odd || even)
        {
            Complex<T> fromNegated = f(-z, w);
            Complex<T> expected = odd ? -actual : actual;
            Check((Same(fromNegated.Real, expected.Real) && Same(fromNegated.Imaginary, expected.Imaginary))
                || IsAllowedSymmetryDifference(name, z, fromNegated, expected),
                () => $"{name} isn't {(odd ? "odd" : "even")}: {Describe()}, {name}({Format(-z)}) = {Format(fromNegated)}");
        }

        // G.5.1 infinity recovery: an infinite operand times a nonzero one, or divided by a finite one, gives an infinity; a
        // finite value divided by an infinite one gives zero; a nonzero value divided by zero gives an infinity.
        bool zNaN = T.IsNaN(z.Real) || T.IsNaN(z.Imaginary), wNaN = T.IsNaN(w.Real) || T.IsNaN(w.Imaginary);
        bool zInf = Complex<T>.IsInfinity(z), wInf = Complex<T>.IsInfinity(w);
        bool zZero = T.IsZero(z.Real) && T.IsZero(z.Imaginary), wZero = T.IsZero(w.Real) && T.IsZero(w.Imaginary);
        if (name == "Multiply" && ((zInf && !wZero && !wNaN) || (wInf && !zZero && !zNaN)))
        {
            Check(Complex<T>.IsInfinity(actual), () => $"infinite times nonzero isn't infinite: {Describe()}");
        }

        if (name == "Divide" && !zNaN && !wNaN)
        {
            if (zInf && !wInf)
            {
                Check(Complex<T>.IsInfinity(actual), () => $"infinite divided by finite isn't infinite: {Describe()}");
            }
            else if (!zInf && wInf)
            {
                Check((T.IsZero(actual.Real) && T.IsZero(actual.Imaginary)) || !s_strict && IsHuge(z), () => $"finite divided by infinite isn't zero: {Describe()}");
            }
            else if (!zZero && wZero)
            {
                Check(Complex<T>.IsInfinity(actual), () => $"nonzero divided by zero isn't infinite: {Describe()}");
            }
        }

        // Formatting round trip.
        string text = actual.ToString("R", CultureInfo.InvariantCulture);
        Check(Complex<T>.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out Complex<T> parsed)
            && Same(parsed.Real, actual.Real) && Same(parsed.Imaginary, actual.Imaginary),
            () => $"'{text}' doesn't parse back to {Format(actual)}: got {Format(parsed)}");
        char[] buffer = new char[text.Length];
        Check(actual.TryFormat(buffer, out int written, "R", CultureInfo.InvariantCulture) && new string(buffer, 0, written) == text,
            () => $"TryFormat disagrees with ToString for {Format(actual)}");
        Check(!actual.TryFormat(buffer.AsSpan(0, text.Length - 1), out _, "R", CultureInfo.InvariantCulture),
            () => $"TryFormat into {text.Length - 1} chars succeeded for '{text}'");
        byte[] utf8 = new byte[text.Length];
        Check(actual.TryFormat(utf8, out written, "R", CultureInfo.InvariantCulture) && Encoding.UTF8.GetString(utf8, 0, written) == text,
            () => $"UTF-8 TryFormat disagrees with ToString for {Format(actual)}");
    }

    // Overflow of e^|x| (or e^|y| for Sin/Cos) in the functions built on e^x * cis(y).
    private static bool IsKnownOverflow<T>(string name, Complex<T> z, Complex<T> w) where T : IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        T limit = T.Log(T.MaxValue);
        return name switch
        {
            "Exp" or "Sinh" or "Cosh" => T.Abs(z.Real) > limit,
            "Sin" or "Cos" => T.Abs(z.Imaginary) > limit,
            "Pow" or "PowReal" => true, // Goes through Exp(power * Log(value)) for overflowing inputs.
            "Tan" or "Tanh" => true,
            "Divide" or "Reciprocal" or "DivideReal" => IsHuge(w) || IsHuge(z),
            "Log" or "Log10" or "Atan" => typeof(T) != typeof(double) && IsHuge(z),
            _ => false,
        };
    }

    private static bool IsKnownInaccuracy<T>(string name, Complex<T> z, Complex<T> w) where T : IFloatingPointIeee754<T>, IMinMaxValue<T> =>
        name is "Divide" or "Reciprocal" or "DivideReal" && (IsHuge(w) || IsHuge(z));

    private static bool IsHuge<T>(Complex<T> z) where T : IFloatingPointIeee754<T>, IMinMaxValue<T> =>
        T.Abs(z.Real) > T.MaxValue / T.CreateTruncating(16) || T.Abs(z.Imaginary) > T.MaxValue / T.CreateTruncating(16);

    private static bool IsKnownAxisIssue<T>(string name, Complex<T> z) where T : IFloatingPointIeee754<T>, IMinMaxValue<T> => IsKnownOverflow(name, z, z);

    private static bool IsAllowedSymmetryDifference<T>(string name, Complex<T> z, Complex<T> x, Complex<T> y) where T : IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        bool zeroSignOnly = SameIgnoringZeroSign(x.Real, y.Real) && SameIgnoringZeroSign(x.Imaginary, y.Imaginary);

        // Annex G leaves the sign of zero, or of an infinity next to a NaN, unspecified for several non-finite inputs (for
        // example cexp(-INF + iINF) and casin(INF + iNaN)).
        if (!Complex<T>.IsFinite(z) && SameIgnoringSign(x.Real, y.Real) && SameIgnoringSign(x.Imaginary, y.Imaginary))
        {
            return true;
        }

        if (s_strict)
        {
            return false;
        }

        if (zeroSignOnly && name is "Asin" or "Acos" or "Atan" or "Reciprocal")
        {
            return true;
        }

        if (name == "Sqrt" && (T.IsSubnormal(z.Imaginary) || T.IsSubnormal(z.Real)))
        {
            return true;
        }

        // The branch cut sides.
        return name is "Asin" or "Acos" && T.IsZero(z.Imaginary) || name == "Atan" && T.IsZero(z.Real);
    }

    private static bool SameIgnoringSign<T>(T x, T y) where T : IFloatingPointIeee754<T> => (T.IsNaN(x) && T.IsNaN(y)) || T.Abs(x) == T.Abs(y);

    private static bool SameIgnoringZeroSign<T>(T x, T y) where T : IFloatingPointIeee754<T> => (T.IsNaN(x) && T.IsNaN(y)) || x == y;

    private static bool IsWellConditioned<T>(string name, Complex<T> z, Complex<T> w) where T : IFloatingPointIeee754<T>, IMinMaxValue<T> =>
        name is "Multiply" or "Divide" or "Reciprocal" or "MultiplyReal" or "DivideReal" or "Abs" or "Sqrt" or "Exp" or "Sinh" or "Cosh" or "FromPolarCoordinates";

    private static bool CloseOrSameSpecial<T>(T actual, T expected) where T : IFloatingPointIeee754<T>
    {
        if (!T.IsFinite(expected) || !T.IsFinite(actual))
        {
            return Same(actual, expected);
        }

        T scale = T.Max(T.Abs(expected), T.Epsilon);
        return T.Abs(actual - expected) <= scale * T.CreateTruncating(1e-2) || T.Abs(actual - expected) <= T.CreateTruncating(1e-30);
    }

    // Bitwise-equal up to the NaN payload and sign.
    private static bool Same<T>(T x, T y) where T : IFloatingPointIeee754<T> =>
        (T.IsNaN(x) && T.IsNaN(y)) || (x == y && T.IsNegative(x) == T.IsNegative(y));

    private static Complex<double> Widen<T>(Complex<T> z) where T : IFloatingPointIeee754<T>, IMinMaxValue<T> =>
        new(double.CreateTruncating(z.Real), double.CreateTruncating(z.Imaginary));

    private static string Format<T>(Complex<T> z) where T : IFloatingPointIeee754<T>, IMinMaxValue<T> =>
        $"({FormatScalar(z.Real)}, {FormatScalar(z.Imaginary)})";

    private static string FormatScalar<T>(T x) where T : IFloatingPointIeee754<T> =>
        T.IsZero(x) && T.IsNegative(x) ? "-0" : x.ToString("R", CultureInfo.InvariantCulture);

    private static void Check(bool condition, Func<string> message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message());
        }
    }
}
