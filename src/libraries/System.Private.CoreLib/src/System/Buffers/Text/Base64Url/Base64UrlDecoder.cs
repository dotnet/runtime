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
    // AVX2 and Vector128 version based on https://github.com/gfoidl/Base64/blob/master/source/gfoidl.Base64/Internal/Encodings/Base64UrlEncoding.cs

    public static partial class Base64Url
    {
        /// <summary>
        /// Returns the maximum length (in bytes) of the result if you were to decode base 64 encoded text within a byte span of size "length".
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

            int remainder = base64Length % 4;

            return (base64Length >> 2) * 3 + (remainder > 0 ? remainder - 1 : 0);
        }

        /// <summary>
        /// Decode the span of UTF-8 encoded text represented as Base64Url into binary data.
        /// If the input is not a multiple of 4, it will decode as much as it can, to the closest multiple of 4.
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
        /// - InvalidData - if the input contains bytes outside of the expected Base64Url range,
        /// or if it contains invalid/more than two padding characters and <paramref name="isFinalBlock"/> is <see langword="true"/>.
        /// </returns>
        public static OperationStatus DecodeFromUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true) =>
            DecodeFromUtf8<Base64UrlDecoder>(source, destination, out bytesConsumed, out bytesWritten, isFinalBlock, ignoreWhiteSpace: true);

        /// <summary>
        /// Decode the span of UTF-8 encoded text in Base64Url (in-place) into binary data.
        /// The decoded binary output is smaller than the text data contained in the input (the operation deflates the data).
        /// TODO: If the input is not a multiple of 4, it will not decode any.
        /// </summary>
        /// <param name="buffer">The input span which contains the base 64 text data that needs to be decoded.</param>
        /// <returns>TODO: It returns the OperationStatus enum values:
        /// - Done - on successful processing of the entire input span
        /// - InvalidData - if the input contains bytes outside of the expected base 64 range, or if it contains invalid/more than two padding characters.
        /// It does not return DestinationTooSmall since that is not possible for base 64 decoding.
        /// It does not return NeedMoreData since this method tramples the data in the buffer and
        /// hence can only be called once with all the data in the buffer.
        /// </returns>
        public static int DecodeFromUtf8InPlace(Span<byte> buffer)
        {
            DecodeFromUtf8InPlace<Base64UrlDecoder>(buffer, out int bytesWritten, ignoreWhiteSpace: true);
            return bytesWritten;
        }

        public static int DecodeFromUtf8(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            OperationStatus status = DecodeFromUtf8(source, destination, out _, out int bytesWritten);

            if (OperationStatus.Done == status)
            {
                return bytesWritten;
            }

            if (OperationStatus.DestinationTooSmall == status)
            {
                throw new ArgumentException("DestinationTooSmall", nameof(destination));
            }

            throw new InvalidOperationException("InvalidData");
        }

        public static bool TryDecodeFromUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            OperationStatus status = DecodeFromUtf8(source, destination, out _, out bytesWritten);

            return status == OperationStatus.Done;
        }

        public static byte[] DecodeFromUtf8(ReadOnlySpan<byte> source)
        {
            Span<byte> destination = stackalloc byte[GetMaxDecodedLength(source.Length)];
            OperationStatus status = DecodeFromUtf8(source, destination, out _, out int bytesWritten);

            return OperationStatus.Done == status ? destination.Slice(0, bytesWritten).ToArray() :
                throw new InvalidOperationException("InvalidData");
        }

        public static OperationStatus DecodeFromChars(ReadOnlySpan<char> source, Span<byte> destination,
            out int charsConsumed, out int bytesWritten, bool isFinalBlock = true) =>
            DecodeFromUtf8(MemoryMarshal.AsBytes(source), destination, out charsConsumed, out bytesWritten, isFinalBlock);

        public static int DecodeFromChars(ReadOnlySpan<char> source, Span<byte> destination)
        {
            OperationStatus status = DecodeFromChars(source, destination, out _, out int bytesWritten);

            if (OperationStatus.Done == status)
            {
                return bytesWritten;
            }

            if (OperationStatus.DestinationTooSmall == status)
            {
                throw new ArgumentException("DestinationTooSmall", nameof(destination));
            }

            throw new InvalidOperationException("InvalidData");
        }

        public static bool TryDecodeFromChars(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten)
        {
            return TryDecodeFromUtf8(MemoryMarshal.AsBytes(source), destination, out bytesWritten);
        }

        public static byte[] DecodeFromChars(ReadOnlySpan<char> source)
        {
            Span<byte> destination = stackalloc byte[GetMaxDecodedLength(source.Length)];
            OperationStatus status = DecodeFromChars(source, destination, out _, out int bytesWritten);

            return OperationStatus.Done == status ? destination.Slice(0, bytesWritten).ToArray() :
                throw new InvalidOperationException("InvalidData");
        }

        private readonly struct Base64UrlDecoder : IBase64Decoder
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

            public static ReadOnlySpan<int> Vector128LutHigh => [ 0x392d0000, 0x7a6f5a4f, 0x00000000, 0x00000000 ];

            public static ReadOnlySpan<int> Vector128LutLow => [0x302d0101, 0x70615041, 0x01010101, 0x01010101];

            public static ReadOnlySpan<uint> Vector128LutShift => [0x04110000, 0xb9b9bfbf, 0x00000000, 0x00000000];

            public static int GetMaxDecodedLength(int utf8Length) => Base64Url.GetMaxDecodedLength(utf8Length);

            public static bool IsInValidLength(int bufferLength) => bufferLength % 4 == 1; // Should we fail here? One byte cannot be decoded completely

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
        }
    }
}
