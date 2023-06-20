// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#if !NETCOREAPP3_1_OR_GREATER
using System.Buffers;
#endif

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// Enables the redaction of potentially sensitive data.
/// </summary>
public abstract class Redactor
{
#if NET6_0_OR_GREATER
    private const int MaximumStackAllocation = 256;
#endif

    /// <summary>
    /// Redacts potentially sensitive data.
    /// </summary>
    /// <param name="source">Value to redact.</param>
    /// <returns>Redacted value.</returns>
    public string Redact(ReadOnlySpan<char> source)
    {
        if (source.IsEmpty)
        {
            return string.Empty;
        }

        var length = GetRedactedLength(source);

#if NETCOREAPP3_1_OR_GREATER
        unsafe
        {
#pragma warning disable 8500
            return string.Create(
                length,
                (this, (IntPtr)(&source)),
                (destination, state) => state.Item1.Redact(*(ReadOnlySpan<char>*)state.Item2, destination));
#pragma warning restore 8500
        }
#else
        var buffer = ArrayPool<char>.Shared.Rent(length);

        try
        {
            var charsWritten = Redact(source, buffer);
            var redactedString = new string(buffer, 0, charsWritten);

            return redactedString;
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
#endif
    }

    /// <summary>
    /// Redacts potentially sensitive data.
    /// </summary>
    /// <param name="source">Value to redact.</param>
    /// <param name="destination">Buffer to store redacted value.</param>
    /// <returns>Number of characters produced when redacting the given source input.</returns>
    /// <exception cref="ArgumentException"><paramref name="destination"/> is too small.</exception>
    public abstract int Redact(ReadOnlySpan<char> source, Span<char> destination);

    /// <summary>
    /// Redacts potentially sensitive data.
    /// </summary>
    /// <param name="source">Value to redact.</param>
    /// <param name="destination">Buffer to redact into.</param>
    /// <remarks>
    /// Returns 0 when <paramref name="source"/> is <see langword="null"/>.
    /// </remarks>
    /// <returns>Number of characters written to the buffer.</returns>
    /// <exception cref="ArgumentException"><paramref name="destination"/> is too small.</exception>
    public int Redact(string? source, Span<char> destination) => Redact(source.AsSpan(), destination);

    /// <summary>
    /// Redacts potentially sensitive data.
    /// </summary>
    /// <param name="source">Value to redact.</param>
    /// <returns>Redacted value.</returns>
    /// <remarks>
    /// Returns an empty string when <paramref name="source"/> is <see langword="null"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public virtual string Redact(string? source) => Redact(source.AsSpan());

    /// <summary>
    /// Redacts potentially sensitive data.
    /// </summary>
    /// <typeparam name="T">Type of value to redact.</typeparam>
    /// <param name="value">Value to redact.</param>
    /// <param name="format">
    /// The optional format that selects the specific formatting operation performed. Refer to the
    /// documentation of the type being formatted to understand the values you can supply here.
    /// </param>
    /// <param name="provider">Format provider to retrieve format for span formattable.</param>
    /// <returns>Redacted value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    [SuppressMessage("Minor Code Smell", "S3247:Duplicate casts should not be made", Justification = "Avoid pattern matching to improve jitted code")]
    public string Redact<T>(T value, string? format = null, IFormatProvider? provider = null)
    {
#if NET6_0_OR_GREATER
        if (value is ISpanFormattable)
        {
            Span<char> buffer = stackalloc char[MaximumStackAllocation];

            // Stryker disable all : Cannot kill the mutant because the only difference is allocating buffer on stack or renting it.
            // Null forgiving operator: The null case is checked with default equality comparer, but compiler doesn't understand it.
            if (((ISpanFormattable)value).TryFormat(buffer, out var written, format.AsSpan(), provider))
            {
                // Stryker enable all : Cannot kill the mutant because the only difference is allocating buffer on stack or renting it.

                var formatted = buffer.Slice(0, written);
                var length = GetRedactedLength(formatted);

                unsafe
                {
#pragma warning disable 8500
                    return string.Create(
                        length,
                        (this, (IntPtr)(&formatted)),
                        (destination, state) => state.Item1.Redact(*(ReadOnlySpan<char>*)state.Item2, destination));
#pragma warning restore 8500
                }
            }
        }
#endif

        if (value is IFormattable)
        {
            return Redact(((IFormattable)value).ToString(format, provider));
        }

        return Redact(value?.ToString());
    }

    /// <summary>
    /// Redacts potentially sensitive data.
    /// </summary>
    /// <typeparam name="T">Type of value to redact.</typeparam>
    /// <param name="value">Value to redact.</param>
    /// <param name="destination">Buffer to redact into.</param>
    /// <param name="format">
    /// The optional format string that selects the specific formatting operation performed. Refer to the
    /// documentation of the type being formatted to understand the values you can supply here.
    /// </param>
    /// <param name="provider">Format provider to retrieve format for span formattable.</param>
    /// <returns>Number of characters written to the buffer.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    [SuppressMessage("Minor Code Smell", "S3247:Duplicate casts should not be made", Justification = "Avoid pattern matching to improve jitted code")]
    public int Redact<T>(T value, Span<char> destination, string? format = null, IFormatProvider? provider = null)
    {
#if NET6_0_OR_GREATER
        if (value is ISpanFormattable)
        {
            Span<char> buffer = stackalloc char[MaximumStackAllocation];

            // Stryker disable all : Cannot kill the mutant because the only difference is allocating buffer on stack or renting it.
            if (((ISpanFormattable)value).TryFormat(buffer, out var written, format.AsSpan(), provider))
            {
                // Stryker enable all : Cannot kill the mutant because the only difference is allocating buffer on stack or renting it.
                var formatted = buffer.Slice(0, written);

                return Redact(formatted, destination);
            }
        }
#endif

        if (value is IFormattable)
        {
            return Redact(((IFormattable)value).ToString(format, provider), destination);
        }

        return Redact(value?.ToString(), destination);
    }

    /// <summary>
    /// Gets the number of characters produced by redacting the input.
    /// </summary>
    /// <param name="input">Value to be redacted.</param>
    /// <returns>Minimum buffer size.</returns>
    public abstract int GetRedactedLength(ReadOnlySpan<char> input);

    /// <summary>
    /// Gets the number of characters produced by redacting the input.
    /// </summary>
    /// <param name="input">Value to be redacted.</param>
    /// <returns>Minimum buffer size.</returns>
    public int GetRedactedLength(string? input) => GetRedactedLength(input.AsSpan());
}
