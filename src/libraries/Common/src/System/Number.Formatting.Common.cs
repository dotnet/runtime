// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    internal static partial class Number
    {
        private const int CharStackBufferSize = 32;

        private const int DefaultPrecisionExponentialFormat = 6;

        private static ReadOnlySpan<byte> GetCurrencyFormat(bool isNegative, int index)
        {
            if (isNegative)
            {
                return index switch
                {
                    0 => "($#)"u8,
                    1 => "-$#"u8,
                    2 => "$-#"u8,
                    3 => "$#-"u8,
                    4 => "(#$)"u8,
                    5 => "-#$"u8,
                    6 => "#-$"u8,
                    7 => "#$-"u8,
                    8 => "-# $"u8,
                    9 => "-$ #"u8,
                    10 => "# $-"u8,
                    11 => "$ #-"u8,
                    12 => "$ -#"u8,
                    13 => "#- $"u8,
                    14 => "($ #)"u8,
                    15 => "(# $)"u8,
                    16 => "$- #"u8,
                    _ => throw new UnreachableException(),
                };
            }

            return index switch
            {
                0 => "$#"u8,
                1 => "#$"u8,
                2 => "$ #"u8,
                3 => "# $"u8,
                _ => throw new UnreachableException(),
            };
        }

        private static ReadOnlySpan<byte> GetPercentFormat(bool isNegative, int index)
        {
            if (isNegative)
            {
                return index switch
                {
                    0 => "-# %"u8,
                    1 => "-#%"u8,
                    2 => "-%#"u8,
                    3 => "%-#"u8,
                    4 => "%#-"u8,
                    5 => "#-%"u8,
                    6 => "#%-"u8,
                    7 => "-% #"u8,
                    8 => "# %-"u8,
                    9 => "% #-"u8,
                    10 => "% -#"u8,
                    11 => "#- %"u8,
                    _ => throw new UnreachableException(),
                };
            }

            return index switch
            {
                0 => "# %"u8,
                1 => "#%"u8,
                2 => "%#"u8,
                3 => "% #"u8,
                _ => throw new UnreachableException(),
            };
        }

        private static ReadOnlySpan<byte> GetNumberFormat(bool isNegative, int index)
        {
            if (!isNegative)
            {
                return "#"u8;
            }

            return index switch
            {
                0 => "(#)"u8,
                1 => "-#"u8,
                2 => "- #"u8,
                3 => "#-"u8,
                4 => "# -"u8,
                _ => throw new UnreachableException(),
            };
        }

        internal static char ParseFormatSpecifier(ReadOnlySpan<char> format, out int digits)
        {
            char c = default;
            if (format.Length > 0)
            {
                // If the format begins with a symbol, see if it's a standard format
                // with or without a specified number of digits.
                c = format[0];
                if (char.IsAsciiLetter(c))
                {
                    // Fast path for sole symbol, e.g. "D"
                    if (format.Length == 1)
                    {
                        digits = -1;
                        return c;
                    }

                    if (format.Length == 2)
                    {
                        // Fast path for symbol and single digit, e.g. "X4"
                        int d = format[1] - '0';
                        if ((uint)d < 10)
                        {
                            digits = d;
                            return c;
                        }
                    }
                    else if (format.Length == 3)
                    {
                        // Fast path for symbol and double digit, e.g. "F12"
                        int d1 = format[1] - '0', d2 = format[2] - '0';
                        if ((uint)d1 < 10 && (uint)d2 < 10)
                        {
                            digits = d1 * 10 + d2;
                            return c;
                        }
                    }

                    // Fallback for symbol and any length digits.  The digits value must be >= 0 && <= 999_999_999,
                    // but it can begin with any number of 0s, and thus we may need to check more than 9
                    // digits.  Further, for compat, we need to stop when we hit a null char.
                    int n = 0;
                    int i = 1;
                    while ((uint)i < (uint)format.Length && char.IsAsciiDigit(format[i]))
                    {
                        // Check if we are about to overflow past our limit of 9 digits
                        if (n >= 100_000_000)
                        {
                            ThrowHelper.ThrowFormatException_BadFormatSpecifier();
                        }
                        n = (n * 10) + format[i++] - '0';
                    }

                    // If we're at the end of the digits rather than having stopped because we hit something
                    // other than a digit or overflowed, return the standard format info.
                    if ((uint)i >= (uint)format.Length || format[i] == '\0')
                    {
                        digits = n;
                        return c;
                    }
                }
            }

            // Default empty format to be "G"; custom format is signified with '\0'.
            digits = -1;
            return format.Length == 0 || c == '\0' ? // For compat, treat '\0' as the end of the specifier, even if the specifier extends beyond it.
                'G' :
                '\0';
        }

        // Optimizations using "TwoDigits" inspired by:
        // https://engineering.fb.com/2013/03/15/developer-tools/three-optimization-tips-for-c/
        // entry[v] = (byte)('0' + v/10) | ((byte)('0' + v%10) << 8), for writing two UTF-8 bytes as a single 2-byte store
        private static ReadOnlySpan<ushort> TwoDigitsBytesTable =>
        [
            0x3030, 0x3130, 0x3230, 0x3330, 0x3430, 0x3530, 0x3630, 0x3730, 0x3830, 0x3930,
            0x3031, 0x3131, 0x3231, 0x3331, 0x3431, 0x3531, 0x3631, 0x3731, 0x3831, 0x3931,
            0x3032, 0x3132, 0x3232, 0x3332, 0x3432, 0x3532, 0x3632, 0x3732, 0x3832, 0x3932,
            0x3033, 0x3133, 0x3233, 0x3333, 0x3433, 0x3533, 0x3633, 0x3733, 0x3833, 0x3933,
            0x3034, 0x3134, 0x3234, 0x3334, 0x3434, 0x3534, 0x3634, 0x3734, 0x3834, 0x3934,
            0x3035, 0x3135, 0x3235, 0x3335, 0x3435, 0x3535, 0x3635, 0x3735, 0x3835, 0x3935,
            0x3036, 0x3136, 0x3236, 0x3336, 0x3436, 0x3536, 0x3636, 0x3736, 0x3836, 0x3936,
            0x3037, 0x3137, 0x3237, 0x3337, 0x3437, 0x3537, 0x3637, 0x3737, 0x3837, 0x3937,
            0x3038, 0x3138, 0x3238, 0x3338, 0x3438, 0x3538, 0x3638, 0x3738, 0x3838, 0x3938,
            0x3039, 0x3139, 0x3239, 0x3339, 0x3439, 0x3539, 0x3639, 0x3739, 0x3839, 0x3939,
        ];

        // entry[v] = (char)('0' + v/10) | ((char)('0' + v%10) << 16), for writing two UTF-16 chars as a single 4-byte store
        private static ReadOnlySpan<uint> TwoDigitsCharsTable =>
        [
            0x00300030u, 0x00310030u, 0x00320030u, 0x00330030u, 0x00340030u, 0x00350030u, 0x00360030u, 0x00370030u, 0x00380030u, 0x00390030u,
            0x00300031u, 0x00310031u, 0x00320031u, 0x00330031u, 0x00340031u, 0x00350031u, 0x00360031u, 0x00370031u, 0x00380031u, 0x00390031u,
            0x00300032u, 0x00310032u, 0x00320032u, 0x00330032u, 0x00340032u, 0x00350032u, 0x00360032u, 0x00370032u, 0x00380032u, 0x00390032u,
            0x00300033u, 0x00310033u, 0x00320033u, 0x00330033u, 0x00340033u, 0x00350033u, 0x00360033u, 0x00370033u, 0x00380033u, 0x00390033u,
            0x00300034u, 0x00310034u, 0x00320034u, 0x00330034u, 0x00340034u, 0x00350034u, 0x00360034u, 0x00370034u, 0x00380034u, 0x00390034u,
            0x00300035u, 0x00310035u, 0x00320035u, 0x00330035u, 0x00340035u, 0x00350035u, 0x00360035u, 0x00370035u, 0x00380035u, 0x00390035u,
            0x00300036u, 0x00310036u, 0x00320036u, 0x00330036u, 0x00340036u, 0x00350036u, 0x00360036u, 0x00370036u, 0x00380036u, 0x00390036u,
            0x00300037u, 0x00310037u, 0x00320037u, 0x00330037u, 0x00340037u, 0x00350037u, 0x00360037u, 0x00370037u, 0x00380037u, 0x00390037u,
            0x00300038u, 0x00310038u, 0x00320038u, 0x00330038u, 0x00340038u, 0x00350038u, 0x00360038u, 0x00370038u, 0x00380038u, 0x00390038u,
            0x00300039u, 0x00310039u, 0x00320039u, 0x00330039u, 0x00340039u, 0x00350039u, 0x00360039u, 0x00370039u, 0x00380039u, 0x00390039u,
        ];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort GetTwoDigitsBytes(uint value)
        {
            ushort pair = TwoDigitsBytesTable[(int)value];
            if (!BitConverter.IsLittleEndian)
            {
                pair = (ushort)((pair << 8) | (pair >> 8));
            }
            return pair;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint GetTwoDigitsChars(uint value)
        {
            uint pair = TwoDigitsCharsTable[(int)value];
            if (!BitConverter.IsLittleEndian)
            {
                pair = uint.RotateRight(pair, 16);
            }
            return pair;
        }

        /// <summary>Writes a value [ 00 .. 99 ] to the start of a pre-sliced 2-element span, using a single store.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteTwoDigits<TChar>(uint value, Span<TChar> destination) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(value <= 99);
            Debug.Assert(destination.Length >= 2);
            Debug.Assert(sizeof(TChar) is sizeof(char) or sizeof(byte));

            if (sizeof(TChar) == sizeof(char))
            {
                // TwoDigitsCharsTable[v] = (char)('0'+v/10) | ((char)('0'+v%10) << 16) — write both chars as one 4-byte store.
                uint pair = GetTwoDigitsChars(value);
                MemoryMarshal.Write(MemoryMarshal.AsBytes(Unsafe.BitCast<Span<TChar>, Span<char>>(destination)), in pair);
            }
            else
            {
                // Write both bytes as a single 2-byte store.
                ushort pair = GetTwoDigitsBytes(value);
                MemoryMarshal.Write(Unsafe.BitCast<Span<TChar>, Span<byte>>(destination), in pair);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteTwoDigits<TChar>(uint value, Span<TChar> buffer, int index) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(value <= 99);
            WriteTwoDigits(value, buffer.Slice(index, 2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CopyNegativeSign<TChar>(ReadOnlySpan<TChar> sign, Span<TChar> destination)
        {
            if (sign.Length == 1)
            {
                destination[0] = sign[0];
            }
            else
            {
                sign.CopyTo(destination);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int UInt32ToDecChars<TChar>(Span<TChar> buffer, int index, uint value) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(sizeof(TChar) is sizeof(char) or sizeof(byte));

            if (value >= 10)
            {
                // Handle all values >= 100 two-digits at a time so as to avoid expensive integer division operations.
                while (value >= 100)
                {
                    index -= 2;
                    (value, uint remainder) = Math.DivRem(value, 100);
                    WriteTwoDigits(remainder, buffer, index);
                }

                // If there are two digits remaining, store them.
                if (value >= 10)
                {
                    index -= 2;
                    WriteTwoDigits(value, buffer, index);
                    return index;
                }
            }

            // Otherwise, store the single digit remaining.
            buffer[--index] = TChar.CastFrom(value + '0');
            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int UInt32ToDecChars<TChar>(Span<TChar> buffer, int index, uint value, int digits) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(sizeof(TChar) is sizeof(char) or sizeof(byte));

            uint remainder;
            while (value >= 100)
            {
                index -= 2;
                digits -= 2;
                (value, remainder) = Math.DivRem(value, 100);
                WriteTwoDigits(remainder, buffer, index);
            }
            while (value != 0 || digits > 0)
            {
                digits--;
                (value, remainder) = Math.DivRem(value, 10);
                buffer[--index] = TChar.CastFrom(remainder + '0');
            }
            return index;
        }

        internal static void NumberToString<TChar>(ref ValueListBuilder<TChar> vlb, ref NumberBuffer number, char format, int nMaxDigits, NumberFormatInfo info) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(sizeof(TChar) is sizeof(char) or sizeof(byte));

            number.CheckConsistency();
            bool isCorrectlyRounded = (number.Kind == NumberBufferKind.FloatingPoint);

            switch (format)
            {
                case 'C':
                case 'c':
                    {
                        if (nMaxDigits < 0)
                        {
                            nMaxDigits = info.CurrencyDecimalDigits;
                        }

                        RoundNumber(ref number, number.Scale + nMaxDigits, isCorrectlyRounded); // Don't change this line to use digPos since digCount could have its sign changed.

                        FormatCurrency(ref vlb, ref number, nMaxDigits, info);

                        break;
                    }

                case 'F':
                case 'f':
                    {
                        if (nMaxDigits < 0)
                        {
                            nMaxDigits = info.NumberDecimalDigits;
                        }

                        RoundNumber(ref number, number.Scale + nMaxDigits, isCorrectlyRounded);

                        if (number.IsNegative)
                        {
                            vlb.Append(info.NegativeSignTChar<TChar>());
                        }

                        FormatFixed(ref vlb, ref number, nMaxDigits, null, info.NumberDecimalSeparatorTChar<TChar>(), null);

                        break;
                    }

                case 'N':
                case 'n':
                    {
                        if (nMaxDigits < 0)
                        {
                            nMaxDigits = info.NumberDecimalDigits; // Since we are using digits in our calculation
                        }

                        RoundNumber(ref number, number.Scale + nMaxDigits, isCorrectlyRounded);

                        FormatNumber(ref vlb, ref number, nMaxDigits, info);

                        break;
                    }

                case 'E':
                case 'e':
                    {
                        if (nMaxDigits < 0)
                        {
                            nMaxDigits = DefaultPrecisionExponentialFormat;
                        }
                        nMaxDigits++;

                        RoundNumber(ref number, nMaxDigits, isCorrectlyRounded);

                        if (number.IsNegative)
                        {
                            vlb.Append(info.NegativeSignTChar<TChar>());
                        }

                        FormatScientific(ref vlb, ref number, nMaxDigits, info, format);

                        break;
                    }

                case 'G':
                case 'g':
                    {
                        bool noRounding = false;
                        if (nMaxDigits < 1)
                        {
                            if ((number.Kind == NumberBufferKind.Decimal) && (nMaxDigits == -1))
                            {
                                noRounding = true;  // Turn off rounding for ECMA compliance to output trailing 0's after decimal as significant

                                if (number.Digits[0] == 0)
                                {
                                    // -0 should be formatted as 0 for decimal. This is normally handled by RoundNumber (which we are skipping)
                                    goto SkipSign;
                                }

                                goto SkipRounding;
                            }
                            else
                            {
                                // This ensures that the PAL code pads out to the correct place even when we use the default precision
                                nMaxDigits = number.DigitsCount;
                            }
                        }

                        RoundNumber(ref number, nMaxDigits, isCorrectlyRounded);

                    SkipRounding:
                        if (number.IsNegative)
                        {
                            vlb.Append(info.NegativeSignTChar<TChar>());
                        }

                    SkipSign:
                        FormatGeneral(ref vlb, ref number, nMaxDigits, info, (char)(format - ('G' - 'E')), noRounding);

                        break;
                    }

                case 'P':
                case 'p':
                    {
                        if (nMaxDigits < 0)
                        {
                            nMaxDigits = info.PercentDecimalDigits;
                        }
                        number.Scale += 2;

                        RoundNumber(ref number, number.Scale + nMaxDigits, isCorrectlyRounded);

                        FormatPercent(ref vlb, ref number, nMaxDigits, info);

                        break;
                    }

                case 'R':
                case 'r':
                    {
                        format = (char)(format - ('R' - 'G'));
                        Debug.Assert(format is 'G' or 'g');
                        goto case 'G';
                    }

                default:
                    ThrowHelper.ThrowFormatException_BadFormatSpecifier();
                    break;
            }
        }

        internal static void NumberToStringFormat<TChar>(ref ValueListBuilder<TChar> vlb, ref NumberBuffer number, ReadOnlySpan<char> format, NumberFormatInfo info) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(sizeof(TChar) is sizeof(char) or sizeof(byte));

            number.CheckConsistency();

            int digitCount;
            int decimalPos;
            int firstDigit;
            int lastDigit;
            int digPos;
            bool scientific;
            int thousandPos;
            int thousandCount = 0;
            bool thousandSeps;
            int scaleAdjust;
            int adjust;

            int section;
            int src;
            char ch;

            section = FindSection(format, number.Digits[0] == 0 ? 2 : number.IsNegative ? 1 : 0);

            while (true)
            {
                digitCount = 0;
                decimalPos = -1;
                firstDigit = 0x7FFFFFFF;
                lastDigit = 0;
                scientific = false;
                thousandPos = -1;
                thousandSeps = false;
                scaleAdjust = 0;
                src = section;

                while (src < format.Length && (ch = format[src++]) != 0 && ch != ';')
                {
                    switch (ch)
                    {
                        case '#':
                            digitCount++;
                            break;

                        case '0':
                            if (firstDigit == 0x7FFFFFFF)
                            {
                                firstDigit = digitCount;
                            }
                            digitCount++;
                            lastDigit = digitCount;
                            break;

                        case '.':
                            if (decimalPos < 0)
                            {
                                decimalPos = digitCount;
                            }
                            break;

                        case ',':
                            if (digitCount > 0 && decimalPos < 0)
                            {
                                if (thousandPos >= 0)
                                {
                                    if (thousandPos == digitCount)
                                    {
                                        thousandCount++;
                                        break;
                                    }
                                    thousandSeps = true;
                                }
                                thousandPos = digitCount;
                                thousandCount = 1;
                            }
                            break;

                        case '%':
                            scaleAdjust += 2;
                            break;

                        case '\x2030':
                            scaleAdjust += 3;
                            break;

                        case '\'':
                        case '"':
                            while (src < format.Length && format[src] != 0 && format[src++] != ch) ;
                            break;

                        case '\\':
                            if (src < format.Length && format[src] != 0)
                            {
                                src++;
                            }
                            break;

                        case 'E':
                        case 'e':
                            if ((src < format.Length && format[src] == '0') ||
                                (src + 1 < format.Length && (format[src] == '+' || format[src] == '-') && format[src + 1] == '0'))
                            {
                                while (++src < format.Length && format[src] == '0') ;
                                scientific = true;
                            }
                            break;
                    }
                }

                if (decimalPos < 0)
                {
                    decimalPos = digitCount;
                }

                if (thousandPos >= 0)
                {
                    if (thousandPos == decimalPos)
                    {
                        scaleAdjust -= thousandCount * 3;
                    }
                    else
                    {
                        thousandSeps = true;
                    }
                }

                if (number.Digits[0] != 0)
                {
                    number.Scale += scaleAdjust;
                    int pos = scientific ? digitCount : number.Scale + digitCount - decimalPos;
                    RoundNumber(ref number, pos, isCorrectlyRounded: false);
                    if (number.Digits[0] == 0)
                    {
                        src = FindSection(format, 2);
                        if (src != section)
                        {
                            section = src;
                            continue;
                        }
                    }
                }
                else
                {
                    if (number.Kind is not (NumberBufferKind.FloatingPoint or NumberBufferKind.DecimalIeee754))
                    {
                        // The integer types don't have a concept of -0 and decimal always format -0 as 0
                        number.IsNegative = false;
                    }
                    number.Scale = 0;      // Decimals with scale ('0.00') should be rounded.
                }

                break;
            }

            firstDigit = firstDigit < decimalPos ? decimalPos - firstDigit : 0;
            lastDigit = lastDigit > decimalPos ? decimalPos - lastDigit : 0;
            if (scientific)
            {
                digPos = decimalPos;
                adjust = 0;
            }
            else
            {
                digPos = number.Scale > decimalPos ? number.Scale : decimalPos;
                adjust = number.Scale - decimalPos;
            }
            src = section;

            // Adjust can be negative, so we make this an int instead of an unsigned int.
            // Adjust represents the number of characters over the formatting e.g. format string is "0000" and you are trying to
            // format 100000 (6 digits). Means adjust will be 2. On the other hand if you are trying to format 10 adjust will be
            // -2 and we'll need to fixup these digits with 0 padding if we have 0 formatting as in this example.
            Span<int> thousandsSepPos = [0, 0, 0, 0];
            int thousandsSepCtr = -1;

            if (thousandSeps)
            {
                // We need to precompute this outside the number formatting loop
                if (info.NumberGroupSeparator.Length > 0)
                {
                    // We need this array to figure out where to insert the thousands separator. We would have to traverse the string
                    // backwards. PIC formatting always traverses forwards. These indices are precomputed to tell us where to insert
                    // the thousands separator so we can get away with traversing forwards. Note we only have to compute up to digPos.
                    // The max is not bound since you can have formatting strings of the form "000,000..", and this
                    // should handle that case too.

                    int[] groupDigits = info.NumberGroupSizes();

                    int groupSizeIndex = 0;     // Index into the groupDigits array.
                    int groupTotalSizeCount = 0;
                    int groupSizeLen = groupDigits.Length;    // The length of groupDigits array.
                    if (groupSizeLen != 0)
                    {
                        groupTotalSizeCount = groupDigits[groupSizeIndex];   // The current running total of group size.
                    }
                    int groupSize = groupTotalSizeCount;

                    int totalDigits = digPos + ((adjust < 0) ? adjust : 0); // Actual number of digits in o/p
                    int numDigits = (firstDigit > totalDigits) ? firstDigit : totalDigits;
                    while (numDigits > groupTotalSizeCount)
                    {
                        if (groupSize == 0)
                        {
                            break;
                        }

                        ++thousandsSepCtr;
                        if (thousandsSepCtr >= thousandsSepPos.Length)
                        {
                            var newThousandsSepPos = new int[thousandsSepPos.Length * 2];
                            thousandsSepPos.CopyTo(newThousandsSepPos);
                            thousandsSepPos = newThousandsSepPos;
                        }

                        thousandsSepPos[thousandsSepCtr] = groupTotalSizeCount;
                        if (groupSizeIndex < groupSizeLen - 1)
                        {
                            groupSizeIndex++;
                            groupSize = groupDigits[groupSizeIndex];
                        }
                        groupTotalSizeCount += groupSize;
                    }
                }
            }

            // A dedicated negative section (the portion after the first ';') is responsible for
            // emitting the sign of negative values. When a negative value rounds to zero -- or is
            // negative zero -- it can fall back to the first section (for example -0.001 or -0.0
            // with "+0.00;-0.00"). In that case the first section already contains the caller's
            // desired representation and we must not emit an extra sign, which would otherwise
            // produce output such as "-+0.00". This only matters when 'section == 0', so
            // 'HasNegativeSection' is evaluated lazily behind that check to avoid an extra format
            // scan on the common path where the negative section is used directly ('section != 0').
            if (number.IsNegative && (section == 0) && (number.Scale != 0) && !HasNegativeSection(format))
            {
                vlb.Append(info.NegativeSignTChar<TChar>());
            }

            bool decimalWritten = false;

            // Slicing to DigitsCount lets the JIT prove digits[i] is in-bounds whenever i < digits.Length.
            // Math.Min proves the Slice length is within the buffer so the JIT can eliminate the cold throw.
            // digits itself is never mutated — curIndex tracks our position so digits.Length remains
            // loop-invariant across the outer format scan, letting the JIT hoist it once.
            ReadOnlySpan<byte> digits = number.Digits;
            digits = digits.Slice(0, Math.Min(number.DigitsCount, digits.Length));
            int curIndex = 0;

            while (src < format.Length && (ch = format[src++]) != 0 && ch != ';')
            {
                if (adjust > 0)
                {
                    switch (ch)
                    {
                        case '#':
                        case '0':
                        case '.':
                            // Emit real digits for the first min(adjust, digits.Length) positions,
                            // then '0' padding for any remaining. The adjust loop always fires before
                            // any main-switch digit consumption (curIndex == 0 at entry), so
                            // Math.Min(adjust, digits.Length) is the tight bound, and iterating the
                            // slice itself lets the JIT eliminate the per-element bounds checks.
                            ReadOnlySpan<byte> adjustDigits = digits.Slice(0, Math.Min(adjust, digits.Length));
                            for (int i = 0; i < adjustDigits.Length; i++)
                            {
                                // digPos will be one greater than thousandsSepPos[thousandsSepCtr] since we are at
                                // the character after which the groupSeparator needs to be appended.
                                vlb.Append(TChar.CastFrom((char)adjustDigits[i]));
                                if (thousandSeps && digPos > 1 && thousandsSepCtr >= 0)
                                {
                                    if (digPos == thousandsSepPos[thousandsSepCtr] + 1)
                                    {
                                        vlb.Append(info.NumberGroupSeparatorTChar<TChar>());
                                        thousandsSepCtr--;
                                    }
                                }
                                digPos--;
                                adjust--;
                            }
                            curIndex = adjustDigits.Length;
                            while (adjust > 0)
                            {
                                vlb.Append(TChar.CastFrom('0'));
                                if (thousandSeps && digPos > 1 && thousandsSepCtr >= 0)
                                {
                                    if (digPos == thousandsSepPos[thousandsSepCtr] + 1)
                                    {
                                        vlb.Append(info.NumberGroupSeparatorTChar<TChar>());
                                        thousandsSepCtr--;
                                    }
                                }
                                digPos--;
                                adjust--;
                            }
                            break;
                    }
                }

                switch (ch)
                {
                    case '#':
                    case '0':
                        {
                            if (adjust < 0)
                            {
                                adjust++;
                                ch = digPos <= firstDigit ? '0' : '\0';
                            }
                            else if (curIndex < digits.Length)
                            {
                                ch = (char)digits[curIndex++];
                            }
                            else
                            {
                                ch = digPos > lastDigit ? '0' : '\0';
                            }

                            if (ch != 0)
                            {
                                vlb.Append(TChar.CastFrom(ch));
                                if (thousandSeps && digPos > 1 && thousandsSepCtr >= 0)
                                {
                                    if (digPos == thousandsSepPos[thousandsSepCtr] + 1)
                                    {
                                        vlb.Append(info.NumberGroupSeparatorTChar<TChar>());
                                        thousandsSepCtr--;
                                    }
                                }
                            }

                            digPos--;
                            break;
                        }

                    case '.':
                        {
                            if (digPos != 0 || decimalWritten)
                            {
                                // For compatibility, don't echo repeated decimals
                                break;
                            }

                            // If the format has trailing zeros or the format has a decimal and digits remain
                            if (lastDigit < 0 || (decimalPos < digitCount && curIndex < digits.Length))
                            {
                                vlb.Append(info.NumberDecimalSeparatorTChar<TChar>());
                                decimalWritten = true;
                            }
                            break;
                        }

                    case '\x2030':
                        vlb.Append(info.PerMilleSymbolTChar<TChar>());
                        break;

                    case '%':
                        vlb.Append(info.PercentSymbolTChar<TChar>());
                        break;

                    case ',':
                        break;

                    case '\'':
                    case '"':
                        while (src < format.Length)
                        {
                            char quoted = format[src];
                            if (quoted == 0 || quoted == ch)
                            {
                                break;
                            }
                            src++;
                            AppendUnknownChar(ref vlb, quoted);
                        }

                        if (src < format.Length && format[src] != 0)
                        {
                            src++;
                        }
                        break;

                    case '\\':
                        if (src < format.Length && format[src] != 0)
                        {
                            AppendUnknownChar(ref vlb, format[src++]);
                        }
                        break;

                    case 'E':
                    case 'e':
                        {
                            bool positiveSign = false;
                            int i = 0;
                            if (scientific)
                            {
                                char exponentChar = src < format.Length ? format[src] : '\0';
                                char exponentNext = src + 1 < format.Length ? format[src + 1] : '\0';

                                if (exponentChar == '0')
                                {
                                    // Handles E0, which should format the same as E-0
                                    i++;
                                }
                                else if (exponentChar is '+' or '-' && exponentNext == '0')
                                {
                                    // Handles E+0 and E-0; only E+0 emits a sign for positive exponents
                                    positiveSign = exponentChar == '+';
                                }
                                else
                                {
                                    vlb.Append(TChar.CastFrom(ch));
                                    break;
                                }

                                while (++src < format.Length && format[src] == '0')
                                {
                                    i++;
                                }

                                if (i > 10)
                                {
                                    i = 10;
                                }

                                int exp = number.Digits[0] == 0 ? 0 : number.Scale - decimalPos;
                                FormatExponent(ref vlb, info, exp, ch, i, positiveSign);
                                scientific = false;
                            }
                            else
                            {
                                vlb.Append(TChar.CastFrom(ch));
                                if (src < format.Length)
                                {
                                    if (format[src] is '+' or '-')
                                    {
                                        AppendUnknownChar(ref vlb, format[src++]);
                                    }

                                    while (src < format.Length && format[src] == '0')
                                    {
                                        AppendUnknownChar(ref vlb, format[src++]);
                                    }
                                }
                            }
                            break;
                        }

                    default:
                        AppendUnknownChar(ref vlb, ch);
                        break;
                }
            }

            if (number.IsNegative && (section == 0) && (number.Scale == 0) && (vlb.Length > 0) && !HasNegativeSection(format))
            {
                vlb.Insert(0, info.NegativeSignTChar<TChar>());
            }
        }

        private static void FormatCurrency<TChar>(ref ValueListBuilder<TChar> vlb, ref NumberBuffer number, int nMaxDigits, NumberFormatInfo info) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(sizeof(TChar) is sizeof(char) or sizeof(byte));

            ReadOnlySpan<byte> fmt = GetCurrencyFormat(
                number.IsNegative,
                number.IsNegative ? info.CurrencyNegativePattern : info.CurrencyPositivePattern);

            foreach (byte ch in fmt)
            {
                switch (ch)
                {
                    case (byte)'#':
                        FormatFixed(ref vlb, ref number, nMaxDigits, info.CurrencyGroupSizes(), info.CurrencyDecimalSeparatorTChar<TChar>(), info.CurrencyGroupSeparatorTChar<TChar>());
                        break;

                    case (byte)'-':
                        vlb.Append(info.NegativeSignTChar<TChar>());
                        break;

                    case (byte)'$':
                        vlb.Append(info.CurrencySymbolTChar<TChar>());
                        break;

                    default:
                        vlb.Append(TChar.CastFrom(ch));
                        break;
                }
            }
        }

        private static void FormatFixed<TChar>(
            ref ValueListBuilder<TChar> vlb, ref NumberBuffer number,
            int nMaxDigits, int[]? groupDigits,
            ReadOnlySpan<TChar> sDecimal, ReadOnlySpan<TChar> sGroup) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(sizeof(TChar) is sizeof(char) or sizeof(byte));

            int digPos = number.Scale;
            ReadOnlySpan<byte> dig = number.Digits;
            dig = dig.Slice(0, Math.Min(number.DigitsCount, dig.Length));
            int digIndex = 0;

            if (digPos > 0)
            {
                if (groupDigits != null)
                {
                    int groupSizeIndex = 0;                             // Index into the groupDigits array.
                    int bufferSize = digPos;                            // The length of the result buffer string.
                    int groupSize = 0;                                  // The current group size.

                    // Find out the size of the string buffer for the result.
                    if (groupDigits.Length != 0) // You can pass in 0 length arrays
                    {
                        int groupSizeCount = groupDigits[groupSizeIndex];   // The current total of group size.

                        while (digPos > groupSizeCount)
                        {
                            groupSize = groupDigits[groupSizeIndex];
                            if (groupSize == 0)
                            {
                                break;
                            }

                            bufferSize += sGroup.Length;
                            if (groupSizeIndex < groupDigits.Length - 1)
                            {
                                groupSizeIndex++;
                            }

                            groupSizeCount += groupDigits[groupSizeIndex];
                            ArgumentOutOfRangeException.ThrowIfNegative(groupSizeCount | bufferSize, string.Empty); // If we overflow
                        }

                        groupSize = groupSizeCount == 0 ? 0 : groupDigits[0]; // If you passed in an array with one entry as 0, groupSizeCount == 0
                    }

                    groupSizeIndex = 0;
                    ReadOnlySpan<byte> intDigits = dig.Slice(0, Math.Min(digPos, dig.Length));
                    Span<TChar> buffer = vlb.AppendSpan(bufferSize);
                    int writePos = bufferSize;
                    int remainingDigits = digPos;

                    while (remainingDigits > 0)
                    {
                        int digitsInGroup = (groupSize > 0) ? Math.Min(groupSize, remainingDigits) : remainingDigits;
                        int groupStartDigit = remainingDigits - digitsInGroup;
                        int groupStartWrite = writePos - digitsInGroup;

                        Span<TChar> groupBuffer = buffer.Slice(groupStartWrite, digitsInGroup);
                        for (int j = 0; j < groupBuffer.Length; j++)
                        {
                            int digitIndex = groupStartDigit + j;
                            groupBuffer[j] = TChar.CastFrom((uint)digitIndex < (uint)intDigits.Length ? (char)intDigits[digitIndex] : '0');
                        }

                        writePos = groupStartWrite;
                        remainingDigits -= digitsInGroup;

                        if ((remainingDigits > 0) && (groupSize > 0))
                        {
                            if (sGroup.Length == 1)
                            {
                                writePos--;
                                buffer[writePos] = sGroup[0];
                            }
                            else
                            {
                                writePos -= sGroup.Length;
                                sGroup.CopyTo(buffer.Slice(writePos, sGroup.Length));
                            }

                            if (groupSizeIndex < groupDigits.Length - 1)
                            {
                                groupSizeIndex++;
                                groupSize = groupDigits[groupSizeIndex];
                            }
                        }
                    }

                    Debug.Assert(writePos == 0, "Underflow");
                    digIndex = intDigits.Length;
                }
                else
                {
                    // Emit actual digits first, then trailing zeros.
                    // Split into two unconditional loops so the JIT can prove bounds safety
                    // for the digit loop (span iteration) and fully optimize the zero loop.
                    int actualDigits = Math.Min(digPos, dig.Length);
                    foreach (byte d in dig.Slice(0, actualDigits))
                    {
                        vlb.Append(TChar.CastFrom((char)d));
                    }
                    digIndex = actualDigits;
                    digPos -= actualDigits;
                    while (digPos > 0)
                    {
                        vlb.Append(TChar.CastFrom('0'));
                        digPos--;
                    }
                }
            }
            else
            {
                vlb.Append(TChar.CastFrom('0'));
            }

            if (nMaxDigits > 0)
            {
                vlb.Append(sDecimal);
                if ((digPos < 0) && (nMaxDigits > 0))
                {
                    int zeroes = Math.Min(-digPos, nMaxDigits);
                    for (int i = 0; i < zeroes; i++)
                    {
                        vlb.Append(TChar.CastFrom('0'));
                    }
                    nMaxDigits -= zeroes;
                }

                int remainingDig = dig.Length - digIndex;
                int decActual = Math.Min(nMaxDigits, remainingDig);
                foreach (byte d in dig.Slice(digIndex, decActual))
                {
                    vlb.Append(TChar.CastFrom((char)d));
                }
                nMaxDigits -= decActual;
                while (nMaxDigits > 0)
                {
                    vlb.Append(TChar.CastFrom('0'));
                    nMaxDigits--;
                }
            }
        }

        /// <summary>Appends a char to the builder when the char is not known to be ASCII.</summary>
        /// <remarks>This requires a helper as if the character isn't ASCII, for UTF-8 encoding it will result in multiple bytes added.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AppendUnknownChar<TChar>(ref ValueListBuilder<TChar> vlb, char ch) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(sizeof(TChar) is sizeof(char) or sizeof(byte));

            if (sizeof(TChar) == sizeof(char) || char.IsAscii(ch))
            {
                vlb.Append(TChar.CastFrom(ch));
            }
            else
            {
                AppendNonAsciiBytes(ref vlb, ch);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void AppendNonAsciiBytes(ref ValueListBuilder<TChar> vlb, char ch)
            {
                var r = new Rune(ch);
                r.EncodeToUtf8(Unsafe.BitCast<Span<TChar>, Span<byte>>(vlb.AppendSpan(r.Utf8SequenceLength)));
            }
        }

        private static void FormatNumber<TChar>(ref ValueListBuilder<TChar> vlb, ref NumberBuffer number, int nMaxDigits, NumberFormatInfo info) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(sizeof(TChar) is sizeof(char) or sizeof(byte));

            ReadOnlySpan<byte> fmt = GetNumberFormat(number.IsNegative, info.NumberNegativePattern);

            foreach (byte ch in fmt)
            {
                switch (ch)
                {
                    case (byte)'#':
                        FormatFixed(ref vlb, ref number, nMaxDigits, info.NumberGroupSizes(), info.NumberDecimalSeparatorTChar<TChar>(), info.NumberGroupSeparatorTChar<TChar>());
                        break;

                    case (byte)'-':
                        vlb.Append(info.NegativeSignTChar<TChar>());
                        break;

                    default:
                        vlb.Append(TChar.CastFrom(ch));
                        break;
                }
            }
        }

        private static void FormatScientific<TChar>(ref ValueListBuilder<TChar> vlb, ref NumberBuffer number, int nMaxDigits, NumberFormatInfo info, char expChar) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(sizeof(TChar) is sizeof(char) or sizeof(byte));

            ReadOnlySpan<byte> dig = number.Digits;
            dig = dig.Slice(0, Math.Min(number.DigitsCount, dig.Length));

            // Emit the leading digit, or '0' when the value has no digits.
            vlb.Append(TChar.CastFrom(!dig.IsEmpty ? (char)dig[0] : '0'));

            if (nMaxDigits != 1) // For E0 we would like to suppress the decimal point
            {
                vlb.Append(info.NumberDecimalSeparatorTChar<TChar>());
            }

            // Emit the remaining nMaxDigits - 1 digits, padding with '0' once exhausted.
            int emitted = 1;
            if (dig.Length > 1)
            {
                foreach (byte b in dig.Slice(1, Math.Min(nMaxDigits - 1, dig.Length - 1)))
                {
                    vlb.Append(TChar.CastFrom((char)b));
                    emitted++;
                }
            }
            for (; emitted < nMaxDigits; emitted++)
            {
                vlb.Append(TChar.CastFrom('0'));
            }

            int e = number.Digits[0] == 0 ? 0 : number.Scale - 1;
            FormatExponent(ref vlb, info, e, expChar, 3, true);
        }

        private static void FormatExponent<TChar>(ref ValueListBuilder<TChar> vlb, NumberFormatInfo info, int value, char expChar, int minDigits, bool positiveSign) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(sizeof(TChar) is sizeof(char) or sizeof(byte));

            vlb.Append(TChar.CastFrom(expChar));

            if (value < 0)
            {
                vlb.Append(info.NegativeSignTChar<TChar>());
                value = -value;
            }
            else
            {
                if (positiveSign)
                {
                    vlb.Append(info.PositiveSignTChar<TChar>());
                }
            }

            int digitCount = Math.Max(minDigits, FormattingHelpers.CountDigits((uint)value));
            Span<TChar> digits = vlb.AppendSpan(digitCount);
            int pos = UInt32ToDecChars(digits, digitCount, (uint)value, minDigits);
            Debug.Assert(pos == 0);
        }

        private static void FormatGeneral<TChar>(ref ValueListBuilder<TChar> vlb, ref NumberBuffer number, int nMaxDigits, NumberFormatInfo info, char expChar, bool suppressScientific) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(sizeof(TChar) is sizeof(char) or sizeof(byte));

            int digPos = number.Scale;
            bool scientific = false;

            if (!suppressScientific)
            {
                // Don't switch to scientific notation
                if (digPos > nMaxDigits || digPos < -3)
                {
                    digPos = 1;
                    scientific = true;
                }
            }

            ReadOnlySpan<byte> dig = number.Digits;
            dig = dig.Slice(0, Math.Min(number.DigitsCount, dig.Length));

            if (digPos > 0)
            {
                // Emit the available integer digits, then pad with '0' up to digPos.
                int intCount = Math.Min(digPos, dig.Length);
                foreach (byte b in dig.Slice(0, intCount))
                {
                    vlb.Append(TChar.CastFrom((char)b));
                }
                for (int i = intCount; i < digPos; i++)
                {
                    vlb.Append(TChar.CastFrom('0'));
                }
                dig = dig.Slice(intCount);
            }
            else
            {
                vlb.Append(TChar.CastFrom('0'));
            }

            if (!dig.IsEmpty || digPos < 0)
            {
                vlb.Append(info.NumberDecimalSeparatorTChar<TChar>());

                while (digPos < 0)
                {
                    vlb.Append(TChar.CastFrom('0'));
                    digPos++;
                }

                foreach (byte b in dig)
                {
                    vlb.Append(TChar.CastFrom((char)b));
                }
            }

            if (scientific)
            {
                FormatExponent(ref vlb, info, number.Scale - 1, expChar, 2, true);
            }
        }

        private static void FormatPercent<TChar>(ref ValueListBuilder<TChar> vlb, ref NumberBuffer number, int nMaxDigits, NumberFormatInfo info) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(sizeof(TChar) is sizeof(char) or sizeof(byte));

            ReadOnlySpan<byte> fmt = GetPercentFormat(
                number.IsNegative,
                number.IsNegative ? info.PercentNegativePattern : info.PercentPositivePattern);

            foreach (byte ch in fmt)
            {
                switch (ch)
                {
                    case (byte)'#':
                        FormatFixed(ref vlb, ref number, nMaxDigits, info.PercentGroupSizes(), info.PercentDecimalSeparatorTChar<TChar>(), info.PercentGroupSeparatorTChar<TChar>());
                        break;

                    case (byte)'-':
                        vlb.Append(info.NegativeSignTChar<TChar>());
                        break;

                    case (byte)'%':
                        vlb.Append(info.PercentSymbolTChar<TChar>());
                        break;

                    default:
                        vlb.Append(TChar.CastFrom(ch));
                        break;
                }
            }
        }

        internal static void RoundNumber(ref NumberBuffer number, int pos, bool isCorrectlyRounded)
        {
            Span<byte> dig = number.Digits;

            int i = 0;
            while (i < pos && dig[i] != '\0')
            {
                i++;
            }

            if ((i == pos) && ShouldRoundUp(dig, i, number.Kind, isCorrectlyRounded))
            {
                while (i > 0 && dig[i - 1] == '9')
                {
                    i--;
                }

                if (i > 0)
                {
                    dig[i - 1]++;
                }
                else
                {
                    number.Scale++;
                    dig[0] = (byte)('1');
                    i = 1;
                }
            }
            else
            {
                while (i > 0 && dig[i - 1] == '0')
                {
                    i--;
                }
            }

            if (i == 0)
            {
                if (number.Kind is not (NumberBufferKind.FloatingPoint or NumberBufferKind.DecimalIeee754))
                {
                    // The integer types don't have a concept of -0 and decimal always format -0 as 0
                    number.IsNegative = false;
                }
                number.Scale = 0;      // Decimals with scale ('0.00') should be rounded.
            }

            dig[i] = (byte)('\0');
            number.DigitsCount = i;
            number.CheckConsistency();

            static bool ShouldRoundUp(ReadOnlySpan<byte> dig, int i, NumberBufferKind numberKind, bool isCorrectlyRounded)
            {
                // We only want to round up if the digit is greater than or equal to 5 and we are
                // not rounding a floating-point number. If we are rounding a floating-point number
                // we have one of two cases.
                //
                // In the case of a standard numeric-format specifier, the exact and correctly rounded
                // string will have been produced. In this scenario, pos will have pointed to the
                // terminating null for the buffer and so this will return false.
                //
                // However, in the case of a custom numeric-format specifier, we currently fall back
                // to generating Single/DoublePrecisionCustomFormat digits and then rely on this
                // function to round correctly instead. This can unfortunately lead to double-rounding
                // bugs but is the best we have right now due to back-compat concerns.

                byte digit = dig[i];

                if ((digit == '\0') || isCorrectlyRounded)
                {
                    // Fast path for the common case with no rounding
                    return false;
                }

                if (numberKind == NumberBufferKind.DecimalIeee754)
                {
                    // The buffer holds the exact coefficient, so a '5' followed by nothing but zeros is a
                    // true tie rather than an artifact of a truncated expansion. IEEE 754 §5.12.1 requires
                    // the conversion to be correctly rounded under the applicable rounding-direction
                    // attribute, which is roundTiesToEven.

                    if (digit != '5')
                    {
                        return digit > '5';
                    }

                    for (int j = i + 1; dig[j] != '\0'; j++)
                    {
                        if (dig[j] != '0')
                        {
                            return true;
                        }
                    }

                    // A tie with no preceding digit rounds toward the implicit leading zero, which is even.
                    return (i > 0) && (((dig[i - 1] - '0') & 1) != 0);
                }

                // Values greater than or equal to 5 should round up, otherwise we round down. The IEEE
                // 754 spec actually dictates that ties (exactly 5) should round to the nearest even number
                // but that can have undesired behavior for custom numeric format strings. This probably
                // needs further thought for .NET 5 so that we can be spec compliant and so that users
                // can get the desired rounding behavior for their needs.

                return digit >= '5';
            }
        }

        // A distinct negative section always begins after the first ';', so its offset is > 0.
        // FindSection returns 0 both for the first section and when no such section exists, so a
        // non-zero result reliably indicates the format defines a dedicated negative section.
        private static bool HasNegativeSection(ReadOnlySpan<char> format) => FindSection(format, 1) != 0;

        private static int FindSection(ReadOnlySpan<char> format, int section)
        {
            int src;
            char ch;

            if (section == 0)
            {
                return 0;
            }

            src = 0;
            while (true)
            {
                if (src >= format.Length)
                {
                    return 0;
                }

                switch (ch = format[src++])
                {
                    case '\'':
                    case '"':
                        while (src < format.Length && format[src] != 0 && format[src++] != ch) ;
                        break;

                    case '\\':
                        if (src < format.Length && format[src] != 0)
                        {
                            src++;
                        }
                        break;

                    case ';':
                        if (--section != 0)
                        {
                            break;
                        }

                        if (src < format.Length && format[src] is not ('\0' or ';'))
                        {
                            return src;
                        }
                        goto case '\0';

                    case '\0':
                        return 0;
                }
            }
        }

#if SYSTEM_PRIVATE_CORELIB
        private static int[] NumberGroupSizes(this NumberFormatInfo info) => info._numberGroupSizes;

        private static int[] CurrencyGroupSizes(this NumberFormatInfo info) => info._currencyGroupSizes;

        private static int[] PercentGroupSizes(this NumberFormatInfo info) => info._percentGroupSizes;
#else

        private static int[] NumberGroupSizes(this NumberFormatInfo info) => info.NumberGroupSizes;

        private static int[] CurrencyGroupSizes(this NumberFormatInfo info) => info.CurrencyGroupSizes;

        private static int[] PercentGroupSizes(this NumberFormatInfo info) => info.PercentGroupSizes;
#endif
    }
}
