// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    /// <summary>Defines a mechanism for shifting a value by another value.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    /// <typeparam name="TResult">The type that contains the result of shifting <typeparamref name="TSelf" /> by <typeparamref name="TResult" />.</typeparam>
    public interface IShiftOperators<TSelf, TResult>
        where TSelf : IShiftOperators<TSelf, TResult>
    {
        /// <summary>Shifts a value left by a given amount.</summary>
        /// <param name="value">The value which is shifted left by <paramref name="shiftAmount" />.</param>
        /// <param name="shiftAmount">The amount by which <paramref name="value" /> is shifted left.</param>
        /// <returns>The result of shifting <paramref name="value" /> left by <paramref name="shiftAmount" />.</returns>
        static abstract TResult operator <<(TSelf value, int shiftAmount); // TODO_GENERIC_MATH: shiftAmount should be TOther

        /// <summary>Shifts a value right by a given amount.</summary>
        /// <param name="value">The value which is shifted right by <paramref name="shiftAmount" />.</param>
        /// <param name="shiftAmount">The amount by which <paramref name="value" /> is shifted right.</param>
        /// <returns>The result of shifting <paramref name="value" /> right by <paramref name="shiftAmount" />.</returns>
        /// <remarks>This operation is meant to perform a signed (otherwise known as an arithmetic) right shift on signed types.</remarks>
        static abstract TResult operator >>(TSelf value, int shiftAmount); // TODO_GENERIC_MATH: shiftAmount should be TOther

        // /// <summary>Shifts a value right by a given amount.</summary>
        // /// <param name="value">The value which is shifted right by <paramref name="shiftAmount" />.</param>
        // /// <param name="shiftAmount">The amount by which <paramref name="value" /> is shifted right.</param>
        // /// <returns>The result of shifting <paramref name="value" /> right by <paramref name="shiftAmount" />.</returns>
        // /// <remarks>This operation is meant to perform n unsigned (otherwise known as a logical) right shift on all types.</remarks>
        // static abstract TResult operator >>>(TSelf value, int shiftAmount); // TODO_GENERIC_MATH: shiftAmount should be TOther
    }
}
