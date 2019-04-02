// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
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
        private static bool TryFormatInt64Default(long value, Span<byte> destination, out int bytesWritten)
        {
            if ((ulong)value < 10)
            {
                return TryFormatUInt32SingleDigit((uint)value, destination, out bytesWritten);
            }

            if (IntPtr.Size == 8)    // x64
            {
                return TryFormatInt64MultipleDigits(value, destination, out bytesWritten);
            }
            else    // x86
            {
                if (value <= int.MaxValue && value >= int.MinValue)
                {
                    return TryFormatInt32MultipleDigits((int)value, destination, out bytesWritten);
                }
                else
                {
                    if (value <= (long)Utf8Constants.BillionMaxUIntValue && value >= -(long)Utf8Constants.BillionMaxUIntValue)
                    {
                        return value < 0 ?
                        TryFormatInt64MoreThanNegativeBillionMaxUInt(-value, destination, out bytesWritten) :
                        TryFormatUInt64LessThanBillionMaxUInt((ulong)value, destination, out bytesWritten);
                    }
                    else
                    {
                        return value < 0 ?
                        TryFormatInt64LessThanNegativeBillionMaxUInt(-value, destination, out bytesWritten) :
                        TryFormatUInt64MoreThanBillionMaxUInt((ulong)value, destination, out bytesWritten);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryFormatInt32MultipleDigits(int value, Span<byte> destination, out int bytesWritten)
        {
            if (value < 0)
            {
                value = -value;
                int digitCount = FormattingHelpers.CountDigits((uint)value);
                // WriteDigits does not do bounds checks
                if (digitCount >= destination.Length)
                {
                    bytesWritten = 0;
                    return false;
                }
                destination[0] = Utf8Constants.Minus;
                bytesWritten = digitCount + 1;
                FormattingHelpers.WriteDigits((uint)value, destination.Slice(1, digitCount));
                return true;
            }
            else
            {
                return TryFormatUInt32MultipleDigits((uint)value, destination, out bytesWritten);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryFormatInt64MultipleDigits(long value, Span<byte> destination, out int bytesWritten)
        {
            if (value < 0)
            {
                value = -value;
                int digitCount = FormattingHelpers.CountDigits((ulong)value);
                // WriteDigits does not do bounds checks
                if (digitCount >= destination.Length)
                {
                    bytesWritten = 0;
                    return false;
                }
                destination[0] = Utf8Constants.Minus;
                bytesWritten = digitCount + 1;
                FormattingHelpers.WriteDigits((ulong)value, destination.Slice(1, digitCount));
                return true;
            }
            else
            {
                return TryFormatUInt64MultipleDigits((ulong)value, destination, out bytesWritten);
            }
        }

        // Split long into two parts that can each fit in a uint - {1-10 digits}{9 digits}
        private static bool TryFormatInt64MoreThanNegativeBillionMaxUInt(long value, Span<byte> destination, out int bytesWritten)
        {
            uint overNineDigits = (uint)(value / Utf8Constants.Billion);
            uint lastNineDigits = (uint)(value - (overNineDigits * Utf8Constants.Billion));

            int digitCountOverNineDigits = FormattingHelpers.CountDigits(overNineDigits);
            Debug.Assert(digitCountOverNineDigits >= 1 && digitCountOverNineDigits <= 10);
            int digitCount = digitCountOverNineDigits + 9;
            // WriteDigits does not do bounds checks
            if (digitCount >= destination.Length)
            {
                bytesWritten = 0;
                return false;
            }
            destination[0] = Utf8Constants.Minus;
            bytesWritten = digitCount + 1;
            FormattingHelpers.WriteDigits(overNineDigits, destination.Slice(1, digitCountOverNineDigits));
            FormattingHelpers.WriteDigits(lastNineDigits, destination.Slice(digitCountOverNineDigits + 1, 9));
            return true;
        }

        // Split long into three parts that can each fit in a uint - {1 digit}{9 digits}{9 digits}
        private static bool TryFormatInt64LessThanNegativeBillionMaxUInt(long value, Span<byte> destination, out int bytesWritten)
        {
            // value can still be negative if value == long.MinValue
            // Therefore, cast to ulong, since (ulong)value actually equals abs(long.MinValue)
            ulong overNineDigits = (ulong)value / Utf8Constants.Billion;
            uint lastNineDigits = (uint)((ulong)value - (overNineDigits * Utf8Constants.Billion));
            uint overEighteenDigits = (uint)(overNineDigits / Utf8Constants.Billion);
            uint middleNineDigits = (uint)(overNineDigits - (overEighteenDigits * Utf8Constants.Billion));

            int digitCountOverEighteenDigits = FormattingHelpers.CountDigits(overEighteenDigits);
            Debug.Assert(digitCountOverEighteenDigits == 1);
            int digitCount = digitCountOverEighteenDigits + 18;
            // WriteDigits does not do bounds checks
            if (digitCount >= destination.Length)
            {
                bytesWritten = 0;
                return false;
            }
            destination[0] = Utf8Constants.Minus;
            bytesWritten = digitCount + 1;
            FormattingHelpers.WriteDigits(overEighteenDigits, destination.Slice(1, digitCountOverEighteenDigits));
            FormattingHelpers.WriteDigits(middleNineDigits, destination.Slice(digitCountOverEighteenDigits + 1, 9));
            FormattingHelpers.WriteDigits(lastNineDigits, destination.Slice(digitCountOverEighteenDigits + 1 + 9, 9));
            return true;
        }
    }
}
