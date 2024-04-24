// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using static System.Buffers.Text.Base64;

namespace System.Buffers.Text
{
    public static partial class Base64Url
    {
        /// <summary>
        /// Encode the span of binary data into UTF-8 encoded text represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the UTF-8 encoded text in Base64Url.</param>
        /// <param name="bytesConsumed">The number of input bytes consumed during the operation. This can be used to slice the input for subsequent calls, if necessary.</param>
        /// <param name="bytesWritten">The number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary.</param>
        /// <param name="isFinalBlock"><see langword="true"/> (default) when the input span contains the entire data to encode.
        /// Set to <see langword="true"/> when the source buffer contains the entirety of the data to encode.
        /// Set to <see langword="false"/> if this method is being called in a loop and if more input data may follow.
        /// At the end of the loop, call this (potentially with an empty source buffer) passing <see langword="true"/>.</param>
        /// <returns>It returns the OperationStatus enum values:
        /// - Done - on successful processing of the entire input span
        /// - DestinationTooSmall - if there is not enough space in the output span to fit the encoded input
        /// - NeedMoreData - only if <paramref name="isFinalBlock"/> is <see langword="false"/>
        /// It does not return InvalidData since that is not possible for base64 encoding.
        /// </returns>
        /// <remarks>The output will not be padded even if the input is not a multiple of 3.</remarks>
        public static unsafe OperationStatus EncodeToUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true) =>
            EncodeToUtf8<Base64UrlEncoder>(source, destination, out bytesConsumed, out bytesWritten, isFinalBlock);

        /// <summary>
        /// Returns the length (in bytes) of the result if you were to encode binary data within a byte span of size "length".
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="bytesLength"/> is less than 0 or larger than 1610612733 (since encode inflates the data by 4/3).
        /// </exception>
        public static int GetEncodedLength(int bytesLength)
        {
            if ((uint)bytesLength > Base64.MaximumEncodeLength)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);

            int remainder = bytesLength % 3;

