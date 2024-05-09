// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

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
        /// <returns>It returns the OperationStatus enum values:
        /// - Done - on successful processing of the entire input span
        /// - DestinationTooSmall - if there is not enough space in the output span to fit the encoded input
        /// - NeedMoreData - only if <paramref name="isFinalBlock"/> is <see langword="false"/>, otherwise the output is padded if the input is not a multiple of 3
        /// It does not return InvalidData since that is not possible for base64 encoding.
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
                    byte* end = srcMax - 64;
                    if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported && (end >= src))
                    {
                        Avx512Encode(ref src, ref dest, end, maxSrcLength, destLength, srcBytes, destBytes);

                        if (src == srcEnd)
                            goto DoneExit;
                    }

                    end = srcMax - 64;
                    if (Avx2.IsSupported && (end >= src))
                    {
                        Avx2Encode(ref src, ref dest, end, maxSrcLength, destLength, srcBytes, destBytes);

                        if (src == srcEnd)
                            goto DoneExit;
                    }

                    end = srcMax - 48;
                    if (AdvSimd.Arm64.IsSupported && (end >= src))
                    {
                        AdvSimdEncode(ref src, ref dest, end, maxSrcLength, destLength, srcBytes, destBytes);

                        if (src == srcEnd)
                            goto DoneExit;
                    }

                    end = srcMax - 16;
                    if ((Ssse3.IsSupported || AdvSimd.Arm64.IsSupported) && BitConverter.IsLittleEndian && (end >= src))
                    {
                        Vector128Encode(ref src, ref dest, end, maxSrcLength, destLength, srcBytes, destBytes);

                        if (src == srcEnd)
                            goto DoneExit;
                    }
                }

                ref byte encodingMap = ref MemoryMarshal.GetReference(EncodingMap);
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
                {
                    if (src == srcEnd)
                        goto DoneExit;

                    goto NeedMoreData;
                }

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
        /// <exception cref="ArgumentOutOfRangeException">
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
                ref byte encodingMap = ref MemoryMarshal.GetReference(EncodingMap);

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
        [CompExactlyDependsOn(typeof(Avx512BW))]
        [CompExactlyDependsOn(typeof(Avx512Vbmi))]
        private static unsafe void Avx512Encode(ref byte* srcBytes, ref byte* destBytes, byte* srcEnd, int sourceLength, int destLength, byte* srcStart, byte* destStart)
        {
            // Reference for VBMI implementation : https://github.com/WojciechMula/base64simd/tree/master/encode
            // If we have AVX512 support, pick off 48 bytes at a time for as long as we can.
            // But because we read 64 bytes at a time, ensure we have enough room to do a
            // full 64-byte read without segfaulting.

            byte* src = srcBytes;
            byte* dest = destBytes;

            // The JIT won't hoist these "constants", so help it
            Vector512<sbyte> shuffleVecVbmi = Vector512.Create(
                0x01020001, 0x04050304, 0x07080607, 0x0a0b090a,
                0x0d0e0c0d, 0x10110f10, 0x13141213, 0x16171516,
                0x191a1819, 0x1c1d1b1c, 0x1f201e1f, 0x22232122,
                0x25262425, 0x28292728, 0x2b2c2a2b, 0x2e2f2d2e).AsSByte();
            Vector512<sbyte> vbmiLookup = Vector512.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"u8).AsSByte();

            Vector512<ushort> maskAC = Vector512.Create((uint)0x0fc0fc00).AsUInt16();
            Vector512<uint> maskBB = Vector512.Create((uint)0x3f003f00);
            Vector512<ushort> shiftAC = Vector512.Create((uint)0x0006000a).AsUInt16();
            Vector512<ushort> shiftBB = Vector512.Create((uint)0x00080004).AsUInt16();

            AssertRead<Vector256<sbyte>>(src, srcStart, sourceLength);

            // This algorithm requires AVX512VBMI support.
            // Vbmi was first introduced in CannonLake and is avaialable from IceLake on.

            // str = [...|PONM|LKJI|HGFE|DCBA]
            Vector512<sbyte> str = Vector512.Load(src).AsSByte();

            while (true)
            {
                // Step 1 : Split 48 bytes into 64 bytes with each byte using 6-bits from input
                // str = [...|KLJK|HIGH|EFDE|BCAB]
                str = Avx512Vbmi.PermuteVar64x8(str, shuffleVecVbmi);

                // TO-DO- This can be achieved faster with multishift
                // Consider the first 4 bytes - BCAB
                // temp1    = [...|0000cccc|cc000000|aaaaaa00|00000000]
                Vector512<ushort> temp1 = (str.AsUInt16() & maskAC);

                // temp2    = [...|00000000|00cccccc|00000000|00aaaaaa]
                Vector512<ushort> temp2 = Avx512BW.ShiftRightLogicalVariable(temp1, shiftAC).AsUInt16();

                // temp3    = [...|ccdddddd|00000000|aabbbbbb|cccc0000]
                Vector512<ushort> temp3 = Avx512BW.ShiftLeftLogicalVariable(str.AsUInt16(), shiftBB).AsUInt16();

                // str      = [...|00dddddd|00cccccc|00bbbbbb|00aaaaaa]
                str = Vector512.ConditionalSelect(maskBB, temp3.AsUInt32(), temp2.AsUInt32()).AsSByte();

                // Step 2: Now we have the indices calculated. Next step is to use these indices to translate.
                str = Avx512Vbmi.PermuteVar64x8(vbmiLookup, str);

                AssertWrite<Vector512<sbyte>>(dest, destStart, destLength);
                str.Store((sbyte*)dest);

                src += 48;
                dest += 64;

                if (src > srcEnd)
                    break;

                AssertRead<Vector512<sbyte>>(src, srcStart, sourceLength);
                str = Vector512.Load(src).AsSByte();
            }

            srcBytes = src;
            destBytes = dest;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
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
            Vector256<sbyte> shuffleVec = Vector256.Create(
                5, 4, 6, 5,
                8, 7, 9, 8,
                11, 10, 12, 11,
                14, 13, 15, 14,
                1, 0, 2, 1,
                4, 3, 5, 4,
                7, 6, 8, 7,
                10, 9, 11, 10);

            Vector256<sbyte> lut = Vector256.Create(
                65, 71, -4, -4,
                -4, -4, -4, -4,
                -4, -4, -4, -4,
                -19, -16, 0, 0,
                65, 71, -4, -4,
                -4, -4, -4, -4,
                -4, -4, -4, -4,
                -19, -16, 0, 0);

            Vector256<sbyte> maskAC = Vector256.Create(0x0fc0fc00).AsSByte();
            Vector256<sbyte> maskBB = Vector256.Create(0x003f03f0).AsSByte();
            Vector256<ushort> shiftAC = Vector256.Create(0x04000040).AsUInt16();
            Vector256<short> shiftBB = Vector256.Create(0x01000010).AsInt16();
            Vector256<byte> const51 = Vector256.Create((byte)51);
            Vector256<sbyte> const25 = Vector256.Create((sbyte)25);

            byte* src = srcBytes;
            byte* dest = destBytes;

            // first load is done at c-0 not to get a segfault
            AssertRead<Vector256<sbyte>>(src, srcStart, sourceLength);
            Vector256<sbyte> str = Avx.LoadVector256(src).AsSByte();

            // shift by 4 bytes, as required by Reshuffle
            str = Avx2.PermuteVar8x32(str.AsInt32(), Vector256.Create(
                0, 0, 0, 0,
                0, 0, 0, 0,
                1, 0, 0, 0,
                2, 0, 0, 0,
                3, 0, 0, 0,
                4, 0, 0, 0,
                5, 0, 0, 0,
                6, 0, 0, 0).AsInt32()).AsSByte();

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

                // subtract -1, so add 1 to indices for range #[1..4], All indices are now correct:
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
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        private static unsafe void AdvSimdEncode(ref byte* srcBytes, ref byte* destBytes, byte* srcEnd, int sourceLength, int destLength, byte* srcStart, byte* destStart)
        {
            // C# implementation of https://github.com/aklomp/base64/blob/3a5add8652076612a8407627a42c768736a4263f/lib/arch/neon64/enc_loop.c
            Vector128<byte> str1;
            Vector128<byte> str2;
            Vector128<byte> str3;
            Vector128<byte> res1;
            Vector128<byte> res2;
            Vector128<byte> res3;
            Vector128<byte> res4;
            Vector128<byte> tblEnc1 = Vector128.Create("ABCDEFGHIJKLMNOP"u8).AsByte();
            Vector128<byte> tblEnc2 = Vector128.Create("QRSTUVWXYZabcdef"u8).AsByte();
            Vector128<byte> tblEnc3 = Vector128.Create("ghijklmnopqrstuv"u8).AsByte();
            Vector128<byte> tblEnc4 = Vector128.Create("wxyz0123456789+/"u8).AsByte();
            byte* src = srcBytes;
            byte* dest = destBytes;

            // If we have Neon support, pick off 48 bytes at a time for as long as we can.
            do
            {
                // Load 48 bytes and deinterleave:
                AssertRead<Vector128<byte>>(src, srcStart, sourceLength);
                (str1, str2, str3) = AdvSimd.Arm64.LoadVector128x3AndUnzip(src);

                // Divide bits of three input bytes over four output bytes:
                res1 = AdvSimd.ShiftRightLogical(str1, 2);
                res2 = AdvSimd.ShiftRightLogical(str2, 4);
                res3 = AdvSimd.ShiftRightLogical(str3, 6);
                res2 = AdvSimd.ShiftLeftAndInsert(res2, str1, 4);
                res3 = AdvSimd.ShiftLeftAndInsert(res3, str2, 2);

                // Clear top two bits:
                res2 &= AdvSimd.DuplicateToVector128((byte)0x3F);
                res3 &= AdvSimd.DuplicateToVector128((byte)0x3F);
                res4 = str3 & AdvSimd.DuplicateToVector128((byte)0x3F);

                // The bits have now been shifted to the right locations;
                // translate their values 0..63 to the Base64 alphabet.
                // Use a 64-byte table lookup:
                res1 = AdvSimd.Arm64.VectorTableLookup((tblEnc1, tblEnc2, tblEnc3, tblEnc4), res1);
                res2 = AdvSimd.Arm64.VectorTableLookup((tblEnc1, tblEnc2, tblEnc3, tblEnc4), res2);
                res3 = AdvSimd.Arm64.VectorTableLookup((tblEnc1, tblEnc2, tblEnc3, tblEnc4), res3);
                res4 = AdvSimd.Arm64.VectorTableLookup((tblEnc1, tblEnc2, tblEnc3, tblEnc4), res4);

                // Interleave and store result:
                AssertWrite<Vector128<byte>>(dest, destStart, destLength);
                AdvSimd.Arm64.StoreVector128x4AndZip(dest, (res1, res2, res3, res4));

                src += 48;
                dest += 64;
            } while (src <= srcEnd);

            srcBytes = src;
            destBytes = dest;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        private static unsafe void Vector128Encode(ref byte* srcBytes, ref byte* destBytes, byte* srcEnd, int sourceLength, int destLength, byte* srcStart, byte* destStart)
        {
            // If we have SSSE3 support, pick off 12 bytes at a time for as long as we can.
            // But because we read 16 bytes at a time, ensure we have enough room to do a
            // full 16-byte read without segfaulting.

            // srcBytes, bytes MSB to LSB:
            // 0 0 0 0 l k j i h g f e d c b a

            // The JIT won't hoist these "constants", so help it
            Vector128<byte>   shuffleVec = Vector128.Create(0x01020001, 0x04050304, 0x07080607, 0x0A0B090A).AsByte();
            Vector128<byte>   lut = Vector128.Create(0xFCFC4741, 0xFCFCFCFC, 0xFCFCFCFC, 0x0000F0ED).AsByte();
            Vector128<byte>   maskAC = Vector128.Create(0x0fc0fc00).AsByte();
            Vector128<byte>   maskBB = Vector128.Create(0x003f03f0).AsByte();
            Vector128<ushort> shiftAC = Vector128.Create(0x04000040).AsUInt16();
            Vector128<short>  shiftBB = Vector128.Create(0x01000010).AsInt16();
            Vector128<byte>   const51 = Vector128.Create((byte)51);
            Vector128<sbyte>  const25 = Vector128.Create((sbyte)25);
            Vector128<byte>   mask8F = Vector128.Create((byte)0x8F);

            byte* src = srcBytes;
            byte* dest = destBytes;

            //while (remaining >= 16)
            do
            {
                AssertRead<Vector128<sbyte>>(src, srcStart, sourceLength);
                Vector128<byte> str = Vector128.LoadUnsafe(ref *src);

                // Reshuffle
                str = SimdShuffle(str, shuffleVec, mask8F);
                // str, bytes MSB to LSB:
                // k l j k
                // h i g h
                // e f d e
                // b c a b

                Vector128<byte> t0 = str & maskAC;
                // bits, upper case are most significant bits, lower case are least significant bits
                // 0000kkkk LL000000 JJJJJJ00 00000000
                // 0000hhhh II000000 GGGGGG00 00000000
                // 0000eeee FF000000 DDDDDD00 00000000
                // 0000bbbb CC000000 AAAAAA00 00000000

                Vector128<byte> t2 = str & maskBB;
                // 00000000 00llllll 000000jj KKKK0000
                // 00000000 00iiiiii 000000gg HHHH0000
                // 00000000 00ffffff 000000dd EEEE0000
                // 00000000 00cccccc 000000aa BBBB0000

                Vector128<ushort> t1;
                if (Ssse3.IsSupported)
                {
                    t1 = Sse2.MultiplyHigh(t0.AsUInt16(), shiftAC);
                }
                else
                {
                    Vector128<ushort> odd =  Vector128.ShiftRightLogical(AdvSimd.Arm64.UnzipOdd(t0.AsUInt16(), t0.AsUInt16()), 6);
                    Vector128<ushort> even = Vector128.ShiftRightLogical(AdvSimd.Arm64.UnzipEven(t0.AsUInt16(), t0.AsUInt16()), 10);
                    t1 = AdvSimd.Arm64.ZipLow(even, odd);
                }
                // 00000000 00kkkkLL 00000000 00JJJJJJ
                // 00000000 00hhhhII 00000000 00GGGGGG
                // 00000000 00eeeeFF 00000000 00DDDDDD
                // 00000000 00bbbbCC 00000000 00AAAAAA

                Vector128<short> t3 = t2.AsInt16() * shiftBB;
                // 00llllll 00000000 00jjKKKK 00000000
                // 00iiiiii 00000000 00ggHHHH 00000000
                // 00ffffff 00000000 00ddEEEE 00000000
                // 00cccccc 00000000 00aaBBBB 00000000

                str = t1.AsByte() | t3.AsByte();
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
                Vector128<byte> indices;
                if (Ssse3.IsSupported)
                {
                    indices = Sse2.SubtractSaturate(str.AsByte(), const51);
                }
                else
                {
                    indices = AdvSimd.SubtractSaturate(str.AsByte(), const51);
                }

                // mask is 0xFF (-1) for range #[1..4] and 0x00 for range #0:
                Vector128<sbyte> mask = Vector128.GreaterThan(str.AsSByte(), const25);

                // subtract -1, so add 1 to indices for range #[1..4], All indices are now correct:
                Vector128<sbyte> tmp = indices.AsSByte() - mask;

                // Add offsets to input values:
                str += SimdShuffle(lut, tmp.AsByte(), mask8F);

                AssertWrite<Vector128<sbyte>>(dest, destStart, destLength);
                str.Store(dest);

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

            if (BitConverter.IsLittleEndian)
            {
                return i0 | (i1 << 8) | (i2 << 16) | (i3 << 24);
            }
            else
            {
                return (i0 << 24) | (i1 << 16) | (i2 << 8) | i3;
            }
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

            if (BitConverter.IsLittleEndian)
            {
                return i0 | (i1 << 8) | (i2 << 16) | (EncodingPad << 24);
            }
            else
            {
                return (i0 << 24) | (i1 << 16) | (i2 << 8) | EncodingPad;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe uint EncodeAndPadTwo(byte* oneByte, ref byte encodingMap)
        {
            uint t0 = oneByte[0];

            uint i = t0 << 8;

            uint i0 = Unsafe.Add(ref encodingMap, (IntPtr)(i >> 10));
            uint i1 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 4) & 0x3F));

            if (BitConverter.IsLittleEndian)
            {
                return i0 | (i1 << 8) | (EncodingPad << 16) | (EncodingPad << 24);
            }
            else
            {
                return (i0 << 24) | (i1 << 16) | (EncodingPad << 8) | EncodingPad;
            }
        }

        internal const uint EncodingPad = '='; // '=', for padding

        private const int MaximumEncodeLength = (int.MaxValue / 4) * 3; // 1610612733

        internal static ReadOnlySpan<byte> EncodingMap => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"u8;
    }
}
