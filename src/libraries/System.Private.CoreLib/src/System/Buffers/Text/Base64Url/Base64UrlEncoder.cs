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
    public static partial class Base64Url
    {
        /// <summary>
        /// Encodes the span of binary data into UTF-8 encoded text represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the UTF-8 encoded text in Base64Url.</param>
        /// <param name="bytesConsumed">When this method returns, contains the number of input bytes consumed during the operation. This can be used to slice the input for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
        /// <param name="isFinalBlock"><see langword="true"/> when the input span contains the entirety of data to encode; <see langword="false"/> when more data may follow,
        /// such as when calling in a loop, subsequent calls with <see langword="false"/> should end with <see langword="true"/> call. The default is <see langword="true" />.</param>
        /// <returns>One of the enumeration values that indicates the success or failure of the operation.</returns>
        /// <remarks>This implementation of the base64url encoding omits the optional padding characters.</remarks>
        public static OperationStatus EncodeToUtf8(ReadOnlySpan<byte> source,
            Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true) =>
            EncodeTo(default(Base64UrlEncoderByte), source, destination, out bytesConsumed, out bytesWritten, isFinalBlock);

        /// <summary>
        /// Returns the length (in bytes) of the result if you were to encode binary data within a byte span of size <paramref name="bytesLength"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="bytesLength"/> is less than 0 or greater than 1610612733.
        /// </exception>
        public static int GetEncodedLength(int bytesLength)
        {
#if NET
            ArgumentOutOfRangeException.ThrowIfGreaterThan<uint>((uint)bytesLength, MaximumEncodeLength);

            (uint whole, uint remainder) = uint.DivRem((uint)bytesLength, 3);

            return (int)(whole * 4 + (remainder > 0 ? remainder + 1 : 0)); // if remainder is 1 or 2, the encoded length will be 1 byte longer.
#else
            if ((uint)bytesLength > MaximumEncodeLength)
            {
                throw new ArgumentOutOfRangeException(nameof(bytesLength));
            }

            int remainder = (int)((uint)bytesLength % 3);

            return (bytesLength / 3) * 4 + (remainder > 0 ? remainder + 1 : 0);
#endif
        }

        /// <summary>
        /// Encodes the span of binary data into UTF-8 encoded text represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the UTF-8 encoded text in Base64Url.</param>
        /// <returns>The number of bytes written into the destination span. This can be used to slice the output for subsequent calls, if necessary.</returns>
        /// <exception cref="ArgumentException">The buffer in <paramref name="destination"/> is too small to hold the encoded output.</exception>
        /// <remarks>This implementation of the base64url encoding omits the optional padding characters.</remarks>
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
        /// Encodes the span of binary data into UTF-8 encoded text represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <returns>The output byte array which contains the result of the operation, i.e. the UTF-8 encoded text in Base64Url.</returns>
        /// <remarks>This implementation of the base64url encoding omits the optional padding characters.</remarks>
        public static byte[] EncodeToUtf8(ReadOnlySpan<byte> source)
        {
            byte[] destination = new byte[GetEncodedLength(source.Length)];
            EncodeToUtf8(source, destination, out _, out int bytesWritten);
            Debug.Assert(destination.Length == bytesWritten);

            return destination;
        }

        /// <summary>
        /// Encodes the span of binary data into unicode ASCII chars represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the ASCII chars in Base64Url.</param>
        /// <param name="bytesConsumed">>When this method returns, contains the number of input bytes consumed during the operation. This can be used to slice the input for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
        /// <param name="charsWritten">>When this method returns, contains the number of chars written into the output span. This can be used to slice the output for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
        /// <param name="isFinalBlock"><see langword="true"/> when the input span contains the entirety of data to encode; <see langword="false"/> when more data may follow,
        /// such as when calling in a loop, subsequent calls with <see langword="false"/> should end with <see langword="true"/> call. The default is <see langword="true" />.</param>
        /// <returns>One of the enumeration values that indicates the success or failure of the operation.</returns>
        /// <remarks>This implementation of the base64url encoding omits the optional padding characters.</remarks>
        public static OperationStatus EncodeToChars(ReadOnlySpan<byte> source, Span<char> destination,
            out int bytesConsumed, out int charsWritten, bool isFinalBlock = true) =>
            EncodeTo(default(Base64UrlEncoderChar), source, MemoryMarshal.Cast<char, ushort>(destination), out bytesConsumed, out charsWritten, isFinalBlock);

        /// <summary>
        /// Encodes the span of binary data into unicode ASCII chars represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the ASCII chars in Base64Url.</param>
        /// <returns>The number of bytes written into the destination span. This can be used to slice the output for subsequent calls, if necessary.</returns>
        /// <exception cref="ArgumentException">The buffer in <paramref name="destination"/> is too small to hold the encoded output.</exception>
        /// <remarks>This implementation of the base64url encoding omits the optional padding characters.</remarks>
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
        /// Encodes the span of binary data into unicode ASCII chars represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <returns>A char array which contains the result of the operation, i.e. the ASCII chars in Base64Url.</returns>
        /// <remarks>This implementation of the base64url encoding omits the optional padding characters.</remarks>
        public static char[] EncodeToChars(ReadOnlySpan<byte> source)
        {
            char[] destination = new char[GetEncodedLength(source.Length)];
            EncodeToChars(source, destination, out _, out int charsWritten);
            Debug.Assert(destination.Length == charsWritten);

            return destination;
        }

        /// <summary>
        /// Encodes the span of binary data into unicode string represented as Base64Url ASCII chars.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <returns>A string which contains the result of the operation, i.e. the ASCII string in Base64Url.</returns>
        /// <remarks>This implementation of the base64url encoding omits the optional padding characters.</remarks>
        public static unsafe string EncodeToString(ReadOnlySpan<byte> source)
        {
#if NET
            int encodedLength = GetEncodedLength(source.Length);

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            return string.Create(encodedLength, (IntPtr)(&source), static (buffer, spanPtr) =>
            {
                ReadOnlySpan<byte> source = *(ReadOnlySpan<byte>*)spanPtr;
                EncodeToChars(source, buffer, out _, out int charsWritten);
                Debug.Assert(buffer.Length == charsWritten, $"The source length: {source.Length}, bytes written: {charsWritten}");
            });
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
#else
            char[] destination = new char[GetEncodedLength(source.Length)];
            EncodeToChars(source, destination, out _, out int charsWritten);
            Debug.Assert(destination.Length == charsWritten);

            return new string(destination);
#endif
        }

        /// <summary>
        /// Encodes the span of binary data into unicode ASCII chars represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the ASCII chars in Base64Url.</param>
        /// <param name="charsWritten">When this method returns, contains the number of chars written into the output span. This can be used to slice the output for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
        /// <returns><see langword="true"/> if chars encoded successfully, otherwise <see langword="false"/>.</returns>
        /// <remarks>This implementation of the base64url encoding omits the optional padding characters.</remarks>
        public static bool TryEncodeToChars(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten)
        {
            OperationStatus status = EncodeToChars(source, destination, out _, out charsWritten);

            return status == OperationStatus.Done;
        }

        /// <summary>
        /// Encodes the span of binary data into UTF-8 encoded chars represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the UTF-8 encoded text in Base64Url.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of chars written into the output span. This can be used to slice the output for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
        /// <returns><see langword="true"/> if bytes encoded successfully, otherwise <see langword="false"/>.</returns>
        /// <remarks>This implementation of the base64url encoding omits the optional padding characters.</remarks>
        public static bool TryEncodeToUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            OperationStatus status = EncodeToUtf8(source, destination, out _, out bytesWritten);

            return status == OperationStatus.Done;
        }

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
        /// <remarks>This implementation of the base64url encoding omits the optional padding characters.</remarks>
        public static bool TryEncodeToUtf8InPlace(Span<byte> buffer, int dataLength, out int bytesWritten)
        {
            OperationStatus status = EncodeToUtf8InPlace(default(Base64UrlEncoderByte), buffer, dataLength, out bytesWritten);

            return status == OperationStatus.Done;
        }

        private readonly struct Base64UrlEncoderByte : Base64Helper.IBase64Encoder<byte>
        {
            public ReadOnlySpan<byte> EncodingMap => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_"u8;

            public sbyte Avx2LutChar62 => -17;  // char '-' diff

            public sbyte Avx2LutChar63 => 32;   // char '_' diff

            public ReadOnlySpan<byte> AdvSimdLut4 => "wxyz0123456789-_"u8;

            public uint Ssse3AdvSimdLutE3 => 0x000020EF;

            public int IncrementPadTwo => 2;

            public int IncrementPadOne => 3;

            public int GetMaxSrcLength(int srcLength, int destLength) =>
                srcLength <= MaximumEncodeLength && destLength >= GetEncodedLength(srcLength) ?
                srcLength : GetMaxDecodedLength(destLength);

            public uint GetInPlaceDestinationLength(int encodedLength, int leftOver) =>
                leftOver > 0 ? (uint)(encodedLength - leftOver - 1) : (uint)(encodedLength - 4);

            public int GetMaxEncodedLength(int srcLength) => GetEncodedLength(srcLength);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void EncodeOneOptionallyPadTwo(byte* oneByte, byte* dest, ref byte encodingMap)
            {
                uint t0 = oneByte[0];

                uint i = t0 << 8;

                uint i0 = Unsafe.Add(ref encodingMap, (IntPtr)(i >> 10));
                uint i1 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 4) & 0x3F));

                dest[0] = (byte)i0;
                dest[1] = (byte)i1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void EncodeTwoOptionallyPadOne(byte* twoBytes, byte* dest, ref byte encodingMap)
            {
                uint t0 = twoBytes[0];
                uint t1 = twoBytes[1];

                uint i = (t0 << 16) | (t1 << 8);

                uint i0 = Unsafe.Add(ref encodingMap, (IntPtr)(i >> 18));
                uint i1 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 12) & 0x3F));
                uint i2 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 6) & 0x3F));

                dest[0] = (byte)i0;
                dest[1] = (byte)i1;
                dest[2] = (byte)i2;
            }

