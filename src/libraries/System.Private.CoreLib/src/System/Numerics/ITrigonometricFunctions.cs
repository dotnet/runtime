// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>Defines support for trigonometric functions.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    public interface ITrigonometricFunctions<TSelf>
        : IFloatingPointConstants<TSelf>
        where TSelf : ITrigonometricFunctions<TSelf>
    {
        /// <summary>Computes the arc-cosine of a value.</summary>
        /// <param name="x">The value whose arc-cosine is to be computed.</param>
        /// <returns>The arc-cosine of <paramref name="x" />.</returns>
        /// <remarks>This computes <c>arccos(x)</c> in the interval <c>[+0, +π]</c> radians.</remarks>
        static abstract TSelf Acos(TSelf x);

        /// <summary>Computes the arc-cosine of a value and divides the result by <c>pi</c>.</summary>
        /// <param name="x">The value whose arc-cosine is to be computed.</param>
        /// <returns>The arc-cosine of <paramref name="x" />, divided by <c>pi</c>.</returns>
        /// <remarks>This computes <c>arccos(x) / π</c> in the interval <c>[-0.5, +0.5]</c>.</remarks>
        static abstract TSelf AcosPi(TSelf x);

        /// <summary>Computes the arc-sine of a value.</summary>
        /// <param name="x">The value whose arc-sine is to be computed.</param>
        /// <returns>The arc-sine of <paramref name="x" />.</returns>
        /// <remarks>This computes <c>arcsin(x)</c> in the interval <c>[-π / 2, +π / 2]</c> radians.</remarks>
        static abstract TSelf Asin(TSelf x);

        /// <summary>Computes the arc-sine of a value and divides the result by <c>pi</c>.</summary>
        /// <param name="x">The value whose arc-sine is to be computed.</param>
        /// <returns>The arc-sine of <paramref name="x" />, divided by <c>pi</c>.</returns>
        /// <remarks>This computes <c>arcsin(x) / π</c> in the interval <c>[-0.5, +0.5]</c>.</remarks>
        static abstract TSelf AsinPi(TSelf x);

        /// <summary>Computes the arc-tangent of a value.</summary>
        /// <param name="x">The value whose arc-tangent is to be computed.</param>
        /// <returns>The arc-tangent of <paramref name="x" />.</returns>
        /// <remarks>This computes <c>arctan(x)</c> in the interval <c>[-π / 2, +π / 2]</c> radians.</remarks>
        static abstract TSelf Atan(TSelf x);

        /// <summary>Computes the arc-tangent of a value and divides the result by pi.</summary>
        /// <param name="x">The value whose arc-tangent is to be computed.</param>
        /// <returns>The arc-tangent of <paramref name="x" />, divided by <c>pi</c>.</returns>
        /// <remarks>This computes <c>arctan(x) / π</c> in the interval <c>[-0.5, +0.5]</c>.</remarks>
        static abstract TSelf AtanPi(TSelf x);

        /// <summary>Computes the cosine of a value.</summary>
        /// <param name="x">The value, in radians, whose cosine is to be computed.</param>
        /// <returns>The cosine of <paramref name="x" />.</returns>
        /// <remarks>This computes <c>cos(x)</c>.</remarks>
        static abstract TSelf Cos(TSelf x);

        /// <summary>Computes the cosine of a value that has been multipled by <c>pi</c>.</summary>
        /// <param name="x">The value, in half-revolutions, whose cosine is to be computed.</param>
        /// <returns>The cosine of <paramref name="x" /> multiplied-by <c>pi</c>.</returns>
        /// <remarks>This computes <c>cos(x * π)</c>.</remarks>
        static abstract TSelf CosPi(TSelf x);

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
        /// <remarks>This computes <c>(sin(x * π), cos(x * π))</c>.</remarks>
        static abstract (TSelf SinPi, TSelf CosPi) SinCosPi(TSelf x);

        /// <summary>Computes the sine of a value that has been multiplied by <c>pi</c>.</summary>
        /// <param name="x">The value, in half-revolutions, that is multipled by <c>pi</c> before computing its sine.</param>
        /// <returns>The sine of <paramref name="x" /> multiplied-by <c>pi</c>.</returns>
        /// <remarks>This computes <c>sin(x * π)</c>.</remarks>
        static abstract TSelf SinPi(TSelf x);

        /// <summary>Computes the tangent of a value.</summary>
        /// <param name="x">The value, in radians, whose tangent is to be computed.</param>
        /// <returns>The tangent of <paramref name="x" />.</returns>
        /// <remarks>This computes <c>tan(x)</c>.</remarks>
        static abstract TSelf Tan(TSelf x);

        /// <summary>Computes the tangent of a value that has been multipled by <c>pi</c>.</summary>
        /// <param name="x">The value, in half-revolutions, that is multipled by <c>pi</c> before computing its tangent.</param>
        /// <returns>The tangent of <paramref name="x" /> multiplied-by <c>pi</c>.</returns>
        /// <remarks>This computes <c>tan(x * π)</c>.</remarks>
        static abstract TSelf TanPi(TSelf x);
    }
}
