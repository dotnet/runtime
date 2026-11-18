// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using JIT.HardwareIntrinsics.X86;
using Xunit;

namespace IntelHardwareIntrinsicTest._Sha
{
    // The SHA instructions only have the legacy SSE encoding. These tests cover the parts of that which the
    // template based tests can't: misaligned memory operands, other addressing forms, the same value passed as
    // several operands (including the implicit XMM0 operand of SHA256RNDS2), 256-bit and 512-bit values and
    // constants around the instructions, and complete SHA-1 and SHA-256 compressions checked against the
    // FIPS 180 examples.
    //
    // The methods that compile a specific pattern are marked AggressiveOptimization, so that they're compiled
    // with full optimization on their first call rather than as Tier0 code, which for example doesn't
    // enregister locals.
    public static unsafe class Program
    {
        private const int Seed = 20261117;
        private const int Iterations = 200;

        private static readonly uint[] s_sha1InitialState = [0x67452301, 0xEFCDAB89, 0x98BADCFE, 0x10325476, 0xC3D2E1F0];

        private static readonly uint[] s_sha256InitialState =
        [
            0x6A09E667, 0xBB67AE85, 0x3C6EF372, 0xA54FF53A, 0x510E527F, 0x9B05688C, 0x1F83D9AB, 0x5BE0CD19,
        ];

        private static readonly uint[] s_sha256RoundConstants =
        [
            0x428A2F98, 0x71374491, 0xB5C0FBCF, 0xE9B5DBA5, 0x3956C25B, 0x59F111F1, 0x923F82A4, 0xAB1C5ED5,
            0xD807AA98, 0x12835B01, 0x243185BE, 0x550C7DC3, 0x72BE5D74, 0x80DEB1FE, 0x9BDC06A7, 0xC19BF174,
            0xE49B69C1, 0xEFBE4786, 0x0FC19DC6, 0x240CA1CC, 0x2DE92C6F, 0x4A7484AA, 0x5CB0A9DC, 0x76F988DA,
            0x983E5152, 0xA831C66D, 0xB00327C8, 0xBF597FC7, 0xC6E00BF3, 0xD5A79147, 0x06CA6351, 0x14292967,
            0x27B70A85, 0x2E1B2138, 0x4D2C6DFC, 0x53380D13, 0x650A7354, 0x766A0ABB, 0x81C2C92E, 0x92722C85,
            0xA2BFE8A1, 0xA81A664B, 0xC24B8B70, 0xC76C51A3, 0xD192E819, 0xD6990624, 0xF40E3585, 0x106AA070,
            0x19A4C116, 0x1E376C08, 0x2748774C, 0x34B0BCB5, 0x391C0CB3, 0x4ED8AA4A, 0x5B9CCA4F, 0x682E6FF3,
            0x748F82EE, 0x78A5636F, 0x84C87814, 0x8CC70208, 0x90BEFFFA, 0xA4506CEB, 0xBEF9A3F7, 0xC67178F2,
        ];

        // These are deliberately not readonly, since the JIT treats a static readonly field as a constant once its
        // class is initialized. The tests need a func value whose upper bits aren't known at compile time and a
        // vector that is loaded from a static field.
        private static byte s_upperFuncBits = 0xA8;
        private static Vector128<byte> s_vectorSeed = Vector128.Create((byte)0xF0, 0xE1, 0xD2, 0xC3, 0xB4, 0xA5, 0x96, 0x87, 0x78, 0x69, 0x5A, 0x4B, 0x3C, 0x2D, 0x1E, 0x0F);

        private enum FuncMode
        {
            Constant,
            Variable,
            VariableWithUpperBits,
        }

        [ConditionalFact(typeof(Sha), nameof(Sha.IsSupported))]
        public static void MisalignedMemoryOperands()
        {
            Random random = new Random(Seed);
            byte[] buffer = new byte[256];
            random.NextBytes(buffer);

            fixed (byte* pBuffer = buffer)
            {
                byte* aligned = (byte*)(((nuint)pBuffer + 15) & ~(nuint)15);

                for (int offset = 0; offset < 32; offset++)
                {
                    byte* p = aligned + offset;
                    byte* q = aligned + 64 + (offset * 3 % 17);

                    Vector128<byte> x = Unsafe.ReadUnaligned<Vector128<byte>>(p);
                    Vector128<byte> y = Unsafe.ReadUnaligned<Vector128<byte>>(q);
                    Vector128<byte> k = Unsafe.ReadUnaligned<Vector128<byte>>(aligned + 128 + offset);

                    // Intel documents that the SHA instructions fault on an unaligned m128 operand, so these loads must not
                    // be folded. Not every processor checks the alignment, but a Checked JIT asserts that they aren't.
                    AssertEqual(ShaVerify.Sha1MessageSchedule1(y, x), Sha.Sha1MessageSchedule1(y, Sse2.LoadVector128(p)));
                    AssertEqual(ShaVerify.Sha1MessageSchedule2(y, x), Sha.Sha1MessageSchedule2(y, Unsafe.ReadUnaligned<Vector128<byte>>(p)));
                    AssertEqual(ShaVerify.Sha1NextE(y, x), Sha.Sha1NextE(y, Vector128.Load(p)));
                    AssertEqual(ShaVerify.Sha1FourRounds(y, x, 1), Sha.Sha1FourRounds(y, Sse2.LoadVector128(p), 1));
                    AssertEqual(ShaVerify.Sha256MessageSchedule1(y, x), Sha.Sha256MessageSchedule1(y, Vector128.LoadUnsafe(ref buffer[0], (nuint)(p - pBuffer))));
                    AssertEqual(ShaVerify.Sha256MessageSchedule2(y, x), Sha.Sha256MessageSchedule2(y, *(Vector128<byte>*)p));
                    AssertEqual(ShaVerify.Sha256TwoRounds(y, x, k), Sha.Sha256TwoRounds(y, Sse2.LoadVector128(p), k));

                    // Every operand loaded from an unaligned address.
                    AssertEqual(ShaVerify.Sha256TwoRounds(y, x, k), Sha.Sha256TwoRounds(Sse2.LoadVector128(q), Sse2.LoadVector128(p), Sse2.LoadVector128(aligned + 128 + offset)));
                    AssertEqual(ShaVerify.Sha1FourRounds(x, y, 3), Sha.Sha1FourRounds(Sse2.LoadVector128(p), Sse2.LoadVector128(q), 3));
                }

                // A misaligned field read through a reference.
                PackedVector[] packed = new PackedVector[3];

                for (int i = 0; i < packed.Length; i++)
                {
                    packed[i] = new PackedVector { Tag = (byte)i, Value = NextVector(random) };
                }

                for (int i = 0; i < packed.Length; i++)
                {
                    Vector128<byte> x = packed[i].Value;
                    Vector128<byte> y = packed[(i + 1) % packed.Length].Value;

                    AssertEqual(ShaVerify.Sha1MessageSchedule1(x, y), MessageSchedule1FromField(x, ref packed[(i + 1) % packed.Length]));
                }
            }
        }

