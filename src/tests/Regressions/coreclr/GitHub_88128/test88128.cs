// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using Xunit;

public class test88113
{
    [Fact]
    public static void TestEntryPoint()
    {
        try
        {
            throw new Exception("boo");
        }
        catch (Exception ex2)
        {
            Console.WriteLine($"1: {ex2}");
            try
            {
                throw;
            }
            catch (Exception ex3)
            {
                Console.WriteLine($"2: {ex3}");
            }
        }
    }
}
