// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

struct S
{
    public Object O1;
    public Object O2;
}

class C
{
#pragma warning disable 0649
    public int i;
#pragma warning restore 0649
    public S s1;

    public C()
    {
        s1.O1 = "Hello";
        s1.O2 = "World";
    }
}

public class Test_test1
{
    [Fact]
    public static void test1()
    {
        C c = new C();

        foo(c.s1);
    }

    [Fact]
    public static void test2()
    {
        C c = new C();
        S s = c.s1;

        foo(s);
    }

    private static void foo(S s)
    {
        Console.WriteLine(s.O1);
        Console.WriteLine(s.O2);
    }
}
