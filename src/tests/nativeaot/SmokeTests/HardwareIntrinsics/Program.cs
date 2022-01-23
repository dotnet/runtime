// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

unsafe class Program
{
    static int Main()
    {
        s_success = true;

        // We expect the AOT compiler generated HW intrinsics with the following characteristics:
        //
        // * TRUE = IsSupported assumed to be true, no runtime check
        // * NULL = IsSupported is a runtime check, code should be behind the check or bad things happen
        // * FALSE = IsSupported assumed to be false, no runtime check, PlatformNotSupportedException if used
        //
        // The test is compiled with multiple defines to test this.

#if BASELINE_INTRINSICS
        bool vectorsAccelerated = true;
        int byteVectorLength = 16;
        bool? Sse2AndBelow = true;
        bool? Sse3Group = null;
        bool? AesLzPcl = null;
        bool? Sse4142 = null;
        bool? PopCnt = null;
        bool? Avx12 = false;
        bool? FmaBmi12 = false;
        bool? Avxvnni = false;
#elif NON_VEX_INTRINSICS
        bool vectorsAccelerated = true;
        int byteVectorLength = 16;
        bool? Sse2AndBelow = true;
        bool? Sse3Group = true;
        bool? AesLzPcl = null;
        bool? Sse4142 = true;
        bool? PopCnt = null;
        bool? Avx12 = false;
        bool? FmaBmi12 = false;
        bool? Avxvnni = false;
#elif VEX_INTRINSICS
        bool vectorsAccelerated = true;
        int byteVectorLength = 32;
        bool? Sse2AndBelow = true;
        bool? Sse3Group = true;
        bool? AesLzPcl = null;
        bool? Sse4142 = true;
        bool? PopCnt = null;
        bool? Avx12 = true;
        bool? FmaBmi12 = null;
        bool? Avxvnni = null;
#else
#error Who dis?
#endif

        if (vectorsAccelerated != Vector.IsHardwareAccelerated)
        {
            throw new Exception($"Vectors HW acceleration state unexpected - expected {vectorsAccelerated}, got {Vector.IsHardwareAccelerated}");
        }

        if (byteVectorLength != Vector<byte>.Count)
        {
            throw new Exception($"Unexpected vector length - expected {byteVectorLength}, got {Vector<byte>.Count}");
        }

        Check("Sse", Sse2AndBelow, &SseIsSupported, Sse.IsSupported, () => Sse.Subtract(Vector128<float>.Zero, Vector128<float>.Zero).Equals(Vector128<float>.Zero));
        Check("Sse.X64", Sse2AndBelow, &SseX64IsSupported, Sse.X64.IsSupported, () => Sse.X64.ConvertToInt64WithTruncation(Vector128<float>.Zero) == 0);

        Check("Sse2", Sse2AndBelow, &Sse2IsSupported, Sse2.IsSupported, () => Sse2.Extract(Vector128<ushort>.Zero, 0) == 0);
        Check("Sse2.X64", Sse2AndBelow, &Sse2X64IsSupported, Sse2.X64.IsSupported, () => Sse2.X64.ConvertToInt64(Vector128<double>.Zero) == 0);

        Check("Sse3", Sse3Group, &Sse3IsSupported, Sse3.IsSupported, () => Sse3.MoveHighAndDuplicate(Vector128<float>.Zero).Equals(Vector128<float>.Zero));
        Check("Sse3.X64", Sse3Group, &Sse3X64IsSupported, Sse3.X64.IsSupported, null);

        Check("Ssse3", Sse3Group, &Ssse3IsSupported, Ssse3.IsSupported, () => Ssse3.Abs(Vector128<short>.Zero).Equals(Vector128<ushort>.Zero));
        Check("Ssse3.X64", Sse3Group, &Ssse3X64IsSupported, Ssse3.X64.IsSupported, null);

        Check("Sse41", Sse4142, &Sse41IsSupported, Sse41.IsSupported, () => Sse41.Max(Vector128<int>.Zero, Vector128<int>.Zero).Equals(Vector128<int>.Zero));
        Check("Sse41.X64", Sse4142, &Sse41X64IsSupported, Sse41.X64.IsSupported, () => Sse41.X64.Extract(Vector128<long>.Zero, 0) == 0);

        Check("Sse42", Sse4142, &Sse42IsSupported, Sse42.IsSupported, () => Sse42.Crc32(0, 0) == 0);
        Check("Sse42.X64", Sse4142, &Sse42X64IsSupported, Sse42.X64.IsSupported, () => Sse42.X64.Crc32(0, 0) == 0);

        Check("Aes", AesLzPcl, &AesIsSupported, Aes.IsSupported, () => Aes.KeygenAssist(Vector128<byte>.Zero, 0).Equals(Vector128.Create((byte)99)));
        Check("Aes.X64", AesLzPcl, &AesX64IsSupported, Aes.X64.IsSupported, null);

        Check("Avx", Avx12, &AvxIsSupported, Avx.IsSupported, () => Avx.Add(Vector256<double>.Zero, Vector256<double>.Zero).Equals(Vector256<double>.Zero));
        Check("Avx.X64", Avx12, &AvxX64IsSupported, Avx.X64.IsSupported, null);

        Check("Avx2", Avx12, &Avx2IsSupported, Avx2.IsSupported, () => Avx2.Abs(Vector256<int>.Zero).Equals(Vector256<uint>.Zero));
        Check("Avx2.X64", Avx12, &Avx2X64IsSupported, Avx2.X64.IsSupported, null);

        Check("Bmi1", FmaBmi12, &Bmi1IsSupported, Bmi1.IsSupported, () => Bmi1.AndNot(0, 0) == 0);
        Check("Bmi1.X64", FmaBmi12, &Bmi1X64IsSupported, Bmi1.X64.IsSupported, () => Bmi1.X64.AndNot(0, 0) == 0);

        Check("Bmi2", FmaBmi12, &Bmi2IsSupported, Bmi2.IsSupported, () => Bmi2.MultiplyNoFlags(0, 0) == 0);
        Check("Bmi2.X64", FmaBmi12, &Bmi2X64IsSupported, Bmi2.X64.IsSupported, () => Bmi2.X64.MultiplyNoFlags(0, 0) == 0);

        Check("Fma", FmaBmi12, &FmaIsSupported, Fma.IsSupported, () => Fma.MultiplyAdd(Vector128<float>.Zero, Vector128<float>.Zero, Vector128<float>.Zero).Equals(Vector128<float>.Zero));
        Check("Fma.X64", FmaBmi12, &FmaX64IsSupported, Fma.X64.IsSupported, null);

        Check("Lzcnt", AesLzPcl, &LzcntIsSupported, Lzcnt.IsSupported, () => Lzcnt.LeadingZeroCount(0) == 32);
        Check("Lzcnt.X64", AesLzPcl, &LzcntX64IsSupported, Lzcnt.X64.IsSupported, () => Lzcnt.X64.LeadingZeroCount(0) == 64);

        Check("Pclmulqdq", AesLzPcl, &PclmulqdqIsSupported, Pclmulqdq.IsSupported, () => Pclmulqdq.CarrylessMultiply(Vector128<long>.Zero, Vector128<long>.Zero, 0).Equals(Vector128<long>.Zero));
        Check("Pclmulqdq.X64", AesLzPcl, &PclmulqdqX64IsSupported, Pclmulqdq.X64.IsSupported, null);

        Check("Popcnt", PopCnt, &PopcntIsSupported, Popcnt.IsSupported, () => Popcnt.PopCount(0) == 0);
        Check("Popcnt.X64", PopCnt, &PopcntX64IsSupported, Popcnt.X64.IsSupported, () => Popcnt.X64.PopCount(0) == 0);

        Check("AvxVnni", Avxvnni, &AvxVnniIsSupported, AvxVnni.IsSupported, () => AvxVnni.MultiplyWideningAndAdd(Vector128<int>.Zero, Vector128<byte>.Zero, Vector128<sbyte>.Zero).Equals(Vector128<int>.Zero));
        Check("AvxVnni.X64", Avxvnni, &AvxVnniX64IsSupported, AvxVnni.X64.IsSupported, null);

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
