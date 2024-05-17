// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Text;
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
        public static unsafe OperationStatus EncodeToUtf8(ReadOnlySpan<byte> source,
            Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true) =>
            EncodeTo<Base64UrlEncoderByte, byte>(source, destination, out bytesConsumed, out bytesWritten, isFinalBlock);

        /// <summary>
        /// Returns the length (in bytes) of the result if you were to encode binary data within a byte span of size "length".
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="bytesLength"/> is less than 0 or larger than 1610612733 (since encode inflates the data by 4/3).
        /// </exception>
        public static int GetEncodedLength(int bytesLength)
        {
            if ((uint)bytesLength > Base64.MaximumEncodeLength)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
            }

            int remainder = bytesLength % 3;

            return (bytesLength / 3) * 4 + (remainder > 0 ? remainder + 1 : 0); // if remainder is 1 or 2, the encoded length will be 1 byte longer.
        }

        /// <summary>
        /// Encode the span of binary data into UTF-8 encoded text represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the UTF-8 encoded text in Base64Url.</param>
        /// <returns>The number of bytes written into the destination span. This can be used to slice the output for subsequent calls, if necessary.</returns>
        /// <exception cref="ArgumentException">Thrown when the encoded output cannot fit in the <paramref name="destination"/> provided.</exception>
        /// <remarks>The output will not be padded even if the input is not a multiple of 3.</remarks>
        public static int EncodeToUtf8(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            OperationStatus status = EncodeToUtf8(source, destination, out _, out int bytesWritten);

            if (OperationStatus.Done == status)
            {
                return bytesWritten;
            }

            if (OperationStatus.DestinationTooSmall == status)
            {
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
            }

            Debug.Fail("Unreachable code");
            return 0;
        }

        /// <summary>
        /// Encode the span of binary data into UTF-8 encoded text represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <returns>The output byte array which contains the result of the operation, i.e. the UTF-8 encoded text in Base64Url.</returns>
        /// <remarks>The output will not be padded even if the input is not a multiple of 3.</remarks>
        public static byte[] EncodeToUtf8(ReadOnlySpan<byte> source)
        {
            Span<byte> destination = stackalloc byte[GetEncodedLength(source.Length)];
            EncodeToUtf8(source, destination, out _, out int bytesWritten);

            return destination.Slice(0, bytesWritten).ToArray();
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
        public static OperationStatus EncodeToChars(ReadOnlySpan<byte> source, Span<char> destination,
            out int bytesConsumed, out int charsWritten, bool isFinalBlock = true) =>
            EncodeTo<Base64UrlEncoderChar, ushort>(source, MemoryMarshal.Cast<char, ushort>(destination), out bytesConsumed, out charsWritten, isFinalBlock);

        /// <summary>
        /// Encode the span of binary data into UTF-8 encoded chars represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the UTF-8 encoded chars in Base64Url.</param>
        /// <returns>The number of bytes written into the destination span. This can be used to slice the output for subsequent calls, if necessary.</returns>
        /// <exception cref="ArgumentException">Thrown when the encoded output cannot fit in the <paramref name="destination"/> provided.</exception>
        /// <remarks>The output will not be padded even if the input is not a multiple of 3.</remarks>
        public static int EncodeToChars(ReadOnlySpan<byte> source, Span<char> destination)
        {
            OperationStatus status = EncodeToChars(source, destination, out _, out int charsWritten);

            if (OperationStatus.Done == status)
            {
                return charsWritten;
            }

            if (OperationStatus.DestinationTooSmall == status)
            {
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
            }

            Debug.Fail("Unreachable code");
            return 0;
        }

        /// <summary>
        /// Encode the span of binary data into UTF-8 encoded chars represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <returns>A char array which contains the result of the operation, i.e. the UTF-8 encoded chars in Base64Url.</returns>
        /// <remarks>The output will not be padded even if the input is not a multiple of 3.</remarks>
        public static char[] EncodeToChars(ReadOnlySpan<byte> source)
        {
            Span<char> destination = stackalloc char[GetEncodedLength(source.Length)];
            EncodeToChars(source, destination, out _, out _);

            return destination.ToArray();
        }

        /// <summary>
        /// Encode the span of binary data into UTF-8 encoded chars represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <returns>A string which contains the result of the operation, i.e. the UTF-8 encoded chars in Base64Url.</returns>
        /// <remarks>The output will not be padded even if the input is not a multiple of 3.</remarks>
        public static string EncodeToString(ReadOnlySpan<byte> source)
        {
            Span<char> destination = stackalloc char[GetEncodedLength(source.Length)];
            EncodeToChars(source, destination, out _, out _);

            return new string(destination);
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
            OperationStatus status = EncodeToChars(source, destination, out _, out charsWritten);

            return status == OperationStatus.Done;
        }

        /// <summary>
        /// Encode the span of binary data into UTF-8 encoded chars represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the UTF-8 encoded text in Base64Url.</param>
        /// <param name="bytesWritten">The number of chars written into the output span. This can be used to slice the output for subsequent calls, if necessary.</param>
        /// <returns><see langword="true"/> if bytes encoded successfully, otherwise <see langword="false"/>.</returns>
        /// <remarks>The output will not be padded even if the input is not a multiple of 3.</remarks>
        public static bool TryEncodeToUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            OperationStatus status = EncodeToUtf8(source, destination, out _, out bytesWritten);

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
            OperationStatus status = EncodeToUtf8InPlace<Base64UrlEncoderByte>(buffer, dataLength, out bytesWritten);
            return status == OperationStatus.Done;
        }

        private readonly struct Base64UrlEncoderChar : IBase64Encoder<ushort>
        {
            public static ReadOnlySpan<byte> EncodingMap => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_"u8;

            public static sbyte Avx2LutChar62 => -17;  // char '-' diff

            public static sbyte Avx2LutChar63 => 32;   // char '_' diff

            public static ReadOnlySpan<byte> AdvSimdLut4 => "wxyz0123456789-_"u8;

            public static uint Ssse3AdvSimdLutE3 => 0x000020EF;

            public static int IncrementPadTwo => 2;

            public static int IncrementPadOne => 3;

            public static int GetMaxSrcLength(int srcLength, int destLength) =>
                srcLength <= MaximumEncodeLength && destLength >= GetEncodedLength(srcLength) ?
                srcLength : (destLength >> 2) * 3 + destLength % 4;

            public static uint GetInPlaceDestinationLength(int encodedLength, int _) => 0; // not used for char encoding

            public static int GetMaxEncodedLength(int _) => 0;  // not used for char encoding

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe void EncodeOneOptionallyPadTwo(byte* oneByte, ushort* dest, ref byte encodingMap)
            {
                uint t0 = oneByte[0];

                uint i = t0 << 8;

                uint i0 = Unsafe.Add(ref encodingMap, (IntPtr)(i >> 10));
                uint i1 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 4) & 0x3F));

                if (BitConverter.IsLittleEndian)
                {
                    dest[0] = (ushort)i0;
                    dest[1] = (ushort)i1;
                }
                else
                {
                    dest[1] = (ushort)i0;
                    dest[0] = (ushort)i1;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe void EncodeTwoOptionallyPadOne(byte* twoBytes, ushort* dest, ref byte encodingMap)
            {
                uint t0 = twoBytes[0];
                uint t1 = twoBytes[1];

                uint i = (t0 << 16) | (t1 << 8);

                uint i0 = Unsafe.Add(ref encodingMap, (IntPtr)(i >> 18));
                uint i1 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 12) & 0x3F));
                uint i2 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 6) & 0x3F));

                if (BitConverter.IsLittleEndian)
                {
                    dest[0] = (ushort)i0;
                    dest[1] = (ushort)i1;
                    dest[2] = (ushort)i2;
                }
                else
                {
                    dest[2] = (ushort)i0;
                    dest[1] = (ushort)i1;
                    dest[0] = (ushort)i2;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe void StoreToDestination(ushort* dest, ushort* destStart, int destLength, Vector512<byte> str)
            {
                AssertWrite<Vector512<ushort>>(dest, destStart, destLength);
                (Vector512<ushort> utf16LowVector, Vector512<ushort> utf16HighVector) = Vector512.Widen(str);
                utf16LowVector.Store(dest);
                utf16HighVector.Store(dest + Vector512<ushort>.Count);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe void StoreToDestination(ushort* dest, ushort* destStart, int destLength, Vector256<byte> str)
            {
                AssertWrite<Vector256<ushort>>(dest, destStart, destLength);
                (Vector256<ushort> utf16LowVector, Vector256<ushort> utf16HighVector) = Vector256.Widen(str);
                utf16LowVector.Store(dest);
                utf16HighVector.Store(dest + Vector256<ushort>.Count);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe void StoreToDestination(ushort* dest, ushort* destStart, int destLength, Vector128<byte> str)
            {
                AssertWrite<Vector128<ushort>>(dest, destStart, destLength);
                (Vector128<ushort> utf16LowVector, Vector128<ushort> utf16HighVector) = Vector128.Widen(str);
                utf16LowVector.Store(dest);
                utf16HighVector.Store(dest + Vector128<ushort>.Count);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            public static unsafe void StoreToDestination(ushort* dest, ushort* destStart, int destLength,
                Vector128<byte> res1, Vector128<byte> res2, Vector128<byte> res3, Vector128<byte> res4)
            {
                AssertWrite<Vector128<ushort>>(dest, destStart, destLength);
                Vector128<ushort> vecWide1 = AdvSimd.Arm64.ZipLow(res1, Vector128<byte>.Zero).AsUInt16();
                Vector128<ushort> vecWide2 = AdvSimd.Arm64.ZipLow(res2, Vector128<byte>.Zero).AsUInt16();
                Vector128<ushort> vecWide3 = AdvSimd.Arm64.ZipLow(res3, Vector128<byte>.Zero).AsUInt16();
                Vector128<ushort> vecWide4 = AdvSimd.Arm64.ZipLow(res4, Vector128<byte>.Zero).AsUInt16();
                AdvSimd.Arm64.StoreVector128x4(dest, (vecWide1, vecWide2, vecWide3, vecWide4));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe void EncodeThreeAndWrite(byte* threeBytes, ushort* destination, ref byte encodingMap)
            {
                uint t0 = threeBytes[0];
                uint t1 = threeBytes[1];
                uint t2 = threeBytes[2];

                uint i = (t0 << 16) | (t1 << 8) | t2;

                byte i0 = Unsafe.Add(ref encodingMap, (IntPtr)(i >> 18));
                byte i1 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 12) & 0x3F));
                byte i2 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 6) & 0x3F));
                byte i3 = Unsafe.Add(ref encodingMap, (IntPtr)(i & 0x3F));

                if (BitConverter.IsLittleEndian)
                {
                    destination[0] = i0;
                    destination[1] = i1;
                    destination[2] = i2;
                    destination[3] = i3;
                }
                else
                {
                    destination[3] = i0;
                    destination[2] = i1;
                    destination[1] = i2;
                    destination[0] = i3;
                }
            }
        }

        private readonly struct Base64UrlEncoderByte : IBase64Encoder<byte>
        {
            public static ReadOnlySpan<byte> EncodingMap => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_"u8;

            public static sbyte Avx2LutChar62 => -17;  // char '-' diff

            public static sbyte Avx2LutChar63 => 32;   // char '_' diff

            public static ReadOnlySpan<byte> AdvSimdLut4 => "wxyz0123456789-_"u8;

            public static uint Ssse3AdvSimdLutE3 => 0x000020EF;

            public static int IncrementPadTwo => 2;

            public static int IncrementPadOne => 3;

            public static int GetMaxSrcLength(int srcLength, int destLength) =>
                srcLength <= MaximumEncodeLength && destLength >= Base64Url.GetEncodedLength(srcLength) ?
                srcLength : (destLength >> 2) * 3 + destLength % 4;

            public static uint GetInPlaceDestinationLength(int encodedLength, int leftOver) =>
                leftOver > 0 ? (uint)(encodedLength - leftOver - 1) : (uint)(encodedLength - 4);

            public static int GetMaxEncodedLength(int srcLength) => GetEncodedLength(srcLength);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe void EncodeOneOptionallyPadTwo(byte* oneByte, byte* dest, ref byte encodingMap)
            {
                uint t0 = oneByte[0];

                uint i = t0 << 8;

                uint i0 = Unsafe.Add(ref encodingMap, (IntPtr)(i >> 10));
                uint i1 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 4) & 0x3F));

                if (BitConverter.IsLittleEndian)
                {
                    dest[0] = (byte)i0;
                    dest[1] = (byte)i1;
                }
                else
                {
                    dest[1] = (byte)i0;
                    dest[0] = (byte)i1;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe void EncodeTwoOptionallyPadOne(byte* twoBytes, byte* dest, ref byte encodingMap)
            {
                uint t0 = twoBytes[0];
                uint t1 = twoBytes[1];

                uint i = (t0 << 16) | (t1 << 8);

                uint i0 = Unsafe.Add(ref encodingMap, (IntPtr)(i >> 18));
                uint i1 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 12) & 0x3F));
                uint i2 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 6) & 0x3F));

                if (BitConverter.IsLittleEndian)
                {
                    dest[0] = (byte)i0;
                    dest[1] = (byte)i1;
                    dest[2] = (byte)i2;
                }
                else
                {
                    dest[2] = (byte)i0;
                    dest[1] = (byte)i1;
                    dest[0] = (byte)i2;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe void StoreToDestination(byte* dest, byte* destStart, int destLength, Vector512<byte> str)
            {
                Base64.AssertWrite<Vector512<sbyte>>(dest, destStart, destLength);
                str.Store(dest);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(Avx2))]
            public static unsafe void StoreToDestination(byte* dest, byte* destStart, int destLength, Vector256<byte> str)
            {
                Base64.AssertWrite<Vector256<sbyte>>(dest, destStart, destLength);
                Avx.Store(dest, str.AsByte());
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe void StoreToDestination(byte* dest, byte* destStart, int destLength, Vector128<byte> str)
            {
                Base64.AssertWrite<Vector128<sbyte>>(dest, destStart, destLength);
                str.Store(dest);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            public static unsafe void StoreToDestination(byte* dest, byte* destStart, int destLength,
                Vector128<byte> res1, Vector128<byte> res2, Vector128<byte> res3, Vector128<byte> res4)
            {
                Base64.AssertWrite<Vector128<byte>>(dest, destStart, destLength);
                AdvSimd.Arm64.StoreVector128x4AndZip(dest, (res1, res2, res3, res4));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe void EncodeThreeAndWrite(byte* threeBytes, byte* destination, ref byte encodingMap)
            {
                uint t0 = threeBytes[0];
                uint t1 = threeBytes[1];
                uint t2 = threeBytes[2];

                uint i = (t0 << 16) | (t1 << 8) | t2;

                byte i0 = Unsafe.Add(ref encodingMap, (IntPtr)(i >> 18));
                byte i1 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 12) & 0x3F));
                byte i2 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 6) & 0x3F));
                byte i3 = Unsafe.Add(ref encodingMap, (IntPtr)(i & 0x3F));

                if (BitConverter.IsLittleEndian)
                {
                    destination[0] = i0;
                    destination[1] = i1;
                    destination[2] = i2;
                    destination[3] = i3;
                }
                else
                {
                    destination[3] = i0;
                    destination[2] = i1;
                    destination[1] = i2;
                    destination[0] = i3;
                }
            }
        }
    }
}
