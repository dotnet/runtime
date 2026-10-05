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
        private const int Avx512EncodeInputLength = 48;
        private const int Avx512EncodeReadLength = 64;
        private const int Avx512EncodeOutputLength = 64;
        private const int Avx2EncodeInputLength = 24;
        private const int Avx2EncodeReadLength = 32;
        private const int Avx2EncodeOutputLength = 32;
        private const int Vector128EncodeInputLength = 12;
        private const int Vector128EncodeReadLength = 16;
        private const int Vector128EncodeOutputLength = 16;
        private const int AdvSimdEncodeInputLength = 48;
        private const int AdvSimdEncodeOutputLength = 64;
        private const int MaxSmallInPlaceInputLength = 2 * Vector128EncodeInputLength;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            return EncodeToCore(encoder, source, destination, out bytesConsumed, out bytesWritten, isFinalBlock);
        }

        private static OperationStatus EncodeToCore<TBase64Encoder, T>(TBase64Encoder encoder, ReadOnlySpan<byte> source,
            Span<T> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)
            where TBase64Encoder : IBase64Encoder<T>
            where T : unmanaged
        {
            int srcLength = source.Length;
            int destLength = destination.Length;
            int maxSrcLength = encoder.GetMaxSrcLength(srcLength, destLength);

            ReadOnlySpan<byte> src = source;
            Span<T> dest = destination;

#if NET
            if (maxSrcLength >= Vector128EncodeInputLength)
            {
                if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported)
                {
                    Avx512Encode(encoder, ref src, ref dest);
                    goto Scalar;
                }

                if (Avx2.IsSupported && src.Length >= Avx2EncodeReadLength && dest.Length >= Avx2EncodeOutputLength)
                {
                    Avx2Encode(encoder, ref src, ref dest);

                    if (src.IsEmpty)
                    {
                        goto DoneExit;
                    }
                }

                if (AdvSimd.Arm64.IsSupported && src.Length >= AdvSimdEncodeInputLength && dest.Length >= AdvSimdEncodeOutputLength)
                {
                    AdvSimdEncode(encoder, ref src, ref dest);

                    if (src.IsEmpty)
                    {
                        goto DoneExit;
                    }
                }

                if ((Ssse3.IsSupported || AdvSimd.Arm64.IsSupported || PackedSimd.IsSupported) &&
                    BitConverter.IsLittleEndian &&
                    src.Length >= Vector128EncodeReadLength &&
                    dest.Length >= Vector128EncodeOutputLength)
                {
                    Vector128Encode(encoder, ref src, ref dest);

                    if (src.IsEmpty)
                    {
                        goto DoneExit;
                    }
                }
            }
        Scalar:
#endif
            ReadOnlySpan<byte> encodingMap = encoder.EncodingMap;

            while (src.Length >= 3 && dest.Length >= 4)
            {
                encoder.EncodeThreeAndWrite(src, dest, encodingMap);
                src = src.Slice(3);
                dest = dest.Slice(4);
            }

            if (maxSrcLength != srcLength)
            {
                goto DestinationTooSmallExit;
            }

            if (!isFinalBlock)
            {
                if (src.IsEmpty)
                {
                    goto DoneExit;
                }

                goto NeedMoreData;
            }

            if (src.Length == 1)
            {
                if (dest.Length < encoder.IncrementPadTwo)
                {
                    goto DestinationTooSmallExit;
                }

                encoder.EncodeOneOptionallyPadTwo(src, dest, encodingMap);
                src = src.Slice(1);
                dest = dest.Slice(encoder.IncrementPadTwo);
            }
            else if (src.Length == 2)
            {
                if (dest.Length < encoder.IncrementPadOne)
                {
                    goto DestinationTooSmallExit;
                }

                encoder.EncodeTwoOptionallyPadOne(src, dest, encodingMap);
                src = src.Slice(2);
                dest = dest.Slice(encoder.IncrementPadOne);
            }

        DoneExit:
            bytesConsumed = srcLength - src.Length;
            bytesWritten = destLength - dest.Length;
            return OperationStatus.Done;

        DestinationTooSmallExit:
            bytesConsumed = srcLength - src.Length;
            bytesWritten = destLength - dest.Length;
            return OperationStatus.DestinationTooSmall;

        NeedMoreData:
            bytesConsumed = srcLength - src.Length;
            bytesWritten = destLength - dest.Length;
            return OperationStatus.NeedMoreData;
        }

