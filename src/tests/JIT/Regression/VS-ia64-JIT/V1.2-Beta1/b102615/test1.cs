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
    public int i;
    public static S s1;

    public C()
    {
        s1.O1 = "Hello";
        s1.O2 = "World";
    }
}

public class Test_test1
{
    [Fact]
    public static int TestEntryPoint()
    {
        test1();
        test2();
        return 100;
    }

    internal static void test1()
    {
        C c = new C();

        foo(C.s1);
    }

    internal static void test2()
    {
        C c = new C();
        S s = C.s1;

        foo(s);
    }

    static void foo(S s)
    {
        Console.WriteLine(s.O1);
        Console.WriteLine(s.O2);
    }
}
