// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text.Unicode;
using static System.Buffers.Text.Base64;

namespace System.Buffers.Text
{
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

            int remainder = base64Length % 3;

            return (base64Length >> 2) * 3 + remainder > 0 ? remainder - 1 : 0;
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
            Base64.DecodeFromUtf8<Base64UrlDecoder>(source, destination, out bytesConsumed, out bytesWritten, isFinalBlock, ignoreWhiteSpace: true);

        private readonly struct Base64UrlDecoder : IBase64Decoder
        {
            // Pre-computing this table using a custom string(s_characters) and GenerateDecodingMapAndVerify (found in tests)
            public static ReadOnlySpan<sbyte> DecodingMap =>
                [
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
                    -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 62, -1, -1,         //62 is placed at index 45 (for -), 63 at index 95 (for _)
                    52, 53, 54, 55, 56, 57, 58, 59, 60, 61, -1, -1, -1, -1, -1, -1,         //52-61 are placed at index 48-57 (for 0-9), TODO: 64 at index 61 (for =) - this is not there
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

            public static Vector512<sbyte> VbmiLookup0 => Vector512.Create(
                0x80808080, 0x80808080, 0x80808080, 0x80808080,
                0x80808080, 0x80808080, 0x80808080, 0x80808080,
                0x80808080, 0x80808080, 0x80808080, 0x80803e80,
                0x37363534, 0x3b3a3938, 0x80803d3c, 0x80808080).AsSByte();

            public static Vector512<sbyte> VbmiLookup1 => Vector512.Create(
                0x02010080, 0x06050403, 0x0a090807, 0x0e0d0c0b,
                0x1211100f, 0x16151413, 0x80191817, 0x3f808080,
                0x1c1b1a80, 0x201f1e1d, 0x24232221, 0x28272625,
                0x2c2b2a29, 0x302f2e2d, 0x80333231, 0x80808080).AsSByte();
        }

        /*public static OperationStatus DecodeFromUtf8InPlace(Span<byte> buffer, out int bytesWritten) => throw new NotImplementedException();

        // Up to this point, this is a mirror of System.Buffers.Text.Base64
        // Below are more helpers that bring over functionality similar to Convert.*Base64*

        // Encode to / decode from chars
        public static bool TryDecodeFromChars(ReadOnlySpan<char> chars, Span<byte> bytes, out int bytesWritten) => throw new NotImplementedException();


        // These are just accelerator methods.
        // Should be efficiently implementable on top of the other ones in just a few lines.

        // Decode from chars => string
        // Decode from chars => byte[]
        // The names could also just be "Decode" without naming the return type
        public static string DecodeToString(ReadOnlySpan<char> chars, Encoding encoding) => throw new NotImplementedException();
        public static byte[] DecodeToByteArray(ReadOnlySpan<char> chars) => throw new NotImplementedException();*/
    }
}
