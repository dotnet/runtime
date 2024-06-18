// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Numerics;

namespace System.Buffers.Text
{
    public static partial class Base64Url
    {
        private const uint EncodingPadEqual = '='; // '=', for padding

        private const uint EncodingPadPercentage = '%'; // allowed for url padding

        private static ReadOnlySpan<sbyte> DecodingMap =>
            [
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 62, -1, -1,         //62 is placed at index 45 (for -), 63 at index 95 (for _)
                    52, 53, 54, 55, 56, 57, 58, 59, 60, 61, -1, -1, -1, -1, -1, -1,         //52-61 are placed at index 48-57 (for 0-9)
                    -1,  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14,
                    15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, -1, -1, -1, -1, 63,         //0-25 are placed at index 65-90 (for A-Z)
                    -1, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40,
                    41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, -1, -1, -1, -1, -1,         //26-51 are placed at index 97-122 (for a-z)
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,         // Bytes over 122 ('z') are invalid and cannot be decoded
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,         // Hence, padding the map with 255, which indicates invalid input
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                ];

        private const int MaxStackallocThreshold = 256;

        /// <summary>
        /// Returns the maximum length (in bytes) of the result if you were to decode base 64 encoded text from a span of size <paramref name="base64Length"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The specified <paramref name="base64Length"/> is less than 0.
        /// </exception>
        public static int GetMaxDecodedLength(int base64Length)
        {
            if (base64Length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(base64Length));
            }

            int remainder = (int)((uint)base64Length % 4);

            return (base64Length >> 2) * 3 + (remainder > 0 ? remainder - 1 : 0);
        }

