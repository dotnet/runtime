// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Xunit;

/// <summary>
/// Regression test case for Dev10 897464 bug: Assemblies that contain global values will work with .NET 2.0  but fail with a BadImageFormat exception on .NET 4.0
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
            var v = Test.MyEnum.Zero;
            Console.WriteLine(v);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Got unexpected error: " + ex);
            return false;
        }

        return true;
    }
}
