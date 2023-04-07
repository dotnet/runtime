// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class T
{
    [Fact]
    public static int TestEntryPoint()
    {
        string s1 = "Hello World";

        foo(1, 2, 3, 4, 5, 6, 7, 8, s1);

        return 100;
    }

    internal static void foo(int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, string s9)
    {
        Console.WriteLine(s9);
    }
}
