// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

using ArmAes = System.Runtime.Intrinsics.Arm.Aes;
using X86Aes = System.Runtime.Intrinsics.X86.Aes;

class Runtime_34587
{
    public static int Main()
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

        bool succeeded = true;

        succeeded &= ValidateArm();
        succeeded &= ValidateX86();

        return succeeded ? 100 : 0;
    }

    private static bool ValidateArm()
    {
        bool succeeded = true;

        succeeded &= ValidateArmBase();
        succeeded &= ValidateAdvSimd();
        succeeded &= ValidateAes();
        succeeded &= ValidateCrc32();
        succeeded &= ValidateDp();
        succeeded &= ValidateRdm();
        succeeded &= ValidateSha1();
        succeeded &= ValidateSha256();

        return succeeded;

        static bool ValidateArmBase()
        {
            bool succeeded = true;

            if (ArmBase.IsSupported)
            {
                succeeded &= (RuntimeInformation.OSArchitecture == Architecture.Arm64);
            }

            if (ArmBase.Arm64.IsSupported)
            {
                succeeded &= ArmBase.IsSupported;
                succeeded &= (RuntimeInformation.OSArchitecture == Architecture.Arm64);
            }

            return succeeded;
        }

        static bool ValidateAdvSimd()
        {
            bool succeeded = true;

            if (AdvSimd.IsSupported)
            {
                succeeded &= ArmBase.IsSupported;
            }

            if (AdvSimd.Arm64.IsSupported)
            {
                succeeded &= AdvSimd.IsSupported;
                succeeded &= ArmBase.Arm64.IsSupported;
            }

            return succeeded;
        }

        static bool ValidateAes()
        {
            bool succeeded = true;

            if (ArmAes.IsSupported)
            {
                succeeded &= ArmBase.IsSupported;
            }

            if (ArmAes.Arm64.IsSupported)
            {
                succeeded &= ArmAes.IsSupported;
                succeeded &= ArmBase.Arm64.IsSupported;
            }

            return succeeded;
        }

        static bool ValidateCrc32()
        {
            bool succeeded = true;

            if (Crc32.IsSupported)
            {
                succeeded &= ArmBase.IsSupported;
            }

            if (Crc32.Arm64.IsSupported)
            {
                succeeded &= Crc32.IsSupported;
                succeeded &= ArmBase.Arm64.IsSupported;
            }

            return succeeded;
        }

        static bool ValidateDp()
        {
            bool succeeded = true;

            if (Dp.IsSupported)
            {
                succeeded &= AdvSimd.IsSupported;
            }

            if (Dp.Arm64.IsSupported)
            {
                succeeded &= Dp.IsSupported;
                succeeded &= AdvSimd.Arm64.IsSupported;
            }

            return succeeded;
        }

        static bool ValidateRdm()
        {
            bool succeeded = true;

            if (Rdm.IsSupported)
            {
                succeeded &= AdvSimd.IsSupported;
            }

            if (Rdm.Arm64.IsSupported)
            {
                succeeded &= Rdm.IsSupported;
                succeeded &= AdvSimd.Arm64.IsSupported;
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

            if (Sha1.Arm64.IsSupported)
            {
                succeeded &= Sha1.IsSupported;
                succeeded &= ArmBase.Arm64.IsSupported;
            }

            return succeeded;
        }

        static bool ValidateSha256()
        {
            bool succeeded = true;

            if (Sha256.IsSupported)
            {
                succeeded &= ArmBase.IsSupported;
            }

            if (Sha256.Arm64.IsSupported)
            {
                succeeded &= Sha256.IsSupported;
                succeeded &= ArmBase.Arm64.IsSupported;
            }

            return succeeded;
        }
    }

    public static bool ValidateX86()
    {
        bool succeeded = true;

        succeeded &= ValidateSse();
        succeeded &= ValidateSse2();
        succeeded &= ValidateSse3();
        succeeded &= ValidateSsse3();
        succeeded &= ValidateSse41();
        succeeded &= ValidateSse42();
        succeeded &= ValidateAvx();
        succeeded &= ValidateAvx2();
        succeeded &= ValidateAes();
        succeeded &= ValidateBmi1();
        succeeded &= ValidateBmi2();
        succeeded &= ValidateFma();
        succeeded &= ValidateLzcnt();
        succeeded &= ValidatePclmulqdq();
        succeeded &= ValidatePopcnt();

        return succeeded;

        static bool ValidateSse()
        {
            bool succeeded = true;

            if (Sse.IsSupported)
            {
                succeeded &= (RuntimeInformation.OSArchitecture == Architecture.X86) || (RuntimeInformation.OSArchitecture == Architecture.X64);
            }

            if (Sse.X64.IsSupported)
            {
                succeeded &= Sse.IsSupported;
                succeeded &= (RuntimeInformation.OSArchitecture == Architecture.X64);
            }

            return succeeded;
        }

        static bool ValidateSse2()
        {
            bool succeeded = true;

            if (Sse2.IsSupported)
            {
                succeeded &= Sse.IsSupported;
            }

            if (Sse2.X64.IsSupported)
            {
                succeeded &= Sse2.IsSupported;
                succeeded &= Sse.X64.IsSupported;
            }

            return succeeded;
        }

        static bool ValidateSse3()
        {
            bool succeeded = true;

            if (Sse3.IsSupported)
            {
                succeeded &= Sse2.IsSupported;
            }

            if (Sse3.X64.IsSupported)
            {
                succeeded &= Sse3.IsSupported;
                succeeded &= Sse2.X64.IsSupported;
            }

            return succeeded;
        }

        static bool ValidateSsse3()
        {
            bool succeeded = true;

            if (Ssse3.IsSupported)
            {
                succeeded &= Sse3.IsSupported;
            }

            if (Ssse3.X64.IsSupported)
            {
                succeeded &= Ssse3.IsSupported;
                succeeded &= Sse3.X64.IsSupported;
            }

            return succeeded;
        }

        static bool ValidateSse41()
        {
            bool succeeded = true;

            if (Sse41.IsSupported)
            {
                succeeded &= Ssse3.IsSupported;
            }

            if (Sse41.X64.IsSupported)
            {
                succeeded &= Sse41.IsSupported;
                succeeded &= Ssse3.X64.IsSupported;
            }

            return succeeded;
        }

        static bool ValidateSse42()
        {
            bool succeeded = true;

            if (Sse42.IsSupported)
            {
                succeeded &= Sse41.IsSupported;
            }

            if (Sse42.X64.IsSupported)
            {
                succeeded &= Sse42.IsSupported;
                succeeded &= Sse41.X64.IsSupported;
            }

            return succeeded;
        }

        static bool ValidateAvx()
        {
            bool succeeded = true;

            if (Avx.IsSupported)
            {
                succeeded &= Sse42.IsSupported;
            }

            if (Avx.X64.IsSupported)
            {
                succeeded &= Avx.IsSupported;
                succeeded &= Sse42.X64.IsSupported;
            }

            return succeeded;
        }

        static bool ValidateAvx2()
        {
            bool succeeded = true;

            if (Avx2.IsSupported)
            {
                succeeded &= Avx.IsSupported;
            }

            if (Avx2.X64.IsSupported)
            {
                succeeded &= Avx2.IsSupported;
                succeeded &= Avx.X64.IsSupported;
            }

            return succeeded;
        }

        static bool ValidateAes()
        {
            bool succeeded = true;

            if (X86Aes.IsSupported)
            {
                succeeded &= Sse2.IsSupported;
            }

            if (X86Aes.X64.IsSupported)
            {
                succeeded &= X86Aes.IsSupported;
                succeeded &= Sse2.X64.IsSupported;
            }

            return succeeded;
        }

        static bool ValidateBmi1()
        {
            bool succeeded = true;

            if (Bmi1.IsSupported)
            {
                succeeded &= (RuntimeInformation.OSArchitecture == Architecture.X86) || (RuntimeInformation.OSArchitecture == Architecture.X64);
            }

            if (Bmi1.X64.IsSupported)
            {
                succeeded &= Bmi1.IsSupported;
                succeeded &= (RuntimeInformation.OSArchitecture == Architecture.X64);
            }

            return succeeded;
        }

        static bool ValidateBmi2()
        {
            bool succeeded = true;

            if (Bmi2.IsSupported)
            {
                succeeded &= (RuntimeInformation.OSArchitecture == Architecture.X86) || (RuntimeInformation.OSArchitecture == Architecture.X64);
            }

            if (Bmi2.X64.IsSupported)
            {
                succeeded &= Bmi2.IsSupported;
                succeeded &= (RuntimeInformation.OSArchitecture == Architecture.X64);
            }

            return succeeded;
        }

        static bool ValidateFma()
        {
            bool succeeded = true;

            if (Fma.IsSupported)
            {
                succeeded &= Avx.IsSupported;
            }

            if (Fma.X64.IsSupported)
            {
                succeeded &= Fma.IsSupported;
                succeeded &= Avx.X64.IsSupported;
            }

            return succeeded;
        }

        static bool ValidateLzcnt()
        {
            bool succeeded = true;

            if (Lzcnt.IsSupported)
            {
                succeeded &= (RuntimeInformation.OSArchitecture == Architecture.X86) || (RuntimeInformation.OSArchitecture == Architecture.X64);
            }

            if (Lzcnt.X64.IsSupported)
            {
                succeeded &= Lzcnt.IsSupported;
                succeeded &= (RuntimeInformation.OSArchitecture == Architecture.X64);
            }

            return succeeded;
        }

        static bool ValidatePclmulqdq()
        {
            bool succeeded = true;

            if (Pclmulqdq.IsSupported)
            {
                succeeded &= Sse2.IsSupported;
            }

            if (Pclmulqdq.X64.IsSupported)
            {
                succeeded &= Pclmulqdq.IsSupported;
                succeeded &= Sse2.X64.IsSupported;
            }

            return succeeded;
        }

        static bool ValidatePopcnt()
        {
            bool succeeded = true;

            if (Popcnt.IsSupported)
            {
                succeeded &= Sse42.IsSupported;
            }

            if (Popcnt.X64.IsSupported)
            {
                succeeded &= Popcnt.IsSupported;
                succeeded &= Sse42.X64.IsSupported;
            }

            return succeeded;
        }
    }
}
