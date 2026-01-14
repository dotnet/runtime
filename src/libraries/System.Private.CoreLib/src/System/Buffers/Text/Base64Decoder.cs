// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Text;
using static System.Buffers.Text.Base64Helper;

namespace System.Buffers.Text
{
    // AVX2 version based on https://github.com/aklomp/base64/tree/e516d769a2a432c08404f1981e73b431566057be/lib/arch/avx2
    // Vector128 version based on https://github.com/aklomp/base64/tree/e516d769a2a432c08404f1981e73b431566057be/lib/arch/ssse3
    public static partial class Base64
    {
        private const int MaxStackallocThreshold = 256;

        /// <summary>
        /// Returns the maximum length (in bytes) of the result if you were to decode base 64 encoded text from a span of size <paramref name="base64Length"/>.
        /// </summary>
        /// <param name="base64Length">The length of the base64-encoded input.</param>
        /// <returns>The maximum number of bytes that decoding could produce.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="base64Length"/> is less than 0.
        /// </exception>
        /// <remarks>
        /// This method is equivalent to <see cref="GetMaxDecodedFromUtf8Length(int)"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMaxDecodedLength(int base64Length)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(base64Length);

            return (base64Length >> 2) * 3;
        }

        /// <summary>
        /// Returns the maximum length (in bytes) of the result if you were to decode base 64 encoded text within a byte span of size "length".
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="length"/> is less than 0.
        /// </exception>
        /// <remarks>
        /// This method is equivalent to <see cref="GetMaxDecodedLength(int)"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMaxDecodedFromUtf8Length(int length) => GetMaxDecodedLength(length);

        /// <summary>
        /// Decode the span of UTF-8 encoded text represented as base64 into binary data.
        /// If the input is not a multiple of 4, it will decode as much as it can, to the closest multiple of 4.
        /// </summary>
        /// <param name="utf8">The input span which contains UTF-8 encoded text in base64 that needs to be decoded.</param>
        /// <param name="bytes">The output span which contains the result of the operation, i.e. the decoded binary data.</param>
        /// <param name="bytesConsumed">The number of input bytes consumed during the operation. This can be used to slice the input for subsequent calls, if necessary.</param>
        /// <param name="bytesWritten">The number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary.</param>
        /// <param name="isFinalBlock"><see langword="true"/> (default) when the input span contains the entire data to decode.
        /// Set to <see langword="true"/> when the source buffer contains the entirety of the data to decode.
        /// Set to <see langword="false"/> if this method is being called in a loop and if more input data may follow.
        /// At the end of the loop, call this (potentially with an empty source buffer) passing <see langword="true"/>.</param>
        /// <returns>It returns the OperationStatus enum values:
        /// - Done - on successful processing of the entire input span
        /// - DestinationTooSmall - if there is not enough space in the output span to fit the decoded input
        /// - NeedMoreData - only if <paramref name="isFinalBlock"/> is false and the input is not a multiple of 4, otherwise the partial input would be considered as InvalidData
        /// - InvalidData - if the input contains bytes outside of the expected base64 range, or if it contains invalid/more than two padding characters,
        ///   or if the input is incomplete (i.e. not a multiple of 4) and <paramref name="isFinalBlock"/> is <see langword="true"/>.
        /// </returns>
        public static OperationStatus DecodeFromUtf8(ReadOnlySpan<byte> utf8, Span<byte> bytes, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true) =>
            DecodeFrom(default(Base64DecoderByte), utf8, bytes, out bytesConsumed, out bytesWritten, isFinalBlock, ignoreWhiteSpace: true);

        /// <summary>
        /// Decodes the span of UTF-8 encoded text represented as Base64 into binary data.
        /// </summary>
        /// <param name="source">The input span which contains UTF-8 encoded text in Base64 that needs to be decoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the decoded binary data.</param>
        /// <returns>The number of bytes written into <paramref name="destination"/>. This can be used to slice the output for subsequent calls, if necessary.</returns>
        /// <exception cref="ArgumentException">The buffer in <paramref name="destination"/> is too small to hold the encoded output.</exception>
        /// <exception cref="FormatException"><paramref name="source"/> contains an invalid Base64 character,
        /// more than two padding characters, or a non white space character among the padding characters.</exception>
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

