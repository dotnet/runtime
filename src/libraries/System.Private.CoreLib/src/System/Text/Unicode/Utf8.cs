// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text.Unicode
{
#if SYSTEM_PRIVATE_CORELIB
    public
#else
    internal
#endif
        static class Utf8
    {
        /*
         * OperationStatus-based APIs for transcoding of chunked data.
         * This method is similar to Encoding.UTF8.GetBytes / GetChars but has a
         * different calling convention, different error handling mechanisms, and
         * different performance characteristics.
         *
         * If 'replaceInvalidSequences' is true, the method will replace any ill-formed
         * subsequence in the source with U+FFFD when transcoding to the destination,
         * then it will continue processing the remainder of the buffers. Otherwise
         * the method will return OperationStatus.InvalidData.
         *
         * If the method does return an error code, the out parameters will represent
         * how much of the data was successfully transcoded, and the location of the
         * ill-formed subsequence can be deduced from these values.
         *
         * If 'replaceInvalidSequences' is true, the method is guaranteed never to return
         * OperationStatus.InvalidData. If 'isFinalBlock' is true, the method is
         * guaranteed never to return OperationStatus.NeedMoreData.
         */

        /// <summary>
        /// Transcodes the UTF-16 <paramref name="source"/> buffer to <paramref name="destination"/> as UTF-8.
        /// </summary>
        /// <remarks>
        /// If <paramref name="replaceInvalidSequences"/> is <see langword="true"/>, invalid UTF-16 sequences
        /// in <paramref name="source"/> will be replaced with U+FFFD in <paramref name="destination"/>, and
        /// this method will not return <see cref="OperationStatus.InvalidData"/>.
        /// </remarks>
        public static unsafe OperationStatus FromUtf16(ReadOnlySpan<char> source, Span<byte> destination, out int charsRead, out int bytesWritten, bool replaceInvalidSequences = true, bool isFinalBlock = true)
        {
            // Throwaway span accesses - workaround for https://github.com/dotnet/runtime/issues/12332

            _ = source.Length;
            _ = destination.Length;

            fixed (char* pOriginalSource = &MemoryMarshal.GetReference(source))
            fixed (byte* pOriginalDestination = &MemoryMarshal.GetReference(destination))
            {
                // We're going to bulk transcode as much as we can in a loop, iterating
                // every time we see bad data that requires replacement.

                OperationStatus operationStatus = OperationStatus.Done;
                char* pInputBufferRemaining = pOriginalSource;
                byte* pOutputBufferRemaining = pOriginalDestination;

                while (!source.IsEmpty)
                {
                    // We've pinned the spans at the entry point to this method.
                    // It's safe for us to use Unsafe.AsPointer on them during this loop.

                    operationStatus = Utf8Utility.TranscodeToUtf8(
                        pInputBuffer: (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source)),
                        inputLength: source.Length,
                        pOutputBuffer: (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination)),
                        outputBytesRemaining: destination.Length,
                        pInputBufferRemaining: out pInputBufferRemaining,
                        pOutputBufferRemaining: out pOutputBufferRemaining);

                    // If we finished the operation entirely or we ran out of space in the destination buffer,
                    // or if we need more input data and the caller told us that there's possibly more data
                    // coming, return immediately.

                    if (operationStatus <= OperationStatus.DestinationTooSmall
                        || (operationStatus == OperationStatus.NeedMoreData && !isFinalBlock))
                    {
                        break;
                    }

                    // We encountered invalid data, or we need more data but the caller told us we're
                    // at the end of the stream. In either case treat this as truly invalid.
                    // If the caller didn't tell us to replace invalid sequences, return immediately.

                    if (!replaceInvalidSequences)
                    {
                        operationStatus = OperationStatus.InvalidData; // status code may have been NeedMoreData - force to be error
                        break;
                    }

                    // We're going to attempt to write U+FFFD to the destination buffer.
                    // Do we even have enough space to do so?

                    destination = destination.Slice((int)(pOutputBufferRemaining - (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination))));

                    if (destination.Length <= 2)
                    {
                        operationStatus = OperationStatus.DestinationTooSmall;
                        break;
                    }

                    destination[0] = 0xEF; // U+FFFD = [ EF BF BD ] in UTF-8
                    destination[1] = 0xBF;
                    destination[2] = 0xBD;
                    destination = destination.Slice(3);

                    // Invalid UTF-16 sequences are always of length 1. Just skip the next character.

                    source = source.Slice((int)(pInputBufferRemaining - (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source))) + 1);

                    operationStatus = OperationStatus.Done; // we patched the error - if we're about to break out of the loop this is a success case
                    pInputBufferRemaining = (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source));
                    pOutputBufferRemaining = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination));
                }

                // Not possible to make any further progress - report to our caller how far we got.

                charsRead = (int)(pInputBufferRemaining - pOriginalSource);
                bytesWritten = (int)(pOutputBufferRemaining - pOriginalDestination);
                return operationStatus;
            }
        }

        /// <summary>
        /// Transcodes the UTF-8 <paramref name="source"/> buffer to <paramref name="destination"/> as UTF-16.
        /// </summary>
        /// <remarks>
        /// If <paramref name="replaceInvalidSequences"/> is <see langword="true"/>, invalid UTF-8 sequences
        /// in <paramref name="source"/> will be replaced with U+FFFD in <paramref name="destination"/>, and
        /// this method will not return <see cref="OperationStatus.InvalidData"/>.
        /// </remarks>
        public static unsafe OperationStatus ToUtf16(ReadOnlySpan<byte> source, Span<char> destination, out int bytesRead, out int charsWritten, bool replaceInvalidSequences = true, bool isFinalBlock = true)
        {
            // Throwaway span accesses - workaround for https://github.com/dotnet/runtime/issues/12332

            _ = source.Length;
            _ = destination.Length;

            // We'll be mutating these values throughout our loop.

            fixed (byte* pOriginalSource = &MemoryMarshal.GetReference(source))
            fixed (char* pOriginalDestination = &MemoryMarshal.GetReference(destination))
            {
                // We're going to bulk transcode as much as we can in a loop, iterating
                // every time we see bad data that requires replacement.

                OperationStatus operationStatus = OperationStatus.Done;
                byte* pInputBufferRemaining = pOriginalSource;
                char* pOutputBufferRemaining = pOriginalDestination;

                while (!source.IsEmpty)
                {
                    // We've pinned the spans at the entry point to this method.
                    // It's safe for us to use Unsafe.AsPointer on them during this loop.

                    operationStatus = Utf8Utility.TranscodeToUtf16(
                        pInputBuffer: (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source)),
                        inputLength: source.Length,
                        pOutputBuffer: (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination)),
                        outputCharsRemaining: destination.Length,
                        pInputBufferRemaining: out pInputBufferRemaining,
                        pOutputBufferRemaining: out pOutputBufferRemaining);

                    // If we finished the operation entirely or we ran out of space in the destination buffer,
                    // or if we need more input data and the caller told us that there's possibly more data
                    // coming, return immediately.

                    if (operationStatus <= OperationStatus.DestinationTooSmall
                        || (operationStatus == OperationStatus.NeedMoreData && !isFinalBlock))
                    {
                        break;
                    }

                    // We encountered invalid data, or we need more data but the caller told us we're
                    // at the end of the stream. In either case treat this as truly invalid.
                    // If the caller didn't tell us to replace invalid sequences, return immediately.

                    if (!replaceInvalidSequences)
                    {
                        operationStatus = OperationStatus.InvalidData; // status code may have been NeedMoreData - force to be error
                        break;
                    }

                    // We're going to attempt to write U+FFFD to the destination buffer.
                    // Do we even have enough space to do so?

                    destination = destination.Slice((int)(pOutputBufferRemaining - (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination))));

                    if (destination.IsEmpty)
                    {
                        operationStatus = OperationStatus.DestinationTooSmall;
                        break;
                    }

                    destination[0] = (char)UnicodeUtility.ReplacementChar;
                    destination = destination.Slice(1);

                    // Now figure out how many bytes of the source we must skip over before we should retry
                    // the operation. This might be more than 1 byte.

                    source = source.Slice((int)(pInputBufferRemaining - (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source))));
                    Debug.Assert(!source.IsEmpty, "Expected 'Done' if source is fully consumed.");

                    Rune.DecodeFromUtf8(source, out _, out int bytesConsumedJustNow);
                    source = source.Slice(bytesConsumedJustNow);

                    operationStatus = OperationStatus.Done; // we patched the error - if we're about to break out of the loop this is a success case
                    pInputBufferRemaining = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source));
                    pOutputBufferRemaining = (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination));
                }

                // Not possible to make any further progress - report to our caller how far we got.

                bytesRead = (int)(pInputBufferRemaining - pOriginalSource);
                charsWritten = (int)(pOutputBufferRemaining - pOriginalDestination);
                return operationStatus;
            }
        }

        /// <summary>Writes the specified interpolated string to the UTF8 byte span.</summary>
        /// <param name="destination">The span to which the interpolated string should be formatted.</param>
        /// <param name="handler">The interpolated string.</param>
        /// <param name="bytesWritten">The number of characters written to the span.</param>
        /// <returns>true if the entire interpolated string could be formatted successfully; otherwise, false.</returns>
        public static bool TryWrite(Span<byte> destination, [InterpolatedStringHandlerArgument(nameof(destination))] ref TryWriteInterpolatedStringHandler handler, out int bytesWritten)
        {
            // The span argument isn't used directly in the method; rather, it'll be used by the compiler to create the handler.
            // We could validate here that span == handler._destination, but that doesn't seem necessary.
            if (handler._success)
            {
                bytesWritten = handler._pos;
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        /// <summary>Writes the specified interpolated string to the UTF8 byte span.</summary>
        /// <param name="destination">The span to which the interpolated string should be formatted.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="handler">The interpolated string.</param>
        /// <param name="bytesWritten">The number of characters written to the span.</param>
        /// <returns>true if the entire interpolated string could be formatted successfully; otherwise, false.</returns>
        public static bool TryWrite(Span<byte> destination, IFormatProvider? provider, [InterpolatedStringHandlerArgument(nameof(destination), nameof(provider))] ref TryWriteInterpolatedStringHandler handler, out int bytesWritten) =>
            // The provider is passed to the handler by the compiler, so the actual implementation of the method
            // is the same as the non-provider overload.
            TryWrite(destination, ref handler, out bytesWritten);

        /// <summary>Provides a handler used by the language compiler to format interpolated strings into UTF8 byte spans.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [InterpolatedStringHandler]
        public ref struct TryWriteInterpolatedStringHandler
        {
            /// <summary>The destination UTF8 buffer.</summary>
            private readonly Span<byte> _destination;
            /// <summary>Optional provider to pass to IFormattable.ToString, ISpanFormattable.TryFormat, and IUtf8SpanFormattable.TryFormat calls.</summary>
            private readonly IFormatProvider? _provider;
            /// <summary>The number of bytes written to <see cref="_destination"/>.</summary>
            internal int _pos;
            /// <summary>true if all formatting operations have succeeded; otherwise, false.</summary>
            internal bool _success;
            /// <summary>Whether <see cref="_provider"/> provides an ICustomFormatter.</summary>
            private readonly bool _hasCustomFormatter;

            /// <summary>Creates a handler used to write an interpolated string into a UTF8 <see cref="Span{Byte}"/>.</summary>
            /// <param name="literalLength">The number of constant characters outside of interpolation expressions in the interpolated string.</param>
            /// <param name="formattedCount">The number of interpolation expressions in the interpolated string.</param>
            /// <param name="destination">The destination buffer.</param>
            /// <param name="shouldAppend">Upon return, true if the destination may be long enough to support the formatting, or false if it won't be.</param>
            /// <remarks>This is intended to be called only by compiler-generated code. Arguments are not validated as they'd otherwise be for members intended to be used directly.</remarks>
            public TryWriteInterpolatedStringHandler(int literalLength, int formattedCount, Span<byte> destination, out bool shouldAppend)
            {
                _destination = destination;
                _provider = null;
                _pos = 0;
                _success = shouldAppend = destination.Length >= literalLength; // UTF8 encoding never produces fewer bytes than input characters
                _hasCustomFormatter = false;
            }

            /// <summary>Creates a handler used to write an interpolated string into a UTF8 <see cref="Span{Byte}"/>.</summary>
            /// <param name="literalLength">The number of constant characters outside of interpolation expressions in the interpolated string.</param>
            /// <param name="formattedCount">The number of interpolation expressions in the interpolated string.</param>
            /// <param name="destination">The destination buffer.</param>
            /// <param name="provider">An object that supplies culture-specific formatting information.</param>
            /// <param name="shouldAppend">Upon return, true if the destination may be long enough to support the formatting, or false if it won't be.</param>
            /// <remarks>This is intended to be called only by compiler-generated code. Arguments are not validated as they'd otherwise be for members intended to be used directly.</remarks>
            public TryWriteInterpolatedStringHandler(int literalLength, int formattedCount, Span<byte> destination, IFormatProvider? provider, out bool shouldAppend)
            {
                _destination = destination;
                _provider = provider;
                _pos = 0;
                _success = shouldAppend = destination.Length >= literalLength; // UTF8 encoding never produces fewer bytes than input characters
                _hasCustomFormatter = provider is not null && DefaultInterpolatedStringHandler.HasCustomFormatter(provider);
            }

            /// <summary>Writes the specified string to the handler.</summary>
            /// <param name="value">The string to write.</param>
            /// <returns>true if the value could be formatted to the span; otherwise, false.</returns>
            public bool AppendLiteral(string value) => AppendFormatted(value.AsSpan());

            // TODO https://github.com/dotnet/csharplang/issues/7072:
            // Add this if/when C# supports u8 literals with string interpolation.
            // If that happens prior to this type being released, the above AppendLiteral(string)
            // should also be removed.  If that doesn't happen, we should look into ways to optimize
            // the above AppendLiteral, such as by making the underlying encoding operation a JIT
            // intrinsic that can emit substitute a "abc"u8 equivalent for an "abc" string literal.
            //public bool AppendLiteral(scoped ReadOnlySpan<byte> value) => AppendFormatted(value);

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <typeparam name="T">The type of the value to write.</typeparam>
            public bool AppendFormatted<T>(T value)
            {
                // This method could delegate to AppendFormatted with a null format, but explicitly passing
                // default as the format to TryFormat helps to improve code quality in some cases when TryFormat is inlined,
                // e.g. for Int32 it enables the JIT to eliminate code in the inlined method based on a length check on the format.

                // If there's a custom formatter, always use it.
                if (_hasCustomFormatter)
                {
                    return AppendCustomFormatter(value, format: null);
                }

                // Special-case enums to avoid boxing them.
                if (typeof(T).IsEnum)
                {
                    // TODO https://github.com/dotnet/runtime/issues/81500:
                    // Once Enum.TryFormat provides direct UTF8 support, use that here instead.
                    return AppendEnum(value, format: null);
                }

                // If the value can format itself directly into our buffer, do so.
                if (value is IUtf8SpanFormattable)
                {
                    if (((IUtf8SpanFormattable)value).TryFormat(_destination.Slice(_pos), out int bytesWritten, format: default, _provider))
                    {
                        _pos += bytesWritten;
                        return true;
                    }

                    return Fail();
                }

                string? s;
                if (value is IFormattable)
                {
                    // If the value can format itself directly into a UTF16 buffer, do so, then transcode.
                    if (value is ISpanFormattable)
                    {
                        return AppendSpanFormattable(value, format: null);
                    }

                    // If the value can ToString with the format / provider, get the resulting string, then append that.
                    s = ((IFormattable)value).ToString(null, _provider);
                }
                else
                {
                    // Fall back to a normal ToString and append that.
                    s = value?.ToString();
                }

                return AppendFormatted(s.AsSpan());
            }

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="format">The format string.</param>
            /// <typeparam name="T">The type of the value to write.</typeparam>
            public bool AppendFormatted<T>(T value, string? format)
            {
                // If there's a custom formatter, always use it.
                if (_hasCustomFormatter)
                {
                    return AppendCustomFormatter(value, format);
                }

                // Special-case enums to avoid boxing them.
                if (typeof(T).IsEnum)
                {
                    // TODO https://github.com/dotnet/runtime/issues/81500:
                    // Once Enum.TryFormat provides direct UTF8 support, use that here instead.
                    return AppendEnum(value, format);
                }

                // If the value can format itself directly into our buffer, do so.
                if (value is IUtf8SpanFormattable)
                {
                    if (((IUtf8SpanFormattable)value).TryFormat(_destination.Slice(_pos), out int bytesWritten, format, _provider))
                    {
                        _pos += bytesWritten;
                        return true;
                    }

                    return Fail();
                }

                string? s;
                if (value is IFormattable)
                {
                    // If the value can format itself directly into a UTF16 buffer, do so, then transcode.
                    if (value is ISpanFormattable)
                    {
                        return AppendSpanFormattable(value, format);
                    }

                    // If the value can ToString with the format / provider, get the resulting string, then append that.
                    s = ((IFormattable)value).ToString(format, _provider);
                }
                else
                {
                    // Fall back to a normal ToString and append that.
                    s = value?.ToString();
                }

                return AppendFormatted(s.AsSpan());
            }

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            /// <typeparam name="T">The type of the value to write.</typeparam>
            public bool AppendFormatted<T>(T value, int alignment)
            {
                int startingPos = _pos;
                if (AppendFormatted(value))
                {
                    return alignment == 0 || TryAppendOrInsertAlignmentIfNeeded(startingPos, alignment);
                }

                return Fail();
            }

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="format">The format string.</param>
            /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            /// <typeparam name="T">The type of the value to write.</typeparam>
            public bool AppendFormatted<T>(T value, int alignment, string? format)
            {
                int startingPos = _pos;
                if (AppendFormatted(value, format))
                {
                    return alignment == 0 || TryAppendOrInsertAlignmentIfNeeded(startingPos, alignment);
                }

                return Fail();
            }

            /// <summary>Writes the specified character span to the handler.</summary>
            /// <param name="value">The span to write.</param>
            public bool AppendFormatted(scoped ReadOnlySpan<char> value)
            {
                if (Encoding.UTF8.TryGetBytes(value, _destination.Slice(_pos), out int bytesWritten))
                {
                    _pos += bytesWritten;
                    return true;
                }

                return Fail();
            }

            /// <summary>Writes the specified string of chars to the handler.</summary>
            /// <param name="value">The span to write.</param>
            /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            /// <param name="format">The format string.</param>
            public bool AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null)
            {
                int startingPos = _pos;
                if (AppendFormatted(value))
                {
                    return alignment == 0 || TryAppendOrInsertAlignmentIfNeeded(startingPos, alignment);
                }

                return Fail();
            }

            /// <summary>Writes the specified span of UTF8 bytes to the handler.</summary>
            /// <param name="utf8Value">The span to write.</param>
            public bool AppendFormatted(scoped ReadOnlySpan<byte> utf8Value)
            {
                if (utf8Value.TryCopyTo(_destination.Slice(_pos)))
                {
                    _pos += utf8Value.Length;
                    return true;
                }

                return Fail();
            }

            /// <summary>Writes the specified span of UTF8 bytes to the handler.</summary>
            /// <param name="utf8Value">The span to write.</param>
            /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            /// <param name="format">The format string.</param>
            public bool AppendFormatted(scoped ReadOnlySpan<byte> utf8Value, int alignment = 0, string? format = null)
            {
                int startingPos = _pos;
                if (AppendFormatted(utf8Value))
                {
                    return alignment == 0 || TryAppendOrInsertAlignmentIfNeeded(startingPos, alignment);
                }

                return Fail();
            }

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            public bool AppendFormatted(string? value) =>
                _hasCustomFormatter ? AppendCustomFormatter(value, format: null) :
                AppendFormatted(value.AsSpan());

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            /// <param name="format">The format string.</param>
            public bool AppendFormatted(string? value, int alignment = 0, string? format = null) =>
                // Format is meaningless for strings and doesn't make sense for someone to specify.  We have the overload
                // simply to disambiguate between ROS and object, just in case someone does specify a format, as
                // string is implicitly convertible to both. Just delegate to the T-based implementation.
                AppendFormatted<string?>(value, alignment, format);

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            /// <param name="format">The format string.</param>
            public bool AppendFormatted(object? value, int alignment = 0, string? format = null) =>
                // This overload is expected to be used rarely, only if either a) something strongly typed as object is
                // formatted with both an alignment and a format, or b) the compiler is unable to target type to T. It
                // exists purely to help make cases from (b) compile. Just delegate to the T-based implementation.
                AppendFormatted<object?>(value, alignment, format);

            /// <summary>Formats the value using the custom formatter from the provider.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="format">The format string.</param>
            /// <typeparam name="T">The type of the value to write.</typeparam>
            [MethodImpl(MethodImplOptions.NoInlining)]
            private bool AppendCustomFormatter<T>(T value, string? format)
            {
                // This case is very rare, but we need to handle it prior to the other checks in case
                // a provider was used that supplied an ICustomFormatter which wanted to intercept the particular value.
                // We do the cast here rather than in the ctor, even though this could be executed multiple times per
                // formatting, to make the cast pay for play.
                Debug.Assert(_hasCustomFormatter);
                Debug.Assert(_provider is not null);

                ICustomFormatter? formatter = (ICustomFormatter?)_provider.GetFormat(typeof(ICustomFormatter));
                Debug.Assert(formatter is not null, "An incorrectly written provider said it implemented ICustomFormatter, and then didn't");

                if (formatter is not null &&
                    formatter.Format(format, value, _provider) is string customFormatted)
                {
                    return AppendFormatted(customFormatted.AsSpan());
                }

                return true;
            }

            /// <summary>Writes the specified ISpanFormattable to the handler.</summary>
            /// <param name="value">The value to write. It must be an ISpanFormattable but isn't constrained because the caller doesn't have a constraint.</param>
            /// <param name="format">The format string.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool AppendSpanFormattable<T>(T value, string? format)
            {
                Debug.Assert(value is ISpanFormattable);

                Span<char> utf16 = stackalloc char[256];
                return ((ISpanFormattable)value).TryFormat(utf16, out int charsWritten, format, _provider) ?
                    AppendFormatted(utf16.Slice(0, charsWritten)) :
                    GrowAndAppendFormatted(ref this, value, utf16.Length, out charsWritten, format);

                [MethodImpl(MethodImplOptions.NoInlining)]
                static bool GrowAndAppendFormatted(scoped ref TryWriteInterpolatedStringHandler thisRef, T value, int length, out int charsWritten, string? format)
                {
                    Debug.Assert(value is ISpanFormattable);

                    while (true)
                    {
                        int newLength = length * 2;
                        if ((uint)newLength > Array.MaxLength)
                        {
                            newLength = length == Array.MaxLength ?
                                Array.MaxLength + 1 : // force OOM
                                Array.MaxLength;
                        }
                        length = newLength;

                        char[] array = ArrayPool<char>.Shared.Rent(length);
                        try
                        {
                            if (((ISpanFormattable)value).TryFormat(array, out charsWritten, format, thisRef._provider))
                            {
                                return thisRef.AppendFormatted(array.AsSpan(0, charsWritten));
                            }
                        }
                        finally
                        {
                            ArrayPool<char>.Shared.Return(array);
                        }
                    }
                }
            }

            // TODO https://github.com/dotnet/runtime/issues/81500:
            // Remove once Enum.TryFormat(Span<byte>, ...) is available.
            /// <summary>Writes the specified enum to the handler.</summary>
            /// <param name="value">The value to write. It must be an enum but isn't constrained because the caller doesn't have a constraint.</param>
            /// <param name="format">The format string.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool AppendEnum<T>(T value, string? format)
            {
                Debug.Assert(typeof(T).IsEnum);

                Span<char> utf16 = stackalloc char[256];
                return Enum.TryFormatUnconstrained(value, utf16, out int charsWritten, format) ?
                    AppendFormatted(utf16.Slice(0, charsWritten)) :
                    GrowAndAppendFormatted(ref this, value, utf16.Length, out charsWritten, format);

                [MethodImpl(MethodImplOptions.NoInlining)]
                static bool GrowAndAppendFormatted(scoped ref TryWriteInterpolatedStringHandler thisRef, T value, int length, out int charsWritten, string? format)
                {
                    Debug.Assert(value is ISpanFormattable);

                    while (true)
                    {
                        int newLength = length * 2;
                        if ((uint)newLength > Array.MaxLength)
                        {
                            newLength = length == Array.MaxLength ?
                                Array.MaxLength + 1 : // force OOM
                                Array.MaxLength;
                        }
                        length = newLength;

                        char[] array = ArrayPool<char>.Shared.Rent(length);
                        try
                        {
                            if (Enum.TryFormatUnconstrained(value, array, out charsWritten, format))
                            {
                                return thisRef.AppendFormatted(array.AsSpan(0, charsWritten));
                            }
                        }
                        finally
                        {
                            ArrayPool<char>.Shared.Return(array);
                        }
                    }
                }
            }

            /// <summary>Handles adding any padding required for aligning a formatted value in an interpolation expression.</summary>
            /// <param name="startingPos">The position at which the written value started.</param>
            /// <param name="alignment">Non-zero minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            private bool TryAppendOrInsertAlignmentIfNeeded(int startingPos, int alignment)
            {
                Debug.Assert(startingPos >= 0 && startingPos <= _pos);
                Debug.Assert(alignment != 0);

                int bytesWritten = _pos - startingPos;

                bool leftAlign = false;
                if (alignment < 0)
                {
                    leftAlign = true;
                    alignment = -alignment;
                }

                int paddingNeeded = alignment - bytesWritten;
                if (paddingNeeded <= 0)
                {
                    return true;
                }

                if (paddingNeeded <= _destination.Length - _pos)
                {
                    if (leftAlign)
                    {
                        _destination.Slice(_pos, paddingNeeded).Fill((byte)' ');
                    }
                    else
                    {
                        _destination.Slice(startingPos, bytesWritten).CopyTo(_destination.Slice(startingPos + paddingNeeded));
                        _destination.Slice(startingPos, paddingNeeded).Fill((byte)' ');
                    }

                    _pos += paddingNeeded;
                    return true;
                }

                return Fail();
            }

            /// <summary>Marks formatting as having failed and returns false.</summary>
            private bool Fail()
            {
                _success = false;
                return false;
            }
        }
    }
}
