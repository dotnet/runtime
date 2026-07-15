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
    /// A generic complex number z = x + yi, where x and y are of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The floating-point type used for the real and imaginary components.</typeparam>
    public readonly struct Complex<T>
        : IEquatable<Complex<T>>,
          IFormattable,
          INumberBase<Complex<T>>,
          ISignedNumber<Complex<T>>,
          IUtf8SpanFormattable
        where T : IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        public static Complex<T> Zero => new(T.Zero, T.Zero);
        public static Complex<T> One => new(T.One, T.Zero);
        public static Complex<T> ImaginaryOne => new(T.Zero, T.One);
        public static Complex<T> NaN => new(T.NaN, T.NaN);
        public static Complex<T> Infinity => new(T.PositiveInfinity, T.PositiveInfinity);

        // 1 / Log(10)
        private static readonly T s_inverseOfLog10 = T.One / T.Log(T.CreateChecked(10));

        // This is the largest x for which (Hypot(x,x) + x) will not overflow. It is used for branching inside Sqrt.
        private static readonly T s_sqrtRescaleThreshold = T.MaxValue / (T.Sqrt(T.CreateChecked(2)) + T.One);

        // This is the largest x for which 2 x^2 will not overflow. It is used for branching inside Asin and Acos.
        private static readonly T s_asinOverflowThreshold = T.Sqrt(T.MaxValue) / T.CreateChecked(2);

        // This value is used inside Asin and Acos.
        private static readonly T s_log2 = T.Log(T.CreateChecked(2));

        private readonly T m_real;
        private readonly T m_imaginary;

        public Complex(T real, T imaginary)
        {
            m_real = real;
            m_imaginary = imaginary;
        }

        public T Real => m_real;
        public T Imaginary => m_imaginary;

        public T GetMagnitude() => Abs(this);
        public T GetPhase() => T.Atan2(m_imaginary, m_real);

        public static Complex<T> FromPolarCoordinates(T magnitude, T phase)
        {
            (T sin, T cos) = T.SinCos(phase);
            return new Complex<T>(magnitude * cos, magnitude * sin);
        }

        public static Complex<T> Negate(Complex<T> value)
        {
            return -value;
        }

        public static Complex<T> Add(Complex<T> left, Complex<T> right)
        {
            return left + right;
        }

        public static Complex<T> Add(Complex<T> left, T right)
        {
            return left + right;
        }

        public static Complex<T> Add(T left, Complex<T> right)
        {
            return left + right;
        }

        public static Complex<T> Subtract(Complex<T> left, Complex<T> right)
        {
            return left - right;
        }

        public static Complex<T> Subtract(Complex<T> left, T right)
        {
            return left - right;
        }

        public static Complex<T> Subtract(T left, Complex<T> right)
        {
            return left - right;
        }

        public static Complex<T> Multiply(Complex<T> left, Complex<T> right)
        {
            return left * right;
        }

        public static Complex<T> Multiply(Complex<T> left, T right)
        {
            return left * right;
        }

        public static Complex<T> Multiply(T left, Complex<T> right)
        {
            return left * right;
        }

        public static Complex<T> Divide(Complex<T> dividend, Complex<T> divisor)
        {
            return dividend / divisor;
        }

        public static Complex<T> Divide(Complex<T> dividend, T divisor)
        {
            return dividend / divisor;
        }

        public static Complex<T> Divide(T dividend, Complex<T> divisor)
        {
            return dividend / divisor;
        }

        public static Complex<T> operator -(Complex<T> value)
        {
            return new Complex<T>(-value.m_real, -value.m_imaginary);
        }

        public static Complex<T> operator +(Complex<T> left, Complex<T> right)
        {
            return new Complex<T>(left.m_real + right.m_real, left.m_imaginary + right.m_imaginary);
        }

        public static Complex<T> operator +(Complex<T> left, T right)
        {
            return new Complex<T>(left.m_real + right, left.m_imaginary);
        }

        public static Complex<T> operator +(T left, Complex<T> right)
        {
            return new Complex<T>(left + right.m_real, right.m_imaginary);
        }

        public static Complex<T> operator -(Complex<T> left, Complex<T> right)
        {
            return new Complex<T>(left.m_real - right.m_real, left.m_imaginary - right.m_imaginary);
        }

        public static Complex<T> operator -(Complex<T> left, T right)
        {
            return new Complex<T>(left.m_real - right, left.m_imaginary);
        }

        public static Complex<T> operator -(T left, Complex<T> right)
        {
            return new Complex<T>(left - right.m_real, -right.m_imaginary);
        }

        public static Complex<T> operator *(Complex<T> left, Complex<T> right)
        {
            // Multiplication:  (a + bi)(c + di) = (ac - bd) + (bc + ad)i
            T result_realpart = (left.m_real * right.m_real) - (left.m_imaginary * right.m_imaginary);
            T result_imaginarypart = (left.m_imaginary * right.m_real) + (left.m_real * right.m_imaginary);
            return new Complex<T>(result_realpart, result_imaginarypart);
        }

        public static Complex<T> operator *(Complex<T> left, T right)
        {
            if (!T.IsFinite(left.m_real))
            {
                if (!T.IsFinite(left.m_imaginary))
                {
                    return new Complex<T>(T.NaN, T.NaN);
                }

                return new Complex<T>(left.m_real * right, T.NaN);
            }

            if (!T.IsFinite(left.m_imaginary))
            {
                return new Complex<T>(T.NaN, left.m_imaginary * right);
            }

            return new Complex<T>(left.m_real * right, left.m_imaginary * right);
        }

        public static Complex<T> operator *(T left, Complex<T> right)
        {
            if (!T.IsFinite(right.m_real))
            {
                if (!T.IsFinite(right.m_imaginary))
                {
                    return new Complex<T>(T.NaN, T.NaN);
                }

                return new Complex<T>(left * right.m_real, T.NaN);
            }

            if (!T.IsFinite(right.m_imaginary))
            {
                return new Complex<T>(T.NaN, left * right.m_imaginary);
            }

            return new Complex<T>(left * right.m_real, left * right.m_imaginary);
        }

        public static Complex<T> operator /(Complex<T> left, Complex<T> right)
        {
            // Division : Smith's formula.
            T a = left.m_real;
            T b = left.m_imaginary;
            T c = right.m_real;
            T d = right.m_imaginary;

            // Computing c * c + d * d will overflow even in cases where the actual result of the division does not overflow.
            if (T.Abs(d) < T.Abs(c))
            {
                T doc = d / c;
                return new Complex<T>((a + b * doc) / (c + d * doc), (b - a * doc) / (c + d * doc));
            }
            else
            {
                T cod = c / d;
                return new Complex<T>((b + a * cod) / (d + c * cod), (-a + b * cod) / (d + c * cod));
            }
        }

        public static Complex<T> operator /(Complex<T> left, T right)
        {
            // IEEE prohibit optimizations which are value changing
            // so we make sure that behaviour for the simplified version exactly match
            // full version.
            if (right == T.Zero)
            {
                return new Complex<T>(T.NaN, T.NaN);
            }

            if (!T.IsFinite(left.m_real))
            {
                if (!T.IsFinite(left.m_imaginary))
                {
                    return new Complex<T>(T.NaN, T.NaN);
                }

                return new Complex<T>(left.m_real / right, T.NaN);
            }

            if (!T.IsFinite(left.m_imaginary))
            {
                return new Complex<T>(T.NaN, left.m_imaginary / right);
            }

            // Here the actual optimized version of code.
            return new Complex<T>(left.m_real / right, left.m_imaginary / right);
        }

        public static Complex<T> operator /(T left, Complex<T> right)
        {
            // Division : Smith's formula.
            T a = left;
            T c = right.m_real;
            T d = right.m_imaginary;

            // Computing c * c + d * d will overflow even in cases where the actual result of the division does not overflow.
            if (T.Abs(d) < T.Abs(c))
            {
                T doc = d / c;
                return new Complex<T>(a / (c + d * doc), (-a * doc) / (c + d * doc));
            }
            else
            {
                T cod = c / d;
                return new Complex<T>(a * cod / (d + c * cod), -a / (d + c * cod));
            }
        }

        public static T Abs(Complex<T> value)
        {
            return T.Hypot(value.m_real, value.m_imaginary);
        }

        private static T Log1P(T x)
        {
            // Compute log(1 + x) without loss of accuracy when x is small.

            // Our only use case so far is for positive values, so this isn't coded to handle negative values.
            Debug.Assert((x >= T.Zero) || T.IsNaN(x));

            T xp1 = T.One + x;
            if (xp1 == T.One)
            {
                return x;
            }
            else if (x < T.CreateChecked(0.75))
            {
                // This is accurate to within 5 ulp with any floating-point system that uses a guard digit,
                // as proven in Theorem 4 of "What Every Computer Scientist Should Know About Floating-Point
                // Arithmetic" (https://docs.oracle.com/cd/E19957-01/806-3568/ncg_goldberg.html)
                return x * T.Log(xp1) / (xp1 - T.One);
            }
            else
            {
                return T.Log(xp1);
            }
        }

        public static Complex<T> Conjugate(Complex<T> value)
        {
            // Conjugate of a Complex number: the conjugate of x+i*y is x-i*y
            return new Complex<T>(value.m_real, -value.m_imaginary);
        }

        public static Complex<T> Reciprocal(Complex<T> value)
        {
            // Reciprocal of a Complex number : the reciprocal of x+i*y is 1/(x+i*y)
            if (value.m_real == T.Zero && value.m_imaginary == T.Zero)
            {
                return Zero;
            }
            return One / value;
        }

        public static bool operator ==(Complex<T> left, Complex<T> right)
        {
            return left.m_real == right.m_real && left.m_imaginary == right.m_imaginary;
        }

        public static bool operator !=(Complex<T> left, Complex<T> right)
        {
            return left.m_real != right.m_real || left.m_imaginary != right.m_imaginary;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Complex<T> other && Equals(other);
        }

        public bool Equals(Complex<T> value)
        {
            return m_real.Equals(value.m_real) && m_imaginary.Equals(value.m_imaginary);
        }

        public override int GetHashCode() => HashCode.Combine(m_real, m_imaginary);

        public override string ToString() => ToString(null, null);

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format) => ToString(format, null);

        public string ToString(IFormatProvider? provider) => ToString(null, provider);

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        {
            // $"<{m_real.ToString(format, provider)}; {m_imaginary.ToString(format, provider)}>";
            var handler = new DefaultInterpolatedStringHandler(4, 2, provider, stackalloc char[512]);
            handler.AppendLiteral("<");
            handler.AppendFormatted(m_real, format);
            handler.AppendLiteral("; ");
            handler.AppendFormatted(m_imaginary, format);
            handler.AppendLiteral(">");
            return handler.ToStringAndClear();
        }

        public static Complex<T> Sin(Complex<T> value)
        {
            (T sin, T cos) = T.SinCos(value.m_real);
            return new Complex<T>(sin * T.Cosh(value.m_imaginary), cos * T.Sinh(value.m_imaginary));
        }

        public static Complex<T> Sinh(Complex<T> value)
        {
            // Use sinh(z) = -i sin(iz) to compute via sin(z).
            Complex<T> sin = Sin(new Complex<T>(-value.m_imaginary, value.m_real));
            return new Complex<T>(sin.m_imaginary, -sin.m_real);
        }

        public static Complex<T> Asin(Complex<T> value)
        {
            Asin_Internal(T.Abs(value.Real), T.Abs(value.Imaginary), out T b, out T bPrime, out T v);

            T u;
            if (bPrime < T.Zero)
            {
                u = T.Asin(b);
            }
            else
            {
                u = T.Atan(bPrime);
            }

            if (value.Real < T.Zero) u = -u;
            if (value.Imaginary < T.Zero) v = -v;

            return new Complex<T>(u, v);
        }

        public static Complex<T> Cos(Complex<T> value)
        {
            (T sin, T cos) = T.SinCos(value.m_real);
            return new Complex<T>(cos * T.Cosh(value.m_imaginary), -sin * T.Sinh(value.m_imaginary));
        }

        public static Complex<T> Cosh(Complex<T> value)
        {
            // Use cosh(z) = cos(iz) to compute via cos(z).
            return Cos(new Complex<T>(-value.m_imaginary, value.m_real));
        }

        public static Complex<T> Acos(Complex<T> value)
        {
            Asin_Internal(T.Abs(value.Real), T.Abs(value.Imaginary), out T b, out T bPrime, out T v);

            T u;
            if (bPrime < T.Zero)
            {
                u = T.Acos(b);
            }
            else
            {
                u = T.Atan(T.One / bPrime);
            }

            if (value.Real < T.Zero) u = T.Pi - u;
            if (value.Imaginary > T.Zero) v = -v;

            return new Complex<T>(u, v);
        }

        public static Complex<T> Tan(Complex<T> value)
        {
            // tan z = sin z / cos z, but to avoid unnecessary repeated trig computations, use
            //   tan z = (sin(2x) + i sinh(2y)) / (cos(2x) + cosh(2y))
            // (see Abramowitz & Stegun 4.3.57 or derive by hand), and compute trig functions here.

            // This approach does not work for |y| > ~355, because sinh(2y) and cosh(2y) overflow,
            // even though their ratio does not. In that case, divide through by cosh to get:
            //   tan z = (sin(2x) / cosh(2y) + i \tanh(2y)) / (1 + cos(2x) / cosh(2y))
            // which correctly computes the (tiny) real part and the (normal-sized) imaginary part.

            T two = T.CreateChecked(2);
            T x2 = two * value.m_real;
            T y2 = two * value.m_imaginary;
            (T sin, T cos) = T.SinCos(x2);
            T cosh = T.Cosh(y2);
            if (T.Abs(value.m_imaginary) <= T.CreateChecked(4))
            {
                T D = cos + cosh;
                return new Complex<T>(sin / D, T.Sinh(y2) / D);
            }
            else
            {
                T D = T.One + cos / cosh;
                return new Complex<T>(sin / cosh / D, T.Tanh(y2) / D);
            }
        }

        public static Complex<T> Tanh(Complex<T> value)
        {
            // Use tanh(z) = -i tan(iz) to compute via tan(z).
            Complex<T> tan = Tan(new Complex<T>(-value.m_imaginary, value.m_real));
            return new Complex<T>(tan.m_imaginary, -tan.m_real);
        }

        public static Complex<T> Atan(Complex<T> value)
        {
            Complex<T> two = new(T.CreateChecked(2), T.Zero);
            return (ImaginaryOne / two) * (Log(One - ImaginaryOne * value) - Log(One + ImaginaryOne * value));
        }

        private static void Asin_Internal(T x, T y, out T b, out T bPrime, out T v)
        {
            // This method for the inverse complex sine (and cosine) is described in Hull, Fairgrieve,
            // and Tang, "Implementing the Complex Arcsine and Arccosine Functions Using Exception Handling",
            // ACM Transactions on Mathematical Software (1997)
            // (https://www.researchgate.net/profile/Ping_Tang3/publication/220493330_Implementing_the_Complex_Arcsine_and_Arccosine_Functions_Using_Exception_Handling/links/55b244b208ae9289a085245d.pdf)

            Debug.Assert((x >= T.Zero) || T.IsNaN(x));
            Debug.Assert((y >= T.Zero) || T.IsNaN(y));

            if ((x > s_asinOverflowThreshold) || (y > s_asinOverflowThreshold))
            {
                b = -T.One;
                bPrime = x / y;

                T small, big;
                if (x < y)
                {
                    small = x;
                    big = y;
                }
                else
                {
                    small = y;
                    big = x;
                }
                T ratio = small / big;
                v = s_log2 + T.Log(big) + Log1P(ratio * ratio) / T.CreateChecked(2);
            }
            else
            {
                T r = T.Hypot(x + T.One, y);
                T s = T.Hypot(x - T.One, y);

                T a = (r + s) / T.CreateChecked(2);
                b = x / a;

                if (b > T.CreateChecked(0.75))
                {
                    if (x <= T.One)
                    {
                        T amx = (y * y / (r + (x + T.One)) + (s + (T.One - x))) / T.CreateChecked(2);
                        bPrime = x / T.Sqrt((a + x) * amx);
                    }
                    else
                    {
                        // In this case, amx ~ y^2. Since we take the square root of amx, we should
                        // pull y out from under the square root so we don't lose its contribution
                        // when y^2 underflows.
                        T t = (T.One / (r + (x + T.One)) + T.One / (s + (x - T.One))) / T.CreateChecked(2);
                        bPrime = x / y / T.Sqrt((a + x) * t);
                    }
                }
                else
                {
                    bPrime = -T.One;
                }

                if (a < T.CreateChecked(1.5))
                {
                    if (x < T.One)
                    {
                        // This is another case where our expression is proportional to y^2 and
                        // we take its square root, so again we pull out a factor of y from
                        // under the square root.
                        T t = (T.One / (r + (x + T.One)) + T.One / (s + (T.One - x))) / T.CreateChecked(2);
                        T am1 = y * y * t;
                        v = Log1P(am1 + y * T.Sqrt(t * (a + T.One)));
                    }
                    else
                    {
                        T am1 = (y * y / (r + (x + T.One)) + (s + (x - T.One))) / T.CreateChecked(2);
                        v = Log1P(am1 + T.Sqrt(am1 * (a + T.One)));
                    }
                }
                else
                {
                    // Because of the test above, we can be sure that a * a will not overflow.
                    v = T.Log(a + T.Sqrt((a - T.One) * (a + T.One)));
                }
            }
        }

        public static bool IsFinite(Complex<T> value) => T.IsFinite(value.m_real) && T.IsFinite(value.m_imaginary);

        public static bool IsInfinity(Complex<T> value) => T.IsInfinity(value.m_real) || T.IsInfinity(value.m_imaginary);

        public static bool IsNaN(Complex<T> value) => !IsInfinity(value) && !IsFinite(value);

        public static Complex<T> Log(Complex<T> value)
        {
            return new Complex<T>(T.Log(Abs(value)), T.Atan2(value.m_imaginary, value.m_real));
        }

        public static Complex<T> Log(Complex<T> value, T baseValue)
        {
            return Log(value) / Log(new Complex<T>(baseValue, T.Zero));
        }

        public static Complex<T> Log10(Complex<T> value)
        {
            Complex<T> tempLog = Log(value);
            return Scale(tempLog, s_inverseOfLog10);
        }

        public static Complex<T> Exp(Complex<T> value)
        {
            T expReal = T.Exp(value.m_real);
            return FromPolarCoordinates(expReal, value.m_imaginary);
        }

        public static Complex<T> Sqrt(Complex<T> value)
        {
            // Handle NaN input cases according to IEEE 754
            if (T.IsNaN(value.m_real))
            {
                if (T.IsInfinity(value.m_imaginary))
                {
                    return new Complex<T>(T.PositiveInfinity, value.m_imaginary);
                }
                return new Complex<T>(T.NaN, T.NaN);
            }
            if (T.IsNaN(value.m_imaginary))
            {
                if (T.IsPositiveInfinity(value.m_real))
                {
                    return new Complex<T>(T.NaN, T.PositiveInfinity);
                }
                if (T.IsNegativeInfinity(value.m_real))
                {
                    return new Complex<T>(T.PositiveInfinity, T.NaN);
                }
                return new Complex<T>(T.NaN, T.NaN);
            }

            if (value.m_imaginary == T.Zero)
            {
                // Handle the trivial case quickly.
                if (value.m_real < T.Zero)
                {
                    return new Complex<T>(T.Zero, T.Sqrt(-value.m_real));
                }

                return new Complex<T>(T.Sqrt(value.m_real), T.Zero);
            }

            // If the components are too large, Hypot will overflow, even though the subsequent sqrt would
            // make the result representable. To avoid this, we re-scale (by exact powers of 2 for accuracy)
            // when we encounter very large components to avoid intermediate infinities.
            bool rescale = false;
            T realCopy = value.m_real;
            T imaginaryCopy = value.m_imaginary;
            if ((T.Abs(realCopy) >= s_sqrtRescaleThreshold) || (T.Abs(imaginaryCopy) >= s_sqrtRescaleThreshold))
            {
                if (T.IsInfinity(value.m_imaginary))
                {
                    // We need to handle infinite imaginary parts specially because otherwise
                    // our formulas below produce inf/inf = NaN.
                    return new Complex<T>(T.PositiveInfinity, imaginaryCopy);
                }

                T quarter = T.CreateChecked(0.25);
                realCopy *= quarter;
                imaginaryCopy *= quarter;
                rescale = true;
            }

            // This is the core of the algorithm. Everything else is special case handling.
            T x, y;
            T half = T.CreateChecked(0.5);
            if (realCopy >= T.Zero)
            {
                x = T.Sqrt((T.Hypot(realCopy, imaginaryCopy) + realCopy) * half);
                y = imaginaryCopy / (T.CreateChecked(2) * x);
            }
            else
            {
                y = T.Sqrt((T.Hypot(realCopy, imaginaryCopy) - realCopy) * half);
                if (imaginaryCopy < T.Zero) y = -y;
                x = imaginaryCopy / (T.CreateChecked(2) * y);
            }

            if (rescale)
            {
                x *= T.CreateChecked(2);
                y *= T.CreateChecked(2);
            }

            return new Complex<T>(x, y);
        }

        public static Complex<T> Pow(Complex<T> value, Complex<T> power)
        {
            if (power == Zero)
            {
                return One;
            }

            if (value == Zero)
            {
                return Zero;
            }

            T valueReal = value.m_real;
            T valueImaginary = value.m_imaginary;
            T powerReal = power.m_real;
            T powerImaginary = power.m_imaginary;

            T rho = Abs(value);
            T theta = T.Atan2(valueImaginary, valueReal);
            T newRho = powerReal * theta + powerImaginary * T.Log(rho);

            T t = T.Pow(rho, powerReal) * T.Exp(-powerImaginary * theta);

            return FromPolarCoordinates(t, newRho);
        }

        public static Complex<T> Pow(Complex<T> value, T power)
        {
            return Pow(value, new Complex<T>(power, T.Zero));
        }

        private static Complex<T> Scale(Complex<T> value, T factor)
        {
            T realResult = factor * value.m_real;
            T imaginaryResult = factor * value.m_imaginary;
            return new Complex<T>(realResult, imaginaryResult);
        }

        //
        // Implicit Conversions To Complex<T>
        //

        public static implicit operator Complex<T>(T value)
        {
            return new Complex<T>(value, T.Zero);
        }

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static Complex<T> IAdditiveIdentity<Complex<T>, Complex<T>>.AdditiveIdentity => Zero;

        //
        // IDecrementOperators
        //

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        public static Complex<T> operator --(Complex<T> value) => value - One;

        //
        // IIncrementOperators
        //

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_Increment(TSelf)" />
        public static Complex<T> operator ++(Complex<T> value) => value + One;

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        static Complex<T> IMultiplicativeIdentity<Complex<T>, Complex<T>>.MultiplicativeIdentity => One;

        //
        // INumberBase
        //

        /// <inheritdoc cref="INumberBase{TSelf}.Radix" />
        static int INumberBase<Complex<T>>.Radix => T.Radix;

        /// <inheritdoc cref="INumberBase{TSelf}.Abs(TSelf)" />
        static Complex<T> INumberBase<Complex<T>>.Abs(Complex<T> value) => Abs(value);

        /// <inheritdoc cref="INumberBase{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex<T> CreateChecked<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            Complex<T> result;

            if (typeof(TOther) == typeof(Complex<T>))
            {
                result = (Complex<T>)(object)value;
            }
            else if (!TryConvertFromCheckedCore(value, out result) && !TOther.TryConvertToChecked(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex<T> CreateSaturating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            Complex<T> result;

            if (typeof(TOther) == typeof(Complex<T>))
            {
                result = (Complex<T>)(object)value;
            }
            else if (!TryConvertFromSaturatingCore(value, out result) && !TOther.TryConvertToSaturating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Complex<T> CreateTruncating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            Complex<T> result;

            if (typeof(TOther) == typeof(Complex<T>))
            {
                result = (Complex<T>)(object)value;
            }
            else if (!TryConvertFromTruncatingCore(value, out result) && !TOther.TryConvertToTruncating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsCanonical(TSelf)" />
        static bool INumberBase<Complex<T>>.IsCanonical(Complex<T> value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsComplexNumber(TSelf)" />
        public static bool IsComplexNumber(Complex<T> value) => (value.m_real != T.Zero) && (value.m_imaginary != T.Zero);

        /// <inheritdoc cref="INumberBase{TSelf}.IsEvenInteger(TSelf)" />
        public static bool IsEvenInteger(Complex<T> value) => (value.m_imaginary == T.Zero) && T.IsEvenInteger(value.m_real);

        /// <inheritdoc cref="INumberBase{TSelf}.IsImaginaryNumber(TSelf)" />
        public static bool IsImaginaryNumber(Complex<T> value) => (value.m_real == T.Zero) && T.IsRealNumber(value.m_imaginary);

        /// <inheritdoc cref="INumberBase{TSelf}.IsInteger(TSelf)" />
        public static bool IsInteger(Complex<T> value) => (value.m_imaginary == T.Zero) && T.IsInteger(value.m_real);

        /// <inheritdoc cref="INumberBase{TSelf}.IsNegative(TSelf)" />
        public static bool IsNegative(Complex<T> value)
        {
            // since complex numbers do not have a well-defined concept of
            // negative we report false if this value has an imaginary part

            return (value.m_imaginary == T.Zero) && T.IsNegative(value.m_real);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsNegativeInfinity(TSelf)" />
        public static bool IsNegativeInfinity(Complex<T> value)
        {
            // since complex numbers do not have a well-defined concept of
            // negative we report false if this value has an imaginary part

            return (value.m_imaginary == T.Zero) && T.IsNegativeInfinity(value.m_real);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsNormal(TSelf)" />
        public static bool IsNormal(Complex<T> value)
        {
            // much as IsFinite requires both parts to be finite, we require both
            // parts to be "normal" (finite, non-zero, and non-subnormal) to be true

            return T.IsNormal(value.m_real)
                && ((value.m_imaginary == T.Zero) || T.IsNormal(value.m_imaginary));
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsOddInteger(TSelf)" />
        public static bool IsOddInteger(Complex<T> value) => (value.m_imaginary == T.Zero) && T.IsOddInteger(value.m_real);

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositive(TSelf)" />
        public static bool IsPositive(Complex<T> value)
        {
            // since complex numbers do not have a well-defined concept of
            // negative we report false if this value has an imaginary part

            return (value.m_imaginary == T.Zero) && T.IsPositive(value.m_real);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositiveInfinity(TSelf)" />
        public static bool IsPositiveInfinity(Complex<T> value)
        {
            // since complex numbers do not have a well-defined concept of
            // positive we report false if this value has an imaginary part

            return (value.m_imaginary == T.Zero) && T.IsPositiveInfinity(value.m_real);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsRealNumber(TSelf)" />
        public static bool IsRealNumber(Complex<T> value) => (value.m_imaginary == T.Zero) && T.IsRealNumber(value.m_real);

        /// <inheritdoc cref="INumberBase{TSelf}.IsSubnormal(TSelf)" />
        public static bool IsSubnormal(Complex<T> value)
        {
            // much as IsInfinite allows either part to be infinite, we allow either
            // part to be "subnormal" (finite, non-zero, and non-normal) to be true

            return T.IsSubnormal(value.m_real) || T.IsSubnormal(value.m_imaginary);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsZero(TSelf)" />
        static bool INumberBase<Complex<T>>.IsZero(Complex<T> value) => (value.m_real == T.Zero) && (value.m_imaginary == T.Zero);

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        public static Complex<T> MaxMagnitude(Complex<T> x, Complex<T> y)
        {
            // complex numbers are not normally comparable, however every complex
            // number has a real magnitude (absolute value) and so we can provide
            // an implementation for MaxMagnitude

            // This matches the IEEE 754:2019 `maximumMagnitude` function
            //
            // It propagates NaN inputs back to the caller and
            // otherwise returns the input with a larger magnitude.
            // It treats +0 as larger than -0 as per the specification.

            T ax = Abs(x);
            T ay = Abs(y);

            if ((ax > ay) || T.IsNaN(ax))
            {
                return x;
            }

            if (ax == ay)
            {
                // We have two equal magnitudes which means we have two of the following
                //   `+a + ib`
                //   `-a + ib`
                //   `+a - ib`
                //   `-a - ib`
                //
                // We want to treat `+a + ib` as greater than everything and `-a - ib` as
                // lesser. For `-a + ib` and `+a - ib` its "ambiguous" which should be preferred
                // so we will just preference `+a - ib` since that's the most correct choice
                // in the face of something like `+a - i0.0` vs `-a + i0.0`. This is the "most
                // correct" choice because both represent real numbers and `+a` is preferred
                // over `-a`.

                if (T.IsNegative(y.m_real))
                {
                    if (T.IsNegative(y.m_imaginary))
                    {
                        return x;
                    }
                    else
                    {
                        if (T.IsNegative(x.m_real))
                        {
                            return y;
                        }
                        else
                        {
                            return x;
                        }
                    }
                }
                else if (T.IsNegative(y.m_imaginary))
                {
                    if (T.IsNegative(x.m_real))
                    {
                        return y;
                    }
                    else
                    {
                        return x;
                    }
                }
            }

            return y;
        }

        internal static Complex<T> MaxMagnitudeNumber(Complex<T> x, Complex<T> y)
        {
            // complex numbers are not normally comparable, however every complex
            // number has a real magnitude (absolute value) and so we can provide
            // an implementation for MaxMagnitudeNumber

            // This matches the IEEE 754:2019 `maximumMagnitudeNumber` function
            //
            // It does not propagate NaN inputs back to the caller and
            // otherwise returns the input with a larger magnitude.
            // It treats +0 as larger than -0 as per the specification.

            T ax = Abs(x);
            T ay = Abs(y);

            if ((ax > ay) || T.IsNaN(ay))
            {
                return x;
            }

            if (ax == ay)
            {
                if (T.IsNegative(y.m_real))
                {
                    if (T.IsNegative(y.m_imaginary))
                    {
                        return x;
                    }
                    else
                    {
                        if (T.IsNegative(x.m_real))
                        {
                            return y;
                        }
                        else
                        {
                            return x;
                        }
                    }
                }
                else if (T.IsNegative(y.m_imaginary))
                {
                    if (T.IsNegative(x.m_real))
                    {
                        return y;
                    }
                    else
                    {
                        return x;
                    }
                }
            }

            return y;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitudeNumber(TSelf, TSelf)" />
        static Complex<T> INumberBase<Complex<T>>.MaxMagnitudeNumber(Complex<T> x, Complex<T> y)
            => MaxMagnitudeNumber(x, y);

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitude(TSelf, TSelf)" />
        public static Complex<T> MinMagnitude(Complex<T> x, Complex<T> y)
        {
            // complex numbers are not normally comparable, however every complex
            // number has a real magnitude (absolute value) and so we can provide
            // an implementation for MinMagnitude

            // This matches the IEEE 754:2019 `minimumMagnitude` function
            //
            // It propagates NaN inputs back to the caller and
            // otherwise returns the input with a smaller magnitude.
            // It treats -0 as smaller than +0 as per the specification.

            T ax = Abs(x);
            T ay = Abs(y);

            if ((ax < ay) || T.IsNaN(ax))
            {
                return x;
            }

            if (ax == ay)
            {
                if (T.IsNegative(y.m_real))
                {
                    if (T.IsNegative(y.m_imaginary))
                    {
                        return y;
                    }
                    else
                    {
                        if (T.IsNegative(x.m_real))
                        {
                            return x;
                        }
                        else
                        {
                            return y;
                        }
                    }
                }
                else if (T.IsNegative(y.m_imaginary))
                {
                    if (T.IsNegative(x.m_real))
                    {
                        return x;
                    }
                    else
                    {
                        return y;
                    }
                }
                else
                {
                    return x;
                }
            }

            return y;
        }

        internal static Complex<T> MinMagnitudeNumber(Complex<T> x, Complex<T> y)
        {
            // complex numbers are not normally comparable, however every complex
            // number has a real magnitude (absolute value) and so we can provide
            // an implementation for MinMagnitudeNumber

            // This matches the IEEE 754:2019 `minimumMagnitudeNumber` function
            //
            // It does not propagate NaN inputs back to the caller and
            // otherwise returns the input with a smaller magnitude.
            // It treats -0 as smaller than +0 as per the specification.

            T ax = Abs(x);
            T ay = Abs(y);

            if ((ax < ay) || T.IsNaN(ay))
            {
                return x;
            }

            if (ax == ay)
            {
                if (T.IsNegative(y.m_real))
                {
                    if (T.IsNegative(y.m_imaginary))
                    {
                        return y;
                    }
                    else
                    {
                        if (T.IsNegative(x.m_real))
                        {
                            return x;
                        }
                        else
                        {
                            return y;
                        }
                    }
                }
                else if (T.IsNegative(y.m_imaginary))
                {
                    if (T.IsNegative(x.m_real))
                    {
                        return x;
                    }
                    else
                    {
                        return y;
                    }
                }
                else
                {
                    return x;
                }
            }

            return y;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitudeNumber(TSelf, TSelf)" />
        static Complex<T> INumberBase<Complex<T>>.MinMagnitudeNumber(Complex<T> x, Complex<T> y)
            => MinMagnitudeNumber(x, y);

        internal static Complex<T> MultiplyAddEstimate(Complex<T> left, Complex<T> right, Complex<T> addend)
        {
            // Multiplication:  (a + bi)(c + di) = (ac - bd) + (bc + ad)i
            // Addition:        (a + bi) + (c + di) = (a + c) + (b + d)i

            T result_realpart = addend.m_real;
            result_realpart = T.MultiplyAddEstimate(-left.m_imaginary, right.m_imaginary, result_realpart);
            result_realpart = T.MultiplyAddEstimate(left.m_real, right.m_real, result_realpart);

            T result_imaginarypart = addend.m_imaginary;
            result_imaginarypart = T.MultiplyAddEstimate(left.m_real, right.m_imaginary, result_imaginarypart);
            result_imaginarypart = T.MultiplyAddEstimate(left.m_imaginary, right.m_real, result_imaginarypart);

            return new Complex<T>(result_realpart, result_imaginarypart);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.MultiplyAddEstimate(TSelf, TSelf, TSelf)" />
        static Complex<T> INumberBase<Complex<T>>.MultiplyAddEstimate(Complex<T> left, Complex<T> right, Complex<T> addend)
            => MultiplyAddEstimate(left, right, addend);

        /// <inheritdoc cref="INumberBase{TSelf}.Parse(ReadOnlySpan{char}, NumberStyles, IFormatProvider?)" />
        public static Complex<T> Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider)
        {
            if (!TryParse(s, style, provider, out Complex<T> result))
            {
                ThrowHelper.ThrowOverflowException();
            }
            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.Parse(ReadOnlySpan{byte}, NumberStyles, IFormatProvider?)" />
        public static Complex<T> Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider)
        {
            if (!TryParse(utf8Text, style, provider, out Complex<T> result))
            {
                ThrowHelper.ThrowOverflowException();
            }
            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.Parse(string, NumberStyles, IFormatProvider?)" />
        public static Complex<T> Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            ArgumentNullException.ThrowIfNull(s);
            return Parse(s.AsSpan(), style, provider);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromChecked{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Complex<T>>.TryConvertFromChecked<TOther>(TOther value, out Complex<T> result)
        {
            return TryConvertFromCheckedCore(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromSaturating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Complex<T>>.TryConvertFromSaturating<TOther>(TOther value, out Complex<T> result)
        {
            return TryConvertFromSaturatingCore(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromTruncating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Complex<T>>.TryConvertFromTruncating<TOther>(TOther value, out Complex<T> result)
        {
            return TryConvertFromTruncatingCore(value, out result);
        }

        internal static bool TryConvertFromCheckedCore<TOther>(TOther value, out Complex<T> result)
            where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(T))
            {
                result = new Complex<T>((T)(object)value, T.Zero);
                return true;
            }

            if (typeof(TOther) == typeof(Complex))
            {
                Complex actualValue = (Complex)(object)value;
                result = new Complex<T>(T.CreateChecked(actualValue.Real), T.CreateChecked(actualValue.Imaginary));
                return true;
            }

            if (T.TryConvertFromChecked(value, out T? realResult) && realResult is not null)
            {
                result = new Complex<T>(realResult, T.Zero);
                return true;
            }

            if (TOther.TryConvertToChecked<T>(value, out T? realResult2) && realResult2 is not null)
            {
                result = new Complex<T>(realResult2, T.Zero);
                return true;
            }

            result = default;
            return false;
        }

        internal static bool TryConvertFromSaturatingCore<TOther>(TOther value, out Complex<T> result)
            where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(T))
            {
                result = new Complex<T>((T)(object)value, T.Zero);
                return true;
            }

            if (typeof(TOther) == typeof(Complex))
            {
                Complex actualValue = (Complex)(object)value;
                result = new Complex<T>(T.CreateSaturating(actualValue.Real), T.CreateSaturating(actualValue.Imaginary));
                return true;
            }

            if (T.TryConvertFromSaturating(value, out T? realResult) && realResult is not null)
            {
                result = new Complex<T>(realResult, T.Zero);
                return true;
            }

            if (TOther.TryConvertToSaturating<T>(value, out T? realResult2) && realResult2 is not null)
            {
                result = new Complex<T>(realResult2, T.Zero);
                return true;
            }

            result = default;
            return false;
        }

        internal static bool TryConvertFromTruncatingCore<TOther>(TOther value, out Complex<T> result)
            where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(T))
            {
                result = new Complex<T>((T)(object)value, T.Zero);
                return true;
            }

            if (typeof(TOther) == typeof(Complex))
            {
                Complex actualValue = (Complex)(object)value;
                result = new Complex<T>(T.CreateTruncating(actualValue.Real), T.CreateTruncating(actualValue.Imaginary));
                return true;
            }

            if (T.TryConvertFromTruncating(value, out T? realResult) && realResult is not null)
            {
                result = new Complex<T>(realResult, T.Zero);
                return true;
            }

            if (TOther.TryConvertToTruncating<T>(value, out T? realResult2) && realResult2 is not null)
            {
                result = new Complex<T>(realResult2, T.Zero);
                return true;
            }

            result = default;
            return false;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToChecked{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Complex<T>>.TryConvertToChecked<TOther>(Complex<T> value, [MaybeNullWhen(false)] out TOther result)
        {
            if (typeof(TOther) == typeof(Complex<T>))
            {
                result = (TOther)(object)value;
                return true;
            }

            return TryConvertToCheckedCore(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToSaturating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Complex<T>>.TryConvertToSaturating<TOther>(Complex<T> value, [MaybeNullWhen(false)] out TOther result)
        {
            if (typeof(TOther) == typeof(Complex<T>))
            {
                result = (TOther)(object)value;
                return true;
            }

            return TryConvertToSaturatingCore(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToTruncating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Complex<T>>.TryConvertToTruncating<TOther>(Complex<T> value, [MaybeNullWhen(false)] out TOther result)
        {
            if (typeof(TOther) == typeof(Complex<T>))
            {
                result = (TOther)(object)value;
                return true;
            }

            return TryConvertToTruncatingCore(value, out result);
        }

        internal static bool TryConvertToCheckedCore<TOther>(Complex<T> value, [MaybeNullWhen(false)] out TOther result)
            where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(T))
            {
                // T is always IFloatingPointIeee754<T>, so NaN is valid for the imaginary case.
                result = (TOther)(object)((value.m_imaginary != T.Zero) ? T.NaN : value.m_real);
                return true;
            }

            if (typeof(TOther) == typeof(Complex))
            {
                result = (TOther)(object)new Complex(double.CreateChecked(value.m_real), double.CreateChecked(value.m_imaginary));
                return true;
            }

            if (typeof(TOther) == typeof(BigInteger))
            {
                if (value.m_imaginary != T.Zero)
                {
                    ThrowHelper.ThrowOverflowException();
                }

                BigInteger actualResult = checked((BigInteger)double.CreateChecked(value.m_real));
                result = (TOther)(object)actualResult;
                return true;
            }

            // A complex number with a non-zero imaginary part cannot be exactly represented as a real number.
            // For floating-point types, we return NaN; for integer/decimal types, we throw.
            if (value.m_imaginary != T.Zero)
            {
                if (!T.TryConvertToChecked(T.NaN, out result))
                {
                    ThrowHelper.ThrowOverflowException();
                }
                return result is not null;
            }

            if (T.TryConvertToChecked(value.m_real, out result))
            {
                return true;
            }

            return TOther.TryConvertFromChecked<T>(value.m_real, out result);
        }

        internal static bool TryConvertToSaturatingCore<TOther>(Complex<T> value, [MaybeNullWhen(false)] out TOther result)
            where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(T))
            {
                result = (TOther)(object)value.m_real;
                return true;
            }

            if (typeof(TOther) == typeof(Complex))
            {
                result = (TOther)(object)new Complex(double.CreateSaturating(value.m_real), double.CreateSaturating(value.m_imaginary));
                return true;
            }

            if (typeof(TOther) == typeof(BigInteger))
            {
                BigInteger actualResult = (BigInteger)double.CreateSaturating(value.m_real);
                result = (TOther)(object)actualResult;
                return true;
            }

            // For saturating conversion, ignore the imaginary part and just saturate the real part
            if (T.TryConvertToSaturating(value.m_real, out result))
            {
                return true;
            }

            return TOther.TryConvertFromSaturating<T>(value.m_real, out result);
        }

        internal static bool TryConvertToTruncatingCore<TOther>(Complex<T> value, [MaybeNullWhen(false)] out TOther result)
            where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(T))
            {
                result = (TOther)(object)value.m_real;
                return true;
            }

            if (typeof(TOther) == typeof(Complex))
            {
                result = (TOther)(object)new Complex(double.CreateTruncating(value.m_real), double.CreateTruncating(value.m_imaginary));
                return true;
            }

            if (typeof(TOther) == typeof(BigInteger))
            {
                BigInteger actualResult = (BigInteger)double.CreateTruncating(value.m_real);
                result = (TOther)(object)actualResult;
                return true;
            }

            // For truncating conversion, ignore the imaginary part and just truncate the real part
            if (T.TryConvertToTruncating(value.m_real, out result))
            {
                return true;
            }

            return TOther.TryConvertFromTruncating<T>(value.m_real, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryParse(ReadOnlySpan{char}, NumberStyles, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Complex<T> result)
            => TryParse(MemoryMarshal.Cast<char, Utf16Char>(s), style, provider, out result, out _);

        /// <inheritdoc cref="INumberBase{TSelf}.TryParse(ReadOnlySpan{byte}, NumberStyles, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out Complex<T> result)
            => TryParse(MemoryMarshal.Cast<byte, Utf8Char>(utf8Text), style, provider, out result, out _);

        /// <inheritdoc cref="INumberBase{TSelf}.TryParse(string, NumberStyles, IFormatProvider?, out TSelf, out int)" />
        static bool INumberBase<Complex<T>>.TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out Complex<T> result, out int charsConsumed)
            => TryParse(MemoryMarshal.Cast<char, Utf16Char>(s.AsSpan()), style, provider, out result, out charsConsumed);

        /// <inheritdoc cref="INumberBase{TSelf}.TryParse(ReadOnlySpan{byte}, NumberStyles, IFormatProvider?, out TSelf, out int)" />
        static bool INumberBase<Complex<T>>.TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out Complex<T> result, out int bytesConsumed)
            => TryParse(MemoryMarshal.Cast<byte, Utf8Char>(utf8Text), style, provider, out result, out bytesConsumed);

        /// <inheritdoc cref="INumberBase{TSelf}.TryParse(ReadOnlySpan{char}, NumberStyles, IFormatProvider?, out TSelf, out int)" />
        static bool INumberBase<Complex<T>>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Complex<T> result, out int charsConsumed)
            => TryParse(MemoryMarshal.Cast<char, Utf16Char>(s), style, provider, out result, out charsConsumed);

        internal static bool TryParse<TChar>(ReadOnlySpan<TChar> text, NumberStyles style, IFormatProvider? provider, out Complex<T> result, out int elementsConsumed)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            ValidateParseStyleFloatingPoint(style);

            int openBracket = text.IndexOf(TChar.CastFrom('<'));
            int semicolon = text.IndexOf(TChar.CastFrom(';'));
            int closeBracket = text.IndexOf(TChar.CastFrom('>'));

            if ((text.Length < 5) || (openBracket == -1) || (semicolon == -1) || (closeBracket == -1) || (openBracket > semicolon) || (openBracket > closeBracket) || (semicolon > closeBracket))
            {
                // We need at least 5 characters for `<0;0>`
                // We also expect to find an open bracket, a semicolon, and a closing bracket in that order

                result = default;
                elementsConsumed = 0;
                return false;
            }

            if ((openBracket != 0) && (((style & NumberStyles.AllowLeadingWhite) == 0) || !text.Slice(0, openBracket).IsWhiteSpace(out _)))
            {
                // The opening bracket wasn't the first and we either didn't allow leading whitespace
                // or one of the leading characters wasn't whitespace at all.

                result = default;
                elementsConsumed = 0;
                return false;
            }

            // The real and imaginary components are exactly delimited by the ';' and '>' separators,
            // so AllowTrailingInvalidCharacters only applies after the closing bracket, not within a
            // component. Otherwise something like "<1.5x;2>" would incorrectly parse as (1.5, 2).
            NumberStyles componentStyle = style & ~NumberStyles.AllowTrailingInvalidCharacters;

            ReadOnlySpan<TChar> slice = text.Slice(openBracket + 1, semicolon - openBracket - 1);

            if ((typeof(TChar) == typeof(Utf8Char))
                ? !T.TryParse(Unsafe.BitCast<ReadOnlySpan<TChar>, ReadOnlySpan<byte>>(slice), componentStyle, provider, out T? real)
                : !T.TryParse(Unsafe.BitCast<ReadOnlySpan<TChar>, ReadOnlySpan<char>>(slice), componentStyle, provider, out real))
            {
                result = default;
                elementsConsumed = 0;
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
                ? !T.TryParse(Unsafe.BitCast<ReadOnlySpan<TChar>, ReadOnlySpan<byte>>(slice), componentStyle, provider, out T? imaginary)
                : !T.TryParse(Unsafe.BitCast<ReadOnlySpan<TChar>, ReadOnlySpan<char>>(slice), componentStyle, provider, out imaginary))
            {
                result = default;
                elementsConsumed = 0;
                return false;
            }

            int trailingWhiteLength = 0;
            if (closeBracket != (text.Length - 1))
            {
                bool isInvalid = true;

                if ((style & NumberStyles.AllowTrailingWhite) != 0)
                {
                    if (text.Slice(closeBracket + 1).IsWhiteSpace(out trailingWhiteLength))
                    {
                        isInvalid = false;
                    }
                }

                if (isInvalid && ((style & NumberStyles.AllowTrailingInvalidCharacters) == 0))
                {
                    // The closing bracket wasn't the last and we either didn't allow trailing whitespace
                    // or one of the trailing characters wasn't whitespace at all.

                    result = default;
                    elementsConsumed = 0;
                    return false;
                }
            }

            result = new Complex<T>(real!, imaginary!);
            elementsConsumed = closeBracket + 1 + trailingWhiteLength;
            return true;
        }

        private static void ValidateParseStyleFloatingPoint(NumberStyles style)
        {
            // Check for undefined flags or hex number
            if ((style & (Complex.InvalidNumberStyles | NumberStyles.AllowHexSpecifier)) != 0)
            {
                ThrowInvalid(style);

                static void ThrowInvalid(NumberStyles value)
                {
                    if ((value & Complex.InvalidNumberStyles) != 0)
                    {
                        throw new ArgumentException(SR.Argument_InvalidNumberStyles, nameof(style));
                    }

                    throw new ArgumentException(SR.Arg_HexStyleNotSupported);
                }
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryParse(string, NumberStyles, IFormatProvider?, out TSelf)" />
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out Complex<T> result)
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
        public static Complex<T> Parse(string s, IFormatProvider? provider) => Parse(s, Complex.DefaultNumberStyle, provider);

        /// <inheritdoc cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)" />
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Complex<T> result) => TryParse(s, Complex.DefaultNumberStyle, provider, out result);

        //
        // ISignedNumber
        //

        /// <inheritdoc cref="ISignedNumber{TSelf}.NegativeOne" />
        static Complex<T> ISignedNumber<Complex<T>>.NegativeOne => new(-T.One, T.Zero);

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
        public static Complex<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, Complex.DefaultNumberStyle, provider);

        /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Complex<T> result) => TryParse(s, Complex.DefaultNumberStyle, provider, out result);

        //
        // IUnaryPlusOperators
        //

        /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_UnaryPlus(TSelf)" />
        public static Complex<T> operator +(Complex<T> value) => value;

        //
        // IUtf8SpanParsable
        //

        /// <inheritdoc cref="IUtf8SpanParsable{TSelf}.Parse(ReadOnlySpan{byte}, IFormatProvider?)" />
        public static Complex<T> Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) => Parse(utf8Text, Complex.DefaultNumberStyle, provider);

        /// <inheritdoc cref="IUtf8SpanParsable{TSelf}.TryParse(ReadOnlySpan{byte}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out Complex<T> result) => TryParse(utf8Text, Complex.DefaultNumberStyle, provider, out result);
    }
}
