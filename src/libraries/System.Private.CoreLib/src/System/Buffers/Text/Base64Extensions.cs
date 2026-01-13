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
    public static partial class Base64
    {
        private const int MaxStackallocThreshold = 256;

        /// <summary>
        /// Returns the length (in bytes) of the result if you were to encode binary data within a byte span of size <paramref name="bytesLength"/>.
        /// </summary>
        /// <param name="bytesLength">The number of bytes to encode.</param>
        /// <returns>The number of characters that encoding will produce.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="bytesLength"/> is less than 0 or greater than 1610612733.
        /// </exception>
        /// <remarks>
        /// This method is equivalent to <see cref="GetMaxEncodedToUtf8Length(int)"/>, but with a name that matches
        /// the equivalent method on <see cref="Base64Url"/>.
        /// </remarks>
        public static int GetEncodedLength(int bytesLength) => GetMaxEncodedToUtf8Length(bytesLength);

        /// <summary>
        /// Returns the maximum length (in bytes) of the result if you were to decode base 64 encoded text from a span of size <paramref name="base64Length"/>.
        /// </summary>
        /// <param name="base64Length">The length of the base64-encoded input.</param>
        /// <returns>The maximum number of bytes that decoding could produce.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="base64Length"/> is less than 0.
        /// </exception>
        /// <remarks>
        /// This method is equivalent to <see cref="GetMaxDecodedFromUtf8Length(int)"/>, but with a name that matches
        /// the equivalent method on <see cref="Base64Url"/>.
        /// </remarks>
        public static int GetMaxDecodedLength(int base64Length) => GetMaxDecodedFromUtf8Length(base64Length);

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
            byte[] destination = new byte[GetMaxEncodedToUtf8Length(source.Length)];
            EncodeToUtf8(source, destination, out _, out int bytesWritten);
            Debug.Assert(destination.Length == bytesWritten);

            return destination;
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
            char[] destination = new char[GetMaxEncodedToUtf8Length(source.Length)];
            EncodeToChars(source, destination, out _, out int charsWritten);
            Debug.Assert(destination.Length == charsWritten);

            return destination;
        }

        /// <summary>
        /// Encodes the span of binary data into unicode string represented as Base64 ASCII chars.
        /// </summary>
        /// <param name="source">The input span which contains binary data that needs to be encoded.</param>
        /// <returns>A string which contains the result of the operation, i.e. the ASCII string in Base64.</returns>
        public static unsafe string EncodeToString(ReadOnlySpan<byte> source)
        {
            int encodedLength = GetMaxEncodedToUtf8Length(source.Length);

            return string.Create(encodedLength, (IntPtr)(&source), static (buffer, spanPtr) =>
            {
                ReadOnlySpan<byte> source = *(ReadOnlySpan<byte>*)spanPtr;
                EncodeToChars(source, buffer, out _, out int charsWritten);
                Debug.Assert(buffer.Length == charsWritten, $"The source length: {source.Length}, bytes written: {charsWritten}");
            });
        }

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
            int upperBound = GetMaxDecodedFromUtf8Length(source.Length);
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
            int upperBound = GetMaxDecodedFromUtf8Length(source.Length);
            byte[]? rented = null;

            Span<byte> destination = (uint)upperBound <= MaxStackallocThreshold
                ? stackalloc byte[MaxStackallocThreshold]
                : (rented = ArrayPool<byte>.Shared.Rent(upperBound));

            OperationStatus status = DecodeFromChars(source, destination, out _, out int bytesWritten);
            byte[] result = destination.Slice(0, bytesWritten).ToArray();

            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }

            return status == OperationStatus.Done ? result : throw new FormatException(SR.Format_BadBase64Char);
        }

        private static OperationStatus DecodeWithWhiteSpaceBlockwise(Base64DecoderChar decoder,
            ReadOnlySpan<ushort> source, Span<byte> bytes, ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock = true)
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

                bool hasAnotherBlock = source.Length >= BlockSize;
                bool localIsFinalBlock = !hasAnotherBlock;

                // If this block contains padding and there's another block, then only whitespace may follow for being valid.
                if (hasAnotherBlock)
                {
                    int paddingCount = GetPaddingCount(ref buffer[BlockSize - 1]);
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

                status = DecodeFrom<Base64DecoderChar, ushort>(decoder, buffer.Slice(0, bufferIdx), bytes, out int localConsumed, out int localWritten, localIsFinalBlock, ignoreWhiteSpace: false);
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
        private static int GetPaddingCount(ref ushort ptrToLastElement)
        {
            int padding = 0;

            if (ptrToLastElement == EncodingPad)
            {
                padding++;
            }

            if (Unsafe.Subtract(ref ptrToLastElement, 1) == EncodingPad)
            {
                padding++;
            }

            return padding;
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

                if (BitConverter.IsLittleEndian)
                {
                    dest[0] = (ushort)i0;
                    dest[1] = (ushort)i1;
                    dest[2] = (ushort)EncodingPad;
                    dest[3] = (ushort)EncodingPad;
                }
                else
                {
                    dest[0] = (ushort)i0;
                    dest[1] = (ushort)i1;
                    dest[2] = (ushort)EncodingPad;
                    dest[3] = (ushort)EncodingPad;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void EncodeTwoOptionallyPadOne(byte* twoBytes, ushort* dest, ref byte encodingMap)
            {
                uint t0 = twoBytes[0];
                uint t1 = twoBytes[1];

                uint i = (t0 << 16) | (t1 << 8);

                ushort i0 = Unsafe.Add(ref encodingMap, (IntPtr)(i >> 18));
                ushort i1 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 12) & 0x3F));
                ushort i2 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 6) & 0x3F));

                dest[0] = i0;
                dest[1] = i1;
                dest[2] = i2;
                dest[3] = (ushort)EncodingPad;
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
            [CompExactlyDependsOn(typeof(System.Runtime.Intrinsics.Arm.AdvSimd.Arm64))]
            public unsafe void StoreArmVector128x4ToDestination(ushort* dest, ushort* destStart, int destLength,
                Vector128<byte> res1, Vector128<byte> res2, Vector128<byte> res3, Vector128<byte> res4)
            {
                AssertWrite<Vector128<ushort>>(dest, destStart, destLength);
                (Vector128<ushort> utf16LowVector1, Vector128<ushort> utf16HighVector1) = Vector128.Widen(res1);
                (Vector128<ushort> utf16LowVector2, Vector128<ushort> utf16HighVector2) = Vector128.Widen(res2);
                (Vector128<ushort> utf16LowVector3, Vector128<ushort> utf16HighVector3) = Vector128.Widen(res3);
                (Vector128<ushort> utf16LowVector4, Vector128<ushort> utf16HighVector4) = Vector128.Widen(res4);
                System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.StoreVectorAndZip(dest, (utf16LowVector1, utf16LowVector2, utf16LowVector3, utf16LowVector4));
                System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.StoreVectorAndZip(dest + 32, (utf16HighVector1, utf16HighVector2, utf16HighVector3, utf16HighVector4));
            }
