// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NET
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
#endif
using System.Text;
using static System.Buffers.Text.Base64Helper;

namespace System.Buffers.Text
{
    // AVX2 and Vector128 version based on https://github.com/gfoidl/Base64/blob/5383320e28cac6c7ac6f86502fb05d23a048a21d/source/gfoidl.Base64/Internal/Encodings/Base64UrlEncoding.cs

    public static partial class Base64Url
    {
        private const int MaxStackallocThreshold = 256;

        /// <summary>
        /// Returns the maximum length (in bytes) of the result if you were to decode base 64 encoded text from a span of size <paramref name="base64Length"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The specified <paramref name="base64Length"/> is less than 0.
        /// </exception>
        public static int GetMaxDecodedLength(int base64Length)
        {
#if NET
            ArgumentOutOfRangeException.ThrowIfNegative(base64Length);

            (uint whole, uint remainder) = uint.DivRem((uint)base64Length, 4);

            return (int)(whole * 3 + (remainder > 0 ? remainder - 1 : 0));
#else
            if (base64Length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(base64Length));
            }

            int remainder = (int)((uint)base64Length % 4);

            return (base64Length >> 2) * 3 + (remainder > 0 ? remainder - 1 : 0);
#endif
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
            DecodeFrom(default(Base64UrlDecoderByte), source, destination, out bytesConsumed, out bytesWritten, isFinalBlock, ignoreWhiteSpace: true);

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
            OperationStatus status = DecodeFromUtf8InPlace<Base64UrlDecoderByte>(default, buffer, out int bytesWritten, ignoreWhiteSpace: true);

            // Base64.DecodeFromUtf8InPlace returns OperationStatus, therefore doesn't throw.
            // For Base64Url, this is not an OperationStatus API and thus throws.
            if (status == OperationStatus.InvalidData)
            {
                throw new FormatException(SR.Format_BadBase64Char);
            }

            Debug.Assert(status is OperationStatus.Done);
            return bytesWritten;
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
            DecodeFrom(default(Base64UrlDecoderChar), MemoryMarshal.Cast<char, ushort>(source), destination,
                out charsConsumed, out bytesWritten, isFinalBlock, ignoreWhiteSpace: true);

        private static OperationStatus DecodeWithWhiteSpaceBlockwise<TBase64Decoder>(TBase64Decoder decoder,
            ReadOnlySpan<ushort> source, Span<byte> bytes, ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock = true)
            where TBase64Decoder : IBase64Decoder<ushort>
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

                bool hasAnotherBlock;

                if (decoder is Base64DecoderByte)
                {
                    hasAnotherBlock = source.Length >= BlockSize;
                }
                else
                {
                    hasAnotherBlock = source.Length > 1;
                }

                bool localIsFinalBlock = !hasAnotherBlock;

