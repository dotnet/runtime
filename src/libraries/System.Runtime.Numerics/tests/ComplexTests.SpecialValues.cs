// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Numerics.Tests
{
    // Special-value conformance for Complex<T>, modeled on the C23 Annex G
    // (IEC 60559-compatible complex arithmetic) value tables. The same table is
    // shared across double/float/Half: every listed expected component is either
    // exactly representable in all three or a multiple of pi that each type forms
    // identically to CreateTruncating of the shared double, so the type-independent
    // special-value handling must reproduce it bit-for-bit, including the sign of zero.
    public static class ComplexGenericSpecialValueTests
    {
        private const double NaN = double.NaN;
        private const double PositiveInfinity = double.PositiveInfinity;
        private const double NegativeInfinity = double.NegativeInfinity;

        private static void AssertSame<T>(T actual, double expected, string context, bool exactZeroSign = true)
            where T : IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            if (double.IsNaN(expected))
            {
                Assert.True(T.IsNaN(actual), $"{context}: expected NaN, got {actual}");
                return;
            }

            T e = T.CreateTruncating(expected);

            // Annex G leaves the sign of a zero result of complex division unspecified, so
            // callers that exercise divide relax the sign check for a zero expected value.
            bool signOk = (T.IsNegative(actual) == T.IsNegative(e)) || (!exactZeroSign && (e == T.Zero));
            Assert.True((actual == e) && signOk, $"{context}: expected {e}, got {actual}");
        }

        private static void Verify<T>(Func<Complex<T>, Complex<T>> op, string name, double real, double imaginary, double expectedReal, double expectedImaginary, bool exactZeroSign = true)
            where T : IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            Complex<T> actual = op(new Complex<T>(T.CreateTruncating(real), T.CreateTruncating(imaginary)));
            string context = $"{name}<{typeof(T).Name}>({real}, {imaginary}).";
            AssertSame(actual.Real, expectedReal, context + "Real", exactZeroSign);
            AssertSame(actual.Imaginary, expectedImaginary, context + "Imaginary", exactZeroSign);
        }

        private static void Verify<T>(Func<Complex<T>, Complex<T>, Complex<T>> op, string name, double leftReal, double leftImaginary, double rightReal, double rightImaginary, double expectedReal, double expectedImaginary, bool exactZeroSign)
            where T : IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            Complex<T> left = new Complex<T>(T.CreateTruncating(leftReal), T.CreateTruncating(leftImaginary));
            Complex<T> right = new Complex<T>(T.CreateTruncating(rightReal), T.CreateTruncating(rightImaginary));
            Complex<T> actual = op(left, right);
            string context = $"{name}<{typeof(T).Name}>(({leftReal}, {leftImaginary}), ({rightReal}, {rightImaginary})).";
            AssertSame(actual.Real, expectedReal, context + "Real", exactZeroSign);
            AssertSame(actual.Imaginary, expectedImaginary, context + "Imaginary", exactZeroSign);
        }

        // Every special-value combination is drawn from this grid; each entry (and every
        // arithmetic result over it) is exactly representable in double, float, and Half.
        private static readonly double[] s_specialGrid = { NegativeInfinity, -1.0, -0.0, 0.0, 1.0, PositiveInfinity, NaN };

        // C23 Annex G.5.1 reference multiply. Complex<T>'s operator * matches this bit-for-bit.
        private static (double, double) ReferenceMultiply(double a, double b, double c, double d)
        {
            double x = (a * c) - (b * d);
            double y = (a * d) + (b * c);

            if (double.IsNaN(x) && double.IsNaN(y))
            {
                bool recalc = false;

                if (double.IsInfinity(a) || double.IsInfinity(b))
                {
                    a = double.CopySign(double.IsInfinity(a) ? 1.0 : 0.0, a);
                    b = double.CopySign(double.IsInfinity(b) ? 1.0 : 0.0, b);

                    if (double.IsNaN(c))
                    {
                        c = double.CopySign(0.0, c);
                    }

                    if (double.IsNaN(d))
                    {
                        d = double.CopySign(0.0, d);
                    }

                    recalc = true;
                }

                if (double.IsInfinity(c) || double.IsInfinity(d))
                {
                    c = double.CopySign(double.IsInfinity(c) ? 1.0 : 0.0, c);
                    d = double.CopySign(double.IsInfinity(d) ? 1.0 : 0.0, d);

                    if (double.IsNaN(a))
                    {
                        a = double.CopySign(0.0, a);
                    }

                    if (double.IsNaN(b))
                    {
                        b = double.CopySign(0.0, b);
                    }

                    recalc = true;
                }

                if (!recalc && (double.IsInfinity(a * c) || double.IsInfinity(b * d) || double.IsInfinity(a * d) || double.IsInfinity(b * c)))
                {
                    if (double.IsNaN(a))
                    {
                        a = double.CopySign(0.0, a);
                    }

                    if (double.IsNaN(b))
                    {
                        b = double.CopySign(0.0, b);
                    }

                    if (double.IsNaN(c))
                    {
                        c = double.CopySign(0.0, c);
                    }

                    if (double.IsNaN(d))
                    {
                        d = double.CopySign(0.0, d);
                    }

                    recalc = true;
                }

                if (recalc)
                {
                    x = double.PositiveInfinity * ((a * c) - (b * d));
                    y = double.PositiveInfinity * ((a * d) + (b * c));
                }
            }

            return (x, y);
        }

        // C23 Annex G.5.1 reference divide (fmax/scalbn form). Complex<T>'s operator / uses
        // Smith's formula, so it agrees on every value except the (unspecified) sign of a zero.
        private static (double, double) ReferenceDivide(double a, double b, double c, double d)
        {
            int ilogbw = 0;
            double fabsC = Math.Abs(c);
            double fabsD = Math.Abs(d);
            double fmax = double.IsNaN(fabsC) ? fabsD : (double.IsNaN(fabsD) ? fabsC : Math.Max(fabsC, fabsD));
            double logbw = Logb(fmax);

            if (double.IsFinite(logbw))
            {
                ilogbw = (int)logbw;
                c = Math.ScaleB(c, -ilogbw);
                d = Math.ScaleB(d, -ilogbw);
            }

            double denom = (c * c) + (d * d);
            double x = Math.ScaleB(((a * c) + (b * d)) / denom, -ilogbw);
            double y = Math.ScaleB(((b * c) - (a * d)) / denom, -ilogbw);

            if (double.IsNaN(x) && double.IsNaN(y))
            {
                if ((denom == 0.0) && (!double.IsNaN(a) || !double.IsNaN(b)))
                {
                    x = double.CopySign(double.PositiveInfinity, c) * a;
                    y = double.CopySign(double.PositiveInfinity, c) * b;
                }
                else if ((double.IsInfinity(a) || double.IsInfinity(b)) && double.IsFinite(c) && double.IsFinite(d))
                {
                    a = double.CopySign(double.IsInfinity(a) ? 1.0 : 0.0, a);
                    b = double.CopySign(double.IsInfinity(b) ? 1.0 : 0.0, b);
                    x = double.PositiveInfinity * ((a * c) + (b * d));
                    y = double.PositiveInfinity * ((b * c) - (a * d));
                }
                else if (double.IsInfinity(logbw) && (logbw > 0.0) && double.IsFinite(a) && double.IsFinite(b))
                {
                    c = double.CopySign(double.IsInfinity(c) ? 1.0 : 0.0, c);
                    d = double.CopySign(double.IsInfinity(d) ? 1.0 : 0.0, d);
                    x = 0.0 * ((a * c) + (b * d));
                    y = 0.0 * ((b * c) - (a * d));
                }
            }

            return (x, y);
        }

        private static double Logb(double value)
        {
            if (value == 0.0)
            {
                return double.NegativeInfinity;
            }

            if (double.IsInfinity(value))
            {
                return double.PositiveInfinity;
            }

            if (double.IsNaN(value))
            {
                return double.NaN;
            }

            return Math.Floor(Math.Log2(Math.Abs(value)));
        }

        [Theory]
        [MemberData(nameof(Multiply_SpecialValues))]
        public static void Multiply(double leftReal, double leftImaginary, double rightReal, double rightImaginary, double expectedReal, double expectedImaginary)
        {
            Verify<double>(static (x, y) => x * y, "Multiply", leftReal, leftImaginary, rightReal, rightImaginary, expectedReal, expectedImaginary, exactZeroSign: true);
            Verify<float>(static (x, y) => x * y, "Multiply", leftReal, leftImaginary, rightReal, rightImaginary, expectedReal, expectedImaginary, exactZeroSign: true);
            Verify<Half>(static (x, y) => x * y, "Multiply", leftReal, leftImaginary, rightReal, rightImaginary, expectedReal, expectedImaginary, exactZeroSign: true);
        }

        [Theory]
        [MemberData(nameof(Divide_SpecialValues))]
        public static void Divide(double leftReal, double leftImaginary, double rightReal, double rightImaginary, double expectedReal, double expectedImaginary)
        {
            Verify<double>(static (x, y) => x / y, "Divide", leftReal, leftImaginary, rightReal, rightImaginary, expectedReal, expectedImaginary, exactZeroSign: false);
            Verify<float>(static (x, y) => x / y, "Divide", leftReal, leftImaginary, rightReal, rightImaginary, expectedReal, expectedImaginary, exactZeroSign: false);
            Verify<Half>(static (x, y) => x / y, "Divide", leftReal, leftImaginary, rightReal, rightImaginary, expectedReal, expectedImaginary, exactZeroSign: false);
        }

        [Theory]
        [MemberData(nameof(Reciprocal_SpecialValues))]
        public static void Reciprocal(double real, double imaginary, double expectedReal, double expectedImaginary)
        {
            Verify<double>(Complex<double>.Reciprocal, "Reciprocal", real, imaginary, expectedReal, expectedImaginary, exactZeroSign: false);
            Verify<float>(Complex<float>.Reciprocal, "Reciprocal", real, imaginary, expectedReal, expectedImaginary, exactZeroSign: false);
            Verify<Half>(Complex<Half>.Reciprocal, "Reciprocal", real, imaginary, expectedReal, expectedImaginary, exactZeroSign: false);
        }

        public static IEnumerable<object[]> Multiply_SpecialValues()
        {
            foreach (double a in s_specialGrid)
            foreach (double b in s_specialGrid)
            foreach (double c in s_specialGrid)
            foreach (double d in s_specialGrid)
            {
                (double expectedReal, double expectedImaginary) = ReferenceMultiply(a, b, c, d);
                yield return new object[] { a, b, c, d, expectedReal, expectedImaginary };
            }
        }

        public static IEnumerable<object[]> Divide_SpecialValues()
        {
            foreach (double a in s_specialGrid)
            foreach (double b in s_specialGrid)
            foreach (double c in s_specialGrid)
            foreach (double d in s_specialGrid)
            {
                (double expectedReal, double expectedImaginary) = ReferenceDivide(a, b, c, d);
                yield return new object[] { a, b, c, d, expectedReal, expectedImaginary };
            }
        }

        public static IEnumerable<object[]> Reciprocal_SpecialValues()
        {
            foreach (double c in s_specialGrid)
            foreach (double d in s_specialGrid)
            {
                double expectedReal, expectedImaginary;

                if ((c == 0.0) && (d == 0.0))
                {
                    // Reciprocal special-cases an exact zero to Zero instead of a directed infinity.
                    expectedReal = 0.0;
                    expectedImaginary = 0.0;
                }
                else
                {
                    (expectedReal, expectedImaginary) = ReferenceDivide(1.0, 0.0, c, d);
                }

                yield return new object[] { c, d, expectedReal, expectedImaginary };
            }
        }

        [Theory]
        [MemberData(nameof(Abs_SpecialValues))]
        public static void Abs(double real, double imaginary, double expected)
        {
            AssertSame(Complex<double>.Abs(new Complex<double>(real, imaginary)), expected, $"Abs<Double>({real}, {imaginary})");
            AssertSame(Complex<float>.Abs(new Complex<float>((float)real, (float)imaginary)), expected, $"Abs<Single>({real}, {imaginary})");
            AssertSame(Complex<Half>.Abs(new Complex<Half>((Half)real, (Half)imaginary)), expected, $"Abs<Half>({real}, {imaginary})");
        }

        public static IEnumerable<object[]> Abs_SpecialValues()
        {
            foreach (double real in s_specialGrid)
            foreach (double imaginary in s_specialGrid)
            {
                // Finite-finite magnitudes (e.g. hypot(1, 1) = sqrt(2)) are ordinary accuracy,
                // not special-value conformance, and are not exact across double/float/Half.
                if (double.IsFinite(real) && double.IsFinite(imaginary))
                {
                    continue;
                }

                yield return new object[] { real, imaginary, ReferenceAbs(real, imaginary) };
            }
        }

        // C23 Annex G.6 cabs: an infinite component yields +inf even when the other is NaN;
        // otherwise a NaN component yields NaN.
        private static double ReferenceAbs(double real, double imaginary)
        {
            if (double.IsInfinity(real) || double.IsInfinity(imaginary))
            {
                return double.PositiveInfinity;
            }
            return double.NaN;
        }

        [Theory]
        [MemberData(nameof(Sqrt_SpecialValues))]
        public static void Sqrt(double real, double imaginary, double expectedReal, double expectedImaginary)
        {
            Verify<double>(Complex<double>.Sqrt, "Sqrt", real, imaginary, expectedReal, expectedImaginary);
            Verify<float>(Complex<float>.Sqrt, "Sqrt", real, imaginary, expectedReal, expectedImaginary);
            Verify<Half>(Complex<Half>.Sqrt, "Sqrt", real, imaginary, expectedReal, expectedImaginary);
        }

        [Theory]
        [MemberData(nameof(Exp_SpecialValues))]
        public static void Exp(double real, double imaginary, double expectedReal, double expectedImaginary)
        {
            Verify<double>(Complex<double>.Exp, "Exp", real, imaginary, expectedReal, expectedImaginary);
            Verify<float>(Complex<float>.Exp, "Exp", real, imaginary, expectedReal, expectedImaginary);
            Verify<Half>(Complex<Half>.Exp, "Exp", real, imaginary, expectedReal, expectedImaginary);
        }

        [Theory]
        [MemberData(nameof(Log_SpecialValues))]
        public static void Log(double real, double imaginary, double expectedReal, double expectedImaginary)
        {
            Verify<double>(Complex<double>.Log, "Log", real, imaginary, expectedReal, expectedImaginary);
            Verify<float>(Complex<float>.Log, "Log", real, imaginary, expectedReal, expectedImaginary);
            Verify<Half>(Complex<Half>.Log, "Log", real, imaginary, expectedReal, expectedImaginary);
        }

        [Theory]
        [MemberData(nameof(Sin_SpecialValues))]
        public static void Sin(double real, double imaginary, double expectedReal, double expectedImaginary)
        {
            Verify<double>(Complex<double>.Sin, "Sin", real, imaginary, expectedReal, expectedImaginary);
            Verify<float>(Complex<float>.Sin, "Sin", real, imaginary, expectedReal, expectedImaginary);
            Verify<Half>(Complex<Half>.Sin, "Sin", real, imaginary, expectedReal, expectedImaginary);
        }

        [Theory]
        [MemberData(nameof(Cos_SpecialValues))]
        public static void Cos(double real, double imaginary, double expectedReal, double expectedImaginary)
        {
            Verify<double>(Complex<double>.Cos, "Cos", real, imaginary, expectedReal, expectedImaginary);
            Verify<float>(Complex<float>.Cos, "Cos", real, imaginary, expectedReal, expectedImaginary);
            Verify<Half>(Complex<Half>.Cos, "Cos", real, imaginary, expectedReal, expectedImaginary);
        }

        [Theory]
        [MemberData(nameof(Tan_SpecialValues))]
        public static void Tan(double real, double imaginary, double expectedReal, double expectedImaginary)
        {
            Verify<double>(Complex<double>.Tan, "Tan", real, imaginary, expectedReal, expectedImaginary);
            Verify<float>(Complex<float>.Tan, "Tan", real, imaginary, expectedReal, expectedImaginary);
            Verify<Half>(Complex<Half>.Tan, "Tan", real, imaginary, expectedReal, expectedImaginary);
        }

        [Theory]
        [MemberData(nameof(Sinh_SpecialValues))]
        public static void Sinh(double real, double imaginary, double expectedReal, double expectedImaginary)
        {
            Verify<double>(Complex<double>.Sinh, "Sinh", real, imaginary, expectedReal, expectedImaginary);
            Verify<float>(Complex<float>.Sinh, "Sinh", real, imaginary, expectedReal, expectedImaginary);
            Verify<Half>(Complex<Half>.Sinh, "Sinh", real, imaginary, expectedReal, expectedImaginary);
        }

        [Theory]
        [MemberData(nameof(Cosh_SpecialValues))]
        public static void Cosh(double real, double imaginary, double expectedReal, double expectedImaginary)
        {
            Verify<double>(Complex<double>.Cosh, "Cosh", real, imaginary, expectedReal, expectedImaginary);
            Verify<float>(Complex<float>.Cosh, "Cosh", real, imaginary, expectedReal, expectedImaginary);
            Verify<Half>(Complex<Half>.Cosh, "Cosh", real, imaginary, expectedReal, expectedImaginary);
        }

        [Theory]
        [MemberData(nameof(Tanh_SpecialValues))]
        public static void Tanh(double real, double imaginary, double expectedReal, double expectedImaginary)
        {
            Verify<double>(Complex<double>.Tanh, "Tanh", real, imaginary, expectedReal, expectedImaginary);
            Verify<float>(Complex<float>.Tanh, "Tanh", real, imaginary, expectedReal, expectedImaginary);
            Verify<Half>(Complex<Half>.Tanh, "Tanh", real, imaginary, expectedReal, expectedImaginary);
        }

        [Fact]
        public static void Tanh_LargeImaginary_HasStableZeroSign()
        {
            // ctanh(+-INF + iy) is +-1 + i*copysign(0, sin(2y)). Once |y| passes MaxValue/2,
            // 2*y overflows and sin() collapses to a NaN, so the zero's sign must be recovered
            // without doubling y. Pin it through the Annex G symmetry ctanh(conj(z)) ==
            // conj(ctanh(z)): the two zero imaginary parts must carry opposite signs.
            TanhLargeImaginaryCore<double>();
            TanhLargeImaginaryCore<float>();
            TanhLargeImaginaryCore<Half>();
        }

        private static void TanhLargeImaginaryCore<T>()
            where T : IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            T y = T.MaxValue; // y + y overflows to +INF for every supported T
            Complex<T> plus = Complex<T>.Tanh(new Complex<T>(T.PositiveInfinity, y));
            Complex<T> minus = Complex<T>.Tanh(new Complex<T>(T.PositiveInfinity, -y));

            string context = $"Tanh<{typeof(T).Name}>(+INF, +-MaxValue)";
            Assert.True(plus.Real == T.One, $"{context}.Real: expected 1, got {plus.Real}");
            Assert.True(minus.Real == T.One, $"{context}(conj).Real: expected 1, got {minus.Real}");
            Assert.True(plus.Imaginary == T.Zero, $"{context}.Imaginary: expected 0, got {plus.Imaginary}");
            Assert.True(minus.Imaginary == T.Zero, $"{context}(conj).Imaginary: expected 0, got {minus.Imaginary}");

            Assert.True(T.IsNegative(plus.Imaginary) != T.IsNegative(minus.Imaginary),
                $"{context}: conjugate symmetry lost, both zero signs are {(T.IsNegative(plus.Imaginary) ? "negative" : "positive")}");
        }

        [Theory]
        [MemberData(nameof(Asin_SpecialValues))]
        public static void Asin(double real, double imaginary, double expectedReal, double expectedImaginary)
        {
            Verify<double>(Complex<double>.Asin, "Asin", real, imaginary, expectedReal, expectedImaginary);
            Verify<float>(Complex<float>.Asin, "Asin", real, imaginary, expectedReal, expectedImaginary);
            Verify<Half>(Complex<Half>.Asin, "Asin", real, imaginary, expectedReal, expectedImaginary);
        }

        [Theory]
        [MemberData(nameof(Acos_SpecialValues))]
        public static void Acos(double real, double imaginary, double expectedReal, double expectedImaginary)
        {
            Verify<double>(Complex<double>.Acos, "Acos", real, imaginary, expectedReal, expectedImaginary);
            Verify<float>(Complex<float>.Acos, "Acos", real, imaginary, expectedReal, expectedImaginary);
            Verify<Half>(Complex<Half>.Acos, "Acos", real, imaginary, expectedReal, expectedImaginary);
        }

        [Theory]
        [MemberData(nameof(Atan_SpecialValues))]
        public static void Atan(double real, double imaginary, double expectedReal, double expectedImaginary)
        {
            Verify<double>(Complex<double>.Atan, "Atan", real, imaginary, expectedReal, expectedImaginary);
            Verify<float>(Complex<float>.Atan, "Atan", real, imaginary, expectedReal, expectedImaginary);
            Verify<Half>(Complex<Half>.Atan, "Atan", real, imaginary, expectedReal, expectedImaginary);
        }

        // C23 G.6.4: cpow(z, w) special values are those of cexp(w * clog(z)). Pow defers any
        // non-finite input or magnitude-overflowing base to that expression; verify the deferral
        // holds bit-for-bit so a future rewrite cannot silently route these through the polar core.
        [Fact]
        public static void Pow_DefersToExpLogForSpecialValues()
        {
            PowDefersCore<double>();
            PowDefersCore<float>();
            PowDefersCore<Half>();
        }

        private static void PowDefersCore<T>()
            where T : IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            T inf = T.PositiveInfinity;
            T ninf = T.NegativeInfinity;
            T nan = T.NaN;
            T zero = T.Zero;
            T one = T.One;
            T two = T.CreateChecked(2);

            Complex<T>[] bases =
            {
                new Complex<T>(inf, zero), new Complex<T>(ninf, one), new Complex<T>(zero, inf),
                new Complex<T>(nan, one), new Complex<T>(one, nan), new Complex<T>(inf, inf),
                new Complex<T>(T.MaxValue, T.MaxValue), // finite, but Abs overflows to infinity
            };
            Complex<T>[] powers =
            {
                new Complex<T>(two, zero), new Complex<T>(one, one), new Complex<T>(zero, one),
                new Complex<T>(inf, zero),
            };

            foreach (Complex<T> b in bases)
            {
                foreach (Complex<T> p in powers)
                {
                    Complex<T> actual = Complex<T>.Pow(b, p);
                    Complex<T> expected = Complex<T>.Exp(p * Complex<T>.Log(b));
                    string context = $"Pow<{typeof(T).Name}>({b}, {p}).";
                    AssertIdentical(actual.Real, expected.Real, context + "Real");
                    AssertIdentical(actual.Imaginary, expected.Imaginary, context + "Imaginary");
                }
            }
        }

        private static void AssertIdentical<T>(T actual, T expected, string context)
            where T : IFloatingPointIeee754<T>
        {
            if (T.IsNaN(expected))
            {
                Assert.True(T.IsNaN(actual), $"{context}: expected NaN, got {actual}");
                return;
            }
            Assert.True((actual == expected) && (T.IsNegative(actual) == T.IsNegative(expected)), $"{context}: expected {expected}, got {actual}");
        }

        public static IEnumerable<object[]> Sqrt_SpecialValues() => new object[][]
        {
            new object[] { NegativeInfinity, NegativeInfinity, PositiveInfinity, NegativeInfinity },
            new object[] { NegativeInfinity, -1.0, 0.0, NegativeInfinity },
            new object[] { NegativeInfinity, -0.0, 0.0, NegativeInfinity },
            new object[] { NegativeInfinity, 0.0, 0.0, PositiveInfinity },
            new object[] { NegativeInfinity, 1.0, 0.0, PositiveInfinity },
            new object[] { NegativeInfinity, PositiveInfinity, PositiveInfinity, PositiveInfinity },
            new object[] { NegativeInfinity, NaN, NaN, PositiveInfinity },
            new object[] { -1.0, NegativeInfinity, PositiveInfinity, NegativeInfinity },
            new object[] { -1.0, -0.0, 0.0, -1.0 },
            new object[] { -1.0, 0.0, 0.0, 1.0 },
            new object[] { -1.0, PositiveInfinity, PositiveInfinity, PositiveInfinity },
            new object[] { -1.0, NaN, NaN, NaN },
            new object[] { -0.0, NegativeInfinity, PositiveInfinity, NegativeInfinity },
            new object[] { -0.0, -0.0, 0.0, -0.0 },
            new object[] { -0.0, 0.0, 0.0, 0.0 },
            new object[] { -0.0, PositiveInfinity, PositiveInfinity, PositiveInfinity },
            new object[] { -0.0, NaN, NaN, NaN },
            new object[] { 0.0, NegativeInfinity, PositiveInfinity, NegativeInfinity },
            new object[] { 0.0, -0.0, 0.0, -0.0 },
            new object[] { 0.0, 0.0, 0.0, 0.0 },
            new object[] { 0.0, PositiveInfinity, PositiveInfinity, PositiveInfinity },
            new object[] { 0.0, NaN, NaN, NaN },
            new object[] { 1.0, NegativeInfinity, PositiveInfinity, NegativeInfinity },
            new object[] { 1.0, -0.0, 1.0, -0.0 },
            new object[] { 1.0, 0.0, 1.0, 0.0 },
            new object[] { 1.0, PositiveInfinity, PositiveInfinity, PositiveInfinity },
            new object[] { 1.0, NaN, NaN, NaN },
            new object[] { PositiveInfinity, NegativeInfinity, PositiveInfinity, NegativeInfinity },
            new object[] { PositiveInfinity, -1.0, PositiveInfinity, -0.0 },
            new object[] { PositiveInfinity, -0.0, PositiveInfinity, -0.0 },
            new object[] { PositiveInfinity, 0.0, PositiveInfinity, 0.0 },
            new object[] { PositiveInfinity, 1.0, PositiveInfinity, 0.0 },
            new object[] { PositiveInfinity, PositiveInfinity, PositiveInfinity, PositiveInfinity },
            new object[] { PositiveInfinity, NaN, PositiveInfinity, NaN },
            new object[] { NaN, NegativeInfinity, PositiveInfinity, NegativeInfinity },
            new object[] { NaN, -1.0, NaN, NaN },
            new object[] { NaN, -0.0, NaN, NaN },
            new object[] { NaN, 0.0, NaN, NaN },
            new object[] { NaN, 1.0, NaN, NaN },
            new object[] { NaN, PositiveInfinity, PositiveInfinity, PositiveInfinity },
            new object[] { NaN, NaN, NaN, NaN },
        };

        public static IEnumerable<object[]> Exp_SpecialValues() => new object[][]
        {
            new object[] { NegativeInfinity, NegativeInfinity, 0.0, 0.0 },
            new object[] { NegativeInfinity, -1.0, 0.0, -0.0 },
            new object[] { NegativeInfinity, -0.0, 0.0, -0.0 },
            new object[] { NegativeInfinity, 0.0, 0.0, 0.0 },
            new object[] { NegativeInfinity, 1.0, 0.0, 0.0 },
            new object[] { NegativeInfinity, PositiveInfinity, 0.0, 0.0 },
            new object[] { NegativeInfinity, NaN, 0.0, 0.0 },
            new object[] { -1.0, NegativeInfinity, NaN, NaN },
            new object[] { -1.0, PositiveInfinity, NaN, NaN },
            new object[] { -1.0, NaN, NaN, NaN },
            new object[] { -0.0, NegativeInfinity, NaN, NaN },
            new object[] { -0.0, -0.0, 1.0, -0.0 },
            new object[] { -0.0, 0.0, 1.0, 0.0 },
            new object[] { -0.0, PositiveInfinity, NaN, NaN },
            new object[] { -0.0, NaN, NaN, NaN },
            new object[] { 0.0, NegativeInfinity, NaN, NaN },
            new object[] { 0.0, -0.0, 1.0, -0.0 },
            new object[] { 0.0, 0.0, 1.0, 0.0 },
            new object[] { 0.0, PositiveInfinity, NaN, NaN },
            new object[] { 0.0, NaN, NaN, NaN },
            new object[] { 1.0, NegativeInfinity, NaN, NaN },
            new object[] { 1.0, PositiveInfinity, NaN, NaN },
            new object[] { 1.0, NaN, NaN, NaN },
            new object[] { PositiveInfinity, NegativeInfinity, PositiveInfinity, NaN },
            new object[] { PositiveInfinity, -1.0, PositiveInfinity, NegativeInfinity },
            new object[] { PositiveInfinity, -0.0, PositiveInfinity, -0.0 },
            new object[] { PositiveInfinity, 0.0, PositiveInfinity, 0.0 },
            new object[] { PositiveInfinity, 1.0, PositiveInfinity, PositiveInfinity },
            new object[] { PositiveInfinity, PositiveInfinity, PositiveInfinity, NaN },
            new object[] { PositiveInfinity, NaN, PositiveInfinity, NaN },
            new object[] { NaN, NegativeInfinity, NaN, NaN },
            new object[] { NaN, -1.0, NaN, NaN },
            new object[] { NaN, -0.0, NaN, -0.0 },
            new object[] { NaN, 0.0, NaN, 0.0 },
            new object[] { NaN, 1.0, NaN, NaN },
            new object[] { NaN, PositiveInfinity, NaN, NaN },
            new object[] { NaN, NaN, NaN, NaN },
        };

        public static IEnumerable<object[]> Log_SpecialValues() => new object[][]
        {
            new object[] { NegativeInfinity, NaN, PositiveInfinity, NaN },
            new object[] { -1.0, NaN, NaN, NaN },
            new object[] { -0.0, NaN, NaN, NaN },
            new object[] { 0.0, -0.0, NegativeInfinity, -0.0 },
            new object[] { 0.0, 0.0, NegativeInfinity, 0.0 },
            new object[] { 0.0, NaN, NaN, NaN },
            new object[] { 1.0, -0.0, 0.0, -0.0 },
            new object[] { 1.0, 0.0, 0.0, 0.0 },
            new object[] { 1.0, NaN, NaN, NaN },
            new object[] { PositiveInfinity, -1.0, PositiveInfinity, -0.0 },
            new object[] { PositiveInfinity, -0.0, PositiveInfinity, -0.0 },
            new object[] { PositiveInfinity, 0.0, PositiveInfinity, 0.0 },
            new object[] { PositiveInfinity, 1.0, PositiveInfinity, 0.0 },
            new object[] { PositiveInfinity, NaN, PositiveInfinity, NaN },
            new object[] { NaN, NegativeInfinity, PositiveInfinity, NaN },
            new object[] { NaN, -1.0, NaN, NaN },
            new object[] { NaN, -0.0, NaN, NaN },
            new object[] { NaN, 0.0, NaN, NaN },
            new object[] { NaN, 1.0, NaN, NaN },
            new object[] { NaN, PositiveInfinity, PositiveInfinity, NaN },
            new object[] { NaN, NaN, NaN, NaN },
        };

        public static IEnumerable<object[]> Sin_SpecialValues() => new object[][]
        {
            new object[] { NegativeInfinity, NegativeInfinity, NaN, NegativeInfinity },
            new object[] { NegativeInfinity, -1.0, NaN, NaN },
            new object[] { NegativeInfinity, -0.0, NaN, -0.0 },
            new object[] { NegativeInfinity, 0.0, NaN, 0.0 },
            new object[] { NegativeInfinity, 1.0, NaN, NaN },
            new object[] { NegativeInfinity, PositiveInfinity, NaN, PositiveInfinity },
            new object[] { NegativeInfinity, NaN, NaN, NaN },
            new object[] { -1.0, NegativeInfinity, NegativeInfinity, NegativeInfinity },
            new object[] { -1.0, PositiveInfinity, NegativeInfinity, PositiveInfinity },
            new object[] { -1.0, NaN, NaN, NaN },
            new object[] { -0.0, NegativeInfinity, -0.0, NegativeInfinity },
            new object[] { -0.0, -0.0, -0.0, -0.0 },
            new object[] { -0.0, 0.0, -0.0, 0.0 },
            new object[] { -0.0, PositiveInfinity, -0.0, PositiveInfinity },
            new object[] { -0.0, NaN, -0.0, NaN },
            new object[] { 0.0, NegativeInfinity, 0.0, NegativeInfinity },
            new object[] { 0.0, -0.0, 0.0, -0.0 },
            new object[] { 0.0, 0.0, 0.0, 0.0 },
            new object[] { 0.0, PositiveInfinity, 0.0, PositiveInfinity },
            new object[] { 0.0, NaN, 0.0, NaN },
            new object[] { 1.0, NegativeInfinity, PositiveInfinity, NegativeInfinity },
            new object[] { 1.0, PositiveInfinity, PositiveInfinity, PositiveInfinity },
            new object[] { 1.0, NaN, NaN, NaN },
            new object[] { PositiveInfinity, NegativeInfinity, NaN, NegativeInfinity },
            new object[] { PositiveInfinity, -1.0, NaN, NaN },
            new object[] { PositiveInfinity, -0.0, NaN, -0.0 },
            new object[] { PositiveInfinity, 0.0, NaN, 0.0 },
            new object[] { PositiveInfinity, 1.0, NaN, NaN },
            new object[] { PositiveInfinity, PositiveInfinity, NaN, PositiveInfinity },
            new object[] { PositiveInfinity, NaN, NaN, NaN },
            new object[] { NaN, NegativeInfinity, NaN, NegativeInfinity },
            new object[] { NaN, -1.0, NaN, NaN },
            new object[] { NaN, -0.0, NaN, -0.0 },
            new object[] { NaN, 0.0, NaN, 0.0 },
            new object[] { NaN, 1.0, NaN, NaN },
            new object[] { NaN, PositiveInfinity, NaN, PositiveInfinity },
            new object[] { NaN, NaN, NaN, NaN },
        };

        public static IEnumerable<object[]> Cos_SpecialValues() => new object[][]
        {
            new object[] { NegativeInfinity, NegativeInfinity, PositiveInfinity, NaN },
            new object[] { NegativeInfinity, -1.0, NaN, NaN },
            new object[] { NegativeInfinity, -0.0, NaN, 0.0 },
            new object[] { NegativeInfinity, 0.0, NaN, -0.0 },
            new object[] { NegativeInfinity, 1.0, NaN, NaN },
            new object[] { NegativeInfinity, PositiveInfinity, PositiveInfinity, NaN },
            new object[] { NegativeInfinity, NaN, NaN, NaN },
            new object[] { -1.0, NegativeInfinity, PositiveInfinity, NegativeInfinity },
            new object[] { -1.0, PositiveInfinity, PositiveInfinity, PositiveInfinity },
            new object[] { -1.0, NaN, NaN, NaN },
            new object[] { -0.0, NegativeInfinity, PositiveInfinity, -0.0 },
            new object[] { -0.0, -0.0, 1.0, -0.0 },
            new object[] { -0.0, 0.0, 1.0, 0.0 },
            new object[] { -0.0, PositiveInfinity, PositiveInfinity, 0.0 },
            new object[] { -0.0, NaN, NaN, -0.0 },
            new object[] { 0.0, NegativeInfinity, PositiveInfinity, 0.0 },
            new object[] { 0.0, -0.0, 1.0, 0.0 },
            new object[] { 0.0, 0.0, 1.0, -0.0 },
            new object[] { 0.0, PositiveInfinity, PositiveInfinity, -0.0 },
            new object[] { 0.0, NaN, NaN, 0.0 },
            new object[] { 1.0, NegativeInfinity, PositiveInfinity, PositiveInfinity },
            new object[] { 1.0, PositiveInfinity, PositiveInfinity, NegativeInfinity },
            new object[] { 1.0, NaN, NaN, NaN },
            new object[] { PositiveInfinity, NegativeInfinity, PositiveInfinity, NaN },
            new object[] { PositiveInfinity, -1.0, NaN, NaN },
            new object[] { PositiveInfinity, -0.0, NaN, 0.0 },
            new object[] { PositiveInfinity, 0.0, NaN, -0.0 },
            new object[] { PositiveInfinity, 1.0, NaN, NaN },
            new object[] { PositiveInfinity, PositiveInfinity, PositiveInfinity, NaN },
            new object[] { PositiveInfinity, NaN, NaN, NaN },
            new object[] { NaN, NegativeInfinity, PositiveInfinity, NaN },
            new object[] { NaN, -1.0, NaN, NaN },
            new object[] { NaN, -0.0, NaN, 0.0 },
            new object[] { NaN, 0.0, NaN, -0.0 },
            new object[] { NaN, 1.0, NaN, NaN },
            new object[] { NaN, PositiveInfinity, PositiveInfinity, NaN },
            new object[] { NaN, NaN, NaN, NaN },
        };

        public static IEnumerable<object[]> Tan_SpecialValues() => new object[][]
        {
            new object[] { NegativeInfinity, NegativeInfinity, -0.0, -1.0 },
            new object[] { NegativeInfinity, -1.0, NaN, NaN },
            new object[] { NegativeInfinity, -0.0, NaN, -0.0 },
            new object[] { NegativeInfinity, 0.0, NaN, 0.0 },
            new object[] { NegativeInfinity, 1.0, NaN, NaN },
            new object[] { NegativeInfinity, PositiveInfinity, -0.0, 1.0 },
            new object[] { NegativeInfinity, NaN, NaN, NaN },
            new object[] { -1.0, NegativeInfinity, -0.0, -1.0 },
            new object[] { -1.0, PositiveInfinity, -0.0, 1.0 },
            new object[] { -1.0, NaN, NaN, NaN },
            new object[] { -0.0, NegativeInfinity, -0.0, -1.0 },
            new object[] { -0.0, -0.0, -0.0, -0.0 },
            new object[] { -0.0, 0.0, -0.0, 0.0 },
            new object[] { -0.0, PositiveInfinity, -0.0, 1.0 },
            new object[] { -0.0, NaN, -0.0, NaN },
            new object[] { 0.0, NegativeInfinity, 0.0, -1.0 },
            new object[] { 0.0, -0.0, 0.0, -0.0 },
            new object[] { 0.0, 0.0, 0.0, 0.0 },
            new object[] { 0.0, PositiveInfinity, 0.0, 1.0 },
            new object[] { 0.0, NaN, 0.0, NaN },
            new object[] { 1.0, NegativeInfinity, 0.0, -1.0 },
            new object[] { 1.0, PositiveInfinity, 0.0, 1.0 },
            new object[] { 1.0, NaN, NaN, NaN },
            new object[] { PositiveInfinity, NegativeInfinity, 0.0, -1.0 },
            new object[] { PositiveInfinity, -1.0, NaN, NaN },
            new object[] { PositiveInfinity, -0.0, NaN, -0.0 },
            new object[] { PositiveInfinity, 0.0, NaN, 0.0 },
            new object[] { PositiveInfinity, 1.0, NaN, NaN },
            new object[] { PositiveInfinity, PositiveInfinity, 0.0, 1.0 },
            new object[] { PositiveInfinity, NaN, NaN, NaN },
            new object[] { NaN, NegativeInfinity, -0.0, -1.0 },
            new object[] { NaN, -1.0, NaN, NaN },
            new object[] { NaN, -0.0, NaN, -0.0 },
            new object[] { NaN, 0.0, NaN, 0.0 },
            new object[] { NaN, 1.0, NaN, NaN },
            new object[] { NaN, PositiveInfinity, -0.0, 1.0 },
            new object[] { NaN, NaN, NaN, NaN },
        };

        public static IEnumerable<object[]> Sinh_SpecialValues() => new object[][]
        {
            new object[] { NegativeInfinity, NegativeInfinity, NegativeInfinity, NaN },
            new object[] { NegativeInfinity, -1.0, NegativeInfinity, NegativeInfinity },
            new object[] { NegativeInfinity, -0.0, NegativeInfinity, -0.0 },
            new object[] { NegativeInfinity, 0.0, NegativeInfinity, 0.0 },
            new object[] { NegativeInfinity, 1.0, NegativeInfinity, PositiveInfinity },
            new object[] { NegativeInfinity, PositiveInfinity, NegativeInfinity, NaN },
            new object[] { NegativeInfinity, NaN, NegativeInfinity, NaN },
            new object[] { -1.0, NegativeInfinity, NaN, NaN },
            new object[] { -1.0, PositiveInfinity, NaN, NaN },
            new object[] { -1.0, NaN, NaN, NaN },
            new object[] { -0.0, NegativeInfinity, -0.0, NaN },
            new object[] { -0.0, -0.0, -0.0, -0.0 },
            new object[] { -0.0, 0.0, -0.0, 0.0 },
            new object[] { -0.0, PositiveInfinity, -0.0, NaN },
            new object[] { -0.0, NaN, -0.0, NaN },
            new object[] { 0.0, NegativeInfinity, 0.0, NaN },
            new object[] { 0.0, -0.0, 0.0, -0.0 },
            new object[] { 0.0, 0.0, 0.0, 0.0 },
            new object[] { 0.0, PositiveInfinity, 0.0, NaN },
            new object[] { 0.0, NaN, 0.0, NaN },
            new object[] { 1.0, NegativeInfinity, NaN, NaN },
            new object[] { 1.0, PositiveInfinity, NaN, NaN },
            new object[] { 1.0, NaN, NaN, NaN },
            new object[] { PositiveInfinity, NegativeInfinity, PositiveInfinity, NaN },
            new object[] { PositiveInfinity, -1.0, PositiveInfinity, NegativeInfinity },
            new object[] { PositiveInfinity, -0.0, PositiveInfinity, -0.0 },
            new object[] { PositiveInfinity, 0.0, PositiveInfinity, 0.0 },
            new object[] { PositiveInfinity, 1.0, PositiveInfinity, PositiveInfinity },
            new object[] { PositiveInfinity, PositiveInfinity, PositiveInfinity, NaN },
            new object[] { PositiveInfinity, NaN, PositiveInfinity, NaN },
            new object[] { NaN, NegativeInfinity, NaN, NaN },
            new object[] { NaN, -1.0, NaN, NaN },
            new object[] { NaN, -0.0, NaN, -0.0 },
            new object[] { NaN, 0.0, NaN, 0.0 },
            new object[] { NaN, 1.0, NaN, NaN },
            new object[] { NaN, PositiveInfinity, NaN, NaN },
            new object[] { NaN, NaN, NaN, NaN },
        };

        public static IEnumerable<object[]> Cosh_SpecialValues() => new object[][]
        {
            new object[] { NegativeInfinity, NegativeInfinity, PositiveInfinity, NaN },
            new object[] { NegativeInfinity, -1.0, PositiveInfinity, PositiveInfinity },
            new object[] { NegativeInfinity, -0.0, PositiveInfinity, 0.0 },
            new object[] { NegativeInfinity, 0.0, PositiveInfinity, -0.0 },
            new object[] { NegativeInfinity, 1.0, PositiveInfinity, NegativeInfinity },
            new object[] { NegativeInfinity, PositiveInfinity, PositiveInfinity, NaN },
            new object[] { NegativeInfinity, NaN, PositiveInfinity, NaN },
            new object[] { -1.0, NegativeInfinity, NaN, NaN },
            new object[] { -1.0, PositiveInfinity, NaN, NaN },
            new object[] { -1.0, NaN, NaN, NaN },
            new object[] { -0.0, NegativeInfinity, NaN, -0.0 },
            new object[] { -0.0, -0.0, 1.0, 0.0 },
            new object[] { -0.0, 0.0, 1.0, -0.0 },
            new object[] { -0.0, PositiveInfinity, NaN, -0.0 },
            new object[] { -0.0, NaN, NaN, -0.0 },
            new object[] { 0.0, NegativeInfinity, NaN, 0.0 },
            new object[] { 0.0, -0.0, 1.0, -0.0 },
            new object[] { 0.0, 0.0, 1.0, 0.0 },
            new object[] { 0.0, PositiveInfinity, NaN, 0.0 },
            new object[] { 0.0, NaN, NaN, 0.0 },
            new object[] { 1.0, NegativeInfinity, NaN, NaN },
            new object[] { 1.0, PositiveInfinity, NaN, NaN },
            new object[] { 1.0, NaN, NaN, NaN },
            new object[] { PositiveInfinity, NegativeInfinity, PositiveInfinity, NaN },
            new object[] { PositiveInfinity, -1.0, PositiveInfinity, NegativeInfinity },
            new object[] { PositiveInfinity, -0.0, PositiveInfinity, -0.0 },
            new object[] { PositiveInfinity, 0.0, PositiveInfinity, 0.0 },
            new object[] { PositiveInfinity, 1.0, PositiveInfinity, PositiveInfinity },
            new object[] { PositiveInfinity, PositiveInfinity, PositiveInfinity, NaN },
            new object[] { PositiveInfinity, NaN, PositiveInfinity, NaN },
            new object[] { NaN, NegativeInfinity, NaN, NaN },
            new object[] { NaN, -1.0, NaN, NaN },
            new object[] { NaN, -0.0, NaN, -0.0 },
            new object[] { NaN, 0.0, NaN, 0.0 },
            new object[] { NaN, 1.0, NaN, NaN },
            new object[] { NaN, PositiveInfinity, NaN, NaN },
            new object[] { NaN, NaN, NaN, NaN },
        };

        public static IEnumerable<object[]> Tanh_SpecialValues() => new object[][]
        {
            new object[] { NegativeInfinity, NegativeInfinity, -1.0, -0.0 },
            new object[] { NegativeInfinity, -1.0, -1.0, -0.0 },
            new object[] { NegativeInfinity, -0.0, -1.0, -0.0 },
            new object[] { NegativeInfinity, 0.0, -1.0, 0.0 },
            new object[] { NegativeInfinity, 1.0, -1.0, 0.0 },
            new object[] { NegativeInfinity, PositiveInfinity, -1.0, 0.0 },
            new object[] { NegativeInfinity, NaN, -1.0, -0.0 },
            new object[] { -1.0, NegativeInfinity, NaN, NaN },
            new object[] { -1.0, PositiveInfinity, NaN, NaN },
            new object[] { -1.0, NaN, NaN, NaN },
            new object[] { -0.0, NegativeInfinity, -0.0, NaN },
            new object[] { -0.0, -0.0, -0.0, -0.0 },
            new object[] { -0.0, 0.0, -0.0, 0.0 },
            new object[] { -0.0, PositiveInfinity, -0.0, NaN },
            new object[] { -0.0, NaN, -0.0, NaN },
            new object[] { 0.0, NegativeInfinity, 0.0, NaN },
            new object[] { 0.0, -0.0, 0.0, -0.0 },
            new object[] { 0.0, 0.0, 0.0, 0.0 },
            new object[] { 0.0, PositiveInfinity, 0.0, NaN },
            new object[] { 0.0, NaN, 0.0, NaN },
            new object[] { 1.0, NegativeInfinity, NaN, NaN },
            new object[] { 1.0, PositiveInfinity, NaN, NaN },
            new object[] { 1.0, NaN, NaN, NaN },
            new object[] { PositiveInfinity, NegativeInfinity, 1.0, -0.0 },
            new object[] { PositiveInfinity, -1.0, 1.0, -0.0 },
            new object[] { PositiveInfinity, -0.0, 1.0, -0.0 },
            new object[] { PositiveInfinity, 0.0, 1.0, 0.0 },
            new object[] { PositiveInfinity, 1.0, 1.0, 0.0 },
            new object[] { PositiveInfinity, PositiveInfinity, 1.0, 0.0 },
            new object[] { PositiveInfinity, NaN, 1.0, -0.0 },
            new object[] { NaN, NegativeInfinity, NaN, NaN },
            new object[] { NaN, -1.0, NaN, NaN },
            new object[] { NaN, -0.0, NaN, -0.0 },
            new object[] { NaN, 0.0, NaN, 0.0 },
            new object[] { NaN, 1.0, NaN, NaN },
            new object[] { NaN, PositiveInfinity, NaN, NaN },
            new object[] { NaN, NaN, NaN, NaN },
        };

        public static IEnumerable<object[]> Asin_SpecialValues() => new object[][]
        {
            new object[] { NegativeInfinity, NegativeInfinity, -(Math.PI / 4), NegativeInfinity },
            new object[] { NegativeInfinity, -1.0, -(Math.PI / 2), NegativeInfinity },
            new object[] { NegativeInfinity, -0.0, -(Math.PI / 2), NegativeInfinity },
            new object[] { NegativeInfinity, 0.0, -(Math.PI / 2), PositiveInfinity },
            new object[] { NegativeInfinity, 1.0, -(Math.PI / 2), PositiveInfinity },
            new object[] { NegativeInfinity, PositiveInfinity, -(Math.PI / 4), PositiveInfinity },
            new object[] { NegativeInfinity, NaN, NaN, NegativeInfinity },
            new object[] { -1.0, NegativeInfinity, -0.0, NegativeInfinity },
            new object[] { -1.0, PositiveInfinity, -0.0, PositiveInfinity },
            new object[] { -1.0, NaN, NaN, NaN },
            new object[] { -0.0, NegativeInfinity, -0.0, NegativeInfinity },
            new object[] { -0.0, PositiveInfinity, -0.0, PositiveInfinity },
            new object[] { -0.0, NaN, -0.0, NaN },
            new object[] { 0.0, NegativeInfinity, 0.0, NegativeInfinity },
            new object[] { 0.0, PositiveInfinity, 0.0, PositiveInfinity },
            new object[] { 0.0, NaN, 0.0, NaN },
            new object[] { 1.0, NegativeInfinity, 0.0, NegativeInfinity },
            new object[] { 1.0, PositiveInfinity, 0.0, PositiveInfinity },
            new object[] { 1.0, NaN, NaN, NaN },
            new object[] { PositiveInfinity, NegativeInfinity, Math.PI / 4, NegativeInfinity },
            new object[] { PositiveInfinity, -1.0, Math.PI / 2, NegativeInfinity },
            new object[] { PositiveInfinity, -0.0, Math.PI / 2, NegativeInfinity },
            new object[] { PositiveInfinity, 0.0, Math.PI / 2, PositiveInfinity },
            new object[] { PositiveInfinity, 1.0, Math.PI / 2, PositiveInfinity },
            new object[] { PositiveInfinity, PositiveInfinity, Math.PI / 4, PositiveInfinity },
            new object[] { PositiveInfinity, NaN, NaN, NegativeInfinity },
            new object[] { NaN, NegativeInfinity, NaN, NegativeInfinity },
            new object[] { NaN, -1.0, NaN, NaN },
            new object[] { NaN, -0.0, NaN, NaN },
            new object[] { NaN, 0.0, NaN, NaN },
            new object[] { NaN, 1.0, NaN, NaN },
            new object[] { NaN, PositiveInfinity, NaN, PositiveInfinity },
            new object[] { NaN, NaN, NaN, NaN },
        };

        public static IEnumerable<object[]> Acos_SpecialValues() => new object[][]
        {
            new object[] { NegativeInfinity, NegativeInfinity, 3.0 * (Math.PI / 4), PositiveInfinity },
            new object[] { NegativeInfinity, -1.0, Math.PI, PositiveInfinity },
            new object[] { NegativeInfinity, -0.0, Math.PI, PositiveInfinity },
            new object[] { NegativeInfinity, 0.0, Math.PI, NegativeInfinity },
            new object[] { NegativeInfinity, 1.0, Math.PI, NegativeInfinity },
            new object[] { NegativeInfinity, PositiveInfinity, 3.0 * (Math.PI / 4), NegativeInfinity },
            new object[] { NegativeInfinity, NaN, NaN, PositiveInfinity },
            new object[] { -1.0, NegativeInfinity, Math.PI / 2, PositiveInfinity },
            new object[] { -1.0, PositiveInfinity, Math.PI / 2, NegativeInfinity },
            new object[] { -1.0, NaN, NaN, NaN },
            new object[] { -0.0, NegativeInfinity, Math.PI / 2, PositiveInfinity },
            new object[] { -0.0, PositiveInfinity, Math.PI / 2, NegativeInfinity },
            new object[] { -0.0, NaN, Math.PI / 2, NaN },
            new object[] { 0.0, NegativeInfinity, Math.PI / 2, PositiveInfinity },
            new object[] { 0.0, PositiveInfinity, Math.PI / 2, NegativeInfinity },
            new object[] { 0.0, NaN, Math.PI / 2, NaN },
            new object[] { 1.0, NegativeInfinity, Math.PI / 2, PositiveInfinity },
            new object[] { 1.0, PositiveInfinity, Math.PI / 2, NegativeInfinity },
            new object[] { 1.0, NaN, NaN, NaN },
            new object[] { PositiveInfinity, NegativeInfinity, Math.PI / 4, PositiveInfinity },
            new object[] { PositiveInfinity, -1.0, 0.0, PositiveInfinity },
            new object[] { PositiveInfinity, -0.0, 0.0, PositiveInfinity },
            new object[] { PositiveInfinity, 0.0, 0.0, NegativeInfinity },
            new object[] { PositiveInfinity, 1.0, 0.0, NegativeInfinity },
            new object[] { PositiveInfinity, PositiveInfinity, Math.PI / 4, NegativeInfinity },
            new object[] { PositiveInfinity, NaN, NaN, PositiveInfinity },
            new object[] { NaN, NegativeInfinity, NaN, PositiveInfinity },
            new object[] { NaN, -1.0, NaN, NaN },
            new object[] { NaN, -0.0, NaN, NaN },
            new object[] { NaN, 0.0, NaN, NaN },
            new object[] { NaN, 1.0, NaN, NaN },
            new object[] { NaN, PositiveInfinity, NaN, NegativeInfinity },
            new object[] { NaN, NaN, NaN, NaN },
        };

        public static IEnumerable<object[]> Atan_SpecialValues() => new object[][]
        {
            new object[] { NegativeInfinity, NegativeInfinity, -(Math.PI / 2), -0.0 },
            new object[] { NegativeInfinity, -1.0, -(Math.PI / 2), -0.0 },
            new object[] { NegativeInfinity, -0.0, -(Math.PI / 2), -0.0 },
            new object[] { NegativeInfinity, 0.0, -(Math.PI / 2), 0.0 },
            new object[] { NegativeInfinity, 1.0, -(Math.PI / 2), 0.0 },
            new object[] { NegativeInfinity, PositiveInfinity, -(Math.PI / 2), 0.0 },
            new object[] { NegativeInfinity, NaN, -(Math.PI / 2), -0.0 },
            new object[] { -1.0, NegativeInfinity, -(Math.PI / 2), -0.0 },
            new object[] { -1.0, PositiveInfinity, -(Math.PI / 2), 0.0 },
            new object[] { -1.0, NaN, NaN, NaN },
            new object[] { -0.0, NegativeInfinity, -(Math.PI / 2), -0.0 },
            new object[] { -0.0, PositiveInfinity, -(Math.PI / 2), 0.0 },
            new object[] { -0.0, NaN, NaN, NaN },
            new object[] { 0.0, NegativeInfinity, Math.PI / 2, -0.0 },
            new object[] { 0.0, PositiveInfinity, Math.PI / 2, 0.0 },
            new object[] { 0.0, NaN, NaN, NaN },
            new object[] { 1.0, NegativeInfinity, Math.PI / 2, -0.0 },
            new object[] { 1.0, PositiveInfinity, Math.PI / 2, 0.0 },
            new object[] { 1.0, NaN, NaN, NaN },
            new object[] { PositiveInfinity, NegativeInfinity, Math.PI / 2, -0.0 },
            new object[] { PositiveInfinity, -1.0, Math.PI / 2, -0.0 },
            new object[] { PositiveInfinity, -0.0, Math.PI / 2, -0.0 },
            new object[] { PositiveInfinity, 0.0, Math.PI / 2, 0.0 },
            new object[] { PositiveInfinity, 1.0, Math.PI / 2, 0.0 },
            new object[] { PositiveInfinity, PositiveInfinity, Math.PI / 2, 0.0 },
            new object[] { PositiveInfinity, NaN, Math.PI / 2, -0.0 },
            new object[] { NaN, NegativeInfinity, NaN, -0.0 },
            new object[] { NaN, -1.0, NaN, NaN },
            new object[] { NaN, -0.0, NaN, -0.0 },
            new object[] { NaN, 0.0, NaN, 0.0 },
            new object[] { NaN, 1.0, NaN, NaN },
            new object[] { NaN, PositiveInfinity, NaN, 0.0 },
            new object[] { NaN, NaN, NaN, NaN },
        };
    }
}
