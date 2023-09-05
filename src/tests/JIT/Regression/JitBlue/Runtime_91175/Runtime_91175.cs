// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.aa

// Found by Antigen


using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Numerics;
using Xunit;

public class TestClass
{
    public struct S1
    {
        public Vector64<uint> v64_uint_0;
    }

    public struct S2
    {
        public struct S2_D1_F4
        {
            public short short_4;
        }
        public bool bool_1;
        public S1 s1_2;
        public S1 s1_3;
    }

    public struct S3
    {
        public struct S3_D1_F4
        {
            public S1 s1_8;
        }
        public S1 s1_5;
        public Vector4 v4_6;
        public Vector256<double> v256_double_7;
    }

    public struct S4
    {
        public struct S4_D1_F2
        {
            public struct S4_D2_F1
            {
                public int int_10;
                public S2 s2_11;
            }
            public struct S4_D2_F3
            {
                public uint uint_13;
            }
            public ulong ulong_12;
        }
        public Vector128<sbyte> v128_sbyte_9;
        public Vector64<sbyte> v64_sbyte_14;
        public double double_15;
    }

    public struct S5
    {
        public struct S5_D1_F1
        {
            public struct S5_D2_F3
            {
                public sbyte sbyte_18;
                public S2.S2_D1_F4 s2_s2_d1_f4_19;
                public Vector256<double> v256_double_20;
            }
            public ushort ushort_16;
            public S2 s2_17;
            public uint uint_21;
        }
    }

