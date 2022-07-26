// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

public class Runtime_72775
{
    public static int Main()
    {
        for (int i = 0; i < 100; i++)
        {
            Call(new Impl1());
            if (i > 30 && i < 40)
                Thread.Sleep(10);
        }

        // With GDV, JIT would optimize Call by fully removing the box since Impl1.Foo does not use it.
        // This would cause null to be passed to Impl2.Foo.
        return Call(new Impl2());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Call(I i) => i.Foo(5);
}

public interface I
{
    int Foo(object o);
}

class Impl1 : I
{
    public int Foo(object o) => 0;
}

class Impl2 : I
{
    public int Foo(object o)
    {
        if (o is not int i || i != 5)
        {
            Console.WriteLine("FAIL: Got {0}", o?.ToString() ?? "(null)");
            return -1;
        }
        else
        {
            Console.WriteLine("PASS: Got 5");
            return 100;
        }
    }
}
