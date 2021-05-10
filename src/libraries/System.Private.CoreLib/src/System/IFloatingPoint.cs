// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

#if !FEATURE_GENERIC_MATH
#error FEATURE_GENERIC_MATH is not defined
#endif

namespace System
{
    /// <summary>Defines a floating-point type.</summary>
    /// <typeparam name="TSelf">The type that implements the interface.</typeparam>
    [RequiresPreviewFeatures]
    public interface IFloatingPoint<TSelf>
        : ISignedNumber<TSelf>
        where TSelf : IFloatingPoint<TSelf>
    {
        /// <summary>Gets the mathematical constant <c>e</c>.</summary>
        static abstract TSelf E { get; }

        /// <summary>Gets the smallest value such that can be added to <c>0</c> that does not result in <c>0</c>.</summary>
        static abstract TSelf Epsilon { get; }

        /// <summary>Gets a value that represents <c>NaN</c>.</summary>
        static abstract TSelf NaN { get; }

        /// <summary>Gets a value that represents negative <c>infinity</c>.</summary>
        static abstract TSelf NegativeInfinity { get; }

        /// <summary>Gets a value that represents negative <c>zero</c>.</summary>
        static abstract TSelf NegativeZero { get; }

        /// <summary>Gets the mathematical constant <c>pi</c>.</summary>
        static abstract TSelf Pi { get; }

        /// <summary>Gets a value that represents positive <c>infinity</c>.</summary>
        static abstract TSelf PositiveInfinity { get; }

        /// <summary>Gets the mathematical constant <c>tau</c>.</summary>
        static abstract TSelf Tau { get; }

        /// <summary>Computes the arc-cosine of a value.</summary>
        /// <param name="x">The value, in radians, whose arc-cosine is to be computed.</param>
        /// <returns>The arc-cosine of <paramref name="x" />.</returns>
        static abstract TSelf Acos(TSelf x);

        /// <summary>Computes the hyperbolic arc-cosine of a value.</summary>
        /// <param name="x">The value, in radians, whose hyperbolic arc-cosine is to be computed.</param>
        /// <returns>The hyperbolic arc-cosine of <paramref name="x" />.</returns>
        static abstract TSelf Acosh(TSelf x);

        /// <summary>Computes the arc-sine of a value.</summary>
        /// <param name="x">The value, in radians, whose arc-sine is to be computed.</param>
        /// <returns>The arc-sine of <paramref name="x" />.</returns>
        static abstract TSelf Asin(TSelf x);

        /// <summary>Computes the hyperbolic arc-sine of a value.</summary>
        /// <param name="x">The value, in radians, whose hyperbolic arc-sine is to be computed.</param>
        /// <returns>The hyperbolic arc-sine of <paramref name="x" />.</returns>
        static abstract TSelf Asinh(TSelf x);

        /// <summary>Computes the arc-tangent of a value.</summary>
        /// <param name="x">The value, in radians, whose arc-tangent is to be computed.</param>
        /// <returns>The arc-tangent of <paramref name="x" />.</returns>
        static abstract TSelf Atan(TSelf x);

        /// <summary>Computes the arc-tangent of the quotient of two values.</summary>
        /// <param name="y">The y-coordinate of a point.</param>
        /// <param name="x">The x-coordinate of a point.</param>
        /// <returns>The arc-tangent of <paramref name="y" /> divided-by <paramref name="x" />.</returns>
        static abstract TSelf Atan2(TSelf y, TSelf x);

        /// <summary>Computes the hyperbolic arc-tangent of a value.</summary>
        /// <param name="x">The value, in radians, whose hyperbolic arc-tangent is to be computed.</param>
        /// <returns>The hyperbolic arc-tangent of <paramref name="x" />.</returns>
        static abstract TSelf Atanh(TSelf x);

        /// <summary>Decrements a value to the smallest value that compares less than a given value.</summary>
        /// <param name="x">The value to be bitwise decremented.</param>
        /// <returns>The smallest value that compares less than <paramref name="x" />.</returns>
        static abstract TSelf BitDecrement(TSelf x);

        /// <summary>Increments a value to the smallest value that compares greater than a given value.</summary>
        /// <param name="x">The value to be bitwise incremented.</param>
        /// <returns>The smallest value that compares greater than <paramref name="x" />.</returns>
        static abstract TSelf BitIncrement(TSelf x);

        /// <summary>Computes the cube-root of a value.</summary>
        /// <param name="x">The value whose cube-root is to be computed.</param>
        /// <returns>The cube-root of <paramref name="x" />.</returns>
        static abstract TSelf Cbrt(TSelf x);

        /// <summary>Computes the ceiling of a value.</summary>
        /// <param name="x">The value whose ceiling is to be computed.</param>
        /// <returns>The ceiling of <paramref name="x" />.</returns>
        static abstract TSelf Ceiling(TSelf x);

        /// <summary>Copies the sign of a value to the sign of another value..</summary>
        /// <param name="x">The value whose magnitude is used in the result.</param>
        /// <param name="y">The value whose sign is used in the result.</param>
        /// <returns>A value with the magnitude of <paramref name="x" /> and the sign of <paramref name="y" />.</returns>
        static abstract TSelf CopySign(TSelf x, TSelf y);

        /// <summary>Computes the cosine of a value.</summary>
        /// <param name="x">The value, in radians, whose cosine is to be computed.</param>
        /// <returns>The cosine of <paramref name="x" />.</returns>
        static abstract TSelf Cos(TSelf x);

        /// <summary>Computes the hyperbolic cosine of a value.</summary>
        /// <param name="x">The value, in radians, whose hyperbolic cosine is to be computed.</param>
        /// <returns>The hyperbolic cosine of <paramref name="x" />.</returns>
        static abstract TSelf Cosh(TSelf x);

        /// <summary>Computes <see cref="E" /> raised to a given power.</summary>
        /// <param name="x">The power to which <see cref="E" /> is raised.</param>
        /// <returns><see cref="E" /> raised to the power of <paramref name="x" />.</returns>
        static abstract TSelf Exp(TSelf x);

        /// <summary>Computes the floor of a value.</summary>
        /// <param name="x">The value whose floor is to be computed.</param>
        /// <returns>The floor of <paramref name="x" />.</returns>
        static abstract TSelf Floor(TSelf x);

        /// <summary>Computes the fused multiply-add of three values.</summary>
        /// <param name="left">The value which <paramref name="right" /> multiplies.</param>
        /// <param name="right">The value which multiplies <paramref name="left" />.</param>
        /// <param name="addend">The value that is added to the product of <paramref name="left" /> and <paramref name="right" />.</param>
        /// <returns>The result of <paramref name="left" /> times <paramref name="right" /> plus <paramref name="addend" /> computed as one ternary operation.</returns>
        static abstract TSelf FusedMultiplyAdd(TSelf left, TSelf right, TSelf addend);

        /// <summary>Computes the remainder of two values as specified by IEEE 754.</summary>
        /// <param name="left">The value which <paramref name="right" /> divides.</param>
        /// <param name="right">The value which divides <paramref name="left" />.</param>
        /// <returns>The remainder of <paramref name="left" /> divided-by <paramref name="right" /> as specified by IEEE 754.</returns>
        static abstract TSelf IEEERemainder(TSelf left, TSelf right);

        /// <summary>Computes the integer logarithm of a value.</summary>
        /// <param name="x">The value whose integer logarithm is to be computed.</param>
        /// <returns>The integer logarithm of <paramref name="x" />.</returns>
        static abstract TInteger ILogB<TInteger>(TSelf x)
            where TInteger : IBinaryInteger<TInteger>;

        /// <summary>Determines if a value is finite.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is finite; otherwise, <c>false</c>.</returns>
        static abstract bool IsFinite(TSelf value);

        /// <summary>Determines if a value is infinite.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is infinite; otherwise, <c>false</c>.</returns>
        static abstract bool IsInfinity(TSelf value);

        /// <summary>Determines if a value is NaN.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is NaN; otherwise, <c>false</c>.</returns>
        static abstract bool IsNaN(TSelf value);

        /// <summary>Determines if a value is negative.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is negative; otherwise, <c>false</c>.</returns>
        static abstract bool IsNegative(TSelf value);

        /// <summary>Determines if a value is negative infinity.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is negative infinity; otherwise, <c>false</c>.</returns>
        static abstract bool IsNegativeInfinity(TSelf value);

        /// <summary>Determines if a value is normal.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is normal; otherwise, <c>false</c>.</returns>
        static abstract bool IsNormal(TSelf value);

        /// <summary>Determines if a value is positive infinity.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is positive infinity; otherwise, <c>false</c>.</returns>
        static abstract bool IsPositiveInfinity(TSelf value);

        /// <summary>Determines if a value is subnormal.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is subnormal; otherwise, <c>false</c>.</returns>
        static abstract bool IsSubnormal(TSelf value);

        /// <summary>Computes the natural (<c>base-<see cref="E" /></c> logarithm of a value.</summary>
        /// <param name="x">The value whose natural logarithm is to be computed.</param>
        /// <returns>The natural logarithm of <paramref name="x" />.</returns>
        static abstract TSelf Log(TSelf x);

        /// <summary>Computes the logarithm of a value in the specified base.</summary>
        /// <param name="x">The value whose logarithm is to be computed.</param>
        /// <param name="newBase">The base in which the logarithm is to be computed.</param>
        /// <returns>The base-<paramref name="newBase" /> logarithm of <paramref name="x" />.</returns>
        static abstract TSelf Log(TSelf x, TSelf newBase);

        /// <summary>Computes the base-2 logarithm of a value.</summary>
        /// <param name="x">The value whose base-2 logarithm is to be computed.</param>
        /// <returns>The base-2 logarithm of <paramref name="x" />.</returns>
        static abstract TSelf Log2(TSelf x);

        /// <summary>Computes the base-10 logarithm of a value.</summary>
        /// <param name="x">The value whose base-10 logarithm is to be computed.</param>
        /// <returns>The base-10 logarithm of <paramref name="x" />.</returns>
        static abstract TSelf Log10(TSelf x);

        /// <summary>Compares two values to compute which is greater.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is greater than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        /// <remarks>For <see cref="IFloatingPoint{TSelf}" /> this method matches the IEEE 754:2019 <c>maximumMagnitude</c> function. This requires NaN inputs to not be propagated back to the caller and for <c>-0.0</c> to be treated as less than <c>+0.0</c>.</remarks>
        static abstract TSelf MaxMagnitude(TSelf x, TSelf y);

        /// <summary>Compares two values to compute which is lesser.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is less than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        /// <remarks>For <see cref="IFloatingPoint{TSelf}" /> this method matches the IEEE 754:2019 <c>minimumMagnitude</c> function. This requires NaN inputs to not be propagated back to the caller and for <c>-0.0</c> to be treated as less than <c>+0.0</c>.</remarks>
        static abstract TSelf MinMagnitude(TSelf x, TSelf y);

        /// <summary>Computes a value raised to a given power.</summary>
        /// <param name="x">The value which is raised to the power of <paramref name="x" />.</param>
        /// <param name="y">The power to which <paramref name="x" /> is raised.</param>
        /// <returns><see cref="E" /> raised to the power of <paramref name="x" />.</returns>
        static abstract TSelf Pow(TSelf x, TSelf y);

        /// <summary>Rounds a value to the nearest integer using the default rounding mode (<see cref="MidpointRounding.ToEven" />).</summary>
        /// <param name="x">The value to round.</param>
        /// <returns>The result of rounding <paramref name="x" /> to the nearest integer using the default rounding mode.</returns>
        static abstract TSelf Round(TSelf x);

        /// <summary>Rounds a value to a specified number of fractional-digits using the default rounding mode (<see cref="MidpointRounding.ToEven" />).</summary>
        /// <param name="x">The value to round.</param>
        /// <param name="digits">The number of fractional digits to which <paramref name="x" /> should be rounded.</param>
        /// <returns>The result of rounding <paramref name="x" /> to <paramref name="digits" /> fractional-digits using the default rounding mode.</returns>
        static abstract TSelf Round<TInteger>(TSelf x, TInteger digits)
            where TInteger : IBinaryInteger<TInteger>;

        /// <summary>Rounds a value to the nearest integer using the specified rounding mode.</summary>
        /// <param name="x">The value to round.</param>
        /// <param name="mode">The mode under which <paramref name="x" /> should be rounded.</param>
        /// <returns>The result of rounding <paramref name="x" /> to the nearest integer using <paramref name="mode" />.</returns>
        static abstract TSelf Round(TSelf x, MidpointRounding mode);

        /// <summary>Rounds a value to a specified number of fractional-digits using the default rounding mode (<see cref="MidpointRounding.ToEven" />).</summary>
        /// <param name="x">The value to round.</param>
        /// <param name="digits">The number of fractional digits to which <paramref name="x" /> should be rounded.</param>
        /// <param name="mode">The mode under which <paramref name="x" /> should be rounded.</param>
        /// <returns>The result of rounding <paramref name="x" /> to <paramref name="digits" /> fractional-digits using <paramref name="mode" />.</returns>
        static abstract TSelf Round<TInteger>(TSelf x, TInteger digits, MidpointRounding mode)
            where TInteger : IBinaryInteger<TInteger>;

        /// <summary>Computes the product of a value and its base-radix raised to the specified power.</summary>
        /// <param name="x">The value which base-radix raised to the power of <paramref name="n" /> multiplies.</param>
        /// <param name="n">The value to which base-radix is raised before multipliying <paramref name="x" />.</param>
        /// <returns>The product of <paramref name="x" /> and base-radix raised to the power of <paramref name="n" />.</returns>
        static abstract TSelf ScaleB<TInteger>(TSelf x, TInteger n)
            where TInteger : IBinaryInteger<TInteger>;

        /// <summary>Computes the sine of a value.</summary>
        /// <param name="x">The value, in radians, whose sine is to be computed.</param>
        /// <returns>The sine of <paramref name="x" />.</returns>
        static abstract TSelf Sin(TSelf x);

        /// <summary>Computes the hyperbolic sine of a value.</summary>
        /// <param name="x">The value, in radians, whose hyperbolic sine is to be computed.</param>
        /// <returns>The hyperbolic sine of <paramref name="x" />.</returns>
        static abstract TSelf Sinh(TSelf x);

        /// <summary>Computes the square-root of a value.</summary>
        /// <param name="x">The value whose square-root is to be computed.</param>
        /// <returns>The square-root of <paramref name="x" />.</returns>
        static abstract TSelf Sqrt(TSelf x);

        /// <summary>Computes the tangent of a value.</summary>
        /// <param name="x">The value, in radians, whose tangent is to be computed.</param>
        /// <returns>The tangent of <paramref name="x" />.</returns>
        static abstract TSelf Tan(TSelf x);

        /// <summary>Computes the hyperbolic tangent of a value.</summary>
        /// <param name="x">The value, in radians, whose hyperbolic tangent is to be computed.</param>
        /// <returns>The hyperbolic tangent of <paramref name="x" />.</returns>
        static abstract TSelf Tanh(TSelf x);

        /// <summary>Truncates a value.</summary>
        /// <param name="x">The value to truncate.</param>
        /// <returns>The truncation of <paramref name="x" />.</returns>
        static abstract TSelf Truncate(TSelf x);

        // static abstract TSelf AcosPi(TSelf x);
        //
        // static abstract TSelf AsinPi(TSelf x);
        //
        // static abstract TSelf AtanPi(TSelf x);
        //
        // static abstract TSelf Atan2Pi(TSelf y, TSelf x);
        //
        // static abstract TSelf Compound(TSelf x, TSelf n);
        //
        // static abstract TSelf CosPi(TSelf x);
        //
        // static abstract TSelf ExpM1(TSelf x);
        //
        // static abstract TSelf Exp2(TSelf x);
        //
        // static abstract TSelf Exp2M1(TSelf x);
        //
        // static abstract TSelf Exp10(TSelf x);
        //
        // static abstract TSelf Exp10M1(TSelf x);
        //
        // static abstract TSelf Hypot(TSelf x, TSelf y);
        //
        // static abstract TSelf LogP1(TSelf x);
        //
        // static abstract TSelf Log2P1(TSelf x);
        //
        // static abstract TSelf Log10P1(TSelf x);
        //
        // static abstract TSelf MaxMagnitudeNumber(TSelf x, TSelf y);
        //
        // static abstract TSelf MaxNumber(TSelf x, TSelf y);
        //
        // static abstract TSelf MinMagnitudeNumber(TSelf x, TSelf y);
        //
        // static abstract TSelf MinNumber(TSelf x, TSelf y);
        //
        // static abstract TSelf Root(TSelf x, TSelf n);
        //
        // static abstract TSelf SinPi(TSelf x);
        //
        // static abstract TSelf TanPi(TSelf x);
    }

    /// <summary>Defines a floating-point type that is represented in a base-2 format.</summary>
    /// <typeparam name="TSelf">The type that implements the interface.</typeparam>
    [RequiresPreviewFeatures]
    public interface IBinaryFloatingPoint<TSelf>
        : IBinaryNumber<TSelf>,
          IFloatingPoint<TSelf>
        where TSelf : IBinaryFloatingPoint<TSelf>
    {
    }
}
