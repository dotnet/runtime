// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Buffers.Text
{
    //
    // Parsing unsigned integers for the 'N' format. Emulating int.TryParse(NumberStyles.AllowThousands | NumberStyles.Integer | NumberStyles.AllowDecimalPoint)
    //
    public static partial class Utf8Parser
    {
        private static bool TryParseByteN(ReadOnlySpan<byte> source, out byte value, out int bytesConsumed)
        {
            if (source.Length < 1)
                goto FalseExit;

            int index = 0;
            int c = source[index];
            if (c == '+')
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

                if (answer > byte.MaxValue)
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
            value = (byte)answer;
            return true;
        }

        private static bool TryParseUInt16N(ReadOnlySpan<byte> source, out ushort value, out int bytesConsumed)
        {
            if (source.Length < 1)
                goto FalseExit;

            int index = 0;
            int c = source[index];
            if (c == '+')
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

                if (answer > ushort.MaxValue)
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
            value = (ushort)answer;
            return true;
        }

        private static bool TryParseUInt32N(ReadOnlySpan<byte> source, out uint value, out int bytesConsumed)
        {
            if (source.Length < 1)
                goto FalseExit;

            int index = 0;
            int c = source[index];
            if (c == '+')
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

                if (((uint)answer) > uint.MaxValue / 10 || (((uint)answer) == uint.MaxValue / 10 && c > '5'))
                    goto FalseExit; // Overflow

                answer = answer * 10 + c - '0';
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
            value = (uint)answer;
            return true;
        }

        private static bool TryParseUInt64N(ReadOnlySpan<byte> source, out ulong value, out int bytesConsumed)
        {
            if (source.Length < 1)
                goto FalseExit;

            int index = 0;
            int c = source[index];
            if (c == '+')
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

                if (((ulong)answer) > ulong.MaxValue / 10 || (((ulong)answer) == ulong.MaxValue / 10 && c > '5'))
                    goto FalseExit; // Overflow

                answer = answer * 10 + c - '0';
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
            value = (ulong)answer;
            return true;
        }
    }
}
