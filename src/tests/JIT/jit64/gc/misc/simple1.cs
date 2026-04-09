// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

struct S
{
    public Object O;
}

public class test
{
    [Fact]
    public static int TestEntryPoint()
    {
        S s = new S();

        s.O = "Hello World";

        S s2 = foo(s);

        Console.WriteLine(s2.O);

#pragma warning disable 0252
        if (s2.O != "Goodbye World") return -1;
        return 100;
#pragma warning restore 0252

    }

    private static S foo(S s)
    {
        Console.WriteLine(s.O);

        s.O = "Goodbye World";

        return s;
    }
}
