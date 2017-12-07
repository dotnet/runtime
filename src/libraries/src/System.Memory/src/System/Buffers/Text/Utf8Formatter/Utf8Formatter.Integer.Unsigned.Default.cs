// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Buffers.Text
{
    /// <summary>
    /// Methods to format common data types as Utf8 strings.
    /// </summary>
    public static partial class Utf8Formatter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryFormatUInt64Default(ulong value, Span<byte> buffer, out int bytesWritten)
        {
            if (value < 10)
            {
                return TryFormatUInt32SingleDigit((uint)value, buffer, out bytesWritten);
            }

            if (IntPtr.Size == 8)    // x64
            {
                return TryFormatUInt64MultipleDigits(value, buffer, out bytesWritten);
            }
            else    // x86
            {
                if (value <= uint.MaxValue)
                {
                    return TryFormatUInt32MultipleDigits((uint)value, buffer, out bytesWritten);
                }
                else
                {
                    if (value <= Utf8Constants.BillionMaxUIntValue)
                    {
                        return TryFormatUInt64LessThanBillionMaxUInt(value, buffer, out bytesWritten);
                    }
                    else
                    {
                        return TryFormatUInt64MoreThanBillionMaxUInt(value, buffer, out bytesWritten);
                    }
                }
            }
        }

        // TODO: Use this instead of TryFormatUInt64Default to format numbers less than uint.MaxValue
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryFormatUInt32Default(uint value, Span<byte> buffer, out int bytesWritten)
        {
            if (value < 10)
            {
                return TryFormatUInt32SingleDigit(value, buffer, out bytesWritten);
            }
            return TryFormatUInt32MultipleDigits(value, buffer, out bytesWritten);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryFormatUInt32SingleDigit(uint value, Span<byte> buffer, out int bytesWritten)
        {
            if (buffer.Length == 0)
            {
                bytesWritten = 0;
                return false;
            }
            buffer[0] = (byte)('0' + value);
            bytesWritten = 1;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryFormatUInt32MultipleDigits(uint value, Span<byte> buffer, out int bytesWritten)
        {
            int digitCount = FormattingHelpers.CountDigits(value);
            // WriteDigits does not do bounds checks
            if (digitCount > buffer.Length)
            {
                bytesWritten = 0;
                return false;
            }
            bytesWritten = digitCount;
            FormattingHelpers.WriteDigits(value, buffer.Slice(0, digitCount));
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryFormatUInt64SingleDigit(ulong value, Span<byte> buffer, out int bytesWritten)
        {
            if (buffer.Length == 0)
            {
                bytesWritten = 0;
                return false;
            }
            buffer[0] = (byte)('0' + value);
            bytesWritten = 1;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryFormatUInt64MultipleDigits(ulong value, Span<byte> buffer, out int bytesWritten)
        {
            int digitCount = FormattingHelpers.CountDigits(value);
            // WriteDigits does not do bounds checks
            if (digitCount > buffer.Length)
            {
                bytesWritten = 0;
                return false;
            }
            bytesWritten = digitCount;
            FormattingHelpers.WriteDigits(value, buffer.Slice(0, digitCount));
            return true;
        }

        // Split ulong into two parts that can each fit in a uint - {1-10 digits}{9 digits}
        private static bool TryFormatUInt64LessThanBillionMaxUInt(ulong value, Span<byte> buffer, out int bytesWritten)
        {
            uint overNineDigits = (uint)(value / Utf8Constants.Billion);
            uint lastNineDigits = (uint)(value - (overNineDigits * Utf8Constants.Billion));

            int digitCountOverNineDigits = FormattingHelpers.CountDigits(overNineDigits);
            Debug.Assert(digitCountOverNineDigits >= 1 && digitCountOverNineDigits <= 10);
            int digitCount = digitCountOverNineDigits + 9;
            // WriteDigits does not do bounds checks
            if (digitCount > buffer.Length)
            {
                bytesWritten = 0;
                return false;
            }
            bytesWritten = digitCount;
            FormattingHelpers.WriteDigits(overNineDigits, buffer.Slice(0, digitCountOverNineDigits));
            FormattingHelpers.WriteDigits(lastNineDigits, buffer.Slice(digitCountOverNineDigits, 9));
            return true;
        }

        // Split ulong into three parts that can each fit in a uint - {1-2 digits}{9 digits}{9 digits}
        private static bool TryFormatUInt64MoreThanBillionMaxUInt(ulong value, Span<byte> buffer, out int bytesWritten)
        {
            ulong overNineDigits = value / Utf8Constants.Billion;
            uint lastNineDigits = (uint)(value - (overNineDigits * Utf8Constants.Billion));
            uint overEighteenDigits = (uint)(overNineDigits / Utf8Constants.Billion);
            uint middleNineDigits = (uint)(overNineDigits - (overEighteenDigits * Utf8Constants.Billion));

            int digitCountOverEighteenDigits = FormattingHelpers.CountDigits(overEighteenDigits);
            Debug.Assert(digitCountOverEighteenDigits >= 1 && digitCountOverEighteenDigits <= 2);
            int digitCount = digitCountOverEighteenDigits + 18;
            // WriteDigits does not do bounds checks
            if (digitCount > buffer.Length)
            {
                bytesWritten = 0;
                return false;
            }
            bytesWritten = digitCount;
            FormattingHelpers.WriteDigits(overEighteenDigits, buffer.Slice(0, digitCountOverEighteenDigits));
            FormattingHelpers.WriteDigits(middleNineDigits, buffer.Slice(digitCountOverEighteenDigits, 9));
            FormattingHelpers.WriteDigits(lastNineDigits, buffer.Slice(digitCountOverEighteenDigits + 9, 9));
            return true;
        }
    }
}
