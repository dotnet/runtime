// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>Defines support for trigonometric functions.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    public interface ITrigonometricFunctions<TSelf>
        : IFloatingPointConstants<TSelf>
        where TSelf : ITrigonometricFunctions<TSelf>?
    {
        /// <summary>Computes the arc-cosine of a value.</summary>
        /// <param name="x">The value whose arc-cosine is to be computed.</param>
        /// <returns>The arc-cosine of <paramref name="x" />.</returns>
        /// <remarks>This computes <c>arccos(x)</c> in the interval <c>[+0, +PI]</c> radians.</remarks>
        static abstract TSelf Acos(TSelf x);

        /// <summary>Computes the arc-cosine of a value and divides the result by <c>pi</c>.</summary>
        /// <param name="x">The value whose arc-cosine is to be computed.</param>
        /// <returns>The arc-cosine of <paramref name="x" />, divided by <c>pi</c>.</returns>
        /// <remarks>This computes <c>arccos(x) / PI</c> in the interval <c>[-0.5, +0.5]</c>.</remarks>
        static abstract TSelf AcosPi(TSelf x);

        /// <summary>Computes the arc-sine of a value.</summary>
        /// <param name="x">The value whose arc-sine is to be computed.</param>
        /// <returns>The arc-sine of <paramref name="x" />.</returns>
        /// <remarks>This computes <c>arcsin(x)</c> in the interval <c>[-PI / 2, +PI / 2]</c> radians.</remarks>
        static abstract TSelf Asin(TSelf x);

        /// <summary>Computes the arc-sine of a value and divides the result by <c>pi</c>.</summary>
        /// <param name="x">The value whose arc-sine is to be computed.</param>
        /// <returns>The arc-sine of <paramref name="x" />, divided by <c>pi</c>.</returns>
        /// <remarks>This computes <c>arcsin(x) / PI</c> in the interval <c>[-0.5, +0.5]</c>.</remarks>
        static abstract TSelf AsinPi(TSelf x);

        /// <summary>Computes the arc-tangent of a value.</summary>
        /// <param name="x">The value whose arc-tangent is to be computed.</param>
        /// <returns>The arc-tangent of <paramref name="x" />.</returns>
        /// <remarks>This computes <c>arctan(x)</c> in the interval <c>[-PI / 2, +PI / 2]</c> radians.</remarks>
        static abstract TSelf Atan(TSelf x);

        /// <summary>Computes the arc-tangent of a value and divides the result by pi.</summary>
        /// <param name="x">The value whose arc-tangent is to be computed.</param>
        /// <returns>The arc-tangent of <paramref name="x" />, divided by <c>pi</c>.</returns>
        /// <remarks>This computes <c>arctan(x) / PI</c> in the interval <c>[-0.5, +0.5]</c>.</remarks>
        static abstract TSelf AtanPi(TSelf x);

        /// <summary>Computes the cosine of a value.</summary>
        /// <param name="x">The value, in radians, whose cosine is to be computed.</param>
        /// <returns>The cosine of <paramref name="x" />.</returns>
        /// <remarks>This computes <c>cos(x)</c>.</remarks>
        static abstract TSelf Cos(TSelf x);

        /// <summary>Computes the cosine of a value that has been multipled by <c>pi</c>.</summary>
        /// <param name="x">The value, in half-revolutions, whose cosine is to be computed.</param>
        /// <returns>The cosine of <paramref name="x" /> multiplied-by <c>pi</c>.</returns>
        /// <remarks>This computes <c>cos(x * PI)</c>.</remarks>
        static abstract TSelf CosPi(TSelf x);

        /// <summary>Converts a given value from degrees to radians.</summary>
        /// <param name="degrees">The value to convert to radians.</param>
        /// <returns>The value of <paramref name="degrees" /> converted to radians.</returns>
        static virtual TSelf DegreesToRadians(TSelf degrees)
        {
            // We don't want to simplify this to: degrees * (Pi / 180)
            //
            // Doing so will change the result and in many cases will
            // cause a loss of precision. The main exception to this
            // is when the initial multiplication causes overflow, but
            // if we decide that should be handled in the future it needs
            // to be more explicit around how its done.
            //
            // Floating-point operations are naturally imprecise due to
            // rounding required to fit the "infinitely-precise result"
            // into the limits of the underlying representation. Because
            // of this, every operation can introduce some amount of rounding
            // error.
            //
            // For integers, the IEEE 754 binary floating-point types can
            // exactly represent them up to the 2^n, where n is the number
            // of bits in the significand. This is 10 for Half, 23 for Single,
            // and 52 for Double. As you approach this limit, the number of
            // digits available to represent the fractional portion decreases.
            //
            // For Half, you get around 3.311 total decimal digits of precision.
            // For Single, this is around 7.225 and around 15.955 for Double.
            //
            // The actual number of digits can be slightly more or less, depending.
            //
            // This means that values such as `Pi` are not exactly `Pi`, instead:
            // * Half:   3.14 0625
            // * Single: 3.14 1592 741012573 2421875
            // * Double: 3.14 1592 653589793 115997963468544185161590576171875
            // * Actual: 3.14 1592 653589793 2384626433832795028841971693993751058209749445923...
            //
            // If we were to simplify this to simply multiply by (Pi / 180), we get:
            // * Half:   0.01745 6054 6875
            // * Single: 0.01745 3292 384743690 49072265625
            // * Double: 0.01745 3292 519943295 4743716805978692718781530857086181640625
            // * Actual: 0.01745 3292 519943295 7692369076848861271344287188854172545609719144...
            //
            // Neither of these end up "perfect". There will be some cases where they will trade
            // in terms of closeness to the "infinitely precise result". Over the entire domain
            // however, doing the separate multiplications tends to produce overall more accurate
            // results. It helps ensure the implementation can be trivial for the DIM case, and
            // covers the vast majority of typical inputs more efficiently largely only pessimizing
            // the case where the first multiplication results in overflow.
            //
            // This is particularly true for `RadiansToDegrees` where 180 is exactly representable
            // and so allows an exactly representable intermediate value to be computed when overflow
            // doesn't occur.

            return (degrees * TSelf.Pi) / TSelf.CreateChecked(180);
        }

        /// <summary>Converts a given value from radians to degrees.</summary>
        /// <param name="radians">The value to convert to degrees.</param>
        /// <returns>The value of <paramref name="radians" /> converted to degrees.</returns>
        static virtual TSelf RadiansToDegrees(TSelf radians)
        {
            // We don't want to simplify this to: radians * (180 / Pi)
            // See DegreesToRadians for a longer explanation as to why

            return (radians * TSelf.CreateChecked(180)) / TSelf.Pi;
        }

        /// <summary>Computes the sine of a value.</summary>
        /// <param name="x">The value, in radians, whose sine is to be computed.</param>
        /// <returns>The sine of <paramref name="x" />.</returns>
        /// <remarks>This computes <c>sin(x)</c>.</remarks>
        static abstract TSelf Sin(TSelf x);

        /// <summary>Computes the sine and cosine of a value.</summary>
        /// <param name="x">The value, in radians, whose sine and cosine are to be computed.</param>
        /// <returns>The sine and cosine of <paramref name="x" />.</returns>
        /// <remarks>This computes <c>(sin(x), cos(x))</c>.</remarks>
        static abstract (TSelf Sin, TSelf Cos) SinCos(TSelf x);

        /// <summary>Computes the sine and cosine of a value that has been multiplied by <c>pi</c>.</summary>
        /// <param name="x">The value, in half-revolutions, that is multipled by <c>pi</c> before computing its sine and cosine.</param>
        /// <returns>The sine and cosine of<paramref name="x" /> multiplied-by <c>pi</c>.</returns>
        /// <remarks>This computes <c>(sin(x * PI), cos(x * PI))</c>.</remarks>
        static abstract (TSelf SinPi, TSelf CosPi) SinCosPi(TSelf x);

        /// <summary>Computes the sine of a value that has been multiplied by <c>pi</c>.</summary>
        /// <param name="x">The value, in half-revolutions, that is multipled by <c>pi</c> before computing its sine.</param>
        /// <returns>The sine of <paramref name="x" /> multiplied-by <c>pi</c>.</returns>
        /// <remarks>This computes <c>sin(x * PI)</c>.</remarks>
        static abstract TSelf SinPi(TSelf x);

        /// <summary>Computes the tangent of a value.</summary>
        /// <param name="x">The value, in radians, whose tangent is to be computed.</param>
        /// <returns>The tangent of <paramref name="x" />.</returns>
        /// <remarks>This computes <c>tan(x)</c>.</remarks>
        static abstract TSelf Tan(TSelf x);

        /// <summary>Computes the tangent of a value that has been multipled by <c>pi</c>.</summary>
        /// <param name="x">The value, in half-revolutions, that is multipled by <c>pi</c> before computing its tangent.</param>
        /// <returns>The tangent of <paramref name="x" /> multiplied-by <c>pi</c>.</returns>
        /// <remarks>This computes <c>tan(x * PI)</c>.</remarks>
        static abstract TSelf TanPi(TSelf x);
    }
}
