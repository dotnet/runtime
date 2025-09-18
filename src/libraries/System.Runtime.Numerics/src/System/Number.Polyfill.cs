// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    // Polyfill CoreLib internal interfaces and methods
    // Define necessary members only

    internal static partial class Number
    {
        private const string SYSTEM_NUMBER_CORELIB = "System.Number, System.Private.CoreLib";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool TryStringToNumber<TChar>(ReadOnlySpan<TChar> value, NumberStyles styles, ref NumberBuffer number, NumberFormatInfo info)
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(byte) || typeof(TChar) == typeof(char));
            return typeof(TChar) == typeof(byte)
                ? TryStringToNumberCore(null, Unsafe.BitCast<ReadOnlySpan<TChar>, ReadOnlySpan<byte>>(value), styles, Unsafe.AsPointer(ref number), info)
                : TryStringToNumberCore(null, Unsafe.BitCast<ReadOnlySpan<TChar>, ReadOnlySpan<char>>(value), styles, Unsafe.AsPointer(ref number), info);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(TryStringToNumberCore))]
        private static extern unsafe bool TryStringToNumberCore([UnsafeAccessorType(SYSTEM_NUMBER_CORELIB)] object? obj, ReadOnlySpan<byte> value, NumberStyles styles, void* number, NumberFormatInfo info);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(TryStringToNumberCore))]
        private static extern unsafe bool TryStringToNumberCore([UnsafeAccessorType(SYSTEM_NUMBER_CORELIB)] object? obj, ReadOnlySpan<char> value, NumberStyles styles, void* number, NumberFormatInfo info);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe TChar* UInt32ToDecChars<TChar>(TChar* bufferEnd, uint value, int digits)
          where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(byte) || typeof(TChar) == typeof(char));
            return typeof(TChar) == typeof(byte)
                ? (TChar*)UInt32ToDecCharsCore(null, (byte*)bufferEnd, value, digits)
                : (TChar*)UInt32ToDecCharsCore(null, (char*)bufferEnd, value, digits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(UInt32ToDecCharsCore))]
        internal static extern unsafe byte* UInt32ToDecCharsCore([UnsafeAccessorType(SYSTEM_NUMBER_CORELIB)] object? obj, byte* bufferEnd, uint value, int digits);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(UInt32ToDecCharsCore))]
        internal static extern unsafe char* UInt32ToDecCharsCore([UnsafeAccessorType(SYSTEM_NUMBER_CORELIB)] object? obj, char* bufferEnd, uint value, int digits);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static char ParseFormatSpecifier(ReadOnlySpan<char> format, out int digits)
            => ParseFormatSpecifierCore(null, format, out digits);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(ParseFormatSpecifier))]
        internal static extern char ParseFormatSpecifierCore([UnsafeAccessorType(SYSTEM_NUMBER_CORELIB)] object? obj, ReadOnlySpan<char> format, out int digits);



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe string NumberToString(ref NumberBuffer number, char fmt, int digits, ReadOnlySpan<char> format, NumberFormatInfo info)
            => NumberToStringCore(null, Unsafe.AsPointer(ref number), fmt, digits, format, info);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(NumberToStringCore))]
        internal static extern unsafe string NumberToStringCore([UnsafeAccessorType(SYSTEM_NUMBER_CORELIB)] object? obj, void* number, char fmt, int digits, ReadOnlySpan<char> format, NumberFormatInfo info);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool FormatNumber<TChar>(ref NumberBuffer number, char fmt, int digits, ReadOnlySpan<char> format, NumberFormatInfo info, Span<TChar> destination, out int charsWritten)
        {
            Debug.Assert(typeof(TChar) == typeof(byte) || typeof(TChar) == typeof(char));
            return typeof(TChar) == typeof(byte)
                ? FormatNumberCore(null, Unsafe.AsPointer(ref number), fmt, digits, format, info, Unsafe.BitCast<Span<TChar>, Span<byte>>(destination), out charsWritten)
                : FormatNumberCore(null, Unsafe.AsPointer(ref number), fmt, digits, format, info, Unsafe.BitCast<Span<TChar>, Span<char>>(destination), out charsWritten);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(FormatNumberCore))]
        internal static extern unsafe bool FormatNumberCore([UnsafeAccessorType(SYSTEM_NUMBER_CORELIB)] object? obj, void* number, char fmt, int digits, ReadOnlySpan<char> format, NumberFormatInfo info, Span<byte> destination, out int charsWritten);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = nameof(FormatNumberCore))]
        internal static extern unsafe bool FormatNumberCore([UnsafeAccessorType(SYSTEM_NUMBER_CORELIB)] object? obj, void* number, char fmt, int digits, ReadOnlySpan<char> format, NumberFormatInfo info, Span<char> destination, out int charsWritten);
    }
}
