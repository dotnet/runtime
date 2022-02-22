// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System
{
    /// <summary>Defines a mechanism for parsing a string to a value.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    public interface IParseable<TSelf>
        where TSelf : IParseable<TSelf>
    {
        /// <summary>Parses a string into a value.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s" />.</param>
        /// <returns>The result of parsing <paramref name="s" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s" /> is <c>null</c>.</exception>
        /// <exception cref="FormatException"><paramref name="s" /> is not in the correct format.</exception>
        /// <exception cref="OverflowException"><paramref name="s" /> is not representable by <typeparamref name="TSelf" />.</exception>
        static abstract TSelf Parse(string s, IFormatProvider? provider);

        /// <summary>Tries to parses a string into a value.</summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="s" />.</param>
        /// <param name="result">On return, contains the result of succesfully parsing <paramref name="s" /> or an undefined value on failure.</param>
        /// <returns><c>true</c> if <paramref name="s" /> was successfully parsed; otherwise, <c>false</c>.</returns>
        static abstract bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out TSelf result);
    }
}
