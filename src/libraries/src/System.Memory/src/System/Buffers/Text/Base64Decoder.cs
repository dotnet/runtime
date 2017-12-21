// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if !netstandard
using Internal.Runtime.CompilerServices;
#endif

namespace System.Buffers.Text
{
    public static partial class Base64
    {
        /// <summary>
        /// Decode the span of UTF-8 encoded text represented as base 64 into binary data.
        /// If the input is not a multiple of 4, it will decode as much as it can, to the closest multiple of 4.
        ///
        /// <param name="utf8">The input span which contains UTF-8 encoded text in base 64 that needs to be decoded.</param>
        /// <param name="bytes">The output span which contains the result of the operation, i.e. the decoded binary data.</param>
        /// <param name="consumed">The number of input bytes consumed during the operation. This can be used to slice the input for subsequent calls, if necessary.</param>
        /// <param name="written">The number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary.</param>
        /// <param name="isFinalBlock">True (default) when the input span contains the entire data to decode. 
        /// Set to false only if it is known that the input span contains partial data with more data to follow.</param>
        /// <returns>It returns the OperationStatus enum values:
        /// - Done - on successful processing of the entire input span
        /// - DestinationTooSmall - if there is not enough space in the output span to fit the decoded input
        /// - NeedMoreData - only if isFinalBlock is false and the input is not a multiple of 4, otherwise the partial input would be considered as InvalidData
        /// - InvalidData - if the input contains bytes outside of the expected base 64 range, or if it contains invalid/more than two padding characters,
        ///   or if the input is incomplete (i.e. not a multiple of 4) and isFinalBlock is true.</returns>
        /// </summary> 
        public static OperationStatus DecodeFromUtf8(ReadOnlySpan<byte> utf8, Span<byte> bytes, out int consumed, out int written, bool isFinalBlock = true)
        {
            ref byte srcBytes = ref MemoryMarshal.GetReference(utf8);
            ref byte destBytes = ref MemoryMarshal.GetReference(bytes);

            int srcLength = utf8.Length & ~0x3;  // only decode input up to the closest multiple of 4.
            int destLength = bytes.Length;

            int sourceIndex = 0;
            int destIndex = 0;

            if (utf8.Length == 0)
                goto DoneExit;

            ref sbyte decodingMap = ref s_decodingMap[0];

            // Last bytes could have padding characters, so process them separately and treat them as valid only if isFinalBlock is true
            // if isFinalBlock is false, padding characters are considered invalid
            int skipLastChunk = isFinalBlock ? 4 : 0;

            int maxSrcLength = 0;
            if (destLength >= GetMaxDecodedFromUtf8Length(srcLength))
            {
                maxSrcLength = srcLength - skipLastChunk;
            }
            else
            {
                // This should never overflow since destLength here is less than int.MaxValue / 4 * 3 (i.e. 1610612733)
                // Therefore, (destLength / 3) * 4 will always be less than 2147483641
                maxSrcLength = (destLength / 3) * 4;
            }

            while (sourceIndex < maxSrcLength)
            {
                int result = Decode(ref Unsafe.Add(ref srcBytes, sourceIndex), ref decodingMap);
                if (result < 0)
                    goto InvalidExit;
                WriteThreeLowOrderBytes(ref Unsafe.Add(ref destBytes, destIndex), result);
                destIndex += 3;
                sourceIndex += 4;
            }

            if (maxSrcLength != srcLength - skipLastChunk)
                goto DestinationSmallExit;

            // If input is less than 4 bytes, srcLength == sourceIndex == 0
            // If input is not a multiple of 4, sourceIndex == srcLength != 0
            if (sourceIndex == srcLength)
            {
                if (isFinalBlock)
                    goto InvalidExit;
                goto NeedMoreExit;
            }

            // if isFinalBlock is false, we will never reach this point

            int i0 = Unsafe.Add(ref srcBytes, srcLength - 4);
            int i1 = Unsafe.Add(ref srcBytes, srcLength - 3);
            int i2 = Unsafe.Add(ref srcBytes, srcLength - 2);
            int i3 = Unsafe.Add(ref srcBytes, srcLength - 1);

            i0 = Unsafe.Add(ref decodingMap, i0);
            i1 = Unsafe.Add(ref decodingMap, i1);

            i0 <<= 18;
            i1 <<= 12;

            i0 |= i1;

            if (i3 != EncodingPad)
            {
                i2 = Unsafe.Add(ref decodingMap, i2);
                i3 = Unsafe.Add(ref decodingMap, i3);

                i2 <<= 6;

                i0 |= i3;
                i0 |= i2;

                if (i0 < 0)
                    goto InvalidExit;
                if (destIndex > destLength - 3)
                    goto DestinationSmallExit;
                WriteThreeLowOrderBytes(ref Unsafe.Add(ref destBytes, destIndex), i0);
                destIndex += 3;
            }
            else if (i2 != EncodingPad)
            {
                i2 = Unsafe.Add(ref decodingMap, i2);

                i2 <<= 6;

                i0 |= i2;

                if (i0 < 0)
                    goto InvalidExit;
                if (destIndex > destLength - 2)
                    goto DestinationSmallExit;
                Unsafe.Add(ref destBytes, destIndex) = (byte)(i0 >> 16);
                Unsafe.Add(ref destBytes, destIndex + 1) = (byte)(i0 >> 8);
                destIndex += 2;
            }
            else
            {
                if (i0 < 0)
                    goto InvalidExit;
                if (destIndex > destLength - 1)
                    goto DestinationSmallExit;
                Unsafe.Add(ref destBytes, destIndex) = (byte)(i0 >> 16);
                destIndex += 1;
            }

            sourceIndex += 4;

            if (srcLength != utf8.Length)
                goto InvalidExit;

        DoneExit:
            consumed = sourceIndex;
            written = destIndex;
            return OperationStatus.Done;

        DestinationSmallExit:
            if (srcLength != utf8.Length && isFinalBlock)
                goto InvalidExit; // if input is not a multiple of 4, and there is no more data, return invalid data instead
            consumed = sourceIndex;
            written = destIndex;
            return OperationStatus.DestinationTooSmall;

        NeedMoreExit:
            consumed = sourceIndex;
            written = destIndex;
            return OperationStatus.NeedMoreData;

        InvalidExit:
            consumed = sourceIndex;
            written = destIndex;
            return OperationStatus.InvalidData;
        }

