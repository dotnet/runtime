// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Note: This test case is found by Antigen. It catches a scenario where we were not accounting
//       for total loop candidates to align.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;
public class TestClass_Loops
{
    public struct S1
    {
        public struct S1_D1_F1
        {
            public decimal decimal_0;
        }
        public long long_1;
    }
    public struct S2
    {
        public decimal decimal_2;
    }
    static byte s_byte_4 = 1;
    static decimal s_decimal_6 = 5.1428571428571428571428571429m;
    static short s_short_8 = 5;
    static sbyte s_sbyte_11 = -1;
    static ulong s_ulong_16 = 2;
    static S1.S1_D1_F1 s_s1_s1_d1_f1_17 = new S1.S1_D1_F1();
    static S1 s_s1_18 = new S1();
    static S2 s_s2_19 = new S2();
    byte byte_21 = 5;
    decimal decimal_23 = -1.987654320987654320987654321m;
    short short_25 = 0;
    long long_27 = 5;
    float float_29 = -0.9423077f;
    S1 s1_35 = new S1();
    S2 s2_36 = new S2();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public decimal LeafMethod3()
    {
        return 0.5m;
    }

    public float LeafMethod9()
    {
        return 0.5f;
    }

    public uint LeafMethod12()
    {
        return 1;
    }

    public ulong Method18(S2 p_s2_492, ref decimal p_decimal_493, out uint p_uint_494, ref float p_float_495, ref int p_int_496, short p_short_497, out uint p_uint_498, ref S1 p_s1_499)
    {
        p_uint_498 = (uint)10;
        p_uint_494=0;
        return 5;
    }

    public float Method21(ref S1 p_s1_565, ref byte p_byte_566, S2 p_s2_567, ref S1 p_s1_568, ref S1.S1_D1_F1 p_s1_s1_d1_f1_569, S1 p_s1_570, out S1.S1_D1_F1 p_s1_s1_d1_f1_571, ref S2 p_s2_572)
    {
        p_s1_s1_d1_f1_571 = s_s1_s1_d1_f1_17;
        return -4.928571f;
    }

    public S2 Method35(ref S1 p_s1_952, ref sbyte p_sbyte_953, ref S1.S1_D1_F1 p_s1_s1_d1_f1_954)
    {
        unchecked
        {
            S2 s2_971 = new S2();
            return s2_36;
        }
    }
  
    public S1 Method60(ref decimal p_decimal_1639, out float p_float_1640, ref byte p_byte_1641, out S2 p_s2_1642, S1.S1_D1_F1 p_s1_s1_d1_f1_1643, ref S1 p_s1_1644, out S2 p_s2_1645, S2 p_s2_1646, ref ulong p_ulong_1647, out ulong p_ulong_1648, ref S2 p_s2_1649, out S2 p_s2_1650)
    {
        unchecked
        {
            char char_1653 = '5';
            decimal decimal_1654 = 5.0333333333333333333333333333m;
            double double_1655 = 1;
            short short_1656 = 1;
            int int_1657 = -1;
            float float_1660 = 5.090909f;
            uint uint_1663 = 5;
            S1.S1_D1_F1 s1_s1_d1_f1_1665 = new S1.S1_D1_F1();
            S1 s1_1666 = new S1();
            S2 s2_1667 = new S2();
            S2 s2_1668 = s2_1667;
            p_float_1640 = ((float)(((float)(float_1660 /= ((float)((((float)(float_29 - ((float)(LeafMethod9() * float_1660))))) + 89)))) % ((float)((Method21(ref s1_35, ref s_byte_4, s_s2_19, ref s1_35, ref p_s1_s1_d1_f1_1643, s_s1_18, out s_s1_s1_d1_f1_17, ref s2_1667)) + 45))));
            p_s2_1642 = s2_36;
            p_s2_1645 = s_s2_19;
            p_ulong_1648 = Method18(s2_36, ref decimal_23, out uint_1663, ref p_float_1640, ref int_1657, ((short)(short_1656 % ((short)((((short)(short_25 - s_short_8))) + 35)))), out uint_1663, ref s_s1_18);
            p_s2_1650 = s2_36;
            switch (((char)(char_1653 = 'M')))
            {
                case 'Y':
                    {
                        S1 s1_1669 = s_s1_18;
                        break;
                    }
                case 'I':
                    {
                        break;
                    }
                case 'J':
                    {
                        long_27 >>= int_1657;
                        s1_s1_d1_f1_1665.decimal_0 %= ((decimal)((((decimal)(((decimal)(s_s2_19.decimal_2 += ((decimal)(((decimal)(decimal_1654 - -1.9841269841269841269841269841m)) + ((decimal)(s_decimal_6 *= s2_1668.decimal_2)))))) * ((decimal)(s_s2_19.decimal_2 *= ((decimal)(s2_36.decimal_2 += ((decimal)(LeafMethod3() + LeafMethod3()))))))))) + 40));
                        break;
                    }
                default:
                    {
                        Method35(ref s1_35, ref s_sbyte_11, ref s_s1_s1_d1_f1_17);
                        break;
                    }
            }
            return p_s1_1644;
        }
    }

    internal void Method0()
    {
        unchecked
        {
            ulong ulong_2733 = 37;
            S1.S1_D1_F1 s1_s1_d1_f1_2734 = new S1.S1_D1_F1();
            S1 s1_2735 = new S1();
            S1 s1_2736 = s1_2735;
            S2 s2_2737 = new S2();
            s_s1_18 = Method60(ref s2_2737.decimal_2, out float_29, ref byte_21, out s_s2_19, s1_s1_d1_f1_2734, ref s_s1_18, out s_s2_19, s2_2737, ref ulong_2733, out s_ulong_16, ref s2_2737, out s2_2737);
            return;
        }
    }
    [Fact]
    public static int TestEntryPoint()
    {
        new TestClass_Loops().Method0();
        return 100;
    }
}
