// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

unsafe class Program
{
    static int Main()
    {
        s_success = true;

#if !DEBUG
        Console.WriteLine("****************************************************");
        Console.WriteLine("* Size test                                        *");
        long fileSize = new System.IO.FileInfo(Environment.ProcessPath).Length;
        Console.WriteLine($"* Size of the executable is {fileSize / 1024,7:n0} kB             *");
        Console.WriteLine("****************************************************");

        long lowerBound, upperBound;
        lowerBound = 1200 * 1024; // ~1.2 MB
        upperBound = 1600 * 1024; // ~1.6 MB

        if (fileSize < lowerBound || fileSize > upperBound)
        {
            Console.WriteLine($"BUG: File size is not in the expected range ({lowerBound} to {upperBound} bytes). Did a libraries change regress size of Hello World?");
            return 1;
        }

        Console.WriteLine();
#endif

        // We expect the AOT compiler generated HW intrinsics with the following characteristics:
        //
        // * TRUE = IsSupported assumed to be true, no runtime check
        // * NULL = IsSupported is a runtime check, code should be behind the check or bad things happen
        // * FALSE = IsSupported assumed to be false, no runtime check, PlatformNotSupportedException if used
        //
        // The test is compiled with multiple defines to test this.

        bool ExpectedVectorsAccelerated = true;

        bool? ExpectedX86Base = true;
        bool? ExpectedSse = true;
        bool? ExpectedSse2 = true;

#if BASELINE_INTRINSICS
        bool? ExpectedSse3 = null;
        bool? ExpectedSsse3 = null;
        bool? ExpectedSse41 = null;
        bool? ExpectedSse42 = null;
        bool? ExpectedPopcnt = null;
        bool? ExpectedAes = null;
        bool? ExpectedPclmulqdq = null;
        bool? ExpectedGfni = null;
        bool? ExpectedSha = null;
        bool? ExpectedWaitPkg = null;
        bool? ExpectedX86Serialize = null;

        bool? ExpectedAvx = false;
        bool? ExpectedAvx2 = false;
        bool? ExpectedBmi1 = false;
        bool? ExpectedBmi2 = false;
        bool? ExpectedF16c = false;
        bool? ExpectedFma = false;
        bool? ExpectedLzcnt = false;
        bool? ExpectedAvx512F = false;
        bool? ExpectedAvx512BW = false;
        bool? ExpectedAvx512CD = false;
        bool? ExpectedAvx512DQ = false;
        bool? ExpectedAvx512Vbmi = false;
        bool? ExpectedAvx512Bitalg = false;
        bool? ExpectedAvx512Vbmi2 = false;
        bool? ExpectedAvx512Vpopcntdq = false;
        bool? ExpectedAvx512Bf16 = false;
        bool? ExpectedAvx512Fp16 = false;
        bool? ExpectedAvx10v1 = false;
        bool? ExpectedAvx10v1V512 = false;
        bool? ExpectedAvx512Vp2intersect = false;
        bool? ExpectedAvxIfma = false;
        bool? ExpectedAvxVnni = false;
        bool? ExpectedGfniV256 = false;
        bool? ExpectedGfniV512 = false;
        bool? ExpectedAesV256 = false;
        bool? ExpectedAesV512 = false;
        bool? ExpectedPclmulqdqV256 = false;
        bool? ExpectedPclmulqdqV512 = false;
#elif SSE42_INTRINSICS
        bool? ExpectedSse3 = true;
        bool? ExpectedSsse3 = true;
        bool? ExpectedSse41 = true;
        bool? ExpectedSse42 = true;
        bool? ExpectedPopcnt = true;

        bool? ExpectedAes = null;
        bool? ExpectedPclmulqdq = null;
        bool? ExpectedGfni = null;
        bool? ExpectedSha = null;
        bool? ExpectedWaitPkg = null;
        bool? ExpectedX86Serialize = null;

        bool? ExpectedAvx = false;
        bool? ExpectedAvx2 = false;
        bool? ExpectedBmi1 = false;
        bool? ExpectedBmi2 = false;
        bool? ExpectedF16c = false;
        bool? ExpectedFma = false;
        bool? ExpectedLzcnt = false;
        bool? ExpectedAvx512F = false;
        bool? ExpectedAvx512BW = false;
        bool? ExpectedAvx512CD = false;
        bool? ExpectedAvx512DQ = false;
        bool? ExpectedAvx512Vbmi = false;
        bool? ExpectedAvx512Bitalg = false;
        bool? ExpectedAvx512Vbmi2 = false;
        bool? ExpectedAvx512Vpopcntdq = false;
        bool? ExpectedAvx512Bf16 = false;
        bool? ExpectedAvx512Fp16 = false;
        bool? ExpectedAvx10v1 = false;
        bool? ExpectedAvx10v1V512 = false;
        bool? ExpectedAvx512Vp2intersect = false;
        bool? ExpectedAvxIfma = false;
        bool? ExpectedAvxVnni = false;
        bool? ExpectedGfniV256 = false;
        bool? ExpectedGfniV512 = false;
        bool? ExpectedAesV256 = false;
        bool? ExpectedAesV512 = false;
        bool? ExpectedPclmulqdqV256 = false;
        bool? ExpectedPclmulqdqV512 = false;
#elif AVX_INTRINSICS
        bool? ExpectedSse3 = true;
        bool? ExpectedSsse3 = true;
        bool? ExpectedSse41 = true;
        bool? ExpectedSse42 = true;
        bool? ExpectedPopcnt = true;
        bool? ExpectedAvx = true;

        bool? ExpectedAvx2 = null;
        bool? ExpectedBmi1 = null;
        bool? ExpectedBmi2 = null;
        bool? ExpectedF16c = null;
        bool? ExpectedFma = null;
        bool? ExpectedLzcnt = null;
        bool? ExpectedAes = null;
        bool? ExpectedPclmulqdq = null;
        bool? ExpectedAvxIfma = null;
        bool? ExpectedAvxVnni = null;
        bool? ExpectedGfni = null;
        bool? ExpectedGfniV256 = null;
        bool? ExpectedSha = null;
        bool? ExpectedAesV256 = null;
        bool? ExpectedPclmulqdqV256 = null;
        bool? ExpectedWaitPkg = null;
        bool? ExpectedX86Serialize = null;

        bool? ExpectedAvx512F = false;
        bool? ExpectedAvx512BW = false;
        bool? ExpectedAvx512CD = false;
        bool? ExpectedAvx512DQ = false;
        bool? ExpectedAvx512Vbmi = false;
        bool? ExpectedAvx512Bitalg = false;
        bool? ExpectedAvx512Vbmi2 = false;
        bool? ExpectedAvx512Vpopcntdq = false;
        bool? ExpectedAvx512Bf16 = false;
        bool? ExpectedAvx512Fp16 = false;
        bool? ExpectedAvx10v1 = false;
        bool? ExpectedAvx10v1V512 = false;
        bool? ExpectedAvx512Vp2intersect = false;
        bool? ExpectedGfniV512 = false;
        bool? ExpectedAesV512 = false;
        bool? ExpectedPclmulqdqV512 = false;
#elif AVX_INTRINSICS_NO_AVX2
        bool? ExpectedSse3 = true;
        bool? ExpectedSsse3 = true;
        bool? ExpectedSse41 = true;
        bool? ExpectedSse42 = true;
        bool? ExpectedPopcnt = true;
        bool? ExpectedAvx = true;

        bool? ExpectedAes = null;
        bool? ExpectedPclmulqdq = null;
        bool? ExpectedGfni = null;
        bool? ExpectedGfniV256 = null;
        bool? ExpectedSha = null;
        bool? ExpectedAesV256 = null;
        bool? ExpectedPclmulqdqV256 = null;
        bool? ExpectedWaitPkg = null;
        bool? ExpectedX86Serialize = null;

        bool? ExpectedAvx2 = false;
        bool? ExpectedBmi1 = false;
        bool? ExpectedBmi2 = false;
        bool? ExpectedF16c = false;
        bool? ExpectedFma = false;
        bool? ExpectedLzcnt = false;
        bool? ExpectedAvx512F = false;
        bool? ExpectedAvx512BW = false;
        bool? ExpectedAvx512CD = false;
        bool? ExpectedAvx512DQ = false;
        bool? ExpectedAvx512Vbmi = false;
        bool? ExpectedAvx512Bitalg = false;
        bool? ExpectedAvx512Vbmi2 = false;
        bool? ExpectedAvx512Vpopcntdq = false;
        bool? ExpectedAvx512Bf16 = false;
        bool? ExpectedAvx512Fp16 = false;
        bool? ExpectedAvx10v1 = false;
        bool? ExpectedAvx10v1V512 = false;
        bool? ExpectedAvx512Vp2intersect = false;
        bool? ExpectedAvxIfma = false;
        bool? ExpectedAvxVnni = false;
        bool? ExpectedGfniV512 = false;
        bool? ExpectedAesV512 = false;
        bool? ExpectedPclmulqdqV512 = false;
#elif AVX2_INTRINSICS
        bool? ExpectedSse3 = true;
        bool? ExpectedSsse3 = true;
        bool? ExpectedSse41 = true;
        bool? ExpectedSse42 = true;
        bool? ExpectedPopcnt = true;
        bool? ExpectedAvx = true;
        bool? ExpectedAvx2 = true;
        bool? ExpectedBmi1 = true;
        bool? ExpectedBmi2 = true;
        bool? ExpectedF16c = true;
        bool? ExpectedFma = true;
        bool? ExpectedLzcnt = true;

        bool? ExpectedAes = null;
        bool? ExpectedPclmulqdq = null;
        bool? ExpectedAvxIfma = null;
        bool? ExpectedAvxVnni = null;
        bool? ExpectedGfni = null;
        bool? ExpectedGfniV256 = null;
        bool? ExpectedSha = null;
        bool? ExpectedAesV256 = null;
        bool? ExpectedPclmulqdqV256 = null;
        bool? ExpectedWaitPkg = null;
        bool? ExpectedX86Serialize = null;

        bool? ExpectedAvx512F = false;
        bool? ExpectedAvx512BW = false;
        bool? ExpectedAvx512CD = false;
        bool? ExpectedAvx512DQ = false;
        bool? ExpectedAvx512Vbmi = false;
        bool? ExpectedAvx512Bitalg = false;
        bool? ExpectedAvx512Vbmi2 = false;
        bool? ExpectedAvx512Vpopcntdq = false;
        bool? ExpectedAvx512Bf16 = false;
        bool? ExpectedAvx512Fp16 = false;
        bool? ExpectedAvx10v1 = false;
        bool? ExpectedAvx10v1V512 = false;
        bool? ExpectedAvx512Vp2intersect = false;
        bool? ExpectedGfniV512 = false;
        bool? ExpectedAesV512 = false;
        bool? ExpectedPclmulqdqV512 = false;
#elif AVX512_INTRINSICS
        bool? ExpectedSse3 = true;
        bool? ExpectedSsse3 = true;
        bool? ExpectedSse41 = true;
        bool? ExpectedSse42 = true;
        bool? ExpectedPopcnt = true;
        bool? ExpectedAvx = true;
        bool? ExpectedAvx2 = true;
        bool? ExpectedBmi1 = true;
        bool? ExpectedBmi2 = true;
        bool? ExpectedF16c = true;
        bool? ExpectedFma = true;
        bool? ExpectedLzcnt = true;
        bool? ExpectedAvx512F = true;
        bool? ExpectedAvx512BW = true;
        bool? ExpectedAvx512CD = true;
        bool? ExpectedAvx512DQ = true;

        bool? ExpectedAvx512Vbmi = null;
        bool? ExpectedAvx512Bitalg = null;
        bool? ExpectedAvx512Vbmi2 = null;
        bool? ExpectedAvx512Vpopcntdq = null;
        bool? ExpectedAvx512Bf16 = null;
        bool? ExpectedAvx512Fp16 = null;
        bool? ExpectedAvx10v1 = null;
        bool? ExpectedAvx10v1V512 = null;
        bool? ExpectedAes = null;
        bool? ExpectedPclmulqdq = null;
        bool? ExpectedAvx512Vp2intersect = null;
        bool? ExpectedAvxIfma = null;
        bool? ExpectedAvxVnni = null;
        bool? ExpectedGfni = null;
        bool? ExpectedGfniV256 = null;
        bool? ExpectedGfniV512 = null;
        bool? ExpectedSha = null;
        bool? ExpectedAesV256 = null;
        bool? ExpectedAesV512 = null;
        bool? ExpectedPclmulqdqV256 = null;
        bool? ExpectedPclmulqdqV512 = null;
        bool? ExpectedWaitPkg = null;
        bool? ExpectedX86Serialize = null;
#else
#error Who dis?
#endif

#if VECTORT128_INTRINSICS
        int byteVectorLength = 16;
#elif VECTORT256_INTRINSICS
        int byteVectorLength = 32;
#elif VECTORT512_INTRINSICS
        int byteVectorLength = 64;
#else
#error Who dis?
#endif

        if (ExpectedVectorsAccelerated != Vector.IsHardwareAccelerated)
        {
            throw new Exception($"Vectors HW acceleration state unexpected - expected {ExpectedVectorsAccelerated}, got {Vector.IsHardwareAccelerated}");
        }

        if (byteVectorLength != Vector<byte>.Count)
        {
            throw new Exception($"Unexpected vector length - expected {byteVectorLength}, got {Vector<byte>.Count}");
        }

        Check("X86Base", ExpectedX86Base, &X86BaseIsSupported, X86Base.IsSupported, () => { X86Base.Pause(); return true; });
        Check("X86Base.X64", ExpectedX86Base, &X86BaseX64IsSupported, X86Base.IsSupported, null);

        Check("Sse", ExpectedSse, &SseIsSupported, Sse.IsSupported, () => Sse.Subtract(Vector128<float>.Zero, Vector128<float>.Zero).Equals(Vector128<float>.Zero));
        Check("Sse.X64", ExpectedSse, &SseX64IsSupported, Sse.X64.IsSupported, () => Sse.X64.ConvertToInt64WithTruncation(Vector128<float>.Zero) == 0);

        Check("Sse2", ExpectedSse2, &Sse2IsSupported, Sse2.IsSupported, () => Sse2.Extract(Vector128<ushort>.Zero, 0) == 0);
        Check("Sse2.X64", ExpectedSse2, &Sse2X64IsSupported, Sse2.X64.IsSupported, () => Sse2.X64.ConvertToInt64(Vector128<double>.Zero) == 0);

        Check("Sse3", ExpectedSse3, &Sse3IsSupported, Sse3.IsSupported, () => Sse3.MoveHighAndDuplicate(Vector128<float>.Zero).Equals(Vector128<float>.Zero));
        Check("Sse3.X64", ExpectedSse3, &Sse3X64IsSupported, Sse3.X64.IsSupported, null);

        Check("Ssse3", ExpectedSsse3, &Ssse3IsSupported, Ssse3.IsSupported, () => Ssse3.Abs(Vector128<short>.Zero).Equals(Vector128<ushort>.Zero));
        Check("Ssse3.X64", ExpectedSsse3, &Ssse3X64IsSupported, Ssse3.X64.IsSupported, null);

        Check("Sse41", ExpectedSse41, &Sse41IsSupported, Sse41.IsSupported, () => Sse41.Max(Vector128<int>.Zero, Vector128<int>.Zero).Equals(Vector128<int>.Zero));
        Check("Sse41.X64", ExpectedSse41, &Sse41X64IsSupported, Sse41.X64.IsSupported, () => Sse41.X64.Extract(Vector128<long>.Zero, 0) == 0);

        Check("Sse42", ExpectedSse42, &Sse42IsSupported, Sse42.IsSupported, () => Sse42.Crc32(0, 0) == 0);
        Check("Sse42.X64", ExpectedSse42, &Sse42X64IsSupported, Sse42.X64.IsSupported, () => Sse42.X64.Crc32(0, 0) == 0);

        Check("Popcnt", ExpectedPopcnt, &PopcntIsSupported, Popcnt.IsSupported, () => Popcnt.PopCount(0) == 0);
        Check("Popcnt.X64", ExpectedPopcnt, &PopcntX64IsSupported, Popcnt.X64.IsSupported, () => Popcnt.X64.PopCount(0) == 0);

        Check("Avx", ExpectedAvx, &AvxIsSupported, Avx.IsSupported, () => Avx.Add(Vector256<double>.Zero, Vector256<double>.Zero).Equals(Vector256<double>.Zero));
        Check("Avx.X64", ExpectedAvx, &AvxX64IsSupported, Avx.X64.IsSupported, null);

        Check("Avx2", ExpectedAvx2, &Avx2IsSupported, Avx2.IsSupported, () => Avx2.Abs(Vector256<int>.Zero).Equals(Vector256<uint>.Zero));
        Check("Avx2.X64", ExpectedAvx2, &Avx2X64IsSupported, Avx2.X64.IsSupported, null);

        Check("Bmi1", ExpectedBmi1, &Bmi1IsSupported, Bmi1.IsSupported, () => Bmi1.AndNot(0, 0) == 0);
        Check("Bmi1.X64", ExpectedBmi1, &Bmi1X64IsSupported, Bmi1.X64.IsSupported, () => Bmi1.X64.AndNot(0, 0) == 0);

        Check("Bmi2", ExpectedBmi2, &Bmi2IsSupported, Bmi2.IsSupported, () => Bmi2.MultiplyNoFlags(0, 0) == 0);
        Check("Bmi2.X64", ExpectedBmi2, &Bmi2X64IsSupported, Bmi2.X64.IsSupported, () => Bmi2.X64.MultiplyNoFlags(0, 0) == 0);

        // Check("F16c", ExpectedF16c, &F16cIsSupported, F16c.IsSupported, null);
        // Check("F16c.X64", ExpectedF16c, &F16cX64IsSupported, F16c.X64.IsSupported, null);

        Check("Fma", ExpectedFma, &FmaIsSupported, Fma.IsSupported, () => Fma.MultiplyAdd(Vector128<float>.Zero, Vector128<float>.Zero, Vector128<float>.Zero).Equals(Vector128<float>.Zero));
        Check("Fma.X64", ExpectedFma, &FmaX64IsSupported, Fma.X64.IsSupported, null);

        Check("Lzcnt", ExpectedLzcnt, &LzcntIsSupported, Lzcnt.IsSupported, () => Lzcnt.LeadingZeroCount(0) == 32);
        Check("Lzcnt.X64", ExpectedLzcnt, &LzcntX64IsSupported, Lzcnt.X64.IsSupported, () => Lzcnt.X64.LeadingZeroCount(0) == 64);

        Check("Avx512F", ExpectedAvx512F, &Avx512FIsSupported, Avx512F.IsSupported, () => Avx512F.Abs(Vector512<int>.Zero).Equals(Vector512<uint>.Zero));
        Check("Avx512F.VL", ExpectedAvx512F, &Avx512FVLIsSupported, Avx512F.VL.IsSupported, null);
        Check("Avx512F.X64", ExpectedAvx512F, &Avx512FX64IsSupported, Avx512F.X64.IsSupported, null);

        Check("Avx512BW", ExpectedAvx512BW, &Avx512BWIsSupported, Avx512BW.IsSupported, () => Avx512BW.Abs(Vector512<sbyte>.Zero).Equals(Vector512<byte>.Zero));
        Check("Avx512BW.VL", ExpectedAvx512BW, &Avx512BWVLIsSupported, Avx512BW.VL.IsSupported, null);
        Check("Avx512BW.X64", ExpectedAvx512BW, &Avx512BWX64IsSupported, Avx512BW.X64.IsSupported, null);

        Check("Avx512CD", ExpectedAvx512CD, &Avx512CDIsSupported, Avx512CD.IsSupported, () => Avx512CD.LeadingZeroCount(Vector512<uint>.AllBitsSet) == Vector512<uint>.Zero);
        Check("Avx512CD.VL", ExpectedAvx512CD, &Avx512CDVLIsSupported, Avx512CD.VL.IsSupported, null);
        Check("Avx512CD.X64", ExpectedAvx512CD, &Avx512CDX64IsSupported, Avx512CD.X64.IsSupported, null);

        Check("Avx512DQ", ExpectedAvx512DQ, &Avx512DQIsSupported, Avx512DQ.IsSupported, () => Avx512DQ.And(Vector512<float>.Zero, Vector512<float>.Zero).Equals(Vector512<float>.Zero));
        Check("Avx512DQ.VL", ExpectedAvx512DQ, &Avx512DQVLIsSupported, Avx512DQ.VL.IsSupported, null);
        Check("Avx512DQ.X64", ExpectedAvx512DQ, &Avx512DQX64IsSupported, Avx512DQ.X64.IsSupported, null);

        Check("Avx512Vbmi", ExpectedAvx512Vbmi, &Avx512VbmiIsSupported, Avx512Vbmi.IsSupported, () => Avx512Vbmi.PermuteVar64x8(Vector512<sbyte>.Zero, Vector512<sbyte>.Zero).Equals(Vector512<sbyte>.Zero));
        Check("Avx512Vbmi.VL", ExpectedAvx512Vbmi, &Avx512VbmiVLIsSupported, Avx512Vbmi.VL.IsSupported, null);
        Check("Avx512Vbmi.X64", ExpectedAvx512Vbmi, &Avx512VbmiX64IsSupported, Avx512Vbmi.X64.IsSupported, null);

        // Check("Avx512Bitalg", ExpectedAvx512Bitalg, &Avx512BitalgIsSupported, Avx512Bitalg.IsSupported, null);
        // Check("Avx512Bitalg.VL", ExpectedAvx512Bitalg, &Avx512BitalgVLIsSupported, Avx512Bitalg.VL.IsSupported, null);
        // Check("Avx512Bitalg.X64", ExpectedAvx512Bitalg, &Avx512BitalgX64IsSupported, Avx512Bitalg.X64.IsSupported, null);

        Check("Avx512Vbmi2", ExpectedAvx512Vbmi2, &Avx512Vbmi2IsSupported, Avx512Vbmi2.IsSupported, null);
        Check("Avx512Vbmi2.VL", ExpectedAvx512Vbmi2, &Avx512Vbmi2VLIsSupported, Avx512Vbmi2.VL.IsSupported, null);
        Check("Avx512Vbmi2.X64", ExpectedAvx512Vbmi2, &Avx512Vbmi2X64IsSupported, Avx512Vbmi2.X64.IsSupported, null);

        // Check("Avx512Vpopcntdq", ExpectedAvx512Vpopcntdq, &Avx512VpopcntdqIsSupported, Avx512Vpopcntdq.IsSupported, null);
        // Check("Avx512Vpopcntdq.VL", ExpectedAvx512Vpopcntdq, &Avx512VpopcntdqVLIsSupported, Avx512Vpopcntdq.VL.IsSupported, null);
        // Check("Avx512Vpopcntdq.X64", ExpectedAvx512Vpopcntdq, &Avx512VpopcntdqX64IsSupported, Avx512Vpopcntdq.X64.IsSupported, null);

        // Check("Avx512Bf16", ExpectedAvx512Bf16, &Avx512Bf16IsSupported, Avx512Bf16.IsSupported, null);
        // Check("Avx512Bf16.VL", ExpectedAvx512Bf16, &Avx512Bf16VLIsSupported, Avx512Bf16.VL.IsSupported, null);
        // Check("Avx512Bf16.X64", ExpectedAvx512Bf16, &Avx512Bf16X64IsSupported, Avx512Bf16.X64.IsSupported, null);

        // Check("Avx512Fp16", ExpectedAvx512Fp16, &Avx512Fp16IsSupported, Avx512Fp16.IsSupported, null);
        // Check("Avx512Fp16.VL", ExpectedAvx512Fp16, &Avx512Fp16VLIsSupported, Avx512Fp16.VL.IsSupported, null);
        // Check("Avx512Fp16.X64", ExpectedAvx512Fp16, &Avx512Fp16X64IsSupported, Avx512Fp16.X64.IsSupported, null);

        Check("Avx10v1", ExpectedAvx10v1, &Avx10v1IsSupported, Avx10v1.IsSupported, () => Avx10v1.Abs(Vector128<long>.Zero).Equals(Vector128<ulong>.Zero));
        Check("Avx10v1.X64", ExpectedAvx10v1, &Avx10v1X64IsSupported, Avx10v1.X64.IsSupported, null);

        Check("Avx10v1.V512", ExpectedAvx10v1V512, &Avx10v1V512IsSupported, Avx10v1.V512.IsSupported, () => Avx10v1.V512.Abs(Vector512<long>.Zero).Equals(Vector512<ulong>.Zero));
        Check("Avx10v1.V512.X64", ExpectedAvx10v1V512, &Avx10v1V512X64IsSupported, Avx10v1.V512.X64.IsSupported, null);

        Check("Aes", ExpectedAes, &AesIsSupported, Aes.IsSupported, () => Aes.KeygenAssist(Vector128<byte>.Zero, 0).Equals(Vector128.Create((byte)99)));
        Check("Aes.X64", ExpectedAes, &AesX64IsSupported, Aes.X64.IsSupported, null);

        Check("Pclmulqdq", ExpectedPclmulqdq, &PclmulqdqIsSupported, Pclmulqdq.IsSupported, () => Pclmulqdq.CarrylessMultiply(Vector128<long>.Zero, Vector128<long>.Zero, 0).Equals(Vector128<long>.Zero));
        Check("Pclmulqdq.X64", ExpectedPclmulqdq, &PclmulqdqX64IsSupported, Pclmulqdq.X64.IsSupported, null);

        // Check("Avx512Vp2intersect", ExpectedAvx512Vp2intersect, &Avx512Vp2intersectIsSupported, Avx512Vp2intersect.IsSupported, null);
        // Check("Avx512Vp2intersect.VL", ExpectedAvx512Vp2intersect, &Avx512Vp2intersectVLIsSupported, Avx512Vp2intersect.VL.IsSupported, null);
        // Check("Avx512Vp2intersect.X64", ExpectedAvx512Vp2intersect, &Avx512Vp2intersectX64IsSupported, Avx512Vp2intersect.X64.IsSupported, null);

        // Check("AvxIfma", ExpectedAvxIfma, &AvxIfmaIsSupported, AvxIfma.IsSupported, null);
        // Check("AvxIfma.X64", ExpectedAvxIfma, &AvxIfmaX64IsSupported, AvxIfma.X64.IsSupported, null);

        Check("AvxVnni", ExpectedAvxVnni, &AvxVnniIsSupported, AvxVnni.IsSupported, () => AvxVnni.MultiplyWideningAndAdd(Vector128<int>.Zero, Vector128<byte>.Zero, Vector128<sbyte>.Zero).Equals(Vector128<int>.Zero));
        Check("AvxVnni.X64", ExpectedAvxVnni, &AvxVnniX64IsSupported, AvxVnni.X64.IsSupported, null);

        Check("Gfni", ExpectedGfni, &GfniIsSupported, Gfni.IsSupported, () => Gfni.GaloisFieldMultiply(Vector128<byte>.Zero, Vector128<byte>.Zero).Equals(Vector128<byte>.Zero));
        Check("Gfni.V256", ExpectedGfniV256, &GfniV256IsSupported, Gfni.V256.IsSupported, () => Gfni.V256.GaloisFieldMultiply(Vector256<byte>.Zero, Vector256<byte>.Zero).Equals(Vector256<byte>.Zero));
        Check("Gfni.V512", ExpectedGfniV512, &GfniV512IsSupported, Gfni.V512.IsSupported, () => Gfni.V512.GaloisFieldMultiply(Vector512<byte>.Zero, Vector512<byte>.Zero).Equals(Vector512<byte>.Zero));
        Check("Gfni.X64", ExpectedGfni, &GfniX64IsSupported, Gfni.X64.IsSupported, null);

        // Check("Sha", ExpectedSha, &ShaIsSupported, Sha.IsSupported, null);
        // Check("Sha.X64", ExpectedSha, &ShaX64IsSupported, Sha.X64.IsSupported, null);

        // Check("Aes.V256", ExpectedAesV256, &AesV256IsSupported, Aes.V256.IsSupported, null);
        // Check("Aes.V512", ExpectedAesV512, &AesV512IsSupported, Aes.V512.IsSupported, null);

        Check("Pclmulqdq.V256", ExpectedPclmulqdqV256, &PclmulqdqV256IsSupported, Pclmulqdq.V256.IsSupported, () => Pclmulqdq.V256.CarrylessMultiply(Vector256<long>.Zero, Vector256<long>.Zero, 0).Equals(Vector256<long>.Zero));
        Check("Pclmulqdq.V512", ExpectedPclmulqdqV512, &PclmulqdqV512IsSupported, Pclmulqdq.V512.IsSupported, () => Pclmulqdq.V512.CarrylessMultiply(Vector512<long>.Zero, Vector512<long>.Zero, 0).Equals(Vector512<long>.Zero));

        // Check("WaitPkg", ExpectedWaitPkg, &WaitPkgIsSupported, WaitPkg.IsSupported, null);
        // Check("WaitPkg.X64", ExpectedWaitPkg, &WaitPkgX64IsSupported, WaitPkg.X64.IsSupported, null);

        Check("X86Serialize", ExpectedX86Serialize, &X86SerializeIsSupported, X86Serialize.IsSupported, () => { X86Serialize.Serialize(); return true; } );
        Check("X86Serialize.X64", ExpectedX86Serialize, &X86SerializeX64IsSupported, X86Serialize.X64.IsSupported, null);

        return s_success ? 100 : 1;
    }

