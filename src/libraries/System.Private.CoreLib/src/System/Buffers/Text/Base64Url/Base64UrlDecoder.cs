// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Text.Unicode;
using static System.Buffers.Text.Base64;

namespace System.Buffers.Text
{
    // AVX2 and Vector128 version based on https://github.com/gfoidl/Base64/blob/5383320e28cac6c7ac6f86502fb05d23a048a21d/source/gfoidl.Base64/Internal/Encodings/Base64UrlEncoding.cs

    public static partial class Base64Url
    {
        /// <summary>
        /// Returns the maximum length (in bytes) of the result if you were to decode base 64 encoded text within a byte span of size "base64Length".
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="base64Length"/> is less than 0.
        /// </exception>
        public static int GetMaxDecodedLength(int base64Length)
        {
            if (base64Length < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
            }

            int remainder = (int)((uint)base64Length % 4);

            return (base64Length >> 2) * 3 + (remainder > 0 ? remainder - 1 : 0);
        }

        /// <summary>
        /// Decode the span of UTF-8 encoded text represented as Base64Url into binary data.
        /// </summary>
        /// <param name="source">The input span which contains UTF-8 encoded text in Base64Url that needs to be decoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the decoded binary data.</param>
        /// <param name="bytesConsumed">The number of input bytes consumed during the operation. This can be used to slice the input for subsequent calls, if necessary.</param>
        /// <param name="bytesWritten">The number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary.</param>
        /// <param name="isFinalBlock"><see langword="true"/> (default) when the input span contains the entire data to encode.
        /// Set to <see langword="true"/> when the source buffer contains the entirety of the data to encode.
        /// Set to <see langword="false"/> if this method is being called in a loop and if more input data may follow.
        /// At the end of the loop, call this (potentially with an empty source buffer) passing <see langword="true"/>.</param>
        /// <returns>It returns the OperationStatus enum values:
        /// - Done - on successful processing of the entire input span
        /// - DestinationTooSmall - if there is not enough space in the output span to fit the decoded input
        /// - NeedMoreData - only if <paramref name="isFinalBlock"/> is false and the input is not a multiple of 4
        /// - InvalidData - if the input contains bytes outside of the expected Base64Url range, or if it contains invalid/more than two padding characters
        /// or if the input is incomplete (i.e. the remainder of <paramref name="source"/> % 4 is 1) and <paramref name="isFinalBlock"/> is <see langword="true"/>.
        /// </returns>
        /// <remarks>
        /// As padding is optional the <paramref name="source"/> length not required to be a multiple of 4 even if <paramref name="isFinalBlock"/> is <see langword="true"/>.
        /// If the <paramref name="source"/> length is not a multiple of 4 and <paramref name="isFinalBlock"/> is <see langword="true"/> the remainders decoded accordingly:
        /// Remainder of 3 bytes - decoded into 2 bytes data, decoding succeeds.
        /// Remainder of 2 bytes - decoded into 1 byte data. decoding succeeds.
        /// Remainder of 1 byte - will cause OperationStatus.InvalidData result.
        /// </remarks>
        public static OperationStatus DecodeFromUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true) =>
            DecodeFrom<Base64UrlDecoderByte, byte>(source, destination, out bytesConsumed, out bytesWritten, isFinalBlock, ignoreWhiteSpace: true);

        /// <summary>
        /// Decode the span of UTF-8 encoded text in Base64Url (in-place) into binary data.
        /// The decoded binary output is smaller than the text data contained in the input (the operation deflates the data).
        /// </summary>
        /// <param name="buffer">The input span which contains the base 64 text data that needs to be decoded.</param>
        /// <returns>The number of bytes written into the <paramref name="buffer"/>. This can be used to slice the output for subsequent calls, if necessary.</returns>
        /// <exception cref="FormatException"><paramref name="buffer"/> contains a invalid Base64Url character,
        /// more than two padding characters, or a non-white space-character among the padding characters.</exception>
        /// <remarks>
        /// As padding is optional the input length not required to be a multiple of 4.
        /// If the input length is not a multiple of 4 the remainders decoded accordingly:
        /// Remainder of 3 bytes - decoded into 2 bytes data, decoding succeeds.
        /// Remainder of 2 bytes - decoded into 1 byte data. decoding succeeds.
        /// Remainder of 1 byte - is invalid input, causes FormatException.
        /// </remarks>
        public static int DecodeFromUtf8InPlace(Span<byte> buffer)
        {
            OperationStatus status = DecodeFromUtf8InPlace<Base64UrlDecoderByte>(buffer, out int bytesWritten, ignoreWhiteSpace: true);

            // Base64.DecodeFromUtf8InPlace returns OperationStatus, therefore doesn't throw.
            // For the Base64Url case I think it is better to throw to inform that invalid data found.
            if (OperationStatus.InvalidData == status)
            {
                throw new FormatException(SR.Format_BadBase64Char);
            }

            return bytesWritten;
        }

