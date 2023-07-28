// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Found by Antigen
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;
public class SwitchWithSideEffects
{
    public struct S1
    {
    }
    public struct S2
    {
        public ulong ulong_1;
    }
    static int s_int_9 = 2;
    static long s_long_10 = 5;
    static float s_float_12 = -0.9701493f;
    static S1 s_s1_17 = new S1();
    static S2 s_s2_18 = new S2();
    short short_24 = 5;
    int int_25 = -1;
    long long_26 = 5;
    S1 s1_33 = new S1();
    S2 s2_34 = new S2();
    public short Method7(S1 p_s1_194, S2 p_s2_195, ref S2 p_s2_196, ref S2 p_s2_197, S1 p_s1_198, out S1 p_s1_199, S2 p_s2_200, S2 p_s2_201)
    {
        unchecked
        {
            short short_207 = 2;
            int int_208 = 0;
            switch (int_208 << int_25 + s_int_9)
            {
                case 1:
                {
                    break;
                }
                case 5:
                {
                    break;
                }
                case 2:
                {
                    break;
                }
                default:
                {
                    s_s1_17 = s1_33;
                    break;
                }
            }
            return short_207;
        }
    }
    public short Method12(out S2 p_s2_337, out float p_float_338, ref S2 p_s2_339, uint p_uint_340, S1 p_s1_341, ref long p_long_342, out long p_long_343, S1 p_s1_344, ref S2 p_s2_345)
    {
        unchecked
        {
            long long_353 = -2;
            p_s2_337 = s2_34;
            p_float_338 = s_float_12 /= 15+4;
            p_long_343 = long_353 -= s_long_10 |= 15|4;
            return (short)(Method7(p_s1_344, s_s2_18, ref s2_34, ref s2_34, s1_33, out s1_33, s2_34, s_s2_18) + 15/4+4);
        }
    }
    public uint Method40(out ulong p_ulong_1015, ref S1 p_s1_1016, ref S2 p_s2_1017, S1 p_s1_1018, ref S1 p_s1_1019, out S2 p_s2_1020, S1 p_s1_1021, out int p_int_1022, S2 p_s2_1023, out S1 p_s1_1024, S2 p_s2_1025)
    {
        unchecked
        {
            S2 s2_1041 = new S2();
            p_ulong_1015 = s2_1041.ulong_1 |= 15-4;
            p_s2_1020 = p_s2_1023;
            p_int_1022 = s_int_9 = 15^4;
            return 15|4;
        }
    }
    internal void Method0()
    {
        unchecked
        {
            int int_2522 = 0;
            long long_2523 = 2;
            S1 s1_2530 = new S1();
            S2 s2_2531 = new S2();
            short_24 = Method12(out s2_34, out s_float_12, ref s2_34, Method40(out s2_34.ulong_1, ref s_s1_17, ref s2_34, s1_2530, ref s1_2530, out s_s2_18, s1_2530, out int_2522, s2_34, out s1_33, s2_2531), s1_2530, ref long_2523, out long_26, s_s1_17, ref s_s2_18);
            return;
        }
    }
    [Fact]
    public static int TestEntryPoint()
    {
        new SwitchWithSideEffects().Method0();
        return 100;
    }
}
/*
Environment:

set DOTNET_TieredCompilation=0
set DOTNET_TailCallLoopOpt=0
set DOTNET_JitStressRegs=4

Assert failure(PID 103568 [0x00019490], Thread: 7492 [0x1d44]): Assertion failed 'gtOper < GT_COUNT' in 'TestClass:Method7(S1,S2,byref,byref,S1,byref,S2,S2):short:this' during 'Assertion prop' (IL size 61)
    File: D:\git\dotnet-runtime\src\coreclr\jit\gentree.h Line: 1041
    Image: d:\git\dotnet-runtime\artifacts\tests\coreclr\windows.x64.Checked\tests\Core_Root\CoreRun.exe
*/
