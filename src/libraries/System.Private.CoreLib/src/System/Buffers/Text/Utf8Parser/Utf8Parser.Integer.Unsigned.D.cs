// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers.Text
{
    public static partial class Utf8Parser
    {
        private static bool TryParseByteD(ReadOnlySpan<byte> source, out byte value, out int bytesConsumed)
        {
            if (source.Length < 1)
                goto FalseExit;

            int index = 0;
            int num = source[index];
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
                if ((uint)answer > byte.MaxValue)
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
            value = (byte)answer;
            return true;
        }

        private static bool TryParseUInt16D(ReadOnlySpan<byte> source, out ushort value, out int bytesConsumed)
        {
            if (source.Length < 1)
                goto FalseExit;

            int index = 0;
            int num = source[index];
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
                if ((uint)answer > ushort.MaxValue)
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
            value = (ushort)answer;
            return true;
        }

        private static bool TryParseUInt32D(ReadOnlySpan<byte> source, out uint value, out int bytesConsumed)
        {
            if (source.Length < 1)
                goto FalseExit;

            int index = 0;
            int num = source[index];
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
                if (((uint)answer) > uint.MaxValue / 10 || (((uint)answer) == uint.MaxValue / 10 && num > '5'))
                    goto FalseExit; // Overflow
                answer = answer * 10 + num - '0';

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
            value = (uint)answer;
            return true;
        }

        private static bool TryParseUInt64D(ReadOnlySpan<byte> source, out ulong value, out int bytesConsumed)
        {
            if (source.IsEmpty)
            {
                goto FalseExit;
            }

            // We use 'nuint' for the firstDigit and nextChar data types in this method because
            // it gives us a free early zero-extension to 64 bits when running on a 64-bit platform.
            //
            // Parse the first digit separately. If invalid here, we need to return false.

            nuint firstDigit = (uint)source[0] - '0';
            if ((uint)firstDigit > 9) { goto FalseExit; }
            ulong parsedValue = firstDigit;

            // At this point, we successfully read a single digit character.
            // The only failure condition from here on out is integer overflow.

            int idx = 1;
            if (source.Length < ParserHelpers.UInt64OverflowLength)
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
                    // might overflow ulong.MaxValue. If the current accumulator is below
                    // this const, there's no risk of overflowing.

                    const ulong OverflowRisk = 0x1999_9999_9999_9999ul;

                    if (parsedValue < OverflowRisk)
                    {
                        parsedValue = parsedValue * 10 + nextChar;
                        continue;
                    }

                    // If the current accumulator is exactly equal to the const above,
                    // then "accumulator * 10 + 5" is the highest we can go without overflowing
                    // ulong.MaxValue. This also implies that if the current accumulator
                    // is higher than the const above, there's no hope that we'll succeed,
                    // so we may as well just fail now.

                    if (parsedValue != OverflowRisk || (uint)nextChar > 5)
                    {
                        goto FalseExit;
                    }

                    parsedValue = OverflowRisk * 10 + nextChar;
                }
            }

            bytesConsumed = idx;
            value = parsedValue;
            return true;

        FalseExit:
            bytesConsumed = 0;
            value = default;
            return false;
        }
    }
}
