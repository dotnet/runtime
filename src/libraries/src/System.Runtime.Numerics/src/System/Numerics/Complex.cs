// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace System.Numerics
{
    /// <summary>
    /// A complex number z is a number of the form z = x + yi, where x and y 
    /// are real numbers, and i is the imaginary unit, with the property i2= -1.
    /// </summary>
    [Serializable]
    public struct Complex : IEquatable<Complex>, IFormattable
    {
        public static readonly Complex Zero = new Complex(0.0, 0.0);
        public static readonly Complex One = new Complex(1.0, 0.0);
        public static readonly Complex ImaginaryOne = new Complex(0.0, 1.0);

        private const double InverseOfLog10 = 0.43429448190325; // 1 / Log(10)

        private double _real;
        private double _imaginary;
        
        public Complex(double real, double imaginary)
        {
            _real = real;
            _imaginary = imaginary;
        }

        public double Real { get { return _real; } }
        public double Imaginary { get { return _imaginary; } }

        public double Magnitude { get { return Abs(this); } }
        public double Phase { get { return Math.Atan2(_imaginary, _real); } }

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

        public static Complex Subtract(Complex left, Complex right)
        {
            return left - right;
        }

        public static Complex Multiply(Complex left, Complex right)
        {
            return left * right;
        }

        public static Complex Divide(Complex dividend, Complex divisor)
        {
            return dividend / divisor;
        }
        
        public static Complex operator -(Complex value)  /* Unary negation of a complex number */
        {
            return new Complex(-value._real, -value._imaginary);
        }
        
        public static Complex operator +(Complex left, Complex right)
        {
            return new Complex(left._real + right._real, left._imaginary + right._imaginary);
        }

        public static Complex operator -(Complex left, Complex right)
        {
            return new Complex(left._real - right._real, left._imaginary - right._imaginary);
        }

        public static Complex operator *(Complex left, Complex right)
        {
            // Multiplication:  (a + bi)(c + di) = (ac -bd) + (bc + ad)i
            double result_Realpart = (left._real * right._real) - (left._imaginary * right._imaginary);
            double result_Imaginarypart = (left._imaginary * right._real) + (left._real * right._imaginary);
            return new Complex(result_Realpart, result_Imaginarypart);
        }

        public static Complex operator /(Complex left, Complex right)
        {
            // Division : Smith's formula.
            double a = left._real;
            double b = left._imaginary;
            double c = right._real;
            double d = right._imaginary;

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

        public static double Abs(Complex value)
        {
            if (double.IsInfinity(value._real) || double.IsInfinity(value._imaginary))
            {
                return double.PositiveInfinity;
            }

            // |value| == sqrt(a^2 + b^2)
            // sqrt(a^2 + b^2) == a/a * sqrt(a^2 + b^2) = a * sqrt(a^2/a^2 + b^2/a^2)
            // Using the above we can factor out the square of the larger component to dodge overflow.
            double c = Math.Abs(value._real);
            double d = Math.Abs(value._imaginary);

            if (c > d)
            {
                double r = d / c;
                return c * Math.Sqrt(1.0 + r * r);
            }
            else if (d == 0.0)
            {
                return c;  // c is either 0.0 or NaN
            }
            else
            {
                double r = c / d;
                return d * Math.Sqrt(1.0 + r * r);
            }
        }

        public static Complex Conjugate(Complex value)
        {
            // Conjugate of a Complex number: the conjugate of x+i*y is x-i*y
            return new Complex(value._real, -value._imaginary);
        }

        public static Complex Reciprocal(Complex value)
        {
            // Reciprocal of a Complex number : the reciprocal of x+i*y is 1/(x+i*y)
            if (value._real == 0 && value._imaginary == 0)
            {
                return Zero;
            }
            return One / value;
        }
        
        public static bool operator ==(Complex left, Complex right)
        {
            return left._real == right._real && left._imaginary == right._imaginary;
        }

        public static bool operator !=(Complex left, Complex right)
        {
            return left._real != right._real || left._imaginary != right._imaginary;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Complex)) return false;
            return Equals((Complex)obj);
        }

        public bool Equals(Complex value)
        {
            return _real.Equals(value._real) && _imaginary.Equals(value._imaginary);
        }

        public override int GetHashCode()
        {
            int n1 = 99999997;
            int realHash = _real.GetHashCode() % n1;
            int imaginaryHash = _imaginary.GetHashCode();
            int finalHash = realHash ^ imaginaryHash;
            return finalHash;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "({0}, {1})", _real, _imaginary);
        }

        public string ToString(string format)
        {
            return string.Format(CultureInfo.CurrentCulture, "({0}, {1})", _real.ToString(format, CultureInfo.CurrentCulture), _imaginary.ToString(format, CultureInfo.CurrentCulture));
        }

        public string ToString(IFormatProvider provider)
        {
            return string.Format(provider, "({0}, {1})", _real, _imaginary);
        }

        public string ToString(string format, IFormatProvider provider)
        {
            return string.Format(provider, "({0}, {1})", _real.ToString(format, provider), _imaginary.ToString(format, provider));
        }

        public static Complex Sin(Complex value)
        {
            double a = value._real;
            double b = value._imaginary;
            return new Complex(Math.Sin(a) * Math.Cosh(b), Math.Cos(a) * Math.Sinh(b));
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sinh", Justification = "Sinh is the name of a mathematical function.")]
        public static Complex Sinh(Complex value)
        {
            double a = value._real;
            double b = value._imaginary;
            return new Complex(Math.Sinh(a) * Math.Cos(b), Math.Cosh(a) * Math.Sin(b));
        }

        public static Complex Asin(Complex value)
        {
            if ((value._imaginary == 0 && value._real < 0) || value._imaginary > 0)
            {
                return -Asin(-value);
            }
            return (-ImaginaryOne) * Log(ImaginaryOne * value + Sqrt(One - value * value));
        }

        public static Complex Cos(Complex value)
        {
            double a = value._real;
            double b = value._imaginary;
            return new Complex(Math.Cos(a) * Math.Cosh(b), -(Math.Sin(a) * Math.Sinh(b)));
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cosh", Justification = "Cosh is the name of a mathematical function.")]
        public static Complex Cosh(Complex value)
        {
            double a = value._real;
            double b = value._imaginary;
            return new Complex(Math.Cosh(a) * Math.Cos(b), Math.Sinh(a) * Math.Sin(b));
        }

        public static Complex Acos(Complex value)
        {
            if ((value._imaginary == 0 && value._real > 0) || value._imaginary < 0)
            {
                return Math.PI - Acos(-value);
            }
            return (-ImaginaryOne) * Log(value + ImaginaryOne * Sqrt(One - (value * value)));
        }

        public static Complex Tan(Complex value)
        {
            return (Sin(value) / Cos(value));
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Tanh", Justification = "Tanh is the name of a mathematical function.")]
        public static Complex Tanh(Complex value)
        {
            return (Sinh(value) / Cosh(value));
        }

        public static Complex Atan(Complex value)
        {
            Complex two = new Complex(2.0, 0.0);
            return (ImaginaryOne / two) * (Log(One - ImaginaryOne * value) - Log(One + ImaginaryOne * value));
        }

        public static Complex Log(Complex value)
        {
            return new Complex(Math.Log(Abs(value)), Math.Atan2(value._imaginary, value._real));
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
            double expReal = Math.Exp(value._real);
            double cosImaginary = expReal * Math.Cos(value._imaginary);
            double sinImaginary = expReal * Math.Sin(value._imaginary);
            return new Complex(cosImaginary, sinImaginary);
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sqrt", Justification = "Sqrt is the name of a mathematical function.")]
        public static Complex Sqrt(Complex value)
        {
            return FromPolarCoordinates(Math.Sqrt(value.Magnitude), value.Phase / 2.0);
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

            double valueReal = value._real;
            double valueImaginary = value._imaginary;
            double powerReal = power._real;
            double powerImaginary = power._imaginary;

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
            double realResult = factor * value._real;
            double imaginaryResuilt = factor * value._imaginary;
            return new Complex(realResult, imaginaryResuilt);
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

        [CLSCompliant(false)]
        public static implicit operator Complex(sbyte value)
        {
            return new Complex(value, 0.0);
        }

        public static implicit operator Complex(byte value)
        {
            return new Complex(value, 0.0);
        }

        public static implicit operator Complex(float value)
        {
            return new Complex(value, 0.0);
        }

        public static implicit operator Complex(double value)
        {
            return new Complex(value, 0.0);
        }

        public static explicit operator Complex(BigInteger value)
        {
            return new Complex((double)value, 0.0);
        }

        public static explicit operator Complex(decimal value)
        {
            return new Complex((double)value, 0.0);
        }
    }
}
