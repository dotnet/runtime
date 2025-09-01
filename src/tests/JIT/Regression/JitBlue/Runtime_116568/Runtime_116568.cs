// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Assert failure(PID 7200 [0x00001c20], Thread: 19780 [0x4d44]): Assertion failed 'false && "found use of a node that is not in the LIR sequence"' in 'Runtime_114572:Main()' during 'Lowering nodeinfo' (IL size 138; hash 0x86f53552; FullOpts)

//     File: C:\repos\runtime1\src\coreclr\jit\lir.cpp:1687
//     Image: c:\repos\runtime1\artifacts\tests\coreclr\windows.x64.Checked\tests\core_root\corerun.exe

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Runtime_116568
{
    public static Vector256<ushort> s_2;
    public static ushort s_4;
    public static byte s_ub = 1;
    public static ushort s_us = 1;
    private struct LocalStruct
    {
        public int Value;
    }

    private static int CalculateChecksum(int initialChecksum)
    {
        // Simple checksum calculation
        int checksum = initialChecksum;
        for (int i = 0; i < 5; i++)
        {
            checksum += i * (i + 1);
        }
        return checksum;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        CalculateChecksum(0);

        if (Avx512F.VL.IsSupported)
        {
            var vr11 = Vector256.Create<ushort>(0);
            var vr12 = Vector256.Create<ushort>(1);
            var vr13 = (ushort)0;
            var vr14 = Vector256.CreateScalar(vr13);
            var vr15 = (ushort)1;
            var vr16 = Vector256.CreateScalar(vr15);
            var vr17 = Vector256.Create<ushort>(s_4);
            var vr18 = Avx2.Max(vr16, vr17);

            for (int i = 0; i < 3; i++)
            {
                s_2 = Avx512F.VL.TernaryLogic(vr11, vr12, Avx512BW.VL.CompareGreaterThanOrEqual(vr14, vr18), 216);
                Console.WriteLine(s_2);

                if (i == 1)
                {
                    Console.WriteLine("Redundant branch: i == 1");
                }

                CalculateChecksum(123);
            }
        }
    }
}