#if NET
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void StoreVector512ToDestination(byte* dest, byte* destStart, int destLength, Vector512<byte> str) =>
                default(Base64EncoderByte).StoreVector512ToDestination(dest, destStart, destLength, str);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(Avx2))]
            public unsafe void StoreVector256ToDestination(byte* dest, byte* destStart, int destLength, Vector256<byte> str) =>
                default(Base64EncoderByte).StoreVector256ToDestination(dest, destStart, destLength, str);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void StoreVector128ToDestination(byte* dest, byte* destStart, int destLength, Vector128<byte> str) =>
                default(Base64EncoderByte).StoreVector128ToDestination(dest, destStart, destLength, str);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            public unsafe void StoreArmVector128x4ToDestination(byte* dest, byte* destStart, int destLength,
                Vector128<byte> res1, Vector128<byte> res2, Vector128<byte> res3, Vector128<byte> res4) =>
                default(Base64EncoderByte).StoreArmVector128x4ToDestination(dest, destStart, destLength, res1, res2, res3, res4);
#endif

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void EncodeThreeAndWrite(byte* threeBytes, byte* destination, ref byte encodingMap) =>
                default(Base64EncoderByte).EncodeThreeAndWrite(threeBytes, destination, ref encodingMap);
        }

        private readonly struct Base64UrlEncoderChar : IBase64Encoder<ushort>
        {
            public ReadOnlySpan<byte> EncodingMap => default(Base64UrlEncoderByte).EncodingMap;

            public sbyte Avx2LutChar62 => default(Base64UrlEncoderByte).Avx2LutChar62;

            public sbyte Avx2LutChar63 => default(Base64UrlEncoderByte).Avx2LutChar63;

            public ReadOnlySpan<byte> AdvSimdLut4 => default(Base64UrlEncoderByte).AdvSimdLut4;

            public uint Ssse3AdvSimdLutE3 => default(Base64UrlEncoderByte).Ssse3AdvSimdLutE3;

            public int IncrementPadTwo => default(Base64UrlEncoderByte).IncrementPadTwo;

            public int IncrementPadOne => default(Base64UrlEncoderByte).IncrementPadOne;

            public int GetMaxSrcLength(int srcLength, int destLength) =>
                default(Base64UrlEncoderByte).GetMaxSrcLength(srcLength, destLength);

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
            }