    static bool s_success;

    // Need these because properties cannot have their address taken
    static bool X86BaseIsSupported() => X86Base.IsSupported;
    static bool X86BaseX64IsSupported() => X86Base.X64.IsSupported;

    static bool SseIsSupported() => Sse.IsSupported;
    static bool SseX64IsSupported() => Sse.X64.IsSupported;

    static bool Sse2IsSupported() => Sse2.IsSupported;
    static bool Sse2X64IsSupported() => Sse2.X64.IsSupported;

    static bool Sse3IsSupported() => Sse3.IsSupported;
    static bool Sse3X64IsSupported() => Sse3.X64.IsSupported;

    static bool Ssse3IsSupported() => Ssse3.IsSupported;
    static bool Ssse3X64IsSupported() => Ssse3.X64.IsSupported;

    static bool Sse41IsSupported() => Sse41.IsSupported;
    static bool Sse41X64IsSupported() => Sse41.X64.IsSupported;

    static bool Sse42IsSupported() => Sse42.IsSupported;
    static bool Sse42X64IsSupported() => Sse42.X64.IsSupported;

    static bool PopcntIsSupported() => Popcnt.IsSupported;
    static bool PopcntX64IsSupported() => Popcnt.X64.IsSupported;

    static bool AvxIsSupported() => Avx.IsSupported;
    static bool AvxX64IsSupported() => Avx.X64.IsSupported;

