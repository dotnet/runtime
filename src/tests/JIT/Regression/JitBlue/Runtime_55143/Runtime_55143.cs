// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
struct S0
{
    public short F0;
    public int F1;
    public S0(short f0, int f1)
    {
        F0 = f0;
        F1 = f1;
    }
}

public class Runtime_55143
{
    [Fact]
    public static int TestEntryPoint()
    {
        int value = M47(-1);
        return value == 0 ? 100 : 101;
    }

    static int M47(short arg0)
    {
        try
        {
            S0 var0 = new S0(arg0++, arg0);
            return var0.F1;
        }
        finally
        {
            arg0++;
        }
    }
}
