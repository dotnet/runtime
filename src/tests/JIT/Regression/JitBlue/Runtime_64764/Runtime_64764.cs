// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adaptated from bug:
// Found by Antigen
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;
public class TestClass
{
    public struct S1
    {
    }
    public struct S2
    {
        public ulong ulong_1;
    }
    static bool s_bool_2 = false;
    static byte s_byte_3 = 5;
    static int s_int_8 = 2;
    static S1 s_s1_16 = new S1();
    static S2 s_s2_17 = new S2();
    int int_24 = -2;
    sbyte sbyte_26 = 10;
    S2 s2_33 = new S2();
    static int s_loopInvariant = 4;
    public bool LeafMethod0()
    {
        unchecked
        {
            return 15>=4&& s_bool_2;
        }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int LeafMethod6()
    {
        unchecked
        {
            return s_int_8 >>= int_24;
        }
    }
    public uint Method1(out S2 p_s2_34, out S1 p_s1_35, S1 p_s1_36, out byte p_byte_37, S1 p_s1_38, ref ulong p_ulong_39)
    {
        unchecked
        {
            bool bool_40 = false;
            char char_42 = 'J';
            sbyte sbyte_48 = 1;
            p_s2_34 = s2_33;
            p_s1_35 = s_s1_16;
            p_byte_37 = s_byte_3 = s_byte_3 <<= 15^4;
            int __loopvar43 = 15-4, __loopSecondaryVar43_0 = s_loopInvariant;
            for (; s_int_8 < 15%4; s_int_8++)
            {
                if ((sbyte_26 += 15-4)< sbyte_48)
                {
                    switch (char_42 = char_42 = char_42)
                    {
                        case 'K':
                        {
                            break;
                        }
                        case 'I':
                        {
                            if (LeafMethod0())
                            {
                                int __loopvar9 = s_loopInvariant;
                                while (bool_40)
                                {
                                    if (bool_40 = s_bool_2 = (int_24 %= 15+4)> (int_24 &= LeafMethod6()))
                                    {
                                    }
                                    else
                                    {
                                    }
                                    try
                                    {
                                        int __loopvar8 = s_loopInvariant, __loopSecondaryVar8_0 = 15+4;
                                    }
                                    catch (System.TimeZoneNotFoundException)
                                    {
                                    }
                                }
                            }
                            else
                            {
                            }
                            break;
                        }
                        case 'A':
                        {
                            break;
                        }
                        case 'C':
                        {
                            break;
                        }
                        case 'M':
                        {
                            break;
                        }
                        case '2':
                        {
                            break;
                        }
                        default:
                        {
                            break;
                        }
                    }
                }
                else
                {
                }
            }
            return 15|4;
        }
    }
    internal void Method0()
    {
        unchecked
        {
            uint uint_99 = 5;
            S1 s1_101 = new S1();
            uint_99 = Method1(out s_s2_17, out s1_101, s_s1_16, out s_byte_3, s_s1_16, ref s2_33.ulong_1);
            return;
        }
    }
    [Fact]
    public static int TestEntryPoint()
    {
        new TestClass().Method0();

        return 100;
    }
}
/*
set DOTNET_TieredCompilation=0
set DOTNET_JitDoCopyProp=1
set DOTNET_EnableSSE41=1
set DOTNET_JitStress=2
set DOTNET_GCStress=0xC
set DOTNET_AltJitName=clrjit_win_x86_x64.dll
set DOTNET_AltJit=Method1

Assert failure(PID 198288 [0x00030690], Thread: 232112 [0x38ab0]): Assertion failed 'fgReachable(begBlk, endBlk)' in 'TestClass:Method1(byref,byref,S1,byref,S1,byref):int:this' during 'Update flow graph opt pass' (IL size 270)
    File: D:\git\dotnet-runtime\src\coreclr\jit\optimizer.cpp Line: 167
    Image: d:\git\dotnet-runtime\artifacts\tests\coreclr\windows.x64.Checked\tests\Core_Root\CoreRun.exe

Reduced repo (x86 VM):

set DOTNET_TieredCompilation=0
set DOTNET_JitStressModeNames=STRESS_BB_PROFILE
*/
