// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Purpose: Some single-precision floating-point math operations
**
===========================================================*/

//This class contains only static members and doesn't require serialization.

//For most of this implementation for .NET Framework we just defer to System.Math and do a cast internally from single to double.
//We do this because it safer and less likely to break people since that is what they are alrady doing. Also, adding in the
//extra pinvokes needed to not do this route would probably incur an extra overhead that would be undersired.

//For any version of .NET Core this just forwards directly to the MathF implementation inside the runtime.

//There are a few cases where .NET Framework handles things differently than .NET Core does. For example, it returns -0 and +0
//when using things like Min/Max, and they count as different values from each other. This is fixed in .NET Core, but since its
//inherent in .NET Framework we decided to leave that behavior as is for this BCL.

using System.Diagnostics.Contracts;

namespace System
{
    /// <summary>
    /// Provides constants and static methods for trigonometric, logarithmic, and other common mathematical functions.
    /// </summary>
    public static class MathF
    {
        /// <summary>
        /// Represents the ratio of the circumference of a circle to its diameter, specified by the constant, p.
        /// </summary>
        public const float PI = 3.14159265f;

        /// <summary>
        /// Represents the natural logarithmic base, specified by the constant, e.
        /// </summary>
        public const float E = 2.71828183f;

        private static readonly float NegativeZero = Int32BitsToSingle(unchecked((int)0x80000000));

        private static unsafe float Int32BitsToSingle(int value)
        {
            return *((float*)&value);
        }

        [Pure]
        private static unsafe bool IsNegative(float f)
        {
            return (*(uint*)(&f) & 0x80000000) == 0x80000000;
        }

        /// <summary>
        /// Returns the absolute value of a single-precision floating-point number.
        /// </summary>
        /// <param name="x">The number to take the absolute value of.</param>
        /// <returns>The absolute value of <paramref name="x"/></returns>
        public static float Abs(float x) => Math.Abs(x);

        /// <summary>
        /// Returns the angle whose cosine is the specified number.
        /// </summary>
        /// <param name="x">The number to take the acos of.</param>
        /// <returns>The acos of <paramref name="x"/></returns>
        public static float Acos(float x) => (float)Math.Acos(x);

        /// <summary>
        /// Returns the angle whose sine is the specified number.
        /// </summary>
        /// <param name="x">The number to take the asin of.</param>
        /// <returns>The asin of <paramref name="x"/></returns>
        public static float Asin(float x) => (float)Math.Asin(x);

        /// <summary>
        /// Returns the angle whose tangent is the specified number.
        /// </summary>
        /// <param name="x">The number to take the atan of.</param>
        /// <returns>The atan of <paramref name="x"/></returns>
        public static float Atan(float x) => (float)Math.Atan(x);

        /// <summary>
        /// Returns the angle whose tangent is the quotient of two specified numbers.
        /// </summary>
        /// <param name="y">The first number.</param>
        /// <param name="x">The second number.</param>
        /// <returns>The angle whose tangent is the quotient of <paramref name="y"/> and <paramref name="x"/></returns>
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);

        /// <summary>
        /// Returns the smallest integral value that is greater than or equal to the specified single-precision floating-point number.
        /// </summary>
        /// <param name="x">The number to take the ceiling of.</param>
        /// <returns>The ceiling of <paramref name="x"/></returns>
        public static float Ceiling(float x) => (float)Math.Ceiling(x);

        /// <summary>
        /// Returns the cosine of the specified angle.
        /// </summary>
        /// <param name="x">The angle to take the cosine of.</param>
        /// <returns>The cosine of <paramref name="x"/></returns>
        public static float Cos(float x) => (float)Math.Cos(x);

        /// <summary>
        /// Returns the hyperbolic cosine of the specified angle.
        /// </summary>
        /// <param name="x">The angle to take the hyperbolic cosine of.</param>
        /// <returns>The hyperbolic cosine of <paramref name="x"/></returns>
        public static float Cosh(float x) => (float)Math.Cosh(x);

