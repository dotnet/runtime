// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=========================================================================
**
** Purpose: 
** A complex number z is a number of the form z = x + yi, where x and y 
** are real numbers, and i is the imaginary unit, with the property i2= -1.
**
===========================================================================*/

using System;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace System.Numerics
{
    public struct Complex : IEquatable<Complex>, IFormattable
    {
        // --------------SECTION: Private Data members ----------- //

        private Double _real;
        private Double _imaginary;

        // ---------------SECTION: Necessary Constants ----------- //

        private const Double LOG_10_INV = 0.43429448190325;


        // --------------SECTION: Public Properties -------------- //

        public Double Real
        {
            get
            {
                return _real;
            }
        }

        public Double Imaginary
        {
            get
            {
                return _imaginary;
            }
        }

        public Double Magnitude
        {
            get
            {
                return Complex.Abs(this);
            }
        }

        public Double Phase
        {
            get
            {
                return Math.Atan2(_imaginary, _real);
            }
        }

        // --------------SECTION: Attributes -------------- //

        public static readonly Complex Zero = new Complex(0.0, 0.0);
        public static readonly Complex One = new Complex(1.0, 0.0);
        public static readonly Complex ImaginaryOne = new Complex(0.0, 1.0);

        // --------------SECTION: Constructors and factory methods -------------- //

        public Complex(Double real, Double imaginary)  /* Constructor to create a complex number with rectangular co-ordinates  */
        {
            _real = real;
            _imaginary = imaginary;
        }

        public static Complex FromPolarCoordinates(Double magnitude, Double phase) /* Factory method to take polar inputs and create a Complex object */
        {
            return new Complex((magnitude * Math.Cos(phase)), (magnitude * Math.Sin(phase)));
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

        // --------------SECTION: Arithmetic Operator(unary) Overloading -------------- //
        public static Complex operator -(Complex value)  /* Unary negation of a complex number */
        {
            return (new Complex((-value._real), (-value._imaginary)));
        }

        // --------------SECTION: Arithmetic Operator(binary) Overloading -------------- //       
        public static Complex operator +(Complex left, Complex right)
        {
            return (new Complex((left._real + right._real), (left._imaginary + right._imaginary)));
        }

        public static Complex operator -(Complex left, Complex right)
        {
            return (new Complex((left._real - right._real), (left._imaginary - right._imaginary)));
        }

        public static Complex operator *(Complex left, Complex right)
        {
            // Multiplication:  (a + bi)(c + di) = (ac -bd) + (bc + ad)i
            Double result_Realpart = (left._real * right._real) - (left._imaginary * right._imaginary);
            Double result_Imaginarypart = (left._imaginary * right._real) + (left._real * right._imaginary);
            return (new Complex(result_Realpart, result_Imaginarypart));
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


        // --------------SECTION: Other arithmetic operations  -------------- //

        public static Double Abs(Complex value)
        {
            if (Double.IsInfinity(value._real) || Double.IsInfinity(value._imaginary))
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

            return (new Complex(value._real, (-value._imaginary)));
        }
        public static Complex Reciprocal(Complex value)
        {
            // Reciprocal of a Complex number : the reciprocal of x+i*y is 1/(x+i*y)
            if ((value._real == 0) && (value._imaginary == 0))
            {
                return Complex.Zero;
            }

            return Complex.One / value;
        }

        // --------------SECTION: Comparison Operator(binary) Overloading -------------- //

        public static bool operator ==(Complex left, Complex right)
        {
            return ((left._real == right._real) && (left._imaginary == right._imaginary));
        }
        public static bool operator !=(Complex left, Complex right)
        {
            return ((left._real != right._real) || (left._imaginary != right._imaginary));
        }

        // --------------SECTION: Comparison operations (methods implementing IEquatable<ComplexNumber>,IComparable<ComplexNumber>) -------------- //

        public override bool Equals(object obj)
        {
            if (!(obj is Complex)) return false;
            return this == ((Complex)obj);
        }
        public bool Equals(Complex value)
        {
            return ((_real.Equals(value._real)) && (_imaginary.Equals(value._imaginary)));
        }

        // --------------SECTION: Type-casting basic numeric data-types to ComplexNumber  -------------- //

        public static implicit operator Complex(Int16 value)
        {
            return (new Complex(value, 0.0));
        }
        public static implicit operator Complex(Int32 value)
        {
            return (new Complex(value, 0.0));
        }
        public static implicit operator Complex(Int64 value)
        {
            return (new Complex(value, 0.0));
        }
        [CLSCompliant(false)]
        public static implicit operator Complex(UInt16 value)
        {
            return (new Complex(value, 0.0));
        }
        [CLSCompliant(false)]
        public static implicit operator Complex(UInt32 value)
        {
            return (new Complex(value, 0.0));
        }
        [CLSCompliant(false)]
        public static implicit operator Complex(UInt64 value)
        {
            return (new Complex(value, 0.0));
        }
        [CLSCompliant(false)]
        public static implicit operator Complex(SByte value)
        {
            return (new Complex(value, 0.0));
        }
        public static implicit operator Complex(Byte value)
        {
            return (new Complex(value, 0.0));
        }
        public static implicit operator Complex(Single value)
        {
            return (new Complex(value, 0.0));
        }
        public static implicit operator Complex(Double value)
        {
            return (new Complex(value, 0.0));
        }
        public static explicit operator Complex(BigInteger value)
        {
            return (new Complex((Double)value, 0.0));
        }
        public static explicit operator Complex(Decimal value)
        {
            return (new Complex((Double)value, 0.0));
        }


        // --------------SECTION: Formattig/Parsing options  -------------- //

        public override String ToString()
        {
            return (String.Format(CultureInfo.CurrentCulture, "({0}, {1})", _real, _imaginary));
        }

        public String ToString(String format)
        {
            return (String.Format(CultureInfo.CurrentCulture, "({0}, {1})", _real.ToString(format, CultureInfo.CurrentCulture), _imaginary.ToString(format, CultureInfo.CurrentCulture)));
        }

        public String ToString(IFormatProvider provider)
        {
            return (String.Format(provider, "({0}, {1})", _real, _imaginary));
        }

        public String ToString(String format, IFormatProvider provider)
        {
            return (String.Format(provider, "({0}, {1})", _real.ToString(format, provider), _imaginary.ToString(format, provider)));
        }


        public override Int32 GetHashCode()
        {
            Int32 n1 = 99999997;
            Int32 hash_real = _real.GetHashCode() % n1;
            Int32 hash_imaginary = _imaginary.GetHashCode();
            Int32 final_hashcode = hash_real ^ hash_imaginary;
            return (final_hashcode);
        }



        // --------------SECTION: Trigonometric operations (methods implementing ITrigonometric)  -------------- //

        public static Complex Sin(Complex value)
        {
            double a = value._real;
            double b = value._imaginary;
            return new Complex(Math.Sin(a) * Math.Cosh(b), Math.Cos(a) * Math.Sinh(b));
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sinh", Justification = "matell: Existing Name")]
        public static Complex Sinh(Complex value) /* Hyperbolic sin */
        {
            double a = value._real;
            double b = value._imaginary;
            return new Complex(Math.Sinh(a) * Math.Cos(b), Math.Cosh(a) * Math.Sin(b));
        }
        public static Complex Asin(Complex value) /* Arcsin */
        {
            return (-ImaginaryOne) * Log(ImaginaryOne * value + Sqrt(One - value * value));
        }

        public static Complex Cos(Complex value)
        {
            double a = value._real;
            double b = value._imaginary;
            return new Complex(Math.Cos(a) * Math.Cosh(b), -(Math.Sin(a) * Math.Sinh(b)));
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Cosh", Justification = "matell: Existing Name")]
        public static Complex Cosh(Complex value) /* Hyperbolic cos */
        {
            double a = value._real;
            double b = value._imaginary;
            return new Complex(Math.Cosh(a) * Math.Cos(b), Math.Sinh(a) * Math.Sin(b));
        }
        public static Complex Acos(Complex value) /* Arccos */
        {
            return (-ImaginaryOne) * Log(value + ImaginaryOne * Sqrt(One - (value * value)));
        }
        public static Complex Tan(Complex value)
        {
            return (Sin(value) / Cos(value));
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Tanh", Justification = "matell: Existing Name")]
        public static Complex Tanh(Complex value) /* Hyperbolic tan */
        {
            return (Sinh(value) / Cosh(value));
        }
        public static Complex Atan(Complex value) /* Arctan */
        {
            Complex Two = new Complex(2.0, 0.0);
            return (ImaginaryOne / Two) * (Log(One - ImaginaryOne * value) - Log(One + ImaginaryOne * value));
        }

        // --------------SECTION: Other numerical functions  -------------- //        

        public static Complex Log(Complex value) /* Log of the complex number value to the base of 'e' */
        {
            return (new Complex((Math.Log(Abs(value))), (Math.Atan2(value._imaginary, value._real))));
        }
        public static Complex Log(Complex value, Double baseValue) /* Log of the complex number to a the base of a double */
        {
            return (Log(value) / Log(baseValue));
        }
        public static Complex Log10(Complex value) /* Log to the base of 10 of the complex number */
        {
            Complex temp_log = Log(value);
            return (Scale(temp_log, (Double)LOG_10_INV));
        }
        public static Complex Exp(Complex value) /* The complex number raised to e */
        {
            Double temp_factor = Math.Exp(value._real);
            Double result_re = temp_factor * Math.Cos(value._imaginary);
            Double result_im = temp_factor * Math.Sin(value._imaginary);
            return (new Complex(result_re, result_im));
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sqrt", Justification = "matell: Existing Name")]
        public static Complex Sqrt(Complex value) /* Square root ot the complex number */
        {
            return Complex.FromPolarCoordinates(Math.Sqrt(value.Magnitude), value.Phase / 2.0);
        }

        public static Complex Pow(Complex value, Complex power) /* A complex number raised to another complex number */
        {
            if (power == Complex.Zero)
            {
                return Complex.One;
            }

            if (value == Complex.Zero)
            {
                return Complex.Zero;
            }

            double a = value._real;
            double b = value._imaginary;
            double c = power._real;
            double d = power._imaginary;

            double rho = Complex.Abs(value);
            double theta = Math.Atan2(b, a);
            double newRho = c * theta + d * Math.Log(rho);

            double t = Math.Pow(rho, c) * Math.Pow(Math.E, -d * theta);

            return new Complex(t * Math.Cos(newRho), t * Math.Sin(newRho));
        }

        public static Complex Pow(Complex value, Double power) // A complex number raised to a real number 
        {
            return Pow(value, new Complex(power, 0));
        }



        //--------------- SECTION: Private member functions for internal use -----------------------------------//

        private static Complex Scale(Complex value, Double factor)
        {
            Double result_re = factor * value._real;
            Double result_im = factor * value._imaginary;
            return (new Complex(result_re, result_im));
        }
    }
}