        /// <summary>
        /// Returns the maximum length (in bytes) of the result if you were to deocde base 64 encoded text within a byte span of size "length".
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="length"/> is less than 0.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMaxDecodedFromUtf8Length(int length)
        {
            if (length < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);

            return (length >> 2) * 3;
        }

        /// <summary>
        /// Decode the span of UTF-8 encoded text in base 64 (in-place) into binary data.
        /// The decoded binary output is smaller than the text data contained in the input (the operation deflates the data).
        /// If the input is not a multiple of 4, it will not decode any.
        ///
        /// <param name="buffer">The input span which contains the base 64 text data that needs to be decoded.</param>
        /// <param name="written">The number of bytes written into the buffer.</param>
        /// <returns>It returns the OperationStatus enum values:
        /// - Done - on successful processing of the entire input span
        /// - InvalidData - if the input contains bytes outside of the expected base 64 range, or if it contains invalid/more than two padding characters, 
        ///   or if the input is incomplete (i.e. not a multiple of 4).
        /// It does not return DestinationTooSmall since that is not possible for base 64 decoding.
        /// It does not return NeedMoreData since this method tramples the data in the buffer and 
        /// hence can only be called once with all the data in the buffer.</returns>
        /// </summary> 
        public static OperationStatus DecodeFromUtf8InPlace(Span<byte> buffer, out int written)
        {
            int bufferLength = buffer.Length;
            int sourceIndex = 0;
            int destIndex = 0;

            // only decode input if it is a multiple of 4
            if (bufferLength != ((bufferLength >> 2) * 4))
                goto InvalidExit;
            if (bufferLength == 0)
                goto DoneExit;

            ref byte bufferBytes = ref MemoryMarshal.GetReference(buffer);

            ref sbyte decodingMap = ref s_decodingMap[0];

            while (sourceIndex < bufferLength - 4)
            {
                int result = Decode(ref Unsafe.Add(ref bufferBytes, sourceIndex), ref decodingMap);
                if (result < 0)
                    goto InvalidExit;
                WriteThreeLowOrderBytes(ref Unsafe.Add(ref bufferBytes, destIndex), result);
                destIndex += 3;
                sourceIndex += 4;
            }

            int i0 = Unsafe.Add(ref bufferBytes, bufferLength - 4);
            int i1 = Unsafe.Add(ref bufferBytes, bufferLength - 3);
            int i2 = Unsafe.Add(ref bufferBytes, bufferLength - 2);
            int i3 = Unsafe.Add(ref bufferBytes, bufferLength - 1);

            i0 = Unsafe.Add(ref decodingMap, i0);
            i1 = Unsafe.Add(ref decodingMap, i1);

            i0 <<= 18;
            i1 <<= 12;

            i0 |= i1;

            if (i3 != EncodingPad)
            {
                i2 = Unsafe.Add(ref decodingMap, i2);
                i3 = Unsafe.Add(ref decodingMap, i3);

                i2 <<= 6;

                i0 |= i3;
                i0 |= i2;

                if (i0 < 0)
                    goto InvalidExit;
                WriteThreeLowOrderBytes(ref Unsafe.Add(ref bufferBytes, destIndex), i0);
                destIndex += 3;
            }
            else if (i2 != EncodingPad)
            {
                i2 = Unsafe.Add(ref decodingMap, i2);

                i2 <<= 6;

                i0 |= i2;

                if (i0 < 0)
                    goto InvalidExit;
                Unsafe.Add(ref bufferBytes, destIndex) = (byte)(i0 >> 16);
                Unsafe.Add(ref bufferBytes, destIndex + 1) = (byte)(i0 >> 8);
                destIndex += 2;
            }
            else
            {
                if (i0 < 0)
                    goto InvalidExit;
                Unsafe.Add(ref bufferBytes, destIndex) = (byte)(i0 >> 16);
                destIndex += 1;
            }

        DoneExit:
            written = destIndex;
            return OperationStatus.Done;

        InvalidExit:
            written = destIndex;
            return OperationStatus.InvalidData;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Decode(ref byte encodedBytes, ref sbyte decodingMap)
        {
            int i0 = encodedBytes;
            int i1 = Unsafe.Add(ref encodedBytes, 1);
            int i2 = Unsafe.Add(ref encodedBytes, 2);
            int i3 = Unsafe.Add(ref encodedBytes, 3);

            i0 = Unsafe.Add(ref decodingMap, i0);
            i1 = Unsafe.Add(ref decodingMap, i1);
            i2 = Unsafe.Add(ref decodingMap, i2);
            i3 = Unsafe.Add(ref decodingMap, i3);

            i0 <<= 18;
            i1 <<= 12;
            i2 <<= 6;

            i0 |= i3;
            i1 |= i2;

            i0 |= i1;
            return i0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteThreeLowOrderBytes(ref byte destination, int value)
        {
            destination = (byte)(value >> 16);
            Unsafe.Add(ref destination, 1) = (byte)(value >> 8);
            Unsafe.Add(ref destination, 2) = (byte)value;
        }

        // Pre-computing this table using a custom string(s_characters) and GenerateDecodingMapAndVerify (found in tests)
        private static readonly sbyte[] s_decodingMap = {
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 62, -1, -1, -1, 63,         //62 is placed at index 43 (for +), 63 at index 47 (for /)
            52, 53, 54, 55, 56, 57, 58, 59, 60, 61, -1, -1, -1, -1, -1, -1,         //52-61 are placed at index 48-57 (for 0-9), 64 at index 61 (for =)
            -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14,
            15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, -1, -1, -1, -1, -1,         //0-25 are placed at index 65-90 (for A-Z)
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
        };
    }
}