        [ConditionalFact(typeof(Sha), nameof(Sha.IsSupported))]
        public static void AlignedMemoryOperands()
        {
            Random random = new Random(Seed);
            byte[] buffer = new byte[64 + 16];
            random.NextBytes(buffer);

            fixed (byte* pBuffer = buffer)
            {
                byte* p = (byte*)(((nuint)pBuffer + 15) & ~(nuint)15);

                Vector128<byte> x = Unsafe.ReadUnaligned<Vector128<byte>>(p);
                Vector128<byte> k = Unsafe.ReadUnaligned<Vector128<byte>>(p + 16);
                Vector128<byte> z = Unsafe.ReadUnaligned<Vector128<byte>>(p + 32);
                Vector128<byte> y = NextVector(random);

                // Aligned loads may be folded into the instruction.
                AssertEqual(ShaVerify.Sha1MessageSchedule1(y, x), AlignedLoads.Sha1MessageSchedule1(y, p));
                AssertEqual(ShaVerify.Sha1MessageSchedule2(y, x), AlignedLoads.Sha1MessageSchedule2(y, p));
                AssertEqual(ShaVerify.Sha1NextE(y, x), AlignedLoads.Sha1NextE(y, p));
                AssertEqual(ShaVerify.Sha1FourRounds(y, x, 2), AlignedLoads.Sha1FourRounds(y, p));
                AssertEqual(ShaVerify.Sha256MessageSchedule1(y, x), AlignedLoads.Sha256MessageSchedule1(y, p));
                AssertEqual(ShaVerify.Sha256MessageSchedule2(y, x), AlignedLoads.Sha256MessageSchedule2(y, p));
                AssertEqual(ShaVerify.Sha256TwoRounds(y, x, k), AlignedLoads.Sha256TwoRounds(y, p, k));

                // Addresses with an index register and a displacement, also followed by an immediate.
                AssertEqual(ShaVerify.Sha1MessageSchedule2(y, z), AlignedLoads.Sha1MessageSchedule2Indexed(y, p, 2));
                AssertEqual(ShaVerify.Sha1FourRounds(y, z, 3), AlignedLoads.Sha1FourRoundsIndexed(y, p, 1));
                AssertEqual(ShaVerify.Sha256TwoRounds(y, z, k), AlignedLoads.Sha256TwoRoundsIndexed(y, p, 1, k));

                // Constants and static fields as the memory operand. A RIP-relative operand followed by an
                // immediate must account for the immediate when computing the displacement.
                Vector128<byte> constant = Vector128.Create((byte)0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF);
                AssertEqual(ShaVerify.Sha1MessageSchedule1(y, constant), AlignedLoads.Sha1MessageSchedule1WithConstant(y));
                AssertEqual(ShaVerify.Sha1FourRounds(y, constant, 1), AlignedLoads.Sha1FourRoundsWithConstant(y));
                AssertEqual(ShaVerify.Sha256TwoRounds(y, constant, k), AlignedLoads.Sha256TwoRoundsWithConstant(y, k));
                AssertEqual(ShaVerify.Sha256TwoRounds(y, s_vectorSeed, k), Sha.Sha256TwoRounds(y, s_vectorSeed, k));
            }
        }

        // The lower 128 bits of an aligned 256-bit or 512-bit load as the memory operand. Lowering removes the
        // GetLower calls, so the instruction consumes the wider load directly, and that load may be folded into
        // it since an address aligned for the wider load is also 16-byte aligned.
        [ConditionalFact(typeof(Sha), nameof(Sha.IsSupported))]
        public static void WiderAlignedMemoryOperands()
        {
            Random random = new Random(Seed);
            byte[] buffer = new byte[64 + 64];
            random.NextBytes(buffer);

            fixed (byte* pBuffer = buffer)
            {
                byte* p = (byte*)(((nuint)pBuffer + 63) & ~(nuint)63);

                Vector128<byte> x = Unsafe.ReadUnaligned<Vector128<byte>>(p);
                Vector128<byte> y = NextVector(random);
                Vector128<byte> k = NextVector(random);

                if (Avx.IsSupported)
                {
                    AssertEqual(ShaVerify.Sha1MessageSchedule1(y, x), AlignedVector256Loads.Sha1MessageSchedule1(y, p));
                    AssertEqual(ShaVerify.Sha1MessageSchedule2(y, x), AlignedVector256Loads.Sha1MessageSchedule2(y, p));
                    AssertEqual(ShaVerify.Sha1NextE(y, x), AlignedVector256Loads.Sha1NextE(y, p));
                    AssertEqual(ShaVerify.Sha1FourRounds(y, x, 2), AlignedVector256Loads.Sha1FourRounds(y, p));
                    AssertEqual(ShaVerify.Sha256MessageSchedule1(y, x), AlignedVector256Loads.Sha256MessageSchedule1(y, p));
                    AssertEqual(ShaVerify.Sha256MessageSchedule2(y, x), AlignedVector256Loads.Sha256MessageSchedule2(y, p));
                    AssertEqual(ShaVerify.Sha256TwoRounds(y, x, k), AlignedVector256Loads.Sha256TwoRounds(y, p, k));
                }

                if (Avx512F.IsSupported)
                {
                    AssertEqual(ShaVerify.Sha1MessageSchedule1(y, x), AlignedVector512Loads.Sha1MessageSchedule1(y, p));
                    AssertEqual(ShaVerify.Sha1MessageSchedule2(y, x), AlignedVector512Loads.Sha1MessageSchedule2(y, p));
                    AssertEqual(ShaVerify.Sha1NextE(y, x), AlignedVector512Loads.Sha1NextE(y, p));
                    AssertEqual(ShaVerify.Sha1FourRounds(y, x, 2), AlignedVector512Loads.Sha1FourRounds(y, p));
                    AssertEqual(ShaVerify.Sha256MessageSchedule1(y, x), AlignedVector512Loads.Sha256MessageSchedule1(y, p));
                    AssertEqual(ShaVerify.Sha256MessageSchedule2(y, x), AlignedVector512Loads.Sha256MessageSchedule2(y, p));
                    AssertEqual(ShaVerify.Sha256TwoRounds(y, x, k), AlignedVector512Loads.Sha256TwoRounds(y, p, k));
                }
            }
        }