#if NET
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx512BW))]
        [CompExactlyDependsOn(typeof(Avx512Vbmi))]
        private static void Avx512Encode<TBase64Encoder, T>(TBase64Encoder encoder, ref ReadOnlySpan<byte> srcBytes, ref Span<T> destBytes)
            where TBase64Encoder : IBase64Encoder<T>
            where T : unmanaged
        {
            // Reference for VBMI implementation : https://github.com/WojciechMula/base64simd/tree/master/encode
            // If we have AVX512 support, pick off 48 bytes at a time for as long as we can.
            // But because we read 64 bytes at a time, ensure we have enough room to do a
            // full 64-byte read within the source span.

            ReadOnlySpan<byte> src = srcBytes;
            Span<T> dest = destBytes;

            // The JIT won't hoist these "constants", so help it
            Vector512<sbyte> shuffleVecVbmi = EncodingShuffleVector;
            Vector512<sbyte> vbmiLookup = Vector512.Create(encoder.EncodingMap).AsSByte();

            Vector512<byte> shifts = Vector512.Create(0x3036242A1016040Aul).AsByte();

            // This algorithm requires AVX512VBMI support.
            // Vbmi was first introduced in CannonLake and is available from IceLake on.

            while (src.Length >= Avx512EncodeReadLength && dest.Length >= Avx512EncodeOutputLength)
            {
                Vector512<byte> str = EncodeVector(Vector512.Create(src), shuffleVecVbmi, vbmiLookup, shifts);
                encoder.StoreVector512ToDestination(dest, str);

                src = src.Slice(Avx512EncodeInputLength);
                dest = dest.Slice(Avx512EncodeOutputLength);
            }

            while (src.Length >= Avx2EncodeReadLength && dest.Length >= Avx2EncodeOutputLength)
            {
                Vector512<byte> input = Vector256.Create(src).ToVector512();
                Vector512<byte> str = EncodeVector(input, shuffleVecVbmi, vbmiLookup, shifts);
                encoder.StoreVector256ToDestination(dest, str.GetLower());
                src = src.Slice(Avx2EncodeInputLength);
                dest = dest.Slice(Avx2EncodeOutputLength);
            }

            while (src.Length >= Vector128EncodeReadLength && dest.Length >= Vector128EncodeOutputLength)
            {
                Vector512<byte> input = Vector128.Create(src).ToVector256().ToVector512();
                Vector512<byte> str = EncodeVector(input, shuffleVecVbmi, vbmiLookup, shifts);
                encoder.StoreVector128ToDestination(dest, str.GetLower().GetLower());
                src = src.Slice(Vector128EncodeInputLength);
                dest = dest.Slice(Vector128EncodeOutputLength);
            }

            if (src.Length >= Vector128EncodeInputLength && dest.Length >= Vector128EncodeOutputLength)
            {
                Vector128<byte> input128 = Vector128.Create(
                    BinaryPrimitives.ReadUInt64LittleEndian(src), (ulong)BinaryPrimitives.ReadUInt32LittleEndian(src.Slice(8))).AsByte();
                Vector512<byte> input = input128.ToVector256().ToVector512();
                Vector512<byte> str = EncodeVector(input, shuffleVecVbmi, vbmiLookup, shifts);
                encoder.StoreVector128ToDestination(dest, str.GetLower().GetLower());
                src = src.Slice(Vector128EncodeInputLength);
                dest = dest.Slice(Vector128EncodeOutputLength);
            }

            srcBytes = src;
            destBytes = dest;
        }

        private static Vector512<sbyte> EncodingShuffleVector
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Vector512.Create(
                0x01020001, 0x04050304, 0x07080607, 0x0a0b090a,
                0x0d0e0c0d, 0x10110f10, 0x13141213, 0x16171516,
                0x191a1819, 0x1c1d1b1c, 0x1f201e1f, 0x22232122,
                0x25262425, 0x28292728, 0x2b2c2a2b, 0x2e2f2d2e).AsSByte();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx512Vbmi))]
        private static Vector512<byte> EncodeVector(Vector512<byte> input, Vector512<sbyte> shuffle, Vector512<sbyte> lookup, Vector512<byte> shifts)
        {
            Vector512<byte> str = Avx512Vbmi.PermuteVar64x8(input, shuffle.AsByte());
            // VPERMB uses only the low six bits of each index.
            str = Avx512Vbmi.MultiShift(shifts, str.AsUInt64());
            return Avx512Vbmi.PermuteVar64x8(lookup.AsByte(), str);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx2))]
        private static void Avx2Encode<TBase64Encoder, T>(TBase64Encoder encoder, ref ReadOnlySpan<byte> srcBytes, ref Span<T> destBytes)
            where TBase64Encoder : IBase64Encoder<T>
            where T : unmanaged
        {
            // If we have AVX2 support, pick off 24 bytes at a time for as long as we can.
            // But because we read 32 bytes at a time, ensure we have enough room to do a
            // full 32-byte read within the source span.

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

            ReadOnlySpan<byte> src = srcBytes;
            Span<T> dest = destBytes;

            if (src.Length < Avx2EncodeReadLength)
            {
                return;
            }

            // The first load does not overlap preceding input.
            Vector256<sbyte> str = Vector256.Create(src).AsSByte();

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

            // Later loads overlap the preceding four bytes.
            int stride = Avx2EncodeInputLength - 4;

            while (src.Length >= Avx2EncodeReadLength && dest.Length >= Avx2EncodeOutputLength)
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

                src = src.Slice(stride);
                stride = Avx2EncodeInputLength;
                dest = dest.Slice(32);

                if (src.Length < Avx2EncodeReadLength)
                {
                    break;
                }

                // Load at the overlapping cursor, as required by Reshuffle.
                str = Vector256.Create(src).AsSByte();
            }

            srcBytes = src.Slice(4);
            destBytes = dest;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        private static void AdvSimdEncode<TBase64Encoder, T>(TBase64Encoder encoder, ref ReadOnlySpan<byte> srcBytes, ref Span<T> destBytes)
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
            ReadOnlySpan<byte> src = srcBytes;
            Span<T> dest = destBytes;

            // If we have Neon support, pick off 48 bytes at a time for as long as we can.
            while (src.Length >= AdvSimdEncodeInputLength && dest.Length >= AdvSimdEncodeOutputLength)
            {
                // Load 48 bytes and deinterleave:
                (str1, str2, str3) = LoadArmVector128x3(src);

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

                src = src.Slice(AdvSimdEncodeInputLength);
                dest = dest.Slice(AdvSimdEncodeOutputLength);
            }

            srcBytes = src;
            destBytes = dest;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Ssse3))]
        [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
        [CompExactlyDependsOn(typeof(PackedSimd))]
        private static void Vector128Encode<TBase64Encoder, T>(TBase64Encoder encoder, ref ReadOnlySpan<byte> srcBytes, ref Span<T> destBytes)
            where TBase64Encoder : IBase64Encoder<T>
            where T : unmanaged
        {
            // If we have SSSE3 support, pick off 12 bytes at a time for as long as we can.
            // But because we read 16 bytes at a time, ensure we have enough room to do a
            // full 16-byte read within the source span.

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

            ReadOnlySpan<byte> src = srcBytes;
            Span<T> dest = destBytes;

            while (src.Length >= Vector128EncodeReadLength && dest.Length >= Vector128EncodeOutputLength)
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

                src = src.Slice(Vector128EncodeInputLength);
                dest = dest.Slice(Vector128EncodeOutputLength);
            }

            srcBytes = src;
            destBytes = dest;
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            ReadOnlySpan<byte> encodingMap = encoder.EncodingMap;
            if (dataLength <= 3)
            {
                if (dataLength == 1)
                {
                    encoder.EncodeOneOptionallyPadTwo(buffer, buffer, encodingMap);
                }
                else if (dataLength == 2)
                {
                    encoder.EncodeTwoOptionallyPadOne(buffer, buffer, encodingMap);
                }
                else if (dataLength == 3)
                {
                    uint input = ((uint)buffer[0] << 16) | ((uint)buffer[1] << 8) | buffer[2];
                    uint result = Encode(input, encodingMap);
                    BinaryPrimitives.WriteUInt32LittleEndian(buffer, result);
                }

                bytesWritten = encodedLength;
                return OperationStatus.Done;
            }

            return EncodeToUtf8InPlaceCore(encoder, buffer, dataLength, encodedLength, out bytesWritten);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static OperationStatus EncodeToUtf8InPlaceCore<TBase64Encoder>(TBase64Encoder encoder, Span<byte> buffer,
            int dataLength, int encodedLength, out int bytesWritten)
            where TBase64Encoder : IBase64Encoder<byte>
        {
            ReadOnlySpan<byte> encodingMap = encoder.EncodingMap;
            int leftover = (int)((uint)dataLength % 3); // how many bytes after packs of 3

            uint destinationIndex = encoder.GetInPlaceDestinationLength(encodedLength, leftover);
            uint sourceIndex = (uint)(dataLength - leftover);

            // encode last pack to avoid conditional in the main loop
            if (leftover != 0)
            {
                if (leftover == 1)
                {
                    encoder.EncodeOneOptionallyPadTwo(buffer.Slice((int)sourceIndex), buffer.Slice((int)destinationIndex), encodingMap);
                }
                else
                {
                    encoder.EncodeTwoOptionallyPadOne(buffer.Slice((int)sourceIndex), buffer.Slice((int)destinationIndex), encodingMap);
                }
            }

            if (sourceIndex > MaxSmallInPlaceInputLength)
            {
                EncodeChunksInPlace(encoder, buffer, (int)sourceIndex);
                bytesWritten = encodedLength;
                return OperationStatus.Done;
            }
#if NET
            if (Vector512.IsHardwareAccelerated && Avx512Vbmi.IsSupported && sourceIndex >= Vector128EncodeInputLength)
            {
                EncodeSmallInPlace(buffer, (int)sourceIndex, encodingMap);
                bytesWritten = encodedLength;
                return OperationStatus.Done;
            }
#endif
            if (leftover != 0)
            {
                destinationIndex -= 4;
            }

            sourceIndex -= 3;
            while ((int)sourceIndex >= 0)
            {
                uint input = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice((int)sourceIndex)) >> 8;
                uint result = Encode(input, encodingMap);
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice((int)destinationIndex), result);
                destinationIndex -= 4;
                sourceIndex -= 3;
            }

            bytesWritten = encodedLength;
            return OperationStatus.Done;
        }

