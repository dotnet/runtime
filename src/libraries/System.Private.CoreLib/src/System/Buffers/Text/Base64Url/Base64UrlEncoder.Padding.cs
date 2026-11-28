// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using static System.Buffers.Text.Base64Helper;

namespace System.Buffers.Text
{
    // These overloads are not compiled into Microsoft.Bcl.Memory. Its net10.0 asset
    // type-forwards Base64Url to a framework that does not have them, so a library
    // compiled against the netstandard asset would fail at run time on .NET 10.
    public static partial class Base64Url
    {
        /// <summary>
        /// Encodes the span of binary data into UTF-8 encoded text represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the UTF-8 encoded text in Base64Url.</param>
        /// <param name="bytesConsumed">When this method returns, contains the number of input bytes consumed during the operation. This can be used to slice the input for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
        /// <param name="options">One of the enumeration values that specifies whether to include padding characters in the encoded output.</param>
        /// <param name="isFinalBlock"><see langword="true"/> when the input span contains the entirety of data to encode; <see langword="false"/> when more data may follow,
        /// such as when calling in a loop, subsequent calls with <see langword="false"/> should end with <see langword="true"/> call. The default is <see langword="true" />.</param>
        /// <returns>One of the enumeration values that indicates the success or failure of the operation.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a valid <see cref="Base64UrlFormattingOptions"/> value.</exception>
        /// <remarks>Padding characters are appended only when <paramref name="options"/> includes <see cref="Base64UrlFormattingOptions.IncludePadding"/> and <paramref name="isFinalBlock"/> is <see langword="true"/>.</remarks>
        public static OperationStatus EncodeToUtf8(ReadOnlySpan<byte> source,
            Span<byte> destination, out int bytesConsumed, out int bytesWritten, Base64UrlFormattingOptions options, bool isFinalBlock = true)
        {
            ValidateOptions(options);

            // Padding is only written for the final block, so a non-final call uses the unpadded
            // encoder. That keeps its destination sizing from reporting DestinationTooSmall for
            // padding that would not be written.
            return (options & Base64UrlFormattingOptions.IncludePadding) != 0 && isFinalBlock ?
                EncodeTo(default(Base64UrlPaddedEncoderByte), source, destination, out bytesConsumed, out bytesWritten, isFinalBlock) :
                EncodeTo(default(Base64UrlEncoderByte), source, destination, out bytesConsumed, out bytesWritten, isFinalBlock);
        }

        /// <summary>
        /// Returns the number of characters required to encode
        /// <paramref name="bytesLength"/> bytes to Base64Url with the specified options.
        /// </summary>
        /// <param name="bytesLength">The number of bytes to encode.</param>
        /// <param name="options">One of the enumeration values that specifies whether to include padding characters in the encoded output.</param>
        /// <returns>The number of characters required to encode <paramref name="bytesLength"/> bytes.</returns>
        /// <remarks>
        /// Because the Base64Url characters are all in the 7-bit ASCII range,
        /// the number of characters is equal both to the number of
        /// <see cref="char"/> values written by methods like
        /// <see cref="EncodeToChars(ReadOnlySpan{byte}, Span{char}, Base64UrlFormattingOptions)"/>
        /// and the number of <see cref="byte"/> values written by UTF-8 methods like
        /// <see cref="EncodeToUtf8(ReadOnlySpan{byte}, Span{byte}, Base64UrlFormattingOptions)"/>.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="bytesLength"/> is less than 0 or greater than 1610612733, or
        /// <paramref name="options"/> is not a valid <see cref="Base64UrlFormattingOptions"/> value.
        /// </exception>
        public static int GetEncodedLength(int bytesLength, Base64UrlFormattingOptions options)
        {
            ValidateOptions(options);

            return (options & Base64UrlFormattingOptions.IncludePadding) != 0 ?
                GetPaddedEncodedLength(bytesLength) :
                GetEncodedLength(bytesLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetPaddedEncodedLength(int bytesLength)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan<uint>((uint)bytesLength, MaximumEncodeLength);

            return ((bytesLength + 2) / 3) * 4;
        }

        private static void ValidateOptions(Base64UrlFormattingOptions options)
        {
            if ((options & ~Base64UrlFormattingOptions.IncludePadding) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options), options, SR.Format(SR.Arg_EnumIllegalVal, options));
            }
        }

