// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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