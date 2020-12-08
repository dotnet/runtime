// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;

namespace IntelHardwareIntrinsicTest
{
    class Program
    {
        const int Pass = 100;
        const int Fail = 0;

        static unsafe int Main(string[] args)
        {
            int testResult = Pass;

            if (!X86Base.IsSupported)
            {
                return testResult;
            }

            (int eax, int ebx, int ecx, int edx) = X86Base.CpuId(0x00000000, 0x00000000);

            bool isAuthenticAmd = (ebx == 0x68747541) && (ecx == 0x444D4163) && (edx == 0x69746E65);
            bool isGenuineIntel = (ebx == 0x756E6547) && (ecx == 0x6C65746E) && (edx == 0x49656E69);

            if (!isAuthenticAmd && !isGenuineIntel)
            {
                // CPUID checks are vendor specific and aren't guaranteed to match up, even across Intel/AMD
                // as such, we limit ourselves to just AuthenticAMD and GenuineIntel right now. Any other
                // vendors would need to be validated against the checks below and added to the list as necessary.

                // An example of a difference is Intel/AMD for LZCNT. While the same underlying bit is used to
                // represent presence of the LZCNT instruction, AMD began using this bit around 2007 for its
                // ABM instruction set, which indicates LZCNT and POPCNT. Intel introduced a separate bit for
                // POPCNT and didn't actually implement LZCNT and begin using the LZCNT bit until 2013. So
                // while everything happens to line up today, it doesn't always and may not always do so.

                Console.WriteLine($"Unrecognized CPU vendor: EBX: {ebx:X8}, ECX: {ecx:X8}, EDX: {edx:X8}");
                testResult = Fail;
            }

            int maxFunctionId = eax;

            if ((maxFunctionId < 0x00000001) || (Environment.GetEnvironmentVariable("COMPlus_EnableHWIntrinsic") is null))
            {
                return testResult;
            }

            (eax, ebx, ecx, edx) = X86Base.CpuId(0x00000001, 0x00000000);

            if (IsBitIncorrect(ecx, 28, Avx.IsSupported, "AVX"))
            {
                Console.WriteLine("CPUID Fn0000_0001_ECX:AVX != Avx.IsSupported");
                testResult = Fail;
            }

            if (IsBitIncorrect(ecx, 25, Aes.IsSupported, "AES"))
            {
                Console.WriteLine("CPUID Fn0000_0001_ECX:AES != Aes.IsSupported");
                testResult = Fail;
            }

            if (IsBitIncorrect(ecx, 23, Popcnt.IsSupported, "POPCNT"))
            {
                Console.WriteLine("CPUID Fn0000_0001_ECX:POPCNT != Popcnt.IsSupported");
                testResult = Fail;
            }

            if (IsBitIncorrect(ecx, 20, Sse42.IsSupported, "SSE42"))
            {
                Console.WriteLine("CPUID Fn0000_0001_ECX:SSE42 != Sse42.IsSupported");
                testResult = Fail;
            }

            if (IsBitIncorrect(ecx, 19, Sse41.IsSupported, "SSE41"))
            {
                Console.WriteLine("CPUID Fn0000_0001_ECX:SSE41 != Sse41.IsSupported");
                testResult = Fail;
            }

            if (IsBitIncorrect(ecx, 12, Fma.IsSupported, "FMA"))
            {
                Console.WriteLine("CPUID Fn0000_0001_ECX:FMA != Fma.IsSupported");
                testResult = Fail;
            }

            if (IsBitIncorrect(ecx, 9, Ssse3.IsSupported, "SSSE3"))
            {
                Console.WriteLine("CPUID Fn0000_0001_ECX:SSSE3 != Ssse3.IsSupported");
                testResult = Fail;
            }

            if (IsBitIncorrect(ecx, 1, Pclmulqdq.IsSupported, "PCLMULQDQ"))
            {
                Console.WriteLine("CPUID Fn0000_0001_ECX:PCLMULQDQ != Pclmulqdq.IsSupported");
                testResult = Fail;
            }

            if (IsBitIncorrect(ecx, 0, Sse3.IsSupported, "SSE3"))
            {
                Console.WriteLine("CPUID Fn0000_0001_ECX:SSE3 != Sse3.IsSupported");
                testResult = Fail;
            }

            if (IsBitIncorrect(edx, 26, Sse2.IsSupported, "SSE2"))
            {
                Console.WriteLine("CPUID Fn0000_0001_ECX:SSE2 != Sse2.IsSupported");
                testResult = Fail;
            }

            if (IsBitIncorrect(edx, 25, Sse.IsSupported, "SSE"))
            {
                Console.WriteLine("CPUID Fn0000_0001_ECX:SSE != Sse.IsSupported");
                testResult = Fail;
            }

            if (maxFunctionId < 0x00000007)
            {
                return testResult;
            }

            (eax, ebx, ecx, edx) = X86Base.CpuId(0x00000007, 0x00000000);

            if (IsBitIncorrect(ebx, 8, Bmi2.IsSupported, "BMI2"))
            {
                Console.WriteLine("CPUID Fn0000_0007_EBX:BMI2 != Bmi2.IsSupported");
                testResult = Fail;
            }

            if (IsBitIncorrect(ebx, 5, Avx2.IsSupported, "AVX2"))
            {
                Console.WriteLine("CPUID Fn0000_0007_EBX:AVX2 != Avx2.IsSupported");
                testResult = Fail;
            }

            if (IsBitIncorrect(ebx, 3, Bmi1.IsSupported, "BMI1"))
            {
                Console.WriteLine("CPUID Fn0000_0001_EBX:BMI1 != Bmi1.IsSupported");
                testResult = Fail;
            }

            (eax, ebx, ecx, edx) = X86Base.CpuId(unchecked((int)0x80000000), 0x00000000);

            if (isAuthenticAmd && ((ebx != 0x68747541) || (ecx != 0x444D4163) || (edx != 0x69746E65)))
            {
                Console.WriteLine("CPUID Fn8000_0000 reported different vendor info from Fn0000_0000");
                testResult = Fail;
            }

            if (isGenuineIntel && ((ebx != 0x756E6547) && (ecx != 0x6C65746E) && (edx != 0x6C656E69)))
            {
                Console.WriteLine("CPUID Fn8000_0000 reported different vendor info from Fn0000_0000");
                testResult = Fail;
            }

            int maxFunctionIdEx = eax;

            if (maxFunctionIdEx < 0x00000001)
            {
                return testResult;
            }

            (eax, ebx, ecx, edx) = X86Base.CpuId(unchecked((int)0x80000001), 0x00000000);

            if (IsBitIncorrect(ecx, 5, Lzcnt.IsSupported, "LZCNT"))
            {
                Console.WriteLine("CPUID Fn8000_0001_ECX:LZCNT != Lzcnt.IsSupported");
                testResult = Fail;
            }

            return testResult;
        }

        static bool IsBitIncorrect(int register, int bitNumber, bool expectedResult, string name)
        {
            return ((register & (1 << bitNumber)) != ((expectedResult ? 1 : 0) << bitNumber))
                && (Environment.GetEnvironmentVariable($"COMPlus_Enable{name}") is null);
        }
    }
}
