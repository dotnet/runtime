// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class Test_constrained1
{
    internal static void M<T>(T t)
    {
        System.Type type = t.GetType();
        Console.WriteLine(type);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        M("Hello"); // Works fine
        M(3); // CLR crashes
        return 100;
    }
}
