// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//Unit test for null check assertion.

using System;
using Xunit;

public class Sample3
{
    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void func(object o)
    {
        if (o == null)
            throw new Exception();
        o.GetType();
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            func(new Object());
            Console.WriteLine("Passed");
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return 666;
        }
    }
}