#if NET
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CompExactlyDependsOn(typeof(Avx512Vbmi))]
        private static void EncodeSmallInPlace(Span<byte> buffer, int sourceLength, ReadOnlySpan<byte> encodingMap)
        {
            // Preserve the suffix before expanding the first twelve input bytes.
            while (sourceLength > Vector128EncodeInputLength)
            {
                sourceLength -= 3;
                uint input = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(sourceLength)) >> 8;
                BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(sourceLength / 3 * 4), Encode(input, encodingMap));
            }

            Vector512<byte> inputVector = Vector128.Create(buffer).ToVector256().ToVector512();
            Vector512<byte> result = EncodeVector(inputVector, EncodingShuffleVector, Vector512.Create(encodingMap).AsSByte(),
                Vector512.Create(0x3036242A1016040Aul).AsByte());
            result.GetLower().GetLower().CopyTo(buffer);
        }
#endif

        private static void EncodeChunksInPlace<TBase64Encoder>(TBase64Encoder encoder, Span<byte> buffer, int sourceLength)
            where TBase64Encoder : IBase64Encoder<byte>
        {
            Span<byte> scratch = stackalloc byte[MaxStackallocThreshold];
            const int MaxChunkLength = MaxStackallocThreshold / 4 * 3;
            while (sourceLength > 0)
            {
                int chunkLength = Math.Min(sourceLength, MaxChunkLength);
                sourceLength -= chunkLength;
                int outputLength = chunkLength / 3 * 4;
                Span<byte> output = scratch.Slice(scratch.Length - outputLength);
                OperationStatus status = EncodeToCore(encoder, buffer.Slice(sourceLength), output,
                    out int consumed, out int written, isFinalBlock: false);
                Debug.Assert(status is OperationStatus.Done or OperationStatus.DestinationTooSmall);
                Debug.Assert(consumed == chunkLength && written == outputLength);
                output.CopyTo(buffer.Slice(sourceLength / 3 * 4));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Encode(ReadOnlySpan<byte> threeBytes, ReadOnlySpan<byte> encodingMap)
        {
            uint i = ((uint)BinaryPrimitives.ReadUInt16BigEndian(threeBytes) << 8) | threeBytes[2];
            return Encode(i, encodingMap);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Encode(uint i, ReadOnlySpan<byte> encodingMap)
        {
            uint i0 = encodingMap[(int)(i >> 18)];
            uint i1 = encodingMap[(int)((i >> 12) & 0x3F)];
            uint i2 = encodingMap[(int)((i >> 6) & 0x3F)];
            uint i3 = encodingMap[(int)(i & 0x3F)];

            return ConstructResult(i0, i1, i2, i3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ConstructResult(uint i0, uint i1, uint i2, uint i3)
        {
            return i0 | (i1 << 8) | (i2 << 16) | (i3 << 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EncodeOneOptionallyPadTwo(ReadOnlySpan<byte> oneByte, Span<ushort> dest, ReadOnlySpan<byte> encodingMap)
        {
            uint t0 = oneByte[0];

            uint i = t0 << 8;

            uint i0 = encodingMap[(int)(i >> 10)];
            uint i1 = encodingMap[(int)((i >> 4) & 0x3F)];

            dest[0] = (ushort)i0;
            dest[1] = (ushort)i1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EncodeTwoOptionallyPadOne(ReadOnlySpan<byte> twoBytes, Span<ushort> dest, ReadOnlySpan<byte> encodingMap)
        {
            uint t0 = twoBytes[0];
            uint t1 = twoBytes[1];

            uint i = (t0 << 16) | (t1 << 8);

            ushort i0 = encodingMap[(int)(i >> 18)];
            ushort i1 = encodingMap[(int)((i >> 12) & 0x3F)];
            ushort i2 = encodingMap[(int)((i >> 6) & 0x3F)];

            dest[0] = i0;
            dest[1] = i1;
            dest[2] = i2;
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

                uint i = t0 << 8;

                uint i0 = encodingMap[(int)(i >> 10)];
                uint i1 = encodingMap[(int)((i >> 4) & 0x3F)];

                uint result = ConstructResult(i0, i1, EncodingPad, EncodingPad);
                BinaryPrimitives.WriteUInt32LittleEndian(dest, result);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void EncodeTwoOptionallyPadOne(ReadOnlySpan<byte> twoBytes, Span<byte> dest, ReadOnlySpan<byte> encodingMap)
            {
                uint t0 = twoBytes[0];
                uint t1 = twoBytes[1];

                uint i = (t0 << 16) | (t1 << 8);

                uint i0 = encodingMap[(int)(i >> 18)];
                uint i1 = encodingMap[(int)((i >> 12) & 0x3F)];
                uint i2 = encodingMap[(int)((i >> 6) & 0x3F)];

                uint result = ConstructResult(i0, i1, i2, EncodingPad);
                BinaryPrimitives.WriteUInt32LittleEndian(dest, result);
            }

#if NET
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void StoreVector512ToDestination(Span<byte> dest, Vector512<byte> str)
            {
                str.CopyTo(dest);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(Avx2))]
            public void StoreVector256ToDestination(Span<byte> dest, Vector256<byte> str)
            {
                str.AsByte().CopyTo(dest);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void StoreVector128ToDestination(Span<byte> dest, Vector128<byte> str)
            {
                str.CopyTo(dest);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            public void StoreArmVector128x4ToDestination(Span<byte> dest,
                Vector128<byte> res1, Vector128<byte> res2, Vector128<byte> res3, Vector128<byte> res4)
            {
                StoreArmVector128x4(dest, res1, res2, res3, res4);
            }
#endif // NET

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void EncodeThreeAndWrite(ReadOnlySpan<byte> threeBytes, Span<byte> destination, ReadOnlySpan<byte> encodingMap)
            {
                uint result = Encode(threeBytes, encodingMap);
                BinaryPrimitives.WriteUInt32LittleEndian(destination, result);
            }
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
                dest[2] = (ushort)EncodingPad;
                dest[3] = (ushort)EncodingPad;
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
                utf16HighVector.CopyTo(dest.Slice(32));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void StoreVector256ToDestination(Span<ushort> dest, Vector256<byte> str)
            {
                (Vector256<ushort> utf16LowVector, Vector256<ushort> utf16HighVector) = Vector256.Widen(str);
                utf16LowVector.CopyTo(dest);
                utf16HighVector.CopyTo(dest.Slice(16));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void StoreVector128ToDestination(Span<ushort> dest, Vector128<byte> str)
            {
                (Vector128<ushort> utf16LowVector, Vector128<ushort> utf16HighVector) = Vector128.Widen(str);
                utf16LowVector.CopyTo(dest);
                utf16HighVector.CopyTo(dest.Slice(8));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(AdvSimd.Arm64))]
            public void StoreArmVector128x4ToDestination(Span<ushort> dest,
                Vector128<byte> res1, Vector128<byte> res2, Vector128<byte> res3, Vector128<byte> res4)
            {
                (Vector128<ushort> utf16LowVector1, Vector128<ushort> utf16HighVector1) = Vector128.Widen(res1);
                (Vector128<ushort> utf16LowVector2, Vector128<ushort> utf16HighVector2) = Vector128.Widen(res2);
                (Vector128<ushort> utf16LowVector3, Vector128<ushort> utf16HighVector3) = Vector128.Widen(res3);
                (Vector128<ushort> utf16LowVector4, Vector128<ushort> utf16HighVector4) = Vector128.Widen(res4);
                StoreArmVector128x4(dest, utf16LowVector1, utf16LowVector2, utf16LowVector3, utf16LowVector4);
                StoreArmVector128x4(dest.Slice(32), utf16HighVector1, utf16HighVector2, utf16HighVector3, utf16HighVector4);
            }
#endif // NET

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void EncodeThreeAndWrite(ReadOnlySpan<byte> threeBytes, Span<ushort> destination, ReadOnlySpan<byte> encodingMap)
            {
                uint t0 = threeBytes[0];
                uint t1 = threeBytes[1];
                uint t2 = threeBytes[2];

                uint i = (t0 << 16) | (t1 << 8) | t2;

                ushort i0 = encodingMap[(int)(i >> 18)];
                ushort i1 = encodingMap[(int)((i >> 12) & 0x3F)];
                ushort i2 = encodingMap[(int)((i >> 6) & 0x3F)];
                ushort i3 = encodingMap[(int)(i & 0x3F)];

                destination[0] = i0;
                destination[1] = i1;
                destination[2] = i2;
                destination[3] = i3;
            }
        }
    }
}
