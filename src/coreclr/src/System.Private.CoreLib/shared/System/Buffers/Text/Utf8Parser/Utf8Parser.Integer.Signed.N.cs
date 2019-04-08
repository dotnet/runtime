// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Buffers.Text
{
    public static partial class Utf8Parser
    {
        private static bool TryParseSByteN(ReadOnlySpan<byte> source, out sbyte value, out int bytesConsumed)
        {
            if (source.Length < 1)
                goto FalseExit;

            int sign = 1;
            int index = 0;
            int c = source[index];
            if (c == '-')
            {
                sign = -1;
                index++;
                if ((uint)index >= (uint)source.Length)
                    goto FalseExit;
                c = source[index];
            }
            else if (c == '+')
            {
                index++;
                if ((uint)index >= (uint)source.Length)
                    goto FalseExit;
                c = source[index];
            }

            int answer;

            // Handle the first digit (or period) as a special case. This ensures some compatible edge-case behavior with the classic parse routines
            // (at least one digit must precede any commas, and a string without any digits prior to the decimal point must have at least 
            // one digit after the decimal point.)
            if (c == Utf8Constants.Period)
                goto FractionalPartWithoutLeadingDigits;
            if (!ParserHelpers.IsDigit(c))
                goto FalseExit;
            answer = c - '0';

            for (; ; )
            {
                index++;
                if ((uint)index >= (uint)source.Length)
                    goto Done;

                c = source[index];
                if (c == Utf8Constants.Comma)
                    continue;

                if (c == Utf8Constants.Period)
                    goto FractionalDigits;

                if (!ParserHelpers.IsDigit(c))
                    goto Done;

                answer = answer * 10 + c - '0';

                // if sign < 0, (-1 * sign + 1) / 2 = 1
                // else, (-1 * sign + 1) / 2 = 0
                if (answer > sbyte.MaxValue + (-1 * sign + 1) / 2)
                    goto FalseExit; // Overflow
            }

        FractionalPartWithoutLeadingDigits: // If we got here, we found a decimal point before we found any digits. This is legal as long as there's at least one zero after the decimal point.
            answer = 0;
            index++;
            if ((uint)index >= (uint)source.Length)
                goto FalseExit;
            if (source[index] != '0')
                goto FalseExit;

        FractionalDigits: // "N" format allows a fractional portion despite being an integer format but only if the post-fraction digits are all 0.
            do
            {
                index++;
                if ((uint)index >= (uint)source.Length)
                    goto Done;
                c = source[index];
            }
            while (c == '0');

            if (ParserHelpers.IsDigit(c))
                goto FalseExit; // The fractional portion contained a non-zero digit. Treat this as an error, not an early termination.
            goto Done;

        FalseExit:
            bytesConsumed = default;
            value = default;
            return false;

        Done:
            bytesConsumed = index;
            value = (sbyte)(answer * sign);
            return true;
        }

        private static bool TryParseInt16N(ReadOnlySpan<byte> source, out short value, out int bytesConsumed)
        {
            if (source.Length < 1)
                goto FalseExit;

            int sign = 1;
            int index = 0;
            int c = source[index];
            if (c == '-')
            {
                sign = -1;
                index++;
                if ((uint)index >= (uint)source.Length)
                    goto FalseExit;
                c = source[index];
            }
            else if (c == '+')
            {
                index++;
                if ((uint)index >= (uint)source.Length)
                    goto FalseExit;
                c = source[index];
            }

            int answer;

            // Handle the first digit (or period) as a special case. This ensures some compatible edge-case behavior with the classic parse routines
            // (at least one digit must precede any commas, and a string without any digits prior to the decimal point must have at least 
            // one digit after the decimal point.)
            if (c == Utf8Constants.Period)
                goto FractionalPartWithoutLeadingDigits;
            if (!ParserHelpers.IsDigit(c))
                goto FalseExit;
            answer = c - '0';

            for (; ; )
            {
                index++;
                if ((uint)index >= (uint)source.Length)
                    goto Done;

                c = source[index];
                if (c == Utf8Constants.Comma)
                    continue;

                if (c == Utf8Constants.Period)
                    goto FractionalDigits;

                if (!ParserHelpers.IsDigit(c))
                    goto Done;

                answer = answer * 10 + c - '0';

                // if sign < 0, (-1 * sign + 1) / 2 = 1
                // else, (-1 * sign + 1) / 2 = 0
                if (answer > short.MaxValue + (-1 * sign + 1) / 2)
                    goto FalseExit; // Overflow
            }

        FractionalPartWithoutLeadingDigits: // If we got here, we found a decimal point before we found any digits. This is legal as long as there's at least one zero after the decimal point.
            answer = 0;
            index++;
            if ((uint)index >= (uint)source.Length)
                goto FalseExit;
            if (source[index] != '0')
                goto FalseExit;

        FractionalDigits: // "N" format allows a fractional portion despite being an integer format but only if the post-fraction digits are all 0.
            do
            {
                index++;
                if ((uint)index >= (uint)source.Length)
                    goto Done;
                c = source[index];
            }
            while (c == '0');

            if (ParserHelpers.IsDigit(c))
                goto FalseExit; // The fractional portion contained a non-zero digit. Treat this as an error, not an early termination.
            goto Done;

        FalseExit:
            bytesConsumed = default;
            value = default;
            return false;

        Done:
            bytesConsumed = index;
            value = (short)(answer * sign);
            return true;
        }

        private static bool TryParseInt32N(ReadOnlySpan<byte> source, out int value, out int bytesConsumed)
        {
            if (source.Length < 1)
                goto FalseExit;

            int sign = 1;
            int index = 0;
            int c = source[index];
            if (c == '-')
            {
                sign = -1;
                index++;
                if ((uint)index >= (uint)source.Length)
                    goto FalseExit;
                c = source[index];
            }
            else if (c == '+')
            {
                index++;
                if ((uint)index >= (uint)source.Length)
                    goto FalseExit;
                c = source[index];
            }

            int answer;

            // Handle the first digit (or period) as a special case. This ensures some compatible edge-case behavior with the classic parse routines
            // (at least one digit must precede any commas, and a string without any digits prior to the decimal point must have at least 
            // one digit after the decimal point.)
            if (c == Utf8Constants.Period)
                goto FractionalPartWithoutLeadingDigits;
            if (!ParserHelpers.IsDigit(c))
                goto FalseExit;
            answer = c - '0';

            for (; ; )
            {
                index++;
                if ((uint)index >= (uint)source.Length)
                    goto Done;

                c = source[index];
                if (c == Utf8Constants.Comma)
                    continue;

                if (c == Utf8Constants.Period)
                    goto FractionalDigits;

                if (!ParserHelpers.IsDigit(c))
                    goto Done;

                if (((uint)answer) > int.MaxValue / 10)
                    goto FalseExit;

                answer = answer * 10 + c - '0';

                // if sign < 0, (-1 * sign + 1) / 2 = 1
                // else, (-1 * sign + 1) / 2 = 0
                if ((uint)answer > (uint)int.MaxValue + (-1 * sign + 1) / 2)
                    goto FalseExit; // Overflow
            }

        FractionalPartWithoutLeadingDigits: // If we got here, we found a decimal point before we found any digits. This is legal as long as there's at least one zero after the decimal point.
            answer = 0;
            index++;
            if ((uint)index >= (uint)source.Length)
                goto FalseExit;
            if (source[index] != '0')
                goto FalseExit;

        FractionalDigits: // "N" format allows a fractional portion despite being an integer format but only if the post-fraction digits are all 0.
            do
            {
                index++;
                if ((uint)index >= (uint)source.Length)
                    goto Done;
                c = source[index];
            }
            while (c == '0');

            if (ParserHelpers.IsDigit(c))
                goto FalseExit; // The fractional portion contained a non-zero digit. Treat this as an error, not an early termination.
            goto Done;

        FalseExit:
            bytesConsumed = default;
            value = default;
            return false;

        Done:
            bytesConsumed = index;
            value = answer * sign;
            return true;
        }

        private static bool TryParseInt64N(ReadOnlySpan<byte> source, out long value, out int bytesConsumed)
        {
            if (source.Length < 1)
                goto FalseExit;

            int sign = 1;
            int index = 0;
            int c = source[index];
            if (c == '-')
            {
                sign = -1;
                index++;
                if ((uint)index >= (uint)source.Length)
                    goto FalseExit;
                c = source[index];
            }
            else if (c == '+')
            {
                index++;
                if ((uint)index >= (uint)source.Length)
                    goto FalseExit;
                c = source[index];
            }

            long answer;

            // Handle the first digit (or period) as a special case. This ensures some compatible edge-case behavior with the classic parse routines
            // (at least one digit must precede any commas, and a string without any digits prior to the decimal point must have at least 
            // one digit after the decimal point.)
            if (c == Utf8Constants.Period)
                goto FractionalPartWithoutLeadingDigits;
            if (!ParserHelpers.IsDigit(c))
                goto FalseExit;
            answer = c - '0';

            for (; ; )
            {
                index++;
                if ((uint)index >= (uint)source.Length)
                    goto Done;

                c = source[index];
                if (c == Utf8Constants.Comma)
                    continue;

                if (c == Utf8Constants.Period)
                    goto FractionalDigits;

                if (!ParserHelpers.IsDigit(c))
                    goto Done;

                if (((ulong)answer) > long.MaxValue / 10)
                    goto FalseExit;

                answer = answer * 10 + c - '0';

                // if sign < 0, (-1 * sign + 1) / 2 = 1
                // else, (-1 * sign + 1) / 2 = 0
                if ((ulong)answer > (ulong)(long.MaxValue + (-1 * sign + 1) / 2))
                    goto FalseExit; // Overflow
            }

        FractionalPartWithoutLeadingDigits: // If we got here, we found a decimal point before we found any digits. This is legal as long as there's at least one zero after the decimal point.
            answer = 0;
            index++;
            if ((uint)index >= (uint)source.Length)
                goto FalseExit;
            if (source[index] != '0')
                goto FalseExit;

        FractionalDigits: // "N" format allows a fractional portion despite being an integer format but only if the post-fraction digits are all 0.
            do
            {
                index++;
                if ((uint)index >= (uint)source.Length)
                    goto Done;
                c = source[index];
            }
            while (c == '0');

            if (ParserHelpers.IsDigit(c))
                goto FalseExit; // The fractional portion contained a non-zero digit. Treat this as an error, not an early termination.
            goto Done;

        FalseExit:
            bytesConsumed = default;
            value = default;
            return false;

        Done:
            bytesConsumed = index;
            value = answer * sign;
            return true;
        }
    }
}
