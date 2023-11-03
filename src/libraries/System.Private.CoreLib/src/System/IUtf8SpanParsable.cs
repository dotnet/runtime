// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System
{
    /// <summary>Defines a mechanism for parsing a span of UTF-8 characters to a value.</summary>
    /// <typeparam name="TSelf">The type that implements this interface.</typeparam>
    public interface IUtf8SpanParsable<TSelf>
        where TSelf : IUtf8SpanParsable<TSelf>?
    {
        /// <summary>Parses a span of UTF-8 characters into a value.</summary>
        /// <param name="utf8Text">The span of UTF-8 characters to parse.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="utf8Text" />.</param>
        /// <returns>The result of parsing <paramref name="utf8Text" />.</returns>
        /// <exception cref="FormatException"><paramref name="utf8Text" /> is not in the correct format.</exception>
        /// <exception cref="OverflowException"><paramref name="utf8Text" /> is not representable by <typeparamref name="TSelf" />.</exception>
        static abstract TSelf Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider);

        /// <summary>Tries to parse a span of UTF-8 characters into a value.</summary>
        /// <param name="utf8Text">The span of UTF-8 characters to parse.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about <paramref name="utf8Text" />.</param>
        /// <param name="result">On return, contains the result of successfully parsing <paramref name="utf8Text" /> or an undefined value on failure.</param>
        /// <returns><c>true</c> if <paramref name="utf8Text" /> was successfully parsed; otherwise, <c>false</c>.</returns>
        static abstract bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(returnValue: false)] out TSelf result);
    }
}
