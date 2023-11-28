// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Reflection;
using Xunit;

namespace XarchHardwareIntrinsicTest._CpuId
{
    public class Program
    {
        const int Pass = 100;
        const int Fail = 0;

        [Fact]
        public unsafe static int TestEntryPoint()
        {
            int testResult = Pass;

            if (!X86Base.IsSupported)
            {
                return testResult;
            }

            (int eax, int ebx, int ecx, int edx) = X86Base.CpuId(0x00000000, 0x00000000);

            bool isAuthenticAmd = (ebx == 0x68747541) && (ecx == 0x444D4163) && (edx == 0x69746E65);
            bool isGenuineIntel = (ebx == 0x756E6547) && (ecx == 0x6C65746E) && (edx == 0x49656E69);
            bool isVirtualCPU = (ebx == 0x74726956) && (ecx == 0x20555043) && (edx == 0x206C6175);

            if (!isAuthenticAmd && !isGenuineIntel && !isVirtualCPU)
            {
                // CPUID checks are vendor specific and aren't guaranteed to match up, even across Intel/AMD
                // as such, we limit ourselves to just AuthenticAMD, GenuineIntel and "Virtual CPU" right now. Any other
                // vendors would need to be validated against the checks below and added to the list as necessary.

                // An example of a difference is Intel/AMD for LZCNT. While the same underlying bit is used to
                // represent presence of the LZCNT instruction, AMD began using this bit around 2007 for its
                // ABM instruction set, which indicates LZCNT and POPCNT. Intel introduced a separate bit for
                // POPCNT and didn't actually implement LZCNT and begin using the LZCNT bit until 2013. So
                // while everything happens to line up today, it doesn't always and may not always do so.

                Console.WriteLine($"Unrecognized CPU vendor: EBX: {ebx:X8}, ECX: {ecx:X8}, EDX: {edx:X8}");
                testResult = Fail;
            }

            uint maxFunctionId = (uint)eax;

            if (maxFunctionId < 0x00000001)
            {
                return testResult;
            }

            bool isX86BaseDisabled = !GetDotnetEnable("HWINTRINSIC");
            bool isHierarchyDisabled = isX86BaseDisabled;

            (eax, ebx, ecx, edx) = X86Base.CpuId(0x00000001, 0x00000000);

            int xarchCpuInfo = eax;

            if (IsBitIncorrect(edx, 25, typeof(Sse), Sse.IsSupported, "SSE", ref isHierarchyDisabled))
            {
                testResult = Fail;
            }

            if (IsBitIncorrect(edx, 26, typeof(Sse2), Sse2.IsSupported, "SSE2", ref isHierarchyDisabled))
            {
                testResult = Fail;
            }

            bool isSse2HierarchyDisabled = isHierarchyDisabled;

            if (IsBitIncorrect(ecx, 25, typeof(Aes), Aes.IsSupported, "AES", ref isHierarchyDisabled))
            {
                testResult = Fail;
            }

            isHierarchyDisabled = isSse2HierarchyDisabled;

            if (IsBitIncorrect(ecx, 1, typeof(Pclmulqdq), Pclmulqdq.IsSupported, "PCLMULQDQ", ref isHierarchyDisabled))
            {
                testResult = Fail;
            }

            isHierarchyDisabled = isSse2HierarchyDisabled | !GetDotnetEnable("SSE3_4");

            if (IsBitIncorrect(ecx, 0, typeof(Sse3), Sse3.IsSupported, "SSE3", ref isHierarchyDisabled))
            {
                testResult = Fail;
            }

            if (IsBitIncorrect(ecx, 9, typeof(Ssse3), Ssse3.IsSupported, "SSSE3", ref isHierarchyDisabled))
            {
                testResult = Fail;
            }

            if (IsBitIncorrect(ecx, 19, typeof(Sse41), Sse41.IsSupported, "SSE41", ref isHierarchyDisabled))
            {
                testResult = Fail;
            }

            if (IsBitIncorrect(ecx, 20, typeof(Sse42), Sse42.IsSupported, "SSE42", ref isHierarchyDisabled))
            {
                testResult = Fail;
            }

            bool isSse42HierarchyDisabled = isHierarchyDisabled;

            if (IsBitIncorrect(ecx, 23, typeof(Popcnt), Popcnt.IsSupported, "POPCNT", ref isHierarchyDisabled))
            {
                testResult = Fail;
            }

            isHierarchyDisabled = isSse42HierarchyDisabled;

            if (IsBitIncorrect(ecx, 28, typeof(Avx), Avx.IsSupported, "AVX", ref isHierarchyDisabled))
            {
                testResult = Fail;
            }

            bool isAvxHierarchyDisabled = isHierarchyDisabled;

            if (IsBitIncorrect(ecx, 12, typeof(Fma), Fma.IsSupported, "FMA", ref isHierarchyDisabled))
            {
                testResult = Fail;
            }

            bool isFmaHierarchyDisabled = isHierarchyDisabled;

            if (maxFunctionId < 0x00000007)
            {
                return testResult;
            }

            (eax, ebx, ecx, edx) = X86Base.CpuId(0x00000007, 0x00000000);

            isHierarchyDisabled = isAvxHierarchyDisabled;

            if (IsBitIncorrect(ebx, 5, typeof(Avx2), Avx2.IsSupported, "AVX2", ref isHierarchyDisabled))
            {
                testResult = Fail;
            }

            bool isAvx2HierarchyDisabled = isHierarchyDisabled;

            isHierarchyDisabled = isAvxHierarchyDisabled;

            if (IsBitIncorrect(ebx, 3, typeof(Bmi1), Bmi1.IsSupported, "BMI1", ref isHierarchyDisabled))
            {
                testResult = Fail;
            }

            isHierarchyDisabled = isAvxHierarchyDisabled;

            if (IsBitIncorrect(ebx, 8, typeof(Bmi2), Bmi2.IsSupported, "BMI2", ref isHierarchyDisabled))
            {
                testResult = Fail;
            }

            isHierarchyDisabled = isAvx2HierarchyDisabled | isFmaHierarchyDisabled | OperatingSystem.IsMacOS();

            for (int i = 0; i < 2; i++)
            {
                // The runtime currently requires that all of F + BW + CD + DQ + VL be supported together or none
                // are supported. To handle this we simple check them all twice so that if any of them are disabled
                // the first time around, we'll then assert that they are all actually disabled the second time around

                if (IsBitIncorrect(ebx, 16, typeof(Avx512F), Avx512F.IsSupported, "AVX512F", ref isHierarchyDisabled))
                {
                    testResult = Fail;
                }

                if (IsBitIncorrect(ebx, 31, typeof(Avx512F.VL), Avx512F.VL.IsSupported, "AVX512F_VL", ref isHierarchyDisabled))
                {
                    testResult = Fail;
                }

                if (IsBitIncorrect(ebx, 30, typeof(Avx512BW), Avx512BW.IsSupported, "AVX512BW", ref isHierarchyDisabled))
                {
                    testResult = Fail;
                }

                if (IsBitIncorrect(ebx, 30, typeof(Avx512BW.VL), Avx512BW.VL.IsSupported, "AVX512BW_VL", ref isHierarchyDisabled))
                {
                    testResult = Fail;
                }

                if (IsBitIncorrect(ebx, 28, typeof(Avx512CD), Avx512CD.IsSupported, "AVX512CD", ref isHierarchyDisabled))
                {
                    testResult = Fail;
                }

                if (IsBitIncorrect(ebx, 28, typeof(Avx512CD.VL), Avx512CD.VL.IsSupported, "AVX512CD_VL", ref isHierarchyDisabled))
                {
                    testResult = Fail;
                }

                if (IsBitIncorrect(ebx, 17, typeof(Avx512DQ), Avx512DQ.IsSupported, "AVX512DQ", ref isHierarchyDisabled))
                {
                    testResult = Fail;
                }

                if (IsBitIncorrect(ebx, 17, typeof(Avx512DQ.VL), Avx512DQ.VL.IsSupported, "AVX512DQ_VL", ref isHierarchyDisabled))
                {
                    testResult = Fail;
                }
            }

            bool isAvx512HierarchyDisabled = isHierarchyDisabled;

            int preferredVectorBitWidth = (GetDotnetEnvVar("PreferredVectorBitWidth", defaultValue: 0) / 128) * 128;
            int preferredVectorByteLength = preferredVectorBitWidth / 8;

            if (preferredVectorByteLength == 0)
            {
                bool isVector512Throttling = false;

                if (isGenuineIntel)
                {
                    int steppingId = xarchCpuInfo & 0b1111;
                    int model = (xarchCpuInfo >> 4) & 0b1111;
                    int familyID = (xarchCpuInfo >> 8) & 0b1111;
                    int extendedModelID = (xarchCpuInfo >> 16) & 0b1111;

                    if (familyID == 0x06)
                    {
                        if (extendedModelID == 0x05)
                        {
                            if (model == 0x05)
                            {
                                // * Skylake (Server)
                                // * Cascade Lake
                                // * Cooper Lake

                                isVector512Throttling = true;
                            }
                        }
                        else if (extendedModelID == 0x06)
                        {
                            if (model == 0x06)
                            {
                                // * Cannon Lake

                                isVector512Throttling = true;
                            }
                        }
                    }
                }

                if (isAvx512HierarchyDisabled || isVector512Throttling)
                {
                    preferredVectorByteLength = 256 / 8;
                }
                else
                {
                    preferredVectorByteLength = 512 / 8;
                }
            }

            if (IsBitIncorrect(ecx, 1, typeof(Avx512Vbmi), Avx512Vbmi.IsSupported, "AVX512VBMI", ref isHierarchyDisabled))
            {
                testResult = Fail;
            }

            if (IsBitIncorrect(ecx, 1, typeof(Avx512Vbmi.VL), Avx512Vbmi.VL.IsSupported, "AVX512VBMI_VL", ref isHierarchyDisabled))
            {
                testResult = Fail;
            }

            isHierarchyDisabled = isX86BaseDisabled;

            if (IsBitIncorrect(edx, 14, typeof(X86Serialize), X86Serialize.IsSupported, "SERIALIZE", ref isHierarchyDisabled))
            {
                testResult = Fail;
            }

            (eax, ebx, ecx, edx) = X86Base.CpuId(0x00000007, 0x00000001);

            isHierarchyDisabled = isAvx2HierarchyDisabled;

#pragma warning disable CA2252 // No need to opt into preview feature for an internal test
            if (IsBitIncorrect(eax, 4, typeof(AvxVnni), AvxVnni.IsSupported, "AVXVNNI", ref isHierarchyDisabled))
            {
                testResult = Fail;
            }
#pragma warning restore CA2252

            (eax, ebx, ecx, edx) = X86Base.CpuId(unchecked((int)0x80000000), 0x00000000);

            uint maxFunctionIdEx = (uint)eax;

            if (maxFunctionIdEx < 0x00000001)
            {
                return testResult;
            }

            (eax, ebx, ecx, edx) = X86Base.CpuId(unchecked((int)0x80000001), 0x00000000);

            isHierarchyDisabled = isX86BaseDisabled;

            if (IsBitIncorrect(ecx, 5, typeof(Lzcnt), Lzcnt.IsSupported, "LZCNT", ref isHierarchyDisabled))
            {
                testResult = Fail;
            }

            if (IsIncorrect(typeof(Vector64), Vector64.IsHardwareAccelerated, isHierarchyDisabled: true))
            {
                testResult = Fail;
            }

            if (IsIncorrect(typeof(Vector128), Vector128.IsHardwareAccelerated, isSse2HierarchyDisabled))
            {
                testResult = Fail;
            }

            if (IsIncorrect(typeof(Vector256), Vector256.IsHardwareAccelerated, isAvx2HierarchyDisabled || (preferredVectorByteLength < 32)))
            {
                testResult = Fail;
            }

            if (IsIncorrect(typeof(Vector512), Vector512.IsHardwareAccelerated, isAvx512HierarchyDisabled || (preferredVectorByteLength < 64)))
            {
                testResult = Fail;
            }

            if (IsIncorrect(typeof(Vector), Vector.IsHardwareAccelerated, isSse2HierarchyDisabled))
            {
                testResult = Fail;
            }

            if (Vector<byte>.Count == 16)
            {
                if (!isAvx2HierarchyDisabled)
                {
                    Console.WriteLine($"{typeof(Vector).FullName}.Count returned 16 but the hardware returned 32");
                    testResult = Fail;
                }
            }
            else if (Vector<byte>.Count == 32)
            {
                if (isAvx2HierarchyDisabled)
                {
                    Console.WriteLine($"{typeof(Vector).FullName}.Count returned 32 but the hardware returned 16");
                    testResult = Fail;
                }
            }
            else
            {
                Console.WriteLine($"{typeof(Vector).FullName}.Count returned {Vector<byte>.Count} which is unexpected");
                testResult = Fail;
            }

            if (Vector<byte>.Count != (int)typeof(Vector<byte>).GetProperty("Count")!.GetValue(null)!)
            {
                Console.WriteLine($"{typeof(Vector).FullName}.Count returned a different result when called via reflection");
                testResult = Fail;
            }

            return testResult;
        }

