// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

struct S
{
    public Object O1;
    public Object O2;
    public Object O3;
    public Object O4;
    public Object O5;
    public Object O6;
    public Object O7;
    public Object O8;
    public Object O9;
    public Object O10;
    public Object O11;
}

public class Test_test3
{
    [Fact]
    public static int TestEntryPoint()
    {
        S s = new S();

        s.O1 = "Hello";
        s.O2 = "World";
        s.O3 = "obj3";
        s.O4 = "obj4";
        s.O5 = "obj5";
        s.O6 = "obj6";
        s.O7 = "obj7";
        s.O8 = "obj8";
        s.O9 = "obj9";
        s.O10 = "obj10";
        s.O11 = "obj11";

        test(s);
        return (100);
    }

    private static void test(S s)
    {
        Console.WriteLine(s.O1);
        Console.WriteLine(s.O2);
        Console.WriteLine(s.O3);
        Console.WriteLine(s.O4);
        Console.WriteLine(s.O5);
        Console.WriteLine(s.O6);
        Console.WriteLine(s.O7);
        Console.WriteLine(s.O8);
        Console.WriteLine(s.O9);
        Console.WriteLine(s.O10);
        Console.WriteLine(s.O11);
    }
}
