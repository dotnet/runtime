// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    /// <summary>Provides functionality to format the string representation of an object into a span as UTF8.</summary>
    public interface IUtf8SpanFormattable
    {
        /// <summary>Tries to format the value of the current instance as UTF8 into the provided span of bytes.</summary>
        /// <param name="utf8Destination">When this method returns, this instance's value formatted as a span of bytes.</param>
        /// <param name="bytesWritten">When this method returns, the number of bytes that were written in <paramref name="utf8Destination"/>.</param>
        /// <param name="format">A span containing the characters that represent a standard or custom format string that defines the acceptable format for <paramref name="utf8Destination"/>.</param>
        /// <param name="provider">An optional object that supplies culture-specific formatting information for <paramref name="utf8Destination"/>.</param>
        /// <returns><see langword="true"/> if the formatting was successful; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// An implementation of this interface should produce the same string of characters as an implementation of <see cref="IFormattable.ToString"/> or <see cref="ISpanFormattable.TryFormat"/>
        /// on the same type. TryFormat should return false only if there is not enough space in the destination buffer; any other failures should throw an exception.
        /// </remarks>
        bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider);
    }
}
