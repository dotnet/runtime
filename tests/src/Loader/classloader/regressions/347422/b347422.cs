// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// this is regression for VSW 347422
// the issue here was that the exception message was incorrect.

// Expected: System.InvalidCastException: Unable to cast object of type 'System.Object' to type 'IFoo'.
// Actual: System.InvalidCastException: Unable to cast object of type 'IFoo' to type 'System.Object'.


using System;

public interface IFoo{}

public class Casting
{
	public static int Main()
	{
            Object obj = new Object();
            try
            {
            		IFoo f = (IFoo) obj;

			Console.WriteLine("FAIL: Did not catch expected InvalidCastException");
			return 101;
		
            }
            catch(InvalidCastException e)
            {
            		string msg ="Unable to cast object of type 'System.Object' to type 'IFoo'.";
				
                	
			if (e.Message.Equals(msg) || e.Message.Contains("Debugging resource strings are unavailable"))
			{
				Console.WriteLine("PASS");
				return 100;
			}
			else
			{
				Console.WriteLine("FAIL: Caught expected exception, but error message is incorrect");
				Console.WriteLine("Expected: " + msg);
				Console.WriteLine("Actual: " + e.Message);

				return 102;
			}
            }
	}
}