    static bool Avx2IsSupported() => Avx2.IsSupported;
    static bool Avx2X64IsSupported() => Avx2.X64.IsSupported;

    static bool Bmi1IsSupported() => Bmi1.IsSupported;
    static bool Bmi1X64IsSupported() => Bmi1.X64.IsSupported;

    static bool Bmi2IsSupported() => Bmi2.IsSupported;
    static bool Bmi2X64IsSupported() => Bmi2.X64.IsSupported;

    // static bool F16cIsSupported() => F16c.IsSupported;
    // static bool F16cX64IsSupported() => F16c.X64.IsSupported;

    static bool FmaIsSupported() => Fma.IsSupported;
    static bool FmaX64IsSupported() => Fma.X64.IsSupported;

    static bool LzcntIsSupported() => Lzcnt.IsSupported;
    static bool LzcntX64IsSupported() => Lzcnt.X64.IsSupported;

    static bool Avx512FIsSupported() => Avx512F.IsSupported;
    static bool Avx512FVLIsSupported() => Avx512F.VL.IsSupported;
    static bool Avx512FX64IsSupported() => Avx512F.X64.IsSupported;

    static bool Avx512BWIsSupported() => Avx512BW.IsSupported;
    static bool Avx512BWVLIsSupported() => Avx512BW.VL.IsSupported;
    static bool Avx512BWX64IsSupported() => Avx512BW.X64.IsSupported;

