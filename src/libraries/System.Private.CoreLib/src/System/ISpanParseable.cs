// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    /// <summary>Defines a mechanism for parsing a span of characters to a value.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    public interface ISpanParseable<TSelf> : IParseable<TSelf>
        where TSelf : ISpanParseable<TSelf>
    {
        /// <summary>Parses a span of characters into a value.</summary>
        /// <param name="s">The span of characters to parse.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s" />.</param>
        /// <returns>The result of parsing <paramref name="s" />.</returns>
        /// <exception cref="FormatException"><paramref name="s" /> is not in the correct format.</exception>
        /// <exception cref="OverflowException"><paramref name="s" /> is not representable by <typeparamref name="TSelf" />.</exception>
        static abstract TSelf Parse(ReadOnlySpan<char> s, IFormatProvider? provider);

        /// <summary>Tries to parses a span of characters into a value.</summary>
        /// <param name="s">The span of characters to parse.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s" />.</param>
        /// <param name="result">On return, contains the result of succesfully parsing <paramref name="s" /> or an undefined value on failure.</param>
        /// <returns><c>true</c> if <paramref name="s" /> was successfully parsed; otherwise, <c>false</c>.</returns>
        static abstract bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out TSelf result);
    }
}
