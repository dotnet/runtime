// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public static partial class Convert
    {
        /// <summary>
        /// Decode the span of UTF-16 encoded text represented as base 64 into binary data.
        /// If the input is not a multiple of 4, or contains illegal characters, it will decode as much as it can, to the largest possible multiple of 4.
        /// This invariant allows continuation of the parse with a slower, whitespace-tolerant algorithm.
        /// </summary>
        /// <param name="utf16">The input span which contains UTF-16 encoded text in base 64 that needs to be decoded.</param>
        /// <param name="bytes">The output span which contains the result of the operation, i.e. the decoded binary data.</param>
        /// <param name="consumed">The number of input bytes consumed during the operation. This can be used to slice the input for subsequent calls, if necessary.</param>
        /// <param name="written">The number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary.</param>
        /// <returns>Returns:
        /// - true  - The entire input span was successfully parsed.
        /// - false - Only a part of the input span was successfully parsed. Failure causes may include embedded or trailing whitespace,
        ///           other illegal Base64 characters, trailing characters after an encoding pad ('='), an input span whose length is not divisible by 4
        ///           or a destination span that's too small. <paramref name="consumed"/> and <paramref name="written"/> are set so that
        ///           parsing can continue with a slower whitespace-tolerant algorithm.
        ///
        /// Note: This is a cut down version of the implementation of Base64.DecodeFromUtf8(), modified the accept UTF-16 chars and act as a fast-path
        /// helper for the Convert routines when the input string contains no whitespace.
        /// </returns>
        private static bool TryDecodeFromUtf16(ReadOnlySpan<char> utf16, Span<byte> bytes, out int consumed, out int written)
        {
            ref char srcChars = ref MemoryMarshal.GetReference(utf16);
            ref byte destBytes = ref MemoryMarshal.GetReference(bytes);

            int srcLength = utf16.Length & ~0x3;  // only decode input up to the closest multiple of 4.
            int destLength = bytes.Length;

            int sourceIndex = 0;
            int destIndex = 0;

            if (utf16.Length == 0)
                goto DoneExit;

            ref sbyte decodingMap = ref MemoryMarshal.GetReference(DecodingMap);

            // Last bytes could have padding characters, so process them separately and treat them as valid.
            const int skipLastChunk = 4;

            int maxSrcLength;
            if (destLength >= (srcLength >> 2) * 3)
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
                int result = Decode(ref Unsafe.Add(ref srcChars, sourceIndex), ref decodingMap);
                if (result < 0)
                    goto InvalidExit;
                WriteThreeLowOrderBytes(ref Unsafe.Add(ref destBytes, destIndex), result);
                destIndex += 3;
                sourceIndex += 4;
            }

            if (maxSrcLength != srcLength - skipLastChunk)
                goto InvalidExit;

            // If input is less than 4 bytes, srcLength == sourceIndex == 0
            // If input is not a multiple of 4, sourceIndex == srcLength != 0
            if (sourceIndex == srcLength)
            {
                goto InvalidExit;
            }

            int i0 = Unsafe.Add(ref srcChars, srcLength - 4);
            int i1 = Unsafe.Add(ref srcChars, srcLength - 3);
            int i2 = Unsafe.Add(ref srcChars, srcLength - 2);
            int i3 = Unsafe.Add(ref srcChars, srcLength - 1);
            if (((i0 | i1 | i2 | i3) & 0xffffff00) != 0)
                goto InvalidExit;

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
                    goto InvalidExit;
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
                    goto InvalidExit;
                Unsafe.Add(ref destBytes, destIndex) = (byte)(i0 >> 16);
                Unsafe.Add(ref destBytes, destIndex + 1) = (byte)(i0 >> 8);
                destIndex += 2;
            }
            else
            {
                if (i0 < 0)
                    goto InvalidExit;
                if (destIndex > destLength - 1)
                    goto InvalidExit;
                Unsafe.Add(ref destBytes, destIndex) = (byte)(i0 >> 16);
                destIndex++;
            }

            sourceIndex += 4;

            if (srcLength != utf16.Length)
                goto InvalidExit;

        DoneExit:
            consumed = sourceIndex;
            written = destIndex;
            return true;

        InvalidExit:
            consumed = sourceIndex;
            written = destIndex;
            Debug.Assert((consumed % 4) == 0);
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Decode(ref char encodedChars, ref sbyte decodingMap)
        {
            int i0 = encodedChars;
            int i1 = Unsafe.Add(ref encodedChars, 1);
            int i2 = Unsafe.Add(ref encodedChars, 2);
            int i3 = Unsafe.Add(ref encodedChars, 3);

            if (((i0 | i1 | i2 | i3) & 0xffffff00) != 0)
                return -1; // One or more chars falls outside the 00..ff range. This cannot be a valid Base64 character.

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
        private static ReadOnlySpan<sbyte> DecodingMap =>
        [
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 62, -1, -1, -1, 63,         // 62 is placed at index 43 (for +), 63 at index 47 (for /)
            52, 53, 54, 55, 56, 57, 58, 59, 60, 61, -1, -1, -1, -1, -1, -1,         // 52-61 are placed at index 48-57 (for 0-9), 64 at index 61 (for =)
            -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14,
            15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, -1, -1, -1, -1, -1,         // 0-25 are placed at index 65-90 (for A-Z)
            -1, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40,
            41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, -1, -1, -1, -1, -1,         // 26-51 are placed at index 97-122 (for a-z)
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,         // Bytes over 122 ('z') are invalid and cannot be decoded
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,         // Hence, padding the map with 255, which indicates invalid input
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        ];

        private const byte EncodingPad = (byte)'='; // '=', for padding
    }
}
