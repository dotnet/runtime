// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Found by Antigen

// On ARM64, Test.Method1 has a "type 5" frame that when compiled
// via OSR, requires a larger than normal initial SP adjust by its funclet.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class LargeFuncletFrame
{
    public struct S1
    {
        public struct S1_D1_F1
        {
            public decimal decimal_0;
            public long long_1;
            public uint uint_2;
        }
        public struct S1_D1_F2
        {
            public S1.S1_D1_F1 s1_s1_d1_f1_3;
            public S1.S1_D1_F1 s1_s1_d1_f1_4;
        }
        public sbyte sbyte_5;
    }
    public struct S2
    {
        public struct S2_D1_F1
        {
            public double double_7;
        }
    }
    static int s_int_15 = -1;
    static ulong s_ulong_22 = 2;
    static S1.S1_D1_F2 s_s1_s1_d1_f2_24 = new S1.S1_D1_F2();
    static S1 s_s1_25 = new S1();
    static S2.S2_D1_F1 s_s2_s2_d1_f1_26 = new S2.S2_D1_F1();
    static S2 s_s2_27 = new S2();
    char char_30 = '8';
    int int_34 = -2147483648;
    ushort ushort_39 = 1;
    ulong ulong_41 = 3;
    S1.S1_D1_F2 s1_s1_d1_f2_43 = new S1.S1_D1_F2();
    S1 s1_44 = new S1();
    S2.S2_D1_F1 s2_s2_d1_f1_45 = new S2.S2_D1_F1();
    S2 s2_46 = new S2();
    static int s_loopInvariant = 8;
    public decimal LeafMethod3()
    {
        unchecked
        {
            return 15+4;
        }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public double LeafMethod4()
    {
        unchecked
        {
            return 15+4;
        }
    }
    public int LeafMethod6()
    {
        unchecked
        {
            return int_34 <<= s_int_15 >>= 15+4;
        }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public sbyte LeafMethod8()
    {
        unchecked
        {
            return 15%4;
        }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public ushort LeafMethod11()
    {
        unchecked
        {
            return 15-4;
        }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public uint LeafMethod12()
    {
        unchecked
        {
            return s1_s1_d1_f2_43.s1_s1_d1_f1_3.uint_2 <<= int_34;
        }
    }
    public S1.S1_D1_F2 LeafMethod15()
    {
        unchecked
        {
            return s_s1_s1_d1_f2_24;
        }
    }
    public S2 Method1(ref S2 p_s2_47)
    {
        unchecked
        {
            bool bool_48 = false;
            double double_52 = 1.0909090909090908;
            short short_53 = -2;
            int int_54 = 5;
            ushort ushort_59 = 5;
            ulong ulong_61 = 2;
            S1 s1_64 = new S1();
            switch (char_30)
            {
                case '8':
                {
                    try
                    {
                        if ((ulong_41 = 15+4)* s_ulong_22<= (15-4* (ulong_61 &= ulong_61)& s_ulong_22 % 15-4+ 11))
                        {
                        }
                        else
                        {
                        }

                        throw new Exception();
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        s_s1_s1_d1_f2_24.s1_s1_d1_f1_4.uint_2 |= s_s1_s1_d1_f2_24.s1_s1_d1_f1_3.uint_2 /= LeafMethod12()+ 7;
                    }
                    break;
                }
                case '2':
                {
                    break;
                }
                case '0':
                {
                    int __loopvar1 = s_loopInvariant, __loopSecondaryVar1_0 = s_loopInvariant;
                    for (;;)
                    {
                        if (__loopvar1 > 15+4)
                            break;
                        int __loopvar0 = s_loopInvariant;
                        while (bool_48 = bool_48)
                        {
                            if (__loopvar0 <= 15-4)
                                break;
                            LeafMethod3();
                        }
                        if (short_53 >= (short_53 = 15+4))
                        {
                            s_s1_25.sbyte_5 |= (sbyte)(LeafMethod8() / LeafMethod8() >> 15+4- s1_64.sbyte_5<< LeafMethod6());
                        }
                        else
                        {
                            LeafMethod15();
                        }
                    }
                    double_52 = s2_s2_d1_f1_45.double_7 * LeafMethod4();
                    break;
                }
                default:
                {
                    ushort_59 ^= ushort_39 >>= s_int_15 *= int_54| 15/4- ushort_59 / LeafMethod11()+ 93/ (ushort_39 | LeafMethod11())+ 26+ 37;
                    break;
                }
            }
            return s2_46;
        }
    }
    public S1 Method3(out S1.S1_D1_F2 p_s1_s1_d1_f2_90, ref double p_double_91, uint p_uint_92, ref S2.S2_D1_F1 p_s2_s2_d1_f1_93, S2.S2_D1_F1 p_s2_s2_d1_f1_94, ref S1.S1_D1_F2 p_s1_s1_d1_f2_95, ref S2 p_s2_96, ref S1.S1_D1_F2 p_s1_s1_d1_f2_97, out decimal p_decimal_98, ref S1.S1_D1_F2 p_s1_s1_d1_f2_99, S1.S1_D1_F2 p_s1_s1_d1_f2_100, S2.S2_D1_F1 p_s2_s2_d1_f1_101, S1.S1_D1_F2 p_s1_s1_d1_f2_102, out S2.S2_D1_F1 p_s2_s2_d1_f1_103, out S2 p_s2_104, ref ulong p_ulong_105)
    {
        unchecked
        {
            S1.S1_D1_F2 s1_s1_d1_f2_121 = new S1.S1_D1_F2();
            p_s1_s1_d1_f2_90 = s1_s1_d1_f2_121;
            p_decimal_98 = 15+4;
            p_s2_s2_d1_f1_103 = s_s2_s2_d1_f1_26;
            Method1(ref s2_46);
            return s1_44;
        }
    }
    public void Method0()
    {
        unchecked
        {
            ulong ulong_3041 = 5;
            S1.S1_D1_F1 s1_s1_d1_f1_3042 = new S1.S1_D1_F1();
            S1.S1_D1_F2 s1_s1_d1_f2_3044 = new S1.S1_D1_F2();
            S2.S2_D1_F1 s2_s2_d1_f1_3046 = new S2.S2_D1_F1();
            S2 s2_3047 = new S2();
            s_s2_27 = Method1(ref s_s2_27);
            s1_44 = Method3(out s1_s1_d1_f2_3044, ref s_s2_s2_d1_f1_26.double_7, 15+4, ref s2_s2_d1_f1_45, s_s2_s2_d1_f1_26, ref s1_s1_d1_f2_3044, ref s2_3047, ref s1_s1_d1_f2_43, out s1_s1_d1_f1_3042.decimal_0, ref s_s1_s1_d1_f2_24, s1_s1_d1_f2_3044, s2_s2_d1_f1_3046, s1_s1_d1_f2_3044, out s2_s2_d1_f1_45, out s_s2_27, ref ulong_3041);
            return;
        }
    }

    public static int Main(string[] args)
    {
        new LargeFuncletFrame().Method0();
        return 100;
    }
}
/*
Environment:

set COMPlus_TC_OnStackReplacement=1
set COMPlus_TC_QuickJitForLoops=1
set COMPlus_TC_OnStackReplacement_InitialCounter=1
set COMPlus_OSR_HitLimit=2
set COMPlus_JitRandomOnStackReplacement=15
set COMPlus_JitStress=2
set COMPlus_AltJitName=clrjit_universal_arm64_x64.dll
set COMPlus_AltJit=Method1

Assert failure(PID 98024 [0x00017ee8], Thread: 54984 [0xd6c8]): Assertion failed 'genFuncletInfo.fiSpDelta1 >= -240' in 'LargeFuncletFrame:Method1(byref):S2:this' during 'Generate code' (IL size 387)
    File: D:\git\dotnet-runtime\src\coreclr\jit\codegenarm64.cpp Line: 1620
    Image: d:\git\dotnet-runtime\artifacts\tests\coreclr\windows.x64.Checked\tests\Core_Root\CoreRun.exe
*/
