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

        private static ReadOnlySpan<uint> UInt32PowersOfTen => [1, 10, 100, 1000, 10000, 100000, 1000000, 10000000, 100000000, 1000000000];

        [DoesNotReturn]
        internal static void ThrowOverflowOrFormatException(ParsingStatus status) => throw GetException(status);

        private static Exception GetException(ParsingStatus status)
        {
            return status == ParsingStatus.Failed
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

        internal static unsafe ParsingStatus TryParseBigInteger(ReadOnlySpan<char> value, NumberStyles style, NumberFormatInfo info, out BigInteger result)
        {
            if (!TryValidateParseStyleInteger(style, out ArgumentException? e))
            {
                throw e; // TryParse still throws ArgumentException on invalid NumberStyles
            }

            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            {
                return TryParseBigIntegerHexOrBinaryNumberStyle<BigIntegerHexParser<char>, char>(value, style, out result);
            }

            if ((style & NumberStyles.AllowBinarySpecifier) != 0)
            {
                return TryParseBigIntegerHexOrBinaryNumberStyle<BigIntegerBinaryParser<char>, char>(value, style, out result);
            }

            return TryParseBigIntegerNumber(value, style, info, out result);
        }

        internal static unsafe ParsingStatus TryParseBigIntegerNumber(ReadOnlySpan<char> value, NumberStyles style, NumberFormatInfo info, out BigInteger result)
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
                NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, buffer);

                if (!TryStringToNumber(MemoryMarshal.Cast<char, Utf16Char>(value), style, ref number, info))
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

        internal static BigInteger ParseBigInteger(ReadOnlySpan<char> value, NumberStyles style, NumberFormatInfo info)
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
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            int whiteIndex;

            // Skip past any whitespace at the beginning.
            if ((style & NumberStyles.AllowLeadingWhite) != 0)
            {
                for (whiteIndex = 0; whiteIndex < value.Length; whiteIndex++)
                {
                    if (!IsWhite(uint.CreateTruncating(value[whiteIndex])))
                        break;
                }

                value = value[whiteIndex..];
            }

            // Skip past any whitespace at the end.
            if ((style & NumberStyles.AllowTrailingWhite) != 0)
            {
                for (whiteIndex = value.Length - 1; whiteIndex >= 0; whiteIndex--)
                {
                    if (!IsWhite(uint.CreateTruncating(value[whiteIndex])))
                        break;
                }

                value = value[..(whiteIndex + 1)];
            }

            if (value.IsEmpty)
            {
                goto FailExit;
            }

            // Remember the sign from original leading input
            // Invalid digits will be caught in parsing below
            uint signBits = TParser.GetSignBitsIfValid(uint.CreateTruncating(value[0]));

            // Start from leading blocks. Leading blocks can be unaligned, or whole of 0/F's that need to be trimmed.
            int leadingBitsCount = value.Length % TParser.DigitsPerBlock;

            uint leading = signBits;
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
                if ((int)(leading ^ signBits) >= 0)
                {
                    // Small value that fits in Int32.
                    // Delegate to the constructor for int.MinValue handling.
                    result = new BigInteger((int)leading);
                    return ParsingStatus.OK;
                }
                else if (leading != 0)
                {
                    // The sign of result differs with leading digit.
                    // Require to store in _bits.

                    // Positive: sign=1, bits=[leading]
                    // Negative: sign=-1, bits=[(leading ^ -1) + 1]=[-leading]
                    result = new BigInteger((int)signBits | 1, [(leading ^ signBits) - signBits]);
                    return ParsingStatus.OK;
                }
                else
                {
                    // -1 << 32, which requires an additional uint
                    result = new BigInteger(-1, [0, 1]);
                    return ParsingStatus.OK;
                }
            }

            // Now the size of bits array can be calculated, except edge cases of -2^32N
            int wholeBlockCount = value.Length / TParser.DigitsPerBlock;
            int totalUIntCount = wholeBlockCount + 1;

            // Early out for too large input
            if (totalUIntCount > BigInteger.MaxLength)
            {
                result = default;
                return ParsingStatus.Overflow;
            }

            uint[] bits = new uint[totalUIntCount];
            Span<uint> wholeBlockDestination = bits.AsSpan(0, wholeBlockCount);

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
                    bits = new uint[bits.Length + 1];
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
#if DEBUG
        // Mutable for unit testing...
        internal static
