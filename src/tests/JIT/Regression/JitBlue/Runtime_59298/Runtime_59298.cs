using System.Runtime.CompilerServices;
using Xunit;
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Note: In below test case, we do not iterate over BBJ_ALWAYS blocks while computing the
//       reachability leading to assert
public class Runtime_59298
{
    public struct S2
    {
        public struct S2_D1_F1
        {
            public double double_1;
        }
    }
    static int s_int_6 = -2;
    static S2 s_s2_16 = new S2();
    int int_6 = -2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int LeafMethod6()
    {
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public S2 Method0(out short p_short_1)
    {
        long long_7 = -1;
        p_short_1 = 0;
        switch (long_7)
        {
            case -5:
                {
                    do
                    {
                        try
                        {
                            int_6 ^= int_6;
                        }
                        finally
                        {
                            // The expression doesn't matter, it just has to be long enough
                            // to have few extra blocks which we don't walk when doing inverse
                            // post order while computing dominance information.
                            long_7 &= long_7;
                            int_6 &= (int_6 /= (int_6 -= LeafMethod6() - int_6) + 69) / ((int_6 << (int_6 - int_6)) + (int_6 |= LeafMethod6()) + (LeafMethod6() >> s_int_6) + 62);
                        }
                    }
                    while (long_7 == 8);
                    break;
                }
            default:
                {
                    break;
                }
        }
        return s_s2_16;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        new Runtime_59298().Method0(out short s);
        return s + 100;
    }
}