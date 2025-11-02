// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Found by Antigen
// Reduced from 50.05 KB to 4.88 KB.
// Assertion failed 'genExactlyOneBit(availableSet)' in 'TestClass:Method0():this' during 'Generate code' (IL size 782; hash 0x46e9aa75; FullOpts)
//
//    File: /Users/runner/work/1/s/src/coreclr/jit/codegencommon.cpp Line: 133


using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Numerics;
using Xunit;

public class Runtime_108609
{
    public struct S1
    {
        public Vector3 v3_0;
        public short short_1;
    }
    static bool s_bool_4 = true;
    static int s_int_10 = 5;
    static sbyte s_sbyte_12 = -5;
    static Vector64<int> s_v64_int_23 = Vector64<int>.Zero;
    static Vector64<double> s_v64_double_28 = Vector64.Create(-4.956521739130435);
    static Vector128<sbyte> s_v128_sbyte_30 = Vector128<sbyte>.Zero;
    static Vector128<short> s_v128_short_31 = Vector128.Create(0, 2, -5, 0, 1, 0, 2, 2);
    static Vector128<ulong> s_v128_ulong_36 = Vector128<ulong>.Zero;
    static Vector128<double> s_v128_double_38 = Vector128.Create(1.0769230769230769, 72.01666666666667);
    static Vector<ulong> s_v_ulong_46 = Vector<ulong>.One;
    static Vector<float> s_v_float_47 = Vector<float>.Zero;
    static S1 s_s1_52 = new S1();
    char char_56 = 'K';
    int int_60 = -1;
    Vector64<int> v64_int_73 = Vector64.Create(2147483646, -1);
    Vector64<double> v64_double_78 = Vector64.Create(-0.9420289855072463);
    Vector128<sbyte> v128_sbyte_80 = Vector128.Create(-2, -2, -1, 0, 5, -1, -1, -1, 1, 2, -1, 0, -1, 72, -5, 0);
    Vector128<short> v128_short_81 = Vector128.CreateScalar((short)-1);
    Vector128<ulong> v128_ulong_86 = Vector128<ulong>.AllBitsSet;
    Vector<ulong> v_ulong_96 = Vector<ulong>.One;
    Vector<float> v_float_97 = Vector.Create(2f);
    Vector3 v3_100 = Vector3.Create(0.09090909f, 2.4f, -0.94f);
    S1 s1_102 = new S1();
    static int s_loopInvariant = 4;
    private static List<string> toPrint = new List<string>();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private decimal LeafMethod3()
    {
        return 19;
    }

    private Vector<float> Method2(decimal p_decimal_130, decimal p_decimal_131, sbyte p_sbyte_132, out Vector128<ulong> p_v128_ulong_133, S1 p_s1_134)
    {
        p_v128_ulong_133 = s_v128_ulong_36;
        return Vector<float>.Zero;
    }

    private Vector128<sbyte> Method3(sbyte p_sbyte_152, Vector<ulong> p_v_ulong_153, S1 p_s1_154, uint p_uint_155, S1 p_s1_156, decimal p_decimal_157, Vector64<double> p_v64_double_158)
    {
        unchecked
        {
            return s_v128_sbyte_30 = s_v128_sbyte_30;
        }
    }

    private void Method0()
    {
        unchecked
        {
            S1 s1_218 = new S1();
            if (!s_bool_4)
            {
                int __loopvar0 = s_loopInvariant;
                for (int __loopSecondaryVar0_0 = 15 - 4; s_int_10 < Vector64.GetElement(s_v64_int_23 - Vector64<int>.Zero * (15 + 4) | (s_v64_int_23 += AdvSimd.Max(Vector64<int>.Zero, v64_int_73)), 15 & 4); __loopvar0--, __loopSecondaryVar0_0++, s_s1_52.short_1 = Vector128.Sum(AdvSimd.ShiftLogicalRoundedSaturate(Vector128<short>.AllBitsSet, v128_short_81) & Vector128.AsInt16(s_v128_double_38) - (15 | 4) * (v128_short_81 *= s_v128_short_31)))
                {
                    v3_100 *= s1_218.v3_0 - (s1_218.v3_0 = s1_218.v3_0 = s1_218.v3_0 *= s1_218.v3_0) * Vector3.RadiansToDegrees(Vector3.Zero + s_s1_52.v3_0 + v3_100 * v3_100) - Vector3.Sin(s1_218.v3_0 *= v3_100 * (s1_218.v3_0 *= s1_218.v3_0) - v3_100 * s_s1_52.v3_0);
                }
            }
            v_float_97 = Method2(15 % 4, LeafMethod3(), s_sbyte_12, out v128_ulong_86, s1_218);
            v128_sbyte_80 = Method3(15 % 4, s_v_ulong_46 *= s_v_ulong_46 += v_ulong_96 = s_v_ulong_46 ^ v_ulong_96 * (15 - 4), s1_218, 15 & 4, s1_102, LeafMethod3(), v64_double_78 * s_v64_double_28 - (s_v64_double_28 = v64_double_78) | Vector64.GreaterThan(v64_double_78, v64_double_78) | (s_v64_double_28 = Vector64<double>.Zero + v64_double_78) | (s_v64_double_28 = v64_double_78) | Vector64.GreaterThan(v64_double_78, v64_double_78) | (s_v64_double_28 = Vector64<double>.Zero + v64_double_78) | s_v64_double_28);
            return;
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Antigen();
    }

    private static int Antigen()
    {
        new Runtime_108609().Method0();
        return string.Join(Environment.NewLine, toPrint).GetHashCode();
    }
}
