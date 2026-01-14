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
using static System.Buffers.Text.Base64Helper;

namespace System.Buffers.Text
{
    // AVX2 version based on https://github.com/aklomp/base64/tree/e516d769a2a432c08404f1981e73b431566057be/lib/arch/avx2
    // Vector128 version based on https://github.com/aklomp/base64/tree/e516d769a2a432c08404f1981e73b431566057be/lib/arch/ssse3

    /// <summary>
    /// Convert between binary data and UTF-8 encoded text that is represented in base 64.
    /// </summary>
    public static partial class Base64
    {
        /// <summary>
        /// Returns the length (in bytes) of the result if you were to encode binary data within a byte span of size <paramref name="bytesLength"/>.
        /// </summary>
        /// <param name="bytesLength">The number of bytes to encode.</param>
        /// <returns>The number of bytes that encoding will produce.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="bytesLength"/> is less than 0 or greater than 1610612733.
        /// </exception>
        /// <remarks>
        /// This method is equivalent to <see cref="GetMaxEncodedToUtf8Length(int)"/>. The encoded length for base64 is exactly calculated, not an upper bound.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetEncodedLength(int bytesLength)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan<uint>((uint)bytesLength, MaximumEncodeLength);

            return ((bytesLength + 2) / 3) * 4;
        }

        /// <summary>
        /// Returns the maximum length (in bytes) of the result if you were to encode binary data within a byte span of size "length".
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="length"/> is less than 0 or larger than 1610612733 (since encode inflates the data by 4/3).
        /// </exception>
        /// <remarks>
        /// This method is equivalent to <see cref="GetEncodedLength(int)"/>. The encoded length for base64 is exactly calculated, not an upper bound.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMaxEncodedToUtf8Length(int length) => GetEncodedLength(length);

        /// <summary>
        /// Encode the span of binary data into UTF-8 encoded text represented as base64.
        /// </summary>
        /// <param name="bytes">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="utf8">The output span which contains the result of the operation, i.e. the UTF-8 encoded text in base64.</param>
        /// <param name="bytesConsumed">The number of input bytes consumed during the operation. This can be used to slice the input for subsequent calls, if necessary.</param>
        /// <param name="bytesWritten">The number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary.</param>
        /// <param name="isFinalBlock"><see langword="true"/> (default) when the input span contains the entire data to encode.
        /// Set to <see langword="true"/> when the source buffer contains the entirety of the data to encode.
        /// Set to <see langword="false"/> if this method is being called in a loop and if more input data may follow.
        /// At the end of the loop, call this (potentially with an empty source buffer) passing <see langword="true"/>.</param>
        /// <returns>It returns the <see cref="OperationStatus"/> enum values:
        /// - Done - on successful processing of the entire input span
        /// - DestinationTooSmall - if there is not enough space in the output span to fit the encoded input
        /// - NeedMoreData - only if <paramref name="isFinalBlock"/> is <see langword="false"/>, otherwise the output is padded if the input is not a multiple of 3
        /// It does not return InvalidData since that is not possible for base64 encoding.
        /// </returns>
        public static OperationStatus EncodeToUtf8(ReadOnlySpan<byte> bytes, Span<byte> utf8, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true) =>
            EncodeTo(default(Base64EncoderByte), bytes, utf8, out bytesConsumed, out bytesWritten, isFinalBlock);

        /// <summary>
        /// Encodes the span of binary data into UTF-8 encoded text represented as Base64.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the UTF-8 encoded text in Base64.</param>
        /// <returns>The number of bytes written into the destination span. This can be used to slice the output for subsequent calls, if necessary.</returns>
        /// <exception cref="ArgumentException">The buffer in <paramref name="destination"/> is too small to hold the encoded output.</exception>
        public static int EncodeToUtf8(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            OperationStatus status = EncodeToUtf8(source, destination, out _, out int bytesWritten);

            if (status == OperationStatus.Done)
            {
                return bytesWritten;
            }

            Debug.Assert(status == OperationStatus.DestinationTooSmall);
            throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
        }

        /// <summary>
        /// Encodes the span of binary data into UTF-8 encoded text represented as Base64.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <returns>The output byte array which contains the result of the operation, i.e. the UTF-8 encoded text in Base64.</returns>
        public static byte[] EncodeToUtf8(ReadOnlySpan<byte> source)
        {
            byte[] destination = new byte[GetEncodedLength(source.Length)];
            EncodeToUtf8(source, destination, out _, out int bytesWritten);
            Debug.Assert(destination.Length == bytesWritten);

            return destination;
        }

        /// <summary>
        /// Encodes the span of binary data into UTF-8 encoded text represented as Base64.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the UTF-8 encoded text in Base64.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
        /// <returns><see langword="true"/> if bytes encoded successfully, otherwise <see langword="false"/>.</returns>
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
        /// <returns>It returns the OperationStatus enum values:
        /// - Done - on successful processing of the entire buffer
        /// - DestinationTooSmall - if there is not enough space in the buffer beyond dataLength to fit the result of encoding the input
        /// It does not return NeedMoreData since this method tramples the data in the buffer and hence can only be called once with all the data in the buffer.
        /// It does not return InvalidData since that is not possible for base 64 encoding.
        /// </returns>
        public static OperationStatus EncodeToUtf8InPlace(Span<byte> buffer, int dataLength, out int bytesWritten) =>
            Base64Helper.EncodeToUtf8InPlace(default(Base64EncoderByte), buffer, dataLength, out bytesWritten);

