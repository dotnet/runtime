// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

class C0
{
    public sbyte F4;
}

#pragma warning disable 0649

struct S1
{
    public C0 F0;
    public bool F8;
    public int F9;
}

public class Runtime_36468
{
    static S1 s_3;
    [Fact]
    public static int TestEntryPoint()
    {
        int result = -1;
        try
        {
            M0();
        }
        catch (NullReferenceException)
        {
            result = 100;
        }

        return result;
    }

    static void M0()
    {
        ulong var0 = 0;
        try
        {
            if (M2(ref s_3, s_3))
            {
                var0 -= 0;
            }
        }
        finally
        {
            int var1 = s_3.F9;
        }
    }

    static bool M2(ref S1 arg0, S1 arg1)
    {
        sbyte var0 = arg1.F0.F4--;
        return arg0.F8;
    }
}
