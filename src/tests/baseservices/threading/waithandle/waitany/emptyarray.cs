// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
//Regression for DevDiv Bugs 1142
class Duplicates
{
	static int Main()
	{
		int retCode = 99;
		
		WaitHandle[] waitHandles = {};

		// Can't catch exception in v2.0. Should be fixed in Post-Orcas. VSWhidbey 543816
		try 
		{ 
			Console.WriteLine("Before WaitAny");
			WaitHandle.WaitAny(waitHandles, 5000);
			Console.WriteLine("After WaitAny");
		}
		catch (ArgumentException)
		{
			retCode = 100;
		}
		catch (Exception ex) 
		{ 
			Console.WriteLine("WaitAny threw unexpected Exception."); 
			Console.WriteLine("WaitAny: {0}", ex);
			retCode = 98;			
		}

		if (retCode ==100)
			Console.WriteLine("Test Passed");
		else
			Console.WriteLine("Test Failed");
		
		return retCode;
	}
}