        /// <summary>
        /// Returns e raised to the specified power.
        /// </summary>
        /// <param name="x">The number to raise e to.</param>
        /// <returns>e raised to the power of <paramref name="x"/></returns>
        public static float Exp(float x) => (float)Math.Exp(x);

        /// <summary>
        /// Returns the largest integral value less than or equal to the specified single-precision floating-point number.
        /// </summary>
        /// <param name="x">The number to take the floor of.</param>
        /// <returns>The floor of <paramref name="x"/></returns>
        public static float Floor(float x) => (float)Math.Floor(x);

        /// <summary>
        /// Returns the remainder resulting from the division of a specified number by another specified number.
        /// </summary>
        /// <param name="x">The numerator</param>
        /// <param name="y">The denominator</param>
        /// <returns>The result of dividing <paramref name="x"/> by <paramref name="y"/></returns>
        public static float IEEERemainder(float x, float y)
        {
            if (float.IsNaN(x))
            {
                return x; // IEEE 754-2008: NaN payload must be preserved
            }

            if (float.IsNaN(y))
            {
                return y; // IEEE 754-2008: NaN payload must be preserved
            }

            var regularMod = x % y;

            if (float.IsNaN(regularMod))
            {
                return float.NaN;
            }

            if ((regularMod == 0) && IsNegative(x))
            {
                return NegativeZero;
            }

            var alternativeResult = (regularMod - (Abs(y) * Sign(x)));

            if (Abs(alternativeResult) == Abs(regularMod))
            {
                var divisionResult = x / y;
                var roundedResult = Round(divisionResult);

                if (Abs(roundedResult) > Abs(divisionResult))
                {
                    return alternativeResult;
                }
                else
                {
                    return regularMod;
                }
            }

            if (Abs(alternativeResult) < Abs(regularMod))
            {
                return alternativeResult;
            }
            else
            {
                return regularMod;
            }
        }

        /// <summary>
        /// Returns the natural (base e) logarithm of a specified number.
        /// </summary>
        /// <param name="x">The number to take the natural log of.</param>
        /// <returns>The natural log of <paramref name="x"/></returns>
        public static float Log(float x) => (float)Math.Log(x);

        /// <summary>
        /// Returns the logarithm of a specified number in a specified base.
        /// </summary>
        /// <param name="x">The number to take the log of.</param>
        /// <param name="y">The base of the log</param>
        /// <returns>The log of <paramref name="x"/> with base <paramref name="y"/></returns>
        public static float Log(float x, float y)
        {
            if (float.IsNaN(x))
            {
                return x; // IEEE 754-2008: NaN payload must be preserved
            }

            if (float.IsNaN(y))
            {
                return y; // IEEE 754-2008: NaN payload must be preserved
            }

            if (y == 1)
            {
                return float.NaN;
            }

            if ((x != 1) && ((y == 0) || float.IsPositiveInfinity(y)))
            {
                return float.NaN;
            }

            return Log(x) / Log(y);
        }

        /// <summary>
        /// Returns the base 10 logarithm of a specified number.
        /// </summary>
        /// <param name="x">The number to take the base 10 log of.</param>
        /// <returns>The base 10 log of <paramref name="x"/></returns>
        public static float Log10(float x) => (float)Math.Log10(x);

        /// <summary>
        /// Returns the larger of two single-precision floating-point numbers.
        /// </summary>
        /// <param name="x">The first number to compare.</param>
        /// <param name="y">The second number to compare.</param>
        /// <returns>The larger of <paramref name="x"/> and <paramref name="y"/></returns>
        public static float Max(float x, float y) => Math.Max(x, y);

        /// <summary>
        /// Returns the smaller of two single-precision floating-point numbers.
        /// </summary>
        /// <param name="x">The first number to compare.</param>
        /// <param name="y">The second number to compare.</param>
        /// <returns>The smaller of <paramref name="x"/> and <paramref name="y"/></returns>
        public static float Min(float x, float y) => Math.Min(x, y);

