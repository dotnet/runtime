// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Buffers.Text
{
    public static partial class Utf8Parser
    {
        [Flags]
        private enum ParseNumberOptions
        {
            AllowExponent = 0x00000001,
        }

        /// <remarks>Caller must null-terminate number.Digits and set number.HasNonZeroTail after a successful parse.</remarks>
        private static bool TryParseNumber(ReadOnlySpan<byte> source, ref Number.NumberBuffer number, out int bytesConsumed, ParseNumberOptions options, out bool textUsedExponentNotation, out int lastNonZeroDigitIndex)
        {
            Debug.Assert(number.DigitsCount == 0);
            Debug.Assert(number.Scale == 0);
            Debug.Assert(!number.IsNegative);
            Debug.Assert(!number.HasNonZeroTail);

            number.CheckConsistency();
            textUsedExponentNotation = false;
            lastNonZeroDigitIndex = -1;

            if (source.Length == 0)
            {
                bytesConsumed = 0;
                return false;
            }

            Span<byte> digits = number.Digits;

            int srcIndex = 0;
            int dstIndex = 0;

            // Consume the leading sign if any.
            byte c = source[srcIndex];
            switch (c)
            {
                case Utf8Constants.Minus:
                    number.IsNegative = true;
                    goto case Utf8Constants.Plus;

                case Utf8Constants.Plus:
                    srcIndex++;
                    if (srcIndex >= source.Length)
                    {
                        bytesConsumed = 0;
                        return false;
                    }
                    c = source[srcIndex];
                    break;

                default:
                    break;
            }

            int startIndexDigitsBeforeDecimal = srcIndex;
            int digitCount = 0;
            int maxDigitCount = digits.Length - 1;

            // Throw away any leading zeroes
            while (srcIndex < source.Length)
            {
                c = source[srcIndex];
                if (c != '0')
                    break;
                srcIndex++;
            }

            if (srcIndex == source.Length)
            {
                bytesConsumed = srcIndex;
                return true;
            }

            int startIndexNonLeadingDigitsBeforeDecimal = srcIndex;

            int nzIndex = -1;
            while (srcIndex < source.Length)
            {
                c = source[srcIndex];
                int value = (byte)(c - (byte)('0'));

                if (value > 9)
                {
                    break;
                }

                if (value != 0)
                {
                    nzIndex = digitCount;
                }

                srcIndex++;
                digitCount++;
            }
            int numDigitsBeforeDecimal = srcIndex - startIndexDigitsBeforeDecimal;
            int numNonLeadingDigitsBeforeDecimal = srcIndex - startIndexNonLeadingDigitsBeforeDecimal;
            lastNonZeroDigitIndex = nzIndex;

            Debug.Assert(dstIndex == 0);
            int numNonLeadingDigitsBeforeDecimalToCopy = Math.Min(numNonLeadingDigitsBeforeDecimal, maxDigitCount);
            source.Slice(startIndexNonLeadingDigitsBeforeDecimal, numNonLeadingDigitsBeforeDecimalToCopy).CopyTo(digits);
            dstIndex = numNonLeadingDigitsBeforeDecimalToCopy;
            number.Scale = numNonLeadingDigitsBeforeDecimal;

            if (srcIndex == source.Length)
            {
                number.DigitsCount = dstIndex;
                bytesConsumed = srcIndex;
                return true;
            }

            int numDigitsAfterDecimal = 0;
            if (c == Utf8Constants.Period)
            {
                //
                // Parse the digits after the decimal point.
                //

                srcIndex++;
                int startIndexDigitsAfterDecimal = srcIndex;

                if (dstIndex == 0)
                {
                    // Not copied any digits to the Number struct yet. This means we must continue discarding leading zeroes even though they appeared after the decimal point.
                    while (srcIndex < source.Length && source[srcIndex] == '0')
                    {
                        number.Scale--;
                        srcIndex++;
                    }
                }

                int startIndexOfDigitsAfterDecimalToCopy = srcIndex;

                while (srcIndex < source.Length)
                {
                    c = source[srcIndex];
                    int value = (byte)(c - (byte)('0'));

                    if (value > 9)
                    {
                        break;
                    }

                    if (value != 0)
                    {
                        nzIndex = digitCount;
                    }

                    srcIndex++;
                    digitCount++;
                }
                numDigitsAfterDecimal = srcIndex - startIndexDigitsAfterDecimal;
                lastNonZeroDigitIndex = nzIndex;

                int numDigitsAfterDecimalToCopy = Math.Min(srcIndex - startIndexOfDigitsAfterDecimalToCopy, maxDigitCount - dstIndex);
                source.Slice(startIndexOfDigitsAfterDecimalToCopy, numDigitsAfterDecimalToCopy).CopyTo(digits.Slice(dstIndex));
                dstIndex += numDigitsAfterDecimalToCopy;

                if (srcIndex == source.Length)
                {
                    if (numDigitsBeforeDecimal == 0 && numDigitsAfterDecimal == 0)
                    {
                        // For compatibility. You can say "5." and ".5" but you can't say "."
                        bytesConsumed = 0;
                        return false;
                    }

                    number.DigitsCount = dstIndex;
                    bytesConsumed = srcIndex;
                    return true;
                }
            }

            if (numDigitsBeforeDecimal == 0 && numDigitsAfterDecimal == 0)
            {
                bytesConsumed = 0;
                return false;
            }

            if ((c & ~0x20u) != 'E')
            {
                number.DigitsCount = dstIndex;
                bytesConsumed = srcIndex;
                return true;
            }

            //
            // Parse the exponent after the "E"
            //
            textUsedExponentNotation = true;
            srcIndex++;

            if ((options & ParseNumberOptions.AllowExponent) == 0)
            {
                bytesConsumed = 0;
                return false;
            }

            if (srcIndex == source.Length)
            {
                bytesConsumed = 0;
                return false;
            }

            bool exponentIsNegative = false;
            c = source[srcIndex];
            switch (c)
            {
                case Utf8Constants.Minus:
                    exponentIsNegative = true;
                    goto case Utf8Constants.Plus;

                case Utf8Constants.Plus:
                    srcIndex++;
                    if (srcIndex == source.Length)
                    {
                        bytesConsumed = 0;
                        return false;
                    }
                    c = source[srcIndex];
                    break;

                default:
                    break;
            }

            // If the next character isn't a digit, an exponent wasn't specified
            if ((byte)(c - (byte)('0')) > 9)
            {
                bytesConsumed = 0;
                return false;
            }

            if (!TryParseUInt32D(source.Slice(srcIndex), out uint absoluteExponent, out int bytesConsumedByExponent))
            {
                // Since we found at least one digit, we know that any failure to parse means we had an
                // exponent that was larger than uint.MaxValue, and we can just eat characters until the end
                absoluteExponent = uint.MaxValue;

                // This also means that we know there was at least 10 characters and we can "eat" those, and
                // continue eating digits from there
                srcIndex += 10;

                while (srcIndex < source.Length)
                {
                    c = source[srcIndex];
                    int value = (byte)(c - (byte)('0'));

                    if (value > 9)
                    {
                        break;
                    }

                    srcIndex++;
                }
            }

            srcIndex += bytesConsumedByExponent;

            if (exponentIsNegative)
            {
                if (number.Scale < int.MinValue + (long)absoluteExponent)
                {
                    // A scale underflow means all non-zero digits are all so far to the right of the decimal point, no
                    // number format we have will be able to see them. Just pin the scale at the absolute minimum
                    // and let the converter produce a 0 with the max precision available for that type.
                    number.Scale = int.MinValue;
                }
                else
                {
                    number.Scale -= (int)absoluteExponent;
                }
            }
            else
            {
                if (number.Scale > int.MaxValue - (long)absoluteExponent)
                {
                    // A scale overflow means all non-zero digits are all so far to the right of the decimal point, no
                    // number format we have will be able to see them. Just pin the scale at the absolute maximum
                    // and let the converter produce a 0 with the max precision available for that type.
                    number.Scale = int.MaxValue;
                }
                else
                {
                    number.Scale += (int)absoluteExponent;
                }
            }

            number.DigitsCount = dstIndex;
            bytesConsumed = srcIndex;
            return true;
        }
    }
}
