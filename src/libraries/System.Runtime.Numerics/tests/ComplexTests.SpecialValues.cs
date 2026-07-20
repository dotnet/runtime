// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Numerics.Tests
{
    // Special-value conformance for Complex<T>, modeled on the C23 Annex G
    // (IEC 60559-compatible complex arithmetic) value tables. The same table is
    // shared across double/float/Half: every listed expected component is exactly
    // representable in all three, so the type-independent special-value handling
    // must reproduce it bit-for-bit, including the sign of zero.
    public static class ComplexGenericSpecialValueTests
    {
        private const double NaN = double.NaN;
        private const double PositiveInfinity = double.PositiveInfinity;
        private const double NegativeInfinity = double.NegativeInfinity;

        private static void AssertSame<T>(T actual, double expected, string context)
            where T : IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            if (double.IsNaN(expected))
            {
                Assert.True(T.IsNaN(actual), $"{context}: expected NaN, got {actual}");
                return;
            }

            T e = T.CreateTruncating(expected);
            Assert.True((actual == e) && (T.IsNegative(actual) == T.IsNegative(e)), $"{context}: expected {e}, got {actual}");
        }

        private static void Verify<T>(Func<Complex<T>, Complex<T>> op, string name, double real, double imaginary, double expectedReal, double expectedImaginary)
            where T : IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            Complex<T> actual = op(new Complex<T>(T.CreateTruncating(real), T.CreateTruncating(imaginary)));
            string context = $"{name}<{typeof(T).Name}>({real}, {imaginary}).";
            AssertSame(actual.Real, expectedReal, context + "Real");
            AssertSame(actual.Imaginary, expectedImaginary, context + "Imaginary");
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

    }
}
