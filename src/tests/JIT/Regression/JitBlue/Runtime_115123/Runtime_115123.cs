// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Simplified from an Antigen test case

using System;
using System.Runtime.CompilerServices;
using Xunit;

public struct S1_D1_F2
{
    public bool bool_1;
    public bool bool_2;
}

public class Runtime_115123
{
    [Fact]
    public static void Test()
    {
        Problem(1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Problem(int a)
    {
        unchecked
        {
            bool b = false;
            S1_D1_F2 s1_s1_d1_f2_1983 = new S1_D1_F2();
            try
            {
                if (s1_s1_d1_f2_1983.bool_1 = (b = (s1_s1_d1_f2_1983.bool_1 = false) || (a == 0)))
                {
                    SideEffect();
                }
            }
            finally
            {
                switch (a)
                {
                    case 0: SideEffect(); break;
                    case 1: SideEffect(); break;
                    case 2: SideEffect(); break;
                    case 3: SideEffect(); break;
                    case 4: SideEffect(); break;
                    default: SideEffect(); break;
                }
            }

            Log("s1_s1_d1_f", s1_s1_d1_f2_1983);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Log(string varName, object varValue) {}

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void SideEffect() {}
}

