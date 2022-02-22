// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    /// <summary>Defines an integer type that is represented in a base-2 format.</summary>
    /// <typeparam name="TSelf">The type that implements the interface.</typeparam>
    public interface IBinaryInteger<TSelf>
        : IBinaryNumber<TSelf>,
          IShiftOperators<TSelf, TSelf>
        where TSelf : IBinaryInteger<TSelf>
    {
        /// <summary>Computes the number of leading zeros in a value.</summary>
        /// <param name="value">The value whose leading zeroes are to be counted.</param>
        /// <returns>The number of leading zeros in <paramref name="value" />.</returns>
        static abstract TSelf LeadingZeroCount(TSelf value);

        /// <summary>Computes the number of bits that are set in a value.</summary>
        /// <param name="value">The value whose set bits are to be counted.</param>
        /// <returns>The number of set bits in <paramref name="value" />.</returns>
        static abstract TSelf PopCount(TSelf value);

        /// <summary>Rotates a value left by a given amount.</summary>
        /// <param name="value">The value which is rotated left by <paramref name="rotateAmount" />.</param>
        /// <param name="rotateAmount">The amount by which <paramref name="value" /> is rotated left.</param>
        /// <returns>The result of rotating <paramref name="value" /> left by <paramref name="rotateAmount" />.</returns>
        static abstract TSelf RotateLeft(TSelf value, int rotateAmount);

        /// <summary>Rotates a value right by a given amount.</summary>
        /// <param name="value">The value which is rotated right by <paramref name="rotateAmount" />.</param>
        /// <param name="rotateAmount">The amount by which <paramref name="value" /> is rotated right.</param>
        /// <returns>The result of rotating <paramref name="value" /> right by <paramref name="rotateAmount" />.</returns>
        static abstract TSelf RotateRight(TSelf value, int rotateAmount);

        /// <summary>Computes the number of trailing zeros in a value.</summary>
        /// <param name="value">The value whose trailing zeroes are to be counted.</param>
        /// <returns>The number of trailing zeros in <paramref name="value" />.</returns>
        static abstract TSelf TrailingZeroCount(TSelf value);
    }
}
