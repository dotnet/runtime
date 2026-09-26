// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#if NET
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.Wasm;
using System.Runtime.Intrinsics.X86;
#endif

namespace System.Buffers.Text
{
    // AVX2 version based on https://github.com/aklomp/base64/tree/e516d769a2a432c08404f1981e73b431566057be/lib/arch/avx2
    // Vector128 version based on https://github.com/aklomp/base64/tree/e516d769a2a432c08404f1981e73b431566057be/lib/arch/ssse3
    internal static partial class Base64Helper
    {
        internal static OperationStatus EncodeTo<TBase64Encoder, T>(TBase64Encoder encoder, ReadOnlySpan<byte> source,
            Span<T> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true)
            where TBase64Encoder : IBase64Encoder<T>
            where T : unmanaged
        {
            if (source.IsEmpty)
            {
                bytesConsumed = 0;
                bytesWritten = 0;
                return OperationStatus.Done;
            }

            int srcLength = source.Length;
            int maxSrcLength = encoder.GetMaxSrcLength(srcLength, destination.Length);

            ReadOnlySpan<byte> src = source.Slice(0, maxSrcLength);
            Span<T> dest = destination;

#if NET
            if (src.Length >= 16)
            {
                if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported && src.Length >= 64)
                {
                    Avx512Encode(encoder, ref src, ref dest);
                }

                if (Avx2.IsSupported && src.Length >= 32)
                {
                    Avx2Encode(encoder, ref src, ref dest);
                }

                if (AdvSimd.Arm64.IsSupported && src.Length >= 48)
                {
                    AdvSimdEncode(encoder, ref src, ref dest);
                }

                if ((Ssse3.IsSupported || AdvSimd.Arm64.IsSupported || PackedSimd.IsSupported) && BitConverter.IsLittleEndian && src.Length >= 16)
                {
                    Vector128Encode(encoder, ref src, ref dest);
                }
            }
#endif
            ReadOnlySpan<byte> encodingMap = encoder.EncodingMap;

            // maxSrcLength guarantees that dest has room for all full blocks, the dest check is only for bounds check elimination.
            while (src.Length >= 3 && dest.Length >= 4)
            {
                encoder.EncodeThreeAndWrite(src, dest, encodingMap);
                src = src.Slice(3);
                dest = dest.Slice(4);
            }

            int written = destination.Length - dest.Length;
            OperationStatus status = OperationStatus.Done;

            if (maxSrcLength != srcLength)
            {
                status = OperationStatus.DestinationTooSmall;
            }
            else if (!isFinalBlock)
            {
                if (!src.IsEmpty)
                {
                    status = OperationStatus.NeedMoreData;
                }
            }
            else if (src.Length == 1)
            {
                encoder.EncodeOneOptionallyPadTwo(src, dest, encodingMap);
                src = src.Slice(1);
                written += encoder.IncrementPadTwo;
            }
            else if (src.Length == 2)
            {
                encoder.EncodeTwoOptionallyPadOne(src, dest, encodingMap);
                src = src.Slice(2);
                written += encoder.IncrementPadOne;
            }

