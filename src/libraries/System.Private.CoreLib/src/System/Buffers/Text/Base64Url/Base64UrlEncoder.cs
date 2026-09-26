// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        /// Returns the number of characters required to encode
        /// <paramref name="bytesLength"/> bytes to Base64Url.
        /// </summary>
        /// <remarks>
        /// Because the Base64Url characters are all in the 7-bit ASCII range,
        /// the number of characters is equal both to the number of
        /// <see cref="char"/> values written by methods like
        /// <see cref="EncodeToChars(ReadOnlySpan{byte}, Span{char})"/>
        /// and the number of <see cref="byte"/> values written by UTF-8 methods like
        /// <see cref="EncodeToUtf8(ReadOnlySpan{byte}, Span{byte})"/>.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="bytesLength"/> is less than 0 or greater than 1610612733.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetEncodedLength(int bytesLength)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan<uint>((uint)bytesLength, MaximumEncodeLength);

            uint whole = (uint)bytesLength / 3;
            uint remainder = (uint)bytesLength % 3;

            return (int)(whole * 4 + (remainder > 0 ? remainder + 1 : 0)); // if remainder is 1 or 2, the encoded length will be 1 byte longer.
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
        /// <param name="bytesConsumed">When this method returns, contains the number of input bytes consumed during the operation. This can be used to slice the input for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
        /// <param name="charsWritten">When this method returns, contains the number of chars written into the output span. This can be used to slice the output for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
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
        /// <returns>The number of chars written into the destination span. This can be used to slice the output for subsequent calls, if necessary.</returns>
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
        public static string EncodeToString(ReadOnlySpan<byte> source)
        {
#if NET
            int encodedLength = GetEncodedLength(source.Length);

            return string.Create(encodedLength, source, static (buffer, source) =>
            {
                EncodeToChars(source, buffer, out _, out int charsWritten);
                Debug.Assert(buffer.Length == charsWritten, $"The source length: {source.Length}, bytes written: {charsWritten}");
            });
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
        /// Encodes the span of binary data into UTF-8 encoded text represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the UTF-8 encoded text in Base64Url.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
        /// <returns><see langword="true"/> if bytes encoded successfully, otherwise <see langword="false"/>.</returns>
        /// <remarks>This implementation of the base64url encoding omits the optional padding characters.</remarks>
        public static bool TryEncodeToUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            OperationStatus status = EncodeToUtf8(source, destination, out _, out bytesWritten);

            return status == OperationStatus.Done;
        }

        /// <summary>
        /// Encodes the span of binary data (in-place) into UTF-8 encoded text represented as Base64Url.
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
            public void EncodeOneOptionallyPadTwo(ReadOnlySpan<byte> oneByte, Span<byte> dest, ReadOnlySpan<byte> encodingMap)
            {
                uint t0 = oneByte[0];

                uint i0 = encodingMap[(int)(t0 >> 2)];
                uint i1 = encodingMap[(int)(t0 << 4) & 0x3F];

                BinaryPrimitives.WriteUInt16LittleEndian(dest, (ushort)(i0 | (i1 << 8)));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void EncodeTwoOptionallyPadOne(ReadOnlySpan<byte> twoBytes, Span<byte> dest, ReadOnlySpan<byte> encodingMap)
            {
                uint i = ((uint)twoBytes[0] << 16) | ((uint)twoBytes[1] << 8);

                byte i0 = encodingMap[(int)(i >> 18) & 0x3F];
                byte i1 = encodingMap[(int)(i >> 12) & 0x3F];
                byte i2 = encodingMap[(int)(i >> 6) & 0x3F];

                dest[2] = i2;
                dest[1] = i1;
                dest[0] = i0;
            }

#if NET
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void StoreVector512ToDestination(Span<byte> dest, Vector512<byte> str) =>
                default(Base64EncoderByte).StoreVector512ToDestination(dest, str);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void StoreVector256ToDestination(Span<byte> dest, Vector256<byte> str) =>
                default(Base64EncoderByte).StoreVector256ToDestination(dest, str);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void StoreVector128ToDestination(Span<byte> dest, Vector128<byte> str) =>
                default(Base64EncoderByte).StoreVector128ToDestination(dest, str);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            public void StoreArmVector128x4ToDestination(Span<byte> dest,
                Vector128<byte> res1, Vector128<byte> res2, Vector128<byte> res3, Vector128<byte> res4) =>
                default(Base64EncoderByte).StoreArmVector128x4ToDestination(dest, res1, res2, res3, res4);
#endif // NET

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void EncodeThreeAndWrite(ReadOnlySpan<byte> threeBytes, Span<byte> destination, ReadOnlySpan<byte> encodingMap) =>
                default(Base64EncoderByte).EncodeThreeAndWrite(threeBytes, destination, encodingMap);
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
            public void EncodeOneOptionallyPadTwo(ReadOnlySpan<byte> oneByte, Span<ushort> dest, ReadOnlySpan<byte> encodingMap) =>
                Base64Helper.EncodeOneOptionallyPadTwo(oneByte, dest, encodingMap);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void EncodeTwoOptionallyPadOne(ReadOnlySpan<byte> twoBytes, Span<ushort> dest, ReadOnlySpan<byte> encodingMap) =>
                Base64Helper.EncodeTwoOptionallyPadOne(twoBytes, dest, encodingMap);

#if NET
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void StoreVector512ToDestination(Span<ushort> dest, Vector512<byte> str) =>
                default(Base64EncoderChar).StoreVector512ToDestination(dest, str);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void StoreVector256ToDestination(Span<ushort> dest, Vector256<byte> str) =>
                default(Base64EncoderChar).StoreVector256ToDestination(dest, str);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void StoreVector128ToDestination(Span<ushort> dest, Vector128<byte> str) =>
                default(Base64EncoderChar).StoreVector128ToDestination(dest, str);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            public void StoreArmVector128x4ToDestination(Span<ushort> dest,
                Vector128<byte> res1, Vector128<byte> res2, Vector128<byte> res3, Vector128<byte> res4) =>
                default(Base64EncoderChar).StoreArmVector128x4ToDestination(dest, res1, res2, res3, res4);
#endif // NET

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void EncodeThreeAndWrite(ReadOnlySpan<byte> threeBytes, Span<ushort> destination, ReadOnlySpan<byte> encodingMap) =>
                default(Base64EncoderChar).EncodeThreeAndWrite(threeBytes, destination, encodingMap);
        }
    }
}
