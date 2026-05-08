// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    internal static partial class Number
    {
        private const NumberStyles InvalidNumberStyles = ~(NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite
                                                           | NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign
                                                           | NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint
                                                           | NumberStyles.AllowThousands | NumberStyles.AllowExponent
                                                           | NumberStyles.AllowCurrencySymbol | NumberStyles.AllowHexSpecifier
                                                           | NumberStyles.AllowBinarySpecifier);

        private static nuint[]? s_cachedPowersOf1e9;

        private static ReadOnlySpan<nuint> UInt32PowersOfTen => nint.Size == 8
            ? MemoryMarshal.Cast<ulong, nuint>(UInt64PowersOfTen)
            : MemoryMarshal.Cast<uint, nuint>(UInt32PowersOfTenCore);

        private static ReadOnlySpan<uint> UInt32PowersOfTenCore => [1, 10, 100, 1000, 10000, 100000, 1000000, 10000000, 100000000, 1000000000];
        private static ReadOnlySpan<ulong> UInt64PowersOfTen => [1, 10, 100, 1000, 10000, 100000, 1000000, 10000000, 100000000, 1000000000];

        [DoesNotReturn]
        internal static void ThrowOverflowOrFormatException(ParsingStatus status)
        {
            throw status == ParsingStatus.Failed
                ? new FormatException(SR.Overflow_ParseBigInteger)
                : new OverflowException(SR.Overflow_ParseBigInteger);
        }

        internal static bool TryValidateParseStyleInteger(NumberStyles style, [NotNullWhen(false)] out ArgumentException? e)
        {
            // Check for undefined flags
            if ((style & InvalidNumberStyles) != 0)
            {
                e = new ArgumentException(SR.Argument_InvalidNumberStyles, nameof(style));
                return false;
            }

            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            { // Check for hex number
                if ((style & ~NumberStyles.HexNumber) != 0)
                {
                    e = new ArgumentException(SR.Argument_InvalidHexStyle, nameof(style));
                    return false;
                }
            }

            e = null;
            return true;
        }

        internal static ParsingStatus TryParseBigInteger<TChar>(ReadOnlySpan<TChar> value, NumberStyles style, NumberFormatInfo info, out BigInteger result)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            if (!TryValidateParseStyleInteger(style, out ArgumentException? e))
            {
                throw e; // TryParse still throws ArgumentException on invalid NumberStyles
            }

            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            {
                return TryParseBigIntegerHexOrBinaryNumberStyle<BigIntegerHexParser<TChar>, TChar>(value, style, out result);
            }

            if ((style & NumberStyles.AllowBinarySpecifier) != 0)
            {
                return TryParseBigIntegerHexOrBinaryNumberStyle<BigIntegerBinaryParser<TChar>, TChar>(value, style, out result);
            }

            return TryParseBigIntegerNumber(value, style, info, out result);
        }

        internal static unsafe ParsingStatus TryParseBigIntegerNumber<TChar>(ReadOnlySpan<TChar> value, NumberStyles style, NumberFormatInfo info, out BigInteger result)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            scoped Span<byte> buffer;
            byte[]? arrayFromPool = null;

            if (value.Length == 0)
            {
                result = default;
                return ParsingStatus.Failed;
            }

            if (value.Length < 255)
            {
                buffer = stackalloc byte[value.Length + 1 + 1];
            }
            else
            {
                buffer = arrayFromPool = ArrayPool<byte>.Shared.Rent(value.Length + 1 + 1);
            }

            ParsingStatus ret;

            fixed (byte* ptr = buffer) // NumberBuffer expects pinned span
            {
                NumberBuffer number = new(NumberBufferKind.Integer, buffer);

                if (!TryStringToNumber(value, style, ref number, info))
                {
                    result = default;
                    ret = ParsingStatus.Failed;
                }
                else
                {
                    ret = NumberToBigInteger(ref number, out result);
                }
            }

            if (arrayFromPool != null)
            {
                ArrayPool<byte>.Shared.Return(arrayFromPool);
            }

            return ret;
        }

        internal static BigInteger ParseBigInteger<TChar>(ReadOnlySpan<TChar> value, NumberStyles style, NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            if (!TryValidateParseStyleInteger(style, out ArgumentException? e))
            {
                throw e;
            }

            ParsingStatus status = TryParseBigInteger(value, style, info, out BigInteger result);
            if (status != ParsingStatus.OK)
            {
                ThrowOverflowOrFormatException(status);
            }

            return result;
        }

        internal static ParsingStatus TryParseBigIntegerHexOrBinaryNumberStyle<TParser, TChar>(ReadOnlySpan<TChar> value, NumberStyles style, out BigInteger result)
            where TParser : struct, IBigIntegerHexOrBinaryParser<TParser, TChar>
            where TChar : unmanaged, IUtfChar<TChar>
        {
            int whiteIndex;

            // Skip past any whitespace at the beginning.
            if ((style & NumberStyles.AllowLeadingWhite) != 0)
            {
                for (whiteIndex = 0; whiteIndex < value.Length; whiteIndex++)
                {
                    if (!IsWhite(TChar.CastToUInt32(value[whiteIndex])))
                    {
                        break;
                    }
                }

                value = value[whiteIndex..];
            }

            // Skip past any whitespace at the end.
            if ((style & NumberStyles.AllowTrailingWhite) != 0)
            {
                for (whiteIndex = value.Length - 1; whiteIndex >= 0; whiteIndex--)
                {
                    if (!IsWhite(TChar.CastToUInt32(value[whiteIndex])))
                    {
                        break;
                    }
                }

                value = value[..(whiteIndex + 1)];
            }

            if (value.IsEmpty)
            {
                goto FailExit;
            }

            // Remember the sign from original leading input
            // Invalid digits will be caught in parsing below
            nuint signBits = TParser.GetSignBitsIfValid(TChar.CastToUInt32(value[0]));

            // Start from leading blocks. Leading blocks can be unaligned, or whole of 0/F's that need to be trimmed.
            int leadingBitsCount = value.Length % TParser.DigitsPerBlock;

            nuint leading = signBits;
            // First parse unaligned leading block if exists.
            if (leadingBitsCount != 0)
            {
                if (!TParser.TryParseUnalignedBlock(value[0..leadingBitsCount], out leading))
                {
                    goto FailExit;
                }

                // Fill leading sign bits
                leading |= signBits << (leadingBitsCount * TParser.BitsPerDigit);
                value = value[leadingBitsCount..];
            }

            // Skip all the blocks consists of the same bit of sign
            while (!value.IsEmpty && leading == signBits)
            {
                if (!TParser.TryParseSingleBlock(value[0..TParser.DigitsPerBlock], out leading))
                {
                    goto FailExit;
                }

                value = value[TParser.DigitsPerBlock..];
            }

            if (value.IsEmpty)
            {
                // There's nothing beyond significant leading block. Return it as the result.
                nint signedLeading = (nint)leading;
                if ((nint)(leading ^ signBits) >= 0 && int.MinValue < signedLeading && signedLeading <= int.MaxValue)
                {
                    // Small value that fits in int _sign.
                    result = new BigInteger((int)signedLeading, null);
                    return ParsingStatus.OK;
                }
                else if (leading != 0)
                {
                    // The sign of result differs with leading digit, or value
                    // doesn't fit in int _sign. Require to store in _bits.

                    // Positive: sign=1, bits=[leading]
                    // Negative: sign=-1, bits=[(leading ^ -1) + 1]=[-leading]
                    result = new BigInteger((int)signBits | 1, [(leading ^ signBits) - signBits]);
                    return ParsingStatus.OK;
                }
                else
                {
                    // -1 << BitsPerLimb, which requires an additional nuint
                    result = new BigInteger(-1, [0, 1]);
                    return ParsingStatus.OK;
                }
            }

            // Now the size of bits array can be calculated, except edge cases of -2^(BitsPerLimb*N)
            int wholeBlockCount = value.Length / TParser.DigitsPerBlock;
            int totalUIntCount = wholeBlockCount + 1;

            // Early out for too large input
            if (totalUIntCount > BigInteger.MaxLength)
            {
                result = default;
                return ParsingStatus.Overflow;
            }

            nuint[] bits = new nuint[totalUIntCount];
            Span<nuint> wholeBlockDestination = bits.AsSpan(0, wholeBlockCount);

            if (!TParser.TryParseWholeBlocks(value, wholeBlockDestination))
            {
                goto FailExit;
            }

            bits[^1] = leading;

            if (signBits != 0)
            {
                // For negative values, negate the whole array
                if (bits.AsSpan().ContainsAnyExcept(0u))
                {
                    NumericsHelpers.DangerousMakeTwosComplement(bits);
                }
                else
                {
                    // For negative values with all-zero trailing digits,
                    // It requires additional leading 1.
                    if (bits.Length + 1 > BigInteger.MaxLength)
                    {
                        result = default;
                        return ParsingStatus.Overflow;
                    }

                    bits = new nuint[bits.Length + 1];
                    bits[^1] = 1;
                }

                result = new BigInteger(-1, bits);
                return ParsingStatus.OK;
            }
            else
            {
                Debug.Assert(leading != 0);

                // For positive values, it's done
                result = new BigInteger(1, bits);
                return ParsingStatus.OK;
            }

        FailExit:
            result = default;
            return ParsingStatus.Failed;
        }

        //
        // This threshold is for choosing the algorithm to use based on the number of digits.
        //
        // Let N be the number of digits. If N is less than or equal to the bound, use a naive
        // algorithm with a running time of O(N^2). And if it is greater than the threshold, use
        // a divide-and-conquer algorithm with a running time of O(NlogN).
        //
        // `1233`, which is approx the upper bound of most RSA key lengths, covers the majority
        // of most common inputs and allows for the less naive algorithm to be used for
        // large/uncommon inputs.
        //
        public
