// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 * This is a potential security exploit. Variance allows a sealed type to be cast to/from another sealed type that is neither it's base class or derived class (which to the JIT makes it look like interfaces or other unsealed types).
 */

using System;
using Xunit;

namespace Test_sealedCastVariance_cs
{
public static class Repro
{
    private static bool CheckType(Action<string> a)
    {
        return a.GetType() == typeof(Action<object>);
    }
    [Fact]
    public static int TestEntryPoint()
    {
        Action<string> a = (Action<object>)Console.WriteLine;
        if (CheckType(a))
        {
            Console.WriteLine("pass");
            return 100;
        }
        Console.WriteLine("FAIL");
        return 101;
    }
}
}
