// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT licens

// Found by Antigen
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;
public class ShortEnregisteredLocal
{
    public struct S2
    {
        public short short_1;
    }
    static byte s_byte_3 = 4;
    static int s_int_8 = -1;
    static float s_float_11 = -5f;
    static S2 s_s2_17 = new S2();
    int int_24 = 5;
    float float_27 = 5.1025643f;
    S2 s2_33 = new S2();
    static int s_loopInvariant = 0;
    public byte LeafMethod1()
    {
        unchecked
        {
            return 15%4;
        }
    }
    public float LeafMethod9()
    {
        unchecked
        {
            return float_27;
        }
    }
    public float Method1(ref float p_float_34, out S2 p_s2_35, byte p_byte_36, S2 p_s2_37, ref int p_int_38)
    {
        unchecked
        {
            uint uint_51 = 5;
            S2 s2_54 = new S2();
            S2 s2_55 = s2_54;
            p_s2_35 = s2_33;
            for (int __loopvar4 = s_loopInvariant; s_int_8 < s_int_8; __loopvar4 += 3, uint_51 &= 15/4)
{}                                                            Log("s2_55", s2_55);
            return float_27 /= 15+4;
        }
    }
    internal void Method0()
    {
        unchecked
        {
            S2 s2_137 = new S2();
            do
            {
            }
            while (Method1(ref float_27, out s_s2_17, s_byte_3, s2_137, ref int_24) - LeafMethod9()== (s_float_11 *= 15+4)/ Method1(ref float_27, out s2_33, LeafMethod1(), s2_33, ref s_int_8)+ 21);
            return;
        }
    }
    [Fact]
    public static int TestEntryPoint()
    {
        new ShortEnregisteredLocal().Method0();
        return 100;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Log(string varName, object varValue)
    {
    }
}
