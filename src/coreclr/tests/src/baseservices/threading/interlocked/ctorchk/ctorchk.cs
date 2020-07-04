// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Reflection;

class TestMonitor
{
    public static int Main()
    {
        // This will look for any private constructor.  
        // There shouldn't be one in Whidbey.
        ConstructorInfo[] m = typeof(Interlocked).GetConstructors(
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        Console.WriteLine(m.Length);

        if(m.Length > 0)
	{
            Console.WriteLine("Test FAILED!");
            return 1;
	}
	else
        {
            Console.WriteLine("Test PASSED!");
            return 100;
        }
    }
}
