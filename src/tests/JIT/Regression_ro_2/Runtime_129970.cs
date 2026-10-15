// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_129970;

public class C
{
    public int F1;
    public int F2;
    public int F3;
}

public static class Runtime_129970
{
    public static C s_obj = new C();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int NeverTwo() => 99;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int M(C c, int x)
    {
        try
        {
            c.F1 = x;
        }
        finally
        {
            switch (NeverTwo())
            {
                case 0:
                    c.F1 = 1;
                    break;
                case 1:
                    c.F1 = 2;
                    break;
                case 2:
                    try
                    {
                        c.F1 = 3;
                    }
                    finally
                    {
                        try
                        {
                            c.F2 = 4;
                        }
                        finally
                        {
                            c.F3 = 5;
                        }
                    }
                    break;
                default:
                    c.F1 = 6;
                    break;
            }
        }

        return x + 1;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return M(s_obj, 99) == 100 ? 100 : -1;
    }
}


