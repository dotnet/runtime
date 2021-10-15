// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Runtime_60035
{
    static class Program
    {
        public static int Main()
        {
            bool success = true;

            if (!TestEnvironmentVariablesAreWorking())
            {
                success = false;
            }

            if (!TestReadyToRunAssumptionsAreCorrect())
            {
                success = false;
            }

            return success ? 100 : 0;
        }

        static bool TestEnvironmentVariablesAreWorking()
        {
            bool success = true;

            if (String.Equals(Environment.GetEnvironmentVariable("DOTNET_EnableHWIntrinsic"), "0") || String.Equals(Environment.GetEnvironmentVariable("COMPlus_EnableHWIntrinsic"), "0"))
            {
                if (System.Runtime.Intrinsics.Arm.ArmBase.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.Arm.ArmBase.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.Arm.ArmBase.Arm64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.Arm.ArmBase.Arm64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Sse.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Sse.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse2.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Sse2.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse2.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Sse2.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse3.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Sse3.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse3.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Sse3.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Ssse3.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Ssse3.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Ssse3.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Ssse3.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse41.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Sse41.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse41.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Sse41.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse42.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Sse42.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse42.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Sse42.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Avx.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Avx.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx2.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Avx2.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx2.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Avx2.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.AvxVnni.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.AvxVnni.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.AvxVnni.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.AvxVnni.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Bmi1.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Bmi1.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Bmi1.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Bmi1.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Bmi2.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Bmi2.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Bmi2.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Bmi2.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Fma.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Fma.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Fma.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Fma.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Lzcnt.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Lzcnt.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Lzcnt.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Lzcnt.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Pclmulqdq.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Pclmulqdq.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Pclmulqdq.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Pclmulqdq.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Popcnt.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Popcnt.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Popcnt.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.X86.Popcnt.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.Arm.AdvSimd.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.Arm.AdvSimd.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.Arm.Aes.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.Arm.Aes.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.Arm.Aes.Arm64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.Arm.Aes.Arm64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.Arm.Crc32.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.Arm.Crc32.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.Arm.Crc32.Arm64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.Arm.Crc32.Arm64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.Arm.Dp.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.Arm.Dp.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.Arm.Dp.Arm64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.Arm.Dp.Arm64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.Arm.Rdm.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.Arm.Rdm.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.Arm.Rdm.Arm64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.Arm.Rdm.Arm64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.Arm.Sha1.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.Arm.Sha1.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.Arm.Sha1.Arm64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.Arm.Sha1.Arm64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.Arm.Sha256.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.Arm.Sha256.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.Arm.Sha256.Arm64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableHWIntrinsic or COMPlus_EnableHWIntrinsic is '0' but System.Runtime.Intrinsics.Arm.Sha256.Arm64.IsSupported returns 'True'");
                }
            }

            if (String.Equals(Environment.GetEnvironmentVariable("DOTNET_EnableSSE"), "0") || String.Equals(Environment.GetEnvironmentVariable("COMPlus_EnableSSE"), "0"))
            {
                if (System.Runtime.Intrinsics.X86.Sse.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE or COMPlus_EnableSSE is '0' but System.Runtime.Intrinsics.X86.Sse.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE or COMPlus_EnableSSE is '0' but System.Runtime.Intrinsics.X86.Sse.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse2.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE or COMPlus_EnableSSE is '0' but System.Runtime.Intrinsics.X86.Sse2.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse2.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE or COMPlus_EnableSSE is '0' but System.Runtime.Intrinsics.X86.Sse2.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse3.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE or COMPlus_EnableSSE is '0' but System.Runtime.Intrinsics.X86.Sse3.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse3.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE or COMPlus_EnableSSE is '0' but System.Runtime.Intrinsics.X86.Sse3.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Ssse3.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE or COMPlus_EnableSSE is '0' but System.Runtime.Intrinsics.X86.Ssse3.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Ssse3.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE or COMPlus_EnableSSE is '0' but System.Runtime.Intrinsics.X86.Ssse3.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse41.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE or COMPlus_EnableSSE is '0' but System.Runtime.Intrinsics.X86.Sse41.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse41.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE or COMPlus_EnableSSE is '0' but System.Runtime.Intrinsics.X86.Sse41.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse42.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE or COMPlus_EnableSSE is '0' but System.Runtime.Intrinsics.X86.Sse42.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse42.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE or COMPlus_EnableSSE is '0' but System.Runtime.Intrinsics.X86.Sse42.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE or COMPlus_EnableSSE is '0' but System.Runtime.Intrinsics.X86.Avx.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE or COMPlus_EnableSSE is '0' but System.Runtime.Intrinsics.X86.Avx.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx2.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE or COMPlus_EnableSSE is '0' but System.Runtime.Intrinsics.X86.Avx2.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx2.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE or COMPlus_EnableSSE is '0' but System.Runtime.Intrinsics.X86.Avx2.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.AvxVnni.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE or COMPlus_EnableSSE is '0' but System.Runtime.Intrinsics.X86.AvxVnni.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.AvxVnni.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE or COMPlus_EnableSSE is '0' but System.Runtime.Intrinsics.X86.AvxVnni.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Fma.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE or COMPlus_EnableSSE is '0' but System.Runtime.Intrinsics.X86.Fma.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Fma.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE or COMPlus_EnableSSE is '0' but System.Runtime.Intrinsics.X86.Fma.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Pclmulqdq.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE or COMPlus_EnableSSE is '0' but System.Runtime.Intrinsics.X86.Pclmulqdq.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Pclmulqdq.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE or COMPlus_EnableSSE is '0' but System.Runtime.Intrinsics.X86.Pclmulqdq.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Popcnt.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE or COMPlus_EnableSSE is '0' but System.Runtime.Intrinsics.X86.Popcnt.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Popcnt.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE or COMPlus_EnableSSE is '0' but System.Runtime.Intrinsics.X86.Popcnt.X64.IsSupported returns 'True'");
                }
            }

            if (String.Equals(Environment.GetEnvironmentVariable("DOTNET_EnableSSE2"), "0") || String.Equals(Environment.GetEnvironmentVariable("COMPlus_EnableSSE2"), "0"))
            {
                if (System.Runtime.Intrinsics.X86.Sse2.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE2 or COMPlus_EnableSSE2 is '0' but System.Runtime.Intrinsics.X86.Sse2.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse2.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE2 or COMPlus_EnableSSE2 is '0' but System.Runtime.Intrinsics.X86.Sse2.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse3.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE2 or COMPlus_EnableSSE2 is '0' but System.Runtime.Intrinsics.X86.Sse3.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse3.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE2 or COMPlus_EnableSSE2 is '0' but System.Runtime.Intrinsics.X86.Sse3.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Ssse3.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE2 or COMPlus_EnableSSE2 is '0' but System.Runtime.Intrinsics.X86.Ssse3.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Ssse3.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE2 or COMPlus_EnableSSE2 is '0' but System.Runtime.Intrinsics.X86.Ssse3.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse41.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE2 or COMPlus_EnableSSE2 is '0' but System.Runtime.Intrinsics.X86.Sse41.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse41.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE2 or COMPlus_EnableSSE2 is '0' but System.Runtime.Intrinsics.X86.Sse41.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse42.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE2 or COMPlus_EnableSSE2 is '0' but System.Runtime.Intrinsics.X86.Sse42.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse42.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE2 or COMPlus_EnableSSE2 is '0' but System.Runtime.Intrinsics.X86.Sse42.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE2 or COMPlus_EnableSSE2 is '0' but System.Runtime.Intrinsics.X86.Avx.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE2 or COMPlus_EnableSSE2 is '0' but System.Runtime.Intrinsics.X86.Avx.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx2.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE2 or COMPlus_EnableSSE2 is '0' but System.Runtime.Intrinsics.X86.Avx2.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx2.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE2 or COMPlus_EnableSSE2 is '0' but System.Runtime.Intrinsics.X86.Avx2.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.AvxVnni.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE2 or COMPlus_EnableSSE2 is '0' but System.Runtime.Intrinsics.X86.AvxVnni.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.AvxVnni.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE2 or COMPlus_EnableSSE2 is '0' but System.Runtime.Intrinsics.X86.AvxVnni.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Fma.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE2 or COMPlus_EnableSSE2 is '0' but System.Runtime.Intrinsics.X86.Fma.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Fma.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE2 or COMPlus_EnableSSE2 is '0' but System.Runtime.Intrinsics.X86.Fma.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Pclmulqdq.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE2 or COMPlus_EnableSSE2 is '0' but System.Runtime.Intrinsics.X86.Pclmulqdq.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Pclmulqdq.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE2 or COMPlus_EnableSSE2 is '0' but System.Runtime.Intrinsics.X86.Pclmulqdq.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Popcnt.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE2 or COMPlus_EnableSSE2 is '0' but System.Runtime.Intrinsics.X86.Popcnt.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Popcnt.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE2 or COMPlus_EnableSSE2 is '0' but System.Runtime.Intrinsics.X86.Popcnt.X64.IsSupported returns 'True'");
                }
            }

            if (String.Equals(Environment.GetEnvironmentVariable("DOTNET_EnableSSE3"), "0") || String.Equals(Environment.GetEnvironmentVariable("COMPlus_EnableSSE3"), "0"))
            {
                if (System.Runtime.Intrinsics.X86.Sse3.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE3 or COMPlus_EnableSSE3 is '0' but System.Runtime.Intrinsics.X86.Sse3.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse3.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE3 or COMPlus_EnableSSE3 is '0' but System.Runtime.Intrinsics.X86.Sse3.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Ssse3.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE3 or COMPlus_EnableSSE3 is '0' but System.Runtime.Intrinsics.X86.Ssse3.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Ssse3.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE3 or COMPlus_EnableSSE3 is '0' but System.Runtime.Intrinsics.X86.Ssse3.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse41.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE3 or COMPlus_EnableSSE3 is '0' but System.Runtime.Intrinsics.X86.Sse41.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse41.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE3 or COMPlus_EnableSSE3 is '0' but System.Runtime.Intrinsics.X86.Sse41.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse42.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE3 or COMPlus_EnableSSE3 is '0' but System.Runtime.Intrinsics.X86.Sse42.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse42.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE3 or COMPlus_EnableSSE3 is '0' but System.Runtime.Intrinsics.X86.Sse42.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE3 or COMPlus_EnableSSE3 is '0' but System.Runtime.Intrinsics.X86.Avx.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE3 or COMPlus_EnableSSE3 is '0' but System.Runtime.Intrinsics.X86.Avx.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx2.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE3 or COMPlus_EnableSSE3 is '0' but System.Runtime.Intrinsics.X86.Avx2.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx2.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE3 or COMPlus_EnableSSE3 is '0' but System.Runtime.Intrinsics.X86.Avx2.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.AvxVnni.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE3 or COMPlus_EnableSSE3 is '0' but System.Runtime.Intrinsics.X86.AvxVnni.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.AvxVnni.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE3 or COMPlus_EnableSSE3 is '0' but System.Runtime.Intrinsics.X86.AvxVnni.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Fma.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE3 or COMPlus_EnableSSE3 is '0' but System.Runtime.Intrinsics.X86.Fma.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Fma.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE3 or COMPlus_EnableSSE3 is '0' but System.Runtime.Intrinsics.X86.Fma.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Popcnt.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE3 or COMPlus_EnableSSE3 is '0' but System.Runtime.Intrinsics.X86.Popcnt.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Popcnt.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE3 or COMPlus_EnableSSE3 is '0' but System.Runtime.Intrinsics.X86.Popcnt.X64.IsSupported returns 'True'");
                }
            }

            if (String.Equals(Environment.GetEnvironmentVariable("DOTNET_EnableSSSE3"), "0") || String.Equals(Environment.GetEnvironmentVariable("COMPlus_EnableSSSE3"), "0"))
            {
                if (System.Runtime.Intrinsics.X86.Ssse3.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSSE3 or COMPlus_EnableSSSE3 is '0' but System.Runtime.Intrinsics.X86.Ssse3.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Ssse3.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSSE3 or COMPlus_EnableSSSE3 is '0' but System.Runtime.Intrinsics.X86.Ssse3.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse41.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSSE3 or COMPlus_EnableSSSE3 is '0' but System.Runtime.Intrinsics.X86.Sse41.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse41.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSSE3 or COMPlus_EnableSSSE3 is '0' but System.Runtime.Intrinsics.X86.Sse41.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse42.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSSE3 or COMPlus_EnableSSSE3 is '0' but System.Runtime.Intrinsics.X86.Sse42.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse42.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSSE3 or COMPlus_EnableSSSE3 is '0' but System.Runtime.Intrinsics.X86.Sse42.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSSE3 or COMPlus_EnableSSSE3 is '0' but System.Runtime.Intrinsics.X86.Avx.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSSE3 or COMPlus_EnableSSSE3 is '0' but System.Runtime.Intrinsics.X86.Avx.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx2.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSSE3 or COMPlus_EnableSSSE3 is '0' but System.Runtime.Intrinsics.X86.Avx2.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx2.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSSE3 or COMPlus_EnableSSSE3 is '0' but System.Runtime.Intrinsics.X86.Avx2.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.AvxVnni.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSSE3 or COMPlus_EnableSSSE3 is '0' but System.Runtime.Intrinsics.X86.AvxVnni.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.AvxVnni.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSSE3 or COMPlus_EnableSSSE3 is '0' but System.Runtime.Intrinsics.X86.AvxVnni.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Fma.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSSE3 or COMPlus_EnableSSSE3 is '0' but System.Runtime.Intrinsics.X86.Fma.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Fma.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSSE3 or COMPlus_EnableSSSE3 is '0' but System.Runtime.Intrinsics.X86.Fma.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Popcnt.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSSE3 or COMPlus_EnableSSSE3 is '0' but System.Runtime.Intrinsics.X86.Popcnt.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Popcnt.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSSE3 or COMPlus_EnableSSSE3 is '0' but System.Runtime.Intrinsics.X86.Popcnt.X64.IsSupported returns 'True'");
                }
            }

            if (String.Equals(Environment.GetEnvironmentVariable("DOTNET_EnableSSE41"), "0") || String.Equals(Environment.GetEnvironmentVariable("COMPlus_EnableSSE41"), "0"))
            {
                if (System.Runtime.Intrinsics.X86.Sse41.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE41 or COMPlus_EnableSSE41 is '0' but System.Runtime.Intrinsics.X86.Sse41.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse41.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE41 or COMPlus_EnableSSE41 is '0' but System.Runtime.Intrinsics.X86.Sse41.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse42.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE41 or COMPlus_EnableSSE41 is '0' but System.Runtime.Intrinsics.X86.Sse42.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse42.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE41 or COMPlus_EnableSSE41 is '0' but System.Runtime.Intrinsics.X86.Sse42.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE41 or COMPlus_EnableSSE41 is '0' but System.Runtime.Intrinsics.X86.Avx.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE41 or COMPlus_EnableSSE41 is '0' but System.Runtime.Intrinsics.X86.Avx.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx2.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE41 or COMPlus_EnableSSE41 is '0' but System.Runtime.Intrinsics.X86.Avx2.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx2.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE41 or COMPlus_EnableSSE41 is '0' but System.Runtime.Intrinsics.X86.Avx2.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.AvxVnni.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE41 or COMPlus_EnableSSE41 is '0' but System.Runtime.Intrinsics.X86.AvxVnni.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.AvxVnni.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE41 or COMPlus_EnableSSE41 is '0' but System.Runtime.Intrinsics.X86.AvxVnni.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Fma.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE41 or COMPlus_EnableSSE41 is '0' but System.Runtime.Intrinsics.X86.Fma.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Fma.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE41 or COMPlus_EnableSSE41 is '0' but System.Runtime.Intrinsics.X86.Fma.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Popcnt.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE41 or COMPlus_EnableSSE41 is '0' but System.Runtime.Intrinsics.X86.Popcnt.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Popcnt.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE41 or COMPlus_EnableSSE41 is '0' but System.Runtime.Intrinsics.X86.Popcnt.X64.IsSupported returns 'True'");
                }
            }

            if (String.Equals(Environment.GetEnvironmentVariable("DOTNET_EnableSSE42"), "0") || String.Equals(Environment.GetEnvironmentVariable("COMPlus_EnableSSE42"), "0"))
            {
                if (System.Runtime.Intrinsics.X86.Sse42.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE42 or COMPlus_EnableSSE42 is '0' but System.Runtime.Intrinsics.X86.Sse42.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Sse42.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE42 or COMPlus_EnableSSE42 is '0' but System.Runtime.Intrinsics.X86.Sse42.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE42 or COMPlus_EnableSSE42 is '0' but System.Runtime.Intrinsics.X86.Avx.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE42 or COMPlus_EnableSSE42 is '0' but System.Runtime.Intrinsics.X86.Avx.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx2.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE42 or COMPlus_EnableSSE42 is '0' but System.Runtime.Intrinsics.X86.Avx2.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx2.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE42 or COMPlus_EnableSSE42 is '0' but System.Runtime.Intrinsics.X86.Avx2.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.AvxVnni.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE42 or COMPlus_EnableSSE42 is '0' but System.Runtime.Intrinsics.X86.AvxVnni.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.AvxVnni.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE42 or COMPlus_EnableSSE42 is '0' but System.Runtime.Intrinsics.X86.AvxVnni.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Fma.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE42 or COMPlus_EnableSSE42 is '0' but System.Runtime.Intrinsics.X86.Fma.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Fma.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE42 or COMPlus_EnableSSE42 is '0' but System.Runtime.Intrinsics.X86.Fma.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Popcnt.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE42 or COMPlus_EnableSSE42 is '0' but System.Runtime.Intrinsics.X86.Popcnt.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Popcnt.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableSSE42 or COMPlus_EnableSSE42 is '0' but System.Runtime.Intrinsics.X86.Popcnt.X64.IsSupported returns 'True'");
                }
            }

            if (String.Equals(Environment.GetEnvironmentVariable("DOTNET_EnableAVX"), "0") || String.Equals(Environment.GetEnvironmentVariable("COMPlus_EnableAVX"), "0"))
            {
                if (System.Runtime.Intrinsics.X86.Avx.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableAVX or COMPlus_EnableAVX is '0' but System.Runtime.Intrinsics.X86.Avx.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableAVX or COMPlus_EnableAVX is '0' but System.Runtime.Intrinsics.X86.Avx.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx2.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableAVX or COMPlus_EnableAVX is '0' but System.Runtime.Intrinsics.X86.Avx2.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx2.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableAVX or COMPlus_EnableAVX is '0' but System.Runtime.Intrinsics.X86.Avx2.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.AvxVnni.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableAVX or COMPlus_EnableAVX is '0' but System.Runtime.Intrinsics.X86.AvxVnni.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.AvxVnni.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableAVX or COMPlus_EnableAVX is '0' but System.Runtime.Intrinsics.X86.AvxVnni.X64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Fma.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableAVX or COMPlus_EnableAVX is '0' but System.Runtime.Intrinsics.X86.Fma.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Fma.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableAVX or COMPlus_EnableAVX is '0' but System.Runtime.Intrinsics.X86.Fma.X64.IsSupported returns 'True'");
                }
            }

            if (String.Equals(Environment.GetEnvironmentVariable("DOTNET_EnableAVX2"), "0") || String.Equals(Environment.GetEnvironmentVariable("COMPlus_EnableAVX2"), "0"))
            {
                if (System.Runtime.Intrinsics.X86.Avx2.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableAVX2 or COMPlus_EnableAVX2 is '0' but System.Runtime.Intrinsics.X86.Avx2.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Avx2.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableAVX2 or COMPlus_EnableAVX2 is '0' but System.Runtime.Intrinsics.X86.Avx2.X64.IsSupported returns 'True'");
                }
            }

            if (String.Equals(Environment.GetEnvironmentVariable("DOTNET_EnableAVXVNNI"), "0") || String.Equals(Environment.GetEnvironmentVariable("COMPlus_EnableAVXVNNI"), "0"))
            {
                if (System.Runtime.Intrinsics.X86.AvxVnni.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableAVXVNNI or COMPlus_EnableAVXVNNI is '0' but System.Runtime.Intrinsics.X86.AvxVnni.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.AvxVnni.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableAVXVNNI or COMPlus_EnableAVXVNNI is '0' but System.Runtime.Intrinsics.X86.AvxVnni.X64.IsSupported returns 'True'");
                }
            }

            if (String.Equals(Environment.GetEnvironmentVariable("DOTNET_EnableBMI1"), "0") || String.Equals(Environment.GetEnvironmentVariable("COMPlus_EnableBMI1"), "0"))
            {
                if (System.Runtime.Intrinsics.X86.Bmi1.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableBMI1 or COMPlus_EnableBMI1 is '0' but System.Runtime.Intrinsics.X86.Bmi1.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Bmi1.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableBMI1 or COMPlus_EnableBMI1 is '0' but System.Runtime.Intrinsics.X86.Bmi1.X64.IsSupported returns 'True'");
                }
            }

            if (String.Equals(Environment.GetEnvironmentVariable("DOTNET_EnableBMI2"), "0") || String.Equals(Environment.GetEnvironmentVariable("COMPlus_EnableBMI2"), "0"))
            {
                if (System.Runtime.Intrinsics.X86.Bmi2.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableBMI2 or COMPlus_EnableBMI2 is '0' but System.Runtime.Intrinsics.X86.Bmi2.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Bmi2.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableBMI2 or COMPlus_EnableBMI2 is '0' but System.Runtime.Intrinsics.X86.Bmi2.X64.IsSupported returns 'True'");
                }
            }

            if (String.Equals(Environment.GetEnvironmentVariable("DOTNET_EnableFMA"), "0") || String.Equals(Environment.GetEnvironmentVariable("COMPlus_EnableFMA"), "0"))
            {
                if (System.Runtime.Intrinsics.X86.Fma.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableFMA or COMPlus_EnableFMA is '0' but System.Runtime.Intrinsics.X86.Fma.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Fma.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableFMA or COMPlus_EnableFMA is '0' but System.Runtime.Intrinsics.X86.Fma.X64.IsSupported returns 'True'");
                }
            }

            if (String.Equals(Environment.GetEnvironmentVariable("DOTNET_EnableLZCNT"), "0") || String.Equals(Environment.GetEnvironmentVariable("COMPlus_EnableLZCNT"), "0"))
            {
                if (System.Runtime.Intrinsics.X86.Lzcnt.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableLZCNT or COMPlus_EnableLZCNT is '0' but System.Runtime.Intrinsics.X86.Lzcnt.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Lzcnt.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableLZCNT or COMPlus_EnableLZCNT is '0' but System.Runtime.Intrinsics.X86.Lzcnt.X64.IsSupported returns 'True'");
                }
            }

            if (String.Equals(Environment.GetEnvironmentVariable("DOTNET_EnablePCLMULQDQ"), "0") || String.Equals(Environment.GetEnvironmentVariable("COMPlus_EnablePCLMULQDQ"), "0"))
            {
                if (System.Runtime.Intrinsics.X86.Pclmulqdq.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnablePCLMULQDQ or COMPlus_EnablePCLMULQDQ is '0' but System.Runtime.Intrinsics.X86.Pclmulqdq.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Pclmulqdq.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnablePCLMULQDQ or COMPlus_EnablePCLMULQDQ is '0' but System.Runtime.Intrinsics.X86.Pclmulqdq.X64.IsSupported returns 'True'");
                }
            }

            if (String.Equals(Environment.GetEnvironmentVariable("DOTNET_EnablePOPCNT"), "0") || String.Equals(Environment.GetEnvironmentVariable("COMPlus_EnablePOPCNT"), "0"))
            {
                if (System.Runtime.Intrinsics.X86.Popcnt.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnablePOPCNT or COMPlus_EnablePOPCNT is '0' but System.Runtime.Intrinsics.X86.Popcnt.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.X86.Popcnt.X64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnablePOPCNT or COMPlus_EnablePOPCNT is '0' but System.Runtime.Intrinsics.X86.Popcnt.X64.IsSupported returns 'True'");
                }
            }

            if (String.Equals(Environment.GetEnvironmentVariable("DOTNET_EnableArm64AdvSimd"), "0") || String.Equals(Environment.GetEnvironmentVariable("COMPlus_EnableArm64AdvSimd"), "0"))
            {
                if (System.Runtime.Intrinsics.Arm.AdvSimd.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableArm64AdvSimd or COMPlus_EnableArm64AdvSimd is '0' but System.Runtime.Intrinsics.Arm.AdvSimd.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableArm64AdvSimd or COMPlus_EnableArm64AdvSimd is '0' but System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.Arm.Dp.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableArm64AdvSimd or COMPlus_EnableArm64AdvSimd is '0' but System.Runtime.Intrinsics.Arm.Dp.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.Arm.Dp.Arm64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableArm64AdvSimd or COMPlus_EnableArm64AdvSimd is '0' but System.Runtime.Intrinsics.Arm.Dp.Arm64.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.Arm.Rdm.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableArm64AdvSimd or COMPlus_EnableArm64AdvSimd is '0' but System.Runtime.Intrinsics.Arm.Rdm.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.Arm.Rdm.Arm64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableArm64AdvSimd or COMPlus_EnableArm64AdvSimd is '0' but System.Runtime.Intrinsics.Arm.Rdm.Arm64.IsSupported returns 'True'");
                }
            }

            if (String.Equals(Environment.GetEnvironmentVariable("DOTNET_EnableArm64Aes"), "0") || String.Equals(Environment.GetEnvironmentVariable("COMPlus_EnableArm64Aes"), "0"))
            {
                if (System.Runtime.Intrinsics.Arm.Aes.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableArm64Aes or COMPlus_EnableArm64Aes is '0' but System.Runtime.Intrinsics.Arm.Aes.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.Arm.Aes.Arm64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableArm64Aes or COMPlus_EnableArm64Aes is '0' but System.Runtime.Intrinsics.Arm.Aes.Arm64.IsSupported returns 'True'");
                }
            }

            if (String.Equals(Environment.GetEnvironmentVariable("DOTNET_(EnableArm64Crc32"), "0") || String.Equals(Environment.GetEnvironmentVariable("COMPlus_(EnableArm64Crc32"), "0"))
            {
                if (System.Runtime.Intrinsics.Arm.Crc32.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_(EnableArm64Crc32 or COMPlus_(EnableArm64Crc32 is '0' but System.Runtime.Intrinsics.Arm.Crc32.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.Arm.Crc32.Arm64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_(EnableArm64Crc32 or COMPlus_(EnableArm64Crc32 is '0' but System.Runtime.Intrinsics.Arm.Crc32.Arm64.IsSupported returns 'True'");
                }
            }

            if (String.Equals(Environment.GetEnvironmentVariable("DOTNET_EnableArm64Dp"), "0") || String.Equals(Environment.GetEnvironmentVariable("COMPlus_EnableArm64Dp"), "0"))
            {
                if (System.Runtime.Intrinsics.Arm.Dp.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableArm64Dp or COMPlus_EnableArm64Dp is '0' but System.Runtime.Intrinsics.Arm.Dp.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.Arm.Dp.Arm64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableArm64Dp or COMPlus_EnableArm64Dp is '0' but System.Runtime.Intrinsics.Arm.Dp.Arm64.IsSupported returns 'True'");
                }
            }

            if (String.Equals(Environment.GetEnvironmentVariable("DOTNET_EnableArm64AdvSimd_v81"), "0") || String.Equals(Environment.GetEnvironmentVariable("COMPlus_EnableArm64AdvSimd_v81"), "0"))
            {
                if (System.Runtime.Intrinsics.Arm.Rdm.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableArm64AdvSimd_v81 or COMPlus_EnableArm64AdvSimd_v81 is '0' but System.Runtime.Intrinsics.Arm.Rdm.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.Arm.Rdm.Arm64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableArm64AdvSimd_v81 or COMPlus_EnableArm64AdvSimd_v81 is '0' but System.Runtime.Intrinsics.Arm.Rdm.Arm64.IsSupported returns 'True'");
                }
            }

            if (String.Equals(Environment.GetEnvironmentVariable("DOTNET_EnableArm64Sha1"), "0") || String.Equals(Environment.GetEnvironmentVariable("COMPlus_EnableArm64Sha1"), "0"))
            {
                if (System.Runtime.Intrinsics.Arm.Sha1.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableArm64Sha1 or COMPlus_EnableArm64Sha1 is '0' but System.Runtime.Intrinsics.Arm.Sha1.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.Arm.Sha1.Arm64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableArm64Sha1 or COMPlus_EnableArm64Sha1 is '0' but System.Runtime.Intrinsics.Arm.Sha1.Arm64.IsSupported returns 'True'");
                }
            }

            if (String.Equals(Environment.GetEnvironmentVariable("DOTNET_EnableArm64Sha256"), "0") || String.Equals(Environment.GetEnvironmentVariable("COMPlus_EnableArm64Sha256"), "0"))
            {
                if (System.Runtime.Intrinsics.Arm.Sha256.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableArm64Sha256 or COMPlus_EnableArm64Sha256 is '0' but System.Runtime.Intrinsics.Arm.Sha256.IsSupported returns 'True'");
                }

                if (System.Runtime.Intrinsics.Arm.Sha256.Arm64.IsSupported)
                {
                    success = false;
                    Console.WriteLine("ERROR: Either DOTNET_EnableArm64Sha256 or COMPlus_EnableArm64Sha256 is '0' but System.Runtime.Intrinsics.Arm.Sha256.Arm64.IsSupported returns 'True'");
                }
            }

            return success;
        }

        static bool TestReadyToRunAssumptionsAreCorrect()
        {
            bool success = true;

            if (System.Runtime.Intrinsics.X86.X86Base.IsSupported != Helper_System_Runtime_Intrinsics_X86_X86Base.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.X86Base.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.X86Base.IsSupported, Helper_System_Runtime_Intrinsics_X86_X86Base.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.X86Base.X64.IsSupported != Helper_System_Runtime_Intrinsics_X86_X86Base_X64.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.X86Base.X64.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.X86Base.X64.IsSupported, Helper_System_Runtime_Intrinsics_X86_X86Base_X64.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Sse.IsSupported != Helper_System_Runtime_Intrinsics_X86_Sse.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Sse.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Sse.IsSupported, Helper_System_Runtime_Intrinsics_X86_Sse.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Sse.X64.IsSupported != Helper_System_Runtime_Intrinsics_X86_Sse_X64.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Sse.X64.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Sse.X64.IsSupported, Helper_System_Runtime_Intrinsics_X86_Sse_X64.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Sse2.IsSupported != Helper_System_Runtime_Intrinsics_X86_Sse2.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Sse2.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Sse2.IsSupported, Helper_System_Runtime_Intrinsics_X86_Sse2.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Sse2.X64.IsSupported != Helper_System_Runtime_Intrinsics_X86_Sse2_X64.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Sse2.X64.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Sse2.X64.IsSupported, Helper_System_Runtime_Intrinsics_X86_Sse2_X64.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Sse3.IsSupported != Helper_System_Runtime_Intrinsics_X86_Sse3.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Sse3.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Sse3.IsSupported, Helper_System_Runtime_Intrinsics_X86_Sse3.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Sse3.X64.IsSupported != Helper_System_Runtime_Intrinsics_X86_Sse3_X64.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Sse3.X64.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Sse3.X64.IsSupported, Helper_System_Runtime_Intrinsics_X86_Sse3_X64.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Ssse3.IsSupported != Helper_System_Runtime_Intrinsics_X86_Ssse3.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Ssse3.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Ssse3.IsSupported, Helper_System_Runtime_Intrinsics_X86_Ssse3.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Ssse3.X64.IsSupported != Helper_System_Runtime_Intrinsics_X86_Ssse3_X64.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Ssse3.X64.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Ssse3.X64.IsSupported, Helper_System_Runtime_Intrinsics_X86_Ssse3_X64.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Sse41.IsSupported != Helper_System_Runtime_Intrinsics_X86_Sse41.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Sse41.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Sse41.IsSupported, Helper_System_Runtime_Intrinsics_X86_Sse41.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Sse41.X64.IsSupported != Helper_System_Runtime_Intrinsics_X86_Sse41_X64.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Sse41.X64.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Sse41.X64.IsSupported, Helper_System_Runtime_Intrinsics_X86_Sse41_X64.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Sse42.IsSupported != Helper_System_Runtime_Intrinsics_X86_Sse42.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Sse42.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Sse42.IsSupported, Helper_System_Runtime_Intrinsics_X86_Sse42.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Sse42.X64.IsSupported != Helper_System_Runtime_Intrinsics_X86_Sse42_X64.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Sse42.X64.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Sse42.X64.IsSupported, Helper_System_Runtime_Intrinsics_X86_Sse42_X64.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Avx.IsSupported != Helper_System_Runtime_Intrinsics_X86_Avx.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Avx.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Avx.IsSupported, Helper_System_Runtime_Intrinsics_X86_Avx.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Avx.X64.IsSupported != Helper_System_Runtime_Intrinsics_X86_Avx_X64.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Avx.X64.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Avx.X64.IsSupported, Helper_System_Runtime_Intrinsics_X86_Avx_X64.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Avx2.IsSupported != Helper_System_Runtime_Intrinsics_X86_Avx2.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Avx2.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Avx2.IsSupported, Helper_System_Runtime_Intrinsics_X86_Avx2.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Avx2.X64.IsSupported != Helper_System_Runtime_Intrinsics_X86_Avx2_X64.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Avx2.X64.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Avx2.X64.IsSupported, Helper_System_Runtime_Intrinsics_X86_Avx2_X64.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.AvxVnni.IsSupported != Helper_System_Runtime_Intrinsics_X86_AvxVnni.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.AvxVnni.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.AvxVnni.IsSupported, Helper_System_Runtime_Intrinsics_X86_AvxVnni.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.AvxVnni.X64.IsSupported != Helper_System_Runtime_Intrinsics_X86_AvxVnni_X64.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.AvxVnni.X64.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.AvxVnni.X64.IsSupported, Helper_System_Runtime_Intrinsics_X86_AvxVnni_X64.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Bmi1.IsSupported != Helper_System_Runtime_Intrinsics_X86_Bmi1.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Bmi1.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Bmi1.IsSupported, Helper_System_Runtime_Intrinsics_X86_Bmi1.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Bmi1.X64.IsSupported != Helper_System_Runtime_Intrinsics_X86_Bmi1_X64.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Bmi1.X64.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Bmi1.X64.IsSupported, Helper_System_Runtime_Intrinsics_X86_Bmi1_X64.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Bmi2.IsSupported != Helper_System_Runtime_Intrinsics_X86_Bmi2.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Bmi2.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Bmi2.IsSupported, Helper_System_Runtime_Intrinsics_X86_Bmi2.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Bmi2.X64.IsSupported != Helper_System_Runtime_Intrinsics_X86_Bmi2_X64.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Bmi2.X64.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Bmi2.X64.IsSupported, Helper_System_Runtime_Intrinsics_X86_Bmi2_X64.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Fma.IsSupported != Helper_System_Runtime_Intrinsics_X86_Fma.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Fma.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Fma.IsSupported, Helper_System_Runtime_Intrinsics_X86_Fma.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Fma.X64.IsSupported != Helper_System_Runtime_Intrinsics_X86_Fma_X64.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Fma.X64.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Fma.X64.IsSupported, Helper_System_Runtime_Intrinsics_X86_Fma_X64.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Lzcnt.IsSupported != Helper_System_Runtime_Intrinsics_X86_Lzcnt.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Lzcnt.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Lzcnt.IsSupported, Helper_System_Runtime_Intrinsics_X86_Lzcnt.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Lzcnt.X64.IsSupported != Helper_System_Runtime_Intrinsics_X86_Lzcnt_X64.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Lzcnt.X64.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Lzcnt.X64.IsSupported, Helper_System_Runtime_Intrinsics_X86_Lzcnt_X64.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Pclmulqdq.IsSupported != Helper_System_Runtime_Intrinsics_X86_Pclmulqdq.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Pclmulqdq.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Pclmulqdq.IsSupported, Helper_System_Runtime_Intrinsics_X86_Pclmulqdq.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Pclmulqdq.X64.IsSupported != Helper_System_Runtime_Intrinsics_X86_Pclmulqdq_X64.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Pclmulqdq.X64.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Pclmulqdq.X64.IsSupported, Helper_System_Runtime_Intrinsics_X86_Pclmulqdq_X64.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Popcnt.IsSupported != Helper_System_Runtime_Intrinsics_X86_Popcnt.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Popcnt.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Popcnt.IsSupported, Helper_System_Runtime_Intrinsics_X86_Popcnt.IsSupported);
            }

            if (System.Runtime.Intrinsics.X86.Popcnt.X64.IsSupported != Helper_System_Runtime_Intrinsics_X86_Popcnt_X64.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.X86.Popcnt.X64.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.X86.Popcnt.X64.IsSupported, Helper_System_Runtime_Intrinsics_X86_Popcnt_X64.IsSupported);
            }

            if (System.Runtime.Intrinsics.Arm.ArmBase.IsSupported != Helper_System_Runtime_Intrinsics_Arm_ArmBase.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.Arm.ArmBase.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.Arm.ArmBase.IsSupported, Helper_System_Runtime_Intrinsics_Arm_ArmBase.IsSupported);
            }

            if (System.Runtime.Intrinsics.Arm.ArmBase.Arm64.IsSupported != Helper_System_Runtime_Intrinsics_Arm_ArmBase_Arm64.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.Arm.ArmBase.Arm64.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.Arm.ArmBase.Arm64.IsSupported, Helper_System_Runtime_Intrinsics_Arm_ArmBase_Arm64.IsSupported);
            }

            if (System.Runtime.Intrinsics.Arm.AdvSimd.IsSupported != Helper_System_Runtime_Intrinsics_Arm_AdvSimd.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.Arm.AdvSimd.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.Arm.AdvSimd.IsSupported, Helper_System_Runtime_Intrinsics_Arm_AdvSimd.IsSupported);
            }

            if (System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported != Helper_System_Runtime_Intrinsics_Arm_AdvSimd_Arm64.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.Arm.AdvSimd.Arm64.IsSupported, Helper_System_Runtime_Intrinsics_Arm_AdvSimd_Arm64.IsSupported);
            }

            if (System.Runtime.Intrinsics.Arm.Aes.IsSupported != Helper_System_Runtime_Intrinsics_Arm_Aes.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.Arm.Aes.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.Arm.Aes.IsSupported, Helper_System_Runtime_Intrinsics_Arm_Aes.IsSupported);
            }

            if (System.Runtime.Intrinsics.Arm.Aes.Arm64.IsSupported != Helper_System_Runtime_Intrinsics_Arm_Aes_Arm64.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.Arm.Aes.Arm64.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.Arm.Aes.Arm64.IsSupported, Helper_System_Runtime_Intrinsics_Arm_Aes_Arm64.IsSupported);
            }

            if (System.Runtime.Intrinsics.Arm.Crc32.IsSupported != Helper_System_Runtime_Intrinsics_Arm_Crc32.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.Arm.Crc32.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.Arm.Crc32.IsSupported, Helper_System_Runtime_Intrinsics_Arm_Crc32.IsSupported);
            }

            if (System.Runtime.Intrinsics.Arm.Crc32.Arm64.IsSupported != Helper_System_Runtime_Intrinsics_Arm_Crc32_Arm64.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.Arm.Crc32.Arm64.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.Arm.Crc32.Arm64.IsSupported, Helper_System_Runtime_Intrinsics_Arm_Crc32_Arm64.IsSupported);
            }

            if (System.Runtime.Intrinsics.Arm.Dp.IsSupported != Helper_System_Runtime_Intrinsics_Arm_Dp.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.Arm.Dp.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.Arm.Dp.IsSupported, Helper_System_Runtime_Intrinsics_Arm_Dp.IsSupported);
            }

            if (System.Runtime.Intrinsics.Arm.Dp.Arm64.IsSupported != Helper_System_Runtime_Intrinsics_Arm_Dp_Arm64.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.Arm.Dp.Arm64.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.Arm.Dp.Arm64.IsSupported, Helper_System_Runtime_Intrinsics_Arm_Dp_Arm64.IsSupported);
            }

            if (System.Runtime.Intrinsics.Arm.Rdm.IsSupported != Helper_System_Runtime_Intrinsics_Arm_Rdm.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.Arm.Rdm.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.Arm.Rdm.IsSupported, Helper_System_Runtime_Intrinsics_Arm_Rdm.IsSupported);
            }

            if (System.Runtime.Intrinsics.Arm.Rdm.Arm64.IsSupported != Helper_System_Runtime_Intrinsics_Arm_Rdm_Arm64.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.Arm.Rdm.Arm64.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.Arm.Rdm.Arm64.IsSupported, Helper_System_Runtime_Intrinsics_Arm_Rdm_Arm64.IsSupported);
            }

            if (System.Runtime.Intrinsics.Arm.Sha1.IsSupported != Helper_System_Runtime_Intrinsics_Arm_Sha1.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.Arm.Sha1.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.Arm.Sha1.IsSupported, Helper_System_Runtime_Intrinsics_Arm_Sha1.IsSupported);
            }

            if (System.Runtime.Intrinsics.Arm.Sha1.Arm64.IsSupported != Helper_System_Runtime_Intrinsics_Arm_Sha1_Arm64.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.Arm.Sha1.Arm64.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.Arm.Sha1.Arm64.IsSupported, Helper_System_Runtime_Intrinsics_Arm_Sha1_Arm64.IsSupported);
            }

            if (System.Runtime.Intrinsics.Arm.Sha256.IsSupported != Helper_System_Runtime_Intrinsics_Arm_Sha256.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.Arm.Sha256.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.Arm.Sha256.IsSupported, Helper_System_Runtime_Intrinsics_Arm_Sha256.IsSupported);
            }

            if (System.Runtime.Intrinsics.Arm.Sha256.Arm64.IsSupported != Helper_System_Runtime_Intrinsics_Arm_Sha256_Arm64.IsSupported)
            {
                success = false;
                Console.WriteLine("ERROR: System.Runtime.Intrinsics.Arm.Sha256.Arm64.IsSupported returns '{0}' while a loaded into the process ReadyToRun image assumes the value being '{1}'", System.Runtime.Intrinsics.Arm.Sha256.Arm64.IsSupported, Helper_System_Runtime_Intrinsics_Arm_Sha256_Arm64.IsSupported);
            }

            return success;
        }
    }
}
