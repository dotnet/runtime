// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
          ISignedNumber<Complex>
    {
        private const NumberStyles DefaultNumberStyle = NumberStyles.Float | NumberStyles.AllowThousands;

        private const NumberStyles InvalidNumberStyles = ~(NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite
                                                         | NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign
                                                         | NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint
                                                         | NumberStyles.AllowThousands | NumberStyles.AllowExponent
                                                         | NumberStyles.AllowCurrencySymbol | NumberStyles.AllowHexSpecifier);

        public static readonly Complex Zero = new Complex(0.0, 0.0);
        public static readonly Complex One = new Complex(1.0, 0.0);
        public static readonly Complex ImaginaryOne = new Complex(0.0, 1.0);
        public static readonly Complex NaN = new Complex(double.NaN, double.NaN);
        public static readonly Complex Infinity = new Complex(double.PositiveInfinity, double.PositiveInfinity);

        private const double InverseOfLog10 = 0.43429448190325; // 1 / Log(10)

        // This is the largest x for which (Hypot(x,x) + x) will not overflow. It is used for branching inside Sqrt.
        private static readonly double s_sqrtRescaleThreshold = double.MaxValue / (Math.Sqrt(2.0) + 1.0);

        // This is the largest x for which 2 x^2 will not overflow. It is used for branching inside Asin and Acos.
        private static readonly double s_asinOverflowThreshold = Math.Sqrt(double.MaxValue) / 2.0;

        // This value is used inside Asin and Acos.
        private static readonly double s_log2 = Math.Log(2.0);

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
            return new Complex(magnitude * Math.Cos(phase), magnitude * Math.Sin(phase));
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
            if (!double.IsFinite(left.m_real))
            {
                if (!double.IsFinite(left.m_imaginary))
                {
                    return new Complex(double.NaN, double.NaN);
                }

                return new Complex(left.m_real * right, double.NaN);
            }

            if (!double.IsFinite(left.m_imaginary))
            {
                return new Complex(double.NaN, left.m_imaginary * right);
            }

            return new Complex(left.m_real * right, left.m_imaginary * right);
        }

        public static Complex operator *(double left, Complex right)
        {
            if (!double.IsFinite(right.m_real))
            {
                if (!double.IsFinite(right.m_imaginary))
                {
                    return new Complex(double.NaN, double.NaN);
                }

                return new Complex(left * right.m_real, double.NaN);
            }

            if (!double.IsFinite(right.m_imaginary))
            {
                return new Complex(double.NaN, left * right.m_imaginary);
            }

            return new Complex(left * right.m_real, left * right.m_imaginary);
        }

        public static Complex operator /(Complex left, Complex right)
        {
            // Division : Smith's formula.
            double a = left.m_real;
            double b = left.m_imaginary;
            double c = right.m_real;
            double d = right.m_imaginary;

            // Computing c * c + d * d will overflow even in cases where the actual result of the division does not overflow.
            if (Math.Abs(d) < Math.Abs(c))
            {
                double doc = d / c;
                return new Complex((a + b * doc) / (c + d * doc), (b - a * doc) / (c + d * doc));
            }
            else
            {
                double cod = c / d;
                return new Complex((b + a * cod) / (d + c * cod), (-a + b * cod) / (d + c * cod));
            }
        }

        public static Complex operator /(Complex left, double right)
        {
            // IEEE prohibit optimizations which are value changing
            // so we make sure that behaviour for the simplified version exactly match
            // full version.
            if (right == 0)
            {
                return new Complex(double.NaN, double.NaN);
            }

            if (!double.IsFinite(left.m_real))
            {
                if (!double.IsFinite(left.m_imaginary))
                {
                    return new Complex(double.NaN, double.NaN);
                }

                return new Complex(left.m_real / right, double.NaN);
            }

            if (!double.IsFinite(left.m_imaginary))
            {
                return new Complex(double.NaN, left.m_imaginary / right);
            }

            // Here the actual optimized version of code.
            return new Complex(left.m_real / right, left.m_imaginary / right);
        }

        public static Complex operator /(double left, Complex right)
        {
            // Division : Smith's formula.
            double a = left;
            double c = right.m_real;
            double d = right.m_imaginary;

            // Computing c * c + d * d will overflow even in cases where the actual result of the division does not overflow.
            if (Math.Abs(d) < Math.Abs(c))
            {
                double doc = d / c;
                return new Complex(a / (c + d * doc), (-a * doc) / (c + d * doc));
            }
            else
            {
                double cod = c / d;
                return new Complex(a * cod / (d + c * cod), -a / (d + c * cod));
            }
        }

        public static double Abs(Complex value)
        {
            return Hypot(value.m_real, value.m_imaginary);
        }

        private static double Hypot(double a, double b)
        {
            // Using
            //   sqrt(a^2 + b^2) = |a| * sqrt(1 + (b/a)^2)
            // we can factor out the larger component to dodge overflow even when a * a would overflow.

            a = Math.Abs(a);
            b = Math.Abs(b);

            double small, large;
            if (a < b)
            {
                small = a;
                large = b;
            }
            else
            {
                small = b;
                large = a;
            }

            if (small == 0.0)
            {
                return (large);
            }
            else if (double.IsPositiveInfinity(large) && !double.IsNaN(small))
            {
                // The NaN test is necessary so we don't return +inf when small=NaN and large=+inf.
                // NaN in any other place returns NaN without any special handling.
                return (double.PositiveInfinity);
            }
            else
            {
                double ratio = small / large;
                return (large * Math.Sqrt(1.0 + ratio * ratio));
            }

        }

        private static double Log1P(double x)
        {
            // Compute log(1 + x) without loss of accuracy when x is small.

            // Our only use case so far is for positive values, so this isn't coded to handle negative values.
            Debug.Assert((x >= 0.0) || double.IsNaN(x));

            double xp1 = 1.0 + x;
            if (xp1 == 1.0)
            {
                return x;
            }
            else if (x < 0.75)
            {
                // This is accurate to within 5 ulp with any floating-point system that uses a guard digit,
                // as proven in Theorem 4 of "What Every Computer Scientist Should Know About Floating-Point
                // Arithmetic" (https://docs.oracle.com/cd/E19957-01/806-3568/ncg_goldberg.html)
                return x * Math.Log(xp1) / (xp1 - 1.0);
            }
            else
            {
                return Math.Log(xp1);
            }

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
            if (!(obj is Complex)) return false;
            return Equals((Complex)obj);
        }

        public bool Equals(Complex value)
        {
            return m_real.Equals(value.m_real) && m_imaginary.Equals(value.m_imaginary);
        }

        public override int GetHashCode()
        {
            int n1 = 99999997;
            int realHash = m_real.GetHashCode() % n1;
            int imaginaryHash = m_imaginary.GetHashCode();
            int finalHash = realHash ^ imaginaryHash;
            return finalHash;
        }

        public override string ToString() => $"<{m_real}; {m_imaginary}>";

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format) => ToString(format, null);

        public string ToString(IFormatProvider? provider) => ToString(null, provider);

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        {
            return string.Format(provider, "<{0}; {1}>", m_real.ToString(format, provider), m_imaginary.ToString(format, provider));
        }

        public static Complex Sin(Complex value)
        {
            // We need both sinh and cosh of imaginary part. To avoid multiple calls to Math.Exp with the same value,
            // we compute them both here from a single call to Math.Exp.
            double p = Math.Exp(value.m_imaginary);
            double q = 1.0 / p;
            double sinh = (p - q) * 0.5;
            double cosh = (p + q) * 0.5;
            return new Complex(Math.Sin(value.m_real) * cosh, Math.Cos(value.m_real) * sinh);
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
            double b, bPrime, v;
            Asin_Internal(Math.Abs(value.Real), Math.Abs(value.Imaginary), out b, out bPrime, out v);

            double u;
            if (bPrime < 0.0)
            {
                u = Math.Asin(b);
            }
            else
            {
                u = Math.Atan(bPrime);
            }

            if (value.Real < 0.0) u = -u;
            if (value.Imaginary < 0.0) v = -v;

            return new Complex(u, v);
        }

        public static Complex Cos(Complex value)
        {
            double p = Math.Exp(value.m_imaginary);
            double q = 1.0 / p;
            double sinh = (p - q) * 0.5;
            double cosh = (p + q) * 0.5;
            return new Complex(Math.Cos(value.m_real) * cosh, -Math.Sin(value.m_real) * sinh);
        }

        public static Complex Cosh(Complex value)
        {
            // Use cosh(z) = cos(iz) to compute via cos(z).
            return Cos(new Complex(-value.m_imaginary, value.m_real));
        }

        public static Complex Acos(Complex value)
        {
            double b, bPrime, v;
            Asin_Internal(Math.Abs(value.Real), Math.Abs(value.Imaginary), out b, out bPrime, out v);

            double u;
            if (bPrime < 0.0)
            {
                u = Math.Acos(b);
            }
            else
            {
                u = Math.Atan(1.0 / bPrime);
            }

            if (value.Real < 0.0) u = Math.PI - u;
            if (value.Imaginary > 0.0) v = -v;

            return new Complex(u, v);
        }

        public static Complex Tan(Complex value)
        {
            // tan z = sin z / cos z, but to avoid unnecessary repeated trig computations, use
            //   tan z = (sin(2x) + i sinh(2y)) / (cos(2x) + cosh(2y))
            // (see Abramowitz & Stegun 4.3.57 or derive by hand), and compute trig functions here.

            // This approach does not work for |y| > ~355, because sinh(2y) and cosh(2y) overflow,
            // even though their ratio does not. In that case, divide through by cosh to get:
            //   tan z = (sin(2x) / cosh(2y) + i \tanh(2y)) / (1 + cos(2x) / cosh(2y))
            // which correctly computes the (tiny) real part and the (normal-sized) imaginary part.

            double x2 = 2.0 * value.m_real;
            double y2 = 2.0 * value.m_imaginary;
            double p = Math.Exp(y2);
            double q = 1.0 / p;
            double cosh = (p + q) * 0.5;
            if (Math.Abs(value.m_imaginary) <= 4.0)
            {
                double sinh = (p - q) * 0.5;
                double D = Math.Cos(x2) + cosh;
                return new Complex(Math.Sin(x2) / D, sinh / D);
            }
            else
            {
                double D = 1.0 + Math.Cos(x2) / cosh;
                return new Complex(Math.Sin(x2) / cosh / D, Math.Tanh(y2) / D);
            }
        }

        public static Complex Tanh(Complex value)
        {
            // Use tanh(z) = -i tan(iz) to compute via tan(z).
            Complex tan = Tan(new Complex(-value.m_imaginary, value.m_real));
            return new Complex(tan.m_imaginary, -tan.m_real);
        }

        public static Complex Atan(Complex value)
        {
            Complex two = new Complex(2.0, 0.0);
            return (ImaginaryOne / two) * (Log(One - ImaginaryOne * value) - Log(One + ImaginaryOne * value));
        }

        private static void Asin_Internal(double x, double y, out double b, out double bPrime, out double v)
        {

            // This method for the inverse complex sine (and cosine) is described in Hull, Fairgrieve,
            // and Tang, "Implementing the Complex Arcsine and Arccosine Functions Using Exception Handling",
            // ACM Transactions on Mathematical Software (1997)
            // (https://www.researchgate.net/profile/Ping_Tang3/publication/220493330_Implementing_the_Complex_Arcsine_and_Arccosine_Functions_Using_Exception_Handling/links/55b244b208ae9289a085245d.pdf)

            // First, the basics: start with sin(w) = (e^{iw} - e^{-iw}) / (2i) = z. Here z is the input
            // and w is the output. To solve for w, define t = e^{i w} and multiply through by t to
            // get the quadratic equation t^2 - 2 i z t - 1 = 0. The solution is t = i z + sqrt(1 - z^2), so
            //   w = arcsin(z) = - i log( i z + sqrt(1 - z^2) )
            // Decompose z = x + i y, multiply out i z + sqrt(1 - z^2), use log(s) = |s| + i arg(s), and do a
            // bunch of algebra to get the components of w = arcsin(z) = u + i v
            //   u = arcsin(beta)  v = sign(y) log(alpha + sqrt(alpha^2 - 1))
            // where
            //   alpha = (rho + sigma) / 2      beta = (rho - sigma) / 2
            //   rho = sqrt((x + 1)^2 + y^2)    sigma = sqrt((x - 1)^2 + y^2)
            // These formulas appear in DLMF section 4.23. (http://dlmf.nist.gov/4.23), along with the analogous
            //   arccos(w) = arccos(beta) - i sign(y) log(alpha + sqrt(alpha^2 - 1))
            // So alpha and beta together give us arcsin(w) and arccos(w).

            // As written, alpha is not susceptible to cancelation errors, but beta is. To avoid cancelation, note
            //   beta = (rho^2 - sigma^2) / (rho + sigma) / 2 = (2 x) / (rho + sigma) = x / alpha
            // which is not subject to cancelation. Note alpha >= 1 and |beta| <= 1.

            // For alpha ~ 1, the argument of the log is near unity, so we compute (alpha - 1) instead,
            // write the argument as 1 + (alpha - 1) + sqrt((alpha - 1)(alpha + 1)), and use the log1p function
            // to compute the log without loss of accuracy.
            // For beta ~ 1, arccos does not accurately resolve small angles, so we compute the tangent of the angle
            // instead.
            // Hull, Fairgrieve, and Tang derive formulas for (alpha - 1) and beta' = tan(u) that do not suffer
            // from cancelation in these cases.

            // For simplicity, we assume all positive inputs and return all positive outputs. The caller should
            // assign signs appropriate to the desired cut conventions. We return v directly since its magnitude
            // is the same for both arcsin and arccos. Instead of u, we usually return beta and sometimes beta'.
            // If beta' is not computed, it is set to -1; if it is computed, it should be used instead of beta
            // to determine u. Compute u = arcsin(beta) or u = arctan(beta') for arcsin, u = arccos(beta)
            // or arctan(1/beta') for arccos.

            Debug.Assert((x >= 0.0) || double.IsNaN(x));
            Debug.Assert((y >= 0.0) || double.IsNaN(y));

            // For x or y large enough to overflow alpha^2, we can simplify our formulas and avoid overflow.
            if ((x > s_asinOverflowThreshold) || (y > s_asinOverflowThreshold))
            {
                b = -1.0;
                bPrime = x / y;

                double small, big;
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
                double ratio = small / big;
                v = s_log2 + Math.Log(big) + 0.5 * Log1P(ratio * ratio);
            }
            else
            {
                double r = Hypot((x + 1.0), y);
                double s = Hypot((x - 1.0), y);

                double a = (r + s) * 0.5;
                b = x / a;

                if (b > 0.75)
                {
                    if (x <= 1.0)
                    {
                        double amx = (y * y / (r + (x + 1.0)) + (s + (1.0 - x))) * 0.5;
                        bPrime = x / Math.Sqrt((a + x) * amx);
                    }
                    else
                    {
                        // In this case, amx ~ y^2. Since we take the square root of amx, we should
                        // pull y out from under the square root so we don't lose its contribution
                        // when y^2 underflows.
                        double t = (1.0 / (r + (x + 1.0)) + 1.0 / (s + (x - 1.0))) * 0.5;
                        bPrime = x / y / Math.Sqrt((a + x) * t);
                    }
                }
                else
                {
                    bPrime = -1.0;
                }

                if (a < 1.5)
                {
                    if (x < 1.0)
                    {
                        // This is another case where our expression is proportional to y^2 and
                        // we take its square root, so again we pull out a factor of y from
                        // under the square root.
                        double t = (1.0 / (r + (x + 1.0)) + 1.0 / (s + (1.0 - x))) * 0.5;
                        double am1 = y * y * t;
                        v = Log1P(am1 + y * Math.Sqrt(t * (a + 1.0)));
                    }
                    else
                    {
                        double am1 = (y * y / (r + (x + 1.0)) + (s + (x - 1.0))) * 0.5;
                        v = Log1P(am1 + Math.Sqrt(am1 * (a + 1.0)));
                    }
                }
                else
                {
                    // Because of the test above, we can be sure that a * a will not overflow.
                    v = Math.Log(a + Math.Sqrt((a - 1.0) * (a + 1.0)));
                }
            }
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
            double cosImaginary = expReal * Math.Cos(value.m_imaginary);
            double sinImaginary = expReal * Math.Sin(value.m_imaginary);
            return new Complex(cosImaginary, sinImaginary);
        }

        public static Complex Sqrt(Complex value)
        {

            if (value.m_imaginary == 0.0)
            {
                // Handle the trivial case quickly.
                if (value.m_real < 0.0)
                {
                    return new Complex(0.0, Math.Sqrt(-value.m_real));
                }

                return new Complex(Math.Sqrt(value.m_real), 0.0);
            }

            // One way to compute Sqrt(z) is just to call Pow(z, 0.5), which coverts to polar coordinates
            // (sqrt + atan), halves the phase, and reconverts to cartesian coordinates (cos + sin).
            // Not only is this more expensive than necessary, it also fails to preserve certain expected
            // symmetries, such as that the square root of a pure negative is a pure imaginary, and that the
            // square root of a pure imaginary has exactly equal real and imaginary parts. This all goes
            // back to the fact that Math.PI is not stored with infinite precision, so taking half of Math.PI
            // does not land us on an argument with cosine exactly equal to zero.

            // To find a fast and symmetry-respecting formula for complex square root,
            // note x + i y = \sqrt{a + i b} implies x^2 + 2 i x y - y^2 = a + i b,
            // so x^2 - y^2 = a and 2 x y = b. Cross-substitute and use the quadratic formula to obtain
            //   x = \sqrt{\frac{\sqrt{a^2 + b^2} + a}{2}}  y = \pm \sqrt{\frac{\sqrt{a^2 + b^2} - a}{2}}
            // There is just one complication: depending on the sign on a, either x or y suffers from
            // cancelation when |b| << |a|. We can get aroud this by noting that our formulas imply
            // x^2 y^2 = b^2 / 4, so |x| |y| = |b| / 2. So after computing the one that doesn't suffer
            // from cancelation, we can compute the other with just a division. This is basically just
            // the right way to evaluate the quadratic formula without cancelation.

            // All this reduces our total cost to two sqrts and a few flops, and it respects the desired
            // symmetries. Much better than atan + cos + sin!

            // The signs are a matter of choice of branch cut, which is traditionally taken so x > 0 and sign(y) = sign(b).

            // If the components are too large, Hypot will overflow, even though the subsequent sqrt would
            // make the result representable. To avoid this, we re-scale (by exact powers of 2 for accuracy)
            // when we encounter very large components to avoid intermediate infinities.
            bool rescale = false;
            double realCopy = value.m_real;
            double imaginaryCopy = value.m_imaginary;
            if ((Math.Abs(realCopy) >= s_sqrtRescaleThreshold) || (Math.Abs(imaginaryCopy) >= s_sqrtRescaleThreshold))
            {
                if (double.IsInfinity(value.m_imaginary) && !double.IsNaN(value.m_real))
                {
                    // We need to handle infinite imaginary parts specially because otherwise
                    // our formulas below produce inf/inf = NaN. The NaN test is necessary
                    // so that we return NaN rather than (+inf,inf) for (NaN,inf).
                    return (new Complex(double.PositiveInfinity, imaginaryCopy));
                }

                realCopy *= 0.25;
                imaginaryCopy *= 0.25;
                rescale = true;
            }

            // This is the core of the algorithm. Everything else is special case handling.
            double x, y;
            if (realCopy >= 0.0)
            {
                x = Math.Sqrt((Hypot(realCopy, imaginaryCopy) + realCopy) * 0.5);
                y = imaginaryCopy / (2.0 * x);
            }
            else
            {
                y = Math.Sqrt((Hypot(realCopy, imaginaryCopy) - realCopy) * 0.5);
                if (imaginaryCopy < 0.0) y = -y;
                x = imaginaryCopy / (2.0 * y);
            }

            if (rescale)
            {
                x *= 2.0;
                y *= 2.0;
            }

            return new Complex(x, y);
        }

        public static Complex Pow(Complex value, Complex power)
        {
            if (power == Zero)
            {
                return One;
            }

            if (value == Zero)
            {
                return Zero;
            }

            double valueReal = value.m_real;
            double valueImaginary = value.m_imaginary;
            double powerReal = power.m_real;
            double powerImaginary = power.m_imaginary;

            double rho = Abs(value);
            double theta = Math.Atan2(valueImaginary, valueReal);
            double newRho = powerReal * theta + powerImaginary * Math.Log(rho);

            double t = Math.Pow(rho, powerReal) * Math.Pow(Math.E, -powerImaginary * theta);

            return new Complex(t * Math.Cos(newRho), t * Math.Sin(newRho));
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
        static Complex IAdditiveIdentity<Complex, Complex>.AdditiveIdentity => new Complex(0.0, 0.0);

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
        static Complex IMultiplicativeIdentity<Complex, Complex>.MultiplicativeIdentity => new Complex(1.0, 0.0);

        //
        // INumberBase
        //

        /// <inheritdoc cref="INumberBase{TSelf}.One" />
        static Complex INumberBase<Complex>.One => new Complex(1.0, 0.0);

        /// <inheritdoc cref="INumberBase{TSelf}.Radix" />
        static int INumberBase<Complex>.Radix => 2;

        /// <inheritdoc cref="INumberBase{TSelf}.Zero" />
        static Complex INumberBase<Complex>.Zero => new Complex(0.0, 0.0);

        /// <inheritdoc cref="INumberBase{TSelf}.Abs(TSelf)" />
        static Complex INumberBase<Complex>.Abs(Complex value) => Abs(value);

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
            // complex numbers are not normally comparable, however every complex
            // number has a real magnitude (absolute value) and so we can provide
            // an implementation for MaxMagnitude

            // This matches the IEEE 754:2019 `maximumMagnitude` function
            //
            // It propagates NaN inputs back to the caller and
            // otherwise returns the input with a larger magnitude.
            // It treats +0 as larger than -0 as per the specification.

            double ax = Abs(x);
            double ay = Abs(y);

            if ((ax > ay) || double.IsNaN(ax))
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

                if (double.IsNegative(y.m_real))
                {
                    if (double.IsNegative(y.m_imaginary))
                    {
                        // when `y` is `-a - ib` we always prefer `x` (its either the same as
                        // `x` or some part of `x` is positive).

                        return x;
                    }
                    else
                    {
                        if (double.IsNegative(x.m_real))
                        {
                            // when `y` is `-a + ib` and `x` is `-a + ib` or `-a - ib` then
                            // we either have same value or both parts of `x` are negative
                            // and we want to prefer `y`.

                            return y;
                        }
                        else
                        {
                            // when `y` is `-a + ib` and `x` is `+a + ib` or `+a - ib` then
                            // we want to prefer `x` because either both parts are positive
                            // or we want to prefer `+a - ib` due to how it handles when `x`
                            // represents a real number.

                            return x;
                        }
                    }
                }
                else if (double.IsNegative(y.m_imaginary))
                {
                    if (double.IsNegative(x.m_real))
                    {
                        // when `y` is `+a - ib` and `x` is `-a + ib` or `-a - ib` then
                        // we either both parts of `x` are negative or we want to prefer
                        // `+a - ib` due to how it handles when `y` represents a real number.

                        return y;
                    }
                    else
                    {
                        // when `y` is `+a - ib` and `x` is `+a + ib` or `+a - ib` then
                        // we want to prefer `x` because either both parts are positive
                        // or they represent the same value.

                        return x;
                    }
                }
            }

            return y;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitudeNumber(TSelf, TSelf)" />
        static Complex INumberBase<Complex>.MaxMagnitudeNumber(Complex x, Complex y)
        {
            // complex numbers are not normally comparable, however every complex
            // number has a real magnitude (absolute value) and so we can provide
            // an implementation for MaxMagnitudeNumber

            // This matches the IEEE 754:2019 `maximumMagnitudeNumber` function
            //
            // It does not propagate NaN inputs back to the caller and
            // otherwise returns the input with a larger magnitude.
            // It treats +0 as larger than -0 as per the specification.

            double ax = Abs(x);
            double ay = Abs(y);

            if ((ax > ay) || double.IsNaN(ay))
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

                if (double.IsNegative(y.m_real))
                {
                    if (double.IsNegative(y.m_imaginary))
                    {
                        // when `y` is `-a - ib` we always prefer `x` (its either the same as
                        // `x` or some part of `x` is positive).

                        return x;
                    }
                    else
                    {
                        if (double.IsNegative(x.m_real))
                        {
                            // when `y` is `-a + ib` and `x` is `-a + ib` or `-a - ib` then
                            // we either have same value or both parts of `x` are negative
                            // and we want to prefer `y`.

                            return y;
                        }
                        else
                        {
                            // when `y` is `-a + ib` and `x` is `+a + ib` or `+a - ib` then
                            // we want to prefer `x` because either both parts are positive
                            // or we want to prefer `+a - ib` due to how it handles when `x`
                            // represents a real number.

                            return x;
                        }
                    }
                }
                else if (double.IsNegative(y.m_imaginary))
                {
                    if (double.IsNegative(x.m_real))
                    {
                        // when `y` is `+a - ib` and `x` is `-a + ib` or `-a - ib` then
                        // we either both parts of `x` are negative or we want to prefer
                        // `+a - ib` due to how it handles when `y` represents a real number.

                        return y;
                    }
                    else
                    {
                        // when `y` is `+a - ib` and `x` is `+a + ib` or `+a - ib` then
                        // we want to prefer `x` because either both parts are positive
                        // or they represent the same value.

                        return x;
                    }
                }
            }

            return y;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitude(TSelf, TSelf)" />
        public static Complex MinMagnitude(Complex x, Complex y)
        {
            // complex numbers are not normally comparable, however every complex
            // number has a real magnitude (absolute value) and so we can provide
            // an implementation for MaxMagnitude

            // This matches the IEEE 754:2019 `minimumMagnitude` function
            //
            // It propagates NaN inputs back to the caller and
            // otherwise returns the input with a smaller magnitude.
            // It treats -0 as smaller than +0 as per the specification.

            double ax = Abs(x);
            double ay = Abs(y);

            if ((ax < ay) || double.IsNaN(ax))
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
                // so we will just preference `-a + ib` since that's the most correct choice
                // in the face of something like `+a - i0.0` vs `-a + i0.0`. This is the "most
                // correct" choice because both represent real numbers and `-a` is preferred
                // over `+a`.

                if (double.IsNegative(y.m_real))
                {
                    if (double.IsNegative(y.m_imaginary))
                    {
                        // when `y` is `-a - ib` we always prefer `y` as both parts are negative
                        return y;
                    }
                    else
                    {
                        if (double.IsNegative(x.m_real))
                        {
                            // when `y` is `-a + ib` and `x` is `-a + ib` or `-a - ib` then
                            // we either have same value or both parts of `x` are negative
                            // and we want to prefer it.

                            return x;
                        }
                        else
                        {
                            // when `y` is `-a + ib` and `x` is `+a + ib` or `+a - ib` then
                            // we want to prefer `y` because either both parts of 'x' are positive
                            // or we want to prefer `-a - ib` due to how it handles when `y`
                            // represents a real number.

                            return y;
                        }
                    }
                }
                else if (double.IsNegative(y.m_imaginary))
                {
                    if (double.IsNegative(x.m_real))
                    {
                        // when `y` is `+a - ib` and `x` is `-a + ib` or `-a - ib` then
                        // either both parts of `x` are negative or we want to prefer
                        // `-a - ib` due to how it handles when `x` represents a real number.

                        return x;
                    }
                    else
                    {
                        // when `y` is `+a - ib` and `x` is `+a + ib` or `+a - ib` then
                        // we want to prefer `y` because either both parts of x are positive
                        // or they represent the same value.

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
        static Complex INumberBase<Complex>.MinMagnitudeNumber(Complex x, Complex y)
        {
            // complex numbers are not normally comparable, however every complex
            // number has a real magnitude (absolute value) and so we can provide
            // an implementation for MinMagnitudeNumber

            // This matches the IEEE 754:2019 `minimumMagnitudeNumber` function
            //
            // It does not propagate NaN inputs back to the caller and
            // otherwise returns the input with a smaller magnitude.
            // It treats -0 as smaller than +0 as per the specification.

            double ax = Abs(x);
            double ay = Abs(y);

            if ((ax < ay) || double.IsNaN(ay))
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
                // so we will just preference `-a + ib` since that's the most correct choice
                // in the face of something like `+a - i0.0` vs `-a + i0.0`. This is the "most
                // correct" choice because both represent real numbers and `-a` is preferred
                // over `+a`.

                if (double.IsNegative(y.m_real))
                {
                    if (double.IsNegative(y.m_imaginary))
                    {
                        // when `y` is `-a - ib` we always prefer `y` as both parts are negative
                        return y;
                    }
                    else
                    {
                        if (double.IsNegative(x.m_real))
                        {
                            // when `y` is `-a + ib` and `x` is `-a + ib` or `-a - ib` then
                            // we either have same value or both parts of `x` are negative
                            // and we want to prefer it.

                            return x;
                        }
                        else
                        {
                            // when `y` is `-a + ib` and `x` is `+a + ib` or `+a - ib` then
                            // we want to prefer `y` because either both parts of 'x' are positive
                            // or we want to prefer `-a - ib` due to how it handles when `y`
                            // represents a real number.

                            return y;
                        }
                    }
                }
                else if (double.IsNegative(y.m_imaginary))
                {
                    if (double.IsNegative(x.m_real))
                    {
                        // when `y` is `+a - ib` and `x` is `-a + ib` or `-a - ib` then
                        // either both parts of `x` are negative or we want to prefer
                        // `-a - ib` due to how it handles when `x` represents a real number.

                        return x;
                    }
                    else
                    {
                        // when `y` is `+a - ib` and `x` is `+a + ib` or `+a - ib` then
                        // we want to prefer `y` because either both parts of x are positive
                        // or they represent the same value.

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

        /// <inheritdoc cref="INumberBase{TSelf}.Parse(ReadOnlySpan{char}, NumberStyles, IFormatProvider?)" />
        public static Complex Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider)
        {
            if (!TryParse(s, style, provider, out Complex result))
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
        static bool INumberBase<Complex>.TryConvertToChecked<TOther>(Complex value, [NotNullWhen(true)] out TOther result)
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
                result = default!;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToSaturating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Complex>.TryConvertToSaturating<TOther>(Complex value, [NotNullWhen(true)] out TOther result)
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
                result = default!;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToTruncating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Complex>.TryConvertToTruncating<TOther>(Complex value, [NotNullWhen(true)] out TOther result)
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
                result = default!;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryParse(ReadOnlySpan{char}, NumberStyles, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Complex result)
        {
            ValidateParseStyleFloatingPoint(style);

            int openBracket = s.IndexOf('<');
            int semicolon = s.IndexOf(';');
            int closeBracket = s.IndexOf('>');

            if ((s.Length < 5) || (openBracket == -1) || (semicolon == -1) || (closeBracket == -1) || (openBracket > semicolon) || (openBracket > closeBracket) || (semicolon > closeBracket))
            {
                // We need at least 5 characters for `<0;0>`
                // We also expect a to find an open bracket, a semicolon, and a closing bracket in that order

                result = default;
                return false;
            }

            if ((openBracket != 0) && (((style & NumberStyles.AllowLeadingWhite) == 0) || !s.Slice(0, openBracket).IsWhiteSpace()))
            {
                // The opening bracket wasn't the first and we either didn't allow leading whitespace
                // or one of the leading characters wasn't whitespace at all.

                result = default;
                return false;
            }

            if (!double.TryParse(s.Slice(openBracket + 1, semicolon), style, provider, out double real))
            {
                result = default;
                return false;
            }

            if (char.IsWhiteSpace(s[semicolon + 1]))
            {
                // We allow a single whitespace after the semicolon regardless of style, this is so that
                // the output of `ToString` can be correctly parsed by default and values will roundtrip.
                semicolon += 1;
            }

            if (!double.TryParse(s.Slice(semicolon + 1, closeBracket - semicolon), style, provider, out double imaginary))
            {
                result = default;
                return false;
            }

            if ((closeBracket != (s.Length - 1)) && (((style & NumberStyles.AllowTrailingWhite) == 0) || !s.Slice(closeBracket).IsWhiteSpace()))
            {
                // The closing bracket wasn't the last and we either didn't allow trailing whitespace
                // or one of the trailing characters wasn't whitespace at all.

                result = default;
                return false;
            }

            result = new Complex(real, imaginary);
            return true;

            static void ValidateParseStyleFloatingPoint(NumberStyles style)
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
        static Complex ISignedNumber<Complex>.NegativeOne => new Complex(-1.0, 0.0);

        //
        // ISpanFormattable
        //

        /// <inheritdoc cref="ISpanFormattable.TryFormat(Span{char}, out int, ReadOnlySpan{char}, IFormatProvider?)" />
        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            int charsWrittenSoFar = 0;

            // We have at least 6 more characters for: <0; 0>
            if (destination.Length < 6)
            {
                charsWritten = charsWrittenSoFar;
                return false;
            }

            destination[charsWrittenSoFar++] = '<';

            bool tryFormatSucceeded = m_real.TryFormat(destination.Slice(charsWrittenSoFar), out int tryFormatCharsWritten, format, provider);
            charsWrittenSoFar += tryFormatCharsWritten;

            // We have at least 4 more characters for: ; 0>
            if (!tryFormatSucceeded || (destination.Length < (charsWrittenSoFar + 4)))
            {
                charsWritten = charsWrittenSoFar;
                return false;
            }

            destination[charsWrittenSoFar++] = ';';
            destination[charsWrittenSoFar++] = ' ';

            tryFormatSucceeded = m_imaginary.TryFormat(destination.Slice(charsWrittenSoFar), out tryFormatCharsWritten, format, provider);
            charsWrittenSoFar += tryFormatCharsWritten;

            // We have at least 1 more character for: >
            if (!tryFormatSucceeded || (destination.Length < (charsWrittenSoFar + 1)))
            {
                charsWritten = charsWrittenSoFar;
                return false;
            }

            destination[charsWrittenSoFar++] = '>';

            charsWritten = charsWrittenSoFar;
            return true;
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
    }
}