    static bool s_bool_22 = false;
    static byte s_byte_23 = 1;
    static char s_char_24 = 'H';
    static decimal s_decimal_25 = -1.9444444444444444444444444444m;
    static double s_double_26 = 2147483647;
    static short s_short_27 = -2;
    static int s_int_28 = 3;
    static long s_long_29 = -1;
    static sbyte s_sbyte_30 = 84;
    static float s_float_31 = 1.0625f;
    static string s_string_32 = "B9PA5V9";
    static ushort s_ushort_33 = 2;
    static uint s_uint_34 = 2;
    static ulong s_ulong_35 = 3;
    static Vector64<byte> s_v64_byte_36 = Vector64.Create(((byte)(3)), 5, 1, 1, 1, 5, 1, 2);
    static Vector64<sbyte> s_v64_sbyte_37 = Vector64.CreateScalar(((sbyte)(7)));
    static Vector64<short> s_v64_short_38 = Vector64<short>.Zero;
    static Vector64<ushort> s_v64_ushort_39 = Vector64.Create(((ushort)(1)));
    static Vector64<int> s_v64_int_40 = Vector64.Create(((int)(3)));
    static Vector64<uint> s_v64_uint_41 = Vector64.Create(((uint)(2)), 1);
    static Vector64<long> s_v64_long_42 = Vector64.Create(((long)(-1)));
    static Vector64<ulong> s_v64_ulong_43 = Vector64.Create(((ulong)(1)));
    static Vector64<float> s_v64_float_44 = Vector64.Create(((float)(5.2105265f)));
    static Vector64<double> s_v64_double_45 = Vector64.Create(((double)(84.08888888888889)));
    static Vector128<byte> s_v128_byte_46 = Vector128.Create(((byte)(1)), 1, 0, 84, 1, 3, 3, 5, 5, 2, 2, 3, 3, 2, 3, 0);
    static Vector128<sbyte> s_v128_sbyte_47 = Vector128.Create(((sbyte)(0)), 5, 0, 2, 6, -1, 5, -1, -2, 84, -2, 1, -1, -2, -2, 1);
    static Vector128<short> s_v128_short_48 = Vector128.Create(((short)(1)), 3, 84, -2, -2, 1, 3, 5);
    static Vector128<ushort> s_v128_ushort_49 = Vector128.Create(((ushort)(1)));
    static Vector128<int> s_v128_int_50 = Vector128.Create(((int)(-2)));
    static Vector128<uint> s_v128_uint_51 = Vector128<uint>.AllBitsSet;
    static Vector128<long> s_v128_long_52 = Vector128.Create(((long)(-1)));
    static Vector128<ulong> s_v128_ulong_53 = Vector128<ulong>.Zero;
    static Vector128<float> s_v128_float_54 = Vector128.Create(((float)(-2f)));
    static Vector128<double> s_v128_double_55 = Vector128.Create(((double)(-4.989010989010989)));
    static Vector256<byte> s_v256_byte_56 = Vector256.Create(((byte)(0)));
    static Vector256<sbyte> s_v256_sbyte_57 = Vector256.Create(((sbyte)(-1)), -2, -2, 84, 1, -2, -1, 84, 1, 0, -2, 5, 1, 5, 5, 1, -1, -1, 1, 5, -1, -1, 84, 84, -5, 1, -1, -2, -1, 84, 5, 1);
    static Vector256<short> s_v256_short_58 = Vector256.Create(((short)(5)));
    static Vector256<ushort> s_v256_ushort_59 = Vector256.CreateScalar(((ushort)(2)));
    static Vector256<int> s_v256_int_60 = Vector256.CreateScalar(((int)(-2)));
    static Vector256<uint> s_v256_uint_61 = Vector256<uint>.Zero;
    static Vector256<long> s_v256_long_62 = Vector256.CreateScalar(((long)(-2)));
    static Vector256<ulong> s_v256_ulong_63 = Vector256.Create(((ulong)(5)));
    static Vector256<float> s_v256_float_64 = Vector256.CreateScalar(((float)(-0.9594595f)));
    static Vector256<double> s_v256_double_65 = Vector256<double>.Zero;
    static Vector2 s_v2_66 = new Vector2(((float)(1.030303f)));
    static Vector3 s_v3_67 = new Vector3(((float)(5.071429f)));
    static Vector4 s_v4_68 = Vector4.UnitW;
    static S1 s_s1_69 = new S1();
    static S2.S2_D1_F4 s_s2_s2_d1_f4_70 = new S2.S2_D1_F4();
    static S2 s_s2_71 = new S2();
    static S3.S3_D1_F4 s_s3_s3_d1_f4_72 = new S3.S3_D1_F4();
    static S3 s_s3_73 = new S3();
    static S4.S4_D1_F2.S4_D2_F1 s_s4_s4_d1_f2_s4_d2_f1_74 = new S4.S4_D1_F2.S4_D2_F1();
    static S4.S4_D1_F2.S4_D2_F3 s_s4_s4_d1_f2_s4_d2_f3_75 = new S4.S4_D1_F2.S4_D2_F3();
    static S4.S4_D1_F2 s_s4_s4_d1_f2_76 = new S4.S4_D1_F2();
    static S4 s_s4_77 = new S4();
    static S5.S5_D1_F1.S5_D2_F3 s_s5_s5_d1_f1_s5_d2_f3_78 = new S5.S5_D1_F1.S5_D2_F3();
    static S5.S5_D1_F1 s_s5_s5_d1_f1_79 = new S5.S5_D1_F1();
    static S5 s_s5_80 = new S5();
    bool bool_81 = false;
    byte byte_82 = 84;
    char char_83 = 'M';
    decimal decimal_84 = 5m;
    double double_85 = 1.0571428571428572;
    short short_86 = 84;
    int int_87 = 5;
    long long_88 = 5;
    sbyte sbyte_89 = -1;
    float float_90 = 5f;
    string string_91 = "MN5AH1GE";
    ushort ushort_92 = 5;
    uint uint_93 = 1;
    ulong ulong_94 = 84;
    Vector64<byte> v64_byte_95 = Vector64.Create(((byte)(1)), 3, 2, 84, 0, 5, 127, 2);
    Vector64<sbyte> v64_sbyte_96 = Vector64.CreateScalar(((sbyte)(-1)));
    Vector64<short> v64_short_97 = Vector64.Create(((short)(-2)));
    Vector64<ushort> v64_ushort_98 = Vector64.Create(((ushort)(5)));
    Vector64<int> v64_int_99 = Vector64.Create(((int)(3)), -2);
    Vector64<uint> v64_uint_100 = Vector64.Create(((uint)(1)), 5);
    Vector64<long> v64_long_101 = Vector64.Create(((long)(0)));
    Vector64<ulong> v64_ulong_102 = Vector64.Create(((ulong)(5)));
    Vector64<float> v64_float_103 = Vector64.Create(((float)(5.0833335f)), -4.9673915f);
    Vector64<double> v64_double_104 = Vector64.Create(((double)(5.023809523809524)));
    Vector128<byte> v128_byte_105 = Vector128.Create(((byte)(0)));
    Vector128<sbyte> v128_sbyte_106 = Vector128.Create(((sbyte)(84)), 0, 3, -2, 5, -1, 5, -1, 5, 84, -1, 84, -5, 5, -1, 84);
    Vector128<short> v128_short_107 = Vector128.CreateScalar(((short)(1)));
    Vector128<ushort> v128_ushort_108 = Vector128.Create(((ushort)(5)));
    Vector128<int> v128_int_109 = Vector128.Create(((int)(1)), -2, 5, -2);
    Vector128<uint> v128_uint_110 = Vector128.Create(((uint)(5)));
    Vector128<long> v128_long_111 = Vector128.CreateScalar(((long)(-2)));
    Vector128<ulong> v128_ulong_112 = Vector128<ulong>.AllBitsSet;
    Vector128<float> v128_float_113 = Vector128<float>.AllBitsSet;
    Vector128<double> v128_double_114 = Vector128.Create(((double)(1)));
    Vector256<byte> v256_byte_115 = Vector256<byte>.AllBitsSet;
    Vector256<sbyte> v256_sbyte_116 = Vector256.Create(((sbyte)(5)));
    Vector256<short> v256_short_117 = Vector256.Create(((short)(-1)));
    Vector256<ushort> v256_ushort_118 = Vector256.CreateScalar(((ushort)(0)));
    Vector256<int> v256_int_119 = Vector256.Create(((int)(84)));
    Vector256<uint> v256_uint_120 = Vector256.Create(((uint)(5)));
    Vector256<long> v256_long_121 = Vector256.CreateScalar(((long)(-2)));
    Vector256<ulong> v256_ulong_122 = Vector256<ulong>.AllBitsSet;
    Vector256<float> v256_float_123 = Vector256.Create(((float)(1.0337079f)));
    Vector256<double> v256_double_124 = Vector256.Create(((double)(1.0701754385964912)));
    Vector2 v2_125 = new Vector2(((float)(-1.925926f)));
    Vector3 v3_126 = new Vector3(((float)(3.0666666f)));
    Vector4 v4_127 = new Vector4(((float)(1.0444444f)));
    S1 s1_128 = new S1();
    S2.S2_D1_F4 s2_s2_d1_f4_129 = new S2.S2_D1_F4();
    S2 s2_130 = new S2();
    S3.S3_D1_F4 s3_s3_d1_f4_131 = new S3.S3_D1_F4();
    S3 s3_132 = new S3();
    S4.S4_D1_F2.S4_D2_F1 s4_s4_d1_f2_s4_d2_f1_133 = new S4.S4_D1_F2.S4_D2_F1();
    S4.S4_D1_F2.S4_D2_F3 s4_s4_d1_f2_s4_d2_f3_134 = new S4.S4_D1_F2.S4_D2_F3();
    S4.S4_D1_F2 s4_s4_d1_f2_135 = new S4.S4_D1_F2();
    S4 s4_136 = new S4();
    S5.S5_D1_F1.S5_D2_F3 s5_s5_d1_f1_s5_d2_f3_137 = new S5.S5_D1_F1.S5_D2_F3();
    S5.S5_D1_F1 s5_s5_d1_f1_138 = new S5.S5_D1_F1();
    S5 s5_139 = new S5();
    static int s_loopInvariant = 8;
    private static List<string> toPrint = new List<string>();

