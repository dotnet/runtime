// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;

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

class Test
{
    public static int Main()
    {
        test1();
        test2();
        return 100;
    }

    public static void test1()
    {
        C c = new C();

        foo(C.s1);
    }

    public static void test2()
    {
        C c = new C();
        S s = C.s1;

        foo(s);
    }

    public static void foo(S s)
    {
        Console.WriteLine(s.O1);
        Console.WriteLine(s.O2);
    }
}
