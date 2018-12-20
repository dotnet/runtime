// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        private static bool TryParseNumber(ReadOnlySpan<byte> source, ref Number.NumberBuffer number, out int bytesConsumed, ParseNumberOptions options, out bool textUsedExponentNotation)
        {
            Debug.Assert(number.DigitsCount == 0);
            Debug.Assert(number.Scale == 0);
            Debug.Assert(number.IsNegative == false);
            Debug.Assert(number.HasNonZeroTail == false);

            number.CheckConsistency();
            textUsedExponentNotation = false;

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

            int startIndexDigitsBeforeDecimal = srcIndex;
            int digitCount = 0;
            int maxDigitCount = digits.Length - 1;

            // Throw away any leading zeroes
            while (srcIndex != source.Length)
            {
                c = source[srcIndex];
                if (c != '0')
                    break;
                srcIndex++;
            }

            if (srcIndex == source.Length)
            {
                bytesConsumed = srcIndex;
                number.CheckConsistency();
                return true;
            }

            int startIndexNonLeadingDigitsBeforeDecimal = srcIndex;

            int hasNonZeroTail = 0;
            while (srcIndex != source.Length)
            {
                c = source[srcIndex];
                int value = (byte)(c - (byte)('0'));

                if (value > 9)
                {
                    break;
                }

                srcIndex++;
                digitCount++;

                if (digitCount >= maxDigitCount)
                {
                    // For decimal and binary floating-point numbers, we only
                    // need to store digits up to maxDigCount. However, we still
                    // need to keep track of whether any additional digits past
                    // maxDigCount were non-zero, as that can impact rounding
                    // for an input that falls evenly between two representable
                    // results.

                    hasNonZeroTail |= value;
                }
            }
            number.HasNonZeroTail = (hasNonZeroTail != 0);

            int numDigitsBeforeDecimal = srcIndex - startIndexDigitsBeforeDecimal;
            int numNonLeadingDigitsBeforeDecimal = srcIndex - startIndexNonLeadingDigitsBeforeDecimal;

            Debug.Assert(dstIndex == 0);
            int numNonLeadingDigitsBeforeDecimalToCopy = Math.Min(numNonLeadingDigitsBeforeDecimal, maxDigitCount);
            source.Slice(startIndexNonLeadingDigitsBeforeDecimal, numNonLeadingDigitsBeforeDecimalToCopy).CopyTo(digits);
            dstIndex = numNonLeadingDigitsBeforeDecimalToCopy;
            number.Scale = numNonLeadingDigitsBeforeDecimal;

            if (srcIndex == source.Length)
            {
                digits[dstIndex] = 0;
                number.DigitsCount = dstIndex;
                bytesConsumed = srcIndex;
                number.CheckConsistency();
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

                while (srcIndex != source.Length)
                {
                    c = source[srcIndex];
                    int value = (byte)(c - (byte)('0'));

                    if (value > 9)
                    {
                        break;
                    }

                    srcIndex++;
                    digitCount++;

                    if (digitCount >= maxDigitCount)
                    {
                        // For decimal and binary floating-point numbers, we only
                        // need to store digits up to maxDigCount. However, we still
                        // need to keep track of whether any additional digits past
                        // maxDigCount were non-zero, as that can impact rounding
                        // for an input that falls evenly between two representable
                        // results.

                        hasNonZeroTail |= value;
                    }
                }
                number.HasNonZeroTail = (hasNonZeroTail != 0);

                numDigitsAfterDecimal = srcIndex - startIndexDigitsAfterDecimal;

                int startIndexOfDigitsAfterDecimalToCopy = startIndexDigitsAfterDecimal;
                if (dstIndex == 0)
                {
                    // Not copied any digits to the Number struct yet. This means we must continue discarding leading zeroes even though they appeared after the decimal point.
                    while (startIndexOfDigitsAfterDecimalToCopy < srcIndex && source[startIndexOfDigitsAfterDecimalToCopy] == '0')
                    {
                        number.Scale--;
                        startIndexOfDigitsAfterDecimalToCopy++;
                    }
                }

                int numDigitsAfterDecimalToCopy = Math.Min(srcIndex - startIndexOfDigitsAfterDecimalToCopy, maxDigitCount - dstIndex);
                source.Slice(startIndexOfDigitsAfterDecimalToCopy, numDigitsAfterDecimalToCopy).CopyTo(digits.Slice(dstIndex));
                dstIndex += numDigitsAfterDecimalToCopy;
                // We "should" really NUL terminate, but there are multiple places we'd have to do this and it is a precondition that the caller pass in a fully zero=initialized Number.

                if (srcIndex == source.Length)
                {
                    if (numDigitsBeforeDecimal == 0 && numDigitsAfterDecimal == 0)
                    {
                        // For compatibility. You can say "5." and ".5" but you can't say "."
                        bytesConsumed = 0;
                        return false;
                    }

                    digits[dstIndex] = 0;
                    number.DigitsCount = dstIndex;
                    bytesConsumed = srcIndex;
                    number.CheckConsistency();
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
                digits[dstIndex] = 0;
                number.DigitsCount = dstIndex;
                bytesConsumed = srcIndex;
                number.CheckConsistency();
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

                while (srcIndex != source.Length)
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

            digits[dstIndex] = 0;
            number.DigitsCount = dstIndex;
            bytesConsumed = srcIndex;
            number.CheckConsistency();
            return true;
        }
    }
}
