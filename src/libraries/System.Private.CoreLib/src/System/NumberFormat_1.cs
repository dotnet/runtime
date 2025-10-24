// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    internal static partial class NumberFormat<TChar>
        where TChar : unmanaged, IUtfChar<TChar>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe TChar* Int32ToHexChars(TChar* buffer, uint value, int hexBase, int digits)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            while (--digits >= 0 || value != 0)
            {
                byte digit = (byte)(value & 0xF);
                *(--buffer) = TChar.CastFrom(digit + (digit < 10 ? (byte)'0' : hexBase));
                value >>= 4;
            }
            return buffer;
        }

#if TARGET_64BIT
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static unsafe TChar* Int64ToHexChars(TChar* buffer, ulong value, int hexBase, int digits)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));
#if TARGET_32BIT
            uint lower = (uint)value;
            uint upper = (uint)(value >> 32);

            if (upper != 0)
            {
                buffer = Int32ToHexChars(buffer, lower, hexBase, 8);
                return Int32ToHexChars(buffer, upper, hexBase, digits - 8);
            }
            else
            {
                return Int32ToHexChars(buffer, lower, hexBase, Math.Max(digits, 1));
            }
#else
            while (--digits >= 0 || value != 0)
            {
                byte digit = (byte)(value & 0xF);
                *(--buffer) = TChar.CastFrom(digit + (digit < 10 ? (byte)'0' : hexBase));
                value >>= 4;
            }
            return buffer;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe TChar* Int128ToHexChars(TChar* buffer, UInt128 value, int hexBase, int digits)
        {
            ulong lower = value.Lower;
            ulong upper = value.Upper;

            if (upper != 0)
            {
                buffer = Int64ToHexChars(buffer, lower, hexBase, 16);
                return Int64ToHexChars(buffer, upper, hexBase, digits - 16);
            }
            else
            {
                return Int64ToHexChars(buffer, lower, hexBase, Math.Max(digits, 1));
            }
        }

        public static unsafe bool TryFormatDecimal(decimal value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<TChar> destination, out int charsWritten)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            char fmt = Number.ParseFormatSpecifier(format, out int digits);

            byte* pDigits = stackalloc byte[Number.DecimalNumberBufferLength];
            Number.NumberBuffer number = new Number.NumberBuffer(Number.NumberBufferKind.Decimal, pDigits, Number.DecimalNumberBufferLength);

            Number.DecimalToNumber(ref value, ref number);

            TChar* stackPtr = stackalloc TChar[Number.CharStackBufferSize];
            var vlb = new ValueListBuilder<TChar>(new Span<TChar>(stackPtr, Number.CharStackBufferSize));

            if (fmt != 0)
            {
                Number.NumberToString(ref vlb, ref number, fmt, digits, info);
            }
            else
            {
                Number.NumberToStringFormat(ref vlb, ref number, format, info);
            }

            bool success = vlb.TryCopyTo(destination, out charsWritten);
            vlb.Dispose();
            return success;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // expose to caller's likely-const format to trim away slow path
        public static bool TryFormatInt32(int value, int hexMask, ReadOnlySpan<char> format, IFormatProvider? provider, Span<TChar> destination, out int charsWritten)
        {
            // Fast path for default format
            if (format.Length == 0)
            {
                return value >= 0 ?
                    TryUInt32ToDecStr((uint)value, destination, out charsWritten) :
                    TryNegativeInt32ToDecStr(value, digits: -1, NumberFormatInfo.GetInstance(provider).NegativeSignTChar<TChar>(), destination, out charsWritten);
            }

            return TryFormatInt32Slow(value, hexMask, format, provider, destination, out charsWritten);

            static unsafe bool TryFormatInt32Slow(int value, int hexMask, ReadOnlySpan<char> format, IFormatProvider? provider, Span<TChar> destination, out int charsWritten)
            {
                char fmt = Number.ParseFormatSpecifier(format, out int digits);
                char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison
                if (fmtUpper == 'G' ? digits < 1 : fmtUpper == 'D')
                {
                    return value >= 0 ?
                        TryUInt32ToDecStr((uint)value, digits, destination, out charsWritten) :
                        TryNegativeInt32ToDecStr(value, digits, NumberFormatInfo.GetInstance(provider).NegativeSignTChar<TChar>(), destination, out charsWritten);
                }
                else if (fmtUpper == 'X')
                {
                    return TryInt32ToHexStr(value & hexMask, Number.GetHexBase(fmt), digits, destination, out charsWritten);
                }
                else if (fmtUpper == 'B')
                {
                    return TryUInt32ToBinaryStr((uint)(value & hexMask), digits, destination, out charsWritten);
                }
                else
                {
                    NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

                    byte* pDigits = stackalloc byte[Number.Int32NumberBufferLength];
                    Number.NumberBuffer number = new Number.NumberBuffer(Number.NumberBufferKind.Integer, pDigits, Number.Int32NumberBufferLength);

                    Number.Int32ToNumber(value, ref number);

                    TChar* stackPtr = stackalloc TChar[Number.CharStackBufferSize];
                    var vlb = new ValueListBuilder<TChar>(new Span<TChar>(stackPtr, Number.CharStackBufferSize));

                    if (fmt != 0)
                    {
                        Number.NumberToString(ref vlb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        Number.NumberToStringFormat(ref vlb, ref number, format, info);
                    }

                    bool success = vlb.TryCopyTo(destination, out charsWritten);
                    vlb.Dispose();
                    return success;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // expose to caller's likely-const format to trim away slow path
        public static bool TryFormatInt64(long value, ReadOnlySpan<char> format, IFormatProvider? provider, Span<TChar> destination, out int charsWritten)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            // Fast path for default format
            if (format.Length == 0)
            {
                return value >= 0 ?
                    TryUInt64ToDecStr((ulong)value, destination, out charsWritten) :
                    TryNegativeInt64ToDecStr(value, digits: -1, NumberFormatInfo.GetInstance(provider).NegativeSignTChar<TChar>(), destination, out charsWritten);
            }

            return TryFormatInt64Slow(value, format, provider, destination, out charsWritten);

            static unsafe bool TryFormatInt64Slow(long value, ReadOnlySpan<char> format, IFormatProvider? provider, Span<TChar> destination, out int charsWritten)
            {
                char fmt = Number.ParseFormatSpecifier(format, out int digits);
                char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison
                if (fmtUpper == 'G' ? digits < 1 : fmtUpper == 'D')
                {
                    return value >= 0 ?
                        TryUInt64ToDecStr((ulong)value, digits, destination, out charsWritten) :
                        TryNegativeInt64ToDecStr(value, digits, NumberFormatInfo.GetInstance(provider).NegativeSignTChar<TChar>(), destination, out charsWritten);
                }
                else if (fmtUpper == 'X')
                {
                    return TryInt64ToHexStr(value, Number.GetHexBase(fmt), digits, destination, out charsWritten);
                }
                else if (fmtUpper == 'B')
                {
                    return TryUInt64ToBinaryStr((ulong)value, digits, destination, out charsWritten);
                }
                else
                {
                    NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

                    byte* pDigits = stackalloc byte[Number.Int64NumberBufferLength];
                    Number.NumberBuffer number = new Number.NumberBuffer(Number.NumberBufferKind.Integer, pDigits, Number.Int64NumberBufferLength);

                    Number.Int64ToNumber(value, ref number);

                    char* stackPtr = stackalloc char[Number.CharStackBufferSize];
                    var vlb = new ValueListBuilder<TChar>(new Span<TChar>(stackPtr, Number.CharStackBufferSize));

                    if (fmt != 0)
                    {
                        Number.NumberToString(ref vlb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        Number.NumberToStringFormat(ref vlb, ref number, format, info);
                    }

                    bool success = vlb.TryCopyTo(destination, out charsWritten);
                    vlb.Dispose();
                    return success;
                }
            }
        }

        public static bool TryFormatInt128(Int128 value, ReadOnlySpan<char> format, IFormatProvider? provider, Span<TChar> destination, out int charsWritten)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            // Fast path for default format
            if (format.Length == 0)
            {
                return Int128.IsPositive(value)
                     ? TryUInt128ToDecStr((UInt128)value, digits: -1, destination, out charsWritten)
                     : TryNegativeInt128ToDecStr(value, digits: -1, NumberFormatInfo.GetInstance(provider).NegativeSignTChar<TChar>(), destination, out charsWritten);
            }

            return TryFormatInt128Slow(value, format, provider, destination, out charsWritten);

            static unsafe bool TryFormatInt128Slow(Int128 value, ReadOnlySpan<char> format, IFormatProvider? provider, Span<TChar> destination, out int charsWritten)
            {
                char fmt = Number.ParseFormatSpecifier(format, out int digits);
                char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison

                if (fmtUpper == 'G' ? digits < 1 : fmtUpper == 'D')
                {
                    return Int128.IsPositive(value)
                        ? TryUInt128ToDecStr((UInt128)value, digits, destination, out charsWritten)
                        : TryNegativeInt128ToDecStr(value, digits, NumberFormatInfo.GetInstance(provider).NegativeSignTChar<TChar>(), destination, out charsWritten);
                }
                else if (fmtUpper == 'X')
                {
                    return TryInt128ToHexStr(value, Number.GetHexBase(fmt), digits, destination, out charsWritten);
                }
                else if (fmtUpper == 'B')
                {
                    return TryUInt128ToBinaryStr(value, digits, destination, out charsWritten);
                }
                else
                {
                    NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

                    byte* pDigits = stackalloc byte[Number.Int128NumberBufferLength];
                    Number.NumberBuffer number = new Number.NumberBuffer(Number.NumberBufferKind.Integer, pDigits, Number.Int128NumberBufferLength);

                    Number.Int128ToNumber(value, ref number);

                    TChar* stackPtr = stackalloc TChar[Number.CharStackBufferSize];
                    var vlb = new ValueListBuilder<TChar>(new Span<TChar>(stackPtr, Number.CharStackBufferSize));

                    if (fmt != 0)
                    {
                        Number.NumberToString(ref vlb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        Number.NumberToStringFormat(ref vlb, ref number, format, info);
                    }

                    bool success = vlb.TryCopyTo(destination, out charsWritten);
                    vlb.Dispose();
                    return success;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // expose to caller's likely-const format to trim away slow path
        public static bool TryFormatUInt32(uint value, ReadOnlySpan<char> format, IFormatProvider? provider, Span<TChar> destination, out int charsWritten)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            // Fast path for default format
            if (format.Length == 0)
            {
                return TryUInt32ToDecStr(value, destination, out charsWritten);
            }

            return TryFormatUInt32Slow(value, format, provider, destination, out charsWritten);

            static unsafe bool TryFormatUInt32Slow(uint value, ReadOnlySpan<char> format, IFormatProvider? provider, Span<TChar> destination, out int charsWritten)
            {
                char fmt = Number.ParseFormatSpecifier(format, out int digits);
                char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison
                if (fmtUpper == 'G' ? digits < 1 : fmtUpper == 'D')
                {
                    return TryUInt32ToDecStr(value, digits, destination, out charsWritten);
                }
                else if (fmtUpper == 'X')
                {
                    return TryInt32ToHexStr((int)value, Number.GetHexBase(fmt), digits, destination, out charsWritten);
                }
                else if (fmtUpper == 'B')
                {
                    return TryUInt32ToBinaryStr(value, digits, destination, out charsWritten);
                }
                else
                {
                    NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

                    byte* pDigits = stackalloc byte[Number.UInt32NumberBufferLength];
                    Number.NumberBuffer number = new Number.NumberBuffer(Number.NumberBufferKind.Integer, pDigits, Number.UInt32NumberBufferLength);

                    Number.UInt32ToNumber(value, ref number);

                    TChar* stackPtr = stackalloc TChar[Number.CharStackBufferSize];
                    var vlb = new ValueListBuilder<TChar>(new Span<TChar>(stackPtr, Number.CharStackBufferSize));

                    if (fmt != 0)
                    {
                        Number.NumberToString(ref vlb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        Number.NumberToStringFormat(ref vlb, ref number, format, info);
                    }

                    bool success = vlb.TryCopyTo(destination, out charsWritten);
                    vlb.Dispose();
                    return success;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // expose to caller's likely-const format to trim away slow path
        public static bool TryFormatUInt64(ulong value, ReadOnlySpan<char> format, IFormatProvider? provider, Span<TChar> destination, out int charsWritten)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            // Fast path for default format
            if (format.Length == 0)
            {
                return TryUInt64ToDecStr(value, destination, out charsWritten);
            }

            return TryFormatUInt64Slow(value, format, provider, destination, out charsWritten);

            static unsafe bool TryFormatUInt64Slow(ulong value, ReadOnlySpan<char> format, IFormatProvider? provider, Span<TChar> destination, out int charsWritten)
            {
                char fmt = Number.ParseFormatSpecifier(format, out int digits);
                char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison
                if (fmtUpper == 'G' ? digits < 1 : fmtUpper == 'D')
                {
                    return TryUInt64ToDecStr(value, digits, destination, out charsWritten);
                }
                else if (fmtUpper == 'X')
                {
                    return TryInt64ToHexStr((long)value, Number.GetHexBase(fmt), digits, destination, out charsWritten);
                }
                else if (fmtUpper == 'B')
                {
                    return TryUInt64ToBinaryStr(value, digits, destination, out charsWritten);
                }
                else
                {
                    NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

                    byte* pDigits = stackalloc byte[Number.UInt64NumberBufferLength];
                    Number.NumberBuffer number = new Number.NumberBuffer(Number.NumberBufferKind.Integer, pDigits, Number.UInt64NumberBufferLength);

                    Number.UInt64ToNumber(value, ref number);

                    TChar* stackPtr = stackalloc TChar[Number.CharStackBufferSize];
                    var vlb = new ValueListBuilder<TChar>(new Span<TChar>(stackPtr, Number.CharStackBufferSize));

                    if (fmt != 0)
                    {
                        Number.NumberToString(ref vlb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        Number.NumberToStringFormat(ref vlb, ref number, format, info);
                    }

                    bool success = vlb.TryCopyTo(destination, out charsWritten);
                    vlb.Dispose();
                    return success;
                }
            }
        }

        public static bool TryFormatUInt128(UInt128 value, ReadOnlySpan<char> format, IFormatProvider? provider, Span<TChar> destination, out int charsWritten)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            // Fast path for default format
            if (format.Length == 0)
            {
                return TryUInt128ToDecStr(value, digits: -1, destination, out charsWritten);
            }

            return TryFormatUInt128Slow(value, format, provider, destination, out charsWritten);

            static unsafe bool TryFormatUInt128Slow(UInt128 value, ReadOnlySpan<char> format, IFormatProvider? provider, Span<TChar> destination, out int charsWritten)
            {
                char fmt = Number.ParseFormatSpecifier(format, out int digits);
                char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison

                if (fmtUpper == 'G' ? digits < 1 : fmtUpper == 'D')
                {
                    return TryUInt128ToDecStr(value, digits, destination, out charsWritten);
                }
                else if (fmtUpper == 'X')
                {
                    return TryInt128ToHexStr((Int128)value, Number.GetHexBase(fmt), digits, destination, out charsWritten);
                }
                else if (fmtUpper == 'B')
                {
                    return TryUInt128ToBinaryStr((Int128)value, digits, destination, out charsWritten);
                }
                else
                {
                    NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

                    byte* pDigits = stackalloc byte[Number.UInt128NumberBufferLength];
                    Number.NumberBuffer number = new Number.NumberBuffer(Number.NumberBufferKind.Integer, pDigits, Number.UInt128NumberBufferLength);

                    Number.UInt128ToNumber(value, ref number);

                    TChar* stackPtr = stackalloc TChar[Number.CharStackBufferSize];
                    var vlb = new ValueListBuilder<TChar>(new Span<TChar>(stackPtr, Number.CharStackBufferSize));

                    if (fmt != 0)
                    {
                        Number.NumberToString(ref vlb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        Number.NumberToStringFormat(ref vlb, ref number, format, info);
                    }

                    bool success = vlb.TryCopyTo(destination, out charsWritten);
                    vlb.Dispose();
                    return success;
                }
            }
        }

        public static unsafe bool TryInt32ToHexStr(int value, char hexBase, int digits, Span<TChar> destination, out int charsWritten)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            if (digits < 1)
            {
                digits = 1;
            }

            int bufferLength = Math.Max(digits, FormattingHelpers.CountHexDigits((uint)value));
            if (bufferLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = bufferLength;
            fixed (TChar* buffer = &MemoryMarshal.GetReference(destination))
            {
                TChar* p = Int32ToHexChars(buffer + bufferLength, (uint)value, hexBase, digits);
                Debug.Assert(p == buffer);
            }
            return true;
        }

        public static unsafe bool TryInt64ToHexStr(long value, char hexBase, int digits, Span<TChar> destination, out int charsWritten)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            if (digits < 1)
            {
                digits = 1;
            }

            int bufferLength = Math.Max(digits, FormattingHelpers.CountHexDigits((ulong)value));
            if (bufferLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = bufferLength;
            fixed (TChar* buffer = &MemoryMarshal.GetReference(destination))
            {
                TChar* p = Int64ToHexChars(buffer + bufferLength, (ulong)value, hexBase, digits);
                Debug.Assert(p == buffer);
            }
            return true;
        }

        public static unsafe bool TryInt128ToHexStr(Int128 value, char hexBase, int digits, Span<TChar> destination, out int charsWritten)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            if (digits < 1)
            {
                digits = 1;
            }

            UInt128 uValue = (UInt128)value;

            int bufferLength = Math.Max(digits, FormattingHelpers.CountHexDigits(uValue));
            if (bufferLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = bufferLength;
            fixed (TChar* buffer = &MemoryMarshal.GetReference(destination))
            {
                TChar* p = Int128ToHexChars(buffer + bufferLength, uValue, hexBase, digits);
                Debug.Assert(p == buffer);
            }
            return true;
        }

        public static unsafe bool TryNegativeInt32ToDecStr(int value, int digits, ReadOnlySpan<TChar> sNegative, Span<TChar> destination, out int charsWritten)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));
            Debug.Assert(value < 0);

            if (digits < 1)
            {
                digits = 1;
            }

            int bufferLength = Math.Max(digits, FormattingHelpers.CountDigits((uint)(-value))) + sNegative.Length;
            if (bufferLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = bufferLength;
            fixed (TChar* buffer = &MemoryMarshal.GetReference(destination))
            {
                TChar* p = UInt32ToDecChars(buffer + bufferLength, (uint)(-value), digits);
                Debug.Assert(p == buffer + sNegative.Length);

                for (int i = sNegative.Length - 1; i >= 0; i--)
                {
                    *(--p) = sNegative[i];
                }
                Debug.Assert(p == buffer);
            }
            return true;
        }

        public static unsafe bool TryNegativeInt64ToDecStr(long value, int digits, ReadOnlySpan<TChar> sNegative, Span<TChar> destination, out int charsWritten)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));
            Debug.Assert(value < 0);

            if (digits < 1)
            {
                digits = 1;
            }

            int bufferLength = Math.Max(digits, FormattingHelpers.CountDigits((ulong)(-value))) + sNegative.Length;
            if (bufferLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = bufferLength;
            fixed (TChar* buffer = &MemoryMarshal.GetReference(destination))
            {
                TChar* p = UInt64ToDecChars(buffer + bufferLength, (ulong)(-value), digits);
                Debug.Assert(p == buffer + sNegative.Length);

                for (int i = sNegative.Length - 1; i >= 0; i--)
                {
                    *(--p) = sNegative[i];
                }
                Debug.Assert(p == buffer);
            }
            return true;
        }

        public static unsafe bool TryNegativeInt128ToDecStr(Int128 value, int digits, ReadOnlySpan<TChar> sNegative, Span<TChar> destination, out int charsWritten)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));
            Debug.Assert(Int128.IsNegative(value));

            if (digits < 1)
            {
                digits = 1;
            }

            UInt128 absValue = (UInt128)(-value);

            int bufferLength = Math.Max(digits, FormattingHelpers.CountDigits(absValue)) + sNegative.Length;
            if (bufferLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = bufferLength;
            fixed (TChar* buffer = &MemoryMarshal.GetReference(destination))
            {
                TChar* p = UInt128ToDecChars(buffer + bufferLength, absValue, digits);
                Debug.Assert(p == buffer + sNegative.Length);

                for (int i = sNegative.Length - 1; i >= 0; i--)
                {
                    *(--p) = sNegative[i];
                }
                Debug.Assert(p == buffer);
            }
            return true;
        }

        public static unsafe bool TryUInt32ToBinaryStr(uint value, int digits, Span<TChar> destination, out int charsWritten)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            if (digits < 1)
            {
                digits = 1;
            }

            int bufferLength = Math.Max(digits, 32 - (int)uint.LeadingZeroCount(value));
            if (bufferLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = bufferLength;
            fixed (TChar* buffer = &MemoryMarshal.GetReference(destination))
            {
                TChar* p = UInt32ToBinaryChars(buffer + bufferLength, value, digits);
                Debug.Assert(p == buffer);
            }
            return true;
        }

        public static unsafe bool TryUInt32ToDecStr(uint value, Span<TChar> destination, out int charsWritten)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            int bufferLength = FormattingHelpers.CountDigits(value);
            if (bufferLength <= destination.Length)
            {
                charsWritten = bufferLength;
                fixed (TChar* buffer = &MemoryMarshal.GetReference(destination))
                {
                    TChar* p = UInt32ToDecChars(buffer + bufferLength, value);
                    Debug.Assert(p == buffer);
                }
                return true;
            }

            charsWritten = 0;
            return false;
        }

        public static unsafe bool TryUInt32ToDecStr(uint value, int digits, Span<TChar> destination, out int charsWritten)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            int countedDigits = FormattingHelpers.CountDigits(value);
            int bufferLength = Math.Max(digits, countedDigits);
            if (bufferLength <= destination.Length)
            {
                charsWritten = bufferLength;
                fixed (TChar* buffer = &MemoryMarshal.GetReference(destination))
                {
                    TChar* p = buffer + bufferLength;
                    p = digits > countedDigits ?
                        UInt32ToDecChars(p, value, digits) :
                        UInt32ToDecChars(p, value);
                    Debug.Assert(p == buffer);
                }
                return true;
            }

            charsWritten = 0;
            return false;
        }

        public static unsafe bool TryUInt64ToBinaryStr(ulong value, int digits, Span<TChar> destination, out int charsWritten)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            if (digits < 1)
            {
                digits = 1;
            }

            int bufferLength = Math.Max(digits, 64 - (int)ulong.LeadingZeroCount(value));
            if (bufferLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = bufferLength;
            fixed (TChar* buffer = &MemoryMarshal.GetReference(destination))
            {
                TChar* p = UInt64ToBinaryChars(buffer + bufferLength, value, digits);
                Debug.Assert(p == buffer);
            }
            return true;
        }

        public static unsafe bool TryUInt64ToDecStr(ulong value, Span<TChar> destination, out int charsWritten)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            int bufferLength = FormattingHelpers.CountDigits(value);
            if (bufferLength <= destination.Length)
            {
                charsWritten = bufferLength;
                fixed (TChar* buffer = &MemoryMarshal.GetReference(destination))
                {
                    TChar* p = buffer + bufferLength;
                    p = UInt64ToDecChars(p, value);
                    Debug.Assert(p == buffer);
                }
                return true;
            }

            charsWritten = 0;
            return false;
        }

        public static unsafe bool TryUInt64ToDecStr(ulong value, int digits, Span<TChar> destination, out int charsWritten)
        {
            int countedDigits = FormattingHelpers.CountDigits(value);
            int bufferLength = Math.Max(digits, countedDigits);
            if (bufferLength <= destination.Length)
            {
                charsWritten = bufferLength;
                fixed (TChar* buffer = &MemoryMarshal.GetReference(destination))
                {
                    TChar* p = buffer + bufferLength;
                    p = digits > countedDigits ?
                        UInt64ToDecChars(p, value, digits) :
                        UInt64ToDecChars(p, value);
                    Debug.Assert(p == buffer);
                }
                return true;
            }

            charsWritten = 0;
            return false;
        }

        public static unsafe bool TryUInt128ToBinaryStr(Int128 value, int digits, Span<TChar> destination, out int charsWritten)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            if (digits < 1)
            {
                digits = 1;
            }

            UInt128 uValue = (UInt128)value;

            int bufferLength = Math.Max(digits, 128 - (int)UInt128.LeadingZeroCount((UInt128)value));
            if (bufferLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = bufferLength;
            fixed (TChar* buffer = &MemoryMarshal.GetReference(destination))
            {
                TChar* p = UInt128ToBinaryChars(buffer + bufferLength, uValue, digits);
                Debug.Assert(p == buffer);
            }
            return true;
        }

        public static unsafe bool TryUInt128ToDecStr(UInt128 value, int digits, Span<TChar> destination, out int charsWritten)
        {
            int countedDigits = FormattingHelpers.CountDigits(value);
            int bufferLength = Math.Max(digits, countedDigits);
            if (bufferLength <= destination.Length)
            {
                charsWritten = bufferLength;
                fixed (TChar* buffer = &MemoryMarshal.GetReference(destination))
                {
                    TChar* p = buffer + bufferLength;
                    p = digits > countedDigits ?
                        UInt128ToDecChars(p, value, digits) :
                        UInt128ToDecChars(p, value);
                    Debug.Assert(p == buffer);
                }
                return true;
            }

            charsWritten = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe TChar* UInt32ToBinaryChars(TChar* buffer, uint value, int digits)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            while (--digits >= 0 || value != 0)
            {
                *(--buffer) = TChar.CastFrom('0' + (byte)(value & 0x1));
                value >>= 1;
            }
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe TChar* UInt32ToDecChars(TChar* bufferEnd, uint value)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            if (value >= 10)
            {
                // Handle all values >= 100 two-digits at a time so as to avoid expensive integer division operations.
                while (value >= 100)
                {
                    bufferEnd -= 2;
                    (value, uint remainder) = Math.DivRem(value, 100);
                    WriteTwoDigits(remainder, bufferEnd);
                }

                // If there are two digits remaining, store them.
                if (value >= 10)
                {
                    bufferEnd -= 2;
                    WriteTwoDigits(value, bufferEnd);
                    return bufferEnd;
                }
            }

            // Otherwise, store the single digit remaining.
            *(--bufferEnd) = TChar.CastFrom(value + '0');
            return bufferEnd;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe TChar* UInt32ToDecChars(TChar* bufferEnd, uint value, int digits)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            uint remainder;
            while (value >= 100)
            {
                bufferEnd -= 2;
                digits -= 2;
                (value, remainder) = Math.DivRem(value, 100);
                WriteTwoDigits(remainder, bufferEnd);
            }

            while (value != 0 || digits > 0)
            {
                digits--;
                (value, remainder) = Math.DivRem(value, 10);
                *(--bufferEnd) = TChar.CastFrom(remainder + '0');
            }

            return bufferEnd;
        }

#if TARGET_64BIT
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static unsafe TChar* UInt64ToBinaryChars(TChar* buffer, ulong value, int digits)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));
#if TARGET_32BIT
            uint lower = (uint)value;
            uint upper = (uint)(value >> 32);

            if (upper != 0)
            {
                buffer = UInt32ToBinaryChars(buffer, lower, 32);
                return UInt32ToBinaryChars(buffer, upper, digits - 32);
            }
            else
            {
                return UInt32ToBinaryChars(buffer, lower, Math.Max(digits, 1));
            }
