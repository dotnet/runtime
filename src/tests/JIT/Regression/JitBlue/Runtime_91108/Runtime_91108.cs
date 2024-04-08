// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Found by Antigen

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class TestClass
{
    public struct S1
    {
        public decimal decimal_0;
    }
    public struct S2
    {
        public long long_1;
    }
    static byte s_byte_3 = 4;
    static int s_int_8 = 50;
    static long s_long_9 = -2;
    static float s_float_11 = 0.4f;
    static S2 s_s2_17 = new S2();
    int int_24 = 0;
    long long_25 = 5;
    sbyte sbyte_26 = 1;
    S2 s2_33 = new S2();
    static int s_loopInvariant = 4;
    private int LeafMethod6()
    {
        unchecked
        {
            return ((int)(((int)(s_int_8 = ((int)(s_int_8 &= ((int)(s_int_8 % ((int)((s_int_8) | 49)))))))) ^ ((int)(s_int_8 & ((int)(s_int_8 += ((int)(int_24 &= s_int_8))))))));
        }
    }
    private S2 Method22(ref S2 p_s2_543, S2 p_s2_544, ref float p_float_545, out int p_int_546, ref S2 p_s2_547)
    {
        unchecked
        {
            int int_554 = -1;
            p_int_546 = ((int)(((int)(int_554 |= ((int)(((int)(int_554 |= -1)) + ((int)(int_24 >>= s_int_8)))))) / ((int)((((int)(((int)(((int)(s_int_8 / ((int)((int_24) | 46)))) & int_24)) | ((int)(((int)(int_554 % ((int)((int_554) | 16)))) << ((int)(int_554 % ((int)((int_24) | 52))))))))) | 26))));
            int __loopvar0 = s_loopInvariant - 7;
            int __loopvar3 = s_loopInvariant - 15;
            for (;;)
            {
                if (__loopvar3 > s_loopInvariant + 4)
                    break;
                int __loopvar2 = s_loopInvariant;
                for (; (__loopvar2 > s_loopInvariant - 4); __loopvar2--)
                {
                    try
                    {
                        s_byte_3 -= s_byte_3;
                    }
                    finally
                    {
                        sbyte_26 <<= ((int)(LeafMethod6() << ((int)(s_int_8 + ((int)(s_int_8 = ((int)(p_int_546 -= p_int_546))))))));
                        ++__loopvar3;
                    }
                    long long_565 = ((long)(((long)(((long)(((long)(s_long_9 + long_25)) << ((int)(s_int_8 % ((int)((int_24) | 22)))))) >> ((int)(((int)(int_24 + LeafMethod6())) + ((int)(int_24 % ((int)((s_int_8) | 81)))))))) << int_554));
                }
            }
            return s2_33;
        }
    }
    private void Method0()
    {
        unchecked
        {
            int int_2518 = 50;
            s_s2_17 = Method22(ref s2_33, s_s2_17, ref s_float_11, out int_2518, ref s_s2_17);
        }
    }
    [Fact]
    public static void TestEntryPoint()
    {
        new TestClass().Method0();
    }
}
/*
Environment:

set COMPlus_TieredCompilation=0

Jump into the middle of try region: BB07 branches to BB06

Assert failure(PID 38788 [0x00009784], Thread: 44976 [0xafb0]): Assertion failed '!"Jump into middle of try region"' in 'TestClass:Method22(byref,TestClass+S2,byref,byref,byref):TestClass+S2:this' during 'Find loops' (IL size 287; hash 0xf1c30f9f; FullOpts)

    File: C:\gh\runtime\src\coreclr\jit\fgdiagnostic.cpp Line: 2624
*/
