// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_62108
{
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            new Runtime_62108().LeafMethod6();
        }
        catch (DivideByZeroException)
        {
            return 100;
        }

        return 101;
    }

    public struct S1
    {
        public struct S1_D1_F1
        {
            public double double_2;
        }
        public int int_4;
    }

    static S1 s_s1_23 = new S1();
    static int s_int_14 = 2;
    
    S1 s1_40 = new S1();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int LeafMethod6()
    {
        return s_s1_23.int_4 / 15 + 4 << (s_int_14 |= s_int_14 / (s_s1_23.int_4 += s1_40.int_4) + 41);
    }
}