#if DEBUG
        static // Mutable for unit testing...
#else
        const
#endif
        int
            BigIntegerParseNaiveThreshold = 1233,
            BigIntegerParseNaiveThresholdInRecursive = 1 << 7;

        private static ParsingStatus NumberToBigInteger(ref NumberBuffer number, out BigInteger result)
        {
            if (number.Scale == int.MaxValue)
            {
                result = default;
                return ParsingStatus.Overflow;
            }

            if (number.Scale < 0)
            {
                result = default;
                return ParsingStatus.Failed;
            }

            // The intermediate decimal representation uses base 10^9 stored in nuint elements.
            // On 64-bit this wastes 32 bits per element, but switching to base 10^19 regresses
            // ToString (UInt128 division by 10^19 is ~10x slower than JIT-optimized ulong / 10^9),
            // and using Span<uint> would require duplicating BigIntegerCalculator arithmetic routines
            // since the D&C algorithm reuses Multiply/Square/Divide on these spans.
            scoped Span<nuint> base1E9;

            ReadOnlySpan<byte> intDigits= number.Digits.Slice(0, Math.Min(number.Scale, number.DigitsCount));
            int intDigitsEnd = intDigits.IndexOf<byte>(0);
            if (intDigitsEnd < 0)
            {
                // Check for nonzero digits after the decimal point.
                ReadOnlySpan<byte> fracDigitsSpan = number.Digits.Slice(intDigits.Length);
                foreach (byte digitChar in fracDigitsSpan)
                {
                    if (digitChar == '\0')
                    {
                        break;
                    }

                    if (digitChar != '0')
                    {
                        result = default;
                        return ParsingStatus.Failed;
                    }
                }
            }
            else
            {
                intDigits = intDigits.Slice(0, intDigitsEnd);
            }

            int base1E9Length = (intDigits.Length + PowersOf1e9.MaxPartialDigits - 1) / PowersOf1e9.MaxPartialDigits;
            base1E9 = BigInteger.RentedBuffer.Create(base1E9Length, out BigInteger.RentedBuffer base1E9Rental);

            int di = base1E9Length;
            ReadOnlySpan<byte> leadingDigits = intDigits[..(intDigits.Length % PowersOf1e9.MaxPartialDigits)];
            if (leadingDigits.Length != 0)
            {
                uint.TryParse(leadingDigits, out uint leadingVal);
                base1E9[--di] = leadingVal;
            }

            intDigits = intDigits.Slice(leadingDigits.Length);
            Debug.Assert(intDigits.Length % PowersOf1e9.MaxPartialDigits == 0);

            for (--di; di >= 0; --di)
            {
                uint.TryParse(intDigits.Slice(0, PowersOf1e9.MaxPartialDigits), out uint partialVal);
                base1E9[di] = partialVal;
                intDigits = intDigits.Slice(PowersOf1e9.MaxPartialDigits);
            }

            Debug.Assert(intDigits.Length == 0);

            // Estimate limb count needed for the decimal value.
            double digitRatio = 0.10381025297 * 32.0 / BigIntegerCalculator.BitsPerLimb; // log_{2^BitsPerLimb}(10)
            int resultLength = checked((int)(digitRatio * number.Scale) + 1 + 2);
            Span<nuint> resultBuffer = BigInteger.RentedBuffer.Create(resultLength, out BigInteger.RentedBuffer resultRental);

            int totalDigitCount = Math.Min(number.DigitsCount, number.Scale);
            int trailingZeroCount = number.Scale - totalDigitCount;

            if (number.Scale <= BigIntegerParseNaiveThreshold)
            {
                Naive(base1E9, trailingZeroCount, resultBuffer);
            }
            else
            {
                DivideAndConquer(base1E9, trailingZeroCount, resultBuffer);
            }

            result = new BigInteger(resultBuffer, number.IsNegative);

            base1E9Rental.Dispose();
            resultRental.Dispose();

            return ParsingStatus.OK;

            static void DivideAndConquer(ReadOnlySpan<nuint> base1E9, int trailingZeroCount, scoped Span<nuint> bits)
            {
                int valueDigits = (base1E9.Length - 1) * PowersOf1e9.MaxPartialDigits + FormattingHelpers.CountDigits(base1E9[^1]);

                int powersOf1e9BufferLength = PowersOf1e9.GetBufferSize(Math.Max(valueDigits, trailingZeroCount + 1), out int maxIndex);
                PowersOf1e9 powersOf1e9 = PowersOf1e9.GetCached(powersOf1e9BufferLength);

                if (trailingZeroCount > 0)
                {
                    double digitRatio = 0.10381025297 * 32.0 / BigIntegerCalculator.BitsPerLimb;
                    int leadingLength = checked((int)(digitRatio * PowersOf1e9.MaxPartialDigits * base1E9.Length) + 3);
                    Span<nuint> leading = BigInteger.RentedBuffer.Create(leadingLength, out BigInteger.RentedBuffer leadingBuffer);

                    Recursive(powersOf1e9, maxIndex, base1E9, leading);
                    leading = leading.Slice(0, BigIntegerCalculator.ActualLength(leading));

                    powersOf1e9.MultiplyPowerOfTen(leading, trailingZeroCount, bits);

                    leadingBuffer.Dispose();
                }
                else
                {
                    Recursive(powersOf1e9, maxIndex, base1E9, bits);
                }
            }

            static void Recursive(in PowersOf1e9 powersOf1e9, int powersOf1e9Index, ReadOnlySpan<nuint> base1E9, Span<nuint> bits)
            {
                Debug.Assert(bits.Trim((nuint)0).Length == 0);
                Debug.Assert(BigIntegerParseNaiveThresholdInRecursive > 1);

                base1E9 = base1E9.Slice(0, BigIntegerCalculator.ActualLength(base1E9));
                if (base1E9.Length < BigIntegerParseNaiveThresholdInRecursive)
                {
                    NaiveBase1E9ToBits(base1E9, bits);
                    return;
                }

                int multiplier1E9Length = 1 << powersOf1e9Index;
                while (base1E9.Length <= multiplier1E9Length)
                {
                    multiplier1E9Length = 1 << (--powersOf1e9Index);
                }

                ReadOnlySpan<nuint> multiplier = powersOf1e9.GetSpan(powersOf1e9Index);
                int multiplierTrailingZeroCount = PowersOf1e9.OmittedLength(powersOf1e9Index);

                Debug.Assert(multiplier1E9Length < base1E9.Length && base1E9.Length <= multiplier1E9Length * 2);

                double digitRatio = 0.10381025297 * 32.0 / BigIntegerCalculator.BitsPerLimb;
                int bufferLength = checked((int)(digitRatio * PowersOf1e9.MaxPartialDigits * multiplier1E9Length) + 1 + 2);
                scoped Span<nuint> buffer = BigInteger.RentedBuffer.Create(bufferLength, out BigInteger.RentedBuffer bufferRental);

                Recursive(powersOf1e9, powersOf1e9Index - 1, base1E9[multiplier1E9Length..], buffer);

                ReadOnlySpan<nuint> buffer2 = buffer.Slice(0, BigIntegerCalculator.ActualLength(buffer));
                Span<nuint> bitsUpper = bits.Slice(multiplierTrailingZeroCount, buffer2.Length + multiplier.Length);
                BigIntegerCalculator.Multiply(buffer2, multiplier, bitsUpper);

                buffer.Clear();

                Recursive(powersOf1e9, powersOf1e9Index - 1, base1E9[..multiplier1E9Length], buffer);

                BigIntegerCalculator.AddSelf(bits, buffer.Slice(0, BigIntegerCalculator.ActualLength(buffer)));

                bufferRental.Dispose();
            }

            static void Naive(ReadOnlySpan<nuint> base1E9, int trailingZeroCount, scoped Span<nuint> bits)
            {
                if (base1E9.Length == 0)
                {
                    // number is 0.
                    return;
                }

                int resultLength = NaiveBase1E9ToBits(base1E9, bits);

                int trailingPartialCount = Math.DivRem(trailingZeroCount, PowersOf1e9.MaxPartialDigits, out int remainingTrailingZeroCount);
                for (int i = 0; i < trailingPartialCount; i++)
                {
                    nuint carry = MultiplyAdd(bits.Slice(0, resultLength), PowersOf1e9.TenPowMaxPartial, 0);
                    Debug.Assert(bits[resultLength] == 0);
                    if (carry != 0)
                    {
                        bits[resultLength++] = carry;
                    }
                }

                if (remainingTrailingZeroCount != 0)
                {
                    nuint multiplier = UInt32PowersOfTen[remainingTrailingZeroCount];
                    nuint carry = MultiplyAdd(bits.Slice(0, resultLength), multiplier, 0);
                    Debug.Assert(bits[resultLength] == 0);
                    if (carry != 0)
                    {
                        bits[resultLength++] = carry;
                    }
                }
            }

            static int NaiveBase1E9ToBits(ReadOnlySpan<nuint> base1E9, Span<nuint> bits)
            {
                if (base1E9.Length == 0)
                {
                    return 0;
                }

                int resultLength = 1;
                bits[0] = base1E9[^1];
                for (int i = base1E9.Length - 2; i >= 0; i--)
                {
                    nuint carry = MultiplyAdd(bits.Slice(0, resultLength), PowersOf1e9.TenPowMaxPartial, base1E9[i]);
                    Debug.Assert(bits[resultLength] == 0);
                    if (carry != 0)
                    {
                        bits[resultLength++] = carry;
                    }
                }

                return resultLength;
            }

            static nuint MultiplyAdd(Span<nuint> bits, nuint multiplier, nuint addValue)
            {
                nuint carry = addValue;

                if (nint.Size == 8)
                {
                    for (int i = 0; i < bits.Length; i++)
                    {
                        UInt128 p = (UInt128)bits[i] * multiplier + carry;
                        bits[i] = (nuint)(ulong)p;
                        carry = (nuint)(ulong)(p >> 64);
                    }
                }
                else
                {
                    for (int i = 0; i < bits.Length; i++)
                    {
                        ulong p = (ulong)multiplier * bits[i] + carry;
                        bits[i] = (uint)p;
                        carry = (uint)(p >> 32);
                    }
                }

                return carry;
            }
        }

        private static string? FormatBigIntegerToHex<TChar>(bool targetSpan, BigInteger value, char format, int digits, NumberFormatInfo info, Span<TChar> destination, out int charsWritten, out bool spanSuccess)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(format is 'x' or 'X');

            // Get the bytes that make up the BigInteger.
            byte[]? arrayToReturnToPool = null;
            Span<byte> bits = stackalloc byte[64]; // arbitrary threshold
            if (!value.TryWriteOrCountBytes(bits, out int bytesWrittenOrNeeded))
            {
                bits = arrayToReturnToPool = ArrayPool<byte>.Shared.Rent(bytesWrittenOrNeeded);
                bool success = value.TryWriteBytes(bits, out bytesWrittenOrNeeded);
                Debug.Assert(success);
            }

            bits = bits.Slice(0, bytesWrittenOrNeeded);

            var sb = new ValueStringBuilder<TChar>(stackalloc TChar[128]); // each byte is typically two chars

            int cur = bits.Length - 1;
            if (cur > -1)
            {
                // [FF..F8] drop the high F as the two's complement negative number remains clear
                // [F7..08] retain the high bits as the two's complement number is wrong without it
                // [07..00] drop the high 0 as the two's complement positive number remains clear
                bool clearHighF = false;
                byte head = bits[cur];

                if (head > 0xF7)
                {
                    head -= 0xF0;
                    clearHighF = true;
                }

                if (head < 0x08 || clearHighF)
                {
                    // {0xF8-0xFF} print as {8-F}
                    // {0x00-0x07} print as {0-7}
                    sb.Append(head < 10 ?
                        TChar.CastFrom(head + '0') :
                        TChar.CastFrom(format == 'X' ? ((head & 0xF) - 10 + 'A') : ((head & 0xF) - 10 + 'a')));
                    cur--;
                }
            }

            if (cur > -1)
            {
                Span<TChar> chars = sb.AppendSpan((cur + 1) * 2);
                int charsPos = 0;
                string hexValues = format == 'x' ? "0123456789abcdef" : "0123456789ABCDEF";
                while (cur > -1)
                {
                    byte b = bits[cur--];
                    chars[charsPos++] = TChar.CastFrom(hexValues[b >> 4]);
                    chars[charsPos++] = TChar.CastFrom(hexValues[b & 0xF]);
                }
            }

            if (digits > sb.Length)
            {
                // Insert leading zeros, e.g. user specified "X5" so we create "0ABCD" instead of "ABCD"
                sb.Insert(
                    0,
                    TChar.CastFrom(value._sign >= 0 ? '0' : (format == 'x') ? 'f' : 'F'),
                    digits - sb.Length);
            }

            if (arrayToReturnToPool != null)
            {
                ArrayPool<byte>.Shared.Return(arrayToReturnToPool);
            }

            if (targetSpan)
            {
                charsWritten = (spanSuccess = sb.AsSpan().TryCopyTo(destination)) ? sb.Length : 0;
                sb.Dispose();
                return null;
            }
            else
            {
                charsWritten = 0;
                spanSuccess = false;
                return sb.ToString();
            }
        }

        private static string? FormatBigIntegerToBinary<TChar>(bool targetSpan, BigInteger value, int digits, Span<TChar> destination, out int charsWritten, out bool spanSuccess)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            // Get the bytes that make up the BigInteger.
            byte[]? arrayToReturnToPool = null;
            Span<byte> bytes = stackalloc byte[64]; // arbitrary threshold
            if (!value.TryWriteOrCountBytes(bytes, out int bytesWrittenOrNeeded))
            {
                bytes = arrayToReturnToPool = ArrayPool<byte>.Shared.Rent(bytesWrittenOrNeeded);
                bool success = value.TryWriteBytes(bytes, out _);
                Debug.Assert(success);
            }

            bytes = bytes.Slice(0, bytesWrittenOrNeeded);

            Debug.Assert(!bytes.IsEmpty);

            byte highByte = bytes[^1];

            int charsInHighByte = 9 - byte.LeadingZeroCount(value._sign >= 0 ? highByte : (byte)~highByte);
            long tmpCharCount = charsInHighByte + ((long)(bytes.Length - 1) << 3);

            if (tmpCharCount > Array.MaxLength)
            {
                Debug.Assert(arrayToReturnToPool is not null);
                ArrayPool<byte>.Shared.Return(arrayToReturnToPool);

                throw new FormatException(SR.Format_TooLarge);
            }

            int charsForBits = (int)tmpCharCount;

            Debug.Assert(digits < Array.MaxLength);
            int charsIncludeDigits = Math.Max(digits, charsForBits);

            {
                scoped ValueStringBuilder<TChar> sb;
                if (targetSpan)
                {
                    if (charsIncludeDigits > destination.Length)
                    {
                        if (arrayToReturnToPool is not null)
                        {
                            ArrayPool<byte>.Shared.Return(arrayToReturnToPool);
                        }

                        charsWritten = 0;
                        spanSuccess = false;
                        return null;
                    }

                    // Because we have ensured destination can take actual TChar length, so now just use ValueStringBuilder as wrapper so that subsequent logic can be reused by 2 flows (targetSpan and non-targetSpan);
                    // meanwhile there is no need to copy to destination again after format data for targetSpan flow.
                    sb = new ValueStringBuilder<TChar>(destination);
                }
                else
                {
                    // each byte is typically eight chars
                    sb = charsIncludeDigits > 512
                        ? new ValueStringBuilder<TChar>(charsIncludeDigits)
                        : new ValueStringBuilder<TChar>(stackalloc TChar[512]);
                }

                if (digits > charsForBits)
                {
                    sb.Append(TChar.CastFrom(value._sign >= 0 ? '0' : '1'), digits - charsForBits);
                }

                AppendByte(ref sb, highByte, charsInHighByte - 1);

                for (int i = bytes.Length - 2; i >= 0; i--)
                {
                    AppendByte(ref sb, bytes[i]);
                }

                Debug.Assert(sb.Length == charsIncludeDigits);

                if (arrayToReturnToPool is not null)
                {
                    ArrayPool<byte>.Shared.Return(arrayToReturnToPool);
                }

                if (targetSpan)
                {
                    charsWritten = charsIncludeDigits;
                    spanSuccess = true;
                    return null;
                }

                charsWritten = 0;
                spanSuccess = false;
                return sb.ToString();
            }

            static void AppendByte(ref ValueStringBuilder<TChar> sb, byte b, int startHighBit = 7)
            {
                for (int i = startHighBit; i >= 0; i--)
                {
                    sb.Append(TChar.CastFrom('0' + ((b >> i) & 0x1)));
                }
            }
        }

        internal static string FormatBigInteger(BigInteger value, string? format, NumberFormatInfo info)
        {
            return FormatBigInteger<Utf16Char>(targetSpan: false, value, format, format, info, default, out _, out _)!;
        }

        internal static bool TryFormatBigInteger<TChar>(BigInteger value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<TChar> destination, out int charsWritten)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            FormatBigInteger(targetSpan: true, value, null, format, info, destination, out charsWritten, out bool spanSuccess);
            return spanSuccess;
        }

        private static unsafe string? FormatBigInteger<TChar>(bool targetSpan, BigInteger value, string? formatString, ReadOnlySpan<char> formatSpan, NumberFormatInfo info, Span<TChar> destination, out int charsWritten, out bool spanSuccess)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(formatString == null || formatString.Length == formatSpan.Length);

            char fmt = ParseFormatSpecifier(formatSpan, out int digits);
            if (fmt is 'x' or 'X')
            {
                return FormatBigIntegerToHex(targetSpan, value, fmt, digits, info, destination, out charsWritten, out spanSuccess);
            }

            if (fmt is 'b' or 'B')
            {
                return FormatBigIntegerToBinary(targetSpan, value, digits, destination, out charsWritten, out spanSuccess);
            }

            if (value._bits == null)
            {
                if (fmt is 'g' or 'G' or 'r' or 'R')
                {
                    formatSpan = formatString = digits > 0 ? $"D{digits}" : "D";
                }

                if (targetSpan)
                {
                    if (typeof(TChar) == typeof(Utf8Char))
                    {
                        spanSuccess = value._sign.TryFormat(Unsafe.BitCast<Span<TChar>, Span<byte>>(destination), out charsWritten, formatSpan, info);
                    }
                    else
                    {
                        Debug.Assert(typeof(TChar) == typeof(Utf16Char));
                        spanSuccess = value._sign.TryFormat(Unsafe.BitCast<Span<TChar>, Span<char>>(destination), out charsWritten, formatSpan, info);
                    }

                    return null;
                }
                else
                {
                    Debug.Assert(formatString != null);
                    charsWritten = 0;
                    spanSuccess = false;
                    return value._sign.ToString(formatString, info);
                }
            }

            // The Ratio is calculated as: log_{10^9}(2^BitsPerLimb)
            // value._bits.Length represents the number of digits when considering value
            // in base 2^BitsPerLimb. This means it satisfies the inequality:
            // value._bits.Length - 1 <= log_{2^BitsPerLimb}(value) < value._bits.Length
            //
            // When converting value to a decimal string, it is first converted to
            // base 1,000,000,000.
            //
            // Dividing the equation by log_{2^BitsPerLimb}(10^9), which is equivalent to
            // multiplying by log_{10^9}(2^BitsPerLimb), and using the base change formula,
            // we get:
            // M - log_{10^9}(2^BitsPerLimb) <= log_{10^9}(value) < M <= Ceiling(M)
            // where M is log_{10^9}(2^BitsPerLimb)*value._bits.Length.
            // In other words, the number of digits of value in base 1,000,000,000 is at most Ceiling(M).
            double digitRatio = 1.070328873472 * BigIntegerCalculator.BitsPerLimb / 32.0;
            Debug.Assert(BigInteger.MaxLength * digitRatio + 1 < Array.MaxLength); // won't overflow

            int base1E9BufferLength = (int)(value._bits.Length * digitRatio) + 1;
            Span<nuint> base1E9Buffer = BigInteger.RentedBuffer.Create(base1E9BufferLength, out BigInteger.RentedBuffer base1E9Rental);


            BigIntegerToBase1E9(value._bits, base1E9Buffer, out int written);
            ReadOnlySpan<nuint> base1E9Value = base1E9Buffer[..written];

            int valueDigits = (base1E9Value.Length - 1) * PowersOf1e9.MaxPartialDigits + FormattingHelpers.CountDigits(base1E9Value[^1]);

            string? strResult;

            if (fmt is 'g' or 'G' or 'd' or 'D' or 'r' or 'R')
            {
                int strDigits = Math.Max(digits, valueDigits);
                ReadOnlySpan<TChar> sNegative = value.Sign < 0 ? info.NegativeSignTChar<TChar>() : default;
                int strLength = strDigits + sNegative.Length;

                if (targetSpan)
                {
                    if (destination.Length < strLength)
                    {
                        spanSuccess = false;
                        charsWritten = 0;
                    }
                    else
                    {
                        sNegative.CopyTo(destination);
                        fixed (TChar* ptr = &MemoryMarshal.GetReference(destination))
                        {
                            BigIntegerToDecChars(ptr + strLength, base1E9Value, digits);
                        }

                        charsWritten = strLength;
                        spanSuccess = true;
                    }

                    strResult = null;
                }
                else
                {
                    Debug.Assert(typeof(TChar) == typeof(Utf16Char));

                    spanSuccess = false;
                    charsWritten = 0;

                    fixed (nuint* ptr = base1E9Value)
                    {
                        var state = new InterpolatedStringHandlerState
                        {
                            digits = digits,
                            base1E9Value = base1E9Value,
                            sNegative = Unsafe.BitCast<ReadOnlySpan<TChar>, ReadOnlySpan<char>>(sNegative),
                        };

                        strResult = string.Create(strLength, state, static (span, state) =>
                        {
                            state.sNegative.CopyTo(span);
                            fixed (char* ptr = &MemoryMarshal.GetReference(span))
                            {
                                BigIntegerToDecChars((Utf16Char*)ptr + span.Length, state.base1E9Value, state.digits);
                            }
                        });
                    }
                }
            }
            else
            {
                byte[]? numberBufferToReturn = null;
                Span<byte> numberBuffer = valueDigits + 1 <= CharStackBufferSize ?
                    stackalloc byte[valueDigits + 1] :
                    (numberBufferToReturn = ArrayPool<byte>.Shared.Rent(valueDigits + 1));
                fixed (byte* ptr = numberBuffer) // NumberBuffer expects pinned Digits
                {
                    scoped NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, ptr, valueDigits + 1);
                    BigIntegerToDecChars((Utf8Char*)ptr + valueDigits, base1E9Value, valueDigits);
                    number.Digits[^1] = 0;
                    number.DigitsCount = valueDigits;
                    number.Scale = valueDigits;
                    number.IsNegative = value.Sign < 0;

                    scoped var vlb = new ValueListBuilder<TChar>(stackalloc TChar[CharStackBufferSize]); // arbitrary stack cut-off

                    if (fmt != 0)
                    {
                        NumberToString(ref vlb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        NumberToStringFormat(ref vlb, ref number, formatSpan, info);
                    }

                    if (targetSpan)
                    {
                        spanSuccess = vlb.TryCopyTo(destination, out charsWritten);
                        strResult = null;
                    }
                    else
                    {
                        charsWritten = 0;
                        spanSuccess = false;

                        if (typeof(TChar) == typeof(Utf8Char))
                        {
                            strResult = Encoding.UTF8.GetString(Unsafe.BitCast<ReadOnlySpan<TChar>, ReadOnlySpan<byte>>(vlb.AsSpan()));
                        }
                        else
                        {
                            Debug.Assert(typeof(TChar) == typeof(Utf16Char));
                            strResult = Unsafe.BitCast<ReadOnlySpan<TChar>, ReadOnlySpan<char>>(vlb.AsSpan()).ToString();
                        }
                    }

                    vlb.Dispose();
                    if (numberBufferToReturn != null)
                    {
                        ArrayPool<byte>.Shared.Return(numberBufferToReturn);
                    }
                }
            }

            base1E9Rental.Dispose();

            return strResult;
        }

        private unsafe ref struct InterpolatedStringHandlerState
        {
            public int digits;
            public ReadOnlySpan<nuint> base1E9Value;
            public ReadOnlySpan<char> sNegative;
        }

        private static unsafe TChar* BigIntegerToDecChars<TChar>(TChar* bufferEnd, ReadOnlySpan<nuint> base1E9Value, int digits)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(base1E9Value[^1] != 0, "Leading zeros should be trimmed by caller.");

            // The base 10^9 value is in reverse order
            for (int i = 0; i < base1E9Value.Length - 1; i++)
            {
                bufferEnd = UInt32ToDecChars(bufferEnd, (uint)base1E9Value[i], PowersOf1e9.MaxPartialDigits);
                digits -= PowersOf1e9.MaxPartialDigits;
            }

            return UInt32ToDecChars(bufferEnd, (uint)base1E9Value[^1], digits);
        }

        public