        /// <summary>
        /// Encodes the span of binary data into UTF-8 encoded text represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the UTF-8 encoded text in Base64Url.</param>
        /// <param name="options">One of the enumeration values that specifies whether to include padding characters in the encoded output.</param>
        /// <returns>The number of bytes written into the destination span. This can be used to slice the output for subsequent calls, if necessary.</returns>
        /// <exception cref="ArgumentException">The buffer in <paramref name="destination"/> is too small to hold the encoded output.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a valid <see cref="Base64UrlFormattingOptions"/> value.</exception>
        /// <remarks>Padding characters are appended only when <paramref name="options"/> includes <see cref="Base64UrlFormattingOptions.IncludePadding"/>.</remarks>
        public static int EncodeToUtf8(ReadOnlySpan<byte> source, Span<byte> destination, Base64UrlFormattingOptions options)
        {
            OperationStatus status = EncodeToUtf8(source, destination, out _, out int bytesWritten, options);

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
        /// <param name="options">One of the enumeration values that specifies whether to include padding characters in the encoded output.</param>
        /// <returns>The output byte array which contains the result of the operation, i.e. the UTF-8 encoded text in Base64Url.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a valid <see cref="Base64UrlFormattingOptions"/> value.</exception>
        /// <remarks>Padding characters are appended only when <paramref name="options"/> includes <see cref="Base64UrlFormattingOptions.IncludePadding"/>.</remarks>
        public static byte[] EncodeToUtf8(ReadOnlySpan<byte> source, Base64UrlFormattingOptions options)
        {
            byte[] destination = new byte[GetEncodedLength(source.Length, options)];
            EncodeToUtf8(source, destination, out _, out int bytesWritten, options);
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
        /// <param name="options">One of the enumeration values that specifies whether to include padding characters in the encoded output.</param>
        /// <param name="isFinalBlock"><see langword="true"/> when the input span contains the entirety of data to encode; <see langword="false"/> when more data may follow,
        /// such as when calling in a loop, subsequent calls with <see langword="false"/> should end with <see langword="true"/> call. The default is <see langword="true" />.</param>
        /// <returns>One of the enumeration values that indicates the success or failure of the operation.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a valid <see cref="Base64UrlFormattingOptions"/> value.</exception>
        /// <remarks>Padding characters are appended only when <paramref name="options"/> includes <see cref="Base64UrlFormattingOptions.IncludePadding"/> and <paramref name="isFinalBlock"/> is <see langword="true"/>.</remarks>
        public static OperationStatus EncodeToChars(ReadOnlySpan<byte> source, Span<char> destination,
            out int bytesConsumed, out int charsWritten, Base64UrlFormattingOptions options, bool isFinalBlock = true)
        {
            ValidateOptions(options);

            return (options & Base64UrlFormattingOptions.IncludePadding) != 0 && isFinalBlock ?
                EncodeTo(default(Base64UrlPaddedEncoderChar), source, MemoryMarshal.Cast<char, ushort>(destination), out bytesConsumed, out charsWritten, isFinalBlock) :
                EncodeTo(default(Base64UrlEncoderChar), source, MemoryMarshal.Cast<char, ushort>(destination), out bytesConsumed, out charsWritten, isFinalBlock);
        }

        /// <summary>
        /// Encodes the span of binary data into unicode ASCII chars represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the ASCII chars in Base64Url.</param>
        /// <param name="options">One of the enumeration values that specifies whether to include padding characters in the encoded output.</param>
        /// <returns>The number of chars written into the destination span. This can be used to slice the output for subsequent calls, if necessary.</returns>
        /// <exception cref="ArgumentException">The buffer in <paramref name="destination"/> is too small to hold the encoded output.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a valid <see cref="Base64UrlFormattingOptions"/> value.</exception>
        /// <remarks>Padding characters are appended only when <paramref name="options"/> includes <see cref="Base64UrlFormattingOptions.IncludePadding"/>.</remarks>
        public static int EncodeToChars(ReadOnlySpan<byte> source, Span<char> destination, Base64UrlFormattingOptions options)
        {
            OperationStatus status = EncodeToChars(source, destination, out _, out int charsWritten, options);

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
        /// <param name="options">One of the enumeration values that specifies whether to include padding characters in the encoded output.</param>
        /// <returns>A char array which contains the result of the operation, i.e. the ASCII chars in Base64Url.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a valid <see cref="Base64UrlFormattingOptions"/> value.</exception>
        /// <remarks>Padding characters are appended only when <paramref name="options"/> includes <see cref="Base64UrlFormattingOptions.IncludePadding"/>.</remarks>
        public static char[] EncodeToChars(ReadOnlySpan<byte> source, Base64UrlFormattingOptions options)
        {
            char[] destination = new char[GetEncodedLength(source.Length, options)];
            EncodeToChars(source, destination, out _, out int charsWritten, options);
            Debug.Assert(destination.Length == charsWritten);

            return destination;
        }

        /// <summary>
        /// Encodes the span of binary data into unicode string represented as Base64Url ASCII chars.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="options">One of the enumeration values that specifies whether to include padding characters in the encoded output.</param>
        /// <returns>A string which contains the result of the operation, i.e. the ASCII string in Base64Url.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a valid <see cref="Base64UrlFormattingOptions"/> value.</exception>
        /// <remarks>Padding characters are appended only when <paramref name="options"/> includes <see cref="Base64UrlFormattingOptions.IncludePadding"/>.</remarks>
        public static unsafe string EncodeToString(ReadOnlySpan<byte> source, Base64UrlFormattingOptions options)
        {
            int encodedLength = GetEncodedLength(source.Length, options);
            return string.Create(encodedLength, (sourcePtr: (IntPtr)(&source), options), static (buffer, state) =>
            {
                ReadOnlySpan<byte> source = *(ReadOnlySpan<byte>*)state.sourcePtr;
                EncodeToChars(source, buffer, out _, out int charsWritten, state.options);
                Debug.Assert(buffer.Length == charsWritten, $"The source length: {source.Length}, bytes written: {charsWritten}");
            });
        }

        /// <summary>
        /// Encodes the span of binary data into unicode ASCII chars represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the ASCII chars in Base64Url.</param>
        /// <param name="charsWritten">When this method returns, contains the number of chars written into the output span. This can be used to slice the output for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
        /// <param name="options">One of the enumeration values that specifies whether to include padding characters in the encoded output.</param>
        /// <returns><see langword="true"/> if chars encoded successfully, otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a valid <see cref="Base64UrlFormattingOptions"/> value.</exception>
        /// <remarks>Padding characters are appended only when <paramref name="options"/> includes <see cref="Base64UrlFormattingOptions.IncludePadding"/>.</remarks>
        public static bool TryEncodeToChars(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten, Base64UrlFormattingOptions options)
        {
            OperationStatus status = EncodeToChars(source, destination, out _, out charsWritten, options);

            return status == OperationStatus.Done;
        }

        /// <summary>
        /// Encodes the span of binary data into UTF-8 encoded text represented as Base64Url.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the UTF-8 encoded text in Base64Url.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
        /// <param name="options">One of the enumeration values that specifies whether to include padding characters in the encoded output.</param>
        /// <returns><see langword="true"/> if bytes encoded successfully, otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a valid <see cref="Base64UrlFormattingOptions"/> value.</exception>
        /// <remarks>Padding characters are appended only when <paramref name="options"/> includes <see cref="Base64UrlFormattingOptions.IncludePadding"/>.</remarks>
        public static bool TryEncodeToUtf8(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, Base64UrlFormattingOptions options)
        {
            OperationStatus status = EncodeToUtf8(source, destination, out _, out bytesWritten, options);

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
        /// <param name="options">One of the enumeration values that specifies whether to include padding characters in the encoded output.</param>
        /// <returns><see langword="true"/> if bytes encoded successfully, otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a valid <see cref="Base64UrlFormattingOptions"/> value.</exception>
        /// <remarks>Padding characters are appended only when <paramref name="options"/> includes <see cref="Base64UrlFormattingOptions.IncludePadding"/>.</remarks>
        public static bool TryEncodeToUtf8InPlace(Span<byte> buffer, int dataLength, out int bytesWritten, Base64UrlFormattingOptions options)
        {
            ValidateOptions(options);

            OperationStatus status = (options & Base64UrlFormattingOptions.IncludePadding) != 0 ?
                EncodeToUtf8InPlace(default(Base64UrlPaddedEncoderByte), buffer, dataLength, out bytesWritten) :
                EncodeToUtf8InPlace(default(Base64UrlEncoderByte), buffer, dataLength, out bytesWritten);

            return status == OperationStatus.Done;
        }

        /// <summary>
        /// Base64Url encoder that appends '=' padding, so that the encoded length is always a multiple of four.
        /// Uses the Base64Url alphabet with the padding behavior of <see cref="Base64EncoderByte"/>.
        /// </summary>
        private readonly struct Base64UrlPaddedEncoderByte : IBase64Encoder<byte>
        {
            public ReadOnlySpan<byte> EncodingMap => default(Base64UrlEncoderByte).EncodingMap;

            public sbyte Avx2LutChar62 => default(Base64UrlEncoderByte).Avx2LutChar62;

            public sbyte Avx2LutChar63 => default(Base64UrlEncoderByte).Avx2LutChar63;

            public ReadOnlySpan<byte> AdvSimdLut4 => default(Base64UrlEncoderByte).AdvSimdLut4;

            public uint Ssse3AdvSimdLutE3 => default(Base64UrlEncoderByte).Ssse3AdvSimdLutE3;

            public int IncrementPadTwo => 4;

            public int IncrementPadOne => 4;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetMaxSrcLength(int srcLength, int destLength) =>
                srcLength <= MaximumEncodeLength && destLength >= GetPaddedEncodedLength(srcLength) ?
                srcLength : (destLength >> 2) * 3;

            public uint GetInPlaceDestinationLength(int encodedLength, int _) => (uint)(encodedLength - 4);

            public int GetMaxEncodedLength(int srcLength) => GetPaddedEncodedLength(srcLength);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void EncodeOneOptionallyPadTwo(byte* oneByte, byte* dest, ref byte encodingMap) =>
                default(Base64EncoderByte).EncodeOneOptionallyPadTwo(oneByte, dest, ref encodingMap);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void EncodeTwoOptionallyPadOne(byte* twoBytes, byte* dest, ref byte encodingMap) =>
                default(Base64EncoderByte).EncodeTwoOptionallyPadOne(twoBytes, dest, ref encodingMap);

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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void EncodeThreeAndWrite(byte* threeBytes, byte* destination, ref byte encodingMap) =>
                default(Base64EncoderByte).EncodeThreeAndWrite(threeBytes, destination, ref encodingMap);
        }

        private readonly struct Base64UrlPaddedEncoderChar : IBase64Encoder<ushort>
        {
            public ReadOnlySpan<byte> EncodingMap => default(Base64UrlEncoderByte).EncodingMap;

            public sbyte Avx2LutChar62 => default(Base64UrlEncoderByte).Avx2LutChar62;

            public sbyte Avx2LutChar63 => default(Base64UrlEncoderByte).Avx2LutChar63;

            public ReadOnlySpan<byte> AdvSimdLut4 => default(Base64UrlEncoderByte).AdvSimdLut4;

            public uint Ssse3AdvSimdLutE3 => default(Base64UrlEncoderByte).Ssse3AdvSimdLutE3;

            public int IncrementPadTwo => default(Base64UrlPaddedEncoderByte).IncrementPadTwo;

            public int IncrementPadOne => default(Base64UrlPaddedEncoderByte).IncrementPadOne;

            public int GetMaxSrcLength(int srcLength, int destLength) =>
                default(Base64UrlPaddedEncoderByte).GetMaxSrcLength(srcLength, destLength);

            public uint GetInPlaceDestinationLength(int encodedLength, int _) => 0; // not used for char encoding

            public int GetMaxEncodedLength(int _) => 0;  // not used for char encoding

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void EncodeOneOptionallyPadTwo(byte* oneByte, ushort* dest, ref byte encodingMap) =>
                default(Base64EncoderChar).EncodeOneOptionallyPadTwo(oneByte, dest, ref encodingMap);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void EncodeTwoOptionallyPadOne(byte* twoBytes, ushort* dest, ref byte encodingMap) =>
                default(Base64EncoderChar).EncodeTwoOptionallyPadOne(twoBytes, dest, ref encodingMap);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void StoreVector512ToDestination(ushort* dest, ushort* destStart, int destLength, Vector512<byte> str) =>
                default(Base64EncoderChar).StoreVector512ToDestination(dest, destStart, destLength, str);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void StoreVector256ToDestination(ushort* dest, ushort* destStart, int destLength, Vector256<byte> str) =>
                default(Base64EncoderChar).StoreVector256ToDestination(dest, destStart, destLength, str);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void StoreVector128ToDestination(ushort* dest, ushort* destStart, int destLength, Vector128<byte> str) =>
                default(Base64EncoderChar).StoreVector128ToDestination(dest, destStart, destLength, str);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            public unsafe void StoreArmVector128x4ToDestination(ushort* dest, ushort* destStart, int destLength,
                Vector128<byte> res1, Vector128<byte> res2, Vector128<byte> res3, Vector128<byte> res4) =>
                default(Base64EncoderChar).StoreArmVector128x4ToDestination(dest, destStart, destLength, res1, res2, res3, res4);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void EncodeThreeAndWrite(byte* threeBytes, ushort* destination, ref byte encodingMap) =>
                default(Base64EncoderChar).EncodeThreeAndWrite(threeBytes, destination, ref encodingMap);
        }
    }
}
