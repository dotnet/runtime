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
    internal void M()
    {
        new GenClass<T>();
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
