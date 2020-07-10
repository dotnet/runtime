// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

class Duplicates
{
	static int Main()
	{
		int retCode = 99;
		
		WaitHandle[] waitHandles = { new Mutex(), new Mutex(), null, new Mutex() };

		// Can't catch exception in v2.0. Should be fixed in Post-Orcas. VSWhidbey 543816
		try 
		{ 
			Console.WriteLine("Before WaitAll");
			WaitHandle.WaitAll(waitHandles, 5000);
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
}