        // The same local passed as several operands. The register allocator expects such operands to share a
        // register, which doesn't hold when the local lives in an upper SIMD register on AVX-512 hardware and
        // every use has to be copied to one of xmm0-xmm15 (DOTNET_JitStressRegs=0x2000 forces that).
        [ConditionalFact(typeof(Sha), nameof(Sha.IsSupported))]
        public static void AliasedOperands()
        {
            Random random = new Random(Seed);
            byte[] buffer = new byte[32];
            random.NextBytes(buffer);

            fixed (byte* pBuffer = buffer)
            {
                byte* p = (byte*)(((nuint)pBuffer + 15) & ~(nuint)15);

                for (int i = 0; i < Iterations; i++)
                {
                    Vector128<byte> a = NextVector(random);
                    Vector128<byte> b = NextVector(random);
                    Vector128<byte> m = Unsafe.ReadUnaligned<Vector128<byte>>(p);

                    // SHA256RNDS2 reads its third operand from XMM0.
                    AssertEqual(ShaVerify.Sha256TwoRounds(a, b, a), Aliasing.FirstAndThird(a, b));
                    AssertEqual(ShaVerify.Sha256TwoRounds(a, b, b), Aliasing.SecondAndThird(a, b));
                    AssertEqual(ShaVerify.Sha256TwoRounds(a, a, b), Aliasing.FirstAndSecond(a, b));
                    AssertEqual(ShaVerify.Sha256TwoRounds(a, a, a), Aliasing.All(a));
                    AssertEqual(ShaVerify.Sha256TwoRounds(a, m, a), Aliasing.FirstAndThirdWithAlignedLoad(a, p));
                    AssertEqual(ShaVerify.Sha256TwoRounds(ShaVerify.Sha256TwoRounds(a, b, a), b, a), Aliasing.Chained(a, b));

                    // The destructive two-operand forms.
                    AssertEqual(ShaVerify.Sha1MessageSchedule1(a, a), Aliasing.Sha1MessageSchedule1(a));
                    AssertEqual(ShaVerify.Sha1MessageSchedule2(a, a), Aliasing.Sha1MessageSchedule2(a));
                    AssertEqual(ShaVerify.Sha1NextE(a, a), Aliasing.Sha1NextE(a));
                    AssertEqual(ShaVerify.Sha1FourRounds(a, a, 3), Aliasing.Sha1FourRounds(a));
                    AssertEqual(ShaVerify.Sha256MessageSchedule1(a, a), Aliasing.Sha256MessageSchedule1(a));
                    AssertEqual(ShaVerify.Sha256MessageSchedule2(a, a), Aliasing.Sha256MessageSchedule2(a));
                }
            }
        }

        // Some processors run the legacy encoded SHA instructions very slowly while the upper bits of the YMM
        // and ZMM registers are dirty, so the JIT clears them with a vzeroupper before the SHA instructions of a method
        // that uses 256-bit or 512-bit instructions. It must not do that while a 256-bit or 512-bit value is live in a
        // register across them, since that would zero the upper bits of the value. These values are live across SHA
        // instructions in the same block and in a loop, and their upper bits are checked afterwards.
        [ConditionalFact(typeof(Sha), nameof(Sha.IsSupported))]
        public static void WideValuesLiveAcrossShaInstructions()
        {
            Random random = new Random(Seed);
            Vector256<byte>* p256 = stackalloc Vector256<byte>[4];
            Vector512<byte>* p512 = stackalloc Vector512<byte>[4];

            for (int i = 0; i < Iterations; i++)
            {
                Vector128<byte> a = NextVector(random);
                Vector128<byte> b = NextVector(random);
                Vector128<byte> x0 = NextVector(random), x1 = NextVector(random), x2 = NextVector(random), x3 = NextVector(random);
                Vector128<byte> y0 = NextVector(random), y1 = NextVector(random), y2 = NextVector(random), y3 = NextVector(random);
                int count = 1 + (i % 4);

                Vector128<byte> s = ShaVerify.Sha1NextE(ShaVerify.Sha256MessageSchedule2(ShaVerify.Sha256MessageSchedule1(a, b), b), a);
                Vector128<byte> la = a;
                Vector128<byte> lb = b;

                for (int j = 0; j < count; j++)
                {
                    la = ShaVerify.Sha256MessageSchedule1(la, lb);
                    lb = ShaVerify.Sha1NextE(lb, la);
                }

                p256[0] = Vector256.Create(x0, x1);
                p256[1] = Vector256.Create(y0, y1);
                AssertEqual(Vector256.Create((x0 + y0) ^ s, (x1 + y1) ^ s), WideValues.Vector256AcrossSha(p256, a, b));
                AssertEqual(Vector256.Create(x0 + y0, x1 + y1), p256[2]);
                AssertEqual(Vector256.Create((x0 ^ y0) + la, (x1 ^ y1) + lb), WideValues.Vector256AcrossShaInLoop(p256, a, b, count));
                AssertEqual(Vector256.Create(x0 ^ y0, x1 ^ y1), p256[3]);
                AssertEqual(Vector256.Create((x0 - y0) ^ ShaVerify.Sha256MessageSchedule1(a, b), x1 - y1), WideValues.Vector256TempAcrossSha(p256, a, b));

                if (Avx512F.IsSupported)
                {
                    p512[0] = Vector512.Create(Vector256.Create(x0, x1), Vector256.Create(x2, x3));
                    p512[1] = Vector512.Create(Vector256.Create(y0, y1), Vector256.Create(y2, y3));

                    Vector512<byte> expected = Vector512.Create(Vector256.Create((x0 + y0) ^ s, (x1 + y1) ^ s), Vector256.Create((x2 + y2) ^ s, (x3 + y3) ^ s));
                    AssertEqual(expected, WideValues.Vector512AcrossSha(p512, a, b));

                    expected = Vector512.Create(Vector256.Create((x0 ^ y0) + la, (x1 ^ y1) + lb), Vector256.Create((x2 ^ y2) + lb, (x3 ^ y3) + la));
                    AssertEqual(expected, WideValues.Vector512AcrossShaInLoop(p512, a, b, count));
                }
            }
        }

