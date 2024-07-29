// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Found by Antigen

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class TestClass
{
    public struct S1
    {
        public Vector64<short> v64_short_0;
        public ulong ulong_1;
    }
    public struct S2
    {
        public S1 s1_3;
    }
    static float s_float_13 = -1f;
    static uint s_uint_16 = 1;
    static ulong s_ulong_17 = 5;
    static Vector128<short> s_v128_short_30 = Vector128.Create((short)2);
    static Vector128<ushort> s_v128_ushort_31 = Vector128<ushort>.AllBitsSet;
    static Vector128<int> s_v128_int_32 = Vector128.CreateScalar(2);
    static Vector128<uint> s_v128_uint_33 = Vector128.Create((uint)1, 1, 1, 2);
    static Vector128<long> s_v128_long_34 = Vector128.Create(-2, 2);
    static Vector128<ulong> s_v128_ulong_35 = Vector128.Create((ulong)1);
    static Vector128<float> s_v128_float_36 = Vector128.CreateScalar(0.1f);
    static Vector128<double> s_v128_double_37 = Vector128.Create(1.0235294117647058, 2);
    static Vector256<byte> s_v256_byte_38 = Vector256.CreateScalar((byte)5);
    static Vector256<sbyte> s_v256_sbyte_39 = Vector256.Create(1, 0, -2, 2, 97, 97, -2, -5, 5, 5, -5, 1, 2, 1, 5, -2, 97, 2, 6, 2, 5, -5, 1, -2, 97, 2, 5, 5, 2, -2, 2, 5);
    static Vector256<short> s_v256_short_40 = Vector256.CreateScalar((short)2);
    static Vector256<ushort> s_v256_ushort_41 = Vector256.Create((ushort)97);
    static Vector256<int> s_v256_int_42 = Vector256<int>.Zero;
    static Vector256<uint> s_v256_uint_43 = Vector256.CreateScalar((uint)2);
    static Vector256<long> s_v256_long_44 = Vector256.Create((long)-1);
    static Vector256<ulong> s_v256_ulong_45 = Vector256<ulong>.Zero;
    static Vector256<float> s_v256_float_46 = Vector256.CreateScalar(-4.969697f);
    static Vector256<double> s_v256_double_47 = Vector256.Create(97.03448275862068);
    static Vector512<byte> s_v512_byte_48 = Vector512.Create((byte)1);
    static Vector512<sbyte> s_v512_sbyte_49 = Vector512.Create((sbyte)5);
    static Vector512<short> s_v512_short_50 = Vector512<short>.AllBitsSet;
    static Vector512<ushort> s_v512_ushort_51 = Vector512<ushort>.AllBitsSet;
    static Vector512<int> s_v512_int_52 = Vector512<int>.AllBitsSet;
    static Vector512<uint> s_v512_uint_53 = Vector512.CreateScalar((uint)5);
    static Vector512<long> s_v512_long_54 = Vector512.CreateScalar((long)2);
    static Vector512<ulong> s_v512_ulong_55 = Vector512<ulong>.Zero;
    static S1 s_s1_61 = new S1();
    static S2 s_s2_62 = new S2();
    int int_69 = 2;
    Vector128<sbyte> v128_sbyte_88 = Vector128<sbyte>.Zero;
    Vector128<short> v128_short_89 = Vector128.CreateScalar((short)97);
    Vector128<ushort> v128_ushort_90 = Vector128<ushort>.Zero;
    Vector128<long> v128_long_93 = Vector128<long>.AllBitsSet;
    Vector128<ulong> v128_ulong_94 = Vector128<ulong>.Zero;
    Vector128<float> v128_float_95 = Vector128.Create(1.1153846f, 5f, 1.0232558f, 97.01786f);
    Vector128<double> v128_double_96 = Vector128.Create(2.0375, 2);
    Vector256<byte> v256_byte_97 = Vector256.CreateScalar((byte)5);
    Vector256<short> v256_short_99 = Vector256.CreateScalar((short)5);
    Vector256<ushort> v256_ushort_100 = Vector256<ushort>.Zero;
    Vector256<int> v256_int_101 = Vector256.CreateScalar(97);
    Vector256<uint> v256_uint_102 = Vector256.Create((uint)97);
    Vector256<long> v256_long_103 = Vector256.CreateScalar((long)2);
    Vector256<ulong> v256_ulong_104 = Vector256.CreateScalar((ulong)2);
    Vector256<float> v256_float_105 = Vector256.Create(5.0869565f);
    Vector256<double> v256_double_106 = Vector256.Create(-1.9411764705882353);
    S1 s1_120 = new S1();
    S2 s2_121 = new S2();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Vector256<int> Method31()
    {
            byte byte_929 = 2;
            return s_v256_int_42 += v256_int_101 | v256_int_101 ^ v256_int_101 | 
                Vector256<int>.AllBitsSet & s_v256_int_42 ^ (s_v256_int_42 *= v256_int_101) - (v256_int_101 ^ Vector256<int>.Zero) & (v256_int_101 *= v256_int_101) - Avx2.ShiftRightLogical(v256_int_101, byte_929);
    }

    [Fact]
    public static void TestEntryPoint()
    {
        if (Avx2.IsSupported)
        {
            new TestClass().Method31();
        }
    }
}