    private S1 Method1(S5 p_s5_140, S2 p_s2_141, byte p_byte_142, out S4.S4_D1_F2.S4_D2_F3 p_s4_s4_d1_f2_s4_d2_f3_143, ref S4.S4_D1_F2.S4_D2_F3 p_s4_s4_d1_f2_s4_d2_f3_144, ref S3 p_s3_145, S4 p_s4_146)
    {
        unchecked
        {
            long long_154 = 84;
            p_s4_s4_d1_f2_s4_d2_f3_143 = s_s4_s4_d1_f2_s4_d2_f3_75;
            int __loopvar31 = s_loopInvariant - 10, __loopSecondaryVar31_0 = s_loopInvariant - 3;
            for (;;)
            {
                if (__loopvar31 >= s_loopInvariant)
                    break;
                switch (((long)(long_88 & long_154)))
                {
                    case 84:
                    {
                        break;
                    }
                    case -1:
                    {
                        break;
                    }
                    case -2:
                    {
                        try
                        {
                        }
                        finally
                        {
                            s_bool_22 = Avx.TestNotZAndNotC(((Vector256<int>)(((Vector256<int>)(v256_int_119 += ((Vector256<int>)(v256_int_119 - s_v256_int_60)))) - ((Vector256<int>)(s_v256_int_60 *= ((Vector256<int>)(s_v256_int_60 - Vector256<int>.Zero)))))), ((Vector256<int>)(((Vector256<int>)(Vector256.AsInt32(s_v256_ushort_59) - ((Vector256<int>)(v256_int_119 *= v256_int_119)))) - Avx2.ShiftRightArithmetic(Vector256<int>.AllBitsSet, Vector128<int>.AllBitsSet))));
                        }
                        break;
                    }
                    default:
                    {
                        break;
                    }
                }

                // Added the following line so we don't loop infinitely
                __loopvar31++;
            }
            return s1_128;
        }
    }

