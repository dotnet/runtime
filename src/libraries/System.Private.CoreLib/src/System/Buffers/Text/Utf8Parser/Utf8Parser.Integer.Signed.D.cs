// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            long sign = 0; // 0 if the value is positive, -1 if the value is negative
            int idx = 0;

            // We use 'nuint' for the firstChar and nextChar data types in this method because
            // it gives us a free early zero-extension to 64 bits when running on a 64-bit platform.

            nuint firstChar;
            while (true)
            {
                if ((uint)idx >= (uint)source.Length) { goto FalseExit; }
                firstChar = (uint)source[idx] - '0';
                if ((uint)firstChar <= 9) { break; }

                // We saw something that wasn't a digit. If it's a '+' or a '-',
                // we'll set the 'sign' value appropriately and resume the "read
                // first char" loop from the next index. If this loops more than
                // once (idx != 0), it means we saw a sign character followed by
                // a non-digit character, which should be considered an error.

                if (idx != 0)
                {
                    goto FalseExit;
                }

                idx++;

                if ((uint)firstChar == unchecked((uint)('-' - '0')))
                {
                    sign--; // set to -1
                }
                else if ((uint)firstChar != unchecked((uint)('+' - '0')))
                {
                    goto FalseExit; // not a digit, not '-', and not '+'; fail
                }
            }

            ulong parsedValue = firstChar;
            int overflowLength = ParserHelpers.Int64OverflowLength + idx; // +idx to account for any sign char we read
            idx++;

            // At this point, we successfully read a single digit character.
            // The only failure condition from here on out is integer overflow.

            if (source.Length < overflowLength)
            {
                // If the input span is short enough such that integer overflow isn't an issue,
                // don't bother performing overflow checks. Just keep shifting in new digits
                // until we see a non-digit character or until we've exhausted our input buffer.

                while (true)
                {
                    if ((uint)idx >= (uint)source.Length) { break; } // EOF
                    nuint nextChar = (uint)source[idx] - '0';
                    if ((uint)nextChar > 9) { break; } // not a digit
                    parsedValue = parsedValue * 10 + nextChar;
                    idx++;
                }
            }
            else
            {
                while (true)
                {
                    if ((uint)idx >= (uint)source.Length) { break; } // EOF
                    nuint nextChar = (uint)source[idx] - '0';
                    if ((uint)nextChar > 9) { break; } // not a digit
                    idx++;

                    // The const below is the smallest unsigned x for which "x * 10 + 9"
                    // might overflow long.MaxValue. If the current accumulator is below
                    // this const, there's no risk of overflowing.

                    const ulong OverflowRisk = 0x0CCC_CCCC_CCCC_CCCCul;

                    if (parsedValue < OverflowRisk)
                    {
                        parsedValue = parsedValue * 10 + nextChar;
                        continue;
                    }

                    // If the current accumulator is exactly equal to the const above,
                    // then "accumulator * 10 + 7" is the highest we can go without overflowing
                    // long.MaxValue. (If we know the value is negative, we can instead allow
                    // +8, since the range of negative numbers is one higher than the range of
                    // positive numbers.) This also implies that if the current accumulator
                    // is higher than the const above, there's no hope that we'll succeed,
                    // so we may as well just fail now.
                    //
                    // The (nextChar + sign) trick below works because sign is 0 or -1,
                    // so if sign is -1 then this actually checks that nextChar > 8.
                    // n.b. signed arithmetic below because nextChar may be 0.

                    if (parsedValue != OverflowRisk || (int)nextChar + (int)sign > 7)
                    {
                        goto FalseExit;
                    }

                    parsedValue = OverflowRisk * 10 + nextChar;
                }
            }

            // 'sign' is 0 for non-negative and -1 for negative. This allows us to perform
            // cheap arithmetic + bitwise operations to mimic a multiplication by 1 or -1
            // without incurring the cost of an actual multiplication operation.
            //
            // If sign = 0,  this becomes value = (parsedValue ^  0) -   0  = parsedValue
            // If sign = -1, this becomes value = (parsedValue ^ -1) - (-1) = ~parsedValue + 1 = -parsedValue

            bytesConsumed = idx;
            value = ((long)parsedValue ^ sign) - sign;
            return true;

        FalseExit:
            bytesConsumed = 0;
            value = default;
            return false;
        }
    }
}