        // The JIT clears the upper state with a vzeroupper before the SHA instructions of these methods, since 256-bit
        // instructions that run before them may leave it dirty: a 32-byte struct copy, a 256-bit operation followed by
        // an inlined helper that uses SHA, a caller that uses 256-bit instructions before it calls a method that only
        // uses SHA (which clears it in its prolog), and a loop that mixes both.
        [ConditionalFact(typeof(Sha), nameof(Sha.IsSupported))]
        public static void ShaAfterWideInstructions()
        {
            Random random = new Random(Seed);
            byte* bytes = stackalloc byte[64];
            Vector256<byte>* p256 = stackalloc Vector256<byte>[6];

            for (int i = 0; i < Iterations; i++)
            {
                Vector128<byte> a = NextVector(random);
                Vector128<byte> b = NextVector(random);
                Vector128<byte> x0 = NextVector(random), x1 = NextVector(random), x2 = NextVector(random), x3 = NextVector(random);

                EightWords source = new EightWords
                {
                    A = (uint)random.Next(), B = (uint)random.Next(), C = (uint)random.Next(), D = (uint)random.Next(),
                    E = (uint)random.Next(), F = (uint)random.Next(), G = (uint)random.Next(), H = (uint)random.Next(),
                };
                EightWords destination = default;
                Vector128<byte> words = Vector128.Create(source.A, source.B, source.C, source.D).AsByte();

                AssertEqual(ShaVerify.Sha256MessageSchedule1(a, words), WideInstructions.StructCopyThenSha(ref source, ref destination, a));
                Assert.Equal(source, destination);

                Vector128.Store(x0, bytes);
                Vector128.Store(x1, bytes + 16);
                Vector128.Store(x2, bytes + 32);
                Vector128.Store(x3, bytes + 48);
                Vector128<byte> one = Vector128<byte>.One;

                AssertEqual(ShaVerify.Sha1NextE(ShaVerify.Sha1MessageSchedule1(a, x2 + one), x2 + one), WideInstructions.InlinedShaAfterVector256(bytes, a));
                AssertEqual(Vector256.Create(x2 + one, x3 + one), Vector256.Load(bytes));

                AssertEqual(ShaVerify.Sha256MessageSchedule1(a, x2 + one + one), WideInstructions.CallShaAfterVector256(bytes, a));
                AssertEqual(Vector256.Create(x2 + one + one, x3 + one + one), Vector256.Load(bytes + 32));

                int count = 1 + (i % 4);
                Vector128<byte>[] lower = new Vector128<byte>[count + 2];
                Vector128<byte>[] upper = new Vector128<byte>[count + 2];
                lower[0] = x0;
                upper[0] = x1;
                lower[1] = x2;
                upper[1] = x3;
                p256[0] = Vector256.Create(x0, x1);
                p256[1] = Vector256.Create(x2, x3);

                Vector128<byte> ea = a;
                Vector128<byte> eb = b;

                for (int j = 0; j < count; j++)
                {
                    lower[j + 2] = lower[j] + lower[j + 1];
                    upper[j + 2] = upper[j] + upper[j + 1];
                    ea = ShaVerify.Sha256TwoRounds(ea, eb, lower[j + 2]);
                    eb = ShaVerify.Sha256MessageSchedule2(eb, ea);
                }

                AssertEqual(ea ^ eb, WideInstructions.Vector256AndShaInLoop(p256, a, b, count));

                for (int j = 0; j < count + 2; j++)
                {
                    AssertEqual(Vector256.Create(lower[j], upper[j]), p256[j]);
                }
            }
        }

        // The same constant wider than 16 bytes is stored before and after a SHA instruction. The register allocator
        // could reuse the register that still holds the first constant for the second one, but a vzeroupper before
        // the SHA instruction zeroes its upper bits, so it must not do that in methods with SHA instructions. Windows
        // x64 never reuses such constants, but other targets do. The constants may also be combined into a local
        // that is live across the SHA instruction, in which case there is no vzeroupper.
        [ConditionalFact(typeof(Sha), nameof(Sha.IsSupported))]
        public static void WideConstantsAcrossShaInstructions()
        {
            Random random = new Random(Seed);
            Vector256<byte>* p256 = stackalloc Vector256<byte>[5];
            Vector256<byte> constant = Vector256.Create((byte)0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20);

            for (int i = 0; i < Iterations; i++)
            {
                Vector128<byte> a = NextVector(random);
                Vector128<byte> b = NextVector(random);
                Vector128<byte> expected = ShaVerify.Sha256MessageSchedule1(a + b, a - b);

                AssertEqual(expected, WideConstants.AllBitsSetAcrossSha(p256, a, b));
                AssertEqual(Vector256<byte>.AllBitsSet, p256[0]);
                AssertEqual(Vector256<byte>.AllBitsSet, p256[1]);

                AssertEqual(expected, WideConstants.ConstantAcrossSha(p256, a, b));
                AssertEqual(constant, p256[0]);
                AssertEqual(constant, p256[1]);
            }
        }

