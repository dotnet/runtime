// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This test exposed a bug with the ordering of evaluation of a cpblk.

using Xunit;
struct S0
{
    public long F0;
    public sbyte F4;
    public S0(long f0): this() { F0 = f0; }
}

class C0
{
    public S0 F5;
    public C0(S0 f5) { F5 = f5; }
}

public class GitHub_19243
{
    static C0 s_13 = new C0(new S0(0));
    static S0 s_37;

    public static int checkValue(long value)
    {
        if (value != 8614979244451975600L)
        {
            System.Console.WriteLine("s_37.F0 was " + value + "; expected 8614979244451975600L");
            return -1;
        }
        return 100;
    }

    static ref S0 M7()
    {
        s_13 = new C0(new S0(8614979244451975600L));
        return ref s_37;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        M7() = s_13.F5;
        return checkValue(s_37.F0);
    }
}
