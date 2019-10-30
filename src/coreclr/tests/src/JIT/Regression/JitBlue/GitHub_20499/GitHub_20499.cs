// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

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

class X
{
    Z z;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.Synchronized)]
    public S G() => z.F();

    public static int Main()
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
