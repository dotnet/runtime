// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers.Text
{
    public static partial class Base64Url
    {
        private const int MaximumEncodeLength = (int.MaxValue / 4) * 3; // 1610612733
        private static ReadOnlySpan<byte> EncodingMap => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_"u8;

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
        public static unsafe OperationStatus EncodeToUtf8(ReadOnlySpan<byte> source, Span<byte> destination,
            out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true)
        {
            if (source.IsEmpty)
            {
                bytesConsumed = 0;
                bytesWritten = 0;
                return OperationStatus.Done;
            }

            fixed (byte* srcBytes = &MemoryMarshal.GetReference(source))
            fixed (byte* destBytes = &MemoryMarshal.GetReference(destination))
            {
                int srcLength = source.Length;
                int destLength = destination.Length;
                int maxSrcLength = srcLength <= MaximumEncodeLength && destLength >= GetEncodedLength(srcLength) ?
                    srcLength : GetMaxDecodedLength(destLength);

                byte* src = srcBytes;
                byte* dest = destBytes;
                byte* srcEnd = srcBytes + (uint)srcLength;
                byte* srcMax = srcBytes + (uint)maxSrcLength;

                ref byte encodingMap = ref MemoryMarshal.GetReference(EncodingMap);

                srcMax -= 2;
                while (src < srcMax)
                {
                    EncodeThreeAndWriteFour(src, dest, ref encodingMap);
                    src += 3;
                    dest += 4;
                }

                if (srcMax + 2 != srcEnd)
                {
                    goto DestinationTooSmallExit;
                }

                if (!isFinalBlock)
                {
                    if (src == srcEnd)
                    {
                        goto DoneExit;
                    }

                    goto NeedMoreData;
                }

                if (src + 1 == srcEnd)
                {
                    EncodeOneAndWriteTwo(src, dest, ref encodingMap);
                    src += 1;
                    dest += 2;
                }
                else if (src + 2 == srcEnd)
                {
                    EncodeTwoAndWriteThree(src, dest, ref encodingMap);
                    src += 2;
                    dest += 3;
                }

            DoneExit:
                bytesConsumed = (int)(src - srcBytes);
                bytesWritten = (int)(dest - destBytes);
                return OperationStatus.Done;

            DestinationTooSmallExit:
                bytesConsumed = (int)(src - srcBytes);
                bytesWritten = (int)(dest - destBytes);
                return OperationStatus.DestinationTooSmall;

            NeedMoreData:
                bytesConsumed = (int)(src - srcBytes);
                bytesWritten = (int)(dest - destBytes);
                return OperationStatus.NeedMoreData;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void EncodeOneAndWriteTwo(byte* oneByte, byte* dest, ref byte encodingMap)
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
        private static unsafe void EncodeTwoAndWriteThree(byte* twoBytes, byte* dest, ref byte encodingMap)
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
        private static unsafe void EncodeThreeAndWriteFour(byte* threeBytes, byte* destination, ref byte encodingMap)
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

        /// <summary>
        /// Returns the length (in bytes) of the result if you were to encode binary data within a byte span of size <paramref name="bytesLength"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="bytesLength"/> is less than 0 or greater than 1610612733.
        /// </exception>
        public static int GetEncodedLength(int bytesLength)
        {
            if ((uint)bytesLength > MaximumEncodeLength)
            {
                throw new ArgumentOutOfRangeException(nameof(bytesLength));
            }

            int remainder = (int)((uint)bytesLength % 3);

            return (bytesLength / 3) * 4 + (remainder > 0 ? remainder + 1 : 0); // if remainder is 1 or 2, the encoded length will be 1 byte longer.
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
        public static unsafe OperationStatus EncodeToChars(ReadOnlySpan<byte> source, Span<char> destination,
            out int bytesConsumed, out int charsWritten, bool isFinalBlock = true)
        {
            if (source.IsEmpty)
            {
                bytesConsumed = 0;
                charsWritten = 0;
                return OperationStatus.Done;
            }

            fixed (byte* srcBytes = &MemoryMarshal.GetReference(source))
            fixed (char* destBytes = &MemoryMarshal.GetReference(destination))
            {
                int srcLength = source.Length;
                int destLength = destination.Length;
                int maxSrcLength = GetEncodedLength(srcLength);

                byte* src = srcBytes;
                char* dest = destBytes;
                byte* srcEnd = srcBytes + (uint)srcLength;
                byte* srcMax = srcBytes + (uint)maxSrcLength;

                ref byte encodingMap = ref MemoryMarshal.GetReference(EncodingMap);

                srcMax -= 2;
                while (src < srcMax)
                {
                    EncodeThreeAndWriteFour(src, dest, ref encodingMap);
                    src += 3;
                    dest += 4;
                }

                if (srcMax + 2 != srcEnd)
                {
                    goto DestinationTooSmallExit;
                }

                if (!isFinalBlock)
                {
                    if (src == srcEnd)
                    {
                        goto DoneExit;
                    }

                    goto NeedMoreData;
                }

                if (src + 1 == srcEnd)
                {
                    EncodeOneAndWriteTwo(src, dest, ref encodingMap);
                    src += 1;
                    dest += 2;
                }
                else if (src + 2 == srcEnd)
                {
                    EncodeTwoAndWriteThree(src, dest, ref encodingMap);
                    src += 2;
                    dest += 3;
                }

            DoneExit:
                bytesConsumed = (int)(src - srcBytes);
                charsWritten = (int)(dest - destBytes);
                return OperationStatus.Done;

            DestinationTooSmallExit:
                bytesConsumed = (int)(src - srcBytes);
                charsWritten = (int)(dest - destBytes);
                return OperationStatus.DestinationTooSmall;

            NeedMoreData:
                bytesConsumed = (int)(src - srcBytes);
                charsWritten = (int)(dest - destBytes);
                return OperationStatus.NeedMoreData;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void EncodeOneAndWriteTwo(byte* oneByte, char* dest, ref byte encodingMap)
        {
            uint t0 = oneByte[0];

            uint i = t0 << 8;

            uint i0 = Unsafe.Add(ref encodingMap, (IntPtr)(i >> 10));
            uint i1 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 4) & 0x3F));

            if (BitConverter.IsLittleEndian)
            {
                dest[0] = (char)i0;
                dest[1] = (char)i1;
            }
            else
            {
                dest[1] = (char)i0;
                dest[0] = (char)i1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void EncodeTwoAndWriteThree(byte* twoBytes, char* dest, ref byte encodingMap)
        {
            uint t0 = twoBytes[0];
            uint t1 = twoBytes[1];

            uint i = (t0 << 16) | (t1 << 8);

            uint i0 = Unsafe.Add(ref encodingMap, (IntPtr)(i >> 18));
            uint i1 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 12) & 0x3F));
            uint i2 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 6) & 0x3F));

            if (BitConverter.IsLittleEndian)
            {
                dest[0] = (char)i0;
                dest[1] = (char)i1;
                dest[2] = (char)i2;
            }
            else
            {
                dest[2] = (char)i0;
                dest[1] = (char)i1;
                dest[0] = (char)i2;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void EncodeThreeAndWriteFour(byte* threeBytes, char* destination, ref byte encodingMap)
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
                destination[0] = (char)i0;
                destination[1] = (char)i1;
                destination[2] = (char)i2;
                destination[3] = (char)i3;
            }
            else
            {
                destination[3] = (char)i0;
                destination[2] = (char)i1;
                destination[1] = (char)i2;
                destination[0] = (char)i3;
            }
        }

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
            char[] destination = new char[GetEncodedLength(source.Length)];
            EncodeToChars(source, destination, out _, out int charsWritten);
            Debug.Assert(destination.Length == charsWritten);

            return new string(destination);
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
        public static unsafe bool TryEncodeToUtf8InPlace(Span<byte> buffer, int dataLength, out int bytesWritten)
        {
            OperationStatus status = EncodeToUtf8InPlace(buffer, dataLength, out bytesWritten);

            return status == OperationStatus.Done;
        }

        private static unsafe OperationStatus EncodeToUtf8InPlace(Span<byte> buffer, int dataLength, out int bytesWritten)
        {
            if (buffer.IsEmpty)
            {
                bytesWritten = 0;
                return OperationStatus.Done;
            }

            fixed (byte* bufferBytes = &MemoryMarshal.GetReference(buffer))
            {
                int encodedLength = GetEncodedLength(dataLength);
                if (buffer.Length < encodedLength)
                {
                    bytesWritten = 0;
                    return OperationStatus.DestinationTooSmall;
                }

                int leftover = (int)((uint)dataLength % 3); // how many bytes after packs of 3

                uint destinationIndex = leftover > 0 ? (uint)(encodedLength - leftover - 1) : (uint)(encodedLength - 4);
                uint sourceIndex = (uint)(dataLength - leftover);
                ref byte encodingMap = ref MemoryMarshal.GetReference(EncodingMap);

                // encode last pack to avoid conditional in the main loop
                if (leftover != 0)
                {
                    if (leftover == 1)
                    {
                        EncodeOneAndWriteTwo(bufferBytes + sourceIndex, bufferBytes + destinationIndex, ref encodingMap);
                    }
                    else
                    {
                        EncodeTwoAndWriteThree(bufferBytes + sourceIndex, bufferBytes + destinationIndex, ref encodingMap);
                    }

                    destinationIndex -= 4;
                }

                sourceIndex -= 3;
                while ((int)sourceIndex >= 0)
                {
                    uint result = Encode(bufferBytes + sourceIndex, ref encodingMap);
                    Unsafe.WriteUnaligned(bufferBytes + destinationIndex, result);
                    destinationIndex -= 4;
                    sourceIndex -= 3;
                }

                bytesWritten = encodedLength;
                return OperationStatus.Done;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint Encode(byte* threeBytes, ref byte encodingMap)
        {
            uint t0 = threeBytes[0];
            uint t1 = threeBytes[1];
            uint t2 = threeBytes[2];

            uint i = (t0 << 16) | (t1 << 8) | t2;

            uint i0 = Unsafe.Add(ref encodingMap, (IntPtr)(i >> 18));
            uint i1 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 12) & 0x3F));
            uint i2 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 6) & 0x3F));
            uint i3 = Unsafe.Add(ref encodingMap, (IntPtr)(i & 0x3F));

            if (BitConverter.IsLittleEndian)
            {
                return i0 | (i1 << 8) | (i2 << 16) | (i3 << 24);
            }
            else
            {
                return (i0 << 24) | (i1 << 16) | (i2 << 8) | i3;
            }
        }
    }
}
