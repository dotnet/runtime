// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;


internal class Foo
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

    public static int Main()
    {
        Foo f = new Foo();
        caller(f);

        Console.WriteLine("test passed");
        return 100;
    }
}