#endif // NET

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void EncodeThreeAndWrite(byte* threeBytes, ushort* destination, ref byte encodingMap)
            {
                uint t0 = threeBytes[0];
                uint t1 = threeBytes[1];
                uint t2 = threeBytes[2];

                uint i = (t0 << 16) | (t1 << 8) | t2;

                ulong i0 = Unsafe.Add(ref encodingMap, (IntPtr)(i >> 18));
                ulong i1 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 12) & 0x3F));
                ulong i2 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 6) & 0x3F));
                ulong i3 = Unsafe.Add(ref encodingMap, (IntPtr)(i & 0x3F));

                ulong result;
                if (BitConverter.IsLittleEndian)
                {
                    result = i0 | (i1 << 16) | (i2 << 32) | (i3 << 48);
                }
                else
                {
                    result = (i0 << 48) | (i1 << 32) | (i2 << 16) | i3;
                }

                Unsafe.WriteUnaligned(destination, result);
            }
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

            public int GetMaxDecodedLength(int sourceLength) => Base64.GetMaxDecodedFromUtf8Length(sourceLength);

            public bool IsInvalidLength(int bufferLength) => bufferLength % 4 != 0;

            public bool IsValidPadding(uint padChar) => padChar == EncodingPad;

            public int SrcLength(bool _, int sourceLength) => sourceLength & ~0x3;

