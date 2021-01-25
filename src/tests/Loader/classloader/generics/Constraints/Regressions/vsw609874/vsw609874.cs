// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApplication3
{
    class Program
    {
        static int Main(string[] args)
        {
	    try{
	            Repro<Program>(null);
	    }
	    catch (Exception e)
	    {
		Console.WriteLine(e.Message);
		Console.WriteLine("FAIL");
		return 99;
	    }
	    Console.WriteLine("PASS");
	    return 100;
        }

        static void Repro<T>(B<T> b)
            where T : Program
        {
        }

    }

    class A<T> { }
    class B<T> where T : class { }
}
