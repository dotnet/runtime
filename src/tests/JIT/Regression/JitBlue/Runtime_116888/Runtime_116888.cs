// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Found by Antigen
// Reduced from 342.86 KB to 1.33 KB.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Numerics;
using Xunit;

public class TestClass
{
    public struct S1
    {
    }
    public struct S2
    {
    }
    static Vector<ulong> s_v_ulong_45 = Vector<ulong>.AllBitsSet;
    static S1 s_s1_51 = new S1();
    S1 s1_101 = new S1();
    S2 s2_102 = new S2();
    private static List<string> toPrint = new List<string>();
    public S2 Method14(ref S1 p_s1_448, S2 p_s2_449, out S2 p_s2_450, S1 p_s1_451, out S1 p_s1_452, Vector<ulong> p_v_ulong_453, out S1 p_s1_454)
    {
        unchecked
        {
            return s2_102;
        }
    }

    private void Method0()
    {
        unchecked
        {
            S1 s1_2842 = new S1();
            S2 s2_2843 = new S2();
            s2_2843 = Method14(ref s_s1_51, s2_102, out s2_102, s1_101, out s_s1_51, Sve.CreateTrueMaskUInt64(SveMaskPattern.LargestMultipleOf4) + s_v_ulong_45 + s_v_ulong_45- s_v_ulong_45 * s_v_ulong_45, out s1_2842);
            return;
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        if (Sve.IsSupported)
        {
            new TestClass().Method0();
        }
        return;
    }
}
