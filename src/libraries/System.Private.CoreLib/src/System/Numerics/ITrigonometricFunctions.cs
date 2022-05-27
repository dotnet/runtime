// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>Defines support for trigonometric functions.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    public interface ITrigonometricFunctions<TSelf>
        where TSelf : ITrigonometricFunctions<TSelf>
    {
        /// <summary>Computes the arc-cosine of a value.</summary>
        /// <param name="x">The value, in radians, whose arc-cosine is to be computed.</param>
        /// <returns>The arc-cosine of <paramref name="x" />.</returns>
        static abstract TSelf Acos(TSelf x);

        /// <summary>Computes the arc-sine of a value.</summary>
        /// <param name="x">The value, in radians, whose arc-sine is to be computed.</param>
        /// <returns>The arc-sine of <paramref name="x" />.</returns>
        static abstract TSelf Asin(TSelf x);

        /// <summary>Computes the arc-tangent of a value.</summary>
        /// <param name="x">The value, in radians, whose arc-tangent is to be computed.</param>
        /// <returns>The arc-tangent of <paramref name="x" />.</returns>
        static abstract TSelf Atan(TSelf x);

        /// <summary>Computes the arc-tangent of the quotient of two values.</summary>
        /// <param name="y">The y-coordinate of a point.</param>
        /// <param name="x">The x-coordinate of a point.</param>
        /// <returns>The arc-tangent of <paramref name="y" /> divided-by <paramref name="x" />.</returns>
        static abstract TSelf Atan2(TSelf y, TSelf x);

        /// <summary>Computes the cosine of a value.</summary>
        /// <param name="x">The value, in radians, whose cosine is to be computed.</param>
        /// <returns>The cosine of <paramref name="x" />.</returns>
        static abstract TSelf Cos(TSelf x);

        /// <summary>Computes the sine of a value.</summary>
        /// <param name="x">The value, in radians, whose sine is to be computed.</param>
        /// <returns>The sine of <paramref name="x" />.</returns>
        static abstract TSelf Sin(TSelf x);

        /// <summary>Computes the sine and cosine of a value.</summary>
        /// <param name="x">The value, in radians, whose sine and cosine are to be computed.</param>
        /// <returns>The sine and cosine of <paramref name="x" />.</returns>
        static abstract (TSelf Sin, TSelf Cos) SinCos(TSelf x);

        /// <summary>Computes the tangent of a value.</summary>
        /// <param name="x">The value, in radians, whose tangent is to be computed.</param>
        /// <returns>The tangent of <paramref name="x" />.</returns>
        static abstract TSelf Tan(TSelf x);

        // The following methods are approved but not yet implemented in the libraries
        // * static abstract TSelf AcosPi(TSelf x);
        // * static abstract TSelf AsinPi(TSelf x);
        // * static abstract TSelf AtanPi(TSelf x);
        // * static abstract TSelf Atan2Pi(TSelf y, TSelf x);
        // * static abstract TSelf CosPi(TSelf x);
        // * static abstract TSelf SinPi(TSelf x);
        // * static abstract TSelf TanPi(TSelf x);
    }
}
