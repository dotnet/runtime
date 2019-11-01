// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Runtime.CompilerServices;

class Foo<T>
{
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    internal bool M()
    {
        return GenClass<T>.StaticField;
    }
}

class Program
{
    static int Main()
    {
        try
        {
            if (!new Foo<string>().M())
            {
                Console.WriteLine("FAIL - bad result");
                return 102;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("FAIL - exception caught");
            return 101;
        }

        Console.WriteLine("PASS");
        return 100;
    }
}
