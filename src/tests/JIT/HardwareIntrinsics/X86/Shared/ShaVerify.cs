// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace JIT.HardwareIntrinsics.X86
{
    // Scalar reference implementations of the SHA extensions, transcribed from the pseudocode in the
    // Intel 64 and IA-32 Architectures Software Developer's Manual, Volume 2.
    //
    // Each 128-bit operand is treated as four dwords, where dword i holds bits [32 * i + 31 : 32 * i]
    // and is stored little-endian at byte offset 4 * i. So SRC[127:96] is dword 3 and SRC[31:0] is dword 0.
    public static class ShaVerify
    {
        // Overloads used by the generated tests. They return byte 'i' of the expected result.

        public static byte Sha1FourRounds(byte[] a, byte[] b, byte func, int i) => ToBytes(Sha1FourRounds(ToDwords(a), ToDwords(b), func))[i];

        public static byte Sha1MessageSchedule1(byte[] a, byte[] b, int i) => ToBytes(Sha1MessageSchedule1(ToDwords(a), ToDwords(b)))[i];

        public static byte Sha1MessageSchedule2(byte[] a, byte[] b, int i) => ToBytes(Sha1MessageSchedule2(ToDwords(a), ToDwords(b)))[i];

        public static byte Sha1NextE(byte[] a, byte[] b, int i) => ToBytes(Sha1NextE(ToDwords(a), ToDwords(b)))[i];

        public static byte Sha256MessageSchedule1(byte[] a, byte[] b, int i) => ToBytes(Sha256MessageSchedule1(ToDwords(a), ToDwords(b)))[i];

        public static byte Sha256MessageSchedule2(byte[] a, byte[] b, int i) => ToBytes(Sha256MessageSchedule2(ToDwords(a), ToDwords(b)))[i];

        public static byte Sha256TwoRounds(byte[] a, byte[] b, byte[] k, int i) => ToBytes(Sha256TwoRounds(ToDwords(a), ToDwords(b), ToDwords(k)))[i];

        // Overloads used by the handwritten tests.

        public static Vector128<byte> Sha1FourRounds(Vector128<byte> a, Vector128<byte> b, byte func) => ToVector(Sha1FourRounds(ToDwords(a), ToDwords(b), func));

        public static Vector128<byte> Sha1MessageSchedule1(Vector128<byte> a, Vector128<byte> b) => ToVector(Sha1MessageSchedule1(ToDwords(a), ToDwords(b)));

        public static Vector128<byte> Sha1MessageSchedule2(Vector128<byte> a, Vector128<byte> b) => ToVector(Sha1MessageSchedule2(ToDwords(a), ToDwords(b)));

        public static Vector128<byte> Sha1NextE(Vector128<byte> a, Vector128<byte> b) => ToVector(Sha1NextE(ToDwords(a), ToDwords(b)));

        public static Vector128<byte> Sha256MessageSchedule1(Vector128<byte> a, Vector128<byte> b) => ToVector(Sha256MessageSchedule1(ToDwords(a), ToDwords(b)));

        public static Vector128<byte> Sha256MessageSchedule2(Vector128<byte> a, Vector128<byte> b) => ToVector(Sha256MessageSchedule2(ToDwords(a), ToDwords(b)));

        public static Vector128<byte> Sha256TwoRounds(Vector128<byte> a, Vector128<byte> b, Vector128<byte> k) => ToVector(Sha256TwoRounds(ToDwords(a), ToDwords(b), ToDwords(k)));

        // SHA1RNDS4 xmm1, xmm2/m128, imm8
        private static uint[] Sha1FourRounds(uint[] src1, uint[] src2, byte imm8)
        {
            // Only imm8[1:0] selects the logical function and the round constant.
            int func = imm8 & 0b11;
            uint k = Sha1RoundConstant(func);

            uint a = src1[3];
            uint b = src1[2];
            uint c = src1[1];
            uint d = src1[0];

            uint[] w = [src2[3], src2[2], src2[1], src2[0]];

            // Round 0 adds W0E, which already includes E. The remaining rounds add the E produced by the previous round.
            uint e = 0;

            for (int i = 0; i < 4; i++)
            {
                uint nextA = Sha1Function(func, b, c, d) + BitOperations.RotateLeft(a, 5) + w[i] + e + k;
                e = d;
                d = c;
                c = BitOperations.RotateLeft(b, 30);
                b = a;
                a = nextA;
            }

            return [d, c, b, a];
        }

        private static uint Sha1Function(int func, uint b, uint c, uint d) => func switch
        {
            0 => (b & c) ^ (~b & d),
            1 => b ^ c ^ d,
            2 => (b & c) ^ (b & d) ^ (c & d),
            _ => b ^ c ^ d,
        };

        private static uint Sha1RoundConstant(int func) => func switch
        {
            0 => 0x5A827999,
            1 => 0x6ED9EBA1,
            2 => 0x8F1BBCDC,
            _ => 0xCA62C1D6,
        };

        // SHA1MSG1 xmm1, xmm2/m128
        private static uint[] Sha1MessageSchedule1(uint[] src1, uint[] src2)
        {
            uint w0 = src1[3];
            uint w1 = src1[2];
            uint w2 = src1[1];
            uint w3 = src1[0];
            uint w4 = src2[3];
            uint w5 = src2[2];

            return [w5 ^ w3, w4 ^ w2, w3 ^ w1, w2 ^ w0];
        }

        // SHA1MSG2 xmm1, xmm2/m128
        private static uint[] Sha1MessageSchedule2(uint[] src1, uint[] src2)
        {
            uint w13 = src2[2];
            uint w14 = src2[1];
            uint w15 = src2[0];
            uint w16 = BitOperations.RotateLeft(src1[3] ^ w13, 1);
            uint w17 = BitOperations.RotateLeft(src1[2] ^ w14, 1);
            uint w18 = BitOperations.RotateLeft(src1[1] ^ w15, 1);
            uint w19 = BitOperations.RotateLeft(src1[0] ^ w16, 1);

            return [w19, w18, w17, w16];
        }

        // SHA1NEXTE xmm1, xmm2/m128
        private static uint[] Sha1NextE(uint[] src1, uint[] src2)
        {
            uint tmp = BitOperations.RotateLeft(src1[3], 30);
            return [src2[0], src2[1], src2[2], src2[3] + tmp];
        }

        // SHA256MSG1 xmm1, xmm2/m128
        private static uint[] Sha256MessageSchedule1(uint[] src1, uint[] src2)
        {
            uint w4 = src2[0];
            uint w3 = src1[3];
            uint w2 = src1[2];
            uint w1 = src1[1];
            uint w0 = src1[0];

            return [w0 + Sigma0(w1), w1 + Sigma0(w2), w2 + Sigma0(w3), w3 + Sigma0(w4)];
        }

        // SHA256MSG2 xmm1, xmm2/m128
        private static uint[] Sha256MessageSchedule2(uint[] src1, uint[] src2)
        {
            uint w14 = src2[2];
            uint w15 = src2[3];
            uint w16 = src1[0] + Sigma1(w14);
            uint w17 = src1[1] + Sigma1(w15);
            uint w18 = src1[2] + Sigma1(w16);
            uint w19 = src1[3] + Sigma1(w17);

            return [w16, w17, w18, w19];
        }

        // SHA256RNDS2 xmm1, xmm2/m128, <XMM0>
        private static uint[] Sha256TwoRounds(uint[] src1, uint[] src2, uint[] xmm0)
        {
            uint a = src2[3];
            uint b = src2[2];
            uint c = src1[3];
            uint d = src1[2];
            uint e = src2[1];
            uint f = src2[0];
            uint g = src1[1];
            uint h = src1[0];

            for (int i = 0; i < 2; i++)
            {
                uint wk = xmm0[i];
                uint t = Ch(e, f, g) + BigSigma1(e) + wk + h;
                uint nextA = t + Maj(a, b, c) + BigSigma0(a);
                uint nextE = t + d;

                h = g;
                g = f;
                f = e;
                e = nextE;
                d = c;
                c = b;
                b = a;
                a = nextA;
            }

            return [f, e, b, a];
        }

        private static uint Ch(uint e, uint f, uint g) => (e & f) ^ (~e & g);

        private static uint Maj(uint a, uint b, uint c) => (a & b) ^ (a & c) ^ (b & c);

        private static uint BigSigma0(uint a) => BitOperations.RotateRight(a, 2) ^ BitOperations.RotateRight(a, 13) ^ BitOperations.RotateRight(a, 22);

        private static uint BigSigma1(uint e) => BitOperations.RotateRight(e, 6) ^ BitOperations.RotateRight(e, 11) ^ BitOperations.RotateRight(e, 25);

        private static uint Sigma0(uint w) => BitOperations.RotateRight(w, 7) ^ BitOperations.RotateRight(w, 18) ^ (w >> 3);

        private static uint Sigma1(uint w) => BitOperations.RotateRight(w, 17) ^ BitOperations.RotateRight(w, 19) ^ (w >> 10);

        private static uint[] ToDwords(ReadOnlySpan<byte> bytes)
        {
            uint[] dwords = new uint[4];

            for (int i = 0; i < dwords.Length; i++)
            {
                dwords[i] = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(4 * i));
            }

            return dwords;
        }

        private static uint[] ToDwords(Vector128<byte> vector)
        {
            Span<byte> bytes = stackalloc byte[Vector128<byte>.Count];
            vector.CopyTo(bytes);
            return ToDwords(bytes);
        }

        private static byte[] ToBytes(uint[] dwords)
        {
            byte[] bytes = new byte[4 * dwords.Length];

            for (int i = 0; i < dwords.Length; i++)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4 * i), dwords[i]);
            }

            return bytes;
        }

        private static Vector128<byte> ToVector(uint[] dwords) => Vector128.Create(ToBytes(dwords));
    }
}