                // If this block contains padding and there's another block, then only whitespace may follow for being valid.
                if (hasAnotherBlock)
                {
                    int paddingCount = GetPaddingCount<TBase64Decoder>(decoder, ref buffer[BlockSize - 1]);
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

                status = DecodeFrom<TBase64Decoder, ushort>(decoder, buffer.Slice(0, bufferIdx), bytes, out int localConsumed, out int localWritten, localIsFinalBlock, ignoreWhiteSpace: false);
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
        private static int GetPaddingCount<TBase64Decoder>(TBase64Decoder decoder, ref ushort ptrToLastElement)
            where TBase64Decoder : IBase64Decoder<ushort>
        {
            int padding = 0;

            if (decoder.IsValidPadding(ptrToLastElement))
            {
                padding++;
            }

            if (decoder.IsValidPadding(Unsafe.Subtract(ref ptrToLastElement, 1)))
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

        private readonly struct Base64UrlDecoderByte : IBase64Decoder<byte>
        {
            public ReadOnlySpan<sbyte> DecodingMap =>
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

            public ReadOnlySpan<uint> VbmiLookup0 =>
                [
                    0x80808080, 0x80808080, 0x80808080, 0x80808080,
                    0x80808080, 0x80808080, 0x80808080, 0x80808080,
                    0x80808080, 0x80808080, 0x80808080, 0x80803e80,
                    0x37363534, 0x3b3a3938, 0x80803d3c, 0x80808080
                ];

            public ReadOnlySpan<uint> VbmiLookup1 =>
                [
                    0x02010080, 0x06050403, 0x0a090807, 0x0e0d0c0b,
                    0x1211100f, 0x16151413, 0x80191817, 0x3f808080,
                    0x1c1b1a80, 0x201f1e1d, 0x24232221, 0x28272625,
                    0x2c2b2a29, 0x302f2e2d, 0x80333231, 0x80808080
                ];

            public ReadOnlySpan<sbyte> Avx2LutHigh =>
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

            public ReadOnlySpan<sbyte> Avx2LutLow =>
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

            public ReadOnlySpan<sbyte> Avx2LutShift =>
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

            public byte MaskSlashOrUnderscore => (byte)'_'; // underscore

            public ReadOnlySpan<int> Vector128LutHigh => [0x392d0000, 0x7a6f5a4f, 0x00000000, 0x00000000];

            public ReadOnlySpan<int> Vector128LutLow => [0x302d0101, 0x70615041, 0x01010101, 0x01010101];

            public ReadOnlySpan<uint> Vector128LutShift => [0x04110000, 0xb9b9bfbf, 0x00000000, 0x00000000];

            public ReadOnlySpan<uint> AdvSimdLutOne3 => [0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFF3EFF];

            public uint AdvSimdLutTwo3Uint1 => 0x1B1AFF3F;

            public int GetMaxDecodedLength(int sourceLength) => Base64Url.GetMaxDecodedLength(sourceLength);

            public bool IsInvalidLength(int bufferLength) => (bufferLength & 3) == 1; // One byte cannot be decoded completely

            public bool IsValidPadding(uint padChar) => padChar is EncodingPad or UrlEncodingPad;

            public int SrcLength(bool isFinalBlock, int sourceLength) => isFinalBlock ? sourceLength : sourceLength & ~0x3;

#if NET
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            [CompExactlyDependsOn(typeof(Ssse3))]
            public bool TryDecode128Core(
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
            public bool TryDecode256Core(
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
            public unsafe bool TryLoadVector512(byte* src, byte* srcStart, int sourceLength, out Vector512<sbyte> str) =>
                default(Base64DecoderByte).TryLoadVector512(src, srcStart, sourceLength, out str);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(Avx2))]
            public unsafe bool TryLoadAvxVector256(byte* src, byte* srcStart, int sourceLength, out Vector256<sbyte> str) =>
                default(Base64DecoderByte).TryLoadAvxVector256(src, srcStart, sourceLength, out str);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe bool TryLoadVector128(byte* src, byte* srcStart, int sourceLength, out Vector128<byte> str) =>
                default(Base64DecoderByte).TryLoadVector128(src, srcStart, sourceLength, out str);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            public unsafe bool TryLoadArmVector128x4(byte* src, byte* srcStart, int sourceLength,
                out Vector128<byte> str1, out Vector128<byte> str2, out Vector128<byte> str3, out Vector128<byte> str4) =>
                default(Base64DecoderByte).TryLoadArmVector128x4(src, srcStart, sourceLength, out str1, out str2, out str3, out str4);
#endif

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe int DecodeFourElements(byte* source, ref sbyte decodingMap) =>
                default(Base64DecoderByte).DecodeFourElements(source, ref decodingMap);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe int DecodeRemaining(byte* srcEnd, ref sbyte decodingMap, long remaining, out uint t2, out uint t3) =>
                default(Base64DecoderByte).DecodeRemaining(srcEnd, ref decodingMap, remaining, out t2, out t3);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int IndexOfAnyExceptWhiteSpace(ReadOnlySpan<byte> span) => default(Base64DecoderByte).IndexOfAnyExceptWhiteSpace(span);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public OperationStatus DecodeWithWhiteSpaceBlockwiseWrapper<TBase64Decoder>(TBase64Decoder decoder, ReadOnlySpan<byte> utf8, Span<byte> bytes,
                ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock = true) where TBase64Decoder : IBase64Decoder<byte> =>
                Base64Helper.DecodeWithWhiteSpaceBlockwise(decoder, utf8, bytes, ref bytesConsumed, ref bytesWritten, isFinalBlock);
        }

        private readonly struct Base64UrlDecoderChar : IBase64Decoder<ushort>
        {
            public ReadOnlySpan<sbyte> DecodingMap => default(Base64UrlDecoderByte).DecodingMap;

            public ReadOnlySpan<uint> VbmiLookup0 => default(Base64UrlDecoderByte).VbmiLookup0;

            public ReadOnlySpan<uint> VbmiLookup1 => default(Base64UrlDecoderByte).VbmiLookup1;

            public ReadOnlySpan<sbyte> Avx2LutHigh => default(Base64UrlDecoderByte).Avx2LutHigh;

            public ReadOnlySpan<sbyte> Avx2LutLow => default(Base64UrlDecoderByte).Avx2LutLow;

            public ReadOnlySpan<sbyte> Avx2LutShift => default(Base64UrlDecoderByte).Avx2LutShift;

            public byte MaskSlashOrUnderscore => default(Base64UrlDecoderByte).MaskSlashOrUnderscore;

            public ReadOnlySpan<int> Vector128LutHigh => default(Base64UrlDecoderByte).Vector128LutHigh;

            public ReadOnlySpan<int> Vector128LutLow => default(Base64UrlDecoderByte).Vector128LutLow;

            public ReadOnlySpan<uint> Vector128LutShift => default(Base64UrlDecoderByte).Vector128LutShift;

            public ReadOnlySpan<uint> AdvSimdLutOne3 => default(Base64UrlDecoderByte).AdvSimdLutOne3;

            public uint AdvSimdLutTwo3Uint1 => default(Base64UrlDecoderByte).AdvSimdLutTwo3Uint1;

            public int GetMaxDecodedLength(int sourceLength) => default(Base64UrlDecoderByte).GetMaxDecodedLength(sourceLength);

            public bool IsInvalidLength(int bufferLength) => default(Base64UrlDecoderByte).IsInvalidLength(bufferLength);

            public bool IsValidPadding(uint padChar) => default(Base64UrlDecoderByte).IsValidPadding(padChar);

            public int SrcLength(bool isFinalBlock, int sourceLength) => default(Base64UrlDecoderByte).SrcLength(isFinalBlock, sourceLength);

#if NET
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            [CompExactlyDependsOn(typeof(Ssse3))]
            public bool TryDecode128Core(Vector128<byte> str, Vector128<byte> hiNibbles, Vector128<byte> maskSlashOrUnderscore, Vector128<byte> mask8F,
                Vector128<byte> lutLow, Vector128<byte> lutHigh, Vector128<sbyte> lutShift, Vector128<byte> shiftForUnderscore, out Vector128<byte> result) =>
                default(Base64UrlDecoderByte).TryDecode128Core(str, hiNibbles, maskSlashOrUnderscore, mask8F, lutLow, lutHigh, lutShift, shiftForUnderscore, out result);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(Avx2))]
            public bool TryDecode256Core(Vector256<sbyte> str, Vector256<sbyte> hiNibbles, Vector256<sbyte> maskSlashOrUnderscore, Vector256<sbyte> lutLow,
                Vector256<sbyte> lutHigh, Vector256<sbyte> lutShift, Vector256<sbyte> shiftForUnderscore, out Vector256<sbyte> result) =>
                default(Base64UrlDecoderByte).TryDecode256Core(str, hiNibbles, maskSlashOrUnderscore, lutLow, lutHigh, lutShift, shiftForUnderscore, out result);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe bool TryLoadVector512(ushort* src, ushort* srcStart, int sourceLength, out Vector512<sbyte> str)
            {
                AssertRead<Vector512<ushort>>(src, srcStart, sourceLength);
                Vector512<ushort> utf16VectorLower = Vector512.Load(src);
                Vector512<ushort> utf16VectorUpper = Vector512.Load(src + 32);

                if (Ascii.VectorContainsNonAsciiChar(utf16VectorLower | utf16VectorUpper))
                {
                    str = default;
                    return false;
                }

                str = Vector512.Narrow(utf16VectorLower, utf16VectorUpper).AsSByte();
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(Avx2))]
            public unsafe bool TryLoadAvxVector256(ushort* src, ushort* srcStart, int sourceLength, out Vector256<sbyte> str)
            {
                AssertRead<Vector256<sbyte>>(src, srcStart, sourceLength);
                Vector256<ushort> utf16VectorLower = Avx.LoadVector256(src);
                Vector256<ushort> utf16VectorUpper = Avx.LoadVector256(src + 16);

                if (Ascii.VectorContainsNonAsciiChar(utf16VectorLower | utf16VectorUpper))
                {
                    str = default;
                    return false;
                }

                str = Vector256.Narrow(utf16VectorLower, utf16VectorUpper).AsSByte();
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe bool TryLoadVector128(ushort* src, ushort* srcStart, int sourceLength, out Vector128<byte> str)
            {
                AssertRead<Vector128<sbyte>>(src, srcStart, sourceLength);
                Vector128<ushort> utf16VectorLower = Vector128.LoadUnsafe(ref *src);
                Vector128<ushort> utf16VectorUpper = Vector128.LoadUnsafe(ref *src, 8);
                if (Ascii.VectorContainsNonAsciiChar(utf16VectorLower | utf16VectorUpper))
                {
                    str = default;
                    return false;
                }

                str = Ascii.ExtractAsciiVector(utf16VectorLower, utf16VectorUpper);
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            public unsafe bool TryLoadArmVector128x4(ushort* src, ushort* srcStart, int sourceLength,
                out Vector128<byte> str1, out Vector128<byte> str2, out Vector128<byte> str3, out Vector128<byte> str4)
            {
                AssertRead<Vector128<sbyte>>(src, srcStart, sourceLength);
                var (s11, s12, s21, s22) = AdvSimd.Arm64.Load4xVector128AndUnzip(src);
                var (s31, s32, s41, s42) = AdvSimd.Arm64.Load4xVector128AndUnzip(src + 32);

                if (Ascii.VectorContainsNonAsciiChar(s11 | s12 | s21 | s22 | s31 | s32 | s41 | s42))
                {
                    str1 = str2 = str3 = str4 = default;
                    return false;
                }

                str1 = Ascii.ExtractAsciiVector(s11, s31);
                str2 = Ascii.ExtractAsciiVector(s12, s32);
                str3 = Ascii.ExtractAsciiVector(s21, s41);
                str4 = Ascii.ExtractAsciiVector(s22, s42);

                return true;
            }
#endif

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe int DecodeFourElements(ushort* source, ref sbyte decodingMap)
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
            public unsafe int DecodeRemaining(ushort* srcEnd, ref sbyte decodingMap, long remaining, out uint t2, out uint t3)
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
            public int IndexOfAnyExceptWhiteSpace(ReadOnlySpan<ushort> span)
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
            public OperationStatus DecodeWithWhiteSpaceBlockwiseWrapper<TBase64Decoder>(TBase64Decoder decoder, ReadOnlySpan<ushort> source, Span<byte> bytes,
                ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock = true) where TBase64Decoder : IBase64Decoder<ushort> =>
                DecodeWithWhiteSpaceBlockwise(decoder, source, bytes, ref bytesConsumed, ref bytesWritten, isFinalBlock);
        }
    }
}