            Debug.Assert(status == OperationStatus.InvalidData);
            throw new FormatException(SR.Format_BadBase64Char);
        }

        /// <summary>
        /// Decodes the span of UTF-8 encoded text represented as Base64 into binary data.
        /// </summary>
        /// <param name="source">The input span which contains UTF-8 encoded text in Base64 that needs to be decoded.</param>
        /// <returns>A byte array which contains the result of the decoding operation.</returns>
        /// <exception cref="FormatException"><paramref name="source"/> contains an invalid Base64 character,
        /// more than two padding characters, or a non white space character among the padding characters.</exception>
        public static byte[] DecodeFromUtf8(ReadOnlySpan<byte> source)
        {
            int upperBound = GetMaxDecodedLength(source.Length);
            byte[]? rented = null;

            Span<byte> destination = (uint)upperBound <= MaxStackallocThreshold
                ? stackalloc byte[MaxStackallocThreshold]
                : (rented = ArrayPool<byte>.Shared.Rent(upperBound));

            OperationStatus status = DecodeFromUtf8(source, destination, out _, out int bytesWritten);
            Debug.Assert(status is OperationStatus.Done or OperationStatus.InvalidData);
            byte[] result = destination.Slice(0, bytesWritten).ToArray();

            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }

            return status == OperationStatus.Done ? result : throw new FormatException(SR.Format_BadBase64Char);
        }

        /// <summary>
        /// Decodes the span of UTF-8 encoded text represented as Base64 into binary data.
        /// </summary>
        /// <param name="source">The input span which contains UTF-8 encoded text in Base64 that needs to be decoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the decoded binary data.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
        /// <returns><see langword="true"/> if bytes decoded successfully, otherwise <see langword="false"/>.</returns>
        /// <exception cref="FormatException"><paramref name="source"/> contains an invalid Base64 character,
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
        /// Decode the span of UTF-8 encoded text in base 64 (in-place) into binary data.
        /// The decoded binary output is smaller than the text data contained in the input (the operation deflates the data).
        /// If the input is not a multiple of 4, it will not decode any.
        /// </summary>
        /// <param name="buffer">The input span which contains the base 64 text data that needs to be decoded.</param>
        /// <param name="bytesWritten">The number of bytes written into the buffer.</param>
        /// <returns>It returns the OperationStatus enum values:
        /// - Done - on successful processing of the entire input span
        /// - InvalidData - if the input contains bytes outside of the expected base 64 range, or if it contains invalid/more than two padding characters,
        ///   or if the input is incomplete (i.e. not a multiple of 4).
        /// It does not return DestinationTooSmall since that is not possible for base 64 decoding.
        /// It does not return NeedMoreData since this method tramples the data in the buffer and
        /// hence can only be called once with all the data in the buffer.
        /// </returns>
        public static OperationStatus DecodeFromUtf8InPlace(Span<byte> buffer, out int bytesWritten) =>
            Base64Helper.DecodeFromUtf8InPlace(default(Base64DecoderByte), buffer, out bytesWritten, ignoreWhiteSpace: true);

        /// <summary>
        /// Decodes the span of unicode ASCII chars represented as Base64 into binary data.
        /// </summary>
        /// <param name="source">The input span which contains unicode ASCII chars in Base64 that needs to be decoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the decoded binary data.</param>
        /// <param name="charsConsumed">When this method returns, contains the number of input chars consumed during the operation. This can be used to slice the input for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
        /// <param name="isFinalBlock"><see langword="true"/> when the input span contains the entirety of data to encode; <see langword="false"/> when more data may follow,
        /// such as when calling in a loop. Calls with <see langword="false"/> should be followed up with another call where this parameter is <see langword="true"/>. The default is <see langword="true" />.</param>
        /// <returns>One of the enumeration values that indicates the success or failure of the operation.</returns>
        public static OperationStatus DecodeFromChars(ReadOnlySpan<char> source, Span<byte> destination,
            out int charsConsumed, out int bytesWritten, bool isFinalBlock = true) =>
            DecodeFrom(default(Base64DecoderChar), MemoryMarshal.Cast<char, ushort>(source), destination,
                out charsConsumed, out bytesWritten, isFinalBlock, ignoreWhiteSpace: true);

        /// <summary>
        /// Decodes the span of unicode ASCII chars represented as Base64 into binary data.
        /// </summary>
        /// <param name="source">The input span which contains ASCII chars in Base64 that needs to be decoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the decoded binary data.</param>
        /// <returns>The number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary.</returns>
        /// <exception cref="ArgumentException">The buffer in <paramref name="destination"/> is too small to hold the encoded output.</exception>
        /// <exception cref="FormatException"><paramref name="source"/> contains an invalid Base64 character,
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
        /// Decodes the span of unicode ASCII chars represented as Base64 into binary data.
        /// </summary>
        /// <param name="source">The input span which contains ASCII chars in Base64 that needs to be decoded.</param>
        /// <param name="destination">The output span which contains the result of the operation, i.e. the decoded binary data.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary. This parameter is treated as uninitialized.</param>
        /// <returns><see langword="true"/> if bytes decoded successfully, otherwise <see langword="false"/>.</returns>
        /// <exception cref="FormatException"><paramref name="source"/> contains an invalid Base64 character,
        /// more than two padding characters, or a non white space character among the padding characters.</exception>
        public static bool TryDecodeFromChars(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten)
        {
            OperationStatus status = DecodeFromChars(source, destination, out _, out bytesWritten);

            if (status == OperationStatus.InvalidData)
            {
                throw new FormatException(SR.Format_BadBase64Char);
            }

            Debug.Assert(status is OperationStatus.Done or OperationStatus.DestinationTooSmall);
            return status == OperationStatus.Done;
        }

        /// <summary>
        /// Decodes the span of unicode ASCII chars represented as Base64 into binary data.
        /// </summary>
        /// <param name="source">The input span which contains ASCII chars in Base64 that needs to be decoded.</param>
        /// <returns>A byte array which contains the result of the decoding operation.</returns>
        /// <exception cref="FormatException"><paramref name="source"/> contains an invalid Base64 character,
        /// more than two padding characters, or a non white space character among the padding characters.</exception>
        public static byte[] DecodeFromChars(ReadOnlySpan<char> source)
        {
            int upperBound = GetMaxDecodedLength(source.Length);
            byte[]? rented = null;

            Span<byte> destination = (uint)upperBound <= MaxStackallocThreshold
                ? stackalloc byte[MaxStackallocThreshold]
                : (rented = ArrayPool<byte>.Shared.Rent(upperBound));

            OperationStatus status = DecodeFromChars(source, destination, out _, out int bytesWritten);
            Debug.Assert(status is OperationStatus.Done or OperationStatus.InvalidData);
            byte[] result = destination.Slice(0, bytesWritten).ToArray();

            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }

            return status == OperationStatus.Done ? result : throw new FormatException(SR.Format_BadBase64Char);
        }

        private readonly struct Base64DecoderChar : IBase64Decoder<ushort>
        {
            public ReadOnlySpan<sbyte> DecodingMap => default(Base64DecoderByte).DecodingMap;

            public ReadOnlySpan<uint> VbmiLookup0 => default(Base64DecoderByte).VbmiLookup0;

            public ReadOnlySpan<uint> VbmiLookup1 => default(Base64DecoderByte).VbmiLookup1;

            public ReadOnlySpan<sbyte> Avx2LutHigh => default(Base64DecoderByte).Avx2LutHigh;

            public ReadOnlySpan<sbyte> Avx2LutLow => default(Base64DecoderByte).Avx2LutLow;

            public ReadOnlySpan<sbyte> Avx2LutShift => default(Base64DecoderByte).Avx2LutShift;

            public byte MaskSlashOrUnderscore => default(Base64DecoderByte).MaskSlashOrUnderscore;

            public ReadOnlySpan<int> Vector128LutHigh => default(Base64DecoderByte).Vector128LutHigh;

            public ReadOnlySpan<int> Vector128LutLow => default(Base64DecoderByte).Vector128LutLow;

            public ReadOnlySpan<uint> Vector128LutShift => default(Base64DecoderByte).Vector128LutShift;

            public ReadOnlySpan<uint> AdvSimdLutOne3 => default(Base64DecoderByte).AdvSimdLutOne3;

            public uint AdvSimdLutTwo3Uint1 => default(Base64DecoderByte).AdvSimdLutTwo3Uint1;

            public int GetMaxDecodedLength(int sourceLength) => Base64.GetMaxDecodedLength(sourceLength);

            public bool IsInvalidLength(int bufferLength) => bufferLength % 4 != 0;

            public bool IsValidPadding(uint padChar) => padChar == EncodingPad;

            public int SrcLength(bool _, int sourceLength) => sourceLength & ~0x3;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            [CompExactlyDependsOn(typeof(Ssse3))]
            public bool TryDecode128Core(Vector128<byte> str, Vector128<byte> hiNibbles, Vector128<byte> maskSlashOrUnderscore, Vector128<byte> mask8F,
                Vector128<byte> lutLow, Vector128<byte> lutHigh, Vector128<sbyte> lutShift, Vector128<byte> shiftForUnderscore, out Vector128<byte> result) =>
                default(Base64DecoderByte).TryDecode128Core(str, hiNibbles, maskSlashOrUnderscore, mask8F, lutLow, lutHigh, lutShift, shiftForUnderscore, out result);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(Avx2))]
            public bool TryDecode256Core(Vector256<sbyte> str, Vector256<sbyte> hiNibbles, Vector256<sbyte> maskSlashOrUnderscore, Vector256<sbyte> lutLow,
                Vector256<sbyte> lutHigh, Vector256<sbyte> lutShift, Vector256<sbyte> shiftForUnderscore, out Vector256<sbyte> result) =>
                default(Base64DecoderByte).TryDecode256Core(str, hiNibbles, maskSlashOrUnderscore, lutLow, lutHigh, lutShift, shiftForUnderscore, out result);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe bool TryLoadVector512(ushort* src, ushort* srcStart, int sourceLength, out Vector512<sbyte> str) =>
                Base64Helper.TryLoadVector512Char(src, srcStart, sourceLength, out str);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(Avx2))]
            public unsafe bool TryLoadAvxVector256(ushort* src, ushort* srcStart, int sourceLength, out Vector256<sbyte> str) =>
                Base64Helper.TryLoadAvxVector256Char(src, srcStart, sourceLength, out str);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe bool TryLoadVector128(ushort* src, ushort* srcStart, int sourceLength, out Vector128<byte> str) =>
                Base64Helper.TryLoadVector128Char(src, srcStart, sourceLength, out str);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            public unsafe bool TryLoadArmVector128x4(ushort* src, ushort* srcStart, int sourceLength,
                out Vector128<byte> str1, out Vector128<byte> str2, out Vector128<byte> str3, out Vector128<byte> str4) =>
                Base64Helper.TryLoadArmVector128x4Char(src, srcStart, sourceLength, out str1, out str2, out str3, out str4);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe int DecodeFourElements(ushort* source, ref sbyte decodingMap) =>
                Base64Helper.DecodeFourElementsChar(source, ref decodingMap);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe int DecodeRemaining(ushort* srcEnd, ref sbyte decodingMap, long remaining, out uint t2, out uint t3) =>
                Base64Helper.DecodeRemainingChar(srcEnd, ref decodingMap, remaining, out t2, out t3);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int IndexOfAnyExceptWhiteSpace(ReadOnlySpan<ushort> span) =>
                Base64Helper.IndexOfAnyExceptWhiteSpaceChar(span);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public OperationStatus DecodeWithWhiteSpaceBlockwiseWrapper<TBase64Decoder>(TBase64Decoder decoder, ReadOnlySpan<ushort> source,
                Span<byte> bytes, ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock = true) where TBase64Decoder : IBase64Decoder<ushort> =>
                Base64Helper.DecodeWithWhiteSpaceBlockwise(default(Base64DecoderChar), source, bytes, ref bytesConsumed, ref bytesWritten, isFinalBlock);
        }
    }
}