        /// <summary>
        /// Returns a specified number raised to the specified power.
        /// </summary>
        /// <param name="x">The base number.</param>
        /// <param name="y">The specified power.</param>
        /// <returns><paramref name="x"/> raised to the power of <paramref name="y"/></returns>
        public static float Pow(float x, float y) => (float)Math.Pow(x, y);

        /// <summary>
        /// Rounds a single-precision floating-point value to the nearest integral value, and rounds midpoint values to the nearest even number.
        /// </summary>
        /// <param name="x">The number to round.</param>
        /// <returns>The rounded representation of <paramref name="x"/></returns>
        public static float Round(float x) => (float)Math.Round(x);

        /// <summary>
        /// Rounds a single-precision floating-point value to a specified number of fractional digits, and rounds midpoint values to the nearest even number.
        /// </summary>
        /// <param name="x">The number to round.</param>
        /// <param name="digits">How many fractional digits to keep.</param>
        /// <returns>The rounded representation of <paramref name="x"/> with <paramref name="digits"/> fractional digits</returns>
        public static float Round(float x, int digits) => (float)Math.Round(x, digits);

        /// <summary>
        /// Rounds a single-precision floating-point value to a specified number of fractional digits using the specified rounding convention.
        /// </summary>
        /// <param name="x">The number to round.</param>
        /// <param name="digits">How many fractional digits to keep.</param>
        /// <param name="mode">The rounding convention to use.</param>
        /// <returns>The rounded representation of <paramref name="x"/> with <paramref name="digits"/> fractional digits using <paramref name="mode"/> rounding convention</returns>
        public static float Round(float x, int digits, MidpointRounding mode) => (float)Math.Round(x, digits, mode);

        /// <summary>
        /// Rounds a single-precision floating-point value to an integer using the specified rounding convention.
        /// </summary>
        /// <param name="x">The number to round.</param>
        /// <param name="mode">The rounding convention to use.</param>
        /// <returns>The rounded representation of <paramref name="x"/> using <paramref name="mode"/> rounding convention</returns>
        public static float Round(float x, MidpointRounding mode) => (float)Math.Round(x, mode);

        /// <summary>
        /// Returns an integer that indicates the sign of a single-precision floating-point number.
        /// </summary>
        /// <param name="x">The number check the sign of.</param>
        /// <returns>The sign of <paramref name="x"/></returns>
        public static int Sign(float x) => Math.Sign(x);

        /// <summary>
        /// Returns the sine of the specified angle.
        /// </summary>
        /// <param name="x">The angle to take the sine of.</param>
        /// <returns>The sine of <paramref name="x"/></returns>
        public static float Sin(float x) => (float)Math.Sin(x);

        /// <summary>
        /// Returns the hyperbolic sine of the specified angle.
        /// </summary>
        /// <param name="x">The angle to take the hyperbolic sine of.</param>
        /// <returns>The hyperbolic sine of <paramref name="x"/></returns>
        public static float Sinh(float x) => (float)Math.Sinh(x);

        /// <summary>
        /// Returns the square root of a specified number.
        /// </summary>
        /// <param name="x">The number to take the square root of.</param>
        /// <returns>The square root of <paramref name="x"/></returns>
        public static float Sqrt(float x) => (float)Math.Sqrt(x);

        /// <summary>
        /// Returns the tangent of the specified angle.
        /// </summary>
        /// <param name="x">The angle to take the tangent of.</param>
        /// <returns>The tangent of <paramref name="x"/></returns>
        public static float Tan(float x) => (float)Math.Tan(x);

        /// <summary>
        /// Returns the hyperbolic tangent of the specified angle.
        /// </summary>
        /// <param name="x">The angle to take the hyperbolic tangent of.</param>
        /// <returns>The hyperbolic tangent of <paramref name="x"/></returns>
        public static float Tanh(float x) => (float)Math.Tanh(x);

        /// <summary>
        /// Calculates the integral part of a specified single-precision floating-point number.
        /// </summary>
        /// <param name="x">The number to truncate.</param>
        /// <returns>The truncated representation of <paramref name="x"/></returns>
        public static float Truncate(float x) => (float)Math.Truncate(x);
    }
}
