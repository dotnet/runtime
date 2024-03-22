// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public unsafe class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        s_success = true;

#if !DEBUG
        Console.WriteLine("****************************************************");
        Console.WriteLine("* Size test                                        *");
        long fileSize = new System.IO.FileInfo(Environment.ProcessPath).Length;
        Console.WriteLine($"* Size of the executable is {fileSize / 1024,7:n0} kB             *");
        Console.WriteLine("****************************************************");

        long lowerBound, upperBound;
        lowerBound = 1300 * 1024; // ~1.3 MB
        upperBound = 1750 * 1024; // ~1.75 MB

        if (fileSize < lowerBound || fileSize > upperBound)
        {
            Console.WriteLine($"BUG: File size is not in the expected range"
                              + " ({lowerBound} to {upperBound} bytes). Did a"
                              + " libraries change regress size of Hello World?");
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

        bool? ExpectedSse = true;
        bool? ExpectedSse2 = true;

#if BASELINE_INTRINSICS
        bool? ExpectedSse3 = null;
        bool? ExpectedSsse3 = null;
        bool? ExpectedAes = null;
        bool? ExpectedLzcnt = null;
        bool? ExpectedPclmulqdq = null;
        bool? ExpectedSse41 = null;
        bool? ExpectedSse42 = null;
        bool? ExpectedPopcnt = null;
        bool? ExpectedAvx = false;
        bool? ExpectedAvx2 = false;
        bool? ExpectedFma = false;
        bool? ExpectedBmi1 = false;
        bool? ExpectedBmi2 = false;
        bool? ExpectedAvxVnni = false;
        bool? ExpectedAvx512F = false;
        bool? ExpectedAvx512BW = false;
        bool? ExpectedAvx512CD = false;
        bool? ExpectedAvx512DQ = false;
        bool? ExpectedAvx512Vbmi = false;
        bool? ExpectedX86Serialize = null;
#elif SSE42_INTRINSICS
        bool? ExpectedSse3 = true;
        bool? ExpectedSsse3 = true;
        bool? ExpectedAes = null;
        bool? ExpectedLzcnt = null;
        bool? ExpectedPclmulqdq = null;
        bool? ExpectedSse41 = true;
        bool? ExpectedSse42 = true;
        bool? ExpectedPopcnt = null;
        bool? ExpectedAvx = false;
        bool? ExpectedAvx2 = false;
        bool? ExpectedFma = false;
        bool? ExpectedBmi1 = false;
        bool? ExpectedBmi2 = false;
        bool? ExpectedAvxVnni = false;
        bool? ExpectedAvx512F = false;
        bool? ExpectedAvx512BW = false;
        bool? ExpectedAvx512CD = false;
        bool? ExpectedAvx512DQ = false;
        bool? ExpectedAvx512Vbmi = false;
        bool? ExpectedX86Serialize = null;
#elif AVX_INTRINSICS
        bool? ExpectedSse3 = true;
        bool? ExpectedSsse3 = true;
        bool? ExpectedAes = null;
        bool? ExpectedLzcnt = null;
        bool? ExpectedPclmulqdq = null;
        bool? ExpectedSse41 = true;
        bool? ExpectedSse42 = true;
        bool? ExpectedPopcnt = null;
        bool? ExpectedAvx = true;
        bool? ExpectedAvx2 = false; // TODO: Fix once opportunistic Avx2 is allowed
        bool? ExpectedFma = null;
        bool? ExpectedBmi1 = null;
        bool? ExpectedBmi2 = null;
        bool? ExpectedAvxVnni = false; // TODO: Fix once opportunistic Avx2 is allowed
        bool? ExpectedAvx512F = false;
        bool? ExpectedAvx512BW = false;
        bool? ExpectedAvx512CD = false;
        bool? ExpectedAvx512DQ = false;
        bool? ExpectedAvx512Vbmi = false;
        bool? ExpectedX86Serialize = null;
#elif AVX2_INTRINSICS
        bool? ExpectedSse3 = true;
        bool? ExpectedSsse3 = true;
        bool? ExpectedAes = null;
        bool? ExpectedLzcnt = null;
        bool? ExpectedPclmulqdq = null;
        bool? ExpectedSse41 = true;
        bool? ExpectedSse42 = true;
        bool? ExpectedPopcnt = null;
        bool? ExpectedAvx = true;
        bool? ExpectedAvx2 = true;
        bool? ExpectedFma = null;
        bool? ExpectedBmi1 = null;
        bool? ExpectedBmi2 = null;
        bool? ExpectedAvxVnni = null;
        bool? ExpectedAvx512F = false;
        bool? ExpectedAvx512BW = false;
        bool? ExpectedAvx512CD = false;
        bool? ExpectedAvx512DQ = false;
        bool? ExpectedAvx512Vbmi = false;
        bool? ExpectedX86Serialize = null;
#elif AVX512_INTRINSICS
        bool? ExpectedSse3 = true;
        bool? ExpectedSsse3 = true;
        bool? ExpectedAes = null;
        bool? ExpectedLzcnt = null;
        bool? ExpectedPclmulqdq = null;
        bool? ExpectedSse41 = true;
        bool? ExpectedSse42 = true;
        bool? ExpectedPopcnt = null;
        bool? ExpectedAvx = true;
        bool? ExpectedAvx2 = true;
        bool? ExpectedFma = true;
        bool? ExpectedBmi1 = null;
        bool? ExpectedBmi2 = null;
        bool? ExpectedAvxVnni = null;
        bool? ExpectedAvx512F = true;
        bool? ExpectedAvx512BW = true;
        bool? ExpectedAvx512CD = true;
        bool? ExpectedAvx512DQ = true;
        bool? ExpectedAvx512Vbmi = null;
        bool? ExpectedX86Serialize = null;
#else
#error Who dis?
#endif

#if VECTORT128_INTRINSICS
        int byteVectorLength = 16;
#elif VECTORT256_INTRINSICS
        int byteVectorLength = 32;
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

        Check("Aes", ExpectedAes, &AesIsSupported, Aes.IsSupported, () => Aes.KeygenAssist(Vector128<byte>.Zero, 0).Equals(Vector128.Create((byte)99)));
        Check("Aes.X64", ExpectedAes, &AesX64IsSupported, Aes.X64.IsSupported, null);

        Check("Avx", ExpectedAvx, &AvxIsSupported, Avx.IsSupported, () => Avx.Add(Vector256<double>.Zero, Vector256<double>.Zero).Equals(Vector256<double>.Zero));
        Check("Avx.X64", ExpectedAvx, &AvxX64IsSupported, Avx.X64.IsSupported, null);

        Check("Avx2", ExpectedAvx2, &Avx2IsSupported, Avx2.IsSupported, () => Avx2.Abs(Vector256<int>.Zero).Equals(Vector256<uint>.Zero));
        Check("Avx2.X64", ExpectedAvx2, &Avx2X64IsSupported, Avx2.X64.IsSupported, null);

        Check("Bmi1", ExpectedBmi1, &Bmi1IsSupported, Bmi1.IsSupported, () => Bmi1.AndNot(0, 0) == 0);
        Check("Bmi1.X64", ExpectedBmi1, &Bmi1X64IsSupported, Bmi1.X64.IsSupported, () => Bmi1.X64.AndNot(0, 0) == 0);

        Check("Bmi2", ExpectedBmi2, &Bmi2IsSupported, Bmi2.IsSupported, () => Bmi2.MultiplyNoFlags(0, 0) == 0);
        Check("Bmi2.X64", ExpectedBmi2, &Bmi2X64IsSupported, Bmi2.X64.IsSupported, () => Bmi2.X64.MultiplyNoFlags(0, 0) == 0);

        Check("Fma", ExpectedFma, &FmaIsSupported, Fma.IsSupported, () => Fma.MultiplyAdd(Vector128<float>.Zero, Vector128<float>.Zero, Vector128<float>.Zero).Equals(Vector128<float>.Zero));
        Check("Fma.X64", ExpectedFma, &FmaX64IsSupported, Fma.X64.IsSupported, null);

        Check("Lzcnt", ExpectedLzcnt, &LzcntIsSupported, Lzcnt.IsSupported, () => Lzcnt.LeadingZeroCount(0) == 32);
        Check("Lzcnt.X64", ExpectedLzcnt, &LzcntX64IsSupported, Lzcnt.X64.IsSupported, () => Lzcnt.X64.LeadingZeroCount(0) == 64);

        Check("Pclmulqdq", ExpectedPclmulqdq, &PclmulqdqIsSupported, Pclmulqdq.IsSupported, () => Pclmulqdq.CarrylessMultiply(Vector128<long>.Zero, Vector128<long>.Zero, 0).Equals(Vector128<long>.Zero));
        Check("Pclmulqdq.X64", ExpectedPclmulqdq, &PclmulqdqX64IsSupported, Pclmulqdq.X64.IsSupported, null);

        Check("Popcnt", ExpectedPopcnt, &PopcntIsSupported, Popcnt.IsSupported, () => Popcnt.PopCount(0) == 0);
        Check("Popcnt.X64", ExpectedPopcnt, &PopcntX64IsSupported, Popcnt.X64.IsSupported, () => Popcnt.X64.PopCount(0) == 0);

        Check("AvxVnni", ExpectedAvxVnni, &AvxVnniIsSupported, AvxVnni.IsSupported, () => AvxVnni.MultiplyWideningAndAdd(Vector128<int>.Zero, Vector128<byte>.Zero, Vector128<sbyte>.Zero).Equals(Vector128<int>.Zero));
        Check("AvxVnni.X64", ExpectedAvxVnni, &AvxVnniX64IsSupported, AvxVnni.X64.IsSupported, null);

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

        Check("X86Serialize", ExpectedX86Serialize, &X86SerializeIsSupported, X86Serialize.IsSupported, () => { X86Serialize.Serialize(); return true; } );
        Check("X86Serialize.X64", ExpectedX86Serialize, &X86SerializeX64IsSupported, X86Serialize.X64.IsSupported, null);

        return s_success ? 100 : 1;
    }

    static bool s_success;

    // Need these because properties cannot have their address taken
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
    static bool AesIsSupported() => Aes.IsSupported;
    static bool AesX64IsSupported() => Aes.X64.IsSupported;
    static bool AvxIsSupported() => Avx.IsSupported;
    static bool AvxX64IsSupported() => Avx.X64.IsSupported;
    static bool Avx2IsSupported() => Avx2.IsSupported;
    static bool Avx2X64IsSupported() => Avx2.X64.IsSupported;
    static bool Bmi1IsSupported() => Bmi1.IsSupported;
    static bool Bmi1X64IsSupported() => Bmi1.X64.IsSupported;
    static bool Bmi2IsSupported() => Bmi2.IsSupported;
    static bool Bmi2X64IsSupported() => Bmi2.X64.IsSupported;
    static bool FmaIsSupported() => Fma.IsSupported;
    static bool FmaX64IsSupported() => Fma.X64.IsSupported;
    static bool LzcntIsSupported() => Lzcnt.IsSupported;
    static bool LzcntX64IsSupported() => Lzcnt.X64.IsSupported;
    static bool PclmulqdqIsSupported() => Pclmulqdq.IsSupported;
    static bool PclmulqdqX64IsSupported() => Pclmulqdq.X64.IsSupported;
    static bool PopcntIsSupported() => Popcnt.IsSupported;
    static bool PopcntX64IsSupported() => Popcnt.X64.IsSupported;
    static bool AvxVnniIsSupported() => AvxVnni.IsSupported;
    static bool AvxVnniX64IsSupported() => AvxVnni.X64.IsSupported;
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
