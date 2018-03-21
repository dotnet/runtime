// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers.Text
{
    public static partial class Utf8Parser
    {
        private static bool TryParseSByteD(ReadOnlySpan<byte> source, out sbyte value, out int bytesConsumed)
        {
            if (source.Length < 1)
                goto FalseExit;

            int sign = 1;
            int index = 0;
            int num = source[index];
            if (num == '-')
            {
                sign = -1;
                index++;
                if ((uint)index >= (uint)source.Length)
                    goto FalseExit;
                num = source[index];
            }
            else if (num == '+')
            {
                index++;
                if ((uint)index >= (uint)source.Length)
                    goto FalseExit;
                num = source[index];
            }

            int answer = 0;

            if (ParserHelpers.IsDigit(num))
            {
                if (num == '0')
                {
                    do
                    {
                        index++;
                        if ((uint)index >= (uint)source.Length)
                            goto Done;
                        num = source[index];
                    } while (num == '0');
                    if (!ParserHelpers.IsDigit(num))
                        goto Done;
                }

                answer = num - '0';
                index++;

                if ((uint)index >= (uint)source.Length)
                    goto Done;
                num = source[index];
                if (!ParserHelpers.IsDigit(num))
                    goto Done;
                index++;
                answer = 10 * answer + num - '0';

                // Potential overflow
                if ((uint)index >= (uint)source.Length)
                    goto Done;
                num = source[index];
                if (!ParserHelpers.IsDigit(num))
                    goto Done;
                index++;
                answer = answer * 10 + num - '0';
                // if sign < 0, (-1 * sign + 1) / 2 = 1
                // else, (-1 * sign + 1) / 2 = 0
                if ((uint)answer > (uint)sbyte.MaxValue + (-1 * sign + 1) / 2)
                    goto FalseExit; // Overflow

                if ((uint)index >= (uint)source.Length)
                    goto Done;
                if (!ParserHelpers.IsDigit(source[index]))
                    goto Done;

                // Guaranteed overflow
                goto FalseExit;
            }

        FalseExit:
            bytesConsumed = default;
            value = default;
            return false;

        Done:
            bytesConsumed = index;
            value = (sbyte)(answer * sign);
            return true;
        }

        private static bool TryParseInt16D(ReadOnlySpan<byte> source, out short value, out int bytesConsumed)
        {
            if (source.Length < 1)
                goto FalseExit;

            int sign = 1;
            int index = 0;
            int num = source[index];
            if (num == '-')
            {
                sign = -1;
                index++;
                if ((uint)index >= (uint)source.Length)
                    goto FalseExit;
                num = source[index];
            }
            else if (num == '+')
            {
                index++;
                if ((uint)index >= (uint)source.Length)
                    goto FalseExit;
                num = source[index];
            }

            int answer = 0;

            if (ParserHelpers.IsDigit(num))
            {
                if (num == '0')
                {
                    do
                    {
                        index++;
                        if ((uint)index >= (uint)source.Length)
                            goto Done;
                        num = source[index];
                    } while (num == '0');
                    if (!ParserHelpers.IsDigit(num))
                        goto Done;
                }

                answer = num - '0';
                index++;

                if ((uint)index >= (uint)source.Length)
                    goto Done;
                num = source[index];
                if (!ParserHelpers.IsDigit(num))
                    goto Done;
                index++;
                answer = 10 * answer + num - '0';

                if ((uint)index >= (uint)source.Length)
                    goto Done;
                num = source[index];
                if (!ParserHelpers.IsDigit(num))
                    goto Done;
                index++;
                answer = 10 * answer + num - '0';

                if ((uint)index >= (uint)source.Length)
                    goto Done;
                num = source[index];
                if (!ParserHelpers.IsDigit(num))
                    goto Done;
                index++;
                answer = 10 * answer + num - '0';

                // Potential overflow
                if ((uint)index >= (uint)source.Length)
                    goto Done;
                num = source[index];
                if (!ParserHelpers.IsDigit(num))
                    goto Done;
                index++;
                answer = answer * 10 + num - '0';
                // if sign < 0, (-1 * sign + 1) / 2 = 1
                // else, (-1 * sign + 1) / 2 = 0
                if ((uint)answer > (uint)short.MaxValue + (-1 * sign + 1) / 2)
                    goto FalseExit; // Overflow

                if ((uint)index >= (uint)source.Length)
                    goto Done;
                if (!ParserHelpers.IsDigit(source[index]))
                    goto Done;

                // Guaranteed overflow
                goto FalseExit;
            }

        FalseExit:
            bytesConsumed = default;
            value = default;
            return false;

        Done:
            bytesConsumed = index;
            value = (short)(answer * sign);
            return true;
        }

        private static bool TryParseInt32D(ReadOnlySpan<byte> source, out int value, out int bytesConsumed)
        {
            if (source.Length < 1)
                goto FalseExit;

            int sign = 1;
            int index = 0;
            int num = source[index];
            if (num == '-')
            {
                sign = -1;
                index++;
                if ((uint)index >= (uint)source.Length)
                    goto FalseExit;
                num = source[index];
            }
            else if (num == '+')
            {
                index++;
                if ((uint)index >= (uint)source.Length)
                    goto FalseExit;
                num = source[index];
            }

            int answer = 0;

            if (ParserHelpers.IsDigit(num))
            {
                if (num == '0')
                {
                    do
                    {
                        index++;
                        if ((uint)index >= (uint)source.Length)
                            goto Done;
                        num = source[index];
                    } while (num == '0');
                    if (!ParserHelpers.IsDigit(num))
                        goto Done;
                }

                answer = num - '0';
                index++;

                if ((uint)index >= (uint)source.Length)
                    goto Done;
                num = source[index];
                if (!ParserHelpers.IsDigit(num))
                    goto Done;
                index++;
                answer = 10 * answer + num - '0';

                if ((uint)index >= (uint)source.Length)
                    goto Done;
                num = source[index];
                if (!ParserHelpers.IsDigit(num))
                    goto Done;
                index++;
                answer = 10 * answer + num - '0';

                if ((uint)index >= (uint)source.Length)
                    goto Done;
                num = source[index];
                if (!ParserHelpers.IsDigit(num))
                    goto Done;
                index++;
                answer = 10 * answer + num - '0';

                if ((uint)index >= (uint)source.Length)
                    goto Done;
                num = source[index];
                if (!ParserHelpers.IsDigit(num))
                    goto Done;
                index++;
                answer = 10 * answer + num - '0';

                if ((uint)index >= (uint)source.Length)
                    goto Done;
                num = source[index];
                if (!ParserHelpers.IsDigit(num))
                    goto Done;
                index++;
                answer = 10 * answer + num - '0';

                if ((uint)index >= (uint)source.Length)
                    goto Done;
                num = source[index];
                if (!ParserHelpers.IsDigit(num))
                    goto Done;
                index++;
                answer = 10 * answer + num - '0';

                if ((uint)index >= (uint)source.Length)
                    goto Done;
                num = source[index];
                if (!ParserHelpers.IsDigit(num))
                    goto Done;
                index++;
                answer = 10 * answer + num - '0';

                if ((uint)index >= (uint)source.Length)
                    goto Done;
                num = source[index];
                if (!ParserHelpers.IsDigit(num))
                    goto Done;
                index++;
                answer = 10 * answer + num - '0';

                // Potential overflow
                if ((uint)index >= (uint)source.Length)
                    goto Done;
                num = source[index];
                if (!ParserHelpers.IsDigit(num))
                    goto Done;
                index++;
                if (answer > int.MaxValue / 10)
                    goto FalseExit; // Overflow
                answer = answer * 10 + num - '0';
                // if sign < 0, (-1 * sign + 1) / 2 = 1
                // else, (-1 * sign + 1) / 2 = 0
                if ((uint)answer > (uint)int.MaxValue + (-1 * sign + 1) / 2)
                    goto FalseExit; // Overflow

                if ((uint)index >= (uint)source.Length)
                    goto Done;
                if (!ParserHelpers.IsDigit(source[index]))
                    goto Done;

                // Guaranteed overflow
                goto FalseExit;
            }

        FalseExit:
            bytesConsumed = default;
            value = default;
            return false;

        Done:
            bytesConsumed = index;
            value = answer * sign;
            return true;
        }

        private static bool TryParseInt64D(ReadOnlySpan<byte> source, out long value, out int bytesConsumed)
        {
            if (source.Length < 1)
            {
                bytesConsumed = 0;
                value = default;
                return false;
            }

            int indexOfFirstDigit = 0;
            int sign = 1;
            if (source[0] == '-')
            {
                indexOfFirstDigit = 1;
                sign = -1;

                if (source.Length <= indexOfFirstDigit)
                {
                    bytesConsumed = 0;
                    value = default;
                    return false;
                }
            }
            else if (source[0] == '+')
            {
                indexOfFirstDigit = 1;

                if (source.Length <= indexOfFirstDigit)
                {
                    bytesConsumed = 0;
                    value = default;
                    return false;
                }
            }

            int overflowLength = ParserHelpers.Int64OverflowLength + indexOfFirstDigit;

            // Parse the first digit separately. If invalid here, we need to return false.
            long firstDigit = source[indexOfFirstDigit] - 48; // '0'
            if (firstDigit < 0 || firstDigit > 9)
            {
                bytesConsumed = 0;
                value = default;
                return false;
            }
            ulong parsedValue = (ulong)firstDigit;

            if (source.Length < overflowLength)
            {
                // Length is less than Parsers.Int64OverflowLength; overflow is not possible
                for (int index = indexOfFirstDigit + 1; index < source.Length; index++)
                {
                    long nextDigit = source[index] - 48; // '0'
                    if (nextDigit < 0 || nextDigit > 9)
                    {
                        bytesConsumed = index;
                        value = ((long)parsedValue) * sign;
                        return true;
                    }
                    parsedValue = parsedValue * 10 + (ulong)nextDigit;
                }
            }
            else
            {
                // Length is greater than Parsers.Int64OverflowLength; overflow is only possible after Parsers.Int64OverflowLength
                // digits. There may be no overflow after Parsers.Int64OverflowLength if there are leading zeroes.
                for (int index = indexOfFirstDigit + 1; index < overflowLength - 1; index++)
                {
                    long nextDigit = source[index] - 48; // '0'
                    if (nextDigit < 0 || nextDigit > 9)
                    {
                        bytesConsumed = index;
                        value = ((long)parsedValue) * sign;
                        return true;
                    }
                    parsedValue = parsedValue * 10 + (ulong)nextDigit;
                }
                for (int index = overflowLength - 1; index < source.Length; index++)
                {
                    long nextDigit = source[index] - 48; // '0'
                    if (nextDigit < 0 || nextDigit > 9)
                    {
                        bytesConsumed = index;
                        value = ((long)parsedValue) * sign;
                        return true;
                    }
                    // If parsedValue > (long.MaxValue / 10), any more appended digits will cause overflow.
                    // if parsedValue == (long.MaxValue / 10), any nextDigit greater than 7 or 8 (depending on sign) implies overflow.
                    bool positive = sign > 0;
                    bool nextDigitTooLarge = nextDigit > 8 || (positive && nextDigit > 7);
                    if (parsedValue > long.MaxValue / 10 || parsedValue == long.MaxValue / 10 && nextDigitTooLarge)
                    {
                        bytesConsumed = 0;
                        value = default;
                        return false;
                    }
                    parsedValue = parsedValue * 10 + (ulong)nextDigit;
                }
            }

            bytesConsumed = source.Length;
            value = ((long)parsedValue) * sign;
            return true;
        }
    }
}