        /// <summary>
        /// Decode the span of UTF-8 encoded text represented as Base64Url into binary data.
        /// </summary>
        /// <param name="source">The input span which contains UTF-8 encoded text in Base64Url that needs to be decoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the decoded binary data.</param>
        /// <returns>The number of bytes written into the <paramref name="destination"/>. This can be used to slice the output for subsequent calls, if necessary.</returns>
        /// <exception cref="ArgumentException">Thrown when the encoded output cannot fit in the <paramref name="destination"/> provided.</exception>
        /// <exception cref="FormatException"><paramref name="source"/> contains a invalid Base64Url character,
        /// more than two padding characters, or a non-white space-character among the padding characters.</exception>
        /// <remarks>
        /// As padding is optional the input length not required to be a multiple of 4.
        /// If the input length is not a multiple of 4 the remainders decoded accordingly:
        /// Remainder of 3 bytes - decoded into 2 bytes data, decoding succeeds.
        /// Remainder of 2 bytes - decoded into 1 byte data. decoding succeeds.
        /// Remainder of 1 byte - is invalid input, causes FormatException.
        /// </remarks>
        public static int DecodeFromUtf8(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            OperationStatus status = DecodeFromUtf8(source, destination, out _, out int bytesWritten);

            if (OperationStatus.Done == status)
            {
                return bytesWritten;
            }

            if (OperationStatus.DestinationTooSmall == status)
            {
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
            }

            throw new FormatException(SR.Format_BadBase64Char);
        }

        /// <summary>
        /// Decode the span of UTF-8 encoded text represented as Base64Url into binary data.
        /// </summary>
        /// <param name="source">The input span which contains UTF-8 encoded text in Base64Url that needs to be decoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the decoded binary data.</param>
        /// <param name="bytesWritten">The number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary.</param>
        /// <returns><see langword="true"/> if bytes decoded successfully, otherwise <see langword="false"/>.</returns>
        public static bool TryDecodeFromUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            OperationStatus status = DecodeFromUtf8(source, destination, out _, out bytesWritten);

