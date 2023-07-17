// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

public class Test
{
    public class TestClass
    {
        public struct S1
        {
            public float float_0;
        }
        public struct S2
        {
            public struct S2_D1_F1
            {
                public uint uint_1;
            }
        }
        static bool s_bool_2 = true;
        static byte s_byte_3 = 1;
        static char s_char_4 = 'M';
        static decimal s_decimal_5 = 2.0405405405405405405405405405m;
        static double s_double_6 = -1.8846153846153846;
        static short s_short_7 = -1;
        static int s_int_8 = -5;
        static long s_long_9 = -5;
        static sbyte s_sbyte_10 = 1;
        static float s_float_11 = -4.952381f;
        static string s_string_12 = "JZDP";
        static ushort s_ushort_13 = 1;
        static uint s_uint_14 = 5;
        static ulong s_ulong_15 = 2;
        static S1 s_s1_16 = new S1();
        static S2.S2_D1_F1 s_s2_s2_d1_f1_17 = new S2.S2_D1_F1();
        static S2 s_s2_18 = new S2();
        bool bool_19 = true;
        byte byte_20 = 0;
        char char_21 = 'W';
        decimal decimal_22 = 2.2307692307692307692307692308m;
        double double_23 = -1;
        short short_24 = 0;
        int int_25 = 0;
        long long_26 = 2;
        sbyte sbyte_27 = -2;
        float float_28 = 0.071428575f;
        string string_29 = "MNK";
        ushort ushort_30 = 2;
        uint uint_31 = 1;
        ulong ulong_32 = 31;
        S1 s1_33 = new S1();
        S2.S2_D1_F1 s2_s2_d1_f1_34 = new S2.S2_D1_F1();
        S2 s2_35 = new S2();
        static int s_loopInvariant = 3;
        private static List<string> toPrint = new List<string>();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool LeafMethod0()
        {
            unchecked
            {
                return ((bool)(((ulong)(((ulong)(((int)(int_25 % ((int)((int_25) | 66)))) % ((int)((((int)(int_25 | s_int_8))) | 53)))) + ulong_32)) == ((ulong)(((ulong)(((ulong)(s_ulong_15 ^ s_ulong_15)) + ((ulong)(s_int_8 % ((int)((s_int_8) | 49)))))) * ((ulong)(((ulong)(ulong_32 * s_ulong_15)) | ulong_32))))));
            }
        }
        public byte LeafMethod1()
        {
            unchecked
            {
                return ((byte)(byte_20 - ((byte)(((int)(s_int_8 / ((int)((((int)(s_int_8 %= ((int)((-1) | 4))))) | 81)))) / ((int)((((int)(int_25 = ((int)(s_int_8 - 5))))) | 85))))));
            }
        }
        public char LeafMethod2()
        {
            unchecked
            {
                return s_char_4;
            }
        }
        public decimal LeafMethod3()
        {
            unchecked
            {
                return ((decimal)(((int)(((int)(((int)(s_int_8 &= s_int_8)) >> ((int)(int_25 >> s_int_8)))) ^ ((int)(int_25 / ((int)((((int)(1 * int_25))) | 83)))))) / ((int)((((int)(((int)(int_25 *= ((int)(int_25 % ((int)((int_25) | 1)))))) >> ((int)(((int)(s_int_8 / ((int)((s_int_8) | 18)))) & ((int)(31 & int_25))))))) | 20))));
            }
        }
        public double LeafMethod4()
        {
            unchecked
            {
                return ((double)(((double)(s_double_6 *= ((double)(((double)(s_int_8 % ((int)((s_int_8) | 4)))) * ((double)(s_double_6 += s_double_6)))))) + ((double)(((int)(s_int_8 <<= ((int)(s_int_8 | int_25)))) % ((int)((((int)(s_int_8 = -5))) | 16))))));
            }
        }
        public short LeafMethod5()
        {
            unchecked
            {
                return ((short)(s_short_7 + ((short)(short_24 |= ((short)(((short)(s_int_8 / ((int)((int_25) | 30)))) << ((int)(int_25 + 5))))))));
            }
        }
        public int LeafMethod6()
        {
            unchecked
            {
                return ((int)(int_25 <<= ((int)(((int)(((int)(s_int_8 * s_int_8)) << ((int)(int_25 - 31)))) & s_int_8))));
            }
        }
        public long LeafMethod7()
        {
            unchecked
            {
                return ((long)(((long)(s_long_9 += ((long)(((long)(s_long_9 >>= LeafMethod6())) >> s_int_8)))) - ((long)(long_26 = long_26))));
            }
        }
        public S1 Method23(out S1 p_s1_653, out S1 p_s1_654, ref int p_int_655, S2.S2_D1_F1 p_s2_s2_d1_f1_656, out S2 p_s2_657, ref S2.S2_D1_F1 p_s2_s2_d1_f1_658, float p_float_659)
        {
            unchecked
            {
                bool bool_660 = true;
                byte byte_661 = 2;
                char char_662 = 'Y';
                decimal decimal_663 = 1.0476190476190476190476190476m;
                double double_664 = -1.95;
                short short_665 = 31;
                int int_666 = 0;
                long long_667 = 31;
                sbyte sbyte_668 = 0;
                float float_669 = 31f;
                string string_670 = "3P5A58X";
                ushort ushort_671 = 5;
                uint uint_672 = 2;
                ulong ulong_673 = 2;
                S1 s1_674 = new S1();
                S2.S2_D1_F1 s2_s2_d1_f1_675 = new S2.S2_D1_F1();
                S2 s2_676 = new S2();
                p_s1_653 = s1_674;
                p_s1_654 = s_s1_16;
                switch (((long)(((long)(s_long_9 *= ((long)(((int)(s_int_8 <<= int_25)) % ((int)((((int)(s_int_8 | s_int_8))) | 82)))))) ^ ((long)(((long)(int_25 /= ((int)((((int)(s_int_8 = LeafMethod6()))) | 38)))) + ((long)(long_26 + ((long)(-2 & LeafMethod7())))))))))
                {
                    case -2147483648:
                        {
                            int __loopvar0 = s_loopInvariant;
                            break;
                        }
                    case 31:
                        {
                            break;
                        }
                    default:
                        {
                            long_667 += ((long)(long_667 |= ((long)(long_667 & ((long)(((int)(LeafMethod6() % ((int)((p_int_655) | 24)))) % ((int)((int_666) | 2))))))));
                            break;
                        }
                }
                return s_s1_16;
            }
        }
        public void Method0()
        {
            unchecked
            {
                int int_2775 = -2147483648;
                S2.S2_D1_F1 s2_s2_d1_f1_2784 = new S2.S2_D1_F1();
                int __loopvar2 = s_loopInvariant, __loopSecondaryVar2_0 = s_loopInvariant;
                s_s1_16 = Method23(out s1_33, out s_s1_16, ref int_2775, s2_s2_d1_f1_2784, out s_s2_18, ref s_s2_s2_d1_f1_17, ((float)(s_int_8 /= ((int)((((int)(((int)(int_25 % ((int)((LeafMethod6()) | 91)))) * ((int)(int_25 >> s_int_8))))) | 29)))));
                return;
            }
        }
    }

    // This is trying to stress the JIT to ensure we do not encounter an assertion.
    [Fact]
    public static int TestEntryPoint()
    {
        new TestClass().Method0();
        return 100;
    }
}