        static bool IsBitIncorrect(int register, int bitNumber, Type isa, bool isSupported, string name, ref bool isHierarchyDisabled)
        {
            bool isSupportedByHardware = (register & (1 << bitNumber)) != 0;
            isHierarchyDisabled |= (!isSupported || !GetDotnetEnable(name));

            if (isSupported)
            {
                if (!isSupportedByHardware)
                {
                    Console.WriteLine($"{isa.FullName}.IsSupported returned true but the hardware returned false");
                    return true;
                }

                if (isHierarchyDisabled)
                {
                    Console.WriteLine($"{isa.FullName}.IsSupported returned true but the runtime returned false");
                    return true;
                }
            }
            else if (isSupportedByHardware)
            {
                if (!isHierarchyDisabled)
                {
                    Console.WriteLine($"{isa.FullName}.IsSupported returned false but the hardware and runtime returned true");
                    return true;
                }
            }
            else
            {
                // The IsSupported query returned false and the hardware
                // says its unsupported, so we're all good
            }

            if (isSupported != (bool)isa.GetProperty("IsSupported")!.GetValue(null)!)
            {
                Console.WriteLine($"{isa.FullName}.IsSupported returned a different result when called via reflection");
                return true;
            }

            return false;
        }

        static bool IsIncorrect(Type isa, bool isHardwareAccelerated, bool isHierarchyDisabled)
        {
            if (isHardwareAccelerated)
            {
                if (isHierarchyDisabled)
                {
                    Console.WriteLine($"{isa.FullName}.IsHardwareAccelerated returned true but the runtime returned false");
                    return true;
                }
            }
            else if (!isHierarchyDisabled)
            {
                Console.WriteLine($"{isa.FullName}.IsHardwareAccelerated returned false but the hardware and runtime returned true");
                return true;
            }

            if (isHardwareAccelerated != (bool)isa.GetProperty("IsHardwareAccelerated")!.GetValue(null)!)
            {
                Console.WriteLine($"{isa.FullName}.IsHardwareAccelerated returned a different result when called via reflection");
                return true;
            }

            return false;
        }

        static bool GetDotnetEnable(string name)
        {
            // Hardware Intrinsic configuration knobs default to true
            return GetDotnetEnvVar($"Enable{name}", defaultValue: 1) != 0;
        }

        static int GetDotnetEnvVar(string name, int defaultValue)
        {
            string? stringValue = Environment.GetEnvironmentVariable($"DOTNET_{name}");

            if ((stringValue is null) || !int.TryParse(stringValue, out int value))
            {
                return defaultValue;
            }

            return value;
        }
    }
}
