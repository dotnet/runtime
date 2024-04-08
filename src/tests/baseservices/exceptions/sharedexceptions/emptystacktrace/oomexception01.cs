// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Diagnostics;
using Xunit;

public class SharedExceptions
{
    public int retVal =0;

    [Fact]
    public static int TestEntryPoint()
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

            string oomStack = e.StackTrace;
            string expectedStack = currStack;

            if (oomStack.IndexOf(':') != -1)
            {
                oomStack = oomStack.Substring(0, oomStack.IndexOf(':') - 1);
            }

            if (expectedStack.IndexOf(':') != -1)
            {
                expectedStack = expectedStack.Substring(0, expectedStack.IndexOf(':') - 1);
            }

            if (oomStack != expectedStack)
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

