// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Xunit;

/// <summary>
/// Regression test case for Dev10 851479 bug: Stackoverflow in .NET when using self referencing generics along with type constraints to another type parameter.
/// </summary>
public class Program
{
    [Fact]
    public static void TestEntryPoint()
    {
        Program p = new Program();

        Assert.True(p.Run());
    }

    public Boolean Run()
    {
        try
        {
            var B = new B();
            System.Console.WriteLine(B);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Got unexpected error: " + ex);
            return false;
        }

        return true;
    }
}

class A<T, U>
    where T : U
    where U : A<T, U> { }

class B : A<B, B>
{
}