#if NET
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void StoreVector512ToDestination(ushort* dest, ushort* destStart, int destLength, Vector512<byte> str)
            {
                AssertWrite<Vector512<ushort>>(dest, destStart, destLength);
                (Vector512<ushort> utf16LowVector, Vector512<ushort> utf16HighVector) = Vector512.Widen(str);
                utf16LowVector.Store(dest);
                utf16HighVector.Store(dest + Vector512<ushort>.Count);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void StoreVector256ToDestination(ushort* dest, ushort* destStart, int destLength, Vector256<byte> str)
            {
                AssertWrite<Vector256<ushort>>(dest, destStart, destLength);
                (Vector256<ushort> utf16LowVector, Vector256<ushort> utf16HighVector) = Vector256.Widen(str);
                utf16LowVector.Store(dest);
                utf16HighVector.Store(dest + Vector256<ushort>.Count);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void StoreVector128ToDestination(ushort* dest, ushort* destStart, int destLength, Vector128<byte> str)
            {
                AssertWrite<Vector128<ushort>>(dest, destStart, destLength);
                (Vector128<ushort> utf16LowVector, Vector128<ushort> utf16HighVector) = Vector128.Widen(str);
                utf16LowVector.Store(dest);
                utf16HighVector.Store(dest + Vector128<ushort>.Count);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            public unsafe void StoreArmVector128x4ToDestination(ushort* dest, ushort* destStart, int destLength,
                Vector128<byte> res1, Vector128<byte> res2, Vector128<byte> res3, Vector128<byte> res4)
            {
                AssertWrite<Vector128<ushort>>(dest, destStart, destLength);
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

                byte i0 = Unsafe.Add(ref encodingMap, (IntPtr)(i >> 18));
                byte i1 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 12) & 0x3F));
                byte i2 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 6) & 0x3F));
                byte i3 = Unsafe.Add(ref encodingMap, (IntPtr)(i & 0x3F));

                destination[0] = i0;
                destination[1] = i1;
                destination[2] = i2;
                destination[3] = i3;
            }
        }
    }
}
