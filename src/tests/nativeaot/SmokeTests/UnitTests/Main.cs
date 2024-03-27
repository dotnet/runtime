// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        bool success = RunTest(BasicThreading.Run);
        success &= RunTest(Delegates.Run);
        success &= RunTest(Generics.Run);
        success &= RunTest(Interfaces.Run);
        success &= RunTest(Threading.Run);
        success &= RunTest(Devirtualization.Run);
        success &= RunTest(StackTraces.Run);
        return success ? 100 : 1;
    }

    static bool RunTest(Func<int> t, [CallerArgumentExpression("t")] string name = null)
    {
        Console.WriteLine($"===== Running test {name} =====");
        bool success = true;
        try
        {
            success = t() == 100;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            success = false;
        }
        Console.WriteLine($"===== Test {name} {(success ? "succeeded" : "failed")} =====");
        return success;
    }
}
