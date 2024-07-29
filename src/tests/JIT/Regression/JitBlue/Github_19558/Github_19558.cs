// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
struct S0
{
    public uint F1;
    public long F2;
    public ulong F3;
    public ushort F5;
    public bool F7;
    public S0(uint f1) : this()
    {
        F1 = f1;
    }
}

public class Program
{
    static S0[] s_7 = new S0[] { new S0(0) };

    [Fact]
    public static int TestEntryPoint()
    {
        return (M9(0) == -1) ? 100 : 1;
    }

    static short M9(short arg1)
    {
        long var0 = 0;
        try
        {
            System.GC.KeepAlive(var0);
        }
        finally
        {
            var vr12 = new ulong[] { 0, 2271009908085114245UL };
            S0[] vr18 = new S0[] { new S0(32768) };
            uint vr19 = vr18[0].F1;
            arg1 = (short)vr19;
            arg1 %= -32767;
            System.GC.KeepAlive(s_7[0]);
            System.GC.KeepAlive(s_7[0]);
        }

        return arg1;
    }
}
