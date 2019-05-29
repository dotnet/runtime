// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Internal.Runtime.CompilerServices;

namespace System.Buffers.Text
{
    // AVX2 version based on https://github.com/aklomp/base64/tree/e516d769a2a432c08404f1981e73b431566057be/lib/arch/avx2
    // SSSE3 version based on https://github.com/aklomp/base64/tree/e516d769a2a432c08404f1981e73b431566057be/lib/arch/ssse3

    /// <summary>
    /// Convert between binary data and UTF-8 encoded text that is represented in base 64.
    /// </summary>
    public static partial class Base64
    {
        /// <summary>
        /// Encode the span of binary data into UTF-8 encoded text represented as base 64.
        /// </summary> 
        /// <param name="bytes">The input span which contains binary data that needs to be encoded.</param>
        /// <param name="utf8">The output span which contains the result of the operation, i.e. the UTF-8 encoded text in base 64.</param>
        /// <param name="bytesConsumed">The number of input bytes consumed during the operation. This can be used to slice the input for subsequent calls, if necessary.</param>
        /// <param name="bytesWritten">The number of bytes written into the output span. This can be used to slice the output for subsequent calls, if necessary.</param>
        /// <param name="isFinalBlock">True (default) when the input span contains the entire data to encode.
        /// Set to false only if it is known that the input span contains partial data with more data to follow.</param>
        /// <returns>It returns the OperationStatus enum values:
        /// - Done - on successful processing of the entire input span
        /// - DestinationTooSmall - if there is not enough space in the output span to fit the encoded input
        /// - NeedMoreData - only if isFinalBlock is false, otherwise the output is padded if the input is not a multiple of 3
        /// It does not return InvalidData since that is not possible for base 64 encoding.
        /// </returns>
        public static unsafe OperationStatus EncodeToUtf8(ReadOnlySpan<byte> bytes, Span<byte> utf8, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true)
        {
            if (bytes.IsEmpty)
            {
                bytesConsumed = 0;
                bytesWritten = 0;
                return OperationStatus.Done;
            }

            fixed (byte* srcBytes = &MemoryMarshal.GetReference(bytes))
            fixed (byte* destBytes = &MemoryMarshal.GetReference(utf8))
            {
                int srcLength = bytes.Length;
                int destLength = utf8.Length;
                int maxSrcLength;

                if (srcLength <= MaximumEncodeLength && destLength >= GetMaxEncodedToUtf8Length(srcLength))
                {
                    maxSrcLength = srcLength;
                }
                else
                {
                    maxSrcLength = (destLength >> 2) * 3;
                }

                byte* src = srcBytes;
                byte* dest = destBytes;
                byte* srcEnd = srcBytes + (uint)srcLength;
                byte* srcMax = srcBytes + (uint)maxSrcLength;

                if (maxSrcLength >= 16)
                {
                    byte* end = srcMax - 32;
                    if (Avx2.IsSupported && (end >= src))
                    {
                        Avx2Encode(ref src, ref dest, end, maxSrcLength, destLength, srcBytes, destBytes);

                        if (src == srcEnd)
                            goto DoneExit;
                    }

                    end = srcMax - 16;
                    if (Ssse3.IsSupported && (end >= src))
                    {
                        Ssse3Encode(ref src, ref dest, end, maxSrcLength, destLength, srcBytes, destBytes);

                        if (src == srcEnd)
                            goto DoneExit;
                    }
                }

                ref byte encodingMap = ref MemoryMarshal.GetReference(s_encodingMap);
                uint result = 0;

                srcMax -= 2;
                while (src < srcMax)
                {
                    result = Encode(src, ref encodingMap);
                    Unsafe.WriteUnaligned(dest, result);
                    src += 3;
                    dest += 4;
                }

                if (srcMax + 2 != srcEnd)
                    goto DestinationTooSmallExit;

                if (!isFinalBlock)
                    goto NeedMoreData;

                if (src + 1 == srcEnd)
                {
                    result = EncodeAndPadTwo(src, ref encodingMap);
                    Unsafe.WriteUnaligned(dest, result);
                    src += 1;
                    dest += 4;
                }
                else if (src + 2 == srcEnd)
                {
                    result = EncodeAndPadOne(src, ref encodingMap);
                    Unsafe.WriteUnaligned(dest, result);
                    src += 2;
                    dest += 4;
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

        /// <summary>
        /// Returns the maximum length (in bytes) of the result if you were to encode binary data within a byte span of size "length".
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="length"/> is less than 0 or larger than 1610612733 (since encode inflates the data by 4/3).
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMaxEncodedToUtf8Length(int length)
        {
            if ((uint)length > MaximumEncodeLength)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);

            return ((length + 2) / 3) * 4;
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
        public static unsafe OperationStatus EncodeToUtf8InPlace(Span<byte> buffer, int dataLength, out int bytesWritten)
        {
            if (buffer.IsEmpty)
            {
                bytesWritten = 0;
                return OperationStatus.Done;
            }

            fixed (byte* bufferBytes = &MemoryMarshal.GetReference(buffer))
            {
                int encodedLength = GetMaxEncodedToUtf8Length(dataLength);
                if (buffer.Length < encodedLength)
                    goto FalseExit;

                int leftover = dataLength - (dataLength / 3) * 3; // how many bytes after packs of 3

                uint destinationIndex = (uint)(encodedLength - 4);
                uint sourceIndex = (uint)(dataLength - leftover);
                uint result = 0;
                ref byte encodingMap = ref MemoryMarshal.GetReference(s_encodingMap);

                // encode last pack to avoid conditional in the main loop
                if (leftover != 0)
                {
                    if (leftover == 1)
                    {
                        result = EncodeAndPadTwo(bufferBytes + sourceIndex, ref encodingMap);
                    }
                    else
                    {
                        result = EncodeAndPadOne(bufferBytes + sourceIndex, ref encodingMap);
                    }

                    Unsafe.WriteUnaligned(bufferBytes + destinationIndex, result);
                    destinationIndex -= 4;
                }

                sourceIndex -= 3;
                while ((int)sourceIndex >= 0)
                {
                    result = Encode(bufferBytes + sourceIndex, ref encodingMap);
                    Unsafe.WriteUnaligned(bufferBytes + destinationIndex, result);
                    destinationIndex -= 4;
                    sourceIndex -= 3;
                }

                bytesWritten = encodedLength;
                return OperationStatus.Done;

            FalseExit:
                bytesWritten = 0;
                return OperationStatus.DestinationTooSmall;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void Avx2Encode(ref byte* srcBytes, ref byte* destBytes, byte* srcEnd, int sourceLength, int destLength, byte* srcStart, byte* destStart)
        {
            // If we have AVX2 support, pick off 24 bytes at a time for as long as we can.
            // But because we read 32 bytes at a time, ensure we have enough room to do a
            // full 32-byte read without segfaulting.

            // translation from SSSE3 into AVX2 of procedure
            // This one works with shifted (4 bytes) input in order to
            // be able to work efficiently in the 2 128-bit lanes

            // srcBytes, bytes MSB to LSB:
            // 0 0 0 0 x w v u t s r q p o n m
            // l k j i h g f e d c b a 0 0 0 0

            // The JIT won't hoist these "constants", so help it
            Vector256<sbyte> shuffleVec = ReadVector<Vector256<sbyte>>(s_avxEncodeShuffleVec);
            Vector256<sbyte> maskAC = Vector256.Create(0x0fc0fc00).AsSByte();
            Vector256<sbyte> maskBB = Vector256.Create(0x003f03f0).AsSByte();
            Vector256<ushort> shiftAC = Vector256.Create(0x04000040).AsUInt16();
            Vector256<short> shiftBB = Vector256.Create(0x01000010).AsInt16();
            Vector256<byte> const51 = Vector256.Create((byte)51);
            Vector256<sbyte> const25 = Vector256.Create((sbyte)25);
            Vector256<sbyte> lut = ReadVector<Vector256<sbyte>>(s_avxEncodeLut);

            byte* src = srcBytes;
            byte* dest = destBytes;

            // first load is done at c-0 not to get a segfault
            AssertRead<Vector256<sbyte>>(src, srcStart, sourceLength);
            Vector256<sbyte> str = Avx.LoadVector256(src).AsSByte();

            // shift by 4 bytes, as required by Reshuffle
            str = Avx2.PermuteVar8x32(str.AsInt32(), ReadVector<Vector256<sbyte>>(s_avxEncodePermuteVec).AsInt32()).AsSByte();

            // Next loads are done at src-4, as required by Reshuffle, so shift it once
            src -= 4;

            while (true)
            {
                // Reshuffle
                str = Avx2.Shuffle(str, shuffleVec);
                // str, bytes MSB to LSB:
                // w x v w
                // t u s t
                // q r p q
                // n o m n
                // k l j k
                // h i g h
                // e f d e
                // b c a b

                Vector256<sbyte> t0 = Avx2.And(str, maskAC);
                // bits, upper case are most significant bits, lower case are least significant bits.
                // 0000wwww XX000000 VVVVVV00 00000000
                // 0000tttt UU000000 SSSSSS00 00000000
                // 0000qqqq RR000000 PPPPPP00 00000000
                // 0000nnnn OO000000 MMMMMM00 00000000
                // 0000kkkk LL000000 JJJJJJ00 00000000
                // 0000hhhh II000000 GGGGGG00 00000000
                // 0000eeee FF000000 DDDDDD00 00000000
                // 0000bbbb CC000000 AAAAAA00 00000000

                Vector256<sbyte> t2 = Avx2.And(str, maskBB);
                // 00000000 00xxxxxx 000000vv WWWW0000
                // 00000000 00uuuuuu 000000ss TTTT0000
                // 00000000 00rrrrrr 000000pp QQQQ0000
                // 00000000 00oooooo 000000mm NNNN0000
                // 00000000 00llllll 000000jj KKKK0000
                // 00000000 00iiiiii 000000gg HHHH0000
                // 00000000 00ffffff 000000dd EEEE0000
                // 00000000 00cccccc 000000aa BBBB0000

                Vector256<ushort> t1 = Avx2.MultiplyHigh(t0.AsUInt16(), shiftAC);
                // 00000000 00wwwwXX 00000000 00VVVVVV
                // 00000000 00ttttUU 00000000 00SSSSSS
                // 00000000 00qqqqRR 00000000 00PPPPPP
                // 00000000 00nnnnOO 00000000 00MMMMMM
                // 00000000 00kkkkLL 00000000 00JJJJJJ
                // 00000000 00hhhhII 00000000 00GGGGGG
                // 00000000 00eeeeFF 00000000 00DDDDDD
                // 00000000 00bbbbCC 00000000 00AAAAAA

                Vector256<short> t3 = Avx2.MultiplyLow(t2.AsInt16(), shiftBB);
                // 00xxxxxx 00000000 00vvWWWW 00000000
                // 00uuuuuu 00000000 00ssTTTT 00000000
                // 00rrrrrr 00000000 00ppQQQQ 00000000
                // 00oooooo 00000000 00mmNNNN 00000000
                // 00llllll 00000000 00jjKKKK 00000000
                // 00iiiiii 00000000 00ggHHHH 00000000
                // 00ffffff 00000000 00ddEEEE 00000000
                // 00cccccc 00000000 00aaBBBB 00000000

                str = Avx2.Or(t1.AsSByte(), t3.AsSByte());
                // 00xxxxxx 00wwwwXX 00vvWWWW 00VVVVVV
                // 00uuuuuu 00ttttUU 00ssTTTT 00SSSSSS
                // 00rrrrrr 00qqqqRR 00ppQQQQ 00PPPPPP
                // 00oooooo 00nnnnOO 00mmNNNN 00MMMMMM
                // 00llllll 00kkkkLL 00jjKKKK 00JJJJJJ
                // 00iiiiii 00hhhhII 00ggHHHH 00GGGGGG
                // 00ffffff 00eeeeFF 00ddEEEE 00DDDDDD
                // 00cccccc 00bbbbCC 00aaBBBB 00AAAAAA

                // Translation
                // LUT contains Absolute offset for all ranges:
                // Translate values 0..63 to the Base64 alphabet. There are five sets:
                // #  From      To         Abs    Index  Characters
                // 0  [0..25]   [65..90]   +65        0  ABCDEFGHIJKLMNOPQRSTUVWXYZ
                // 1  [26..51]  [97..122]  +71        1  abcdefghijklmnopqrstuvwxyz
                // 2  [52..61]  [48..57]    -4  [2..11]  0123456789
                // 3  [62]      [43]       -19       12  +
                // 4  [63]      [47]       -16       13  /

                // Create LUT indices from input:
                // the index for range #0 is right, others are 1 less than expected:
                Vector256<byte> indices = Avx2.SubtractSaturate(str.AsByte(), const51);

                // mask is 0xFF (-1) for range #[1..4] and 0x00 for range #0:
                Vector256<sbyte> mask = Avx2.CompareGreaterThan(str, const25);

                // substract -1, so add 1 to indices for range #[1..4], All indices are now correct:
                Vector256<sbyte> tmp = Avx2.Subtract(indices.AsSByte(), mask);

                // Add offsets to input values:
                str = Avx2.Add(str, Avx2.Shuffle(lut, tmp));

                AssertWrite<Vector256<sbyte>>(dest, destStart, destLength);
                Avx.Store(dest, str.AsByte());

                src += 24;
                dest += 32;

                if (src > srcEnd)
                    break;

                // Load at src-4, as required by Reshuffle (already shifted by -4)
                AssertRead<Vector256<sbyte>>(src, srcStart, sourceLength);
                str = Avx.LoadVector256(src).AsSByte();
            }

            srcBytes = src + 4;
            destBytes = dest;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void Ssse3Encode(ref byte* srcBytes, ref byte* destBytes, byte* srcEnd, int sourceLength, int destLength, byte* srcStart, byte* destStart)
        {
            // If we have SSSE3 support, pick off 12 bytes at a time for as long as we can.
            // But because we read 16 bytes at a time, ensure we have enough room to do a
            // full 16-byte read without segfaulting.

            // srcBytes, bytes MSB to LSB:
            // 0 0 0 0 l k j i h g f e d c b a

            // The JIT won't hoist these "constants", so help it
            Vector128<sbyte> shuffleVec = ReadVector<Vector128<sbyte>>(s_sseEncodeShuffleVec);
            Vector128<sbyte> maskAC = Vector128.Create(0x0fc0fc00).AsSByte();
            Vector128<sbyte> maskBB = Vector128.Create(0x003f03f0).AsSByte();
            Vector128<ushort> shiftAC = Vector128.Create(0x04000040).AsUInt16();
            Vector128<short> shiftBB = Vector128.Create(0x01000010).AsInt16();
            Vector128<byte> const51 = Vector128.Create((byte)51);
            Vector128<sbyte> const25 = Vector128.Create((sbyte)25);
            Vector128<sbyte> lut = ReadVector<Vector128<sbyte>>(s_sseEncodeLut);

            byte* src = srcBytes;
            byte* dest = destBytes;

            //while (remaining >= 16)
            do
            {
                AssertRead<Vector128<sbyte>>(src, srcStart, sourceLength);
                Vector128<sbyte> str = Sse2.LoadVector128(src).AsSByte();

                // Reshuffle
                str = Ssse3.Shuffle(str, shuffleVec);
                // str, bytes MSB to LSB:
                // k l j k
                // h i g h
                // e f d e
                // b c a b

                Vector128<sbyte> t0 = Sse2.And(str, maskAC);
                // bits, upper case are most significant bits, lower case are least significant bits
                // 0000kkkk LL000000 JJJJJJ00 00000000
                // 0000hhhh II000000 GGGGGG00 00000000
                // 0000eeee FF000000 DDDDDD00 00000000
                // 0000bbbb CC000000 AAAAAA00 00000000

                Vector128<sbyte> t2 = Sse2.And(str, maskBB);
                // 00000000 00llllll 000000jj KKKK0000
                // 00000000 00iiiiii 000000gg HHHH0000
                // 00000000 00ffffff 000000dd EEEE0000
                // 00000000 00cccccc 000000aa BBBB0000

                Vector128<ushort> t1 = Sse2.MultiplyHigh(t0.AsUInt16(), shiftAC);
                // 00000000 00kkkkLL 00000000 00JJJJJJ
                // 00000000 00hhhhII 00000000 00GGGGGG
                // 00000000 00eeeeFF 00000000 00DDDDDD
                // 00000000 00bbbbCC 00000000 00AAAAAA

                Vector128<short> t3 = Sse2.MultiplyLow(t2.AsInt16(), shiftBB);
                // 00llllll 00000000 00jjKKKK 00000000
                // 00iiiiii 00000000 00ggHHHH 00000000
                // 00ffffff 00000000 00ddEEEE 00000000
                // 00cccccc 00000000 00aaBBBB 00000000

                str = Sse2.Or(t1.AsSByte(), t3.AsSByte());
                // 00llllll 00kkkkLL 00jjKKKK 00JJJJJJ
                // 00iiiiii 00hhhhII 00ggHHHH 00GGGGGG
                // 00ffffff 00eeeeFF 00ddEEEE 00DDDDDD
                // 00cccccc 00bbbbCC 00aaBBBB 00AAAAAA

                // Translation
                // LUT contains Absolute offset for all ranges:
                // Translate values 0..63 to the Base64 alphabet. There are five sets:
                // #  From      To         Abs    Index  Characters
                // 0  [0..25]   [65..90]   +65        0  ABCDEFGHIJKLMNOPQRSTUVWXYZ
                // 1  [26..51]  [97..122]  +71        1  abcdefghijklmnopqrstuvwxyz
                // 2  [52..61]  [48..57]    -4  [2..11]  0123456789
                // 3  [62]      [43]       -19       12  +
                // 4  [63]      [47]       -16       13  /

                // Create LUT indices from input:
                // the index for range #0 is right, others are 1 less than expected:
                Vector128<byte> indices = Sse2.SubtractSaturate(str.AsByte(), const51);

                // mask is 0xFF (-1) for range #[1..4] and 0x00 for range #0:
                Vector128<sbyte> mask = Sse2.CompareGreaterThan(str, const25);

                // substract -1, so add 1 to indices for range #[1..4], All indices are now correct:
                Vector128<sbyte> tmp = Sse2.Subtract(indices.AsSByte(), mask);

                // Add offsets to input values:
                str = Sse2.Add(str, Ssse3.Shuffle(lut, tmp));

                AssertWrite<Vector128<sbyte>>(dest, destStart, destLength);
                Sse2.Store(dest, str.AsByte());

                src += 12;
                dest += 16;
            }
            while (src <= srcEnd);

            srcBytes = src;
            destBytes = dest;
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

            return i0 | (i1 << 8) | (i2 << 16) | (i3 << 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint EncodeAndPadOne(byte* twoBytes, ref byte encodingMap)
        {
            uint t0 = twoBytes[0];
            uint t1 = twoBytes[1];

            uint i = (t0 << 16) | (t1 << 8);

            uint i0 = Unsafe.Add(ref encodingMap, (IntPtr)(i >> 18));
            uint i1 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 12) & 0x3F));
            uint i2 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 6) & 0x3F));

            return i0 | (i1 << 8) | (i2 << 16) | (EncodingPad << 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint EncodeAndPadTwo(byte* oneByte, ref byte encodingMap)
        {
            uint t0 = oneByte[0];

            uint i = t0 << 8;

            uint i0 = Unsafe.Add(ref encodingMap, (IntPtr)(i >> 10));
            uint i1 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 4) & 0x3F));

            return i0 | (i1 << 8) | (EncodingPad << 16) | (EncodingPad << 24);
        }

        private const uint EncodingPad = '='; // '=', for padding

        private const int MaximumEncodeLength = (int.MaxValue / 4) * 3; // 1610612733

        // Pre-computing this table using a custom string(s_characters) and GenerateEncodingMapAndVerify (found in tests)
        private static ReadOnlySpan<byte> s_encodingMap => new byte[] {
            65, 66, 67, 68, 69, 70, 71, 72,         //A..H
            73, 74, 75, 76, 77, 78, 79, 80,         //I..P
            81, 82, 83, 84, 85, 86, 87, 88,         //Q..X
            89, 90, 97, 98, 99, 100, 101, 102,      //Y..Z, a..f
            103, 104, 105, 106, 107, 108, 109, 110, //g..n
            111, 112, 113, 114, 115, 116, 117, 118, //o..v
            119, 120, 121, 122, 48, 49, 50, 51,     //w..z, 0..3
            52, 53, 54, 55, 56, 57, 43, 47          //4..9, +, /
        };

        private static ReadOnlySpan<sbyte> s_sseEncodeShuffleVec => new sbyte[] {
            1, 0, 2, 1,
            4, 3, 5, 4,
            7, 6, 8, 7,
            10, 9, 11, 10
        };

        private static ReadOnlySpan<sbyte> s_sseEncodeLut => new sbyte[] {
            65, 71, -4, -4,
            -4, -4, -4, -4,
            -4, -4, -4, -4,
            -19, -16, 0, 0
        };

        private static ReadOnlySpan<sbyte> s_avxEncodePermuteVec => new sbyte[] {
            0, 0, 0, 0,
            0, 0, 0, 0,
            1, 0, 0, 0,
            2, 0, 0, 0,
            3, 0, 0, 0,
            4, 0, 0, 0,
            5, 0, 0, 0,
            6, 0, 0, 0
        };

        private static ReadOnlySpan<sbyte> s_avxEncodeShuffleVec => new sbyte[] {
            5, 4, 6, 5,
            8, 7, 9, 8,
            11, 10, 12, 11,
            14, 13, 15, 14,
            1, 0, 2, 1,
            4, 3, 5, 4,
            7, 6, 8, 7,
            10, 9, 11, 10
        };

        private static ReadOnlySpan<sbyte> s_avxEncodeLut => new sbyte[] {
            65, 71, -4, -4,
            -4, -4, -4, -4,
            -4, -4, -4, -4,
            -19, -16, 0, 0,
            65, 71, -4, -4,
            -4, -4, -4, -4,
            -4, -4, -4, -4,
            -19, -16, 0, 0
        };
    }
}
