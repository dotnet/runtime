// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.CompilerServices;

class Test 
{
    int countdown;

    Test(int n) 
    {
        countdown = n;
    }

    ~Test()
    {
        if (countdown > 0)
        { 
            new Test(countdown - 1).ToString(); 
        }

        for (int i = 0; i < 1000; i++)
        {
            table.Add(new Object(), new Object());
        }
     }
     

    static ConditionalWeakTable<Object,Object> table = new ConditionalWeakTable<Object,Object>();

    public static int Main() 
    {
        for (int i = 0; i < 10; i++)
        {
            table.Add(new Object(), new Object());
        }

        new Test(5).ToString();

	Console.WriteLine("PASS: Test did not assert");
	return 100;
    }
}
    