        /// <summary>
        /// Decodes the span of UTF-8 encoded text represented as Base64Url into binary data.
        /// </summary>
        /// <param name="source">The input span which contains UTF-8 encoded text in Base64Url that needs to be decoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the decoded binary data.</param>
        /// <param name="bytesConsumed">When this method returns, contains the number of input bytes consumed during the operation. This can be used to slice the input for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
        /// <param name="isFinalBlock"><see langword="true"/> when the input span contains the entirety of data to encode; <see langword="false"/> when more data may follow,
        /// such as when calling in a loop. Calls with <see langword="false"/> should be followed up with another call where this parameter is <see langword="true"/> call. The default is <see langword="true" />.</param>
        /// <returns>One of the enumeration values that indicates the success or failure of the operation.</returns>
        /// <remarks>
        /// As padding is optional for Base64Url the <paramref name="source"/> length not required to be a multiple of 4 even if <paramref name="isFinalBlock"/> is <see langword="true"/>.
        /// If the <paramref name="source"/> length is not a multiple of 4 and <paramref name="isFinalBlock"/> is <see langword="true"/> the remainders decoded accordingly:
        /// - Remainder of 3 bytes - decoded into 2 bytes data, decoding succeeds.
        /// - Remainder of 2 bytes - decoded into 1 byte data. decoding succeeds.
        /// - Remainder of 1 byte - will cause OperationStatus.InvalidData result.
        /// </remarks>
        public static OperationStatus DecodeFromUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true) =>
            DecodeFromUtf8(source, destination, out bytesConsumed, out bytesWritten, isFinalBlock, ignoreWhiteSpace: true);

        private static unsafe OperationStatus DecodeFromUtf8(ReadOnlySpan<byte> source, Span<byte> bytes,
            out int bytesConsumed, out int bytesWritten, bool isFinalBlock, bool ignoreWhiteSpace)
        {
            if (source.IsEmpty)
            {
                bytesConsumed = 0;
                bytesWritten = 0;
                return OperationStatus.Done;
            }

            fixed (byte* srcBytes = &MemoryMarshal.GetReference(source))
            fixed (byte* destBytes = &MemoryMarshal.GetReference(bytes))
            {
                int srcLength = isFinalBlock ? source.Length : source.Length & ~0x3;
                int destLength = bytes.Length;
                int maxSrcLength;
                int decodedLength = GetMaxDecodedLength(srcLength);

                byte* src = srcBytes;
                byte* dest = destBytes;
                byte* srcEnd = srcBytes + (uint)srcLength;

                // Last bytes could have padding characters, so process them separately and treat them as valid only if isFinalBlock is true
                // if isFinalBlock is false, padding characters are considered invalid
                int skipLastChunk = isFinalBlock ? 4 : 0;

                if (destLength >= decodedLength)
                {
                    maxSrcLength = srcLength - skipLastChunk;
                }
                else
                {
                    // This should never overflow since destLength here is less than int.MaxValue / 4 * 3 (i.e. 1610612733)
                    // Therefore, (destLength / 3) * 4 will always be less than 2147483641
                    Debug.Assert(destLength < (int.MaxValue / 4 * 3));
                    maxSrcLength = (destLength / 3) * 4;
                    int remainder = (int)((uint)destLength % 3);
                    if (isFinalBlock && remainder > 0)
                    {
                        srcLength &= ~0x3; // In case of Base64UrlDecoder source can be not a multiple of 4, round down to multiple of 4
                    }
                }

                ref sbyte decodingMap = ref MemoryMarshal.GetReference(DecodingMap);
                byte*  srcMax = srcBytes + maxSrcLength;

                while (src < srcMax)
                {
                    int result = DecodeFourElements(src, ref decodingMap);

                    if (result < 0)
                    {
                        goto InvalidDataExit;
                    }

                    WriteThreeLowOrderBytes(dest, result);
                    src += 4;
                    dest += 3;
                }

                if (maxSrcLength != srcLength - skipLastChunk)
                {
                    goto DestinationTooSmallExit;
                }

                if (src == srcEnd)
                {
                    if (isFinalBlock)
                    {
                        goto InvalidDataExit;
                    }

                    if (src == srcBytes + source.Length)
                    {
                        goto DoneExit;
                    }

                    goto NeedMoreDataExit;
                }

                // if isFinalBlock is false, we will never reach this point
                // Handle remaining bytes, if more than 4 bytes remained it will end up in InvalidDataExit (might succeed after whitespace removed)
                long remaining = srcEnd - src;
                Debug.Assert(remaining < 8);
                uint t0, t1, t2, t3;
                switch (remaining)
                {
                    case 2:
                        t0 = srcEnd[-2];
                        t1 = srcEnd[-1];
                        t2 = EncodingPadEqual;
                        t3 = EncodingPadEqual;
                        break;
                    case 3:
                        t0 = srcEnd[-3];
                        t1 = srcEnd[-2];
                        t2 = srcEnd[-1];
                        t3 = EncodingPadEqual;
                        break;
                    case 4:
                        t0 = srcEnd[-4];
                        t1 = srcEnd[-3];
                        t2 = srcEnd[-2];
                        t3 = srcEnd[-1];
                        break;
                    default:
                        goto InvalidDataExit;
                }

                int i0 = Unsafe.Add(ref decodingMap, (IntPtr)t0);
                int i1 = Unsafe.Add(ref decodingMap, (IntPtr)t1);

                i0 <<= 18;
                i1 <<= 12;

                i0 |= i1;

                byte* destMax = destBytes + (uint)destLength;

                if (!IsValidPadding(t3))
                {
                    int i2 = Unsafe.Add(ref decodingMap, (IntPtr)t2);
                    int i3 = Unsafe.Add(ref decodingMap, (IntPtr)t3);

                    i2 <<= 6;

                    i0 |= i3;
                    i0 |= i2;

                    if (i0 < 0)
                    {
                        goto InvalidDataExit;
                    }
                    if (dest + 3 > destMax)
                    {
                        goto DestinationTooSmallExit;
                    }

                    WriteThreeLowOrderBytes(dest, i0);
                    dest += 3;
                    src += 4;
                }
                else if (!IsValidPadding(t2))
                {
                    int i2 = Unsafe.Add(ref decodingMap, (IntPtr)t2);

                    i2 <<= 6;

                    i0 |= i2;

                    if (i0 < 0)
                    {
                        goto InvalidDataExit;
                    }
                    if (dest + 2 > destMax)
                    {
                        goto DestinationTooSmallExit;
                    }

                    dest[0] = (byte)(i0 >> 16);
                    dest[1] = (byte)(i0 >> 8);
                    dest += 2;
                    src += remaining;
                }
                else
                {
                    if (i0 < 0)
                    {
                        goto InvalidDataExit;
                    }
                    if (dest + 1 > destMax)
                    {
                        goto DestinationTooSmallExit;
                    }

                    dest[0] = (byte)(i0 >> 16);
                    dest += 1;
                    src += remaining;
                }

                if (srcLength != source.Length)
                {
                    goto InvalidDataExit;
                }

            DoneExit:
                bytesConsumed = (int)(src - srcBytes);
                bytesWritten = (int)(dest - destBytes);
                return OperationStatus.Done;

            DestinationTooSmallExit:
                if (srcLength != source.Length && isFinalBlock)
                {
                    goto InvalidDataExit; // if input is not a multiple of 4, and there is no more data, return invalid data instead
                }

                bytesConsumed = (int)(src - srcBytes);
                bytesWritten = (int)(dest - destBytes);
                return OperationStatus.DestinationTooSmall;

            NeedMoreDataExit:
                bytesConsumed = (int)(src - srcBytes);
                bytesWritten = (int)(dest - destBytes);
                return OperationStatus.NeedMoreData;

            InvalidDataExit:
                bytesConsumed = (int)(src - srcBytes);
                bytesWritten = (int)(dest - destBytes);
                return ignoreWhiteSpace ?
                    InvalidDataFallback(source, bytes, ref bytesConsumed, ref bytesWritten, isFinalBlock) :
                    OperationStatus.InvalidData;
            }

            static OperationStatus InvalidDataFallback(ReadOnlySpan<byte> source, Span<byte> bytes, ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock)
            {
                source = source.Slice(bytesConsumed);
                bytes = bytes.Slice(bytesWritten);

                OperationStatus status;
                do
                {
                    int localConsumed = IndexOfAnyExceptWhiteSpace(source);
                    if (localConsumed < 0)
                    {
                        // The remainder of the input is all whitespace. Mark it all as having been consumed,
                        // and mark the operation as being done.
                        bytesConsumed += source.Length;
                        status = OperationStatus.Done;
                        break;
                    }

                    if (localConsumed == 0)
                    {
                        // Non-whitespace was found at the beginning of the input. Since it wasn't consumed
                        // by the previous call to DecodeFromUtf8, it must be part of a Base64 sequence
                        // that was interrupted by whitespace or something else considered invalid.
                        // Fall back to block-wise decoding. This is very slow, but it's also very non-standard
                        // formatting of the input; whitespace is typically only found between blocks, such as
                        // when Convert.ToBase64String inserts a line break every 76 output characters.
                        return DecodeWithWhiteSpaceBlockwise(source, bytes, ref bytesConsumed, ref bytesWritten, isFinalBlock);
                    }

                    // Skip over the starting whitespace and continue.
                    bytesConsumed += localConsumed;
                    source = source.Slice(localConsumed);

                    // Try again after consumed whitespace
                    status = DecodeFromUtf8(source, bytes, out localConsumed, out int localWritten, isFinalBlock, ignoreWhiteSpace: false);
                    bytesConsumed += localConsumed;
                    bytesWritten += localWritten;
                    if (status is not OperationStatus.InvalidData)
                    {
                        break;
                    }

                    source = source.Slice(localConsumed);
                    bytes = bytes.Slice(localWritten);
                }
                while (!source.IsEmpty);

                return status;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int DecodeFourElements(byte* source, ref sbyte decodingMap)
        {
            // The 'source' span expected to have at least 4 elements, and the 'decodingMap' consists 256 sbytes
            uint t0 = source[0];
            uint t1 = source[1];
            uint t2 = source[2];
            uint t3 = source[3];

            int i0 = Unsafe.Add(ref decodingMap, (int)t0);
            int i1 = Unsafe.Add(ref decodingMap, (int)t1);
            int i2 = Unsafe.Add(ref decodingMap, (int)t2);
            int i3 = Unsafe.Add(ref decodingMap, (int)t3);

            i0 <<= 18;
            i1 <<= 12;
            i2 <<= 6;

            i0 |= i3;
            i1 |= i2;

            i0 |= i1;
            return i0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void WriteThreeLowOrderBytes(byte* destination, int value)
        {
            destination[0] = (byte)(value >> 16);
            destination[1] = (byte)(value >> 8);
            destination[2] = (byte)value;
        }

        private static bool IsValidPadding(uint padChar) => padChar == EncodingPadEqual || padChar == EncodingPadPercentage;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int IndexOfAnyExceptWhiteSpace(ReadOnlySpan<byte> span)
        {
            for (int i = 0; i < span.Length; i++)
            {
                if (!IsWhiteSpace(span[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsWhiteSpace(int value)
        {
            if (Environment.Is64BitProcess)
            {
                // For description see https://github.com/dotnet/runtime/blob/48e74187cb15386c29eedaa046a5ee2c7ddef161/src/libraries/Common/src/System/HexConverter.cs#L314-L330
                // Lookup bit mask for "\t\n\r ".
                const ulong MagicConstant = 0xC800010000000000UL;
                ulong i = (uint)value - '\t';
                ulong shift = MagicConstant << (int)i;
                ulong mask = i - 64;
                return (long)(shift & mask) < 0;
            }

            if (value < 32)
            {
                const int BitMask = (1 << (int)'\t') | (1 << (int)'\n') | (1 << (int)'\r');
                return ((1 << value) & BitMask) != 0;
            }

            return value == 32;
        }

        private static OperationStatus DecodeWithWhiteSpaceBlockwise(ReadOnlySpan<byte> source, Span<byte> bytes, ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock = true)
        {
            const int BlockSize = 4;
            Span<byte> buffer = stackalloc byte[BlockSize];
            OperationStatus status = OperationStatus.Done;

            while (!source.IsEmpty)
            {
                int encodedIdx = 0;
                int bufferIdx = 0;
                int skipped = 0;

                for (; encodedIdx < source.Length && (uint)bufferIdx < (uint)buffer.Length; ++encodedIdx)
                {
                    if (IsWhiteSpace(source[encodedIdx]))
                    {
                        skipped++;
                    }
                    else
                    {
                        buffer[bufferIdx] = source[encodedIdx];
                        bufferIdx++;
                    }
                }

                source = source.Slice(encodedIdx);
                bytesConsumed += skipped;

                if (bufferIdx == 0)
                {
                    continue;
                }

                bool hasAnotherBlock = source.Length > 1;

                bool localIsFinalBlock = !hasAnotherBlock;

                // If this block contains padding and there's another block, then only whitespace may follow for being valid.
                if (hasAnotherBlock)
                {
                    int paddingCount = GetPaddingCount(ref buffer[BlockSize - 1]);
                    if (paddingCount > 0)
                    {
                        hasAnotherBlock = false;
                        localIsFinalBlock = true;
                    }
                }

                if (localIsFinalBlock && !isFinalBlock)
                {
                    localIsFinalBlock = false;
                }

                status = DecodeFromUtf8(buffer.Slice(0, bufferIdx), bytes, out int localConsumed, out int localWritten, localIsFinalBlock, ignoreWhiteSpace: false);
                bytesConsumed += localConsumed;
                bytesWritten += localWritten;

                if (status != OperationStatus.Done)
                {
                    return status;
                }

                // The remaining data must all be whitespace in order to be valid.
                if (!hasAnotherBlock)
                {
                    for (int i = 0; i < source.Length; ++i)
                    {
                        if (!IsWhiteSpace(source[i]))
                        {
                            // Revert previous dest increment, since an invalid state followed.
                            bytesConsumed -= localConsumed;
                            bytesWritten -= localWritten;

                            return OperationStatus.InvalidData;
                        }

                        bytesConsumed++;
                    }

                    break;
                }

                bytes = bytes.Slice(localWritten);
            }

            return status;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetPaddingCount(ref byte ptrToLastElement)
        {
            int padding = 0;

            if (IsValidPadding(ptrToLastElement))
            {
                padding++;
            }

            if (IsValidPadding(Unsafe.Subtract(ref ptrToLastElement, 1)))
            {
                padding++;
            }

            return padding;
        }

        /// <summary>
        /// Decodes the span of UTF-8 encoded text in Base64Url into binary data, in-place.
        /// The decoded binary output is smaller than the text data contained in the input (the operation deflates the data).
        /// </summary>
        /// <param name="buffer">The input span which contains the base 64 text data that needs to be decoded.</param>
        /// <returns>The number of bytes written into <paramref name="buffer"/>. This can be used to slice the output for subsequent calls, if necessary.</returns>
        /// <exception cref="FormatException"><paramref name="buffer"/> contains an invalid Base64Url character,
        /// more than two padding characters, or a non white space character among the padding characters.</exception>
        /// <remarks>
        /// As padding is optional for Base64Url the <paramref name="buffer"/> length not required to be a multiple of 4.
        /// If the <paramref name="buffer"/> length is not a multiple of 4 the remainders decoded accordingly:
        /// - Remainder of 3 bytes - decoded into 2 bytes data, decoding succeeds.
        /// - Remainder of 2 bytes - decoded into 1 byte data. decoding succeeds.
        /// - Remainder of 1 byte - is invalid input, causes FormatException.
        /// </remarks>
        public static int DecodeFromUtf8InPlace(Span<byte> buffer)
        {
            OperationStatus status = DecodeFromUtf8InPlace(buffer, out int bytesWritten, ignoreWhiteSpace: true);

            // Base64.DecodeFromUtf8InPlace returns OperationStatus, therefore doesn't throw.
            // For Base64Url, this is not an OperationStatus API and thus throws.
            if (status == OperationStatus.InvalidData)
            {
                throw new FormatException(SR.Format_BadBase64Char);
            }

            Debug.Assert(status is OperationStatus.Done);
            return bytesWritten;
        }

        internal static unsafe OperationStatus DecodeFromUtf8InPlace(Span<byte> buffer, out int bytesWritten, bool ignoreWhiteSpace)
        {
            if (buffer.IsEmpty)
            {
                bytesWritten = 0;
                return OperationStatus.Done;
            }

            fixed (byte* bufferBytes = &MemoryMarshal.GetReference(buffer))
            {
                uint bufferLength = (uint)buffer.Length;
                uint sourceIndex = 0;
                uint destIndex = 0;

                if ((bufferLength & 3) == 1) // One byte cannot be decoded completely)
                {
                    goto InvalidExit;
                }

                ref sbyte decodingMap = ref MemoryMarshal.GetReference(DecodingMap);

                if (bufferLength > 4)
                {
                    while (sourceIndex < bufferLength - 4)
                    {
                        int result = DecodeFourElements(bufferBytes + sourceIndex, ref decodingMap);
                        if (result < 0)
                        {
                            goto InvalidExit;
                        }

                        WriteThreeLowOrderBytes(bufferBytes + destIndex, result);
                        destIndex += 3;
                        sourceIndex += 4;
                    }
                }

                uint t0, t1, t2, t3;

                switch (bufferLength - sourceIndex)
                {
                    case 2:
                        t0 = bufferBytes[bufferLength - 2];
                        t1 = bufferBytes[bufferLength - 1];
                        t2 = EncodingPadEqual;
                        t3 = EncodingPadEqual;
                        break;
                    case 3:
                        t0 = bufferBytes[bufferLength - 3];
                        t1 = bufferBytes[bufferLength - 2];
                        t2 = bufferBytes[bufferLength - 1];
                        t3 = EncodingPadEqual;
                        break;
                    case 4:
                        t0 = bufferBytes[bufferLength - 4];
                        t1 = bufferBytes[bufferLength - 3];
                        t2 = bufferBytes[bufferLength - 2];
                        t3 = bufferBytes[bufferLength - 1];
                        break;
                    default:
                        goto InvalidExit;
                }

                int i0 = Unsafe.Add(ref decodingMap, (int)t0);
                int i1 = Unsafe.Add(ref decodingMap, (int)t1);

                i0 <<= 18;
                i1 <<= 12;

                i0 |= i1;

                if (!IsValidPadding(t3))
                {
                    int i2 = Unsafe.Add(ref decodingMap, (int)t2);
                    int i3 = Unsafe.Add(ref decodingMap, (int)t3);

                    i2 <<= 6;

                    i0 |= i3;
                    i0 |= i2;

                    if (i0 < 0)
                    {
                        goto InvalidExit;
                    }

                    WriteThreeLowOrderBytes(bufferBytes + destIndex, i0);
                    destIndex += 3;
                }
                else if (!IsValidPadding(t2))
                {
                    int i2 = Unsafe.Add(ref decodingMap, (int)t2);

                    i2 <<= 6;

                    i0 |= i2;

                    if (i0 < 0)
                    {
                        goto InvalidExit;
                    }

                    bufferBytes[destIndex] = (byte)(i0 >> 16);
                    bufferBytes[destIndex + 1] = (byte)(i0 >> 8);
                    destIndex += 2;
                }
                else
                {
                    if (i0 < 0)
                    {
                        goto InvalidExit;
                    }

                    bufferBytes[destIndex] = (byte)(i0 >> 16);
                    destIndex += 1;
                }

                bytesWritten = (int)destIndex;
                return OperationStatus.Done;

            InvalidExit:
                bytesWritten = (int)destIndex;
                return ignoreWhiteSpace ?
                    DecodeWithWhiteSpaceFromUtf8InPlace(buffer, ref bytesWritten, sourceIndex) : // The input may have whitespace, attempt to decode while ignoring whitespace.
                    OperationStatus.InvalidData;
            }
        }

        private static OperationStatus DecodeWithWhiteSpaceFromUtf8InPlace(Span<byte> source, ref int destIndex, uint sourceIndex)
        {
            int BlockSize = Math.Min(source.Length - (int)sourceIndex, 4);
            Span<byte> buffer = stackalloc byte[BlockSize];

            OperationStatus status = OperationStatus.Done;
            int localDestIndex = destIndex;
            bool hasPaddingBeenProcessed = false;
            int localBytesWritten = 0;

            while (sourceIndex < (uint)source.Length)
            {
                int bufferIdx = 0;

                while (bufferIdx < BlockSize && sourceIndex < (uint)source.Length)
                {
                    if (!IsWhiteSpace(source[(int)sourceIndex]))
                    {
                        buffer[bufferIdx] = source[(int)sourceIndex];
                        bufferIdx++;
                    }

                    sourceIndex++;
                }

                if (bufferIdx == 0)
                {
                    continue;
                }

                if (bufferIdx != 4)
                {
                    // For Base64Url 1 byte is not decodeable.
                    if (bufferIdx == 1)
                    {
                        status = OperationStatus.InvalidData;
                        break;
                    }
                    else // Fill empty slots in last block with padding
                    {
                        while (bufferIdx < BlockSize)  // Can happen only for last block
                        {
                            Debug.Assert(source.Length == sourceIndex);
                            buffer[bufferIdx++] = (byte)EncodingPadEqual;
                        }
                    }
                }

                if (hasPaddingBeenProcessed)
                {
                    // Padding has already been processed, a new valid block cannot be processed.
                    // Revert previous dest increment, since an invalid state followed.
                    localDestIndex -= localBytesWritten;
                    status = OperationStatus.InvalidData;
                    break;
                }

                status = DecodeFromUtf8InPlace(buffer, out localBytesWritten, ignoreWhiteSpace: false);
                localDestIndex += localBytesWritten;
                hasPaddingBeenProcessed = localBytesWritten < 3;

                if (status != OperationStatus.Done)
                {
                    break;
                }

                // Write result to source span in place.
                for (int i = 0; i < localBytesWritten; i++)
                {
                    source[localDestIndex - localBytesWritten + i] = buffer[i];
                }
            }

            destIndex = localDestIndex;
            return status;
        }

        /// <summary>
        /// Decodes the span of UTF-8 encoded text represented as Base64Url into binary data.
        /// </summary>
        /// <param name="source">The input span which contains UTF-8 encoded text in Base64Url that needs to be decoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the decoded binary data.</param>
        /// <returns>The number of bytes written into <paramref name="destination"/>. This can be used to slice the output for subsequent calls, if necessary.</returns>
        /// <exception cref="ArgumentException">The buffer in <paramref name="destination"/> is too small to hold the encoded output.</exception>
        /// <exception cref="FormatException"><paramref name="source"/> contains an invalid Base64Url character,
        /// more than two padding characters, or a non white space character among the padding characters.</exception>
        /// <remarks>
        /// As padding is optional for Base64Url the <paramref name="source"/> length not required to be a multiple of 4.
        /// If the <paramref name="source"/> length is not a multiple of 4 the remainders decoded accordingly:
        /// - Remainder of 3 bytes - decoded into 2 bytes data, decoding succeeds.
        /// - Remainder of 2 bytes - decoded into 1 byte data. decoding succeeds.
        /// - Remainder of 1 byte - is invalid input, causes FormatException.
        /// </remarks>
        public static int DecodeFromUtf8(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            OperationStatus status = DecodeFromUtf8(source, destination, out _, out int bytesWritten);

            if (status == OperationStatus.Done)
            {
                return bytesWritten;
            }

            if (status == OperationStatus.DestinationTooSmall)
            {
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
            }

            Debug.Assert(status is OperationStatus.InvalidData);
            throw new FormatException(SR.Format_BadBase64Char);
        }

        /// <summary>
        /// Decodes the span of UTF-8 encoded text represented as Base64Url into binary data.
        /// </summary>
        /// <param name="source">The input span which contains UTF-8 encoded text in Base64Url that needs to be decoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the decoded binary data.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
        /// <returns><see langword="true"/> if bytes decoded successfully, otherwise <see langword="false"/>.</returns>
        /// <exception cref="FormatException"><paramref name="source"/> contains an invalid Base64Url character,
        /// more than two padding characters, or a non white space character among the padding characters.</exception>
        public static bool TryDecodeFromUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            OperationStatus status = DecodeFromUtf8(source, destination, out _, out bytesWritten);

            if (status == OperationStatus.InvalidData)
            {
                throw new FormatException(SR.Format_BadBase64Char);
            }

            Debug.Assert(status is OperationStatus.Done or OperationStatus.DestinationTooSmall);
            return status == OperationStatus.Done;
        }

        /// <summary>
        /// Decodes the span of UTF-8 encoded text represented as Base64Url into binary data.
        /// </summary>
        /// <param name="source">The input span which contains UTF-8 encoded text in Base64Url that needs to be decoded.</param>
        /// <returns>>A byte array which contains the result of the decoding operation.</returns>
        /// <exception cref="FormatException"><paramref name="source"/> contains an invalid Base64Url character,
        /// more than two padding characters, or a non white space character among the padding characters.</exception>
        public static byte[] DecodeFromUtf8(ReadOnlySpan<byte> source)
        {
            int upperBound = GetMaxDecodedLength(source.Length);
            byte[]? rented = null;

            Span<byte> destination = upperBound <= MaxStackallocThreshold
                ? stackalloc byte[MaxStackallocThreshold]
                : (rented = ArrayPool<byte>.Shared.Rent(upperBound));

            OperationStatus status = DecodeFromUtf8(source, destination, out _, out int bytesWritten);
            Debug.Assert(status is OperationStatus.Done or OperationStatus.InvalidData);
            byte[] ret = destination.Slice(0, bytesWritten).ToArray();

            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }

            return status == OperationStatus.Done ? ret : throw new FormatException(SR.Format_BadBase64Char);
        }

        /// <summary>
        /// Decodes the span of unicode ASCII chars represented as Base64Url into binary data.
        /// </summary>
        /// <param name="source">The input span which contains unicode ASCII chars in Base64Url that needs to be decoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the decoded binary data.</param>
        /// <param name="charsConsumed">When this method returns, contains the number of input chars consumed during the operation. This can be used to slice the input for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
        /// <param name="isFinalBlock"><see langword="true"/> when the input span contains the entirety of data to encode; <see langword="false"/> when more data may follow,
        /// such as when calling in a loop. Calls with <see langword="false"/> should be followed up with another call where this parameter is <see langword="true"/> call. The default is <see langword="true" />.</param>
        /// <returns>One of the enumeration values that indicates the success or failure of the operation.</returns>
        /// <remarks>
        /// As padding is optional for Base64Url the <paramref name="source"/> length not required to be a multiple of 4 even if <paramref name="isFinalBlock"/> is <see langword="true"/>.
        /// If the <paramref name="source"/> length is not a multiple of 4 and <paramref name="isFinalBlock"/> is <see langword="true"/> the remainders decoded accordingly:
        /// - Remainder of 3 chars - decoded into 2 bytes data, decoding succeeds.
        /// - Remainder of 2 chars - decoded into 1 byte data. decoding succeeds.
        /// - Remainder of 1 char - will cause OperationStatus.InvalidData result.
        /// </remarks>
        public static OperationStatus DecodeFromChars(ReadOnlySpan<char> source, Span<byte> destination,
            out int charsConsumed, out int bytesWritten, bool isFinalBlock = true) =>
            DecodeFromChars(MemoryMarshal.Cast<char, ushort>(source), destination, out charsConsumed, out bytesWritten, isFinalBlock, ignoreWhiteSpace: true);

        private static unsafe OperationStatus DecodeFromChars(ReadOnlySpan<ushort> source, Span<byte> bytes,
            out int bytesConsumed, out int bytesWritten, bool isFinalBlock, bool ignoreWhiteSpace)
        {
            if (source.IsEmpty)
            {
                bytesConsumed = 0;
                bytesWritten = 0;
                return OperationStatus.Done;
            }

            fixed (ushort* srcBytes = &MemoryMarshal.GetReference(source))
            fixed (byte* destBytes = &MemoryMarshal.GetReference(bytes))
            {
                int srcLength = isFinalBlock ? source.Length : source.Length & ~0x3;
                int destLength = bytes.Length;
                int maxSrcLength = srcLength;
                int decodedLength = GetMaxDecodedLength(srcLength);

                ushort* src = srcBytes;
                byte* dest = destBytes;
                ushort* srcEnd = srcBytes + (uint)srcLength;
                ushort* srcMax = srcBytes + (uint)maxSrcLength;

                // Last bytes could have padding characters, so process them separately and treat them as valid only if isFinalBlock is true
                // if isFinalBlock is false, padding characters are considered invalid
                int skipLastChunk = isFinalBlock ? 4 : 0;

                if (destLength >= decodedLength)
                {
                    maxSrcLength = srcLength - skipLastChunk;
                }
                else
                {
                    // This should never overflow since destLength here is less than int.MaxValue / 4 * 3 (i.e. 1610612733)
                    // Therefore, (destLength / 3) * 4 will always be less than 2147483641
                    Debug.Assert(destLength < (int.MaxValue / 4 * 3));
                    maxSrcLength = destLength / 3 * 4;
                    int remainder = (int)((uint)destLength % 3);
                    if (isFinalBlock && remainder > 0)
                    {
                        srcLength &= ~0x3; // In case of Base64UrlDecoder source can be not a multiple of 4, round down to multiple of 4
                    }
                }

                ref sbyte decodingMap = ref MemoryMarshal.GetReference(DecodingMap);
                srcMax = srcBytes + maxSrcLength;

                while (src < srcMax)
                {
                    int result = DecodeFourElements(src, ref decodingMap);

                    if (result < 0)
                    {
                        goto InvalidDataExit;
                    }

                    WriteThreeLowOrderBytes(dest, result);
                    src += 4;
                    dest += 3;
                }

                if (maxSrcLength != srcLength - skipLastChunk)
                {
                    goto DestinationTooSmallExit;
                }

                if (src == srcEnd)
                {
                    if (isFinalBlock)
                    {
                        goto InvalidDataExit;
                    }

                    if (src == srcBytes + source.Length)
                    {
                        goto DoneExit;
                    }

                    goto NeedMoreDataExit;
                }

                // if isFinalBlock is false, we will never reach this point
                // Handle remaining bytes, if more than 4 bytes remained it will end up in InvalidDataExit (might succeed after whitespace removed)
                long remaining = srcEnd - src;
                Debug.Assert(remaining < 8);
                uint t0, t1, t2, t3;
                switch (remaining)
                {
                    case 2:
                        t0 = srcEnd[-2];
                        t1 = srcEnd[-1];
                        t2 = EncodingPadEqual;
                        t3 = EncodingPadEqual;
                        break;
                    case 3:
                        t0 = srcEnd[-3];
                        t1 = srcEnd[-2];
                        t2 = srcEnd[-1];
                        t3 = EncodingPadEqual;
                        break;
                    case 4:
                        t0 = srcEnd[-4];
                        t1 = srcEnd[-3];
                        t2 = srcEnd[-2];
                        t3 = srcEnd[-1];
                        break;
                    default:
                        goto InvalidDataExit;
                }

                int i0 = Unsafe.Add(ref decodingMap, (IntPtr)t0);
                int i1 = Unsafe.Add(ref decodingMap, (IntPtr)t1);

                i0 <<= 18;
                i1 <<= 12;

                i0 |= i1;

                byte* destMax = destBytes + (uint)destLength;

                if (!IsValidPadding(t3))
                {
                    int i2 = Unsafe.Add(ref decodingMap, (IntPtr)t2);
                    int i3 = Unsafe.Add(ref decodingMap, (IntPtr)t3);

                    i2 <<= 6;

                    i0 |= i3;
                    i0 |= i2;

                    if (i0 < 0)
                    {
                        goto InvalidDataExit;
                    }
                    if (dest + 3 > destMax)
                    {
                        goto DestinationTooSmallExit;
                    }

                    WriteThreeLowOrderBytes(dest, i0);
                    dest += 3;
                    src += 4;
                }
                else if (!IsValidPadding(t2))
                {
                    int i2 = Unsafe.Add(ref decodingMap, (IntPtr)t2);

                    i2 <<= 6;

                    i0 |= i2;

                    if (i0 < 0)
                    {
                        goto InvalidDataExit;
                    }
                    if (dest + 2 > destMax)
                    {
                        goto DestinationTooSmallExit;
                    }

                    dest[0] = (byte)(i0 >> 16);
                    dest[1] = (byte)(i0 >> 8);
                    dest += 2;
                    src += remaining;
                }
                else
                {
                    if (i0 < 0)
                    {
                        goto InvalidDataExit;
                    }
                    if (dest + 1 > destMax)
                    {
                        goto DestinationTooSmallExit;
                    }

                    dest[0] = (byte)(i0 >> 16);
                    dest += 1;
                    src += remaining;
                }

                if (srcLength != source.Length)
                {
                    goto InvalidDataExit;
                }

            DoneExit:
                bytesConsumed = (int)(src - srcBytes);
                bytesWritten = (int)(dest - destBytes);
                return OperationStatus.Done;

            DestinationTooSmallExit:
                if (srcLength != source.Length && isFinalBlock)
                {
                    goto InvalidDataExit; // if input is not a multiple of 4, and there is no more data, return invalid data instead
                }

                bytesConsumed = (int)(src - srcBytes);
                bytesWritten = (int)(dest - destBytes);
                return OperationStatus.DestinationTooSmall;

            NeedMoreDataExit:
                bytesConsumed = (int)(src - srcBytes);
                bytesWritten = (int)(dest - destBytes);
                return OperationStatus.NeedMoreData;

            InvalidDataExit:
                bytesConsumed = (int)(src - srcBytes);
                bytesWritten = (int)(dest - destBytes);
                return ignoreWhiteSpace ?
                    InvalidDataFallback(source, bytes, ref bytesConsumed, ref bytesWritten, isFinalBlock) :
                    OperationStatus.InvalidData;
            }

            static OperationStatus InvalidDataFallback(ReadOnlySpan<ushort> source, Span<byte> bytes, ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock)
            {
                source = source.Slice(bytesConsumed);
                bytes = bytes.Slice(bytesWritten);

                OperationStatus status;
                do
                {
                    int localConsumed = IndexOfAnyExceptWhiteSpace(source);
                    if (localConsumed < 0)
                    {
                        // The remainder of the input is all whitespace. Mark it all as having been consumed,
                        // and mark the operation as being done.
                        bytesConsumed += source.Length;
                        status = OperationStatus.Done;
                        break;
                    }

                    if (localConsumed == 0)
                    {
                        // Non-whitespace was found at the beginning of the input. Since it wasn't consumed
                        // by the previous call to DecodeFromUtf8, it must be part of a Base64 sequence
                        // that was interrupted by whitespace or something else considered invalid.
                        // Fall back to block-wise decoding. This is very slow, but it's also very non-standard
                        // formatting of the input; whitespace is typically only found between blocks, such as
                        // when Convert.ToBase64String inserts a line break every 76 output characters.
                        return DecodeWithWhiteSpaceBlockwise(source, bytes, ref bytesConsumed, ref bytesWritten, isFinalBlock);
                    }

                    // Skip over the starting whitespace and continue.
                    bytesConsumed += localConsumed;
                    source = source.Slice(localConsumed);

                    // Try again after consumed whitespace
                    status = DecodeFromChars(source, bytes, out localConsumed, out int localWritten, isFinalBlock, ignoreWhiteSpace: false);
                    bytesConsumed += localConsumed;
                    bytesWritten += localWritten;
                    if (status is not OperationStatus.InvalidData)
                    {
                        break;
                    }

                    source = source.Slice(localConsumed);
                    bytes = bytes.Slice(localWritten);
                }
                while (!source.IsEmpty);

                return status;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int DecodeFourElements(ushort* source, ref sbyte decodingMap)
        {
            // The 'source' span expected to have at least 4 elements, and the 'decodingMap' consists 256 sbytes
            uint t0 = source[0];
            uint t1 = source[1];
            uint t2 = source[2];
            uint t3 = source[3];

            if (((t0 | t1 | t2 | t3) & 0xffffff00) != 0)
            {
                return -1; // One or more chars falls outside the 00..ff range, invalid Base64Url character.
            }

            int i0 = Unsafe.Add(ref decodingMap, (int)t0);
            int i1 = Unsafe.Add(ref decodingMap, (int)t1);
            int i2 = Unsafe.Add(ref decodingMap, (int)t2);
            int i3 = Unsafe.Add(ref decodingMap, (int)t3);

            i0 <<= 18;
            i1 <<= 12;
            i2 <<= 6;

            i0 |= i3;
            i1 |= i2;

            i0 |= i1;
            return i0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int IndexOfAnyExceptWhiteSpace(ReadOnlySpan<ushort> span)
        {
            for (int i = 0; i < span.Length; i++)
            {
                if (!IsWhiteSpace(span[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private static OperationStatus DecodeWithWhiteSpaceBlockwise(ReadOnlySpan<ushort> source, Span<byte> bytes, ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock = true)
        {
            const int BlockSize = 4;
            Span<ushort> buffer = stackalloc ushort[BlockSize];
            OperationStatus status = OperationStatus.Done;

            while (!source.IsEmpty)
            {
                int encodedIdx = 0;
                int bufferIdx = 0;
                int skipped = 0;

                for (; encodedIdx < source.Length && (uint)bufferIdx < (uint)buffer.Length; ++encodedIdx)
                {
                    if (IsWhiteSpace(source[encodedIdx]))
                    {
                        skipped++;
                    }
                    else
                    {
                        buffer[bufferIdx] = source[encodedIdx];
                        bufferIdx++;
                    }
                }

                source = source.Slice(encodedIdx);
                bytesConsumed += skipped;

                if (bufferIdx == 0)
                {
                    continue;
                }

                bool hasAnotherBlock = source.Length >= BlockSize && bufferIdx == BlockSize;
                bool localIsFinalBlock = !hasAnotherBlock;

                // If this block contains padding and there's another block, then only whitespace may follow for being valid.
                if (hasAnotherBlock)
                {
                    int paddingCount = GetPaddingCount(ref buffer[3]);
                    if (paddingCount > 0)
                    {
                        hasAnotherBlock = false;
                        localIsFinalBlock = true;
                    }
                }

                if (localIsFinalBlock && !isFinalBlock)
                {
                    localIsFinalBlock = false;
                }

                status = DecodeFromChars(buffer.Slice(0, bufferIdx), bytes, out int localConsumed, out int localWritten, localIsFinalBlock, ignoreWhiteSpace: false);
                bytesConsumed += localConsumed;
                bytesWritten += localWritten;

                if (status != OperationStatus.Done)
                {
                    return status;
                }

                // The remaining data must all be whitespace in order to be valid.
                if (!hasAnotherBlock)
                {
                    for (int i = 0; i < source.Length; ++i)
                    {
                        if (!IsWhiteSpace(source[i]))
                        {
                            // Revert previous dest increment, since an invalid state followed.
                            bytesConsumed -= localConsumed;
                            bytesWritten -= localWritten;

                            return OperationStatus.InvalidData;
                        }

                        bytesConsumed++;
                    }

                    break;
                }

                bytes = bytes.Slice(localWritten);
            }

            return status;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetPaddingCount(ref ushort ptrToLastElement)
        {
            int padding = 0;

            if (IsValidPadding(ptrToLastElement))
            {
                padding++;
            }

            if (IsValidPadding(Unsafe.Subtract(ref ptrToLastElement, 1)))
            {
                padding++;
            }

            return padding;
        }

        /// <summary>
        /// Decodes the span of unicode ASCII chars represented as Base64Url into binary data.
        /// </summary>
        /// <param name="source">The input span which contains ASCII chars in Base64Url that needs to be decoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the decoded binary data.</param>
        /// <returns>The number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary.</returns>
        /// <exception cref="ArgumentException">The buffer in <paramref name="destination"/> is too small to hold the encoded output.</exception>
        /// <exception cref="FormatException"><paramref name="source"/> contains a invalid Base64Url character,
        /// more than two padding characters, or a non white space character among the padding characters.</exception>
        public static int DecodeFromChars(ReadOnlySpan<char> source, Span<byte> destination)
        {
            OperationStatus status = DecodeFromChars(source, destination, out _, out int bytesWritten);

            if (status == OperationStatus.Done)
            {
                return bytesWritten;
            }

            if (status == OperationStatus.DestinationTooSmall)
            {
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
            }

            Debug.Assert(status == OperationStatus.InvalidData);
            throw new FormatException(SR.Format_BadBase64Char);
        }

        /// <summary>
        /// Decodes the span of unicode ASCII chars represented as Base64Url into binary data.
        /// </summary>
        /// <param name="source">The input span which contains ASCII chars in Base64Url that needs to be decoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the decoded binary data.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
        /// <returns><see langword="true"/> if bytes decoded successfully, otherwise <see langword="false"/>.</returns>
        /// <exception cref="FormatException"><paramref name="source"/> contains an invalid Base64Url character,
        /// more than two padding characters, or a non white space character among the padding characters.</exception>
        public static bool TryDecodeFromChars(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten)
        {
            OperationStatus status = DecodeFromChars(source, destination, out _, out bytesWritten);

            if (status == OperationStatus.InvalidData)
            {
                throw new FormatException(SR.Format_BadBase64Char);
            }

            return status == OperationStatus.Done;
        }

        /// <summary>
        /// Decodes the span of unicode ASCII chars represented as Base64Url into binary data.
        /// </summary>
        /// <param name="source">The input span which contains ASCII chars in Base64Url that needs to be decoded.</param>
        /// <returns>A byte array which contains the result of the decoding operation.</returns>
        /// <exception cref="FormatException"><paramref name="source"/> contains a invalid Base64Url character,
        /// more than two padding characters, or a non white space character among the padding characters.</exception>
        public static byte[] DecodeFromChars(ReadOnlySpan<char> source)
        {
            int upperBound = GetMaxDecodedLength(source.Length);
            byte[]? rented = null;

            Span<byte> destination = upperBound <= MaxStackallocThreshold
                ? stackalloc byte[MaxStackallocThreshold]
                : (rented = ArrayPool<byte>.Shared.Rent(upperBound));

            OperationStatus status = DecodeFromChars(source, destination, out _, out int bytesWritten);
            byte[] ret = destination.Slice(0, bytesWritten).ToArray();

            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }

            return status == OperationStatus.Done ? ret : throw new FormatException(SR.Format_BadBase64Char);
        }
     }
}