    static bool Avx512CDIsSupported() => Avx512CD.IsSupported;
    static bool Avx512CDVLIsSupported() => Avx512CD.VL.IsSupported;
    static bool Avx512CDX64IsSupported() => Avx512CD.X64.IsSupported;

    static bool Avx512DQIsSupported() => Avx512DQ.IsSupported;
    static bool Avx512DQVLIsSupported() => Avx512DQ.VL.IsSupported;
    static bool Avx512DQX64IsSupported() => Avx512DQ.X64.IsSupported;

    static bool Avx512VbmiIsSupported() => Avx512Vbmi.IsSupported;
    static bool Avx512VbmiVLIsSupported() => Avx512Vbmi.VL.IsSupported;
    static bool Avx512VbmiX64IsSupported() => Avx512Vbmi.X64.IsSupported;

    // static bool Avx512BitalgIsSupported() => Avx512Bitalg.IsSupported;
    // static bool Avx512BitalgVLIsSupported() => Avx512Bitalg.VL.IsSupported;
    // static bool Avx512BitalgX64IsSupported() => Avx512Bitalg.X64.IsSupported;

    static bool Avx512Vbmi2IsSupported() => Avx512Vbmi2.IsSupported;
    static bool Avx512Vbmi2VLIsSupported() => Avx512Vbmi2.VL.IsSupported;
    static bool Avx512Vbmi2X64IsSupported() => Avx512Vbmi2.X64.IsSupported;

