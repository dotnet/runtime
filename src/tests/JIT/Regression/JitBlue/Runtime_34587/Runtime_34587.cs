// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Runtime.CompilerServices;

using ArmAes = System.Runtime.Intrinsics.Arm.Aes;
using X86Aes = System.Runtime.Intrinsics.X86.Aes;
using Xunit;

public class Runtime_34587
{
    [Fact]
    public static int TestEntryPoint()
    {
        TestLibrary.TestFramework.LogInformation("Supported x86 ISAs:");
        TestLibrary.TestFramework.LogInformation($"  AES:           {X86Aes.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  AVX:           {Avx.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  AVX2:          {Avx2.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  BMI1:          {Bmi1.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  BMI2:          {Bmi2.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  FMA:           {Fma.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  LZCNT:         {Lzcnt.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  PCLMULQDQ:     {Pclmulqdq.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  POPCNT:        {Popcnt.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  SSE:           {Sse.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  SSE2:          {Sse2.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  SSE3:          {Sse3.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  SSE4.1:        {Sse41.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  SSE4.2:        {Sse42.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  SSSE3:         {Ssse3.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  X86Base:       {X86Base.IsSupported}");

        TestLibrary.TestFramework.LogInformation("Supported x64 ISAs:");
        TestLibrary.TestFramework.LogInformation($"  AES.X64:       {X86Aes.X64.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  AVX.X64:       {Avx.X64.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  AVX2.X64:      {Avx2.X64.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  BMI1.X64:      {Bmi1.X64.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  BMI2.X64:      {Bmi2.X64.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  FMA.X64:       {Fma.X64.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  LZCNT.X64:     {Lzcnt.X64.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  PCLMULQDQ.X64: {Pclmulqdq.X64.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  POPCNT.X64:    {Popcnt.X64.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  SSE.X64:       {Sse.X64.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  SSE2.X64:      {Sse2.X64.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  SSE3.X64:      {Sse3.X64.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  SSE4.1.X64:    {Sse41.X64.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  SSE4.2.X64:    {Sse42.X64.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  SSSE3.X64:     {Ssse3.X64.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  X86Base.X64:   {X86Base.X64.IsSupported}");

        TestLibrary.TestFramework.LogInformation("Supported Arm ISAs:");
        TestLibrary.TestFramework.LogInformation($"  AdvSimd:       {AdvSimd.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  Aes:           {ArmAes.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  ArmBase:       {ArmBase.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  Crc32:         {Crc32.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  Dp:            {Dp.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  Rdm:           {Rdm.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  Sha1:          {Sha1.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  Sha256:        {Sha256.IsSupported}");

        TestLibrary.TestFramework.LogInformation("Supported Arm64 ISAs:");
        TestLibrary.TestFramework.LogInformation($"  AdvSimd.Arm64: {AdvSimd.Arm64.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  Aes.Arm64:     {ArmAes.Arm64.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  ArmBase.Arm64: {ArmBase.Arm64.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  Crc32.Arm64:   {Crc32.Arm64.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  Dp.Arm64:      {Dp.Arm64.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  Rdm.Arm64:     {Rdm.Arm64.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  Sha1.Arm64:    {Sha1.Arm64.IsSupported}");
        TestLibrary.TestFramework.LogInformation($"  Sha256.Arm64:  {Sha256.Arm64.IsSupported}");

        TestLibrary.TestFramework.LogInformation("Supported Cross Platform ISAs:");
        TestLibrary.TestFramework.LogInformation($"  Vector<T>:     {Vector.IsHardwareAccelerated}; {Vector<byte>.Count}");
        TestLibrary.TestFramework.LogInformation($"  Vector64<T>:   {Vector64.IsHardwareAccelerated}");
        TestLibrary.TestFramework.LogInformation($"  Vector128<T>:  {Vector128.IsHardwareAccelerated}");
        TestLibrary.TestFramework.LogInformation($"  Vector256<T>:  {Vector256.IsHardwareAccelerated}");

        bool succeeded = true;
        bool testSucceeded;

        testSucceeded = ValidateArm();
        Console.WriteLine($"ValidateArm: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateX86();
        Console.WriteLine($"ValidateX86: {testSucceeded}");
        succeeded &= testSucceeded;

        return succeeded ? 100 : 0;
    }

    private static bool ValidateArm()
    {
        bool succeeded = true;
        bool testSucceeded;

        testSucceeded = ValidateArmBase();
        Console.WriteLine($"ValidateArmBase: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateAdvSimd();
        Console.WriteLine($"ValidateAdvSimd: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateAes();
        Console.WriteLine($"ValidateAes: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateCrc32();
        Console.WriteLine($"ValidateCrc32: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateDp();
        Console.WriteLine($"ValidateDp: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateRdm();
        Console.WriteLine($"ValidateRdm: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateSha1();
        Console.WriteLine($"ValidateSha1: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateSha256();
        Console.WriteLine($"ValidateSha256: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateVectorT();
        Console.WriteLine($"ValidateVectorT: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateVector64();
        Console.WriteLine($"ValidateVector64: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateVector128();
        Console.WriteLine($"ValidateVector128: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateVector256();
        Console.WriteLine($"ValidateVector256: {testSucceeded}");
        succeeded &= testSucceeded;

        return succeeded;

        static bool ValidateArmBase()
        {
            bool succeeded = true;

            if (ArmBaseIsSupported)
            {
                succeeded &= (RuntimeInformation.OSArchitecture == Architecture.Arm64);
            }

            if (ArmBaseArm64IsSupported)
            {
                succeeded &= ArmBaseIsSupported;
                succeeded &= (RuntimeInformation.OSArchitecture == Architecture.Arm64);
            }

            return succeeded;
        }

        static bool ValidateAdvSimd()
        {
            bool succeeded = true;

            if (AdvSimdIsSupported)
            {
                succeeded &= ArmBaseIsSupported;
            }

            if (AdvSimdArm64IsSupported)
            {
                succeeded &= AdvSimdIsSupported;
                succeeded &= ArmBaseArm64IsSupported;
            }

            return succeeded;
        }

        static bool ValidateAes()
        {
            bool succeeded = true;

            if (ArmAesIsSupported)
            {
                succeeded &= ArmBaseIsSupported;
            }

            if (ArmAesArm64IsSupported)
            {
                succeeded &= ArmAesIsSupported;
                succeeded &= ArmBaseArm64IsSupported;
            }

            return succeeded;
        }

        static bool ValidateCrc32()
        {
            bool succeeded = true;

            if (Crc32IsSupported)
            {
                succeeded &= ArmBaseIsSupported;
            }

            if (Crc32Arm64IsSupported)
            {
                succeeded &= Crc32IsSupported;
                succeeded &= ArmBaseArm64IsSupported;
            }

            return succeeded;
        }

        static bool ValidateDp()
        {
            bool succeeded = true;

            if (DpIsSupported)
            {
                succeeded &= AdvSimdIsSupported;
            }

            if (DpArm64IsSupported)
            {
                succeeded &= DpIsSupported;
                succeeded &= AdvSimdArm64IsSupported;
            }

            return succeeded;
        }

        static bool ValidateRdm()
        {
            bool succeeded = true;

            if (RdmIsSupported)
            {
                succeeded &= AdvSimdIsSupported;
            }

            if (RdmArm64IsSupported)
            {
                succeeded &= RdmIsSupported;
                succeeded &= AdvSimdArm64IsSupported;
            }

            return succeeded;
        }

        static bool ValidateSha1()
        {
            bool succeeded = true;

            if (Sha1.IsSupported)
            {
                succeeded &= ArmBase.IsSupported;
            }

            if (Sha1Arm64IsSupported)
            {
                succeeded &= Sha1IsSupported;
                succeeded &= ArmBaseArm64IsSupported;
            }

            return succeeded;
        }

        static bool ValidateSha256()
        {
            bool succeeded = true;

            if (Sha256IsSupported)
            {
                succeeded &= ArmBaseIsSupported;
            }

            if (Sha256Arm64IsSupported)
            {
                succeeded &= Sha256IsSupported;
                succeeded &= ArmBaseArm64IsSupported;
            }

            return succeeded;
        }

        static bool ValidateVectorT()
        {
            bool succeeded = true;

            if (AdvSimdIsSupported)
            {
                succeeded &= VectorIsHardwareAccelerated;
                succeeded &= VectorByteCount == 16;
            }
            else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                succeeded &= !VectorIsHardwareAccelerated;
                succeeded &= VectorByteCount == 16;
            }

            return succeeded;
        }

        static bool ValidateVector64()
        {
            bool succeeded = true;

            if (AdvSimdIsSupported)
            {
                succeeded &= Vector64IsHardwareAccelerated;
                succeeded &= Vector64ByteCount == 8;
            }
            else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                succeeded &= !Vector64IsHardwareAccelerated;
                succeeded &= Vector64ByteCount == 8;
            }

            return succeeded;
        }

        static bool ValidateVector128()
        {
            bool succeeded = true;

            if (AdvSimdIsSupported)
            {
                succeeded &= Vector128IsHardwareAccelerated;
                succeeded &= Vector128ByteCount == 16;
            }
            else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                succeeded &= !Vector128IsHardwareAccelerated;
                succeeded &= Vector128ByteCount == 16;
            }

            return succeeded;
        }

        static bool ValidateVector256()
        {
            bool succeeded = true;

            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                succeeded &= !Vector256IsHardwareAccelerated;
                succeeded &= Vector256ByteCount == 32;
            }

            return succeeded;
        }
    }

    public static bool ValidateX86()
    {
        bool succeeded = true;
        bool testSucceeded;

        testSucceeded = ValidateX86Base();
        succeeded &= testSucceeded;
        Console.WriteLine($"ValidateX86Base: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateSse();
        Console.WriteLine($"ValidateSse: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateSse2();
        Console.WriteLine($"ValidateSse2: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateSse3();
        Console.WriteLine($"ValidateSse3: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateSsse3();
        Console.WriteLine($"ValidateSsse3: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateSse41();
        Console.WriteLine($"ValidateSse41: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateSse42();
        Console.WriteLine($"ValidateSse42: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateAvx();
        Console.WriteLine($"ValidateAvx: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateAvx2();
        Console.WriteLine($"ValidateAvx2: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateAes();
        Console.WriteLine($"ValidateAes: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateBmi1();
        Console.WriteLine($"ValidateBmi1: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateBmi2();
        Console.WriteLine($"ValidateBmi2: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateFma();
        Console.WriteLine($"ValidateFma: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateLzcnt();
        Console.WriteLine($"ValidateLzcnt: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidatePclmulqdq();
        Console.WriteLine($"ValidatePclmulqdq: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidatePopcnt();
        Console.WriteLine($"ValidatePopcnt: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateVectorT();
        Console.WriteLine($"ValidateVectorT: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateVector64();
        Console.WriteLine($"ValidateVector64: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateVector128();
        Console.WriteLine($"ValidateVector128: {testSucceeded}");
        succeeded &= testSucceeded;
        testSucceeded = ValidateVector256();
        Console.WriteLine($"ValidateVector256: {testSucceeded}");
        succeeded &= testSucceeded;
        
        return succeeded;

        static bool ValidateX86Base()
        {
            bool succeeded = true;

            if (X86BaseIsSupported)
            {
                succeeded &= (RuntimeInformation.ProcessArchitecture == Architecture.X86) || (RuntimeInformation.ProcessArchitecture == Architecture.X64);
            }

            if (X86BaseX64IsSupported)
            {
                succeeded &= X86BaseIsSupported;
                succeeded &= (RuntimeInformation.ProcessArchitecture == Architecture.X64);
            }

            return succeeded;
        }

        static bool ValidateSse()
        {
            bool succeeded = true;

            if (SseIsSupported)
            {
                succeeded &= X86BaseIsSupported;
            }

            if (SseX64IsSupported)
            {
                succeeded &= SseIsSupported;
                succeeded &= X86BaseX64IsSupported;
            }

            return succeeded;
        }

        static bool ValidateSse2()
        {
            bool succeeded = true;

            if (Sse2IsSupported)
            {
                succeeded &= SseIsSupported;
            }

            if (Sse2X64IsSupported)
            {
                succeeded &= Sse2IsSupported;
                succeeded &= SseX64IsSupported;
            }

            return succeeded;
        }

        static bool ValidateSse3()
        {
            bool succeeded = true;

            if (Sse3IsSupported)
            {
                succeeded &= Sse2IsSupported;
            }

            if (Sse3X64IsSupported)
            {
                succeeded &= Sse3IsSupported;
                succeeded &= Sse2X64IsSupported;
            }

            return succeeded;
        }

        static bool ValidateSsse3()
        {
            bool succeeded = true;

            if (Ssse3.IsSupported)
            {
                succeeded &= Sse3IsSupported;
            }

            if (Ssse3X64IsSupported)
            {
                succeeded &= Ssse3IsSupported;
                succeeded &= Sse3X64IsSupported;
            }

            return succeeded;
        }

        static bool ValidateSse41()
        {
            bool succeeded = true;

            if (Sse41IsSupported)
            {
                succeeded &= Ssse3IsSupported;
            }

            if (Sse41X64IsSupported)
            {
                succeeded &= Sse41IsSupported;
                succeeded &= Ssse3X64IsSupported;
            }

            return succeeded;
        }

        static bool ValidateSse42()
        {
            bool succeeded = true;

            if (Sse42IsSupported)
            {
                succeeded &= Sse41IsSupported;
            }

            if (Sse42X64IsSupported)
            {
                succeeded &= Sse42IsSupported;
                succeeded &= Sse41X64IsSupported;
            }

            return succeeded;
        }

        static bool ValidateAvx()
        {
            bool succeeded = true;

            if (AvxIsSupported)
            {
                succeeded &= Sse42IsSupported;
            }

            if (AvxX64IsSupported)
            {
                succeeded &= AvxIsSupported;
                succeeded &= Sse42X64IsSupported;
            }

            return succeeded;
        }

        static bool ValidateAvx2()
        {
            bool succeeded = true;

            if (Avx2IsSupported)
            {
                succeeded &= AvxIsSupported;
            }

            if (Avx2X64IsSupported)
            {
                succeeded &= Avx2IsSupported;
                succeeded &= AvxX64IsSupported;
            }

            return succeeded;
        }

        static bool ValidateAes()
        {
            bool succeeded = true;

            if (X86AesIsSupported)
            {
                succeeded &= Sse2IsSupported;
            }

            if (X86AesX64IsSupported)
            {
                succeeded &= X86AesIsSupported;
                succeeded &= Sse2X64IsSupported;
            }

            return succeeded;
        }

        static bool ValidateBmi1()
        {
            bool succeeded = true;

            if (Bmi1IsSupported)
            {
                succeeded &= (RuntimeInformation.OSArchitecture == Architecture.X86) || (RuntimeInformation.OSArchitecture == Architecture.X64);
            }

            if (Bmi1X64IsSupported)
            {
                succeeded &= Bmi1IsSupported;
                succeeded &= (RuntimeInformation.OSArchitecture == Architecture.X64);
            }

            return succeeded;
        }

        static bool ValidateBmi2()
        {
            bool succeeded = true;

            if (Bmi2IsSupported)
            {
                succeeded &= (RuntimeInformation.OSArchitecture == Architecture.X86) || (RuntimeInformation.OSArchitecture == Architecture.X64);
            }

            if (Bmi2X64IsSupported)
            {
                succeeded &= Bmi2IsSupported;
                succeeded &= (RuntimeInformation.OSArchitecture == Architecture.X64);
            }

            return succeeded;
        }

        static bool ValidateFma()
        {
            bool succeeded = true;

            if (FmaIsSupported)
            {
                succeeded &= Avx.IsSupported;
            }

            if (FmaX64IsSupported)
            {
                succeeded &= FmaIsSupported;
                succeeded &= AvxX64IsSupported;
            }

            return succeeded;
        }

        static bool ValidateLzcnt()
        {
            bool succeeded = true;

            if (LzcntIsSupported)
            {
                succeeded &= (RuntimeInformation.OSArchitecture == Architecture.X86) || (RuntimeInformation.OSArchitecture == Architecture.X64);
            }

            if (LzcntX64IsSupported)
            {
                succeeded &= LzcntIsSupported;
                succeeded &= (RuntimeInformation.OSArchitecture == Architecture.X64);
            }

            return succeeded;
        }

        static bool ValidatePclmulqdq()
        {
            bool succeeded = true;

            if (PclmulqdqIsSupported)
            {
                succeeded &= Sse2IsSupported;
            }

            if (PclmulqdqX64IsSupported)
            {
                succeeded &= PclmulqdqIsSupported;
                succeeded &= Sse2X64IsSupported;
            }

            return succeeded;
        }

        static bool ValidatePopcnt()
        {
            bool succeeded = true;

            if (PopcntIsSupported)
            {
                succeeded &= Sse42IsSupported;
            }

            if (PopcntX64IsSupported)
            {
                succeeded &= PopcntIsSupported;
                succeeded &= Sse42X64IsSupported;
            }

            return succeeded;
        }

        static bool ValidateVectorT()
        {
            bool succeeded = true;

            if (Avx2IsSupported)
            {
                succeeded &= VectorIsHardwareAccelerated;
                succeeded &= VectorByteCount == 32;
            }
            else if (Sse2IsSupported)
            {
                succeeded &= VectorIsHardwareAccelerated;
                succeeded &= VectorByteCount == 16;
            }
            else if ((RuntimeInformation.ProcessArchitecture == Architecture.X86) || (RuntimeInformation.ProcessArchitecture == Architecture.X64))
            {
                succeeded &= !Vector.IsHardwareAccelerated;
                succeeded &= VectorByteCount == 16;
            }

            return succeeded;
        }

        static bool ValidateVector64()
        {
            bool succeeded = true;

            if ((RuntimeInformation.ProcessArchitecture == Architecture.X86) || (RuntimeInformation.ProcessArchitecture == Architecture.X64))
            {
                succeeded &= !Vector64IsHardwareAccelerated;
                succeeded &= Vector64ByteCount == 8;
            }

            return succeeded;
        }

        static bool ValidateVector128()
        {
            bool succeeded = true;

            if (Sse2IsSupported)
            {
                succeeded &= Vector128IsHardwareAccelerated;
                succeeded &= Vector128ByteCount == 16;
            }
            else if ((RuntimeInformation.ProcessArchitecture == Architecture.X86) || (RuntimeInformation.ProcessArchitecture == Architecture.X64))
            {
                succeeded &= !Vector128IsHardwareAccelerated;
                succeeded &= Vector128ByteCount == 16;
            }

            return succeeded;
        }

        static bool ValidateVector256()
        {
            bool succeeded = true;

            if (Avx2IsSupported)
            {
                succeeded &= Vector256IsHardwareAccelerated;
                succeeded &= Vector256ByteCount == 32;
            }
            else if ((RuntimeInformation.ProcessArchitecture == Architecture.X86) || (RuntimeInformation.ProcessArchitecture == Architecture.X64))
            {
                succeeded &= !Vector256IsHardwareAccelerated;
                succeeded &= Vector256ByteCount == 32;
            }

            return succeeded;
        }
    }

    // Break issupported checks into non-inlined helper functions so that this test will catch issues
    // that occur during crossgen2. Crossgen2 is more fine grained in terms of what is supported than the runtime
    // so placing the entire set of checks in a single function will generally fall back to jitting all of the time
    // but there are cases where crossgen2 can produce code when only 1or 2 of these checks are hit. By
    // placing the checks into helper functions, the test isolates the checks in the jit, so that a single
    // instance of incorrect behavior can be identified.

    static bool X86BaseIsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return X86Base.IsSupported; } }
    static bool SseIsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Sse.IsSupported; } }
    static bool Sse2IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Sse2.IsSupported; } }
    static bool Sse3IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Sse3.IsSupported; } }
    static bool Ssse3IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Ssse3.IsSupported; } }
    static bool Sse41IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Sse41.IsSupported; } }
    static bool Sse42IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Sse42.IsSupported; } }
    static bool AvxIsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Avx.IsSupported; } }
    static bool Avx2IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Avx2.IsSupported; } }

    static bool X86AesIsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return X86Aes.IsSupported; } }
    static bool Bmi1IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Bmi1.IsSupported; } }
    static bool Bmi2IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Bmi2.IsSupported; } }
    static bool FmaIsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Fma.IsSupported; } }
    static bool LzcntIsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Lzcnt.IsSupported; } }
    static bool PclmulqdqIsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Pclmulqdq.IsSupported; } }
    static bool PopcntIsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Popcnt.IsSupported; } }

    static bool X86BaseX64IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return X86Base.X64.IsSupported; } }
    static bool SseX64IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Sse.X64.IsSupported; } }
    static bool Sse2X64IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Sse2.X64.IsSupported; } }
    static bool Sse3X64IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Sse3.X64.IsSupported; } }
    static bool Ssse3X64IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Ssse3.X64.IsSupported; } }
    static bool Sse41X64IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Sse41.X64.IsSupported; } }
    static bool Sse42X64IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Sse42.X64.IsSupported; } }
    static bool AvxX64IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Avx.X64.IsSupported; } }
    static bool Avx2X64IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Avx2.X64.IsSupported; } }

    static bool X86AesX64IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return X86Aes.X64.IsSupported; } }
    static bool Bmi1X64IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Bmi1.X64.IsSupported; } }
    static bool Bmi2X64IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Bmi2.X64.IsSupported; } }
    static bool FmaX64IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Fma.X64.IsSupported; } }
    static bool LzcntX64IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Lzcnt.X64.IsSupported; } }
    static bool PclmulqdqX64IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Pclmulqdq.X64.IsSupported; } }
    static bool PopcntX64IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Popcnt.X64.IsSupported; } }

    static bool ArmBaseIsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return ArmBase.IsSupported; } }
    static bool AdvSimdIsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return AdvSimd.IsSupported; } }
    static bool ArmAesIsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return ArmAes.IsSupported; } }
    static bool Crc32IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Crc32.IsSupported; } }
    static bool DpIsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Dp.IsSupported; } }
    static bool RdmIsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Rdm.IsSupported; } }
    static bool Sha1IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Sha1.IsSupported; } }
    static bool Sha256IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Sha256.IsSupported; } }

    static bool ArmBaseArm64IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return ArmBase.Arm64.IsSupported; } }
    static bool AdvSimdArm64IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return AdvSimd.Arm64.IsSupported; } }
    static bool ArmAesArm64IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return ArmAes.Arm64.IsSupported; } }
    static bool Crc32Arm64IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Crc32.Arm64.IsSupported; } }
    static bool DpArm64IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Dp.Arm64.IsSupported; } }
    static bool RdmArm64IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Rdm.Arm64.IsSupported; } }
    static bool Sha1Arm64IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Sha1.Arm64.IsSupported; } }
    static bool Sha256Arm64IsSupported { [MethodImpl(MethodImplOptions.NoInlining)] get { return Sha256.Arm64.IsSupported; } }

    static bool VectorIsHardwareAccelerated { [MethodImpl(MethodImplOptions.NoInlining)] get { return Vector.IsHardwareAccelerated; } }
    static bool Vector64IsHardwareAccelerated { [MethodImpl(MethodImplOptions.NoInlining)] get { return Vector64.IsHardwareAccelerated; } }
    static bool Vector128IsHardwareAccelerated { [MethodImpl(MethodImplOptions.NoInlining)] get { return Vector128.IsHardwareAccelerated; } }
    static bool Vector256IsHardwareAccelerated { [MethodImpl(MethodImplOptions.NoInlining)] get { return Vector256.IsHardwareAccelerated; } }
    static int VectorByteCount { [MethodImpl(MethodImplOptions.NoInlining)] get { return Vector<byte>.Count; } }
    static int Vector64ByteCount { [MethodImpl(MethodImplOptions.NoInlining)] get { return Vector64<byte>.Count; } }
    static int Vector128ByteCount { [MethodImpl(MethodImplOptions.NoInlining)] get { return Vector128<byte>.Count; } }
    static int Vector256ByteCount { [MethodImpl(MethodImplOptions.NoInlining)] get { return Vector256<byte>.Count; } }
}
