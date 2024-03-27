// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// regression test for VSWhidbey 529206 for throwing TypeInitialization inside module .cctor
// We were appending every time the callstack and using the same exception object.
// Now we still use the same object, but callstack is cleared every time.


using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Test_vsw529206ModuleCctor
{
	public static bool pass;
    	
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void RunTest()
	{
		// TRIGGER: static field access, ref type
		TriggerModuleCctorClass.intStatic = 5;
	}


	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void RunTest2()
	{
		// TRIGGER: static field access, ref type
		TriggerModuleCctorClass.intStatic = 5;
	}

    	[Fact]
    	public static int TestEntryPoint()
    	{
    		pass = true;
			
    		try
		{
			RunTest();
		
			Console.WriteLine("Did not catch expected TypeInitializationException exception");
			pass = false;
		}
		catch (TypeInitializationException e)
		{
			Console.WriteLine("Caught expected exception 1st time\n" + e);
			
		}
		catch (Exception e)
		{
			Console.WriteLine("Caught unexpected exception 1st time: " + e);
			pass = false;
		}

		
		try
		{
			RunTest2();
			
			Console.WriteLine("Did not catch expected TypeInitializationException exception");
			pass = false;
		}
		catch (TypeInitializationException e )
		{
			Console.WriteLine("Caught expected exception 2nd time\n" + e);
		
			// if this string is found in the callstack it means we're appending callstack 
			// instead of having a new one each time.
			if (e.StackTrace.IndexOf("at Test.RunTest()") != -1)
			{
				Console.WriteLine("2nd time: Incorrect stack trace");
				pass = false;
			}

		}
		catch (Exception e)
		{
			Console.WriteLine("Caught unexpected exception 2nd time: " + e);
			pass = false;

		}

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
