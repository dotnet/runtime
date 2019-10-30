// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Diagnostics;

public class SharedExceptions
{
    public int retVal =0;

    public static int Main()
    {
        Console.WriteLine("Test that StackTrace for OOM is proper if memory is available");
        SharedExceptions test = new SharedExceptions();
        test.RunTest();
        Console.WriteLine(100 == test.retVal ? "Test Passed":"Test Failed");
        return test.retVal;
    }

    public void RunTest()
    {
        CreateAndThrow();        
    }

    public void CreateAndThrow()
    {
    	string currStack;
		
        try
        {
        	throw new Exception();
        }
	catch(Exception e)
	{
		currStack = e.StackTrace;
	}
	
        try
        {            
            Guid[] g = new Guid[Int32.MaxValue];
        }
        catch(OutOfMemoryException e)
        {
            retVal = 100;
			
            Console.WriteLine("Caught OOM");     

            if(e.StackTrace.ToString().Substring(0, e.StackTrace.Length - 8) != currStack.Substring(0, currStack.Length - 8))
            {	
            	Console.WriteLine("Actual Exception Stack Trace:");
                Console.WriteLine(e.StackTrace);
                Console.WriteLine();				
            	Console.WriteLine("Expected Stack Trace:");
		        Console.WriteLine(currStack.ToString());
                retVal = 50;
            }
        }
            
    }

}

