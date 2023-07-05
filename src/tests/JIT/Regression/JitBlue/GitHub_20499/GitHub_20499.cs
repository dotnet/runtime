// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

struct S
{
    public long y;
    public int x;
}

class Z
{
    virtual public S F()
    {
        S s = new S();
        s.x = 100;
        s.y = -1;
        return s;
    }

}

public class X
{
    Z z;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.Synchronized)]
    S G() => z.F();

    [Fact]
    public static int TestEntryPoint()
    {
        int result = Test();
        if (result == 100) {
            Console.WriteLine("SUCCESS");
        }
        else {
            Console.WriteLine("FAILURE");
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Test()
    {
        var x = new X();
        x.z = new Z();
        return x.G().x;
    }
}