#else
            while (--digits >= 0 || value != 0)
            {
                *(--buffer) = TChar.CastFrom('0' + (byte)(value & 0x1));
                value >>= 1;
            }
            return buffer;
#endif
        }

#if TARGET_64BIT
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static unsafe TChar* UInt64ToDecChars(TChar* bufferEnd, ulong value)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

#if TARGET_32BIT
            while ((uint)(value >> 32) != 0)
            {
                bufferEnd = UInt32ToDecChars(bufferEnd, Number.Int64DivMod1E9(ref value), 9);
            }
            return UInt32ToDecChars(bufferEnd, (uint)value);
#else
            if (value >= 10)
            {
                // Handle all values >= 100 two-digits at a time so as to avoid expensive integer division operations.
                while (value >= 100)
                {
                    bufferEnd -= 2;
                    (value, ulong remainder) = Math.DivRem(value, 100);
                    WriteTwoDigits((uint)remainder, bufferEnd);
                }

                // If there are two digits remaining, store them.
                if (value >= 10)
                {
                    bufferEnd -= 2;
                    WriteTwoDigits((uint)value, bufferEnd);
                    return bufferEnd;
                }
            }

            // Otherwise, store the single digit remaining.
            *(--bufferEnd) = TChar.CastFrom(value + '0');
            return bufferEnd;