        // "abc" and the 448-bit message are the SHA-1 and SHA-256 examples of FIPS 180, and the 896-bit message is
        // its SHA-384 and SHA-512 example.
        [ConditionalTheory(typeof(Sha), nameof(Sha.IsSupported))]
        [InlineData("abc", "a9993e364706816aba3e25717850c26c9cd0d89d", "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")]
        [InlineData("", "da39a3ee5e6b4b0d3255bfef95601890afd80709", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
        [InlineData("abcdbcdecdefdefgefghfghighijhijkijkljklmklmnlmnomnopnopq", "84983e441c3bd26ebaae4aa1f95129e5e54670f1", "248d6a61d20638b8e5c026930c3e6039a33ce45964ff2167f6ecedd419db06c1")]
        [InlineData("abcdefghbcdefghicdefghijdefghijkefghijklfghijklmghijklmnhijklmnoijklmnopjklmnopqklmnopqrlmnopqrsmnopqrstnopqrstu", "a49b2446a02c645bf419f995b67091253a04a259", "cf5b16a778af8380036ce59e7b0492370b249b11e8f07a51afac45037afee9d1")]
        public static void MatchesKnownDigests(string message, string sha1, string sha256)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(message);

            Assert.Equal(sha1, ToHex(Sha1Intrinsics(bytes, FuncMode.Constant)));
            Assert.Equal(sha1, ToHex(Sha1Intrinsics(bytes, FuncMode.Variable)));
            Assert.Equal(sha1, ToHex(Sha1Intrinsics(bytes, FuncMode.VariableWithUpperBits)));

            Assert.Equal(sha256, ToHex(Sha256Intrinsics(bytes)));
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static Vector128<byte> MessageSchedule1FromField(Vector128<byte> x, ref PackedVector packed) => Sha.Sha1MessageSchedule1(x, packed.Value);

        private static byte[] Sha1Intrinsics(ReadOnlySpan<byte> message, FuncMode mode)
        {
            byte[] padded = Pad(message);
            uint[] state = (uint[])s_sha1InitialState.Clone();

            for (int offset = 0; offset < padded.Length; offset += 64)
            {
                Sha1Compress(state, padded.AsSpan(offset, 64), mode);
            }

            return ToBigEndianBytes(state);
        }

        // The vectors used by the SHA-1 instructions hold the first dword of a group in the highest lane.
        private static void Sha1Compress(uint[] state, ReadOnlySpan<byte> block, FuncMode mode)
        {
            Vector128<uint> abcd = Vector128.Create(state[3], state[2], state[1], state[0]);
            Vector128<uint> abcdSave = abcd;
            Vector128<uint> eSave = Vector128.Create(0, 0, 0, state[4]);

            Vector128<uint> m0 = LoadSha1Words(block);
            Vector128<uint> m1 = LoadSha1Words(block.Slice(16));
            Vector128<uint> m2 = LoadSha1Words(block.Slice(32));
            Vector128<uint> m3 = LoadSha1Words(block.Slice(48));

            Vector128<uint> previous = default;

            for (int group = 0; group < 20; group++)
            {
                Vector128<uint> w;

                if (group < 4)
                {
                    w = group switch { 0 => m0, 1 => m1, 2 => m2, _ => m3 };
                }
                else
                {
                    // W[t..t+3] = SHA1MSG2(SHA1MSG1(W[t-16..t-13], W[t-12..t-9]) ^ W[t-8..t-5], W[t-4..t-1])
                    Vector128<uint> partial = Sha.Sha1MessageSchedule1(m0.AsByte(), m1.AsByte()).AsUInt32() ^ m2;
                    w = Sha.Sha1MessageSchedule2(partial.AsByte(), m3.AsByte()).AsUInt32();

                    m0 = m1;
                    m1 = m2;
                    m2 = m3;
                    m3 = w;
                }

                // E for the first group comes from the state. Afterwards it is derived from A four rounds earlier.
                Vector128<uint> we = (group == 0) ? eSave + w : Sha.Sha1NextE(previous.AsByte(), w.AsByte()).AsUInt32();

                previous = abcd;
                abcd = Sha1FourRounds(abcd, we, group / 5, mode);
            }

            Vector128<uint> e = Sha.Sha1NextE(previous.AsByte(), eSave.AsByte()).AsUInt32();
            abcd += abcdSave;

            state[0] = abcd.GetElement(3);
            state[1] = abcd.GetElement(2);
            state[2] = abcd.GetElement(1);
            state[3] = abcd.GetElement(0);
            state[4] = e.GetElement(3);
        }

        private static Vector128<uint> Sha1FourRounds(Vector128<uint> abcd, Vector128<uint> we, int func, FuncMode mode)
        {
            Vector128<byte> a = abcd.AsByte();
            Vector128<byte> b = we.AsByte();

            Vector128<byte> result = mode switch
            {
                FuncMode.Constant => func switch
                {
                    0 => Sha.Sha1FourRounds(a, b, 0),
                    1 => Sha.Sha1FourRounds(a, b, 1),
                    2 => Sha.Sha1FourRounds(a, b, 2),
                    _ => Sha.Sha1FourRounds(a, b, 3),
                },
                FuncMode.Variable => Sha.Sha1FourRounds(a, b, (byte)func),
                _ => Sha.Sha1FourRounds(a, b, (byte)(func | s_upperFuncBits)),
            };

            return result.AsUInt32();
        }

        private static byte[] Sha256Intrinsics(ReadOnlySpan<byte> message)
        {
            byte[] padded = Pad(message);
            uint[] state = (uint[])s_sha256InitialState.Clone();

            for (int offset = 0; offset < padded.Length; offset += 64)
            {
                Sha256Compress(state, padded.AsSpan(offset, 64));
            }

            return ToBigEndianBytes(state);
        }

        // The vectors used by the SHA-256 instructions hold the first dword of a group in the lowest lane, while
        // the state is split into ABEF and CDGH with A and C in the highest lane.
        private static void Sha256Compress(uint[] state, ReadOnlySpan<byte> block)
        {
            Vector128<uint> abef = Vector128.Create(state[5], state[4], state[1], state[0]);
            Vector128<uint> cdgh = Vector128.Create(state[7], state[6], state[3], state[2]);
            Vector128<uint> abefSave = abef;
            Vector128<uint> cdghSave = cdgh;

            Vector128<uint> m0 = LoadSha256Words(block);
            Vector128<uint> m1 = LoadSha256Words(block.Slice(16));
            Vector128<uint> m2 = LoadSha256Words(block.Slice(32));
            Vector128<uint> m3 = LoadSha256Words(block.Slice(48));

            for (int group = 0; group < 16; group++)
            {
                Vector128<uint> w;

                if (group < 4)
                {
                    w = group switch { 0 => m0, 1 => m1, 2 => m2, _ => m3 };
                }
                else
                {
                    // W[t..t+3] = SHA256MSG2(SHA256MSG1(W[t-16..t-13], W[t-12..t-9]) + W[t-7..t-4], W[t-4..t-1])
                    Vector128<uint> wMinus7 = Ssse3.AlignRight(m3.AsByte(), m2.AsByte(), 4).AsUInt32();
                    Vector128<uint> partial = Sha.Sha256MessageSchedule1(m0.AsByte(), m1.AsByte()).AsUInt32() + wMinus7;
                    w = Sha.Sha256MessageSchedule2(partial.AsByte(), m3.AsByte()).AsUInt32();

                    m0 = m1;
                    m1 = m2;
                    m2 = m3;
                    m3 = w;
                }

                Vector128<uint> wk = w + Vector128.Create(s_sha256RoundConstants, 4 * group);

                // Each SHA256RNDS2 consumes the low two dwords of the third operand and produces the new ABEF,
                // while the old ABEF becomes the new CDGH.
                cdgh = Sha.Sha256TwoRounds(cdgh.AsByte(), abef.AsByte(), wk.AsByte()).AsUInt32();
                abef = Sha.Sha256TwoRounds(abef.AsByte(), cdgh.AsByte(), Sse2.Shuffle(wk, 0x0E).AsByte()).AsUInt32();
            }

            abef += abefSave;
            cdgh += cdghSave;

            state[0] = abef.GetElement(3);
            state[1] = abef.GetElement(2);
            state[2] = cdgh.GetElement(3);
            state[3] = cdgh.GetElement(2);
            state[4] = abef.GetElement(1);
            state[5] = abef.GetElement(0);
            state[6] = cdgh.GetElement(1);
            state[7] = cdgh.GetElement(0);
        }

        private static Vector128<uint> LoadSha1Words(ReadOnlySpan<byte> bytes)
        {
            return Vector128.Create(ReadWord(bytes, 3), ReadWord(bytes, 2), ReadWord(bytes, 1), ReadWord(bytes, 0));
        }

        private static Vector128<uint> LoadSha256Words(ReadOnlySpan<byte> bytes)
        {
            return Vector128.Create(ReadWord(bytes, 0), ReadWord(bytes, 1), ReadWord(bytes, 2), ReadWord(bytes, 3));
        }

        private static uint ReadWord(ReadOnlySpan<byte> bytes, int index) => BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(4 * index));

        // Appends the 0x80 terminator, zero padding and the big-endian bit length, as defined by FIPS 180-4.
        private static byte[] Pad(ReadOnlySpan<byte> message)
        {
            int paddedLength = (message.Length + 1 + 8 + 63) / 64 * 64;
            byte[] padded = new byte[paddedLength];

            message.CopyTo(padded);
            padded[message.Length] = 0x80;
            BinaryPrimitives.WriteUInt64BigEndian(padded.AsSpan(paddedLength - 8), (ulong)message.Length * 8);

            return padded;
        }

        private static byte[] ToBigEndianBytes(uint[] state)
        {
            byte[] bytes = new byte[4 * state.Length];

            for (int i = 0; i < state.Length; i++)
            {
                BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(4 * i), state[i]);
            }

            return bytes;
        }

