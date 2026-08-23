// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            int idx = 1;

            if (source.Length == 1)
            {
                goto Done;
            }

            // Parse the first four digits individually so early invalid inputs remain cheap.
            while (idx < 4)
            {
                if ((uint)idx >= (uint)source.Length) { goto Done; }
                nuint nextChar = (uint)source[idx] - '0';
                if ((uint)nextChar > 9) { goto Done; }
                parsedValue = parsedValue * 10 + nextChar;
                idx++;
            }

            if (source.Length < 8) { goto ParseRemainingBounded; }
            uint second = BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(4));
            if (!AreFourDigits(second)) { goto ParseRemaining; }
            parsedValue = parsedValue * 10_000 + ParseFourDigits(second);
            idx = 8;

            if (source.Length < 12) { goto ParseRemainingBounded; }
            uint third = BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(8));
            if (!AreFourDigits(third)) { goto ParseRemaining; }
            parsedValue = parsedValue * 10_000 + ParseFourDigits(third);
            idx = 12;

            if (source.Length < 16) { goto ParseRemainingBounded; }
            uint fourth = BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(12));
            if (!AreFourDigits(fourth)) { goto ParseRemaining; }
            parsedValue = parsedValue * 10_000 + ParseFourDigits(fourth);
            idx = 16;

            if (source.Length < ParserHelpers.UInt64OverflowLength) { goto ParseRemainingBounded; }

            while (true)
            {
                if ((uint)idx >= (uint)source.Length) { break; } // EOF
                nuint nextChar = (uint)source[idx] - '0';
                if ((uint)nextChar > 9) { break; } // not a digit
                idx++;

                const ulong OverflowRisk = 0x1999_9999_9999_9999ul;

                if (parsedValue < OverflowRisk)
                {
                    parsedValue = parsedValue * 10 + nextChar;
                    continue;
                }

                if (parsedValue != OverflowRisk || (uint)nextChar > 5)
                {
                    goto FalseExit;
                }

                parsedValue = OverflowRisk * 10 + nextChar;
            }

            goto Done;

        ParseRemaining:
            while (true)
            {
                nuint nextChar = (uint)source[idx] - '0';
                if ((uint)nextChar > 9) { break; }
                parsedValue = parsedValue * 10 + nextChar;
                idx++;
            }

            goto Done;

        ParseRemainingBounded:
            while ((uint)idx < (uint)source.Length)
            {
                nuint nextChar = (uint)source[idx] - '0';
                if ((uint)nextChar > 9) { break; }
                parsedValue = parsedValue * 10 + nextChar;
                idx++;
            }

        Done:
            bytesConsumed = idx;
            value = parsedValue;
            return true;

        FalseExit:
            bytesConsumed = 0;
            value = default;
            return false;
        }

        // Each byte is an ASCII digit iff neither boundary calculation sets its high bit.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AreFourDigits(uint value) =>
            (((value + 0x46464646u) | (value - 0x30303030u)) & 0x80808080u) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ParseFourDigits(uint value)
        {
            value -= 0x30303030u;
            return (value & 0xFF) * 1_000 +
                ((value >> 8) & 0xFF) * 100 +
                ((value >> 16) & 0xFF) * 10 +
                (value >> 24);
        }
    }
}
