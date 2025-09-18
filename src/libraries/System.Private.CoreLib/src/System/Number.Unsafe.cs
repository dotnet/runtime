// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System
{
    // The methods defined in this file are intended to be accessed from System.Runtime.Numerics
    // via the UnsafeAccessorAttribute. To prevent them from being trimmed, they also need to be
    // listed in src/coreclr/System.Private.CoreLib/src/ILLink/ILLink.Descriptors.Shared.xml.
    internal static partial class Number
    {
        /// <summary>
        /// For System.Runtime.Numerics.
        /// <paramref name="number"/> must be a <see cref="NumberBuffer"/> with fixed fields.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe string NumberToStringCore(void* number, char fmt, int digits, ReadOnlySpan<char> format, NumberFormatInfo info)
        {
            char* stackPtr = stackalloc char[CharStackBufferSize];
            var vlb = new ValueListBuilder<char>(new Span<char>(stackPtr, CharStackBufferSize));

            if (fmt != 0)
            {
                NumberToString(ref vlb, ref Unsafe.AsRef<NumberBuffer>(number), fmt, digits, info);
            }
            else
            {
                NumberToStringFormat(ref vlb, ref Unsafe.AsRef<NumberBuffer>(number), format, info);
            }

            string result = vlb.AsSpan().ToString();
            vlb.Dispose();
            return result;
        }

        /// <summary>
        /// For System.Runtime.Numerics.
        /// <paramref name="number"/> must be a <see cref="NumberBuffer"/> with fixed fields.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool FormatNumberCore(void* number, char fmt, int digits, ReadOnlySpan<char> format, NumberFormatInfo info, Span<byte> destination, out int charsWritten)
            => FormatNumber(ref Unsafe.AsRef<NumberBuffer>(number), fmt, digits, format, info, destination, out charsWritten);

        /// <summary>
        /// For System.Runtime.Numerics.
        /// <paramref name="number"/> must be a <see cref="NumberBuffer"/> with fixed fields.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool FormatNumberCore(void* number, char fmt, int digits, ReadOnlySpan<char> format, NumberFormatInfo info, Span<char> destination, out int charsWritten)
            => FormatNumber(ref Unsafe.AsRef<NumberBuffer>(number), fmt, digits, format, info, destination, out charsWritten);

        /// <summary>
        /// For System.Runtime.Numerics.
        /// <paramref name="number"/> must be a <see cref="NumberBuffer"/> with fixed fields.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool TryStringToNumberCore(ReadOnlySpan<byte> value, NumberStyles styles, void* number, NumberFormatInfo info)
            => TryStringToNumber(value, styles, ref Unsafe.AsRef<NumberBuffer>(number), info);

        /// <summary>
        /// For System.Runtime.Numerics.
        /// <paramref name="number"/> must be a <see cref="NumberBuffer"/> with fixed fields.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool TryStringToNumberCore(ReadOnlySpan<char> value, NumberStyles styles, void* number, NumberFormatInfo info)
            => TryStringToNumber(value, styles, ref Unsafe.AsRef<NumberBuffer>(number), info);


        /// <summary>
        /// For System.Runtime.Numerics.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe byte* UInt32ToDecCharsCore(byte* bufferEnd, uint value, int digits)
            => UInt32ToDecChars(bufferEnd, value, digits);

        /// <summary>
        /// For System.Runtime.Numerics.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe char* UInt32ToDecCharsCore(char* bufferEnd, uint value, int digits)
            => UInt32ToDecChars(bufferEnd, value, digits);
    }
}
