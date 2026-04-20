// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;
using TestLibrary;

class Foo<T>
{
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    internal bool M()
    {
        return GenClass<T>.StaticField;
    }
}

public class Program
{
    [ActiveIssue("These tests are not supposed to be run with mono.", TestRuntimes.Mono)]
    [Fact]
    public static int TestEntryPoint()
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