        /// <summary>
        /// Encodes the span of binary data (in-place) into UTF-8 encoded text represented as base 64.
        /// The encoded text output is larger than the binary data contained in the input (the operation inflates the data).
        /// </summary>
        /// <param name="buffer">The input span which contains binary data that needs to be encoded.
        /// It needs to be large enough to fit the result of the operation.</param>
        /// <param name="dataLength">The amount of binary data contained within the buffer that needs to be encoded
        /// (and needs to be smaller than the buffer length).</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes written into the buffer. This parameter is treated as uninitialized.</param>
        /// <returns><see langword="true"/> if bytes encoded successfully, otherwise <see langword="false"/>.</returns>
        public static bool TryEncodeToUtf8InPlace(Span<byte> buffer, int dataLength, out int bytesWritten)
        {
            OperationStatus status = EncodeToUtf8InPlace(buffer, dataLength, out bytesWritten);

            return status == OperationStatus.Done;
        }

        /// <summary>
        /// Encodes the span of binary data into unicode ASCII chars represented as Base64.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the ASCII chars in Base64.</param>
        /// <param name="bytesConsumed">When this method returns, contains the number of input bytes consumed during the operation. This can be used to slice the input for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
        /// <param name="charsWritten">When this method returns, contains the number of chars written into the output span. This can be used to slice the output for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
        /// <param name="isFinalBlock"><see langword="true"/> when the input span contains the entirety of data to encode; <see langword="false"/> when more data may follow,
        /// such as when calling in a loop, subsequent calls with <see langword="false"/> should end with <see langword="true"/> call. The default is <see langword="true" />.</param>
        /// <returns>One of the enumeration values that indicates the success or failure of the operation.</returns>
        public static OperationStatus EncodeToChars(ReadOnlySpan<byte> source, Span<char> destination,
            out int bytesConsumed, out int charsWritten, bool isFinalBlock = true) =>
            EncodeTo(default(Base64EncoderChar), source, MemoryMarshal.Cast<char, ushort>(destination), out bytesConsumed, out charsWritten, isFinalBlock);

        /// <summary>
        /// Encodes the span of binary data into unicode ASCII chars represented as Base64.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the ASCII chars in Base64.</param>
        /// <returns>The number of chars written into the destination span. This can be used to slice the output for subsequent calls, if necessary.</returns>
        /// <exception cref="ArgumentException">The buffer in <paramref name="destination"/> is too small to hold the encoded output.</exception>
        public static int EncodeToChars(ReadOnlySpan<byte> source, Span<char> destination)
        {
            OperationStatus status = EncodeToChars(source, destination, out _, out int charsWritten);

            if (status == OperationStatus.Done)
            {
                return charsWritten;
            }

            Debug.Assert(status == OperationStatus.DestinationTooSmall);
            throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
        }

        /// <summary>
        /// Encodes the span of binary data into unicode ASCII chars represented as Base64.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <returns>A char array which contains the result of the operation, i.e. the ASCII chars in Base64.</returns>
        public static char[] EncodeToChars(ReadOnlySpan<byte> source)
        {
            char[] destination = new char[GetEncodedLength(source.Length)];
            EncodeToChars(source, destination, out _, out int charsWritten);
            Debug.Assert(destination.Length == charsWritten);

            return destination;
        }

        /// <summary>
        /// Encodes the span of binary data into unicode string represented as Base64 ASCII chars.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <returns>A string which contains the result of the operation, i.e. the ASCII string in Base64.</returns>
        public static string EncodeToString(ReadOnlySpan<byte> source) =>
            string.Create(GetEncodedLength(source.Length), source, static (buffer, source) =>
            {
                EncodeToChars(source, buffer, out _, out int charsWritten);
                Debug.Assert(buffer.Length == charsWritten, $"The source length: {source.Length}, chars written: {charsWritten}");
            });

        /// <summary>
        /// Encodes the span of binary data into unicode ASCII chars represented as Base64.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the ASCII chars in Base64.</param>
        /// <param name="charsWritten">When this method returns, contains the number of chars written into the output span. This can be used to slice the output for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
        /// <returns><see langword="true"/> if chars encoded successfully, otherwise <see langword="false"/>.</returns>
        public static bool TryEncodeToChars(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten)
        {
            OperationStatus status = EncodeToChars(source, destination, out _, out charsWritten);

            return status == OperationStatus.Done;
        }

