// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Numerics
{
    /// <summary>
    /// A complex number z is a number of the form z = x + yi, where x and y
    /// are real numbers, and i is the imaginary unit, with the property i2= -1.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("System.Numerics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public readonly struct Complex
        : IEquatable<Complex>,
          IFormattable,
          INumberBase<Complex>,
          ISignedNumber<Complex>,
          IUtf8SpanFormattable
    {
        internal const NumberStyles DefaultNumberStyle = NumberStyles.Float | NumberStyles.AllowThousands;

        internal const NumberStyles InvalidNumberStyles = ~(NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite
                                                         | NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign
                                                         | NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint
                                                         | NumberStyles.AllowThousands | NumberStyles.AllowExponent
                                                         | NumberStyles.AllowCurrencySymbol | NumberStyles.AllowHexSpecifier);

        public static readonly Complex Zero = new(0.0, 0.0);
        public static readonly Complex One = new(1.0, 0.0);
        public static readonly Complex ImaginaryOne = new(0.0, 1.0);
        public static readonly Complex NaN = new(double.NaN, double.NaN);
        public static readonly Complex Infinity = new(double.PositiveInfinity, double.PositiveInfinity);

        private const double InverseOfLog10 = 0.43429448190325; // 1 / Log(10)

        // Do not rename, these fields are needed for binary serialization
        private readonly double m_real; // Do not rename (binary serialization)
        private readonly double m_imaginary; // Do not rename (binary serialization)

        public Complex(double real, double imaginary)
        {
            m_real = real;
            m_imaginary = imaginary;
        }

        public double Real { get { return m_real; } }
        public double Imaginary { get { return m_imaginary; } }

        public double Magnitude { get { return Abs(this); } }
        public double Phase { get { return Math.Atan2(m_imaginary, m_real); } }

        public static Complex FromPolarCoordinates(double magnitude, double phase)
        {
            (double sin, double cos) = Math.SinCos(phase);
            return new Complex(magnitude * cos, magnitude * sin);
        }

        public static Complex Negate(Complex value)
        {
            return -value;
        }

        public static Complex Add(Complex left, Complex right)
        {
            return left + right;
        }

        public static Complex Add(Complex left, double right)
        {
            return left + right;
        }

        public static Complex Add(double left, Complex right)
        {
            return left + right;
        }

        public static Complex Subtract(Complex left, Complex right)
        {
            return left - right;
        }

        public static Complex Subtract(Complex left, double right)
        {
            return left - right;
        }

        public static Complex Subtract(double left, Complex right)
        {
            return left - right;
        }

        public static Complex Multiply(Complex left, Complex right)
        {
            return left * right;
        }

        public static Complex Multiply(Complex left, double right)
        {
            return left * right;
        }

        public static Complex Multiply(double left, Complex right)
        {
            return left * right;
        }

        public static Complex Divide(Complex dividend, Complex divisor)
        {
            return dividend / divisor;
        }

        public static Complex Divide(Complex dividend, double divisor)
        {
            return dividend / divisor;
        }

        public static Complex Divide(double dividend, Complex divisor)
        {
            return dividend / divisor;
        }

        public static Complex operator -(Complex value)  /* Unary negation of a complex number */
        {
            return new Complex(-value.m_real, -value.m_imaginary);
        }

        public static Complex operator +(Complex left, Complex right)
        {
            return new Complex(left.m_real + right.m_real, left.m_imaginary + right.m_imaginary);
        }

        public static Complex operator +(Complex left, double right)
        {
            return new Complex(left.m_real + right, left.m_imaginary);
        }

        public static Complex operator +(double left, Complex right)
        {
            return new Complex(left + right.m_real, right.m_imaginary);
        }

        public static Complex operator -(Complex left, Complex right)
        {
            return new Complex(left.m_real - right.m_real, left.m_imaginary - right.m_imaginary);
        }

        public static Complex operator -(Complex left, double right)
        {
            return new Complex(left.m_real - right, left.m_imaginary);
        }

        public static Complex operator -(double left, Complex right)
        {
            return new Complex(left - right.m_real, -right.m_imaginary);
        }

        public static Complex operator *(Complex left, Complex right)
        {
            // Multiplication:  (a + bi)(c + di) = (ac -bd) + (bc + ad)i
            double result_realpart = (left.m_real * right.m_real) - (left.m_imaginary * right.m_imaginary);
            double result_imaginarypart = (left.m_imaginary * right.m_real) + (left.m_real * right.m_imaginary);
            return new Complex(result_realpart, result_imaginarypart);
        }

        public static Complex operator *(Complex left, double right)
        {
            Complex<double> result = new Complex<double>(left.m_real, left.m_imaginary) * right;
            return new Complex(result.Real, result.Imaginary);
        }

        public static Complex operator *(double left, Complex right)
        {
            Complex<double> result = left * new Complex<double>(right.m_real, right.m_imaginary);
            return new Complex(result.Real, result.Imaginary);
        }

        public static Complex operator /(Complex left, Complex right)
        {
            Complex<double> result = new Complex<double>(left.m_real, left.m_imaginary) / new Complex<double>(right.m_real, right.m_imaginary);
            return new Complex(result.Real, result.Imaginary);
        }

        public static Complex operator /(Complex left, double right)
        {
            Complex<double> result = new Complex<double>(left.m_real, left.m_imaginary) / right;
            return new Complex(result.Real, result.Imaginary);
        }

        public static Complex operator /(double left, Complex right)
        {
            Complex<double> result = left / new Complex<double>(right.m_real, right.m_imaginary);
            return new Complex(result.Real, result.Imaginary);
        }

        public static double Abs(Complex value)
        {
            return double.Hypot(value.m_real, value.m_imaginary);
        }

        public static Complex Conjugate(Complex value)
        {
            // Conjugate of a Complex number: the conjugate of x+i*y is x-i*y
            return new Complex(value.m_real, -value.m_imaginary);
        }

        public static Complex Reciprocal(Complex value)
        {
            // Reciprocal of a Complex number : the reciprocal of x+i*y is 1/(x+i*y)
            if (value.m_real == 0 && value.m_imaginary == 0)
            {
                return Zero;
            }
            return One / value;
        }

        public static bool operator ==(Complex left, Complex right)
        {
            return left.m_real == right.m_real && left.m_imaginary == right.m_imaginary;
        }

        public static bool operator !=(Complex left, Complex right)
        {
            return left.m_real != right.m_real || left.m_imaginary != right.m_imaginary;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Complex other && Equals(other);
        }

        public bool Equals(Complex value)
        {
            return m_real.Equals(value.m_real) && m_imaginary.Equals(value.m_imaginary);
        }

        public override int GetHashCode() => HashCode.Combine(m_real, m_imaginary);

        public override string ToString() => ToString(null, null);

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format) => ToString(format, null);

        public string ToString(IFormatProvider? provider) => ToString(null, provider);

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
            => new Complex<double>(m_real, m_imaginary).ToString(format, provider);

        public static Complex Sin(Complex value)
        {
            (double sin, double cos) = Math.SinCos(value.m_real);
            return new Complex(sin * Math.Cosh(value.m_imaginary), cos * Math.Sinh(value.m_imaginary));
            // There is a known limitation with this algorithm: inputs that cause sinh and cosh to overflow, but for
            // which sin or cos are small enough that sin * cosh or cos * sinh are still representable, nonetheless
            // produce overflow. For example, Sin((0.01, 711.0)) should produce (~3.0E306, PositiveInfinity), but
            // instead produces (PositiveInfinity, PositiveInfinity).
        }

        public static Complex Sinh(Complex value)
        {
            // Use sinh(z) = -i sin(iz) to compute via sin(z).
            Complex sin = Sin(new Complex(-value.m_imaginary, value.m_real));
            return new Complex(sin.m_imaginary, -sin.m_real);
        }

        public static Complex Asin(Complex value)
        {
            Complex<double> result = Complex<double>.Asin(new Complex<double>(value.m_real, value.m_imaginary));
            return new Complex(result.Real, result.Imaginary);
        }

        public static Complex Cos(Complex value)
        {
            (double sin, double cos) = Math.SinCos(value.m_real);
            return new Complex(cos * Math.Cosh(value.m_imaginary), -sin * Math.Sinh(value.m_imaginary));
        }

        public static Complex Cosh(Complex value)
        {
            // Use cosh(z) = cos(iz) to compute via cos(z).
            return Cos(new Complex(-value.m_imaginary, value.m_real));
        }

        public static Complex Acos(Complex value)
        {
            Complex<double> result = Complex<double>.Acos(new Complex<double>(value.m_real, value.m_imaginary));
            return new Complex(result.Real, result.Imaginary);
        }

        public static Complex Tan(Complex value)
        {
            Complex<double> result = Complex<double>.Tan(new Complex<double>(value.m_real, value.m_imaginary));
            return new Complex(result.Real, result.Imaginary);
        }

        public static Complex Tanh(Complex value)
        {
            // Use tanh(z) = -i tan(iz) to compute via tan(z).
            Complex tan = Tan(new Complex(-value.m_imaginary, value.m_real));
            return new Complex(tan.m_imaginary, -tan.m_real);
        }

        public static Complex Atan(Complex value)
        {
            Complex two = new(2.0, 0.0);
            return (ImaginaryOne / two) * (Log(One - ImaginaryOne * value) - Log(One + ImaginaryOne * value));
        }

        public static bool IsFinite(Complex value) => double.IsFinite(value.m_real) && double.IsFinite(value.m_imaginary);

        public static bool IsInfinity(Complex value) => double.IsInfinity(value.m_real) || double.IsInfinity(value.m_imaginary);

        public static bool IsNaN(Complex value) => !IsInfinity(value) && !IsFinite(value);

        public static Complex Log(Complex value)
        {
            return new Complex(Math.Log(Abs(value)), Math.Atan2(value.m_imaginary, value.m_real));
        }

        public static Complex Log(Complex value, double baseValue)
        {
            return Log(value) / Log(baseValue);
        }

        public static Complex Log10(Complex value)
        {
            Complex tempLog = Log(value);
            return Scale(tempLog, InverseOfLog10);
        }

        public static Complex Exp(Complex value)
        {
            double expReal = Math.Exp(value.m_real);
            return FromPolarCoordinates(expReal, value.m_imaginary);
        }

        public static Complex Sqrt(Complex value)
        {
            Complex<double> result = Complex<double>.Sqrt(new Complex<double>(value.m_real, value.m_imaginary));
            return new Complex(result.Real, result.Imaginary);
        }

        public static Complex Pow(Complex value, Complex power)
        {
            Complex<double> result = Complex<double>.Pow(new Complex<double>(value.m_real, value.m_imaginary), new Complex<double>(power.m_real, power.m_imaginary));
            return new Complex(result.Real, result.Imaginary);
        }

        public static Complex Pow(Complex value, double power)
        {
            return Pow(value, new Complex(power, 0));
        }

        private static Complex Scale(Complex value, double factor)
        {
            double realResult = factor * value.m_real;
            double imaginaryResuilt = factor * value.m_imaginary;
            return new Complex(realResult, imaginaryResuilt);
        }

        //
        // Explicit Conversions To Complex
        //

        public static explicit operator Complex(decimal value)
        {
            return new Complex((double)value, 0.0);
        }

        /// <summary>Explicitly converts a <see cref="Int128" /> value to a double-precision complex number.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a double-precision complex number.</returns>
        public static explicit operator Complex(Int128 value)
        {
            return new Complex((double)value, 0.0);
        }

        public static explicit operator Complex(BigInteger value)
        {
            return new Complex((double)value, 0.0);
        }

        /// <summary>Explicitly converts a <see cref="UInt128" /> value to a double-precision complex number.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a double-precision complex number.</returns>
        [CLSCompliant(false)]
        public static explicit operator Complex(UInt128 value)
        {
            return new Complex((double)value, 0.0);
        }

        //
        // Implicit Conversions To Complex
        //

        public static implicit operator Complex(byte value)
        {
            return new Complex(value, 0.0);
        }

        /// <summary>Implicitly converts a <see cref="char" /> value to a double-precision complex number.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a double-precision complex number.</returns>
        public static implicit operator Complex(char value)
        {
            return new Complex(value, 0.0);
        }

        public static implicit operator Complex(double value)
        {
            return new Complex(value, 0.0);
        }

        /// <summary>Implicitly converts a <see cref="Half" /> value to a double-precision complex number.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a double-precision complex number.</returns>
        public static implicit operator Complex(Half value)
        {
            return new Complex((double)value, 0.0);
        }

        /// <summary>Implicitly converts a <see cref="BFloat16" /> value to a double-precision complex number.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a double-precision complex number.</returns>
        public static implicit operator Complex(BFloat16 value)
        {
            return new Complex((double)value, 0.0);
        }

        public static implicit operator Complex(short value)
        {
            return new Complex(value, 0.0);
        }

        public static implicit operator Complex(int value)
        {
            return new Complex(value, 0.0);
        }

        public static implicit operator Complex(long value)
        {
            return new Complex(value, 0.0);
        }

        /// <summary>Implicitly converts a <see cref="IntPtr" /> value to a double-precision complex number.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a double-precision complex number.</returns>
        public static implicit operator Complex(nint value)
        {
            return new Complex(value, 0.0);
        }

        [CLSCompliant(false)]
        public static implicit operator Complex(sbyte value)
        {
            return new Complex(value, 0.0);
        }

        public static implicit operator Complex(float value)
        {
            return new Complex(value, 0.0);
        }

        [CLSCompliant(false)]
        public static implicit operator Complex(ushort value)
        {
            return new Complex(value, 0.0);
        }

        [CLSCompliant(false)]
        public static implicit operator Complex(uint value)
        {
            return new Complex(value, 0.0);
        }

        [CLSCompliant(false)]
        public static implicit operator Complex(ulong value)
        {
            return new Complex(value, 0.0);
        }

        /// <summary>Implicitly converts a <see cref="UIntPtr" /> value to a double-precision complex number.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a double-precision complex number.</returns>
        [CLSCompliant(false)]
        public static implicit operator Complex(nuint value)
        {
            return new Complex(value, 0.0);
        }

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static Complex IAdditiveIdentity<Complex, Complex>.AdditiveIdentity => new(0.0, 0.0);

        //
        // IDecrementOperators
        //

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        public static Complex operator --(Complex value) => value - One;

        //
        // IIncrementOperators
        //

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_Increment(TSelf)" />
        public static Complex operator ++(Complex value) => value + One;

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        static Complex IMultiplicativeIdentity<Complex, Complex>.MultiplicativeIdentity => new(1.0, 0.0);

        //
        // INumberBase
        //

        /// <inheritdoc cref="INumberBase{TSelf}.One" />
        static Complex INumberBase<Complex>.One => new(1.0, 0.0);

        /// <inheritdoc cref="INumberBase{TSelf}.Radix" />
        static int INumberBase<Complex>.Radix => 2;

        /// <inheritdoc cref="INumberBase{TSelf}.Zero" />
        static Complex INumberBase<Complex>.Zero => new(0.0, 0.0);

        /// <inheritdoc cref="INumberBase{TSelf}.Abs(TSelf)" />
        static Complex INumberBase<Complex>.Abs(Complex value) => Abs(value);

        /// <inheritdoc cref="INumberBase{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex CreateChecked<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            Complex result;

            if (typeof(TOther) == typeof(Complex))
            {
                result = (Complex)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToChecked(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex CreateSaturating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            Complex result;

            if (typeof(TOther) == typeof(Complex))
            {
                result = (Complex)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToSaturating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex CreateTruncating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            Complex result;

            if (typeof(TOther) == typeof(Complex))
            {
                result = (Complex)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToTruncating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsCanonical(TSelf)" />
        static bool INumberBase<Complex>.IsCanonical(Complex value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsComplexNumber(TSelf)" />
        public static bool IsComplexNumber(Complex value) => (value.m_real != 0.0) && (value.m_imaginary != 0.0);

        /// <inheritdoc cref="INumberBase{TSelf}.IsEvenInteger(TSelf)" />
        public static bool IsEvenInteger(Complex value) => (value.m_imaginary == 0) && double.IsEvenInteger(value.m_real);

        /// <inheritdoc cref="INumberBase{TSelf}.IsImaginaryNumber(TSelf)" />
        public static bool IsImaginaryNumber(Complex value) => (value.m_real == 0.0) && double.IsRealNumber(value.m_imaginary);

        /// <inheritdoc cref="INumberBase{TSelf}.IsInteger(TSelf)" />
        public static bool IsInteger(Complex value) => (value.m_imaginary == 0) && double.IsInteger(value.m_real);

        /// <inheritdoc cref="INumberBase{TSelf}.IsNegative(TSelf)" />
        public static bool IsNegative(Complex value)
        {
            // since complex numbers do not have a well-defined concept of
            // negative we report false if this value has an imaginary part

            return (value.m_imaginary == 0.0) && double.IsNegative(value.m_real);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsNegativeInfinity(TSelf)" />
        public static bool IsNegativeInfinity(Complex value)
        {
            // since complex numbers do not have a well-defined concept of
            // negative we report false if this value has an imaginary part

            return (value.m_imaginary == 0.0) && double.IsNegativeInfinity(value.m_real);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsNormal(TSelf)" />
        public static bool IsNormal(Complex value)
        {
            // much as IsFinite requires both part to be finite, we require both
            // part to be "normal" (finite, non-zero, and non-subnormal) to be true

            return double.IsNormal(value.m_real)
                && ((value.m_imaginary == 0.0) || double.IsNormal(value.m_imaginary));
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsOddInteger(TSelf)" />
        public static bool IsOddInteger(Complex value) => (value.m_imaginary == 0) && double.IsOddInteger(value.m_real);

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositive(TSelf)" />
        public static bool IsPositive(Complex value)
        {
            // since complex numbers do not have a well-defined concept of
            // negative we report false if this value has an imaginary part

            return (value.m_imaginary == 0.0) && double.IsPositive(value.m_real);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositiveInfinity(TSelf)" />
        public static bool IsPositiveInfinity(Complex value)
        {
            // since complex numbers do not have a well-defined concept of
            // positive we report false if this value has an imaginary part

            return (value.m_imaginary == 0.0) && double.IsPositiveInfinity(value.m_real);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsRealNumber(TSelf)" />
        public static bool IsRealNumber(Complex value) => (value.m_imaginary == 0.0) && double.IsRealNumber(value.m_real);

        /// <inheritdoc cref="INumberBase{TSelf}.IsSubnormal(TSelf)" />
        public static bool IsSubnormal(Complex value)
        {
            // much as IsInfinite allows either part to be infinite, we allow either
            // part to be "subnormal" (finite, non-zero, and non-normal) to be true

            return double.IsSubnormal(value.m_real) || double.IsSubnormal(value.m_imaginary);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsZero(TSelf)" />
        static bool INumberBase<Complex>.IsZero(Complex value) => (value.m_real == 0.0) && (value.m_imaginary == 0.0);

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        public static Complex MaxMagnitude(Complex x, Complex y)
        {
            Complex<double> result = Complex<double>.MaxMagnitude(new Complex<double>(x.m_real, x.m_imaginary), new Complex<double>(y.m_real, y.m_imaginary));
            return new Complex(result.Real, result.Imaginary);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitudeNumber(TSelf, TSelf)" />
        static Complex INumberBase<Complex>.MaxMagnitudeNumber(Complex x, Complex y)
        {
            Complex<double> result = Complex<double>.MaxMagnitudeNumber(new Complex<double>(x.m_real, x.m_imaginary), new Complex<double>(y.m_real, y.m_imaginary));
            return new Complex(result.Real, result.Imaginary);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitude(TSelf, TSelf)" />
        public static Complex MinMagnitude(Complex x, Complex y)
        {
            Complex<double> result = Complex<double>.MinMagnitude(new Complex<double>(x.m_real, x.m_imaginary), new Complex<double>(y.m_real, y.m_imaginary));
            return new Complex(result.Real, result.Imaginary);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitudeNumber(TSelf, TSelf)" />
        static Complex INumberBase<Complex>.MinMagnitudeNumber(Complex x, Complex y)
        {
            Complex<double> result = Complex<double>.MinMagnitudeNumber(new Complex<double>(x.m_real, x.m_imaginary), new Complex<double>(y.m_real, y.m_imaginary));
            return new Complex(result.Real, result.Imaginary);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.MultiplyAddEstimate(TSelf, TSelf, TSelf)" />
        static Complex INumberBase<Complex>.MultiplyAddEstimate(Complex left, Complex right, Complex addend)
        {
            Complex<double> result = Complex<double>.MultiplyAddEstimate(new Complex<double>(left.m_real, left.m_imaginary), new Complex<double>(right.m_real, right.m_imaginary), new Complex<double>(addend.m_real, addend.m_imaginary));
            return new Complex(result.Real, result.Imaginary);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.Parse(ReadOnlySpan{char}, NumberStyles, IFormatProvider?)" />
        public static Complex Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider)
        {
            if (!TryParse(s, style, provider, out Complex result))
            {
                ThrowHelper.ThrowOverflowException();
            }
            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.Parse(ReadOnlySpan{byte}, NumberStyles, IFormatProvider?)" />
        public static Complex Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider)
        {
            if (!TryParse(utf8Text, style, provider, out Complex result))
            {
                ThrowHelper.ThrowOverflowException();
            }
            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.Parse(string, NumberStyles, IFormatProvider?)" />
        public static Complex Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            ArgumentNullException.ThrowIfNull(s);
            return Parse(s.AsSpan(), style, provider);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromChecked{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Complex>.TryConvertFromChecked<TOther>(TOther value, out Complex result)
        {
            return TryConvertFrom<TOther>(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromSaturating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Complex>.TryConvertFromSaturating<TOther>(TOther value, out Complex result)
        {
            return TryConvertFrom<TOther>(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromTruncating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Complex>.TryConvertFromTruncating<TOther>(TOther value, out Complex result)
        {
            return TryConvertFrom<TOther>(value, out result);
        }

        private static bool TryConvertFrom<TOther>(TOther value, out Complex result)
            where TOther : INumberBase<TOther>
        {
            // We don't want to defer to `double.Create*(value)` because some type might have its own
            // `TOther.ConvertTo*(value, out Complex result)` handling that would end up bypassed.

            if (typeof(TOther) == typeof(byte))
            {
                byte actualValue = (byte)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualValue = (char)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualValue = (decimal)(object)value;
                result = (Complex)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                double actualValue = (double)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualValue = (Half)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(BFloat16))
            {
                BFloat16 actualValue = (BFloat16)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualValue = (short)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualValue = (int)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualValue = (long)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualValue = (Int128)(object)value;
                result = (Complex)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualValue = (nint)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualValue = (sbyte)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualValue = (float)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualValue = (ushort)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualValue = (uint)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualValue = (ulong)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualValue = (UInt128)(object)value;
                result = (Complex)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualValue = (nuint)(object)value;
                result = actualValue;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToChecked{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Complex>.TryConvertToChecked<TOther>(Complex value, [MaybeNullWhen(false)] out TOther result)
        {
            // Complex numbers with an imaginary part can't be represented as a "real number"
            // so we'll throw an OverflowException for this scenario for integer types and
            // for decimal. However, we will convert it to NaN for the floating-point types,
            // since that's what Sqrt(-1) (which is `new Complex(0, 1)`) results in.

            if (typeof(TOther) == typeof(byte))
            {
                if (value.m_imaginary != 0)
                {
                    ThrowHelper.ThrowOverflowException();
                }

                byte actualResult = checked((byte)value.m_real);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                if (value.m_imaginary != 0)
                {
                    ThrowHelper.ThrowOverflowException();
                }

                char actualResult = checked((char)value.m_real);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                if (value.m_imaginary != 0)
                {
                    ThrowHelper.ThrowOverflowException();
                }

                decimal actualResult = checked((decimal)value.m_real);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                double actualResult = (value.m_imaginary != 0) ? double.NaN : value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualResult = (value.m_imaginary != 0) ? Half.NaN : (Half)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(BFloat16))
            {
                BFloat16 actualResult = (value.m_imaginary != 0) ? BFloat16.NaN : (BFloat16)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                if (value.m_imaginary != 0)
                {
                    ThrowHelper.ThrowOverflowException();
                }

                short actualResult = checked((short)value.m_real);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                if (value.m_imaginary != 0)
                {
                    ThrowHelper.ThrowOverflowException();
                }

                int actualResult = checked((int)value.m_real);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                if (value.m_imaginary != 0)
                {
                    ThrowHelper.ThrowOverflowException();
                }

                long actualResult = checked((long)value.m_real);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                if (value.m_imaginary != 0)
                {
                    ThrowHelper.ThrowOverflowException();
                }

                Int128 actualResult = checked((Int128)value.m_real);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                if (value.m_imaginary != 0)
                {
                    ThrowHelper.ThrowOverflowException();
                }

                nint actualResult = checked((nint)value.m_real);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(BigInteger))
            {
                if (value.m_imaginary != 0)
                {
                    ThrowHelper.ThrowOverflowException();
                }

                BigInteger actualResult = checked((BigInteger)value.m_real);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                if (value.m_imaginary != 0)
                {
                    ThrowHelper.ThrowOverflowException();
                }

                sbyte actualResult = checked((sbyte)value.m_real);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualResult = (value.m_imaginary != 0) ? float.NaN : (float)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                if (value.m_imaginary != 0)
                {
                    ThrowHelper.ThrowOverflowException();
                }

                ushort actualResult = checked((ushort)value.m_real);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                if (value.m_imaginary != 0)
                {
                    ThrowHelper.ThrowOverflowException();
                }

                uint actualResult = checked((uint)value.m_real);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                if (value.m_imaginary != 0)
                {
                    ThrowHelper.ThrowOverflowException();
                }

                ulong actualResult = checked((ulong)value.m_real);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                if (value.m_imaginary != 0)
                {
                    ThrowHelper.ThrowOverflowException();
                }

                UInt128 actualResult = checked((UInt128)value.m_real);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                if (value.m_imaginary != 0)
                {
                    ThrowHelper.ThrowOverflowException();
                }

                nuint actualResult = checked((nuint)value.m_real);
                result = (TOther)(object)actualResult;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToSaturating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Complex>.TryConvertToSaturating<TOther>(Complex value, [MaybeNullWhen(false)] out TOther result)
        {
            // Complex numbers with an imaginary part can't be represented as a "real number"
            // and there isn't really a well-defined way to "saturate" to just a real value.
            //
            // The two potential options are that we either treat complex numbers with a non-
            // zero imaginary part as NaN and then convert that to 0 -or- we ignore the imaginary
            // part and only consider the real part.
            //
            // We use the latter below since that is "more useful" given an unknown number type.
            // Users who want 0 instead can always check `IsComplexNumber` and special-case the
            // handling.

            if (typeof(TOther) == typeof(byte))
            {
                byte actualResult = (value.m_real >= byte.MaxValue) ? byte.MaxValue :
                                    (value.m_real <= byte.MinValue) ? byte.MinValue : (byte)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualResult = (value.m_real >= char.MaxValue) ? char.MaxValue :
                                    (value.m_real <= char.MinValue) ? char.MinValue : (char)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualResult = (value.m_real >= (double)decimal.MaxValue) ? decimal.MaxValue :
                                       (value.m_real <= (double)decimal.MinValue) ? decimal.MinValue : (decimal)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                double actualResult = value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualResult = (Half)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(BFloat16))
            {
                BFloat16 actualResult = (BFloat16)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualResult = (value.m_real >= short.MaxValue) ? short.MaxValue :
                                     (value.m_real <= short.MinValue) ? short.MinValue : (short)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualResult = (value.m_real >= int.MaxValue) ? int.MaxValue :
                                   (value.m_real <= int.MinValue) ? int.MinValue : (int)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualResult = (value.m_real >= long.MaxValue) ? long.MaxValue :
                                    (value.m_real <= long.MinValue) ? long.MinValue : (long)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualResult = (value.m_real >= +170141183460469231731687303715884105727.0) ? Int128.MaxValue :
                                      (value.m_real <= -170141183460469231731687303715884105728.0) ? Int128.MinValue : (Int128)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualResult = (value.m_real >= nint.MaxValue) ? nint.MaxValue :
                                    (value.m_real <= nint.MinValue) ? nint.MinValue : (nint)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(BigInteger))
            {
                BigInteger actualResult = (BigInteger)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualResult = (value.m_real >= sbyte.MaxValue) ? sbyte.MaxValue :
                                     (value.m_real <= sbyte.MinValue) ? sbyte.MinValue : (sbyte)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualResult = (float)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualResult = (value.m_real >= ushort.MaxValue) ? ushort.MaxValue :
                                      (value.m_real <= ushort.MinValue) ? ushort.MinValue : (ushort)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualResult = (value.m_real >= uint.MaxValue) ? uint.MaxValue :
                                    (value.m_real <= uint.MinValue) ? uint.MinValue : (uint)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualResult = (value.m_real >= ulong.MaxValue) ? ulong.MaxValue :
                                     (value.m_real <= ulong.MinValue) ? ulong.MinValue : (ulong)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualResult = (value.m_real >= 340282366920938463463374607431768211455.0) ? UInt128.MaxValue :
                                       (value.m_real <= 0.0) ? UInt128.MinValue : (UInt128)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualResult = (value.m_real >= nuint.MaxValue) ? nuint.MaxValue :
                                     (value.m_real <= nuint.MinValue) ? nuint.MinValue : (nuint)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToTruncating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Complex>.TryConvertToTruncating<TOther>(Complex value, [MaybeNullWhen(false)] out TOther result)
        {
            // Complex numbers with an imaginary part can't be represented as a "real number"
            // so we'll only consider the real part for the purposes of truncation.

            if (typeof(TOther) == typeof(byte))
            {
                byte actualResult = (value.m_real >= byte.MaxValue) ? byte.MaxValue :
                                    (value.m_real <= byte.MinValue) ? byte.MinValue : (byte)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualResult = (value.m_real >= char.MaxValue) ? char.MaxValue :
                                    (value.m_real <= char.MinValue) ? char.MinValue : (char)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualResult = (value.m_real >= (double)decimal.MaxValue) ? decimal.MaxValue :
                                       (value.m_real <= (double)decimal.MinValue) ? decimal.MinValue : (decimal)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                double actualResult = value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualResult = (Half)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(BFloat16))
            {
                BFloat16 actualResult = (BFloat16)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualResult = (value.m_real >= short.MaxValue) ? short.MaxValue :
                                     (value.m_real <= short.MinValue) ? short.MinValue : (short)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualResult = (value.m_real >= int.MaxValue) ? int.MaxValue :
                                   (value.m_real <= int.MinValue) ? int.MinValue : (int)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualResult = (value.m_real >= long.MaxValue) ? long.MaxValue :
                                    (value.m_real <= long.MinValue) ? long.MinValue : (long)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualResult = (value.m_real >= +170141183460469231731687303715884105727.0) ? Int128.MaxValue :
                                      (value.m_real <= -170141183460469231731687303715884105728.0) ? Int128.MinValue : (Int128)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualResult = (value.m_real >= nint.MaxValue) ? nint.MaxValue :
                                    (value.m_real <= nint.MinValue) ? nint.MinValue : (nint)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(BigInteger))
            {
                BigInteger actualResult = (BigInteger)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualResult = (value.m_real >= sbyte.MaxValue) ? sbyte.MaxValue :
                                     (value.m_real <= sbyte.MinValue) ? sbyte.MinValue : (sbyte)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualResult = (float)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualResult = (value.m_real >= ushort.MaxValue) ? ushort.MaxValue :
                                      (value.m_real <= ushort.MinValue) ? ushort.MinValue : (ushort)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualResult = (value.m_real >= uint.MaxValue) ? uint.MaxValue :
                                    (value.m_real <= uint.MinValue) ? uint.MinValue : (uint)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualResult = (value.m_real >= ulong.MaxValue) ? ulong.MaxValue :
                                     (value.m_real <= ulong.MinValue) ? ulong.MinValue : (ulong)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualResult = (value.m_real >= 340282366920938463463374607431768211455.0) ? UInt128.MaxValue :
                                       (value.m_real <= 0.0) ? UInt128.MinValue : (UInt128)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualResult = (value.m_real >= nuint.MaxValue) ? nuint.MaxValue :
                                     (value.m_real <= nuint.MinValue) ? nuint.MinValue : (nuint)value.m_real;
                result = (TOther)(object)actualResult;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryParse(ReadOnlySpan{char}, NumberStyles, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Complex result)
            => TryParse(MemoryMarshal.Cast<char, Utf16Char>(s), style, provider, out result);

        /// <inheritdoc cref="INumberBase{TSelf}.TryParse(ReadOnlySpan{byte}, NumberStyles, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out Complex result)
            => TryParse(MemoryMarshal.Cast<byte, Utf8Char>(utf8Text), style, provider, out result);

        private static bool TryParse<TChar>(ReadOnlySpan<TChar> text, NumberStyles style, IFormatProvider? provider, out Complex result)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            ValidateParseStyleFloatingPoint(style);

            int openBracket = text.IndexOf(TChar.CastFrom('<'));
            int semicolon = text.IndexOf(TChar.CastFrom(';'));
            int closeBracket = text.IndexOf(TChar.CastFrom('>'));

            if ((text.Length < 5) || (openBracket == -1) || (semicolon == -1) || (closeBracket == -1) || (openBracket > semicolon) || (openBracket > closeBracket) || (semicolon > closeBracket))
            {
                // We need at least 5 characters for `<0;0>`
                // We also expect a to find an open bracket, a semicolon, and a closing bracket in that order

                result = default;
                return false;
            }

            if ((openBracket != 0) && (((style & NumberStyles.AllowLeadingWhite) == 0) || !text.Slice(0, openBracket).IsWhiteSpace()))
            {
                // The opening bracket wasn't the first and we either didn't allow leading whitespace
                // or one of the leading characters wasn't whitespace at all.

                result = default;
                return false;
            }

            ReadOnlySpan<TChar> slice = text.Slice(openBracket + 1, semicolon - openBracket - 1);

            if ((typeof(TChar) == typeof(Utf8Char))
                ? !double.TryParse(Unsafe.BitCast<ReadOnlySpan<TChar>, ReadOnlySpan<byte>>(slice), style, provider, out double real)
                : !double.TryParse(Unsafe.BitCast<ReadOnlySpan<TChar>, ReadOnlySpan<char>>(slice), style, provider, out real))
            {
                result = default;
                return false;
            }

            if (Number.DecodeFromUtfChar(text[(semicolon + 1)..], out Rune rune, out int elemsConsumed) == OperationStatus.Done)
            {
                if (Rune.IsWhiteSpace(rune))
                {
                    // We allow a single whitespace after the semicolon regardless of style, this is so that
                    // the output of `ToString` can be correctly parsed by default and values will roundtrip.
                    semicolon += elemsConsumed;
                }
            }

            slice = text.Slice(semicolon + 1, closeBracket - semicolon - 1);

            if ((typeof(TChar) == typeof(Utf8Char))
                ? !double.TryParse(Unsafe.BitCast<ReadOnlySpan<TChar>, ReadOnlySpan<byte>>(slice), style, provider, out double imaginary)
                : !double.TryParse(Unsafe.BitCast<ReadOnlySpan<TChar>, ReadOnlySpan<char>>(slice), style, provider, out imaginary))
            {
                result = default;
                return false;
            }

            if ((closeBracket != (text.Length - 1)) && (((style & NumberStyles.AllowTrailingWhite) == 0) || !text.Slice(closeBracket).IsWhiteSpace()))
            {
                // The closing bracket wasn't the last and we either didn't allow trailing whitespace
                // or one of the trailing characters wasn't whitespace at all.

                result = default;
                return false;
            }

            result = new Complex(real, imaginary);
            return true;
        }

        private static void ValidateParseStyleFloatingPoint(NumberStyles style)
        {
            // Check for undefined flags or hex number
            if ((style & (InvalidNumberStyles | NumberStyles.AllowHexSpecifier)) != 0)
            {
                ThrowInvalid(style);

                static void ThrowInvalid(NumberStyles value)
                {
                    if ((value & InvalidNumberStyles) != 0)
                    {
                        throw new ArgumentException(SR.Argument_InvalidNumberStyles, nameof(style));
                    }

                    throw new ArgumentException(SR.Arg_HexStyleNotSupported);
                }
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryParse(string, NumberStyles, IFormatProvider?, out TSelf)" />
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out Complex result)
        {
            if (s is null)
            {
                result = default;
                return false;
            }
            return TryParse(s.AsSpan(), style, provider, out result);
        }

        //
        // IParsable
        //

        /// <inheritdoc cref="IParsable{TSelf}.Parse(string, IFormatProvider?)" />
        public static Complex Parse(string s, IFormatProvider? provider) => Parse(s, DefaultNumberStyle, provider);

        /// <inheritdoc cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)" />
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Complex result) => TryParse(s, DefaultNumberStyle, provider, out result);

        //
        // ISignedNumber
        //

        /// <inheritdoc cref="ISignedNumber{TSelf}.NegativeOne" />
        static Complex ISignedNumber<Complex>.NegativeOne => new(-1.0, 0.0);

        //
        // ISpanFormattable
        //

        /// <inheritdoc cref="ISpanFormattable.TryFormat(Span{char}, out int, ReadOnlySpan{char}, IFormatProvider?)" />
        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null) =>
            TryFormat(MemoryMarshal.Cast<char, Utf16Char>(destination), out charsWritten, format, provider);

        /// <inheritdoc cref="IUtf8SpanFormattable.TryFormat(Span{byte}, out int, ReadOnlySpan{char}, IFormatProvider?)" />
        public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null) =>
            TryFormat(MemoryMarshal.Cast<byte, Utf8Char>(utf8Destination), out bytesWritten, format, provider);

        private bool TryFormat<TChar>(Span<TChar> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(Utf8Char) || typeof(TChar) == typeof(Utf16Char));

            // We have at least 6 more characters for: <0; 0>
            if (destination.Length >= 6)
            {
                if ((typeof(TChar) == typeof(Utf8Char))
                    ? m_real.TryFormat(Unsafe.BitCast<Span<TChar>, Span<byte>>(destination.Slice(1)), out int realChars, format, provider)
                    : m_real.TryFormat(Unsafe.BitCast<Span<TChar>, Span<char>>(destination.Slice(1)), out realChars, format, provider))
                {
                    destination[0] = TChar.CastFrom('<');
                    destination = destination.Slice(1 + realChars); // + 1 for <

                    // We have at least 4 more characters for: ; 0>
                    if (destination.Length >= 4)
                    {
                        if ((typeof(TChar) == typeof(Utf8Char))
                            ? m_imaginary.TryFormat(Unsafe.BitCast<Span<TChar>, Span<byte>>(destination.Slice(2)), out int imaginaryChars, format, provider)
                            : m_imaginary.TryFormat(Unsafe.BitCast<Span<TChar>, Span<char>>(destination.Slice(2)), out imaginaryChars, format, provider))
                        {
                            // We have 1 more character for: >
                            if ((uint)(2 + imaginaryChars) < (uint)destination.Length)
                            {
                                destination[0] = TChar.CastFrom(';');
                                destination[1] = TChar.CastFrom(' ');
                                destination[2 + imaginaryChars] = TChar.CastFrom('>');

                                charsWritten = realChars + imaginaryChars + 4;
                                return true;
                            }
                        }
                    }
                }
            }

            charsWritten = 0;
            return false;
        }

        //
        // ISpanParsable
        //

        /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static Complex Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, DefaultNumberStyle, provider);

        /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Complex result) => TryParse(s, DefaultNumberStyle, provider, out result);

        //
        // IUnaryPlusOperators
        //

        /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_UnaryPlus(TSelf)" />
        public static Complex operator +(Complex value) => value;

        //
        // IUtf8SpanParsable
        //

        /// <inheritdoc cref="IUtf8SpanParsable{TSelf}.Parse(ReadOnlySpan{byte}, IFormatProvider?)" />
        public static Complex Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) => Parse(utf8Text, DefaultNumberStyle, provider);

        /// <inheritdoc cref="IUtf8SpanParsable{TSelf}.TryParse(ReadOnlySpan{byte}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out Complex result) => TryParse(utf8Text, DefaultNumberStyle, provider, out result);
    }
}