    // static bool Avx512VpopcntdqIsSupported() => Avx512Vpopcntdq.IsSupported;
    // static bool Avx512VpopcntdqVLIsSupported() => Avx512Vpopcntdq.VL.IsSupported;
    // static bool Avx512VpopcntdqX64IsSupported() => Avx512Vpopcntdq.X64.IsSupported;

    // static bool Avx512Bf16IsSupported() => Avx512Bf16.IsSupported;
    // static bool Avx512Bf16VLIsSupported() => Avx512Bf16.VL.IsSupported;
    // static bool Avx512Bf16X64IsSupported() => Avx512Bf16.X64.IsSupported;

    // static bool Avx512Fp16IsSupported() => Avx512Fp16.IsSupported;
    // static bool Avx512Fp16VLIsSupported() => Avx512Fp16.VL.IsSupported;
    // static bool Avx512Fp16X64IsSupported() => Avx512Fp16.X64.IsSupported;

    static bool Avx10v1IsSupported() => Avx10v1.IsSupported;
    static bool Avx10v1X64IsSupported() => Avx10v1.X64.IsSupported;
    static bool Avx10v1V512IsSupported() => Avx10v1.V512.IsSupported;
    static bool Avx10v1V512X64IsSupported() => Avx10v1.V512.X64.IsSupported;

