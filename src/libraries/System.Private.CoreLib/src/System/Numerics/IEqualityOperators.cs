// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    /// <summary>Defines a mechanism for comparing two values to determine equality.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    /// <typeparam name="TOther">The type that will be compared with <typeparamref name="TSelf" />.</typeparam>
    /// <typeparam name="TResult">The type that is returned as a result of the comparison.</typeparam>
    public interface IEqualityOperators<TSelf, TOther, TResult>
        where TSelf : IEqualityOperators<TSelf, TOther, TResult>
    {
        /// <summary>Compares two values to determine equality.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is equal to <paramref name="right" />; otherwise, <c>false</c>.</returns>
        static abstract TResult operator ==(TSelf left, TOther right);

        /// <summary>Compares two values to determine inequality.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is not equal to <paramref name="right" />; otherwise, <c>false</c>.</returns>
        static abstract TResult operator !=(TSelf left, TOther right);
    }
}
