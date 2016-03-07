// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

public class Test
{

	public static int Main()
	{
                Console.WriteLine("Start");
		int retVal = 100;
		string STORAGE = "OLD";
		string NOW = "NOW";
                string ret = "";

		Console.WriteLine("ref loc: " + STORAGE);
		Console.WriteLine("Return: " + ret);
		                
		Console.WriteLine("Echanging in:" + NOW);
                
		string OLDSTORAGE = STORAGE;
		ret = Interlocked.Exchange<string>(ref STORAGE,NOW); 

		Console.WriteLine("ref loc: " + STORAGE);
		Console.WriteLine("Return: " + ret);
		
		//if(ret == "" || STORAGE != NOW)
		if(ret != OLDSTORAGE || STORAGE != NOW)
			retVal = -1;

		if (retVal == 100)
			Console.WriteLine("Test passed");
		else
			Console.WriteLine("Test failed");

		return retVal;
		
	}
}