    private void Method0()
    {
        unchecked
        {
            byte byte_229 = 0;
            S1 s1_242 = new S1();
            S2 s2_246 = new S2();
            S4.S4_D1_F2.S4_D2_F3 s4_s4_d1_f2_s4_d2_f3_254 = new S4.S4_D1_F2.S4_D2_F3();
            S4.S4_D1_F2.S4_D2_F3 s4_s4_d1_f2_s4_d2_f3_255 = s4_s4_d1_f2_s4_d2_f3_254;
            S4 s4_257 = new S4();
            S4 s4_258 = s4_257;
            S5 s5_263 = new S5();
            S5 s5_264 = s5_263;
            s1_242 = Method1(s5_264, s2_246, ((byte)(byte_229 | ((byte)(((byte)(s_byte_23 + s_byte_23)) + ((byte)(byte_229 ^ s_byte_23)))))), out s4_s4_d1_f2_s4_d2_f3_134, ref s4_s4_d1_f2_s4_d2_f3_255, ref s_s3_73, s4_258);
            return;
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        new TestClass().Method0();
    }
}
/*

Assert failure(PID 1312 [0x00000520], Thread: 31184 [0x79d0]): Assertion failed '!varTypeIsSIMD(vns->TypeOfVN(argVN))' in 'TestClass:Method1(TestClass+S5,TestClass+S2,ubyte,byref,byref,byref,TestClass+S4):TestClass+S1:this' during 'Do value numbering' (IL size 224; hash 0xf238d379; Tier1-OSR)
    File: D:\git\runtime\src\coreclr\jit\valuenum.cpp Line: 6668
    Image: D:\git\runtime\artifacts\tests\coreclr\windows.x64.Checked\tests\Core_Root\CoreRun.exe
*/