#endif
        }

#if TARGET_64BIT
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static unsafe TChar* UInt64ToDecChars(TChar* bufferEnd, ulong value, int digits)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

#if TARGET_32BIT
            while ((uint)(value >> 32) != 0)
            {
                bufferEnd = UInt32ToDecChars(bufferEnd, Number.Int64DivMod1E9(ref value), 9);
                digits -= 9;
            }
            return UInt32ToDecChars(bufferEnd, (uint)value, digits);
#else
            ulong remainder;
            while (value >= 100)
            {
                bufferEnd -= 2;
                digits -= 2;
                (value, remainder) = Math.DivRem(value, 100);
                WriteTwoDigits((uint)remainder, bufferEnd);
            }

            while (value != 0 || digits > 0)
            {
                digits--;
                (value, remainder) = Math.DivRem(value, 10);
                *(--bufferEnd) = TChar.CastFrom(remainder + '0');
            }

            return bufferEnd;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe TChar* UInt128ToBinaryChars(TChar* buffer, UInt128 value, int digits)
        {
            ulong lower = value.Lower;
            ulong upper = value.Upper;

            if (upper != 0)
            {
                buffer = UInt64ToBinaryChars(buffer, lower, 64);
                return UInt64ToBinaryChars(buffer, upper, digits - 64);
            }
            else
            {
                return UInt64ToBinaryChars(buffer, lower, Math.Max(digits, 1));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe TChar* UInt128ToDecChars(TChar* bufferEnd, UInt128 value)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            while (value.Upper != 0)
            {
                bufferEnd = UInt64ToDecChars(bufferEnd, Number.Int128DivMod1E19(ref value), 19);
            }
            return UInt64ToDecChars(bufferEnd, value.Lower);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe TChar* UInt128ToDecChars(TChar* bufferEnd, UInt128 value, int digits)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            while (value.Upper != 0)
            {
                bufferEnd = UInt64ToDecChars(bufferEnd, Number.Int128DivMod1E19(ref value), 19);
                digits -= 19;
            }
            return UInt64ToDecChars(bufferEnd, value.Lower, digits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteDigits(uint value, TChar* ptr, int count)
        {
            TChar* cur;
            for (cur = ptr + count - 1; cur > ptr; cur--)
            {
                uint temp = '0' + value;
                value /= 10;
                *cur = TChar.CastFrom(temp - (value * 10));
            }

            Debug.Assert(value < 10);
            Debug.Assert(cur == ptr);
            *cur = TChar.CastFrom('0' + value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteTwoDigits(uint value, TChar* ptr)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));
            Debug.Assert(value <= 99);

            Unsafe.CopyBlockUnaligned(
                ref *(byte*)ptr,
                ref Unsafe.Add(ref Number.GetTwoDigitsBytesRef(typeof(TChar) == typeof(char)), (uint)sizeof(TChar) * 2 * value),
                (uint)sizeof(TChar) * 2);
        }

        /// <summary>
        /// Writes a value [ 0000 .. 9999 ] to the buffer starting at the specified offset.
        /// This method performs best when the starting index is a constant literal.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteFourDigits(uint value, TChar* ptr)
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));
            Debug.Assert(value <= 9999);

            (value, uint remainder) = Math.DivRem(value, 100);

            ref byte charsArray = ref Number.GetTwoDigitsBytesRef(typeof(TChar) == typeof(char));

            Unsafe.CopyBlockUnaligned(
                ref *(byte*)ptr,
                ref Unsafe.Add(ref charsArray, (uint)sizeof(TChar) * 2 * value),
                (uint)sizeof(TChar) * 2);

            Unsafe.CopyBlockUnaligned(
                ref *(byte*)(ptr + 2),
                ref Unsafe.Add(ref charsArray, (uint)sizeof(TChar) * 2 * remainder),
                (uint)sizeof(TChar) * 2);
        }
    }
}