        private static string ToHex(byte[] bytes) => Convert.ToHexStringLower(bytes);

        private static Vector128<byte> NextVector(Random random)
        {
            Span<byte> bytes = stackalloc byte[Vector128<byte>.Count];
            random.NextBytes(bytes);
            return Vector128.Create(bytes);
        }

        private static void AssertEqual(Vector128<byte> expected, Vector128<byte> actual, [CallerLineNumber] int line = 0)
        {
            if (expected != actual)
            {
                Assert.Fail($"Line {line}: expected {expected} but got {actual}");
            }
        }

        private static void AssertEqual(Vector256<byte> expected, Vector256<byte> actual, [CallerLineNumber] int line = 0)
        {
            if (expected != actual)
            {
                Assert.Fail($"Line {line}: expected {expected} but got {actual}");
            }
        }

        private static void AssertEqual(Vector512<byte> expected, Vector512<byte> actual, [CallerLineNumber] int line = 0)
        {
            if (expected != actual)
            {
                Assert.Fail($"Line {line}: expected {expected} but got {actual}");
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct PackedVector
        {
            public byte Tag;
            public Vector128<byte> Value;
        }

        // A 32-byte struct without GC references, which the JIT copies with a YMM register when AVX is available.
        private struct EightWords
        {
            public uint A, B, C, D, E, F, G, H;
        }

        private static class AlignedLoads
        {
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha1MessageSchedule1(Vector128<byte> a, byte* p) => Sha.Sha1MessageSchedule1(a, Sse2.LoadAlignedVector128(p));

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha1MessageSchedule2(Vector128<byte> a, byte* p) => Sha.Sha1MessageSchedule2(a, Sse2.LoadAlignedVector128(p));

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha1NextE(Vector128<byte> a, byte* p) => Sha.Sha1NextE(a, Sse2.LoadAlignedVector128(p));

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha1FourRounds(Vector128<byte> a, byte* p) => Sha.Sha1FourRounds(a, Sse2.LoadAlignedVector128(p), 2);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha256MessageSchedule1(Vector128<byte> a, byte* p) => Sha.Sha256MessageSchedule1(a, Sse2.LoadAlignedVector128(p));

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha256MessageSchedule2(Vector128<byte> a, byte* p) => Sha.Sha256MessageSchedule2(a, Sse2.LoadAlignedVector128(p));

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha256TwoRounds(Vector128<byte> a, byte* p, Vector128<byte> k) => Sha.Sha256TwoRounds(a, Sse2.LoadAlignedVector128(p), k);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha1MessageSchedule2Indexed(Vector128<byte> a, byte* p, nint index) => Sha.Sha1MessageSchedule2(a, Sse2.LoadAlignedVector128(p + (index * 16)));

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha1FourRoundsIndexed(Vector128<byte> a, byte* p, nint index) => Sha.Sha1FourRounds(a, Sse2.LoadAlignedVector128(p + (index * 16) + 16), 3);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha256TwoRoundsIndexed(Vector128<byte> a, byte* p, nint index, Vector128<byte> k) => Sha.Sha256TwoRounds(a, Sse2.LoadAlignedVector128(p + (index * 16) + 16), k);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha1MessageSchedule1WithConstant(Vector128<byte> a)
            {
                return Sha.Sha1MessageSchedule1(a, Vector128.Create((byte)0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF));
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha1FourRoundsWithConstant(Vector128<byte> a)
            {
                return Sha.Sha1FourRounds(a, Vector128.Create((byte)0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF), 1);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha256TwoRoundsWithConstant(Vector128<byte> a, Vector128<byte> k)
            {
                return Sha.Sha256TwoRounds(a, Vector128.Create((byte)0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF), k);
            }
        }

        private static class AlignedVector256Loads
        {
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha1MessageSchedule1(Vector128<byte> a, byte* p) => Sha.Sha1MessageSchedule1(a, Avx.LoadAlignedVector256(p).GetLower());

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha1MessageSchedule2(Vector128<byte> a, byte* p) => Sha.Sha1MessageSchedule2(a, Avx.LoadAlignedVector256(p).GetLower());

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha1NextE(Vector128<byte> a, byte* p) => Sha.Sha1NextE(a, Avx.LoadAlignedVector256(p).GetLower());

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha1FourRounds(Vector128<byte> a, byte* p) => Sha.Sha1FourRounds(a, Avx.LoadAlignedVector256(p).GetLower(), 2);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha256MessageSchedule1(Vector128<byte> a, byte* p) => Sha.Sha256MessageSchedule1(a, Avx.LoadAlignedVector256(p).GetLower());

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha256MessageSchedule2(Vector128<byte> a, byte* p) => Sha.Sha256MessageSchedule2(a, Avx.LoadAlignedVector256(p).GetLower());

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha256TwoRounds(Vector128<byte> a, byte* p, Vector128<byte> k) => Sha.Sha256TwoRounds(a, Avx.LoadAlignedVector256(p).GetLower(), k);
        }

        private static class AlignedVector512Loads
        {
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha1MessageSchedule1(Vector128<byte> a, byte* p) => Sha.Sha1MessageSchedule1(a, Avx512F.LoadAlignedVector512(p).GetLower().GetLower());

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha1MessageSchedule2(Vector128<byte> a, byte* p) => Sha.Sha1MessageSchedule2(a, Avx512F.LoadAlignedVector512(p).GetLower().GetLower());

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha1NextE(Vector128<byte> a, byte* p) => Sha.Sha1NextE(a, Avx512F.LoadAlignedVector512(p).GetLower().GetLower());

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha1FourRounds(Vector128<byte> a, byte* p) => Sha.Sha1FourRounds(a, Avx512F.LoadAlignedVector512(p).GetLower().GetLower(), 2);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha256MessageSchedule1(Vector128<byte> a, byte* p) => Sha.Sha256MessageSchedule1(a, Avx512F.LoadAlignedVector512(p).GetLower().GetLower());

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha256MessageSchedule2(Vector128<byte> a, byte* p) => Sha.Sha256MessageSchedule2(a, Avx512F.LoadAlignedVector512(p).GetLower().GetLower());

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha256TwoRounds(Vector128<byte> a, byte* p, Vector128<byte> k) => Sha.Sha256TwoRounds(a, Avx512F.LoadAlignedVector512(p).GetLower().GetLower(), k);
        }

        private static class Aliasing
        {
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> FirstAndThird(Vector128<byte> a, Vector128<byte> b) => Sha.Sha256TwoRounds(a, b, a);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> SecondAndThird(Vector128<byte> a, Vector128<byte> b) => Sha.Sha256TwoRounds(a, b, b);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> FirstAndSecond(Vector128<byte> a, Vector128<byte> b) => Sha.Sha256TwoRounds(a, a, b);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> All(Vector128<byte> a) => Sha.Sha256TwoRounds(a, a, a);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> FirstAndThirdWithAlignedLoad(Vector128<byte> a, byte* p) => Sha.Sha256TwoRounds(a, Sse2.LoadAlignedVector128(p), a);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Chained(Vector128<byte> a, Vector128<byte> b) => Sha.Sha256TwoRounds(Sha.Sha256TwoRounds(a, b, a), b, a);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha1MessageSchedule1(Vector128<byte> a) => Sha.Sha1MessageSchedule1(a, a);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha1MessageSchedule2(Vector128<byte> a) => Sha.Sha1MessageSchedule2(a, a);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha1NextE(Vector128<byte> a) => Sha.Sha1NextE(a, a);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha1FourRounds(Vector128<byte> a) => Sha.Sha1FourRounds(a, a, 3);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha256MessageSchedule1(Vector128<byte> a) => Sha.Sha256MessageSchedule1(a, a);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Sha256MessageSchedule2(Vector128<byte> a) => Sha.Sha256MessageSchedule2(a, a);
        }

        // 256-bit and 512-bit values that are live in a register across SHA instructions, so the JIT must not clear
        // the upper state before them.
        private static class WideValues
        {
            // A 256-bit local that is live across SHA instructions in the same block.
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector256<byte> Vector256AcrossSha(Vector256<byte>* p, Vector128<byte> a, Vector128<byte> b)
            {
                Vector256<byte> wide = p[0] + p[1];
                p[2] = wide;
                Vector128<byte> s = Sha.Sha1NextE(Sha.Sha256MessageSchedule2(Sha.Sha256MessageSchedule1(a, b), b), a);
                return wide ^ Vector256.Create(s, s);
            }

            // A 256-bit local that is live across SHA instructions in a loop.
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector256<byte> Vector256AcrossShaInLoop(Vector256<byte>* p, Vector128<byte> a, Vector128<byte> b, int count)
            {
                Vector256<byte> wide = p[0] ^ p[1];

                for (int i = 0; i < count; i++)
                {
                    a = Sha.Sha256MessageSchedule1(a, b);
                    b = Sha.Sha1NextE(b, a);
                    p[3] = wide;
                }

                return wide + Vector256.Create(a, b);
            }

            // A 256-bit temp, rather than a local, that is live across a SHA instruction.
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector256<byte> Vector256TempAcrossSha(Vector256<byte>* p, Vector128<byte> a, Vector128<byte> b)
            {
                return Xor(p[0] - p[1], Sha.Sha256MessageSchedule1(a, b));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector256<byte> Xor(Vector256<byte> x, Vector128<byte> y) => x ^ y.ToVector256();

            // A 512-bit local that is live across SHA instructions in the same block.
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector512<byte> Vector512AcrossSha(Vector512<byte>* p, Vector128<byte> a, Vector128<byte> b)
            {
                Vector512<byte> wide = p[0] + p[1];
                p[2] = wide;
                Vector128<byte> s = Sha.Sha1NextE(Sha.Sha256MessageSchedule2(Sha.Sha256MessageSchedule1(a, b), b), a);
                return wide ^ Vector512.Create(Vector256.Create(s, s), Vector256.Create(s, s));
            }

            // A 512-bit local that is live across SHA instructions in a loop.
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector512<byte> Vector512AcrossShaInLoop(Vector512<byte>* p, Vector128<byte> a, Vector128<byte> b, int count)
            {
                Vector512<byte> wide = p[0] ^ p[1];

                for (int i = 0; i < count; i++)
                {
                    a = Sha.Sha256MessageSchedule1(a, b);
                    b = Sha.Sha1NextE(b, a);
                    p[3] = wide;
                }

                return wide + Vector512.Create(Vector256.Create(a, b), Vector256.Create(b, a));
            }
        }

        // SHA instructions that run after 256-bit instructions whose results are no longer live.
        private static class WideInstructions
        {
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> StructCopyThenSha(ref EightWords source, ref EightWords destination, Vector128<byte> a)
            {
                destination = source;
                return Sha.Sha256MessageSchedule1(a, Vector128.Create(destination.A, destination.B, destination.C, destination.D).AsByte());
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> InlinedShaAfterVector256(byte* p, Vector128<byte> a)
            {
                Vector256.Store(Vector256.Load(p + 32) + Vector256<byte>.One, p);
                return Sha1Helper(a, Vector128.Load(p));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Vector128<byte> Sha1Helper(Vector128<byte> a, Vector128<byte> b) => Sha.Sha1NextE(Sha.Sha1MessageSchedule1(a, b), b);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> CallShaAfterVector256(byte* p, Vector128<byte> a)
            {
                Vector256.Store(Vector256.Load(p) + Vector256.Create((byte)1), p + 32);
                return ShaOnly(a, Vector128.Load(p + 32));
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static Vector128<byte> ShaOnly(Vector128<byte> a, Vector128<byte> b) => Sha.Sha256MessageSchedule1(a, b);

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> Vector256AndShaInLoop(Vector256<byte>* p, Vector128<byte> a, Vector128<byte> b, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    p[i + 2] = p[i] + p[i + 1];
                    a = Sha.Sha256TwoRounds(a, b, Vector128.Load((byte*)(p + i + 2)));
                    b = Sha.Sha256MessageSchedule2(b, a);
                }

                return a ^ b;
            }
        }

        // The same 256-bit constant stored before and after a SHA instruction whose operands are already in registers.
        private static class WideConstants
        {
            // AllBitsSet is created in a register rather than loaded from memory.
            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> AllBitsSetAcrossSha(Vector256<byte>* p, Vector128<byte> a, Vector128<byte> b)
            {
                Vector128<byte> x = a + b;
                Vector128<byte> y = a - b;
                Vector128.Store(x, (byte*)(p + 3));
                Vector128.Store(y, (byte*)(p + 4));
                p[0] = Vector256<byte>.AllBitsSet;
                Vector128.Store(Sha.Sha256MessageSchedule1(x, y), (byte*)(p + 2));
                p[1] = Vector256<byte>.AllBitsSet;
                return Vector128.Load((byte*)(p + 2));
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            public static Vector128<byte> ConstantAcrossSha(Vector256<byte>* p, Vector128<byte> a, Vector128<byte> b)
            {
                Vector128<byte> x = a + b;
                Vector128<byte> y = a - b;
                Vector128.Store(x, (byte*)(p + 3));
                Vector128.Store(y, (byte*)(p + 4));
                p[0] = Vector256.Create((byte)0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20);
                Vector128.Store(Sha.Sha256MessageSchedule1(x, y), (byte*)(p + 2));
                p[1] = Vector256.Create((byte)0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20);
                return Vector128.Load((byte*)(p + 2));
            }
        }
    }
}
