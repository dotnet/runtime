// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

public class Test{

	public static int Main()
	{
		int rValue = 0;		
		Object foo = new Object();
		
		try{
			Monitor.Enter(foo);
			Monitor.Exit(foo);			
		}
		catch
		{
			return -1;
		}		

		try{
			Monitor.Exit(foo);
			rValue = -1;
		}
		catch(SynchronizationLockException)
		{
			rValue = 100;
		}
		Console.WriteLine(100 == rValue ? "Test Passed":"Test Failed");
		return rValue;
	}
}