// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Test_test448035 
{
    int countdown;

    Test_test448035(int n) 
    {
        countdown = n;
    }

    ~Test_test448035()
    {
        if (countdown > 0)
        { 
            new Test_test448035(countdown - 1).ToString(); 
        }

        for (int i = 0; i < 1000; i++)
        {
            table.Add(new Object(), new Object());
        }
     }
     

    static ConditionalWeakTable<Object,Object> table = new ConditionalWeakTable<Object,Object>();

    [Fact]
    public static void TestEntryPoint()
    {
        for (int i = 0; i < 10; i++)
        {
            table.Add(new Object(), new Object());
        }

        new Test_test448035(5).ToString();

        Console.WriteLine("PASS: Test did not assert");
    }
}
