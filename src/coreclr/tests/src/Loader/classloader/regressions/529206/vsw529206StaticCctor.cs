// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// regression test for VSWhidbey 529206 for throwing TypeInitialization inside class .cctor
// We were appending every time the callstack and using the same exception object.
// Now we still use the same object, but callstack is cleared every time.

using System;

class BadInit
{
    static BadInit()
    {
        Console.WriteLine("BadInit.cctor() called.");
        throw new ArgumentException("Hi. I'm the exception thrown by BadInit.cctor()");
    }
}


class Test
{
	public static bool pass;
	
    	public static void foo()
    	{
    		Console.WriteLine(new BadInit());
   	}

    	public static void One()
    	{
    		try
        	{
            		foo();
        	}
       		catch (TypeInitializationException e)
        	{
            		Console.WriteLine(e);
        	}
    	}


    	public static void Two()
    	{
    		try
        	{
            		foo();
        	}
        	catch (TypeInitializationException e)
        	{
            		Console.WriteLine(e);

			// if this string is found in the callstack it means we're appending callstack 
			// instead of having a new one each time.
			if (e.StackTrace.IndexOf("   at Test.One()") != -1)
			{
				Console.WriteLine("2nd time: Incorrect stack trace");
				pass = false;
			}
        	}

    	}


    	public static int Main()
    	{
    		pass = true;
			
    		Console.WriteLine("Loading BadInit the first time...\n");
        	One();

        	Console.WriteLine("\nLoading BadInit the second time...\n");
        	Two();

		if (pass)
		{
			Console.WriteLine("PASS");
			return 100;
		}
		else
		{
			Console.WriteLine("FAIL");
			return 101;
		}
    	}
}