            return bytesLength / 3 * 4 + (remainder > 0 ? remainder + 1 : 0); // if remainder is 1 or 2, the encoded length will be 1 byte longer.
        }

        /// <summary>
        /// Encode the span of binary data into UTF-8 encoded text represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the UTF-8 encoded text in Base64Url.</param>
        /// <returns>The number of bytes written into the destination span. This can be used to slice the output for subsequent calls, if necessary.</returns>
        /// <remarks>The output will not be padded even if the input is not a multiple of 3.</remarks>
        public static int EncodeToUtf8(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            EncodeToUtf8(source, destination, out _, out int written);

            return written;
        }

        /// <summary>
        /// Encode the span of binary data into UTF-8 encoded text represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <returns>The output byte array which contains the result of the operation, i.e. the UTF-8 encoded text in Base64Url.</returns>
        /// <remarks>The output will not be padded even if the input is not a multiple of 3.</remarks>
        public static byte[] EncodeToUtf8(ReadOnlySpan<byte> source)
        {
            if (source.Length == 0)
            {
                return Array.Empty<byte>();
            }

            Span<byte> destination = stackalloc byte[GetEncodedLength(source.Length)]; // or new byte[GetEncodedLength(source.Length)]
            EncodeToUtf8(source, destination, out _, out int written);

            return destination.Slice(0, written).ToArray();
        }

        /// <summary>
        /// Encode the span of binary data into UTF-8 encoded chars represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the UTF-8 encoded chars in Base64Url.</param>
        /// <param name="bytesConsumed">The number of input bytes consumed during the operation. This can be used to slice the input for subsequent calls, if necessary.</param>
        /// <param name="charsWritten">The number of chars written into the output span. This can be used to slice the output for subsequent calls, if necessary.</param>
        /// <param name="isFinalBlock"><see langword="true"/> (default) when the input span contains the entire data to encode.
        /// Set to <see langword="true"/> when the source buffer contains the entirety of the data to encode.
        /// Set to <see langword="false"/> if this method is being called in a loop and if more input data may follow.
        /// At the end of the loop, call this (potentially with an empty source buffer) passing <see langword="true"/>.</param>
        /// <returns>It returns the OperationStatus enum values:
        /// - Done - on successful processing of the entire input span
        /// - DestinationTooSmall - if there is not enough space in the output span to fit the encoded input
        /// - NeedMoreData - only if <paramref name="isFinalBlock"/> is <see langword="false"/>
        /// It does not return InvalidData since that is not possible for base64 encoding.
        /// </returns>
        /// <remarks>The output will not be padded even if the input is not a multiple of 3.</remarks>
        public static OperationStatus EncodeToChars(ReadOnlySpan<byte> source, Span<char> destination, out int bytesConsumed, out int charsWritten, bool isFinalBlock = true)
        {
            if (source.Length == 0)
            {
                bytesConsumed = 0;
                charsWritten = 0;
                return OperationStatus.Done;
            }

            return EncodeToUtf8(source, MemoryMarshal.AsBytes(destination), out bytesConsumed, out charsWritten, isFinalBlock);
        }

        /// <summary>
        /// Encode the span of binary data into UTF-8 encoded chars represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the UTF-8 encoded chars in Base64Url.</param>
        /// <returns>The number of bytes written into the destination span. This can be used to slice the output for subsequent calls, if necessary.</returns>
        /// <remarks>The output will not be padded even if the input is not a multiple of 3.</remarks>
        public static int EncodeToChars(ReadOnlySpan<byte> source, Span<char> destination)
        {
            EncodeToUtf8(source, MemoryMarshal.AsBytes(destination), out _, out int written);
            return written;
        }

        /// <summary>
        /// Encode the span of binary data into UTF-8 encoded chars represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <returns>A char array which contains the result of the operation, i.e. the UTF-8 encoded chars in Base64Url.</returns>
        /// <remarks>The output will not be padded even if the input is not a multiple of 3.</remarks>
        public static char[] EncodeToChars(ReadOnlySpan<byte> source)
        {
            if (source.Length == 0)
            {
                return Array.Empty<char>();
            }

            Span<char> destination = stackalloc char[GetEncodedLength(source.Length)];
            EncodeToUtf8(source, MemoryMarshal.AsBytes(destination), out _, out int charsWritten);

            return destination.Slice(0, charsWritten).ToArray();
        }

        /// <summary>
        /// Encode the span of binary data into UTF-8 encoded chars represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <returns>A string which contains the result of the operation, i.e. the UTF-8 encoded chars in Base64Url.</returns>
        /// <remarks>The output will not be padded even if the input is not a multiple of 3.</remarks>
        public static string EncodeToString(ReadOnlySpan<byte> source)
        {
            if (source.Length == 0)
            {
                return string.Empty;
            }

            Span<byte> destination = stackalloc byte[GetEncodedLength(source.Length)];
            EncodeToUtf8(source, destination, out _, out int charsWritten);

            return destination.Slice(0, charsWritten).ToString(); // Encoding.UTF8.GetString(utf8.Slice(0, bytesWritten))
        }

        /// <summary>
        /// Encode the span of binary data into UTF-8 encoded chars represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the UTF-8 encoded chars in Base64Url.</param>
        /// <param name="charsWritten">The number of chars written into the output span. This can be used to slice the output for subsequent calls, if necessary.</param>
        /// <returns><see langword="true"/> if chars encoded successfully, otherwise <see langword="false"/>.</returns>
        /// <remarks>The output will not be padded even if the input is not a multiple of 3.</remarks>
        public static bool TryEncodeToChars(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten)
        {
            OperationStatus status = EncodeToUtf8(source, MemoryMarshal.AsBytes(destination), out _, out charsWritten);

            return status == OperationStatus.Done;
        }

        /// <summary>
        /// Encode the span of binary data into UTF-8 encoded chars represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the UTF-8 encoded text in Base64Url.</param>
        /// <param name="charsWritten">The number of chars written into the output span. This can be used to slice the output for subsequent calls, if necessary.</param>
        /// <returns><see langword="true"/> if bytes encoded successfully, otherwise <see langword="false"/>.</returns>
        /// <remarks>The output will not be padded even if the input is not a multiple of 3.</remarks>
        public static bool TryEncodeToUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int charsWritten)
        {
            OperationStatus status = EncodeToUtf8(source, destination, out _, out charsWritten);

            return status == OperationStatus.Done;
        }

        /// <summary>
        /// Encode the span of binary data (in-place) into UTF-8 encoded text represented as base 64.
        /// The encoded text output is larger than the binary data contained in the input (the operation inflates the data).
        /// </summary>
        /// <param name="buffer">The input span which contains binary data that needs to be encoded.
        /// It needs to be large enough to fit the result of the operation.</param>
        /// <param name="dataLength">The amount of binary data contained within the buffer that needs to be encoded
        /// (and needs to be smaller than the buffer length).</param>
        /// <param name="bytesWritten">The number of bytes written into the buffer.</param>
        /// <returns><see langword="true"/> if bytes encoded successfully, otherwise <see langword="false"/>.</returns>
        public static unsafe bool TryEncodeToUtf8InPlace(Span<byte> buffer, int dataLength, out int bytesWritten)
        {
            if (buffer.IsEmpty)
            {
                bytesWritten = 0;
                return true;
            }

            fixed (byte* bufferBytes = &MemoryMarshal.GetReference(buffer))
            {
                int encodedLength = GetEncodedLength(dataLength);
                if (buffer.Length < encodedLength)
                {
                    bytesWritten = 0;
                    return false;
                }

                int leftover = dataLength % 3; // how many bytes left after packs of 3

                uint destinationIndex = leftover > 0 ? (uint)(encodedLength - leftover - 1) : (uint)(encodedLength - 4);
                uint sourceIndex = (uint)(dataLength - leftover);
                uint result = 0;
                ref byte encodingMap = ref MemoryMarshal.GetReference(Base64UrlEncoder.EncodingMap);

                // encode last pack to avoid conditional in the main loop
                if (leftover != 0)
                {
                    if (leftover == 1)
                    {
                        result = Base64UrlEncoder.EncodeOneOptionallyPadTwo(bufferBytes + sourceIndex, ref encodingMap);
                    }
                    else
                    {
                        result = Base64UrlEncoder.EncodeTwoOptionallyPadOne(bufferBytes + sourceIndex, ref encodingMap);
                    }

                    Unsafe.WriteUnaligned(bufferBytes + destinationIndex, result);
                    destinationIndex -= 4;
                }

                sourceIndex -= 3;
                while ((int)sourceIndex >= 0)
                {
                    result = Encode(bufferBytes + sourceIndex, ref encodingMap);
                    Unsafe.WriteUnaligned(bufferBytes + destinationIndex, result);
                    destinationIndex -= 4;
                    sourceIndex -= 3;
                }

                bytesWritten = encodedLength;
                return true;
            }
        }

        private readonly struct Base64UrlEncoder : IBase64Encoder
        {
            public static ReadOnlySpan<byte> EncodingMap => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_"u8;

            public static Vector256<sbyte> Avx2Lut => Vector256.Create(
                            65, 71, -4, -4,
                            -4, -4, -4, -4,
                            -4, -4, -4, -4,
                            -17, 32, 0, 0,
                            65, 71, -4, -4,
                            -4, -4, -4, -4,
                            -4, -4, -4, -4,
                            -17, 32, 0, 0);

            public static Vector128<byte> AdvSimdLut4 => Vector128.Create("wxyz0123456789-_"u8).AsByte();

            public static Vector128<byte> Ssse3AdvSimdLut => Vector128.Create(0xFCFC4741, 0xFCFCFCFC, 0xFCFCFCFC, 0x000020EF).AsByte();

            public static int IncrementPadTwo => 2;

            public static int IncrementPadOne => 3;

            public static int GetMaxSrcLength(int srcLength, int destLength) =>
                srcLength <= MaximumEncodeLength && destLength >= GetEncodedLength(srcLength) ? srcLength : (destLength >> 2) * 3 + destLength % 4;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe uint EncodeOneOptionallyPadTwo(byte* oneByte, ref byte encodingMap)
            {
                uint t0 = oneByte[0];

                uint i = t0 << 8;

                uint i0 = Unsafe.Add(ref encodingMap, (IntPtr)(i >> 10));
                uint i1 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 4) & 0x3F));

                if (BitConverter.IsLittleEndian)
                {
                    return i0 | (i1 << 8);
                }
                else
                {
                    return (i0 << 8) | i1;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe uint EncodeTwoOptionallyPadOne(byte* twoBytes, ref byte encodingMap)
            {
                uint t0 = twoBytes[0];
                uint t1 = twoBytes[1];

                uint i = (t0 << 16) | (t1 << 8);

                uint i0 = Unsafe.Add(ref encodingMap, (IntPtr)(i >> 18));
                uint i1 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 12) & 0x3F));
                uint i2 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 6) & 0x3F));

                if (BitConverter.IsLittleEndian)
                {
                    return i0 | (i1 << 8) | (i2 << 16);
                }
                else
                {
                    return (i0 << 16) | (i1 << 8) | i2;
                }
            }
        }
    }
}