            bytesConsumed = maxSrcLength - src.Length;
            bytesWritten = written;
            return status;
        }

#if NET
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx512BW))]
        [CompExactlyDependsOn(typeof(Avx512Vbmi))]
        private static void Avx512Encode<TBase64Encoder, T>(TBase64Encoder encoder, ref ReadOnlySpan<byte> srcRef, ref Span<T> destRef)
            where TBase64Encoder : IBase64Encoder<T>
            where T : unmanaged
        {
            // Reference for VBMI implementation : https://github.com/WojciechMula/base64simd/tree/master/encode
            // If we have AVX512 support, pick off 48 bytes at a time for as long as we can.
            // But because we read 64 bytes at a time, ensure we have enough room to do a full 64-byte read.

            ReadOnlySpan<byte> src = srcRef;
            Span<T> dest = destRef;

            // The JIT won't hoist these "constants", so help it
            Vector512<sbyte> shuffleVecVbmi = Vector512.Create(
                0x01020001, 0x04050304, 0x07080607, 0x0a0b090a,
                0x0d0e0c0d, 0x10110f10, 0x13141213, 0x16171516,
                0x191a1819, 0x1c1d1b1c, 0x1f201e1f, 0x22232122,
                0x25262425, 0x28292728, 0x2b2c2a2b, 0x2e2f2d2e).AsSByte();
            Vector512<sbyte> vbmiLookup = Vector512.Create(encoder.EncodingMap).AsSByte();

            Vector512<ushort> maskAC = Vector512.Create((uint)0x0fc0fc00).AsUInt16();
            Vector512<uint> maskBB = Vector512.Create((uint)0x3f003f00);
            Vector512<ushort> shiftAC = Vector512.Create((uint)0x0006000a).AsUInt16();
            Vector512<ushort> shiftBB = Vector512.Create((uint)0x00080004).AsUInt16();

            // This algorithm requires AVX512VBMI support.
            // Vbmi was first introduced in CannonLake and is available from IceLake on.
            // Two blocks per iteration halve the span bookkeeping per block.
            if (src.Length >= 48 + 64 && dest.Length >= 64 + 64)
            {
                do
                {
                    encoder.StoreVector512ToDestination(dest, Avx512EncodeBlock(Vector512.Create(src).AsSByte(), shuffleVecVbmi, vbmiLookup, maskAC, maskBB, shiftAC, shiftBB));
                    encoder.StoreVector512ToDestination(dest.Slice(64), Avx512EncodeBlock(Vector512.Create(src.Slice(48)).AsSByte(), shuffleVecVbmi, vbmiLookup, maskAC, maskBB, shiftAC, shiftBB));

                    src = src.Slice(96);
                    dest = dest.Slice(128);
                }
                while (src.Length >= 48 + 64 && dest.Length >= 64 + 64);
            }

            while (src.Length >= 64 && dest.Length >= 64)
            {
                encoder.StoreVector512ToDestination(dest, Avx512EncodeBlock(Vector512.Create(src).AsSByte(), shuffleVecVbmi, vbmiLookup, maskAC, maskBB, shiftAC, shiftBB));

                src = src.Slice(48);
                dest = dest.Slice(64);
            }

            srcRef = src;
            destRef = dest;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx512BW))]
        [CompExactlyDependsOn(typeof(Avx512Vbmi))]
        private static Vector512<byte> Avx512EncodeBlock(Vector512<sbyte> str, Vector512<sbyte> shuffleVecVbmi, Vector512<sbyte> vbmiLookup,
            Vector512<ushort> maskAC, Vector512<uint> maskBB, Vector512<ushort> shiftAC, Vector512<ushort> shiftBB)
        {
            // str = [...|PONM|LKJI|HGFE|DCBA]

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
            return Avx512Vbmi.PermuteVar64x8(vbmiLookup, str).AsByte();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        private static void Avx2Encode<TBase64Encoder, T>(TBase64Encoder encoder, ref ReadOnlySpan<byte> srcRef, ref Span<T> destRef)
            where TBase64Encoder : IBase64Encoder<T>
            where T : unmanaged
        {
            // If we have AVX2 support, pick off 24 bytes at a time for as long as we can.
            // But because we read 32 bytes at a time, ensure we have enough room to do a full 32-byte read.

            // translation from SSSE3 into AVX2 of procedure
            // This one works with shifted (4 bytes) input in order to
            // be able to work efficiently in the 2 128-bit lanes

            // srcBytes, bytes MSB to LSB:
            // 0 0 0 0 x w v u t s r q p o n m
            // l k j i h g f e d c b a 0 0 0 0

            ReadOnlySpan<byte> src = srcRef;
            Span<T> dest = destRef;

            if (src.Length < 32 || dest.Length < 32)
            {
                return;
            }

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
                encoder.Avx2LutChar62, encoder.Avx2LutChar63, 0, 0,
                65, 71, -4, -4,
                -4, -4, -4, -4,
                -4, -4, -4, -4,
                encoder.Avx2LutChar62, encoder.Avx2LutChar63, 0, 0);

            Vector256<sbyte> maskAC = Vector256.Create(0x0fc0fc00).AsSByte();
            Vector256<sbyte> maskBB = Vector256.Create(0x003f03f0).AsSByte();
            Vector256<ushort> shiftAC = Vector256.Create(0x04000040).AsUInt16();
            Vector256<short> shiftBB = Vector256.Create(0x01000010).AsInt16();
            Vector256<byte> const51 = Vector256.Create((byte)51);
            Vector256<sbyte> const25 = Vector256.Create((sbyte)25);

            // The first load is done at offset 0 and shifted by 4 bytes, as required by Reshuffle
            Vector256<sbyte> str = Avx2.PermuteVar8x32(Vector256.Create(src).AsInt32(), Vector256.Create(
                0, 0, 0, 0,
                0, 0, 0, 0,
                1, 0, 0, 0,
                2, 0, 0, 0,
                3, 0, 0, 0,
                4, 0, 0, 0,
                5, 0, 0, 0,
                6, 0, 0, 0).AsInt32()).AsSByte();

            // Next loads are done 4 bytes before the current position, as required by Reshuffle
            src = src.Slice(24 - 4);

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

                Vector256<sbyte> t0 = str & maskAC;
                // bits, upper case are most significant bits, lower case are least significant bits.
                // 0000wwww XX000000 VVVVVV00 00000000
                // 0000tttt UU000000 SSSSSS00 00000000
                // 0000qqqq RR000000 PPPPPP00 00000000
                // 0000nnnn OO000000 MMMMMM00 00000000
                // 0000kkkk LL000000 JJJJJJ00 00000000
                // 0000hhhh II000000 GGGGGG00 00000000
                // 0000eeee FF000000 DDDDDD00 00000000
                // 0000bbbb CC000000 AAAAAA00 00000000

                Vector256<sbyte> t2 = str & maskBB;
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

                Vector256<short> t3 = t2.AsInt16() * shiftBB;
                // 00xxxxxx 00000000 00vvWWWW 00000000
                // 00uuuuuu 00000000 00ssTTTT 00000000
                // 00rrrrrr 00000000 00ppQQQQ 00000000
                // 00oooooo 00000000 00mmNNNN 00000000
                // 00llllll 00000000 00jjKKKK 00000000
                // 00iiiiii 00000000 00ggHHHH 00000000
                // 00ffffff 00000000 00ddEEEE 00000000
                // 00cccccc 00000000 00aaBBBB 00000000

                str = t1.AsSByte() | t3.AsSByte();
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
                Vector256<sbyte> tmp = indices.AsSByte() - mask;

                // Add offsets to input values:
                str += Avx2.Shuffle(lut, tmp);

                encoder.StoreVector256ToDestination(dest, str.AsByte());
                dest = dest.Slice(32);

                if (src.Length < 32 || dest.Length < 32)
                {
                    break;
                }

                str = Vector256.Create(src).AsSByte();
                src = src.Slice(24);
            }

            srcRef = src.Slice(4);
            destRef = dest;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        private static void AdvSimdEncode<TBase64Encoder, T>(TBase64Encoder encoder, ref ReadOnlySpan<byte> srcRef, ref Span<T> destRef)
            where TBase64Encoder : IBase64Encoder<T>
            where T : unmanaged
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
            Vector128<byte> tblEnc4 = Vector128.Create(encoder.AdvSimdLut4).AsByte();

            // Deinterleave indices (emulates LD3)
            Vector128<byte> unzip3Idx0 = Vector128.Create((byte)0, 3, 6, 9, 12, 15, 18, 21, 24, 27, 30, 33, 36, 39, 42, 45);
            Vector128<byte> unzip3Idx1 = unzip3Idx0 + Vector128<byte>.One;
            Vector128<byte> unzip3Idx2 = unzip3Idx1 + Vector128<byte>.One;

            ReadOnlySpan<byte> src = srcRef;
            Span<T> dest = destRef;

            // If we have Neon support, pick off 48 bytes at a time for as long as we can.
            while (src.Length >= 48 && dest.Length >= 64)
            {
                // Load 48 bytes and deinterleave:
                Vector128<byte> in1 = Vector128.Create(src);
                Vector128<byte> in2 = Vector128.Create(src.Slice(16, 16));
                Vector128<byte> in3 = Vector128.Create(src.Slice(32, 16));
                str1 = AdvSimd.Arm64.VectorTableLookup((in1, in2, in3), unzip3Idx0);
                str2 = AdvSimd.Arm64.VectorTableLookup((in1, in2, in3), unzip3Idx1);
                str3 = AdvSimd.Arm64.VectorTableLookup((in1, in2, in3), unzip3Idx2);

                // Divide bits of three input bytes over four output bytes:
                res1 = str1 >>> 2;
                res2 = str2 >>> 4;
                res3 = str3 >>> 6;
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
                encoder.StoreArmVector128x4ToDestination(dest, res1, res2, res3, res4);

                src = src.Slice(48);
                dest = dest.Slice(64);
            }

            srcRef = src;
            destRef = dest;
        }

        /// <summary>Interleaves the bytes of 4 vectors (emulates ST4).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        internal static void AdvSimdZip4(Vector128<byte> res1, Vector128<byte> res2, Vector128<byte> res3, Vector128<byte> res4,
            out Vector128<byte> out1, out Vector128<byte> out2, out Vector128<byte> out3, out Vector128<byte> out4)
        {
            Vector128<ushort> zip12Low = AdvSimd.Arm64.ZipLow(res1, res2).AsUInt16();
            Vector128<ushort> zip12High = AdvSimd.Arm64.ZipHigh(res1, res2).AsUInt16();
            Vector128<ushort> zip34Low = AdvSimd.Arm64.ZipLow(res3, res4).AsUInt16();
            Vector128<ushort> zip34High = AdvSimd.Arm64.ZipHigh(res3, res4).AsUInt16();

            out1 = AdvSimd.Arm64.ZipLow(zip12Low, zip34Low).AsByte();
            out2 = AdvSimd.Arm64.ZipHigh(zip12Low, zip34Low).AsByte();
            out3 = AdvSimd.Arm64.ZipLow(zip12High, zip34High).AsByte();
            out4 = AdvSimd.Arm64.ZipHigh(zip12High, zip34High).AsByte();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        private static void Vector128Encode<TBase64Encoder, T>(TBase64Encoder encoder, ref ReadOnlySpan<byte> srcRef, ref Span<T> destRef)
            where TBase64Encoder : IBase64Encoder<T>
            where T : unmanaged
        {
            // If we have SSSE3 support, pick off 12 bytes at a time for as long as we can.
            // But because we read 16 bytes at a time, ensure we have enough room to do a full 16-byte read.

            // srcBytes, bytes MSB to LSB:
            // 0 0 0 0 l k j i h g f e d c b a

            // The JIT won't hoist these "constants", so help it
            Vector128<byte> shuffleVec = Vector128.Create(0x01020001, 0x04050304, 0x07080607, 0x0A0B090A).AsByte();
            Vector128<byte> lut = Vector128.Create(0xFCFC4741, 0xFCFCFCFC, 0xFCFCFCFC, encoder.Ssse3AdvSimdLutE3).AsByte();
            Vector128<byte> maskAC = Vector128.Create(0x0fc0fc00).AsByte();
            Vector128<byte> maskBB = Vector128.Create(0x003f03f0).AsByte();
            Vector128<ushort> shiftAC = Vector128.Create(0x04000040).AsUInt16();
            Vector128<short> shiftBB = Vector128.Create(0x01000010).AsInt16();
            Vector128<byte> const51 = Vector128.Create((byte)51);
            Vector128<sbyte> const25 = Vector128.Create((sbyte)25);
            Vector128<byte> mask8F = Vector128.Create((byte)0x8F);

            ReadOnlySpan<byte> src = srcRef;
            Span<T> dest = destRef;

            while (src.Length >= 16 && dest.Length >= 16)
            {
                Vector128<byte> str = Vector128.Create(src);

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
                else if (AdvSimd.Arm64.IsSupported)
                {
                    Vector128<ushort> odd = Vector128.ShiftRightLogical(AdvSimd.Arm64.UnzipOdd(t0.AsUInt16(), t0.AsUInt16()), 6);
                    Vector128<ushort> even = Vector128.ShiftRightLogical(AdvSimd.Arm64.UnzipEven(t0.AsUInt16(), t0.AsUInt16()), 10);
                    t1 = AdvSimd.Arm64.ZipLow(even, odd);
                }
                else if (PackedSimd.IsSupported)
                {
                    // MultiplyHigh by {2^6, 2^10} is a right shift of the even u16 lanes by 10 and the odd lanes by 6.
                    Vector128<ushort> shr6 = Vector128.ShiftRightLogical(t0.AsUInt16(), 6);
                    Vector128<ushort> shr10 = Vector128.ShiftRightLogical(t0.AsUInt16(), 10);
                    t1 = Vector128.ConditionalSelect(Vector128.Create(0x0000FFFFu).AsUInt16(), shr10, shr6);
                }
                else
                {
                    // We explicitly recheck each IsSupported query to ensure that the trimmer can see which paths are live/dead
                    ThrowUnreachableException();
                    t1 = default;
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
                else if (AdvSimd.IsSupported)
                {
                    indices = AdvSimd.SubtractSaturate(str.AsByte(), const51);
                }
                else if (PackedSimd.IsSupported)
                {
                    indices = PackedSimd.SubtractSaturate(str.AsByte(), const51);
                }
                else
                {
                    // We explicitly recheck each IsSupported query to ensure that the trimmer can see which paths are live/dead
                    ThrowUnreachableException();
                    indices = default;
                }

                // mask is 0xFF (-1) for range #[1..4] and 0x00 for range #0:
                Vector128<sbyte> mask = Vector128.GreaterThan(str.AsSByte(), const25);

                // subtract -1, so add 1 to indices for range #[1..4], All indices are now correct:
                Vector128<sbyte> tmp = indices.AsSByte() - mask;

                // Add offsets to input values:
                str += SimdShuffle(lut, tmp.AsByte(), mask8F);

                encoder.StoreVector128ToDestination(dest, str);

                src = src.Slice(12);
                dest = dest.Slice(16);
            }

            srcRef = src;
            destRef = dest;
        }
#endif

        internal static OperationStatus EncodeToUtf8InPlace<TBase64Encoder>(TBase64Encoder encoder, Span<byte> buffer, int dataLength, out int bytesWritten)
            where TBase64Encoder : IBase64Encoder<byte>
        {
            if (buffer.IsEmpty)
            {
                bytesWritten = 0;
                return OperationStatus.Done;
            }

            int encodedLength = encoder.GetMaxEncodedLength(dataLength);
            if (buffer.Length < encodedLength)
            {
                bytesWritten = 0;
                return OperationStatus.DestinationTooSmall;
            }

            int leftover = (int)((uint)dataLength % 3); // how many bytes after packs of 3

            int destinationIndex = (int)encoder.GetInPlaceDestinationLength(encodedLength, leftover);
            int sourceIndex = dataLength - leftover;
            ReadOnlySpan<byte> encodingMap = encoder.EncodingMap;

            // encode last pack first, the full blocks below may overwrite its source
            if (leftover != 0)
            {
                if (leftover == 1)
                {
                    encoder.EncodeOneOptionallyPadTwo(buffer.Slice(sourceIndex, 1), buffer.Slice(destinationIndex), encodingMap);
                }
                else
                {
                    encoder.EncodeTwoOptionallyPadOne(buffer.Slice(sourceIndex, 2), buffer.Slice(destinationIndex), encodingMap);
                }
            }

            bytesWritten = encodedLength;

            // Encode the full blocks back to front. The output of each block starts at or after its own source start,
            // so it never overwrites source that hasn't been read yet.
            if (sourceIndex >= 16)
            {
                EncodeInPlaceChunked(encoder, buffer, sourceIndex);
                return OperationStatus.Done;
            }

            // Too small for the vectorized paths. Read all (at most 15) source bytes into registers first, then encode forward.
            if (sourceIndex != 0)
            {
                ulong lo;
                ulong hi = 0;
                if (buffer.Length >= 16)
                {
                    lo = BinaryPrimitives.ReadUInt64LittleEndian(buffer);
                    hi = BinaryPrimitives.ReadUInt64LittleEndian(buffer.Slice(8));
                }
                else if (buffer.Length >= 8)
                {
                    lo = BinaryPrimitives.ReadUInt64LittleEndian(buffer);
                    if (buffer.Length >= 12)
                    {
                        hi = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(8));
                    }
                }
                else
                {
                    // sourceIndex is 3 here, and the encoded output needs at least 4 bytes.
                    lo = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
                }

                Span<byte> dest = buffer.Slice(0, (int)((uint)sourceIndex / 3) * 4);
                while (dest.Length >= 4)
                {
                    // The low 3 bytes of 'lo' are the next block, big-endian order gives the 24-bit value.
                    uint i = BinaryPrimitives.ReverseEndianness((uint)lo) >> 8;
                    BinaryPrimitives.WriteUInt32LittleEndian(dest, EncodeBlock(i, encodingMap));
                    lo = (lo >> 24) | (hi << 40);
                    hi >>= 24;
                    dest = dest.Slice(4);
                }
            }

            return OperationStatus.Done;
        }

        /// <summary>Encodes the first <paramref name="sourceLength"/> (a multiple of 3) bytes of <paramref name="buffer"/> in place, back to front.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void EncodeInPlaceChunked<TBase64Encoder>(TBase64Encoder encoder, Span<byte> buffer, int sourceLength)
            where TBase64Encoder : IBase64Encoder<byte>
        {
            // Each chunk is copied aside first, so the vectorized EncodeTo can overwrite its source.
            Span<byte> chunk = stackalloc byte[Math.Min(sourceLength, InPlaceChunkSize)];
            while (sourceLength > 0)
            {
                int chunkLength = Math.Min(sourceLength, chunk.Length);
                sourceLength -= chunkLength;

                buffer.Slice(sourceLength, chunkLength).CopyTo(chunk);
                OperationStatus status = EncodeTo(encoder, chunk.Slice(0, chunkLength), buffer.Slice(sourceLength / 3 * 4), out _, out _);
                Debug.Assert(status == OperationStatus.Done);
            }
        }

        private const int InPlaceChunkSize = 768; // multiple of 3 (and of the 48-byte vector blocks)

        /// <summary>Encodes three bytes into four Base64 characters packed as a little-endian uint.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Encode(ReadOnlySpan<byte> threeBytes, ReadOnlySpan<byte> encodingMap)
        {
            uint b0 = threeBytes[0];
            uint b1 = threeBytes[1];
            uint b2 = threeBytes[2];

            uint i0 = encodingMap[(int)(b0 >> 2)];
            uint i1 = encodingMap[(int)((b0 << 4) | (b1 >> 4)) & 0x3F];
            uint i2 = encodingMap[(int)((b1 << 2) | (b2 >> 6)) & 0x3F];
            uint i3 = encodingMap[(int)b2 & 0x3F];

            return i0 | (i1 << 8) | (i2 << 16) | (i3 << 24);
        }

        /// <summary>Encodes a 24-bit block into four Base64 characters packed as a little-endian uint.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint EncodeBlock(uint i, ReadOnlySpan<byte> encodingMap)
        {
            uint i0 = encodingMap[(int)(i >> 18) & 0x3F];
            uint i1 = encodingMap[(int)(i >> 12) & 0x3F];
            uint i2 = encodingMap[(int)(i >> 6) & 0x3F];
            uint i3 = encodingMap[(int)i & 0x3F];

            return i0 | (i1 << 8) | (i2 << 16) | (i3 << 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EncodeOneOptionallyPadTwo(ReadOnlySpan<byte> oneByte, Span<ushort> dest, ReadOnlySpan<byte> encodingMap)
        {
            uint t0 = oneByte[0];

            dest[1] = encodingMap[(int)(t0 << 4) & 0x3F];
            dest[0] = encodingMap[(int)(t0 >> 2)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EncodeTwoOptionallyPadOne(ReadOnlySpan<byte> twoBytes, Span<ushort> dest, ReadOnlySpan<byte> encodingMap)
        {
            uint i = ((uint)twoBytes[0] << 16) | ((uint)twoBytes[1] << 8);

            dest[2] = encodingMap[(int)(i >> 6) & 0x3F];
            dest[1] = encodingMap[(int)(i >> 12) & 0x3F];
            dest[0] = encodingMap[(int)(i >> 18) & 0x3F];
        }

        internal const uint EncodingPad = '='; // '=', for padding

        internal const int MaximumEncodeLength = (int.MaxValue / 4) * 3; // 1610612733

        internal readonly struct Base64EncoderByte : IBase64Encoder<byte>
        {
            public ReadOnlySpan<byte> EncodingMap => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"u8;

            public sbyte Avx2LutChar62 => -19;  // char '+' diff

            public sbyte Avx2LutChar63 => -16;   // char '/' diff

            public ReadOnlySpan<byte> AdvSimdLut4 => "wxyz0123456789+/"u8;

            public uint Ssse3AdvSimdLutE3 => 0x0000F0ED;

            public int IncrementPadTwo => 4;

            public int IncrementPadOne => 4;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetMaxSrcLength(int srcLength, int destLength) =>
                srcLength <= MaximumEncodeLength && destLength >= Base64.GetMaxEncodedToUtf8Length(srcLength) ?
                srcLength : (destLength >> 2) * 3;

            public uint GetInPlaceDestinationLength(int encodedLength, int _) => (uint)(encodedLength - 4);

            public int GetMaxEncodedLength(int srcLength) => Base64.GetMaxEncodedToUtf8Length(srcLength);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void EncodeOneOptionallyPadTwo(ReadOnlySpan<byte> oneByte, Span<byte> dest, ReadOnlySpan<byte> encodingMap)
            {
                uint t0 = oneByte[0];

                uint i0 = encodingMap[(int)(t0 >> 2)];
                uint i1 = encodingMap[(int)(t0 << 4) & 0x3F];

                BinaryPrimitives.WriteUInt32LittleEndian(dest, i0 | (i1 << 8) | (EncodingPad << 16) | (EncodingPad << 24));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void EncodeTwoOptionallyPadOne(ReadOnlySpan<byte> twoBytes, Span<byte> dest, ReadOnlySpan<byte> encodingMap)
            {
                uint i = ((uint)twoBytes[0] << 16) | ((uint)twoBytes[1] << 8);

                uint i0 = encodingMap[(int)(i >> 18) & 0x3F];
                uint i1 = encodingMap[(int)(i >> 12) & 0x3F];
                uint i2 = encodingMap[(int)(i >> 6) & 0x3F];

                BinaryPrimitives.WriteUInt32LittleEndian(dest, i0 | (i1 << 8) | (i2 << 16) | (EncodingPad << 24));
            }

#if NET
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void StoreVector512ToDestination(Span<byte> dest, Vector512<byte> str) => str.CopyTo(dest);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void StoreVector256ToDestination(Span<byte> dest, Vector256<byte> str) => str.CopyTo(dest);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void StoreVector128ToDestination(Span<byte> dest, Vector128<byte> str) => str.CopyTo(dest);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            public void StoreArmVector128x4ToDestination(Span<byte> dest,
                Vector128<byte> res1, Vector128<byte> res2, Vector128<byte> res3, Vector128<byte> res4)
            {
                AdvSimdZip4(res1, res2, res3, res4, out Vector128<byte> out1, out Vector128<byte> out2, out Vector128<byte> out3, out Vector128<byte> out4);
                out1.CopyTo(dest);
                out2.CopyTo(dest.Slice(16, 16));
                out3.CopyTo(dest.Slice(32, 16));
                out4.CopyTo(dest.Slice(48, 16));
            }
#endif // NET

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void EncodeThreeAndWrite(ReadOnlySpan<byte> threeBytes, Span<byte> destination, ReadOnlySpan<byte> encodingMap) =>
                BinaryPrimitives.WriteUInt32LittleEndian(destination, Encode(threeBytes, encodingMap));
        }

        internal readonly struct Base64EncoderChar : IBase64Encoder<ushort>
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
            public void EncodeOneOptionallyPadTwo(ReadOnlySpan<byte> oneByte, Span<ushort> dest, ReadOnlySpan<byte> encodingMap)
            {
                Base64Helper.EncodeOneOptionallyPadTwo(oneByte, dest, encodingMap);
                dest[3] = (ushort)EncodingPad;
                dest[2] = (ushort)EncodingPad;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void EncodeTwoOptionallyPadOne(ReadOnlySpan<byte> twoBytes, Span<ushort> dest, ReadOnlySpan<byte> encodingMap)
            {
                Base64Helper.EncodeTwoOptionallyPadOne(twoBytes, dest, encodingMap);
                dest[3] = (ushort)EncodingPad;
            }

#if NET
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void StoreVector512ToDestination(Span<ushort> dest, Vector512<byte> str)
            {
                (Vector512<ushort> utf16LowVector, Vector512<ushort> utf16HighVector) = Vector512.Widen(str);
                utf16LowVector.CopyTo(dest);
                utf16HighVector.CopyTo(dest.Slice(32, 32));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void StoreVector256ToDestination(Span<ushort> dest, Vector256<byte> str)
            {
                (Vector256<ushort> utf16LowVector, Vector256<ushort> utf16HighVector) = Vector256.Widen(str);
                utf16LowVector.CopyTo(dest);
                utf16HighVector.CopyTo(dest.Slice(16, 16));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void StoreVector128ToDestination(Span<ushort> dest, Vector128<byte> str)
            {
                (Vector128<ushort> utf16LowVector, Vector128<ushort> utf16HighVector) = Vector128.Widen(str);
                utf16LowVector.CopyTo(dest);
                utf16HighVector.CopyTo(dest.Slice(8, 8));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            public void StoreArmVector128x4ToDestination(Span<ushort> dest,
                Vector128<byte> res1, Vector128<byte> res2, Vector128<byte> res3, Vector128<byte> res4)
            {
                AdvSimdZip4(res1, res2, res3, res4, out Vector128<byte> out1, out Vector128<byte> out2, out Vector128<byte> out3, out Vector128<byte> out4);
                (Vector128<ushort> utf16LowVector1, Vector128<ushort> utf16HighVector1) = Vector128.Widen(out1);
                (Vector128<ushort> utf16LowVector2, Vector128<ushort> utf16HighVector2) = Vector128.Widen(out2);
                (Vector128<ushort> utf16LowVector3, Vector128<ushort> utf16HighVector3) = Vector128.Widen(out3);
                (Vector128<ushort> utf16LowVector4, Vector128<ushort> utf16HighVector4) = Vector128.Widen(out4);
                utf16LowVector1.CopyTo(dest);
                utf16HighVector1.CopyTo(dest.Slice(8, 8));
                utf16LowVector2.CopyTo(dest.Slice(16, 8));
                utf16HighVector2.CopyTo(dest.Slice(24, 8));
                utf16LowVector3.CopyTo(dest.Slice(32, 8));
                utf16HighVector3.CopyTo(dest.Slice(40, 8));
                utf16LowVector4.CopyTo(dest.Slice(48, 8));
                utf16HighVector4.CopyTo(dest.Slice(56, 8));
            }
#endif // NET

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void EncodeThreeAndWrite(ReadOnlySpan<byte> threeBytes, Span<ushort> destination, ReadOnlySpan<byte> encodingMap)
            {
                uint b0 = threeBytes[0];
                uint b1 = threeBytes[1];
                uint b2 = threeBytes[2];

                destination[0] = encodingMap[(int)(b0 >> 2)];
                destination[1] = encodingMap[(int)((b0 << 4) | (b1 >> 4)) & 0x3F];
                destination[2] = encodingMap[(int)((b1 << 2) | (b2 >> 6)) & 0x3F];
                destination[3] = encodingMap[(int)b2 & 0x3F];
            }
        }
    }
}
