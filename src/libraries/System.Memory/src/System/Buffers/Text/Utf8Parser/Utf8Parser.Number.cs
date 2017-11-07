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

        private static bool TryParseNumber(ReadOnlySpan<byte> text, ref NumberBuffer number, out int bytesConsumed, ParseNumberOptions options, out bool textUsedExponentNotation)
        {
            Debug.Assert(number.Digits[0] == 0 && number.Scale == 0 && !number.IsNegative, "Number not initialized to default(NumberBuffer)");

            textUsedExponentNotation = false;

            if (text.Length == 0)
            {
                bytesConsumed = 0;
                return false;
            }

            Span<byte> digits = number.Digits;

            int srcIndex = 0;
            int dstIndex = 0;

            // Consume the leading sign if any.
            byte c = text[srcIndex];
            switch (c)
            {
                case Utf8Constants.Minus:
                    number.IsNegative = true;
                    goto case Utf8Constants.Plus;

                case Utf8Constants.Plus:
                    srcIndex++;
                    if (srcIndex == text.Length)
                    {
                        bytesConsumed = 0;
                        return false;
                    }
                    c = text[srcIndex];
                    break;

                default:
                    break;
            }

            int startIndexDigitsBeforeDecimal = srcIndex;

            // Throw away any leading zeroes
            while (srcIndex != text.Length)
            {
                c = text[srcIndex];
                if (c != '0')
                    break;
                srcIndex++;
            }

            if (srcIndex == text.Length)
            {
                digits[0] = 0;
                number.Scale = 0;
                bytesConsumed = srcIndex;
                number.CheckConsistency();
                return true;
            }

            int startIndexNonLeadingDigitsBeforeDecimal = srcIndex;
            while (srcIndex != text.Length)
            {
                c = text[srcIndex];
                if ((c - 48u) > 9)
                    break;
                srcIndex++;
            }

            int numDigitsBeforeDecimal = srcIndex - startIndexDigitsBeforeDecimal;
            int numNonLeadingDigitsBeforeDecimal = srcIndex - startIndexNonLeadingDigitsBeforeDecimal;

            Debug.Assert(dstIndex == 0);
            int numNonLeadingDigitsBeforeDecimalToCopy = Math.Min(numNonLeadingDigitsBeforeDecimal, NumberBuffer.BufferSize - 1);
            text.Slice(startIndexNonLeadingDigitsBeforeDecimal, numNonLeadingDigitsBeforeDecimalToCopy).CopyTo(digits);
            dstIndex = numNonLeadingDigitsBeforeDecimalToCopy;
            number.Scale = numNonLeadingDigitsBeforeDecimal;

            if (srcIndex == text.Length)
            {
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
                while (srcIndex != text.Length)
                {
                    c = text[srcIndex];
                    if ((c - 48u) > 9)
                        break;
                    srcIndex++;
                }
                numDigitsAfterDecimal = srcIndex - startIndexDigitsAfterDecimal;

                int startIndexOfDigitsAfterDecimalToCopy = startIndexDigitsAfterDecimal;
                if (dstIndex == 0)
                {
                    // Not copied any digits to the Number struct yet. This means we must continue discarding leading zeroes even though they appeared after the decimal point.
                    while (startIndexOfDigitsAfterDecimalToCopy < srcIndex && text[startIndexOfDigitsAfterDecimalToCopy] == '0')
                    {
                        number.Scale--;
                        startIndexOfDigitsAfterDecimalToCopy++;
                    }
                }

                int numDigitsAfterDecimalToCopy = Math.Min(srcIndex - startIndexOfDigitsAfterDecimalToCopy, NumberBuffer.BufferSize - dstIndex - 1);
                text.Slice(startIndexOfDigitsAfterDecimalToCopy, numDigitsAfterDecimalToCopy).CopyTo(digits.Slice(dstIndex));
                dstIndex += numDigitsAfterDecimalToCopy;
                // We "should" really NUL terminate, but there are multiple places we'd have to do this and it is a precondition that the caller pass in a fully zero=initialized Number.

                if (srcIndex == text.Length)
                {
                    if (numDigitsBeforeDecimal == 0 && numDigitsAfterDecimal == 0)
                    {
                        // For compatibility. You can say "5." and ".5" but you can't say "."
                        bytesConsumed = 0;
                        return false;
                    }

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

            if (srcIndex == text.Length)
            {
                bytesConsumed = 0;
                return false;
            }

            bool exponentIsNegative = false;
            c = text[srcIndex];
            switch (c)
            {
                case Utf8Constants.Minus:
                    exponentIsNegative = true;
                    goto case Utf8Constants.Plus;

                case Utf8Constants.Plus:
                    srcIndex++;
                    if (srcIndex == text.Length)
                    {
                        bytesConsumed = 0;
                        return false;
                    }
                    c = text[srcIndex];
                    break;

                default:
                    break;
            }

            if (!Utf8Parser.TryParseUInt32D(text.Slice(srcIndex), out uint absoluteExponent, out int bytesConsumedByExponent))
            {
                bytesConsumed = 0;
                return false;
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
                    bytesConsumed = 0;
                    return false;
                }
                number.Scale += (int)absoluteExponent;
            }

            bytesConsumed = srcIndex;
            number.CheckConsistency();
            return true;
        }
    }
}