    static bool AesIsSupported() => Aes.IsSupported;
    static bool AesX64IsSupported() => Aes.X64.IsSupported;

    static bool PclmulqdqIsSupported() => Pclmulqdq.IsSupported;
    static bool PclmulqdqX64IsSupported() => Pclmulqdq.X64.IsSupported;

    // static bool Avx512Vp2intersectIsSupported() => Avx512Vp2intersect.IsSupported;
    // static bool Avx512Vp2intersectVLIsSupported() => Avx512Vp2intersect.VL.IsSupported;
    // static bool Avx512Vp2intersectX64IsSupported() => Avx512Vp2intersect.X64.IsSupported;

    // static bool AvxIfmaIsSupported() => AvxIfma.IsSupported;
    // static bool AvxIfmaX64IsSupported() => AvxIfma.X64.IsSupported;

    static bool AvxVnniIsSupported() => AvxVnni.IsSupported;
    static bool AvxVnniX64IsSupported() => AvxVnni.X64.IsSupported;

    static bool GfniIsSupported() => Gfni.IsSupported;
    static bool GfniV256IsSupported() => Gfni.V256.IsSupported;
    static bool GfniV512IsSupported() => Gfni.V512.IsSupported;
    static bool GfniX64IsSupported() => Gfni.X64.IsSupported;