#if NET
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(System.Runtime.Intrinsics.Arm.AdvSimd.Arm64))]
            [CompExactlyDependsOn(typeof(System.Runtime.Intrinsics.X86.Ssse3))]
            public bool TryDecode128Core(Vector128<byte> str, Vector128<byte> hiNibbles, Vector128<byte> maskSlashOrUnderscore, Vector128<byte> mask8F,
                Vector128<byte> lutLow, Vector128<byte> lutHigh, Vector128<sbyte> lutShift, Vector128<byte> shiftForUnderscore, out Vector128<byte> result) =>
                default(Base64DecoderByte).TryDecode128Core(str, hiNibbles, maskSlashOrUnderscore, mask8F, lutLow, lutHigh, lutShift, shiftForUnderscore, out result);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(System.Runtime.Intrinsics.X86.Avx2))]
            public bool TryDecode256Core(Vector256<sbyte> str, Vector256<sbyte> hiNibbles, Vector256<sbyte> maskSlashOrUnderscore, Vector256<sbyte> lutLow,
                Vector256<sbyte> lutHigh, Vector256<sbyte> lutShift, Vector256<sbyte> shiftForUnderscore, out Vector256<sbyte> result) =>
                default(Base64DecoderByte).TryDecode256Core(str, hiNibbles, maskSlashOrUnderscore, lutLow, lutHigh, lutShift, shiftForUnderscore, out result);

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

                str = Ascii.ExtractAsciiVector(utf16VectorLower, utf16VectorUpper).AsSByte();
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(System.Runtime.Intrinsics.X86.Avx2))]
            public unsafe bool TryLoadAvxVector256(ushort* src, ushort* srcStart, int sourceLength, out Vector256<sbyte> str)
            {
                AssertRead<Vector256<sbyte>>(src, srcStart, sourceLength);
                Vector256<ushort> utf16VectorLower = System.Runtime.Intrinsics.X86.Avx.LoadVector256(src);
                Vector256<ushort> utf16VectorUpper = System.Runtime.Intrinsics.X86.Avx.LoadVector256(src + 16);

                if (Ascii.VectorContainsNonAsciiChar(utf16VectorLower | utf16VectorUpper))
                {
                    str = default;
                    return false;
                }

                str = Ascii.ExtractAsciiVector(utf16VectorLower, utf16VectorUpper).AsSByte();
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
            [CompExactlyDependsOn(typeof(System.Runtime.Intrinsics.Arm.AdvSimd.Arm64))]
            public unsafe bool TryLoadArmVector128x4(ushort* src, ushort* srcStart, int sourceLength,
                out Vector128<byte> str1, out Vector128<byte> str2, out Vector128<byte> str3, out Vector128<byte> str4)
            {
                AssertRead<Vector128<sbyte>>(src, srcStart, sourceLength);
                var (s11, s12, s21, s22) = System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.Load4xVector128AndUnzip(src);
                var (s31, s32, s41, s42) = System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.Load4xVector128AndUnzip(src + 32);

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
#endif // NET

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
                    return -1; // One or more chars falls outside the 00..ff range, invalid Base64 character.
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
            public OperationStatus DecodeWithWhiteSpaceBlockwiseWrapper<TBase64Decoder>(TBase64Decoder decoder, ReadOnlySpan<ushort> source,
                Span<byte> bytes, ref int bytesConsumed, ref int bytesWritten, bool isFinalBlock = true) where TBase64Decoder : IBase64Decoder<ushort> =>
                DecodeWithWhiteSpaceBlockwise(default(Base64DecoderChar), source, bytes, ref bytesConsumed, ref bytesWritten, isFinalBlock);
        }
    }
}
