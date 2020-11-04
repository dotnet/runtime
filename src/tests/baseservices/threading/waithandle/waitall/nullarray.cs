// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
//Regression for DevDiv Bugs 1142
class Duplicates
{
	static int Main()
	{
		int retCode = 99;

        // Console.WriteLine(Thread.CurrentThread.GetApartmentState());

        // Not supported (https://github.com/dotnet/runtime/issues/5059)
        // AppDomain.CurrentDomain.UnhandledException += Unhandled;

        WaitHandle[] waitHandles = null;

		// Can't catch exception in v2.0. Should be fixed in Post-Orcas. VSWhidbey 543816
		try 
		{ 
			Console.WriteLine("Before WaitAll");
			WaitHandle.WaitAll(waitHandles, 5000);//, false);
			Console.WriteLine("After WaitAll");
		}
		catch (ArgumentNullException)
		{
			retCode = 100;
		}
		catch (Exception ex) 
		{ 
			Console.WriteLine("WaitAll threw unexpected Exception."); 
			Console.WriteLine("WaitAll: {0}", ex);
			retCode = 98;			
		}

		if (retCode ==100)
			Console.WriteLine("Test Passed");
		else
			Console.WriteLine("Test Failed");
		
		return retCode;
	}

	// private static void Unhandled(object sender, UnhandledExceptionEventArgs args)
	// {
	// 	Exception e = (Exception) args.ExceptionObject;
	// 	Console.WriteLine("Unhandled reports: {0}", e);
	// }
}
