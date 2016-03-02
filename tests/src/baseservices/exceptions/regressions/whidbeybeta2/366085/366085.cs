// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

public class MainClass
{	
	static public int Main(String[] args)
	{
		int returnCode = 99;
		
		try
		{
			Convert.FromBase64String(null);
		}
		catch (ArgumentNullException e)
		{
			Console.WriteLine("Caught ArgumentNullException");
            Console.WriteLine(e.Message);
			// the error message may be in the debug pack
			if (((e.Message.IndexOf("Value cannot be null.") >= 0 && e.Message.IndexOf("Parameter name:") >= 0) ||
				(e.Message.IndexOf("[ArgumentNull_Generic]") >= 0)))
				returnCode = 100;			
		}
		catch (Exception e)
		{
			Console.WriteLine("Caught wrong Exception: " + e.Message);
			returnCode = 98;
		}

		if (returnCode == 100)
			Console.WriteLine("Test PASSED");
		else
			Console.WriteLine("Test FAILED");
		return returnCode;
	}

}


