// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for consecutive compare optimization.

using System;
using Xunit;
public class Foo
{
    [Fact]
    public static int TestEntryPoint()
    {
        string s1 = "NonNull";
        string s2 = null;

        if ((s1 == null) == (s2 == null))
        {
            Console.WriteLine("Fail");
            return 1;
        }
        else
        {
            Console.WriteLine("Pass");
            return 100;
        }
    }
}
