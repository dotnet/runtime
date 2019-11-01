// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;

struct S
{
    public Object O1;
    public Object O2;
    public Object O3;
    public Object O4;
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
        s1.O3 = "Have";
        s1.O4 = "Fun";
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
        Console.WriteLine(s.O3);
        Console.WriteLine(s.O4);
    }
}