#if DEBUG
        static // Mutable for unit testing...
#else
        const
#endif
            int ToStringNaiveThreshold = BigIntegerCalculator.DivideBurnikelZieglerThreshold;
        private static void BigIntegerToBase1E9(ReadOnlySpan<nuint> bits, Span<nuint> base1E9Buffer, out int base1E9Written)
        {
            Debug.Assert(ToStringNaiveThreshold >= 2);

            if (bits.Length <= ToStringNaiveThreshold)
            {
                Naive(bits, base1E9Buffer, out base1E9Written);
                return;
            }

            PowersOf1e9.FloorBufferSize(bits.Length, out int powersOf1e9BufferLength, out int maxIndex);
            PowersOf1e9 powersOf1e9 = PowersOf1e9.GetCached(powersOf1e9BufferLength);

            DivideAndConquer(powersOf1e9, maxIndex, bits, base1E9Buffer, out base1E9Written);

            static void DivideAndConquer(in PowersOf1e9 powersOf1e9, int powersIndex, ReadOnlySpan<nuint> bits, Span<nuint> base1E9Buffer, out int base1E9Written)
            {
                Debug.Assert(bits.Length == 0 || bits[^1] != 0);
                Debug.Assert(powersIndex >= 0);

                if (bits.Length <= ToStringNaiveThreshold)
                {
                    Naive(bits, base1E9Buffer, out base1E9Written);
                    return;
                }

                ReadOnlySpan<nuint> powOfTen = powersOf1e9.GetSpan(powersIndex);
                int omittedLength = PowersOf1e9.OmittedLength(powersIndex);

                while (bits.Length < powOfTen.Length + omittedLength || BigIntegerCalculator.Compare(bits.Slice(omittedLength), powOfTen) < 0)
                {
                    --powersIndex;
                    powOfTen = powersOf1e9.GetSpan(powersIndex);
                    omittedLength = PowersOf1e9.OmittedLength(powersIndex);
                }

                int upperLength = bits.Length - powOfTen.Length - omittedLength + 1;
                Span<nuint> upper = BigInteger.RentedBuffer.Create(upperLength, out BigInteger.RentedBuffer upperBuffer);

                int lowerLength = bits.Length;
                Span<nuint> lower = BigInteger.RentedBuffer.Create(lowerLength, out BigInteger.RentedBuffer lowerBuffer);

                bits.Slice(0, omittedLength).CopyTo(lower);
                BigIntegerCalculator.Divide(bits.Slice(omittedLength), powOfTen, upper, lower.Slice(omittedLength));

                Debug.Assert(!upper.Trim((nuint)0).IsEmpty);

                int lower1E9Length = 1 << powersIndex;

                DivideAndConquer(
                    powersOf1e9,
                    powersIndex - 1,
                    lower.Slice(0, BigIntegerCalculator.ActualLength(lower)),
                    base1E9Buffer,
                    out int lowerWritten);

                lowerBuffer.Dispose();

                Debug.Assert(lower1E9Length >= lowerWritten);

                DivideAndConquer(
                    powersOf1e9,
                    powersIndex - 1,
                    upper.Slice(0, BigIntegerCalculator.ActualLength(upper)),
                    base1E9Buffer.Slice(lower1E9Length),
                    out base1E9Written);

                upperBuffer.Dispose();

                base1E9Written += lower1E9Length;
            }

            static void Naive(ReadOnlySpan<nuint> bits, Span<nuint> base1E9Buffer, out int base1E9Written)
            {
                base1E9Written = 0;

                for (int iuSrc = bits.Length; --iuSrc >= 0;)
                {
                    if (nint.Size == 8)
                    {
                        // Process each 64-bit limb as two 32-bit halves (high then low).
                        // This keeps each division as ulong / constant_uint which the JIT
                        // optimizes to a fast multiply-by-reciprocal, avoiding expensive
                        // 128÷64 software division through BigIntegerCalculator.DivRem.
                        // Net effect: (base * 2^32 + hi) * 2^32 + lo = base * 2^64 + limb.
                        ulong limb = bits[iuSrc];
                        NaiveDigit((uint)(limb >> 32), base1E9Buffer, ref base1E9Written);
                        NaiveDigit((uint)limb, base1E9Buffer, ref base1E9Written);
                    }
                    else
                    {
                        NaiveDigit((uint)bits[iuSrc], base1E9Buffer, ref base1E9Written);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void NaiveDigit(uint digit, Span<nuint> base1E9Buffer, ref int base1E9Written)
            {
                const uint Divisor = (uint)PowersOf1e9.TenPowMaxPartial;

                uint uCarry = digit;
                for (int iuDst = 0; iuDst < base1E9Written; iuDst++)
                {
                    ulong value = ((ulong)(uint)base1E9Buffer[iuDst] << 32) | uCarry;
                    ulong quo = value / Divisor;
                    base1E9Buffer[iuDst] = (uint)(value - quo * Divisor);
                    uCarry = (uint)quo;
                }

                while (uCarry != 0)
                {
                    base1E9Buffer[base1E9Written++] = uCarry % Divisor;
                    uCarry /= Divisor;
                }
            }
        }

        internal readonly ref struct PowersOf1e9
        {
            /// <summary>Holds 1000000000^(1&lt;&lt;&lt;n).</summary>
            private readonly ReadOnlySpan<nuint> pow1E9;
            public const nuint TenPowMaxPartial = 1000000000;
            public const int MaxPartialDigits = 9;

            private PowersOf1e9(nuint[] pow1E9)
            {
                this.pow1E9 = pow1E9;
            }

            public static PowersOf1e9 GetCached(int bufferLength)
            {
                nuint[]? cached = s_cachedPowersOf1e9;
                if (cached is not null && cached.Length >= bufferLength)
                {
                    return new PowersOf1e9(cached);
                }

                nuint[] buffer = new nuint[bufferLength];
                PowersOf1e9 result = new((Span<nuint>)buffer);

                // Only cache buffers large enough to contain computed powers.
                // Small buffers (≤ LeadingPowers1E9.Length) aren't populated by
                // the constructor — it uses the static LeadingPowers1E9 directly.
                if (buffer.Length > LeadingPowers1E9.Length &&
                    (cached is null || buffer.Length > cached.Length))
                {
                    // The write is safe without explicit memory barriers because:
                    // 1. The array is fully initialized before being stored.
                    // 2. On ARM64, the .NET GC write barrier uses stlr (store-release),
                    //    providing release semantics for reference-type stores.
                    // 3. Readers have a data dependency (load reference -> access elements),
                    //    providing natural acquire ordering on all architectures.
                    s_cachedPowersOf1e9 = buffer;
                }

                return result;
            }

            /// <summary>
            /// Pre-calculated cumulative lengths into <see cref="pow1E9"/>.
            /// <c>pow1E9[Indexes[i-1]..Indexes[i]]</c> equals <c>1000000000^(1&lt;&lt;i)</c>.
            /// </summary>
            private static ReadOnlySpan<int> Indexes => nint.Size == 8 ? Indexes64 : Indexes32;

            private static ReadOnlySpan<int> Indexes32 =>
            [
                0,
                1,
                3,
                6,
                12,
                23,
                44,
                86,
                170,
                338,
                673,
                1342,
                2680,
                5355,
                10705,
                21405,
                42804,
                85602,
                171198,
                342390,
                684773,
                1369538,
                2739067,
                5478125,
                10956241,
                21912473,
                43824936,
                87649862,
                175299713,
                484817143,
                969634274,
                1939268536,
            ];

            private static ReadOnlySpan<int> Indexes64 =>
            [
                0,
                1,
                2,
                4,
                7,
                13,
                24,
                45,
                87,
                171,
                339,
                674,
                1343,
                2681,
                5356,
                10706,
                21406,
                42805,
                85603,
                171199,
                342391,
                684774,
                1369539,
                2739068,
                5478126,
                10956242,
                21912474,
                43824937,
                87649863,
                175299714,
                350599416,
                701198819,
            ];

            /// <summary>
            /// Pre-computed leading powers of 10^9 for small exponents. Entries up to
            /// <c>1000000000^(1&lt;&lt;5)</c> are stored directly because their low limb is never zero.
            /// </summary>
            private static ReadOnlySpan<nuint> LeadingPowers1E9 => nint.Size == 8
                ? MemoryMarshal.Cast<ulong, nuint>(LeadingPowers1E9_64)
                : MemoryMarshal.Cast<uint, nuint>(LeadingPowers1E9_32);

            private static ReadOnlySpan<uint> LeadingPowers1E9_32 =>
            [
                // 1000000000^(1<<0)
                1000000000,
                // 1000000000^(1<<1)
                2808348672,
                232830643,
                // 1000000000^(1<<2)
                3008077584,
                2076772117,
                12621774,
                // 1000000000^(1<<3)
                4130660608,
                835571558,
                1441351422,
                977976457,
                264170013,
                37092,
                // 1000000000^(1<<4)
                767623168,
                4241160024,
                1260959332,
                2541775228,
                2965753944,
                1796720685,
                484800439,
                1311835347,
                2945126454,
                3563705203,
                1375821026,
                // 1000000000^(1<<5)
                3940379521,
                184513341,
                2872588323,
                2214530454,
                38258512,
                2980860351,
                114267010,
                2188874685,
                234079247,
                2101059099,
                1948702207,
                947446250,
                864457656,
                507589568,
                1321007357,
                3911984176,
                1011110295,
                2382358050,
                2389730781,
                730678769,
                440721283,
            ];

            private static ReadOnlySpan<ulong> LeadingPowers1E9_64 =>
            [
                // 1000000000^(1<<0) = 10^9
                1000000000,
                // 1000000000^(1<<1) = 10^18
                1000000000000000000,
                // 1000000000^(1<<2) = 10^36
                12919594847110692864,
                54210108624275221,
                // 1000000000^(1<<3) = 10^72
                3588752519208427776,
                4200376900514301694,
                159309191113245,
                // 1000000000^(1<<4) = 10^144
                18215643600950198272,
                10916841479303902820,
                7716856585087471704,
                5634289913586612151,
                15305997302415167542,
                1375821026,
                // 1000000000^(1<<5) = 10^288
                16923801176523145216,
                12337672902340997949,
                164319060048154006,
                490773073942565311,
                1005362712726180797,
                8369612250809081371,
                3712817362244264426,
                5673683396597986240,
                4342685653585896496,
                10263815553021896226,
                1892883497866839537,
            ];

            public PowersOf1e9(Span<nuint> pow1E9)
            {
                Debug.Assert(pow1E9.Length >= 1);
                Debug.Assert(Indexes[6] == LeadingPowers1E9.Length);
                if (pow1E9.Length <= LeadingPowers1E9.Length)
                {
                    this.pow1E9 = LeadingPowers1E9;
                    return;
                }

                LeadingPowers1E9.CopyTo(pow1E9.Slice(0, LeadingPowers1E9.Length));
                this.pow1E9 = pow1E9;

                ReadOnlySpan<nuint> src = pow1E9.Slice(Indexes[5], Indexes[6] - Indexes[5]);
                int toExclusive = Indexes[6];
                for (int i = 6; i + 1 < Indexes.Length; i++)
                {
                    Debug.Assert(2 * src.Length - (Indexes[i + 1] - Indexes[i]) is 0 or 1);
                    if (pow1E9.Length - toExclusive < (src.Length << 1))
                    {
                        break;
                    }

                    Span<nuint> dst = pow1E9.Slice(toExclusive, src.Length << 1);
                    BigIntegerCalculator.Square(src, dst);

                    // When 9*(1<<(i-1)) is not evenly divisible by BitsPerLimb, the stored
                    // power at index i-1 carries a residual factor of 2^r. Squaring doubles
                    // that residual; if 2r >= BitsPerLimb the result has extra trailing zero
                    // limbs that must be stripped to yield the correct stored representation.
                    int shift = OmittedLength(i) - 2 * OmittedLength(i - 1);
                    if (shift > 0)
                    {
                        dst.Slice(shift).CopyTo(dst);
                        dst.Slice(dst.Length - shift).Clear();
                    }

                    int from = toExclusive;
                    toExclusive = Indexes[i + 1];
                    src = pow1E9.Slice(from, toExclusive - from);
                    Debug.Assert(toExclusive == pow1E9.Length || pow1E9[toExclusive] == 0);
                }
            }

            public static int GetBufferSize(int digits, out int maxIndex)
            {
                uint scale1E9 = (uint)(digits - 1) / MaxPartialDigits;
                maxIndex = BitOperations.Log2(scale1E9);
                int index = maxIndex + 1;
                int bufferSize;
                if ((uint)index < (uint)Indexes.Length)
                {
                    bufferSize = Indexes[index];
                }
                else
                {
                    maxIndex = Indexes.Length - 2;
                    bufferSize = Indexes[^1];
                }

                return ++bufferSize;
            }

            public ReadOnlySpan<nuint> GetSpan(int index)
            {
                // Returns 1E9^(1<<index) >> (BitsPerLimb*(9*(1<<index)/BitsPerLimb))
                int from = Indexes[index];
                int toExclusive = Indexes[index + 1];
                return pow1E9.Slice(from, toExclusive - from);
            }

            public static int OmittedLength(int index)
            {
                // Returns 9*(1<<index)/BitsPerLimb
                return (MaxPartialDigits * (1 << index)) / BigIntegerCalculator.BitsPerLimb;
            }

            public static void FloorBufferSize(int size, out int bufferSize, out int maxIndex)
            {
                Debug.Assert(size > 0);

                // binary search
                // size < Indexes[hi+1] - Indexes[hi]
                // size >= Indexes[lo+1] - Indexes[lo]
                int hi = Indexes.Length - 1;
                maxIndex = 0;
                while (maxIndex + 1 < hi)
                {
                    int i = (hi + maxIndex) >> 1;
                    if (size < Indexes[i + 1] - Indexes[i])
                    {
                        hi = i;
                    }
                    else
                    {
                        maxIndex = i;
                    }
                }

                bufferSize = Indexes[maxIndex + 1] + 1;
            }

            public void MultiplyPowerOfTen(ReadOnlySpan<nuint> left, int trailingZeroCount, Span<nuint> bits)
            {
                Debug.Assert(trailingZeroCount >= 0);
                if (trailingZeroCount < UInt32PowersOfTen.Length)
                {
                    BigIntegerCalculator.Multiply(left, UInt32PowersOfTen[trailingZeroCount], bits.Slice(0, left.Length + 1));
                    return;
                }

                Span<nuint> powersOfTen = BigInteger.RentedBuffer.Create(bits.Length, out BigInteger.RentedBuffer powersOfTenBuffer);
                scoped Span<nuint> powersOfTen2 = bits;

                int trailingPartialCount = Math.DivRem(trailingZeroCount, MaxPartialDigits, out int remainingTrailingZeroCount);

                int fi = BitOperations.TrailingZeroCount(trailingPartialCount);
                int omittedLength = OmittedLength(fi);

                // Copy first
                ReadOnlySpan<nuint> first = GetSpan(fi);
                int curLength = first.Length;
                trailingPartialCount >>= fi;
                trailingPartialCount >>= 1;

                if ((BitOperations.PopCount((uint)trailingPartialCount) & 1) != 0)
                {
                    powersOfTen2 = powersOfTen;
                    powersOfTen = bits;
                    powersOfTen2.Clear();
                }

                first.CopyTo(powersOfTen);

                for (++fi; trailingPartialCount != 0; ++fi, trailingPartialCount >>= 1)
                {
                    Debug.Assert(fi + 1 < Indexes.Length);
                    if ((trailingPartialCount & 1) != 0)
                    {
                        omittedLength += OmittedLength(fi);

                        ReadOnlySpan<nuint> power = GetSpan(fi);
                        Span<nuint> src = powersOfTen.Slice(0, curLength);
                        Span<nuint> dst = powersOfTen2.Slice(0, curLength += power.Length);

                        BigIntegerCalculator.Multiply(src, power, dst);

                        Span<nuint> tmp = powersOfTen;
                        powersOfTen = powersOfTen2;
                        powersOfTen2 = tmp;
                        powersOfTen2.Clear();

                        // Trim
                        while (--curLength >= 0 && powersOfTen[curLength] == 0) ;
                        ++curLength;
                    }
                }

                Debug.Assert(Unsafe.AreSame(ref bits[0], ref powersOfTen2[0]));

                powersOfTen = powersOfTen.Slice(0, curLength);
                Span<nuint> bits2 = bits.Slice(omittedLength, curLength += left.Length);

                BigIntegerCalculator.Multiply(left, powersOfTen, bits2);

                powersOfTenBuffer.Dispose();

                if (remainingTrailingZeroCount > 0)
                {
                    nuint multiplier = UInt32PowersOfTen[remainingTrailingZeroCount];
                    nuint carry = 0;
                    if (nint.Size == 8)
                    {
                        for (int i = 0; i < bits2.Length; i++)
                        {
                            UInt128 p = (UInt128)multiplier * bits2[i] + carry;
                            bits2[i] = (nuint)(ulong)p;
                            carry = (nuint)(ulong)(p >> 64);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < bits2.Length; i++)
                        {
                            ulong p = (ulong)multiplier * bits2[i] + carry;
                            bits2[i] = (uint)p;
                            carry = (uint)(p >> 32);
                        }
                    }

                    if (carry != 0)
                    {
                        bits[omittedLength + curLength] = carry;
                    }
                }
            }
        }
    }

    internal interface IBigIntegerHexOrBinaryParser<TParser, TChar>
        where TParser : struct, IBigIntegerHexOrBinaryParser<TParser, TChar>
        where TChar : unmanaged, IUtfChar<TChar>
    {
        static abstract int BitsPerDigit { get; }

        static virtual int DigitsPerBlock => nint.Size * 8 / TParser.BitsPerDigit;

        static abstract NumberStyles BlockNumberStyle { get; }

        static abstract nuint GetSignBitsIfValid(uint ch);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static virtual bool TryParseUnalignedBlock(ReadOnlySpan<TChar> input, out nuint result)
        {
            if (typeof(TChar) == typeof(Utf8Char))
            {
                return nuint.TryParse(Unsafe.BitCast<ReadOnlySpan<TChar>, ReadOnlySpan<byte>>(input), TParser.BlockNumberStyle, null, out result);
            }
            else
            {
                Debug.Assert(typeof(TChar) == typeof(Utf16Char));
                return nuint.TryParse(Unsafe.BitCast<ReadOnlySpan<TChar>, ReadOnlySpan<char>>(input), TParser.BlockNumberStyle, null, out result);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static virtual bool TryParseSingleBlock(ReadOnlySpan<TChar> input, out nuint result)
            => TParser.TryParseUnalignedBlock(input, out result);

        static virtual bool TryParseWholeBlocks(ReadOnlySpan<TChar> input, Span<nuint> destination)
        {
            Debug.Assert(destination.Length * TParser.DigitsPerBlock == input.Length);

            for (int i = 0; i < destination.Length; i++)
            {
                int blockStart = input.Length - (i + 1) * TParser.DigitsPerBlock;
                if (!TParser.TryParseSingleBlock(input.Slice(blockStart, TParser.DigitsPerBlock), out destination[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }

    internal readonly struct BigIntegerHexParser<TChar> : IBigIntegerHexOrBinaryParser<BigIntegerHexParser<TChar>, TChar>
        where TChar : unmanaged, IUtfChar<TChar>
    {
        public static int BitsPerDigit => 4;

        public static NumberStyles BlockNumberStyle => NumberStyles.AllowHexSpecifier;

        /// <summary>Returns all-zero bits if <paramref name="ch"/> is a valid hex digit and considered positive ('0'-'7'), or all-one bits otherwise.</summary>
        public static nuint GetSignBitsIfValid(uint ch) => (nuint)(nint)((ch & 0b_1111_1000) == 0b_0011_0000 ? 0 : -1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseWholeBlocks(ReadOnlySpan<TChar> input, Span<nuint> destination)
        {
            if ((typeof(TChar) == typeof(Utf8Char))
                ? (Convert.FromHexString(Unsafe.BitCast<ReadOnlySpan<TChar>, ReadOnlySpan<byte>>(input), MemoryMarshal.AsBytes(destination), out _, out _) != OperationStatus.Done)
                : (Convert.FromHexString(Unsafe.BitCast<ReadOnlySpan<TChar>, ReadOnlySpan<char>>(input), MemoryMarshal.AsBytes(destination), out _, out _) != OperationStatus.Done))
            {
                return false;
            }

            if (BitConverter.IsLittleEndian)
            {
                MemoryMarshal.AsBytes(destination).Reverse();
            }
            else
            {
                destination.Reverse();
            }

            return true;
        }
    }

    internal readonly struct BigIntegerBinaryParser<TChar> : IBigIntegerHexOrBinaryParser<BigIntegerBinaryParser<TChar>, TChar>
        where TChar : unmanaged, IUtfChar<TChar>
    {
        public static int BitsPerDigit => 1;

        public static NumberStyles BlockNumberStyle => NumberStyles.AllowBinarySpecifier;

        /// <summary>Returns all-zero bits if <paramref name="ch"/> is '0', or all-one bits if '1' (using LSB sign extension).</summary>
        public static nuint GetSignBitsIfValid(uint ch) => (nuint)(nint)(((int)ch << 31) >> 31);
    }
}
