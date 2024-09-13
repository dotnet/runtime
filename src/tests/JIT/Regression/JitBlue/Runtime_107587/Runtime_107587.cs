// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Found by Antigen
// Reduced from 20.86 KB to 1.7 KB.
// JIT assert failed:
// Assertion failed 'unreached' in 'Runtime_107587:Method0():this' during 'Lowering nodeinfo' (IL size 133; hash 0x46e9aa75; FullOpts)

    // File: D:\a\_work\1\s\src\coreclr\jit\lowerxarch.cpp Line: 11752

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Numerics;
using Xunit;

public class Runtime_107587
{
    public struct S2
    {
    }
    static Vector256<ushort> s_v256_ushort_34 = Vector256.Create((ushort)2);
    static Vector512<sbyte> s_v512_sbyte_42 = Vector512.Create((sbyte)92);
    static Vector512<short> s_v512_short_43 = Vector512.Create(1, -2, -2, 0, 92, 2, 0, 1, 0, 0, 5, 1, 1, 5, 0, 1, -2, -1, 1, 5, 92, 92, -1, 5, -1, 3, 5, 0, -1, 92, 2, 5);
    static S2 s_s2_65 = new S2();
    Vector512<sbyte> v512_sbyte_102 = Vector512.CreateScalar((sbyte)-2);
    Vector3 v3_122 = Vector3.UnitX;
    private static List<string> toPrint = new List<string>();
    public Vector512<short> Method1(Vector3 p_v3_126, S2 p_s2_127, int p_int_128, S2 p_s2_129, Vector512<sbyte> p_v512_sbyte_130, ref Vector256<ushort> p_v256_ushort_131, byte p_byte_132)
    {
        unchecked
        {
            return s_v512_short_43 -= s_v512_short_43;
        }
    }
    public void Method0()
    {
        unchecked
        {
            byte byte_152 = 3;
            int int_157 = 0;
            S2 s2_167 = new S2();
            s_v512_short_43 = Method1(15*4* (v3_122 += Vector3.Round(v3_122)), s_s2_65, int_157, s2_167, s_v512_sbyte_42 *= s_v512_sbyte_42 *= Avx512F.TernaryLogic(v512_sbyte_102, s_v512_sbyte_42, s_v512_sbyte_42, byte_152), ref s_v256_ushort_34, 15+4);
            return;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        new Runtime_107587().Method0();
        return string.Join(Environment.NewLine, toPrint).GetHashCode();
    }
}
