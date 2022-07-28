// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

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
    public S G()
    {
        S s = new S();

        for (int i = 0; i < 100_000; i++)
        {
            if (!Monitor.IsEntered(this))
            {
                throw new Exception();
            }
            s = z.F();
        }

        return s;
    }

    public static int Main()
    {
        int result = -1;
        try
        {
            result = Test();
        }
        catch (Exception)
        {
            Console.WriteLine("EXCEPTION");
        }

        if (result == 100)
        {
            Console.WriteLine("SUCCESS");
        }
        else
        {
            Console.WriteLine("FAILURE");
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Test()
    {
        var x = new X();
        x.z = new Z();

        for (int i = 0; i < 100; i++)
        {
            _ = x.G();
            Thread.Sleep(15);
        }

        return x.G().x;
    }
}
