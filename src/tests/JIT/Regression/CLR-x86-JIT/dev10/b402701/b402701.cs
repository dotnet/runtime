// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security;
using Xunit;


public class Foo
{
    public virtual void callee()
    {
        Console.WriteLine("callee");
    }

    public static void caller(object o)
    {
        if (o == null)
            return;
        if (o.GetType() == typeof(Foo))
        {
            ((Foo)o).callee();
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Foo f = new Foo();
        caller(f);

        Console.WriteLine("test passed");
        return 100;
    }
}
