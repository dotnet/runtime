// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_110326A
{
    public struct S1
    {
        public bool bool_2;
        public Vector512<short> v512_short_3;
        public Vector512<float> v512_float_4;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Runtime_110326A.Method1();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Method1()
    {
        S1 s1_s1_d1_f3_160 = new S1();
        return Vector512.ExtractMostSignificantBits(s1_s1_d1_f3_160.v512_short_3);
    }
}

public class Runtime_110326B
{
    public struct S2_D1_F2
    {
        public struct S2_D2_F2
        {
            public Vector<double> v_double_0;
        }

        public struct S2_D2_F3
        {
            public Vector3 v3_10;
        }
    }

    public struct S2_D1_F3
    {
        public struct S2_D2_F3
        {
            public Vector256<int> v256_int_14;
        }

        public Vector128<long> v128_long_13;
        public Vector512<uint> v512_uint_16;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Runtime_110326B.Method0();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Method0()
    {
        S2_D1_F2.S2_D2_F2 s2_s2_d1_f2_s2_d2_f2_262 = new S2_D1_F2.S2_D2_F2();
        S2_D1_F2.S2_D2_F2 s2_s2_d1_f2_s2_d2_f2_263 = s2_s2_d1_f2_s2_d2_f2_262;
        S2_D1_F2.S2_D2_F3 s2_s2_d1_f2_s2_d2_f3_264 = new S2_D1_F2.S2_D2_F3();
        S2_D1_F2.S2_D2_F3 s2_s2_d1_f2_s2_d2_f3_265 = s2_s2_d1_f2_s2_d2_f3_264;
        S2_D1_F2 s2_s2_d1_f2_266 = new S2_D1_F2();
        S2_D1_F3.S2_D2_F3 s2_s2_d1_f3_s2_d2_f3_268 = new S2_D1_F3.S2_D2_F3();
        S2_D1_F3 s2_s2_d1_f3_269 = new S2_D1_F3();
        S2_D1_F3 s2_s2_d1_f3_270 = s2_s2_d1_f3_269;
        s2_s2_d1_f3_270.v512_uint_16 = Vector512.IsZero(Vector512<uint>.AllBitsSet);

        Log("s2_s2_d1_f", s2_s2_d1_f2_s2_d2_f2_262.v_double_0);
        Log("s2_s2_d1_f", s2_s2_d1_f2_s2_d2_f2_263.v_double_0);
        Log("s2_s2_d1_f", s2_s2_d1_f2_s2_d2_f3_264);
        Log("s2_s2_d1_f", s2_s2_d1_f2_s2_d2_f3_265.v3_10);
        Log("s2_s2_d1_f", s2_s2_d1_f2_266);
        Log("s2_s2_d1_f", s2_s2_d1_f3_s2_d2_f3_268.v256_int_14);
        Log("s2_s2_d1_f", s2_s2_d1_f3_269.v128_long_13);
        Log("s2_s2_d1_f", s2_s2_d1_f3_270.v128_long_13);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Log(string varName, object varValue)
    {
    }
}