            return status == OperationStatus.Done;
        }

        /// <summary>
        /// Decode the span of UTF-8 encoded text represented as Base64Url into binary data.
        /// </summary>
        /// <param name="source">The input span which contains UTF-8 encoded text in Base64Url that needs to be decoded.</param>
        /// <returns>>A byte array which contains the result of the decoding operation.</returns>
        /// <exception cref="FormatException"><paramref name="source"/> contains a invalid Base64Url character,
        /// more than two padding characters, or a non-white space-character among the padding characters.</exception>
        public static byte[] DecodeFromUtf8(ReadOnlySpan<byte> source)
        {
            int upperBound = GetMaxDecodedLength(source.Length);
            Span<byte> destination = stackalloc byte[256];
            byte[]? rented = null;

            if (upperBound <= destination.Length)
            {
                destination = destination.Slice(0, upperBound);
            }
            else
            {
                rented = ArrayPool<byte>.Shared.Rent(upperBound);
                destination = rented.AsSpan(0, upperBound);
            }

            OperationStatus status = DecodeFromUtf8(source, destination, out _, out int bytesWritten);
            byte[] ret = destination.Slice(0, bytesWritten).ToArray();

            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }

            return OperationStatus.Done == status ? destination.Slice(0, bytesWritten).ToArray() :
                throw new FormatException(SR.Format_BadBase64Char);
        }

        /// <summary>
        /// Decode the span of UTF-8 encoded chars represented as Base64Url into binary data.
        /// </summary>
        /// <param name="source">The input span which contains UTF-8 encoded chars in Base64Url that needs to be decoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the decoded binary data.</param>
        /// <param name="charsConsumed">The number of input chars consumed during the operation. This can be used to slice the input for subsequent calls, if necessary.</param>
        /// <param name="bytesWritten">The number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary.</param>
        /// <param name="isFinalBlock"><see langword="true"/> (default) when the input span contains the entire data to encode.
        /// Set to <see langword="true"/> when the source buffer contains the entirety of the data to encode.
        /// Set to <see langword="false"/> if this method is being called in a loop and if more input data may follow.
        /// At the end of the loop, call this (potentially with an empty source buffer) passing <see langword="true"/>.</param>
        /// <returns>It returns the OperationStatus enum values:
        /// - Done - on successful processing of the entire input span
        /// - DestinationTooSmall - if there is not enough space in the output span to fit the decoded input
        /// - NeedMoreData - only if <paramref name="isFinalBlock"/> is false and the input is not a multiple of 4
        /// - InvalidData - if the input contains bytes outside of the expected Base64Url range, or if it contains invalid/more than two padding characters
        /// or if the input is incomplete (i.e. the remainder of <paramref name="source"/> % 4 is 1) and <paramref name="isFinalBlock"/> is <see langword="true"/>.
        /// </returns>
        /// <remarks>
        /// As padding is optional the <paramref name="source"/> length not required to be a multiple of 4 even if <paramref name="isFinalBlock"/> is <see langword="true"/>.
        /// If the <paramref name="source"/> length is not a multiple of 4 and <paramref name="isFinalBlock"/> is <see langword="true"/> the remainders decoded accordingly:
        /// Remainder of 3 chars - decoded into 2 bytes data, decoding succeeds.
        /// Remainder of 2 chars - decoded into 1 byte data. decoding succeeds.
        /// Remainder of 1 char - will cause OperationStatus.InvalidData result.
        /// </remarks>
        public static OperationStatus DecodeFromChars(ReadOnlySpan<char> source, Span<byte> destination,
            out int charsConsumed, out int bytesWritten, bool isFinalBlock = true) =>
            DecodeFrom<Base64UrlDecoderChar, ushort>(MemoryMarshal.Cast<char, ushort>(source), destination, out charsConsumed, out bytesWritten, isFinalBlock, ignoreWhiteSpace: true);

        private static OperationStatus DecodeWithWhiteSpaceBlockwise<TBase64Decoder>(ReadOnlySpan<ushort> utf8, Span<byte> bytes, ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock = true)
            where TBase64Decoder : IBase64Decoder<ushort>
        {
            const int BlockSize = 4;
            Span<ushort> buffer = stackalloc ushort[BlockSize];
            OperationStatus status = OperationStatus.Done;

            while (!utf8.IsEmpty)
            {
                int encodedIdx = 0;
                int bufferIdx = 0;
                int skipped = 0;

                for (; encodedIdx < utf8.Length && (uint)bufferIdx < (uint)buffer.Length; ++encodedIdx)
                {
                    if (IsWhiteSpace(utf8[encodedIdx]))
                    {
                        skipped++;
                    }
                    else
                    {
                        buffer[bufferIdx] = utf8[encodedIdx];
                        bufferIdx++;
                    }
                }

                utf8 = utf8.Slice(encodedIdx);
                bytesConsumed += skipped;

                if (bufferIdx == 0)
                {
                    continue;
                }

                bool hasAnotherBlock = utf8.Length >= BlockSize && bufferIdx == BlockSize;
                bool localIsFinalBlock = !hasAnotherBlock;

                // If this block contains padding and there's another block, then only whitespace may follow for being valid.
                if (hasAnotherBlock)
                {
                    int paddingCount = GetPaddingCount(ref buffer[^1]);
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

                status = DecodeFrom<TBase64Decoder, ushort>(buffer.Slice(0, bufferIdx), bytes, out int localConsumed, out int localWritten, localIsFinalBlock, ignoreWhiteSpace: false);
                bytesConsumed += localConsumed;
                bytesWritten += localWritten;

                if (status != OperationStatus.Done)
                {
                    return status;
                }

                // The remaining data must all be whitespace in order to be valid.
                if (!hasAnotherBlock)
                {
                    for (int i = 0; i < utf8.Length; ++i)
                    {
                        if (!IsWhiteSpace(utf8[i]))
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

            if (ptrToLastElement == EncodingPad) padding++;
            if (Unsafe.Subtract(ref ptrToLastElement, 1) == EncodingPad) padding++;

            return padding;
        }

        /// <summary>
        /// Decode the span of UTF-8 encoded chars represented as Base64Url into binary data.
        /// </summary>
        /// <param name="source">The input span which contains UTF-8 encoded chars in Base64Url that needs to be decoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the decoded binary data.</param>
        /// <returns>The number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary.</returns>
        /// <exception cref="ArgumentException">Thrown when the encoded output cannot fit in the <paramref name="destination"/> provided.</exception>
        /// <exception cref="FormatException"><paramref name="source"/> contains a invalid Base64Url character,
        /// more than two padding characters, or a non-white space-character among the padding characters.</exception>
        public static int DecodeFromChars(ReadOnlySpan<char> source, Span<byte> destination)
        {
            OperationStatus status = DecodeFromChars(source, destination, out _, out int bytesWritten);

            if (OperationStatus.Done == status)
            {
                return bytesWritten;
            }

            if (OperationStatus.DestinationTooSmall == status)
            {
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
            }

            throw new FormatException(SR.Format_BadBase64Char);
        }

        /// <summary>
        /// Decode the span of UTF-8 encoded chars represented as Base64Url into binary data.
        /// </summary>
        /// <param name="source">The input span which contains UTF-8 encoded chars in Base64Url that needs to be decoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the decoded binary data.</param>
        /// <param name="bytesWritten">The number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary.</param>
        /// <returns><see langword="true"/> if bytes decoded successfully, otherwise <see langword="false"/>.</returns>
        public static bool TryDecodeFromChars(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten)
        {
            OperationStatus status = DecodeFromChars(source, destination, out _, out bytesWritten);

            return OperationStatus.Done == status;
        }

        /// <summary>
        /// Decode the span of UTF-8 encoded chars represented as Base64Url into binary data.
        /// </summary>
        /// <param name="source">The input span which contains UTF-8 encoded chars in Base64Url that needs to be decoded.</param>
        /// <returns>A byte array which contains the result of the decoding operation.</returns>
        /// <exception cref="FormatException"><paramref name="source"/> contains a invalid Base64Url character,
        /// more than two padding characters, or a non-white space-character among the padding characters.</exception>
        public static byte[] DecodeFromChars(ReadOnlySpan<char> source)
        {
            int upperBound = GetMaxDecodedLength(source.Length);
            Span<byte> destination = stackalloc byte[256];
            byte[]? rented = null;

            if (upperBound <= destination.Length)
            {
                destination = destination.Slice(0, upperBound);
            }
            else
            {
                rented = ArrayPool<byte>.Shared.Rent(upperBound);
                destination = rented.AsSpan(0, upperBound);
            }

            OperationStatus status = DecodeFromChars(source, destination, out _, out int bytesWritten);
            byte[] ret = destination.Slice(0, bytesWritten).ToArray();

            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }

            return OperationStatus.Done == status ? destination.Slice(0, bytesWritten).ToArray() :
                throw new FormatException(SR.Format_BadBase64Char);
        }

        private readonly struct Base64UrlDecoderByte : IBase64Decoder<byte>
        {
            public static ReadOnlySpan<sbyte> DecodingMap =>
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

            public static ReadOnlySpan<uint> VbmiLookup0 =>
                [
                    0x80808080, 0x80808080, 0x80808080, 0x80808080,
                    0x80808080, 0x80808080, 0x80808080, 0x80808080,
                    0x80808080, 0x80808080, 0x80808080, 0x80803e80,
                    0x37363534, 0x3b3a3938, 0x80803d3c, 0x80808080
                ];

            public static ReadOnlySpan<uint> VbmiLookup1 =>
                [
                    0x02010080, 0x06050403, 0x0a090807, 0x0e0d0c0b,
                    0x1211100f, 0x16151413, 0x80191817, 0x3f808080,
                    0x1c1b1a80, 0x201f1e1d, 0x24232221, 0x28272625,
                    0x2c2b2a29, 0x302f2e2d, 0x80333231, 0x80808080
                ];

            public static ReadOnlySpan<sbyte> Avx2LutHigh =>
                [
                    0x00, 0x00, 0x2d, 0x39,
                    0x4f, 0x5a, 0x6f, 0x7a,
                    0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x2d, 0x39,
                    0x4f, 0x5a, 0x6f, 0x7a,
                    0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00
                ];

            public static ReadOnlySpan<sbyte> Avx2LutLow =>
                [
                    0x01, 0x01, 0x2d, 0x30,
                    0x41, 0x50, 0x61, 0x70,
                    0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x2d, 0x30,
                    0x41, 0x50, 0x61, 0x70,
                    0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x01, 0x01
                ];

            public static ReadOnlySpan<sbyte> Avx2LutShift =>
                [
                    0,   0,  17,   4,
                  -65, -65, -71, -71,
                    0,   0,   0,   0,
                    0,   0,   0,   0,
                    0,   0,  17,   4,
                  -65, -65, -71, -71,
                    0,   0,   0,   0,
                    0,   0,   0,   0
                ];

            public static byte MaskSlashOrUnderscore => (byte)'_'; // underscore

            public static ReadOnlySpan<int> Vector128LutHigh => [0x392d0000, 0x7a6f5a4f, 0x00000000, 0x00000000];

            public static ReadOnlySpan<int> Vector128LutLow => [0x302d0101, 0x70615041, 0x01010101, 0x01010101];

            public static ReadOnlySpan<uint> Vector128LutShift => [0x04110000, 0xb9b9bfbf, 0x00000000, 0x00000000];

            public static ReadOnlySpan<uint> AdvSimdLutOne3 => [0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFF3EFF];

            public static uint AdvSimdLutTwo3Uint1 => 0x1B1AFF3F;

            public static int GetMaxDecodedLength(int utf8Length) => Base64Url.GetMaxDecodedLength(utf8Length);

            public static bool IsInValidLength(int bufferLength) => bufferLength % 4 == 1; // One byte cannot be decoded completely

            public static int SrcLength(bool isFinalBlock, int utf8Length) => isFinalBlock ? utf8Length : utf8Length & ~0x3;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            [CompExactlyDependsOn(typeof(Ssse3))]
            public static bool TryDecode128Core(
                Vector128<byte> str,
                Vector128<byte> hiNibbles,
                Vector128<byte> maskSlashOrUnderscore,
                Vector128<byte> mask8F,
                Vector128<byte> lutLow,
                Vector128<byte> lutHigh,
                Vector128<sbyte> lutShift,
                Vector128<byte> shiftForUnderscore,
                out Vector128<byte> result)
            {
                Vector128<byte> lowerBound = SimdShuffle(lutLow, hiNibbles, mask8F);
                Vector128<byte> upperBound = SimdShuffle(lutHigh, hiNibbles, mask8F);

                Vector128<byte> below = Vector128.LessThan(str, lowerBound);
                Vector128<byte> above = Vector128.GreaterThan(str, upperBound);
                Vector128<byte> eq5F = Vector128.Equals(str, maskSlashOrUnderscore);

                // Take care as arguments are flipped in order!
                Vector128<byte> outside = Vector128.AndNot(below | above, eq5F);

                if (outside != Vector128<byte>.Zero)
                {
                    result = default;
                    return false;
                }

                Vector128<byte> shift = SimdShuffle(lutShift.AsByte(), hiNibbles, mask8F);
                str += shift;

                result = str + (eq5F & shiftForUnderscore);
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(Avx2))]
            public static bool TryDecode256Core(
                Vector256<sbyte> str,
                Vector256<sbyte> hiNibbles,
                Vector256<sbyte> maskSlashOrUnderscore,
                Vector256<sbyte> lutLow,
                Vector256<sbyte> lutHigh,
                Vector256<sbyte> lutShift,
                Vector256<sbyte> shiftForUnderscore,
                out Vector256<sbyte> result)
            {
                Vector256<sbyte> lowerBound = Avx2.Shuffle(lutLow, hiNibbles);
                Vector256<sbyte> upperBound = Avx2.Shuffle(lutHigh, hiNibbles);

                Vector256<sbyte> below = Vector256.LessThan(str, lowerBound);
                Vector256<sbyte> above = Vector256.GreaterThan(str, upperBound);
                Vector256<sbyte> eq5F = Vector256.Equals(str, maskSlashOrUnderscore);

                // Take care as arguments are flipped in order!
                Vector256<sbyte> outside = Vector256.AndNot(below | above, eq5F);

                if (outside != Vector256<sbyte>.Zero)
                {
                    result = default;
                    return false;
                }

                Vector256<sbyte> shift = Avx2.Shuffle(lutShift, hiNibbles);
                str += shift;

                result = str + (eq5F & shiftForUnderscore);
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe int Decode(byte* encodedBytes, ref sbyte decodingMap)
            {
                uint t0 = encodedBytes[0];
                uint t1 = encodedBytes[1];
                uint t2 = encodedBytes[2];
                uint t3 = encodedBytes[3];

                if (((t0 | t1 | t2 | t3) & 0xffffff00) != 0)
                {
                    return -1; // One or more chars falls outside the 00..ff range, invalid Base64Url character.
                }

                int i0 = Unsafe.Add(ref decodingMap, t0);
                int i1 = Unsafe.Add(ref decodingMap, t1);
                int i2 = Unsafe.Add(ref decodingMap, t2);
                int i3 = Unsafe.Add(ref decodingMap, t3);

                i0 <<= 18;
                i1 <<= 12;
                i2 <<= 6;

                i0 |= i3;
                i1 |= i2;

                i0 |= i1;
                return i0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe int DecodeRemaining(byte* srcEnd, ref sbyte decodingMap, long remaining, out uint t2, out uint t3)
            {
                uint t0;
                uint t1;
                t2 = EncodingPad;
                t3 = EncodingPad;
                switch (remaining)
                {
                    case 2:
                        t0 = srcEnd[-2];
                        t1 = srcEnd[-1];
                        break;
                    case 3:
                        t0 = srcEnd[-3];
                        t1 = srcEnd[-2];
                        t2 = srcEnd[-1];
                        break;
                    case 4:
                        t0 = srcEnd[-4];
                        t1 = srcEnd[-3];
                        t2 = srcEnd[-2];
                        t3 = srcEnd[-1];
                        break;
                    default:
                        return -1;
                }

                if (((t0 | t1 | t2 | t3) & 0xffffff00) != 0)
                {
                    return -1;
                }

                int i0 = Unsafe.Add(ref decodingMap, (IntPtr)t0);
                int i1 = Unsafe.Add(ref decodingMap, (IntPtr)t1);

                i0 <<= 18;
                i1 <<= 12;

                i0 |= i1;
                return i0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int IndexOfAnyExceptWhiteSpace(ReadOnlySpan<byte> span)
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
            public static OperationStatus DecodeWithWhiteSpaceBlockwiseWrapper<TBase64Decoder>(ReadOnlySpan<byte> utf8, Span<byte> bytes,
                ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock = true)
                where TBase64Decoder : IBase64Decoder<byte>
            {
                return Base64.DecodeWithWhiteSpaceBlockwise<TBase64Decoder>(utf8, bytes, ref bytesConsumed, ref bytesWritten, isFinalBlock);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe bool TryLoadVector512(byte* src, byte* srcStart, int sourceLength, out Vector512<sbyte> str)
            {
                Base64.AssertRead<Vector512<sbyte>>(src, srcStart, sourceLength);
                str = Vector512.Load(src).AsSByte();
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(Avx2))]
            public static unsafe bool TryLoadAvxVector256(byte* src, byte* srcStart, int sourceLength, out Vector256<sbyte> str)
            {
                Base64.AssertRead<Vector256<sbyte>>(src, srcStart, sourceLength);
                str = Avx.LoadVector256(src).AsSByte();
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe bool TryLoadVector128(byte* src, byte* srcStart, int sourceLength, out Vector128<byte> str)
            {
                Base64.AssertRead<Vector128<sbyte>>(src, srcStart, sourceLength);
                str =  Vector128.LoadUnsafe(ref *src);
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            public static unsafe bool TryLoadArmVector128x4(byte* src, byte* srcStart, int sourceLength,
                out Vector128<byte> str1, out Vector128<byte> str2, out Vector128<byte> str3, out Vector128<byte> str4)
            {
                AssertRead<Vector128<byte>>(src, srcStart, sourceLength);
                (str1, str2, str3, str4) = AdvSimd.Arm64.LoadVector128x4AndUnzip(src);

                return true;
            }
        }

        private readonly struct Base64UrlDecoderChar : IBase64Decoder<ushort>
        {
            public static ReadOnlySpan<sbyte> DecodingMap =>
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

            public static ReadOnlySpan<uint> VbmiLookup0 =>
                [
                    0x80808080, 0x80808080, 0x80808080, 0x80808080,
                    0x80808080, 0x80808080, 0x80808080, 0x80808080,
                    0x80808080, 0x80808080, 0x80808080, 0x80803e80,
                    0x37363534, 0x3b3a3938, 0x80803d3c, 0x80808080
                ];

            public static ReadOnlySpan<uint> VbmiLookup1 =>
                [
                    0x02010080, 0x06050403, 0x0a090807, 0x0e0d0c0b,
                    0x1211100f, 0x16151413, 0x80191817, 0x3f808080,
                    0x1c1b1a80, 0x201f1e1d, 0x24232221, 0x28272625,
                    0x2c2b2a29, 0x302f2e2d, 0x80333231, 0x80808080
                ];

            public static ReadOnlySpan<sbyte> Avx2LutHigh =>
                [
                    0x00, 0x00, 0x2d, 0x39,
                    0x4f, 0x5a, 0x6f, 0x7a,
                    0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x2d, 0x39,
                    0x4f, 0x5a, 0x6f, 0x7a,
                    0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00
                ];

            public static ReadOnlySpan<sbyte> Avx2LutLow =>
                [
                    0x01, 0x01, 0x2d, 0x30,
                    0x41, 0x50, 0x61, 0x70,
                    0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x2d, 0x30,
                    0x41, 0x50, 0x61, 0x70,
                    0x01, 0x01, 0x01, 0x01,
                    0x01, 0x01, 0x01, 0x01
                ];

            public static ReadOnlySpan<sbyte> Avx2LutShift =>
                [
                    0,   0,  17,   4,
                  -65, -65, -71, -71,
                    0,   0,   0,   0,
                    0,   0,   0,   0,
                    0,   0,  17,   4,
                  -65, -65, -71, -71,
                    0,   0,   0,   0,
                    0,   0,   0,   0
                ];

            public static byte MaskSlashOrUnderscore => (byte)'_'; // underscore

            public static ReadOnlySpan<int> Vector128LutHigh => [0x392d0000, 0x7a6f5a4f, 0x00000000, 0x00000000];

            public static ReadOnlySpan<int> Vector128LutLow => [0x302d0101, 0x70615041, 0x01010101, 0x01010101];

            public static ReadOnlySpan<uint> Vector128LutShift => [0x04110000, 0xb9b9bfbf, 0x00000000, 0x00000000];

            public static ReadOnlySpan<uint> AdvSimdLutOne3 => [0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFF3EFF];

            public static uint AdvSimdLutTwo3Uint1 => 0x1B1AFF3F;

            public static int GetMaxDecodedLength(int utf8Length) => Base64Url.GetMaxDecodedLength(utf8Length);

            public static bool IsInValidLength(int bufferLength) => bufferLength % 4 == 1; // One byte cannot be decoded completely

            public static int SrcLength(bool isFinalBlock, int utf8Length) => isFinalBlock ? utf8Length : utf8Length & ~0x3;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            [CompExactlyDependsOn(typeof(Ssse3))]
            public static bool TryDecode128Core(
                Vector128<byte> str,
                Vector128<byte> hiNibbles,
                Vector128<byte> maskSlashOrUnderscore,
                Vector128<byte> mask8F,
                Vector128<byte> lutLow,
                Vector128<byte> lutHigh,
                Vector128<sbyte> lutShift,
                Vector128<byte> shiftForUnderscore,
                out Vector128<byte> result)
            {
                Vector128<byte> lowerBound = SimdShuffle(lutLow, hiNibbles, mask8F);
                Vector128<byte> upperBound = SimdShuffle(lutHigh, hiNibbles, mask8F);

                Vector128<byte> below = Vector128.LessThan(str, lowerBound);
                Vector128<byte> above = Vector128.GreaterThan(str, upperBound);
                Vector128<byte> eq5F = Vector128.Equals(str, maskSlashOrUnderscore);

                // Take care as arguments are flipped in order!
                Vector128<byte> outside = Vector128.AndNot(below | above, eq5F);

                if (outside != Vector128<byte>.Zero)
                {
                    result = default;
                    return false;
                }

                Vector128<byte> shift = SimdShuffle(lutShift.AsByte(), hiNibbles, mask8F);
                str += shift;

                result = str + (eq5F & shiftForUnderscore);
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(Avx2))]
            public static bool TryDecode256Core(
                Vector256<sbyte> str,
                Vector256<sbyte> hiNibbles,
                Vector256<sbyte> maskSlashOrUnderscore,
                Vector256<sbyte> lutLow,
                Vector256<sbyte> lutHigh,
                Vector256<sbyte> lutShift,
                Vector256<sbyte> shiftForUnderscore,
                out Vector256<sbyte> result)
            {
                Vector256<sbyte> lowerBound = Avx2.Shuffle(lutLow, hiNibbles);
                Vector256<sbyte> upperBound = Avx2.Shuffle(lutHigh, hiNibbles);

                Vector256<sbyte> below = Vector256.LessThan(str, lowerBound);
                Vector256<sbyte> above = Vector256.GreaterThan(str, upperBound);
                Vector256<sbyte> eq5F = Vector256.Equals(str, maskSlashOrUnderscore);

                // Take care as arguments are flipped in order!
                Vector256<sbyte> outside = Vector256.AndNot(below | above, eq5F);

                if (outside != Vector256<sbyte>.Zero)
                {
                    result = default;
                    return false;
                }

                Vector256<sbyte> shift = Avx2.Shuffle(lutShift, hiNibbles);
                str += shift;

                result = str + (eq5F & shiftForUnderscore);
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe int Decode(ushort* encodedBytes, ref sbyte decodingMap)
            {
                uint t0 = encodedBytes[0];
                uint t1 = encodedBytes[1];
                uint t2 = encodedBytes[2];
                uint t3 = encodedBytes[3];

                if (((t0 | t1 | t2 | t3) & 0xffffff00) != 0)
                {
                    return -1; // One or more chars falls outside the 00..ff range, invalid Base64Url character.
                }

                int i0 = Unsafe.Add(ref decodingMap, t0);
                int i1 = Unsafe.Add(ref decodingMap, t1);
                int i2 = Unsafe.Add(ref decodingMap, t2);
                int i3 = Unsafe.Add(ref decodingMap, t3);

                i0 <<= 18;
                i1 <<= 12;
                i2 <<= 6;

                i0 |= i3;
                i1 |= i2;

                i0 |= i1;
                return i0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe int DecodeRemaining(ushort* srcEnd, ref sbyte decodingMap, long remaining, out uint t2, out uint t3)
            {
                uint t0;
                uint t1;
                t2 = EncodingPad;
                t3 = EncodingPad;
                switch (remaining)
                {
                    case 2:
                        t0 = srcEnd[-2];
                        t1 = srcEnd[-1];
                        break;
                    case 3:
                        t0 = srcEnd[-3];
                        t1 = srcEnd[-2];
                        t2 = srcEnd[-1];
                        break;
                    case 4:
                        t0 = srcEnd[-4];
                        t1 = srcEnd[-3];
                        t2 = srcEnd[-2];
                        t3 = srcEnd[-1];
                        break;
                    default:
                        return -1;
                }

                if (((t0 | t1 | t2 | t3) & 0xffffff00) != 0)
                {
                    return -1;
                }

                int i0 = Unsafe.Add(ref decodingMap, (IntPtr)t0);
                int i1 = Unsafe.Add(ref decodingMap, (IntPtr)t1);

                i0 <<= 18;
                i1 <<= 12;

                i0 |= i1;
                return i0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int IndexOfAnyExceptWhiteSpace(ReadOnlySpan<ushort> span)
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
            public static OperationStatus DecodeWithWhiteSpaceBlockwiseWrapper<TBase64Decoder>(ReadOnlySpan<ushort> utf8, Span<byte> bytes,
                ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock = true)
                where TBase64Decoder : IBase64Decoder<ushort>
            {
                return DecodeWithWhiteSpaceBlockwise<TBase64Decoder>(utf8, bytes, ref bytesConsumed, ref bytesWritten, isFinalBlock);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe bool TryLoadVector512(ushort* src, ushort* srcStart, int sourceLength, out Vector512<sbyte> str)
            {
                AssertRead<Vector512<ushort>>(src, srcStart, sourceLength);
                Vector512<ushort> utf16VectorLower = Vector512.Load(src);
                Vector512<ushort> utf16VectorUpper = Vector512.Load(src + 32);

                if (VectorContainsNonAsciiChar(utf16VectorLower) || VectorContainsNonAsciiChar(utf16VectorUpper))
                {
                    str = default;
                    return false;
                }

                str = Vector512.Narrow(utf16VectorLower, utf16VectorUpper).AsSByte();
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(Avx2))]
            public static unsafe bool TryLoadAvxVector256(ushort* src, ushort* srcStart, int sourceLength, out Vector256<sbyte> str)
            {
                AssertRead<Vector256<sbyte>>(src, srcStart, sourceLength);
                Vector256<ushort> utf16VectorLower = Avx.LoadVector256(src);
                Vector256<ushort> utf16VectorUpper = Avx.LoadVector256(src + 16);

                if (VectorContainsNonAsciiChar(utf16VectorLower) || VectorContainsNonAsciiChar(utf16VectorUpper))
                {
                    str = default;
                    return false;
                }

                str = Vector256.Narrow(utf16VectorLower, utf16VectorUpper).AsSByte();
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe bool TryLoadVector128(ushort* src, ushort* srcStart, int sourceLength, out Vector128<byte> str)
            {
                AssertRead<Vector128<sbyte>>(src, srcStart, sourceLength);
                Vector128<ushort> utf16VectorLower = Vector128.LoadUnsafe(ref *src);
                Vector128<ushort> utf16VectorUpper = Vector128.LoadUnsafe(ref *src, 8);
                if (VectorContainsNonAsciiChar(utf16VectorLower) || VectorContainsNonAsciiChar(utf16VectorUpper))
                {
                    str = default;
                    return false;
                }

                str = Vector128.Narrow(utf16VectorLower, utf16VectorUpper);
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            public static unsafe bool TryLoadArmVector128x4(ushort* src, ushort* srcStart, int sourceLength,
                out Vector128<byte> str1, out Vector128<byte> str2, out Vector128<byte> str3, out Vector128<byte> str4)
            {
                AssertRead<Vector128<sbyte>>(src, srcStart, sourceLength);
                var (s11, s12, s21, s22) = AdvSimd.Arm64.LoadVector128x4AndUnzip(src);
                var (s31, s32, s41, s42) = AdvSimd.Arm64.LoadVector128x4AndUnzip(src + 32);

                if (VectorContainsNonAsciiChar(s11) || VectorContainsNonAsciiChar(s12) ||
                    VectorContainsNonAsciiChar(s21) || VectorContainsNonAsciiChar(s22) ||
                    VectorContainsNonAsciiChar(s31) || VectorContainsNonAsciiChar(s32) ||
                    VectorContainsNonAsciiChar(s41) || VectorContainsNonAsciiChar(s42))
                {
                    str1 = str2 = str3 = str4 = default;
                    return false;
                }

                str1 = AdvSimd.Arm64.UnzipEven(s11.AsByte(), s12.AsByte());
                str2 = AdvSimd.Arm64.UnzipEven(s21.AsByte(), s22.AsByte());
                str3 = AdvSimd.Arm64.UnzipEven(s31.AsByte(), s32.AsByte());
                str4 = AdvSimd.Arm64.UnzipEven(s41.AsByte(), s42.AsByte());

                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool VectorContainsNonAsciiChar(Vector512<ushort> utf16Vector)
            {
                const ushort asciiMask = ushort.MaxValue - 127; // 0xFF80
                Vector512<ushort> zeroIsAscii = utf16Vector & Vector512.Create(asciiMask);
                // If a non-ASCII bit is set in any WORD of the vector, we have seen non-ASCII data.
                return zeroIsAscii != Vector512<ushort>.Zero;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool VectorContainsNonAsciiChar(Vector256<ushort> utf16Vector)
            {
                if (Avx.IsSupported)
                {
                    Vector256<ushort> asciiMaskForTestZ = Vector256.Create((ushort)0xFF80);
                    return !Avx.TestZ(utf16Vector.AsInt16(), asciiMaskForTestZ.AsInt16());
                }
                else
                {
                    const ushort asciiMask = ushort.MaxValue - 127; // 0xFF80
                    Vector256<ushort> zeroIsAscii = utf16Vector & Vector256.Create(asciiMask);
                    // If a non-ASCII bit is set in any WORD of the vector, we have seen non-ASCII data.
                    return zeroIsAscii != Vector256<ushort>.Zero;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool VectorContainsNonAsciiChar(Vector128<ushort> utf16Vector)
            {
                // prefer architecture specific intrinsic as they offer better perf
                if (Sse2.IsSupported)
                {
                    if (Sse41.IsSupported)
                    {
                        Vector128<ushort> asciiMaskForTestZ = Vector128.Create((ushort)0xFF80);
                        // If a non-ASCII bit is set in any WORD of the vector, we have seen non-ASCII data.
                        return !Sse41.TestZ(utf16Vector.AsInt16(), asciiMaskForTestZ.AsInt16());
                    }
                    else
                    {
                        Vector128<ushort> asciiMaskForAddSaturate = Vector128.Create((ushort)0x7F80);
                        // The operation below forces the 0x8000 bit of each WORD to be set iff the WORD element
                        // has value >= 0x0800 (non-ASCII). Then we'll treat the vector as a BYTE vector in order
                        // to extract the mask. Reminder: the 0x0080 bit of each WORD should be ignored.
                        return (Sse2.MoveMask(Sse2.AddSaturate(utf16Vector, asciiMaskForAddSaturate).AsByte()) & 0b_1010_1010_1010_1010) != 0;
                    }
                }
                else if (AdvSimd.Arm64.IsSupported)
                {
                    // First we pick four chars, a larger one from all four pairs of adjecent chars in the vector.
                    // If any of those four chars has a non-ASCII bit set, we have seen non-ASCII data.
                    Vector128<ushort> maxChars = AdvSimd.Arm64.MaxPairwise(utf16Vector, utf16Vector);
                    return (maxChars.AsUInt64().ToScalar() & 0xFF80FF80FF80FF80) != 0;
                }
                else
                {
                    const ushort asciiMask = ushort.MaxValue - 127; // 0xFF80
                    Vector128<ushort> zeroIsAscii = utf16Vector & Vector128.Create(asciiMask);
                    // If a non-ASCII bit is set in any WORD of the vector, we have seen non-ASCII data.
                    return zeroIsAscii != Vector128<ushort>.Zero;
                }
            }
        }
    }
}
