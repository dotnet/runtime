// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;

class Foo<T>
{
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    internal void M()
    {
        new GenClass<T>();
    }
}

class Program
{
    static int Main()
    {
        try
        {
            new Foo<string>().M();
        }
        catch (Exception e)
        {
            Console.WriteLine("FAIL");
            return 101;
        }

        Console.WriteLine("PASS");
        return 100;
    }
}