        private readonly struct Base64EncoderChar : IBase64Encoder<ushort>
        {
            public ReadOnlySpan<byte> EncodingMap => default(Base64EncoderByte).EncodingMap;

            public sbyte Avx2LutChar62 => default(Base64EncoderByte).Avx2LutChar62;

            public sbyte Avx2LutChar63 => default(Base64EncoderByte).Avx2LutChar63;

            public ReadOnlySpan<byte> AdvSimdLut4 => default(Base64EncoderByte).AdvSimdLut4;

            public uint Ssse3AdvSimdLutE3 => default(Base64EncoderByte).Ssse3AdvSimdLutE3;

            public int IncrementPadTwo => default(Base64EncoderByte).IncrementPadTwo;

            public int IncrementPadOne => default(Base64EncoderByte).IncrementPadOne;

            public int GetMaxSrcLength(int srcLength, int destLength) =>
                default(Base64EncoderByte).GetMaxSrcLength(srcLength, destLength);

            public uint GetInPlaceDestinationLength(int encodedLength, int _) => 0; // not used for char encoding

            public int GetMaxEncodedLength(int _) => 0;  // not used for char encoding

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void EncodeOneOptionallyPadTwo(byte* oneByte, ushort* dest, ref byte encodingMap)
            {
                uint t0 = oneByte[0];

                uint i = t0 << 8;

                uint i0 = Unsafe.Add(ref encodingMap, (IntPtr)(i >> 10));
                uint i1 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 4) & 0x3F));

                dest[0] = (ushort)i0;
                dest[1] = (ushort)i1;
                dest[2] = (ushort)EncodingPad;
                dest[3] = (ushort)EncodingPad;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void EncodeTwoOptionallyPadOne(byte* twoBytes, ushort* dest, ref byte encodingMap)
            {
                uint t0 = twoBytes[0];
                uint t1 = twoBytes[1];

                uint i = (t0 << 16) | (t1 << 8);

                uint i0 = Unsafe.Add(ref encodingMap, (IntPtr)(i >> 18));
                uint i1 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 12) & 0x3F));
                uint i2 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 6) & 0x3F));

                dest[0] = (ushort)i0;
                dest[1] = (ushort)i1;
                dest[2] = (ushort)i2;
                dest[3] = (ushort)EncodingPad;
            }

#if NET
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void StoreVector512ToDestination(ushort* dest, ushort* destStart, int destLength, Vector512<byte> str)
            {
                Base64Helper.AssertWrite<Vector512<short>>(dest, destStart, destLength);
                (Vector512<ushort> utf16LowVector, Vector512<ushort> utf16HighVector) = Vector512.Widen(str);
                utf16LowVector.Store(dest);
                utf16HighVector.Store(dest + 32);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void StoreVector256ToDestination(ushort* dest, ushort* destStart, int destLength, Vector256<byte> str)
            {
                Base64Helper.AssertWrite<Vector256<short>>(dest, destStart, destLength);
                (Vector256<ushort> utf16LowVector, Vector256<ushort> utf16HighVector) = Vector256.Widen(str);
                utf16LowVector.Store(dest);
                utf16HighVector.Store(dest + 16);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void StoreVector128ToDestination(ushort* dest, ushort* destStart, int destLength, Vector128<byte> str)
            {
                Base64Helper.AssertWrite<Vector128<short>>(dest, destStart, destLength);
                (Vector128<ushort> utf16LowVector, Vector128<ushort> utf16HighVector) = Vector128.Widen(str);
                utf16LowVector.Store(dest);
                utf16HighVector.Store(dest + 8);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            public unsafe void StoreArmVector128x4ToDestination(ushort* dest, ushort* destStart, int destLength,
                Vector128<byte> res1, Vector128<byte> res2, Vector128<byte> res3, Vector128<byte> res4)
            {
                Base64Helper.AssertWrite<Vector128<short>>(dest, destStart, destLength);
                (Vector128<ushort> utf16LowVector1, Vector128<ushort> utf16HighVector1) = Vector128.Widen(res1);
                (Vector128<ushort> utf16LowVector2, Vector128<ushort> utf16HighVector2) = Vector128.Widen(res2);
                (Vector128<ushort> utf16LowVector3, Vector128<ushort> utf16HighVector3) = Vector128.Widen(res3);
                (Vector128<ushort> utf16LowVector4, Vector128<ushort> utf16HighVector4) = Vector128.Widen(res4);
                AdvSimd.Arm64.StoreVectorAndZip(dest, (utf16LowVector1, utf16LowVector2, utf16LowVector3, utf16LowVector4));
                AdvSimd.Arm64.StoreVectorAndZip(dest + 32, (utf16HighVector1, utf16HighVector2, utf16HighVector3, utf16HighVector4));
            }
#endif

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void EncodeThreeAndWrite(byte* threeBytes, ushort* destination, ref byte encodingMap)
            {
                uint t0 = threeBytes[0];
                uint t1 = threeBytes[1];
                uint t2 = threeBytes[2];

                uint i = (t0 << 16) | (t1 << 8) | t2;

                uint i0 = Unsafe.Add(ref encodingMap, (IntPtr)(i >> 18));
                uint i1 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 12) & 0x3F));
                uint i2 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 6) & 0x3F));
                uint i3 = Unsafe.Add(ref encodingMap, (IntPtr)(i & 0x3F));

                destination[0] = (ushort)i0;
                destination[1] = (ushort)i1;
                destination[2] = (ushort)i2;
                destination[3] = (ushort)i3;
            }
        }
    }
}