    // static bool ShaIsSupported() => Sha.IsSupported;
    // static bool ShaX64IsSupported() => Sha.X64.IsSupported;

    // static bool AesV256IsSupported() => Aes.V256.IsSupported;
    // static bool AesV512IsSupported() => Aes.V512.IsSupported;

    static bool PclmulqdqV256IsSupported() => Pclmulqdq.V256.IsSupported;
    static bool PclmulqdqV512IsSupported() => Pclmulqdq.V512.IsSupported;

    // static bool WaitPkgIsSupported() => WaitPkg.IsSupported;
    // static bool WaitPkgX64IsSupported() => WaitPkg.X64.IsSupported;

    static bool X86SerializeIsSupported() => X86Serialize.IsSupported;
    static bool X86SerializeX64IsSupported() => X86Serialize.X64.IsSupported;

    static bool IsConstantTrue(delegate*<bool> code)
    {
        return
            // mov eax, 1; ret
            memcmp((byte*)code, new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3 })
            // push rbp; sub rsp, 10h; lea rbp, [rsp+10h]; mov dword ptr [rbp-4], 1
            || memcmp((byte*)code, new byte[] { 0x55, 0x48, 0x83, 0xEC, 0x10, 0x48, 0x8D, 0x6C, 0x24, 0x10, 0xC7, 0x45, 0xFC, 0x01, 0x00, 0x00, 0x00 })
            // push rbp; push rdi; push rax; lea rbp, [rsp+10h]; mov dword ptr [rbp-C], 1
            || memcmp((byte*)code, new byte[] { 0x55, 0x57, 0x50, 0x48, 0x8D, 0x6C, 0x24, 0x10, 0xC7, 0x45, 0xF4, 0x01, 0x00, 0x00, 0x00 });
    }

    static bool IsConstantFalse(delegate*<bool> code)
    {
        return
            // xor eax, eax; ret
            memcmp((byte*)code, new byte[] { 0x33, 0xC0, 0xC3 })
            // push rbp; sub rsp, 10h; lea rbp, [rsp+10h]; xor eax, eax
            || memcmp((byte*)code, new byte[] { 0x55, 0x48, 0x83, 0xEC, 0x10, 0x48, 0x8D, 0x6C, 0x24, 0x10, 0x33, 0xC0 })
            // push rbp; push rdi; push rax; lea rbp, [rsp+10h]; xor eax, eax
            || memcmp((byte*)code, new byte[] { 0x55, 0x57, 0x50, 0x48, 0x8D, 0x6C, 0x24, 0x10, 0x33, 0xC0 });
    }

    static void AssertIsConstantTrue(delegate*<bool> code)
    {
        bool constant = IsConstantTrue(code);
        if (constant)
            Console.WriteLine(" (constant)");
        else
            Console.WriteLine(" (BUG: NOT CONSTANT)");

        s_success &= constant;
    }

    static void AssertIsConstantFalse(delegate*<bool> code)
    {
        bool constant = IsConstantFalse(code);
        if (constant)
            Console.WriteLine(" (constant)");
        else
            Console.WriteLine(" (BUG: NOT CONSTANT)");

        s_success &= constant;
    }

    static void AssertIsNotConstant(delegate*<bool> code)
    {
        bool constant = IsConstantTrue(code) || IsConstantFalse(code);
        if (constant)
            Console.WriteLine(" (BUG: CONSTANT)");
        else
            Console.WriteLine(" (not constant)");

        s_success &= constant;
    }

    static void Check(string name, bool? expectedConstantValue, delegate*<bool> code, bool runtimeValue, Func<bool> runtimeValidator)
    {
        Console.Write($"{name}.IsSupported: {runtimeValue}");

        if (!expectedConstantValue.HasValue)
        {
            bool constant = IsConstantTrue(code) || IsConstantFalse(code);
            if (constant)
                Console.Write(" (BUG: CONSTANT), ");
            else
                Console.Write(" (not constant), ");

            s_success &= !constant;
        }
        else if (expectedConstantValue.Value)
        {
            bool constant = IsConstantTrue(code);
            if (constant)
                Console.Write(" (constant), ");
            else
                Console.Write(" (BUG: NOT CONSTANT TRUE), ");

            s_success &= constant;
        }
        else
        {
            bool constant = IsConstantFalse(code);
            if (constant)
                Console.Write(" (constant), ");
            else
                Console.Write(" (BUG: NOT CONSTANT FALSE), ");

            s_success &= constant;
        }

        if (runtimeValidator == null)
        {
            Console.WriteLine("[not checking body]");
            return;
        }

        if (runtimeValue)
        {
            bool good = runtimeValidator();
            if (good)
                Console.WriteLine("OK");
            else
                Console.WriteLine("BUG: NOT OK");

            s_success &= good;
        }
        else if (expectedConstantValue.HasValue)
        {
            // We should have generated a throwing body.
            bool good = false;
            try
            {
                runtimeValidator();
                Console.WriteLine("BUG: NOT THROWING");
            }
            catch (PlatformNotSupportedException)
            {
                good = true;
                Console.WriteLine("throwing");
            }

            s_success &= good;
        }
        else
        {
            Console.WriteLine("[no CPU support to run body]");
        }
    }

    static unsafe bool memcmp(byte* mem, byte[] bytes)
    {
        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] != *(mem + i))
                return false;
        }

        return true;
    }
}
