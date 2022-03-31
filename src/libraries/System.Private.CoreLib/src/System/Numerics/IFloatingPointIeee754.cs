// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>Defines an IEEE 754 floating-point type.</summary>
    /// <typeparam name="TSelf">The type that implements the interface.</typeparam>
    public interface IFloatingPointIeee754<TSelf>
        : IExponentialFunctions<TSelf>,
          IFloatingPoint<TSelf>,
          IHyperbolicFunctions<TSelf>,
          ILogarithmicFunctions<TSelf>,
          IPowerFunctions<TSelf>,
          IRootFunctions<TSelf>,
          ITrigonometricFunctions<TSelf>
        where TSelf : IFloatingPointIeee754<TSelf>
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

        /// <summary>Decrements a value to the smallest value that compares less than a given value.</summary>
        /// <param name="x">The value to be bitwise decremented.</param>
        /// <returns>The smallest value that compares less than <paramref name="x" />.</returns>
        static abstract TSelf BitDecrement(TSelf x);

        /// <summary>Increments a value to the smallest value that compares greater than a given value.</summary>
        /// <param name="x">The value to be bitwise incremented.</param>
        /// <returns>The smallest value that compares greater than <paramref name="x" />.</returns>
        static abstract TSelf BitIncrement(TSelf x);

        /// <summary>Copies the sign of a value to the sign of another value..</summary>
        /// <param name="x">The value whose magnitude is used in the result.</param>
        /// <param name="y">The value whose sign is used in the result.</param>
        /// <returns>A value with the magnitude of <paramref name="x" /> and the sign of <paramref name="y" />.</returns>
        static abstract TSelf CopySign(TSelf x, TSelf y);

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
        static abstract TSelf Ieee754Remainder(TSelf left, TSelf right);

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

        /// <summary>Compares two values to compute which is greater.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is greater than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        /// <remarks>For <see cref="IFloatingPointIeee754{TSelf}" /> this method matches the IEEE 754:2019 <c>maximumMagnitude</c> function. This requires NaN inputs to not be propagated back to the caller and for <c>-0.0</c> to be treated as less than <c>+0.0</c>.</remarks>
        static abstract TSelf MaxMagnitude(TSelf x, TSelf y);

        /// <summary>Compares two values to compute which is lesser.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is less than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        /// <remarks>For <see cref="IFloatingPointIeee754{TSelf}" /> this method matches the IEEE 754:2019 <c>minimumMagnitude</c> function. This requires NaN inputs to not be propagated back to the caller and for <c>-0.0</c> to be treated as less than <c>+0.0</c>.</remarks>
        static abstract TSelf MinMagnitude(TSelf x, TSelf y);

        // The following methods are approved but not yet implemented in the libraries
        // * static abstract TSelf Compound(TSelf x, TSelf n);
        // * static abstract TSelf MaxMagnitudeNumber(TSelf x, TSelf y);
        // * static abstract TSelf MaxNumber(TSelf x, TSelf y);
        // * static abstract TSelf MinMagnitudeNumber(TSelf x, TSelf y);
        // * static abstract TSelf MinNumber(TSelf x, TSelf y);
    }
}
