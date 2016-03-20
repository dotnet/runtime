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
                Console.WriteLine("Comparand:" + STORAGE);
		Console.WriteLine("Attempting Exchange in:" + NOW);
 
                string OLDSTORAGE = STORAGE;
		ret = Interlocked.CompareExchange<string>(ref STORAGE,NOW,STORAGE); 

		Console.WriteLine("ref loc: " + STORAGE);
		Console.WriteLine("Return: " + ret);

		//if(ret == "" || STORAGE != NOW)
		if(ret != OLDSTORAGE || STORAGE != NOW)
			retVal = -1;
          
                Console.WriteLine(100 == retVal ? "Test Passed":"Test Failed");


                STORAGE = "OLD";
		NOW = "NOW";
                ret = "";      

                Console.WriteLine("==================================");
                Console.WriteLine("ref loc: " + STORAGE);
		Console.WriteLine("Return: " + ret);
		Console.WriteLine("Comparand:" + NOW);                
		Console.WriteLine("Attempting Exchange in:" + NOW);
                
                ret = Interlocked.CompareExchange<string>(ref STORAGE,NOW,NOW); 

		Console.WriteLine("ref loc: " + STORAGE);
		Console.WriteLine("Return: " + ret);
		if(ret != "OLD" || STORAGE != "OLD")
			retVal = -1;

                Console.WriteLine(100 == retVal ? "Test Passed":"Test Failed");
		return retVal;
	}
}
