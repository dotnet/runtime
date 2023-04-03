// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class Test_b06464
{
    static int[] a = new int[10];

    static int[] A()
    {
        Console.WriteLine("A");
        return a;
    }

    static int F()
    {
        Console.WriteLine("F");
        return 1;
    }

    static int G()
    {
        Console.WriteLine("G");
        return 1;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        A()[F()] = G();
        return 100;
    }
}
