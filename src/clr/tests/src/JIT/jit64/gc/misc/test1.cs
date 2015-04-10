// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

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

class Test
{
    public static int Main()
    {
        test1();
        test2();
        return (100);
    }

    public static void test1()
    {
        C c = new C();

        foo(c.s1);
    }

    public static void test2()
    {
        C c = new C();
        S s = c.s1;

        foo(s);
    }

    public static void foo(S s)
    {
        Console.WriteLine(s.O1);
        Console.WriteLine(s.O2);
    }
}