#else
        internal const
#endif
        int s_naiveThreshold = 1233;
        private static ParsingStatus NumberToBigInteger(ref NumberBuffer number, out BigInteger result)
        {
            int currentBufferSize = 0;

            int totalDigitCount = 0;
            int numberScale = number.Scale;

            const int MaxPartialDigits = 9;
            const uint TenPowMaxPartial = 1000000000;

            int[]? arrayFromPoolForResultBuffer = null;

            if (numberScale == int.MaxValue)
            {
                result = default;
                return ParsingStatus.Overflow;
            }

            if (numberScale < 0)
            {
                result = default;
                return ParsingStatus.Failed;
            }

            try
            {
                if (number.DigitsCount <= s_naiveThreshold)
                {
                    return Naive(ref number, out result);
                }
                else
                {
                    return DivideAndConquer(ref number, out result);
                }
            }
            finally
            {
                if (arrayFromPoolForResultBuffer != null)
                {
                    ArrayPool<int>.Shared.Return(arrayFromPoolForResultBuffer);
                }
            }

            ParsingStatus Naive(ref NumberBuffer number, out BigInteger result)
            {
                Span<uint> stackBuffer = stackalloc uint[BigIntegerCalculator.StackAllocThreshold];
                Span<uint> currentBuffer = stackBuffer;
                uint partialValue = 0;
                int partialDigitCount = 0;

                if (!ProcessChunk(number.Digits[..number.DigitsCount], ref currentBuffer))
                {
                    result = default;
                    return ParsingStatus.Failed;
                }

                if (partialDigitCount > 0)
                {
                    MultiplyAdd(ref currentBuffer, UInt32PowersOfTen[partialDigitCount], partialValue);
                }

                result = NumberBufferToBigInteger(currentBuffer, number.IsNegative);
                return ParsingStatus.OK;

                bool ProcessChunk(ReadOnlySpan<byte> chunkDigits, ref Span<uint> currentBuffer)
                {
                    int remainingIntDigitCount = Math.Max(numberScale - totalDigitCount, 0);
                    ReadOnlySpan<byte> intDigitsSpan = chunkDigits.Slice(0, Math.Min(remainingIntDigitCount, chunkDigits.Length));

                    bool endReached = false;

                    // Storing these captured variables in locals for faster access in the loop.
                    uint _partialValue = partialValue;
                    int _partialDigitCount = partialDigitCount;
                    int _totalDigitCount = totalDigitCount;

                    for (int i = 0; i < intDigitsSpan.Length; i++)
                    {
                        char digitChar = (char)chunkDigits[i];
                        if (digitChar == '\0')
                        {
                            endReached = true;
                            break;
                        }

                        _partialValue = _partialValue * 10 + (uint)(digitChar - '0');
                        _partialDigitCount++;
                        _totalDigitCount++;

                        // Update the buffer when enough partial digits have been accumulated.
                        if (_partialDigitCount == MaxPartialDigits)
                        {
                            MultiplyAdd(ref currentBuffer, TenPowMaxPartial, _partialValue);
                            _partialValue = 0;
                            _partialDigitCount = 0;
                        }
                    }

                    // Check for nonzero digits after the decimal point.
                    if (!endReached)
                    {
                        ReadOnlySpan<byte> fracDigitsSpan = chunkDigits.Slice(intDigitsSpan.Length);
                        for (int i = 0; i < fracDigitsSpan.Length; i++)
                        {
                            char digitChar = (char)fracDigitsSpan[i];
                            if (digitChar == '\0')
                            {
                                break;
                            }
                            if (digitChar != '0')
                            {
                                return false;
                            }
                        }
                    }

                    partialValue = _partialValue;
                    partialDigitCount = _partialDigitCount;
                    totalDigitCount = _totalDigitCount;

                    return true;
                }
            }

            ParsingStatus DivideAndConquer(ref NumberBuffer number, out BigInteger result)
            {
                Span<uint> currentBuffer;
                int[]? arrayFromPoolForMultiplier = null;
                try
                {
                    totalDigitCount = Math.Min(number.DigitsCount, numberScale);
                    int bufferSize = (totalDigitCount + MaxPartialDigits - 1) / MaxPartialDigits;

                    Span<uint> buffer = new uint[bufferSize];
                    arrayFromPoolForResultBuffer = ArrayPool<int>.Shared.Rent(bufferSize);
                    Span<uint> newBuffer = MemoryMarshal.Cast<int, uint>(arrayFromPoolForResultBuffer).Slice(0, bufferSize);
                    newBuffer.Clear();

                    // Separate every MaxPartialDigits digits and store them in the buffer.
                    // Buffers are treated as little-endian. That means, the array { 234567890, 1 }
                    // represents the number 1234567890.
                    int bufferIndex = bufferSize - 1;
                    uint currentBlock = 0;
                    int shiftUntil = (totalDigitCount - 1) % MaxPartialDigits;
                    int remainingIntDigitCount = totalDigitCount;

                    ReadOnlySpan<byte> digitsChunkSpan = number.Digits[..number.DigitsCount];
                    ReadOnlySpan<byte> intDigitsSpan = digitsChunkSpan.Slice(0, Math.Min(remainingIntDigitCount, digitsChunkSpan.Length));

                    for (int i = 0; i < intDigitsSpan.Length; i++)
                    {
                        char digitChar = (char)intDigitsSpan[i];
                        Debug.Assert(char.IsDigit(digitChar));
                        currentBlock *= 10;
                        currentBlock += unchecked((uint)(digitChar - '0'));
                        if (shiftUntil == 0)
                        {
                            buffer[bufferIndex] = currentBlock;
                            currentBlock = 0;
                            bufferIndex--;
                            shiftUntil = MaxPartialDigits;
                        }
                        shiftUntil--;
                    }
                    remainingIntDigitCount -= intDigitsSpan.Length;
                    Debug.Assert(0 <= remainingIntDigitCount);

                    ReadOnlySpan<byte> fracDigitsSpan = digitsChunkSpan.Slice(intDigitsSpan.Length);
                    for (int i = 0; i < fracDigitsSpan.Length; i++)
                    {
                        char digitChar = (char)fracDigitsSpan[i];
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

                    Debug.Assert(currentBlock == 0);
                    Debug.Assert(bufferIndex == -1);

                    int blockSize = 1;
                    arrayFromPoolForMultiplier = ArrayPool<int>.Shared.Rent(blockSize);
                    Span<uint> multiplier = MemoryMarshal.Cast<int, uint>(arrayFromPoolForMultiplier).Slice(0, blockSize);
                    multiplier[0] = TenPowMaxPartial;

                    // This loop is executed ceil(log_2(bufferSize)) times.
                    while (true)
                    {
                        // merge each block pairs.
                        // When buffer represents:
                        // |     A     |     B     |     C     |     D     |
                        // Make newBuffer like:
                        // |  A + B * multiplier   |  C + D * multiplier   |
                        for (int i = 0; i < bufferSize; i += blockSize * 2)
                        {
                            Span<uint> curBuffer = buffer.Slice(i);
                            Span<uint> curNewBuffer = newBuffer.Slice(i);

                            int len = Math.Min(bufferSize - i, blockSize * 2);
                            int lowerLen = Math.Min(len, blockSize);
                            int upperLen = len - lowerLen;
                            if (upperLen != 0)
                            {
                                Debug.Assert(blockSize == lowerLen);
                                Debug.Assert(blockSize == multiplier.Length);
                                Debug.Assert(multiplier.Length == lowerLen);
                                BigIntegerCalculator.Multiply(multiplier, curBuffer.Slice(blockSize, upperLen), curNewBuffer.Slice(0, len));
                            }

                            long carry = 0;
                            int j = 0;
                            for (; j < lowerLen; j++)
                            {
                                long digit = (curBuffer[j] + carry) + curNewBuffer[j];
                                curNewBuffer[j] = unchecked((uint)digit);
                                carry = digit >> 32;
                            }
                            if (carry != 0)
                            {
                                while (true)
                                {
                                    curNewBuffer[j]++;
                                    if (curNewBuffer[j] != 0)
                                    {
                                        break;
                                    }
                                    j++;
                                }
                            }
                        }

                        Span<uint> tmp = buffer;
                        buffer = newBuffer;
                        newBuffer = tmp;
                        blockSize *= 2;

                        if (bufferSize <= blockSize)
                        {
                            break;
                        }
                        newBuffer.Clear();
                        int[]? arrayToReturn = arrayFromPoolForMultiplier;

                        arrayFromPoolForMultiplier = ArrayPool<int>.Shared.Rent(blockSize);
                        Span<uint> newMultiplier = MemoryMarshal.Cast<int, uint>(arrayFromPoolForMultiplier).Slice(0, blockSize);
                        newMultiplier.Clear();
                        BigIntegerCalculator.Square(multiplier, newMultiplier);
                        multiplier = newMultiplier;
                        if (arrayToReturn is not null)
                        {
                            ArrayPool<int>.Shared.Return(arrayToReturn);
                        }
                    }

                    // shrink buffer to the currently used portion.
                    // First, calculate the rough size of the buffer from the ratio that the number
                    // of digits follows. Then, shrink the size until there is no more space left.
                    // The Ratio is calculated as: log_{2^32}(10^9)
                    const double digitRatio = 0.934292276687070661;
                    currentBufferSize = Math.Min((int)(bufferSize * digitRatio) + 1, bufferSize);
                    Debug.Assert(buffer.Length == currentBufferSize || buffer[currentBufferSize] == 0);
                    while (0 < currentBufferSize && buffer[currentBufferSize - 1] == 0)
                    {
                        currentBufferSize--;
                    }
                    currentBuffer = buffer.Slice(0, currentBufferSize);
                    result = NumberBufferToBigInteger(currentBuffer, number.IsNegative);
                }
                finally
                {
                    if (arrayFromPoolForMultiplier != null)
                    {
                        ArrayPool<int>.Shared.Return(arrayFromPoolForMultiplier);
                    }
                }
                return ParsingStatus.OK;
            }

            BigInteger NumberBufferToBigInteger(Span<uint> currentBuffer, bool signa)
            {
                int trailingZeroCount = numberScale - totalDigitCount;

                while (trailingZeroCount >= MaxPartialDigits)
                {
                    MultiplyAdd(ref currentBuffer, TenPowMaxPartial, 0);
                    trailingZeroCount -= MaxPartialDigits;
                }

                if (trailingZeroCount > 0)
                {
                    MultiplyAdd(ref currentBuffer, UInt32PowersOfTen[trailingZeroCount], 0);
                }

                int sign;
                uint[]? bits;

                if (currentBufferSize == 0)
                {
                    sign = 0;
                    bits = null;
                }
                else if (currentBufferSize == 1 && currentBuffer[0] <= int.MaxValue)
                {
                    sign = (int)(signa ? -currentBuffer[0] : currentBuffer[0]);
                    bits = null;
                }
                else
                {
                    sign = signa ? -1 : 1;
                    bits = currentBuffer.Slice(0, currentBufferSize).ToArray();
                }

                return new BigInteger(sign, bits);
            }

            // This function should only be used for result buffer.
            void MultiplyAdd(ref Span<uint> currentBuffer, uint multiplier, uint addValue)
            {
                Span<uint> curBits = currentBuffer.Slice(0, currentBufferSize);
                uint carry = addValue;

                for (int i = 0; i < curBits.Length; i++)
                {
                    ulong p = (ulong)multiplier * curBits[i] + carry;
                    curBits[i] = (uint)p;
                    carry = (uint)(p >> 32);
                }

                if (carry == 0)
                {
                    return;
                }

                if (currentBufferSize == currentBuffer.Length)
                {
                    int[]? arrayToReturn = arrayFromPoolForResultBuffer;

                    arrayFromPoolForResultBuffer = ArrayPool<int>.Shared.Rent(checked(currentBufferSize * 2));
                    Span<uint> newBuffer = MemoryMarshal.Cast<int, uint>(arrayFromPoolForResultBuffer);
                    currentBuffer.CopyTo(newBuffer);
                    currentBuffer = newBuffer;

                    if (arrayToReturn != null)
                    {
                        ArrayPool<int>.Shared.Return(arrayToReturn);
                    }
                }

                currentBuffer[currentBufferSize] = carry;
                currentBufferSize++;
            }
        }

        private static string? FormatBigIntegerToHex(bool targetSpan, BigInteger value, char format, int digits, NumberFormatInfo info, Span<char> destination, out int charsWritten, out bool spanSuccess)
        {
            Debug.Assert(format == 'x' || format == 'X');

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

            var sb = new ValueStringBuilder(stackalloc char[128]); // each byte is typically two chars

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
                        (char)(head + '0') :
                        format == 'X' ? (char)((head & 0xF) - 10 + 'A') : (char)((head & 0xF) - 10 + 'a'));
                    cur--;
                }
            }

            if (cur > -1)
            {
                Span<char> chars = sb.AppendSpan((cur + 1) * 2);
                int charsPos = 0;
                string hexValues = format == 'x' ? "0123456789abcdef" : "0123456789ABCDEF";
                while (cur > -1)
                {
                    byte b = bits[cur--];
                    chars[charsPos++] = hexValues[b >> 4];
                    chars[charsPos++] = hexValues[b & 0xF];
                }
            }

            if (digits > sb.Length)
            {
                // Insert leading zeros, e.g. user specified "X5" so we create "0ABCD" instead of "ABCD"
                sb.Insert(
                    0,
                    value._sign >= 0 ? '0' : (format == 'x') ? 'f' : 'F',
                    digits - sb.Length);
            }

            if (arrayToReturnToPool != null)
            {
                ArrayPool<byte>.Shared.Return(arrayToReturnToPool);
            }

            if (targetSpan)
            {
                spanSuccess = sb.TryCopyTo(destination, out charsWritten);
                return null;
            }
            else
            {
                charsWritten = 0;
                spanSuccess = false;
                return sb.ToString();
            }
        }

        private static string? FormatBigIntegerToBinary(bool targetSpan, BigInteger value, int digits, Span<char> destination, out int charsWritten, out bool spanSuccess)
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

            try
            {
                scoped ValueStringBuilder sb;
                if (targetSpan)
                {
                    if (charsIncludeDigits > destination.Length)
                    {
                        charsWritten = 0;
                        spanSuccess = false;
                        return null;
                    }

                    // Because we have ensured destination can take actual char length, so now just use ValueStringBuilder as wrapper so that subsequent logic can be reused by 2 flows (targetSpan and non-targetSpan);
                    // meanwhile there is no need to copy to destination again after format data for targetSpan flow.
                    sb = new ValueStringBuilder(destination);
                }
                else
                {
                    // each byte is typically eight chars
                    sb = charsIncludeDigits > 512
                        ? new ValueStringBuilder(charsIncludeDigits)
                        : new ValueStringBuilder(stackalloc char[512]);
                }

                if (digits > charsForBits)
                {
                    sb.Append(value._sign >= 0 ? '0' : '1', digits - charsForBits);
                }

                AppendByte(ref sb, highByte, charsInHighByte - 1);

                for (int i = bytes.Length - 2; i >= 0; i--)
                {
                    AppendByte(ref sb, bytes[i]);
                }

                Debug.Assert(sb.Length == charsIncludeDigits);

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
            finally
            {
                if (arrayToReturnToPool is not null)
                {
                    ArrayPool<byte>.Shared.Return(arrayToReturnToPool);
                }
            }

            static void AppendByte(ref ValueStringBuilder sb, byte b, int startHighBit = 7)
            {
                for (int i = startHighBit; i >= 0; i--)
                {
                    sb.Append((char)('0' + ((b >> i) & 0x1)));
                }
            }
        }

        internal static string FormatBigInteger(BigInteger value, string? format, NumberFormatInfo info)
        {
            return FormatBigInteger(targetSpan: false, value, format, format, info, default, out _, out _)!;
        }

        internal static bool TryFormatBigInteger(BigInteger value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<char> destination, out int charsWritten)
        {
            FormatBigInteger(targetSpan: true, value, null, format, info, destination, out charsWritten, out bool spanSuccess);
            return spanSuccess;
        }

        private const uint kuBase = 1_000_000_000; // 10^9
        private const int kcchBase = 9;

        private static unsafe string? FormatBigInteger(
            bool targetSpan, BigInteger value,
            string? formatString, ReadOnlySpan<char> formatSpan,
            NumberFormatInfo info, Span<char> destination, out int charsWritten, out bool spanSuccess)
        {
            Debug.Assert(formatString == null || formatString.Length == formatSpan.Length);

            int digits = 0;
            char fmt = ParseFormatSpecifier(formatSpan, out digits);
            if (fmt == 'x' || fmt == 'X')
            {
                return FormatBigIntegerToHex(targetSpan, value, fmt, digits, info, destination, out charsWritten, out spanSuccess);
            }
            if (fmt == 'b' || fmt == 'B')
            {
                return FormatBigIntegerToBinary(targetSpan, value, digits, destination, out charsWritten, out spanSuccess);
            }

            if (value._bits == null)
            {
                if (fmt == 'g' || fmt == 'G' || fmt == 'r' || fmt == 'R')
                {
                    formatSpan = formatString = digits > 0 ? $"D{digits}" : "D";
                }

                if (targetSpan)
                {
                    spanSuccess = value._sign.TryFormat(destination, out charsWritten, formatSpan, info);
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

            // First convert to base 10^9.
            int cuSrc = value._bits.Length;
            // A quick conservative max length of base 10^9 representation
            // A uint contributes to no more than 10/9 of 10^9 block, +1 for ceiling of division
            int cuMax = cuSrc * (kcchBase + 1) / kcchBase + 1;
            Debug.Assert((long)BigInteger.MaxLength * (kcchBase + 1) / kcchBase + 1 < (long)int.MaxValue); // won't overflow

            uint[]? bufferToReturn = null;
            Span<uint> base1E9Buffer = cuMax < BigIntegerCalculator.StackAllocThreshold ?
                stackalloc uint[cuMax] :
                (bufferToReturn = ArrayPool<uint>.Shared.Rent(cuMax));

            int cuDst = 0;

            for (int iuSrc = cuSrc; --iuSrc >= 0;)
            {
                uint uCarry = value._bits[iuSrc];
                for (int iuDst = 0; iuDst < cuDst; iuDst++)
                {
                    Debug.Assert(base1E9Buffer[iuDst] < kuBase);

                    // Use X86Base.DivRem when stable
                    ulong uuRes = NumericsHelpers.MakeUInt64(base1E9Buffer[iuDst], uCarry);
                    (ulong quo, ulong rem) = Math.DivRem(uuRes, kuBase);
                    uCarry = (uint)quo;
                    base1E9Buffer[iuDst] = (uint)rem;
                }
                if (uCarry != 0)
                {
                    (uCarry, base1E9Buffer[cuDst++]) = Math.DivRem(uCarry, kuBase);
                    if (uCarry != 0)
                        base1E9Buffer[cuDst++] = uCarry;
                }
            }

            ReadOnlySpan<uint> base1E9Value = base1E9Buffer[..cuDst];

            int valueDigits = (base1E9Value.Length - 1) * kcchBase + FormattingHelpers.CountDigits(base1E9Value[^1]);

            string? strResult;

            if (fmt == 'g' || fmt == 'G' || fmt == 'd' || fmt == 'D' || fmt == 'r' || fmt == 'R')
            {
                int strDigits = Math.Max(digits, valueDigits);
                string? sNegative = value.Sign < 0 ? info.NegativeSign : null;
                int strLength = strDigits + (sNegative?.Length ?? 0);

                if (targetSpan)
                {
                    if (destination.Length < strLength)
                    {
                        spanSuccess = false;
                        charsWritten = 0;
                    }
                    else
                    {
                        sNegative?.CopyTo(destination);
                        fixed (char* ptr = &MemoryMarshal.GetReference(destination))
                        {
                            BigIntegerToDecChars((Utf16Char*)ptr + strLength, base1E9Value, digits);
                        }
                        charsWritten = strLength;
                        spanSuccess = true;
                    }
                    strResult = null;
                }
                else
                {
                    spanSuccess = false;
                    charsWritten = 0;
                    fixed (uint* ptr = base1E9Value)
                    {
                        strResult = string.Create(strLength, (digits, ptr: (IntPtr)ptr, base1E9Value.Length, sNegative), static (span, state) =>
                        {
                            state.sNegative?.CopyTo(span);
                            fixed (char* ptr = &MemoryMarshal.GetReference(span))
                            {
                                BigIntegerToDecChars((Utf16Char*)ptr + span.Length, new ReadOnlySpan<uint>((void*)state.ptr, state.Length), state.digits);
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

                    scoped var vlb = new ValueListBuilder<Utf16Char>(stackalloc Utf16Char[CharStackBufferSize]); // arbitrary stack cut-off

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
                        spanSuccess = vlb.TryCopyTo(MemoryMarshal.Cast<char, Utf16Char>(destination), out charsWritten);
                        strResult = null;
                    }
                    else
                    {
                        charsWritten = 0;
                        spanSuccess = false;
                        strResult = MemoryMarshal.Cast<Utf16Char, char>(vlb.AsSpan()).ToString();
                    }

                    vlb.Dispose();
                    if (numberBufferToReturn != null)
                    {
                        ArrayPool<byte>.Shared.Return(numberBufferToReturn);
                    }
                }
            }

            if (bufferToReturn != null)
            {
                ArrayPool<uint>.Shared.Return(bufferToReturn);
            }

            return strResult;
        }

        private static unsafe TChar* BigIntegerToDecChars<TChar>(TChar* bufferEnd, ReadOnlySpan<uint> base1E9Value, int digits)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(base1E9Value[^1] != 0, "Leading zeros should be trimmed by caller.");

            // The base 10^9 value is in reverse order
            for (int i = 0; i < base1E9Value.Length - 1; i++)
            {
                bufferEnd = UInt32ToDecChars(bufferEnd, base1E9Value[i], kcchBase);
                digits -= kcchBase;
            }

            return UInt32ToDecChars(bufferEnd, base1E9Value[^1], digits);
        }
    }

    internal interface IBigIntegerHexOrBinaryParser<TParser, TChar>
        where TParser : struct, IBigIntegerHexOrBinaryParser<TParser, TChar>
        where TChar : unmanaged, IBinaryInteger<TChar>
    {
        static abstract int BitsPerDigit { get; }

        static virtual int DigitsPerBlock => sizeof(uint) * 8 / TParser.BitsPerDigit;

        static abstract NumberStyles BlockNumberStyle { get; }

        static abstract uint GetSignBitsIfValid(uint ch);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static virtual bool TryParseUnalignedBlock(ReadOnlySpan<TChar> input, out uint result)
        {
            if (typeof(TChar) == typeof(char))
            {
                return uint.TryParse(MemoryMarshal.Cast<TChar, char>(input), TParser.BlockNumberStyle, null, out result);
            }

            throw new NotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static virtual bool TryParseSingleBlock(ReadOnlySpan<TChar> input, out uint result)
            => TParser.TryParseUnalignedBlock(input, out result);

        static virtual bool TryParseWholeBlocks(ReadOnlySpan<TChar> input, Span<uint> destination)
        {
            Debug.Assert(destination.Length * TParser.DigitsPerBlock == input.Length);
            ref TChar lastWholeBlockStart = ref Unsafe.Add(ref MemoryMarshal.GetReference(input), input.Length - TParser.DigitsPerBlock);

            for (int i = 0; i < destination.Length; i++)
            {
                if (!TParser.TryParseSingleBlock(
                    MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Subtract(ref lastWholeBlockStart, i * TParser.DigitsPerBlock), TParser.DigitsPerBlock),
                    out destination[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }

    internal readonly struct BigIntegerHexParser<TChar> : IBigIntegerHexOrBinaryParser<BigIntegerHexParser<TChar>, TChar>
        where TChar : unmanaged, IBinaryInteger<TChar>
    {
        public static int BitsPerDigit => 4;

        public static NumberStyles BlockNumberStyle => NumberStyles.AllowHexSpecifier;

        // A valid ASCII hex digit is positive (0-7) if it starts with 00110
        public static uint GetSignBitsIfValid(uint ch) => (uint)((ch & 0b_1111_1000) == 0b_0011_0000 ? 0 : -1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseWholeBlocks(ReadOnlySpan<TChar> input, Span<uint> destination)
        {
            if (typeof(TChar) == typeof(char))
            {
                if (Convert.FromHexString(MemoryMarshal.Cast<TChar, char>(input), MemoryMarshal.AsBytes(destination), out _, out _) != OperationStatus.Done)
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

            throw new NotSupportedException();
        }
    }

    internal readonly struct BigIntegerBinaryParser<TChar> : IBigIntegerHexOrBinaryParser<BigIntegerBinaryParser<TChar>, TChar>
        where TChar : unmanaged, IBinaryInteger<TChar>
    {
        public static int BitsPerDigit => 1;

        public static NumberStyles BlockNumberStyle => NumberStyles.AllowBinarySpecifier;

        // Taking the LSB is enough for distinguishing 0/1
        public static uint GetSignBitsIfValid(uint ch) => (uint)(((int)ch << 31) >> 31);
    }
}
