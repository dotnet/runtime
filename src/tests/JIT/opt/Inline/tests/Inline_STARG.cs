// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class MainApp
{
    internal static void Foo_NoInline(string s)
    {
        Console.WriteLine(s);
        s = "New string";
        Console.WriteLine(s);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            string orig = "Original string";
            Console.WriteLine(orig);
            Foo_NoInline(orig);

            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return 666;
        }
    }
}


