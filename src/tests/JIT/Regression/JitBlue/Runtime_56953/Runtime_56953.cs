// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
public class TestClass13
{
    // The test exposed a place where we were using uninitialized `gtUseNum` variable.
    public struct S1
    {
        public byte byte_1;
    }
    public struct S2
    {
    }
    public struct S3
    {
    }
    public struct S5
    {
        public struct S5_D1_F2
        {
            public S1 s1_1;
            public ushort uint16_2;
        }
    }
    static ushort s_uint16_11 = 19036;
    static S2 s_s2_15 = new S2();
    static S3 s_s3_16 = new S3();
    static S5.S5_D1_F2 s_s5_s5_d1_f2_18 = new S5.S5_D1_F2();
    static S5 s_s5_19 = new S5();
    static int s_loopInvariant = 1;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public ushort LeafMethod11()
    {
        unchecked
        {
            return s_s5_s5_d1_f2_18.uint16_2 <<= 15 | 4;
        }
    }
    public S3 Method4(ref S2 p_s2_0, S5.S5_D1_F2 p_s5_s5_d1_f2_1, S5.S5_D1_F2 p_s5_s5_d1_f2_2, ushort p_uint16_3, ref S5 p_s5_4, S5.S5_D1_F2 p_s5_s5_d1_f2_5, short p_int16_6)
    {
        unchecked
        {
            {
            }
            return s_s3_16;
        }
    }
    public void Method0()
    {
        unchecked
        {
            if ((s_uint16_11 %= s_s5_s5_d1_f2_18.uint16_2 <<= 15 | 4) < 15 + 4 - LeafMethod11())
            { }
            else
            { }
            { }
            s_s3_16 = Method4(ref s_s2_15, s_s5_s5_d1_f2_18, s_s5_s5_d1_f2_18, LeafMethod11(), ref s_s5_19, s_s5_s5_d1_f2_18, 11592);
            return;
        }
    }
    public static int Main(string[] args)
    {
        try
        {
          TestClass13 objTestClass13 = new TestClass13();
          objTestClass13.Method0();
        }
        catch(Exception)
        {
            // ignore exceptions
        }
        return 100;
    }
}