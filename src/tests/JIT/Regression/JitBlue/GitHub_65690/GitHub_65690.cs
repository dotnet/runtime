// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Note: In below test case, there are two backedges to a loop that is marked for align.
//       If we see intersecting aligned loops, we mark that loop as not needing alignment
//       but then when we see 2nd backedge we again try to mark it as not needing alignment
//       and that triggers an assert.
// Found by Antigen
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;
public class TestClass_65690
{
    public struct S1
    {
        public struct S1_D1_F1
        {
            public decimal decimal_0;
            public bool bool_1;
            public sbyte sbyte_2;
        }
        public double double_3;
    }
    public struct S2
    {
        public S1.S1_D1_F1 s1_s1_d1_f1_5;
    }
    static byte s_byte_7 = 2;
    static decimal s_decimal_9 = -1.9647058823529411764705882353m;
    static int s_int_12 = 1;
    static long s_long_13 = -2147483647;
    static ulong s_ulong_19 = 1;
    static S1.S1_D1_F1 s_s1_s1_d1_f1_20 = new S1.S1_D1_F1();
    bool bool_23 = true;
    byte byte_24 = 5;
    int int_29 = 1;
    ushort ushort_34 = 5;
    uint uint_35 = 5;
    S1.S1_D1_F1 s1_s1_d1_f1_37 = new S1.S1_D1_F1();
    S1 s1_38 = new S1();
    S2 s2_39 = new S2();
    static int s_loopInvariant = 4;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public byte LeafMethod1()
    {
        unchecked
        {
            return 15 + 4;
        }
    }
 
    public double Method3(out sbyte p_sbyte_93, out double p_double_94, out S1.S1_D1_F1 p_s1_s1_d1_f1_98)
    {
        unchecked
        {
            p_double_94 = 15 + 4;
            p_s1_s1_d1_f1_98 = new S1.S1_D1_F1();
            p_sbyte_93 = 15 % 4;
            return 15 + 4;
        }
    }

    internal void Method0()
    {
        unchecked
        {
            int int_145 = -5;
            ulong ulong_152 = 5;
            S1.S1_D1_F1 s1_s1_d1_f1_153 = new S1.S1_D1_F1();
            S2 s2_155 = new S2();
            if (15 - 4 != LeafMethod1())
            {
            }
            else
            {
                try
                {
                    ulong_152 >>= s_int_12 = int_145 -= 15 + 4;
                }
                finally
                {
                    s2_39.s1_s1_d1_f1_5.decimal_0 = 15 % 4 + s1_s1_d1_f1_153.decimal_0 - 15 + 4 + 40;
                }
                int __loopvar25 = 15 + 4;
                do
                {
                    if (__loopvar25 < s_loopInvariant - 1)
                        break;
                }
                while (s1_s1_d1_f1_37.bool_1 = s1_s1_d1_f1_37.bool_1 = bool_23);
            }
            if (15 / 4 % ulong_152 + 22 + 78 - s_ulong_19 > 19)
            {
                int __loopvar26 = 15 - 4;
                for (; ; )
                {
                    if (__loopvar26 >= s_loopInvariant)
                        break;
                    if (s1_s1_d1_f1_37.bool_1)
                    {
                        Method3(out s2_155.s1_s1_d1_f1_5.sbyte_2, out s1_38.double_3, out s1_s1_d1_f1_153);
                    }
                    else
                    {
                        ushort_34 *= 15 % 4;
                    }
                    if ((byte_24 = s_byte_7 ^= 15 | 4) >= (15 | 4))
                    {
                    }
                }
            }
            return;
        }
    }
    [Fact]
    public static int TestEntryPoint()
    {
        new TestClass_65690().Method0();
        return 100;
    }
}
